#!/usr/bin/env python3
"""
Interactive BEV Calibration UI — OhhO View bring-up wizard.

Shows the fused bird's-eye-view with live trackbars for adjusting each
camera's extrinsic parameters. No checkerboard needed — align visually
and save the calibration when the surround view looks right.

Usage:
    ros2 run ros2_bev_stitcher bev_cal_ui

Controls:
    Trackbar 0-3: select camera (front, rear, left, right)
    Trackbars for: pos_x, pos_y, pos_z, roll, pitch, yaw
    s: save calibration to output file
    q / ESC: quit
"""

import os
import argparse
import sys
from typing import Dict, List, Optional

import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image

try:
    from cv_bridge import CvBridge

    _CV_BRIDGE = True
except ImportError:
    print("[ERROR] cv_bridge not installed.")
    sys.exit(1)

try:
    import cv2

    _CV2 = True
except ImportError:
    print("[ERROR] opencv-python not installed.")
    sys.exit(1)

from ros2_bev_stitcher.bev_ipm import (
    compute_ipm_homography,
    camera_rotation,
    DEFAULT_CAMERA_CONFIGS,
    DEFAULT_INTRINSICS,
)

# ── Trackbar helpers ──────────────────────────────────────────────────────────

TBAR_POS_X = "pos X (m)"
TBAR_POS_Y = "pos Y (m)"
TBAR_POS_Z = "pos Z (m)"
TBAR_ROLL = "roll (rad)"
TBAR_PITCH = "pitch (rad)"
TBAR_YAW = "yaw (rad)"
TBAR_CAM_SEL = "camera"
TBAR_FX = "focal X"
TBAR_FY = "focal Y"
TBAR_CX = "centre X"
TBAR_CY = "centre Y"
TBAR_PPM = "px / metre"
TBAR_GROUND = "ground Z (m)"

# Trackbar scales — map integer [0, N] → parameter range
SCALES = {
    TBAR_POS_X: (-0.5, 0.5, 200),
    TBAR_POS_Y: (-0.5, 0.5, 200),
    TBAR_POS_Z: (0.0, 0.5, 200),
    TBAR_ROLL: (-0.6, 0.6, 200),
    TBAR_PITCH: (-0.9, 0.9, 200),
    TBAR_YAW: (-3.1416, 3.1416, 200),
    TBAR_FX: (100.0, 800.0, 700),
    TBAR_FY: (100.0, 800.0, 700),
    TBAR_CX: (0.0, 800.0, 800),
    TBAR_CY: (0.0, 600.0, 600),
    TBAR_PPM: (20.0, 200.0, 180),
    TBAR_GROUND: (-0.3, 0.3, 120),
}


def _trackbar_to_value(name: str, tb_val: int) -> float:
    lo, hi, steps = SCALES[name]
    return lo + (hi - lo) * tb_val / steps


def _value_to_trackbar(name: str, val: float) -> int:
    lo, hi, steps = SCALES[name]
    return int(np.clip((val - lo) / (hi - lo) * steps, 0, steps))


# ── Calibration UI node ───────────────────────────────────────────────────────


