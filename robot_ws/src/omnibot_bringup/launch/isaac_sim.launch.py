"""
isaac_sim.launch.py
-------------------
ROS 2 launch file for OmniBot with NVIDIA Isaac Sim as the physics/rendering backend.

Prerequisites
-------------
1. Isaac Sim 4.x installed (NVIDIA GPU ≥ 24 GB VRAM for RTX rendering).
2. omni.isaac.ros2_bridge extension enabled in Isaac Sim.
3. Isaac Sim already running with the OmniBot USD loaded:

       cd <isaac_sim_root>
       ./python.sh omni.isaac.ros2_bridge/ros2_bridge.py

   OR via the Isaac Sim GUI:
       - Open robot_ws/src/omnibot_description/usd/omnibot.usd
       - Enable Window → Extensions → omni.isaac.ros2_bridge
       - Press Play

4. ROS 2 Jazzy sourced in the same terminal before running this launch.

Usage
-----
    ros2 launch omnibot_bringup isaac_sim.launch.py
    ros2 launch omnibot_bringup isaac_sim.launch.py rviz:=false
    ros2 launch omnibot_bringup isaac_sim.launch.py arm:=false

Difference from simulation.launch.py (Gazebo)
----------------------------------------------
- Does NOT start a simulator process (Isaac Sim must already be running).
- Does NOT use ros_gz_bridge; Isaac Sim's native bridge publishes all topics.
- robot_state_publisher + arm_driver_node are identical to the Gazebo launch.
- bev_stitcher_node is included here (required for smolvla_node) because Isaac
  Sim provides the 4 individual base cameras that the stitcher needs.
"""

import os

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, TimerAction
from launch.conditions import IfCondition
from launch.substitutions import Command, LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue
from launch_ros.substitutions import FindPackageShare


def generate_launch_description():
    pkg_desc = FindPackageShare("omnibot_description").find("omnibot_description")
    pkg_bringup = FindPackageShare("omnibot_bringup").find("omnibot_bringup")
    pkg_arm = FindPackageShare("omnibot_arm").find("omnibot_arm")
    pkg_lerobot = FindPackageShare("omnibot_lerobot").find("omnibot_lerobot")

    xacro_file = os.path.join(pkg_desc, "urdf", "omnibot.urdf.xacro")
    rviz_config = os.path.join(pkg_bringup, "config", "omnibot.rviz")
    arm_params = os.path.join(pkg_arm, "config", "arm_params.yaml")
    bev_params = os.path.join(pkg_lerobot, "config", "bev_params.yaml")

    robot_desc = ParameterValue(Command(["xacro ", xacro_file]), value_type=str)

    # ── Launch arguments ─────────────────────────────────────────────────────
    declare_rviz = DeclareLaunchArgument(
        "rviz", default_value="true", description="Launch RViz2 for visualization"
    )
    declare_arm = DeclareLaunchArgument(
        "arm",
        default_value="true",
        description="Launch arm_driver_node (set false for base-only tests)",
    )
    declare_bev = DeclareLaunchArgument(
        "bev",
        default_value="true",
        description="Launch bev_stitcher_node (required for smolvla_node)",
    )
    declare_foxglove = DeclareLaunchArgument(
        "foxglove",
        default_value="true",
        description="Launch Foxglove bridge on ws://localhost:8765",
    )

    # ── 1. Robot State Publisher ─────────────────────────────────────────────
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[{"robot_description": robot_desc}],
    )

    # ── 2. Arm driver — simulation passthrough mode (4 s delay) ─────────────
    # arm_driver_node subscribes to /arm/joint_commands/out (routed by
    # arm_cmd_mux when omnibot_rl is active, or directly from SmolVLA when not).
    # When omnibot_rl is NOT launched, /arm/joint_commands/out will not be
    # published; remap /arm/joint_commands → /arm/joint_commands/out to
    # allow direct SmolVLA → arm_driver pass-through without arm_cmd_mux.
    arm_driver = TimerAction(
        period=4.0,
        actions=[
            Node(
                package="omnibot_arm",
                executable="arm_driver_node.py",
                name="arm_driver_node",
                output="screen",
                condition=IfCondition(LaunchConfiguration("arm")),
                parameters=[arm_params],
                remappings=[
                    ("/arm/joint_states", "/joint_states"),
                    # Allow SmolVLA to reach arm_driver directly in Isaac Sim
                    # testing without launching the full omnibot_rl stack.
                    # When arm_cmd_mux IS running it publishes /arm/joint_commands/out
                    # and this remap is overridden by the topic being present.
                    ("/arm/joint_commands/out", "/arm/joint_commands"),
                ],
            )
        ],
    )

    # ── 3. BEV stitcher ──────────────────────────────────────────────────────
    bev_stitcher = Node(
        package="omnibot_lerobot",
        executable="bev_stitcher_node.py",
        name="bev_stitcher_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("bev")),
        parameters=[bev_params],
    )

    # ── 4. depthimage_to_laserscan — virtual /scan from Isaac Sim depth ──────
    # Isaac Sim publishes /camera/depth/image_raw via omni.isaac.ros2_bridge.
    # slam_toolbox needs /scan; this converts depth → LaserScan identically
    # to the Gazebo and hardware pipelines.
    depth_to_scan = Node(
        package="depthimage_to_laserscan",
        executable="depthimage_to_laserscan_node",
        name="depthimage_to_laserscan",
        output="screen",
        parameters=[
            {
                "scan_height": 1,
                "range_min": 0.6,
                "range_max": 8.0,
                "output_frame": "depth_camera_link",  # MUST be x-forward frame, not optical (z-fwd) — scan was rotated 90°
            }
        ],
        remappings=[
            ("depth", "/camera/depth/image_raw"),
            ("depth_camera_info", "/camera/depth/camera_info"),
            ("scan", "/scan"),
        ],
    )

    # ── 5. Foxglove bridge — browser-based live monitoring ───────────────────
    # Open https://app.foxglove.dev → Connect → ws://localhost:8765
    foxglove_bridge = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
        condition=IfCondition(LaunchConfiguration("foxglove")),
    )

    # ── 6. RViz ──────────────────────────────────────────────────────────────
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
            declare_arm,
            declare_bev,
            declare_foxglove,
            robot_state_publisher,
            arm_driver,
            bev_stitcher,
            depth_to_scan,
            foxglove_bridge,
            rviz,
        ]
    )
