"""
benchmarks/kinematics/bench_mecanum_kinematics.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Benchmarks for mecanum wheel kinematics (IK, FK, pose integration).

No hardware, no ROS — pure Python + optional numpy.
Run:
    pytest benchmarks/kinematics/bench_mecanum_kinematics.py -v
    python benchmarks/kinematics/bench_mecanum_kinematics.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO / "packages" / "mecanum_drive_ros2"))
sys.path.insert(0, str(_REPO))

from mecanum_drive_ros2.kinematics import (
    RobotGeometry,
    forward_kinematics,
    integrate_pose,
    inverse_kinematics,
)
from benchmarks.conftest import TimingHarness, check_slo, print_stats, write_results


# ---------------------------------------------------------------------------
# Robot geometry — matches production hardware constants
# ---------------------------------------------------------------------------

GEOM = RobotGeometry(
    wheel_radius=0.04,
    wheel_separation_width=0.215,
    wheel_separation_length=0.165,
)

# Fixed inputs to avoid branch mispredictions skewing results
_VX, _VY, _OMEGA = 0.2, 0.05, 0.3
_WHEELS = inverse_kinematics(_VX, _VY, _OMEGA, GEOM)  # pre-computed for FK input
_X, _Y, _THETA, _DT = 1.5, -0.3, 0.785, 0.05


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_inverse_kinematics():
    """inverse_kinematics() — body twist → 4 wheel ω (used at 20–100 Hz)."""
    h = TimingHarness()
    stats = h.run(
        lambda: inverse_kinematics(_VX, _VY, _OMEGA, GEOM),
        n=100000,
        warmup=1000,
    )
    print_stats("ik_ms (inverse_kinematics)", stats)
    assert check_slo("ik_ms", stats["p95_ms"])
    return stats


def test_bench_forward_kinematics():
    """forward_kinematics() — 4 wheel ω → body twist."""
    h = TimingHarness()
    stats = h.run(
        lambda: forward_kinematics(_WHEELS, GEOM),
        n=100000,
        warmup=1000,
    )
    print_stats("fk_ms (forward_kinematics)", stats)
    assert check_slo("fk_ms", stats["p95_ms"])
    return stats


def test_bench_integrate_pose():
    """integrate_pose() — 2× trig per call, critical at 100 Hz odometry."""
    h = TimingHarness()
    stats = h.run(
        lambda: integrate_pose(_X, _Y, _THETA, _VX, _VY, _OMEGA, _DT),
        n=100000,
        warmup=1000,
    )
    print_stats("odometry_integrate_ms (integrate_pose)", stats)
    assert check_slo("odometry_integrate_ms", stats["p95_ms"])
    return stats


def test_bench_full_kinematics_cycle():
    """Full IK → FK → integrate_pose cycle (as yahboom_controller_node runs it)."""
    h = TimingHarness()

    def _cycle():
        w = inverse_kinematics(_VX, _VY, _OMEGA, GEOM)
        forward_kinematics(w, GEOM)
        integrate_pose(_X, _Y, _THETA, _VX, _VY, _OMEGA, _DT)

    stats = h.run(_cycle, n=50000, warmup=500)
    print_stats("full_kinematics_cycle_ms", stats)
    assert check_slo("full_kinematics_cycle_ms", stats["p95_ms"])
    return stats


def test_bench_integrate_pose_numpy_batch():
    """
    Numpy vectorized batch integrate_pose — benchmark potential speedup over
    the per-call Python version when processing a burst of odometry packets.

    This is informational: shows how many poses/sec are achievable with numpy
    versus the per-call path.
    """
    import numpy as np

    N = 1000  # number of poses to integrate in one batch

    xs = np.full(N, _X, dtype=np.float64)
    ys = np.full(N, _Y, dtype=np.float64)
    thetas = np.linspace(0, math.pi, N, dtype=np.float64)
    vxs = np.full(N, _VX, dtype=np.float64)
    vys = np.full(N, _VY, dtype=np.float64)
    omegas = np.full(N, _OMEGA, dtype=np.float64)
    dts = np.full(N, _DT, dtype=np.float64)

    def _numpy_batch():
        cos_th = np.cos(thetas)
        sin_th = np.sin(thetas)
        new_x = xs + (vxs * cos_th - vys * sin_th) * dts
        new_y = ys + (vxs * sin_th + vys * cos_th) * dts
        new_th = thetas + omegas * dts
        # atan2 wrap
        np.arctan2(np.sin(new_th), np.cos(new_th))
        return new_x, new_y, new_th

    h = TimingHarness()
    stats = h.run(_numpy_batch, n=5000, warmup=100)

    per_pose_ms = stats["mean_ms"] / N
    python_per_pose = None

    # Compare against Python per-call
    python_stats = h.run(
        lambda: integrate_pose(_X, _Y, _THETA, _VX, _VY, _OMEGA, _DT),
        n=5000,
        warmup=100,
    )
    python_per_pose = python_stats["mean_ms"]

    speedup = python_per_pose / per_pose_ms if per_pose_ms > 0 else 0

    print_stats(f"integrate_pose_numpy_batch_ms ({N} poses)", stats)
    print(
        f"  INFO  Numpy per-pose: {per_pose_ms:.4f}ms  "
        f"Python per-pose: {python_per_pose:.4f}ms  "
        f"Speedup: {speedup:.1f}×"
    )
    return stats


def test_bench_atan2_vs_fmod():
    """
    Compare theta-wrapping strategies:
      A) math.atan2(sin(theta), cos(theta)) — current approach in integrate_pose
      B) Simple modulo arithmetic — theta % (2*pi)  (no wrapping to [-pi, pi])

    Informational: quantifies cost of the extra atan2 normalize step.
    """
    h = TimingHarness()

    theta = 0.785

    def _with_atan2():
        t = theta + 0.01
        return math.atan2(math.sin(t), math.cos(t))

    def _with_fmod():
        t = theta + 0.01
        # simpler but doesn't give [-pi, pi] — just for comparison
        return t % (2 * math.pi)

    stats_atan2 = h.run(_with_atan2, n=100000, warmup=1000)
    stats_fmod = h.run(_with_fmod, n=100000, warmup=1000)

    print_stats("theta_wrap_atan2_ms", stats_atan2)
    print_stats("theta_wrap_fmod_ms (no [-pi,pi])", stats_fmod)
    overhead = stats_atan2["mean_ms"] - stats_fmod["mean_ms"]
    print(f"  INFO  atan2 overhead over fmod: {overhead:.4f}ms/call")
    return stats_atan2


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — Mecanum Kinematics Benchmarks")
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[Core Kinematics]")
    all_results["inverse_kinematics"] = test_bench_inverse_kinematics()
    all_results["forward_kinematics"] = test_bench_forward_kinematics()
    all_results["integrate_pose"] = test_bench_integrate_pose()
    all_results["full_kinematics_cycle"] = test_bench_full_kinematics_cycle()

    print("\n[Numpy Batch Comparison]")
    all_results["integrate_pose_numpy_batch"] = test_bench_integrate_pose_numpy_batch()

    print("\n[Theta Wrapping Cost]")
    all_results["theta_wrap"] = test_bench_atan2_vs_fmod()

    write_results("mecanum_kinematics", all_results)
    print("\nDone.")
