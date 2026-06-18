"""Adapters that wire the existing ``learning_engine`` into the agent harness.

These satisfy the harness ports (``core.interfaces``) by reusing components
that already exist — nothing here re-implements safety, reflection or
learning. ``learning_engine`` is imported lazily so ``agent_engine.core``
stays importable without it.

- :class:`WorldStateVerifier`  → ``VerifierPort`` over ``SafetyCheck`` /
  ``ReachabilityCheck`` (real Yahboom/Feetech hardware limits).
- :class:`EvaluatorReflector`  → ``Reflector`` over the evaluator chain
  (``LanguageGoalEvaluator`` → ``ReflectionEvaluator`` → ``HeuristicSelfEvaluator``),
  labelling and (optionally) persisting the episode to a ``ReplayDataset``.
- :class:`ReplayMemorySource` → recent labelled-episode summaries for the
  working-memory prompt context.
- :class:`ContinualLearningClosure` → fires ``ContinualLearningScheduler``
  when enough new experience has accumulated (the reflect→learn closure).
"""

from __future__ import annotations

from typing import Any, Callable, Dict, List, Optional, Sequence, Tuple

import numpy as np

from ..core.blackboard import WorldState
from ..core.types import Goal, Reflection, ToolResult

ActionMapper = Callable[[str, Dict[str, Any]], Optional[np.ndarray]]


def default_action_mapper(tool: str, args: Dict[str, Any]) -> Optional[np.ndarray]:
    """Map a low-level tool call to an action vector the verifier understands.

    Returns ``None`` when the call carries no direct actuation (so the gate
    passes it through). Base actions are 3-D ``[vx, vy, omega]``; arm actions
    are 9-D ``[6 joint deltas, 0, 0, 0]`` — matching ``data.schema`` widths.
    """
    if any(k in args for k in ("vx", "vy", "omega")):
        return np.array(
            [
                float(args.get("vx", 0.0)),
                float(args.get("vy", 0.0)),
                float(args.get("omega", 0.0)),
            ],
            dtype=np.float32,
        )
    deltas = args.get("joint_deltas") or args.get("joints")
    if deltas is not None:
        arm = list(np.asarray(deltas, dtype=np.float32).ravel()[:6])
        arm += [0.0] * (6 - len(arm))
        return np.array(arm + [0.0, 0.0, 0.0], dtype=np.float32)
    return None


class WorldStateVerifier:
    """``VerifierPort`` backed by the learning-engine hard safety checks."""

    def __init__(
        self,
        checks: Optional[Sequence[Any]] = None,
        horizon: int = 10,
        action_mapper: ActionMapper = default_action_mapper,
    ) -> None:
        if checks is None:
            from learning_engine.verification.verifier import (
                ReachabilityCheck,
                SafetyCheck,
            )

            checks = [SafetyCheck(), ReachabilityCheck()]
        self.checks = list(checks)
        self.horizon = horizon
        self._map = action_mapper

    def verify(
        self, tool: str, args: Dict[str, Any], world: WorldState
    ) -> Tuple[bool, Dict[str, Any], str]:
        action = self._map(tool, args)
        if action is None:
            return True, args, "no direct actuation — passthrough"
        from learning_engine.core.types import CandidatePlan

        plan = CandidatePlan(actions=np.tile(action, (self.horizon, 1)))
        obs = world.to_observation()
        ctx: Dict[str, Any] = {
            "task": "",
            "robot_xy": list(world.base_pose[:2]),
        }
        if "goal_xy" in world.extra:
            ctx["goal_xy"] = world.extra["goal_xy"]
        for check in self.checks:
            score, reason = check.check(plan, obs, ctx)
            if getattr(check, "hard", False) and score <= 0.0:
                return False, args, f"{check.name}: {reason}"
        return True, args, "ok"


