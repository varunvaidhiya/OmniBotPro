#!/usr/bin/env python3
"""
Arm Command Multiplexer for OmniBot RL integration.

Mirrors the design of cmd_vel_mux.py exactly, selecting one arm joint command
source and forwarding it to /arm/joint_commands/out.

arm_driver_node subscribes to /arm/joint_commands/out (remapped from the
original /arm/joint_commands).

Topic routing
─────────────
  Inputs (one active at a time):
    /arm/joint_commands        ← SmolVLA / Android / teleop_recorder
    /arm/joint_commands/rl     ← RL arm policy (rl_arm_node)

  Mode control:
    /arm/cmd_mode  ← std_msgs/String  "smolvla" | "rl_arm"

  Output:
    /arm/joint_commands/out  → arm_driver_node

  Feedback:
    /arm/cmd_mode/active  → currently selected mode (published at 1 Hz)

Usage
─────
  # Switch to RL arm control
  ros2 topic pub /arm/cmd_mode std_msgs/msg/String "data: 'rl_arm'"

  # Return to SmolVLA
  ros2 topic pub /arm/cmd_mode std_msgs/msg/String "data: 'smolvla'"

When mode is "smolvla" (default), this node is a transparent pass-through —
SmolVLA and the Android app continue to work without any changes.
"""

import rclpy
from rclpy.node import Node
from sensor_msgs.msg import JointState
from std_msgs.msg import String


class ArmCmdMux(Node):
    """
    Mode-based arm joint command multiplexer.

    Parameters
    ----------
    default_mode : str
        Starting mode. One of "smolvla" (default) or "rl_arm".
    """

    VALID_MODES = ('smolvla', 'rl_arm')

    def __init__(self):
        super().__init__('arm_cmd_mux')

        self.declare_parameter('default_mode', 'smolvla')
        self._active_mode: str = self.get_parameter('default_mode').value

        # ── Inputs ────────────────────────────────────────────────────────────
        self.create_subscription(
            JointState, '/arm/joint_commands',    self._smolvla_cb, 10)
        self.create_subscription(
            JointState, '/arm/joint_commands/rl', self._rl_arm_cb,  10)
        self.create_subscription(
            String, '/arm/cmd_mode',              self._mode_cb,    10)

        # ── Output ────────────────────────────────────────────────────────────
        self._out_pub = self.create_publisher(
            JointState, '/arm/joint_commands/out', 10)
        self._active_mode_pub = self.create_publisher(
            String, '/arm/cmd_mode/active', 10)

        # Publish active mode at 1 Hz
        self.create_timer(1.0, self._publish_active_mode)

        self.get_logger().info(
            f'ArmCmdMux ready. Default mode: "{self._active_mode}". '
            f'Valid modes: {self.VALID_MODES}')

    # ── Mode switch ───────────────────────────────────────────────────────────

    def _mode_cb(self, msg: String) -> None:
        mode = msg.data.strip().lower()
        if mode not in self.VALID_MODES:
            self.get_logger().warn(
                f'Unknown arm mode "{mode}" ignored. Valid: {self.VALID_MODES}')
            return
        if mode != self._active_mode:
            self.get_logger().info(
                f'[ArmCmdMux] Mode switch: {self._active_mode} → {mode}')
            self._active_mode = mode
            self._publish_active_mode()

    # ── Source callbacks ──────────────────────────────────────────────────────

    def _smolvla_cb(self, msg: JointState) -> None:
        if self._active_mode == 'smolvla':
            self._out_pub.publish(msg)

    def _rl_arm_cb(self, msg: JointState) -> None:
        if self._active_mode == 'rl_arm':
            self._out_pub.publish(msg)

    # ── Periodic feedback ─────────────────────────────────────────────────────

    def _publish_active_mode(self) -> None:
        msg = String()
        msg.data = self._active_mode
        self._active_mode_pub.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    node = ArmCmdMux()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
