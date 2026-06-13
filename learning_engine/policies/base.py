"""Baseline policies usable everywhere (no heavy deps), plus the ONNX
policy that runs the rl_engine-exported checkpoints."""

from __future__ import annotations

from typing import Optional

import numpy as np

from ..core.interfaces import Policy
from ..core.registry import POLICIES
from ..core.types import Observation
from ..data import schema


@POLICIES.register("random")
class RandomPolicy(Policy):
    """Uniform random actions — exploration baseline and test stub."""

    def __init__(
        self,
        action_dim: int = schema.MOBILE_MANIP_ACTION_DIM,
        scale: float = 0.1,
        seed: Optional[int] = None,
    ) -> None:
        self.action_dim = action_dim
        self.scale = scale
        self._rng = np.random.default_rng(seed)

    def predict(self, observation: Observation, task: str = "") -> np.ndarray:
        return self._rng.uniform(-self.scale, self.scale, size=self.action_dim).astype(
            np.float32
        )


@POLICIES.register("zero")
class ZeroPolicy(Policy):
    """Always outputs zeros — the safe fallback the verifier can select when
    every candidate plan is rejected."""

    def __init__(self, action_dim: int = schema.MOBILE_MANIP_ACTION_DIM) -> None:
        self.action_dim = action_dim

    def predict(self, observation: Observation, task: str = "") -> np.ndarray:
        return np.zeros(self.action_dim, dtype=np.float32)


@POLICIES.register("onnx")
class OnnxPolicy(Policy):
    """Runs policies exported by ``rl_engine/export/export_policy.py``
    (same format the omnibot_rl ROS nodes consume: flat float32 obs in,
    flat float32 action out).

    Execution providers default to the best ones for this machine
    (TensorRT/CUDA on Jetson and dGPU, CoreML on Apple Silicon, CPU
    elsewhere) via ``learning_engine.hardware``."""

    def __init__(
        self,
        model_path: str,
        obs_key: str = schema.OBS_STATE,
        action_dim: int = 3,
        providers: Optional[list] = None,
        prefer_tensorrt: bool = False,
    ) -> None:
        """``providers`` overrides EP selection outright; otherwise the best
        providers for this machine are chosen. Set ``prefer_tensorrt=True``
        to bake/run a TensorRT engine on a discrete GPU (it is already
        first on Jetson)."""
        try:
            import onnxruntime as ort
        except ImportError as e:
            raise RuntimeError("OnnxPolicy requires `pip install onnxruntime`") from e
        import os

        from ..hardware import onnx_providers

        self.providers = providers or onnx_providers(prefer_tensorrt=prefer_tensorrt)
        self.session = ort.InferenceSession(
            os.path.expanduser(model_path), providers=self.providers
        )
        self.obs_key = obs_key
        self.action_dim = action_dim
        self._input_name = self.session.get_inputs()[0].name

    def predict(self, observation: Observation, task: str = "") -> np.ndarray:
        obs = np.asarray(observation[self.obs_key], dtype=np.float32).reshape(1, -1)
        out = self.session.run(None, {self._input_name: obs})[0]
        return np.asarray(out, dtype=np.float32).reshape(-1)[: self.action_dim]
