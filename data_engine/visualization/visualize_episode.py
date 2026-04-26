"""
visualize_episode.py
--------------------
Play back a LeRobot v2.0 episode from disk.

Reads the Parquet file for state/action overlay and the MP4 files for video.

Usage:
    python -m data_engine.visualization.visualize_episode \\
        --dataset /data/lerobot/pick_place \\
        --episode 0 \\
        --fps 30

Controls:
    SPACE — pause / resume
    n     — step one frame (when paused)
    q     — quit
"""

from __future__ import annotations

import json
import time
from pathlib import Path

import click
import cv2
import numpy as np
import pandas as pd

CHUNKS_SIZE = 1000


def _chunk(ep_idx: int) -> str:
    return f"chunk-{ep_idx // CHUNKS_SIZE:03d}"


def _load_episode_parquet(dataset_root: Path, ep_idx: int) -> pd.DataFrame:
    path = dataset_root / "data" / _chunk(ep_idx) / f"episode_{ep_idx:06d}.parquet"
    if not path.exists():
        raise FileNotFoundError(f"Parquet not found: {path}")
    return pd.read_parquet(path)


def _open_video(
    dataset_root: Path, cam_key: str, ep_idx: int
) -> cv2.VideoCapture | None:
    path = (
        dataset_root / "videos" / _chunk(ep_idx) / cam_key / f"episode_{ep_idx:06d}.mp4"
    )
    if not path.exists():
        return None
    cap = cv2.VideoCapture(str(path))
    return cap if cap.isOpened() else None


def _overlay(img: np.ndarray, lines: list[str]) -> np.ndarray:
    y, dy = 28, 22
    for line in lines:
        cv2.putText(
            img, line, (8, y), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (0, 0, 0), 3, cv2.LINE_AA
        )
        cv2.putText(
            img,
            line,
            (8, y),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.55,
            (0, 255, 80),
            1,
            cv2.LINE_AA,
        )
        y += dy
    return img


@click.command()
@click.option(
    "--dataset",
    required=True,
    type=click.Path(exists=True),
    help="LeRobot dataset root",
)
@click.option("--episode", default=0, show_default=True, help="Episode index to play")
@click.option("--fps", default=30.0, show_default=True, help="Playback frame rate")
def main(dataset: str, episode: int, fps: float) -> None:
    root = Path(dataset)
    ep_idx = episode

    # ── Load task name ────────────────────────────────────────────────────────
    tasks_path = root / "meta" / "tasks.jsonl"
    task_name = "unknown"
    if tasks_path.exists():
        episodes_path = root / "meta" / "episodes.jsonl"
        if episodes_path.exists():
            for line in episodes_path.read_text().splitlines():
                rec = json.loads(line)
                if rec["episode_index"] == ep_idx:
                    task_idx = rec["tasks"][0] if rec["tasks"] else 0
                    for tline in tasks_path.read_text().splitlines():
                        trec = json.loads(tline)
                        if trec["task_index"] == task_idx:
                            task_name = trec["task"]

    # ── Load Parquet ──────────────────────────────────────────────────────────
    df = _load_episode_parquet(root, ep_idx)
    n_frames = len(df)
    click.echo(f"Episode {ep_idx} — {n_frames} frames — task: {task_name}")

    states = df["observation.state"].tolist()
    actions = df["action"].tolist()

    # ── Open video captures ───────────────────────────────────────────────────
    cap_front = _open_video(root, "observation.images.front", ep_idx)
    cap_wrist = _open_video(root, "observation.images.wrist", ep_idx)

    if cap_front is None and cap_wrist is None:
        click.echo("[WARN] No video files found — showing state/action text only.")

    win_front = "Front camera"
    win_wrist = "Wrist camera"
    if cap_front:
        cv2.namedWindow(win_front, cv2.WINDOW_NORMAL)
        cv2.resizeWindow(win_front, 640, 480)
    if cap_wrist:
        cv2.namedWindow(win_wrist, cv2.WINDOW_NORMAL)
        cv2.resizeWindow(win_wrist, 320, 240)

    click.echo("Controls: SPACE=pause/resume  n=step  q=quit")

    paused = False
    idx = 0
    dt_frame = 1.0 / fps

    while idx < n_frames:
        t_start = time.time()

        state = states[idx]
        action = actions[idx]

        # Arm joints (indices 0-5) and base (6-8)
        s_arm = [f"{v:+.2f}" for v in state[:6]]
        s_base = [f"{v:+.3f}" for v in state[6:]]
        a_arm = [f"{v:+.2f}" for v in action[:6]]
        a_base = [f"{v:+.3f}" for v in action[6:]]

        overlay_lines = [
            f"Frame {idx}/{n_frames}",
            f"State arm : {' '.join(s_arm)}",
            f"State base: vx={s_base[0]} vy={s_base[1]} vz={s_base[2]}",
            f"Action arm: {' '.join(a_arm)}",
            f"Action base: vx={a_base[0]} vy={a_base[1]} vz={a_base[2]}",
            f"Task: {task_name[:60]}",
        ]

        if cap_front:
            ret, frame = cap_front.read()
            if ret:
                _overlay(frame, overlay_lines)
                cv2.imshow(win_front, frame)

        if cap_wrist:
            ret, frame = cap_wrist.read()
            if ret:
                cv2.imshow(win_wrist, frame)

        key = cv2.waitKey(1) & 0xFF
        if key == ord("q"):
            break
        elif key == ord(" "):
            paused = not paused
        elif key == ord("n") and paused:
            idx += 1
            continue

        if not paused:
            idx += 1
            elapsed = time.time() - t_start
            wait = dt_frame - elapsed
            if wait > 0:
                time.sleep(wait)

    if cap_front:
        cap_front.release()
    if cap_wrist:
        cap_wrist.release()
    cv2.destroyAllWindows()
    click.echo("Done.")


if __name__ == "__main__":
    main()
