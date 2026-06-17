#!/usr/bin/env python3
"""
ros2_bev_stitcher — Bird's Eye View compositor for ROS 2.

Subscribes to N perspective cameras, warps each into a shared top-down
ground-plane canvas using per-camera homography matrices, blends
overlapping regions with alpha compositing, and publishes a single BEV image.

ROS Parameters
--------------
camera_names    (string list, default ['front','rear','left','right'])
    Names of the cameras.  Each creates a subscription to
    /camera/<name>/image_raw and expects a homography key <name> in the
    calibration file.

input_topic_pattern  (string, default '/camera/{name}/image_raw')
    Topic pattern — {name} is replaced with each camera name.

output_topic    (string, default '/camera/bev/image_raw')
    Published BEV image topic.

canvas_size     (int, default 800)     Internal compositing canvas (square).
output_width    (int, default 800)     Final output image width.
output_height   (int, default 800)     Final output image height.
src_width       (int, default 640)     Expected input image width.
src_height      (int, default 480)     Expected input image height.
publish_hz      (float, default 30.0)  Publish rate.

calibration_mode (string, default 'auto')
    Homography source: 'checkerboard' | 'ipm' | 'auto'.
    - 'checkerboard': load from calibration_file .npz.
    - 'ipm': compute geometrically from camera pose + intrinsics params.
    - 'auto': try IPM params first, then .npz file, then tiled fallback.

calibration_file (string, default '~/bev_calibration.npz')
    Path to .npz file with per-camera homography matrices.
    Run `ros2 run ros2_bev_stitcher bev_calibrate` or `bev_ipm`
    to generate this file.

IPM parameters (used when calibration_mode is 'ipm' or 'auto'):
    pixels_per_meter (float, default 80.0)  Pixels per world metre on canvas.
    ground_z        (float, default 0.0)    Ground plane height (m).
    <camera>.position        (float[3])  Camera position [x, y, z] in base_link.
    <camera>.orientation_rpy (float[3])  Camera orientation [roll, pitch, yaw] rad.
    <camera>.fx, .fy, .cx, .cy (float)  Camera intrinsics.

output_frame_id (string, default 'bev_frame')  TF frame for output header.
"""

import collections
import os
import statistics as _statistics
import time
from typing import Optional
import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image
from std_msgs.msg import Header

try:
    from cv_bridge import CvBridge

    _CV_BRIDGE = True
except ImportError:
    _CV_BRIDGE = False

try:
    import cv2

    _CV2 = True
except ImportError:
    _CV2 = False

# Local IPM imports (optional — gracefully degrade if not available)
try:
    from ros2_bev_stitcher.bev_ipm import compute_ipm_homography, camera_rotation

    _IPM = True
except ImportError:
    compute_ipm_homography = None  # type: ignore[assignment]
    camera_rotation = None  # type: ignore[assignment]
    _IPM = False

# TF2 lookup (optional)
try:
    from tf2_ros import Buffer, TransformListener
    from tf2_ros.transformations import euler_from_quaternion

    _TF2 = True
except ImportError:
    Buffer = None  # type: ignore[assignment]
    TransformListener = None  # type: ignore[assignment]
    euler_from_quaternion = None  # type: ignore[assignment]
    _TF2 = False

# Camera info (optional — for auto-intrinsics)
try:
    from sensor_msgs.msg import CameraInfo

    _CAM_INFO = True
except ImportError:
    CameraInfo = None  # type: ignore[assignment]
    _CAM_INFO = False


# ---------------------------------------------------------------------------
# Fallback tiled homographies (no calibration)
# ---------------------------------------------------------------------------


def _tiled_homographies(
    names: list,
    canvas: int,
    src_w: int,
    src_h: int,
) -> dict:
    """
    Distribute cameras evenly in a grid across the canvas.
    Works for 1–4 cameras; beyond 4 wraps to a 2-column grid.
    """
    n = len(names)
    cols = min(n, 2)
    rows = (n + cols - 1) // cols
    cell_w = canvas / cols
    cell_h = canvas / rows
    sx = cell_w / src_w
    sy = cell_h / src_h

    Hs = {}
    for i, name in enumerate(names):
        row, col = divmod(i, cols)
        tx = col * cell_w
        ty = row * cell_h
        H = np.array(
            [
                [sx, 0, tx],
                [0, sy, ty],
                [0, 0, 1],
            ],
            dtype=np.float64,
        )
        Hs[name] = H
    return Hs


