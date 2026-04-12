"""
OmniBot Arm Manipulation RL Environment for Isaac Lab.

Trains a precision pick-and-place arm policy using joint position deltas.
Robot base is fixed during arm training to isolate arm learning.

Observation space (30D):
  [0:6]   joint_pos_norm   — arm joints normalized to [-1, 1]
  [6:11]  arm_joint_vel    — joints 0-4 angular velocity (rad/s)
  [11:14] ee_pos           — end-effector position in base_link frame (m)
  [14:20] ee_rot_6d        — EE rotation matrix columns 0+1 (6D continuous)
  [20:23] target_pos       — target object position in base_link (m) + pose noise
  [23]    gripper_opening  — normalized [0, 1]
  [24:30] previous_action  — last 6D delta action

Action space (6D):
  [Δq0 … Δq5] — joint position deltas (rad/step), clipped to ±0.05 rad

See rl_engine/config/arm_train.yaml for training hyperparameters.

Usage:
  python rl_engine/scripts/train_arm.py --num_envs 256
"""

from __future__ import annotations

from dataclasses import dataclass, field

import isaaclab.sim as sim_utils
from isaaclab.assets import ArticulationCfg, RigidObjectCfg
from isaaclab.envs import ManagerBasedRLEnvCfg
from isaaclab.managers import (
    ObservationGroupCfg,
    ObservationTermCfg,
    RewardTermCfg,
    TerminationTermCfg,
)
from isaaclab.scene import InteractiveSceneCfg
from isaaclab.sensors import ContactSensorCfg
from isaaclab.utils import configclass

from rl_engine.tasks.mdp import (
    ArmJointDeltaActionTermCfg,
    ArmNormJointPosObsTermCfg,
    arm_ee_approach,
    arm_grasp_success,
    arm_object_lifted,
    arm_object_placed,
    arm_joint_limit_penalty,
    arm_action_smoothness,
    episode_timeout,
    arm_place_success,
    arm_object_dropped,
    arm_self_collision,
)

ROBOT_USD_PATH  = "{ROBOT_WS}/src/omnibot_description/usd/omnibot.usd"
# Default target object — override with curriculum_stages in arm_train.yaml
MUSTARD_USD = "Isaac/Props/YCB/Obj_006_mustard_bottle/mustard_bottle.usd"


@configclass
class OmnibotArmSceneCfg(InteractiveSceneCfg):
    """Scene: flat table + fixed-base OmniBot + target object + contact sensors."""

    terrain = sim_utils.GroundPlaneCfg()

    # Table (fixed RigidObject)
    table: sim_utils.UsdFileCfg = sim_utils.UsdFileCfg(
        prim_path='{ENV_REGEX_NS}/Table',
        spawn=sim_utils.CuboidCfg(
            size=(0.6, 0.8, 0.74),
            rigid_props=sim_utils.RigidBodyPropertiesCfg(kinematic_enabled=True),
            mass_props=sim_utils.MassPropertiesCfg(mass=20.0),
            collision_props=sim_utils.CollisionPropertiesCfg(),
            visual_material=sim_utils.PreviewSurfaceCfg(diffuse_color=(0.6, 0.5, 0.4)),
        ),
        init_state=sim_utils.UsdFileCfg.InitialStateCfg(pos=(0.5, 0.0, 0.37)),
    )

    # Robot (base fixed in place for arm training)
    robot: ArticulationCfg = ArticulationCfg(
        prim_path='{ENV_REGEX_NS}/Robot',
        spawn=sim_utils.UsdFileCfg(
            usd_path=ROBOT_USD_PATH,
            rigid_props=sim_utils.RigidBodyPropertiesCfg(disable_gravity=False),
            articulation_props=sim_utils.ArticulationRootPropertiesCfg(
                fix_root_link=True,   # Fixed base for arm-only training
                enabled_self_collisions=False,
            ),
        ),
        init_state=ArticulationCfg.InitialStateCfg(
            pos=(0.0, 0.0, 0.075),
            joint_pos={
                'arm_shoulder_pan':   0.0,
                'arm_shoulder_lift': -0.5,
                'arm_elbow_flex':     0.5,
                'arm_wrist_flex':     0.0,
                'arm_wrist_roll':     0.0,
                'arm_gripper':        0.0,
            },
        ),
    )

    # Target object (randomized per episode in curriculum)
    target_object: RigidObjectCfg = RigidObjectCfg(
        prim_path='{ENV_REGEX_NS}/TargetObject',
        spawn=sim_utils.UsdFileCfg(
            usd_path=MUSTARD_USD,
            rigid_props=sim_utils.RigidBodyPropertiesCfg(disable_gravity=False),
            mass_props=sim_utils.MassPropertiesCfg(mass=0.6),
            collision_props=sim_utils.CollisionPropertiesCfg(),
        ),
        init_state=RigidObjectCfg.InitialStateCfg(pos=(0.45, 0.0, 0.75)),
    )

    # Gripper contact sensor
    gripper_contact_sensor: ContactSensorCfg = ContactSensorCfg(
        prim_path='{ENV_REGEX_NS}/Robot/arm_gripper_link',
        update_period=0.0,
        history_length=6,
        debug_vis=False,
    )

    # Light
    light = sim_utils.DistantLightCfg(color=(0.75, 0.75, 0.75), intensity=3000.0)


