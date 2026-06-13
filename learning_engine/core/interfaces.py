"""Abstract interfaces for every pluggable component in the learning engine.

The architecture rule: **layers depend on these interfaces, never on concrete
implementations.** Concrete classes register themselves in
``learning_engine.core.registry`` so configs can instantiate them by name.
"""

from __future__ import annotations

import abc
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

import numpy as np

from .types import (
    CandidatePlan,
    Episode,
    EvaluationReport,
    Observation,
    TrainResult,
    Transition,
)

# ---------------------------------------------------------------------------
# Layer 1 — Data collection
# ---------------------------------------------------------------------------


class DataCollector(abc.ABC):
    """Produces episodes from some source (teleop logs, sim rollouts, bags)."""

    @abc.abstractmethod
    def collect(self, **kwargs: Any) -> Iterable[Episode]:
        """Yield zero or more new episodes."""


# ---------------------------------------------------------------------------
# Layer 2 — Simulation environments
# ---------------------------------------------------------------------------


class SimulationEnv(abc.ABC):
    """Gym-style environment interface (single env; vectorization is the
    adapter's concern). Observations follow the canonical dict format."""

    action_dim: int = 9

    @abc.abstractmethod
    def reset(self, **kwargs: Any) -> Observation: ...

    @abc.abstractmethod
    def step(
        self, action: np.ndarray
    ) -> Tuple[Observation, float, bool, Dict[str, Any]]:
        """Returns (next_observation, reward, done, info)."""

    def set_goal(self, goal: Dict[str, Any]) -> None:
        """Goal-conditioned hook; default no-op."""

    def randomize(self, params: Dict[str, Any]) -> None:
        """Domain-randomization hook; default no-op."""

    def close(self) -> None:
        pass


# ---------------------------------------------------------------------------
# Layer 3/4 — Rewards
# ---------------------------------------------------------------------------


class RewardTerm(abc.ABC):
    """One component of the multi-objective reward (dense, safety, ...).

    Terms are stateful per episode (e.g. smoothness needs the previous
    action) — the engine calls ``reset()`` at episode boundaries.
    """

    name: str = "term"

    def __init__(self, weight: float = 1.0, **kwargs: Any) -> None:
        self.weight = float(weight)

    def reset(self) -> None:
        """Called at the start of every episode."""

    @abc.abstractmethod
    def compute(self, transition: Transition, context: Dict[str, Any]) -> float:
        """Raw (unweighted) value for this step."""


class RewardModel(abc.ABC):
    """Vision-based / learned reward: maps (instruction, frames) → score."""

    @abc.abstractmethod
    def score(self, instruction: str, frames: Sequence[np.ndarray]) -> float:
        """Task-completion score in [0, 1]."""


# ---------------------------------------------------------------------------
# Layer 5/6 — Evaluation
# ---------------------------------------------------------------------------


class VLMClient(abc.ABC):
    """Minimal multimodal LLM client used by judges, evaluators and
    vision-reward models. Swap implementations to change foundation model."""

    @abc.abstractmethod
    def complete(
        self,
        prompt: str,
        images: Optional[Sequence[np.ndarray]] = None,
        system: str = "",
    ) -> str: ...


class EpisodeEvaluator(abc.ABC):
    """Judges an episode: language-goal verification, self-critique, etc."""

    @abc.abstractmethod
    def evaluate(self, episode: Episode) -> EvaluationReport: ...


# ---------------------------------------------------------------------------
# Layer 7 — Experience replay
# ---------------------------------------------------------------------------


class ReplayBuffer(abc.ABC):
    @abc.abstractmethod
    def add(self, transition: Transition, priority: Optional[float] = None) -> None: ...

    @abc.abstractmethod
    def sample(self, batch_size: int) -> List[Transition]: ...

    @abc.abstractmethod
    def __len__(self) -> int: ...


# ---------------------------------------------------------------------------
# Layer 8/11 — Policies and trainers
# ---------------------------------------------------------------------------


class Policy(abc.ABC):
    """A deployable policy. Foundation-model policies (OpenVLA, SmolVLA),
    ONNX RL policies and learned BC policies all implement this."""

    action_dim: int = 9

    @abc.abstractmethod
    def predict(self, observation: Observation, task: str = "") -> np.ndarray: ...

    def sample_plans(
        self,
        observation: Observation,
        task: str = "",
        n: int = 4,
        horizon: int = 10,
        noise_scale: float = 0.05,
    ) -> List[CandidatePlan]:
        """Default Best-of-N candidate generation: perturb the greedy action
        with a constant per-plan offset (constant, not per-step, so the
        candidates stay smooth and respect per-step delta limits).

        Policies with native sampling (action chunking, temperature) should
        override this.
        """
        base = self.predict(observation, task)
        plans = []
        for i in range(n):
            noise = (
                0.0
                if i == 0
                else np.random.normal(0.0, noise_scale, size=base.shape[-1])
            )
            actions = np.tile(base + noise, (horizon, 1))
            plans.append(
                CandidatePlan(
                    actions=actions.astype(np.float32), source=type(self).__name__
                )
            )
        return plans

    def save(self, path: str) -> None:
        raise NotImplementedError(f"{type(self).__name__} does not support save()")

    def load(self, path: str) -> None:
        raise NotImplementedError(f"{type(self).__name__} does not support load()")


class PolicyTrainer(abc.ABC):
    """One learning strategy (BC, offline RL, online RL, fine-tuning)."""

    name: str = "trainer"

    @abc.abstractmethod
    def train(self, dataset: Any, policy: Optional[Policy] = None) -> TrainResult:
        """``dataset`` is a ReplayBuffer, ReplayDataset or env depending on
        the strategy; each trainer documents what it accepts."""


# ---------------------------------------------------------------------------
# Layer 10 — Inference-time verification
# ---------------------------------------------------------------------------


class PlanCheck(abc.ABC):
    """One verification criterion applied to a candidate plan."""

    name: str = "check"
    hard: bool = False  # hard checks mark the plan infeasible when they fail

    @abc.abstractmethod
    def check(
        self, plan: CandidatePlan, observation: Observation, context: Dict[str, Any]
    ) -> Tuple[float, str]:
        """Returns (score in [0,1], reason). Score 0 on a hard check rejects
        the plan."""
