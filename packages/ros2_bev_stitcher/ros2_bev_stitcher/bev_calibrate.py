#!/usr/bin/env python3
"""
ros2_bev_stitcher — BEV Calibration Tool
=========================================

Computes per-camera homography matrices by detecting a flat checkerboard
placed on the ground and saves them to a .npz file that `bev_stitcher_node`
loads automatically.

Usage
-----
  # Place a checkerboard (default: 9×6 inner corners, 25mm squares) flat on
  # the ground where ALL cameras can see it.  Then run:

  ros2 run ros2_bev_stitcher bev_calibrate \\
      --camera-names front rear left right \\
      --input-topics /camera/front/image_raw /camera/rear/image_raw \\
                     /camera/left/image_raw /camera/right/image_raw \\
      --checkerboard-cols 9 --checkerboard-rows 6 \\
      --square-size 0.025 \\
      --canvas-size 800 \\
      --output ~/bev_calibration.npz

The tool will:
  1. Subscribe to each camera topic and capture one frame per camera.
  2. Detect checkerboard corners in each frame.
  3. Map the detected corners to a shared BEV coordinate system
     (metres, origin at the centre of the canvas).
  4. Compute a 3×3 homography per camera via RANSAC.
  5. Save all homographies to the output .npz file.
"""

import argparse
import os
import sys
from typing import Dict, List, Optional

import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image

try:
    from cv_bridge import CvBridge
except ImportError:
    print("[ERROR] cv_bridge not installed.")
    sys.exit(1)

try:
    import cv2
except ImportError:
    print("[ERROR] opencv-python not installed.")
    sys.exit(1)


class BevCalibrator(Node):
    """
    One-shot ROS 2 node that captures frames, detects checkerboard corners,
    computes homographies, and saves them.
    """

    def __init__(
        self,
        camera_names: List[str],
        topics: List[str],
        cb_cols: int,
        cb_rows: int,
        square_size: float,
        canvas_size: int,
        output_path: str,
    ) -> None:
        super().__init__("bev_calibrate")
        self._names = camera_names
        self._cb_pattern = (cb_cols, cb_rows)
        self._sq = square_size
        self._canvas = canvas_size
        self._output = output_path
        self._bridge = CvBridge()
        self._frames: Dict[str, Optional[np.ndarray]] = {n: None for n in camera_names}

        for name, topic in zip(camera_names, topics):
            self.create_subscription(
                Image, topic, lambda msg, n=name: self._frame_cb(msg, n), 1
            )
            self.get_logger().info(f"Waiting for: {topic}")

    def _frame_cb(self, msg: Image, name: str) -> None:
        if self._frames[name] is None:
            try:
                img = self._bridge.imgmsg_to_cv2(msg, "bgr8")
                self._frames[name] = np.asarray(img)
                self.get_logger().info(f"Got frame from [{name}]")
            except Exception as exc:
                self.get_logger().warn(f"[{name}] conversion error: {exc}")

    def all_received(self) -> bool:
        return all(v is not None for v in self._frames.values())

    def calibrate(self) -> bool:
        """
        Detect corners, compute homographies, save to .npz.
        Returns True on success.
        """
        cb_cols, cb_rows = self._cb_pattern
        pattern_size = (cb_cols, cb_rows)

        # World coordinates of checkerboard corners in metres
        # Origin at the top-left inner corner
        objp = np.zeros((cb_cols * cb_rows, 2), dtype=np.float64)
        objp[:, 0] = np.tile(np.arange(cb_cols), cb_rows) * self._sq
        objp[:, 1] = np.repeat(np.arange(cb_rows), cb_cols) * self._sq

        # Centre the world frame at the checkerboard centre
        objp[:, 0] -= objp[:, 0].max() / 2.0
        objp[:, 1] -= objp[:, 1].max() / 2.0

        # Scale to canvas pixels: 1 m → canvas/2 pixels
        scale = self._canvas / 2.0  # pixels per metre (1 m fills half canvas)
        dst_pts = objp * scale + self._canvas / 2.0  # shift origin to canvas centre

        homographies: Dict[str, np.ndarray] = {}
        success = True

        for name in self._names:
            img = self._frames[name]
            gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
            found, corners = cv2.findChessboardCorners(gray, pattern_size)

            if not found:
                self.get_logger().error(
                    f"[{name}] Checkerboard NOT found. "
                    f"Check lighting and that the full board is visible."
                )
                success = False
                continue

            # Sub-pixel refinement
            criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 30, 0.001)
            corners_refined = cv2.cornerSubPix(
                gray, corners, (11, 11), (-1, -1), criteria
            )

            src_pts = corners_refined.reshape(-1, 2).astype(np.float64)

            H, mask = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)
            if H is None:
                self.get_logger().error(f"[{name}] Homography computation failed.")
                success = False
                continue

            inliers = int(mask.sum()) if mask is not None else 0
            self.get_logger().info(
                f"[{name}] Homography computed — {inliers}/{len(src_pts)} inliers."
            )
            homographies[name] = H

            # Visual verification — show warped result
            h, w = img.shape[:2]
            warped = cv2.warpPerspective(img, H, (self._canvas, self._canvas))
            cv2.imshow(f"BEV preview [{name}]", warped)

        cv2.waitKey(2000)
        cv2.destroyAllWindows()

        if success:
            os.makedirs(os.path.dirname(os.path.abspath(self._output)), exist_ok=True)
            np.savez(self._output, **homographies)
            self.get_logger().info(
                f"Saved {len(homographies)} homographies to {self._output}"
            )
        else:
            self.get_logger().error(
                "Calibration incomplete — fix errors above and retry."
            )
        return success


def main(args=None):
    parser = argparse.ArgumentParser(description="BEV calibration tool")
    parser.add_argument(
        "--camera-names", nargs="+", default=["front", "rear", "left", "right"]
    )
    parser.add_argument(
        "--input-topics",
        nargs="+",
        default=None,
        help="Topic per camera (default: /camera/<name>/image_raw)",
    )
    parser.add_argument("--checkerboard-cols", type=int, default=9)
    parser.add_argument("--checkerboard-rows", type=int, default=6)
    parser.add_argument(
        "--square-size",
        type=float,
        default=0.025,
        help="Checkerboard square size in metres",
    )
    parser.add_argument("--canvas-size", type=int, default=800)
    parser.add_argument("--output", default=os.path.expanduser("~/bev_calibration.npz"))

    parsed, ros_args = parser.parse_known_args(args)

    topics = parsed.input_topics or [
        f"/camera/{n}/image_raw" for n in parsed.camera_names
    ]

    rclpy.init(args=ros_args)
    node = BevCalibrator(
        camera_names=parsed.camera_names,
        topics=topics,
        cb_cols=parsed.checkerboard_cols,
        cb_rows=parsed.checkerboard_rows,
        square_size=parsed.square_size,
        canvas_size=parsed.canvas_size,
        output_path=parsed.output,
    )

    print("Waiting for camera frames...")
    while rclpy.ok() and not node.all_received():
        rclpy.spin_once(node, timeout_sec=0.1)

    if node.all_received():
        node.calibrate()
    else:
        node.get_logger().error("Did not receive all camera frames.")

    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
