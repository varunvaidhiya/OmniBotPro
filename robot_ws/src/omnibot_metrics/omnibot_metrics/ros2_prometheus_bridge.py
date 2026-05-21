#!/usr/bin/env python3
"""
OmniBot Prometheus metrics bridge.

Subscribes to ROS 2 topics (/diagnostics, /odom, /mission/*, /arm/joint_states,
/emergency_stop, /control_mode/active, /ai/command) and exposes all collected
data as Prometheus metrics via an HTTP server.

Run one instance on the Pi (port 8888) and one on the GPU desktop (port 8889)
so Prometheus can scrape both machines independently.

Usage:
  ros2 run omnibot_metrics metrics_bridge --ros-args \
      -p metrics_port:=8888 \
      -p machine_label:=raspberry_pi
"""

import threading

import rclpy
from rclpy.node import Node
from rclpy.qos import QoSProfile, ReliabilityPolicy, DurabilityPolicy

from diagnostic_msgs.msg import DiagnosticArray
from nav_msgs.msg import Odometry
from sensor_msgs.msg import JointState
from std_msgs.msg import Bool, String

try:
    from prometheus_client import (
        Counter,
        Gauge,
        Info,
        start_http_server,
    )

    _PROMETHEUS_AVAILABLE = True
except ImportError:
    _PROMETHEUS_AVAILABLE = False


# ── Metric definitions ─────────────────────────────────────────────────────────
# All metrics are module-level singletons (Prometheus client enforces uniqueness).

if _PROMETHEUS_AVAILABLE:
    # Control loop cycle times (sourced from /diagnostics published by each node)
    NODE_CYCLE_P50 = Gauge(
        "omnibot_node_cycle_p50_ms", "P50 cycle time ms", ["node", "machine"]
    )
    NODE_CYCLE_P95 = Gauge(
        "omnibot_node_cycle_p95_ms", "P95 cycle time ms", ["node", "machine"]
    )
    NODE_CYCLE_MAX = Gauge(
        "omnibot_node_cycle_max_ms", "Max cycle time ms", ["node", "machine"]
    )

    # VLA inference latency (sourced from /diagnostics on the vla_node)
    VLA_INFERENCE_MS = Gauge(
        "omnibot_vla_inference_ms", "VLA model inference latency ms"
    )
    VLA_PREPROCESS_MS = Gauge(
        "omnibot_vla_preprocess_ms", "VLA image preprocess latency ms"
    )

    # Robot odometry
    ROBOT_VX = Gauge("omnibot_robot_vx_ms", "Robot linear x velocity m/s")
    ROBOT_VY = Gauge("omnibot_robot_vy_ms", "Robot linear y velocity m/s")
    ROBOT_OMEGA = Gauge("omnibot_robot_omega_rads", "Robot angular z velocity rad/s")

    # Arm joints
    ARM_JOINT_POS = Gauge(
        "omnibot_arm_joint_pos_rad", "Arm joint position rad", ["joint"]
    )
    ARM_JOINT_VEL = Gauge(
        "omnibot_arm_joint_vel_rads", "Arm joint velocity rad/s", ["joint"]
    )

    # Mission tracking
    MISSIONS_TOTAL = Counter(
        "omnibot_missions_total", "Total missions issued", ["type"]
    )
    MISSIONS_DONE = Counter(
        "omnibot_missions_done_total", "Missions completed", ["type", "result"]
    )

    # Safety
    ESTOP_ACTIVE = Gauge("omnibot_estop_active", "1 if emergency stop is active")

    # Control mode (Info metric — string label, not numeric)
    CONTROL_MODE = Info("omnibot_control_mode", "Active control mode")

    # AI commands
    AI_COMMANDS_TOTAL = Counter(
        "omnibot_ai_commands_total", "Natural language AI commands received"
    )

    # RL policy timing (sourced from /diagnostics on rl_nav_node / rl_arm_node)
    RL_INFER_MS = Gauge(
        "omnibot_rl_inference_ms", "RL policy inference latency ms", ["policy"]
    )


