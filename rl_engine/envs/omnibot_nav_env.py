"""
OmniBot Navigation RL Environment for Isaac Lab.

Trains a local obstacle avoidance + short-range goal approach policy
for the mecanum-wheel base. No images — compact 27D observation space
for fast sim-to-real transfer.

Observation space (27D):
  [0:2]   goal_relative_xy  — goal in robot frame (m)
  [2:5]   base_velocity     — vx, vy, omega (m/s, rad/s)
  [5:13]  lidar_sectors     — 8× min distance at 45° intervals (m)
  [13:16] previous_action   — last Δvx, Δvy, Δω
  [16]    distance_to_goal
  [17]    yaw_error_to_goal
  [18]    goal_heading
  [19:27] lidar_sectors     — repeated (doubles weight in input)

Action space (3D):
  [Δvx, Δvy, Δω] — accumulated velocity deltas, mirroring Yahboom ramp limiter

See rl_engine/config/nav_train.yaml for training hyperparameters.
See rl_engine/tasks/mdp/ for reward/termination/action/observation terms.

Usage:
  python rl_engine/scripts/train_nav.py --num_envs 512
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import ClassVar

import isaaclab.sim as sim_utils
from isaaclab.assets import ArticulationCfg, AssetBaseCfg
from isaaclab.envs import ManagerBasedRLEnvCfg
from isaaclab.managers import (
    ActionTermCfg,
    CurriculumTermCfg,
    EventTermCfg,
    ObservationGroupCfg,
    ObservationTermCfg,
    RewardTermCfg,
    SceneEntityCfg,
    TerminationTermCfg,
)
from isaaclab.scene import InteractiveSceneCfg
from isaaclab.sensors import RayCasterCfg, patterns
from isaaclab.terrains import TerrainImporterCfg
from isaaclab.utils import configclass

from rl_engine.tasks.mdp import (
    MecanumWheelActionTermCfg,
    LidarSectorObsTermCfg,
    GoalRelativeObsTermCfg,
    nav_goal_approach,
    nav_heading_alignment,
    nav_action_smoothness,
    nav_collision_penalty,
    nav_goal_reached_bonus,
    episode_timeout,
    nav_goal_reached,
    nav_collision,
)

# ── Robot USD path ─────────────────────────────────────────────────────────────
# Must be imported into Isaac Sim from the URDF first:
#   ./IsaacLab/scripts/tools/convert_urdf.py \
#     robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro \
#     robot_ws/src/omnibot_description/usd/omnibot.usd
ROBOT_USD_PATH = "{ROBOT_WS}/src/omnibot_description/usd/omnibot.usd"


@configclass
class OmnibotNavSceneCfg(InteractiveSceneCfg):
    """Scene configuration: flat ground + OmniBot robot + ray-cast lidar."""

    # Ground plane
    terrain = TerrainImporterCfg(
        prim_path='/World/ground',
        terrain_type='plane',
        collision_group=-1,
        physics_material=sim_utils.RigidBodyMaterialCfg(
            friction_combine_mode='multiply',
            restitution_combine_mode='multiply',
            static_friction=1.0,
            dynamic_friction=1.0,
        ),
    )

    # OmniBot robot articulation
    robot: ArticulationCfg = ArticulationCfg(
        prim_path='{ENV_REGEX_NS}/Robot',
        spawn=sim_utils.UsdFileCfg(
            usd_path=ROBOT_USD_PATH,
            rigid_props=sim_utils.RigidBodyPropertiesCfg(
                disable_gravity=False,
                max_depenetration_velocity=1.0,
            ),
            articulation_props=sim_utils.ArticulationRootPropertiesCfg(
                enabled_self_collisions=False,
                solver_position_iteration_count=4,
                solver_velocity_iteration_count=0,
            ),
        ),
        init_state=ArticulationCfg.InitialStateCfg(
            pos=(0.0, 0.0, 0.075),  # slightly above ground
            joint_pos={
                'front_left_wheel_joint':  0.0,
                'front_right_wheel_joint': 0.0,
                'rear_left_wheel_joint':   0.0,
                'rear_right_wheel_joint':  0.0,
            },
        ),
    )

    # Synthetic lidar via spherical ray-cast (replaces depth camera in sim)
    lidar_sensor: RayCasterCfg = RayCasterCfg(
        prim_path='{ENV_REGEX_NS}/Robot/base_link',
        mesh_prim_paths=['/World/ground', '{ENV_REGEX_NS}/Obstacles'],
        pattern_cfg=patterns.LidarPatternCfg(
            channels=1,
            vertical_fov_range=(0.0, 0.0),   # horizontal only
            horizontal_fov_range=(-180.0, 180.0),
            horizontal_res=45.0,              # 8 rays at 45° intervals
        ),
        max_distance=3.0,
        drift_range=(-0.005, 0.005),
        attach_yaw_only=True,
        debug_vis=False,
    )

    # Lighting
    light = AssetBaseCfg(
        prim_path='/World/light',
        spawn=sim_utils.DistantLightCfg(color=(0.75, 0.75, 0.75), intensity=3000.0),
    )


@configclass
class OmnibotNavActionsCfg:
    """Mecanum wheel velocity delta actions."""
    mecanum_drive: MecanumWheelActionTermCfg = MecanumWheelActionTermCfg(
        asset_name='robot',
        wheel_joint_ids=[0, 1, 2, 3],  # FL, FR, RL, RR
    )


@configclass
class OmnibotNavObservationsCfg:
    """27D observation vector for the navigation policy."""

    @configclass
    class PolicyCfg(ObservationGroupCfg):
        """Policy observation group."""
        # Goal-relative position, distance, yaw error, heading (5D)
        goal_relative: GoalRelativeObsTermCfg = GoalRelativeObsTermCfg()
        # Base velocity from odometry (3D)
        base_vel: ObservationTermCfg = ObservationTermCfg(
            func=lambda env: env.scene.articulations['robot'].data.root_lin_vel_b[
                :, :2].cat(env.scene.articulations['robot'].data.root_ang_vel_b[:, 2:3], dim=1),
        )
        # Lidar sectors (8D)
        lidar: LidarSectorObsTermCfg = LidarSectorObsTermCfg(noise_sigma_m=0.03)
        # Previous action (3D) — filled in by ActionManager

        concatenate_terms: bool = True
        enable_corruption: bool = True

    policy: PolicyCfg = PolicyCfg()


@configclass
class OmnibotNavRewardsCfg:
    """Navigation reward configuration matching nav_train.yaml weights."""
    approach       = RewardTermCfg(func=nav_goal_approach,      weight=1.0, params={'sigma': 0.5})
    heading        = RewardTermCfg(func=nav_heading_alignment,   weight=0.5)
    smoothness     = RewardTermCfg(func=nav_action_smoothness,   weight=0.1)
    collision      = RewardTermCfg(func=nav_collision_penalty,   weight=5.0)
    goal_reached   = RewardTermCfg(func=nav_goal_reached_bonus,  weight=20.0, params={'tolerance': 0.25})


@configclass
class OmnibotNavTerminationsCfg:
    """Navigation termination conditions."""
    timeout       = TerminationTermCfg(func=episode_timeout,     time_out=True)
    goal_reached  = TerminationTermCfg(func=nav_goal_reached,    params={'tolerance': 0.25})
    collision     = TerminationTermCfg(func=nav_collision,        params={'force_threshold': 10.0})


@configclass
class OmnibotNavEnvCfg(ManagerBasedRLEnvCfg):
    """
    Complete environment configuration for OmniBot navigation RL.

    Instantiate and pass to a training script:
      env_cfg = OmnibotNavEnvCfg()
      env_cfg.scene.num_envs = 512
    """
    # Scene
    scene: OmnibotNavSceneCfg = OmnibotNavSceneCfg(num_envs=512, env_spacing=5.0)

    # Managers
    actions:      OmnibotNavActionsCfg      = OmnibotNavActionsCfg()
    observations: OmnibotNavObservationsCfg = OmnibotNavObservationsCfg()
    rewards:      OmnibotNavRewardsCfg      = OmnibotNavRewardsCfg()
    terminations: OmnibotNavTerminationsCfg = OmnibotNavTerminationsCfg()

    # Episode
    episode_length_s: float = 25.0          # 500 steps at 20 Hz
    decimation:       int   = 5             # physics at 100 Hz, policy at 20 Hz

    def __post_init__(self):
        super().__post_init__()
        # Sim dt: 100 Hz physics
        self.sim.dt = 0.01
        self.sim.render_interval = self.decimation
