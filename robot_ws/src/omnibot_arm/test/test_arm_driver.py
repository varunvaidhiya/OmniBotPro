"""
Unit tests for ArmDriverNode — tick↔radian conversions and joint clamping.

Wrong conversions move servos to unintended positions and can damage the arm,
so these are safety-critical regressions to catch on every PR.

All tests run against the real class methods using a lightweight stub that
provides only the geometric/limit attributes — no rclpy.init() needed,
no hardware, no LeRobot.
"""

import math
import os
import sys

import pytest

# Add scripts/ to sys.path so the module is importable without being installed.
sys.path.insert(
    0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "scripts"))
)

import arm_driver_node as _mod  # noqa: E402 — must come after sys.path patch

TICKS_PER_REV = 4096
TICKS_PER_RAD = TICKS_PER_REV / (2.0 * math.pi)
HOME_TICKS = [2048, 2048, 2048, 2048, 2048, 2048]
JOINT_MIN = [-3.14, -1.57, -1.57, -1.57, -3.14, -0.1]
JOINT_MAX = [3.14, 1.57, 1.57, 1.57, 3.14, 0.8]


class _Stub:
    """Provides only the attributes the conversion methods read from self."""

    ticks_per_rev = TICKS_PER_REV
    home_ticks = HOME_TICKS
    joint_min = JOINT_MIN
    joint_max = JOINT_MAX
    ticks_per_rad = TICKS_PER_RAD

    ticks_to_radians = _mod.ArmDriverNode.ticks_to_radians
    radians_to_ticks = _mod.ArmDriverNode.radians_to_ticks
    clamp_radians = _mod.ArmDriverNode.clamp_radians


@pytest.fixture
def arm():
    return _Stub()


# ---------------------------------------------------------------------------
# ticks_to_radians
# ---------------------------------------------------------------------------


class TestTicksToRadians:
    def test_home_is_zero(self, arm):
        result = arm.ticks_to_radians(HOME_TICKS)
        assert all(r == pytest.approx(0.0) for r in result)

    def test_quarter_revolution_positive(self, arm):
        # 1024 ticks above home = (4096/4) ticks = pi/2 rad
        ticks = [2048 + 1024, 2048, 2048, 2048, 2048, 2048]
        result = arm.ticks_to_radians(ticks)
        assert result[0] == pytest.approx(math.pi / 2, rel=1e-3)

    def test_quarter_revolution_negative(self, arm):
        ticks = [2048 - 1024, 2048, 2048, 2048, 2048, 2048]
        result = arm.ticks_to_radians(ticks)
        assert result[0] == pytest.approx(-math.pi / 2, rel=1e-3)

    def test_full_revolution(self, arm):
        ticks = [2048 + 4096, 2048, 2048, 2048, 2048, 2048]
        result = arm.ticks_to_radians(ticks)
        assert result[0] == pytest.approx(2 * math.pi, rel=1e-3)

    def test_output_length(self, arm):
        assert len(arm.ticks_to_radians(HOME_TICKS)) == 6

    def test_all_joints_independent(self, arm):
        ticks = [2048 + 512, 2048 - 512, 2048, 2048, 2048, 2048]
        result = arm.ticks_to_radians(ticks)
        assert result[0] > 0
        assert result[1] < 0
        assert result[2] == pytest.approx(0.0)


# ---------------------------------------------------------------------------
# radians_to_ticks
# ---------------------------------------------------------------------------


class TestRadiansToTicks:
    def test_zero_is_home(self, arm):
        result = arm.radians_to_ticks([0.0] * 6)
        assert result == HOME_TICKS

    def test_returns_integers(self, arm):
        result = arm.radians_to_ticks([0.1, -0.2, 0.3, 0.0, 0.0, 0.0])
        assert all(isinstance(t, int) for t in result)

    def test_roundtrip_quarter_revolution(self, arm):
        rads = [math.pi / 2, 0.0, 0.0, 0.0, 0.0, 0.0]
        back = arm.ticks_to_radians(arm.radians_to_ticks(rads))
        assert back[0] == pytest.approx(math.pi / 2, rel=1e-2)

    def test_roundtrip_negative(self, arm):
        rads = [-math.pi / 4, 0.0, 0.0, 0.0, 0.0, 0.0]
        back = arm.ticks_to_radians(arm.radians_to_ticks(rads))
        assert back[0] == pytest.approx(-math.pi / 4, rel=1e-2)

    def test_positive_ticks_above_home(self, arm):
        result = arm.radians_to_ticks([math.pi / 2, 0.0, 0.0, 0.0, 0.0, 0.0])
        assert result[0] > HOME_TICKS[0]

    def test_negative_ticks_below_home(self, arm):
        result = arm.radians_to_ticks([-math.pi / 2, 0.0, 0.0, 0.0, 0.0, 0.0])
        assert result[0] < HOME_TICKS[0]


# ---------------------------------------------------------------------------
# clamp_radians
# ---------------------------------------------------------------------------


class TestClampRadians:
    def test_within_limits_unchanged(self, arm):
        rads = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]
        assert arm.clamp_radians(rads) == pytest.approx(rads)

    def test_above_max_clamped_to_max(self, arm):
        rads = [99.0, 0.0, 0.0, 0.0, 0.0, 0.0]
        result = arm.clamp_radians(rads)
        assert result[0] == pytest.approx(JOINT_MAX[0])

    def test_below_min_clamped_to_min(self, arm):
        rads = [-99.0, 0.0, 0.0, 0.0, 0.0, 0.0]
        result = arm.clamp_radians(rads)
        assert result[0] == pytest.approx(JOINT_MIN[0])

    def test_all_joints_clamped_independently(self, arm):
        rads = [99.0] * 6
        result = arm.clamp_radians(rads)
        assert result == pytest.approx(JOINT_MAX)

    def test_gripper_max_is_0_8(self, arm):
        result = arm.clamp_radians([0.0, 0.0, 0.0, 0.0, 0.0, 99.0])
        assert result[5] == pytest.approx(0.8)

    def test_gripper_min_is_neg_0_1(self, arm):
        result = arm.clamp_radians([0.0, 0.0, 0.0, 0.0, 0.0, -99.0])
        assert result[5] == pytest.approx(-0.1)

    def test_at_exact_limit_not_clamped(self, arm):
        result = arm.clamp_radians(JOINT_MAX)
        assert result == pytest.approx(JOINT_MAX)
