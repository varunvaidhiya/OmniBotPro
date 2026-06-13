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
When no calibration file is found, the node computes geometric IPM
(inverse perspective mapping) homographies from the URDF camera mounting
poses — this already produces a true fused top-down view.  For a refined
calibration:

  1. Place a checkerboard flat on the ground.
  2. Run:
       ros2 run omnibot_lerobot bev_calibrate  (see bev_calibrate.py)
  3. This saves per-camera 3×3 homography matrices to
       ~/omnibot_bev_calibration.npz
  4. Restart this node — it will load that file automatically.
"""

import math
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
# Geometric IPM (inverse perspective mapping) — default when no manual
# calibration file is present.  Projects each camera onto the z=0 ground
# plane using the known mounting pose from the URDF, producing a true
# fused top-down view instead of a tiled mosaic.
# ---------------------------------------------------------------------------


def _geometric_ipm(
    names,
    poses: dict,
    cam_height: float,
    hfov: float,
    src_w: int,
    src_h: int,
    canvas: int,
    range_m: float,
    min_depth: float = 0.03,
):
    """
    Compute per-camera homographies (image px → BEV canvas px) and
    canvas-space blend masks by projecting the ground plane (z=0) through
    each camera's pinhole model.

    poses: {name: (x, y, yaw)} camera position/orientation in base_link.
    cam_height: camera optical centre height above the ground (m).
    range_m: half-extent of the BEV canvas in metres (canvas covers
             ±range_m around base_link; +X forward = image up,
             +Y left = image left).

    Returns (homographies: dict, masks: dict).  Masks are feathered by
    bearing off the optical axis so overlapping wedges blend smoothly.
    """
    fx = (src_w / 2.0) / math.tan(hfov / 2.0)
    fy = fx
    cx, cy = src_w / 2.0, src_h / 2.0
    mpp = (2.0 * range_m) / canvas  # metres per canvas pixel
    c0 = (canvas - 1) / 2.0
    tan_half_h = math.tan(hfov / 2.0)
    tan_half_v = (src_h / 2.0) / fy

    homographies, masks = {}, {}
    rows, cols = np.mgrid[0:canvas, 0:canvas].astype(np.float32)
    gX = (c0 - rows) * mpp  # ground X (forward) per canvas pixel
    gY = (c0 - cols) * mpp  # ground Y (left)    per canvas pixel

    for name in names:
        px, py, yaw = poses[name]
        cos_y, sin_y = math.cos(yaw), math.sin(yaw)

        # ── Homography from 4 ground points inside the camera's view ──
        img_pts, can_pts = [], []
        for bearing in (-hfov * 0.3, hfov * 0.3):
            for dist in (range_m * 0.25, range_m * 0.9):
                wx = px + dist * math.cos(yaw + bearing)
                wy = py + dist * math.sin(yaw + bearing)
                # base → camera link frame
                dx, dy = wx - px, wy - py
                xl = cos_y * dx + sin_y * dy
                yl = -sin_y * dx + cos_y * dy
                # link → optical (z fwd, x right, y down)
                xo, yo, zo = -yl, cam_height, xl
                img_pts.append((fx * xo / zo + cx, fy * yo / zo + cy))
                can_pts.append((c0 - wy / mpp, c0 - wx / mpp))  # (col, row)
        H = cv2.getPerspectiveTransform(np.float32(img_pts), np.float32(can_pts))
        homographies[name] = H.astype(np.float64)

        # ── Canvas-domain visibility + feathered blend mask ──
        dx, dy = gX - px, gY - py
        xl = cos_y * dx + sin_y * dy
        yl = -sin_y * dx + cos_y * dy
        zo = xl  # optical depth
        xo = -yl
        yo = cam_height  # ground is cam_height below the optical centre
        with np.errstate(divide="ignore", invalid="ignore"):
            bearing_ratio = np.abs(xo) / np.maximum(zo, 1e-6) / tan_half_h
            elev_ratio = yo / np.maximum(zo, 1e-6) / tan_half_v
        valid = (zo > min_depth) & (bearing_ratio < 1.0) & (elev_ratio <= 1.0)
        weight = np.clip(1.0 - bearing_ratio, 0.0, 1.0)
        masks[name] = np.where(valid, weight, 0.0).astype(np.float32)

    return homographies, masks


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
        # ── Geometric IPM parameters (used when no calibration file) ──
        # Defaults match omnibot.urdf.xacro base-camera mounting.
        self.declare_parameter("bev_range_m", 2.0)
        self.declare_parameter("camera_hfov", 1.745)
        self.declare_parameter("camera_height", 0.0885)
        self.declare_parameter("cam_front_pose", [0.1175, 0.0, 0.0])
        self.declare_parameter("cam_rear_pose", [-0.1175, 0.0, math.pi])
        self.declare_parameter("cam_left_pose", [0.0, 0.110, math.pi / 2.0])
        self.declare_parameter("cam_right_pose", [0.0, -0.110, -math.pi / 2.0])

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
            f"| calibration={'loaded' if self._cal_loaded else 'geometric IPM'}"
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
                    f"Failed to load calibration ({exc}). "
                    "Falling back to geometric IPM from URDF camera poses."
                )

        poses = {
            name: tuple(self.get_parameter(f"cam_{name}_pose").value)
            for name in self.CAMERA_NAMES
        }
        Hs, self._geo_masks = _geometric_ipm(
            self.CAMERA_NAMES,
            poses,
            float(self.get_parameter("camera_height").value),
            float(self.get_parameter("camera_hfov").value),
            self.src_w,
            self.src_h,
            self.canvas_size,
            float(self.get_parameter("bev_range_m").value),
        )
        self.get_logger().info(
            "Using geometric IPM homographies (true fused BEV). "
            "Run bev_calibrate for a refined ground-truth calibration."
        )
        return Hs

    # ------------------------------------------------------------------
    # Blend weight precomputation
    # ------------------------------------------------------------------

    def _compute_blend_weights(self):
        """
        Compute a per-pixel distance-to-edge weight for each camera's
        warped region.  Used for alpha blending in overlap zones.

        Returns a dict {name: weight_map (canvas_size x canvas_size, float32)}.

        Geometric-IPM mode uses analytic canvas-space visibility wedges
        (feathered by bearing), which also suppress the mirrored/behind-
        camera artefacts that a raw warpPerspective would produce.
        """
        if getattr(self, "_geo_masks", None) is not None:
            return self._geo_masks

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