class ROS2PrometheusBridge(Node):
    """Subscribes to key OmniBot topics and exports them as Prometheus metrics."""

    def __init__(self) -> None:
        super().__init__("ros2_prometheus_bridge")

        self.declare_parameter("metrics_port", 8888)
        self.declare_parameter("machine_label", "unknown")

        self._port: int = self.get_parameter("metrics_port").value
        self._machine: str = self.get_parameter("machine_label").value

        if not _PROMETHEUS_AVAILABLE:
            self.get_logger().fatal(
                "prometheus_client is not installed. Run: pip install prometheus_client"
            )
            return

        # Start HTTP server for Prometheus scraping
        start_http_server(self._port)
        self.get_logger().info(
            f"Prometheus metrics available at http://0.0.0.0:{self._port}/metrics "
            f"(machine={self._machine})"
        )

        # Track last mission type so we can label outcome counters correctly
        self._last_mission_type: str = "unknown"
        self._mission_lock = threading.Lock()

        self._setup_subscriptions()

    def _setup_subscriptions(self) -> None:
        best_effort_qos = QoSProfile(
            depth=10,
            reliability=ReliabilityPolicy.BEST_EFFORT,
            durability=DurabilityPolicy.VOLATILE,
        )
        reliable_qos = QoSProfile(depth=10)

        # /diagnostics carries timing stats from all nodes that enable it
        self.create_subscription(
            DiagnosticArray, "/diagnostics", self._on_diagnostics, reliable_qos
        )

        # Odometry (best-effort — published at 20 Hz, don't block on drops)
        self.create_subscription(Odometry, "/odom", self._on_odom, best_effort_qos)

        # Control mode feedback
        self.create_subscription(
            String, "/control_mode/active", self._on_control_mode, reliable_qos
        )

        # Mission lifecycle
        self.create_subscription(
            String, "/mission/command", self._on_mission_command, reliable_qos
        )
        self.create_subscription(
            String, "/mission/status", self._on_mission_status, reliable_qos
        )

        # Safety
        self.create_subscription(Bool, "/emergency_stop", self._on_estop, reliable_qos)

        # Arm joints (best-effort — 100 Hz)
        self.create_subscription(
            JointState, "/arm/joint_states", self._on_arm_joints, best_effort_qos
        )

        # AI commands
        self.create_subscription(
            String, "/ai/command", self._on_ai_command, reliable_qos
        )

    # ── Callbacks ──────────────────────────────────────────────────────────────

    def _on_diagnostics(self, msg: DiagnosticArray) -> None:
        for status in msg.status:
            node_name = status.name.replace("/", "_").strip("_")
            kv = {kv_pair.key: kv_pair.value for kv_pair in status.values}

            # Cycle time stats (published by yahboom_controller, arm_driver, etc.)
            for key, gauge in [
                ("p50_ms", NODE_CYCLE_P50),
                ("p95_ms", NODE_CYCLE_P95),
                ("max_ms", NODE_CYCLE_MAX),
            ]:
                if key in kv:
                    try:
                        gauge.labels(node=node_name, machine=self._machine).set(
                            float(kv[key])
                        )
                    except (ValueError, TypeError):
                        pass

            # VLA inference timing (published by vla_node)
            for key, gauge in [
                ("inference_ms", VLA_INFERENCE_MS),
                ("preprocess_ms", VLA_PREPROCESS_MS),
            ]:
                if key in kv:
                    try:
                        gauge.set(float(kv[key]))
                    except (ValueError, TypeError):
                        pass

            # RL policy inference timing (published by rl_nav_node / rl_arm_node)
            if "rl_inference_ms" in kv:
                policy = "nav" if "nav" in node_name else "arm"
                try:
                    RL_INFER_MS.labels(policy=policy).set(float(kv["rl_inference_ms"]))
                except (ValueError, TypeError):
                    pass

    def _on_odom(self, msg: Odometry) -> None:
        ROBOT_VX.set(msg.twist.twist.linear.x)
        ROBOT_VY.set(msg.twist.twist.linear.y)
        ROBOT_OMEGA.set(msg.twist.twist.angular.z)

    def _on_control_mode(self, msg: String) -> None:
        if msg.data:
            CONTROL_MODE.info({"mode": msg.data})

    def _on_mission_command(self, msg: String) -> None:
        raw = msg.data.strip()
        # Parse the mission type from the command string format:
        # "navigate:kitchen,vla:find cup"  → "navigate"
        # "rl_nav:kitchen,rl_arm:pick"     → "rl_nav"
        # "vla:find the cup"               → "vla"
        mtype = raw.split(":")[0] if ":" in raw else "direct"
        with self._mission_lock:
            self._last_mission_type = mtype
        MISSIONS_TOTAL.labels(type=mtype).inc()

    def _on_mission_status(self, msg: String) -> None:
        status = msg.data.lower()
        if "complete" in status or "done" in status or "success" in status:
            result = "success"
        elif "fail" in status or "error" in status or "abort" in status:
            result = "failed"
        else:
            return  # Intermediate status messages (PROCESSING, NAVIGATING, etc.)

        with self._mission_lock:
            mtype = self._last_mission_type
        MISSIONS_DONE.labels(type=mtype, result=result).inc()

    def _on_estop(self, msg: Bool) -> None:
        ESTOP_ACTIVE.set(1.0 if msg.data else 0.0)

    def _on_arm_joints(self, msg: JointState) -> None:
        for i, name in enumerate(msg.name):
            if i < len(msg.position):
                ARM_JOINT_POS.labels(joint=name).set(msg.position[i])
            if i < len(msg.velocity):
                ARM_JOINT_VEL.labels(joint=name).set(msg.velocity[i])

    def _on_ai_command(self, msg: String) -> None:
        AI_COMMANDS_TOTAL.inc()


def main(args=None) -> None:
    rclpy.init(args=args)
    node = ROS2PrometheusBridge()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
