"""
benchmarks/vision/bench_bev_stitcher.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Benchmarks for the BEV (Bird's Eye View) image stitching pipeline.

The BEV stitcher runs at 30 Hz (33ms budget) on Pi 5 CPU.
Based on code analysis, 4× warpPerspective at 800×800 float32 is estimated
at 50–80ms on Pi 5 CPU — already over budget. This benchmark quantifies it.

No hardware, no ROS — uses OpenCV and numpy directly.
Run:
    pytest benchmarks/vision/bench_bev_stitcher.py -v
    python benchmarks/vision/bench_bev_stitcher.py
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

from benchmarks.conftest import TimingHarness, check_slo, print_stats, write_results

# ---------------------------------------------------------------------------
# Production-matching constants (from bev_stitcher_node.py)
# ---------------------------------------------------------------------------

CANVAS_SIZE = 800  # pixels — square compositing canvas
SRC_W, SRC_H = 640, 480  # input camera resolution
OUT_W, OUT_H = 800, 800  # output resolution (default same as canvas)
NUM_CAMERAS = 4

# Homography matrices — identity-based tiled layout (worst case for warping,
# no early-exit optimization from degenerate H).
# Production uses calibrated homographies; tiled = most expensive case.


def _tiled_homographies() -> list[np.ndarray]:
    """Generate tiled homographies matching bev_stitcher_node fallback path."""
    Hs = []
    cols, rows = 2, 2
    cell_w, cell_h = CANVAS_SIZE / cols, CANVAS_SIZE / rows
    sx, sy = cell_w / SRC_W, cell_h / SRC_H
    for i in range(NUM_CAMERAS):
        row, col = divmod(i, cols)
        H = np.array(
            [[sx, 0, col * cell_w], [0, sy, row * cell_h], [0, 0, 1]],
            dtype=np.float64,
        )
        Hs.append(H)
    return Hs


HOMOGRAPHIES = _tiled_homographies()

# Pre-allocate test images (random uint8, mimics real camera frames)
_CAMERA_IMGS = [
    np.random.randint(0, 256, (SRC_H, SRC_W, 3), dtype=np.uint8)
    for _ in range(NUM_CAMERAS)
]

# Pre-compute blend weights (done once at init in production)
_src_ones = np.ones((SRC_H, SRC_W), dtype=np.float32)
_BLEND_WEIGHTS = [
    cv2.warpPerspective(_src_ones, H, (CANVAS_SIZE, CANVAS_SIZE)) for H in HOMOGRAPHIES
]


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_single_warp_perspective():
    """
    Single cv2.warpPerspective float32 @ 800×800 — the inner loop operation.
    4 of these run per frame in production.
    """
    img_f32 = _CAMERA_IMGS[0].astype(np.float32)
    H = HOMOGRAPHIES[0]
    h = TimingHarness()
    stats = h.run(
        lambda: cv2.warpPerspective(img_f32, H, (CANVAS_SIZE, CANVAS_SIZE)),
        n=500,
        warmup=20,
    )
    print_stats("warp_perspective_ms (single camera, 800×800 float32)", stats)
    assert check_slo("warp_perspective_ms", stats["p95_ms"])
    return stats


def test_bench_four_warp_perspectives():
    """
    4× warpPerspective — full per-frame warp cost for all cameras.
    This is the primary bottleneck for the 33ms 30 Hz budget.
    """
    imgs_f32 = [img.astype(np.float32) for img in _CAMERA_IMGS]

    def _four_warps():
        results = []
        for img, H in zip(imgs_f32, HOMOGRAPHIES):
            results.append(cv2.warpPerspective(img, H, (CANVAS_SIZE, CANVAS_SIZE)))
        return results

    h = TimingHarness()
    stats = h.run(_four_warps, n=200, warmup=10)
    print_stats("warp_4cam_cpu_ms (4× warpPerspective 800×800)", stats)
    # SLO: 4 warps must fit within the 33ms total stitch budget
    check_slo("bev_stitch_frame_ms", stats["p95_ms"], fail_on_max=False)
    return stats


def test_bench_astype_float32():
    """
    img.astype(np.float32) — dtype cast applied to each camera frame before warp.
    Quantifies whether pre-caching float32 frames at subscription time would help.
    """
    img = _CAMERA_IMGS[0]
    h = TimingHarness()
    stats = h.run(lambda: img.astype(np.float32), n=5000, warmup=100)
    print_stats("astype_float32_ms (640×480 uint8→float32)", stats)
    return stats


def test_bench_alpha_compositing():
    """
    Alpha compositing accumulation: canvas += warped * w_broadcast.
    Measures the weighted sum step separate from warpPerspective.
    """
    canvas = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 3), dtype=np.float32)
    w_sum = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 1), dtype=np.float32)
    warped = np.random.rand(CANVAS_SIZE, CANVAS_SIZE, 3).astype(np.float32)
    w = _BLEND_WEIGHTS[0][:, :, np.newaxis]

    def _accumulate():
        np.add(canvas, warped * w, out=canvas)
        np.add(w_sum, w, out=w_sum)

    h = TimingHarness()
    stats = h.run(_accumulate, n=1000, warmup=50)
    print_stats("bev_blend_ms (canvas += warped*w per camera)", stats)
    return stats


def test_bench_masked_division():
    """
    canvas[mask] /= w_sum[mask] — the normalization step after all warps.
    This masked-index operation can be slow on large canvases.
    """
    canvas = np.random.rand(CANVAS_SIZE, CANVAS_SIZE, 3).astype(np.float32)
    w_sum = np.random.uniform(0.1, 4.0, (CANVAS_SIZE, CANVAS_SIZE, 1)).astype(
        np.float32
    )
    mask = w_sum[:, :, 0] > 0  # all True in this synthetic case

    h = TimingHarness()
    stats = h.run(
        lambda: canvas.__setitem__(mask, canvas[mask] / w_sum[mask]), n=1000, warmup=50
    )
    print_stats("bev_masked_div_ms (canvas[mask] /= w_sum[mask])", stats)
    return stats


def test_bench_full_stitch_frame():
    """
    Full _timer_cb() equivalent: 4× warp + blend + masked div + clip + resize.
    This is the end-to-end per-frame latency measurement.
    Target: <33ms for 30 Hz operation.
    """
    imgs_f32 = [img.astype(np.float32) for img in _CAMERA_IMGS]

    def _full_stitch():
        canvas = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 3), dtype=np.float32)
        w_sum = np.zeros((CANVAS_SIZE, CANVAS_SIZE, 1), dtype=np.float32)

        for img, H, bw in zip(imgs_f32, HOMOGRAPHIES, _BLEND_WEIGHTS):
            warped = cv2.warpPerspective(img, H, (CANVAS_SIZE, CANVAS_SIZE))
            w = bw[:, :, np.newaxis]
            canvas += warped * w
            w_sum += w

        mask = w_sum[:, :, 0] > 0
        canvas[mask] /= w_sum[mask]

        out = canvas.clip(0, 255).astype(np.uint8)
        if OUT_W != CANVAS_SIZE or OUT_H != CANVAS_SIZE:
            out = cv2.resize(out, (OUT_W, OUT_H))
        return out

    h = TimingHarness()
    stats = h.run(_full_stitch, n=200, warmup=10)
    print_stats("bev_stitch_frame_ms (full _timer_cb equivalent)", stats)
    assert check_slo("bev_stitch_frame_ms", stats["p95_ms"])

    fps_achievable = 1000.0 / stats["median_ms"]
    print(
        f"  INFO  Achievable FPS on this hardware: "
        f"{fps_achievable:.1f} Hz (target: 30 Hz)"
    )
    return stats


def test_bench_blend_weights_init():
    """
    _compute_blend_weights() — one-time cost at node startup.
    Not on the hot path but impacts startup latency.
    """
    src_ones = np.ones((SRC_H, SRC_W), dtype=np.float32)

    def _compute_all():
        return [
            cv2.warpPerspective(src_ones, H, (CANVAS_SIZE, CANVAS_SIZE))
            for H in HOMOGRAPHIES
        ]

    h = TimingHarness()
    stats = h.run(_compute_all, n=100, warmup=5)
    print_stats("blend_weights_init_ms (4-camera, at startup)", stats)
    return stats


def test_bench_output_resize():
    """cv2.resize 800×800→640×480 — optional final step if output != canvas."""
    img = np.random.randint(0, 256, (CANVAS_SIZE, CANVAS_SIZE, 3), dtype=np.uint8)
    h = TimingHarness()
    stats = h.run(lambda: cv2.resize(img, (640, 480)), n=2000, warmup=50)
    print_stats("output_resize_ms (800×800 → 640×480)", stats)
    return stats


def test_bench_pre_resize_optimization():
    """
    Optimization comparison: SmolVLA needs 320×240 input.
    Option A (current): stitch at 800×800, then SmolVLA resizes to 320×240.
    Option B: stitch at 800×800, resize to 320×240 at stitcher output.

    This benchmark shows Option B savings vs Option A (SmolVLA resize side).
    """
    # Option A: smolvla does the resize (measured in bench_smolvla_preprocess)
    big_img = np.random.randint(0, 256, (CANVAS_SIZE, CANVAS_SIZE, 3), dtype=np.uint8)

    h = TimingHarness()
    stats_resize = h.run(
        lambda: cv2.resize(big_img, (320, 240), interpolation=cv2.INTER_LINEAR),
        n=2000,
        warmup=50,
    )
    print_stats(
        "bev_smolvla_resize_ms (800×800 → 320×240 at SmolVLA side)", stats_resize
    )
    print(
        "  INFO  Pre-resizing at stitcher output (320×240) would save this resize "
        "cost per SmolVLA inference. See bench_smolvla_preprocess.py for full analysis."
    )
    return stats_resize


def test_bench_gpu_warp_perspective():
    """
    GPU-accelerated warpPerspective via cv2.cuda (if available).
    Only runs on machines with CUDA-enabled OpenCV.
    Skipped silently if cv2.cuda not present.
    """
    if not hasattr(cv2, "cuda"):
        pytest.skip("cv2.cuda not available — build OpenCV with CUDA support")

    try:
        cv2.cuda.getCudaEnabledDeviceCount()
    except Exception:
        pytest.skip("CUDA device not accessible via cv2.cuda")

    img_gpu = cv2.cuda_GpuMat()
    img_f32 = _CAMERA_IMGS[0].astype(np.float32)
    img_gpu.upload(img_f32)
    H = HOMOGRAPHIES[0]

    def _gpu_warp():
        return cv2.cuda.warpPerspective(img_gpu, H, (CANVAS_SIZE, CANVAS_SIZE))

    h = TimingHarness()
    stats = h.run(_gpu_warp, n=500, warmup=50)
    print_stats("warp_perspective_gpu_ms (cv2.cuda, 800×800)", stats)

    cpu_stats = h.run(
        lambda: cv2.warpPerspective(img_f32, H, (CANVAS_SIZE, CANVAS_SIZE)),
        n=500,
        warmup=50,
    )
    speedup = cpu_stats["mean_ms"] / stats["mean_ms"] if stats["mean_ms"] > 0 else 0
    print(f"  INFO  GPU speedup vs CPU: {speedup:.1f}×")
    return stats


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — BEV Stitcher Vision Benchmarks")
    print(
        f"Canvas: {CANVAS_SIZE}×{CANVAS_SIZE}  Input: {SRC_W}×{SRC_H}  Cameras: {NUM_CAMERAS}"
    )
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[warpPerspective — Core Operation]")
    all_results["single_warp"] = test_bench_single_warp_perspective()
    all_results["four_warps"] = test_bench_four_warp_perspectives()
    all_results["astype_float32"] = test_bench_astype_float32()

    print("\n[Alpha Compositing]")
    all_results["alpha_composite"] = test_bench_alpha_compositing()
    all_results["masked_division"] = test_bench_masked_division()

    print("\n[End-to-End Frame]")
    all_results["full_stitch"] = test_bench_full_stitch_frame()

    print("\n[Startup & Misc]")
    all_results["blend_weights_init"] = test_bench_blend_weights_init()
    all_results["output_resize"] = test_bench_output_resize()
    all_results["pre_resize_opt"] = test_bench_pre_resize_optimization()

    write_results("bev_stitcher", all_results)
    print("\nDone.")
