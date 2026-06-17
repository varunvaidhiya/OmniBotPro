#!/usr/bin/env python3
"""
Geometric Inverse Perspective Mapping (IPM) for Bird's-Eye-View stitching.

Computes per-camera homographies from camera extrinsic parameters
(position + orientation in base_link) and intrinsic parameters,
using pinhole camera geometry with a known ground plane.

No checkerboard required — reads camera poses from URDF/tf2 or
ROS 2 params. Produces a .npz file consumable by bev_stitcher_node.

Usage (CLI):
    ros2 run ros2_bev_stitcher bev_ipm

Usage (Python API):
    from ros2_bev_stitcher.bev_ipm import compute_ipm_homography, camera_rotation
"""

import argparse
import os
from typing import Dict, List, Optional, Tuple

import numpy as np


# ── Pure math functions (no ROS dependency) ───────────────────────────────────


# ROS imports — lazy/optional so pure functions are importable without ROS
try:
    import rclpy as _rclpy
    from rclpy.node import Node as _Node

    _ROS = True
except ImportError:
    _rclpy = None  # type: ignore[assignment]
    _Node = object  # type: ignore[assignment]
    _ROS = False


def rotation_from_euler(roll: float, pitch: float, yaw: float) -> np.ndarray:
    """
    Legacy generic Euler rotation: Rz(yaw) @ Ry(pitch) @ Rx(roll).
    Prefer :func:`camera_rotation` for camera-specific use.
    """
    cr, sr = np.cos(roll), np.sin(roll)
    cp, sp = np.cos(pitch), np.sin(pitch)
    cy, sy = np.cos(yaw), np.sin(yaw)

    Rx = np.array([[1, 0, 0], [0, cr, -sr], [0, sr, cr]], dtype=np.float64)
    Ry = np.array([[cp, 0, sp], [0, 1, 0], [-sp, 0, cp]], dtype=np.float64)
    Rz = np.array([[cy, -sy, 0], [sy, cy, 0], [0, 0, 1]], dtype=np.float64)

    return Rz @ Ry @ Rx  # type: ignore[no-any-return]


def camera_rotation(yaw: float, pitch: float, roll: float) -> np.ndarray:
    """
    Compute world→camera rotation matrix from intuitive camera angles.

    Parameters
    ----------
    yaw : float
        Horizontal look direction (radians).  0 = +X (forward), π/2 = +Y (left).
    pitch : float
        Vertical angle from horizontal.  Positive = looking down, 0 = horizontal.
    roll : float
        Twist about the optical axis.  0 = camera "right" is horizontal.

    Returns
    -------
    R : (3,3) ndarray
        Rotation such that X_cam = R @ X_world.
        Row 0 = camera X (right) in world, row 1 = camera Y (down),
        row 2 = camera Z (optical axis).

    Notes
    -----
    World:  X forward, Y left, Z up.
    Camera: Z forward (optical axis), X right, Y down.
    """
    # Optical axis direction in world
    cp = np.cos(pitch)
    d = np.array([np.cos(yaw) * cp, np.sin(yaw) * cp, -np.sin(pitch)], dtype=np.float64)

    # Right direction — horizontal, perpendicular to optical axis XY projection
    r_raw = np.array([np.sin(yaw), -np.cos(yaw), 0.0], dtype=np.float64)

    # Down direction (camera Y) = optical_axis × right
    u = np.cross(d, r_raw)

    R = np.vstack([r_raw, u, d])
    return R  # type: ignore[no-any-return]


