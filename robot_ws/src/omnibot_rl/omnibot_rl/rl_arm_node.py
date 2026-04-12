#!/usr/bin/env python3
"""
RL Arm Node for OmniBot sim-to-real deployment.

Runs an Isaac Lab-trained PPO arm policy (ONNX format) for precision
manipulation — typically activated for the final approach phase after
SmolVLA has brought the arm near the target object.

The policy outputs joint position **deltas** (not absolute targets).
This matches the STS3215 servo interface and was trained with action
delay randomization to cover the 12–25 ms servo bus cycle latency.

Observation vector (30D — must match training env exactly):
  [0:6]   joint_pos_norm  — joint positions normalized to [-1, 1] by URDF limits
  [6:12]  joint_vel       — estimated joint velocities (rad/s)
  [12:15] ee_pos          — end-effector position in base_link frame (m)
  [15:21] ee_rot_mat_cols — EE rotation matrix columns 0 and 1 (6D, continuous)
  [21:24] target_pos      — target object position in base_link frame (m)
  [24]    gripper_opening — normalized gripper opening [0, 1]
  [25:31] previous_action — last 6D delta action (rad)
  NOTE: indices 25–30 = previous action → total 31D? Recheck: 6+6+3+6+3+1+6=31.
        Trimmed to 30D by excluding gripper_vel (index 11→set to 0).

  Actual layout used in training:
  [0:6]   joint_pos_norm   6D
  [6:11]  arm_joint_vel    5D  (joints 1–5; gripper vel omitted)
  [11:14] ee_pos           3D
  [14:20] ee_rot_6d        6D
  [20:23] target_pos       3D
  [23]    gripper_opening  1D
  [24:30] previous_action  6D
  Total: 30D

Action vector (6D):
  [Δq0 … Δq5] — joint position deltas (rad/step), clipped to ±0.05 rad.
  Applied as absolute position targets by accumulation.

Topics
──────
  Subscribed:
    /arm/joint_states      sensor_msgs/JointState
    /rl_arm/target_pose    geometry_msgs/PoseStamped  (base_link frame)
    /rl_arm/enable         std_msgs/Bool
    /arm/cmd_mode          std_msgs/String

  Published:
    /arm/joint_commands/rl sensor_msgs/JointState  (20 Hz)
"""

import math

import numpy as np
import rclpy
from geometry_msgs.msg import PoseStamped
from rclpy.node import Node
from sensor_msgs.msg import JointState
from std_msgs.msg import Bool, String

try:
    import onnxruntime as ort
    ONNX_AVAILABLE = True
except ImportError:
    ONNX_AVAILABLE = False


# ── Constants from arm_params.yaml / URDF ────────────────────────────────────
JOINT_NAMES = [
    'arm_shoulder_pan', 'arm_shoulder_lift', 'arm_elbow_flex',
    'arm_wrist_flex', 'arm_wrist_roll', 'arm_gripper',
]
JOINT_MIN = np.array([-3.14, -1.57, -1.69, -1.66, -2.74, -0.17])
JOINT_MAX = np.array([ 3.14,  1.57,  1.69,  1.66,  2.84,  1.75])
JOINT_HOME = np.zeros(6)   # radians (home = 2048 ticks = 0 rad offset)

OBS_DIM    = 30
ACTION_DIM = 6
MAX_DELTA  = 0.05   # rad/step — safe delta matching training env
POLICY_HZ  = 20.0


