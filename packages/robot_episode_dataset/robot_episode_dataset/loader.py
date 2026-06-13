"""
robot_episode_dataset.loader
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
PyTorch Dataset that reads episodes stored in LeRobot v2.0 format
(Parquet tables + H.264 MP4 video files).
"""

from __future__ import annotations

from pathlib import Path
from typing import Any, Callable, Dict, List, Optional

import numpy as np
import pyarrow.parquet as pq  # guaranteed by setup_requires


class EpisodeDataset:
    """
    Reads a LeRobot v2.0 episode dataset from disk.

    Directory layout expected::

        <root>/
          data/chunk-000/episode_000000.parquet
          videos/chunk-000/observation.images.<name>/episode_000000.mp4
          meta/info.json

    Args:
        root:         Path to the dataset root directory.
        split:        'train', 'val', or None (loads all).
        transform:    Optional callable applied to each sample dict.
        image_keys:   List of image keys to load (e.g. ['observation.images.bev']).
                      Pass None to skip image loading (faster for state-only tasks).
    """

    def __init__(
        self,
        root: str,
        split: Optional[str] = None,
        transform: Optional[Callable] = None,
        image_keys: Optional[List[str]] = None,
    ) -> None:
        self._root = Path(root)
        self._transform = transform
        self._image_keys = image_keys

        # Build an index of (parquet_file, row_index) pairs — O(1) memory per
        # sample rather than loading all rows into Python lists.
        self._index: List[tuple] = []
        for chunk_dir in sorted((self._root / "data").glob("chunk-*")):
            for pq_file in sorted(chunk_dir.glob("episode_*.parquet")):
                pf = pq.ParquetFile(pq_file)
                n_rows = pf.metadata.num_rows
                for i in range(n_rows):
                    self._index.append((str(pq_file), i))

        # Video capture cache: keyed by video path, reused across __getitem__
        # calls that access the same video file.
        self._video_cache: Dict[str, Any] = {}

    def __len__(self) -> int:
        return len(self._index)

    def __getitem__(self, idx: int) -> Dict[str, Any]:
        pq_path, row_idx = self._index[idx]
        table = pq.read_table(pq_path)
        row = {col: table[col][row_idx].as_py() for col in table.schema.names}
        row["_parquet_file"] = pq_path

        if self._image_keys:
            frame_idx = row.get("frame_index", 0)
            ep_idx = row.get("episode_index", 0)
            chunk = f"chunk-{ep_idx // 1000:03d}"
            ep_str = f"episode_{ep_idx:06d}"

            for key in self._image_keys:
                video_path = str(self._root / "videos" / chunk / key / f"{ep_str}.mp4")
                row[key] = self._load_video_frame(video_path, frame_idx)

        if self._transform:
            row = self._transform(row)

        return row

    def _load_video_frame(self, path: str, frame_idx: int):
        """Read a single frame from an MP4 video, caching the VideoCapture."""
        try:
            import cv2
        except ImportError:
            raise ImportError("opencv-python is required for image loading.")

        cap = self._video_cache.get(path)
        if cap is None:
            cap = cv2.VideoCapture(path)
            self._video_cache[path] = cap
        cap.set(cv2.CAP_PROP_POS_FRAMES, frame_idx)
        ret, frame = cap.read()
        if not ret:
            return np.zeros((1, 1, 3), dtype=np.uint8)

        return cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    def as_torch_dataset(self):
        """Wrap in a torch.utils.data.Dataset."""
        try:
            from torch.utils.data import Dataset
        except ImportError:
            raise ImportError("torch is required: pip install torch")

        parent = self

        class _TorchWrapper(Dataset):
            def __len__(self):
                return len(parent)

            def __getitem__(self, idx):
                return parent[idx]

        return _TorchWrapper()