def compute_ipm_homography(
    camera_matrix: np.ndarray,
    rotation: np.ndarray,
    translation: np.ndarray,
    canvas_size: int,
    pixels_per_meter: float,
    canvas_center: Optional[Tuple[float, float]] = None,
    ground_z: float = 0.0,
) -> np.ndarray:
    """
    Compute the 3×3 homography mapping image pixels → BEV canvas pixels.

    Parameters
    ----------
    camera_matrix : (3,3) ndarray
        Camera intrinsic matrix K = [[fx, 0, cx], [0, fy, cy], [0, 0, 1]].
    rotation : (3,3) ndarray
        Rotation from *world* (base_link) to *camera optical* frame.
    translation : (3,) ndarray
        Position of camera optical origin in world frame [x, y, z].
    canvas_size : int
        Side length of the square BEV canvas in pixels.
    pixels_per_meter : float
        Scale factor — how many canvas pixels per world metre.
    canvas_center : (float, float) or None
        Canvas pixel coordinate that corresponds to world origin (0,0,0).
        If None, defaults to centre of the canvas.
    ground_z : float
        Height of the ground plane in the world frame (default 0.0).

    Returns
    -------
    H : (3,3) ndarray
        Homography from image (u,v) to BEV canvas (x,y) pixels.

    Notes
    -----
    - World frame is base_link: X forward, Y left, Z up.
    - Camera optical frame follows ROS convention: Z forward (out of lens),
      X right, Y down.
    - The ground plane is Z = ground_z in the world frame.
    - BEV canvas convention: X right = world −Y (right side), Y down = world −X
      (forward), so the robot faces upward on screen.
    """
    K = np.asarray(camera_matrix, dtype=np.float64)
    R = np.asarray(rotation, dtype=np.float64)
    C = np.asarray(translation, dtype=np.float64).reshape(3)

    r1 = R[:, 0]
    r2 = R[:, 1]
    r3 = R[:, 2]

    # Camera extrinsics: X_cam = R @ X_world + t_cam
    # where t_cam = -R @ C, and C is the camera centre in world frame.
    t_cam = -R @ C

    # For a ground-plane point P = [X_w, Y_w, ground_z] in world:
    #   s * [u, v, 1]^T = K * [R | t_cam] * [X_w, Y_w, ground_z, 1]^T
    #                  = K * [r1,  r2,  r3*ground_z + t_cam] * [X_w, Y_w, 1]^T
    M = np.column_stack([r1, r2, r3 * ground_z + t_cam])
    H_w2i = K @ M  # world → image

    # Image → world (ground plane)
    H_i2w = np.linalg.inv(H_w2i)

    # World (metres) → BEV canvas (pixels)
    # Canvas convention: x_canvas = cx - Y_w * ppm,  y_canvas = cy - X_w * ppm
    if canvas_center is None:
        canvas_center = (canvas_size / 2.0, canvas_size / 2.0)
    cx, cy = canvas_center
    ppm = pixels_per_meter

    H_w2c = np.array(
        [
            [0.0, -ppm, cx],
            [-ppm, 0.0, cy],
            [0.0, 0.0, 1.0],
        ],
        dtype=np.float64,
    )

    return H_w2c @ H_i2w  # type: ignore[no-any-return]


# ---------------------------------------------------------------------------
# Preset camera configs — sensible defaults for common layouts
# ---------------------------------------------------------------------------

# Default camera positions for a ~50 cm square robot base.
# All values in base_link frame: X forward, Y left, Z up.
# Cameras are mounted at the perimeter, angled slightly downward.

DEFAULT_CAMERA_CONFIGS: Dict[str, Dict[str, List[float]]] = {
    "front": {
        "position": [0.24, 0.0, 0.12],
        "orientation_rpy": [0.0, 0.35, 0.0],
    },
    "rear": {
        "position": [-0.24, 0.0, 0.12],
        "orientation_rpy": [3.1416, 0.35, 0.0],
    },
    "left": {
        "position": [0.0, 0.17, 0.12],
        "orientation_rpy": [1.5708, 0.35, 0.0],
    },
    "right": {
        "position": [0.0, -0.17, 0.12],
        "orientation_rpy": [-1.5708, 0.35, 0.0],
    },
}

DEFAULT_INTRINSICS: Dict[str, List[float]] = {
    "fx": 320.0,
    "fy": 320.0,
    "cx": 320.0,
    "cy": 240.0,
}


# ---------------------------------------------------------------------------
# ROS 2 IPM calibrator node (requires rclpy)
# ---------------------------------------------------------------------------


