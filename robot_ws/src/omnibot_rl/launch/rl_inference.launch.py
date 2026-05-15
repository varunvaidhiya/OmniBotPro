#!/usr/bin/env python3
"""
RL Inference Launch — Isaac Lab policy deployment nodes for OmniBot.

Launches four nodes that add RL-based control alongside the existing
Nav2 + SmolVLA hybrid stack.  All nodes are additive — the existing
stack continues to work unchanged when RL modes are not active.

Nodes started
─────────────
  rl_nav_node        — runs Isaac Lab nav policy, publishes /cmd_vel/rl
  rl_arm_node        — runs Isaac Lab arm policy, publishes /arm/joint_commands/rl
  arm_cmd_mux        — routes SmolVLA or RL arm commands to /arm/joint_commands/out
  rl_object_pose_node — ArUco-based object pose estimation for arm policy

Usage
─────
  # Include in hybrid_robot.launch.py via use_rl:=true, or launch standalone:
  ros2 launch omnibot_rl rl_inference.launch.py

  # Disable ArUco pose node (if using external perception):
  ros2 launch omnibot_rl rl_inference.launch.py use_object_pose:=false

  # Test RL navigation only (no arm RL):
  ros2 topic pub /control_mode std_msgs/msg/String "data: 'rl_nav'"
  ros2 topic pub /rl_nav/goal geometry_msgs/msg/PoseStamped \\
    "{header: {frame_id: 'map'}, pose: {position: {x: 1.5, y: 0.0}}}"

  # Test RL arm only:
  ros2 topic pub /arm/cmd_mode std_msgs/msg/String "data: 'rl_arm'"

  # Return to SmolVLA arm control:
  ros2 topic pub /arm/cmd_mode std_msgs/msg/String "data: 'smolvla'"
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg_rl = get_package_share_directory('omnibot_rl')

    nav_params  = os.path.join(pkg_rl, 'config', 'rl_nav_params.yaml')
    arm_params  = os.path.join(pkg_rl, 'config', 'rl_arm_params.yaml')

    use_sim_time      = LaunchConfiguration('use_sim_time',      default='false')
    use_object_pose   = LaunchConfiguration('use_object_pose',   default='true')

    # ── RL Navigation Node ────────────────────────────────────────────────────
    # Subscribes: /odom, /camera/depth/points, /rl_nav/goal, /control_mode/active
    # Publishes:  /cmd_vel/rl  (forwarded by cmd_vel_mux when mode="rl_nav")
    rl_nav_node = Node(
        package='omnibot_rl',
        executable='rl_nav_node',
        name='rl_nav_node',
        output='screen',
        parameters=[
            nav_params,
            {'use_sim_time': use_sim_time},
        ],
    )

    # ── RL Arm Node ───────────────────────────────────────────────────────────
    # Subscribes: /arm/joint_states, /rl_arm/target_pose, /rl_arm/enable, /arm/cmd_mode
    # Publishes:  /arm/joint_commands/rl  (forwarded by arm_cmd_mux when mode="rl_arm")
    rl_arm_node = Node(
        package='omnibot_rl',
        executable='rl_arm_node',
        name='rl_arm_node',
        output='screen',
        parameters=[
            arm_params,
            {'use_sim_time': use_sim_time},
        ],
    )

    # ── Arm Command Mux ───────────────────────────────────────────────────────
    # Subscribes: /arm/joint_commands (SmolVLA), /arm/joint_commands/rl, /arm/cmd_mode
    # Publishes:  /arm/joint_commands/out  → arm_driver_node
    # Default mode: "smolvla" — transparent pass-through, no behaviour change.
    arm_cmd_mux_node = Node(
        package='omnibot_rl',
        executable='arm_cmd_mux',
        name='arm_cmd_mux',
        output='screen',
        parameters=[
            arm_params,
            {'use_sim_time': use_sim_time},
        ],
    )

    # ── Object Pose Node ──────────────────────────────────────────────────────
    # Subscribes: /camera/wrist/image_raw, /camera/depth/image_raw,
    #             /camera/wrist/camera_info
    # Publishes:  /rl_arm/target_pose, /rl_arm/target_detected
    rl_object_pose_node = Node(
        package='omnibot_rl',
        executable='rl_object_pose_node',
        name='rl_object_pose_node',
        output='screen',
        condition=IfCondition(use_object_pose),
        parameters=[
            arm_params,
            {'use_sim_time': use_sim_time},
        ],
    )

    return LaunchDescription([
        DeclareLaunchArgument(
            'use_sim_time', default_value='false',
            description='Use simulation clock'),
        DeclareLaunchArgument(
            'use_object_pose', default_value='true',
            description='Launch ArUco-based object pose estimation node'),

        rl_nav_node,
        rl_arm_node,
        arm_cmd_mux_node,
        rl_object_pose_node,
    ])
