"""
Termination conditions for OmniBot Isaac Lab RL environments.

All functions follow the Isaac Lab TerminationTerm signature:
  func(env: ManagerBasedRLEnv, **kwargs) -> torch.Tensor  (bool, shape: num_envs,)
"""

import torch
from isaaclab.envs import ManagerBasedRLEnv


def episode_timeout(env: ManagerBasedRLEnv) -> torch.Tensor:
    """Terminate when the episode step limit is reached."""
    return env.episode_length_buf >= env.max_episode_length


def nav_goal_reached(env: ManagerBasedRLEnv, tolerance: float = 0.25) -> torch.Tensor:
    """Terminate (success) when the robot reaches the navigation goal."""
    robot_pos = env.scene.articulations['robot'].data.root_pos_w[:, :2]
    goal_pos  = env.command_manager.get_command('goal_command')[:, :2]
    dist = torch.norm(goal_pos - robot_pos, dim=1)
    return dist < tolerance


def nav_collision(env: ManagerBasedRLEnv, force_threshold: float = 10.0) -> torch.Tensor:
    """Terminate when the robot body sustains a collision force above threshold."""
    try:
        contact_sensor = env.scene.sensors['contact_sensor']
        max_force = contact_sensor.data.net_forces_w.norm(dim=-1).max(dim=-1).values
        return max_force > force_threshold
    except (KeyError, AttributeError):
        return torch.zeros(env.num_envs, dtype=torch.bool, device=env.device)


def arm_place_success(env: ManagerBasedRLEnv, tolerance: float = 0.05) -> torch.Tensor:
    """Terminate (success) when the object reaches the placement goal."""
    try:
        obj_pos  = env.scene.rigid_objects['target_object'].data.root_pos_w[:, :3]
        goal_pos = env.extras.get('place_goal_pos',
                                   torch.zeros(env.num_envs, 3, device=env.device))
        dist = torch.norm(obj_pos - goal_pos, dim=1)
        return dist < tolerance
    except Exception:
        return torch.zeros(env.num_envs, dtype=torch.bool, device=env.device)


def arm_object_dropped(env: ManagerBasedRLEnv, drop_height: float = -0.05) -> torch.Tensor:
    """Terminate (failure) when the object falls below its initial z position."""
    try:
        obj_z = env.scene.rigid_objects['target_object'].data.root_pos_w[:, 2]
        initial_z = env.extras.get('target_initial_z',
                                    torch.zeros(env.num_envs, device=env.device))
        return (obj_z - initial_z) < drop_height
    except Exception:
        return torch.zeros(env.num_envs, dtype=torch.bool, device=env.device)


def arm_self_collision(env: ManagerBasedRLEnv, force_threshold: float = 5.0) -> torch.Tensor:
    """Terminate when arm self-collision force exceeds threshold."""
    try:
        arm_contact = env.scene.sensors['arm_contact_sensor']
        max_force = arm_contact.data.net_forces_w.norm(dim=-1).max(dim=-1).values
        return max_force > force_threshold
    except (KeyError, AttributeError):
        return torch.zeros(env.num_envs, dtype=torch.bool, device=env.device)