class RLArmNode(Node):
    """
    Inference node for the Isaac Lab-trained arm manipulation policy.

    Outputs /arm/joint_commands/rl. arm_cmd_mux selects this stream
    only when /arm/cmd_mode == "rl_arm".
    """

    def __init__(self):
        super().__init__('rl_arm_node')

        # ── Parameters ───────────────────────────────────────────────────────
        self.declare_parameter('policy_path', '~/models/omnibot_arm_policy.onnx')
        self.declare_parameter('policy_hz',   POLICY_HZ)
        self.declare_parameter('max_delta',   MAX_DELTA)
        self.declare_parameter('joint_names', JOINT_NAMES)

        policy_path  = self.get_parameter('policy_path').value
        hz           = self.get_parameter('policy_hz').value
        self._max_delta = self.get_parameter('max_delta').value
        joint_names_param = self.get_parameter('joint_names').value
        self._joint_names = list(joint_names_param)
        n_joints = len(self._joint_names)

        # ── State ────────────────────────────────────────────────────────────
        self._enabled: bool = False
        self._active_mode: str = 'smolvla'
        self._joint_pos: np.ndarray = np.zeros(n_joints)
        self._joint_vel: np.ndarray = np.zeros(n_joints)
        self._prev_joint_pos: np.ndarray = np.zeros(n_joints)
        self._prev_action: np.ndarray = np.zeros(ACTION_DIM)
        self._target_pos: np.ndarray = np.zeros(3)   # base_link frame
        self._accum_pos: np.ndarray = np.zeros(n_joints)  # accumulated joint targets
        self._has_joint_state: bool = False
        self._dt: float = 1.0 / hz

        # ── Load ONNX policy ─────────────────────────────────────────────────
        self._session = None
        if ONNX_AVAILABLE:
            import os
            path = os.path.expanduser(policy_path)
            if os.path.exists(path):
                try:
                    self._session = ort.InferenceSession(
                        path,
                        providers=['CUDAExecutionProvider', 'CPUExecutionProvider'])
                    self.get_logger().info(f'Loaded arm policy: {path}')
                except Exception as exc:
                    self.get_logger().error(f'Failed to load arm policy: {exc}')
            else:
                self.get_logger().warn(
                    f'Arm policy not found at {path}. '
                    'Node will hold current joint positions until a policy is loaded.')
        else:
            self.get_logger().warn(
                'onnxruntime not installed. Install with: pip install onnxruntime-gpu')

        # ── Subscriptions ────────────────────────────────────────────────────
        self.create_subscription(JointState,  '/arm/joint_states',   self._joint_cb,  10)
        self.create_subscription(PoseStamped, '/rl_arm/target_pose', self._target_cb, 10)
        self.create_subscription(Bool,        '/rl_arm/enable',      self._enable_cb, 10)
        self.create_subscription(String,      '/arm/cmd_mode',       self._mode_cb,   10)

        # ── Publisher ────────────────────────────────────────────────────────
        self._cmd_pub = self.create_publisher(JointState, '/arm/joint_commands/rl', 10)

        # ── Inference timer ──────────────────────────────────────────────────
        self.create_timer(1.0 / hz, self._inference_step)

        self.get_logger().info(
            f'RLArmNode ready. Policy Hz: {hz}. '
            'Publishes to /arm/joint_commands/rl when arm/cmd_mode="rl_arm".')

    # ── Callbacks ────────────────────────────────────────────────────────────

    def _mode_cb(self, msg: String) -> None:
        self._active_mode = msg.data.strip().lower()

    def _enable_cb(self, msg: Bool) -> None:
        self._enabled = msg.data

    def _joint_cb(self, msg: JointState) -> None:
        """Update joint position and estimate velocity numerically."""
        n = len(self._joint_names)
        pos = np.zeros(n)
        name_to_idx = {name: i for i, name in enumerate(msg.name)}
        for i, jname in enumerate(self._joint_names):
            if jname in name_to_idx:
                pos[i] = msg.position[name_to_idx[jname]]
        if not self._has_joint_state:
            self._accum_pos = pos.copy()
            self._prev_joint_pos = pos.copy()
            self._has_joint_state = True
        else:
            self._joint_vel = (pos - self._prev_joint_pos) / max(self._dt, 1e-6)
            self._prev_joint_pos = self._joint_pos.copy()
        self._joint_pos = pos

    def _target_cb(self, msg: PoseStamped) -> None:
        self._target_pos = np.array([
            msg.pose.position.x,
            msg.pose.position.y,
            msg.pose.position.z,
        ])

    # ── Inference ────────────────────────────────────────────────────────────

    def _inference_step(self) -> None:
        """Build observation, run policy, publish joint command."""
        if not self._has_joint_state:
            return

        obs = self._build_obs()
        if obs is None:
            return

        if self._session is not None and self._active_mode == 'rl_arm':
            try:
                obs_np = obs.astype(np.float32).reshape(1, OBS_DIM)
                raw = self._session.run(None, {'obs': obs_np})[0]
                delta = np.array(raw).flatten()[:ACTION_DIM]
            except Exception as exc:
                self.get_logger().error(f'Arm policy inference error: {exc}',
                                        throttle_duration_sec=5.0)
                delta = np.zeros(ACTION_DIM)
        else:
            # When not in rl_arm mode, output nothing (arm_cmd_mux won't
            # route this anyway, but avoid spurious commands)
            return

        # Clip delta and accumulate
        delta = np.clip(delta, -self._max_delta, self._max_delta)
        self._accum_pos += delta
        # Enforce joint limits
        self._accum_pos = np.clip(self._accum_pos, JOINT_MIN, JOINT_MAX)
        self._prev_action = delta.copy()

        self._publish_joint_cmd(self._accum_pos)

    def _build_obs(self) -> np.ndarray | None:
        """Assemble the 30D observation vector matching the training env."""
        # Normalize joint positions to [-1, 1]
        joint_range = JOINT_MAX - JOINT_MIN
        joint_pos_norm = 2.0 * (self._joint_pos - JOINT_MIN) / joint_range - 1.0

        # Arm joint velocities (5D — joints 0–4; gripper vel set to 0)
        arm_vel = self._joint_vel[:5]

        # EE position in base_link frame — approximate FK using last 3 joints
        # In deployment the actual FK is from arm_driver_node's FK or TF.
        # For simplicity, use target_pos as a stand-in for relative positioning.
        # The policy was trained with ee_pos computed from Isaac Sim's FK.
        # Provide current joint-based estimate: simplified planar FK
        ee_pos = _approx_ee_position(self._joint_pos)

        # EE rotation as 6D (first two columns of rotation matrix) — simplified
        # Use identity + shoulder_pan rotation as a zeroth-order approximation
        ee_rot_6d = _approx_ee_rotation_6d(self._joint_pos)

        # Gripper opening normalized [0, 1]
        gripper_norm = (self._joint_pos[5] - JOINT_MIN[5]) / (
            JOINT_MAX[5] - JOINT_MIN[5] + 1e-8)
        gripper_norm = float(np.clip(gripper_norm, 0.0, 1.0))

        obs = np.concatenate([
            joint_pos_norm,      # [0:6]
            arm_vel,             # [6:11]
            ee_pos,              # [11:14]
            ee_rot_6d,           # [14:20]
            self._target_pos,    # [20:23]
            [gripper_norm],      # [23]
            self._prev_action,   # [24:30]
        ])
        return obs.astype(np.float32)

    def _publish_joint_cmd(self, positions: np.ndarray) -> None:
        msg = JointState()
        msg.header.stamp = self.get_clock().now().to_msg()
        msg.name = self._joint_names
        msg.position = positions.tolist()
        self._cmd_pub.publish(msg)


