"""OmniBot MDP terms (actions, observations, rewards, terminations)."""
from .actions import (
    MecanumWheelActionTerm,
    MecanumWheelActionTermCfg,
    ArmJointDeltaActionTerm,
    ArmJointDeltaActionTermCfg,
)
from .observations import (
    LidarSectorObsTerm,
    LidarSectorObsTermCfg,
    GoalRelativeObsTerm,
    GoalRelativeObsTermCfg,
    ArmNormJointPosObsTerm,
    ArmNormJointPosObsTermCfg,
)
from .rewards import (
    nav_goal_approach,
    nav_heading_alignment,
    nav_action_smoothness,
    nav_collision_penalty,
    nav_goal_reached_bonus,
    arm_ee_approach,
    arm_grasp_success,
    arm_object_lifted,
    arm_object_placed,
    arm_joint_limit_penalty,
    arm_action_smoothness,
)
from .terminations import (
    episode_timeout,
    nav_goal_reached,
    nav_collision,
    arm_place_success,
    arm_object_dropped,
    arm_self_collision,
)

__all__ = [
    'MecanumWheelActionTerm', 'MecanumWheelActionTermCfg',
    'ArmJointDeltaActionTerm', 'ArmJointDeltaActionTermCfg',
    'LidarSectorObsTerm', 'LidarSectorObsTermCfg',
    'GoalRelativeObsTerm', 'GoalRelativeObsTermCfg',
    'ArmNormJointPosObsTerm', 'ArmNormJointPosObsTermCfg',
    'nav_goal_approach', 'nav_heading_alignment', 'nav_action_smoothness',
    'nav_collision_penalty', 'nav_goal_reached_bonus',
    'arm_ee_approach', 'arm_grasp_success', 'arm_object_lifted',
    'arm_object_placed', 'arm_joint_limit_penalty', 'arm_action_smoothness',
    'episode_timeout', 'nav_goal_reached', 'nav_collision',
    'arm_place_success', 'arm_object_dropped', 'arm_self_collision',
]
