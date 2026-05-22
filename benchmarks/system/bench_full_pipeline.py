"""
benchmarks/system/bench_full_pipeline.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
End-to-end pipeline latency: camera frames → BEV stitch → SmolVLA preprocess → inference.

This is the single most important system-level latency number for SmolVLA operation.
It measures the total time from raw camera frame arrival to robot action output,
excluding ROS serialization and serial I/O.

Target: <200ms total for ≥5 Hz SmolVLA operation.

No hardware, no ROS — numpy/cv2/torch only.
Run:
    pytest benchmarks/system/bench_full_pipeline.py -v
    python benchmarks/system/bench_full_pipeline.py
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

import numpy as np
import pytest

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO))

try:
    import cv2

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False
    pytest.skip("OpenCV not installed", allow_module_level=True)

try:
    import torch

    TORCH_AVAILABLE = True
    DEVICE = torch.device("cuda" if torch.cuda.is_available() else "cpu")
except ImportError:
    TORCH_AVAILABLE = False
    DEVICE = None

from benchmarks.conftest import TimingHarness, print_stats, write_results

# ---------------------------------------------------------------------------
# Production constants
# ---------------------------------------------------------------------------

CANVAS_SIZE = 800
SRC_W, SRC_H = 640, 480
NUM_CAMERAS = 4
IMG_W, IMG_H = 320, 240  # SmolVLA target


def _tiled_homographies() -> list[np.ndarray]:
    cols, rows = 2, 2
    cell_w, cell_h = CANVAS_SIZE / cols, CANVAS_SIZE / rows
    sx, sy = cell_w / SRC_W, cell_h / SRC_H
    Hs = []
    for i in range(NUM_CAMERAS):
        row, col = divmod(i, cols)
        H = np.array(
            [[sx, 0, col * cell_w], [0, sy, row * cell_h], [0, 0, 1]],
            dtype=np.float64,
        )
        Hs.append(H)
    return Hs


HOMOGRAPHIES = _tiled_homographies()
_src_ones = np.ones((SRC_H, SRC_W), dtype=np.float32)
BLEND_WEIGHTS = [
    cv2.warpPerspective(_src_ones, H, (CANVAS_SIZE, CANVAS_SIZE)) for H in HOMOGRAPHIES
]
CAMERA_IMGS = [
    np.random.randint(0, 256, (SRC_H, SRC_W, 3), dtype=np.uint8)
    for _ in range(NUM_CAMERAS)
]
WRIST_IMG = np.random.randint(0, 256, (SRC_H, SRC_W, 3), dtype=np.uint8)


# ---------------------------------------------------------------------------
# Stage implementations matching production code exactly
# ---------------------------------------------------------------------------


def bev_stitch(camera_imgs: list[np.ndarray]) -> np.ndarray:
    """Replicate bev_stitcher_node._timer_cb() without ROS."""
    canvas = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 3), dtype=np.float32)
    w_sum = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 1), dtype=np.float32)
    for img, H, bw in zip(camera_imgs, HOMOGRAPHIES, BLEND_WEIGHTS):
        warped = cv2.warpPerspective(
            img.astype(np.float32), H, (CANVAS_SIZE, CANVAS_SIZE)
        )
        w = bw[:, :, np.newaxis]
        canvas += warped * w
        w_sum += w
    mask = w_sum[:, :, 0] > 0
    canvas[mask] /= w_sum[mask]
    return canvas.clip(0, 255).astype(np.uint8)


def numpy_to_tensor(img_np: np.ndarray, device=None):
    """Replicate smolvla_node._numpy_to_tensor()."""
    resized = cv2.resize(img_np, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR)
    arr = resized.astype(np.float32) / 255.0
    arr = arr.transpose(2, 0, 1)
    if TORCH_AVAILABLE:
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t
    return arr[np.newaxis]


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_pipeline_stages():
    """
    Break down the full pipeline into timed stages:
    1. BEV stitch (4 cameras)
    2. Wrist preprocess
    3. BEV preprocess
    4. State tensor build
    5. Inference (if lerobot available)
    """
    stages: dict[str, float] = {}

    t0 = time.perf_counter()
    bev_out = bev_stitch(CAMERA_IMGS)
    stages["bev_stitch_ms"] = (time.perf_counter() - t0) * 1000.0

    t0 = time.perf_counter()
    wrist_t = numpy_to_tensor(WRIST_IMG, DEVICE)
    stages["wrist_preprocess_ms"] = (time.perf_counter() - t0) * 1000.0

    t0 = time.perf_counter()
    bev_t = numpy_to_tensor(bev_out, DEVICE)
    stages["bev_preprocess_ms"] = (time.perf_counter() - t0) * 1000.0

    if TORCH_AVAILABLE:
        arm_positions = np.zeros(6, dtype=np.float32)
        base_vel = np.zeros(3, dtype=np.float32)
        t0 = time.perf_counter()
        state = np.concatenate([arm_positions, base_vel])
        state_t = torch.from_numpy(state).unsqueeze(0)
        if DEVICE:
            state_t = state_t.to(DEVICE)
        stages["state_build_ms"] = (time.perf_counter() - t0) * 1000.0

    total_preprocess = sum(v for k, v in stages.items() if "inference" not in k)
    stages["total_preprocess_ms"] = total_preprocess

    print("\n  Pipeline stage breakdown:")
    for name, ms in stages.items():
        print(f"    {name:35s}: {ms:7.2f}ms")

    return stages


def test_bench_full_pipeline_no_inference():
    """
    Full pipeline WITHOUT model inference: BEV stitch + both camera preprocesses.
    This is the per-cycle overhead even when the model is fast.
    """
    h = TimingHarness()

    def _pipeline():
        bev_out = bev_stitch(CAMERA_IMGS)
        wrist_t = numpy_to_tensor(WRIST_IMG, DEVICE)
        bev_t = numpy_to_tensor(bev_out, DEVICE)
        return wrist_t, bev_t

    stats = h.run(_pipeline, n=100, warmup=5)
    print_stats("full_pipeline_no_inference_ms (stitch + 2× preprocess)", stats)

    budget_ms = 100.0  # 10 Hz inference budget total
    inference_budget_remaining = budget_ms - stats["median_ms"]
    print(
        f"  INFO  Preprocessing uses {stats['median_ms']:.1f}ms of {budget_ms}ms budget.\n"
        f"  INFO  Remaining for inference: {inference_budget_remaining:.1f}ms\n"
        f"  INFO  If preprocessing > budget, reduce BEV stitcher output to 320×240."
    )
    return stats


def test_bench_full_pipeline_with_dummy_policy():
    """
    Full pipeline WITH DummyPolicy (zero latency inference) — measures overhead
    of observation assembly, tensor allocation, and action decoding.
    """
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    # Replicate DummyPolicy from smolvla_node.py
    class DummyPolicy:
        def select_action(self, obs):
            return torch.zeros(1, 9)

    policy = DummyPolicy()

    h = TimingHarness()

    def _full_with_dummy():
        bev_out = bev_stitch(CAMERA_IMGS)
        wrist_t = numpy_to_tensor(WRIST_IMG, DEVICE)
        bev_t = numpy_to_tensor(bev_out, DEVICE)

        arm_positions = np.zeros(6, dtype=np.float32)
        base_vel = np.zeros(3, dtype=np.float32)
        state = np.concatenate([arm_positions, base_vel])
        state_t = torch.from_numpy(state).unsqueeze(0)
        if DEVICE:
            state_t = state_t.to(DEVICE)

        obs = {
            "observation.images.wrist": wrist_t,
            "observation.images.bev": bev_t,
            "observation.state": state_t,
            "task": "pick up the object",
        }

        with torch.no_grad():
            action = policy.select_action(obs)

        action_np = action.cpu().numpy()
        if action_np.ndim == 2:
            action_np = action_np[0]

        arm = action_np[:6]
        base = action_np[6:9]
        return arm, base

    stats = h.run(_full_with_dummy, n=100, warmup=5)
    print_stats(
        "full_pipeline_dummy_policy_ms (stitch + preprocess + dummy inference)", stats
    )
    return stats


def test_bench_optimized_pipeline():
    """
    Optimized pipeline: pre-resize BEV at stitcher output (320×240).
    Eliminates the double-resize in smolvla_node._numpy_to_tensor() for BEV.
    """
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    def _bev_stitch_small():
        """BEV stitcher with 320×240 output (proposed optimization)."""
        canvas = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 3), dtype=np.float32)
        w_sum = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 1), dtype=np.float32)
        for img, H, bw in zip(CAMERA_IMGS, HOMOGRAPHIES, BLEND_WEIGHTS):
            warped = cv2.warpPerspective(
                img.astype(np.float32), H, (CANVAS_SIZE, CANVAS_SIZE)
            )
            w = bw[:, :, np.newaxis]
            canvas += warped * w
            w_sum += w
        mask = w_sum[:, :, 0] > 0
        canvas[mask] /= w_sum[mask]
        out = canvas.clip(0, 255).astype(np.uint8)
        return cv2.resize(out, (IMG_W, IMG_H))  # pre-resize to SmolVLA target

    def _numpy_to_tensor_no_resize(img_np: np.ndarray, device=None):
        """Optimized preprocess: no resize needed (image already at target size)."""
        arr = img_np.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t

    h = TimingHarness()

    def _opt_pipeline():
        bev_out_small = _bev_stitch_small()
        wrist_t = numpy_to_tensor(WRIST_IMG, DEVICE)
        bev_t = _numpy_to_tensor_no_resize(bev_out_small, DEVICE)
        return wrist_t, bev_t

    stats_opt = h.run(_opt_pipeline, n=100, warmup=5)
    stats_orig = h.run(
        test_bench_full_pipeline_no_inference, n=1, warmup=0
    )  # single run

    print_stats("full_pipeline_optimized_ms (pre-resize BEV at stitcher)", stats_opt)

    # Run original for direct comparison
    def _orig_pipeline():
        bev_out = bev_stitch(CAMERA_IMGS)
        wrist_t = numpy_to_tensor(WRIST_IMG, DEVICE)
        bev_t = numpy_to_tensor(bev_out, DEVICE)
        return wrist_t, bev_t

    stats_orig = h.run(_orig_pipeline, n=100, warmup=5)
    savings = stats_orig["median_ms"] - stats_opt["median_ms"]
    print(
        f"  INFO  Original: {stats_orig['median_ms']:.1f}ms  "
        f"Optimized: {stats_opt['median_ms']:.1f}ms  "
        f"Savings: {savings:.1f}ms/frame = {savings * 10:.0f}ms/s at 10 Hz"
    )
    return stats_opt


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — Full Pipeline System Benchmarks")
    print(f"BEV: {CANVAS_SIZE}×{CANVAS_SIZE}→{IMG_W}×{IMG_H}  Device: {DEVICE}")
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[Stage Breakdown]")
    all_results["stage_breakdown"] = test_bench_pipeline_stages()

    print("\n[End-to-End Pipeline]")
    all_results["no_inference"] = test_bench_full_pipeline_no_inference()
    if TORCH_AVAILABLE:
        all_results["dummy_policy"] = test_bench_full_pipeline_with_dummy_policy()
        all_results["optimized"] = test_bench_optimized_pipeline()

    write_results("full_pipeline", all_results)
    print("\nDone.")
