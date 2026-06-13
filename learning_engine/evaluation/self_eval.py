"""Self-evaluation layer — reflection after every episode.

Produces task summary + success/failure analysis. Two evaluators:

- ``HeuristicSelfEvaluator`` — dependency-free; summarizes reward
  statistics, collisions and goal progress. Always available (Pi, CI).
- ``ReflectionEvaluator`` — LLM self-critique over episode statistics and
  sampled frames; richer analysis on the GPU/cloud side.

Reports are persisted into the episode's metadata by the learning loop and
become retrieval material for failure-mode analysis.
"""

from __future__ import annotations

import json
import re
from typing import Any, Dict

import numpy as np

from ..core.interfaces import EpisodeEvaluator, VLMClient
from ..core.registry import EVALUATORS
from ..core.types import Episode, EvaluationReport, TaskOutcome
from .language_goal import classify_outcome, episode_frames

_REFLECT_PROMPT = """You are a robot reflecting on an episode you just executed.

Instruction: "{instruction}"
Episode statistics (JSON): {stats}

Frames from the episode are attached in chronological order.

Respond in JSON only:
{{"confidence": <0.0-1.0 task completion confidence>,
  "task_summary": "<2 sentences: what happened>",
  "success_analysis": "<what worked, '' if nothing notable>",
  "failure_analysis": "<root cause of failure or risk observed, '' if none>"}}"""


def episode_stats(episode: Episode) -> Dict[str, Any]:
    """Cheap numeric summary used by both evaluators."""
    stats: Dict[str, Any] = {
        "steps": len(episode),
        "duration_s": round(len(episode) / max(episode.meta.fps, 1e-6), 2),
        "collisions": sum(1 for s in episode.steps if s.info.get("collision")),
        "env_success": any(s.info.get("success") for s in episode.steps),
        "aborted": any(s.info.get("emergency_stop") for s in episode.steps),
    }
    rewarded = [s.reward for s in episode.steps if s.reward]
    if rewarded:
        stats["total_reward"] = round(sum(r.total for r in rewarded), 3)
        term_sums: Dict[str, float] = {}
        for r in rewarded:
            for k, v in r.terms.items():
                term_sums[k] = term_sums.get(k, 0.0) + v
        stats["reward_terms"] = {k: round(v, 3) for k, v in term_sums.items()}
    if episode.steps:
        actions = np.stack([s.action for s in episode.steps])
        stats["mean_abs_action"] = round(float(np.mean(np.abs(actions))), 4)
        stats["action_jerk"] = (
            round(float(np.mean(np.abs(np.diff(actions, axis=0)))), 4)
            if len(actions) > 1
            else 0.0
        )
    return stats


@EVALUATORS.register("heuristic_self_eval")
class HeuristicSelfEvaluator(EpisodeEvaluator):
    """Rule-based reflection from episode statistics — the always-on
    fallback when no LLM judge is reachable."""

    def evaluate(self, episode: Episode) -> EvaluationReport:
        stats = episode_stats(episode)
        if stats["env_success"]:
            confidence = 1.0
        elif stats["aborted"]:
            confidence = 0.0
        else:
            # Positive accumulated reward suggests partial progress.
            total = stats.get("total_reward", 0.0)
            confidence = float(np.clip(0.4 + 0.1 * np.tanh(total), 0.0, 0.7))
        outcome = (
            TaskOutcome.ABORTED if stats["aborted"] else classify_outcome(confidence)
        )

        failures = []
        if stats["collisions"]:
            failures.append(f"{stats['collisions']} collision step(s)")
        if stats["aborted"]:
            failures.append("episode aborted (emergency stop)")
        if stats.get("action_jerk", 0.0) > 0.1:
            failures.append(f"jerky actions (mean |Δa|={stats['action_jerk']})")

        return EvaluationReport(
            episode_id=episode.meta.episode_id,
            completion_confidence=confidence,
            outcome=outcome,
            task_summary=(
                f"{stats['steps']} steps over {stats['duration_s']}s; "
                f"total_reward={stats.get('total_reward', 'n/a')}; "
                f"env_success={stats['env_success']}"
            ),
            success_analysis="environment reported success"
            if stats["env_success"]
            else "",
            failure_analysis="; ".join(failures),
            judge="heuristic_self_eval",
            extra={"stats": stats},
        )


@EVALUATORS.register("reflection")
class ReflectionEvaluator(EpisodeEvaluator):
    """LLM-as-a-judge self-critique over statistics + sampled frames."""

    def __init__(self, client: VLMClient, max_frames: int = 4) -> None:
        self.client = client
        self.max_frames = max_frames

    def evaluate(self, episode: Episode) -> EvaluationReport:
        stats = episode_stats(episode)
        frames = episode_frames(episode, self.max_frames)
        reply = self.client.complete(
            _REFLECT_PROMPT.format(
                instruction=episode.meta.task_instruction,
                stats=json.dumps(stats),
            ),
            images=frames or None,
        )
        doc = _parse_json(reply)
        confidence = float(np.clip(float(doc.get("confidence", 0.0)), 0.0, 1.0))
        return EvaluationReport(
            episode_id=episode.meta.episode_id,
            completion_confidence=confidence,
            outcome=classify_outcome(confidence),
            task_summary=str(doc.get("task_summary", "")),
            success_analysis=str(doc.get("success_analysis", "")),
            failure_analysis=str(doc.get("failure_analysis", "")),
            judge="reflection",
            extra={"stats": stats},
        )


def _parse_json(reply: str) -> Dict[str, Any]:
    try:
        m = re.search(r"\{.*\}", reply, re.DOTALL)
        return json.loads(m.group()) if m else {}
    except (ValueError, TypeError):
        return {}
