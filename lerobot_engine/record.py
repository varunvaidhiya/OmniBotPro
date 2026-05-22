#!/usr/bin/env python3
"""Standalone dataset recording — gamepad + webcams, no ROS required.

For the full ROS-integrated recording workflow (leader arm + base + ROS topics),
use the teleop_recorder_node instead:
    ros2 launch omnibot_lerobot teleop_record.launch.py

This script is useful when:
  - You want to record quickly without a ROS stack running
  - You are testing on a workstation without the robot connected
  - You want to collect data from webcams only (no leader arm)

Controls (Xbox controller via pygame or /dev/input, fallback keyboard)
----------------------------------------------------------------------
  RB (button 5) — start / stop recording
  LB (button 4) — discard current episode
  Left stick     — base velocity (vx, vy)
  Right stick X  — base angular (vz)
  Keyboard: SPACE=record, D=discard, Q=quit (fallback when no gamepad)

Usage
-----
python lerobot_engine/record.py \\
    --output-dir ~/datasets/mobile_manipulation \\
    --repo-id local/mobile_manipulation \\
    --task "pick up the red cube" \\
    --record-hz 30 \\
    --wrist-camera 0 \\
    --bev-camera 1
"""

import argparse
import os
import time
from pathlib import Path

import numpy as np

try:
    import cv2

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

try:
    import pygame

    PYGAME_AVAILABLE = True
except ImportError:
    PYGAME_AVAILABLE = False

try:
    from lerobot.common.datasets.lerobot_dataset import LeRobotDataset

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False

# Canonical names matching arm_driver_node and the robot schema
ARM_JOINT_NAMES = [
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
]
STATE_NAMES = ARM_JOINT_NAMES + ["base_vx", "base_vy", "base_vz"]
ACTION_NAMES = STATE_NAMES
IMAGE_W, IMAGE_H = 320, 240
MAX_LINEAR = 0.2  # m/s
MAX_ANGULAR = 1.0  # rad/s


# ---------------------------------------------------------------------------
# Args
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(description="Standalone dataset recording.")
    parser.add_argument(
        "--output-dir", type=str, default="~/datasets/mobile_manipulation"
    )
    parser.add_argument("--repo-id", type=str, default="local/mobile_manipulation")
    parser.add_argument("--task", type=str, default="mobile manipulation task")
    parser.add_argument("--record-hz", type=float, default=30.0)
    parser.add_argument(
        "--episode-timeout",
        type=float,
        default=60.0,
        help="Auto-save after this many seconds.",
    )
    parser.add_argument(
        "--wrist-camera",
        type=int,
        default=0,
        help="OpenCV camera index for the wrist camera.",
    )
    parser.add_argument(
        "--bev-camera",
        type=int,
        default=-1,
        help="OpenCV camera index for BEV camera. -1 = dummy frame.",
    )
    return parser.parse_args()


# ---------------------------------------------------------------------------
# Gamepad
# ---------------------------------------------------------------------------


def init_gamepad():
    if not PYGAME_AVAILABLE:
        return None
    pygame.init()
    pygame.joystick.init()
    if pygame.joystick.get_count() == 0:
        return None
    joy = pygame.joystick.Joystick(0)
    joy.init()
    print(f"  Gamepad: {joy.get_name()}")
    return joy


def read_gamepad(joy) -> tuple[np.ndarray, bool, bool]:
    """Returns (base_vel_3d, record_pressed, discard_pressed)."""
    pygame.event.pump()
    vx = float(joy.get_axis(1)) * MAX_LINEAR
    vy = float(joy.get_axis(0)) * MAX_LINEAR
    vz = float(joy.get_axis(3)) * MAX_ANGULAR
    record = bool(joy.get_button(5))  # RB
    discard = bool(joy.get_button(4))  # LB
    return np.array([vx, vy, vz], dtype=np.float32), record, discard


# ---------------------------------------------------------------------------
# Camera
# ---------------------------------------------------------------------------


def open_camera(idx: int):
    if not CV2_AVAILABLE or idx < 0:
        return None
    cap = cv2.VideoCapture(idx)
    return cap if cap.isOpened() else None


def read_frame(cap, w: int = IMAGE_W, h: int = IMAGE_H) -> np.ndarray:
    if cap is None:
        return np.zeros((h, w, 3), dtype=np.uint8)
    ret, frame = cap.read()
    if not ret or frame is None:
        return np.zeros((h, w, 3), dtype=np.uint8)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    return cv2.resize(rgb, (w, h), interpolation=cv2.INTER_LINEAR)


# ---------------------------------------------------------------------------
# Save episode
# ---------------------------------------------------------------------------


def save_episode_lerobot(
    frames: list[dict],
    output_dir: str,
    repo_id: str,
    record_hz: float,
    task: str,
):
    features = {
        "observation.state": {
            "dtype": "float32",
            "shape": (9,),
            "names": {"joints": STATE_NAMES},
        },
        "action": {
            "dtype": "float32",
            "shape": (9,),
            "names": {"joints": ACTION_NAMES},
        },
        "observation.images.wrist": {
            "dtype": "video",
            "shape": (3, IMAGE_H, IMAGE_W),
            "names": ["channels", "height", "width"],
        },
        "observation.images.bev": {
            "dtype": "video",
            "shape": (3, IMAGE_H, IMAGE_W),
            "names": ["channels", "height", "width"],
        },
    }

    dataset = LeRobotDataset.create(
        repo_id=repo_id,
        fps=int(record_hz),
        root=output_dir,
        features=features,
        image_writer_threads=4,
    )

    for f in frames:
        dataset.add_frame(
            {
                "observation.state": f["state"],
                "action": f["action"],
                "observation.images.wrist": f["wrist_image"],
                "observation.images.bev": f["bev_image"],
            }
        )
    dataset.save_episode(task=task)