class BevCalUI(Node):
    def __init__(
        self,
        camera_names: List[str],
        topics: List[str],
        canvas_size: int = 800,
        src_width: int = 640,
        src_height: int = 480,
        output_path: str = os.path.expanduser("~/bev_calibration.npz"),
    ) -> None:
        super().__init__("bev_cal_ui")
        self._names = camera_names
        self._canvas = canvas_size
        self._src_w = src_width
        self._src_h = src_height
        self._output = output_path
        self._bridge = CvBridge()

        self._images: Dict[str, Optional[np.ndarray]] = {n: None for n in camera_names}
        self._blend_weights: Optional[Dict[str, np.ndarray]] = None
        self._selected_cam = 0
        self._dirty = True  # recompute blend weights when params change

        # Per-camera parameters (indexed by camera index)
        self._params: Dict[str, List[float]] = {
            "pos_x": [],
            "pos_y": [],
            "pos_z": [],
            "roll": [],
            "pitch": [],
            "yaw": [],
            "fx": [],
            "fy": [],
            "cx": [],
            "cy": [],
        }

        # Global parameters
        self._ppm = 80.0
        self._ground_z = 0.0

        # Initialise from presets
        for i, name in enumerate(self._names):
            cfg = DEFAULT_CAMERA_CONFIGS.get(name, {})
            pos = cfg.get("position", [0.0, 0.0, 0.1])
            rpy = cfg.get("orientation_rpy", [0.0, 0.3, 0.0])
            self._params["pos_x"].append(pos[0])
            self._params["pos_y"].append(pos[1])
            self._params["pos_z"].append(pos[2])
            self._params["yaw"].append(rpy[0])
            self._params["pitch"].append(rpy[1])
            self._params["roll"].append(rpy[2])
            self._params["fx"].append(DEFAULT_INTRINSICS["fx"])
            self._params["fy"].append(DEFAULT_INTRINSICS["fy"])
            self._params["cx"].append(DEFAULT_INTRINSICS["cx"])
            self._params["cy"].append(DEFAULT_INTRINSICS["cy"])

        # Subscribers
        for name, topic in zip(camera_names, topics):
            self.create_subscription(
                Image, topic, lambda msg, n=name: self._img_cb(msg, n), 10
            )
            self.get_logger().info(f"Subscribed: {topic}")

        # Create UI window
        self._win = "OhhO View — BEV Calibration"
        cv2.namedWindow(self._win, cv2.WINDOW_NORMAL)
        cv2.resizeWindow(self._win, self._canvas, self._canvas)

        # Control panel window
        ctrl_win = "Controls"
        cv2.namedWindow(ctrl_win, cv2.WINDOW_NORMAL)
        cv2.resizeWindow(ctrl_win, 480, 600)

        # Camera selector
        max_cam = max(len(self._names) - 1, 0)
        cv2.createTrackbar(TBAR_CAM_SEL, ctrl_win, 0, max_cam, self._on_cam_sel)

        # Per-camera DOF trackbars
        for name in [
            TBAR_POS_X,
            TBAR_POS_Y,
            TBAR_POS_Z,
            TBAR_ROLL,
            TBAR_PITCH,
            TBAR_YAW,
        ]:
            _, _, steps = SCALES[name]
            cv2.createTrackbar(name, ctrl_win, 0, steps, self._on_trackbar)

        # Intrinsic trackbars
        for name in [TBAR_FX, TBAR_FY, TBAR_CX, TBAR_CY]:
            _, _, steps = SCALES[name]
            cv2.createTrackbar(name, ctrl_win, 0, steps, self._on_trackbar)

        # Global params
        _, _, steps = SCALES[TBAR_PPM]
        cv2.createTrackbar(TBAR_PPM, ctrl_win, 0, steps, self._on_trackbar)
        _, _, steps = SCALES[TBAR_GROUND]
        cv2.createTrackbar(TBAR_GROUND, ctrl_win, 0, steps, self._on_trackbar)

        # Initialise trackbar positions from defaults
        self._sync_trackbars_to_params()

        self._info = [
            f"Cameras: {', '.join(self._names)}",
            f"Canvas: {self._canvas}×{self._canvas}",
            "Controls: s=save  q=quit",
            "Select camera with top trackbar,",
            "then adjust its 6-DOF pose.",
            "",
            f"Output: {self._output}",
        ]

    # ── Image callback ────────────────────────────────────────────────────────

    def _img_cb(self, msg: Image, name: str) -> None:
        try:
            img = self._bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
            self._images[name] = np.asarray(img, dtype=np.uint8)
        except Exception as exc:
            self.get_logger().warn(
                f"[{name}] image error: {exc}", throttle_duration_sec=5.0
            )

    # ── Trackbar callbacks ────────────────────────────────────────────────────

    def _on_cam_sel(self, val: int) -> None:
        if val != self._selected_cam:
            self._selected_cam = val
            self._sync_trackbars_to_params()

    def _on_trackbar(self, val: int) -> None:
        self._dirty = True

    def _read_params_from_trackbars(self) -> None:
        """Read all trackbar values and update parameter arrays."""
        ctrl = "Controls"
        ci = self._selected_cam

        # Read selected camera's DOF params
        for key, tb_name in [
            ("pos_x", TBAR_POS_X),
            ("pos_y", TBAR_POS_Y),
            ("pos_z", TBAR_POS_Z),
            ("roll", TBAR_ROLL),
            ("pitch", TBAR_PITCH),
            ("yaw", TBAR_YAW),
        ]:
            tb_val = cv2.getTrackbarPos(tb_name, ctrl)
            self._params[key][ci] = _trackbar_to_value(tb_name, tb_val)

        # Read global params
        self._ppm = _trackbar_to_value(TBAR_PPM, cv2.getTrackbarPos(TBAR_PPM, ctrl))
        self._ground_z = _trackbar_to_value(
            TBAR_GROUND, cv2.getTrackbarPos(TBAR_GROUND, ctrl)
        )

        # Read intrinsics for selected camera
        self._params["fx"][ci] = _trackbar_to_value(
            TBAR_FX, cv2.getTrackbarPos(TBAR_FX, ctrl)
        )
        self._params["fy"][ci] = _trackbar_to_value(
            TBAR_FY, cv2.getTrackbarPos(TBAR_FY, ctrl)
        )
        self._params["cx"][ci] = _trackbar_to_value(
            TBAR_CX, cv2.getTrackbarPos(TBAR_CX, ctrl)
        )
        self._params["cy"][ci] = _trackbar_to_value(
            TBAR_CY, cv2.getTrackbarPos(TBAR_CY, ctrl)
        )

        self._dirty = False
        self._blend_weights = None  # force recompute

    def _sync_trackbars_to_params(self) -> None:
        """Set trackbar positions from current parameter values for selected camera."""
        ctrl = "Controls"
        ci = self._selected_cam

        for key, tb_name in [
            ("pos_x", TBAR_POS_X),
            ("pos_y", TBAR_POS_Y),
            ("pos_z", TBAR_POS_Z),
            ("roll", TBAR_ROLL),
            ("pitch", TBAR_PITCH),
            ("yaw", TBAR_YAW),
        ]:
            val = self._params[key][ci]
            cv2.setTrackbarPos(tb_name, ctrl, _value_to_trackbar(tb_name, val))

        for key, tb_name in [
            ("fx", TBAR_FX),
            ("fy", TBAR_FY),
            ("cx", TBAR_CX),
            ("cy", TBAR_CY),
        ]:
            val = self._params[key][ci]
            cv2.setTrackbarPos(tb_name, ctrl, _value_to_trackbar(tb_name, val))

        cv2.setTrackbarPos(TBAR_PPM, ctrl, _value_to_trackbar(TBAR_PPM, self._ppm))
        cv2.setTrackbarPos(
            TBAR_GROUND, ctrl, _value_to_trackbar(TBAR_GROUND, self._ground_z)
        )

    # ── Homographies & rendering ──────────────────────────────────────────────

    def _compute_homographies(self) -> Dict[str, np.ndarray]:
        """Build homographies from current parameter state."""
        Hs: Dict[str, np.ndarray] = {}
        for i, name in enumerate(self._names):
            R = camera_rotation(
                self._params["yaw"][i],
                self._params["pitch"][i],
                self._params["roll"][i],
            )
            t = np.array(
                [
                    self._params["pos_x"][i],
                    self._params["pos_y"][i],
                    self._params["pos_z"][i],
                ],
                dtype=np.float64,
            )
            K = np.array(
                [
                    [self._params["fx"][i], 0.0, self._params["cx"][i]],
                    [0.0, self._params["fy"][i], self._params["cy"][i]],
                    [0.0, 0.0, 1.0],
                ],
                dtype=np.float64,
            )
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

    def _compute_blend_weights(
        self, homographies: Dict[str, np.ndarray]
    ) -> Dict[str, np.ndarray]:
        """Compute per-camera blend-weight maps for the current homographies."""
        weights: Dict[str, np.ndarray] = {}
        src_ones = np.ones((self._src_h, self._src_w), dtype=np.float32)
        for name in self._names:
            w = cv2.warpPerspective(
                src_ones, homographies[name], (self._canvas, self._canvas)
            )
            weights[name] = w
        return weights

    def _render_bev(self) -> np.ndarray:
        """Stitch and render the current BEV."""
        if self._dirty:
            self._read_params_from_trackbars()

        Hs = self._compute_homographies()
        if self._blend_weights is None:
            self._blend_weights = self._compute_blend_weights(Hs)

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
                Hs[name],
                (self._canvas, self._canvas),
            )
            w = self._blend_weights[name][:, :, np.newaxis]
            canvas += warped * w
            w_sum += w

        if not any_img:
            # Return a dark placeholder
            out = np.zeros((self._canvas, self._canvas, 3), dtype=np.uint8)
        else:
            mask = w_sum[:, :, 0] > 0
            canvas[mask] /= w_sum[mask]
            out = canvas.clip(0, 255).astype(np.uint8)

        return cv2.cvtColor(out, cv2.COLOR_RGB2BGR)

    def _draw_overlay(self, bev: np.ndarray) -> np.ndarray:
        """Draw overlay info on the BEV image."""
        import cv2 as _cv

        h, w = bev.shape[:2]
        # Robot centre crosshair
        cx, cy = w // 2, h // 2
        _cv.line(bev, (cx - 20, cy), (cx + 20, cy), (0, 255, 200), 1)
        _cv.line(bev, (cx, cy - 20), (cx, cy + 20), (0, 255, 200), 1)
        _cv.circle(bev, (cx, cy), 4, (0, 255, 200), 1)

        # Selected camera label
        sel_name = self._names[self._selected_cam]
        _cv.putText(
            bev,
            f"Editing: {sel_name}",
            (10, 24),
            _cv.FONT_HERSHEY_SIMPLEX,
            0.5,
            (0, 255, 200),
            1,
        )

        # Info lines
        for i, line in enumerate(self._info):
            _cv.putText(
                bev,
                line,
                (10, h - 10 - (len(self._info) - 1 - i) * 16),
                _cv.FONT_HERSHEY_SIMPLEX,
                0.4,
                (180, 180, 180),
                1,
            )

        return bev

    # ── Save ──────────────────────────────────────────────────────────────────

    def _save(self) -> None:
        """Save current homographies to .npz file."""
        Hs = self._compute_homographies()
        out_dir = os.path.dirname(os.path.abspath(self._output))
        os.makedirs(out_dir, exist_ok=True)
        np.savez(self._output, **Hs)
        self.get_logger().info(f"Saved {len(Hs)} homographies → {self._output}")
        print(f"\n✓ Calibration saved to {self._output}")

        # Also save per-camera params as a human-readable YAML-ish file
        params_path = self._output.replace(".npz", "_params.txt")
        try:
            with open(params_path, "w") as f:
                f.write("# OhhO View calibration — IPM parameters\n")
                f.write("# Generated by bev_cal_ui\n")
                f.write(f"pixels_per_meter: {self._ppm:.2f}\n")
                f.write(f"ground_z: {self._ground_z:.3f}\n")
                f.write(f"canvas_size: {self._canvas}\n")
                f.write(f"src_width: {self._src_w}\n")
                f.write(f"src_height: {self._src_h}\n\n")
                for i, name in enumerate(self._names):
                    f.write(f"{name}:\n")
                    f.write(
                        f"  position: [{self._params['pos_x'][i]:.4f}, {self._params['pos_y'][i]:.4f}, {self._params['pos_z'][i]:.4f}]\n"
                    )
                    f.write(
                        f"  orientation_rpy: [{self._params['yaw'][i]:.4f}, {self._params['pitch'][i]:.4f}, {self._params['roll'][i]:.4f}]\n"
                    )
                    f.write(f"  fx: {self._params['fx'][i]:.1f}\n")
                    f.write(f"  fy: {self._params['fy'][i]:.1f}\n")
                    f.write(f"  cx: {self._params['cx'][i]:.1f}\n")
                    f.write(f"  cy: {self._params['cy'][i]:.1f}\n\n")
            self.get_logger().info(f"Saved params reference → {params_path}")
        except Exception as exc:
            self.get_logger().warn(f"Could not save params file: {exc}")

    # ── Main loop ─────────────────────────────────────────────────────────────

    def run(self) -> None:
        """Run the interactive calibration loop."""
        print(f"\n{'=' * 50}")
        print("  OhhO View — BEV Calibration UI")
        print(f"  Cameras: {', '.join(self._names)}")
        print(f"  Output:  {self._output}")
        print(f"{'=' * 50}")
        print("  s      = save & continue")
        print("  q/ESC  = quit")
        print("  Use the Controls window trackbars to align cameras.")
        print(f"{'=' * 50}\n")

        while rclpy.ok():
            rclpy.spin_once(self, timeout_sec=0.01)

            bev = self._render_bev()
            bev = self._draw_overlay(bev)
            cv2.imshow(self._win, bev)

            key = cv2.waitKey(1) & 0xFF
            if key == ord("q") or key == 27:  # q or ESC
                break
            elif key == ord("s"):
                self._save()

        cv2.destroyAllWindows()


