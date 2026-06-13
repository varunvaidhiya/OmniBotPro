"""OpenVLA 7B policy adapter (HuggingFace Transformers).

Requires ≥16 GB VRAM. Uses only the wrist camera (single-image VLA).
Action output is 7D from OpenVLA; dims 7-8 are padded with zeros for the
base (vx, vy). Base angular velocity (dim 8) is mapped from action[5] (yaw).
"""

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
    from transformers import AutoModelForVision2Seq, AutoProcessor

    TRANSFORMERS_AVAILABLE = True
except ImportError:
    TRANSFORMERS_AVAILABLE = False


def _select_attn() -> str:
    """Select the best available attention implementation."""
    try:
        import flash_attn  # noqa: F401 — import is the availability probe

        return "flash_attention_2"
    except ImportError:
        pass
    try:
        import torch

        major, minor = map(int, torch.__version__.split(".")[:2])
        if major >= 2 and minor >= 0:
            return "sdpa"  # PyTorch 2.0+ native scaled dot-product attention
    except Exception:
        pass
    return "eager"


@register("openvla")
class OpenVLAAdapter(PolicyAdapter):
    """Wraps the OpenVLA 7B model via HuggingFace Transformers.

    OpenVLA outputs 7D: [dx, dy, dz, d_roll, d_pitch, d_yaw, gripper].
    We map this to the 9D robot action space as:
        dims 0-5  → arm joints (direct pass-through)
        dim  6    → base_vx = 0  (OpenVLA doesn't output base velocity)
        dim  7    → base_vy = 0
        dim  8    → base_vz = 0
    Use SmolVLA for unified arm+base control.
    """

    def __init__(self):
        self._model = None
        self._processor = None
        self._device = None

    def load(self, checkpoint: str, device: str = "cuda") -> None:
        if not TRANSFORMERS_AVAILABLE:
            raise ImportError(
                "transformers is required: pip install transformers accelerate"
            )
        if not TORCH_AVAILABLE:
            raise ImportError("torch is required: pip install torch")

        dev = torch.device(
            device if (device == "cuda" and torch.cuda.is_available()) else "cpu"
        )
        self._device = dev
        self._processor = AutoProcessor.from_pretrained(
            checkpoint, trust_remote_code=True
        )
        self._model = AutoModelForVision2Seq.from_pretrained(
            checkpoint,
            attn_implementation=_select_attn(),
            torch_dtype=torch.bfloat16,
            low_cpu_mem_usage=True,
            trust_remote_code=True,
        ).to(dev)
        self._model.eval()

    def reset(self) -> None:
        pass  # stateless per-step inference

    def select_action(self, obs: dict) -> np.ndarray:
        if self._model is None:
            return np.zeros(self.action_dim, dtype=np.float32)

        try:
            import PIL.Image as PILImage
        except ImportError:
            return np.zeros(self.action_dim, dtype=np.float32)

        img_t = obs.get(self.image_keys[0])
        if img_t is None:
            return np.zeros(self.action_dim, dtype=np.float32)

        arr = img_t.squeeze(0).permute(1, 2, 0).cpu().float().numpy()
        arr = (arr * 255).clip(0, 255).astype(np.uint8)
        pil_img = PILImage.fromarray(arr)

        task = obs.get("task", "complete the manipulation task")
        prompt = f"In: What action should the robot take to {task}?\nOut:"

        inputs = self._processor(prompt, pil_img, return_tensors="pt").to(
            self._device, dtype=torch.bfloat16
        )
        with torch.no_grad():
            action_tokens = self._model.predict_action(**inputs)

        raw = action_tokens.cpu().float().numpy().flatten()
        result = np.zeros(9, dtype=np.float32)
        result[: min(7, len(raw))] = raw[:7]
        return result

    @property
    def image_keys(self) -> list[str]:
        return ["observation.images.wrist"]

    @property
    def task_key(self):
        return "task"

    @property
    def action_dim(self) -> int:
        return 9

    @property
    def image_size(self) -> tuple[int, int]:
        return (224, 224)
