#!/usr/bin/env python3
"""
Standalone teleoperation recorder for mobile manipulation.

Uses LeRobot's native pipeline:
  - Leader arm (SO-101) → follower arm mirroring
  - Xbox controller → base velocity commands (logged only; base control handled via ROS)
  - Records episodes in LeRobot HF dataset format

Usage:
  python record.py --task "pick up the red cube" --num-episodes 20 \\
                   --output-dir ~/datasets/mobile_manipulation
"""

import argparse
import time
import numpy as np
from pathlib import Path
import json
from datetime import datetime

# ---------------------------------------------------------------------------
# Optional LeRobot imports
# ---------------------------------------------------------------------------
try:
    from lerobot.common.robot_devices.motors.feetech import FeetechMotorsBus
    from lerobot.common.datasets.lerobot_dataset import LeRobotDataset

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
JOINT_NAMES = [
    "shoulder_pan",
    "shoulder_lift",
    "elbow_flex",
    "wrist_flex",
    "wrist_roll",
    "gripper",
]
NUM_JOINTS = len(JOINT_NAMES)
MOTOR_IDS = [1, 2, 3, 4, 5, 6]
HOME_TICKS = [2048, 2048, 2048, 2048, 2048, 2048]
TICKS_PER_REV = 4096

DATASET_FEATURES = {
    "observation.state": {
        "dtype": "float32",
        "shape": (9,),
        "names": {"joints": JOINT_NAMES + ["base_vx", "base_vy", "base_vz"]},
    },
    "action": {
        "dtype": "float32",
        "shape": (9,),
        "names": {"joints": JOINT_NAMES + ["base_vx", "base_vy", "base_vz"]},
    },
    "observation.images.wrist": {
        "dtype": "video",
        "shape": (3, 240, 320),
        "names": ["channels", "height", "width"],
    },
    "observation.images.front": {
        "dtype": "video",
        "shape": (3, 240, 320),
        "names": ["channels", "height", "width"],
    },
}


# ---------------------------------------------------------------------------
# Tick <-> radian conversion
# ---------------------------------------------------------------------------


def ticks_to_radians(ticks: list) -> np.ndarray:
    ticks_per_rad = TICKS_PER_REV / (2.0 * np.pi)
    return np.array(
        [(t - h) / ticks_per_rad for t, h in zip(ticks, HOME_TICKS)], dtype=np.float32
    )


def radians_to_ticks(rads: np.ndarray) -> list:
    ticks_per_rad = TICKS_PER_REV / (2.0 * np.pi)
    return [int(round(r * ticks_per_rad + h)) for r, h in zip(rads, HOME_TICKS)]


# ---------------------------------------------------------------------------
# Bus helpers
# ---------------------------------------------------------------------------


def build_motors_dict():
    return {name: (mid, "sts3215") for name, mid in zip(JOINT_NAMES, MOTOR_IDS)}


def read_positions(bus) -> np.ndarray:
    ticks_dict = bus.read("Present_Position")
    ticks = [ticks_dict[name] for name in JOINT_NAMES]
    return ticks_to_radians(ticks)


def write_positions(bus, rads: np.ndarray):
    ticks = radians_to_ticks(rads)
    values_dict = {name: tick for name, tick in zip(JOINT_NAMES, ticks)}
    bus.write("Goal_Position", values_dict)


# ---------------------------------------------------------------------------
# Dummy image placeholder (no cameras in standalone mode)
# ---------------------------------------------------------------------------


def dummy_image() -> np.ndarray:
    return np.zeros((240, 320, 3), dtype=np.uint8)


# ---------------------------------------------------------------------------
# Episode recording
# ---------------------------------------------------------------------------


