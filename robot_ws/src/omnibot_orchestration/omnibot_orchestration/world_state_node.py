"""world_state_node — fuses scattered robot state into one snapshot.

Subscribes the topics that today carry OmniBot's state piecemeal (`/odom`,
`/arm/joint_states`, `/perception/object_info`, `/mission/status`,
`/emergency_stop`) and publishes a single fused `WorldState` as JSON on
`/agent/world_state` at a fixed rate. This is what the agent harness's
``Perceptor`` consumes each tick — filling the "no unified state endpoint" gap.

The fused snapshot type lives in the pure-Python ``agent_engine`` package
(install with ``pip install -e agent_engine`` on the robot / in the image), so
the schema is shared with the harness and its unit tests.
"""

import json
import math

import rclpy
from geometry_msgs.msg import Twist  # noqa: F401  (documents the velocity convention)
from nav_msgs.msg import Odometry
from rclpy.node import Node
from sensor_msgs.msg import JointState
from std_msgs.msg import Bool, String

from agent_engine.core.blackboard import DetectedObject, WorldState

ARM_JOINT_NAMES = [
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
]


def _yaw_from_quaternion(x: float, y: float, z: float, w: float) -> float:
    return math.atan2(2.0 * (w * z + x * y), 1.0 - 2.0 * (y * y + z * z))


class WorldStateNode(Node):
    def __init__(self) -> None:
        super().__init__("world_state_node")
        self.declare_parameter("publish_hz", 5.0)
        self.declare_parameter("entity_memory_path", "~/.omnibot/entity_memory.json")
        self.declare_parameter("scene_description", "")

        self._pose = (0.0, 0.0, 0.0)
        self._vel = (0.0, 0.0, 0.0)
        self._arm = []
        self._objects = []
        self._nearest = float("nan")
        self._mission_phase = "idle"
        self._estop = False

        self._entity = self._load_entity_memory()

        self.create_subscription(Odometry, "/odom", self._on_odom, 10)
        self.create_subscription(JointState, "/arm/joint_states", self._on_arm, 10)
        self.create_subscription(
            String, "/perception/object_info", self._on_objects, 10
        )
        self.create_subscription(String, "/mission/status", self._on_mission_status, 10)
        self.create_subscription(Bool, "/emergency_stop", self._on_estop, 10)

        self._pub = self.create_publisher(String, "/agent/world_state", 10)
        hz = float(self.get_parameter("publish_hz").value)
        self.create_timer(1.0 / max(hz, 0.1), self._publish)
        self.get_logger().info(
            f"world_state_node publishing /agent/world_state at {hz} Hz"
        )

    # -- subscriptions -----------------------------------------------------
    def _on_odom(self, msg: Odometry) -> None:
        p = msg.pose.pose.position
        q = msg.pose.pose.orientation
        t = msg.twist.twist
        self._pose = (p.x, p.y, _yaw_from_quaternion(q.x, q.y, q.z, q.w))
        self._vel = (t.linear.x, t.linear.y, t.angular.z)

    def _on_arm(self, msg: JointState) -> None:
        by_name = dict(zip(msg.name, msg.position))
        self._arm = [float(by_name.get(n, 0.0)) for n in ARM_JOINT_NAMES]

    def _on_objects(self, msg: String) -> None:
        try:
            data = json.loads(msg.data)
        except (ValueError, TypeError):
            return
        objs, nearest = [], float("nan")
        for d in data if isinstance(data, list) else []:
            try:
                obj = DetectedObject.from_dict(d)
            except (KeyError, TypeError, ValueError):
                continue
            objs.append(obj)
            if not math.isnan(obj.distance_m) and (
                math.isnan(nearest) or obj.distance_m < nearest
            ):
                nearest = obj.distance_m
        self._objects, self._nearest = objs, nearest

    def _on_mission_status(self, msg: String) -> None:
        # Format: "phase={phase} mission={...}"
        text = msg.data
        if "phase=" in text:
            self._mission_phase = text.split("phase=", 1)[1].split(" ", 1)[0]

    def _on_estop(self, msg: Bool) -> None:
        self._estop = bool(msg.data)

    # -- publish -----------------------------------------------------------
    def _publish(self) -> None:
        ws = WorldState(
            base_pose=self._pose,
            base_velocity=self._vel,
            arm_joint_positions=list(self._arm),
            detected_objects=list(self._objects),
            nearest_distance_m=self._nearest,
            mission_phase=self._mission_phase,
            scene_description=str(self.get_parameter("scene_description").value),
            memory_summary=self._entity.get_summary() if self._entity else "",
            emergency_stop=self._estop,
        )
        self._pub.publish(String(data=json.dumps(ws.to_dict())))

    def _load_entity_memory(self):
        try:
            from omnibot_orchestration.memory.entity_memory import EntityMemory

            path = str(self.get_parameter("entity_memory_path").value)
            return EntityMemory(path)
        except Exception as exc:  # noqa: BLE001 — memory is optional context
            self.get_logger().warning(f"entity memory unavailable: {exc}")
            return None


def main(args=None) -> None:
    rclpy.init(args=args)
    node = WorldStateNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
