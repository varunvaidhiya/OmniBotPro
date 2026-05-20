"""SmolVLA policy adapter (HuggingFace LeRobot)."""

from __future__ import annotations

import numpy as np

from .base import PolicyAdapter
from .registry import register

try:
    import torch

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

try:
    from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False


@register("smolvla")
class SmolVLAAdapter(PolicyAdapter):
    """Wraps lerobot SmolVLAPolicy with the standard PolicyAdapter interface."""

    def __init__(self):
        self._policy = None
        self._device = None

    def load(self, checkpoint: str, device: str = "cuda") -> None:
        if not LEROBOT_AVAILABLE:
            raise ImportError(
                "lerobot is required for SmolVLA.\n"
                '  pip install "lerobot @ git+https://github.com/huggingface/lerobot.git"'
            )
        if not TORCH_AVAILABLE:
            raise ImportError("torch is required: pip install torch")

        dev = torch.device(
            device if (device == "cuda" and torch.cuda.is_available()) else "cpu"
        )
        self._device = dev
        self._policy = SmolVLAPolicy.from_pretrained(checkpoint)
        self._policy = self._policy.to(dev)
        self._policy.eval()

    def reset(self) -> None:
        if self._policy is not None and hasattr(self._policy, "reset"):
            self._policy.reset()

    def select_action(self, obs: dict) -> np.ndarray:
        if self._policy is None:
            return np.zeros(self.action_dim, dtype=np.float32)
        with torch.no_grad():
            action = self._policy.select_action(obs)
        arr = action.cpu().numpy() if hasattr(action, "cpu") else np.array(action)
        return arr[0] if arr.ndim == 2 else arr

    @property
    def image_keys(self) -> list[str]:
        return ["observation.images.wrist", "observation.images.bev"]

    @property
    def task_key(self):
        return "task"

    @property
    def action_dim(self) -> int:
        return 9

    @property
    def image_size(self) -> tuple[int, int]:
        return (320, 240)