class EvaluatorReflector:
    """``Reflector`` over the learning-engine evaluator chain.

    Builds a lightweight :class:`Episode` from the goal + final world state,
    runs evaluators in order (first decisive verdict wins; heuristic is the
    always-available fallback), optionally persists the labelled episode to a
    ``ReplayDataset`` (feeding continual learning), and returns a
    :class:`Reflection` the harness writes back to memory.
    """

    def __init__(
        self,
        evaluators: Optional[Sequence[Any]] = None,
        vlm_client: Any = None,
        dataset: Any = None,
        environment: str = "real",
    ) -> None:
        self.dataset = dataset
        self.environment = environment
        if evaluators is None:
            from learning_engine.evaluation.self_eval import HeuristicSelfEvaluator

            evaluators = []
            if vlm_client is not None:
                from learning_engine.evaluation.language_goal import (
                    LanguageGoalEvaluator,
                )
                from learning_engine.evaluation.self_eval import ReflectionEvaluator

                evaluators += [
                    LanguageGoalEvaluator(vlm_client),
                    ReflectionEvaluator(vlm_client),
                ]
            evaluators.append(HeuristicSelfEvaluator())
        self.evaluators = list(evaluators)

    def reflect(
        self, goal: Goal, world: WorldState, history: Sequence[ToolResult]
    ) -> Reflection:
        from learning_engine.core.types import TaskOutcome

        episode = self._build_episode(goal, world, history)
        report = self._run_chain(episode)

        if self.dataset is not None:
            self.dataset.add_episode(episode)
            self.dataset.update_meta(
                episode.meta.episode_id,
                outcome=report.outcome,
                success_score=report.completion_confidence,
            )

        success = report.outcome in (TaskOutcome.SUCCESS, TaskOutcome.NEAR_SUCCESS)
        return Reflection(
            success=success,
            confidence=report.completion_confidence,
            summary=report.task_summary,
            analysis=report.failure_analysis or report.success_analysis,
            learned_objects=self._learned_objects(goal, world, success),
        )

    # -- internals ---------------------------------------------------------
    def _build_episode(
        self, goal: Goal, world: WorldState, history: Sequence[ToolResult]
    ):
        from learning_engine.core.types import (
            DataSource,
            Episode,
            EpisodeMeta,
            Step,
        )

        tool_failed = any(not r.ok for r in history)
        env_success = bool(history) and not tool_failed and not world.emergency_stop
        obs: Dict[str, np.ndarray] = world.to_observation()
        frames = world.extra.get("frames")
        if frames:
            obs["images.front"] = np.asarray(frames[-1])
        step = Step(
            observation=obs,
            action=np.zeros(9, dtype=np.float32),
            timestamp=world.stamp,
            info={"success": env_success, "emergency_stop": world.emergency_stop},
        )
        meta = EpisodeMeta(
            task_instruction=goal.text,
            source=DataSource.SIMULATION
            if self.environment != "real"
            else DataSource.REAL_EXECUTION,
            environment=self.environment,
        )
        return Episode(meta=meta, steps=[step])

    def _run_chain(self, episode):
        from learning_engine.core.types import TaskOutcome

        report = None
        for ev in self.evaluators:
            report = ev.evaluate(episode)
            if report.outcome is not TaskOutcome.UNKNOWN:
                return report
        return report  # last (heuristic) report, even if UNKNOWN

    @staticmethod
    def _learned_objects(
        goal: Goal, world: WorldState, success: bool
    ) -> Dict[str, str]:
        location = (goal.structured or {}).get("location") or world.extra.get(
            "location_name"
        )
        if not (success and location and world.detected_objects):
            return {}
        return {o.label: str(location) for o in world.detected_objects}


class ReplayMemorySource:
    """Recent labelled-episode summaries from a ``ReplayDataset`` — long-term
    episodic recall injected into the planning prompt."""

    def __init__(self, dataset: Any, max_recent: int = 3) -> None:
        self.dataset = dataset
        self.max_recent = max_recent

    def __call__(self) -> str:
        ids = self.dataset.episode_ids()[-self.max_recent :]
        if not ids:
            return ""
        lines = []
        for eid in ids:
            entry = self.dataset._index[eid]
            lines.append(
                "- {}: {} (score {:.2f})".format(
                    entry["task_instruction"] or "(no instruction)",
                    entry["outcome"],
                    entry.get("success_score", 0.0),
                )
            )
        return "Past episodes:\n" + "\n".join(lines)


class ContinualLearningClosure:
    """Closes reflect → learn: after episodes land in the dataset, fire the
    ``ContinualLearningScheduler`` when a trigger is met. Wraps the existing
    scheduler so the live agent improves its policies over time."""

    def __init__(self, scheduler: Any) -> None:
        self.scheduler = scheduler

    def maybe_learn(self, force: bool = False):
        """Returns an ``IterationReport`` when retraining ran, else ``None``."""
        return self.scheduler.step(force=force)

    def pending(self) -> List[str]:
        return self.scheduler.pending_triggers()
