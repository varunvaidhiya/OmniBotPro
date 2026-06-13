#!/usr/bin/env python3
"""Standalone inference test for any registered policy — no ROS required.

Runs the selected policy against a live camera feed (or dummy frames) and
displays the predicted arm + base actions overlaid on the camera image.

Usage
-----
# SmolVLA with a live wrist camera
python lerobot_engine/infer.py \\
    --model smolvla \\
    --checkpoint ~/checkpoints/smolvla_run1/best \\
    --task "pick up the red cube"

# ACT with a dummy frame (no camera needed)
python lerobot_engine/infer.py --model act --checkpoint ~/checkpoints/act_run1/best

# List available models
python lerobot_engine/infer.py --list-models
"""

import argparse
import time

import numpy as np

try:
    import cv2

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

try:
    import torch

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

try:
    from vla_engine.trt import patch_policy_vision_encoder

    TRT_PATCH_AVAILABLE = True
except ImportError:
    TRT_PATCH_AVAILABLE = False

from lerobot_engine.models import list_models, make_policy

# Canonical joint names (arm_ prefix matches arm_driver_node defaults)
ARM_JOINT_NAMES = [
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
]


# ---------------------------------------------------------------------------
# Preprocessing helpers
# ---------------------------------------------------------------------------


def frame_to_tensor(frame_bgr: np.ndarray, target_w: int, target_h: int, device):
    """BGR frame → float32 CHW tensor on device."""
    if CV2_AVAILABLE:
        rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
        resized = cv2.resize(rgb, (target_w, target_h), interpolation=cv2.INTER_LINEAR)
    else:
        resized = np.zeros((target_h, target_w, 3), dtype=np.uint8)
    arr = resized.astype(np.float32) / 255.0
    arr = arr.transpose(2, 0, 1)  # HWC → CHW
    if TORCH_AVAILABLE:
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t
    return arr[np.newaxis]


def dummy_tensor(w: int, h: int, device):
    if TORCH_AVAILABLE:
        t = torch.zeros(1, 3, h, w, dtype=torch.float32)
        return t.to(device) if device is not None else t
    return np.zeros((1, 3, h, w), dtype=np.float32)


def build_state_tensor(state_dim: int, device):
    arr = np.zeros((1, state_dim), dtype=np.float32)
    if TORCH_AVAILABLE:
        t = torch.from_numpy(arr)
        return t.to(device) if device is not None else t
    return arr


# ---------------------------------------------------------------------------
# Overlay
# ---------------------------------------------------------------------------


