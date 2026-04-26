"""
Tests for mecanum_drive_ros2.kinematics — IK, FK, and pose integration.

These maths underpin odometry and motion control. A sign error here produces
drift or mirrored motion that's hard to diagnose on hardware.
"""

import math

import pytest

from mecanum_drive_ros2.kinematics import (
    RobotGeometry,
    forward_kinematics,
    integrate_pose,
    inverse_kinematics,
)

# Physical constants matching the real robot
GEOM = RobotGeometry(
    wheel_radius=0.04,
    wheel_separation_width=0.215,
    wheel_separation_length=0.165,
)


# ---------------------------------------------------------------------------
# inverse_kinematics
# ---------------------------------------------------------------------------


class TestInverseKinematics:
    def test_pure_forward_motion(self):
        fl, fr, bl, br = inverse_kinematics(0.5, 0.0, 0.0, GEOM)
        # All wheels same speed for forward translation
        assert fl == pytest.approx(fr, rel=1e-6)
        assert fl == pytest.approx(bl, rel=1e-6)
        assert fl == pytest.approx(br, rel=1e-6)
        assert fl > 0

    def test_pure_lateral_motion(self):
        fl, fr, bl, br = inverse_kinematics(0.0, 0.3, 0.0, GEOM)
        # Left strafe: fl and br negative, fr and bl positive
        assert fl < 0
        assert fr > 0
        assert bl > 0
        assert br < 0

    def test_pure_rotation(self):
        fl, fr, bl, br = inverse_kinematics(0.0, 0.0, 1.0, GEOM)
        # CCW: left wheels backward, right wheels forward
        assert fl < 0
        assert fr > 0
        assert bl < 0
        assert br > 0

    def test_zero_velocity_all_zero(self):
        wheels = inverse_kinematics(0.0, 0.0, 0.0, GEOM)
        assert all(w == pytest.approx(0.0) for w in wheels)

    def test_returns_four_values(self):
        assert len(inverse_kinematics(0.1, 0.0, 0.0, GEOM)) == 4

    def test_forward_speed_proportional_to_vx(self):
        fl1, _, _, _ = inverse_kinematics(0.2, 0.0, 0.0, GEOM)
        fl2, _, _, _ = inverse_kinematics(0.4, 0.0, 0.0, GEOM)
        assert fl2 == pytest.approx(2 * fl1, rel=1e-6)


# ---------------------------------------------------------------------------
# forward_kinematics
# ---------------------------------------------------------------------------


class TestForwardKinematics:
    def test_forward_motion(self):
        fl, fr, bl, br = inverse_kinematics(0.5, 0.0, 0.0, GEOM)
        vx, vy, omega = forward_kinematics((fl, fr, bl, br), GEOM)
        assert vx == pytest.approx(0.5, rel=1e-6)
        assert vy == pytest.approx(0.0, abs=1e-9)
        assert omega == pytest.approx(0.0, abs=1e-9)

    def test_lateral_motion_roundtrip(self):
        fl, fr, bl, br = inverse_kinematics(0.0, 0.3, 0.0, GEOM)
        vx, vy, omega = forward_kinematics((fl, fr, bl, br), GEOM)
        assert vx == pytest.approx(0.0, abs=1e-9)
        assert vy == pytest.approx(0.3, rel=1e-6)

    def test_rotation_roundtrip(self):
        fl, fr, bl, br = inverse_kinematics(0.0, 0.0, 1.0, GEOM)
        vx, vy, omega = forward_kinematics((fl, fr, bl, br), GEOM)
        assert omega == pytest.approx(1.0, rel=1e-6)

    def test_all_zero_wheels(self):
        vx, vy, omega = forward_kinematics((0.0, 0.0, 0.0, 0.0), GEOM)
        assert vx == pytest.approx(0.0)
        assert vy == pytest.approx(0.0)
        assert omega == pytest.approx(0.0)

    def test_ik_fk_roundtrip_combined(self):
        vx_in, vy_in, omega_in = 0.3, -0.1, 0.5
        wheels = inverse_kinematics(vx_in, vy_in, omega_in, GEOM)
        vx_out, vy_out, omega_out = forward_kinematics(wheels, GEOM)
        assert vx_out == pytest.approx(vx_in, rel=1e-6)
        assert vy_out == pytest.approx(vy_in, rel=1e-6)
        assert omega_out == pytest.approx(omega_in, rel=1e-6)


# ---------------------------------------------------------------------------
# integrate_pose
# ---------------------------------------------------------------------------


class TestIntegratePose:
    def test_stationary_no_change(self):
        x, y, th = integrate_pose(1.0, 2.0, 0.5, 0.0, 0.0, 0.0, 0.1)
        assert x == pytest.approx(1.0)
        assert y == pytest.approx(2.0)
        assert th == pytest.approx(0.5)

    def test_forward_along_x_axis(self):
        # Starting at origin facing +x, move forward 1 m/s for 1 s
        x, y, th = integrate_pose(0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 1.0)
        assert x == pytest.approx(1.0)
        assert y == pytest.approx(0.0)
        assert th == pytest.approx(0.0)

    def test_lateral_motion(self):
        # Facing +x, strafe +y at 1 m/s for 1 s
        x, y, th = integrate_pose(0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0)
        assert x == pytest.approx(0.0)
        assert y == pytest.approx(1.0)

    def test_rotation_wraps_to_pi(self):
        # Start at theta just below pi, rotate past pi
        _, _, th = integrate_pose(0.0, 0.0, 3.1, 0.0, 0.0, 1.0, 1.0)
        assert -math.pi <= th <= math.pi

    def test_rotation_quarter_turn(self):
        # Rotate 90 degrees (pi/2 rad) at omega=pi/2 rad/s for 1 s
        _, _, th = integrate_pose(0.0, 0.0, 0.0, 0.0, 0.0, math.pi / 2, 1.0)
        assert th == pytest.approx(math.pi / 2, rel=1e-6)

    def test_heading_affects_translation(self):
        # Facing +y (theta=pi/2), vx=1 should move in +y direction
        x, y, _ = integrate_pose(0.0, 0.0, math.pi / 2, 1.0, 0.0, 0.0, 1.0)
        assert x == pytest.approx(0.0, abs=1e-9)
        assert y == pytest.approx(1.0, rel=1e-6)