class IPMCalibratorNode(_Node if _ROS else object):
    """
    One-shot ROS 2 node that computes homographies from camera poses
    and saves them to a .npz file.
    """

    def __init__(self) -> None:
        if not _ROS:
            raise ImportError(
                "rclpy is required to run IPMCalibratorNode. "
                "Pure IPM functions can be used without ROS."
            )
        _Node.__init__(self, "bev_ipm")

        self.declare_parameter("camera_names", ["front", "rear", "left", "right"])
        self.declare_parameter("canvas_size", 800)
        self.declare_parameter("pixels_per_meter", 80.0)
        self.declare_parameter("ground_z", 0.0)
        self.declare_parameter("output", os.path.expanduser("~/bev_calibration.npz"))

        # Per-camera parameters are declared dynamically below.

        self._names = list(self.get_parameter("camera_names").value)
        self._canvas = self.get_parameter("canvas_size").value
        self._ppm = self.get_parameter("pixels_per_meter").value
        self._ground_z = self.get_parameter("ground_z").value
        self._output = self.get_parameter("output").value

        # Declare per-camera params with defaults from presets
        for name in self._names:
            defaults = DEFAULT_CAMERA_CONFIGS.get(name, {})
            pos_default = defaults.get("position", [0.0, 0.0, 0.1])
            rpy_default = defaults.get("orientation_rpy", [0.0, 0.3, 0.0])
            fx_default = DEFAULT_INTRINSICS["fx"]
            fy_default = DEFAULT_INTRINSICS["fy"]
            cx_default = DEFAULT_INTRINSICS["cx"]
            cy_default = DEFAULT_INTRINSICS["cy"]

            self.declare_parameter(f"{name}.position", pos_default)
            self.declare_parameter(f"{name}.orientation_rpy", rpy_default)
            self.declare_parameter(f"{name}.fx", fx_default)
            self.declare_parameter(f"{name}.fy", fy_default)
            self.declare_parameter(f"{name}.cx", cx_default)
            self.declare_parameter(f"{name}.cy", cy_default)

    def compute_all(self) -> Dict[str, np.ndarray]:
        """Compute homographies for all configured cameras."""
        homographies: Dict[str, np.ndarray] = {}
        for name in self._names:
            pos = list(self.get_parameter(f"{name}.position").value)
            rpy = list(self.get_parameter(f"{name}.orientation_rpy").value)
            fx = self.get_parameter(f"{name}.fx").value
            fy = self.get_parameter(f"{name}.fy").value
            cx_i = self.get_parameter(f"{name}.cx").value
            cy_i = self.get_parameter(f"{name}.cy").value

            K = np.array(
                [[fx, 0.0, cx_i], [0.0, fy, cy_i], [0.0, 0.0, 1.0]],
                dtype=np.float64,
            )
            R = camera_rotation(rpy[0], rpy[1], rpy[2])
            t = np.array(pos, dtype=np.float64)

            H = compute_ipm_homography(
                camera_matrix=K,
                rotation=R,
                translation=t,
                canvas_size=self._canvas,
                pixels_per_meter=self._ppm,
                ground_z=self._ground_z,
            )
            homographies[name] = H
            self.get_logger().info(
                f"[{name}] IPM homography computed "
                f"(pos={[round(v, 3) for v in pos]}, "
                f"rpy={[round(v, 3) for v in rpy]})"
            )
        return homographies

    def save(self, homographies: Dict[str, np.ndarray]) -> None:
        """Save homographies to a .npz file."""
        out_dir = os.path.dirname(os.path.abspath(self._output))
        os.makedirs(out_dir, exist_ok=True)
        np.savez(self._output, **homographies)
        self.get_logger().info(
            f"Saved {len(homographies)} IPM homographies → {self._output}"
        )


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------