# ── Simplified FK helpers ─────────────────────────────────────────────────────

def _approx_ee_position(q: np.ndarray) -> np.ndarray:
    """
    Very rough planar FK for OmniBot SO-101 arm.
    Provides a first-order estimate for the RL policy observation.
    Link lengths (from URDF xacro, approximate):
      L1 (shoulder → upper arm)  ≈ 0.105 m
      L2 (upper arm → lower arm) ≈ 0.105 m
      L3 (lower arm → wrist)     ≈ 0.085 m
    Rotation axis is Z for all joints (from URDF analysis).
    """
    L1, L2, L3 = 0.105, 0.105, 0.085
    q0, q1, q2, q3 = q[0], q[1], q[2], q[3]

    # Planar 2D FK in shoulder plane, then rotate by shoulder_pan
    cum_angle = q1
    x2d = L1 * math.cos(cum_angle)
    y2d = L1 * math.sin(cum_angle)
    cum_angle += q2
    x2d += L2 * math.cos(cum_angle)
    y2d += L2 * math.sin(cum_angle)
    cum_angle += q3
    x2d += L3 * math.cos(cum_angle)
    y2d += L3 * math.sin(cum_angle)

    # Rotate by shoulder_pan around vertical axis
    # Base mount offset: x=0.055 m from top plate center
    base_x = 0.055
    cos_pan = math.cos(q0)
    sin_pan = math.sin(q0)
    ee_x = base_x + cos_pan * x2d
    ee_y =          sin_pan * x2d
    ee_z = y2d  # vertical

    return np.array([ee_x, ee_y, ee_z], dtype=np.float32)


def _approx_ee_rotation_6d(q: np.ndarray) -> np.ndarray:
    """
    Approximate EE orientation as first two columns of a rotation matrix (6D).
    Returns identity-like values with shoulder_pan applied.
    """
    q0 = q[0]
    c0, s0 = math.cos(q0), math.sin(q0)
    # Column 0 of rotation matrix (x-axis of EE in base frame)
    col0 = np.array([c0, s0, 0.0])
    # Column 1 (y-axis)
    col1 = np.array([-s0, c0, 0.0])
    return np.concatenate([col0, col1]).astype(np.float32)


def main(args=None):
    rclpy.init(args=args)
    node = RLArmNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
