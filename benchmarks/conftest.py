"""
benchmarks/conftest.py
~~~~~~~~~~~~~~~~~~~~~~~
Shared timing primitives, SLO table, JSON reporter, and pytest configuration
for all OmniBot performance benchmarks.

Usage in benchmark files:
    from benchmarks.conftest import TimingHarness, SLO_TABLE, check_slo, write_results
"""

from __future__ import annotations

import json
import os
import platform
import statistics
import sys
import time
from collections import deque
from datetime import datetime, timezone
from pathlib import Path

import pytest

# ---------------------------------------------------------------------------
# SLO Table — (target, max) in milliseconds
# ---------------------------------------------------------------------------
# target = desired operating point
# max    = hard failure threshold (CI will fail if p95 exceeds this)

SLO_TABLE: dict[str, dict[str, float]] = {
    "yahboom_tx_packet_ms": {"target": 1.0, "max": 5.0},
    "yahboom_rx_parse_ms": {"target": 0.5, "max": 2.0},
    "yahboom_rx_parse_multi_ms": {"target": 2.0, "max": 8.0},
    "yahboom_rx_parse_noisy_ms": {"target": 1.0, "max": 4.0},
    "odometry_integrate_ms": {"target": 0.1, "max": 0.5},
    "ik_ms": {"target": 0.05, "max": 0.5},
    "fk_ms": {"target": 0.05, "max": 0.5},
    "full_kinematics_cycle_ms": {"target": 0.2, "max": 1.0},
    "cmd_vel_mux_route_ms": {"target": 0.5, "max": 2.0},
    "bev_stitch_frame_ms": {"target": 33.0, "max": 50.0},
    "warp_perspective_ms": {"target": 8.0, "max": 15.0},
    "bev_blend_ms": {"target": 5.0, "max": 15.0},
    "smolvla_preprocess_ms": {"target": 5.0, "max": 20.0},
    "smolvla_resize_ms": {"target": 1.0, "max": 5.0},
    "smolvla_inference_ms": {"target": 100.0, "max": 200.0},
    "openvla_inference_ms": {"target": 500.0, "max": 2000.0},
    "arm_servo_read_ms": {"target": 5.0, "max": 20.0},
    "arm_servo_write_ms": {"target": 5.0, "max": 20.0},
    "full_control_loop_ms": {"target": 50.0, "max": 100.0},
    "odom_stamp_delta_ms": {"target": 2.0, "max": 10.0},
    "joint_state_stamp_delta_ms": {"target": 2.0, "max": 10.0},
    "bev_image_latency_ms": {"target": 40.0, "max": 80.0},
}

# Platform-specific SLO overrides (looser on Pi 5 for vision ops)
_SLO_OVERRIDES: dict[str, dict[str, dict[str, float]]] = {
    "pi5": {
        "bev_stitch_frame_ms": {"target": 33.0, "max": 80.0},
        "warp_perspective_ms": {"target": 15.0, "max": 30.0},
        "odometry_integrate_ms": {"target": 0.2, "max": 1.0},
    },
    "gpu": {
        "smolvla_inference_ms": {"target": 80.0, "max": 150.0},
        "openvla_inference_ms": {"target": 300.0, "max": 1000.0},
    },
}


def _machine_type() -> str:
    """Detect running machine type from env var or hardware."""
    if os.environ.get("OMNIBOT_MACHINE"):
        return os.environ["OMNIBOT_MACHINE"].lower()
    try:
        import torch

        if torch.cuda.is_available():
            return "gpu"
    except ImportError:
        pass
    if platform.machine() == "aarch64":
        return "pi5"
    return "ci"


_MACHINE = _machine_type()


def get_slo(metric_name: str) -> dict[str, float]:
    """Return SLO dict for metric, with platform overrides applied."""
    base = SLO_TABLE.get(metric_name, {"target": float("inf"), "max": float("inf")})
    override = _SLO_OVERRIDES.get(_MACHINE, {}).get(metric_name, {})
    return {**base, **override}


# ---------------------------------------------------------------------------
# TimingHarness
# ---------------------------------------------------------------------------


class TimingHarness:
    """
    Run a callable N times and collect wall-clock durations in milliseconds.

    Returns a stats dict with mean/median/p95/p99/min/max/stdev.
    Uses time.perf_counter() — monotonic, sub-microsecond resolution.
    """

    def run(
        self,
        fn,
        n: int = 1000,
        warmup: int = 50,
    ) -> dict[str, float | int]:
        for _ in range(warmup):
            fn()
        samples: list[float] = []
        for _ in range(n):
            t0 = time.perf_counter()
            fn()
            samples.append((time.perf_counter() - t0) * 1000.0)

        s = sorted(samples)
        return {
            "n": n,
            "mean_ms": statistics.mean(samples),
            "median_ms": statistics.median(samples),
            "p95_ms": s[max(0, int(0.95 * n) - 1)],
            "p99_ms": s[max(0, int(0.99 * n) - 1)],
            "min_ms": s[0],
            "max_ms": s[-1],
            "stdev_ms": statistics.stdev(samples) if n > 1 else 0.0,
        }

    def run_returning(
        self,
        fn,
        n: int = 100,
        warmup: int = 10,
    ) -> tuple[list[float], list]:
        """Like run() but also collects return values. Useful for throughput tests."""
        for _ in range(warmup):
            fn()
        durations: list[float] = []
        results = []
        for _ in range(n):
            t0 = time.perf_counter()
            r = fn()
            durations.append((time.perf_counter() - t0) * 1000.0)
            results.append(r)
        return durations, results


