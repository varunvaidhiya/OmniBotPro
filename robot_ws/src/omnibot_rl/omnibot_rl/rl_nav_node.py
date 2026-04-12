#!/usr/bin/env python3
"""
RL Navigation Node for OmniBot sim-to-real deployment.

Runs an Isaac Lab-trained PPO navigation policy (ONNX format) to provide
short-range reactive obstacle avoidance and precise goal approach (≤ 3 m).

Intended to complement Nav2, not replace it:
  - Nav2 global planner + AMCL handle room-to-room navigation.
  - This node handles the final approach / cluttered-space phase.
  - cmd_vel_mux routes /cmd_vel/rl to the driver when mode == "rl_nav".

Observation vector (27D — must match training env exactly):
  [0:2]   goal_relative_xy  — goal position in robot frame (m)
  [2:5]   base_velocity     — vx, vy, omega (m/s, rad/s)
  [5:13]  lidar_sectors     — min distance per 45° sector (m), 8 sectors
  [13:16] previous_action   — last delta action (Δvx, Δvy, Δω)
  [16]    distance_to_goal  — Euclidean distance (m)
  [17]    yaw_error_to_goal — heading error (rad)
  [18]    goal_heading      — absolute goal yaw (rad)
  [19:27] obstacle_min_dist — same 8-sector min distances (replicated)

Action vector (3D):
  [Δvx, Δvy, Δω] — velocity deltas accumulated per step.
  Mirrors the Yahboom board's 0.05 m/s ramp limiter.

Topics
──────
  Subscribed:
    /odom                    nav_msgs/Odometry
    /camera/depth/points     sensor_msgs/PointCloud2
    /rl_nav/goal             geometry_msgs/PoseStamped
    /control_mode/active     std_msgs/String

  Published:
    /cmd_vel/rl              geometry_msgs/Twist  (20 Hz)
"""

import math

import numpy as np
import rclpy
from geometry_msgs.msg import PoseStamped, Twist
from nav_msgs.msg import Odometry
from rclpy.node import Node
from sensor_msgs.msg import PointCloud2
from std_msgs.msg import String

try:
    import onnxruntime as ort
    ONNX_AVAILABLE = True
except ImportError:
    ONNX_AVAILABLE = False

try:
    import struct
    _STRUCT_AVAILABLE = True
except ImportError:
    _STRUCT_AVAILABLE = False


# ── Constants matching training env ─────────────────────────────────────────
OBS_DIM        = 27
ACTION_DIM     = 3
MAX_LIN_VEL    = 0.20   # m/s  — hardware limit (yahboom_controller_node.py)
MAX_ANG_VEL    = 1.00   # rad/s
MAX_DELTA      = 0.05   # m/s per step — mirrors board ramp limiter at 20 Hz
LIDAR_MIN      = 0.30   # m
LIDAR_MAX      = 3.00   # m
N_SECTORS      = 8
SECTOR_ANGLE   = 2.0 * math.pi / N_SECTORS   # 45° per sector
LIDAR_Z_MIN    = 0.05   # m above base_link — ignore floor
LIDAR_Z_MAX    = 1.50   # m above base_link — ignore ceiling
POLICY_HZ      = 20.0


