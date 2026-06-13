"""Inference-time verification — Best-of-N plan selection with safety gating.

Flow (runs before actions reach the robot)::

    policy.sample_plans(obs, task, n)        # N candidate action sequences
        → hard checks   (safety, reachability)   reject infeasible plans
        → soft checks   (task likelihood, consistency)  score the rest
        → argmax aggregate score; fall back to the safe policy when all fail

Hard checks use the platform's real limits from ``data.schema`` (Yahboom
0.2 m/s clamp, ±0.05 rad/step arm delta, URDF joint limits) so a plan that
passes verification is also executable without the driver silently
clipping it.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Sequence, Tuple

import numpy as np

from ..core.interfaces import PlanCheck, Policy, RewardModel
from ..core.registry import PLAN_CHECKS
from ..core.types import CandidatePlan, Observation
from ..data import schema
from ..policies.base import ZeroPolicy


@PLAN_CHECKS.register("safety")
class SafetyCheck(PlanCheck):
    """Hard limits: base velocity, arm step delta, joint excursion.

    Plans are 9-D (arm ×6 + base ×3) or 3-D (base only) — inferred from
    action width."""

    name = "safety"
    hard = True

    def __init__(
        self,
        max_linear: float = schema.MAX_LINEAR_VEL,
        max_angular: float = schema.MAX_ANGULAR_VEL,
        max_arm_delta: float = schema.MAX_ARM_DELTA_PER_STEP,
    ) -> None:
        self.max_linear = max_linear
        self.max_angular = max_angular
        self.max_arm_delta = max_arm_delta

    def check(
        self, plan: CandidatePlan, observation: Observation, context: Dict[str, Any]
    ) -> Tuple[float, str]:
        a = np.asarray(plan.actions, dtype=np.float64)
        if a.shape[-1] >= schema.MOBILE_MANIP_ACTION_DIM:
            arm, base = (
                a[:, : schema.ARM_DIM],
                a[:, schema.ARM_DIM : schema.ARM_DIM + 3],
            )
        else:
            arm, base = None, a[:, :3]

        if np.any(np.abs(base[:, :2]) > self.max_linear + 1e-9):
            return 0.0, f"base linear vel exceeds {self.max_linear} m/s"
        if base.shape[1] > 2 and np.any(np.abs(base[:, 2]) > self.max_angular + 1e-9):
            return 0.0, f"base angular vel exceeds {self.max_angular} rad/s"

        if arm is not None:
            deltas = np.diff(arm, axis=0, prepend=arm[:1])
            if np.any(np.abs(deltas) > self.max_arm_delta + 1e-9):
                return 0.0, f"arm joint delta exceeds {self.max_arm_delta} rad/step"
            state = observation.get(schema.OBS_STATE)
            if state is not None and state.shape[-1] >= schema.ARM_DIM:
                q = np.asarray(state[: schema.ARM_DIM], dtype=np.float64)
                q_final = q + np.sum(deltas, axis=0)
                if np.any(q_final < schema.JOINT_MIN) or np.any(
                    q_final > schema.JOINT_MAX
                ):
                    return 0.0, "plan drives arm past URDF joint limits"
        # Feasible — score by margin to limits (gentler plans score higher).
        margin = 1.0 - float(np.mean(np.abs(base[:, :2])) / self.max_linear)
        return float(np.clip(margin, 0.1, 1.0)), "ok"


@PLAN_CHECKS.register("reachability")
class ReachabilityCheck(PlanCheck):
    """Hard check that the plan's integrated base displacement stays inside
    a sane envelope, and (when context provides ``goal_xy``) soft-scores how
    close the rollout ends to the goal."""

    name = "reachability"
    hard = True

    def __init__(self, max_displacement_m: float = 3.0, dt: float = 0.05) -> None:
        self.max_displacement_m = max_displacement_m
        self.dt = dt

    def check(
        self, plan: CandidatePlan, observation: Observation, context: Dict[str, Any]
    ) -> Tuple[float, str]:
        a = np.asarray(plan.actions, dtype=np.float64)
        base = (
            a[:, schema.ARM_DIM : schema.ARM_DIM + 2] if a.shape[-1] >= 9 else a[:, :2]
        )
        displacement = np.sum(base, axis=0) * self.dt  # crude dead-reckoning
        if np.linalg.norm(displacement) > self.max_displacement_m:
            return 0.0, f"plan displaces base > {self.max_displacement_m} m"
        goal = context.get("goal_xy")
        if goal is None:
            return 1.0, "ok"
        start = np.asarray(context.get("robot_xy", [0.0, 0.0]), dtype=np.float64)
        end_dist = float(np.linalg.norm(np.asarray(goal) - (start + displacement)))
        start_dist = float(np.linalg.norm(np.asarray(goal) - start))
        improvement = (start_dist - end_dist) / max(start_dist, 1e-6)
        return float(np.clip(0.5 + 0.5 * improvement, 0.0, 1.0)), "ok"


@PLAN_CHECKS.register("task_likelihood")
class TaskLikelihoodCheck(PlanCheck):
    """Soft score from a RewardModel (or any callable scorer) estimating how
    likely the plan completes the task. Needs the current camera frame."""

    name = "task_likelihood"
    hard = False

    def __init__(
        self, reward_model: RewardModel, image_key: str = schema.OBS_IMAGE_WRIST
    ) -> None:
        self.reward_model = reward_model
        self.image_key = image_key

    def check(
        self, plan: CandidatePlan, observation: Observation, context: Dict[str, Any]
    ) -> Tuple[float, str]:
        frame = observation.get(self.image_key)
        task = str(context.get("task", ""))
        if frame is None or not task:
            return 0.5, "no frame/task — neutral score"
        return float(self.reward_model.score(task, [frame])), "ok"


@dataclass
class VerificationResult:
    plan: CandidatePlan
    fallback_used: bool = False
    candidates: List[CandidatePlan] = field(default_factory=list)

    @property
    def action(self) -> np.ndarray:
        """First action of the selected plan (receding-horizon execution)."""
        return self.plan.actions[0]


class InferenceVerifier:
    """Best-of-N planning with verification.

    Self-consistency is built into selection: candidates whose first action
    agrees with the candidate majority get a consistency bonus, damping
    single-sample VLA outliers.
    """

    def __init__(
        self,
        checks: Sequence[PlanCheck],
        n_candidates: int = 4,
        horizon: int = 10,
        score_weights: Optional[Dict[str, float]] = None,
        consistency_weight: float = 0.5,
        fallback_policy: Optional[Policy] = None,
    ) -> None:
        self.checks = list(checks)
        self.n_candidates = n_candidates
        self.horizon = horizon
        self.score_weights = score_weights or {}
        self.consistency_weight = consistency_weight
        self.fallback_policy = fallback_policy or ZeroPolicy()

    def select(
        self,
        policy: Policy,
        observation: Observation,
        task: str = "",
        context: Optional[Dict[str, Any]] = None,
    ) -> VerificationResult:
        context = dict(context or {})
        context.setdefault("task", task)
        candidates = policy.sample_plans(
            observation, task, n=self.n_candidates, horizon=self.horizon
        )
        for plan in candidates:
            for check in self.checks:
                score, reason = check.check(plan, observation, context)
                plan.scores[check.name] = score
                if check.hard and score <= 0.0:
                    plan.feasible = False
                    plan.rejection_reason = f"{check.name}: {reason}"
                    break

        feasible = [p for p in candidates if p.feasible]
        if not feasible:
            safe = CandidatePlan(
                actions=np.tile(
                    self.fallback_policy.predict(observation, task), (self.horizon, 1)
                ),
                source="fallback",
                feasible=True,
            )
            return VerificationResult(
                plan=safe, fallback_used=True, candidates=candidates
            )

        if len(feasible) > 1 and self.consistency_weight > 0:
            self._apply_consistency_bonus(feasible)

        best = max(feasible, key=lambda p: p.aggregate_score(self.score_weights))
        return VerificationResult(plan=best, candidates=candidates)

    def _apply_consistency_bonus(self, plans: List[CandidatePlan]) -> None:
        firsts = np.stack([p.actions[0] for p in plans])
        centroid = firsts.mean(axis=0)
        dists = np.linalg.norm(firsts - centroid, axis=1)
        scale = float(dists.max()) or 1.0
        for plan, d in zip(plans, dists):
            plan.scores["consistency"] = self.consistency_weight * (
                1.0 - float(d) / scale
            )
