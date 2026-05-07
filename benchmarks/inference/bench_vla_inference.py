"""
benchmarks/inference/bench_vla_inference.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Benchmarks for OpenVLA 7B inference via the vla_serve FastAPI server.

Measures: model load time, cold/warm inference latency, 4-bit quantization
speedup, max_new_tokens impact, base64 encoding overhead.

The vla_serve server must be started separately:
    VLA_AUTO_LOAD=1 uvicorn vla_serve.inference.server:app --port 8000

GPU required. Uses @pytest.mark.gpu and @pytest.mark.slow.
Run:
    pytest benchmarks/inference/bench_vla_inference.py -v -m "gpu and slow"
    python benchmarks/inference/bench_vla_inference.py
"""

from __future__ import annotations

import base64
import io
import sys
import time
from pathlib import Path

import numpy as np
import pytest

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO / "packages" / "vla_serve"))
sys.path.insert(0, str(_REPO))

pytestmark = [pytest.mark.gpu, pytest.mark.slow]

from benchmarks.conftest import (
    TimingHarness,
    check_slo,
    print_stats,
    skip_if_no_cuda,
    write_results,
)

skip_if_no_cuda()

try:
    import httpx

    HTTPX_AVAILABLE = True
except ImportError:
    HTTPX_AVAILABLE = False

try:
    from PIL import Image as PILImage

    PIL_AVAILABLE = True
except ImportError:
    PIL_AVAILABLE = False

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

_SERVER_URL = "http://localhost:8000"
_INSTRUCTION = "move forward to the red cube"


def _make_test_image_b64(width: int = 640, height: int = 480) -> str:
    """Create a synthetic RGB image encoded as base64 JPEG."""
    if PIL_AVAILABLE:
        arr = np.random.randint(0, 256, (height, width, 3), dtype=np.uint8)
        img = PILImage.fromarray(arr, "RGB")
        buf = io.BytesIO()
        img.save(buf, format="JPEG", quality=85)
        return base64.b64encode(buf.getvalue()).decode("utf-8")
    else:
        # Fallback: raw random bytes (not a valid JPEG but tests encoding cost)
        raw = np.random.randint(0, 256, (height, width, 3), dtype=np.uint8).tobytes()
        return base64.b64encode(raw).decode("utf-8")


def _server_available() -> bool:
    if not HTTPX_AVAILABLE:
        return False
    try:
        r = httpx.get(f"{_SERVER_URL}/health", timeout=2.0)
        return r.status_code == 200
    except Exception:
        return False


# ---------------------------------------------------------------------------
# Base64 encoding benchmarks (no server needed)
# ---------------------------------------------------------------------------


def test_bench_base64_encode():
    """
    Measure base64 encoding cost for a 640×480 JPEG frame.
    This overhead applies to every inference request sent over HTTP.
    """
    if not PIL_AVAILABLE:
        pytest.skip("PIL not installed")

    h = TimingHarness()
    arr = np.random.randint(0, 256, (480, 640, 3), dtype=np.uint8)
    img = PILImage.fromarray(arr)

    def _encode():
        buf = io.BytesIO()
        img.save(buf, format="JPEG", quality=85)
        return base64.b64encode(buf.getvalue()).decode("utf-8")

    stats = h.run(_encode, n=200, warmup=10)
    print_stats("base64_encode_ms (640×480 JPEG)", stats)
    return stats


def test_bench_base64_decode():
    """Measure base64 decode + PIL open cost (server-side per request)."""
    if not PIL_AVAILABLE:
        pytest.skip("PIL not installed")

    b64 = _make_test_image_b64()

    h = TimingHarness()

    def _decode():
        raw = base64.b64decode(b64)
        return PILImage.open(io.BytesIO(raw))

    stats = h.run(_decode, n=200, warmup=10)
    print_stats("base64_decode_ms (server-side decode)", stats)
    return stats


# ---------------------------------------------------------------------------
# Server benchmarks (require running vla_serve)
# ---------------------------------------------------------------------------


def test_bench_openvla_health():
    """Verify server is up. Skip all server tests if not running."""
    if not _server_available():
        pytest.skip(
            f"vla_serve not running at {_SERVER_URL}. "
            "Start with: VLA_AUTO_LOAD=1 uvicorn vla_serve.inference.server:app"
        )
    if not HTTPX_AVAILABLE:
        pytest.skip("httpx not installed: pip install httpx")


def test_bench_openvla_load_model():
    """
    POST /load_model — measure model loading time.
    One-shot. Informational for deployment startup planning.
    """
    if not _server_available():
        pytest.skip("vla_serve not running")

    t0 = time.perf_counter()
    r = httpx.post(
        f"{_SERVER_URL}/load_model",
        json={"model_path": "openvla/openvla-7b", "load_in_4bit": False},
        timeout=300.0,
    )
    load_s = time.perf_counter() - t0

    assert r.status_code == 200, f"Load failed: {r.text}"
    print(f"\n  openvla_load_s: {load_s:.1f}s")
    return {"load_s": load_s}


