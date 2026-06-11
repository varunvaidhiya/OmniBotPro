#!/usr/bin/env python3
"""
Unified perception node: detection → tracking → segmentation.

Modes (set via /perception/mode):
  detect          – YOLO bounding-box inference, no ID persistence
  track           – ByteTrack persistent IDs on top of detection (default)
  segment         – Instance-segmentation masks (yolov8n-seg or FastSAM)
  vla             – Segment mode + signals VlaTriggerNode to hand off to SmolVLA

Depth integration:
  When /camera/depth/image_raw is available each detection is enriched with
  distance_m and position_3d (camera optical frame).

Published topics mirroring existing RL interfaces:
  /rl_arm/target_pose    – PoseStamped  (same as rl_object_pose_node, replaced)
  /rl_arm/target_detected – Bool
"""

import json
import threading

import cv2
import numpy as np
import rclpy
from rclpy.node import Node
from cv_bridge import CvBridge
from geometry_msgs.msg import PoseStamped
from sensor_msgs.msg import CameraInfo, Image
from std_msgs.msg import Bool, Float32, String

try:
    from ultralytics import YOLO

    _ULTRA_OK = True
except ImportError:
    _ULTRA_OK = False


class PerceptionNode(Node):
    MODE_DETECT = "detect"
    MODE_TRACK = "track"
    MODE_SEGMENT = "segment"
    MODE_VLA = "vla"
    _VALID_MODES = {MODE_DETECT, MODE_TRACK, MODE_SEGMENT, MODE_VLA}

    def __init__(self):
        super().__init__("perception_node")

        # ── Parameters ────────────────────────────────────────────────────────
        self.declare_parameter("front_camera_topic", "/camera/front/image_raw")
        self.declare_parameter("wrist_camera_topic", "/camera/wrist/image_raw")
        self.declare_parameter("depth_topic", "/camera/depth/image_raw")
        self.declare_parameter("depth_info_topic", "/camera/depth/camera_info")
        self.declare_parameter("det_model", "yolov8n.pt")
        self.declare_parameter("seg_model", "yolov8n-seg.pt")
        self.declare_parameter("confidence", 0.45)
        self.declare_parameter("iou", 0.45)
        self.declare_parameter("mode", "track")
        self.declare_parameter("target_class", "")
        self.declare_parameter("inference_hz", 10.0)
        self.declare_parameter("publish_annotated", True)
        self.declare_parameter("enable_wrist", True)
        self.declare_parameter("device", "cpu")  # 'cuda' on GPU machine

        p = self.get_parameter
        self._conf = p("confidence").value
        self._iou = p("iou").value
        self.mode = p("mode").value
        self._target_class = p("target_class").value.lower()
        self._pub_annotated = p("publish_annotated").value
        self._device = p("device").value

        # ── State ─────────────────────────────────────────────────────────────
        self._bridge = CvBridge()
        self._lock = threading.Lock()
        self._front: np.ndarray | None = None
        self._wrist: np.ndarray | None = None
        self._depth: np.ndarray | None = None
        self._cam_info: dict | None = None

        # ── Models ────────────────────────────────────────────────────────────
        self._det = None
        self._seg = None
        if _ULTRA_OK:
            try:
                self._det = YOLO(p("det_model").value)
                self.get_logger().info(
                    f"Detection model loaded: {p('det_model').value}"
                )
            except Exception as exc:
                self.get_logger().error(f"Detection model load failed: {exc}")
            try:
                self._seg = YOLO(p("seg_model").value)
                self.get_logger().info(
                    f"Segmentation model loaded: {p('seg_model').value}"
                )
            except Exception as exc:
                self.get_logger().warn(f"Segmentation model unavailable: {exc}")
        else:
            self.get_logger().warn(
                "ultralytics not installed — perception_node running in passthrough mode"
            )

        # ── Subscriptions ─────────────────────────────────────────────────────
        self.create_subscription(
            Image, p("front_camera_topic").value, self._front_cb, 1
        )
        self.create_subscription(Image, p("depth_topic").value, self._depth_cb, 1)
        self.create_subscription(
            CameraInfo, p("depth_info_topic").value, self._info_cb, 1
        )
        self.create_subscription(String, "/perception/mode", self._mode_cb, 10)
        self.create_subscription(
            String, "/perception/target_class", self._target_cb, 10
        )
        if p("enable_wrist").value:
            self.create_subscription(
                Image, p("wrist_camera_topic").value, self._wrist_cb, 1
            )

        # ── Publications ──────────────────────────────────────────────────────
        self._pub_det = self.create_publisher(String, "/perception/detections", 10)
        self._pub_lost = self.create_publisher(Bool, "/perception/tracking_lost", 10)
        self._pub_target_pose = self.create_publisher(
            PoseStamped, "/perception/target_pose", 10
        )
        # Backward-compatible outputs that rl_arm_node already listens to
        self._pub_rl_pose = self.create_publisher(
            PoseStamped, "/rl_arm/target_pose", 10
        )
        self._pub_rl_det = self.create_publisher(Bool, "/rl_arm/target_detected", 10)
        self._pub_nearest = self.create_publisher(
            Float32, "/perception/nearest_obstacle_m", 10
        )

        if self._pub_annotated:
            self._pub_ann_front = self.create_publisher(
                Image, "/perception/annotated/front", 1
            )
            self._pub_ann_wrist = self.create_publisher(
                Image, "/perception/annotated/wrist", 1
            )
            self._pub_masks = self.create_publisher(Image, "/perception/masks", 1)

        # ── Inference timer ───────────────────────────────────────────────────
        period = 1.0 / max(1.0, p("inference_hz").value)
        self.create_timer(period, self._infer)
        self.get_logger().info(
            f"PerceptionNode ready  mode={self.mode}  hz={1 / period:.0f}"
        )

    # ── Callbacks ─────────────────────────────────────────────────────────────

    def _front_cb(self, msg: Image) -> None:
        with self._lock:
            self._front = self._bridge.imgmsg_to_cv2(msg, "bgr8")

    def _wrist_cb(self, msg: Image) -> None:
        with self._lock:
            self._wrist = self._bridge.imgmsg_to_cv2(msg, "bgr8")

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
            }

    def _mode_cb(self, msg: String) -> None:
        v = msg.data.strip().lower()
        if v in self._VALID_MODES:
            self.mode = v
            self.get_logger().info(f"Perception mode → {self.mode}")

    def _target_cb(self, msg: String) -> None:
        self._target_class = msg.data.strip().lower()
        self.get_logger().info(f"Target class → {self._target_class!r}")

    # ── Inference loop ────────────────────────────────────────────────────────

    def _infer(self) -> None:
        with self._lock:
            front = self._front.copy() if self._front is not None else None
            wrist = self._wrist.copy() if self._wrist is not None else None
            depth = self._depth.copy() if self._depth is not None else None

        if front is None or not _ULTRA_OK or self._det is None:
            return

        use_track = self.mode in (self.MODE_TRACK, self.MODE_SEGMENT, self.MODE_VLA)
        use_seg = (
            self.mode in (self.MODE_SEGMENT, self.MODE_VLA) and self._seg is not None
        )

        try:
            model = self._seg if use_seg else self._det
            if use_track:
                results = model.track(
                    front,
                    persist=True,
                    conf=self._conf,
                    iou=self._iou,
                    verbose=False,
                    tracker="bytetrack.yaml",
                    device=self._device,
                )
            else:
                results = model.predict(
                    front,
                    conf=self._conf,
                    iou=self._iou,
                    verbose=False,
                    device=self._device,
                )
        except Exception as exc:
            self.get_logger().error(f"Inference error: {exc}")
            return

        detections = self._parse(results, depth)
        self._publish_detections(detections)
        self._publish_tracking_state(detections)
        self._publish_nearest_obstacle(depth)

        if self._pub_annotated and results:
            ann = results[0].plot()
            self._pub_ann_front.publish(self._bridge.cv2_to_imgmsg(ann, "bgr8"))
            if use_seg and results[0].masks is not None:
                self._pub_masks.publish(
                    self._bridge.cv2_to_imgmsg(
                        self._render_masks(results[0], front), "bgr8"
                    )
                )

        # Wrist-camera pass (detect only, no tracking, saves compute)
        if wrist is not None and self._pub_annotated:
            try:
                w_res = self._det.predict(
                    wrist, conf=self._conf, verbose=False, device=self._device
                )
                if w_res:
                    self._pub_ann_wrist.publish(
                        self._bridge.cv2_to_imgmsg(w_res[0].plot(), "bgr8")
                    )
            except Exception:
                pass

    # ── Parsing helpers ───────────────────────────────────────────────────────

    def _parse(self, results, depth: np.ndarray | None) -> list[dict]:
        dets = []
        if not results:
            return dets
        r = results[0]
        if r.boxes is None:
            return dets
        names = r.names
        for box in r.boxes:
            cls = int(box.cls[0])
            conf = float(box.conf[0])
            x1, y1, x2, y2 = (int(v) for v in box.xyxy[0].tolist())
            cx, cy = (x1 + x2) // 2, (y1 + y2) // 2
            det: dict = {
                "class_id": cls,
                "class_name": names.get(cls, str(cls)),
                "confidence": round(conf, 3),
                "bbox": [x1, y1, x2, y2],
                "center": [cx, cy],
                "track_id": int(box.id[0]) if box.id is not None else None,
            }
            if depth is not None:
                dist = self._depth_at(depth, cx, cy)
                if dist > 0.05:
                    det["distance_m"] = round(dist, 3)
                    if self._cam_info:
                        det["position_3d"] = self._unproject(cx, cy, dist)
            dets.append(det)
        return dets

    def _depth_at(self, depth: np.ndarray, cx: int, cy: int, r: int = 4) -> float:
        h, w = depth.shape[:2]
        if not (0 <= cy < h and 0 <= cx < w):
            return 0.0
        patch = depth[
            max(0, cy - r) : min(h, cy + r + 1), max(0, cx - r) : min(w, cx + r + 1)
        ]
        valid = patch[patch > 0]
        return float(np.median(valid)) / 1000.0 if valid.size else 0.0

    def _unproject(self, cx: int, cy: int, z: float) -> list[float]:
        i = self._cam_info
        return [
            round((cx - i["cx"]) * z / i["fx"], 4),
            round((cy - i["cy"]) * z / i["fy"], 4),
            round(z, 4),
        ]

    # ── Publishers ────────────────────────────────────────────────────────────

    def _publish_detections(self, dets: list[dict]) -> None:
        msg = String()
        msg.data = json.dumps(dets)
        self._pub_det.publish(msg)

    def _publish_tracking_state(self, dets: list[dict]) -> None:
        if not self._target_class:
            return
        target_dets = [d for d in dets if d["class_name"].lower() == self._target_class]
        lost = Bool()
        lost.data = len(target_dets) == 0
        self._pub_lost.publish(lost)

        if target_dets:
            best = max(target_dets, key=lambda d: d.get("confidence", 0))
            if "position_3d" in best:
                self._emit_pose(best)

    def _emit_pose(self, det: dict) -> None:
        px, py, pz = det["position_3d"]
        pose = PoseStamped()
        pose.header.stamp = self.get_clock().now().to_msg()
        pose.header.frame_id = "camera_color_optical_frame"
        pose.pose.position.x = px
        pose.pose.position.y = py
        pose.pose.position.z = pz
        pose.pose.orientation.w = 1.0
        self._pub_target_pose.publish(pose)
        self._pub_rl_pose.publish(pose)
        detected = Bool()
        detected.data = True
        self._pub_rl_det.publish(detected)

    def _publish_nearest_obstacle(self, depth: np.ndarray | None) -> None:
        if depth is None:
            return
        h, w = depth.shape[:2]
        # Centre 30 % horizontal strip, top 80 % vertically (ignore floor)
        strip = depth[: int(h * 0.8), int(w * 0.35) : int(w * 0.65)]
        valid = strip[(strip > 50) & (strip < 4000)]  # 5 cm – 4 m in mm
        if valid.size == 0:
            return
        msg = Float32()
        msg.data = float(np.percentile(valid, 5)) / 1000.0
        self._pub_nearest.publish(msg)

    def _render_masks(self, result, frame: np.ndarray) -> np.ndarray:
        overlay = frame.copy()
        _COLORS = [
            (0, 230, 0),
            (230, 0, 0),
            (0, 0, 230),
            (230, 230, 0),
            (0, 230, 230),
            (230, 0, 230),
        ]
        for i, mask in enumerate(result.masks.data):
            m = cv2.resize(
                mask.cpu().numpy().astype(np.uint8) * 255,
                (frame.shape[1], frame.shape[0]),
            )
            color_layer = np.full_like(frame, _COLORS[i % len(_COLORS)])
            overlay = np.where(
                m[:, :, None] > 128,
                cv2.addWeighted(overlay, 0.55, color_layer, 0.45, 0),
                overlay,
            )
        return overlay


def main(args=None) -> None:
    rclpy.init(args=args)
    node = PerceptionNode()
    try:
        rclpy.spin(node)
    finally:
        node.destroy_node()
        rclpy.shutdown()
