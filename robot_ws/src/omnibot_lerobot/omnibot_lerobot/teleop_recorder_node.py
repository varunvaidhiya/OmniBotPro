#!/usr/bin/env python3
"""
Teleoperation + dataset recording node for OmniBot mobile manipulation.

Records synchronized arm + base demonstrations for SmolVLA fine-tuning.

Leader arm (SO-101) provides arm action commands.
Xbox controller drives the base and triggers recording.

State machine:
  IDLE      → waiting for RB button press
  RECORDING → accumulating frames
  SAVING    → writing episode to disk

Topics subscribed:
  /arm/leader_states           (sensor_msgs/JointState) - leader arm (arm actions)
  /arm/joint_states            (sensor_msgs/JointState) - follower arm (arm observations)
  /joy                         (sensor_msgs/Joy)         - Xbox controller
  /camera/wrist/image_raw      (sensor_msgs/Image)
  /camera/base/bev/image_raw   (sensor_msgs/Image)       - BEV mosaic from 4 base cameras
  /odom                        (nav_msgs/Odometry)       - base velocity

Unified 9D action:  [arm(6), base_vx, base_vy, base_vz]
Unified 9D state:   [arm(6), base_vx, base_vy, base_vz]
"""

import os
import time
import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import JointState, Joy, Image
from nav_msgs.msg import Odometry
from enum import Enum, auto

try:
    from cv_bridge import CvBridge

    CV_BRIDGE_AVAILABLE = True
except ImportError:
    CV_BRIDGE_AVAILABLE = False

try:
    import cv2  # noqa: F401

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

# ---------------------------------------------------------------------------
# Optional LeRobot import
# ---------------------------------------------------------------------------
try:
    from lerobot.common.datasets.lerobot_dataset import LeRobotDataset

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
JOINT_NAMES = [
    "shoulder_pan",
    "shoulder_lift",
    "elbow_flex",
    "wrist_flex",
    "wrist_roll",
    "gripper",
]
MAX_LINEAR = 0.2  # m/s safe recording speed
MAX_ANGULAR = 1.0  # rad/s


class RecordState(Enum):
    IDLE = auto()
    RECORDING = auto()
    SAVING = auto()


