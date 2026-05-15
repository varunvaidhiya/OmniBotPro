"""
Reward functions for OmniBot Isaac Lab RL environments.

All functions follow the Isaac Lab RewardTerm signature:
  func(env: ManagerBasedRLEnv, **kwargs) -> torch.Tensor  (shape: num_envs,)
"""

import math

import torch
from isaaclab.envs import ManagerBasedRLEnv


# ── Navigation rewards ────────────────────────────────────────────────────────

def nav_goal_approach(env: ManagerBasedRLEnv, sigma: float = 0.5) -> torch.Tensor:
    """
    Exponential approach reward: exp(-dist / sigma).
    Dense reward that provides signal throughout the episode.
    """
    robot_pos = env.scene.articulations['robot'].data.root_pos_w[:, :2]
    goal_pos  = env.command_manager.get_command('goal_command')[:, :2]
    dist = torch.norm(goal_pos - robot_pos, dim=1)
    return torch.exp(-dist / sigma)


def nav_heading_alignment(env: ManagerBasedRLEnv) -> torch.Tensor:
    """
    Reward for facing the goal direction: cos(yaw_error).
    Range [-1, 1] — clips negative values to 0 to avoid penalising when far.
    """
    robot_pos  = env.scene.articulations['robot'].data.root_pos_w[:, :2]
    robot_quat = env.scene.articulations['robot'].data.root_quat_w
    goal_pos   = env.command_manager.get_command('goal_command')[:, :2]

    qw = robot_quat[:, 0]
    qz = robot_quat[:, 3]
    robot_yaw = 2.0 * torch.atan2(qz, qw)

    dx = goal_pos[:, 0] - robot_pos[:, 0]
    dy = goal_pos[:, 1] - robot_pos[:, 1]
    goal_yaw = torch.atan2(dy, dx)
    yaw_error = _angle_wrap(goal_yaw - robot_yaw)
    return torch.cos(yaw_error).clamp(min=0.0)


def nav_action_smoothness(env: ManagerBasedRLEnv) -> torch.Tensor:
    """Penalize large action changes (jerk): 1 - ||a_t - a_{t-1}||."""
    if not hasattr(env, '_prev_nav_action'):
        env._prev_nav_action = torch.zeros(
            env.num_envs, 3, device=env.device)
    curr = env.action_manager.action
    jerk = torch.norm(curr - env._prev_nav_action, dim=1)
    env._prev_nav_action = curr.clone()
    return (1.0 - jerk).clamp(min=0.0)


def nav_collision_penalty(env: ManagerBasedRLEnv) -> torch.Tensor:
    """
    Return -1.0 for environments in collision, 0.0 otherwise.
    Multiply by weight in reward config.
    """
    try:
        contact_sensor = env.scene.sensors['contact_sensor']
        in_contact = (contact_sensor.data.net_forces_w.norm(dim=-1) > 1.0).any(dim=-1)
    except (KeyError, AttributeError):
        in_contact = torch.zeros(env.num_envs, dtype=torch.bool, device=env.device)
    return -in_contact.float()


def nav_goal_reached_bonus(env: ManagerBasedRLEnv, tolerance: float = 0.25) -> torch.Tensor:
    """Sparse +1.0 bonus when goal is reached (dist < tolerance)."""
    robot_pos = env.scene.articulations['robot'].data.root_pos_w[:, :2]
    goal_pos  = env.command_manager.get_command('goal_command')[:, :2]
    dist = torch.norm(goal_pos - robot_pos, dim=1)
    return (dist < tolerance).float()


# ── Arm rewards ───────────────────────────────────────────────────────────────

def arm_ee_approach(
        env: ManagerBasedRLEnv,
        sigma: float = 0.05,
        ee_body_name: str = 'arm_gripper_link') -> torch.Tensor:
    """Exponential reward for end-effector approaching the target object."""
    try:
        ee_pos = env.scene.articulations['robot'].data.body_pos_w[
            :, env.scene.articulations['robot'].find_bodies(ee_body_name)[0]]
        target_pos = env.scene.rigid_objects['target_object'].data.root_pos_w
        dist = torch.norm(target_pos - ee_pos, dim=1)
        return torch.exp(-dist / sigma)
    except Exception:
        return torch.zeros(env.num_envs, device=env.device)


def arm_grasp_success(env: ManagerBasedRLEnv) -> torch.Tensor:
    """
    +1.0 when the gripper makes contact with the target object and
    gripper is partially closed (joint < 0.3 rad from closed position).
    """
    try:
        # Check gripper joint position (joint index 9 = gripper)
        gripper_pos = env.scene.articulations['robot'].data.joint_pos[:, 9]
        gripper_closed = gripper_pos > 0.5   # > 0.5 rad means partially closed

        contact_sensor = env.scene.sensors['gripper_contact_sensor']
        in_contact = (
            contact_sensor.data.net_forces_w.norm(dim=-1) > 0.5).any(dim=-1)

        return (gripper_closed & in_contact).float()
    except Exception:
        return torch.zeros(env.num_envs, device=env.device)


def arm_object_lifted(env: ManagerBasedRLEnv, lift_height: float = 0.05) -> torch.Tensor:
    """
    +1.0 when the target object has been lifted above lift_height from its
    initial resting position.
    """
    try:
        obj_z = env.scene.rigid_objects['target_object'].data.root_pos_w[:, 2]
        initial_z = env.extras.get('target_initial_z',
                                    torch.zeros(env.num_envs, device=env.device))
        lifted = (obj_z - initial_z) > lift_height
        return lifted.float()
    except Exception:
        return torch.zeros(env.num_envs, device=env.device)


def arm_object_placed(env: ManagerBasedRLEnv, place_tolerance: float = 0.05) -> torch.Tensor:
    """
    +1.0 (terminal) when the object is at the goal placement position.
    """
    try:
        obj_pos  = env.scene.rigid_objects['target_object'].data.root_pos_w[:, :3]
        goal_pos = env.extras.get('place_goal_pos',
                                   torch.zeros(env.num_envs, 3, device=env.device))
        dist = torch.norm(obj_pos - goal_pos, dim=1)
        return (dist < place_tolerance).float()
    except Exception:
        return torch.zeros(env.num_envs, device=env.device)


def arm_joint_limit_penalty(env: ManagerBasedRLEnv, margin: float = 0.1) -> torch.Tensor:
    """
    Small penalty when joints are within 'margin' radians of their limits.
    Encourages the policy to stay away from singularities.
    """
    from .actions import ARM_JOINT_MIN, ARM_JOINT_MAX  # noqa: F401

    joint_pos = env.scene.articulations['robot'].data.joint_pos[:, 4:10]
    jmin = ARM_JOINT_MIN.to(env.device)
    jmax = ARM_JOINT_MAX.to(env.device)

    near_min = (joint_pos - jmin) < margin
    near_max = (jmax - joint_pos) < margin
    return -(near_min | near_max).float().sum(dim=1)


def arm_action_smoothness(env: ManagerBasedRLEnv) -> torch.Tensor:
    """Penalize large arm delta actions (jerk)."""
    if not hasattr(env, '_prev_arm_action'):
        env._prev_arm_action = torch.zeros(env.num_envs, 6, device=env.device)
    curr = env.action_manager.action
    jerk = torch.norm(curr - env._prev_arm_action, dim=1)
    env._prev_arm_action = curr.clone()
    return -jerk


# ── Helpers ──────────────────────────────────────────────────────────────────

def _angle_wrap(angle: torch.Tensor) -> torch.Tensor:
    return (angle + math.pi) % (2 * math.pi) - math.pi