@configclass
class OmnibotArmActionsCfg:
    """Joint position delta actions for the arm."""
    arm_deltas: ArmJointDeltaActionTermCfg = ArmJointDeltaActionTermCfg(
        asset_name='robot',
        arm_joint_ids=[4, 5, 6, 7, 8, 9],  # arm joints in articulation order
    )


@configclass
class OmnibotArmObservationsCfg:
    """30D arm observation space."""

    @configclass
    class PolicyCfg(ObservationGroupCfg):
        joint_pos_norm: ArmNormJointPosObsTermCfg = ArmNormJointPosObsTermCfg(
            arm_joint_ids=[4, 5, 6, 7, 8, 9])
        # Additional terms (arm_joint_vel, ee_pos, ee_rot, target_pos,
        # gripper_opening, prev_action) are handled by the env's
        # observation manager once the full env class is implemented.
        concatenate_terms: bool = True
        enable_corruption: bool = True

    policy: PolicyCfg = PolicyCfg()


@configclass
class OmnibotArmRewardsCfg:
    """Arm manipulation rewards with weights from arm_train.yaml."""
    ee_approach     = RewardTermCfg(func=arm_ee_approach,       weight=2.0,   params={'sigma': 0.05})
    grasp_success   = RewardTermCfg(func=arm_grasp_success,     weight=10.0)
    object_lifted   = RewardTermCfg(func=arm_object_lifted,     weight=20.0,  params={'lift_height': 0.05})
    object_placed   = RewardTermCfg(func=arm_object_placed,     weight=50.0)
    joint_limits    = RewardTermCfg(func=arm_joint_limit_penalty, weight=0.1)
    smoothness      = RewardTermCfg(func=arm_action_smoothness,  weight=0.01)


@configclass
class OmnibotArmTerminationsCfg:
    """Arm manipulation terminations."""
    timeout        = TerminationTermCfg(func=episode_timeout,    time_out=True)
    place_success  = TerminationTermCfg(func=arm_place_success,  params={'tolerance': 0.05})
    object_dropped = TerminationTermCfg(func=arm_object_dropped, params={'drop_height': -0.05})
    self_collision = TerminationTermCfg(func=arm_self_collision,  params={'force_threshold': 5.0})


@configclass
class OmnibotArmEnvCfg(ManagerBasedRLEnvCfg):
    """
    Complete environment configuration for OmniBot arm manipulation RL.

    Instantiate and pass to a training script:
      env_cfg = OmnibotArmEnvCfg()
      env_cfg.scene.num_envs = 256
    """
    scene:        OmnibotArmSceneCfg        = OmnibotArmSceneCfg(num_envs=256, env_spacing=2.0)
    actions:      OmnibotArmActionsCfg      = OmnibotArmActionsCfg()
    observations: OmnibotArmObservationsCfg = OmnibotArmObservationsCfg()
    rewards:      OmnibotArmRewardsCfg      = OmnibotArmRewardsCfg()
    terminations: OmnibotArmTerminationsCfg = OmnibotArmTerminationsCfg()

    episode_length_s: float = 20.0   # 400 steps at 20 Hz
    decimation:       int   = 5      # physics at 100 Hz, policy at 20 Hz

    def __post_init__(self):
        super().__post_init__()
        self.sim.dt = 0.01
        self.sim.render_interval = self.decimation