def main(args=None) -> None:
    parser = argparse.ArgumentParser(
        description="BEV IPM calibrator — compute homographies from camera poses"
    )
    parser.add_argument(
        "--camera-names",
        nargs="+",
        default=["front", "rear", "left", "right"],
        help="Camera names (default: front rear left right)",
    )
    parser.add_argument(
        "--canvas-size",
        type=int,
        default=800,
        help="BEV canvas width/height in pixels",
    )
    parser.add_argument(
        "--pixels-per-meter",
        type=float,
        default=80.0,
        help="Canvas pixels per world metre",
    )
    parser.add_argument(
        "--ground-z",
        type=float,
        default=0.0,
        help="Ground plane height in world frame (m)",
    )
    parser.add_argument(
        "--output",
        default=os.path.expanduser("~/bev_calibration.npz"),
        help="Output .npz file path",
    )
    # Allow overriding per-camera params on CLI (position, orientation, intrinsics)
    parser.add_argument(
        "--positions",
        nargs="*",
        default=None,
        help=(
            "Per-camera positions: "
            "name:x:y:z name:x:y:z ... "
            "(e.g. front:0.24:0:0.12 rear:-0.24:0:0.12)"
        ),
    )
    parser.add_argument(
        "--orientations",
        nargs="*",
        default=None,
        help=(
            "Per-camera orientations (RPY radians): "
            "name:r:p:y ... "
            "(e.g. front:0:0.35:0 left:0:0.35:1.57)"
        ),
    )
    parser.add_argument(
        "--focal-length",
        type=float,
        default=320.0,
        help="Focal length in pixels (fx = fy)",
    )
    parser.add_argument(
        "--principal-point",
        nargs=2,
        type=float,
        default=[320.0, 240.0],
        help="Principal point cx cy (default: 320 240)",
    )

    parsed, ros_args = parser.parse_known_args(args)
    _rclpy.init(args=ros_args)

    node = IPMCalibratorNode()

    # Override declared params with CLI values
    from rclpy.parameter import Parameter

    node.set_parameters(
        [
            Parameter(
                "camera_names", Parameter.Type.STRING_ARRAY, value=parsed.camera_names
            ),
            Parameter("canvas_size", Parameter.Type.INTEGER, value=parsed.canvas_size),
            Parameter(
                "pixels_per_meter", Parameter.Type.DOUBLE, value=parsed.pixels_per_meter
            ),
            Parameter("ground_z", Parameter.Type.DOUBLE, value=parsed.ground_z),
            Parameter("output", Parameter.Type.STRING, value=parsed.output),
        ]
    )

    # Parse per-camera overrides
    if parsed.positions:
        for entry in parsed.positions:
            parts = entry.split(":")
            if len(parts) == 4:
                name, x, y, z = parts
                node.set_parameters(
                    [
                        Parameter(
                            f"{name}.position",
                            Parameter.Type.DOUBLE_ARRAY,
                            value=[float(x), float(y), float(z)],
                        )
                    ]
                )

    if parsed.orientations:
        for entry in parsed.orientations:
            parts = entry.split(":")
            if len(parts) == 4:
                name, r, p, y = parts
                node.set_parameters(
                    [
                        Parameter(
                            f"{name}.orientation_rpy",
                            Parameter.Type.DOUBLE_ARRAY,
                            value=[float(r), float(p), float(y)],
                        )
                    ]
                )

    for name in parsed.camera_names:
        node.set_parameters(
            [
                Parameter(
                    f"{name}.fx", Parameter.Type.DOUBLE, value=parsed.focal_length
                ),
                Parameter(
                    f"{name}.fy", Parameter.Type.DOUBLE, value=parsed.focal_length
                ),
                Parameter(
                    f"{name}.cx", Parameter.Type.DOUBLE, value=parsed.principal_point[0]
                ),
                Parameter(
                    f"{name}.cy", Parameter.Type.DOUBLE, value=parsed.principal_point[1]
                ),
            ]
        )

    # Compute and save
    homographies = node.compute_all()
    node.save(homographies)

    node.destroy_node()
    _rclpy.shutdown()


if __name__ == "__main__":
    main()
