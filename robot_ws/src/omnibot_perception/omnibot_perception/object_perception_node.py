#!/usr/bin/env python3
"""
object_perception_node — modular AI perception for OmniBot.

Measures object distance and estimates object pose using the Astra Pro
depth camera (plus optional YOLO labels from the RGB stream), and exposes
the results in forms the Nav2 stack / mission planner / AI orchestration
layer can act on.

Two detection backends (selected automatically):
  1. YOLO (ultralytics) on /camera/color/image_raw — labelled detections,
     each fused with the median depth inside its bounding box. Used when
     the `ultralytics` package is importable and `use_yolo` is true.
  2. Depth clustering fallback — connected-component segmentation of the
     depth image into obstacle blobs (no ML dependency, runs on the Pi).

For every detected object the node back-projects to a 3-D point in the
optical frame, transforms it into `base_link` via TF, and estimates a yaw
orientation from the cluster's principal axis (PCA).

Subscribed topics
  /camera/depth/image_raw     sensor_msgs/Image   (16UC1 mm or 32FC1 m)
  /camera/depth/camera_info   sensor_msgs/CameraInfo
  /camera/color/image_raw     sensor_msgs/Image   (only for YOLO backend)
  /perception/query_pixel     geometry_msgs/PointStamped
        (x=u, y=v) — ask "how far is the object at this pixel?";
        answered on /perception/query_result. Lets the VLA / LangGraph
        agent ground language like "that object" to a metric position.

Published topics
  /perception/objects          geometry_msgs/PoseArray  (base_link)
  /perception/object_info      std_msgs/String — JSON list per object:
        {id, label, confidence, distance_m, bearing_rad, position[3],
         yaw, size_m}
  /perception/nearest_distance std_msgs/Float32 — distance (m) of nearest
        detected object, NaN when none. Usable as a go/no-go signal.
  /perception/query_result     geometry_msgs/PoseStamped (base_link)
  /perception/markers          visualization_msgs/MarkerArray (RViz)

Parameters: see config/perception_params.yaml.
"""

import json
import math

import numpy as np
import rclpy
from geometry_msgs.msg import Pose, PoseArray, PointStamped, PoseStamped
from rclpy.node import Node
from sensor_msgs.msg import CameraInfo, Image
from std_msgs.msg import Float32, String
from visualization_msgs.msg import Marker, MarkerArray

try:
    from cv_bridge import CvBridge

    CV_BRIDGE_AVAILABLE = True
except ImportError:
    CV_BRIDGE_AVAILABLE = False

try:
    import cv2

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

try:
    import tf2_ros

    TF2_AVAILABLE = True
except ImportError:
    TF2_AVAILABLE = False


def _quat_to_rot(qx, qy, qz, qw):
    """3×3 rotation matrix from quaternion."""
    n = math.sqrt(qx * qx + qy * qy + qz * qz + qw * qw) or 1.0
    qx, qy, qz, qw = qx / n, qy / n, qz / n, qw / n
    return np.array(
        [
            [1 - 2 * (qy * qy + qz * qz), 2 * (qx * qy - qz * qw), 2 * (qx * qz + qy * qw)],
            [2 * (qx * qy + qz * qw), 1 - 2 * (qx * qx + qz * qz), 2 * (qy * qz - qx * qw)],
            [2 * (qx * qz - qy * qw), 2 * (qy * qz + qx * qw), 1 - 2 * (qx * qx + qy * qy)],
        ]
    )


