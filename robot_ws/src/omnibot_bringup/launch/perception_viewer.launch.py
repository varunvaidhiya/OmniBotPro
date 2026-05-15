#!/usr/bin/env python3
"""
perception_viewer.launch.py — Workstation-side RViz viewer for all cameras.

Run this on ANY machine that has ROS_DOMAIN_ID=30 and can reach the Pi
over the network.  The Pi must be running perception.launch.py.

Setup on workstation (once):
    export ROS_DOMAIN_ID=30
    export ROS_STATIC_PEERS=<pi-ip>       # if not on the same subnet
    source /opt/ros/jazzy/setup.bash
    source ~/robot_ws/install/setup.bash  # for the omnibot_bringup config

Run:
    ros2 launch omnibot_bringup perception_viewer.launch.py

Displays (all sourced from the Pi over DDS):
  • Robot model + TF tree
  • Camera feeds: front / rear / left / right / wrist / color (Astra) / depth
  • BEV stitched image  (/camera/base/bev/image_raw)
  • Depth PointCloud    (/camera/depth/points)
  • Virtual LaserScan   (/scan — from depthimage_to_laserscan on Pi)
  • Odometry arrow      (/odom)
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg_bringup = get_package_share_directory("omnibot_bringup")
    rviz_config = os.path.join(pkg_bringup, "config", "perception_full.rviz")

    declare_rviz_config = DeclareLaunchArgument(
        "rviz_config",
        default_value=rviz_config,
        description="Path to RViz config file",
    )

    rviz = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        output="screen",
        arguments=["-d", LaunchConfiguration("rviz_config")],
    )

    return LaunchDescription([
        declare_rviz_config,
        rviz,
    ])
