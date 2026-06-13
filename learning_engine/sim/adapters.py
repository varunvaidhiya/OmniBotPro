"""Simulation backend adapters.

Each adapter wraps an external simulator behind ``SimulationEnv`` so the
rest of the framework never imports simulator code directly. All heavy
imports happen lazily inside ``__init__`` — constructing an adapter on a
machine without that simulator raises a clear RuntimeError instead of
breaking imports.

Backends:
- Isaac Lab  — primary RL backend; reuses the envs in ``rl_engine/``.
- MuJoCo     — lightweight backend via Gymnasium for fast CPU iteration.
- Gazebo     — bridges live ROS topics; for hardware-in-the-loop tests.
- ManiSkill  — optional manipulation benchmark backend (Gymnasium API).
"""

from __future__ import annotations

from typing import Any, Dict, Tuple

import numpy as np

from ..core.interfaces import SimulationEnv
from ..core.registry import ENVS
from ..core.types import Observation
from ..data import schema


def _gym_obs_to_dict(obs: Any) -> Observation:
    if isinstance(obs, dict):
        return {str(k): np.asarray(v, dtype=np.float32) for k, v in obs.items()}
    return {schema.OBS_STATE: np.asarray(obs, dtype=np.float32)}


class _GymnasiumAdapter(SimulationEnv):
    """Shared logic for Gymnasium-API simulators (MuJoCo, ManiSkill)."""

    def __init__(self, env: Any) -> None:
        self._env = env
        space = getattr(env, "action_space", None)
        if space is not None and hasattr(space, "shape") and space.shape:
            self.action_dim = int(space.shape[0])

    def reset(self, **kwargs: Any) -> Observation:
        obs, _info = self._env.reset(**kwargs)
        return _gym_obs_to_dict(obs)

    def step(
        self, action: np.ndarray
    ) -> Tuple[Observation, float, bool, Dict[str, Any]]:
        obs, reward, terminated, truncated, info = self._env.step(action)
        info = dict(info)
        info["truncated"] = bool(truncated)
        info.setdefault("success", bool(info.get("is_success", False)))
        return _gym_obs_to_dict(obs), float(reward), bool(terminated or truncated), info

    def close(self) -> None:
        self._env.close()


@ENVS.register("mujoco")
class MuJoCoEnv(_GymnasiumAdapter):
    def __init__(self, env_id: str, **env_kwargs: Any) -> None:
        try:
            import gymnasium
        except ImportError as e:
            raise RuntimeError(
                "MuJoCoEnv requires `pip install gymnasium[mujoco]`"
            ) from e
        super().__init__(gymnasium.make(env_id, **env_kwargs))


@ENVS.register("maniskill")
class ManiSkillEnv(_GymnasiumAdapter):
    def __init__(self, env_id: str, **env_kwargs: Any) -> None:
        try:
            import gymnasium
            import mani_skill.envs  # noqa: F401 — registers ManiSkill env ids
        except ImportError as e:
            raise RuntimeError("ManiSkillEnv requires `pip install mani-skill`") from e
        super().__init__(gymnasium.make(env_id, **env_kwargs))


@ENVS.register("isaac_lab")
class IsaacLabEnv(SimulationEnv):
    """Wraps the Isaac Lab task envs defined in ``rl_engine/`` (e.g. the nav
    and arm envs trained by rl_engine/scripts/train_*.py). Isaac Lab is
    vectorized; this adapter exposes env index 0 for the framework's
    single-env collectors, while ``vec_env`` stays available for the
    OnlineRLTrainer which trains directly on the batch."""

    def __init__(self, task: str, num_envs: int = 1, **kwargs: Any) -> None:
        try:
            from isaaclab.app import AppLauncher  # noqa: F401
        except ImportError as e:
            raise RuntimeError(
                "IsaacLabEnv requires Isaac Lab (isaaclab>=1.1.0) — only "
                "available on the GPU workstation; see rl_engine/README"
            ) from e
        import gymnasium
        import rl_engine.tasks  # noqa: F401 — registers OmniBot Isaac tasks

        self.vec_env = gymnasium.make(task, num_envs=num_envs, **kwargs)
        self.action_dim = int(np.prod(self.vec_env.action_space.shape[-1:]))

    def reset(self, **kwargs: Any) -> Observation:
        obs, _ = self.vec_env.reset(**kwargs)
        return _gym_obs_to_dict(_first(obs))

    def step(
        self, action: np.ndarray
    ) -> Tuple[Observation, float, bool, Dict[str, Any]]:
        import torch

        batch = torch.as_tensor(action, dtype=torch.float32).unsqueeze(0)
        obs, reward, terminated, truncated, info = self.vec_env.step(batch)
        done = bool(_first(terminated)) or bool(_first(truncated))
        return _gym_obs_to_dict(_first(obs)), float(_first(reward)), done, dict(info)

    def close(self) -> None:
        self.vec_env.close()


@ENVS.register("gazebo")
class GazeboEnv(SimulationEnv):
    """Hardware-in-the-loop env over live ROS 2 topics (Gazebo Harmonic via
    ros_gz_bridge, ROS_DOMAIN_ID=30). Stepping publishes /cmd_vel and
    /arm/joint_commands and samples /odom + /arm/joint_states at policy_hz.

    Episode reset requires the sim to be restarted externally (Gazebo has no
    in-band reset service in our bringup) — ``reset()`` therefore only
    re-zeros commands and waits for fresh observations.
    """

    def __init__(self, policy_hz: float = 10.0) -> None:
        try:
            import rclpy  # noqa: F401
        except ImportError as e:
            raise RuntimeError("GazeboEnv requires a sourced ROS 2 environment") from e
        raise NotImplementedError(
            "GazeboEnv is an integration point; implement against "
            "learning_engine/ros2/topics.py when running on a ROS machine."
        )

    def reset(self, **kwargs: Any) -> Observation:  # pragma: no cover
        raise NotImplementedError

    def step(self, action: np.ndarray):  # pragma: no cover
        raise NotImplementedError


def _first(x: Any) -> Any:
    """First element of a batched tensor/array/dict from a vectorized env."""
    if isinstance(x, dict):
        return {k: _first(v) for k, v in x.items()}
    if hasattr(x, "cpu"):
        x = x.cpu().numpy()
    arr = np.asarray(x)
    return arr[0] if arr.ndim > 0 else arr
