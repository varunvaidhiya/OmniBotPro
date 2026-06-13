"""Built-in reward terms, grouped by objective.

All terms read from ``Transition`` fields and the shared ``context`` dict
(populated by the RewardEngine caller, e.g. goal position, collision flags).
Conventions:
- rewards are positive, penalties negative;
- terms return *raw* values; the engine applies weights;
- terms that need history (smoothness, progress) keep per-episode state and
  are reset by the engine at episode boundaries.

Context keys used (all optional):
    goal_xy: np.ndarray (2,)   robot_xy: np.ndarray (2,)
"""

from __future__ import annotations

from typing import Any, Dict, Optional

import numpy as np

from ..core.interfaces import RewardTerm
from ..core.registry import REWARD_TERMS
from ..core.types import Transition
from ..data import schema

# ---------------------------------------------------------------- task


@REWARD_TERMS.register("task_success")
class TaskSuccessReward(RewardTerm):
    """Sparse success/failure: +bonus when info['success'], -penalty on
    info['failure'] (default 0)."""

    name = "task_success"

    def __init__(
        self, weight: float = 1.0, bonus: float = 10.0, failure_penalty: float = 0.0
    ) -> None:
        super().__init__(weight)
        self.bonus = bonus
        self.failure_penalty = failure_penalty

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        if transition.info.get("success"):
            return self.bonus
        if transition.info.get("failure"):
            return -self.failure_penalty
        return 0.0


# ---------------------------------------------------------------- dense


def _robot_xy(transition: Transition, context: Dict[str, Any]) -> Optional[np.ndarray]:
    if "robot_xy" in context:
        return np.asarray(context["robot_xy"], dtype=np.float64)
    state = transition.next_observation.get(schema.OBS_STATE)
    if state is not None and state.shape[-1] >= 2:
        return np.asarray(state[..., :2], dtype=np.float64)
    return None


@REWARD_TERMS.register("goal_distance")
class GoalDistanceReward(RewardTerm):
    """Dense −distance_to_goal (normalized by max_range)."""

    name = "goal_distance"

    def __init__(self, weight: float = 1.0, max_range_m: float = 5.0) -> None:
        super().__init__(weight)
        self.max_range_m = max_range_m

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        goal = context.get("goal_xy")
        pos = _robot_xy(transition, context)
        if goal is None or pos is None:
            return 0.0
        dist = float(np.linalg.norm(np.asarray(goal, dtype=np.float64) - pos))
        return -min(dist / self.max_range_m, 1.0)


@REWARD_TERMS.register("goal_progress")
class GoalProgressReward(RewardTerm):
    """Dense progress: previous_distance − current_distance (m/step).
    Positive when moving toward the goal — better shaped than raw −distance
    because it is invariant to starting distance."""

    name = "goal_progress"

    def __init__(self, weight: float = 1.0) -> None:
        super().__init__(weight)
        self._prev_dist: Optional[float] = None

    def reset(self) -> None:
        self._prev_dist = None

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        goal = context.get("goal_xy")
        pos = _robot_xy(transition, context)
        if goal is None or pos is None:
            return 0.0
        dist = float(np.linalg.norm(np.asarray(goal, dtype=np.float64) - pos))
        progress = 0.0 if self._prev_dist is None else self._prev_dist - dist
        self._prev_dist = dist
        return progress


# ---------------------------------------------------------------- safety


@REWARD_TERMS.register("collision")
class CollisionPenalty(RewardTerm):
    """−1 per collision step (info['collision'] or min lidar sector below
    min_distance_m)."""

    name = "collision"

    def __init__(self, weight: float = 5.0, min_distance_m: float = 0.25) -> None:
        super().__init__(weight)
        self.min_distance_m = min_distance_m

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        if transition.info.get("collision"):
            return -1.0
        sectors = transition.next_observation.get(schema.OBS_LIDAR_SECTORS)
        if sectors is not None and float(np.min(sectors)) < self.min_distance_m:
            return -1.0
        return 0.0


@REWARD_TERMS.register("joint_limit")
class JointLimitPenalty(RewardTerm):
    """Penalize arm joints within ``margin`` of their URDF limits.

    Assumes the first ARM_DIM entries of the state vector are arm joint
    positions (the 9-D mobile-manip layout: arm ×6 + base ×3)."""

    name = "joint_limit"

    def __init__(self, weight: float = 1.0, margin_rad: float = 0.1) -> None:
        super().__init__(weight)
        self.margin = margin_rad
        self._min = np.array(schema.JOINT_MIN)
        self._max = np.array(schema.JOINT_MAX)

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        state = transition.next_observation.get(schema.OBS_STATE)
        if state is None or state.shape[-1] < schema.MOBILE_MANIP_STATE_DIM:
            return 0.0
        q = np.asarray(state[: schema.ARM_DIM], dtype=np.float64)
        near = (q < self._min + self.margin) | (q > self._max - self.margin)
        return -float(np.count_nonzero(near)) / schema.ARM_DIM


# ------------------------------------------------------------- efficiency


@REWARD_TERMS.register("energy")
class EnergyPenalty(RewardTerm):
    """−‖action‖² — proxy for actuation energy."""

    name = "energy"

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        return -float(np.sum(np.square(transition.action)))


@REWARD_TERMS.register("time")
class ExecutionTimePenalty(RewardTerm):
    """Constant −cost per step — pressure to finish quickly."""

    name = "time"

    def __init__(self, weight: float = 1.0, per_step: float = 0.01) -> None:
        super().__init__(weight)
        self.per_step = per_step

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        return -self.per_step


# ------------------------------------------------------------- smoothness


@REWARD_TERMS.register("action_smoothness")
class ActionSmoothnessPenalty(RewardTerm):
    """−‖aₜ − aₜ₋₁‖² — discourages jerky commands (protects the Feetech bus
    and the Yahboom ramp limiter from saturating)."""

    name = "action_smoothness"

    def __init__(self, weight: float = 1.0) -> None:
        super().__init__(weight)
        self._prev: Optional[np.ndarray] = None

    def reset(self) -> None:
        self._prev = None

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        a = np.asarray(transition.action, dtype=np.float64)
        penalty = (
            0.0 if self._prev is None else -float(np.sum(np.square(a - self._prev)))
        )
        self._prev = a
        return penalty


@REWARD_TERMS.register("trajectory_smoothness")
class TrajectorySmoothnessPenalty(RewardTerm):
    """−‖Δvelocity‖² on the state's velocity components — penalizes jerk in
    the executed trajectory rather than the commands."""

    name = "trajectory_smoothness"

    def __init__(self, weight: float = 1.0, vel_slice: int = schema.ARM_DIM) -> None:
        super().__init__(weight)
        self.vel_slice = vel_slice  # index where base velocities start
        self._prev: Optional[np.ndarray] = None

    def reset(self) -> None:
        self._prev = None

    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        state = transition.next_observation.get(schema.OBS_STATE)
        if state is None or state.shape[-1] < schema.MOBILE_MANIP_STATE_DIM:
            return 0.0
        vel = np.asarray(
            state[self.vel_slice : self.vel_slice + schema.BASE_STATE_DIM],
            dtype=np.float64,
        )
        penalty = (
            0.0 if self._prev is None else -float(np.sum(np.square(vel - self._prev)))
        )
        self._prev = vel
        return penalty
