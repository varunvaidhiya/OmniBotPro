"""
benchmarks/serial/bench_yahboom_protocol.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Benchmarks for Yahboom serial protocol encode/decode and odometry integration.

No hardware, no ROS — pure Python.
Run:
    pytest benchmarks/serial/bench_yahboom_protocol.py -v
    # or directly:
    python benchmarks/serial/bench_yahboom_protocol.py
"""

from __future__ import annotations

import struct
import sys
from pathlib import Path

# Make packages importable without installation
_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO / "packages" / "yahboom_ros2"))
sys.path.insert(0, str(_REPO / "packages" / "mecanum_drive_ros2"))
sys.path.insert(0, str(_REPO))

from yahboom_ros2.protocol import (
    FUNC_BEEP,
    FUNC_MOTION,
    FUNC_MOTOR,
    TYPE_ACCEL,
    TYPE_ATTITUDE,
    TYPE_GYRO,
    TYPE_VELOCITY,
    build_packet,
    packet_motion,
    packet_motor,
    parse_rx_buffer,
)
from mecanum_drive_ros2.kinematics import RobotGeometry, integrate_pose
from benchmarks.conftest import TimingHarness, check_slo, print_stats, write_results

import pytest

# ---------------------------------------------------------------------------
# Test data setup
# ---------------------------------------------------------------------------

_GEOM = RobotGeometry(
    wheel_radius=0.04,
    wheel_separation_width=0.215,
    wheel_separation_length=0.165,
)

# Pre-built payloads so the benchmark measures only packet assembly
_MOTION_PAYLOAD = struct.pack("<bhhh", 1, 200, 0, 500)  # vx=0.2 m/s, vz=0.5 rad/s
_MOTOR_PAYLOAD = struct.pack("<hhhh", 100, 100, 100, 100)


def _build_rx_packet(pkt_type: int, payload: bytes) -> bytes:
    """Build a valid Yahboom RX packet (copied from test conftest to avoid ROS dep)."""
    length = 3 + len(payload)
    header = bytes([0xFF, 0xFB, length, pkt_type])
    body = header[2:] + payload
    checksum = sum(body) & 0xFF
    return header + payload + bytes([checksum])


# Single velocity packet
_VEL_PAYLOAD = struct.pack("<hhh", 200, 0, 500)
_BUF_SINGLE = _build_rx_packet(TYPE_VELOCITY, _VEL_PAYLOAD)

# Multi-packet buffer: 10 velocity packets back-to-back
_BUF_MULTI = _BUF_SINGLE * 10

# Noisy buffer: 20 junk bytes then 1 velocity packet
_BUF_NOISY = bytes([0xAA, 0xBB] * 10) + _BUF_SINGLE

# Mixed-type buffer: velocity + accel + gyro + attitude
_ACCEL_BUF = _build_rx_packet(TYPE_ACCEL, struct.pack("<hhh", 10, 20, 1000))
_GYRO_BUF = _build_rx_packet(TYPE_GYRO, struct.pack("<hhh", 5, 3, 10))
_ATT_BUF = _build_rx_packet(TYPE_ATTITUDE, struct.pack("<hhh", 100, -50, 3600))
_BUF_MIXED = _BUF_SINGLE + _ACCEL_BUF + _GYRO_BUF + _ATT_BUF


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_build_packet_motion():
    """build_packet with FUNC_MOTION 7-byte payload."""
    h = TimingHarness()
    stats = h.run(lambda: build_packet(FUNC_MOTION, _MOTION_PAYLOAD), n=10000, warmup=200)
    print_stats("yahboom_tx_packet_ms (build_packet MOTION)", stats)
    assert check_slo("yahboom_tx_packet_ms", stats["p95_ms"])
    return stats


def test_bench_packet_motion_helper():
    """packet_motion() convenience function — includes struct.pack."""
    h = TimingHarness()
    stats = h.run(lambda: packet_motion(0.2, 0.0, 0.5), n=10000, warmup=200)
    print_stats("yahboom_tx_packet_ms (packet_motion)", stats)
    assert check_slo("yahboom_tx_packet_ms", stats["p95_ms"])
    return stats


def test_bench_packet_motor():
    """packet_motor() — direct PWM 4×int16 payload."""
    h = TimingHarness()
    stats = h.run(lambda: packet_motor(100, 100, 100, 100), n=10000, warmup=200)
    print_stats("yahboom_tx_packet_ms (packet_motor)", stats)
    assert check_slo("yahboom_tx_packet_ms", stats["p95_ms"])
    return stats