# ---------------------------------------------------------------------------
# SLO checking and reporting
# ---------------------------------------------------------------------------


def check_slo(metric_name: str, p95_ms: float, *, fail_on_max: bool = True) -> bool:
    """
    Check p95 against SLO table. Prints PASS/WARN/FAIL.
    Returns False if p95 > SLO max (hard failure).
    """
    slo = get_slo(metric_name)
    target = slo["target"]
    max_val = slo["max"]

    if p95_ms <= target:
        print(
            f"  PASS  {metric_name}: p95={p95_ms:.3f}ms "
            f"(target={target}ms  max={max_val}ms)"
        )
        return True
    elif p95_ms <= max_val:
        print(
            f"  WARN  {metric_name}: p95={p95_ms:.3f}ms > target={target}ms "
            f"(max={max_val}ms) [{_MACHINE}]"
        )
        return True
    else:
        status = "FAIL" if fail_on_max else "OVER"
        print(
            f"  {status}  {metric_name}: p95={p95_ms:.3f}ms > max={max_val}ms "
            f"(target={target}ms) [{_MACHINE}]"
        )
        return not fail_on_max


def print_stats(metric_name: str, stats: dict) -> None:
    """Print a formatted stats line for a benchmark result."""
    print(
        f"  {metric_name:40s} "
        f"mean={stats['mean_ms']:7.3f}ms  "
        f"p50={stats['median_ms']:7.3f}ms  "
        f"p95={stats['p95_ms']:7.3f}ms  "
        f"p99={stats['p99_ms']:7.3f}ms  "
        f"min={stats['min_ms']:7.3f}ms  "
        f"max={stats['max_ms']:7.3f}ms  "
        f"(n={stats['n']})"
    )


# ---------------------------------------------------------------------------
# JSON result writer
# ---------------------------------------------------------------------------

_RESULTS_DIR = Path(__file__).parent / "results"


def write_results(name: str, results: dict[str, dict]) -> Path:
    """
    Write benchmark results to benchmarks/results/<name>_<timestamp>.json.
    Creates the results/ directory if needed.
    """
    _RESULTS_DIR.mkdir(exist_ok=True)
    ts = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    out = _RESULTS_DIR / f"{name}_{ts}.json"

    payload = {
        "benchmark": name,
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "platform": platform.platform(),
        "machine": _MACHINE,
        "python_version": sys.version,
        "cpu": platform.processor() or platform.machine(),
        "metrics": results,
    }

    out.write_text(json.dumps(payload, indent=2))
    print(f"\n  Results written → {out}")
    return out


# ---------------------------------------------------------------------------
# Pytest marker registration
# ---------------------------------------------------------------------------


def pytest_configure(config):
    config.addinivalue_line("markers", "pi5: run on Raspberry Pi 5 only")
    config.addinivalue_line("markers", "gpu: requires CUDA GPU")
    config.addinivalue_line("markers", "hardware: requires physical serial device")
    config.addinivalue_line("markers", "ros: requires rclpy and a live DDS context")
    config.addinivalue_line("markers", "slow: long-running benchmark (>30s)")


# ---------------------------------------------------------------------------
# Shared skip helpers (import in benchmark files)
# ---------------------------------------------------------------------------


def skip_if_no_cuda():
    """Skip test/module if CUDA is unavailable."""
    try:
        import torch

        if not torch.cuda.is_available():
            pytest.skip("No CUDA GPU available", allow_module_level=True)
    except ImportError:
        pytest.skip("torch not installed", allow_module_level=True)


def skip_if_no_serial():
    """Skip test/module if OMNIBOT_SERIAL_PORT is not set."""
    port = os.environ.get("OMNIBOT_SERIAL_PORT", "")
    if not port:
        pytest.skip(
            "Set OMNIBOT_SERIAL_PORT env var to run serial benchmarks",
            allow_module_level=True,
        )
    return port


def skip_if_no_ros():
    """Skip test/module if rclpy is not importable."""
    try:
        import rclpy  # noqa: F401
    except ImportError:
        pytest.skip("rclpy not installed — ROS 2 required", allow_module_level=True)


# ---------------------------------------------------------------------------
# Rolling stats helper (used by production node instrumentation)
# ---------------------------------------------------------------------------


class RollingStats:
    """
    Lightweight rolling statistics accumulator backed by a deque.
    Designed to be embedded in ROS 2 nodes with zero overhead when disabled.
    """

    def __init__(self, maxlen: int = 100):
        self._samples: deque[float] = deque(maxlen=maxlen)

    def add(self, value_ms: float) -> None:
        self._samples.append(value_ms)

    def stats(self) -> dict[str, float]:
        if not self._samples:
            return {"n": 0, "mean_ms": 0.0, "p50_ms": 0.0, "p95_ms": 0.0}
        s = sorted(self._samples)
        n = len(s)
        return {
            "n": n,
            "mean_ms": statistics.mean(s),
            "p50_ms": s[n // 2],
            "p95_ms": s[max(0, int(0.95 * n) - 1)],
            "max_ms": s[-1],
        }
