"""
benchmarks/ros/bench_topic_latency.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Measures ROS 2 message header.stamp → subscriber arrival latency.

For stamped messages (Odometry, JointState, Image), measures the delta between
the publisher's timestamp and the wall-clock time when the subscriber fires.

For Twist (no header), measures round-trip via a relay node.

Requires ROS 2 Jazzy + rclpy.
Run:
    pytest benchmarks/ros/bench_topic_latency.py -v -m ros
    python benchmarks/ros/bench_topic_latency.py
"""

from __future__ import annotations

import sys
import threading
import time
from pathlib import Path
from typing import Optional

import pytest

_REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(_REPO))

from benchmarks.conftest import check_slo, print_stats, skip_if_no_ros, write_results

pytestmark = pytest.mark.ros
skip_if_no_ros()

import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist
from nav_msgs.msg import Odometry
from sensor_msgs.msg import Image, JointState
from std_msgs.msg import String


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _stamp_to_ns(header) -> int:
    return header.stamp.sec * 1_000_000_000 + header.stamp.nanosec


def _rolling_p95(samples: list[float]) -> float:
    if not samples:
        return 0.0
    s = sorted(samples)
    return s[max(0, int(0.95 * len(s)) - 1)]


# ---------------------------------------------------------------------------
# Stamp-delta probe — for messages with header.stamp
# ---------------------------------------------------------------------------


class StampDeltaNode(Node):
    """Subscribe to a stamped topic, measure pub-stamp → arrival wall-clock delta."""

    def __init__(self, topic: str, msg_type, node_suffix: str = ""):
        super().__init__(f"stamp_delta_probe{node_suffix}")
        self.latencies_ms: list[float] = []
        self._count = 0
        self.create_subscription(msg_type, topic, self._cb, 10)

    def _cb(self, msg):
        recv_ns = self.get_clock().now().nanoseconds
        pub_ns = _stamp_to_ns(msg.header)
        delta_ms = (recv_ns - pub_ns) / 1e6
        # Ignore negative deltas (clock skew artefacts on first packet)
        if delta_ms >= 0:
            self.latencies_ms.append(delta_ms)
        self._count += 1


# ---------------------------------------------------------------------------
# Round-trip probe — for Twist (no header)
# ---------------------------------------------------------------------------


class RoundTripNode(Node):
    """
    Publish ping messages, relay to pong, measure wall-clock round-trip.
    Divide by 2 for approximate one-way latency.
    """

    def __init__(self, ping_topic: str = "/bench/ping", pong_topic: str = "/bench/pong"):
        super().__init__("round_trip_timer")
        self._send_time: Optional[float] = None
        self.round_trip_ms: list[float] = []
        self._pub = self.create_publisher(String, ping_topic, 10)
        self.create_subscription(String, pong_topic, self._pong_cb, 10)

    def ping(self):
        self._send_time = time.perf_counter()
        msg = String()
        msg.data = "ping"
        self._pub.publish(msg)

    def _pong_cb(self, _msg):
        if self._send_time is not None:
            self.round_trip_ms.append((time.perf_counter() - self._send_time) * 1000.0)
            self._send_time = None


class RelayNode(Node):
    """Echo /bench/ping → /bench/pong."""

    def __init__(self):
        super().__init__("bench_relay")
        self._pub = self.create_publisher(String, "/bench/pong", 10)
        self.create_subscription(String, "/bench/ping", lambda m: self._pub.publish(m), 10)


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@pytest.fixture(scope="module", autouse=True)
def ros_context():
    rclpy.init()
    yield
    rclpy.shutdown()


def _spin(node: Node, stop_event: threading.Event):
    while not stop_event.is_set():
        rclpy.spin_once(node, timeout_sec=0.01)


# ---------------------------------------------------------------------------
# Benchmarks
# ---------------------------------------------------------------------------


