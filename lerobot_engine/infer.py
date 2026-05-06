#!/usr/bin/env python3
"""
Standalone inference test for SmolVLA mobile manipulation policy.

Tests the policy with dummy/live camera inputs without ROS.

Usage:
  python infer.py --checkpoint ~/checkpoints/smolvla_mobile --camera-id 0 --task "pick up the cube"
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
    from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False

try:
    from vla_engine.trt import patch_policy_vision_encoder

    TRT_PATCH_AVAILABLE = True
except ImportError:
    TRT_PATCH_AVAILABLE = False

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
IMAGE_H = 240
IMAGE_W = 320


# ---------------------------------------------------------------------------
# Dummy policy fallback
# ---------------------------------------------------------------------------
class DummyPolicy:
    def __init__(self, action_dim=9):
        self.action_dim = action_dim

    def select_action(self, obs):
        arr = np.zeros((1, self.action_dim), dtype=np.float32)
        if TORCH_AVAILABLE:
            return torch.from_numpy(arr)
        return arr

    def reset(self):
        pass


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def frame_to_tensor(frame_bgr: np.ndarray, device):
    """Convert BGR uint8 HxWx3 frame to float32 CHW tensor in [0,1], batched."""
    rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
    resized = cv2.resize(rgb, (IMAGE_W, IMAGE_H), interpolation=cv2.INTER_LINEAR)
    arr = resized.astype(np.float32) / 255.0
    arr = arr.transpose(2, 0, 1)  # HWC → CHW

    if TORCH_AVAILABLE:
        t = torch.from_numpy(arr).unsqueeze(0)  # 1xCxHxW
        if device is not None:
            t = t.to(device)
        return t
    return arr[np.newaxis]


def dummy_tensor(device):
    arr = np.zeros((1, 3, IMAGE_H, IMAGE_W), dtype=np.float32)
    if TORCH_AVAILABLE:
        t = torch.from_numpy(arr)
        if device is not None:
            t = t.to(device)
        return t
    return arr


def build_state_tensor(device):
    """Build dummy 9D state tensor (zeros)."""
    arr = np.zeros((1, 9), dtype=np.float32)
    if TORCH_AVAILABLE:
        t = torch.from_numpy(arr)
        if device is not None:
            t = t.to(device)
        return t
    return arr


def overlay_action_text(
    frame_bgr: np.ndarray, action_np: np.ndarray, fps: float, frame_idx: int
) -> np.ndarray:
    """Draw action values and stats onto the frame."""
    overlay = frame_bgr.copy()

    # Background rectangle for text
    cv2.rectangle(overlay, (0, 0), (frame_bgr.shape[1], 160), (0, 0, 0), -1)
    cv2.addWeighted(overlay, 0.6, frame_bgr, 0.4, 0, overlay)

    arm = action_np[:6]
    base = action_np[6:9]

    lines = [
        f"Frame: {frame_idx}  |  Policy FPS: {fps:.1f}",
        "",
        "Arm joints (rad):",
        "  " + "  ".join(f"{n[:4]}:{v:+.3f}" for n, v in zip(JOINT_NAMES, arm)),
        "",
        f"Base vel:  vx={base[0]:+.3f}  vy={base[1]:+.3f}  vz={base[2]:+.3f}",
    ]

    y = 18
    for line in lines:
        cv2.putText(
            overlay,
            line,
            (8, y),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.45,
            (0, 255, 0),
            1,
            cv2.LINE_AA,
        )
        y += 20

    return overlay


# ---------------------------------------------------------------------------
# Args
# ---------------------------------------------------------------------------


def parse_args():
    parser = argparse.ArgumentParser(
        description="Standalone SmolVLA inference test (no ROS required)."
    )
    parser.add_argument(
        "--checkpoint",
        type=str,
        default="lerobot/smolvla_base",
        help="Path to SmolVLA checkpoint directory or HF model ID.",
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
        default="pick up the cube",
        help="Natural language task description.",
    )
    parser.add_argument(
        "--device", type=str, default="cuda", help="Inference device: cuda or cpu."
    )
    parser.add_argument(
        "--fps", type=float, default=10.0, help="Target inference frequency in Hz."
    )
    parser.add_argument(
        "--trt-engine",
        type=str,
        default=None,
        help="Path to a pre-built TRT vision encoder engine (.trt). "
             "When set, replaces the PyTorch vision encoder for faster inference. "
             "Build with: python -m vla_engine.trt.build_engine --help",
    )
    return parser.parse_args()


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main():
    args = parse_args()

    if not CV2_AVAILABLE:
        print("[ERROR] opencv-python is required. Install: pip install opencv-python")
        return

    # Device
    if TORCH_AVAILABLE:
        if args.device == "cuda" and torch.cuda.is_available():
            device = torch.device("cuda")
        else:
            if args.device == "cuda":
                print("[WARNING] CUDA not available, using CPU.")
            device = torch.device("cpu")
    else:
        device = None
        print("[WARNING] PyTorch not installed — using DummyPolicy.")

    # Policy
    policy = None
    if LEROBOT_AVAILABLE:
        try:
            print(f'Loading SmolVLAPolicy from "{args.checkpoint}"...')
            policy = SmolVLAPolicy.from_pretrained(args.checkpoint)
            policy = policy.to(device)
            policy.eval()
            print("Policy loaded.")

            if args.trt_engine:
                if not TRT_PATCH_AVAILABLE:
                    print(
                        "[WARNING] --trt-engine specified but vla_engine.trt is not importable. "
                        "Install tensorrt and add vla_engine to PYTHONPATH. "
                        "Running with PyTorch encoder."
                    )
                else:
                    try:
                        patch_policy_vision_encoder(policy, args.trt_engine)
                        print(f"[TRT] Vision encoder replaced with TRT engine: {args.trt_engine}")
                    except Exception as exc:
                        print(f"[WARNING] TRT patch failed: {exc}. Running with PyTorch encoder.")

        except Exception as exc:
            print(f"[WARNING] Failed to load policy: {exc}. Using DummyPolicy.")
            policy = DummyPolicy()
    else:
        print("[WARNING] lerobot not installed — using DummyPolicy (returns zeros).")
        policy = DummyPolicy()

    # Camera
    cap = cv2.VideoCapture(args.camera_id)
    if not cap.isOpened():
        print(f"[WARNING] Could not open camera {args.camera_id}. Using dummy frames.")
        cap = None

    print(f'\nRunning inference at {args.fps:.0f} Hz | Task: "{args.task}"')
    print('Press "q" in the display window to quit.\n')

    tick_period = 1.0 / args.fps
    frame_idx = 0
    fps_actual = args.fps
    t_prev = time.perf_counter()

    if hasattr(policy, "reset"):
        policy.reset()

    window_name = "SmolVLA Inference — OmniBot"
    cv2.namedWindow(window_name, cv2.WINDOW_NORMAL)
    cv2.resizeWindow(window_name, 640, 480)

    try:
        while True:
            t_start = time.perf_counter()

            # Capture frame
            if cap is not None:
                ret, frame_bgr = cap.read()
                if not ret:
                    print("[WARNING] Camera read failed. Using dummy frame.")
                    frame_bgr = np.zeros((IMAGE_H, IMAGE_W, 3), dtype=np.uint8)
            else:
                frame_bgr = np.zeros((IMAGE_H, IMAGE_W, 3), dtype=np.uint8)

            # Build observation
            wrist_t = frame_to_tensor(frame_bgr, device)
            front_t = dummy_tensor(device)  # front cam not available standalone
            state_t = build_state_tensor(device)

            obs = {
                "observation.images.wrist": wrist_t,
                "observation.images.front": front_t,
                "observation.state": state_t,
                "task": args.task,
            }

            # Inference
            try:
                with torch.no_grad() if TORCH_AVAILABLE else _null_context():
                    action = policy.select_action(obs)

                if TORCH_AVAILABLE and hasattr(action, "cpu"):
                    action_np = action.cpu().numpy()
                else:
                    action_np = np.array(action)

                if action_np.ndim == 2:
                    action_np = action_np[0]

            except Exception as exc:
                print(f"[WARNING] Inference error: {exc}")
                action_np = np.zeros(9, dtype=np.float32)

            # Compute actual FPS
            t_now = time.perf_counter()
            fps_actual = 1.0 / max(t_now - t_prev, 1e-6)
            t_prev = t_now

            # Print action to console
            arm = action_np[:6]
            base = action_np[6:9]
            print(
                f"[{frame_idx:5d}] "
                f"arm: [{', '.join(f'{v:+.4f}' for v in arm)}] | "
                f"base: vx={base[0]:+.4f} vy={base[1]:+.4f} vz={base[2]:+.4f} | "
                f"{fps_actual:.1f} Hz"
            )

            # Display
            display = cv2.resize(frame_bgr, (640, 480))
            display = overlay_action_text(display, action_np, fps_actual, frame_idx)
            cv2.imshow(window_name, display)

            frame_idx += 1

            # Exit on 'q'
            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                print("\nExiting (q pressed).")
                break

            # Pace loop
            elapsed = time.perf_counter() - t_start
            sleep_t = tick_period - elapsed
            if sleep_t > 0:
                time.sleep(sleep_t)

    except KeyboardInterrupt:
        print("\nInterrupted by user.")
    finally:
        if cap is not None:
            cap.release()
        cv2.destroyAllWindows()
        print("Done.")


class _null_context:
    def __enter__(self):
        return self

    def __exit__(self, *a):
        pass


if __name__ == "__main__":
    main()
