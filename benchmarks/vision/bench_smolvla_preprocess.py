"""
benchmarks/vision/bench_smolvla_preprocess.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Benchmarks for the SmolVLA image preprocessing pipeline.

Replicates the exact call chain from smolvla_node.py:
  _ros_image_to_numpy() → _numpy_to_tensor()
which runs at every 10 Hz inference cycle.

Also demonstrates the BEV double-resize waste:
  stitcher outputs 800×800 → SmolVLA _numpy_to_tensor resizes to 320×240.

No ROS, no hardware — numpy/cv2/torch only.
Run:
    pytest benchmarks/vision/bench_smolvla_preprocess.py -v
    python benchmarks/vision/bench_smolvla_preprocess.py
"""

from __future__ import annotations

import sys
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

from benchmarks.conftest import TimingHarness, check_slo, print_stats, write_results

# ---------------------------------------------------------------------------
# Test images — matching production camera resolutions
# ---------------------------------------------------------------------------

# Wrist camera (raw from _ros_image_to_numpy)
IMG_WRIST = np.random.randint(0, 256, (480, 640, 3), dtype=np.uint8)

# BEV camera (800×800 from bev_stitcher_node)
IMG_BEV_800 = np.random.randint(0, 256, (800, 800, 3), dtype=np.uint8)

# SmolVLA target resolution (from node parameters: image_width=320, image_height=240)
IMG_W, IMG_H = 320, 240


# ---------------------------------------------------------------------------
# Step-by-step pipeline benchmarks
# ---------------------------------------------------------------------------


def test_bench_cv_bridge_simulation():
    """
    Simulate cv_bridge imgmsg_to_cv2 cost using np.frombuffer + reshape.
    The actual cv_bridge call wraps this with header parsing overhead.
    This gives a lower bound — real cv_bridge is slightly slower.
    """
    # Simulate a raw ROS image message buffer (640×480 RGB8)
    raw_bytes = IMG_WRIST.tobytes()

    def _simulate_bridge():
        arr = np.frombuffer(raw_bytes, dtype=np.uint8)
        return arr.reshape((480, 640, 3)).copy()

    h = TimingHarness()
    stats = h.run(_simulate_bridge, n=2000, warmup=50)
    print_stats("cv_bridge_simulate_ms (frombuffer + reshape, 640×480)", stats)
    return stats


def test_bench_resize_wrist():
    """cv2.resize 640×480 → 320×240 (wrist camera path in _numpy_to_tensor)."""
    h = TimingHarness()
    stats = h.run(
        lambda: cv2.resize(
            IMG_WRIST, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR
        ),
        n=5000,
        warmup=100,
    )
    print_stats("smolvla_resize_ms (640×480 → 320×240, wrist)", stats)
    assert check_slo("smolvla_resize_ms", stats["p95_ms"])
    return stats


def test_bench_resize_bev_800():
    """
    cv2.resize 800×800 → 320×240 (BEV camera path — LARGER input than wrist).
    This is the double-resize waste: stitcher already outputting 800×800,
    SmolVLA resizes again. Pre-resizing at stitcher to 320×240 would save this.
    """
    h = TimingHarness()
    stats = h.run(
        lambda: cv2.resize(
            IMG_BEV_800, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR
        ),
        n=5000,
        warmup=100,
    )
    print_stats("bev_resize_ms (800×800 → 320×240, bev double-resize)", stats)

    # Compare against same-size wrist resize to show overhead
    stats_wrist = TimingHarness().run(
        lambda: cv2.resize(IMG_WRIST, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR),
        n=5000,
        warmup=100,
    )
    overhead = stats["mean_ms"] - stats_wrist["mean_ms"]
    print(
        f"  INFO  BEV resize overhead vs wrist: +{overhead:.3f}ms/frame "
        f"(fix: set stitcher output_width=320, output_height=240)"
    )
    return stats


def test_bench_normalize():
    """arr.astype(float32) / 255.0 — normalization to [0,1] range."""
    resized = cv2.resize(IMG_WRIST, (IMG_W, IMG_H))
    h = TimingHarness()
    stats = h.run(lambda: resized.astype(np.float32) / 255.0, n=10000, warmup=200)
    print_stats("normalize_ms (astype float32 / 255, 320×240)", stats)
    return stats


def test_bench_transpose():
    """arr.transpose(2, 0, 1) — HWC→CHW channel reorder."""
    arr = np.random.rand(IMG_H, IMG_W, 3).astype(np.float32)
    h = TimingHarness()
    stats = h.run(lambda: arr.transpose(2, 0, 1), n=10000, warmup=200)
    print_stats("transpose_ms (HWC→CHW, 240×320×3)", stats)
    return stats


def test_bench_cpu_tensor():
    """torch.from_numpy + unsqueeze — CPU tensor creation from numpy array."""
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")
    arr = np.random.rand(3, IMG_H, IMG_W).astype(np.float32)
    h = TimingHarness()
    stats = h.run(lambda: torch.from_numpy(arr).unsqueeze(0), n=10000, warmup=200)
    print_stats("cpu_tensor_ms (from_numpy + unsqueeze, 3×240×320)", stats)
    return stats


def test_bench_gpu_transfer():
    """tensor.to(device) — host→GPU data transfer (cuda path)."""
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")
    if not torch.cuda.is_available():
        pytest.skip("CUDA not available")
    arr = np.random.rand(3, IMG_H, IMG_W).astype(np.float32)
    t = torch.from_numpy(arr).unsqueeze(0)
    device = torch.device("cuda")
    h = TimingHarness()
    stats = h.run(lambda: t.to(device), n=2000, warmup=100)
    print_stats("gpu_transfer_ms (H→D, 1×3×240×320)", stats)
    return stats


