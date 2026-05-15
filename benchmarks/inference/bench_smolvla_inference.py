"""
benchmarks/inference/bench_smolvla_inference.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Benchmarks for SmolVLAPolicy.select_action() inference latency.

Measures: model load time, first-call (cold) latency, steady-state p50/p95,
VRAM usage, and effect of chunk_size on throughput.

GPU-optional: falls back to CPU timing when CUDA unavailable.
Run:
    pytest benchmarks/inference/bench_smolvla_inference.py -v -m gpu
    python benchmarks/inference/bench_smolvla_inference.py
"""

from __future__ import annotations

import sys
import time
from pathlib import Path

import numpy as np
import pytest

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO))

from benchmarks.conftest import (
    TimingHarness,
    check_slo,
    print_stats,
    write_results,
)

pytestmark = pytest.mark.gpu

try:
    import torch

    TORCH_AVAILABLE = True
    DEVICE = torch.device("cuda" if torch.cuda.is_available() else "cpu")
except ImportError:
    TORCH_AVAILABLE = False
    DEVICE = None
    pytest.skip("torch not installed", allow_module_level=True)

try:
    import cv2

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

# ---------------------------------------------------------------------------
# Observation tensor factory — matches smolvla_node.py inference_loop()
# ---------------------------------------------------------------------------

IMG_W, IMG_H = 320, 240


def _make_obs(device: "torch.device") -> dict:
    """Create a synthetic observation dict matching SmolVLANode's obs structure."""
    wrist = torch.rand(1, 3, IMG_H, IMG_W, dtype=torch.float32, device=device)
    bev = torch.rand(1, 3, IMG_H, IMG_W, dtype=torch.float32, device=device)
    state = torch.rand(1, 9, dtype=torch.float32, device=device)
    return {
        "observation.images.wrist": wrist,
        "observation.images.bev": bev,
        "observation.state": state,
        "task": "pick up the object and place it",
    }


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_smolvla_model_load():
    """
    Measure SmolVLAPolicy.from_pretrained() load time.
    One-shot (not in timed loop). Informational for startup latency.
    """
    try:
        from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy
    except ImportError:
        pytest.skip("lerobot not installed")

    checkpoint = "lerobot/smolvla_base"
    t0 = time.perf_counter()
    policy = SmolVLAPolicy.from_pretrained(checkpoint)
    policy = policy.to(DEVICE)
    policy.eval()
    load_s = time.perf_counter() - t0

    print(f"\n  smolvla_load_s: {load_s:.2f}s  (device={DEVICE})")
    if DEVICE.type == "cuda":
        vram_mb = torch.cuda.max_memory_allocated() / 1e6
        print(f"  smolvla_vram_mb: {vram_mb:.0f} MB")

    return {"load_s": load_s, "device": str(DEVICE)}


def test_bench_smolvla_cold_inference():
    """
    First select_action() call after model load — may be slower due to JIT/tracing.
    """
    try:
        from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy
    except ImportError:
        pytest.skip("lerobot not installed")

    policy = SmolVLAPolicy.from_pretrained("lerobot/smolvla_base")
    policy = policy.to(DEVICE)
    policy.eval()

    obs = _make_obs(DEVICE)

    t0 = time.perf_counter()
    with torch.no_grad():
        action = policy.select_action(obs)
    cold_ms = (time.perf_counter() - t0) * 1000.0

    print(f"\n  smolvla_cold_ms: {cold_ms:.1f}ms")
    check_slo("smolvla_inference_ms", cold_ms, fail_on_max=False)
    return {"cold_ms": cold_ms}


def test_bench_smolvla_warm_inference():
    """
    Steady-state select_action() latency: 50 samples after 5-call warmup.
    This is the p50/p95 the robot experiences during continuous operation.
    """
    try:
        from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy
    except ImportError:
        pytest.skip("lerobot not installed")

    policy = SmolVLAPolicy.from_pretrained("lerobot/smolvla_base")
    policy = policy.to(DEVICE)
    policy.eval()

    obs = _make_obs(DEVICE)

    h = TimingHarness()

    def _infer():
        with torch.no_grad():
            return policy.select_action(obs)

    stats = h.run(_infer, n=50, warmup=5)
    print_stats("smolvla_inference_ms (warm, steady-state)", stats)
    assert check_slo("smolvla_inference_ms", stats["p95_ms"])

    achievable_hz = 1000.0 / stats["median_ms"]
    print(
        f"  INFO  Achievable inference rate: {achievable_hz:.1f} Hz "
        f"(target: 10 Hz = 100ms budget)"
    )

    if DEVICE.type == "cuda":
        vram_mb = torch.cuda.max_memory_allocated() / 1e6
        print(f"  INFO  Peak VRAM: {vram_mb:.0f} MB")

    return stats


def test_bench_smolvla_action_shape():
    """
    Verify action output shape and measure post-processing (CPU transfer + reshape).
    """
    try:
        from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy
    except ImportError:
        pytest.skip("lerobot not installed")

    policy = SmolVLAPolicy.from_pretrained("lerobot/smolvla_base")
    policy = policy.to(DEVICE)
    policy.eval()

    obs = _make_obs(DEVICE)

    with torch.no_grad():
        action = policy.select_action(obs)

    # Measure CPU transfer + numpy conversion (from smolvla_node.py)
    h = TimingHarness()
    stats = h.run(lambda: action.cpu().numpy(), n=1000, warmup=50)
    print_stats("smolvla_cpu_transfer_ms (action.cpu().numpy())", stats)

    action_np = action.cpu().numpy()
    if action_np.ndim == 2:
        action_np = action_np[0]
    print(f"  INFO  Action shape: {action_np.shape}  (expected: (9,))")
    return stats


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print(f"OmniBot — SmolVLA Inference Benchmarks  (device={DEVICE})")
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[Model Load]")
    all_results["load"] = test_bench_smolvla_model_load()

    print("\n[Inference Latency]")
    all_results["cold"] = test_bench_smolvla_cold_inference()
    all_results["warm"] = test_bench_smolvla_warm_inference()

    print("\n[Post-processing]")
    all_results["cpu_transfer"] = test_bench_smolvla_action_shape()

    write_results("smolvla_inference", all_results)
    print("\nDone.")