# ---------------------------------------------------------------------------
# Node
# ---------------------------------------------------------------------------


class BevStitcherNode(Node):
    def __init__(self) -> None:
        super().__init__("bev_stitcher")

        # ── Parameters ──────────────────────────────────────────────────────
        self.declare_parameter("camera_names", ["front", "rear", "left", "right"])
        self.declare_parameter("input_topic_pattern", "/camera/{name}/image_raw")
        self.declare_parameter("output_topic", "/camera/bev/image_raw")
        self.declare_parameter("canvas_size", 800)
        self.declare_parameter("output_width", 800)
        self.declare_parameter("output_height", 800)
        self.declare_parameter("src_width", 640)
        self.declare_parameter("src_height", 480)
        self.declare_parameter("publish_hz", 30.0)
        self.declare_parameter(
            "calibration_file", os.path.expanduser("~/bev_calibration.npz")
        )
        self.declare_parameter("calibration_mode", "auto")
        # IPM parameters
        self.declare_parameter("pixels_per_meter", 80.0)
        self.declare_parameter("ground_z", 0.0)
        # TF2 auto-discovery
        self.declare_parameter("use_tf", True)
        self.declare_parameter("base_frame", "base_link")
        self.declare_parameter("tf_frame_pattern", "{name}_camera_optical_frame")
        # Camera info auto-discovery
        self.declare_parameter("use_camera_info", True)
        self.declare_parameter("camera_info_pattern", "/camera/{name}/camera_info")
        self.declare_parameter("output_frame_id", "bev_frame")
        # Set True to publish rolling stitch timing to /diagnostics at 1 Hz.
        # Zero overhead when False.
        self.declare_parameter("publish_diagnostics", False)

        self._names = list(self.get_parameter("camera_names").value)
        self._topic_pattern = self.get_parameter("input_topic_pattern").value
        self._out_topic = self.get_parameter("output_topic").value
        self._canvas = self.get_parameter("canvas_size").value
        self._out_w = self.get_parameter("output_width").value
        self._out_h = self.get_parameter("output_height").value
        self._src_w = self.get_parameter("src_width").value
        self._src_h = self.get_parameter("src_height").value
        self._hz = self.get_parameter("publish_hz").value
        self._cal_file = self.get_parameter("calibration_file").value
        self._cal_mode = self.get_parameter("calibration_mode").value
        self._ppm = self.get_parameter("pixels_per_meter").value
        self._ground_z = self.get_parameter("ground_z").value
        self._use_tf = self.get_parameter("use_tf").value
        self._base_frame = self.get_parameter("base_frame").value
        self._tf_pattern = self.get_parameter("tf_frame_pattern").value
        self._use_ci = self.get_parameter("use_camera_info").value
        self._ci_pattern = self.get_parameter("camera_info_pattern").value
        self._frame_id = self.get_parameter("output_frame_id").value
        self._diag_enabled = self.get_parameter("publish_diagnostics").value

        # Declare per-camera IPM params with defaults
        _default_camera_configs = {
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
        _default_intrinsics = {"fx": 320.0, "fy": 320.0, "cx": 320.0, "cy": 240.0}

        for name in self._names:
            defaults_cfg = _default_camera_configs.get(name, {})
            pos_default = defaults_cfg.get("position", [0.0, 0.0, 0.1])
            rpy_default = defaults_cfg.get("orientation_rpy", [0.0, 0.3, 0.0])
            self.declare_parameter(f"{name}.position", pos_default)
            self.declare_parameter(f"{name}.orientation_rpy", rpy_default)
            self.declare_parameter(f"{name}.fx", _default_intrinsics["fx"])
            self.declare_parameter(f"{name}.fy", _default_intrinsics["fy"])
            self.declare_parameter(f"{name}.cx", _default_intrinsics["cx"])
            self.declare_parameter(f"{name}.cy", _default_intrinsics["cy"])

        # ── TF2 auto-discovery ──────────────────────────────────────────────
        self._tf_buffer = None
        self._tf_listener = None
        if self._use_tf and _TF2:
            try:
                self._tf_buffer = Buffer()
                self._tf_listener = TransformListener(self._tf_buffer, self)
                self._discover_poses_from_tf()
            except Exception as exc:
                self.get_logger().warn(
                    f"TF2 auto-discovery failed ({exc}) — using param defaults."
                )
        elif self._use_tf and not _TF2:
            self.get_logger().warn("use_tf=True but tf2_ros not available.")

        # ── Camera info auto-discovery ───────────────────────────────────────
        self._camera_intrinsics: dict[str, Optional[np.ndarray]] = {
            n: None for n in self._names
        }
        if self._use_ci and _CAM_INFO:
            for name in self._names:
                ci_topic = self._ci_pattern.replace("{name}", name)
                self.create_subscription(
                    CameraInfo,
                    ci_topic,
                    lambda msg, n=name: self._camera_info_cb(msg, n),
                    10,
                )
                self.get_logger().info(f"Camera info sub: {ci_topic}")
        elif self._use_ci and not _CAM_INFO:
            self.get_logger().warn("use_camera_info=True but CameraInfo not available.")

        # Rolling timing accumulators (active only when _diag_enabled=True)
        self._t_stitch_total: collections.deque = collections.deque(maxlen=100)
        self._t_per_cam: dict[str, collections.deque] = {}

        if not _CV_BRIDGE:
            self.get_logger().error("cv_bridge not available — cannot run.")
            return
        if not _CV2:
            self.get_logger().error("OpenCV not available — cannot run.")
            return

        self._bridge = CvBridge()

        # ── Homographies ─────────────────────────────────────────────────────
        self._homographies, self._calibrated = self._load_homographies()
        self._blend_weights = self._compute_blend_weights()

        # ── Image buffers ────────────────────────────────────────────────────
        self._images = {n: None for n in self._names}

        # ── Subscribers ──────────────────────────────────────────────────────
        for name in self._names:
            topic = self._topic_pattern.replace("{name}", name)
            self.create_subscription(
                Image, topic, lambda msg, n=name: self._img_cb(msg, n), 10
            )
            self.get_logger().info(f"Subscribed: {topic}")

        # ── Publisher ────────────────────────────────────────────────────────
        self._pub = self.create_publisher(Image, self._out_topic, 10)

        # ── Per-camera timing deques (one per camera name) ───────────────────
        for name in self._names:
            self._t_per_cam[name] = collections.deque(maxlen=100)

        # ── Timer ────────────────────────────────────────────────────────────
        self.create_timer(1.0 / self._hz, self._timer_cb)

        if self._diag_enabled:
            from diagnostic_msgs.msg import DiagnosticArray, DiagnosticStatus, KeyValue

            self._DiagnosticArray = DiagnosticArray
            self._DiagnosticStatus = DiagnosticStatus
            self._KeyValue = KeyValue
            self._diag_pub = self.create_publisher(DiagnosticArray, "/diagnostics", 10)
            self.create_timer(1.0, self._publish_diagnostics)

        self.get_logger().info(
            f"BevStitcherNode ready | cameras={self._names} "
            f"| canvas={self._canvas} | {self._out_w}×{self._out_h} "
            f"@ {self._hz}Hz | calibration="
            f"{'loaded' if self._calibrated else 'tiled fallback'}"
            f" | mode={self._cal_mode}"
        )

    # ── Homography helpers ───────────────────────────────────────────────────

    def _discover_poses_from_tf(self) -> None:
        """
        Look up camera transforms from TF and override per-camera params.

        Waits briefly for transforms to become available.  If a transform
        cannot be found, the parameter default is kept.
        """
        if self._tf_buffer is None:
            return

        discovered = 0

        for name in self._names:
            tf_frame = self._tf_pattern.replace("{name}", name)
            try:
                t = self._tf_buffer.lookup_transform(
                    self._base_frame,
                    tf_frame,
                    rclpy.time.Time(),
                    timeout=rclpy.duration.Duration(seconds=2.0),
                )
                pos = t.transform.translation
                quat = t.transform.rotation

                # Override position param
                from rclpy.parameter import Parameter

                self.set_parameters(
                    [
                        Parameter(
                            f"{name}.position",
                            Parameter.Type.DOUBLE_ARRAY,
                            value=[pos.x, pos.y, pos.z],
                        )
                    ]
                )

                # Convert quaternion to yaw/pitch/roll for camera_rotation
                if euler_from_quaternion is not None:
                    e = euler_from_quaternion([quat.x, quat.y, quat.z, quat.w])
                    # euler_from_quaternion returns (roll, pitch, yaw) in the
                    # standard ROS convention.  We need (yaw, pitch, roll) for
                    # camera_rotation.
                    yaw, pitch, roll = float(e[2]), float(e[1]), float(e[0])
                else:
                    # Fallback: compute approximate RPY from quaternion
                    import math

                    qx, qy, qz, qw = quat.x, quat.y, quat.z, quat.w
                    sinr = 2.0 * (qw * qx + qy * qz)
                    cosr = 1.0 - 2.0 * (qx * qx + qy * qy)
                    roll = math.atan2(sinr, cosr)
                    sinp = 2.0 * (qw * qy - qz * qx)
                    pitch = math.asin(np.clip(sinp, -1.0, 1.0))
                    siny = 2.0 * (qw * qz + qx * qy)
                    cosy = 1.0 - 2.0 * (qy * qy + qz * qz)
                    yaw = math.atan2(siny, cosy)

                self.set_parameters(
                    [
                        Parameter(
                            f"{name}.orientation_rpy",
                            Parameter.Type.DOUBLE_ARRAY,
                            value=[yaw, pitch, roll],
                        )
                    ]
                )

                discovered += 1
                self.get_logger().info(
                    f"[{name}] TF pose from '{self._base_frame}' → "
                    f"'{tf_frame}': pos=({pos.x:.3f},{pos.y:.3f},{pos.z:.3f}) "
                    f"rpy=({yaw:.3f},{pitch:.3f},{roll:.3f})"
                )
            except Exception as exc:
                self.get_logger().info(
                    f"[{name}] TF lookup failed for '{tf_frame}' "
                    f"({exc}) — keeping param default."
                )

        self.get_logger().info(
            f"TF auto-discovery: {discovered}/{len(self._names)} cameras found."
        )

    def _camera_info_cb(self, msg, name: str) -> None:
        """Store camera intrinsics from CameraInfo — one-shot per camera."""
        if self._camera_intrinsics[name] is not None:
            return  # already captured
        K = np.array(msg.k).reshape(3, 3)
        self._camera_intrinsics[name] = K
        self.get_logger().info(
            f"[{name}] intrinsics from camera_info: "
            f"fx={K[0, 0]:.1f} fy={K[1, 1]:.1f} "
            f"cx={K[0, 2]:.1f} cy={K[1, 2]:.1f}"
        )
        from rclpy.parameter import Parameter

        self.set_parameters(
            [
                Parameter(f"{name}.fx", Parameter.Type.DOUBLE, value=float(K[0, 0])),
                Parameter(f"{name}.fy", Parameter.Type.DOUBLE, value=float(K[1, 1])),
                Parameter(f"{name}.cx", Parameter.Type.DOUBLE, value=float(K[0, 2])),
                Parameter(f"{name}.cy", Parameter.Type.DOUBLE, value=float(K[1, 2])),
            ]
        )

        # If in IPM/auto mode and all intrinsics are now available, refresh
        if self._cal_mode in ("ipm", "auto") and _IPM:
            if all(self._camera_intrinsics[n] is not None for n in self._names):
                self._refresh_ipm_homographies()

    def _refresh_ipm_homographies(self) -> None:
        """Recompute IPM homographies and blend weights from current params."""
        Hs = self._compute_ipm_homographies_raw()
        if Hs:
            self._homographies = Hs
            self._blend_weights = self._compute_blend_weights()
            self._calibrated = True
            self.get_logger().info("Homographies refreshed from camera_info intrinsics")

    def _load_homographies(self):
        """
        Load or compute homographies based on calibration_mode.

        Returns (homographies_dict, is_calibrated_bool).
        """
        mode = self._cal_mode

        # IPM mode: compute geometrically from camera poses
        if mode == "ipm":
            if not _IPM:
                self.get_logger().error(
                    "calibration_mode='ipm' but bev_ipm module not available. "
                    "Falling back to tiled."
                )
                return _tiled_homographies(
                    self._names, self._canvas, self._src_w, self._src_h
                ), False
            return self._compute_ipm_homographies(), True

        # Checkerboard mode: load from .npz file only
        if mode == "checkerboard":
            return self._load_checkerboard_homographies()

        # Auto mode: saved .npz first, then IPM defaults, then tiled
        if mode == "auto":
            # Prefer a saved calibration (checkerboard or IPM-saved)
            Hs, calibrated = self._load_checkerboard_homographies()
            if calibrated:
                self.get_logger().info("Auto: using saved .npz calibration")
                return Hs, calibrated

            # Fall back to geometric IPM with default/provided camera poses
            if _IPM:
                try:
                    Hs = self._compute_ipm_homographies_raw()
                    if Hs:
                        self.get_logger().info(
                            "Auto: using IPM defaults (no saved calibration found). "
                            "Run bev_cal_ui to fine-tune or bev_ipm to regenerate."
                        )
                        return Hs, True
                except Exception as exc:
                    self.get_logger().info(
                        f"Auto: IPM failed ({exc}), using tiled fallback."
                    )

        # Fallback
        self.get_logger().warn(
            f"No calibration available (mode={mode}) — using tiled fallback."
        )
        return _tiled_homographies(
            self._names, self._canvas, self._src_w, self._src_h
        ), False

    def _load_checkerboard_homographies(self):
        """Load homographies from a checkerboard .npz file."""
        if os.path.isfile(self._cal_file):
            try:
                data = np.load(self._cal_file)
                Hs = {n: data[n] for n in self._names}
                self.get_logger().info(f"Loaded BEV calibration from {self._cal_file}")
                return Hs, True
            except Exception as exc:
                self.get_logger().warn(
                    f"Calibration load failed ({exc}) — using tiled fallback."
                )
        return _tiled_homographies(
            self._names, self._canvas, self._src_w, self._src_h
        ), False

    def _compute_ipm_homographies_raw(self) -> dict:
        """
        Compute IPM homographies from current ROS parameters.
        Returns empty dict if any camera is missing position/orientation params.
        """
        Hs = {}
        for name in self._names:
            pos_val = self.get_parameter(f"{name}.position").value
            rpy_val = self.get_parameter(f"{name}.orientation_rpy").value
            fx = self.get_parameter(f"{name}.fx").value
            fy = self.get_parameter(f"{name}.fy").value
            cx_i = self.get_parameter(f"{name}.cx").value
            cy_i = self.get_parameter(f"{name}.cy").value

            if pos_val is None or rpy_val is None:
                return {}

            K = np.array(
                [[fx, 0.0, cx_i], [0.0, fy, cy_i], [0.0, 0.0, 1.0]],
                dtype=np.float64,
            )
            R = camera_rotation(rpy_val[0], rpy_val[1], rpy_val[2])
            t = np.array(pos_val, dtype=np.float64)

            H = compute_ipm_homography(
                camera_matrix=K,
                rotation=R,
                translation=t,
                canvas_size=self._canvas,
                pixels_per_meter=self._ppm,
                ground_z=self._ground_z,
            )
            Hs[name] = H
        return Hs

    def _compute_ipm_homographies(self):
        """Compute IPM homographies and log the result."""
        Hs = self._compute_ipm_homographies_raw()
        if not Hs:
            self.get_logger().warn(
                "IPM calibration requested but camera pose params not set. "
                "Using tiled fallback."
            )
            return _tiled_homographies(
                self._names, self._canvas, self._src_w, self._src_h
            ), False
        self.get_logger().info(f"Computed {len(Hs)} IPM homographies from camera poses")
        return Hs, True

    def _compute_blend_weights(self) -> dict:
        weights = {}
        src_ones = np.ones((self._src_h, self._src_w), dtype=np.float32)
        for name in self._names:
            H = self._homographies[name]
            w = cv2.warpPerspective(src_ones, H, (self._canvas, self._canvas))
            weights[name] = w
        return weights

    # ── Callbacks ────────────────────────────────────────────────────────────

    def _img_cb(self, msg: Image, name: str) -> None:
        try:
            img = self._bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            self._images[name] = np.asarray(img, dtype=np.uint8)
        except Exception as exc:
            self.get_logger().warn(
                f"[{name}] image error: {exc}", throttle_duration_sec=5.0
            )

    def _timer_cb(self) -> None:
        _t0 = time.perf_counter() if self._diag_enabled else None
        canvas = np.zeros((self._canvas, self._canvas, 3), dtype=np.float32)
        w_sum = np.zeros((self._canvas, self._canvas, 1), dtype=np.float32)
        any_img = False

        for name in self._names:
            img = self._images[name]
            if img is None:
                continue
            any_img = True

            _tc = time.perf_counter() if self._diag_enabled else None

            if img.shape[1] != self._src_w or img.shape[0] != self._src_h:
                sx = self._src_w / img.shape[1]
                sy = self._src_h / img.shape[0]
                img = cv2.resize(img, (self._src_w, self._src_h))
                # Scale the homography to account for the resize:
                # H maps original-image pixels to BEV canvas.
                # Resized pixel (u',v') maps to original pixel (u'*sx, v'*sy).
                # So H_scaled = H @ diag(sx, sy, 1).
                H = self._homographies[name].copy()
                H[:, 0] *= sx
                H[:, 1] *= sy
            else:
                H = self._homographies[name]

            warped = cv2.warpPerspective(
                img.astype(np.float32),
                H,
                (self._canvas, self._canvas),
            )

            if self._diag_enabled and _tc is not None:
                self._t_per_cam[name].append((time.perf_counter() - _tc) * 1000.0)

            w = self._blend_weights[name][:, :, np.newaxis]
            canvas += warped * w
            w_sum += w

        if not any_img:
            self.get_logger().warn(
                "No camera images received yet.", throttle_duration_sec=5.0
            )
            return

        mask = w_sum[:, :, 0] > 0
        canvas[mask] /= w_sum[mask]

        out = canvas.clip(0, 255).astype(np.uint8)
        if self._out_w != self._canvas or self._out_h != self._canvas:
            out = cv2.resize(out, (self._out_w, self._out_h))

        if self._diag_enabled and _t0 is not None:
            self._t_stitch_total.append((time.perf_counter() - _t0) * 1000.0)

        try:
            msg = self._bridge.cv2_to_imgmsg(out, encoding="rgb8")
            msg.header = Header()
            msg.header.stamp = self.get_clock().now().to_msg()
            msg.header.frame_id = self._frame_id
            self._pub.publish(msg)
        except Exception as exc:
            self.get_logger().error(f"Publish error: {exc}", throttle_duration_sec=5.0)

    # ── Diagnostics helper ───────────────────────────────────────────────────

    def _publish_diagnostics(self) -> None:
        msg = self._DiagnosticArray()
        msg.header.stamp = self.get_clock().now().to_msg()
        statuses = []

        def _make(name, deque_):
            st = self._DiagnosticStatus()
            st.name = name
            if not deque_:
                st.level = self._DiagnosticStatus.OK
                st.message = "no data"
                return st
            s = sorted(deque_)
            n = len(s)
            p95 = s[max(0, int(0.95 * n) - 1)]
            st.level = (
                self._DiagnosticStatus.ERROR
                if p95 > 50.0
                else self._DiagnosticStatus.WARN
                if p95 > 33.0
                else self._DiagnosticStatus.OK
            )
            st.message = f"p95={p95:.2f}ms"
            for k, v in [
                ("mean_ms", _statistics.mean(s)),
                ("p50_ms", s[n // 2]),
                ("p95_ms", p95),
                ("max_ms", s[-1]),
            ]:
                kv = self._KeyValue()
                kv.key = k
                kv.value = f"{v:.3f}"
                st.values.append(kv)
            return st

        statuses.append(_make("bev_stitcher/total_stitch_ms", self._t_stitch_total))
        for name in self._names:
            statuses.append(
                _make(f"bev_stitcher/warp_{name}_ms", self._t_per_cam[name])
            )
        msg.status = statuses
        self._diag_pub.publish(msg)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main(args=None) -> None:
    rclpy.init(args=args)
    node = BevStitcherNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
