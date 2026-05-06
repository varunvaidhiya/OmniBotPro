#!/usr/bin/env python3
"""
SmolVLA inference node for OmniBot mobile manipulation.

Runs a VLA (Vision-Language-Action) policy that simultaneously controls:
  - SO-101 6-DOF arm  → /arm/joint_commands (sensor_msgs/JointState)
  - Mecanum base      → /cmd_vel (geometry_msgs/Twist)

Unified 9D action space: [shoulder_pan, shoulder_lift, elbow_flex,
                           wrist_flex, wrist_roll, gripper,
                           base_vx, base_vy, base_vz]

Topics subscribed:
  /camera/wrist/image_raw        (sensor_msgs/Image)   — OV9732 on gripper
  /camera/base/bev/image_raw     (sensor_msgs/Image)   — BEV mosaic from 4 base cameras
  /camera/depth/image_raw        (sensor_msgs/Image)   — Orbbec Astra Pro (rear, optional)
  /arm/joint_states              (sensor_msgs/JointState)
  /odom                          (nav_msgs/Odometry)
  /smolvla/task                  (std_msgs/String)
  /smolvla/enable                (std_msgs/Bool)

Topics published:
  /arm/joint_commands  (sensor_msgs/JointState)
  /cmd_vel             (geometry_msgs/Twist)
"""

import collections
import statistics as _statistics
import time
import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image, JointState
from geometry_msgs.msg import Twist
from nav_msgs.msg import Odometry
from std_msgs.msg import String, Bool

try:
    from cv_bridge import CvBridge

    CV_BRIDGE_AVAILABLE = True
except ImportError:
    CV_BRIDGE_AVAILABLE = False

try:
    import cv2

    CV2_AVAILABLE = True
except ImportError:
    CV2_AVAILABLE = False

try:
    import torch

    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

# ---------------------------------------------------------------------------
# Optional LeRobot import
# ---------------------------------------------------------------------------
try:
    from lerobot.common.policies.smolvla.modeling_smolvla import SmolVLAPolicy

    LEROBOT_AVAILABLE = True
except ImportError:
    LEROBOT_AVAILABLE = False

try:
    from vla_engine.trt import patch_policy_vision_encoder

    TRT_PATCH_AVAILABLE = True
except ImportError:
    TRT_PATCH_AVAILABLE = False


JOINT_NAMES = [
    "shoulder_pan",
    "shoulder_lift",
    "elbow_flex",
    "wrist_flex",
    "wrist_roll",
    "gripper",
]


# ---------------------------------------------------------------------------
# Dummy policy for when lerobot is not installed
# ---------------------------------------------------------------------------
class DummyPolicy:
    """Returns zero actions — used when lerobot is not available."""

    def __init__(self, action_dim: int):
        self.action_dim = action_dim

    def select_action(self, obs: dict):
        if TORCH_AVAILABLE:
            return torch.zeros(1, self.action_dim)
        return np.zeros((1, self.action_dim), dtype=np.float32)

    def reset(self):
        pass


