"""ROS 2 transport — bridges the Robot API to ROS 2 topics.

Uses the runtime's rclpy node to publish ``/cmd_vel`` (Twist), subscribe to
``/odom`` (Odometry) and ``/arm/joint_states`` (JointState) for telemetry, and
publish ``/arm/joint_commands`` (JointState) for arm commands. This is the
transport that lets ``Robot.connect("omnibot", "ros2://")`` drive the full
``robot_ws/`` stack (Nav2, SLAM, MoveIt 2) through DDS.

All ROS 2 message types are imported lazily inside ``connect()`` so the module
imports without a ROS 2 install. A ``msg_factory`` can be injected for tests.
"""

from __future__ import annotations

import math
import time
from typing import Any, Optional

from .. import capabilities as caps
from ..registry import RobotSpec
from ..schema import (
    ConnectionState,
    JointReading,
    Odometry,
    Telemetry,
    TransportStatus,
    Velocity,
)
from ..transport import BaseTransport
from .errors import AdapterUnavailable

TICKS_PER_RAD = 4096.0 / (2.0 * math.pi)


class Ros2Transport(BaseTransport):
    """Bridges the unified Transport interface to ROS 2 topics.

    Requires a :class:`~ohho.runtime.ros2.Ros2Runtime` (or any runtime whose
    ``node`` attribute is an rclpy node). The transport creates publishers and
    subscribers on that node, so there's exactly one DDS participant.
    """

    protocol = "ros2"

    def __init__(
        self,
        spec: RobotSpec,
        address: str = "",
        *,
        runtime=None,
        node: Any = None,
        msg_factory: Any = None,
    ) -> None:
        super().__init__()
        self.spec = spec
        self.address = address  # namespace prefix (e.g. "/robot1")
        self._runtime = runtime
        self._node: Any = node
        self._msg_factory = msg_factory
        self._pub_cmd_vel: Any = None
        self._pub_joint_cmd: Any = None
        self._latest_odom: Optional[Odometry] = None
        self._latest_joints: list[JointReading] = []
        self._latest_battery: Optional[float] = None
        self._estopped = False
        self._state = ConnectionState.IDLE
        self._connected_since: Optional[float] = None

    def _resolve_node(self) -> Any:
        if self._node is not None:
            return self._node
        if self._runtime is not None:
            node = getattr(self._runtime, "node", None)
            if node is not None:
                self._node = node
                return node
        raise AdapterUnavailable(
            "Ros2Transport needs a Ros2Runtime (pass runtime= or node=). "
            "Use Robot.connect(robot, 'ros2://', runtime='ros2')."
        )

    def _resolve_msg(self, module_path: str, msg_name: str) -> Any:
        """Import a ROS 2 message class lazily, or from the injected factory."""
        if self._msg_factory is not None:
            factory = getattr(self._msg_factory, msg_name, None)
            if callable(factory):
                return factory
        import importlib

        mod = importlib.import_module(module_path)
        return getattr(mod, msg_name)

    def _ns(self, topic: str) -> str:
        """Apply a namespace prefix if one was set."""
        if self._address and not topic.startswith("~"):
            base = topic.lstrip("/")
            return f"{self._address}/{base}"
        return topic

    @property
    def _address(self) -> str:
        return self.address.rstrip("/") if self.address else ""

    # ── lifecycle ─────────────────────────────────────────────────────────────
    def connect(self) -> TransportStatus:
        node = self._resolve_node()
        try:
            Twist = self._resolve_msg("geometry_msgs.msg", "Twist")
            Vector3 = self._resolve_msg("geometry_msgs.msg", "Vector3")
        except Exception as e:
            raise AdapterUnavailable(
                "Ros2Transport needs ROS 2 message types (geometry_msgs). "
                f"Source your ROS 2 setup. ({e})"
            ) from e

        self._Twist = Twist
        self._Vector3 = Vector3

        create_pub = getattr(node, "create_publisher", None)
        if callable(create_pub):
            self._pub_cmd_vel = create_pub(Twist, self._ns("/cmd_vel"), 10)

            if self.spec.has(caps.MANIPULATION):
                try:
                    JointState = self._resolve_msg("sensor_msgs.msg", "JointState")
                    self._JointState = JointState
                    self._pub_joint_cmd = create_pub(
                        JointState, self._ns("/arm/joint_commands"), 10
                    )
                except Exception:
                    pass

            self._setup_subscriptions(node)

        self._state = ConnectionState.CONNECTED
        self._connected_since = time.time()
        s = self.status()
        self._emit_status(s)
        return s

    def _setup_subscriptions(self, node: Any) -> None:
        create_sub = getattr(node, "create_subscription", None)
        if not callable(create_sub):
            return

        # /odom → Odometry
        try:
            OdometryMsg = self._resolve_msg("nav_msgs.msg", "Odometry")
            create_sub(OdometryMsg, self._ns("/odom"), self._on_odom, 10)
        except Exception:
            pass

        # /joint_states → JointState (arm)
        if self.spec.has(caps.MANIPULATION):
            try:
                JointStateMsg = self._resolve_msg("sensor_msgs.msg", "JointState")
                create_sub(
                    JointStateMsg, self._ns("/arm/joint_states"), self._on_joints, 10
                )
            except Exception:
                pass

    def _on_odom(self, msg: Any) -> None:
        """Bridge nav_msgs/Odometry → ohho Odometry."""
        pose = getattr(msg, "pose", None)
        twist = getattr(msg, "twist", None)
        position = getattr(getattr(pose, "pose", None), "position", None)
        orientation = getattr(getattr(pose, "pose", None), "orientation", None)
        linear = getattr(getattr(twist, "twist", None), "linear", None)
        angular = getattr(getattr(twist, "twist", None), "angular", None)

        x = getattr(position, "x", 0.0) if position else 0.0
        y = getattr(position, "y", 0.0) if position else 0.0
        # Extract yaw from quaternion (simple z-axis rotation)
        qz = getattr(orientation, "z", 0.0) if orientation else 0.0
        qw = getattr(orientation, "w", 1.0) if orientation else 1.0
        theta = 2.0 * math.atan2(qz, qw)

        vx = getattr(linear, "x", 0.0) if linear else 0.0
        vy = getattr(linear, "y", 0.0) if linear else 0.0
        omega = getattr(angular, "z", 0.0) if angular else 0.0

        self._latest_odom = Odometry(x, y, theta, vx, vy, omega)
        self._emit_telemetry(self._snapshot())

    def _on_joints(self, msg: Any) -> None:
        """Bridge sensor_msgs/JointState → ohho JointReading list."""
        names = list(getattr(msg, "name", []))
        positions = list(getattr(msg, "position", []))
        self._latest_joints = [
            JointReading(name=n, position=p) for n, p in zip(names, positions)
        ]
        self._emit_telemetry(self._snapshot())

    def disconnect(self) -> None:
        self._state = ConnectionState.DISCONNECTED
        self._emit_status(self.status())

    def status(self) -> TransportStatus:
        return TransportStatus(
            protocol=self.protocol,
            state=self._state,
            label=f"ROS 2 · {self._address or '/'}",
            connected_since=self._connected_since,
        )

    # ── commands ──────────────────────────────────────────────────────────────
    def send_velocity(self, vel: Velocity) -> None:
        if self._estopped or self._pub_cmd_vel is None:
            return
        try:
            twist = self._Twist()
            twist.linear = self._Vector3(x=vel.linear_x, y=vel.linear_y, z=0.0)
            twist.angular = self._Vector3(x=0.0, y=0.0, z=vel.angular_z)
            self._pub_cmd_vel.publish(twist)
        except Exception:
            pass

    def send_joint_command(self, name: str, position: float) -> None:
        if self._estopped or self._pub_joint_cmd is None:
            return
        try:
            js = self._JointState()
            js.name = [name]
            js.position = [position]
            self._pub_joint_cmd.publish(js)
        except Exception:
            pass

    def emergency_stop(self) -> None:
        self._estopped = True
        # Publish zero velocity directly (send_velocity is blocked by _estopped)
        if self._pub_cmd_vel is not None:
            try:
                twist = self._Twist()
                twist.linear = self._Vector3()
                twist.angular = self._Vector3()
                self._pub_cmd_vel.publish(twist)
            except Exception:
                pass

    def release_stop(self) -> None:
        self._estopped = False

    # ── telemetry ─────────────────────────────────────────────────────────────
    def read(self) -> Telemetry:
        return self._snapshot()

    def _snapshot(self) -> Telemetry:
        return Telemetry(
            odom=self._latest_odom or Odometry(),
            joints=list(self._latest_joints),
            battery=self._latest_battery,
        )
