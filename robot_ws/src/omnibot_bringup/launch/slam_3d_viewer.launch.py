#!/usr/bin/env python3
"""
slam_3d_viewer.launch.py — Workstation-side RViz viewer for SLAM + 3-D map.

Run this on ANY machine that has ROS_DOMAIN_ID=30 and can reach the Pi.
The Pi must be running both perception.launch.py AND slam_3d_mapping.launch.py.

Setup on workstation (once):
    export ROS_DOMAIN_ID=30
    export ROS_STATIC_PEERS=<pi-ip>       # if not on the same subnet
    source /opt/ros/jazzy/setup.bash
    source ~/robot_ws/install/setup.bash

Run:
    ros2 launch omnibot_bringup slam_3d_viewer.launch.py

Displays (all sourced from the Pi over DDS):
  • Robot model
  • Virtual LaserScan          (/scan)
  • 2-D SLAM map               (/map  from slam_toolbox)
  • RTAB-Map 3-D point cloud   (/rtabmap/cloud_map)
  • OctoMap voxels             (/occupied_cells_vis_array)
  • OctoMap projected 2-D map  (/projected_map)
  • Odometry trail             (/odom)
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg_bringup = get_package_share_directory("omnibot_bringup")
    rviz_config = os.path.join(pkg_bringup, "config", "slam_3d_mapping.rviz")

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

    return LaunchDescription(
        [
            declare_rviz_config,
            rviz,
        ]
    )