class RLNavNode(Node):
    """
    Inference node for the Isaac Lab-trained navigation policy.

    When control_mode/active == "rl_nav" this node publishes velocity commands
    at POLICY_HZ to /cmd_vel/rl.  cmd_vel_mux forwards these to /cmd_vel/out.
    """

    def __init__(self):
        super().__init__('rl_nav_node')

        # ── Parameters ───────────────────────────────────────────────────────
        self.declare_parameter('policy_path',    '~/models/omnibot_nav_policy.onnx')
        self.declare_parameter('policy_hz',      POLICY_HZ)
        self.declare_parameter('goal_tolerance', 0.25)   # m — goal considered reached
        self.declare_parameter('max_lin_vel',    MAX_LIN_VEL)
        self.declare_parameter('max_ang_vel',    MAX_ANG_VEL)
        self.declare_parameter('lidar_min',      LIDAR_MIN)
        self.declare_parameter('lidar_max',      LIDAR_MAX)

        policy_path    = self.get_parameter('policy_path').value
        hz             = self.get_parameter('policy_hz').value
        self._goal_tol = self.get_parameter('goal_tolerance').value
        self._max_lin  = self.get_parameter('max_lin_vel').value
        self._max_ang  = self.get_parameter('max_ang_vel').value
        self._lidar_min = self.get_parameter('lidar_min').value
        self._lidar_max = self.get_parameter('lidar_max').value

        # ── State ────────────────────────────────────────────────────────────
        self._active_mode: str = 'nav2'
        self._goal: PoseStamped | None = None
        self._odom: Odometry | None = None
        self._lidar_sectors: np.ndarray = np.full(N_SECTORS, self._lidar_max)
        self._prev_action: np.ndarray = np.zeros(ACTION_DIM)
        self._cmd_vel_accum: np.ndarray = np.zeros(ACTION_DIM)   # accumulated velocity

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
                    self.get_logger().info(f'Loaded nav policy: {path}')
                except Exception as exc:
                    self.get_logger().error(f'Failed to load nav policy: {exc}')
            else:
                self.get_logger().warn(
                    f'Nav policy not found at {path}. '
                    'Node will publish zero velocity until a policy is loaded.')
        else:
            self.get_logger().warn(
                'onnxruntime not installed. Install with: pip install onnxruntime-gpu')

        # ── Subscriptions ────────────────────────────────────────────────────
        self.create_subscription(Odometry,      '/odom',                 self._odom_cb,        10)
        self.create_subscription(PointCloud2,   '/camera/depth/points',  self._cloud_cb,       10)
        self.create_subscription(PoseStamped,   '/rl_nav/goal',          self._goal_cb,        10)
        self.create_subscription(String,        '/control_mode/active',  self._mode_cb,        10)

        # ── Publisher ────────────────────────────────────────────────────────
        self._cmd_pub = self.create_publisher(Twist, '/cmd_vel/rl', 10)

        # ── Inference timer ──────────────────────────────────────────────────
        self.create_timer(1.0 / hz, self._inference_step)

        self.get_logger().info(
            f'RLNavNode ready. Policy Hz: {hz}. '
            f'Goal tolerance: {self._goal_tol} m. '
            f'Publish to /cmd_vel/rl (active only when mode="rl_nav").')

    # ── Callbacks ────────────────────────────────────────────────────────────

    def _mode_cb(self, msg: String) -> None:
        self._active_mode = msg.data.strip().lower()

    def _goal_cb(self, msg: PoseStamped) -> None:
        self._goal = msg
        self.get_logger().info(
            f'[RLNav] New goal: ({msg.pose.position.x:.2f}, '
            f'{msg.pose.position.y:.2f})')

    def _odom_cb(self, msg: Odometry) -> None:
        self._odom = msg

    def _cloud_cb(self, msg: PointCloud2) -> None:
        """Project depth pointcloud to 8 horizontal lidar sectors."""
        sectors = np.full(N_SECTORS, self._lidar_max)
        try:
            sectors = _pointcloud_to_sectors(
                msg, N_SECTORS, self._lidar_min, self._lidar_max,
                LIDAR_Z_MIN, LIDAR_Z_MAX)
        except Exception as exc:
            self.get_logger().warn(f'Lidar sector computation failed: {exc}',
                                   throttle_duration_sec=5.0)
        self._lidar_sectors = sectors

    # ── Inference ────────────────────────────────────────────────────────────

    def _inference_step(self) -> None:
        """Build observation, run policy, publish velocity command."""
        # Only publish when the mux is routing this node's output
        if self._active_mode != 'rl_nav':
            return

        if self._odom is None:
            return

        # Build observation
        obs = self._build_obs()
        if obs is None:
            return

        # Run policy
        if self._session is not None:
            try:
                obs_np = obs.astype(np.float32).reshape(1, OBS_DIM)
                raw = self._session.run(None, {'obs': obs_np})[0]
                delta_action = np.array(raw).flatten()[:ACTION_DIM]
            except Exception as exc:
                self.get_logger().error(f'Policy inference error: {exc}',
                                        throttle_duration_sec=5.0)
                delta_action = np.zeros(ACTION_DIM)
        else:
            delta_action = np.zeros(ACTION_DIM)

        # Clip delta and accumulate velocity (mirrors Yahboom ramp limiter)
        delta_action = np.clip(delta_action, -MAX_DELTA, MAX_DELTA)
        self._cmd_vel_accum += delta_action
        self._cmd_vel_accum[0:2] = np.clip(self._cmd_vel_accum[0:2],
                                            -self._max_lin, self._max_lin)
        self._cmd_vel_accum[2]   = np.clip(self._cmd_vel_accum[2],
                                            -self._max_ang, self._max_ang)
        self._prev_action = delta_action.copy()

        # Check goal reached — stop publishing
        if self._goal_reached():
            self.get_logger().info('[RLNav] Goal reached.')
            self._goal = None
            self._cmd_vel_accum[:] = 0.0
            self._publish_twist(0.0, 0.0, 0.0)
            return

        self._publish_twist(
            self._cmd_vel_accum[0],
            self._cmd_vel_accum[1],
            self._cmd_vel_accum[2])

    def _build_obs(self) -> np.ndarray | None:
        """Assemble the 27D observation vector matching the training env."""
        odom = self._odom
        if odom is None:
            return None

        # Robot pose in world frame
        rx = odom.pose.pose.position.x
        ry = odom.pose.pose.position.y
        qz = odom.pose.pose.orientation.z
        qw = odom.pose.pose.orientation.w
        robot_yaw = 2.0 * math.atan2(qz, qw)

        # Current velocity
        vx   = odom.twist.twist.linear.x
        vy   = odom.twist.twist.linear.y
        omega = odom.twist.twist.angular.z

        if self._goal is None:
            goal_rel = np.zeros(2)
            dist_to_goal = 0.0
            yaw_error    = 0.0
            goal_heading = 0.0
        else:
            gx = self._goal.pose.position.x
            gy = self._goal.pose.position.y
            dx = gx - rx
            dy = gy - ry
            dist_to_goal = math.sqrt(dx * dx + dy * dy)
            # Transform to robot frame
            cos_r = math.cos(-robot_yaw)
            sin_r = math.sin(-robot_yaw)
            goal_rel = np.array([
                cos_r * dx - sin_r * dy,
                sin_r * dx + cos_r * dy,
            ])
            goal_world_yaw = math.atan2(dy, dx)
            yaw_error = _angle_wrap(goal_world_yaw - robot_yaw)

            # Goal heading from quaternion
            gz = self._goal.pose.orientation.z
            gw = self._goal.pose.orientation.w
            goal_heading = 2.0 * math.atan2(gz, gw)

        obs = np.concatenate([
            goal_rel,                              # [0:2]
            [vx, vy, omega],                       # [2:5]
            self._lidar_sectors,                   # [5:13]
            self._prev_action,                     # [13:16]
            [dist_to_goal, yaw_error, goal_heading],  # [16:19]
            self._lidar_sectors,                   # [19:27] — replicated
        ])
        return obs.astype(np.float32)

    def _goal_reached(self) -> bool:
        if self._goal is None or self._odom is None:
            return False
        rx = self._odom.pose.pose.position.x
        ry = self._odom.pose.pose.position.y
        gx = self._goal.pose.position.x
        gy = self._goal.pose.position.y
        dist = math.sqrt((gx - rx) ** 2 + (gy - ry) ** 2)
        return dist < self._goal_tol

    def _publish_twist(self, vx: float, vy: float, omega: float) -> None:
        msg = Twist()
        msg.linear.x  = float(vx)
        msg.linear.y  = float(vy)
        msg.angular.z = float(omega)
        self._cmd_pub.publish(msg)


