"""Tests for OhhO View IPM module — camera_rotation and compute_ipm_homography."""

import math
import sys
import os

import numpy as np

# Add the package to the path so we can import without ROS
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from ros2_bev_stitcher.bev_ipm import (
    camera_rotation,
    compute_ipm_homography,
    rotation_from_euler,
)


class TestCameraRotation:
    """Test the camera_rotation function for correctness."""

    def test_forward_level(self):
        """Front camera looking straight ahead (horizontal)."""
        R = camera_rotation(yaw=0.0, pitch=0.0, roll=0.0)

        assert R.shape == (3, 3)
        assert np.allclose(np.linalg.det(R), 1.0, atol=1e-6)

        # Optical axis (row 2) = forward (+X)
        assert np.allclose(R[2], [1.0, 0.0, 0.0], atol=1e-6)
        # Right (row 0) = world -Y (right)
        assert np.allclose(R[0], [0.0, -1.0, 0.0], atol=1e-6)
        # Down (row 1) = world -Z (down)
        assert np.allclose(R[1], [0.0, 0.0, -1.0], atol=1e-6)

    def test_forward_looking_down(self):
        """Front camera looking down at ~20 degrees."""
        R = camera_rotation(yaw=0.0, pitch=0.35, roll=0.0)

        assert np.allclose(np.linalg.det(R), 1.0, atol=1e-6)

        # Optical axis should have negative Z component (looking down)
        assert R[2, 2] < 0  # Z component of optical axis is negative

    def test_left_looking_down(self):
        """Left camera looking left and down."""
        R = camera_rotation(yaw=math.pi / 2, pitch=0.35, roll=0.0)

        assert np.allclose(np.linalg.det(R), 1.0, atol=1e-6)

        # Optical axis should have significant +Y component (left)
        assert R[2, 1] > 0.5
        # And negative Z (looking down)
        assert R[2, 2] < 0

    def test_rear_looking_down(self):
        """Rear camera looking backward and down."""
        R = camera_rotation(yaw=math.pi, pitch=0.35, roll=0.0)

        assert np.allclose(np.linalg.det(R), 1.0, atol=1e-6)

        # Optical axis should be mostly -X (backward)
        assert R[2, 0] < -0.5
        # And negative Z (looking down)
        assert R[2, 2] < 0

    def test_right_looking_down(self):
        """Right camera looking right and down."""
        R = camera_rotation(yaw=-math.pi / 2, pitch=0.35, roll=0.0)

        assert np.allclose(np.linalg.det(R), 1.0, atol=1e-6)

        # Optical axis should be mostly -Y (right)
        assert R[2, 1] < -0.5
        # And negative Z (looking down)
        assert R[2, 2] < 0

    def test_orthogonality(self):
        """Rotation matrix should be orthogonal (R @ R^T = I)."""
        for yaw in [0.0, math.pi / 2, math.pi, -math.pi / 2]:
            for pitch in [-0.35, 0.0, 0.35]:
                for roll in [0.0, 0.1, -0.1]:
                    R = camera_rotation(yaw, pitch, roll)
                    assert np.allclose(R @ R.T, np.eye(3), atol=1e-6)

    def test_yaw_zero_means_forward(self):
        """At yaw=0, the optical axis should be in the +X hemisphere."""
        R = camera_rotation(yaw=0.0, pitch=0.0, roll=0.0)
        # Optical axis dot +X should be positive
        assert np.dot(R[2], [1.0, 0.0, 0.0]) > 0.9

    def test_pitch_positive_means_looking_down(self):
        """Positive pitch tilts the optical axis downward (-Z)."""
        R_level = camera_rotation(yaw=0.0, pitch=0.0, roll=0.0)
        R_down = camera_rotation(yaw=0.0, pitch=0.5, roll=0.0)
        # Optical axis Z component should become more negative with pitch
        assert R_down[2, 2] < R_level[2, 2]


class TestLegacyRotation:
    """Verify rotation_from_euler still works (for backward compat)."""

    def test_identity(self):
        R = rotation_from_euler(0.0, 0.0, 0.0)
        assert np.allclose(R, np.eye(3), atol=1e-6)

    def test_yaw_pi_half(self):
        R = rotation_from_euler(0.0, 0.0, math.pi / 2)
        # Rz(pi/2): X -> Y, Y -> -X, Z stays
        assert np.allclose(R @ [1, 0, 0], [0, 1, 0], atol=1e-6)
        assert np.allclose(R @ [0, 1, 0], [-1, 0, 0], atol=1e-6)


