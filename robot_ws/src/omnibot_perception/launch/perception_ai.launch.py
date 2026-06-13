#!/usr/bin/env python3
"""
perception_ai.launch.py — modular AI perception layer.

Launches object_perception_node (object distance measurement + pose
estimation from the depth camera, optional YOLO labels).

Run alongside perception.launch.py (which provides the camera drivers):
    ros2 launch omnibot_perception perception_ai.launch.py
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch_ros.actions import Node


def generate_launch_description():
    params = os.path.join(
        get_package_share_directory("omnibot_perception"),
        "config",
        "perception_params.yaml",
    )
    return LaunchDescription(
        [
            Node(
                package="omnibot_perception",
                executable="object_perception_node",
                name="object_perception_node",
                output="screen",
                parameters=[params],
            )
        ]
    )
