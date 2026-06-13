"""Core data types shared by every layer of the learning engine.

These types are the *lingua franca* of the framework: collectors produce
``Episode`` objects, the reward engine annotates ``Transition`` objects with
``RewardBreakdown``, evaluators produce ``EvaluationReport`` objects, and the
verification layer scores ``CandidatePlan`` objects.

Dimensions and key names must stay consistent with
``data_engine/schema/constants.py`` (see ``learning_engine.data.schema``).
"""

from __future__ import annotations

import time
import uuid
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, Iterator, List, Optional

import numpy as np

# ---------------------------------------------------------------------------
# Enumerations
# ---------------------------------------------------------------------------


class DataSource(str, Enum):
    """Where an episode came from."""

    TELEOP = "teleop"  # leader-arm / joystick teleoperation
    HUMAN_DEMO = "human_demo"  # kinesthetic or scripted human demonstration
    SIMULATION = "simulation"  # Isaac Lab / Isaac Sim / MuJoCo / Gazebo rollout
    REAL_EXECUTION = "real_execution"  # autonomous execution on the robot
    AUGMENTED = "augmented"  # relabeled / augmented from another episode


class TaskOutcome(str, Enum):
    """Outcome label attached to an episode after evaluation."""

    SUCCESS = "success"
    NEAR_SUCCESS = "near_success"
    FAILURE = "failure"
    ABORTED = "aborted"  # e-stop, watchdog, operator interruption
    UNKNOWN = "unknown"  # not yet evaluated


# ---------------------------------------------------------------------------
# Rewards
# ---------------------------------------------------------------------------


@dataclass
class RewardBreakdown:
    """Multi-objective reward for a single step.

    ``terms`` holds the raw (unweighted) value of each reward term;
    ``weights`` holds the weight the engine applied. ``total`` is the
    weighted scalar used for RL; the vector form is kept so trainers that
    do multi-objective optimization can re-weight offline.
    """

    terms: Dict[str, float] = field(default_factory=dict)
    weights: Dict[str, float] = field(default_factory=dict)

    @property
    def total(self) -> float:
        return float(sum(self.weights.get(k, 1.0) * v for k, v in self.terms.items()))

    def as_vector(self, order: Optional[List[str]] = None) -> np.ndarray:
        keys = order if order is not None else sorted(self.terms)
        return np.array([self.terms.get(k, 0.0) for k in keys], dtype=np.float32)

    def to_dict(self) -> Dict[str, Any]:
        return {
            "terms": dict(self.terms),
            "weights": dict(self.weights),
            "total": self.total,
        }


# ---------------------------------------------------------------------------
# Transitions / steps / episodes
# ---------------------------------------------------------------------------

# Observations are dictionaries of named numpy arrays, e.g.
#   {"state": (9,), "images.wrist": (H, W, 3), "images.bev": (H, W, 3)}
Observation = Dict[str, np.ndarray]


@dataclass
class Transition:
    """A single (o, a, r, o', done) tuple — the unit of RL training."""

    observation: Observation
    action: np.ndarray
    reward: float
    next_observation: Observation
    done: bool
    info: Dict[str, Any] = field(default_factory=dict)
    reward_breakdown: Optional[RewardBreakdown] = None


@dataclass
class Step:
    """A single timestep inside an episode (observation + action taken)."""

    observation: Observation
    action: np.ndarray
    timestamp: float = 0.0
    reward: Optional[RewardBreakdown] = None
    info: Dict[str, Any] = field(default_factory=dict)


@dataclass
class EpisodeMeta:
    episode_id: str = field(default_factory=lambda: uuid.uuid4().hex[:12])
    task_instruction: str = ""
    source: DataSource = DataSource.REAL_EXECUTION
    outcome: TaskOutcome = TaskOutcome.UNKNOWN
    success_score: float = 0.0  # completion confidence in [0, 1]
    fps: float = 30.0
    robot: str = "omnibot"
    environment: str = ""  # e.g. "real", "isaac_lab:OmnibotNav-v0", "mujoco"
    created_at: float = field(default_factory=time.time)
    tags: List[str] = field(default_factory=list)
    extra: Dict[str, Any] = field(default_factory=dict)


@dataclass
class Episode:
    """An ordered sequence of steps plus metadata.

    The canonical storage unit for demonstrations, sim rollouts and real
    execution logs alike.
    """

    meta: EpisodeMeta
    steps: List[Step] = field(default_factory=list)

    def __len__(self) -> int:
        return len(self.steps)

    @property
    def total_reward(self) -> float:
        return float(sum(s.reward.total for s in self.steps if s.reward))

    def transitions(self) -> Iterator[Transition]:
        """View the episode as (o, a, r, o', done) transitions."""
        for i, step in enumerate(self.steps):
            last = i == len(self.steps) - 1
            nxt = self.steps[i].observation if last else self.steps[i + 1].observation
            yield Transition(
                observation=step.observation,
                action=step.action,
                reward=step.reward.total if step.reward else 0.0,
                next_observation=nxt,
                done=last,
                info=step.info,
                reward_breakdown=step.reward,
            )


# ---------------------------------------------------------------------------
# Evaluation
# ---------------------------------------------------------------------------


@dataclass
class EvaluationReport:
    """Output of the language-goal / self-evaluation layers for one episode."""

    episode_id: str
    completion_confidence: float  # [0, 1]
    outcome: TaskOutcome
    task_summary: str = ""
    success_analysis: str = ""
    failure_analysis: str = ""
    judge: str = "heuristic"  # which evaluator produced this
    extra: Dict[str, Any] = field(default_factory=dict)


# ---------------------------------------------------------------------------
# Inference-time verification
# ---------------------------------------------------------------------------


@dataclass
class CandidatePlan:
    """A candidate action sequence proposed at inference time.

    ``actions`` has shape (horizon, action_dim). ``scores`` maps check name
    to a score in [0, 1]; ``feasible`` is False when any hard check failed.
    """

    actions: np.ndarray
    source: str = "policy"
    scores: Dict[str, float] = field(default_factory=dict)
    feasible: bool = True
    rejection_reason: str = ""

    def aggregate_score(self, weights: Optional[Dict[str, float]] = None) -> float:
        if not self.feasible:
            return float("-inf")
        if not self.scores:
            return 0.0
        w = weights or {}
        return float(
            sum(w.get(k, 1.0) * v for k, v in self.scores.items())
            / max(sum(w.get(k, 1.0) for k in self.scores), 1e-9)
        )


@dataclass
class TrainResult:
    """Summary returned by every PolicyTrainer."""

    trainer: str
    steps: int = 0
    final_loss: float = float("nan")
    metrics: Dict[str, float] = field(default_factory=dict)
    checkpoint_path: str = ""