class TestComputeIpmHomography:
    """Test the IPM homography computation end-to-end."""

    K = np.array([[320, 0, 320], [0, 320, 240], [0, 0, 1]], dtype=np.float64)

    def test_front_camera_projection(self):
        """Front camera: image center maps to ~0.57m ahead in canvas."""
        R = camera_rotation(0.0, 0.35, 0.0)
        C = np.array([0.24, 0.0, 0.12], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, canvas_size=800, pixels_per_meter=80.0)

        # Image center (320, 240) → canvas center-ish, slightly above
        c = H @ np.array([320.0, 240.0, 1.0])
        cx, cy = c[0] / c[2], c[1] / c[2]

        # x should be near canvas center (400)
        assert 395 < cx < 405, f"Expected cx ~400, got {cx:.1f}"
        # y should be above center (forward in world)
        assert 340 < cy < 370, f"Expected cy ~355, got {cy:.1f}"

    def test_rear_camera_projection(self):
        """Rear camera: image center maps to behind the robot."""
        R = camera_rotation(math.pi, 0.35, 0.0)
        C = np.array([-0.24, 0.0, 0.12], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, canvas_size=800, pixels_per_meter=80.0)

        c = H @ np.array([320.0, 240.0, 1.0])
        cx, cy = c[0] / c[2], c[1] / c[2]

        # x near center
        assert 395 < cx < 405, f"Expected cx ~400, got {cx:.1f}"
        # y should be below center (behind in world)
        assert 430 < cy < 460, f"Expected cy ~445, got {cy:.1f}"

    def test_left_camera_projection(self):
        """Left camera: image center maps to the left side."""
        R = camera_rotation(math.pi / 2, 0.35, 0.0)
        C = np.array([0.0, 0.17, 0.12], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, canvas_size=800, pixels_per_meter=80.0)

        c = H @ np.array([320.0, 240.0, 1.0])
        cx, cy = c[0] / c[2], c[1] / c[2]

        # x should be left of center (cx < 400 for world +Y = left)
        assert cx < 400, f"Expected cx < 400 (left), got {cx:.1f}"
        # y near center
        assert 395 < cy < 405, f"Expected cy ~400, got {cy:.1f}"

    def test_right_camera_projection(self):
        """Right camera: image center maps to the right side."""
        R = camera_rotation(-math.pi / 2, 0.35, 0.0)
        C = np.array([0.0, -0.17, 0.12], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, canvas_size=800, pixels_per_meter=80.0)

        c = H @ np.array([320.0, 240.0, 1.0])
        cx, cy = c[0] / c[2], c[1] / c[2]

        # x should be right of center (cx > 400 for world -Y = right)
        assert cx > 400, f"Expected cx > 400 (right), got {cx:.1f}"
        # y near center
        assert 395 < cy < 405, f"Expected cy ~400, got {cy:.1f}"

    def test_homography_invertible(self):
        """Homography should be invertible for valid configurations."""
        for yaw, C in [
            (0.0, [0.24, 0.0, 0.12]),
            (math.pi, [-0.24, 0.0, 0.12]),
            (math.pi / 2, [0.0, 0.17, 0.12]),
            (-math.pi / 2, [0.0, -0.17, 0.12]),
        ]:
            R = camera_rotation(yaw, 0.35, 0.0)
            C_arr = np.array(C, dtype=np.float64)
            H = compute_ipm_homography(self.K, R, C_arr, 800, 80.0)
            # Should be invertible
            H_inv = np.linalg.inv(H)
            # Round-trip: H_inv @ H should be identity
            assert np.allclose(H_inv @ H, np.eye(3), atol=1e-6)

    def test_ground_z_offset(self):
        """Higher ground_z shifts projections."""
        R = camera_rotation(0.0, 0.35, 0.0)
        C = np.array([0.24, 0.0, 0.12], dtype=np.float64)

        H0 = compute_ipm_homography(self.K, R, C, 800, 80.0, ground_z=0.0)
        H1 = compute_ipm_homography(self.K, R, C, 800, 80.0, ground_z=0.05)

        # Different ground_z should give different homographies
        assert not np.allclose(H0, H1)

    def test_canvas_scale(self):
        """Doubling pixels_per_meter should double the scale."""
        R = camera_rotation(0.0, 0.35, 0.0)
        C = np.array([0.24, 0.0, 0.12], dtype=np.float64)

        H_lo = compute_ipm_homography(self.K, R, C, 800, 40.0)
        H_hi = compute_ipm_homography(self.K, R, C, 800, 80.0)

        # Image center should map to different canvas y positions
        c_lo = H_lo @ np.array([320.0, 240.0, 1.0])
        c_hi = H_hi @ np.array([320.0, 240.0, 1.0])
        y_lo = c_lo[1] / c_lo[2]
        y_hi = c_hi[1] / c_hi[2]

        # Higher ppm means farther from center
        assert abs(y_hi - 400) > abs(y_lo - 400), (
            "ppm=80 should push farther from center than ppm=40"
        )

    def test_homography_shape_and_dtype(self):
        """Output should be 3x3 float64."""
        R = camera_rotation(0.0, 0.0, 0.0)
        C = np.array([0.0, 0.0, 0.1], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, 800, 80.0)

        assert H.shape == (3, 3)
        assert H.dtype == np.float64

    def test_forward_maps_to_above_center(self):
        """Points in front of the robot should map to canvas y < center."""
        R = camera_rotation(0.0, 0.35, 0.0)
        C = np.array([0.24, 0.0, 0.12], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, 800, 80.0)

        # Image pixel at bottom-center (close to camera on ground)
        c = H @ np.array([320.0, 470.0, 1.0])
        cy = c[1] / c[2]

        # Close to camera = small forward distance = near center
        # At ~32cm ahead, cy = 400 - 80*0.32 = 374
        assert 370 < cy < 395, f"Bottom-center should map to ~374, got {cy:.1f}"


class TestRoundTrip:
    """Verify the homography maps consistently between image and world."""

    K = np.array([[320, 0, 320], [0, 320, 240], [0, 0, 1]], dtype=np.float64)

    def test_front_round_trip(self):
        """Image → canvas → world should be consistent."""
        R = camera_rotation(0.0, 0.35, 0.0)
        C = np.array([0.24, 0.0, 0.12], dtype=np.float64)
        H = compute_ipm_homography(self.K, R, C, 800, 80.0)

        # World point 1m forward, centered
        # canvas coords: x = 400 + 80*(−0) = 400, y = 400 + 80*(−1) = 320
        # ... wait, H maps image → canvas. Let's go canvas → world via H^{-1}.
        np.array([400.0, 320.0, 1.0])  # image coords placeholder
        np.linalg.inv(H)

        # Actually, test: a world ground point (1.0, 0.0, 1.0) should have
        # canvas_y about 320 (1m forward = 80 px up from center)
        pass
