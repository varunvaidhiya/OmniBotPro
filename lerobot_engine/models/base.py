"""Abstract base class for all visuomotor policy adapters.

Every model backend (SmolVLA, ACT, Diffusion, OpenVLA, …) implements this
interface so training, inference, and the ROS node never import model-specific
code directly.
"""

from __future__ import annotations

from abc import ABC, abstractmethod
from typing import Optional

import numpy as np


class PolicyAdapter(ABC):
    """Uniform interface every policy backend must satisfy.

    Lifecycle:
        adapter = make_policy("smolvla")
        adapter.load("lerobot/smolvla_base", device="cuda")
        adapter.reset()                          # between episodes
        action = adapter.select_action(obs_dict) # at each control step

    Training access:
        adapter.model   # the raw nn.Module for optimizer / scheduler
    """

    # ------------------------------------------------------------------
    # Lifecycle
    # ------------------------------------------------------------------

    @abstractmethod
    def load(self, checkpoint: str, device: str = "cuda") -> None:
        """Load weights from a HuggingFace hub ID or local checkpoint path."""

    @abstractmethod
    def reset(self) -> None:
        """Reset any recurrent state or action-chunk queue between episodes."""

    # ------------------------------------------------------------------
    # Inference
    # ------------------------------------------------------------------

    @abstractmethod
    def select_action(self, obs: dict) -> np.ndarray:
        """Return a flat float32 numpy array of shape (action_dim,).

        obs keys must include all entries in image_keys, state_key, and
        (if task_key is not None) task_key. Image tensors are (1, C, H, W)
        float32 on the adapter's device. state is (1, state_dim) float32.
        """

    # ------------------------------------------------------------------
    # Training access
    # ------------------------------------------------------------------

    @property
    def model(self):
        """Return the underlying nn.Module for the training loop.

        Override in subclasses that wrap a trainable model.
        Returns None for adapters that delegate to a remote API.
        """
        return getattr(self, "_policy", None) or getattr(self, "_model", None)

    # ------------------------------------------------------------------
    # Schema — consumed by the recording pipeline and the ROS node
    # ------------------------------------------------------------------

    @property
    def image_keys(self) -> list[str]:
        """Ordered list of image keys this model expects in the obs dict."""
        return ["observation.images.wrist", "observation.images.bev"]

    @property
    def state_key(self) -> str:
        return "observation.state"

    @property
    def task_key(self) -> Optional[str]:
        """Language task key, or None for non-language-conditioned models."""
        return None

    @property
    def action_dim(self) -> int:
        """Number of action dimensions returned by select_action()."""
        return 9

    @property
    def image_size(self) -> tuple[int, int]:
        """(width, height) to resize images to before passing to the model."""
        return (320, 240)
