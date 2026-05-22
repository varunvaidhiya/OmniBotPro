"""benchmarks/vision/bench_preprocess.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Image preprocessing pipeline benchmarks for visuomotor policy inference.

Replicates the exact call chain from policy_node.py:
  _ros_image_to_numpy() → _numpy_to_tensor()
which runs at every inference cycle.

Also demonstrates the BEV double-resize waste:
  stitcher outputs 800×800 → policy resizes to 320×240.
  Fix: set stitcher output_width=320, output_height=240.

No ROS, no hardware — numpy/cv2/torch only.

Run:
    pytest benchmarks/vision/bench_preprocess.py -v
    python benchmarks/vision/bench_preprocess.py
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

IMG_WRIST = np.random.randint(
    0, 256, (480, 640, 3), dtype=np.uint8
)  # 640×480 wrist cam
IMG_BEV_800 = np.random.randint(
    0, 256, (800, 800, 3), dtype=np.uint8
)  # 800×800 BEV stitcher output

# Policy target resolution (policy_node.py defaults: image_width=320, image_height=240)
IMG_W, IMG_H = 320, 240


# ---------------------------------------------------------------------------
# Reusable numpy→tensor function (mirrors policy_node._numpy_to_tensor)
# ---------------------------------------------------------------------------


def numpy_to_tensor(
    img_np: np.ndarray, target_w: int, target_h: int, device
) -> "torch.Tensor":
    resized = cv2.resize(img_np, (target_w, target_h), interpolation=cv2.INTER_LINEAR)
    arr = resized.astype(np.float32) / 255.0
    arr = arr.transpose(2, 0, 1)
    t = torch.from_numpy(arr).unsqueeze(0)
    if device is not None:
        t = t.to(device)
    return t


# ---------------------------------------------------------------------------
# Individual step benchmarks
# ---------------------------------------------------------------------------


def test_bench_cv_bridge_simulation():
    """Simulate cv_bridge imgmsg_to_cv2 (frombuffer + reshape)."""
    raw_bytes = IMG_WRIST.tobytes()

    def _sim():
        arr = np.frombuffer(raw_bytes, dtype=np.uint8)
        return arr.reshape((480, 640, 3)).copy()

    stats = TimingHarness().run(_sim, n=2000, warmup=50)
    print_stats("cv_bridge_simulate_ms (frombuffer + reshape, 640×480)", stats)
    return stats


def test_bench_resize_wrist():
    """cv2.resize 640×480 → 320×240 (wrist camera path)."""
    stats = TimingHarness().run(
        lambda: cv2.resize(IMG_WRIST, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR),
        n=5000,
        warmup=100,
    )
    print_stats("resize_ms (640×480 → 320×240, wrist)", stats)
    assert check_slo("policy_preprocess_ms", stats["p95_ms"])
    return stats


def test_bench_resize_bev_800():
    """cv2.resize 800×800 → 320×240 (BEV double-resize path).

    The stitcher outputs 800×800 and the policy resizes again.
    Pre-resizing at the stitcher to 320×240 eliminates this cost.
    """
    stats = TimingHarness().run(
        lambda: cv2.resize(IMG_BEV_800, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR),
        n=5000,
        warmup=100,
    )
    wrist_stats = TimingHarness().run(
        lambda: cv2.resize(IMG_WRIST, (IMG_W, IMG_H), interpolation=cv2.INTER_LINEAR),
        n=5000,
        warmup=100,
    )
    print_stats("bev_resize_ms (800×800 → 320×240, double-resize)", stats)
    overhead = stats["mean_ms"] - wrist_stats["mean_ms"]
    print(
        f"  INFO  BEV overhead vs wrist: +{overhead:.3f}ms/frame "
        f"(fix: set stitcher output_width=320, output_height=240)"
    )
    return stats


def test_bench_normalize():
    resized = cv2.resize(IMG_WRIST, (IMG_W, IMG_H))
    stats = TimingHarness().run(
        lambda: resized.astype(np.float32) / 255.0, n=10000, warmup=200
    )
    print_stats("normalize_ms (astype float32 / 255, 320×240)", stats)
    return stats


def test_bench_transpose():
    arr = np.random.rand(IMG_H, IMG_W, 3).astype(np.float32)
    stats = TimingHarness().run(lambda: arr.transpose(2, 0, 1), n=10000, warmup=200)
    print_stats("transpose_ms (HWC→CHW, 240×320×3)", stats)
    return stats


# ---------------------------------------------------------------------------
# Full pipeline benchmarks
# ---------------------------------------------------------------------------


def test_bench_full_wrist():
    """Full preprocessing for wrist camera (640×480 input → 1×3×240×320 GPU tensor)."""
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    stats = TimingHarness().run(
        lambda: numpy_to_tensor(IMG_WRIST, IMG_W, IMG_H, DEVICE),
        n=2000,
        warmup=50,
    )
    print_stats("preprocess_ms (wrist, 640×480→320×240)", stats)
    assert check_slo("policy_preprocess_ms", stats["p95_ms"])
    return stats


def test_bench_full_bev():
    """Full preprocessing for BEV camera (800×800 → 320×240, double-resize path)."""
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    stats = TimingHarness().run(
        lambda: numpy_to_tensor(IMG_BEV_800, IMG_W, IMG_H, DEVICE),
        n=2000,
        warmup=50,
    )
    wrist_stats = TimingHarness().run(
        lambda: numpy_to_tensor(IMG_WRIST, IMG_W, IMG_H, DEVICE),
        n=2000,
        warmup=50,
    )
    print_stats("preprocess_bev_ms (800×800→320×240)", stats)
    overhead = stats["mean_ms"] - wrist_stats["mean_ms"]
    print(f"  INFO  BEV preprocess overhead: +{overhead:.3f}ms per frame")
    return stats


def test_bench_both_cameras():
    """Both cameras in sequence: wrist + BEV (actual per-inference cost)."""
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    def _both():
        wrist_t = numpy_to_tensor(IMG_WRIST, IMG_W, IMG_H, DEVICE)
        bev_t = numpy_to_tensor(IMG_BEV_800, IMG_W, IMG_H, DEVICE)
        return wrist_t, bev_t

    stats = TimingHarness().run(_both, n=1000, warmup=50)
    print_stats("preprocess_both_cameras_ms (wrist + bev combined)", stats)
    fps = 1000.0 / stats["median_ms"]
    print(
        f"  INFO  Preprocess alone allows up to {fps:.1f} Hz before inference is added."
    )
    return stats


def test_bench_bev_preresized():
    """Optimised BEV path: stitcher outputs 320×240 directly (no double-resize)."""
    if not TORCH_AVAILABLE:
        pytest.skip("torch not installed")

    img_bev_320 = np.random.randint(0, 256, (IMG_H, IMG_W, 3), dtype=np.uint8)

    def _opt(img_np):
        arr = img_np.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)
        t = torch.from_numpy(arr).unsqueeze(0)
        return t.to(DEVICE) if DEVICE is not None else t

    stats_opt = TimingHarness().run(lambda: _opt(img_bev_320), n=2000, warmup=50)
    stats_orig = TimingHarness().run(
        lambda: numpy_to_tensor(IMG_BEV_800, IMG_W, IMG_H, DEVICE),
        n=2000,
        warmup=50,
    )
    print_stats("preprocess_opt_ms (320×240 BEV, no double-resize)", stats_opt)
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
    print("OmniBot — Image Preprocessing Benchmarks")
    print(f"Target: {IMG_W}×{IMG_H}  Device: {DEVICE}")
    print("=" * 70)

    results = {}

    print("\n[Individual pipeline steps]")
    results["cv_bridge_sim"] = test_bench_cv_bridge_simulation()
    results["resize_wrist"] = test_bench_resize_wrist()
    results["resize_bev_800"] = test_bench_resize_bev_800()
    results["normalize"] = test_bench_normalize()
    results["transpose"] = test_bench_transpose()

    if TORCH_AVAILABLE:
        print("\n[Full pipeline]")
        results["full_wrist"] = test_bench_full_wrist()
        results["full_bev"] = test_bench_full_bev()
        results["both_cameras"] = test_bench_both_cameras()

        print("\n[Optimisation: pre-resize BEV at stitcher]")
        results["bev_preresized"] = test_bench_bev_preresized()

    write_results("policy_preprocess", results)
    print("\nDone.")
