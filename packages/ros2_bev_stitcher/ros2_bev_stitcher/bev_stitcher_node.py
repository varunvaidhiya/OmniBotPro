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
calibration_file (string, default '~/bev_calibration.npz')
    Path to .npz file with per-camera homography matrices.
    Run `ros2 run ros2_bev_stitcher bev_calibrate` to generate this file.
output_frame_id (string, default 'bev_frame')  TF frame for output header.
"""

import os
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
        self.declare_parameter("output_frame_id", "bev_frame")

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
        self._frame_id = self.get_parameter("output_frame_id").value

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

        # ── Timer ────────────────────────────────────────────────────────────
        self.create_timer(1.0 / self._hz, self._timer_cb)

        self.get_logger().info(
            f"BevStitcherNode ready | cameras={self._names} "
            f"| canvas={self._canvas} | {self._out_w}×{self._out_h} "
            f"@ {self._hz}Hz | calibration="
            f"{'loaded' if self._calibrated else 'tiled fallback'}"
        )

    # ── Homography helpers ───────────────────────────────────────────────────

    def _load_homographies(self):
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
        canvas = np.zeros((self._canvas, self._canvas, 3), dtype=np.float32)
        w_sum = np.zeros((self._canvas, self._canvas, 1), dtype=np.float32)
        any_img = False

        for name in self._names:
            img = self._images[name]
            if img is None:
                continue
            any_img = True

            if img.shape[1] != self._src_w or img.shape[0] != self._src_h:
                img = cv2.resize(img, (self._src_w, self._src_h))

            warped = cv2.warpPerspective(
                img.astype(np.float32),
                self._homographies[name],
                (self._canvas, self._canvas),
            )

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

        try:
            msg = self._bridge.cv2_to_imgmsg(out, encoding="rgb8")
            msg.header = Header()
            msg.header.stamp = self.get_clock().now().to_msg()
            msg.header.frame_id = self._frame_id
            self._pub.publish(msg)
        except Exception as exc:
            self.get_logger().error(f"Publish error: {exc}", throttle_duration_sec=5.0)


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
