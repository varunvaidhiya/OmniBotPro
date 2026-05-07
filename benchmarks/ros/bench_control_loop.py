"""
benchmarks/ros/bench_control_loop.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Measures the full 20 Hz update_callback() latency in yahboom_controller_node.py
using a mock serial port — no hardware required.

Isolates pure Python overhead (odometry integration, packet assembly, ROS message
construction) from actual serial I/O latency.

Requires ROS 2 Jazzy + rclpy.
Run:
    pytest benchmarks/ros/bench_control_loop.py -v -m ros
    python benchmarks/ros/bench_control_loop.py
"""

from __future__ import annotations

import struct
import sys
import threading
import time
from collections import deque
from pathlib import Path
from unittest.mock import MagicMock, patch

import pytest

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO))

from benchmarks.conftest import TimingHarness, check_slo, print_stats, skip_if_no_ros, write_results

pytestmark = pytest.mark.ros
skip_if_no_ros()

import rclpy
from rclpy.node import Node


# ---------------------------------------------------------------------------
# Packet builder (copied from test conftest to avoid ROS sys.path dep)
# ---------------------------------------------------------------------------


def _build_rx_packet(pkt_type: int, payload: bytes) -> bytes:
    length = 3 + len(payload)
    header = bytes([0xFF, 0xFB, length, pkt_type])
    body = header[2:] + payload
    checksum = sum(body) & 0xFF
    return header + payload + bytes([checksum])


def _build_velocity_packet(vx_mms: int, vy_mms: int, vz_mrads: int) -> bytes:
    payload = struct.pack("<hhh", vx_mms, vy_mms, vz_mrads)
    return _build_rx_packet(0x0C, payload)


def _build_accel_packet() -> bytes:
    payload = struct.pack("<hhh", 10, 20, 1000)
    return _build_rx_packet(0x61, payload)


def _build_gyro_packet() -> bytes:
    payload = struct.pack("<hhh", 5, 3, 10)
    return _build_rx_packet(0x62, payload)


def _build_attitude_packet() -> bytes:
    payload = struct.pack("<hhh", 100, -50, 3600)
    return _build_rx_packet(0x63, payload)


# Realistic RX buffer: velocity + accel + gyro + attitude (as Yahboom board sends)
_REALISTIC_RX = (
    _build_velocity_packet(200, 0, 500)
    + _build_accel_packet()
    + _build_gyro_packet()
    + _build_attitude_packet()
)


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@pytest.fixture(scope="module", autouse=True)
def ros_context():
    rclpy.init()
    yield
    rclpy.shutdown()


def _make_mock_serial(rx_data: bytes = _REALISTIC_RX):
    """Create a mock serial.Serial that returns realistic Yahboom RX data."""
    ser = MagicMock()
    ser.is_open = True
    ser.in_waiting = len(rx_data)
    ser.read.return_value = rx_data
    ser.write.return_value = None
    return ser


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_update_callback_mock_serial():
    """
    Benchmark update_callback() with mock serial — measures pure Python overhead.
    Serial I/O is instantaneous (mock); the measured time is odometry + publish overhead.

    This isolates: read_yahboom_odometry() + publish_odometry() + publish_imu()
    + send_motion_command() (minus the 2ms sleep — see bench_serial_io.py for that).
    """
    from geometry_msgs.msg import Twist

    mock_ser = _make_mock_serial()

    with patch("serial.Serial", return_value=mock_ser):
        sys.path.insert(
            0,
            str(_REPO / "robot_ws" / "src" / "omnibot_driver" / "scripts"),
        )
        from yahboom_controller_node import YahboomControllerNode

        node = YahboomControllerNode()
        node.serial_port = mock_ser

        # Provide a cmd_vel so send_motion_command has something to do
        cmd = Twist()
        cmd.linear.x = 0.1
        cmd.angular.z = 0.2
        node.current_twist = cmd

        # Patch send_packet to remove the time.sleep(0.002) — measure CPU only
        send_calls = []

        def _fast_send(msg_type, payload):
            send_calls.append((msg_type, payload))

        node.send_packet = _fast_send

        h = TimingHarness()

        def _one_callback():
            mock_ser.in_waiting = len(_REALISTIC_RX)
            mock_ser.read.return_value = _REALISTIC_RX
            node.update_callback()

        # Warmup
        for _ in range(20):
            _one_callback()

        stats = h.run(_one_callback, n=500, warmup=20)
        print_stats("update_callback_mock_ms (no sleep, pure Python)", stats)
        check_slo("full_control_loop_ms", stats["p95_ms"], fail_on_max=False)

        node.destroy_node()

    return stats


