"""
bag_to_omnibot.py
-----------------
Convert a single ROS 2 bag into a LeRobot v2.0 dataset episode.

Output layout (appended to an existing or new dataset root):
    <dataset_root>/
        meta/
            info.json
            tasks.jsonl
            episodes.jsonl
            stats.json         (written/updated after all episodes are added)
        data/
            chunk-000/
                episode_XXXXXX.parquet
        videos/
            chunk-000/
                observation.images.front/
                    episode_XXXXXX.mp4
                observation.images.wrist/
                    episode_XXXXXX.mp4

Usage (CLI):
    python -m data_engine.ingestion.bag_to_omnibot \
        --bag /data/bags/ep000 \
        --dataset /data/lerobot/pick_place \
        --task "pick up the red cube" \
        --fps 30
"""

from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Any

import click
import cv2
import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq

from data_engine.ingestion.ros_parser import ROSBagParser
from data_engine.ingestion.sync_topics import TopicSynchronizer
from data_engine.schema.constants import (
    MOBILE_MANIP_ACTION_SPEC,
    MOBILE_MANIP_STATE_SPEC,
    CAMERA_FRONT,
    CAMERA_WRIST,
)

# ── Constants ────────────────────────────────────────────────────────────────

CHUNK_SIZE_EPISODES = 1000  # episodes per chunk folder
CODEBASE_VERSION = "v2.0"

_PARQUET_SCHEMA = pa.schema(
    [
        pa.field("observation.state", pa.list_(pa.float32())),
        pa.field("action", pa.list_(pa.float32())),
        pa.field("timestamp", pa.float64()),
        pa.field("frame_index", pa.int64()),
        pa.field("episode_index", pa.int64()),
        pa.field("index", pa.int64()),
        pa.field("task_index", pa.int64()),
        pa.field("next.done", pa.bool_()),
    ]
)


# ── Helpers ──────────────────────────────────────────────────────────────────


def _load_meta(dataset_root: Path) -> dict[str, Any]:
    info_path = dataset_root / "meta" / "info.json"
    if info_path.exists():
        return json.loads(info_path.read_text())
    return {
        "codebase_version": CODEBASE_VERSION,
        "robot_type": "omnibot",
        "fps": 30,
        "total_episodes": 0,
        "total_frames": 0,
        "chunks_size": CHUNK_SIZE_EPISODES,
        "features": {
            "observation.state": {
                "dtype": "float32",
                "shape": [MOBILE_MANIP_STATE_SPEC.dim],
                "names": MOBILE_MANIP_STATE_SPEC.names,
            },
            "action": {
                "dtype": "float32",
                "shape": [MOBILE_MANIP_ACTION_SPEC.dim],
                "names": MOBILE_MANIP_ACTION_SPEC.names,
            },
            "observation.images.front": {
                "dtype": "video",
                "shape": [CAMERA_FRONT.height, CAMERA_FRONT.width, 3],
                "info": {
                    "video.fps": 30,
                    "video.codec": "h264",
                    "video.pix_fmt": "yuv420p",
                },
            },
            "observation.images.wrist": {
                "dtype": "video",
                "shape": [CAMERA_WRIST.height, CAMERA_WRIST.width, 3],
                "info": {
                    "video.fps": 30,
                    "video.codec": "h264",
                    "video.pix_fmt": "yuv420p",
                },
            },
        },
    }


def _save_meta(dataset_root: Path, info: dict) -> None:
    meta_dir = dataset_root / "meta"
    meta_dir.mkdir(parents=True, exist_ok=True)
    (meta_dir / "info.json").write_text(json.dumps(info, indent=2))


def _append_jsonl(path: Path, record: dict) -> None:
    with path.open("a") as f:
        f.write(json.dumps(record) + "\n")


