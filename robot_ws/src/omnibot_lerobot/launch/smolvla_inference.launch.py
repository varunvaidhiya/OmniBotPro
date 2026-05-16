#!/usr/bin/env python3
"""
SmolVLA inference launch for OmniBot mobile manipulation.

Remappings applied here so smolvla_node.py stays hardware-agnostic:
  /cmd_vel          → /cmd_vel/vla     (cmd_vel_mux selects this in "vla" mode)
  /arm/joint_commands stays as-is     (arm_cmd_mux routes to /arm/joint_commands/out)

Usage:
    ros2 launch omnibot_lerobot smolvla_inference.launch.py
    ros2 launch omnibot_lerobot smolvla_inference.launch.py checkpoint:=lerobot/smolvla_base
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg = get_package_share_directory("omnibot_lerobot")
    smolvla_params = os.path.join(pkg, "config", "smolvla_params.yaml")

    checkpoint = LaunchConfiguration("checkpoint")
    device = LaunchConfiguration("device")

    smolvla_node = Node(
        package="omnibot_lerobot",
        executable="smolvla_node",
        name="smolvla_node",
        output="screen",
        parameters=[
            smolvla_params,
            {"checkpoint_path": checkpoint, "device": device},
        ],
        remappings=[
            # Route base velocity into the cmd_vel_mux "vla" slot
            ("/cmd_vel", "/cmd_vel/vla"),
        ],
    )

    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "checkpoint",
                default_value="lerobot/smolvla_base",
                description="HuggingFace model ID or local path for SmolVLA checkpoint",
            ),
            DeclareLaunchArgument(
                "device",
                default_value="cuda",
                description="PyTorch device: cuda or cpu",
            ),
            smolvla_node,
        ]
    )