# ── CLI entry point ───────────────────────────────────────────────────────────


def main(args=None) -> None:
    parser = argparse.ArgumentParser(
        description="OhhO View — interactive BEV calibration UI"
    )
    parser.add_argument(
        "--camera-names",
        nargs="+",
        default=["front", "rear", "left", "right"],
        help="Camera names (default: front rear left right)",
    )
    parser.add_argument(
        "--input-topics",
        nargs="+",
        default=None,
        help="Topics per camera (default: /camera/<name>/image_raw)",
    )
    parser.add_argument("--canvas-size", type=int, default=800)
    parser.add_argument("--src-width", type=int, default=640)
    parser.add_argument("--src-height", type=int, default=480)
    parser.add_argument(
        "--output",
        default=os.path.expanduser("~/bev_calibration.npz"),
        help="Output .npz file path",
    )

    parsed, ros_args = parser.parse_known_args(args)

    topics = parsed.input_topics or [
        f"/camera/{n}/image_raw" for n in parsed.camera_names
    ]

    rclpy.init(args=ros_args)
    node = BevCalUI(
        camera_names=parsed.camera_names,
        topics=topics,
        canvas_size=parsed.canvas_size,
        src_width=parsed.src_width,
        src_height=parsed.src_height,
        output_path=parsed.output,
    )
    node.run()
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
