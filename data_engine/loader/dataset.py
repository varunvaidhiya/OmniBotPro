"""
dataset.py
----------
Thin PyTorch Dataset wrapper over a LeRobot v2.0 dataset on disk.

Prefers lerobot.common.datasets.lerobot_dataset.LeRobotDataset when the
lerobot package is installed.  Falls back to a lightweight pure-pandas/pyarrow
implementation so the data_engine works without a full lerobot install.
"""

from __future__ import annotations

from pathlib import Path

import numpy as np
import pandas as pd
import torch
from torch.utils.data import Dataset


def load_dataset(dataset_root: str | Path, split: str = "train", **kwargs):
    """
    Return a Dataset for the given LeRobot dataset root.
    Tries LeRobotDataset first; falls back to LeRobotDatasetLite.
    """
    try:
        from lerobot.common.datasets.lerobot_dataset import LeRobotDataset

        return LeRobotDataset(str(dataset_root), split=split, **kwargs)
    except ImportError:
        return LeRobotDatasetLite(dataset_root, **kwargs)


class LeRobotDatasetLite(Dataset):
    """
    Minimal read-only Dataset over a LeRobot v2.0 dataset stored on disk.
    Loads all Parquet files, returns (observation_state, action) tensors.
    Images are NOT decoded here — use the full LeRobotDataset for image training.
    """

    def __init__(self, dataset_root: str | Path, chunk_size: int = 50):
        self.root = Path(dataset_root)
        self.chunk_size = chunk_size

        parquet_files = sorted((self.root / "data").rglob("*.parquet"))
        if not parquet_files:
            raise FileNotFoundError(
                f"No parquet files found under {self.root / 'data'}"
            )

        frames = pd.concat(
            [pd.read_parquet(p) for p in parquet_files],
            ignore_index=True,
        )
        self._states = np.stack(frames["observation.state"].tolist()).astype(np.float32)
        self._actions = np.stack(frames["action"].tolist()).astype(np.float32)
        self._episode = frames["episode_index"].to_numpy(dtype=np.int64)
        self._done = frames["next.done"].to_numpy(dtype=bool)

        # Build episode-boundary index for chunk sampling
        self._ep_starts: dict[int, int] = {}
        self._ep_ends: dict[int, int] = {}
        for i, ep in enumerate(self._episode):
            if ep not in self._ep_starts:
                self._ep_starts[ep] = i
            self._ep_ends[ep] = i

        # Valid start indices: frames where a full chunk fits inside the episode
        self._valid: list[int] = []
        for ep, start in self._ep_starts.items():
            end = self._ep_ends[ep]
            for idx in range(start, end - chunk_size + 2):
                self._valid.append(idx)

    def __len__(self) -> int:
        return len(self._valid)

    def __getitem__(self, idx: int) -> dict[str, torch.Tensor]:
        start = self._valid[idx]
        end = min(start + self.chunk_size, len(self._states))
        pad = self.chunk_size - (end - start)

        state = torch.from_numpy(self._states[start])
        actions = torch.from_numpy(self._actions[start:end])
        if pad > 0:
            actions = torch.cat([actions, actions[-1:].expand(pad, -1)], dim=0)
        return {
            "observation.state": state,
            "action": actions,  # [chunk_size, action_dim]
            "episode_index": torch.tensor(int(self._episode[start])),
            "frame_index": torch.tensor(start - self._ep_starts[self._episode[start]]),
        }
