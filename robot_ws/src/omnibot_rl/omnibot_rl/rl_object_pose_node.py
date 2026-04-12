#!/usr/bin/env python3
"""
RL Object Pose Node — ArUco-based object pose estimator for RL arm deployment.

Bridges the gap between the RL policy's ground-truth object pose assumption
(used during Isaac Lab training) and real-hardware deployment.

Phase 1 (initial): Uses ArUco markers attached to objects for reliable,
accurate pose estimation. Place a 50 mm ArUco marker (dictionary DICT_4X4_50,
marker ID 0) on the top face of the target object.

Phase 2 (future): Replace ArUco with a lightweight colour-blob or depth-based
centroid estimator once the arm policy is validated on hardware.

The estimated pose is published in the base_link frame so rl_arm_node can
use it directly as the 'target_pos' component of the observation vector.

Topics
──────
  Subscribed:
    /camera/wrist/image_raw    sensor_msgs/Image
    /camera/depth/image_raw    sensor_msgs/Image  (mono16, mm)

  Published:
    /rl_arm/target_pose        geometry_msgs/PoseStamped  (base_link frame)
    /rl_arm/target_detected    std_msgs/Bool

Parameters
──────────
  aruco_dict_id  : int   = 4  (cv2.aruco.DICT_4X4_50)
  marker_id      : int   = 0
  marker_size_m  : float = 0.05  (50 mm marker side length)
  camera_frame   : str   = 'wrist_camera_link'
  base_frame     : str   = 'base_link'
  fx, fy, cx, cy : float = pinhole camera intrinsics (set via YAML or CameraInfo)
  depth_scale_mm : float = 1.0  (depth image pixel → metres)
  publish_hz     : float = 20.0

Notes
─────
  - Camera intrinsics are loaded from /camera/wrist/camera_info if available.
    Override with YAML parameters when camera_info is unavailable.
  - TF lookup (wrist_camera_link → base_link) requires the URDF to be loaded
    and robot_state_publisher to be running.
  - If ArUco detection fails the node publishes detected=False and the last
    known pose (held for up to 0.5 s, then zeroed).
"""

import math
import time

import numpy as np
import rclpy
from geometry_msgs.msg import PoseStamped
from rclpy.node import Node
from sensor_msgs.msg import CameraInfo, Image
from std_msgs.msg import Bool

try:
    import cv2
    import cv2.aruco as aruco
    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

try:
    from cv_bridge import CvBridge
    CV_BRIDGE_AVAILABLE = True
except ImportError:
    CV_BRIDGE_AVAILABLE = False

try:
    from tf2_ros import Buffer, TransformListener
    import tf2_geometry_msgs  # noqa: F401 — needed for do_transform_pose
    TF2_AVAILABLE = True
except ImportError:
    TF2_AVAILABLE = False


