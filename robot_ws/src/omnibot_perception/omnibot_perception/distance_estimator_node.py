#!/usr/bin/env python3
"""
Distance estimator node.

Priority order for obstacle/object distance:
  1. Depth camera  – per-pixel median in a bbox patch (accurate, directional)
  2. Ultrasonic    – forward-facing ground truth (sensor_msgs/Range)
  3. Bbox-size heuristic – fallback when neither sensor is available

Publishes:
  /perception/nearest_obstacle_m   Float32  – nearest forward obstacle
  /perception/enriched_detections  String   – JSON detections enriched with
                                              distance_m + sensor_source
  /perception/ultrasonic_range_m   Float32  – raw ultrasonic reading (if sensor present)
"""

import json
import threading

import numpy as np
import rclpy
from cv_bridge import CvBridge
from rclpy.node import Node
from sensor_msgs.msg import CameraInfo, Image, Range
from std_msgs.msg import Float32, String


class DistanceEstimatorNode(Node):
    # Reference object half-heights (m) for bbox-size fallback (80 px ≈ 0.5 m ref)
    _BBOX_REF_PX = 80.0
    _BBOX_REF_M = 0.5

    def __init__(self):
        super().__init__("distance_estimator_node")

        self.declare_parameter("depth_topic", "/camera/depth/image_raw")
        self.declare_parameter("depth_info_topic", "/camera/depth/camera_info")
        self.declare_parameter("ultrasonic_topic", "/sensors/ultrasonic/range")
        self.declare_parameter("detections_topic", "/perception/detections")
        self.declare_parameter("min_depth_m", 0.10)
        self.declare_parameter("max_depth_m", 5.0)
        self.declare_parameter("publish_hz", 10.0)
        self.declare_parameter("ultrasonic_fov_deg", 15.0)  # sensor cone half-angle

        p = self.get_parameter
        self._min_d = p("min_depth_m").value
        self._max_d = p("max_depth_m").value
        self._us_fov = p("ultrasonic_fov_deg").value

        self._lock = threading.Lock()
        self._bridge = CvBridge()
        self._depth: np.ndarray | None = None
        self._cam_info: dict | None = None
        self._ultrasonic_m: float | None = None

        # ── Subscriptions ─────────────────────────────────────────────────────
        self.create_subscription(Image, p("depth_topic").value, self._depth_cb, 1)
        self.create_subscription(
            CameraInfo, p("depth_info_topic").value, self._info_cb, 1
        )
        self.create_subscription(Range, p("ultrasonic_topic").value, self._us_cb, 10)
        self.create_subscription(String, p("detections_topic").value, self._dets_cb, 10)

        # ── Publications ──────────────────────────────────────────────────────
        self._pub_nearest = self.create_publisher(
            Float32, "/perception/nearest_obstacle_m", 10
        )
        self._pub_enriched = self.create_publisher(
            String, "/perception/enriched_detections", 10
        )
        self._pub_us = self.create_publisher(
            Float32, "/perception/ultrasonic_range_m", 10
        )

        hz = max(1.0, p("publish_hz").value)
        self.create_timer(1.0 / hz, self._timer_cb)
        self.get_logger().info("DistanceEstimatorNode ready")

    # ── Callbacks ─────────────────────────────────────────────────────────────

    def _depth_cb(self, msg: Image) -> None:
        with self._lock:
            self._depth = self._bridge.imgmsg_to_cv2(
                msg, desired_encoding="passthrough"
            )

    def _info_cb(self, msg: CameraInfo) -> None:
        if self._cam_info is None:
            self._cam_info = {
                "fx": msg.k[0],
                "fy": msg.k[4],
                "cx": msg.k[2],
                "cy": msg.k[5],
                "w": msg.width,
                "h": msg.height,
            }

    def _us_cb(self, msg: Range) -> None:
        if msg.min_range <= msg.range <= msg.max_range:
            with self._lock:
                self._ultrasonic_m = float(msg.range)
            out = Float32()
            out.data = float(msg.range)
            self._pub_us.publish(out)

    def _dets_cb(self, msg: String) -> None:
        """Enrich detections that don't yet carry a depth-based distance."""
        try:
            dets: list[dict] = json.loads(msg.data)
        except (json.JSONDecodeError, ValueError):
            return

        with self._lock:
            depth = self._depth.copy() if self._depth is not None else None
            us = self._ultrasonic_m

        changed = False
        for det in dets:
            if "distance_m" in det:
                continue  # already set by perception_node
            cx, cy = det.get("center", [0, 0])
            x1, y1, x2, y2 = det.get("bbox", [0, 0, 0, 0])
            bbox_h = max(1, y2 - y1)

            if depth is not None:
                d = self._depth_sample(depth, cx, cy)
                if d > 0:
                    det["distance_m"] = round(d, 3)
                    det["sensor_source"] = "depth_camera"
                    changed = True
                    continue

            # Ultrasonic: only trust it when the detection is near image centre
            if us is not None and self._cam_info:
                img_cx = self._cam_info["cx"]
                off_ratio = abs(cx - img_cx) / max(1, self._cam_info["w"] / 2)
                if off_ratio < 0.3:  # within ~30 % of centre
                    det["distance_m"] = round(us, 3)
                    det["sensor_source"] = "ultrasonic"
                    changed = True
                    continue

            # Fallback: angular-size heuristic (rough estimate)
            estimated = (self._BBOX_REF_PX / bbox_h) * self._BBOX_REF_M
            if self._min_d < estimated < self._max_d:
                det["distance_m"] = round(estimated, 3)
                det["sensor_source"] = "bbox_heuristic"
                changed = True

        if changed:
            out = String()
            out.data = json.dumps(dets)
            self._pub_enriched.publish(out)

    # ── Timer: publish nearest obstacle ───────────────────────────────────────

    def _timer_cb(self) -> None:
        with self._lock:
            depth = self._depth.copy() if self._depth is not None else None
            us = self._ultrasonic_m

        nearest = float("inf")

        if depth is not None:
            h, w = depth.shape[:2]
            # Forward-looking centre column (35–65 %) ignoring floor (top 75 %)
            strip = depth[: int(h * 0.75), int(w * 0.35) : int(w * 0.65)]
            mm_min = self._min_d * 1000
            mm_max = self._max_d * 1000
            valid = strip[(strip > mm_min) & (strip < mm_max)]
            if valid.size:
                nearest = float(np.percentile(valid, 5)) / 1000.0

        if us is not None:
            nearest = min(nearest, us)

        if nearest != float("inf"):
            msg = Float32()
            msg.data = float(np.clip(nearest, self._min_d, self._max_d))
            self._pub_nearest.publish(msg)

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _depth_sample(self, depth: np.ndarray, cx: int, cy: int, r: int = 5) -> float:
        h, w = depth.shape[:2]
        if not (0 <= cy < h and 0 <= cx < w):
            return 0.0
        patch = depth[
            max(0, cy - r) : min(h, cy + r + 1), max(0, cx - r) : min(w, cx + r + 1)
        ]
        valid = patch[(patch > self._min_d * 1000) & (patch < self._max_d * 1000)]
        return float(np.median(valid)) / 1000.0 if valid.size else 0.0


def main(args=None) -> None:
    rclpy.init(args=args)
    node = DistanceEstimatorNode()
    try:
        rclpy.spin(node)
    finally:
        node.destroy_node()
        rclpy.shutdown()