def _ensure_task(dataset_root: Path, task_name: str) -> int:
    """Return task_index for task_name, creating it if new."""
    tasks_path = dataset_root / "meta" / "tasks.jsonl"
    tasks_path.parent.mkdir(parents=True, exist_ok=True)
    if tasks_path.exists():
        for line in tasks_path.read_text().splitlines():
            rec = json.loads(line)
            if rec["task"] == task_name:
                return rec["task_index"]
    # New task — count existing lines
    count = sum(1 for _ in tasks_path.open()) if tasks_path.exists() else 0
    _append_jsonl(tasks_path, {"task_index": count, "task": task_name})
    return count


def _write_video(frames: list[np.ndarray], path: Path, fps: int) -> None:
    """Write list of BGR numpy frames to an H.264 MP4."""
    if not frames:
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    h, w = frames[0].shape[:2]
    fourcc = cv2.VideoWriter_fourcc(*"mp4v")
    writer = cv2.VideoWriter(str(path), fourcc, fps, (w, h))
    for frame in frames:
        if frame.shape[:2] != (h, w):
            frame = cv2.resize(frame, (w, h))
        writer.write(frame)
    writer.release()


def _compute_stats(states: list[list[float]], actions: list[list[float]]) -> dict:
    s = np.array(states, dtype=np.float32)
    a = np.array(actions, dtype=np.float32)
    return {
        "observation.state": {
            "mean": s.mean(axis=0).tolist(),
            "std": s.std(axis=0).tolist(),
            "min": s.min(axis=0).tolist(),
            "max": s.max(axis=0).tolist(),
        },
        "action": {
            "mean": a.mean(axis=0).tolist(),
            "std": a.std(axis=0).tolist(),
            "min": a.min(axis=0).tolist(),
            "max": a.max(axis=0).tolist(),
        },
    }


# ── Core conversion ──────────────────────────────────────────────────────────


def bag_to_omnibot(
    bag_path: str | Path,
    dataset_root: str | Path,
    task_name: str,
    fps: int = 30,
) -> int:
    """
    Convert one ROS 2 bag to a LeRobot episode and append it to dataset_root.
    Returns the episode_index assigned.
    """
    bag_path = Path(bag_path)
    dataset_root = Path(dataset_root)

    # ── Load existing metadata ────────────────────────────────────────────
    info = _load_meta(dataset_root)
    episode_idx = info["total_episodes"]
    task_idx = _ensure_task(dataset_root, task_name)
    chunk = episode_idx // CHUNK_SIZE_EPISODES
    ep_str = f"episode_{episode_idx:06d}"

    # ── Parse bag ─────────────────────────────────────────────────────────
    parser = ROSBagParser(str(bag_path))
    raw = parser.extract_all()  # returns dict of topic→list[(ts, data)]

    syncer = TopicSynchronizer(fps=fps)
    frames_sync = syncer.synchronize(raw)  # list of dicts, one per target frame

    if not frames_sync:
        raise RuntimeError(f"No synchronized frames found in {bag_path}")

    global_offset = info["total_frames"]
    t0 = frames_sync[0]["timestamp"]

    rows: list[dict] = []
    front_frames: list[np.ndarray] = []
    wrist_frames: list[np.ndarray] = []

    for local_idx, frame in enumerate(frames_sync):
        state = frame.get("state", [0.0] * MOBILE_MANIP_STATE_SPEC.dim)
        action = frame.get("action", [0.0] * MOBILE_MANIP_ACTION_SPEC.dim)

        rows.append(
            {
                "observation.state": state,
                "action": action,
                "timestamp": frame["timestamp"] - t0,
                "frame_index": local_idx,
                "episode_index": episode_idx,
                "index": global_offset + local_idx,
                "task_index": task_idx,
                "next.done": local_idx == len(frames_sync) - 1,
            }
        )

        # Images: syncer returns np arrays (BGR) or None
        front = frame.get("camera_front")
        wrist = frame.get("camera_wrist")
        front_frames.append(
            front
            if front is not None
            else np.zeros((CAMERA_FRONT.height, CAMERA_FRONT.width, 3), np.uint8)
        )
        wrist_frames.append(
            wrist
            if wrist is not None
            else np.zeros((CAMERA_WRIST.height, CAMERA_WRIST.width, 3), np.uint8)
        )

    # ── Write Parquet ─────────────────────────────────────────────────────
    data_dir = dataset_root / "data" / f"chunk-{chunk:03d}"
    data_dir.mkdir(parents=True, exist_ok=True)

    table = pa.Table.from_pydict(
        {col: [r[col] for r in rows] for col in _PARQUET_SCHEMA.names},
        schema=_PARQUET_SCHEMA,
    )
    pq.write_table(table, data_dir / f"{ep_str}.parquet", compression="snappy")

    # ── Write videos ──────────────────────────────────────────────────────
    vid_base = dataset_root / "videos" / f"chunk-{chunk:03d}"
    _write_video(
        front_frames, vid_base / "observation.images.front" / f"{ep_str}.mp4", fps
    )
    _write_video(
        wrist_frames, vid_base / "observation.images.wrist" / f"{ep_str}.mp4", fps
    )

    # ── Update metadata ───────────────────────────────────────────────────
    ep_length = len(rows)
    info["total_episodes"] += 1
    info["total_frames"] += ep_length
    info["fps"] = fps
    _save_meta(dataset_root, info)

    episodes_path = dataset_root / "meta" / "episodes.jsonl"
    _append_jsonl(
        episodes_path,
        {
            "episode_index": episode_idx,
            "tasks": [task_idx],
            "length": ep_length,
        },
    )

    # ── Update running stats ──────────────────────────────────────────────
    _update_stats(dataset_root, rows)

    print(
        f"[bag_to_omnibot] episode {episode_idx} written ({ep_length} frames) → {ep_str}"
    )
    return episode_idx