def test_bench_odometry_stamp_latency():
    """
    /odom (Odometry) header.stamp → arrival latency.
    Requires a running yahboom_controller_node or any odom publisher.
    """
    probe = StampDeltaNode("/odom", Odometry, "_odom")
    stop = threading.Event()
    t = threading.Thread(target=_spin, args=(probe, stop), daemon=True)
    t.start()

    # Wait up to 5 seconds to collect 50 samples
    deadline = time.time() + 5.0
    while len(probe.latencies_ms) < 50 and time.time() < deadline:
        time.sleep(0.1)

    stop.set()
    probe.destroy_node()

    if len(probe.latencies_ms) < 5:
        pytest.skip("Not enough /odom messages received — is yahboom_controller_node running?")

    samples = probe.latencies_ms
    p95 = _rolling_p95(samples)
    stats = {
        "n": len(samples),
        "mean_ms": sum(samples) / len(samples),
        "median_ms": sorted(samples)[len(samples) // 2],
        "p95_ms": p95,
        "min_ms": min(samples),
        "max_ms": max(samples),
    }
    print_stats("odom_stamp_delta_ms (/odom header.stamp latency)", stats)
    check_slo("odom_stamp_delta_ms", p95, fail_on_max=False)
    return stats


def test_bench_joint_state_stamp_latency():
    """
    /arm/joint_states (JointState) header.stamp → arrival latency.
    Requires a running arm_driver_node.
    """
    probe = StampDeltaNode("/arm/joint_states", JointState, "_js")
    stop = threading.Event()
    t = threading.Thread(target=_spin, args=(probe, stop), daemon=True)
    t.start()

    deadline = time.time() + 5.0
    while len(probe.latencies_ms) < 50 and time.time() < deadline:
        time.sleep(0.1)

    stop.set()
    probe.destroy_node()

    if len(probe.latencies_ms) < 5:
        pytest.skip("Not enough /arm/joint_states messages — is arm_driver_node running?")

    samples = probe.latencies_ms
    p95 = _rolling_p95(samples)
    stats = {
        "n": len(samples),
        "mean_ms": sum(samples) / len(samples),
        "median_ms": sorted(samples)[len(samples) // 2],
        "p95_ms": p95,
        "min_ms": min(samples),
        "max_ms": max(samples),
    }
    print_stats("joint_state_stamp_delta_ms", stats)
    check_slo("joint_state_stamp_delta_ms", p95, fail_on_max=False)
    return stats


def test_bench_bev_image_latency():
    """
    /camera/bev/image_raw (Image) header.stamp → arrival latency.
    Requires bev_stitcher_node running.
    """
    probe = StampDeltaNode("/camera/bev/image_raw", Image, "_bev")
    stop = threading.Event()
    t = threading.Thread(target=_spin, args=(probe, stop), daemon=True)
    t.start()

    deadline = time.time() + 5.0
    while len(probe.latencies_ms) < 20 and time.time() < deadline:
        time.sleep(0.1)

    stop.set()
    probe.destroy_node()

    if len(probe.latencies_ms) < 3:
        pytest.skip("Not enough BEV image messages — is bev_stitcher_node running?")

    samples = probe.latencies_ms
    p95 = _rolling_p95(samples)
    stats = {
        "n": len(samples),
        "mean_ms": sum(samples) / len(samples),
        "median_ms": sorted(samples)[len(samples) // 2],
        "p95_ms": p95,
        "min_ms": min(samples),
        "max_ms": max(samples),
    }
    print_stats("bev_image_latency_ms", stats)
    check_slo("bev_image_latency_ms", p95, fail_on_max=False)
    return stats


def test_bench_intraprocess_roundtrip():
    """
    Intra-process String round-trip via relay: ping → relay → pong.
    Measures raw DDS localhost latency with no hardware involved.
    This is the lower bound for any pub/sub latency measurement.
    """
    relay = RelayNode()
    prober = RoundTripNode()

    stop_relay = threading.Event()
    stop_prober = threading.Event()
    t_relay = threading.Thread(target=_spin, args=(relay, stop_relay), daemon=True)
    t_prober = threading.Thread(target=_spin, args=(prober, stop_prober), daemon=True)
    t_relay.start()
    t_prober.start()

    # Warmup
    for _ in range(10):
        prober.ping()
        time.sleep(0.02)

    prober.round_trip_ms.clear()

    # Measure
    for _ in range(100):
        prober.ping()
        time.sleep(0.01)

    stop_relay.set()
    stop_prober.set()
    relay.destroy_node()
    prober.destroy_node()

    samples = prober.round_trip_ms
    if len(samples) < 10:
        pytest.skip("Not enough round-trip responses — DDS discovery may not be ready")

    one_way_ms = [s / 2.0 for s in samples]
    p95 = _rolling_p95(one_way_ms)
    stats = {
        "n": len(one_way_ms),
        "mean_ms": sum(one_way_ms) / len(one_way_ms),
        "median_ms": sorted(one_way_ms)[len(one_way_ms) // 2],
        "p95_ms": p95,
        "min_ms": min(one_way_ms),
        "max_ms": max(one_way_ms),
    }
    print_stats("dds_intraprocess_oneway_ms (String round-trip / 2)", stats)
    print(
        f"  INFO  DDS intra-process one-way p95={p95:.3f}ms — "
        "this is the irreducible ROS messaging overhead on this machine."
    )
    return stats


# ---------------------------------------------------------------------------
# Standalone runner
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("\n" + "=" * 70)
    print("OmniBot — ROS 2 Topic Latency Benchmarks")
    print("Requires running nodes for most tests.")
    print("=" * 70)

    rclpy.init()

    all_results: dict[str, dict] = {}

    print("\n[Intra-process Round-Trip (no hardware needed)]")
    all_results["intraprocess_roundtrip"] = test_bench_intraprocess_roundtrip()

    print("\n[Live Topic Stamp Deltas (requires running nodes)]")
    try:
        all_results["odom_stamp"] = test_bench_odometry_stamp_latency()
    except Exception as e:
        print(f"  SKIP  /odom: {e}")

    try:
        all_results["joint_state_stamp"] = test_bench_joint_state_stamp_latency()
    except Exception as e:
        print(f"  SKIP  /arm/joint_states: {e}")

    try:
        all_results["bev_image_latency"] = test_bench_bev_image_latency()
    except Exception as e:
        print(f"  SKIP  BEV image: {e}")

    rclpy.shutdown()

    write_results("ros_topic_latency", all_results)
    print("\nDone.")
