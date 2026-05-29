#!/usr/bin/env python3
"""
slam_3d_mapping.launch.py — 2-D SLAM + 3-D RGB-D mapping pipeline.

Must be run AFTER (or alongside) perception.launch.py, which provides:
  /scan                        from depthimage_to_laserscan
  /camera/color/image_raw      from Astra Pro
  /camera/depth/image_raw      from Astra Pro
  /camera/depth/camera_info
  /odom                        from yahboom_controller_node / serial_bridge_node
  /tf  (odom→base_link)

Starts:
  1. slam_toolbox (async)     2-D LiDAR SLAM → /map + /tf (map→odom)
  2. rtabmap                  RGB-D SLAM → /rtabmap/cloud_map, loop-closure
  3. octomap_server           3-D voxel map → /occupied_cells_vis_array
                              + /projected_map (2-D slice for Nav2)
  4. rviz2                    (optional — default true so you can see results
                               immediately; use slam_3d_viewer.launch.py on
                               the workstation instead for headless Pi runs)

Run on Pi (alongside perception.launch.py):
    ros2 launch omnibot_bringup slam_3d_mapping.launch.py

Headless Pi + viewer on workstation:
    # Pi
    ros2 launch omnibot_bringup slam_3d_mapping.launch.py rviz:=false
    # Workstation
    ros2 launch omnibot_bringup slam_3d_viewer.launch.py

SLAM only (no 3-D mapping):
    ros2 launch omnibot_bringup slam_3d_mapping.launch.py slam3d:=false

3-D mapping only (no slam_toolbox):
    ros2 launch omnibot_bringup slam_3d_mapping.launch.py slam2d:=false

Save / re-use a SLAM map (localization mode):
    ros2 launch omnibot_bringup slam_3d_mapping.launch.py \\
        slam_mode:=localization map_file:=/path/to/omnibot_map
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg_nav = get_package_share_directory("omnibot_navigation")
    pkg_bringup = get_package_share_directory("omnibot_bringup")

    slam_params = os.path.join(pkg_nav, "config", "slam_toolbox_params.yaml")
    rtabmap_params = os.path.join(pkg_nav, "config", "rtabmap_params.yaml")
    octomap_params = os.path.join(pkg_nav, "config", "octomap_params.yaml")
    rviz_config = os.path.join(pkg_bringup, "config", "slam_3d_mapping.rviz")

    # ── Launch arguments ──────────────────────────────────────────────────────
    declare_rviz = DeclareLaunchArgument(
        "rviz",
        default_value="true",
        description="Launch RViz2 for SLAM + 3-D mapping visualisation",
    )
    declare_slam2d = DeclareLaunchArgument(
        "slam2d",
        default_value="true",
        description="Run slam_toolbox for 2-D occupancy map + odometry correction",
    )
    declare_slam3d = DeclareLaunchArgument(
        "slam3d",
        default_value="true",
        description="Run RTAB-Map + octomap_server for 3-D voxel map",
    )
    declare_use_sim_time = DeclareLaunchArgument(
        "use_sim_time",
        default_value="false",
        description="Use simulation clock (Gazebo / Isaac Sim)",
    )
    declare_slam_mode = DeclareLaunchArgument(
        "slam_mode",
        default_value="mapping",
        description="slam_toolbox mode: 'mapping' or 'localization'",
    )
    declare_map_file = DeclareLaunchArgument(
        "map_file",
        default_value="omnibot_map",
        description="Map file path for localization mode",
    )

    use_sim_time = LaunchConfiguration("use_sim_time")

    # ── 1. slam_toolbox (2-D) ─────────────────────────────────────────────────
    # Reads /scan + /odom + /tf → publishes /map + TF (map → odom)
    slam_toolbox = Node(
        package="slam_toolbox",
        executable="async_slam_toolbox_node",
        name="slam_toolbox",
        output="screen",
        condition=IfCondition(LaunchConfiguration("slam2d")),
        parameters=[
            slam_params,
            {
                "use_sim_time": use_sim_time,
                "mode": LaunchConfiguration("slam_mode"),
                "map_file_name": LaunchConfiguration("map_file"),
            },
        ],
    )

    # ── 2. RTAB-Map (3-D RGB-D SLAM) ─────────────────────────────────────────
    # Reads /camera/color + /camera/depth + /odom
    # Publishes /rtabmap/cloud_map (PointCloud2), /rtabmap/octomap_*,
    #           /rtabmap/grid_map (OccupancyGrid)
    rtabmap = Node(
        package="rtabmap_slam",
        executable="rtabmap",
        name="rtabmap",
        output="screen",
        condition=IfCondition(LaunchConfiguration("slam3d")),
        parameters=[
            rtabmap_params,
            {"use_sim_time": use_sim_time},
        ],
        remappings=[
            ("rgb/image", "/camera/color/image_raw"),
            ("rgb/camera_info", "/camera/color/camera_info"),
            ("depth/image", "/camera/depth/image_raw"),
            ("depth/camera_info", "/camera/depth/camera_info"),
            # Use EKF-fused odometry when available; falls back to raw /odom
            # if state_estimation.launch.py is not running.
            ("odom", "/odometry/filtered"),
            ("grid_map", "/rtabmap/grid_map"),
        ],
        arguments=["--delete_db_on_start"],
    )

    # ── 3. RTAB-Map visualizer (embedded) ────────────────────────────────────
    # Lightweight map-only viewer for the Pi; RViz is the primary viewer.
    # Disabled — enable manually with:
    #   ros2 run rtabmap_ros rtabmapviz
    # rtabmapviz = Node(...)

    # ── 4. OctoMap server ────────────────────────────────────────────────────
    # Reads /rtabmap/cloud_map → publishes voxel MarkerArray + projected 2-D map
    octomap_server = Node(
        package="octomap_server",
        executable="octomap_server_node",
        name="octomap_server",
        output="screen",
        condition=IfCondition(LaunchConfiguration("slam3d")),
        parameters=[
            octomap_params,
            {"use_sim_time": use_sim_time},
        ],
        remappings=[
            ("cloud_in", "/rtabmap/cloud_map"),
        ],
    )

    # ── 5. RViz ───────────────────────────────────────────────────────────────
    rviz = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        output="screen",
        condition=IfCondition(LaunchConfiguration("rviz")),
        arguments=["-d", rviz_config],
    )

    return LaunchDescription(
        [
            declare_rviz,
            declare_slam2d,
            declare_slam3d,
            declare_use_sim_time,
            declare_slam_mode,
            declare_map_file,
            slam_toolbox,
            rtabmap,
            octomap_server,
            rviz,
        ]
    )