class RLObjectPoseNode(Node):
    """
    Estimates the 3D pose of an ArUco-marked target object and publishes
    it in the base_link frame for consumption by rl_arm_node.
    """

    def __init__(self):
        super().__init__('rl_object_pose_node')

        # ── Parameters ───────────────────────────────────────────────────────
        self.declare_parameter('aruco_dict_id',  4)      # DICT_4X4_50
        self.declare_parameter('marker_id',      0)
        self.declare_parameter('marker_size_m',  0.05)   # 50 mm
        self.declare_parameter('camera_frame',   'wrist_camera_link')
        self.declare_parameter('base_frame',     'base_link')
        self.declare_parameter('fx',             250.0)  # fallback intrinsics
        self.declare_parameter('fy',             250.0)
        self.declare_parameter('cx',             160.0)
        self.declare_parameter('cy',             120.0)
        self.declare_parameter('depth_scale_mm', 1.0)
        self.declare_parameter('publish_hz',     20.0)
        self.declare_parameter('pose_hold_s',    0.5)

        self._marker_id   = self.get_parameter('marker_id').value
        self._marker_size = self.get_parameter('marker_size_m').value
        self._cam_frame   = self.get_parameter('camera_frame').value
        self._base_frame  = self.get_parameter('base_frame').value
        self._depth_scale = self.get_parameter('depth_scale_mm').value
        self._pose_hold   = self.get_parameter('pose_hold_s').value
        hz                = self.get_parameter('publish_hz').value

        # Camera intrinsics (updated from CameraInfo if available)
        self._fx = self.get_parameter('fx').value
        self._fy = self.get_parameter('fy').value
        self._cx = self.get_parameter('cx').value
        self._cy = self.get_parameter('cy').value

        # ArUco setup
        self._aruco_detector = None
        if CV2_AVAILABLE:
            dict_id = self.get_parameter('aruco_dict_id').value
            try:
                aruco_dict = aruco.getPredefinedDictionary(dict_id)
                aruco_params = aruco.DetectorParameters()
                self._aruco_detector = aruco.ArucoDetector(aruco_dict, aruco_params)
                self.get_logger().info(
                    f'ArUco detector ready. dict_id={dict_id}, '
                    f'marker_id={self._marker_id}, '
                    f'marker_size={self._marker_size*100:.0f} cm')
            except Exception as exc:
                self.get_logger().error(f'Failed to init ArUco detector: {exc}')
        else:
            self.get_logger().warn(
                'OpenCV not available. Install: pip install opencv-contrib-python')

        self._bridge = CvBridge() if CV_BRIDGE_AVAILABLE else None

        # TF2 for transforming camera_frame → base_link
        self._tf_buffer = None
        self._tf_listener = None
        if TF2_AVAILABLE:
            self._tf_buffer = Buffer()
            self._tf_listener = TransformListener(self._tf_buffer, self)

        # ── State ────────────────────────────────────────────────────────────
        self._last_pose: PoseStamped | None = None
        self._last_detect_time: float = 0.0
        self._rgb_img: np.ndarray | None = None
        self._depth_img: np.ndarray | None = None

        # ── Subscriptions ────────────────────────────────────────────────────
        self.create_subscription(Image,      '/camera/wrist/image_raw',   self._rgb_cb,   10)
        self.create_subscription(Image,      '/camera/depth/image_raw',   self._depth_cb, 10)
        self.create_subscription(CameraInfo, '/camera/wrist/camera_info', self._info_cb,  10)

        # ── Publishers ───────────────────────────────────────────────────────
        self._pose_pub     = self.create_publisher(PoseStamped, '/rl_arm/target_pose',     10)
        self._detected_pub = self.create_publisher(Bool,        '/rl_arm/target_detected', 10)

        # ── Timer ────────────────────────────────────────────────────────────
        self.create_timer(1.0 / hz, self._detect_and_publish)

        self.get_logger().info('RLObjectPoseNode ready. Publishes /rl_arm/target_pose.')

    # ── Callbacks ────────────────────────────────────────────────────────────

    def _info_cb(self, msg: CameraInfo) -> None:
        """Update intrinsics from CameraInfo (preferred over YAML params)."""
        if msg.k[0] > 0.0:
            self._fx = msg.k[0]
            self._fy = msg.k[4]
            self._cx = msg.k[2]
            self._cy = msg.k[5]

    def _rgb_cb(self, msg: Image) -> None:
        if self._bridge is None:
            return
        try:
            self._rgb_img = self._bridge.imgmsg_to_cv2(msg, desired_encoding='bgr8')
        except Exception:
            pass

    def _depth_cb(self, msg: Image) -> None:
        if self._bridge is None:
            return
        try:
            self._depth_img = self._bridge.imgmsg_to_cv2(msg, desired_encoding='passthrough')
        except Exception:
            pass

    # ── Detection ────────────────────────────────────────────────────────────

    def _detect_and_publish(self) -> None:
        detected = False
        pose_cam: np.ndarray | None = None  # 3D position in camera frame

        if (CV2_AVAILABLE and self._aruco_detector is not None
                and self._rgb_img is not None):
            pose_cam = self._detect_aruco(self._rgb_img)
            if pose_cam is not None:
                detected = True
                self._last_detect_time = time.monotonic()

        # Hold last known pose for up to pose_hold_s
        if not detected and self._last_pose is not None:
            elapsed = time.monotonic() - self._last_detect_time
            if elapsed < self._pose_hold:
                detected = True  # publish stale pose

        # Build and publish PoseStamped in base_link frame
        if detected and pose_cam is not None:
            pose_base = self._transform_to_base(pose_cam)
            if pose_base is not None:
                self._last_pose = pose_base
                self._pose_pub.publish(pose_base)
        elif detected and self._last_pose is not None:
            self._pose_pub.publish(self._last_pose)

        det_msg = Bool()
        det_msg.data = detected
        self._detected_pub.publish(det_msg)

    def _detect_aruco(self, bgr: np.ndarray) -> np.ndarray | None:
        """
        Run ArUco detection and return the marker centre position in the
        camera coordinate frame (x right, y down, z forward in metres).
        Returns None if the target marker is not detected.
        """
        gray = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY)
        corners, ids, _ = self._aruco_detector.detectMarkers(gray)

        if ids is None:
            return None

        # Find our target marker
        for i, mid in enumerate(ids.flatten()):
            if mid != self._marker_id:
                continue

            # Estimate pose via solvePnP
            obj_pts = np.array([
                [-self._marker_size / 2,  self._marker_size / 2, 0],
                [ self._marker_size / 2,  self._marker_size / 2, 0],
                [ self._marker_size / 2, -self._marker_size / 2, 0],
                [-self._marker_size / 2, -self._marker_size / 2, 0],
            ], dtype=np.float32)

            cam_matrix = np.array([
                [self._fx, 0,        self._cx],
                [0,        self._fy, self._cy],
                [0,        0,        1       ],
            ], dtype=np.float32)
            dist_coeffs = np.zeros(5, dtype=np.float32)

            img_pts = corners[i].reshape(4, 2).astype(np.float32)
            ok, rvec, tvec = cv2.solvePnP(
                obj_pts, img_pts, cam_matrix, dist_coeffs,
                flags=cv2.SOLVEPNP_IPPE_SQUARE)

            if not ok:
                return None

            # Use depth image to refine Z if available
            pos_cam = tvec.flatten()  # (x, y, z) in camera frame (metres)
            if self._depth_img is not None:
                pos_cam = self._refine_z_from_depth(img_pts, pos_cam)

            return pos_cam

        return None

    def _refine_z_from_depth(
            self, img_pts: np.ndarray, pos_cam: np.ndarray) -> np.ndarray:
        """Replace Z estimate from solvePnP with depth image sample at marker centre."""
        cx = float(np.mean(img_pts[:, 0]))
        cy = float(np.mean(img_pts[:, 1]))
        ix, iy = int(round(cx)), int(round(cy))
        h, w = self._depth_img.shape[:2]
        if 0 <= ix < w and 0 <= iy < h:
            depth_val = float(self._depth_img[iy, ix])
            if depth_val > 0:
                z_m = depth_val * self._depth_scale / 1000.0  # mm → m
                pos_cam = pos_cam.copy()
                pos_cam[2] = z_m
        return pos_cam

    def _transform_to_base(self, pos_cam: np.ndarray) -> PoseStamped | None:
        """Transform 3D point from camera frame to base_link frame via TF2."""
        pose_cam = PoseStamped()
        pose_cam.header.frame_id = self._cam_frame
        pose_cam.header.stamp = self.get_clock().now().to_msg()
        pose_cam.pose.position.x = float(pos_cam[0])
        pose_cam.pose.position.y = float(pos_cam[1])
        pose_cam.pose.position.z = float(pos_cam[2])
        pose_cam.pose.orientation.w = 1.0

        if self._tf_buffer is None:
            # No TF available — return as-is with base_link frame label
            pose_cam.header.frame_id = self._base_frame
            return pose_cam

        try:
            transform = self._tf_buffer.lookup_transform(
                self._base_frame,
                self._cam_frame,
                rclpy.time.Time(),
                timeout=rclpy.duration.Duration(seconds=0.1))
            import tf2_geometry_msgs
            pose_base = tf2_geometry_msgs.do_transform_pose_stamped(
                pose_cam, transform)
            return pose_base
        except Exception as exc:
            self.get_logger().warn(
                f'TF lookup {self._cam_frame}→{self._base_frame} failed: {exc}',
                throttle_duration_sec=5.0)
            return None


def main(args=None):
    rclpy.init(args=args)
    node = RLObjectPoseNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
