#!/usr/bin/env python3
"""
rosbag_recorder — remote-controlled rosbag recording for the Android app.

The Android app publishes:
  /rosbag_recorder/start   std_msgs/String  — bag name (empty = timestamp)
  /rosbag_recorder/stop    std_msgs/Empty

This node spawns / terminates a `ros2 bag record` subprocess accordingly
and reports state on:
  /rosbag_recorder/status  std_msgs/String  — "idle" | "recording:<path>"

Parameters
  output_dir   (string, default '~/rosbags')
  topics       (string list, default [] = record key telemetry set)
  record_all   (bool, default False) — record every topic (-a). Heavy on
               the Pi when cameras are streaming; default records the
               curated telemetry list instead.
"""

import datetime
import os
import signal
import subprocess

import rclpy
from rclpy.node import Node
from std_msgs.msg import Empty, String

# Curated default: full telemetry without raw camera floods
DEFAULT_TOPICS = [
    "/odom",
    "/odometry/filtered",
    "/imu/data",
    "/scan",
    "/map",
    "/tf",
    "/tf_static",
    "/cmd_vel/out",
    "/control_mode/active",
    "/arm/joint_states",
    "/arm/joint_commands/out",
    "/mission/command",
    "/mission/status",
    "/ai/status",
    "/perception/object_info",
    "/perception/nearest_distance",
    "/diagnostics",
]


class RosbagRecorderNode(Node):
    def __init__(self):
        super().__init__("rosbag_recorder")
        self.declare_parameter("output_dir", "~/rosbags")
        self.declare_parameter("topics", [""])
        self.declare_parameter("record_all", False)

        self._out_dir = os.path.expanduser(self.get_parameter("output_dir").value)
        topics = [t for t in self.get_parameter("topics").value if t]
        self._topics = topics or DEFAULT_TOPICS
        self._record_all = bool(self.get_parameter("record_all").value)
        self._proc = None
        self._bag_path = ""

        self.create_subscription(String, "/rosbag_recorder/start", self._start_cb, 5)
        self.create_subscription(Empty, "/rosbag_recorder/stop", self._stop_cb, 5)
        self._status_pub = self.create_publisher(String, "/rosbag_recorder/status", 5)
        self.create_timer(1.0, self._publish_status)

        self.get_logger().info(
            f"rosbag_recorder ready | dir={self._out_dir} | "
            f"{'ALL topics' if self._record_all else f'{len(self._topics)} topics'}"
        )

    def _start_cb(self, msg: String) -> None:
        if self._proc is not None and self._proc.poll() is None:
            self.get_logger().warn("Already recording — ignoring start request.")
            return
        name = msg.data.strip() or datetime.datetime.now().strftime(
            "omnibot_%Y%m%d_%H%M%S"
        )
        # Sanitise: bag name becomes a directory component
        name = "".join(c for c in name if c.isalnum() or c in "-_") or "omnibot_bag"
        os.makedirs(self._out_dir, exist_ok=True)
        self._bag_path = os.path.join(self._out_dir, name)

        cmd = ["ros2", "bag", "record", "-o", self._bag_path]
        cmd += ["-a"] if self._record_all else self._topics
        try:
            self._proc = subprocess.Popen(
                cmd,
                stdout=subprocess.DEVNULL,
                stderr=subprocess.DEVNULL,
                start_new_session=True,  # own process group for clean SIGINT
            )
            self.get_logger().info(f"Recording → {self._bag_path}")
        except Exception as exc:
            self.get_logger().error(f"Failed to start recording: {exc}")
            self._proc = None

    def _stop_cb(self, _msg: Empty) -> None:
        if self._proc is None or self._proc.poll() is not None:
            self.get_logger().warn("Not recording — ignoring stop request.")
            self._proc = None
            return
        try:
            # SIGINT lets ros2 bag flush and finalise the bag cleanly
            os.killpg(os.getpgid(self._proc.pid), signal.SIGINT)
            self._proc.wait(timeout=10)
            self.get_logger().info(f"Recording stopped: {self._bag_path}")
        except Exception as exc:
            self.get_logger().error(f"Stop error: {exc} — killing process.")
            self._proc.kill()
        finally:
            self._proc = None

    def _publish_status(self) -> None:
        recording = self._proc is not None and self._proc.poll() is None
        msg = String()
        msg.data = f"recording:{self._bag_path}" if recording else "idle"
        self._status_pub.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    node = RosbagRecorderNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        if node._proc is not None and node._proc.poll() is None:
            try:
                os.killpg(os.getpgid(node._proc.pid), signal.SIGINT)
                node._proc.wait(timeout=10)
            except Exception:
                node._proc.kill()
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
