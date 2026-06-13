"""Episode logger node — records real-world execution into the replay format.

Runs on the Pi (or any ROS machine), buffers synchronized observations and
post-mux commands at ``record_hz``, and writes one ReplayDataset-format
episode per start/stop cycle into ``output_dir``. The learning loop ingests
that directory with ``ExecutionLogCollector`` — no rosbag round-trip needed.

Control:
    ros2 topic pub --once /learning/episode/start std_msgs/msg/String "data: 'pick up the red cup'"
    ros2 topic pub --once /learning/episode/stop  std_msgs/msg/String "data: 'success'"

Episodes are also auto-segmented by /mission/status transitions (a mission
starting begins an episode; done/idle ends it) so autonomous runs are
captured without manual topic pubs.

Run:
    python3 -m learning_engine.ros2.episode_logger_node --ros-args \
        -p output_dir:=~/datasets/execution_logs -p record_hz:=10.0
"""

from __future__ import annotations

import time
from typing import Any, Dict, Optional

import numpy as np

try:
    import rclpy
    from rclpy.node import Node
    from geometry_msgs.msg import Twist
    from nav_msgs.msg import Odometry
    from sensor_msgs.msg import Image, JointState
    from std_msgs.msg import Bool, String

    _HAS_ROS = True
except ImportError:  # not a ROS machine — module stays importable for docs/tests
    _HAS_ROS = False
    Node = object  # type: ignore[assignment, misc]

from ..core.types import DataSource, Episode, EpisodeMeta, Step, TaskOutcome
from ..data import schema
from ..data.replay_dataset import ReplayDataset
from . import topics


