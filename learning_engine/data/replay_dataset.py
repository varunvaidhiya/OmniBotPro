"""On-disk replay dataset — the single store for all collected experience.

Layout::

    <root>/
        index.jsonl                 # one JSON line per episode (metadata)
        episodes/<episode_id>/
            meta.json               # EpisodeMeta + per-step reward breakdowns
            steps.npz               # stacked arrays: actions, every obs key

Arrays are stacked over time, so ``steps.npz`` holds e.g. ``state`` with
shape (T, 9) and ``images.wrist`` with shape (T, H, W, 3). Image streams may
be large; callers can pass ``store_images=False`` to keep only
low-dimensional state (useful for RL replay where images are not needed).

This format is deliberately simple (npz + json, no DB) so it works on the
Pi, in CI and inside Isaac containers. Export to the LeRobot HF format for
BC/VLA fine-tuning is provided via ``export_lerobot``.
"""

from __future__ import annotations

import json
import shutil
from dataclasses import asdict
from pathlib import Path
from typing import Dict, Iterator, List, Optional

import numpy as np

from ..core.types import (
    DataSource,
    Episode,
    EpisodeMeta,
    RewardBreakdown,
    Step,
    TaskOutcome,
)
from . import schema


class ReplayDataset:
    def __init__(self, root: str, store_images: bool = True) -> None:
        self.root = Path(root).expanduser()
        self.store_images = store_images
        self.episodes_dir = self.root / "episodes"
        self.index_path = self.root / "index.jsonl"
        self.episodes_dir.mkdir(parents=True, exist_ok=True)
        self._index: Dict[str, dict] = {}
        if self.index_path.exists():
            for line in self.index_path.read_text().splitlines():
                if line.strip():
                    entry = json.loads(line)
                    self._index[entry["episode_id"]] = entry

    # ------------------------------------------------------------------ add
    def add_episode(self, episode: Episode) -> str:
        meta = episode.meta
        ep_dir = self.episodes_dir / meta.episode_id
        ep_dir.mkdir(parents=True, exist_ok=True)

        arrays: Dict[str, np.ndarray] = {
            "actions": np.stack([s.action for s in episode.steps]),
            "timestamps": np.array([s.timestamp for s in episode.steps]),
        }
        obs_keys = episode.steps[0].observation.keys() if episode.steps else []
        for key in obs_keys:
            if not self.store_images and key in schema.IMAGE_KEYS:
                continue
            arrays[f"obs/{key}"] = np.stack([s.observation[key] for s in episode.steps])
        np.savez_compressed(ep_dir / "steps.npz", **arrays)

        meta_doc = {
            **asdict(meta),
            "source": meta.source.value,
            "outcome": meta.outcome.value,
            "length": len(episode),
            "schema_version": schema.SCHEMA_VERSION,
            "rewards": [
                s.reward.to_dict() if s.reward else None for s in episode.steps
            ],
            "step_infos": [_jsonable(s.info) for s in episode.steps],
        }
        (ep_dir / "meta.json").write_text(json.dumps(meta_doc, indent=1))
        self._write_index_entry(meta_doc)
        return meta.episode_id

    def update_meta(self, episode_id: str, **changes: object) -> None:
        """Re-label an episode after evaluation (outcome, success_score...)."""
        ep_dir = self.episodes_dir / episode_id
        meta_doc = json.loads((ep_dir / "meta.json").read_text())
        for k, v in changes.items():
            meta_doc[k] = v.value if isinstance(v, (DataSource, TaskOutcome)) else v
        (ep_dir / "meta.json").write_text(json.dumps(meta_doc, indent=1))
        self._write_index_entry(meta_doc, rewrite=True)

    # ----------------------------------------------------------------- read
    def get(self, episode_id: str) -> Episode:
        ep_dir = self.episodes_dir / episode_id
        meta_doc = json.loads((ep_dir / "meta.json").read_text())
        data = np.load(ep_dir / "steps.npz")
        length = meta_doc["length"]
        meta = EpisodeMeta(
            episode_id=meta_doc["episode_id"],
            task_instruction=meta_doc["task_instruction"],
            source=DataSource(meta_doc["source"]),
            outcome=TaskOutcome(meta_doc["outcome"]),
            success_score=meta_doc["success_score"],
            fps=meta_doc["fps"],
            robot=meta_doc["robot"],
            environment=meta_doc["environment"],
            created_at=meta_doc["created_at"],
            tags=meta_doc["tags"],
            extra=meta_doc["extra"],
        )
        steps: List[Step] = []
        obs_keys = [k[4:] for k in data.files if k.startswith("obs/")]
        for t in range(length):
            reward_doc = meta_doc["rewards"][t]
            steps.append(
                Step(
                    observation={k: data[f"obs/{k}"][t] for k in obs_keys},
                    action=data["actions"][t],
                    timestamp=float(data["timestamps"][t]),
                    reward=RewardBreakdown(
                        **{k: reward_doc[k] for k in ("terms", "weights")}
                    )
                    if reward_doc
                    else None,
                    info=meta_doc["step_infos"][t],
                )
            )
        return Episode(meta=meta, steps=steps)

    def episode_ids(
        self,
        source: Optional[DataSource] = None,
        outcome: Optional[TaskOutcome] = None,
        task_contains: str = "",
    ) -> List[str]:
        out = []
        for eid, entry in self._index.items():
            if source and entry["source"] != source.value:
                continue
            if outcome and entry["outcome"] != outcome.value:
                continue
            if task_contains and task_contains not in entry["task_instruction"]:
                continue
            out.append(eid)
        return sorted(out, key=lambda e: self._index[e]["created_at"])

    def iter_episodes(self, **filters: object) -> Iterator[Episode]:
        for eid in self.episode_ids(**filters):  # type: ignore[arg-type]
            yield self.get(eid)

    def __len__(self) -> int:
        return len(self._index)

    def stats(self) -> Dict[str, int]:
        s: Dict[str, int] = {"episodes": len(self._index)}
        for entry in self._index.values():
            s[f"outcome/{entry['outcome']}"] = (
                s.get(f"outcome/{entry['outcome']}", 0) + 1
            )
            s[f"source/{entry['source']}"] = s.get(f"source/{entry['source']}", 0) + 1
        return s

    def remove(self, episode_id: str) -> None:
        shutil.rmtree(self.episodes_dir / episode_id, ignore_errors=True)
        self._index.pop(episode_id, None)
        self._rewrite_index()

    # --------------------------------------------------------------- export
    def export_lerobot(self, repo_id: str, output_dir: str) -> str:
        """Export to LeRobot HF format for SmolVLA/BC fine-tuning via
        ``packages/robot_episode_dataset``. Requires `lerobot` installed."""
        from robot_episode_dataset import writer  # noqa: F401  (optional dep)

        raise NotImplementedError(
            "LeRobot export is wired through packages/robot_episode_dataset; "
            "see learning_engine/ARCHITECTURE.md §4 for the key map "
            f"(schema.LEROBOT_KEY_MAP). Target: {repo_id} -> {output_dir}"
        )

    # -------------------------------------------------------------- private
    def _write_index_entry(self, meta_doc: dict, rewrite: bool = False) -> None:
        entry = {
            k: meta_doc[k]
            for k in (
                "episode_id",
                "task_instruction",
                "source",
                "outcome",
                "success_score",
                "length",
                "created_at",
                "environment",
                "tags",
            )
        }
        self._index[entry["episode_id"]] = entry
        if rewrite:
            self._rewrite_index()
        else:
            with self.index_path.open("a") as f:
                f.write(json.dumps(entry) + "\n")

    def _rewrite_index(self) -> None:
        with self.index_path.open("w") as f:
            for entry in self._index.values():
                f.write(json.dumps(entry) + "\n")


def _jsonable(d: dict) -> dict:
    out = {}
    for k, v in d.items():
        if isinstance(v, np.ndarray):
            out[k] = v.tolist()
        elif isinstance(v, (np.floating, np.integer)):
            out[k] = v.item()
        elif isinstance(v, (str, int, float, bool, list, dict, type(None))):
            out[k] = v
        else:
            out[k] = str(v)
    return out