def test_bench_full_numpy_to_tensor_wrist():
    """
    Full _numpy_to_tensor() pipeline for wrist camera (640×480 input).
    Matches smolvla_node.py: resize → normalize → transpose → tensor → GPU.
    """
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    device = DEVICE

    def _numpy_to_tensor(img_np: np.ndarray):
        resized = cv2.resize(img_np, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR)
        arr = resized.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t

    h = TimingHarness()
    stats = h.run(lambda: _numpy_to_tensor(IMG_WRIST), n=2000, warmup=50)
    print_stats("smolvla_preprocess_ms (wrist, 640×480→320×240)", stats)
    assert check_slo("smolvla_preprocess_ms", stats["p95_ms"])
    return stats


def test_bench_full_numpy_to_tensor_bev():
    """
    Full _numpy_to_tensor() pipeline for BEV camera (800×800 input).
    Demonstrates the double-resize waste — larger input than necessary.
    """
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    device = DEVICE

    def _numpy_to_tensor_bev(img_np: np.ndarray):
        resized = cv2.resize(img_np, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR)
        arr = resized.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t

    h = TimingHarness()
    stats = h.run(lambda: _numpy_to_tensor_bev(IMG_BEV_800), n=2000, warmup=50)
    print_stats("smolvla_preprocess_bev_ms (bev, 800×800→320×240)", stats)
    check_slo("smolvla_preprocess_ms", stats["p95_ms"], fail_on_max=False)

    # Compare against wrist path
    wrist_stats = h.run(
        lambda: _numpy_to_tensor_bev(IMG_WRIST), n=2000, warmup=50
    )
    overhead = stats["mean_ms"] - wrist_stats["mean_ms"]
    print(
        f"  INFO  BEV preprocess overhead vs wrist: +{overhead:.3f}ms "
        f"(fix: pre-resize BEV to 320×240 at stitcher output)"
    )
    return stats


def test_bench_two_cameras_combined():
    """
    Both cameras together: wrist + BEV preprocess in sequence.
    This is the actual per-inference cost in inference_loop().
    """
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    device = DEVICE

    def _preprocess(img_np):
        resized = cv2.resize(img_np, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR)
        arr = resized.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t

    def _both_cameras():
        wrist_t = _preprocess(IMG_WRIST)
        bev_t = _preprocess(IMG_BEV_800)
        return wrist_t, bev_t

    h = TimingHarness()
    stats = h.run(_both_cameras, n=1000, warmup=50)
    print_stats("smolvla_two_cameras_ms (wrist + bev combined)", stats)

    fps_headroom = 1000.0 / stats["median_ms"]
    print(
        f"  INFO  Preprocess alone allows up to {fps_headroom:.1f} Hz. "
        f"(SmolVLA inference adds on top of this.)"
    )
    return stats


def test_bench_optimized_bev_preprocess():
    """
    Optimized path: BEV stitcher outputs 320×240 directly (no double-resize).
    Measures savings from setting stitcher output_width=320, output_height=240.
    """
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    device = DEVICE

    # Simulate stitcher outputting 320×240 directly
    img_bev_320 = np.random.randint(0, 256, (IMG_H, IMG_W, 3), dtype=np.uint8)

    def _numpy_to_tensor_opt(img_np: np.ndarray):
        # No resize needed — already at target resolution
        arr = img_np.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)
        t = torch.from_numpy(arr).unsqueeze(0)
        if device is not None:
            t = t.to(device)
        return t

    h = TimingHarness()
    stats_opt = h.run(lambda: _numpy_to_tensor_opt(img_bev_320), n=2000, warmup=50)
    stats_orig = h.run(
        lambda: _numpy_to_tensor_opt(
            cv2.resize(IMG_BEV_800, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR)
        ),
        n=2000,
        warmup=50,
    )

    print_stats("smolvla_preprocess_opt_ms (320×240 BEV, no double-resize)", stats_opt)
    savings = stats_orig["mean_ms"] - stats_opt["mean_ms"]
    print(
        f"  INFO  Savings from pre-resizing BEV at stitcher: "
        f"{savings:.3f}ms/inference = {savings * 10:.1f}ms/s at 10 Hz"
    )
    return stats_opt


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — SmolVLA Image Preprocessing Benchmarks")
    print(f"Target: {IMG_W}×{IMG_H}  Device: {DEVICE}")
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[Pipeline Steps — Individual]")
    all_results["cv_bridge_sim"] = test_bench_cv_bridge_simulation()
    all_results["resize_wrist"] = test_bench_resize_wrist()
    all_results["resize_bev_800"] = test_bench_resize_bev_800()
    all_results["normalize"] = test_bench_normalize()
    all_results["transpose"] = test_bench_transpose()

    if TORCH_AVAILABLE:
        print("\n[Tensor Operations]")
        all_results["cpu_tensor"] = test_bench_cpu_tensor()
        if DEVICE and DEVICE.type == "cuda":
            all_results["gpu_transfer"] = test_bench_gpu_transfer()

        print("\n[Full Pipeline]")
        all_results["full_wrist"] = test_bench_full_numpy_to_tensor_wrist()
        all_results["full_bev"] = test_bench_full_numpy_to_tensor_bev()
        all_results["two_cameras"] = test_bench_two_cameras_combined()

        print("\n[Optimization: Pre-resize at Stitcher]")
        all_results["optimized_bev"] = test_bench_optimized_bev_preprocess()

    write_results("smolvla_preprocess", all_results)
    print("\nDone.")