def overlay_action(frame_bgr: np.ndarray, action: np.ndarray, fps: float) -> np.ndarray:
    if not CV2_AVAILABLE:
        return frame_bgr
    out = frame_bgr.copy()
    arm, base = action[:6], action[6:9]
    lines = [
        f"FPS: {fps:.1f}",
        "Arm (rad): "
        + "  ".join(f"{n[4:8]}:{v:+.3f}" for n, v in zip(ARM_JOINT_NAMES, arm)),
        f"Base:  vx={base[0]:+.3f}  vy={base[1]:+.3f}  vz={base[2]:+.3f}",
    ]
    y = 20
    for line in lines:
        cv2.putText(out, line, (8, y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 1)
        y += 22
    return out


# ---------------------------------------------------------------------------
# Args
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(
        description="Standalone policy inference test (no ROS required).",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument(
        "--model", type=str, default="smolvla", help="Model type from the registry."
    )
    parser.add_argument(
        "--list-models",
        action="store_true",
        help="Print registered model names and exit.",
    )
    parser.add_argument(
        "--checkpoint",
        type=str,
        default=None,
        help="HuggingFace hub ID or local checkpoint path.",
    )
    parser.add_argument(
        "--camera-id",
        type=int,
        default=0,
        help="OpenCV camera index for the wrist camera.",
    )
    parser.add_argument(
        "--task",
        type=str,
        default="pick up the object and place it",
        help="Task description (used by language-conditioned models).",
    )
    parser.add_argument("--device", type=str, default="cuda")
    parser.add_argument("--fps", type=float, default=10.0)
    parser.add_argument(
        "--trt-engine",
        type=str,
        default=None,
        help="Path to a pre-built TRT vision encoder (.trt).",
    )
    return parser.parse_args()


# ---------------------------------------------------------------------------
# Defaults per model
# ---------------------------------------------------------------------------

_CHECKPOINTS = {
    "smolvla": "lerobot/smolvla_base",
    "act": "lerobot/act_base",
    "diffusion": "lerobot/diffusion_pusht",
    "openvla": "openvla/openvla-7b",
}


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main():
    args = parse_args()

    if args.list_models:
        print("Registered models:", list_models())
        return

    if not CV2_AVAILABLE:
        print("[ERROR] opencv-python is required: pip install opencv-python")
        return

    checkpoint = args.checkpoint or _CHECKPOINTS.get(args.model)
    if checkpoint is None:
        print(f"[ERROR] --checkpoint is required for model '{args.model}'.")
        return

    device_str = args.device
    if TORCH_AVAILABLE:
        device = torch.device(
            "cuda" if (device_str == "cuda" and torch.cuda.is_available()) else "cpu"
        )
        if device_str == "cuda" and device.type == "cpu":
            print("[WARNING] CUDA not available, using CPU.")
    else:
        device = None

    print(f'Loading {args.model} from "{checkpoint}"...')
    try:
        adapter = make_policy(
            args.model, checkpoint=checkpoint, device=str(device or "cpu")
        )
    except Exception as exc:
        print(f"[ERROR] Failed to load policy: {exc}")
        return

    # Optional TRT vision encoder patch
    if args.trt_engine:
        if TRT_PATCH_AVAILABLE and adapter.model is not None:
            try:
                patch_policy_vision_encoder(adapter.model, args.trt_engine)
                print(f"[TRT] Vision encoder → {args.trt_engine}")
            except Exception as exc:
                print(f"[WARNING] TRT patch failed: {exc}")
        else:
            print("[WARNING] --trt-engine ignored (vla_engine.trt not available).")

    w, h = adapter.image_size
    cap = cv2.VideoCapture(args.camera_id)
    if not cap.isOpened():
        print(f"[WARNING] Camera {args.camera_id} unavailable — using dummy frames.")
        cap = None

    print(f'\nRunning {args.model} at {args.fps:.0f} Hz | task: "{args.task}"')
    print('Press "q" to quit.\n')

    adapter.reset()
    tick = 1.0 / args.fps
    fps_actual = args.fps
    t_prev = time.perf_counter()

    cv2.namedWindow("Policy Inference — OmniBot", cv2.WINDOW_NORMAL)

    try:
        while True:
            t0 = time.perf_counter()

            if cap is not None:
                ret, frame_bgr = cap.read()
                frame_bgr = (
                    frame_bgr
                    if (ret and frame_bgr is not None)
                    else np.zeros((h, w, 3), dtype=np.uint8)
                )
            else:
                frame_bgr = np.zeros((h, w, 3), dtype=np.uint8)

            wrist_t = frame_to_tensor(frame_bgr, w, h, device)

            obs = {k: dummy_tensor(w, h, device) for k in adapter.image_keys}
            obs[adapter.image_keys[0]] = wrist_t  # wrist is always first
            if len(adapter.image_keys) > 1:
                print(
                    "[WARNING] Policy expects %d cameras but only 1 (wrist) is "
                    "available. Secondary cameras are fed zeros. "
                    "Use --camera 0 (e.g., webcam) for single-cam policies."
                    % len(adapter.image_keys)
                )
            obs[adapter.state_key] = build_state_tensor(adapter.action_dim, device)
            if adapter.task_key:
                obs[adapter.task_key] = args.task

            action = adapter.select_action(obs)

            # Display
            t1 = time.perf_counter()
            fps_actual = 0.9 * fps_actual + 0.1 / max(t1 - t_prev, 1e-6)
            t_prev = t1

            display = cv2.resize(frame_bgr, (640, 480))
            display = overlay_action(display, action, fps_actual)
            cv2.imshow("Policy Inference — OmniBot", display)
            if cv2.waitKey(1) & 0xFF == ord("q"):
                break

            elapsed = time.perf_counter() - t0
            if elapsed < tick:
                time.sleep(tick - elapsed)

    finally:
        if cap:
            cap.release()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