def _update_stats(dataset_root: Path, rows: list[dict]) -> None:
    stats_path = dataset_root / "meta" / "stats.json"
    new_states = [r["observation.state"] for r in rows]
    new_actions = [r["action"] for r in rows]
    new_stats = _compute_stats(new_states, new_actions)

    if not stats_path.exists():
        stats_path.write_text(json.dumps(new_stats, indent=2))
        return

    old = json.loads(stats_path.read_text())

    # Track episode count so we can compute merged mean/std via Welford
    old["_num_episodes"] = old.get("_num_episodes", 1)

    # Merge: simple min/max update, mean/std via Welford's algorithm
    for key in ("observation.state", "action"):
        for stat in ("min", "max"):
            fn = min if stat == "min" else max
            old[key][stat] = [
                fn(a, b) for a, b in zip(old[key][stat], new_stats[key][stat])
            ]
        # Merge mean/std using Welford's online algorithm
        old_mean = old[key]["mean"]
        old_std = old[key]["std"]
        new_mean = new_stats[key]["mean"]
        new_std = new_stats[key]["std"]
        n_old = old["_num_episodes"]
        n_new = 1  # one new episode

        # Welford merge for each dimension
        merged_mean = []
        merged_std = []
        for i in range(len(old_mean)):
            m_a = old_mean[i]
            m_b = new_mean[i]
            s_a = old_std[i]
            s_b = new_std[i]
            # Combined mean
            mc = (n_old * m_a + n_new * m_b) / (n_old + n_new)
            # Combined variance (parallel algorithm)
            var_a = s_a**2
            var_b = s_b**2
            vc = (
                n_old * (var_a + (m_a - mc) ** 2) + n_new * (var_b + (m_b - mc) ** 2)
            ) / (n_old + n_new)
            merged_mean.append(mc)
            merged_std.append(math.sqrt(vc))
        old[key]["mean"] = merged_mean
        old[key]["std"] = merged_std

    old["_num_episodes"] += 1

    stats_path.write_text(json.dumps(old, indent=2))


# ── CLI ──────────────────────────────────────────────────────────────────────


@click.command()
@click.option("--bag", required=True, help="Path to ROS 2 bag directory")
@click.option("--dataset", required=True, help="LeRobot dataset root to append to")
@click.option("--task", required=True, help="Natural language task description")
@click.option("--fps", default=30, show_default=True, help="Target frame rate")
def main(bag: str, dataset: str, task: str, fps: int) -> None:
    bag_to_omnibot(bag, dataset, task, fps)


if __name__ == "__main__":
    main()
