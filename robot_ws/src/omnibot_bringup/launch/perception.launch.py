#!/usr/bin/env python3
"""
perception.launch.py — Run on the Pi to start all cameras + BEV stitcher.

Starts:
  1. robot_state_publisher          (URDF → /robot_description + TF)
  2. usb_cam × 5                    (front / rear / left / right / wrist)
  3. astra_camera_node              (Orbbec Astra Pro — RGB + depth)
  4. depthimage_to_laserscan        (depth → /scan for SLAM)
  5. bev_stitcher_node              (4 base cams → /camera/base/bev/image_raw)
  6. foxglove_bridge                (ws://pi-ip:8765 — workstation browser viewer)
  7. rviz2                          (optional — default false; run viewer on workstation)

Run on Pi (all cameras active, no local RViz):
    ros2 launch omnibot_bringup perception.launch.py

Run with local RViz (Pi has a screen):
    ros2 launch omnibot_bringup perception.launch.py rviz:=true

Skip Astra Pro depth camera:
    ros2 launch omnibot_bringup perception.launch.py depth_camera:=false

Override camera device paths:
    ros2 launch omnibot_bringup perception.launch.py \\
        cam_front:=/dev/video0 cam_rear:=/dev/video2 \\
        cam_left:=/dev/video4 cam_right:=/dev/video6 \\
        cam_wrist:=/dev/video8

View outputs on workstation (separate terminal, matching ROS_DOMAIN_ID=30):
    ros2 launch omnibot_bringup perception_viewer.launch.py
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition, UnlessCondition
from launch.substitutions import Command, LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue


def generate_launch_description():
    pkg_desc = get_package_share_directory("omnibot_description")
    pkg_bringup = get_package_share_directory("omnibot_bringup")

    xacro_file = os.path.join(pkg_desc, "urdf", "omnibot.urdf.xacro")
    rviz_config = os.path.join(pkg_bringup, "config", "perception_full.rviz")

    robot_desc = ParameterValue(Command(["xacro ", xacro_file]), value_type=str)

    # ── Launch arguments ──────────────────────────────────────────────────────
    declare_rviz = DeclareLaunchArgument(
        "rviz", default_value="false",
        description="Launch RViz2 locally (set true if Pi has a display)",
    )
    declare_foxglove = DeclareLaunchArgument(
        "foxglove", default_value="true",
        description="Foxglove WebSocket bridge (ws://pi-ip:8765)",
    )
    declare_depth_camera = DeclareLaunchArgument(
        "depth_camera", default_value="true",
        description="Start Orbbec Astra Pro RGB-D driver",
    )
    declare_bev = DeclareLaunchArgument(
        "bev", default_value="true",
        description="Start BEV stitcher (requires all 4 base cameras)",
    )

    # Camera device paths — override for your hardware
    declare_cam_front = DeclareLaunchArgument(
        "cam_front", default_value="/dev/video0",
        description="Front base camera V4L2 device",
    )
    declare_cam_rear = DeclareLaunchArgument(
        "cam_rear", default_value="/dev/video2",
        description="Rear base camera V4L2 device",
    )
    declare_cam_left = DeclareLaunchArgument(
        "cam_left", default_value="/dev/video4",
        description="Left base camera V4L2 device",
    )
    declare_cam_right = DeclareLaunchArgument(
        "cam_right", default_value="/dev/video6",
        description="Right base camera V4L2 device",
    )
    declare_cam_wrist = DeclareLaunchArgument(
        "cam_wrist", default_value="/dev/video8",
        description="Wrist camera V4L2 device",
    )

    # ── 1. Robot State Publisher ──────────────────────────────────────────────
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[{"robot_description": robot_desc}],
    )

    # ── 2. Base cameras (front / rear / left / right) ─────────────────────────
    # Each usb_cam node publishes:
    #   /camera/<name>/image_raw          (sensor_msgs/Image)
    #   /camera/<name>/camera_info        (sensor_msgs/CameraInfo)
    _cam_params = {
        "framerate": 30.0,
        "image_width": 640,
        "image_height": 480,
        "pixel_format": "mjpeg2rgb",
        "io_method": "mmap",
    }

    cam_front = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_front",
        namespace="camera/front",
        output="screen",
        parameters=[{**_cam_params,
                     "video_device": LaunchConfiguration("cam_front"),
                     "camera_name": "front",
                     "camera_frame_id": "camera_front_optical_frame"}],
    )
    cam_rear = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_rear",
        namespace="camera/rear",
        output="screen",
        parameters=[{**_cam_params,
                     "video_device": LaunchConfiguration("cam_rear"),
                     "camera_name": "rear",
                     "camera_frame_id": "camera_rear_optical_frame"}],
    )
    cam_left = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_left",
        namespace="camera/left",
        output="screen",
        parameters=[{**_cam_params,
                     "video_device": LaunchConfiguration("cam_left"),
                     "camera_name": "left",
                     "camera_frame_id": "camera_left_optical_frame"}],
    )
    cam_right = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_right",
        namespace="camera/right",
        output="screen",
        parameters=[{**_cam_params,
                     "video_device": LaunchConfiguration("cam_right"),
                     "camera_name": "right",
                     "camera_frame_id": "camera_right_optical_frame"}],
    )

    # ── 3. Wrist camera ───────────────────────────────────────────────────────
    cam_wrist = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_wrist",
        namespace="camera/wrist",
        output="screen",
        parameters=[{**_cam_params,
                     "video_device": LaunchConfiguration("cam_wrist"),
                     "camera_name": "wrist",
                     "camera_frame_id": "camera_wrist_optical_frame"}],
    )

    # ── 4. Orbbec Astra Pro (RGB-D) ───────────────────────────────────────────
    # Publishes:
    #   /camera/color/image_raw      — 640×480 RGB @ 30 Hz
    #   /camera/color/camera_info
    #   /camera/depth/image_raw      — 640×480 16-bit depth in mm
    #   /camera/depth/camera_info
    #   /camera/depth/points         — PointCloud2
    astra_camera = Node(
        package="astra_camera",
        executable="astra_camera_node",
        name="astra_camera",
        namespace="camera",
        output="screen",
        condition=IfCondition(LaunchConfiguration("depth_camera")),
        parameters=[{
            "depth_registration": True,
            "camera_name": "camera",
            "color_width": 640,
            "color_height": 480,
            "color_fps": 30,
            "depth_width": 640,
            "depth_height": 480,
            "depth_fps": 30,
            "enable_point_cloud": True,
            "enable_colored_point_cloud": False,
        }],
    )

    # ── 5. depth → /scan (required by SLAM toolbox) ───────────────────────────
    depth_to_scan = Node(
        package="depthimage_to_laserscan",
        executable="depthimage_to_laserscan_node",
        name="depthimage_to_laserscan",
        output="screen",
        condition=IfCondition(LaunchConfiguration("depth_camera")),
        parameters=[{
            "scan_height": 1,
            "range_min": 0.3,
            "range_max": 8.0,
            "output_frame": "depth_camera_optical_frame",
        }],
        remappings=[
            ("depth", "/camera/depth/image_raw"),
            ("depth_camera_info", "/camera/depth/camera_info"),
            ("scan", "/scan"),
        ],
    )

    # ── 6. BEV stitcher ───────────────────────────────────────────────────────
    # Consumes /camera/{front,rear,left,right}/image_raw
    # Publishes /camera/base/bev/image_raw (800×800 RGB)
    bev_stitcher = Node(
        package="omnibot_lerobot",
        executable="bev_stitcher_node.py",
        name="bev_stitcher_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("bev")),
    )

    # ── 7. Foxglove bridge — browser / workstation viewer ─────────────────────
    # Open https://app.foxglove.dev → Connect → ws://<pi-ip>:8765
    foxglove_bridge = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        condition=IfCondition(LaunchConfiguration("foxglove")),
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
    )

    # ── 8. RViz (optional — prefer running on workstation) ────────────────────
    rviz = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        output="screen",
        condition=IfCondition(LaunchConfiguration("rviz")),
        arguments=["-d", rviz_config],
    )

    return LaunchDescription([
        declare_rviz,
        declare_foxglove,
        declare_depth_camera,
        declare_bev,
        declare_cam_front,
        declare_cam_rear,
        declare_cam_left,
        declare_cam_right,
        declare_cam_wrist,
        robot_state_publisher,
        cam_front,
        cam_rear,
        cam_left,
        cam_right,
        cam_wrist,
        astra_camera,
        depth_to_scan,
        bev_stitcher,
        foxglove_bridge,
        rviz,
    ])
