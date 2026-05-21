#!/usr/bin/env python3
"""Policy inference launch for OmniBot mobile manipulation.

Starts the model-agnostic policy_node and (optionally) the arm_cmd_mux.
Swap the model by passing model_type:=act, model_type:=diffusion, etc.

Remappings applied:
  /cmd_vel → /cmd_vel/vla   (cmd_vel_mux routes this in "vla" mode)

Usage
-----
ros2 launch omnibot_lerobot policy_inference.launch.py
ros2 launch omnibot_lerobot policy_inference.launch.py model_type:=act checkpoint:=lerobot/act_base
ros2 launch omnibot_lerobot policy_inference.launch.py model_type:=smolvla checkpoint:=~/checkpoints/smolvla_run1/best
ros2 launch omnibot_lerobot policy_inference.launch.py include_arm_mux:=false
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg_lerobot = get_package_share_directory("omnibot_lerobot")
    pkg_rl = get_package_share_directory("omnibot_rl")
    policy_params = os.path.join(pkg_lerobot, "config", "policy_params.yaml")
    arm_params = os.path.join(pkg_rl, "config", "rl_arm_params.yaml")

    model_type = LaunchConfiguration("model_type")
    checkpoint = LaunchConfiguration("checkpoint")
    device = LaunchConfiguration("device")
    include_arm_mux = LaunchConfiguration("include_arm_mux")

    policy_node = Node(
        package="omnibot_lerobot",
        executable="policy_node",
        name="policy_node",
        output="screen",
        parameters=[
            policy_params,
            {
                "model_type": model_type,
                "checkpoint_path": checkpoint,
                "device": device,
            },
        ],
        remappings=[
            ("/cmd_vel", "/cmd_vel/vla"),
        ],
    )

    arm_cmd_mux_node = Node(
        package="omnibot_rl",
        executable="arm_cmd_mux",
        name="arm_cmd_mux",
        output="screen",
        parameters=[arm_params],
        condition=IfCondition(include_arm_mux),
    )

    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "model_type",
                default_value="smolvla",
                description="Policy model type: smolvla | act | diffusion | openvla",
            ),
            DeclareLaunchArgument(
                "checkpoint",
                default_value="lerobot/smolvla_base",
                description="HuggingFace hub ID or local path to the model checkpoint",
            ),
            DeclareLaunchArgument(
                "device",
                default_value="cuda",
                description="Inference device: cuda | cpu",
            ),
            DeclareLaunchArgument(
                "include_arm_mux",
                default_value="true",
                description="Start arm_cmd_mux here. Set false if a parent launch already starts it.",
            ),
            policy_node,
            arm_cmd_mux_node,
        ]
    )
