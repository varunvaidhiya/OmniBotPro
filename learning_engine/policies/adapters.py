"""Foundation-model policy adapters.

Generic rule: a foundation model enters the framework as a ``Policy`` (and
optionally a ``VLMClient`` for judging). Adding GR00T, RT-2-style models or
a future custom VLA means writing one adapter class here — nothing else in
the framework changes.

The adapters reuse the existing serving code rather than re-loading
checkpoints: OpenVLA through the ``vla_serve`` FastAPI server (or in-process
via ``vla_serve.models``), SmolVLA through ``lerobot``.
"""

from __future__ import annotations

import json
import urllib.request
from typing import Any

import numpy as np

from ..core.interfaces import Policy
from ..core.registry import POLICIES
from ..core.types import Observation
from ..data import schema


@POLICIES.register("vla_serve")
class VlaServePolicy(Policy):
    """Calls a running ``packages/vla_serve`` server (``POST /predict``).
    This is how the learning loop evaluates whatever VLA the robot is
    actually serving, regardless of which model class is loaded."""

    def __init__(
        self,
        url: str = "http://localhost:8000",
        image_key: str = schema.OBS_IMAGE_FRONT,
        action_dim: int = 7,
    ) -> None:
        self.url = url.rstrip("/")
        self.image_key = image_key
        self.action_dim = action_dim

    def predict(self, observation: Observation, task: str = "") -> np.ndarray:
        import base64

        img = np.asarray(observation[self.image_key], dtype=np.uint8)
        payload = json.dumps(
            {
                "instruction": task,
                "image_b64": base64.b64encode(img.tobytes()).decode(),
                "image_shape": list(img.shape),
            }
        ).encode()
        req = urllib.request.Request(
            f"{self.url}/predict",
            data=payload,
            headers={"Content-Type": "application/json"},
        )
        with urllib.request.urlopen(req, timeout=30) as resp:
            doc = json.loads(resp.read())
        return np.asarray(doc["action"], dtype=np.float32)[: self.action_dim]


@POLICIES.register("smolvla")
class SmolVLAPolicy(Policy):
    """In-process SmolVLA (lerobot) — the unified 9-DOF mobile-manipulation
    policy. Mirrors smolvla_node.py's observation mapping so a policy
    evaluated here behaves identically when deployed via ROS."""

    def __init__(
        self, checkpoint_path: str = "lerobot/smolvla_base", device: str = "auto"
    ) -> None:
        try:
            import torch  # noqa: F401
            from lerobot.common.policies.smolvla.modeling_smolvla import (
                SmolVLAPolicy as _LP,
            )
        except ImportError as e:
            raise RuntimeError(
                "SmolVLAPolicy requires `pip install lerobot torch`"
            ) from e
        from ..hardware import resolve_device

        self.device = resolve_device(device)
        self._policy = _LP.from_pretrained(checkpoint_path).to(self.device).eval()
        self.action_dim = schema.MOBILE_MANIP_ACTION_DIM

    def predict(self, observation: Observation, task: str = "") -> np.ndarray:
        import torch

        def img(key: str) -> "torch.Tensor":
            arr = np.asarray(observation[key], dtype=np.float32) / 255.0
            return torch.from_numpy(arr).permute(2, 0, 1).unsqueeze(0).to(self.device)

        batch = {
            "observation.state": torch.as_tensor(
                observation[schema.OBS_STATE], dtype=torch.float32
            )
            .reshape(1, -1)
            .to(self.device),
            "observation.images.wrist": img(schema.OBS_IMAGE_WRIST),
            "observation.images.bev": img(schema.OBS_IMAGE_BEV),
            "task": task,
        }
        with torch.no_grad():
            action = self._policy.select_action(batch)
        return action.cpu().numpy().reshape(-1)[: self.action_dim]


@POLICIES.register("openvla")
class OpenVLAPolicy(Policy):
    """In-process OpenVLA via the existing ``vla_serve`` model wrapper
    (packages/vla_serve/vla_serve/models/openvla.py). 7-D action; the base
    mapping (action[0]→vx, action[1]→vy, action[5]→ωz) matches vla_node."""

    def __init__(
        self,
        model_path: str = "openvla/openvla-7b",
        device: str = "auto",
        load_in_4bit: bool = False,
    ) -> None:
        try:
            from vla_serve.models.openvla import OpenVLAModel
        except ImportError as e:
            raise RuntimeError(
                "OpenVLAPolicy requires `pip install -e packages/vla_serve` "
                "plus torch/transformers (GPU workstation only)"
            ) from e
        from ..hardware import resolve_device

        self.device = resolve_device(device)
        self._model = OpenVLAModel(
            model_path=model_path, device=self.device, load_in_4bit=load_in_4bit
        )
        self._model.load()
        self.action_dim = 7

    def predict(self, observation: Observation, task: str = "") -> np.ndarray:
        image = np.asarray(observation[schema.OBS_IMAGE_FRONT], dtype=np.uint8)
        action = self._model.predict(image=image, instruction=task)
        return np.asarray(action, dtype=np.float32)[: self.action_dim]


def make_policy(name: str, **kwargs: Any) -> Policy:
    """Convenience factory over the POLICIES registry."""
    return POLICIES.create(name, **kwargs)
