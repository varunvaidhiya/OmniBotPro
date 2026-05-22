"""benchmarks/inference/bench_policy_inference.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Model-agnostic policy inference latency benchmarks.

Measures: load time, cold latency, steady-state p50/p95/max, VRAM, and
throughput for any registered policy adapter.

Run against a single model:
    pytest benchmarks/inference/bench_policy_inference.py -v -m gpu
    python benchmarks/inference/bench_policy_inference.py --model smolvla

Compare all registered models:
    python benchmarks/inference/bench_policy_inference.py --all
"""

from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

import numpy as np
import pytest

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO))

from benchmarks.conftest import TimingHarness, check_slo, print_stats, write_results

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
    from lerobot_engine.models import list_models, make_policy

    REGISTRY_AVAILABLE = True
except ImportError:
    REGISTRY_AVAILABLE = False
    pytest.skip("lerobot_engine not on PYTHONPATH", allow_module_level=True)

# ---------------------------------------------------------------------------
# Default pretrained checkpoints for each registered model
# ---------------------------------------------------------------------------
_CHECKPOINTS = {
    "smolvla": "lerobot/smolvla_base",
    "act": "lerobot/act_base",
    "diffusion": "lerobot/diffusion_pusht",
    "openvla": "openvla/openvla-7b",
}


# ---------------------------------------------------------------------------
# Observation factory — reads schema from the adapter itself
# ---------------------------------------------------------------------------


def make_obs(adapter, device: "torch.device") -> dict:
    """Build a synthetic observation dict matching the adapter's expected schema."""
    w, h = adapter.image_size
    obs = {}
    for key in adapter.image_keys:
        obs[key] = torch.rand(1, 3, h, w, dtype=torch.float32, device=device)
    obs[adapter.state_key] = torch.rand(
        1, adapter.action_dim, dtype=torch.float32, device=device
    )
    if adapter.task_key:
        obs[adapter.task_key] = "pick up the object and place it"
    return obs


# ---------------------------------------------------------------------------
# Benchmark helpers
# ---------------------------------------------------------------------------


def _load_adapter(model_type: str):
    checkpoint = _CHECKPOINTS.get(model_type)
    if not checkpoint:
        pytest.skip(f"No default checkpoint for model '{model_type}'")
    try:
        return make_policy(model_type, checkpoint=checkpoint, device=str(DEVICE))
    except ImportError as exc:
        pytest.skip(str(exc))


def _bench_model(model_type: str) -> dict:
    """Run the full benchmark suite for one model type. Returns stats dict."""
    results = {}

    # ── Load time ──────────────────────────────────────────────────────────
    t0 = time.perf_counter()
    adapter = _load_adapter(model_type)
    load_s = time.perf_counter() - t0
    print(f"\n  [{model_type}] load_s: {load_s:.2f}s  device={DEVICE}")
    if DEVICE and DEVICE.type == "cuda":
        vram_mb = torch.cuda.max_memory_allocated() / 1e6
        print(f"  [{model_type}] vram_after_load_mb: {vram_mb:.0f}")
    results["load_s"] = load_s

    obs = make_obs(adapter, DEVICE)
    adapter.reset()

    # ── Cold inference ─────────────────────────────────────────────────────
    t0 = time.perf_counter()
    with torch.no_grad():
        adapter.select_action(obs)
    cold_ms = (time.perf_counter() - t0) * 1000.0
    print(f"  [{model_type}] cold_ms: {cold_ms:.1f}")
    results["cold_ms"] = cold_ms

    # ── Warm steady-state ──────────────────────────────────────────────────
    h = TimingHarness()

    def _infer():
        with torch.no_grad():
            return adapter.select_action(obs)

    stats = h.run(_infer, n=50, warmup=5)
    print_stats(f"{model_type}_inference_ms (warm, steady-state)", stats)
    check_slo("policy_inference_ms", stats["p95_ms"])
    results["warm"] = stats

    achievable_hz = 1000.0 / stats["median_ms"]
    print(f"  [{model_type}] achievable_hz: {achievable_hz:.1f}")

    if DEVICE and DEVICE.type == "cuda":
        vram_mb = torch.cuda.max_memory_allocated() / 1e6
        print(f"  [{model_type}] peak_vram_mb: {vram_mb:.0f}")
        results["peak_vram_mb"] = vram_mb

    # ── Action shape ──────────────────────────────────────────────────────
    with torch.no_grad():
        action = adapter.select_action(obs)
    assert isinstance(action, np.ndarray), "select_action must return np.ndarray"
    assert action.shape == (adapter.action_dim,), (
        f"Expected ({adapter.action_dim},), got {action.shape}"
    )
    print(
        f"  [{model_type}] action_shape: {action.shape}  (expected ({adapter.action_dim},))"
    )

    return results


# ---------------------------------------------------------------------------
# Pytest test functions (one per model via parametrize)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("model_type", list_models() if REGISTRY_AVAILABLE else [])
def test_bench_policy_load(model_type):
    adapter = _load_adapter(model_type)
    assert adapter is not None


@pytest.mark.parametrize("model_type", list_models() if REGISTRY_AVAILABLE else [])
def test_bench_policy_warm_inference(model_type):
    adapter = _load_adapter(model_type)
    obs = make_obs(adapter, DEVICE)
    adapter.reset()

    h = TimingHarness()

    def _infer():
        with torch.no_grad():
            return adapter.select_action(obs)

    stats = h.run(_infer, n=50, warmup=5)
    print_stats(f"{model_type}_inference_ms", stats)
    assert check_slo("policy_inference_ms", stats["p95_ms"])


@pytest.mark.parametrize("model_type", list_models() if REGISTRY_AVAILABLE else [])
def test_bench_policy_action_shape(model_type):
    adapter = _load_adapter(model_type)
    obs = make_obs(adapter, DEVICE)
    with torch.no_grad():
        action = adapter.select_action(obs)
    assert isinstance(action, np.ndarray)
    assert action.shape == (adapter.action_dim,)


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------


def _parse():
    p = argparse.ArgumentParser()
    p.add_argument("--model", default="smolvla", help="Model type to benchmark.")
    p.add_argument(
        "--all", action="store_true", help="Benchmark all registered models."
    )
    return p.parse_args()


if __name__ == "__main__":
    args = _parse()
    models = list_models() if args.all else [args.model]

    print("\n" + "=" * 70)
    print(f"OmniBot — Policy Inference Benchmarks  (device={DEVICE})")
    print("=" * 70)

    all_results = {}
    for m in models:
        print(f"\n{'─' * 40}")
        print(f" Model: {m}")
        print(f"{'─' * 40}")
        try:
            all_results[m] = _bench_model(m)
        except Exception as exc:
            print(f"  [SKIP] {m}: {exc}")

    if all_results:
        write_results("policy_inference", all_results)

    print("\nDone.")