def record_episode(
    follower_bus,
    leader_bus,
    dataset,
    episode_idx: int,
    fps: float,
    episode_time_s: float,
    task: str,
) -> int:
    """
    Record one episode of leader→follower mirroring.

    Returns number of frames recorded.
    """
    print(
        f"\n  Episode {episode_idx}: Recording for up to {episode_time_s:.0f}s "
        f"at {fps:.0f} Hz. Press Ctrl+C to finish early.\n"
    )

    tick_period = 1.0 / fps
    frame_count = 0
    start_time = time.time()

    try:
        while (time.time() - start_time) < episode_time_s:
            t0 = time.perf_counter()

            # Read leader → write to follower
            leader_pos = read_positions(leader_bus)
            write_positions(follower_bus, leader_pos)

            # Observation: follower positions
            follower_pos = read_positions(follower_bus)

            # 9D state/action (base velocity = 0 in arm-only mode)
            base_zeros = np.zeros(3, dtype=np.float32)
            state = np.concatenate([follower_pos, base_zeros])
            action = np.concatenate([leader_pos, base_zeros])

            # Placeholder images
            wrist_img = dummy_image()
            front_img = dummy_image()

            dataset.add_frame(
                {
                    "observation.state": state,
                    "action": action,
                    "observation.images.wrist": wrist_img,
                    "observation.images.front": front_img,
                }
            )

            frame_count += 1

            # Pace to fps
            elapsed = time.perf_counter() - t0
            sleep_time = tick_period - elapsed
            if sleep_time > 0:
                time.sleep(sleep_time)

    except KeyboardInterrupt:
        print("\n  Ctrl+C detected — finishing episode early.")

    dataset.save_episode(task=task)
    duration = time.time() - start_time
    print(
        f"  Episode {episode_idx}: {frame_count} frames in {duration:.1f}s "
        f"({frame_count / duration:.1f} fps actual)."
    )
    return frame_count


# ---------------------------------------------------------------------------
# Numpy fallback recording
# ---------------------------------------------------------------------------


