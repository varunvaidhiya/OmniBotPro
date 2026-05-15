#!/usr/bin/env python3
"""
SO-101 arm driver node for OmniBot mobile manipulation.

Interfaces with the SO-ARM100/SO-101 6-DOF arm via FeetechMotorsBus (LeRobot).
Falls back to simulation mode if lerobot is not installed.

Topics published:
  /arm/joint_states  (sensor_msgs/JointState)  - follower arm positions
  /arm/leader_states (sensor_msgs/JointState)  - leader arm positions (teleop_mode only)

Topics subscribed:
  /arm/joint_commands/out (sensor_msgs/JointState) - commanded positions for follower
                          routed via arm_cmd_mux (omnibot_rl) which selects between
                          SmolVLA (/arm/joint_commands) and RL (/arm/joint_commands/rl)
  /arm/enable             (std_msgs/Bool)           - enable/disable torque
"""

import collections
import math
import statistics as _statistics
import time
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import JointState
from std_msgs.msg import Bool

# ---------------------------------------------------------------------------
# Optional LeRobot import
# ---------------------------------------------------------------------------
try:
    from lerobot.common.robot_devices.motors.feetech import FeetechMotorsBus

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False


class ArmDriverNode(Node):
    """ROS 2 driver for the SO-101 arm using FeetechMotorsBus."""

    def __init__(self):
        super().__init__("arm_driver_node")

        # ------------------------------------------------------------------
        # Parameters
        # ------------------------------------------------------------------
        self.declare_parameter("follower_port", "/dev/ttyACM0")
        self.declare_parameter("leader_port", "/dev/ttyACM1")
        self.declare_parameter("baudrate", 1000000)
        self.declare_parameter("publish_rate", 100.0)
        self.declare_parameter("teleop_mode", False)
        self.declare_parameter("ticks_per_rev", 4096)
        self.declare_parameter(
            "joint_names",
            [
                "arm_shoulder_pan",
                "arm_shoulder_lift",
                "arm_elbow_flex",
                "arm_wrist_flex",
                "arm_wrist_roll",
                "arm_gripper",
            ],
        )
        self.declare_parameter("motor_ids", [1, 2, 3, 4, 5, 6])
        self.declare_parameter("home_ticks", [2048, 2048, 2048, 2048, 2048, 2048])
        self.declare_parameter("joint_min", [-3.14, -1.57, -1.57, -1.57, -3.14, -0.1])
        self.declare_parameter("joint_max", [3.14, 1.57, 1.57, 1.57, 3.14, 0.8])
        # Set True to publish rolling cycle-time stats to /diagnostics at 1 Hz.
        self.declare_parameter("publish_diagnostics", False)

        self.follower_port = self.get_parameter("follower_port").value
        self.leader_port = self.get_parameter("leader_port").value
        self.baudrate = self.get_parameter("baudrate").value
        self.publish_rate = self.get_parameter("publish_rate").value
        self.teleop_mode = self.get_parameter("teleop_mode").value
        self.ticks_per_rev = self.get_parameter("ticks_per_rev").value
        self.joint_names = list(self.get_parameter("joint_names").value)
        self.motor_ids = list(self.get_parameter("motor_ids").value)
        self.home_ticks = list(self.get_parameter("home_ticks").value)
        self.joint_min = list(self.get_parameter("joint_min").value)
        self.joint_max = list(self.get_parameter("joint_max").value)

        self.num_joints = len(self.joint_names)
        self.ticks_per_rad = self.ticks_per_rev / (2.0 * math.pi)
        self._diag_enabled = self.get_parameter("publish_diagnostics").value

        # Rolling timing accumulators (active only when _diag_enabled=True)
        self._t_read_follower = collections.deque(maxlen=100)
        self._t_publish = collections.deque(maxlen=100)
        self._t_read_leader = collections.deque(maxlen=100)

        # ------------------------------------------------------------------
        # State
        # ------------------------------------------------------------------
        self.follower_bus = None
        self.leader_bus = None
        self.sim_positions = [0.0] * self.num_joints  # simulation passthrough
        self.torque_enabled = True

        # ------------------------------------------------------------------
        # Publishers
        # ------------------------------------------------------------------
        self.joint_state_pub = self.create_publisher(
            JointState, "/arm/joint_states", 10
        )
        if self.teleop_mode:
            self.leader_state_pub = self.create_publisher(
                JointState, "/arm/leader_states", 10
            )

        # ------------------------------------------------------------------
        # Subscribers
        # ------------------------------------------------------------------
        self.create_subscription(
            JointState, '/arm/joint_commands/out', self.joint_command_cb, 10)
        self.create_subscription(
            Bool, '/arm/enable', self.enable_cb, 10)

        # ------------------------------------------------------------------
        # Connect hardware
        # ------------------------------------------------------------------
        if LEROBOT_AVAILABLE:
            self.connect_follower()
            if self.teleop_mode:
                self.connect_leader()
        else:
            self.get_logger().warn(
                "lerobot not installed — running in simulation mode. "
                "Joint states will reflect commanded positions with no hardware."
            )

        # ------------------------------------------------------------------
        # Timer
        # ------------------------------------------------------------------
        timer_period = 1.0 / self.publish_rate
        self.create_timer(timer_period, self.publish_states)

        if self._diag_enabled:
            from diagnostic_msgs.msg import DiagnosticArray, DiagnosticStatus, KeyValue
            self._DiagnosticArray = DiagnosticArray
            self._DiagnosticStatus = DiagnosticStatus
            self._KeyValue = KeyValue
            self._diag_pub = self.create_publisher(DiagnosticArray, "/diagnostics", 10)
            self.create_timer(1.0, self._publish_diagnostics)

        self.get_logger().info(
            f"ArmDriverNode started | hardware={'real' if LEROBOT_AVAILABLE and self.follower_bus else 'sim'} "
            f"| teleop={self.teleop_mode} | joints={self.joint_names}"
        )

    # ------------------------------------------------------------------
    # Hardware connection helpers
    # ------------------------------------------------------------------

    def _motors_dict(self):
        """Build motors dict for FeetechMotorsBus: {name: (id, model)}."""
        return {
            name: (mid, "sts3215")
            for name, mid in zip(self.joint_names, self.motor_ids)
        }

    def connect_follower(self):
        """Connect to the follower arm bus."""
        try:
            self.follower_bus = FeetechMotorsBus(
                port=self.follower_port, motors=self._motors_dict()
            )
            self.follower_bus.connect()
            self.get_logger().info(f"Follower bus connected on {self.follower_port}")
        except Exception as exc:
            self.get_logger().error(
                f"Failed to connect follower bus on {self.follower_port}: {exc}. "
                "Falling back to simulation mode."
            )
            self.follower_bus = None

    def connect_leader(self):
        """Connect to the leader arm bus (teleop mode only)."""
        try:
            self.leader_bus = FeetechMotorsBus(
                port=self.leader_port, motors=self._motors_dict()
            )
            self.leader_bus.connect()
            self.get_logger().info(f"Leader bus connected on {self.leader_port}")
        except Exception as exc:
            self.get_logger().error(
                f"Failed to connect leader bus on {self.leader_port}: {exc}."
            )
            self.leader_bus = None

    # ------------------------------------------------------------------
    # Unit conversions
    # ------------------------------------------------------------------

    def ticks_to_radians(self, ticks_list):
        """Convert raw servo ticks to joint angles in radians."""
        return [
            (ticks - home) / self.ticks_per_rad
            for ticks, home in zip(ticks_list, self.home_ticks)
        ]

    def radians_to_ticks(self, radians_list):
        """Convert joint angles in radians to servo ticks."""
        return [
            int(round(rad * self.ticks_per_rad + home))
            for rad, home in zip(radians_list, self.home_ticks)
        ]

    def clamp_radians(self, radians_list):
        """Clamp joint angles to declared limits."""
        return [
            max(mn, min(mx, rad))
            for rad, mn, mx in zip(radians_list, self.joint_min, self.joint_max)
        ]

    # ------------------------------------------------------------------
    # Publish timer
    # ------------------------------------------------------------------

    def publish_states(self):
        now = self.get_clock().now().to_msg()

        # --- follower arm ---
        _t0 = time.perf_counter() if self._diag_enabled else None
        positions = self._read_follower_positions()
        _t1 = time.perf_counter() if self._diag_enabled else None

        js = JointState()
        js.header.stamp = now
        js.name = self.joint_names
        js.position = positions
        self.joint_state_pub.publish(js)

        _t2 = time.perf_counter() if self._diag_enabled else None

        if self._diag_enabled and _t0 is not None:
            self._t_read_follower.append((_t1 - _t0) * 1000.0)
            self._t_publish.append((_t2 - _t1) * 1000.0)

        # --- leader arm (teleop only) ---
        if self.teleop_mode and hasattr(self, "leader_state_pub"):
            _tl0 = time.perf_counter() if self._diag_enabled else None
            leader_pos = self._read_leader_positions()
            if self._diag_enabled and _tl0 is not None:
                self._t_read_leader.append((time.perf_counter() - _tl0) * 1000.0)

            ljs = JointState()
            ljs.header.stamp = now
            ljs.name = self.joint_names
            ljs.position = leader_pos
            self.leader_state_pub.publish(ljs)

    def _read_follower_positions(self):
        """Read follower arm positions; returns radians list."""
        if self.follower_bus is not None:
            try:
                ticks_dict = self.follower_bus.read("Present_Position")
                ticks = [ticks_dict[name] for name in self.joint_names]
                return self.ticks_to_radians(ticks)
            except Exception as exc:
                self.get_logger().warn(
                    f"Follower read error: {exc}", throttle_duration_sec=5.0
                )
        return list(self.sim_positions)

    def _read_leader_positions(self):
        """Read leader arm positions; returns radians list."""
        if self.leader_bus is not None:
            try:
                ticks_dict = self.leader_bus.read("Present_Position")
                ticks = [ticks_dict[name] for name in self.joint_names]
                return self.ticks_to_radians(ticks)
            except Exception as exc:
                self.get_logger().warn(
                    f"Leader read error: {exc}", throttle_duration_sec=5.0
                )
        return [0.0] * self.num_joints

    # ------------------------------------------------------------------
    # Subscribers
    # ------------------------------------------------------------------

    def _publish_diagnostics(self) -> None:
        msg = self._DiagnosticArray()
        msg.header.stamp = self.get_clock().now().to_msg()

        def _make(name, deque_, warn_ms, err_ms):
            st = self._DiagnosticStatus()
            st.name = name
            if not deque_:
                st.level = self._DiagnosticStatus.OK
                st.message = "no data"
                return st
            s = sorted(deque_)
            n = len(s)
            p95 = s[max(0, int(0.95 * n) - 1)]
            st.level = (
                self._DiagnosticStatus.ERROR if p95 > err_ms
                else self._DiagnosticStatus.WARN if p95 > warn_ms
                else self._DiagnosticStatus.OK
            )
            st.message = f"p95={p95:.2f}ms"
            for k, v in [
                ("mean_ms", _statistics.mean(s)),
                ("p50_ms", s[n // 2]),
                ("p95_ms", p95),
                ("max_ms", s[-1]),
            ]:
                kv = self._KeyValue()
                kv.key = k
                kv.value = f"{v:.3f}"
                st.values.append(kv)
            return st

        msg.status = [
            _make("arm_driver/follower_read_ms", self._t_read_follower, 5.0, 20.0),
            _make("arm_driver/publish_ms", self._t_publish, 1.0, 5.0),
            _make("arm_driver/leader_read_ms", self._t_read_leader, 5.0, 20.0),
        ]
        self._diag_pub.publish(msg)

    def joint_command_cb(self, msg: JointState):
        """Receive commanded joint positions and write to follower arm."""
        if not self.torque_enabled:
            return

        # Build ordered position list matching self.joint_names
        name_to_pos = dict(zip(msg.name, msg.position))
        commanded = [name_to_pos.get(n, 0.0) for n in self.joint_names]
        commanded = self.clamp_radians(commanded)

        if self.follower_bus is not None:
            try:
                ticks = self.radians_to_ticks(commanded)
                values_dict = {
                    name: tick for name, tick in zip(self.joint_names, ticks)
                }
                self.follower_bus.write("Goal_Position", values_dict)
            except Exception as exc:
                self.get_logger().warn(
                    f"Follower write error: {exc}", throttle_duration_sec=5.0
                )
        else:
            # Simulation mode: passthrough
            self.sim_positions = commanded

    def enable_cb(self, msg: Bool):
        """Enable or disable torque on the follower arm."""
        self.torque_enabled = msg.data
        status = "enabled" if msg.data else "disabled"
        self.get_logger().info(f"Arm torque {status}")

        if self.follower_bus is not None:
            try:
                torque_val = 1 if msg.data else 0
                values_dict = {name: torque_val for name in self.joint_names}
                self.follower_bus.write("Torque_Enable", values_dict)
            except Exception as exc:
                self.get_logger().warn(f"Torque write error: {exc}")

    # ------------------------------------------------------------------
    # Cleanup
    # ------------------------------------------------------------------

    def destroy_node(self):
        """Disconnect buses before shutdown."""
        if self.follower_bus is not None:
            try:
                self.follower_bus.disconnect()
                self.get_logger().info("Follower bus disconnected.")
            except Exception as exc:
                self.get_logger().warn(f"Error disconnecting follower: {exc}")
        if self.leader_bus is not None:
            try:
                self.leader_bus.disconnect()
                self.get_logger().info("Leader bus disconnected.")
            except Exception as exc:
                self.get_logger().warn(f"Error disconnecting leader: {exc}")
        super().destroy_node()


def main(args=None):
    rclpy.init(args=args)
    node = ArmDriverNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
