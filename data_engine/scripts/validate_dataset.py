"""
validate_dataset.py
-------------------
Validate a LeRobot v2.0 dataset on disk.

Checks:
  - meta/info.json exists and has required fields
  - meta/tasks.jsonl and meta/episodes.jsonl are consistent with info.json
  - meta/stats.json exists
  - Parquet files exist for every declared episode
  - Video files exist for every declared episode
  - Each Parquet file has required columns and correct row count

Usage:
    python -m data_engine.scripts.validate_dataset --dataset /data/lerobot/pick_place
"""

from __future__ import annotations

import json
from pathlib import Path

import click
import pyarrow.parquet as pq

REQUIRED_META = {
    "codebase_version",
    "fps",
    "total_episodes",
    "total_frames",
    "features",
}
REQUIRED_PARQUET = {
    "observation.state",
    "action",
    "timestamp",
    "frame_index",
    "episode_index",
    "index",
    "task_index",
    "next.done",
}
REQUIRED_CAM_KEYS = {"observation.images.front", "observation.images.wrist"}
CHUNKS_SIZE = 1000


def _chunk(ep_idx: int) -> str:
    return f"chunk-{ep_idx // CHUNKS_SIZE:03d}"


def validate(dataset_root: Path, verbose: bool) -> bool:
    ok = True

    def fail(msg: str) -> None:
        nonlocal ok
        ok = False
        click.echo(f"  [FAIL] {msg}")

    def info_msg(msg: str) -> None:
        if verbose:
            click.echo(f"  [OK]   {msg}")

    # ── meta/info.json ────────────────────────────────────────────────────────
    info_path = dataset_root / "meta" / "info.json"
    if not info_path.exists():
        fail("meta/info.json not found")
        return False

    info = json.loads(info_path.read_text())
    for field in REQUIRED_META:
        if field not in info:
            fail(f"meta/info.json missing field: {field}")
    info_msg(
        f"meta/info.json — {info.get('total_episodes')} episodes, "
        f"{info.get('total_frames')} frames"
    )

    # ── meta/tasks.jsonl ──────────────────────────────────────────────────────
    tasks_path = dataset_root / "meta" / "tasks.jsonl"
    if not tasks_path.exists():
        fail("meta/tasks.jsonl not found")
    else:
        tasks = [
            json.loads(line)
            for line in tasks_path.read_text().splitlines()
            if line.strip()
        ]
        info_msg(f"meta/tasks.jsonl — {len(tasks)} task(s)")

    # ── meta/episodes.jsonl ───────────────────────────────────────────────────
    episodes_path = dataset_root / "meta" / "episodes.jsonl"
    if not episodes_path.exists():
        fail("meta/episodes.jsonl not found")
        return False

    episodes = [
        json.loads(line)
        for line in episodes_path.read_text().splitlines()
        if line.strip()
    ]
    declared = info.get("total_episodes", 0)
    if len(episodes) != declared:
        fail(
            f"episodes.jsonl has {len(episodes)} entries but info.json says {declared}"
        )
    info_msg(f"meta/episodes.jsonl — {len(episodes)} entries match info.json")

    # ── meta/stats.json ───────────────────────────────────────────────────────
    if not (dataset_root / "meta" / "stats.json").exists():
        fail("meta/stats.json not found")
    else:
        info_msg("meta/stats.json present")

    # ── Per-episode checks ────────────────────────────────────────────────────
    total_frames_seen = 0
    for ep in episodes:
        ep_idx = ep["episode_index"]
        ep_len = ep["length"]
        chunk = _chunk(ep_idx)
        ep_str = f"episode_{ep_idx:06d}"

        # Parquet
        pq_path = dataset_root / "data" / chunk / f"{ep_str}.parquet"
        if not pq_path.exists():
            fail(f"Missing parquet: {pq_path.relative_to(dataset_root)}")
        else:
            try:
                table = pq.read_table(pq_path)
                missing_cols = REQUIRED_PARQUET - set(table.schema.names)
                if missing_cols:
                    fail(f"{ep_str}.parquet missing columns: {missing_cols}")
                if len(table) != ep_len:
                    fail(f"{ep_str}.parquet has {len(table)} rows, expected {ep_len}")
                else:
                    info_msg(f"{ep_str}.parquet — {ep_len} rows OK")
                total_frames_seen += len(table)
            except Exception as e:
                fail(f"Cannot read {ep_str}.parquet: {e}")

        # Videos
        for cam_key in REQUIRED_CAM_KEYS:
            vid_path = dataset_root / "videos" / chunk / cam_key / f"{ep_str}.mp4"
            if not vid_path.exists():
                fail(f"Missing video: {vid_path.relative_to(dataset_root)}")
            else:
                info_msg(f"{cam_key}/{ep_str}.mp4 present")

    # ── Frame count consistency ───────────────────────────────────────────────
    if total_frames_seen != info.get("total_frames", -1):
        fail(
            f"Frame count mismatch: counted {total_frames_seen}, "
            f"info.json says {info.get('total_frames')}"
        )
    else:
        info_msg(f"Total frame count consistent: {total_frames_seen}")

    return ok


@click.command()
@click.option(
    "--dataset",
    required=True,
    type=click.Path(exists=True),
    help="LeRobot dataset root directory",
)
@click.option(
    "--verbose",
    is_flag=True,
    default=False,
    help="Print OK lines in addition to failures",
)
def main(dataset: str, verbose: bool) -> None:
    root = Path(dataset)
    click.echo(f"Validating LeRobot dataset: {root}\n")
    passed = validate(root, verbose)
    if passed:
        click.echo("\nAll checks passed.")
    else:
        click.echo("\nValidation FAILED — see errors above.")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
