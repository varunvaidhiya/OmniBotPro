"""The post-training learning loop:

    collect → annotate rewards → evaluate → store → train → eval policy → repeat

Every stage is a pluggable component injected through ``LoopComponents``;
the loop itself contains *no* algorithm — only orchestration, bookkeeping
and failure isolation (a judge outage must not lose collected episodes,
so storage happens before training and evaluation errors degrade to the
heuristic evaluator).
"""

from __future__ import annotations

import json
import logging
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence

from ..core.interfaces import DataCollector, EpisodeEvaluator, Policy, PolicyTrainer
from ..core.types import Episode, EvaluationReport, TaskOutcome, TrainResult
from ..data.replay_dataset import ReplayDataset
from ..evaluation.self_eval import HeuristicSelfEvaluator
from ..replay.buffer import EpisodicReplayStore
from ..rewards.engine import RewardEngine

log = logging.getLogger("learning_engine.loop")


@dataclass
class LoopComponents:
    collectors: Sequence[DataCollector]
    dataset: ReplayDataset
    reward_engine: RewardEngine
    trainer: PolicyTrainer
    policy: Optional[Policy] = None
    evaluators: Sequence[EpisodeEvaluator] = field(default_factory=list)
    replay_store: Optional[EpisodicReplayStore] = None
    eval_collector: Optional[DataCollector] = None  # held-out policy evaluation


@dataclass
class IterationReport:
    iteration: int
    episodes_collected: int = 0
    outcomes: Dict[str, int] = field(default_factory=dict)
    train_result: Optional[TrainResult] = None
    eval_success_rate: float = float("nan")
    duration_s: float = 0.0
    failure_analyses: List[str] = field(default_factory=list)

    def to_dict(self) -> Dict[str, Any]:
        d = {
            "iteration": self.iteration,
            "episodes_collected": self.episodes_collected,
            "outcomes": self.outcomes,
            "eval_success_rate": self.eval_success_rate,
            "duration_s": round(self.duration_s, 2),
            "failure_analyses": self.failure_analyses,
        }
        if self.train_result:
            d["train"] = {
                "trainer": self.train_result.trainer,
                "steps": self.train_result.steps,
                "final_loss": self.train_result.final_loss,
                "checkpoint": self.train_result.checkpoint_path,
                **self.train_result.metrics,
            }
        return d


