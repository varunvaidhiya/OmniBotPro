#!/usr/bin/env python3
"""
cmd_vel Multiplexer for OmniBot Hybrid Control.

Selects one cmd_vel source based on the active control mode and
forwards it to /cmd_vel/out, which the robot driver reads.

Topic routing
─────────────
  Inputs (one active at a time):
    /cmd_vel          ← Nav2 velocity_smoother (native Nav2 output)
    /cmd_vel/vla      ← VLA inference node
    /cmd_vel/teleop   ← keyboard / joystick teleoperation
    /cmd_vel/rl       ← Isaac Lab RL navigation policy (rl_nav_node)

  Mode control:
    /control_mode     ← std_msgs/String  "nav2" | "vla" | "teleop" | "rl_nav"

  Output:
    /cmd_vel/out      → robot driver (remapped from /cmd_vel in hybrid launch)

  Feedback:
    /control_mode/active  → currently selected mode (published at 1 Hz)

Usage
─────
  ros2 topic pub /control_mode std_msgs/msg/String "data: 'vla'"
  ros2 topic pub /control_mode std_msgs/msg/String "data: 'nav2'"
  ros2 topic pub /control_mode std_msgs/msg/String "data: 'teleop'"
  ros2 topic pub /control_mode std_msgs/msg/String "data: 'rl_nav'"
"""

import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist
from std_msgs.msg import String


class CmdVelMux(Node):
    """
    Mode-based cmd_vel multiplexer.

    Parameters
    ----------
    default_mode : str
        Starting mode before any /control_mode message arrives.
        One of "nav2" (default), "vla", "teleop".
    """

    VALID_MODES = ("nav2", "vla", "teleop", "rl_nav")

    def __init__(self):
        super().__init__("cmd_vel_mux")

        self.declare_parameter("default_mode", "nav2")
        self._active_mode: str = self.get_parameter("default_mode").value

        # ── Inputs ────────────────────────────────────────────────────────────
        self.create_subscription(Twist, "/cmd_vel", self._nav2_cb, 10)
        self.create_subscription(Twist, "/cmd_vel/vla", self._vla_cb, 10)
        self.create_subscription(Twist, "/cmd_vel/teleop", self._teleop_cb, 10)
        self.create_subscription(Twist, "/cmd_vel/rl", self._rl_nav_cb, 10)
        self.create_subscription(String, "/control_mode", self._mode_cb, 10)

        # ── Output ────────────────────────────────────────────────────────────
        self._out_pub = self.create_publisher(Twist, "/cmd_vel/out", 10)
        self._active_mode_pub = self.create_publisher(
            String, "/control_mode/active", 10
        )

        # Publish active mode at 1 Hz so other nodes can query it
        self.create_timer(1.0, self._publish_active_mode)

        self.get_logger().info(
            f'CmdVelMux ready. Default mode: "{self._active_mode}". '
            f"Valid modes: {self.VALID_MODES}"
        )

    # ── Mode switch ───────────────────────────────────────────────────────────

    def _mode_cb(self, msg: String) -> None:
        mode = msg.data.strip().lower()
        if mode not in self.VALID_MODES:
            self.get_logger().warn(
                f'Unknown mode "{mode}" ignored. Valid: {self.VALID_MODES}'
            )
            return
        if mode != self._active_mode:
            self.get_logger().info(
                f"[CmdVelMux] Mode switch: {self._active_mode} → {mode}"
            )
            self._active_mode = mode
            self._publish_active_mode()

    # ── Source callbacks ──────────────────────────────────────────────────────

    def _nav2_cb(self, msg: Twist) -> None:
        if self._active_mode == "nav2":
            self._out_pub.publish(msg)

    def _vla_cb(self, msg: Twist) -> None:
        if self._active_mode == "vla":
            self._out_pub.publish(msg)

    def _teleop_cb(self, msg: Twist) -> None:
        if self._active_mode == "teleop":
            self._out_pub.publish(msg)

    def _rl_nav_cb(self, msg: Twist) -> None:
        if self._active_mode == "rl_nav":
            self._out_pub.publish(msg)

    # ── Periodic feedback ─────────────────────────────────────────────────────

    def _publish_active_mode(self) -> None:
        msg = String()
        msg.data = self._active_mode
        self._active_mode_pub.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    node = CmdVelMux()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