def test_bench_parse_rx_single():
    """parse_rx_buffer() — single velocity packet (11 bytes)."""
    h = TimingHarness()
    stats = h.run(lambda: parse_rx_buffer(_BUF_SINGLE), n=10000, warmup=200)
    print_stats("yahboom_rx_parse_ms (single packet)", stats)
    assert check_slo("yahboom_rx_parse_ms", stats["p95_ms"])
    return stats


def test_bench_parse_rx_multi():
    """parse_rx_buffer() — 10 velocity packets back-to-back (110 bytes)."""
    h = TimingHarness()
    stats = h.run(lambda: parse_rx_buffer(_BUF_MULTI), n=5000, warmup=100)
    print_stats("yahboom_rx_parse_multi_ms (10 packets)", stats)
    assert check_slo("yahboom_rx_parse_multi_ms", stats["p95_ms"])
    return stats


def test_bench_parse_rx_noisy():
    """parse_rx_buffer() — 20 junk bytes + 1 packet (linear scan cost)."""
    h = TimingHarness()
    stats = h.run(lambda: parse_rx_buffer(_BUF_NOISY), n=10000, warmup=200)
    print_stats("yahboom_rx_parse_noisy_ms (noisy buffer)", stats)
    assert check_slo("yahboom_rx_parse_noisy_ms", stats["p95_ms"])
    return stats


def test_bench_parse_rx_mixed():
    """parse_rx_buffer() — velocity + accel + gyro + attitude (4 packet types)."""
    h = TimingHarness()
    stats = h.run(lambda: parse_rx_buffer(_BUF_MIXED), n=5000, warmup=100)
    print_stats("yahboom_rx_parse_mixed_ms (4-type buffer)", stats)
    # Use the multi-packet SLO as a proxy
    check_slo("yahboom_rx_parse_multi_ms", stats["p95_ms"], fail_on_max=False)
    return stats


def test_bench_odometry_integrate():
    """integrate_pose() — the hot-path trig call in odometry at 20–100 Hz."""
    h = TimingHarness()
    x, y, theta = 0.0, 0.0, 0.785
    vx, vy, omega, dt = 0.2, 0.05, 0.1, 0.05
    stats = h.run(
        lambda: integrate_pose(x, y, theta, vx, vy, omega, dt),
        n=100000,
        warmup=1000,
    )
    print_stats("odometry_integrate_ms (integrate_pose)", stats)
    assert check_slo("odometry_integrate_ms", stats["p95_ms"])
    return stats


def test_bench_sleep_overhead():
    """
    Measure actual duration of time.sleep(0.002) — the forced delay in
    send_packet() at yahboom_controller_node.py:166.

    On Linux with the default POSIX timer (1ms resolution), this sleep
    typically overshoots by 1–3ms on a loaded system.
    On Pi 5, kernel timer resolution may cause 3–5ms actual duration.
    """
    import time

    h = TimingHarness()
    stats = h.run(lambda: time.sleep(0.002), n=500, warmup=10)
    print_stats("sleep_2ms_actual_ms (time.sleep(0.002))", stats)

    # Informational only — not gated against an SLO since this is OS-dependent
    overhead = stats["mean_ms"] - 2.0
    print(
        f"  INFO  sleep(0.002) mean overshoot: +{overhead:.3f}ms "
        f"(p95={stats['p95_ms']:.3f}ms, max={stats['max_ms']:.3f}ms)"
    )
    print(
        f"  INFO  At 20 Hz, total sleep overhead per second: "
        f"~{stats['mean_ms'] * 20:.1f}ms/s"
    )
    return stats


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — Yahboom Serial Protocol Benchmarks")
    print("=" * 70)

    all_results: dict[str, dict] = {}

    print("\n[TX — Packet Building]")
    all_results["yahboom_tx_build_motion"] = test_bench_build_packet_motion()
    all_results["yahboom_tx_packet_motion"] = test_bench_packet_motion_helper()
    all_results["yahboom_tx_packet_motor"] = test_bench_packet_motor()

    print("\n[RX — Buffer Parsing]")
    all_results["yahboom_rx_single"] = test_bench_parse_rx_single()
    all_results["yahboom_rx_multi"] = test_bench_parse_rx_multi()
    all_results["yahboom_rx_noisy"] = test_bench_parse_rx_noisy()
    all_results["yahboom_rx_mixed"] = test_bench_parse_rx_mixed()

    print("\n[Odometry Integration]")
    all_results["odometry_integrate"] = test_bench_odometry_integrate()

    print("\n[OS Sleep Overhead — time.sleep(0.002)]")
    all_results["sleep_2ms"] = test_bench_sleep_overhead()

    write_results("yahboom_protocol", all_results)
    print("\nDone.")
