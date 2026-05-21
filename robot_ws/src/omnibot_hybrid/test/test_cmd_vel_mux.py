"""
Unit tests for CmdVelMux — pure logic, no DDS required.

Callbacks are invoked directly (no rclpy.spin() thread) so tests are
fully deterministic and run in milliseconds regardless of DDS timing.
The output publisher and active-mode publisher are replaced with mocks
so we can inspect calls without any DDS infrastructure.
"""

from unittest.mock import MagicMock

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


@pytest.fixture(scope="module")
def mux(ros_context):
    """Single CmdVelMux node shared across the whole module.

    Creating one node avoids repeated DDS entity teardown/creation without
    an executor spinning, which can cause race conditions in CI.
    """
    node = CmdVelMux()
    node._out_pub = MagicMock()
    node._active_mode_pub = MagicMock()
    yield node
    node.destroy_node()


@pytest.fixture(autouse=True)
def reset_mux(mux):
    """Reset mock call counts and active mode to defaults before each test."""
    mux._out_pub.reset_mock()
    mux._active_mode_pub.reset_mock()
    mux._active_mode = "nav2"


# ── Helpers ──────────────────────────────────────────────────────────────────


def _mode_msg(mode: str) -> String:
    msg = String()
    msg.data = mode
    return msg


def _twist(x: float = 1.0) -> Twist:
    t = Twist()
    t.linear.x = x
    return t


# ── Tests ────────────────────────────────────────────────────────────────────


class TestDefaultMode:
    def test_default_is_nav2(self, mux):
        assert mux._active_mode == "nav2"


class TestModeSwitching:
    def test_switch_to_vla(self, mux):
        mux._mode_cb(_mode_msg("vla"))
        assert mux._active_mode == "vla"

    def test_switch_to_teleop(self, mux):
        mux._mode_cb(_mode_msg("teleop"))
        assert mux._active_mode == "teleop"

    def test_switch_back_to_nav2(self, mux):
        mux._mode_cb(_mode_msg("vla"))
        mux._mode_cb(_mode_msg("nav2"))
        assert mux._active_mode == "nav2"

    def test_unknown_mode_ignored(self, mux):
        mux._mode_cb(_mode_msg("nav2"))
        mux._mode_cb(_mode_msg("unknown_mode"))
        assert mux._active_mode == "nav2"

    def test_mode_is_case_insensitive(self, mux):
        mux._mode_cb(_mode_msg("VLA"))
        assert mux._active_mode == "vla"

    def test_mode_strips_whitespace(self, mux):
        mux._mode_cb(_mode_msg("  teleop  "))
        assert mux._active_mode == "teleop"


class TestForwarding:
    """Verify that Twist messages are forwarded only when mode matches."""

    def test_nav2_forwarded_in_nav2_mode(self, mux):
        # default mode is nav2
        mux._nav2_cb(_twist(0.5))
        mux._out_pub.publish.assert_called_once()
        assert mux._out_pub.publish.call_args[0][0].linear.x == pytest.approx(0.5)

    def test_vla_not_forwarded_in_nav2_mode(self, mux):
        # default mode is nav2; VLA messages must be dropped
        mux._vla_cb(_twist(0.7))
        mux._out_pub.publish.assert_not_called()

    def test_vla_forwarded_in_vla_mode(self, mux):
        mux._mode_cb(_mode_msg("vla"))
        mux._vla_cb(_twist(0.3))
        mux._out_pub.publish.assert_called_once()
        assert mux._out_pub.publish.call_args[0][0].linear.x == pytest.approx(0.3)

    def test_teleop_forwarded_in_teleop_mode(self, mux):
        mux._mode_cb(_mode_msg("teleop"))
        mux._teleop_cb(_twist(0.1))
        mux._out_pub.publish.assert_called_once()

    def test_nav2_not_forwarded_in_vla_mode(self, mux):
        mux._mode_cb(_mode_msg("vla"))
        mux._nav2_cb(_twist(0.9))
        mux._out_pub.publish.assert_not_called()


class TestActiveModePublisher:
    def test_active_mode_published(self, mux):
        mux._mode_cb(_mode_msg("vla"))
        mux._publish_active_mode()
        mux._active_mode_pub.publish.assert_called()
        last_msg = mux._active_mode_pub.publish.call_args[0][0]
        assert last_msg.data == "vla"

    def test_mode_change_triggers_publish(self, mux):
        # _mode_cb calls _publish_active_mode on every mode change
        mux._mode_cb(_mode_msg("teleop"))
        mux._active_mode_pub.publish.assert_called()
        last_msg = mux._active_mode_pub.publish.call_args[0][0]
        assert last_msg.data == "teleop"
