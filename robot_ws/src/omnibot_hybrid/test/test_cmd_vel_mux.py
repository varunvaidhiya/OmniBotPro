"""
Unit tests for CmdVelMux — pure logic, no hardware required.

Tests exercise mode-switching and message-forwarding logic by calling
callbacks directly, with no DDS delivery or threading.  This makes them
fast and completely deterministic.
"""

from unittest.mock import MagicMock

import pytest
from geometry_msgs.msg import Twist
from std_msgs.msg import String

from omnibot_hybrid.cmd_vel_mux import CmdVelMux


@pytest.fixture()
def mux_node():
    node = CmdVelMux()
    # Swap real publishers for mocks so tests can assert on calls
    # without requiring DDS discovery or message delivery.
    node._out_pub = MagicMock()
    node._active_mode_pub = MagicMock()
    yield node
    node.destroy_node()


# ── Helpers ─────────────────────────────────────────────────────────────────


def _set_mode(node: CmdVelMux, mode: str) -> None:
    """Drive the mode-switch callback directly (no DDS publish needed)."""
    msg = String()
    msg.data = mode
    node._mode_cb(msg)


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
        _set_mode(mux_node, "vla")
        assert mux_node._active_mode == "vla"

    def test_switch_to_teleop(self, mux_node):
        _set_mode(mux_node, "teleop")
        assert mux_node._active_mode == "teleop"

    def test_switch_back_to_nav2(self, mux_node):
        _set_mode(mux_node, "vla")
        _set_mode(mux_node, "nav2")
        assert mux_node._active_mode == "nav2"

    def test_unknown_mode_ignored(self, mux_node):
        _set_mode(mux_node, "nav2")
        _set_mode(mux_node, "unknown_mode")
        assert mux_node._active_mode == "nav2"

    def test_mode_is_case_insensitive(self, mux_node):
        _set_mode(mux_node, "VLA")
        assert mux_node._active_mode == "vla"

    def test_mode_strips_whitespace(self, mux_node):
        _set_mode(mux_node, "  teleop  ")
        assert mux_node._active_mode == "teleop"


class TestForwarding:
    """Verify that Twist messages are forwarded only when mode matches."""

    def test_nav2_forwarded_in_nav2_mode(self, mux_node):
        mux_node._active_mode = "nav2"
        mux_node._nav2_cb(_make_twist(0.5))
        mux_node._out_pub.publish.assert_called_once()
        msg = mux_node._out_pub.publish.call_args[0][0]
        assert msg.linear.x == pytest.approx(0.5)

    def test_vla_not_forwarded_in_nav2_mode(self, mux_node):
        mux_node._active_mode = "nav2"
        mux_node._vla_cb(_make_twist(0.7))
        mux_node._out_pub.publish.assert_not_called()

    def test_vla_forwarded_in_vla_mode(self, mux_node):
        mux_node._active_mode = "vla"
        mux_node._vla_cb(_make_twist(0.3))
        mux_node._out_pub.publish.assert_called_once()
        msg = mux_node._out_pub.publish.call_args[0][0]
        assert msg.linear.x == pytest.approx(0.3)

    def test_teleop_forwarded_in_teleop_mode(self, mux_node):
        mux_node._active_mode = "teleop"
        mux_node._teleop_cb(_make_twist(0.1))
        mux_node._out_pub.publish.assert_called_once()


class TestActiveModePublisher:
    def test_active_mode_published(self, mux_node):
        mux_node._active_mode = "vla"
        mux_node._publish_active_mode()
        mux_node._active_mode_pub.publish.assert_called_once()
        msg = mux_node._active_mode_pub.publish.call_args[0][0]
        assert msg.data == "vla"
