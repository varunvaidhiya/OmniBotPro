"""Shared test stubs — a deterministic env and episode factory."""

from __future__ import annotations

from typing import Any, Dict, Tuple

import numpy as np

from learning_engine.core.interfaces import SimulationEnv
from learning_engine.core.types import (
    DataSource,
    Episode,
    EpisodeMeta,
    Observation,
    Step,
)
from learning_engine.data import schema


class DummyEnv(SimulationEnv):
    """5-step env that always succeeds on the final step."""

    action_dim = schema.MOBILE_MANIP_ACTION_DIM

    def __init__(self, episode_len: int = 5, succeed: bool = True) -> None:
        self.episode_len = episode_len
        self.succeed = succeed
        self._t = 0

    def _obs(self) -> Observation:
        state = np.zeros(schema.MOBILE_MANIP_STATE_DIM, dtype=np.float32)
        state[schema.ARM_DIM] = 0.1  # constant base vx
        return {
            schema.OBS_STATE: state,
            schema.OBS_IMAGE_WRIST: np.full((8, 8, 3), self._t * 10, dtype=np.uint8),
        }

    def reset(self, **kwargs: Any) -> Observation:
        self._t = 0
        return self._obs()

    def step(
        self, action: np.ndarray
    ) -> Tuple[Observation, float, bool, Dict[str, Any]]:
        self._t += 1
        done = self._t >= self.episode_len
        info = {"success": done and self.succeed}
        return self._obs(), 1.0 if info["success"] else 0.0, done, info


def make_episode(
    n_steps: int = 5, task: str = "pick up the red cup", success: bool = True
) -> Episode:
    steps = []
    for t in range(n_steps):
        state = np.arange(schema.MOBILE_MANIP_STATE_DIM, dtype=np.float32) * 0.01 * t
        steps.append(
            Step(
                observation={
                    schema.OBS_STATE: state,
                    schema.OBS_IMAGE_WRIST: np.full((4, 4, 3), t, dtype=np.uint8),
                },
                action=np.full(
                    schema.MOBILE_MANIP_ACTION_DIM, 0.01 * t, dtype=np.float32
                ),
                timestamp=float(t),
                info={"success": success and t == n_steps - 1},
            )
        )
    return Episode(
        meta=EpisodeMeta(
            task_instruction=task, source=DataSource.SIMULATION, environment="dummy"
        ),
        steps=steps,
    )
