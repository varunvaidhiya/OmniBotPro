"""
sim_pc.launch.py
----------------
ROS 2 launch file for the simulation workstation (PC2) in multi-workstation
deployment mode.

What this starts
----------------
  bev_stitcher_node  Stitches 4 base-mounted camera feeds into
                     /camera/base/bev/image_raw at 30 Hz.
                     Required by smolvla_node running on PC1.
  RViz               Optional visualisation (disable with rviz:=false).

What this does NOT start
------------------------
  Isaac Sim   Start manually or via data_engine/isaac_sim/collect_episodes.py.
  Gazebo      Use simulation.launch.py for Gazebo-only runs.
  VLA nodes   Those run on PC1 via vla_desktop.launch.py.

Prerequisites
-------------
  1. deployment.env configured for multi mode:  python deploy.py --mode multi
  2. Isaac Sim running with omni.isaac.ros2_bridge enabled, OR
     Gazebo running via:  ros2 launch omnibot_bringup simulation.launch.py
  3. ROS_DOMAIN_ID=30 (default) set identically on all three machines.
  4. ROS_STATIC_PEERS set in the calling shell (handled by launch_sim_pc.sh).

Usage
-----
  Invoked by launch_sim_pc.sh automatically.
  Direct use:
    ros2 launch omnibot_bringup sim_pc.launch.py
    ros2 launch omnibot_bringup sim_pc.launch.py rviz:=false
"""

import os

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.substitutions import FindPackageShare


def generate_launch_description():
    pkg_bringup = FindPackageShare("omnibot_bringup").find("omnibot_bringup")
    pkg_lerobot = FindPackageShare("omnibot_lerobot").find("omnibot_lerobot")

    rviz_config = os.path.join(pkg_bringup, "config", "omnibot.rviz")
    bev_params = os.path.join(pkg_lerobot, "config", "bev_params.yaml")

    # ── Launch arguments ─────────────────────────────────────────────────────
    declare_rviz = DeclareLaunchArgument(
        "rviz",
        default_value="true",
        description="Launch RViz2 for visualisation",
    )

    # ── BEV stitcher ─────────────────────────────────────────────────────────
    # Subscribes to /camera/{front,rear,left,right}/image_raw published by
    # Isaac Sim or Gazebo on this machine and stitches them into a single
    # 800×800 bird's-eye-view image.  smolvla_node on PC1 subscribes to the
    # output topic /camera/base/bev/image_raw over DDS.
    bev_stitcher = Node(
        package="omnibot_lerobot",
        executable="bev_stitcher_node.py",
        name="bev_stitcher_node",
        output="screen",
        parameters=[bev_params],
    )

    # ── RViz ─────────────────────────────────────────────────────────────────
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
            bev_stitcher,
            rviz,
        ]
    )