# ---------------------------------------------------------------------------
# Node
# ---------------------------------------------------------------------------
class TeleopRecorderNode(Node):
    """Records teleoperation episodes in LeRobot HF dataset format."""

    def __init__(self):
        super().__init__("teleop_recorder_node")

        # ------------------------------------------------------------------
        # Parameters
        # ------------------------------------------------------------------
        self.declare_parameter("output_dir", "~/datasets/mobile_manipulation")
        self.declare_parameter("repo_id", "local/mobile_manipulation")
        self.declare_parameter("record_hz", 30.0)
        self.declare_parameter("episode_timeout_s", 60.0)
        self.declare_parameter("joy_record_button", 7)  # Xbox RB
        self.declare_parameter("joy_discard_button", 6)  # Xbox LB

        self.output_dir = os.path.expanduser(self.get_parameter("output_dir").value)
        self.repo_id = self.get_parameter("repo_id").value
        self.record_hz = self.get_parameter("record_hz").value
        self.episode_timeout_s = self.get_parameter("episode_timeout_s").value
        self.joy_record_button = self.get_parameter("joy_record_button").value
        self.joy_discard_button = self.get_parameter("joy_discard_button").value

        os.makedirs(self.output_dir, exist_ok=True)

        # ------------------------------------------------------------------
        # Runtime state
        # ------------------------------------------------------------------
        self.record_state = RecordState.IDLE
        self.episode_buffer = []
        self.episode_index = 0
        self.frame_count = 0
        self.recording_start_time = None
        self.task_description = "mobile manipulation task"

        # Sensor cache
        self.leader_positions = np.zeros(len(JOINT_NAMES), dtype=np.float32)
        self.follower_positions = np.zeros(len(JOINT_NAMES), dtype=np.float32)
        self.base_vel = np.zeros(3, dtype=np.float32)
        self.joy_axes = [0.0] * 8
        self.joy_buttons = [0] * 16
        self.prev_joy_buttons = [0] * 16
        self.wrist_image = None  # numpy HxWx3 uint8 RGB
        self.bev_image = None  # numpy HxWx3 uint8 RGB — BEV mosaic from 4 base cameras

        # ------------------------------------------------------------------
        # cv_bridge
        # ------------------------------------------------------------------
        if CV_BRIDGE_AVAILABLE:
            self.bridge = CvBridge()
        else:
            self.bridge = None
            self.get_logger().warn("cv_bridge not available — images will be zeros.")

        # ------------------------------------------------------------------
        # Subscribers
        # ------------------------------------------------------------------
        self.create_subscription(
            JointState, "/arm/leader_states", self.leader_state_cb, 10
        )
        self.create_subscription(
            JointState, "/arm/joint_states", self.follower_state_cb, 10
        )
        self.create_subscription(Joy, "/joy", self.joy_cb, 10)
        self.create_subscription(
            Image, "/camera/wrist/image_raw", self.wrist_image_cb, 10
        )
        self.create_subscription(
            Image, "/camera/base/bev/image_raw", self.bev_image_cb, 10
        )
        self.create_subscription(Odometry, "/odom", self.odom_cb, 10)

        # ------------------------------------------------------------------
        # Record timer
        # ------------------------------------------------------------------
        timer_period = 1.0 / self.record_hz
        self.create_timer(timer_period, self.record_timer_cb)

        self.get_logger().info(
            f"TeleopRecorderNode started | hz={self.record_hz} | "
            f"output={self.output_dir} | lerobot={'yes' if LEROBOT_AVAILABLE else 'no (numpy fallback)'}"
        )
        self.get_logger().info(
            f"Press button {self.joy_record_button} (RB) to start/stop recording. "
            f"Button {self.joy_discard_button} (LB) discards the current episode."
        )

    # ------------------------------------------------------------------
    # Image helpers
    # ------------------------------------------------------------------

    def _ros_image_to_numpy_rgb(self, msg: Image):
        """Convert ROS Image to numpy HxWx3 uint8 RGB."""
        if self.bridge is not None:
            try:
                return np.array(
                    self.bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8"),
                    dtype=np.uint8,
                )
            except Exception as exc:
                self.get_logger().warn(
                    f"Image conversion error: {exc}", throttle_duration_sec=5.0
                )
        return np.zeros((240, 320, 3), dtype=np.uint8)

    # ------------------------------------------------------------------
    # Subscribers
    # ------------------------------------------------------------------

    def leader_state_cb(self, msg: JointState):
        name_to_pos = dict(zip(msg.name, msg.position))
        self.leader_positions = np.array(
            [name_to_pos.get(n, 0.0) for n in JOINT_NAMES], dtype=np.float32
        )

    def follower_state_cb(self, msg: JointState):
        name_to_pos = dict(zip(msg.name, msg.position))
        self.follower_positions = np.array(
            [name_to_pos.get(n, 0.0) for n in JOINT_NAMES], dtype=np.float32
        )

    def odom_cb(self, msg: Odometry):
        self.base_vel = np.array(
            [
                msg.twist.twist.linear.x,
                msg.twist.twist.linear.y,
                msg.twist.twist.angular.z,
            ],
            dtype=np.float32,
        )

    def joy_cb(self, msg: Joy):
        self.prev_joy_buttons = list(self.joy_buttons)
        self.joy_axes = list(msg.axes)
        self.joy_buttons = list(msg.buttons)
        self._handle_button_events()

    def wrist_image_cb(self, msg: Image):
        self.wrist_image = self._ros_image_to_numpy_rgb(msg)

    def bev_image_cb(self, msg: Image):
        self.bev_image = self._ros_image_to_numpy_rgb(msg)

    # ------------------------------------------------------------------
    # Button handling
    # ------------------------------------------------------------------

    def _button_just_pressed(self, idx: int) -> bool:
        """True if button transitioned 0→1 this tick."""
        if idx >= len(self.joy_buttons) or idx >= len(self.prev_joy_buttons):
            return False
        return self.joy_buttons[idx] == 1 and self.prev_joy_buttons[idx] == 0

    def _handle_button_events(self):
        if self._button_just_pressed(self.joy_record_button):
            self._on_record_button()

        if self._button_just_pressed(self.joy_discard_button):
            self._on_discard_button()

    def _on_record_button(self):
        if self.record_state == RecordState.IDLE:
            self.episode_buffer = []
            self.frame_count = 0
            self.recording_start_time = time.time()
            self.record_state = RecordState.RECORDING
            self.get_logger().info(
                f"Recording episode {self.episode_index} ... "
                f"Press RB again to save, LB to discard."
            )
        elif self.record_state == RecordState.RECORDING:
            self.record_state = RecordState.SAVING
            self.get_logger().info(
                f"Stopping recording. Saving {self.frame_count} frames..."
            )
            self._save_episode()

    def _on_discard_button(self):
        if self.record_state == RecordState.RECORDING:
            self.get_logger().warn(
                f"Discarding episode {self.episode_index} "
                f"({self.frame_count} frames recorded)."
            )
            self.episode_buffer = []
            self.frame_count = 0
            self.record_state = RecordState.IDLE

    # ------------------------------------------------------------------
    # Record timer
    # ------------------------------------------------------------------

    def record_timer_cb(self):
        # Check timeout
        if self.record_state == RecordState.RECORDING:
            elapsed = time.time() - self.recording_start_time
            if elapsed > self.episode_timeout_s:
                self.get_logger().warn(
                    f"Episode timeout ({self.episode_timeout_s}s). Auto-saving..."
                )
                self.record_state = RecordState.SAVING
                self._save_episode()
                return

            self._record_frame()

    def _record_frame(self):
        """Capture and append one frame to the episode buffer."""
        # Base action from joy axes (standard Xbox mapping)
        base_vx = (
            float(self.joy_axes[1]) * MAX_LINEAR if len(self.joy_axes) > 1 else 0.0
        )
        base_vy = (
            float(self.joy_axes[0]) * MAX_LINEAR if len(self.joy_axes) > 0 else 0.0
        )
        base_vz = (
            float(self.joy_axes[3]) * MAX_ANGULAR if len(self.joy_axes) > 3 else 0.0
        )

        # 9D action: [arm(6), base_vx, base_vy, base_vz]
        arm_action = self.leader_positions.copy()
        base_action = np.array([base_vx, base_vy, base_vz], dtype=np.float32)
        full_action = np.concatenate([arm_action, base_action])

        # 9D state: [follower_arm(6), base_vx, base_vy, base_vz]
        full_state = np.concatenate(
            [self.follower_positions.copy(), self.base_vel.copy()]
        )

        # Images (copy to avoid mutation)
        wrist_img = (
            self.wrist_image.copy()
            if self.wrist_image is not None
            else np.zeros((240, 320, 3), dtype=np.uint8)
        )
        bev_img = (
            self.bev_image.copy()
            if self.bev_image is not None
            else np.zeros((240, 320, 3), dtype=np.uint8)
        )

        frame = {
            "state": full_state.astype(np.float32),
            "action": full_action.astype(np.float32),
            "wrist_image": wrist_img,
            "bev_image": bev_img,  # BEV mosaic from 4 base cameras
            "timestamp": time.time(),
        }
        self.episode_buffer.append(frame)
        self.frame_count += 1

    # ------------------------------------------------------------------
    # Episode saving
    # ------------------------------------------------------------------

    def _save_episode(self):
        """Save the current episode buffer to disk."""
        if not self.episode_buffer:
            self.get_logger().warn("No frames to save.")
            self.record_state = RecordState.IDLE
            return

        try:
            if LEROBOT_AVAILABLE:
                self._save_lerobot_format()
            else:
                self._save_numpy_format()

            self.get_logger().info(
                f"Episode {self.episode_index} saved "
                f"({self.frame_count} frames @ {self.record_hz:.0f} Hz, "
                f"{self.frame_count / self.record_hz:.1f}s)."
            )
            self.episode_index += 1
        except Exception as exc:
            self.get_logger().error(f"Failed to save episode: {exc}")
        finally:
            self.episode_buffer = []
            self.frame_count = 0
            self.record_state = RecordState.IDLE

    def _save_lerobot_format(self):
        """Save episode using LeRobot HuggingFace dataset format."""
        features = {
            "observation.state": {
                "dtype": "float32",
                "shape": (9,),
                "names": {"joints": JOINT_NAMES + ["base_vx", "base_vy", "base_vz"]},
            },
            "action": {
                "dtype": "float32",
                "shape": (9,),
                "names": {"joints": JOINT_NAMES + ["base_vx", "base_vy", "base_vz"]},
            },
            "observation.images.wrist": {
                "dtype": "video",
                "shape": (3, 240, 320),
                "names": ["channels", "height", "width"],
            },
            "observation.images.bev": {
                "dtype": "video",
                "shape": (3, 240, 320),
                "names": ["channels", "height", "width"],
            },
        }

        dataset = LeRobotDataset.create(
            repo_id=self.repo_id,
            fps=int(self.record_hz),
            root=self.output_dir,
            features=features,
            image_writer_threads=4,
        )

        for frame in self.episode_buffer:
            dataset.add_frame(
                {
                    "observation.state": frame["state"],
                    "action": frame["action"],
                    "observation.images.wrist": frame["wrist_image"],
                    "observation.images.bev": frame["bev_image"],
                }
            )

        dataset.save_episode(task=self.task_description)

    def _save_numpy_format(self):
        """Fallback: save episode as .npz file."""
        states = np.stack([f["state"] for f in self.episode_buffer])
        actions = np.stack([f["action"] for f in self.episode_buffer])
        wrist_imgs = np.stack([f["wrist_image"] for f in self.episode_buffer])
        bev_imgs = np.stack([f["bev_image"] for f in self.episode_buffer])
        timestamps = np.array([f["timestamp"] for f in self.episode_buffer])

        ep_dir = os.path.join(self.output_dir, f"episode_{self.episode_index:05d}")
        os.makedirs(ep_dir, exist_ok=True)

        np.savez_compressed(
            os.path.join(ep_dir, "data.npz"),
            states=states,
            actions=actions,
            wrist_images=wrist_imgs,
            bev_images=bev_imgs,
            timestamps=timestamps,
            joint_names=np.array(JOINT_NAMES),
            task=np.array([self.task_description]),
            fps=np.array([self.record_hz]),
        )
        self.get_logger().info(
            f"Saved numpy episode to {ep_dir}/data.npz (install lerobot for HF format)"
        )


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main(args=None):
    rclpy.init(args=args)
    node = TeleopRecorderNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