class PostTrainingLoop:
    def __init__(
        self,
        components: LoopComponents,
        collect_per_iteration: int = 10,
        train_episodes_per_iteration: int = 50,
        eval_episodes: int = 5,
        report_dir: str = "",
        reporters: Sequence[Any] = (),
    ) -> None:
        """``reporters``: benchmark Reporter instances (W&B, Prometheus,
        JSON) that receive each iteration's scalar metrics."""
        self.c = components
        self.collect_per_iteration = collect_per_iteration
        self.train_episodes_per_iteration = train_episodes_per_iteration
        self.eval_episodes = eval_episodes
        self.report_dir = Path(report_dir).expanduser() if report_dir else None
        self.reporters = list(reporters)
        self.iteration = 0
        self._fallback_evaluator = HeuristicSelfEvaluator()

    # ------------------------------------------------------------------ run
    def run(self, iterations: int = 1) -> List[IterationReport]:
        return [self.run_iteration() for _ in range(iterations)]

    def run_iteration(self) -> IterationReport:
        self.iteration += 1
        t0 = time.time()
        report = IterationReport(iteration=self.iteration)

        # 1. Collect from every source.
        episodes = self._collect(report)

        # 2-4. Annotate rewards, evaluate, store (per episode, fail-safe).
        for ep in episodes:
            self._annotate_rewards(ep)
            evaluation = self._evaluate(ep)
            ep.meta.outcome = evaluation.outcome
            ep.meta.success_score = evaluation.completion_confidence
            ep.meta.extra["evaluation"] = {
                "judge": evaluation.judge,
                "task_summary": evaluation.task_summary,
                "failure_analysis": evaluation.failure_analysis,
                "success_analysis": evaluation.success_analysis,
            }
            self.c.dataset.add_episode(ep)
            key = ep.meta.outcome.value
            report.outcomes[key] = report.outcomes.get(key, 0) + 1
            if evaluation.failure_analysis:
                report.failure_analyses.append(
                    f"{ep.meta.episode_id}: {evaluation.failure_analysis}"
                )

        # 5. Train on prioritized replay (stratified across outcomes).
        report.train_result = self._train()

        # 6. Evaluate the updated policy on held-out rollouts.
        report.eval_success_rate = self._evaluate_policy()

        report.duration_s = time.time() - t0
        self._persist_report(report)
        self._publish_metrics(report)
        log.info("iteration %d: %s", self.iteration, report.to_dict())
        return report

    # --------------------------------------------------------------- stages
    def _collect(self, report: IterationReport) -> List[Episode]:
        episodes: List[Episode] = []
        for collector in self.c.collectors:
            try:
                for ep in collector.collect(num_episodes=self.collect_per_iteration):
                    episodes.append(ep)
            except TypeError:
                # Ingest-style collectors take no num_episodes argument.
                episodes.extend(collector.collect())
            except Exception:
                log.exception(
                    "collector %s failed; continuing", type(collector).__name__
                )
        report.episodes_collected = len(episodes)
        return episodes

    def _annotate_rewards(self, episode: Episode) -> None:
        context = dict(episode.meta.extra.get("reward_context", {}))
        self.c.reward_engine.annotate_episode(episode, context)

    def _evaluate(self, episode: Episode) -> EvaluationReport:
        for evaluator in self.c.evaluators:
            try:
                result = evaluator.evaluate(episode)
                if result.outcome is not TaskOutcome.UNKNOWN:
                    return result
            except Exception:
                log.exception(
                    "evaluator %s failed; falling back", type(evaluator).__name__
                )
        return self._fallback_evaluator.evaluate(episode)

    def _train(self) -> Optional[TrainResult]:
        if len(self.c.dataset) == 0:
            return None
        if self.c.replay_store is not None:
            train_data: Any = self.c.replay_store.sample_episodes(
                self.train_episodes_per_iteration
            )
        else:
            train_data = self.c.dataset
        result = self.c.trainer.train(train_data, self.c.policy)
        return result

    def _evaluate_policy(self) -> float:
        if self.c.eval_collector is None:
            return float("nan")
        successes, total = 0, 0
        for ep in self.c.eval_collector.collect(num_episodes=self.eval_episodes):
            self._annotate_rewards(ep)
            evaluation = self._evaluate(ep)
            successes += evaluation.outcome is TaskOutcome.SUCCESS
            total += 1
        return successes / total if total else float("nan")

    def _publish_metrics(self, report: IterationReport) -> None:
        if not self.reporters:
            return
        metrics: Dict[str, float] = {
            "episodes_collected": float(report.episodes_collected),
            "eval_success_rate": report.eval_success_rate,
            "duration_s": report.duration_s,
            "dataset_episodes": float(len(self.c.dataset)),
        }
        for outcome, count in report.outcomes.items():
            metrics[f"outcome_{outcome}"] = float(count)
        if report.train_result:
            metrics["train_final_loss"] = report.train_result.final_loss
            metrics["train_steps"] = float(report.train_result.steps)
        for reporter in self.reporters:
            try:
                reporter.log_metrics(metrics, step=self.iteration, context="loop")
            except Exception:
                log.exception("reporter %s failed; continuing", type(reporter).__name__)

    def _persist_report(self, report: IterationReport) -> None:
        if not self.report_dir:
            return
        self.report_dir.mkdir(parents=True, exist_ok=True)
        path = self.report_dir / f"iteration_{report.iteration:05d}.json"
        path.write_text(json.dumps(report.to_dict(), indent=1))