def save_episode_numpy(frames: list[dict], output_dir: str, episode_idx: int):
    ep_dir = Path(output_dir) / f"episode_{episode_idx:05d}"
    ep_dir.mkdir(parents=True, exist_ok=True)
    np.savez_compressed(
        ep_dir / "data.npz",
        states=np.stack([f["state"] for f in frames]),
        actions=np.stack([f["action"] for f in frames]),
        wrist_images=np.stack([f["wrist_image"] for f in frames]),
        bev_images=np.stack([f["bev_image"] for f in frames]),
        timestamps=np.array([f["timestamp"] for f in frames]),
        state_names=np.array(STATE_NAMES),
        action_names=np.array(ACTION_NAMES),
    )
    print(f"  Saved numpy episode to {ep_dir}/data.npz")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main():
    args = parse_args()
    output_dir = os.path.expanduser(args.output_dir)
    os.makedirs(output_dir, exist_ok=True)

    if not CV2_AVAILABLE:
        print("[ERROR] opencv-python is required: pip install opencv-python")
        return

    joy = init_gamepad()
    if joy is None:
        print("  No gamepad found. Keyboard fallback: SPACE=record, D=discard, Q=quit.")

    wrist_cap = open_camera(args.wrist_camera)
    bev_cap = open_camera(args.bev_camera)

    if wrist_cap is None:
        print(
            f"[WARNING] Wrist camera {args.wrist_camera} not available — using dummy frames."
        )
    if bev_cap is None and args.bev_camera >= 0:
        print(
            f"[WARNING] BEV camera {args.bev_camera} not available — using dummy frames."
        )

    print(f"\nRecording to: {output_dir}")
    print(f"Task: {args.task}")
    print(f"Rate: {args.record_hz:.0f} Hz")
    if joy:
        print("RB = start/stop, LB = discard")
    else:
        print("SPACE = start/stop, D = discard, Q = quit")

    episode_idx = 0
    recording = False
    buffer: list[dict] = []
    t_start = 0.0
    tick = 1.0 / args.record_hz

    prev_record_btn = False
    prev_discard_btn = False

    cv2.namedWindow("Recording — OmniBot", cv2.WINDOW_NORMAL)

    try:
        while True:
            t0 = time.perf_counter()

            # Read inputs
            if joy is not None:
                base_vel, record_btn, discard_btn = read_gamepad(joy)
            else:
                base_vel = np.zeros(3, dtype=np.float32)
                record_btn = False
                discard_btn = False

            # Keyboard fallback
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                break
            if key == ord(" "):
                record_btn = True
            if key == ord("d"):
                discard_btn = True

            # Button edge detection
            record_press = record_btn and not prev_record_btn
            discard_press = discard_btn and not prev_discard_btn
            prev_record_btn = record_btn
            prev_discard_btn = discard_btn

            if record_press:
                if not recording:
                    buffer = []
                    t_start = time.time()
                    recording = True
                    print(f"\nRecording episode {episode_idx}...")
                else:
                    recording = False
                    print(f"  Saving {len(buffer)} frames...")
                    try:
                        if LEROBOT_AVAILABLE:
                            save_episode_lerobot(
                                buffer,
                                output_dir,
                                args.repo_id,
                                args.record_hz,
                                args.task,
                            )
                        else:
                            save_episode_numpy(buffer, output_dir, episode_idx)
                        print(f"  Episode {episode_idx} saved.")
                        episode_idx += 1
                    except Exception as exc:
                        print(f"  [ERROR] Save failed: {exc}")
                    buffer = []

            if discard_press and recording:
                print(f"  Discarding episode {episode_idx} ({len(buffer)} frames).")
                buffer = []
                recording = False

            # Timeout
            if recording and (time.time() - t_start) > args.episode_timeout:
                print(f"  Timeout — auto-saving {len(buffer)} frames.")
                recording = False
                try:
                    if LEROBOT_AVAILABLE:
                        save_episode_lerobot(
                            buffer, output_dir, args.repo_id, args.record_hz, args.task
                        )
                    else:
                        save_episode_numpy(buffer, output_dir, episode_idx)
                    episode_idx += 1
                except Exception as exc:
                    print(f"  [ERROR] Save failed: {exc}")
                buffer = []

            # Capture
            wrist_img = read_frame(wrist_cap)
            bev_img = read_frame(bev_cap)
            state = np.zeros(9, dtype=np.float32)
            state[6:9] = base_vel
            action = state.copy()

            if recording:
                buffer.append(
                    {
                        "state": state.copy(),
                        "action": action.copy(),
                        "wrist_image": wrist_img.copy(),
                        "bev_image": bev_img.copy(),
                        "timestamp": time.time(),
                    }
                )

            # Display
            status = f"REC {len(buffer)}fr" if recording else f"IDLE ep={episode_idx}"
            display = cv2.resize(cv2.cvtColor(wrist_img, cv2.COLOR_RGB2BGR), (640, 480))
            cv2.putText(
                display,
                status,
                (8, 24),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.7,
                (0, 0, 255) if recording else (0, 255, 0),
                2,
            )
            cv2.imshow("Recording — OmniBot", display)

            elapsed = time.perf_counter() - t0
            if elapsed < tick:
                time.sleep(tick - elapsed)

    finally:
        if wrist_cap:
            wrist_cap.release()
        if bev_cap:
            bev_cap.release()
        cv2.destroyAllWindows()
        if PYGAME_AVAILABLE:
            pygame.quit()


if __name__ == "__main__":
    main()