# ---------------------------------------------------------------------------
# Main node
# ---------------------------------------------------------------------------
class SmolVLANode(Node):
    """SmolVLA policy inference node."""

    def __init__(self):
        super().__init__("smolvla_node")

        # ------------------------------------------------------------------
        # Parameters
        # ------------------------------------------------------------------
        self.declare_parameter("checkpoint_path", "lerobot/smolvla_base")
        self.declare_parameter("device", "cuda")
        self.declare_parameter("policy_hz", 10.0)
        self.declare_parameter("chunk_size", 50)
        self.declare_parameter("state_dim", 9)
        self.declare_parameter("action_dim", 9)
        self.declare_parameter("image_width", 320)
        self.declare_parameter("image_height", 240)
        self.declare_parameter("task_description", "pick up the object and place it")
        self.declare_parameter("base_vel_scale", 0.3)
        # Set True to publish rolling inference timing to /diagnostics at 1 Hz.
        self.declare_parameter("publish_diagnostics", False)
        self.declare_parameter("use_trt", False)
        self.declare_parameter("trt_engine_path", "")

        self.checkpoint_path = self.get_parameter("checkpoint_path").value
        self.device_str = self.get_parameter("device").value
        self.policy_hz = self.get_parameter("policy_hz").value
        self.chunk_size = self.get_parameter("chunk_size").value
        self.state_dim = self.get_parameter("state_dim").value
        self.action_dim = self.get_parameter("action_dim").value
        self.image_width = self.get_parameter("image_width").value
        self.image_height = self.get_parameter("image_height").value
        self.task_description = self.get_parameter("task_description").value
        self.base_vel_scale = self.get_parameter("base_vel_scale").value
        self._diag_enabled = self.get_parameter("publish_diagnostics").value
        self.use_trt = self.get_parameter("use_trt").value
        self.trt_engine_path = self.get_parameter("trt_engine_path").value

        # Rolling timing accumulators (active only when _diag_enabled=True)
        self._t_preprocess = collections.deque(maxlen=100)
        self._t_inference = collections.deque(maxlen=100)
        self._t_total = collections.deque(maxlen=100)

        # ------------------------------------------------------------------
        # State
        # ------------------------------------------------------------------
        self.declare_parameter("use_depth", False)
        self.use_depth = self.get_parameter("use_depth").value

        self.enabled = False
        self.wrist_image = None  # numpy HxWx3 uint8
        self.bev_image = None  # numpy HxWx3 uint8 — BEV mosaic from 4 base cameras
        self.depth_image = None  # numpy HxWx3 uint8 — Orbbec Astra Pro (optional)
        self.arm_positions = np.zeros(6, dtype=np.float32)
        self.base_vel = np.zeros(3, dtype=np.float32)

        # ------------------------------------------------------------------
        # cv_bridge
        # ------------------------------------------------------------------
        if CV_BRIDGE_AVAILABLE:
            self.bridge = CvBridge()
        else:
            self.bridge = None
            self.get_logger().warn(
                "cv_bridge not available — image data will be zeroed."
            )

        # ------------------------------------------------------------------
        # Device selection
        # ------------------------------------------------------------------
        if TORCH_AVAILABLE:
            if self.device_str == "cuda" and torch.cuda.is_available():
                self.device = torch.device("cuda")
            else:
                self.device = torch.device("cpu")
                if self.device_str == "cuda":
                    self.get_logger().warn("CUDA not available, falling back to CPU.")
        else:
            self.device = None
            self.get_logger().warn("PyTorch not installed — using DummyPolicy.")

        # ------------------------------------------------------------------
        # Load policy
        # ------------------------------------------------------------------
        self.policy = self._load_policy()

        # ------------------------------------------------------------------
        # Publishers
        # ------------------------------------------------------------------
        self.joint_cmd_pub = self.create_publisher(
            JointState, "/arm/joint_commands", 10
        )
        self.cmd_vel_pub = self.create_publisher(Twist, "/cmd_vel", 10)

        # ------------------------------------------------------------------
        # Subscribers
        # ------------------------------------------------------------------
        self.create_subscription(
            Image, "/camera/wrist/image_raw", self.wrist_image_cb, 10
        )
        self.create_subscription(
            Image, "/camera/base/bev/image_raw", self.bev_image_cb, 10
        )
        # Orbbec Astra Pro — optional, enabled by use_depth parameter
        if self.use_depth:
            self.create_subscription(
                Image, "/camera/depth/image_raw", self.depth_image_cb, 10
            )
        self.create_subscription(JointState, "/arm/joint_states", self.arm_state_cb, 10)
        self.create_subscription(Odometry, "/odom", self.odom_cb, 10)
        self.create_subscription(String, "/smolvla/task", self.task_cb, 10)
        self.create_subscription(Bool, "/smolvla/enable", self.enable_cb, 10)

        # ------------------------------------------------------------------
        # Inference timer
        # ------------------------------------------------------------------
        timer_period = 1.0 / self.policy_hz
        self.create_timer(timer_period, self.inference_loop)

        if self._diag_enabled:
            from diagnostic_msgs.msg import DiagnosticArray, DiagnosticStatus, KeyValue
            self._DiagnosticArray = DiagnosticArray
            self._DiagnosticStatus = DiagnosticStatus
            self._KeyValue = KeyValue
            self._diag_pub = self.create_publisher(DiagnosticArray, "/diagnostics", 10)
            self.create_timer(1.0, self._publish_diagnostics)

        trt_status = f"TRT({self.trt_engine_path})" if self.use_trt else "PyTorch"
        self.get_logger().info(
            f"SmolVLANode started | policy={'SmolVLA' if LEROBOT_AVAILABLE else 'Dummy'} "
            f"| encoder={trt_status} | device={self.device} | hz={self.policy_hz} | enabled={self.enabled}"
        )

    # ------------------------------------------------------------------
    # Policy loading
    # ------------------------------------------------------------------

    def _load_policy(self):
        if not LEROBOT_AVAILABLE:
            self.get_logger().warn(
                "lerobot not installed — using DummyPolicy that returns zero actions."
            )
            return DummyPolicy(self.action_dim)

        try:
            self.get_logger().info(
                f'Loading SmolVLAPolicy from "{self.checkpoint_path}" ...'
            )
            policy = SmolVLAPolicy.from_pretrained(self.checkpoint_path)
            policy = policy.to(self.device)
            policy.eval()
            self.get_logger().info("SmolVLAPolicy loaded successfully.")

            if self.use_trt:
                self._apply_trt_patch(policy)

            return policy
        except Exception as exc:
            self.get_logger().error(
                f"Failed to load SmolVLAPolicy: {exc}. Using DummyPolicy."
            )
            return DummyPolicy(self.action_dim)

    def _apply_trt_patch(self, policy) -> None:
        """Swap the vision encoder for a TRT-backed module."""
        if not TRT_PATCH_AVAILABLE:
            self.get_logger().error(
                "use_trt=True but vla_engine.trt is not importable. "
                "Install tensorrt and ensure vla_engine is on PYTHONPATH. "
                "Falling back to PyTorch encoder."
            )
            return

        if not self.trt_engine_path:
            self.get_logger().error(
                "use_trt=True but trt_engine_path is empty. "
                "Build an engine with: python -m vla_engine.trt.build_engine "
                "--checkpoint <ckpt> --output <engine.trt> "
                "Then set the trt_engine_path parameter. "
                "Falling back to PyTorch encoder."
            )
            return

        try:
            patch_policy_vision_encoder(policy, self.trt_engine_path)
            self.get_logger().info(
                f"TRT vision encoder patch applied from: {self.trt_engine_path}"
            )
        except Exception as exc:
            self.get_logger().error(
                f"TRT patch failed: {exc}. Falling back to PyTorch encoder."
            )

    # ------------------------------------------------------------------
    # Image conversion helpers
    # ------------------------------------------------------------------

    def _ros_image_to_numpy(self, msg: Image):
        """Convert ROS Image message to numpy HxWx3 uint8 RGB array."""
        if self.bridge is not None:
            try:
                cv_img = self.bridge.imgmsg_to_cv2(msg, desired_encoding="rgb8")
                return np.array(cv_img, dtype=np.uint8)
            except Exception as exc:
                self.get_logger().warn(f"cv_bridge conversion error: {exc}")

        # Fallback: return zeros
        return np.zeros((self.image_height, self.image_width, 3), dtype=np.uint8)

    def _numpy_to_tensor(self, img_np: np.ndarray):
        """Convert numpy HxWx3 uint8 to float32 CHW tensor in [0, 1], batched."""
        if not CV2_AVAILABLE:
            resized = img_np
        else:
            resized = cv2.resize(
                img_np,
                (self.image_width, self.image_height),
                interpolation=cv2.INTER_LINEAR,
            )
        arr = resized.astype(np.float32) / 255.0
        arr = arr.transpose(2, 0, 1)  # HWC → CHW

        if TORCH_AVAILABLE:
            t = torch.from_numpy(arr).unsqueeze(0)  # 1xCxHxW
            if self.device is not None:
                t = t.to(self.device)
            return t
        return arr[np.newaxis]  # fallback numpy 1xCxHxW

    # ------------------------------------------------------------------
    # Subscribers
    # ------------------------------------------------------------------

    def wrist_image_cb(self, msg: Image):
        self.wrist_image = self._ros_image_to_numpy(msg)

    def bev_image_cb(self, msg: Image):
        self.bev_image = self._ros_image_to_numpy(msg)

    def depth_image_cb(self, msg: Image):
        self.depth_image = self._ros_image_to_numpy(msg)

    def arm_state_cb(self, msg: JointState):
        name_to_pos = dict(zip(msg.name, msg.position))
        self.arm_positions = np.array(
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

    def task_cb(self, msg: String):
        self.task_description = msg.data
        self.get_logger().info(f'Task updated: "{self.task_description}"')
        # Reset policy recurrent state if applicable
        if hasattr(self.policy, "reset"):
            self.policy.reset()

    def enable_cb(self, msg: Bool):
        self.enabled = msg.data
        status = "ENABLED" if msg.data else "DISABLED"
        self.get_logger().info(f"SmolVLA policy {status}.")
        if msg.data and hasattr(self.policy, "reset"):
            self.policy.reset()

    # ------------------------------------------------------------------
    # Inference loop
    # ------------------------------------------------------------------

    def inference_loop(self):
        """Run one policy inference step and publish commands."""
        if not self.enabled:
            return

        if self.wrist_image is None or self.bev_image is None:
            self.get_logger().warn(
                "Waiting for camera images: "
                f"wrist={'OK' if self.wrist_image is not None else 'MISSING'}, "
                f"bev={'OK' if self.bev_image is not None else 'MISSING'}",
                throttle_duration_sec=2.0,
            )
            return

        _t0 = time.perf_counter() if self._diag_enabled else None
        try:
            # Build observation tensors
            wrist_t = self._numpy_to_tensor(self.wrist_image)
            bev_t = self._numpy_to_tensor(self.bev_image)

            state = np.concatenate([self.arm_positions, self.base_vel])  # (9,)

            if TORCH_AVAILABLE and self.device is not None:
                state_t = torch.from_numpy(state).unsqueeze(0).to(self.device)  # (1, 9)
            else:
                state_t = state[np.newaxis]

            obs = {
                "observation.images.wrist": wrist_t,
                "observation.images.bev": bev_t,  # BEV mosaic from 4 base cameras
                "observation.state": state_t,
            }

            # Add task description if policy supports it
            if hasattr(self.policy, "set_task") or hasattr(self.policy, "task"):
                obs["task"] = self.task_description

            _t1 = time.perf_counter() if self._diag_enabled else None

            # Run inference
            with torch.no_grad() if TORCH_AVAILABLE else _null_context():
                action = self.policy.select_action(obs)

            _t2 = time.perf_counter() if self._diag_enabled else None

            if self._diag_enabled and _t0 is not None:
                ms = lambda a, b: (b - a) * 1000.0
                self._t_preprocess.append(ms(_t0, _t1))
                self._t_inference.append(ms(_t1, _t2))
                self._t_total.append(ms(_t0, _t2))

            # Convert to numpy
            if TORCH_AVAILABLE and hasattr(action, "cpu"):
                action_np = action.cpu().numpy()
            else:
                action_np = np.array(action)

            if action_np.ndim == 2:
                action_np = action_np[0]  # remove batch dim → (9,)

            self._publish_arm_command(action_np[:6])
            self._publish_base_command(action_np[6:9])

        except Exception as exc:
            self.get_logger().error(
                f"Inference error: {exc}", throttle_duration_sec=5.0
            )

    def _publish_diagnostics(self) -> None:
        msg = self._DiagnosticArray()
        msg.header.stamp = self.get_clock().now().to_msg()

        def _make(name, deque_):
            st = self._DiagnosticStatus()
            st.name = name
            if not deque_:
                st.level = self._DiagnosticStatus.OK
                st.message = "no data"
                return st
            s = sorted(deque_)
            n = len(s)
            p95 = s[max(0, int(0.95 * n) - 1)]
            hz_target = self.policy_hz
            budget_ms = 1000.0 / hz_target
            st.level = (
                self._DiagnosticStatus.ERROR if p95 > budget_ms
                else self._DiagnosticStatus.WARN if p95 > budget_ms * 0.8
                else self._DiagnosticStatus.OK
            )
            st.message = f"p95={p95:.1f}ms (budget={budget_ms:.0f}ms)"
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
            _make("smolvla/total_inference_ms", self._t_total),
            _make("smolvla/preprocess_ms", self._t_preprocess),
            _make("smolvla/policy_select_action_ms", self._t_inference),
        ]
        self._diag_pub.publish(msg)

    def _publish_arm_command(self, arm_action: np.ndarray):
        """Publish 6D arm action as JointState."""
        msg = JointState()
        msg.header.stamp = self.get_clock().now().to_msg()
        msg.name = JOINT_NAMES
        msg.position = arm_action.tolist()
        self.joint_cmd_pub.publish(msg)

    def _publish_base_command(self, base_action: np.ndarray):
        """Publish 3D base velocity as Twist, scaled and clamped."""
        vx = float(np.clip(base_action[0] * self.base_vel_scale, -0.3, 0.3))
        vy = float(np.clip(base_action[1] * self.base_vel_scale, -0.3, 0.3))
        vz = float(np.clip(base_action[2] * self.base_vel_scale, -1.0, 1.0))

        msg = Twist()
        msg.linear.x = vx
        msg.linear.y = vy
        msg.angular.z = vz
        self.cmd_vel_pub.publish(msg)


# ---------------------------------------------------------------------------
# Utility
# ---------------------------------------------------------------------------


class _null_context:
    """No-op context manager for when torch.no_grad() is unavailable."""

    def __enter__(self):
        return self

    def __exit__(self, *args):
        pass


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main(args=None):
    rclpy.init(args=args)
    node = SmolVLANode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