def test_bench_read_yahboom_odometry():
    """
    Benchmark read_yahboom_odometry() alone — serial parse + odometry integration.
    """
    mock_ser = _make_mock_serial()

    with patch("serial.Serial", return_value=mock_ser):
        sys.path.insert(
            0, str(_REPO / "robot_ws" / "src" / "omnibot_driver" / "scripts")
        )
        from yahboom_controller_node import YahboomControllerNode

        node = YahboomControllerNode()
        node.serial_port = mock_ser

        h = TimingHarness()

        def _one_read():
            mock_ser.in_waiting = len(_REALISTIC_RX)
            mock_ser.read.return_value = _REALISTIC_RX
            node.read_yahboom_odometry()

        stats = h.run(_one_read, n=2000, warmup=50)
        print_stats("read_yahboom_odometry_ms (parse + integrate)", stats)
        node.destroy_node()

    return stats


def test_bench_cmd_vel_mux_routing():
    """
    Benchmark cmd_vel_mux routing latency: publish Twist → /cmd_vel/out callback.
    Runs mux node in its own thread, measures pub-to-callback wall time.
    """
    sys.path.insert(
        0,
        str(_REPO / "robot_ws" / "src" / "omnibot_hybrid" / "omnibot_hybrid"),
    )
    try:
        from cmd_vel_mux import CmdVelMux
    except ImportError:
        pytest.skip("cmd_vel_mux not importable — check PYTHONPATH or colcon build")

    from geometry_msgs.msg import Twist

    mux = CmdVelMux()
    stop = threading.Event()
    t = threading.Thread(
        target=lambda: [rclpy.spin_once(mux, timeout_sec=0.005)
                        for _ in iter(lambda: stop.is_set(), True)],
        daemon=True,
    )
    t.start()

    # Publisher to /cmd_vel, subscriber on /cmd_vel/out
    probe = Node("mux_bench_probe")
    pub = probe.create_publisher(Twist, "/cmd_vel", 10)

    arrival_times: list[float] = []
    send_times: list[float] = []

    def _out_cb(_msg):
        arrival_times.append(time.perf_counter())

    probe.create_subscription(Twist, "/cmd_vel/out", _out_cb, 10)

    stop_probe = threading.Event()
    t_probe = threading.Thread(
        target=lambda: [rclpy.spin_once(probe, timeout_sec=0.005)
                        for _ in iter(lambda: stop_probe.is_set(), True)],
        daemon=True,
    )
    t_probe.start()

    # Warmup
    for _ in range(20):
        msg = Twist()
        pub.publish(msg)
        time.sleep(0.005)

    arrival_times.clear()
    send_times.clear()

    # Measure 200 round-trips
    for _ in range(200):
        msg = Twist()
        msg.linear.x = 0.1
        send_times.append(time.perf_counter())
        pub.publish(msg)
        time.sleep(0.005)

    time.sleep(0.1)  # drain remaining callbacks
    stop.set()
    stop_probe.set()
    mux.destroy_node()
    probe.destroy_node()

    if len(arrival_times) < 50:
        pytest.skip("Not enough /cmd_vel/out messages received")

    # Pair send → arrival times (approximate — not perfectly aligned)
    n = min(len(send_times), len(arrival_times))
    latencies_ms = [
        (arrival_times[i] - send_times[i]) * 1000.0
        for i in range(n)
        if arrival_times[i] > send_times[i]
    ]

    if not latencies_ms:
        pytest.skip("Could not compute latencies — check topic remapping")

    p95 = sorted(latencies_ms)[max(0, int(0.95 * len(latencies_ms)) - 1)]
    stats = {
        "n": len(latencies_ms),
        "mean_ms": sum(latencies_ms) / len(latencies_ms),
        "median_ms": sorted(latencies_ms)[len(latencies_ms) // 2],
        "p95_ms": p95,
        "min_ms": min(latencies_ms),
        "max_ms": max(latencies_ms),
    }
    print_stats("cmd_vel_mux_route_ms (/cmd_vel → /cmd_vel/out)", stats)
    check_slo("cmd_vel_mux_route_ms", p95, fail_on_max=False)
    return stats


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — ROS 2 Control Loop Benchmarks")
    print("=" * 70)

    rclpy.init()

    all_results: dict[str, dict] = {}

    print("\n[update_callback (mock serial — CPU overhead only)]")
    all_results["update_callback"] = test_bench_update_callback_mock_serial()
    all_results["read_odometry"] = test_bench_read_yahboom_odometry()

    print("\n[cmd_vel_mux routing]")
    try:
        all_results["mux_routing"] = test_bench_cmd_vel_mux_routing()
    except Exception as e:
        print(f"  SKIP  mux routing: {e}")

    rclpy.shutdown()

    write_results("ros_control_loop", all_results)
    print("\nDone.")
