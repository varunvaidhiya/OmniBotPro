#!/usr/bin/env python3
"""
rtabmap.launch.py — 3-D RGB-D SLAM for OmniBot using Orbbec Astra Pro.

Starts (physical robot):
  1. astra_camera      — ros2_astra_camera driver (Orbbec OpenNI2 SDK)
  2. rtabmap           — RGB-D SLAM with loop-closure + OctoMap output
  3. octomap_server    — converts RTAB-Map cloud_map → projected 2-D map

In simulation (use_sim_time:=true) the astra_camera driver is skipped;
Gazebo's built-in RGBD sensor + ros_gz_bridge supply the same topics.

Topic layout from ros2_astra_camera driver / Gazebo bridge:
  /camera/color/image_raw    ← RGB 1280×960 (real) or 640×480 (sim)
  /camera/color/camera_info  ← RGB intrinsics
  /camera/depth/image_raw    ← 16-bit depth in mm, 640×480
  /camera/depth/camera_info  ← depth intrinsics
  /camera/depth/points       ← PointCloud2 (unregistered, for Nav2)

RTAB-Map outputs:
  /rtabmap/cloud_map         → PointCloud2 accumulated map
  /rtabmap/octomap_binary    → OctoMap (binary)
  /rtabmap/octomap_full      → OctoMap (full probability)
  /projected_map             → OccupancyGrid (Nav2-compatible 2-D slice)

Usage:
  # Physical robot
  ros2 launch omnibot_navigation rtabmap.launch.py

  # Gazebo simulation
  ros2 launch omnibot_navigation rtabmap.launch.py use_sim_time:=true

Install Orbbec driver (build from source — not on apt):
  sudo apt install libgflags-dev ros-jazzy-image-geometry \\
      ros-jazzy-camera-info-manager ros-jazzy-image-transport \\
      ros-jazzy-image-publisher libgoogle-glog-dev libusb-1.0-0-dev \\
      libeigen3-dev
  cd ~/robot_ws/src
  git clone https://github.com/orbbec/ros2_astra_camera --depth 1
  cd ~/robot_ws && colcon build --packages-select astra_camera
"""

import os
from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import UnlessCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    nav_dir = get_package_share_directory("omnibot_navigation")
    rtabmap_params = os.path.join(nav_dir, "config", "rtabmap_params.yaml")
    octomap_params = os.path.join(nav_dir, "config", "octomap_params.yaml")

    use_sim_time = LaunchConfiguration("use_sim_time")

    declare_use_sim_time = DeclareLaunchArgument(
        "use_sim_time",
        default_value="false",
        description="Use Gazebo clock; skips the real Astra Pro driver when true",
    )

    # ── Orbbec Astra Pro driver (physical robot only) ─────────────────────────
    # Skipped in simulation — Gazebo RGBD sensor + ros_gz_bridge provide the
    # same topics (/camera/color/image_raw, /camera/depth/image_raw, etc.)
    #
    # The ros2_astra_camera package must be built from source:
    #   https://github.com/orbbec/ros2_astra_camera
    astra_camera_node = Node(
        package="astra_camera",
        executable="astra_camera_node",
        name="astra_camera",
        namespace="camera",
        output="screen",
        parameters=[
            {
                "use_sim_time": use_sim_time,
                # Publish depth registered with colour for better RTAB-Map quality
                "depth_registration": True,
                # Astra Pro device type
                "camera_name": "camera",
                "color_width": 640,
                "color_height": 480,
                "color_fps": 30,
                "depth_width": 640,
                "depth_height": 480,
                "depth_fps": 30,
                "enable_point_cloud": True,
                "enable_colored_point_cloud": False,  # unregistered cloud for Nav2
            }
        ],
        # Only start on physical robot
        condition=UnlessCondition(use_sim_time),
    )

    # ── RTAB-Map core node ────────────────────────────────────────────────────
    # Full RGB-D SLAM: uses colour image for loop-closure bag-of-words and
    # depth image for 3-D reconstruction + OctoMap generation.
    rtabmap_node = Node(
        package="rtabmap_ros",
        executable="rtabmap",
        name="rtabmap",
        output="screen",
        parameters=[
            rtabmap_params,
            {"use_sim_time": use_sim_time},
        ],
        remappings=[
            # Astra Pro driver publishes colour and depth on separate topics;
            # this gives RTAB-Map real RGB for superior loop-closure detection.
            ("rgb/image", "/camera/color/image_raw"),
            ("rgb/camera_info", "/camera/color/camera_info"),
            ("depth/image", "/camera/depth/image_raw"),
            ("depth/camera_info", "/camera/depth/camera_info"),
            # Odometry from EKF
            ("odom", "/odom"),
            # Published map topics
            ("grid_map", "/rtabmap/grid_map"),
        ],
        arguments=["--delete_db_on_start"],  # remove to preserve map across restarts
    )

    # ── OctoMap server ────────────────────────────────────────────────────────
    # Consumes /rtabmap/cloud_map (full accumulated PointCloud2) and publishes:
    #   /octomap_binary        — compressed OctoMap
    #   /octomap_full          — full-probability OctoMap
    #   /projected_map         — 2-D OccupancyGrid (Nav2 compatible)
    octomap_server_node = Node(
        package="octomap_server",
        executable="octomap_server_node",
        name="octomap_server",
        output="screen",
        parameters=[
            octomap_params,
            {"use_sim_time": use_sim_time},
        ],
        remappings=[
            ("cloud_in", "/rtabmap/cloud_map"),
        ],
    )

    ld = LaunchDescription()
    ld.add_action(declare_use_sim_time)
    ld.add_action(astra_camera_node)
    ld.add_action(rtabmap_node)
    ld.add_action(octomap_server_node)
    # Uncomment to open the RTAB-Map visualiser:
    # ld.add_action(Node(package='rtabmap_ros', executable='rtabmapviz', ...))

    return ld