def test_bench_openvla_cold_inference():
    """
    First /predict call after model load — may be slower due to GPU warmup.
    """
    if not _server_available():
        pytest.skip("vla_serve not running")

    b64 = _make_test_image_b64()
    t0 = time.perf_counter()
    r = httpx.post(
        f"{_SERVER_URL}/predict",
        json={"instruction": _INSTRUCTION, "image_base64": b64},
        timeout=60.0,
    )
    client_ms = (time.perf_counter() - t0) * 1000.0

    assert r.status_code == 200, f"Predict failed: {r.text}"
    data = r.json()
    server_latency_ms = data.get("latency_ms", 0.0)
    network_overhead_ms = client_ms - server_latency_ms

    print(
        f"\n  openvla_cold_client_ms: {client_ms:.0f}ms  "
        f"server_latency_ms: {server_latency_ms:.0f}ms  "
        f"network_overhead_ms: {network_overhead_ms:.1f}ms"
    )
    return {
        "cold_client_ms": client_ms,
        "cold_server_ms": server_latency_ms,
        "network_overhead_ms": network_overhead_ms,
    }


def test_bench_openvla_warm_inference():
    """
    Steady-state /predict latency: 20 samples after 3-call warmup.
    Separates client-side (HTTP) from server-side (pure inference) time.
    """
    if not _server_available():
        pytest.skip("vla_serve not running")

    b64 = _make_test_image_b64()

    client_times: list[float] = []
    server_times: list[float] = []

    # Warmup
    for _ in range(3):
        httpx.post(
            f"{_SERVER_URL}/predict",
            json={"instruction": _INSTRUCTION, "image_base64": b64},
            timeout=60.0,
        )

    # Measure
    for _ in range(20):
        t0 = time.perf_counter()
        r = httpx.post(
            f"{_SERVER_URL}/predict",
            json={"instruction": _INSTRUCTION, "image_base64": b64},
            timeout=60.0,
        )
        client_times.append((time.perf_counter() - t0) * 1000.0)
        if r.status_code == 200:
            server_times.append(r.json().get("latency_ms", 0.0))

    if not server_times:
        pytest.skip("No valid server responses")

    s_server = sorted(server_times)
    s_client = sorted(client_times)
    n = len(s_server)
    p95_server = s_server[max(0, int(0.95 * n) - 1)]
    p95_client = s_client[max(0, int(0.95 * n) - 1)]

    print(
        f"\n  OpenVLA warm inference (n={n}):\n"
        f"    Server: mean={sum(server_times)/n:.0f}ms  "
        f"p50={s_server[n//2]:.0f}ms  p95={p95_server:.0f}ms\n"
        f"    Client: mean={sum(client_times)/n:.0f}ms  "
        f"p50={s_client[n//2]:.0f}ms  p95={p95_client:.0f}ms\n"
        f"    Network overhead: ~{p95_client - p95_server:.1f}ms"
    )

    check_slo("openvla_inference_ms", p95_server, fail_on_max=False)
    return {
        "server": {"n": n, "mean_ms": sum(server_times)/n,
                   "p95_ms": p95_server, "median_ms": s_server[n//2]},
        "client": {"n": n, "mean_ms": sum(client_times)/n,
                   "p95_ms": p95_client, "median_ms": s_client[n//2]},
    }


def test_bench_openvla_max_tokens_comparison():
    """
    Compare inference latency for max_new_tokens=128 (current) vs =7 (minimal).

    OpenVLA produces a 7-DOF action vector. Using 128 tokens is wasteful —
    autoregressive generation runs the transformer 128 times vs 7.
    This benchmark quantifies the savings from reducing max_new_tokens.
    """
    if not _server_available():
        pytest.skip("vla_serve not running")

    b64 = _make_test_image_b64()

    results = {}
    for n_tokens in [128, 50, 7]:
        times = []
        for _ in range(5):
            r = httpx.post(
                f"{_SERVER_URL}/predict",
                json={
                    "instruction": _INSTRUCTION,
                    "image_base64": b64,
                    "config": {"max_new_tokens": n_tokens},
                },
                timeout=120.0,
            )
            if r.status_code == 200:
                times.append(r.json().get("latency_ms", 0.0))
        if times:
            mean_ms = sum(times) / len(times)
            results[f"max_tokens_{n_tokens}"] = mean_ms
            print(f"  max_new_tokens={n_tokens:3d}: mean={mean_ms:.0f}ms")

    if "max_tokens_128" in results and "max_tokens_7" in results:
        speedup = results["max_tokens_128"] / results["max_tokens_7"]
        print(f"  INFO  Speedup from 128→7 tokens: {speedup:.1f}×")

    return results


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — OpenVLA Inference Benchmarks (via vla_serve)")
    print(f"Server URL: {_SERVER_URL}")
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[Image Encoding (no server required)]")
    all_results["base64_encode"] = test_bench_base64_encode()
    all_results["base64_decode"] = test_bench_base64_decode()

    if _server_available():
        print("\n[Model Load]")
        all_results["model_load"] = test_bench_openvla_load_model()

        print("\n[Inference Latency]")
        all_results["cold"] = test_bench_openvla_cold_inference()
        all_results["warm"] = test_bench_openvla_warm_inference()

        print("\n[max_new_tokens Comparison]")
        all_results["tokens_comparison"] = test_bench_openvla_max_tokens_comparison()
    else:
        print(f"\n  SKIP  Server not available at {_SERVER_URL}")
        print("  Start: VLA_AUTO_LOAD=1 uvicorn vla_serve.inference.server:app --port 8000")

    write_results("openvla_inference", all_results)
    print("\nDone.")
