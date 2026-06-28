"""Lightweight LeRobot v2.0 dataset reader — no torch required.

Reads the on-disk dataset written by :mod:`ohho.data.writer`. Loads meta files
and episode data (Parquet via pyarrow, or JSON Lines fallback) into plain Python
lists/dicts so the trainer and evaluator can inspect data without a heavy ML
stack.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List


@dataclass
class DatasetInfo:
    """Contents of ``meta/info.json``."""

    codebase_version: str = "2.0"
    repo_id: str = ""
    fps: float = 10.0
    total_episodes: int = 0
    total_frames: int = 0
    features: Dict[str, Any] = field(default_factory=dict)
    tasks: List[str] = field(default_factory=list)


@dataclass
class DatasetFrame:
    """One row from the dataset."""

    observation_state: List[float]
    action: List[float]
    timestamp: float
    frame_index: int
    episode_index: int
    task_index: int
    next_done: bool


class DatasetReader:
    """Reads a LeRobot v2.0 dataset from disk (no torch/lerobot needed)."""

    def __init__(self, root: str) -> None:
        self.root = Path(root).expanduser()
        self.info = self._load_info()
        self._episodes = self._load_episodes()
        self._tasks = self._load_tasks()

    def _load_info(self) -> DatasetInfo:
        p = self.root / "meta" / "info.json"
        if not p.exists():
            return DatasetInfo()
        with open(p, encoding="utf-8") as f:
            d = json.load(f)
        return DatasetInfo(
            codebase_version=d.get("codebase_version", "2.0"),
            repo_id=d.get("repo_id", ""),
            fps=d.get("fps", 10.0),
            total_episodes=d.get("total_episodes", 0),
            total_frames=d.get("total_frames", 0),
            features=d.get("features", {}),
            tasks=d.get("tasks", []),
        )

    def _load_episodes(self) -> List[dict]:
        p = self.root / "meta" / "episodes.jsonl"
        if not p.exists():
            return []
        with open(p, encoding="utf-8") as f:
            return [json.loads(line) for line in f if line.strip()]

    def _load_tasks(self) -> List[dict]:
        p = self.root / "meta" / "tasks.jsonl"
        if not p.exists():
            return []
        with open(p, encoding="utf-8") as f:
            return [json.loads(line) for line in f if line.strip()]

    @property
    def episode_count(self) -> int:
        return self.info.total_episodes

    @property
    def frame_count(self) -> int:
        return self.info.total_frames

    @property
    def state_dim(self) -> int:
        feat = self.info.features.get("observation.state", {})
        shape = feat.get("shape", [0])
        return shape[0] if shape else 0

    @property
    def action_dim(self) -> int:
        feat = self.info.features.get("action", {})
        shape = feat.get("shape", [0])
        return shape[0] if shape else 0

    def task_text(self, task_index: int) -> str:
        for t in self._tasks:
            if t.get("task_index") == task_index:
                return t.get("task", "")
        return ""

    def load_episode(self, episode_index: int) -> List[DatasetFrame]:
        """Load all frames for one episode from disk."""
        chunk = episode_index // 1000
        chunk_dir = self.root / "data" / f"chunk-{chunk:03d}"
        # Try Parquet first, then JSON Lines
        parquet_path = chunk_dir / f"episode_{episode_index:06d}.parquet"
        jsonl_path = chunk_dir / f"episode_{episode_index:06d}.jsonl"
        if parquet_path.exists():
            return self._load_parquet(parquet_path)
        if jsonl_path.exists():
            return self._load_jsonl(jsonl_path)
        raise FileNotFoundError(f"episode {episode_index} not found in {chunk_dir}")

    def _load_parquet(self, path: Path) -> List[DatasetFrame]:
        import pyarrow.parquet as pq

        table = pq.read_table(path)
        frames = []
        for i in range(table.num_rows):
            row = table.slice(i, 1).to_pydict()
            frames.append(
                DatasetFrame(
                    observation_state=row["observation.state"][0],
                    action=row["action"][0],
                    timestamp=row["timestamp"][0],
                    frame_index=row["frame_index"][0],
                    episode_index=row["episode_index"][0],
                    task_index=row["task_index"][0],
                    next_done=row["next.done"][0],
                )
            )
        return frames

    def _load_jsonl(self, path: Path) -> List[DatasetFrame]:
        frames = []
        with open(path, encoding="utf-8") as f:
            for line in f:
                if not line.strip():
                    continue
                d = json.loads(line)
                frames.append(
                    DatasetFrame(
                        observation_state=d["observation.state"],
                        action=d["action"],
                        timestamp=d["timestamp"],
                        frame_index=d["frame_index"],
                        episode_index=d["episode_index"],
                        task_index=d["task_index"],
                        next_done=d["next.done"],
                    )
                )
        return frames

    def iter_episodes(self):
        """Yield (episode_meta, frames) for each episode."""
        for ep_meta in self._episodes:
            idx = ep_meta["episode_index"]
            yield ep_meta, self.load_episode(idx)

    def all_frames(self) -> List[DatasetFrame]:
        """Load every frame across all episodes into memory."""
        out: List[DatasetFrame] = []
        for ep_meta in self._episodes:
            out.extend(self.load_episode(ep_meta["episode_index"]))
        return out

    def stats(self) -> Dict[str, Dict[str, float]]:
        """Per-dimension min/max/mean for observation.state and action."""
        frames = self.all_frames()
        if not frames:
            return {}
        n_state = len(frames[0].observation_state)
        n_action = len(frames[0].action)
        result: Dict[str, Dict[str, float]] = {}
        for key, n, getter in [
            ("observation.state", n_state, lambda f: f.observation_state),
            ("action", n_action, lambda f: f.action),
        ]:
            cols = [[getter(f)[i] for f in frames] for i in range(n)]
            result[key] = {f"dim{i}_min": min(c) for i, c in enumerate(cols)}
            result[key].update({f"dim{i}_max": max(c) for i, c in enumerate(cols)})
            result[key].update(
                {f"dim{i}_mean": sum(c) / len(c) for i, c in enumerate(cols)}
            )
        return result
