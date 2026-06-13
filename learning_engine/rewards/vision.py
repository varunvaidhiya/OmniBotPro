"""Vision-based reward module — task-completion scores from camera frames.

Two implementations of the ``RewardModel`` interface:

- ``VLMRewardModel``  — zero-shot visual task verification with any
  ``VLMClient`` (Claude by default). Best for sparse end-of-episode scoring
  and for labeling data to train a cheaper learned model.
- ``LearnedRewardModel`` — a distilled torch model for dense, per-step
  scoring at policy frequency (the VLM is far too slow for 10–20 Hz).

The intended progression: VLM labels episodes → labels accumulate in the
ReplayDataset → train LearnedRewardModel on them → use it densely.
"""

from __future__ import annotations

import re
from typing import Sequence

import numpy as np

from ..core.interfaces import RewardModel, VLMClient
from ..core.registry import REWARD_MODELS

_SCORE_PROMPT = """You are evaluating whether a robot completed a task.

Task instruction: "{instruction}"

You are shown {n} frames sampled across the episode in chronological order
(first frame = start, last frame = end).

Rate task completion from 0.0 (no progress) to 1.0 (fully completed).
Consider only what is visible. Respond with ONLY a number between 0.0 and 1.0."""


def sample_frames(frames: Sequence[np.ndarray], k: int = 4) -> Sequence[np.ndarray]:
    """Evenly sample k frames (always including first and last)."""
    if len(frames) <= k:
        return frames
    idx = np.linspace(0, len(frames) - 1, k).round().astype(int)
    return [frames[i] for i in idx]


@REWARD_MODELS.register("vlm")
class VLMRewardModel(RewardModel):
    def __init__(self, client: VLMClient, max_frames: int = 4) -> None:
        self.client = client
        self.max_frames = max_frames

    def score(self, instruction: str, frames: Sequence[np.ndarray]) -> float:
        picked = sample_frames(frames, self.max_frames)
        prompt = _SCORE_PROMPT.format(instruction=instruction, n=len(picked))
        reply = self.client.complete(prompt, images=picked)
        return _parse_score(reply)


@REWARD_MODELS.register("learned")
class LearnedRewardModel(RewardModel):
    """Distilled per-frame reward model (image + instruction → score).

    Loads a TorchScript checkpoint trained on VLM-labeled episodes
    (training script is a roadmap item — see ARCHITECTURE.md §10)."""

    def __init__(self, checkpoint_path: str, device: str = "cuda") -> None:
        try:
            import torch
        except ImportError as e:
            raise RuntimeError("LearnedRewardModel requires torch") from e
        self._torch = torch
        self.device = device
        self.model = torch.jit.load(checkpoint_path, map_location=device).eval()

    def score(self, instruction: str, frames: Sequence[np.ndarray]) -> float:
        torch = self._torch
        frame = np.asarray(frames[-1], dtype=np.float32) / 255.0
        x = torch.from_numpy(frame).permute(2, 0, 1).unsqueeze(0).to(self.device)
        with torch.no_grad():
            return float(self.model(x).sigmoid().item())


def _parse_score(text: str, default: float = 0.0) -> float:
    """Extract the first float in [0, 1] from a model reply."""
    m = re.search(r"(?:0?\.\d+|[01](?:\.\d+)?)", text)
    if not m:
        return default
    return float(np.clip(float(m.group()), 0.0, 1.0))