def record_episode_numpy(
    follower_bus,
    leader_bus,
    output_dir: Path,
    episode_idx: int,
    fps: float,
    episode_time_s: float,
    task: str,
) -> int:
    """Fallback recording to .npz when lerobot is not available."""
    tick_period = 1.0 / fps
    frames = []
    start_time = time.time()

    print(f"\n  Episode {episode_idx} (numpy fallback): Recording ...\n")

    try:
        while (time.time() - start_time) < episode_time_s:
            t0 = time.perf_counter()

            leader_pos = read_positions(leader_bus)
            write_positions(follower_bus, leader_pos)
            follower_pos = read_positions(follower_bus)

            base_zeros = np.zeros(3, dtype=np.float32)
            frames.append(
                {
                    "state": np.concatenate([follower_pos, base_zeros]),
                    "action": np.concatenate([leader_pos, base_zeros]),
                    "timestamp": time.time(),
                }
            )

            elapsed = time.perf_counter() - t0
            if tick_period - elapsed > 0:
                time.sleep(tick_period - elapsed)

    except KeyboardInterrupt:
        print("\n  Ctrl+C — finishing early.")

    ep_dir = output_dir / f"episode_{episode_idx:05d}"
    ep_dir.mkdir(parents=True, exist_ok=True)

    states = np.stack([f["state"] for f in frames])
    actions = np.stack([f["action"] for f in frames])
    timestamps = np.array([f["timestamp"] for f in frames])

    np.savez_compressed(
        ep_dir / "data.npz",
        states=states,
        actions=actions,
        timestamps=timestamps,
        joint_names=np.array(JOINT_NAMES),
        task=np.array([task]),
        fps=np.array([fps]),
    )
    print(f"  Saved {len(frames)} frames to {ep_dir}/data.npz")
    return len(frames)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(
        description="Standalone teleoperation recorder for mobile manipulation."
    )
    parser.add_argument(
        "--task",
        type=str,
        default="pick up the object and place it",
        help="Natural language task description for this recording session.",
    )
    parser.add_argument(
        "--num-episodes", type=int, default=10, help="Number of episodes to record."
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default="~/datasets/mobile_manipulation",
        help="Directory to save the dataset.",
    )
    parser.add_argument(
        "--fps", type=float, default=30.0, help="Recording frequency in Hz."
    )
    parser.add_argument(
        "--follower-port",
        type=str,
        default="/dev/ttyACM0",
        help="Serial port for the follower arm.",
    )
    parser.add_argument(
        "--leader-port",
        type=str,
        default="/dev/ttyACM1",
        help="Serial port for the leader arm.",
    )
    parser.add_argument(
        "--episode-time-s",
        type=float,
        default=30.0,
        help="Maximum duration of each episode in seconds.",
    )
    parser.add_argument(
        "--warmup-time-s",
        type=float,
        default=3.0,
        help="Warmup duration before recording starts (seconds).",
    )
    parser.add_argument(
        "--repo-id",
        type=str,
        default="local/mobile_manipulation",
        help="LeRobot dataset repo ID (used for HF format).",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    output_dir = Path(args.output_dir).expanduser()
    output_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 60)
    print("  OmniBot Mobile Manipulation — Teleoperation Recorder")
    print("=" * 60)
    print(f"  Task:         {args.task}")
    print(f"  Episodes:     {args.num_episodes}")
    print(f"  FPS:          {args.fps}")
    print(f"  Episode time: {args.episode_time_s}s")
    print(f"  Output:       {output_dir}")
    print(
        f"  LeRobot:      {'available' if LEROBOT_AVAILABLE else 'NOT available (numpy fallback)'}"
    )
    print("=" * 60)

    if not LEROBOT_AVAILABLE:
        print(
            "\n[WARNING] lerobot is not installed.\n"
            "  Install it with:\n"
            '    pip install "lerobot[feetech] @ git+https://github.com/huggingface/lerobot.git"\n'
            "  Falling back to numpy .npz format.\n"
        )

    # Connect hardware
    print("\nConnecting to arm hardware...")
    motors_dict = build_motors_dict()

    follower_bus = None
    leader_bus = None

    try:
        if LEROBOT_AVAILABLE:
            follower_bus = FeetechMotorsBus(port=args.follower_port, motors=motors_dict)
            follower_bus.connect()
            print(f"  Follower connected on {args.follower_port}")

            leader_bus = FeetechMotorsBus(port=args.leader_port, motors=motors_dict)
            leader_bus.connect()
            print(f"  Leader connected on {args.leader_port}")
        else:
            print("[ERROR] lerobot is required for hardware access. Exiting.")
            print(
                '  Install: pip install "lerobot[feetech] @ git+https://github.com/huggingface/lerobot.git"'
            )
            return

        # Warmup
        print(f"\nWarmup for {args.warmup_time_s}s — move leader arm to start pose...")
        warmup_end = time.time() + args.warmup_time_s
        while time.time() < warmup_end:
            leader_pos = read_positions(leader_bus)
            write_positions(follower_bus, leader_pos)
            time.sleep(1.0 / args.fps)
        print("Warmup done.")

        # Create dataset (LeRobot format)
        dataset = None
        if LEROBOT_AVAILABLE:
            print("\nInitializing LeRobot dataset...")
            dataset = LeRobotDataset.create(
                repo_id=args.repo_id,
                fps=int(args.fps),
                root=str(output_dir),
                features=DATASET_FEATURES,
                image_writer_threads=4,
            )

        # Recording loop
        total_frames = 0
        session_start = datetime.now().isoformat(timespec="seconds")

        for ep_idx in range(args.num_episodes):
            print(
                f"\n[{ep_idx + 1}/{args.num_episodes}] Ready. "
                "Move to start pose, then press Enter to begin episode..."
            )
            try:
                input()
            except EOFError:
                pass

            try:
                if dataset is not None:
                    frames = record_episode(
                        follower_bus,
                        leader_bus,
                        dataset,
                        ep_idx,
                        args.fps,
                        args.episode_time_s,
                        args.task,
                    )
                else:
                    frames = record_episode_numpy(
                        follower_bus,
                        leader_bus,
                        output_dir,
                        ep_idx,
                        args.fps,
                        args.episode_time_s,
                        args.task,
                    )
                total_frames += frames

            except KeyboardInterrupt:
                print("\nRecording interrupted by user.")
                break

        # Session summary
        print("\n" + "=" * 60)
        print("  Recording session complete.")
        print(f"  Episodes recorded: {ep_idx + 1}")
        print(f"  Total frames:      {total_frames}")
        print(f"  Total duration:    ~{total_frames / args.fps:.1f}s")
        print(f"  Started at:        {session_start}")
        print(f"  Output dir:        {output_dir}")
        print("=" * 60)

        # Save session metadata
        metadata = {
            "task": args.task,
            "num_episodes": ep_idx + 1,
            "total_frames": total_frames,
            "fps": args.fps,
            "session_start": session_start,
            "session_end": datetime.now().isoformat(timespec="seconds"),
            "joint_names": JOINT_NAMES,
            "state_dim": 9,
            "action_dim": 9,
            "note": "base velocity = 0 (arm-only recording; use ROS teleop_recorder_node for full mobile manipulation)",
        }
        with open(output_dir / "session_metadata.json", "w") as f:
            json.dump(metadata, f, indent=2)
        print(f"\nMetadata saved to {output_dir}/session_metadata.json")

    finally:
        if follower_bus is not None:
            try:
                follower_bus.disconnect()
                print("Follower bus disconnected.")
            except Exception:
                pass
        if leader_bus is not None:
            try:
                leader_bus.disconnect()
                print("Leader bus disconnected.")
            except Exception:
                pass


if __name__ == "__main__":
    main()
