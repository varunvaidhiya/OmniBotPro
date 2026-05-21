#!/usr/bin/env python3
"""
state_estimation.launch.py — robot_localization EKF for OmniBot.

Fuses wheel odometry (/odom) and IMU (/imu/data) into a single, drift-
corrected pose estimate:

  /odom          (raw — commanded-velocity dead-reckoning, from yahboom_controller_node)
  /imu/data      (Yahboom board IMU — accel, gyro, fused attitude)
       ↓
  ekf_node       (Extended Kalman Filter — robot_localization)
       ↓
  /odometry/filtered   (fused pose + twist, lower noise, IMU-corrected heading)
  TF: odom → base_link (EKF owns this — driver must run with publish_tf:=false)

Why this matters
----------------
Without the EKF the robot publishes raw dead-reckoning: position is
integrated from the *commanded* velocity, not actual encoder feedback.
The IMU heading is discarded entirely. Over time this produces large yaw
errors that confuse SLAM and Nav2.  The EKF:
  1. Corrects yaw drift using the IMU gyroscope and fused attitude angles.
  2. Reduces velocity noise with the Kalman gain.
  3. Provides a single authoritative odom→base_link TF frame.

Usage
-----
This launch file is included by robot.launch.py (ekf:=true by default).
Run standalone only for debugging:

    ros2 launch omnibot_bringup state_estimation.launch.py

Verify it is working:
    ros2 topic hz /odometry/filtered   # should match ~30 Hz
    ros2 topic echo /odometry/filtered --field pose.pose.position
    ros2 run tf2_ros tf2_echo odom base_link
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg_nav = get_package_share_directory("omnibot_navigation")
    ekf_config = os.path.join(pkg_nav, "config", "robot_localization.yaml")

    declare_use_sim_time = DeclareLaunchArgument(
        "use_sim_time",
        default_value="false",
        description="Use simulation clock",
    )

    ekf_node = Node(
        package="robot_localization",
        executable="ekf_node",
        name="ekf_filter_node",
        output="screen",
        parameters=[
            ekf_config,
            {"use_sim_time": LaunchConfiguration("use_sim_time")},
        ],
        # ekf_node publishes to odometry/filtered by default;
        # remap to the global absolute topic so SLAM and Nav2 find it.
        remappings=[
            ("odometry/filtered", "/odometry/filtered"),
        ],
    )

    return LaunchDescription(
        [
            declare_use_sim_time,
            ekf_node,
        ]
    )
