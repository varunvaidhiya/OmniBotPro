#!/usr/bin/env python3
"""
Bird's Eye View (BEV) stitcher node for OmniBot multi-camera base.

Subscribes to four base cameras (front, rear, left, right), warps each
into a common top-down ground-plane canvas using per-camera homography
matrices, blends the overlapping regions with alpha compositing, and
publishes a single BEV image.

Topics subscribed:
  /camera/front/image_raw  (sensor_msgs/Image)
  /camera/rear/image_raw   (sensor_msgs/Image)
  /camera/left/image_raw   (sensor_msgs/Image)
  /camera/right/image_raw  (sensor_msgs/Image)

Topics published:
  /camera/base/bev/image_raw  (sensor_msgs/Image)  — 800×800 RGB

Homography calibration
----------------------
On first run (or when no calibration file is found), the node uses
identity-based fallback transforms that tile the four cameras in
quadrants.  To calibrate properly:

  1. Place a checkerboard flat on the ground.
  2. Run:
       ros2 run omnibot_lerobot bev_calibrate  (see bev_calibrate.py)
  3. This saves per-camera 3×3 homography matrices to
       ~/omnibot_bev_calibration.npz
  4. Restart this node — it will load that file automatically.
"""

import os
import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image
from std_msgs.msg import Header

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


# ---------------------------------------------------------------------------
# Default calibration: tile four cameras into quadrants (no perspective warp)
# ---------------------------------------------------------------------------


def _default_homographies(canvas: int, src_w: int, src_h: int):
    """
    Fallback: map each camera to one quadrant of the canvas.
    Cameras are scaled so each fills exactly one quarter of the BEV canvas.

    Layout:
        +-------+-------+
        | front |  left |
        +-------+-------+
        | right |  rear |
        +-------+-------+
    """
    half = canvas // 2
    sx = half / src_w
    sy = half / src_h

    scale = np.array([[sx, 0, 0], [0, sy, 0], [0, 0, 1]], dtype=np.float64)

    offsets = {
        "front": np.array([[1, 0, 0], [0, 1, 0], [0, 0, 1]], dtype=np.float64),
        "left": np.array([[1, 0, half], [0, 1, 0], [0, 0, 1]], dtype=np.float64),
        "right": np.array([[1, 0, 0], [0, 1, half], [0, 0, 1]], dtype=np.float64),
        "rear": np.array([[1, 0, half], [0, 1, half], [0, 0, 1]], dtype=np.float64),
    }
    return {k: offsets[k] @ scale for k in offsets}


# ---------------------------------------------------------------------------
# Node
# ---------------------------------------------------------------------------