class ObjectPerceptionNode(Node):
    def __init__(self):
        super().__init__("object_perception_node")

        # ── Parameters ──────────────────────────────────────────────
        self.declare_parameter("depth_topic", "/camera/depth/image_raw")
        self.declare_parameter("depth_info_topic", "/camera/depth/camera_info")
        self.declare_parameter("color_topic", "/camera/color/image_raw")
        self.declare_parameter("target_frame", "base_link")
        self.declare_parameter("rate_hz", 5.0)
        self.declare_parameter("range_min", 0.3)
        self.declare_parameter("range_max", 6.0)
        self.declare_parameter("use_yolo", True)
        self.declare_parameter("yolo_model", "yolov8n.pt")
        self.declare_parameter("yolo_conf", 0.4)
        # Depth clustering fallback tuning
        self.declare_parameter("cluster_min_pixels", 400)
        self.declare_parameter("cluster_depth_step", 0.25)  # m bands
        self.declare_parameter("max_objects", 10)
        # Ignore returns near the floor (camera height + tilt dependent)
        self.declare_parameter("min_height_above_ground", 0.03)

        gp = lambda n: self.get_parameter(n).value  # noqa: E731
        self._depth_topic = gp("depth_topic")
        self._info_topic = gp("depth_info_topic")
        self._color_topic = gp("color_topic")
        self._target_frame = gp("target_frame")
        self._rate = float(gp("rate_hz"))
        self._rmin = float(gp("range_min"))
        self._rmax = float(gp("range_max"))
        self._cluster_min_px = int(gp("cluster_min_pixels"))
        self._depth_step = float(gp("cluster_depth_step"))
        self._max_objects = int(gp("max_objects"))
        self._min_h = float(gp("min_height_above_ground"))

        if not (CV_BRIDGE_AVAILABLE and CV2_AVAILABLE):
            self.get_logger().error("cv_bridge / OpenCV missing — cannot run.")
            return
        self._bridge = CvBridge()

        # ── Optional YOLO backend ───────────────────────────────────
        self._yolo = None
        if gp("use_yolo"):
            try:
                from ultralytics import YOLO

                self._yolo = YOLO(gp("yolo_model"))
                self.get_logger().info(f"YOLO backend active ({gp('yolo_model')})")
            except Exception as exc:  # ImportError or model load failure
                self.get_logger().warn(
                    f"YOLO unavailable ({exc}) — using depth-clustering backend."
                )

        # ── TF ──────────────────────────────────────────────────────
        self._tf_buffer = None
        if TF2_AVAILABLE:
            self._tf_buffer = tf2_ros.Buffer()
            self._tf_listener = tf2_ros.TransformListener(self._tf_buffer, self)
        else:
            self.get_logger().warn("tf2_ros missing — poses stay in optical frame.")

        # ── State ───────────────────────────────────────────────────
        self._depth = None  # float32 metres
        self._depth_frame = None
        self._color = None
        self._fx = self._fy = self._cx = self._cy = None

        # ── Subs / pubs ─────────────────────────────────────────────
        self.create_subscription(Image, self._depth_topic, self._depth_cb, 5)
        self.create_subscription(CameraInfo, self._info_topic, self._info_cb, 5)
        if self._yolo is not None:
            self.create_subscription(Image, self._color_topic, self._color_cb, 5)
        self.create_subscription(
            PointStamped, "/perception/query_pixel", self._query_cb, 5
        )

        self._pub_objects = self.create_publisher(PoseArray, "/perception/objects", 10)
        self._pub_info = self.create_publisher(String, "/perception/object_info", 10)
        self._pub_nearest = self.create_publisher(
            Float32, "/perception/nearest_distance", 10
        )
        self._pub_query = self.create_publisher(
            PoseStamped, "/perception/query_result", 10
        )
        self._pub_markers = self.create_publisher(
            MarkerArray, "/perception/markers", 5
        )

        self.create_timer(1.0 / self._rate, self._timer_cb)
        backend = "yolo" if self._yolo is not None else "depth-clustering"
        self.get_logger().info(
            f"ObjectPerceptionNode ready | backend={backend} "
            f"| range=[{self._rmin},{self._rmax}]m | {self._rate}Hz"
        )

    # ── Callbacks ───────────────────────────────────────────────────

    def _info_cb(self, msg: CameraInfo):
        self._fx, self._fy = msg.k[0], msg.k[4]
        self._cx, self._cy = msg.k[2], msg.k[5]

    def _depth_cb(self, msg: Image):
        try:
            img = self._bridge.imgmsg_to_cv2(msg, desired_encoding="passthrough")
            if img.dtype == np.uint16:  # mm → m
                img = img.astype(np.float32) / 1000.0
            self._depth = img.astype(np.float32)
            self._depth_frame = msg.header.frame_id
        except Exception as exc:
            self.get_logger().warn(f"depth error: {exc}", throttle_duration_sec=5.0)

    def _color_cb(self, msg: Image):
        try:
            self._color = self._bridge.imgmsg_to_cv2(msg, desired_encoding="bgr8")
        except Exception as exc:
            self.get_logger().warn(f"color error: {exc}", throttle_duration_sec=5.0)

    def _query_cb(self, msg: PointStamped):
        """Answer 'how far / where is the object at pixel (u,v)?'."""
        if self._depth is None or self._fx is None:
            return
        u, v = int(msg.point.x), int(msg.point.y)
        h, w = self._depth.shape[:2]
        if not (0 <= u < w and 0 <= v < h):
            return
        # Median over a small window for robustness
        win = self._depth[max(0, v - 4) : v + 5, max(0, u - 4) : u + 5]
        valid = win[(win > self._rmin) & (win < self._rmax)]
        if valid.size == 0:
            return
        z = float(np.median(valid))
        pt_opt = self._backproject(u, v, z)
        pose = self._to_target_frame(pt_opt)
        if pose is None:
            return
        out = PoseStamped()
        out.header.stamp = self.get_clock().now().to_msg()
        out.header.frame_id = self._target_frame
        out.pose = pose
        self._pub_query.publish(out)

    # ── Geometry helpers ────────────────────────────────────────────

    def _backproject(self, u, v, z):
        x = (u - self._cx) / self._fx * z
        y = (v - self._cy) / self._fy * z
        return np.array([x, y, z])

    def _lookup_tf(self):
        """Return (R, t) optical→target, or None."""
        if self._tf_buffer is None or self._depth_frame is None:
            return None
        try:
            tr = self._tf_buffer.lookup_transform(
                self._target_frame, self._depth_frame, rclpy.time.Time()
            )
        except Exception:
            return None
        q = tr.transform.rotation
        t = tr.transform.translation
        return _quat_to_rot(q.x, q.y, q.z, q.w), np.array([t.x, t.y, t.z])

    def _to_target_frame(self, pt_opt, yaw=0.0):
        rt = self._lookup_tf()
        if rt is None:
            # No TF — return pose in optical frame (still useful in sim/tests)
            pose = Pose()
            pose.position.x, pose.position.y, pose.position.z = map(float, pt_opt)
            pose.orientation.w = 1.0
            return pose
        R, t = rt
        p = R @ pt_opt + t
        pose = Pose()
        pose.position.x, pose.position.y, pose.position.z = map(float, p)
        pose.orientation.z = math.sin(yaw / 2.0)
        pose.orientation.w = math.cos(yaw / 2.0)
        return pose

    # ── Detection backends ──────────────────────────────────────────

    def _detect_yolo(self):
        """YOLO on RGB; depth-fused 3-D position per box."""
        if self._color is None:
            return []
        results = self._yolo.predict(
            self._color,
            conf=float(self.get_parameter("yolo_conf").value),
            verbose=False,
        )
        dets = []
        dh, dw = self._depth.shape[:2]
        ch, cw = self._color.shape[:2]
        for r in results:
            for b in r.boxes:
                x1, y1, x2, y2 = (float(v) for v in b.xyxy[0])
                # Scale colour-image bbox into depth-image pixels
                sx, sy = dw / cw, dh / ch
                du1, dv1 = int(x1 * sx), int(y1 * sy)
                du2, dv2 = int(x2 * sx), int(y2 * sy)
                roi = self._depth[max(0, dv1) : dv2, max(0, du1) : du2]
                valid = roi[(roi > self._rmin) & (roi < self._rmax)]
                if valid.size < 20:
                    continue
                z = float(np.median(valid))
                u, v = (du1 + du2) / 2.0, (dv1 + dv2) / 2.0
                size = (du2 - du1) * z / self._fx  # approx metric width
                dets.append(
                    {
                        "label": r.names[int(b.cls[0])],
                        "confidence": float(b.conf[0]),
                        "centroid_px": (u, v),
                        "depth": z,
                        "size_m": size,
                        "yaw": 0.0,
                    }
                )
        return dets

    def _detect_clusters(self):
        """Depth-band connected components → obstacle blobs."""
        depth = self._depth
        valid = (depth > self._rmin) & (depth < self._rmax)
        dets = []
        band_edges = np.arange(self._rmin, self._rmax, self._depth_step)
        for lo in band_edges:
            hi = lo + self._depth_step * 1.5  # overlapping bands
            mask = (valid & (depth >= lo) & (depth < hi)).astype(np.uint8)
            if mask.sum() < self._cluster_min_px:
                continue
            n, labels, stats, centroids = cv2.connectedComponentsWithStats(mask, 8)
            for i in range(1, n):
                if stats[i, cv2.CC_STAT_AREA] < self._cluster_min_px:
                    continue
                u, v = centroids[i]
                blob = (labels == i)
                z = float(np.median(depth[blob]))
                if not (self._rmin < z < self._rmax):
                    continue
                # PCA on blob pixels for a coarse yaw estimate
                ys, xs = np.nonzero(blob)
                pts = np.stack([xs - xs.mean(), ys - ys.mean()], axis=1).astype(
                    np.float32
                )
                if len(pts) >= 10:
                    cov = np.cov(pts.T)
                    evals, evecs = np.linalg.eigh(cov)
                    major = evecs[:, np.argmax(evals)]
                    yaw = math.atan2(-major[1], major[0])  # image y is down
                else:
                    yaw = 0.0
                size = stats[i, cv2.CC_STAT_WIDTH] * z / self._fx
                dets.append(
                    {
                        "label": "obstacle",
                        "confidence": 1.0,
                        "centroid_px": (float(u), float(v)),
                        "depth": z,
                        "size_m": size,
                        "yaw": yaw,
                    }
                )
        # Deduplicate near-identical blobs from overlapping bands
        dets.sort(key=lambda d: d["depth"])
        kept = []
        for d in dets:
            dup = any(
                abs(d["depth"] - k["depth"]) < self._depth_step * 0.5
                and np.hypot(
                    d["centroid_px"][0] - k["centroid_px"][0],
                    d["centroid_px"][1] - k["centroid_px"][1],
                )
                < 40
                for k in kept
            )
            if not dup:
                kept.append(d)
        return kept[: self._max_objects]

    # ── Main loop ───────────────────────────────────────────────────

    def _timer_cb(self):
        if self._depth is None or self._fx is None:
            return
        dets = self._detect_yolo() if self._yolo is not None else self._detect_clusters()

        now = self.get_clock().now().to_msg()
        pa = PoseArray()
        pa.header.stamp = now
        pa.header.frame_id = self._target_frame
        info, markers = [], MarkerArray()
        nearest = float("nan")

        rt = self._lookup_tf()
        for i, d in enumerate(dets):
            u, v = d["centroid_px"]
            pt_opt = self._backproject(u, v, d["depth"])
            if rt is not None:
                R, t = rt
                p = R @ pt_opt + t
                if p[2] < self._min_h:  # floor return — skip
                    continue
            else:
                p = pt_opt
            pose = Pose()
            pose.position.x, pose.position.y, pose.position.z = map(float, p)
            pose.orientation.z = math.sin(d["yaw"] / 2.0)
            pose.orientation.w = math.cos(d["yaw"] / 2.0)
            pa.poses.append(pose)

            dist = float(np.linalg.norm(p[:2])) if rt is not None else d["depth"]
            bearing = math.atan2(p[1], p[0]) if rt is not None else 0.0
            if math.isnan(nearest) or dist < nearest:
                nearest = dist
            info.append(
                {
                    "id": i,
                    "label": d["label"],
                    "confidence": round(d["confidence"], 3),
                    "distance_m": round(dist, 3),
                    "bearing_rad": round(bearing, 3),
                    "position": [round(float(x), 3) for x in p],
                    "yaw": round(d["yaw"], 3),
                    "size_m": round(d["size_m"], 3),
                }
            )

            m = Marker()
            m.header.frame_id = self._target_frame
            m.header.stamp = now
            m.ns, m.id = "perception", i
            m.type, m.action = Marker.SPHERE, Marker.ADD
            m.pose = pose
            m.scale.x = m.scale.y = m.scale.z = max(0.08, min(d["size_m"], 0.5))
            m.color.r, m.color.g, m.color.a = 1.0, 0.4, 0.9
            m.lifetime.sec = 1
            markers.markers.append(m)

        self._pub_objects.publish(pa)
        self._pub_info.publish(String(data=json.dumps(info)))
        self._pub_nearest.publish(Float32(data=nearest))
        self._pub_markers.publish(markers)


def main(args=None):
    rclpy.init(args=args)
    node = ObjectPerceptionNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
