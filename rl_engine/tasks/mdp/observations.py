"""
Custom Isaac Lab observation terms for OmniBot RL environments.

LidarSectorObsTerm
  Synthesizes N angular sectors of minimum distance from a depth pointcloud
  (or ray-cast sensor data) — matches the observation computed by rl_nav_node.py
  from /camera/depth/points on hardware.

GoalRelativeObsTerm
  Computes the 2D goal position in the robot's local frame, heading error,
  and distance to goal.

ArmObsTerm
  Builds the 30D arm observation vector matching rl_arm_node.py.
"""

import math

import torch
from isaaclab.envs import ManagerBasedRLEnv
from isaaclab.managers import ObservationTerm, ObservationTermCfg
from dataclasses import dataclass, field


# ── Navigation observation terms ─────────────────────────────────────────────

class LidarSectorObsTerm(ObservationTerm):
    """
    Synthesize 8 lidar sector distances from depth ray-cast data.

    In Isaac Lab, use a RayCasterCamera or lidar sensor attached to the robot.
    Projects ray hits to the XY plane and computes minimum range per sector.

    Observation: (num_envs, N_SECTORS)
    """

    def __init__(self, cfg: 'LidarSectorObsTermCfg', env: ManagerBasedRLEnv):
        super().__init__(cfg, env)
        self._n_sectors   = cfg.n_sectors
        self._lidar_min   = cfg.lidar_min_m
        self._lidar_max   = cfg.lidar_max_m
        self._sector_angle = 2.0 * math.pi / cfg.n_sectors
        self._noise_sigma  = cfg.noise_sigma_m

    def __call__(self, env: ManagerBasedRLEnv) -> torch.Tensor:
        """Return (num_envs, n_sectors) tensor of min distances per sector."""
        # Access ray-cast sensor data from environment's sensor manager
        # The sensor must be registered in the environment config as 'lidar_sensor'
        try:
            sensor = env.scene.sensors['lidar_sensor']
            # ray_hits_w: (num_envs, num_rays, 3) in world frame
            ray_hits_w  = sensor.data.ray_hits_w
            ray_origins = sensor.data.pos_w.unsqueeze(1)   # (num_envs, 1, 3)

            # Convert to local frame (XY plane)
            local_hits  = ray_hits_w - ray_origins          # (num_envs, num_rays, 3)
            dx = local_hits[..., 0]
            dy = local_hits[..., 1]
            dz = local_hits[..., 2]

            # Filter by height (ignore floor/ceiling)
            z_mask  = (dz > 0.05) & (dz < 1.5)
            r       = torch.sqrt(dx**2 + dy**2)
            range_mask = (r > self._lidar_min) & (r < self._lidar_max)
            valid   = z_mask & range_mask

            angles  = torch.atan2(dy, dx)                   # [-π, π]
            angles  = (angles + 2 * math.pi) % (2 * math.pi)  # [0, 2π]
            sector_idx = (angles / self._sector_angle).long() % self._n_sectors

            # Scatter min distances per sector
            num_envs = env.num_envs
            sectors  = torch.full(
                (num_envs, self._n_sectors), self._lidar_max,
                device=env.device)

            for s in range(self._n_sectors):
                mask = valid & (sector_idx == s)
                if mask.any():
                    # Min range in this sector, per environment
                    masked_r = r.clone()
                    masked_r[~mask] = self._lidar_max
                    sectors[:, s] = masked_r.min(dim=1).values

        except (KeyError, AttributeError):
            # Fallback: return max range if sensor not available
            sectors = torch.full(
                (env.num_envs, self._n_sectors), self._lidar_max,
                device=env.device)

        # Add observation noise (domain randomization)
        noise = torch.randn_like(sectors) * self._noise_sigma
        sectors = (sectors + noise).clamp(self._lidar_min, self._lidar_max)
        return sectors


@dataclass
class LidarSectorObsTermCfg(ObservationTermCfg):
    class_type: type = LidarSectorObsTerm
    n_sectors:      int   = 8
    lidar_min_m:    float = 0.30
    lidar_max_m:    float = 3.00
    noise_sigma_m:  float = 0.03


class GoalRelativeObsTerm(ObservationTerm):
    """
    Compute goal-relative observations:
      - Goal position in robot frame (2D)
      - Distance to goal (scalar)
      - Yaw error to goal (scalar)
      - Absolute goal heading (scalar)

    Total: 5D output.
    """

    def __call__(self, env: ManagerBasedRLEnv) -> torch.Tensor:
        # Robot position and yaw in world frame
        robot_pos_w = env.scene.articulations['robot'].data.root_pos_w  # (N, 3)
        robot_quat_w = env.scene.articulations['robot'].data.root_quat_w  # (N, 4) [w,x,y,z]

        # Yaw from quaternion (qw, qx, qy, qz)
        qw = robot_quat_w[:, 0]
        qz = robot_quat_w[:, 3]
        robot_yaw = 2.0 * torch.atan2(qz, qw)

        # Goal position from environment's command manager
        goal_pos_w = env.command_manager.get_command('goal_command')[:, :2]  # (N, 2)

        dx = goal_pos_w[:, 0] - robot_pos_w[:, 0]
        dy = goal_pos_w[:, 1] - robot_pos_w[:, 1]
        dist = torch.sqrt(dx**2 + dy**2)

        # Transform goal to robot frame
        cos_r = torch.cos(-robot_yaw)
        sin_r = torch.sin(-robot_yaw)
        goal_rel_x = cos_r * dx - sin_r * dy
        goal_rel_y = sin_r * dx + cos_r * dy

        goal_world_yaw = torch.atan2(dy, dx)
        yaw_error = _angle_wrap_tensor(goal_world_yaw - robot_yaw)

        # Goal heading (desired final orientation from command)
        goal_heading = env.command_manager.get_command('goal_command')[:, 2]

        return torch.stack([
            goal_rel_x, goal_rel_y, dist, yaw_error, goal_heading
        ], dim=1)


@dataclass
class GoalRelativeObsTermCfg(ObservationTermCfg):
    class_type: type = GoalRelativeObsTerm


# ── Arm observation terms ────────────────────────────────────────────────────

class ArmNormJointPosObsTerm(ObservationTerm):
    """
    Normalized arm joint positions to [-1, 1] using URDF limits.
    Returns (num_envs, 6).
    """

    _joint_min = torch.tensor([-3.14, -1.57, -1.69, -1.66, -2.74, -0.17])
    _joint_max = torch.tensor([ 3.14,  1.57,  1.69,  1.66,  2.84,  1.75])

    def __call__(self, env: ManagerBasedRLEnv) -> torch.Tensor:
        arm  = env.scene.articulations['robot']
        # joint_ids for arm joints [4:10] — set in env config
        joint_ids = self.cfg.arm_joint_ids
        pos = arm.data.joint_pos[:, joint_ids]
        jmin = self._joint_min.to(env.device)
        jmax = self._joint_max.to(env.device)
        return 2.0 * (pos - jmin) / (jmax - jmin + 1e-8) - 1.0


@dataclass
class ArmNormJointPosObsTermCfg(ObservationTermCfg):
    class_type: type = ArmNormJointPosObsTerm
    arm_joint_ids: list[int] = field(default_factory=lambda: [4, 5, 6, 7, 8, 9])


# ── Helpers ──────────────────────────────────────────────────────────────────

def _angle_wrap_tensor(angle: torch.Tensor) -> torch.Tensor:
    """Wrap angle tensor to [-π, π]."""
    return (angle + math.pi) % (2 * math.pi) - math.pi