class EpisodeLoggerNode(Node):  # type: ignore[misc]
    def __init__(self) -> None:
        super().__init__("episode_logger")
        self.declare_parameter("output_dir", "~/datasets/execution_logs")
        self.declare_parameter("record_hz", 10.0)
        self.declare_parameter("store_images", True)
        self.declare_parameter("max_episode_s", 300.0)

        out = self.get_parameter("output_dir").value
        self.dataset = ReplayDataset(
            out, store_images=bool(self.get_parameter("store_images").value)
        )
        self.record_hz = float(self.get_parameter("record_hz").value)
        self.max_episode_s = float(self.get_parameter("max_episode_s").value)

        self._latest: Dict[str, Any] = {}
        self._episode: Optional[Episode] = None
        self._episode_started: float = 0.0

        # Observation sources
        self.create_subscription(Odometry, topics.ODOM, self._on_odom, 10)
        self.create_subscription(JointState, topics.ARM_JOINT_STATES, self._on_arm, 10)
        self.create_subscription(
            Image,
            topics.CAMERA_WRIST,
            lambda m: self._on_image(schema.OBS_IMAGE_WRIST, m),
            5,
        )
        self.create_subscription(
            Image,
            topics.CAMERA_FRONT,
            lambda m: self._on_image(schema.OBS_IMAGE_FRONT, m),
            5,
        )
        self.create_subscription(
            Image,
            topics.CAMERA_BEV,
            lambda m: self._on_image(schema.OBS_IMAGE_BEV, m),
            5,
        )
        # Executed actions (post-mux = what the hardware actually received)
        self.create_subscription(Twist, topics.CMD_VEL_OUT, self._on_cmd_vel, 10)
        self.create_subscription(
            JointState, topics.ARM_COMMANDS_OUT, self._on_arm_cmd, 10
        )
        # Context / control
        self.create_subscription(Bool, topics.EMERGENCY_STOP, self._on_estop, 10)
        self.create_subscription(String, topics.EPISODE_START, self._on_start, 10)
        self.create_subscription(String, topics.EPISODE_STOP, self._on_stop, 10)
        self.create_subscription(
            String, topics.MISSION_STATUS, self._on_mission_status, 10
        )

        self.status_pub = self.create_publisher(String, topics.EPISODE_STATUS, 10)
        self.create_timer(1.0 / self.record_hz, self._record_tick)
        self.create_timer(1.0, self._publish_status)
        self.get_logger().info(f"episode_logger writing to {out}")

    # ------------------------------------------------------------ callbacks
    def _on_odom(self, msg: Any) -> None:
        t = msg.twist.twist
        self._latest["base_vel"] = np.array(
            [t.linear.x, t.linear.y, t.angular.z], dtype=np.float32
        )

    def _on_arm(self, msg: Any) -> None:
        pos = dict(zip(msg.name, msg.position))
        self._latest["arm_pos"] = np.array(
            [pos.get(j, 0.0) for j in schema.ARM_JOINT_NAMES], dtype=np.float32
        )

    def _on_image(self, key: str, msg: Any) -> None:
        if msg.encoding not in ("bgr8", "rgb8"):
            return
        arr = np.frombuffer(msg.data, dtype=np.uint8).reshape(msg.height, msg.width, 3)
        self._latest[key] = arr.copy()

    def _on_cmd_vel(self, msg: Any) -> None:
        self._latest["base_action"] = np.array(
            [msg.linear.x, msg.linear.y, msg.angular.z], dtype=np.float32
        )

    def _on_arm_cmd(self, msg: Any) -> None:
        pos = dict(zip(msg.name, msg.position))
        self._latest["arm_action"] = np.array(
            [pos.get(j, 0.0) for j in schema.ARM_JOINT_NAMES], dtype=np.float32
        )

    def _on_estop(self, msg: Any) -> None:
        if msg.data and self._episode is not None:
            for step in self._episode.steps[-1:]:
                step.info["emergency_stop"] = True
            self._finish(TaskOutcome.ABORTED)

    def _on_start(self, msg: Any) -> None:
        self._begin(msg.data)

    def _on_stop(self, msg: Any) -> None:
        hint = (msg.data or "").strip().lower()
        outcome = (
            TaskOutcome(hint)
            if hint in TaskOutcome._value2member_map_
            else TaskOutcome.UNKNOWN
        )
        self._finish(outcome)

    def _on_mission_status(self, msg: Any) -> None:
        status = (msg.data or "").lower()
        if any(s in status for s in ("navigating", "vla", "rl_nav", "rl_arm")):
            if self._episode is None:
                self._begin(f"mission:{msg.data}")
        elif any(s in status for s in ("done", "idle", "cancel")):
            if self._episode is not None:
                self._finish(TaskOutcome.UNKNOWN)

    # ------------------------------------------------------------ recording
    def _begin(self, instruction: str) -> None:
        if self._episode is not None:
            self._finish(TaskOutcome.UNKNOWN)
        self._episode = Episode(
            meta=EpisodeMeta(
                task_instruction=instruction,
                source=DataSource.REAL_EXECUTION,
                environment="real",
                fps=self.record_hz,
            )
        )
        self._episode_started = time.time()
        self.get_logger().info(f"recording episode: '{instruction}'")

    def _finish(self, outcome: TaskOutcome) -> None:
        ep, self._episode = self._episode, None
        if ep is None or not ep.steps:
            return
        ep.meta.outcome = outcome
        eid = self.dataset.add_episode(ep)
        self.get_logger().info(
            f"saved episode {eid}: {len(ep.steps)} steps, outcome={outcome.value}"
        )

    def _record_tick(self) -> None:
        if self._episode is None:
            return
        if time.time() - self._episode_started > self.max_episode_s:
            self.get_logger().warn("max episode length reached; closing episode")
            self._finish(TaskOutcome.UNKNOWN)
            return
        arm = self._latest.get("arm_pos")
        base = self._latest.get("base_vel")
        if arm is None and base is None:
            return  # nothing observed yet
        state = np.concatenate(
            [
                arm if arm is not None else np.zeros(schema.ARM_DIM, dtype=np.float32),
                base
                if base is not None
                else np.zeros(schema.BASE_STATE_DIM, dtype=np.float32),
            ]
        )
        action = np.concatenate(
            [
                self._latest.get(
                    "arm_action", np.zeros(schema.ARM_DIM, dtype=np.float32)
                ),
                self._latest.get(
                    "base_action", np.zeros(schema.BASE_ACTION_DIM, dtype=np.float32)
                ),
            ]
        )
        obs: Dict[str, np.ndarray] = {schema.OBS_STATE: state}
        for key in schema.IMAGE_KEYS:
            if key in self._latest:
                obs[key] = self._latest[key]
        self._episode.steps.append(
            Step(observation=obs, action=action, timestamp=time.time())
        )

    def _publish_status(self) -> None:
        msg = String()
        msg.data = (
            f"recording:{self._episode.meta.episode_id}" if self._episode else "idle"
        )
        self.status_pub.publish(msg)


def main(args: Optional[list] = None) -> None:
    if not _HAS_ROS:
        raise SystemExit("episode_logger_node requires a sourced ROS 2 environment")
    rclpy.init(args=args)
    node = EpisodeLoggerNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