class BevStitcherNode(Node):
    """Subscribes to 4 base cameras and publishes a single BEV mosaic."""

    CAMERA_NAMES = ("front", "rear", "left", "right")

    def __init__(self):
        super().__init__("bev_stitcher_node")

        # ------------------------------------------------------------------
        # Parameters
        # ------------------------------------------------------------------
        self.declare_parameter("canvas_size", 800)
        self.declare_parameter("output_width", 800)
        self.declare_parameter("output_height", 800)
        self.declare_parameter("src_width", 640)
        self.declare_parameter("src_height", 480)
        self.declare_parameter("publish_hz", 30.0)
        self.declare_parameter(
            "calibration_file", os.path.expanduser("~/omnibot_bev_calibration.npz")
        )

        self.canvas_size = self.get_parameter("canvas_size").value
        self.out_w = self.get_parameter("output_width").value
        self.out_h = self.get_parameter("output_height").value
        self.src_w = self.get_parameter("src_width").value
        self.src_h = self.get_parameter("src_height").value
        self.publish_hz = self.get_parameter("publish_hz").value
        self.calibration_file = self.get_parameter("calibration_file").value

        # ------------------------------------------------------------------
        # cv_bridge / OpenCV check
        # ------------------------------------------------------------------
        if not CV_BRIDGE_AVAILABLE:
            self.get_logger().error("cv_bridge not available — node cannot run.")
            return
        if not CV2_AVAILABLE:
            self.get_logger().error("opencv-python not available — node cannot run.")
            return

        self.bridge = CvBridge()

        # ------------------------------------------------------------------
        # Load homography matrices
        # ------------------------------------------------------------------
        self.homographies = self._load_homographies()

        # Precompute alpha weight maps for seamless blending
        self._blend_weights = self._compute_blend_weights()

        # ------------------------------------------------------------------
        # Image buffers
        # ------------------------------------------------------------------
        self._images = {name: None for name in self.CAMERA_NAMES}

        # ------------------------------------------------------------------
        # Subscribers
        # ------------------------------------------------------------------
        for name in self.CAMERA_NAMES:
            topic = f"/camera/{name}/image_raw"
            self.create_subscription(
                Image, topic, lambda msg, n=name: self._image_cb(msg, n), 10
            )

        # ------------------------------------------------------------------
        # Publisher
        # ------------------------------------------------------------------
        self._pub = self.create_publisher(Image, "/camera/base/bev/image_raw", 10)

        # ------------------------------------------------------------------
        # Timer
        # ------------------------------------------------------------------
        self.create_timer(1.0 / self.publish_hz, self._timer_cb)

        self.get_logger().info(
            f"BevStitcherNode started | canvas={self.canvas_size} "
            f"| output={self.out_w}x{self.out_h} | hz={self.publish_hz} "
            f"| calibration={'loaded' if self._cal_loaded else 'default (tiled)'}"
        )

    # ------------------------------------------------------------------
    # Homography loading
    # ------------------------------------------------------------------

    def _load_homographies(self):
        self._cal_loaded = False
        if os.path.isfile(self.calibration_file):
            try:
                data = np.load(self.calibration_file)
                Hs = {name: data[name] for name in self.CAMERA_NAMES}
                self._cal_loaded = True
                self.get_logger().info(
                    f"Loaded BEV calibration from {self.calibration_file}"
                )
                return Hs
            except Exception as exc:
                self.get_logger().warn(
                    f"Failed to load calibration ({exc}). Using default tiled layout."
                )

        return _default_homographies(self.canvas_size, self.src_w, self.src_h)

    # ------------------------------------------------------------------
    # Blend weight precomputation
    # ------------------------------------------------------------------

    def _compute_blend_weights(self):
        """
        Compute a per-pixel distance-to-edge weight for each camera's
        warped region.  Used for alpha blending in overlap zones.

        Returns a dict {name: weight_map (canvas_size x canvas_size, float32)}.
        For the default tiled layout these are binary masks (no real overlap).
        """
        weights = {}
        for name in self.CAMERA_NAMES:
            # Create a white source image (src weight map)
            src_weight = np.ones((self.src_h, self.src_w), dtype=np.float32)
            H = self.homographies[name]
            warped_w = cv2.warpPerspective(
                src_weight, H, (self.canvas_size, self.canvas_size)
            )
            weights[name] = warped_w
        return weights

    # ------------------------------------------------------------------
    # Image callback
    # ------------------------------------------------------------------

    def _image_cb(self, msg: Image, name: str):
        try:
            cv_img = self.bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            self._images[name] = np.array(cv_img, dtype=np.uint8)
        except Exception as exc:
            self.get_logger().warn(
                f"[{name}] image conversion error: {exc}", throttle_duration_sec=5.0
            )

    # ------------------------------------------------------------------
    # Timer: stitch and publish
    # ------------------------------------------------------------------

    def _timer_cb(self):
        canvas = np.zeros((self.canvas_size, self.canvas_size, 3), dtype=np.float32)
        weight_sum = np.zeros((self.canvas_size, self.canvas_size, 1), dtype=np.float32)

        any_image = False
        for name in self.CAMERA_NAMES:
            img = self._images[name]
            if img is None:
                continue
            any_image = True

            # Resize source image to expected src dimensions if needed
            if img.shape[1] != self.src_w or img.shape[0] != self.src_h:
                img = cv2.resize(
                    img, (self.src_w, self.src_h), interpolation=cv2.INTER_LINEAR
                )

            # Warp to BEV canvas
            warped = cv2.warpPerspective(
                img.astype(np.float32),
                self.homographies[name],
                (self.canvas_size, self.canvas_size),
            )

            # Weight map for this camera
            w = self._blend_weights[name][:, :, np.newaxis]  # (H, W, 1)

            canvas += warped * w
            weight_sum += w

        if not any_image:
            self.get_logger().warn(
                "BEV stitcher: no camera images received yet.",
                throttle_duration_sec=5.0,
            )
            return

        # Normalize by weight sum (avoid division by zero)
        mask = weight_sum[:, :, 0] > 0
        canvas[mask] /= weight_sum[mask]

        # Resize to output resolution and convert back to uint8
        bev_uint8 = canvas.clip(0, 255).astype(np.uint8)
        if self.out_w != self.canvas_size or self.out_h != self.canvas_size:
            bev_uint8 = cv2.resize(
                bev_uint8, (self.out_w, self.out_h), interpolation=cv2.INTER_LINEAR
            )

        # Publish as ROS Image
        try:
            msg = self.bridge.cv2_to_imgmsg(bev_uint8, encoding="rgb8")
            msg.header = Header()
            msg.header.stamp = self.get_clock().now().to_msg()
            msg.header.frame_id = "base_bev_frame"
            self._pub.publish(msg)
        except Exception as exc:
            self.get_logger().error(
                f"Failed to publish BEV image: {exc}", throttle_duration_sec=5.0
            )


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main(args=None):
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
