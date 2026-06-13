"""Curriculum learning and domain randomization.

Both are simulator-agnostic: the curriculum decides *what* the env should
look like (stage kwargs and randomization ranges); the env's
``randomize()`` / ``set_goal()`` hooks apply it.
"""

from __future__ import annotations

import random
from collections import deque
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Tuple


@dataclass
class CurriculumStage:
    name: str
    env_kwargs: Dict[str, Any] = field(default_factory=dict)
    randomization: Dict[str, Tuple[float, float]] = field(default_factory=dict)
    promote_success_rate: float = 0.8  # promote when rolling success ≥ this
    window: int = 50  # rolling window of episodes


class CurriculumScheduler:
    """Promotes through stages based on rolling success rate.

    Example::

        stages = [
            CurriculumStage("close_goals", {"goal_radius_m": 1.0}),
            CurriculumStage("far_goals",   {"goal_radius_m": 3.0},
                            randomization={"friction": (0.6, 1.2)}),
        ]
        curriculum = CurriculumScheduler(stages)
        ...
        curriculum.report(success=True)
        env_kwargs = curriculum.current.env_kwargs
    """

    def __init__(self, stages: List[CurriculumStage]) -> None:
        if not stages:
            raise ValueError("need at least one curriculum stage")
        self.stages = stages
        self.stage_idx = 0
        self._results: deque = deque(maxlen=stages[0].window)

    @property
    def current(self) -> CurriculumStage:
        return self.stages[self.stage_idx]

    @property
    def finished(self) -> bool:
        return self.stage_idx == len(self.stages) - 1 and self._promotable()

    def report(self, success: bool) -> bool:
        """Record an episode result; returns True if the stage advanced."""
        self._results.append(bool(success))
        if self.stage_idx < len(self.stages) - 1 and self._promotable():
            self.stage_idx += 1
            self._results = deque(maxlen=self.current.window)
            return True
        return False

    def _promotable(self) -> bool:
        if len(self._results) < self._results.maxlen:
            return False
        rate = sum(self._results) / len(self._results)
        return rate >= self.current.promote_success_rate


class DomainRandomizer:
    """Samples per-episode randomization params from configured ranges and
    pushes them to the env. Mirrors the structure of
    ``data_engine/isaac_sim/randomization_config.yaml`` — keep parameter
    names aligned when adding new ones."""

    def __init__(
        self,
        ranges: Optional[Dict[str, Tuple[float, float]]] = None,
        enabled: bool = True,
        seed: Optional[int] = None,
    ) -> None:
        self.ranges = dict(ranges or {})
        self.enabled = enabled
        self._rng = random.Random(seed)

    def sample(self) -> Dict[str, float]:
        if not self.enabled:
            return {}
        return {k: self._rng.uniform(lo, hi) for k, (lo, hi) in self.ranges.items()}

    def apply(self, env: Any) -> Dict[str, float]:
        params = self.sample()
        if params:
            env.randomize(params)
        return params
