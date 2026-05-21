"""
Unit tests for CmdVelMux — pure logic, no hardware required.

These tests spin a real rclpy node in a thread so that ROS callbacks fire
without needing a full ROS 2 environment beyond the library itself.
"""

import threading
import time

import pytest
import rclpy
from geometry_msgs.msg import Twist
from std_msgs.msg import String

from omnibot_hybrid.cmd_vel_mux import CmdVelMux


@pytest.fixture(scope="module", autouse=True)
def ros_context():
    rclpy.init()
    yield
    rclpy.shutdown()


@pytest.fixture()
def mux_node():
    node = CmdVelMux()
    thread = threading.Thread(target=rclpy.spin, args=(node,), daemon=True)
    thread.start()
    yield node
    node.destroy_node()


# ── Helpers ─────────────────────────────────────────────────────────────────


def _pub_mode(node, mode: str, *, wait: float = 0.3):
    """Publish a control_mode message and wait for the callback."""
    pub = node.create_publisher(String, "/control_mode", 10)
    time.sleep(0.2)  # Allow DDS to match publisher against the node's subscription
    msg = String()
    msg.data = mode
    pub.publish(msg)
    time.sleep(wait)
    pub.destroy()


def _make_twist(x: float = 1.0) -> Twist:
    t = Twist()
    t.linear.x = x
    return t


# ── Tests ────────────────────────────────────────────────────────────────────


class TestDefaultMode:
    def test_default_is_nav2(self, mux_node):
        assert mux_node._active_mode == "nav2"


class TestModeSwitching:
    def test_switch_to_vla(self, mux_node):
        _pub_mode(mux_node, "vla")
        assert mux_node._active_mode == "vla"

    def test_switch_to_teleop(self, mux_node):
        _pub_mode(mux_node, "teleop")
        assert mux_node._active_mode == "teleop"

    def test_switch_back_to_nav2(self, mux_node):
        _pub_mode(mux_node, "vla")
        _pub_mode(mux_node, "nav2")
        assert mux_node._active_mode == "nav2"

    def test_unknown_mode_ignored(self, mux_node):
        _pub_mode(mux_node, "nav2")
        _pub_mode(mux_node, "unknown_mode")
        assert mux_node._active_mode == "nav2"

    def test_mode_is_case_insensitive(self, mux_node):
        _pub_mode(mux_node, "VLA")
        assert mux_node._active_mode == "vla"

    def test_mode_strips_whitespace(self, mux_node):
        _pub_mode(mux_node, "  teleop  ")
        assert mux_node._active_mode == "teleop"


class TestForwarding:
    """Verify that Twist messages are forwarded only when mode matches."""

    def test_nav2_forwarded_in_nav2_mode(self, mux_node):
        _pub_mode(mux_node, "nav2")
        received: list[Twist] = []
        sub = mux_node.create_subscription(Twist, "/cmd_vel/out", received.append, 10)
        pub = mux_node.create_publisher(Twist, "/cmd_vel", 10)
        time.sleep(0.5)  # Allow DDS to match new pub and sub against the mux node

        pub.publish(_make_twist(0.5))
        time.sleep(0.5)

        sub.destroy()
        pub.destroy()
        assert len(received) == 1
        assert received[0].linear.x == pytest.approx(0.5)

    def test_vla_not_forwarded_in_nav2_mode(self, mux_node):
        _pub_mode(mux_node, "nav2")
        received: list[Twist] = []
        sub = mux_node.create_subscription(Twist, "/cmd_vel/out", received.append, 10)
        pub = mux_node.create_publisher(Twist, "/cmd_vel/vla", 10)
        time.sleep(0.5)  # Allow DDS discovery

        pub.publish(_make_twist(0.7))
        time.sleep(0.5)

        sub.destroy()
        pub.destroy()
        assert len(received) == 0

    def test_vla_forwarded_in_vla_mode(self, mux_node):
        _pub_mode(mux_node, "vla")
        received: list[Twist] = []
        sub = mux_node.create_subscription(Twist, "/cmd_vel/out", received.append, 10)
        pub = mux_node.create_publisher(Twist, "/cmd_vel/vla", 10)
        time.sleep(0.5)  # Allow DDS discovery

        pub.publish(_make_twist(0.3))
        time.sleep(0.5)

        sub.destroy()
        pub.destroy()
        assert len(received) == 1
        assert received[0].linear.x == pytest.approx(0.3)

    def test_teleop_forwarded_in_teleop_mode(self, mux_node):
        _pub_mode(mux_node, "teleop")
        received: list[Twist] = []
        sub = mux_node.create_subscription(Twist, "/cmd_vel/out", received.append, 10)
        pub = mux_node.create_publisher(Twist, "/cmd_vel/teleop", 10)
        time.sleep(0.5)  # Allow DDS discovery

        pub.publish(_make_twist(0.1))
        time.sleep(0.5)

        sub.destroy()
        pub.destroy()
        assert len(received) == 1


class TestActiveModePublisher:
    def test_active_mode_published(self, mux_node):
        _pub_mode(mux_node, "vla")
        received: list[String] = []
        sub = mux_node.create_subscription(
            String, "/control_mode/active", received.append, 10
        )
        time.sleep(2.0)  # 1 Hz timer fires at least once

        sub.destroy()
        assert any(m.data == "vla" for m in received)