# ── Helpers ──────────────────────────────────────────────────────────────────

def _angle_wrap(angle: float) -> float:
    """Wrap angle to [-π, π]."""
    while angle >  math.pi: angle -= 2.0 * math.pi
    while angle < -math.pi: angle += 2.0 * math.pi
    return angle


def _pointcloud_to_sectors(
        msg: PointCloud2,
        n_sectors: int,
        lidar_min: float,
        lidar_max: float,
        z_min: float,
        z_max: float) -> np.ndarray:
    """
    Project a PointCloud2 message onto the XY plane and compute the minimum
    range per angular sector (measured from base_link origin).

    Assumes the cloud is already in base_link frame (as bridged by Isaac Sim /
    ros_gz_bridge for /camera/depth/points).
    """
    sectors = np.full(n_sectors, lidar_max)

    # Parse PointCloud2 fields: find byte offsets for x, y, z
    field_map = {f.name: (f.offset, f.datatype) for f in msg.fields}
    if 'x' not in field_map or 'y' not in field_map or 'z' not in field_map:
        return sectors

    x_off = field_map['x'][0]
    y_off = field_map['y'][0]
    z_off = field_map['z'][0]
    point_step = msg.point_step
    data = bytes(msg.data)

    sector_angle = 2.0 * math.pi / n_sectors

    for i in range(msg.width * msg.height):
        base = i * point_step
        x = struct.unpack_from('<f', data, base + x_off)[0]
        y = struct.unpack_from('<f', data, base + y_off)[0]
        z = struct.unpack_from('<f', data, base + z_off)[0]

        if not math.isfinite(x) or not math.isfinite(y) or not math.isfinite(z):
            continue
        if z < z_min or z > z_max:
            continue

        r = math.sqrt(x * x + y * y)
        if r < lidar_min or r > lidar_max:
            continue

        angle = math.atan2(y, x)
        if angle < 0.0:
            angle += 2.0 * math.pi

        sector_idx = int(angle / sector_angle) % n_sectors
        if r < sectors[sector_idx]:
            sectors[sector_idx] = r

    return sectors


def main(args=None):
    rclpy.init(args=args)
    node = RLNavNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
