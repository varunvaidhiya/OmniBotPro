#!/usr/bin/env python3
"""
SmolVLA inference launch for OmniBot mobile manipulation.

Remappings applied here so smolvla_node.py stays hardware-agnostic:
  /cmd_vel          → /cmd_vel/vla     (cmd_vel_mux selects this in "vla" mode)
  /arm/joint_commands stays as-is     (arm_cmd_mux routes to /arm/joint_commands/out)

Also starts arm_cmd_mux so SmolVLA arm commands reach arm_driver_node even
when rl_inference.launch.py is not running. Pass include_arm_mux:=false if
arm_cmd_mux is already started by a parent launch.

Usage:
    ros2 launch omnibot_lerobot smolvla_inference.launch.py
    ros2 launch omnibot_lerobot smolvla_inference.launch.py checkpoint:=lerobot/smolvla_base
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
    smolvla_params = os.path.join(pkg_lerobot, "config", "smolvla_params.yaml")
    arm_params = os.path.join(pkg_rl, "config", "rl_arm_params.yaml")

    checkpoint = LaunchConfiguration("checkpoint")
    device = LaunchConfiguration("device")
    include_arm_mux = LaunchConfiguration("include_arm_mux")

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

    # arm_cmd_mux: bridges /arm/joint_commands (SmolVLA) → /arm/joint_commands/out
    # → arm_driver_node. Skip with include_arm_mux:=false when a parent launch
    # already starts this node.
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
                "checkpoint",
                default_value="lerobot/smolvla_base",
                description="HuggingFace model ID or local path for SmolVLA checkpoint",
            ),
            DeclareLaunchArgument(
                "device",
                default_value="cuda",
                description="PyTorch device: cuda or cpu",
            ),
            DeclareLaunchArgument(
                "include_arm_mux",
                default_value="true",
                description=(
                    "Start arm_cmd_mux here. Set false if a parent launch "
                    "already starts it."
                ),
            ),
            smolvla_node,
            arm_cmd_mux_node,
        ]
    )
