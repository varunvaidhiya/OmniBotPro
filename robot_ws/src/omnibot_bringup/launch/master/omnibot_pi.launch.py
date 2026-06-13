#!/usr/bin/env python3
"""
omnibot_pi.launch.py — Master launch for the Raspberry Pi 5.

Run this ONCE on the Pi to bring up the complete robot stack.
Run omnibot_workstation.launch.py on the GPU PC at the same time.

What this starts (in order):
  ┌─ HARDWARE ──────────────────────────────────────────────────────────────┐
  │  robot_state_publisher   URDF → /robot_description + static TF          │
  │  yahboom_controller_node Yahboom serial → /odom, /imu/data, motors      │
  │  arm_driver_node         SO-101 6-DOF arm via Feetech bus                │
  ├─ PERCEPTION ────────────────────────────────────────────────────────────┤
  │  usb_cam × 5             front/rear/left/right/wrist cameras             │
  │  astra_camera_node       Orbbec Astra Pro — RGB + depth (OpenNI2)        │
  │  depthimage_to_laserscan depth → /scan for SLAM                          │
  │  bev_stitcher_node       4 base cams → /camera/base/bev/image_raw        │
  ├─ STATE ESTIMATION ──────────────────────────────────────────────────────┤
  │  ekf_node                EKF fuses /odom + /imu → /odometry/filtered     │
  ├─ MAPPING ───────────────────────────────────────────────────────────────┤
  │  slam_toolbox            2-D LiDAR SLAM → /map + TF (map→odom)           │
  │  rtabmap                 RGB-D SLAM → /rtabmap/cloud_map                  │
  │  octomap_server          3-D voxel map → /octomap_*                      │
  ├─ NAVIGATION ────────────────────────────────────────────────────────────┤
  │  Nav2 stack              controller, planner, behavior, bt_navigator      │
  │                          velocity_smoother, lifecycle_manager             │
  ├─ CONTROL ROUTING ───────────────────────────────────────────────────────┤
  │  cmd_vel_mux             routes nav2/vla/teleop/rl_nav → motors           │
  │  arm_cmd_mux             routes smolvla/rl_arm → arm driver               │
  │  mission_planner         parses /mission/command → Nav2 + VLA goals       │
  ├─ CONNECTIVITY ──────────────────────────────────────────────────────────┤
  │  foxglove_bridge         ws://pi-ip:8765  (workstation Foxglove viewer)   │
  │  web_video_server        http://pi-ip:8080 (browser MJPEG streams)        │
  │  rosbridge_websocket     ws://pi-ip:9090  (Android app)                   │
  └─────────────────────────────────────────────────────────────────────────┘

Usage:
    ros2 launch omnibot_bringup master/omnibot_pi.launch.py

Skip depth camera (save ~70% CPU):
    ros2 launch omnibot_bringup master/omnibot_pi.launch.py depth_camera:=false

Skip SLAM (cameras + drive only):
    ros2 launch omnibot_bringup master/omnibot_pi.launch.py slam:=false

Skip Nav2 (drive + SLAM, no autonomous navigation):
    ros2 launch omnibot_bringup master/omnibot_pi.launch.py nav2:=false

Headless (no foxglove/web_video):
    ros2 launch omnibot_bringup master/omnibot_pi.launch.py \
        foxglove:=false web_video:=false

Pair with workstation:
    ros2 launch omnibot_bringup master/omnibot_workstation.launch.py  ← GPU PC
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import (
    DeclareLaunchArgument,
    IncludeLaunchDescription,
    TimerAction,
)
from launch.conditions import IfCondition
from launch.launch_description_sources import (
    AnyLaunchDescriptionSource,
    PythonLaunchDescriptionSource,
)
from launch.substitutions import Command, LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue


def generate_launch_description():

    # ── Package directories ───────────────────────────────────────────────────
    pkg_desc = get_package_share_directory("omnibot_description")
    pkg_nav = get_package_share_directory("omnibot_navigation")

    xacro_file = os.path.join(pkg_desc, "urdf", "omnibot.urdf.xacro")
    slam_params = os.path.join(pkg_nav, "config", "slam_toolbox_params.yaml")
    rtabmap_params = os.path.join(pkg_nav, "config", "rtabmap_params.yaml")
    octomap_params = os.path.join(pkg_nav, "config", "octomap_params.yaml")
    ekf_params = os.path.join(pkg_nav, "config", "robot_localization.yaml")

    robot_desc = ParameterValue(Command(["xacro ", xacro_file]), value_type=str)

    # ── Launch arguments ──────────────────────────────────────────────────────
    declare_depth_camera = DeclareLaunchArgument(
        "depth_camera",
        default_value="true",
        description="Start Orbbec Astra Pro RGB-D driver (~70% CPU)",
    )
    declare_slam = DeclareLaunchArgument(
        "slam",
        default_value="true",
        description="Start SLAM stack (slam_toolbox + rtabmap + octomap)",
    )
    declare_nav2 = DeclareLaunchArgument(
        "nav2",
        default_value="true",
        description="Start Nav2 autonomous navigation stack",
    )
    declare_arm = DeclareLaunchArgument(
        "arm", default_value="true", description="Start SO-101 arm driver"
    )
    declare_foxglove = DeclareLaunchArgument(
        "foxglove",
        default_value="true",
        description="Foxglove WebSocket bridge (ws://pi-ip:8765)",
    )
    declare_web_video = DeclareLaunchArgument(
        "web_video",
        default_value="true",
        description="web_video_server MJPEG streams (http://pi-ip:8080)",
    )
    declare_rosbridge = DeclareLaunchArgument(
        "rosbridge",
        default_value="true",
        description="ROSBridge WebSocket for Android app (ws://pi-ip:9090)",
    )

    # Camera device paths
    declare_cam_front = DeclareLaunchArgument("cam_front", default_value="/dev/video2")
    declare_cam_rear = DeclareLaunchArgument("cam_rear", default_value="/dev/video10")
    declare_cam_left = DeclareLaunchArgument("cam_left", default_value="/dev/video6")
    declare_cam_right = DeclareLaunchArgument("cam_right", default_value="/dev/video4")
    declare_cam_wrist = DeclareLaunchArgument("cam_wrist", default_value="/dev/video0")

    # ── 1. Robot State Publisher ──────────────────────────────────────────────
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[{"robot_description": robot_desc}],
    )

    # ── 2. Yahboom motor driver ───────────────────────────────────────────────
    # Publishes /odom, /imu/data, /joint_states; subscribes /cmd_vel/out
    yahboom_controller = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_controller_node",
        output="screen",
        parameters=[
            {
                "serial_port": "/dev/ttyUSB0",
                "baud_rate": 115200,
                "wheel_radius": 0.04,
                "wheel_separation_width": 0.215,
                "wheel_separation_length": 0.165,
                # EKF owns odom->base_link TF (double-broadcast causes jitter)
                "publish_tf": False,
                # Android app subscribes /diagnostics — publish driver health
                "publish_diagnostics": True,
            }
        ],
        remappings=[("cmd_vel", "/cmd_vel/out")],
    )

    # -- AI perception: object distance + pose from depth camera ------------
    object_perception = Node(
        package="omnibot_perception",
        executable="object_perception_node",
        name="object_perception_node",
        output="screen",
    )

    # -- Remote rosbag recorder (Android app record button) -----------------
    rosbag_recorder = Node(
        package="omnibot_hybrid",
        executable="rosbag_recorder",
        name="rosbag_recorder",
        output="screen",
    )

    # ── 3. SO-101 Arm driver ──────────────────────────────────────────────────
    arm_driver = Node(
        package="omnibot_arm",
        executable="arm_driver_node.py",
        name="arm_driver_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("arm")),
        parameters=[
            {
                "follower_port": "/dev/ttyACM0",
                "baudrate": 1000000,
                "publish_rate": 100.0,
            }
        ],
        remappings=[("joint_commands", "/arm/joint_commands/out")],
    )

    # ── 4. Cameras ────────────────────────────────────────────────────────────
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
        parameters=[
            {
                **_cam_params,
                "video_device": LaunchConfiguration("cam_front"),
                "camera_name": "front",
                "camera_frame_id": "camera_front_optical_frame",
            }
        ],
    )
    cam_rear = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_rear",
        namespace="camera/rear",
        output="screen",
        parameters=[
            {
                **_cam_params,
                "video_device": LaunchConfiguration("cam_rear"),
                "camera_name": "rear",
                "camera_frame_id": "camera_rear_optical_frame",
            }
        ],
    )
    cam_left = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_left",
        namespace="camera/left",
        output="screen",
        parameters=[
            {
                **_cam_params,
                "video_device": LaunchConfiguration("cam_left"),
                "camera_name": "left",
                "camera_frame_id": "camera_left_optical_frame",
            }
        ],
    )
    cam_right = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_right",
        namespace="camera/right",
        output="screen",
        parameters=[
            {
                **_cam_params,
                "video_device": LaunchConfiguration("cam_right"),
                "camera_name": "right",
                "camera_frame_id": "camera_right_optical_frame",
            }
        ],
    )
    cam_wrist = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="usb_cam_wrist",
        namespace="camera/wrist",
        output="screen",
        parameters=[
            {
                **_cam_params,
                "video_device": LaunchConfiguration("cam_wrist"),
                "camera_name": "wrist",
                "camera_frame_id": "camera_wrist_optical_frame",
            }
        ],
    )

    # ── 5. Orbbec Astra Pro RGB-D ─────────────────────────────────────────────
    astra_camera = IncludeLaunchDescription(
        AnyLaunchDescriptionSource(
            os.path.join(
                get_package_share_directory("astra_camera"),
                "launch",
                "astra_pro.launch.xml",
            )
        ),
        launch_arguments={
            "camera_name": "camera",
            "depth_registration": "true",
            "enable_point_cloud": "true",
            "enable_colored_point_cloud": "false",
            "color_depth_synchronization": "false",
            "oni_log_level": "none",
        }.items(),
        condition=IfCondition(LaunchConfiguration("depth_camera")),
    )

    # ── 6. depth → /scan ─────────────────────────────────────────────────────
    depth_to_scan = Node(
        package="depthimage_to_laserscan",
        executable="depthimage_to_laserscan_node",
        name="depthimage_to_laserscan",
        output="screen",
        condition=IfCondition(LaunchConfiguration("depth_camera")),
        parameters=[
            {
                "scan_height": 1,
                "range_min": 0.3,
                "range_max": 8.0,
                "output_frame": "depth_camera_optical_frame",
            }
        ],
        remappings=[
            ("depth", "/camera/depth/image_raw"),
            ("depth_camera_info", "/camera/depth/camera_info"),
            ("scan", "/scan"),
        ],
    )

    # ── 7. BEV stitcher ───────────────────────────────────────────────────────
    bev_stitcher = Node(
        package="omnibot_lerobot",
        executable="bev_stitcher_node.py",
        name="bev_stitcher_node",
        output="screen",
    )

    # ── 8. EKF state estimation ───────────────────────────────────────────────
    ekf = Node(
        package="robot_localization",
        executable="ekf_node",
        name="ekf_filter_node",
        output="screen",
        parameters=[ekf_params],
        remappings=[("odometry/filtered", "/odometry/filtered")],
    )

    # ── 9. SLAM toolbox (2-D) ─────────────────────────────────────────────────
    slam_toolbox = Node(
        package="slam_toolbox",
        executable="async_slam_toolbox_node",
        name="slam_toolbox",
        output="screen",
        condition=IfCondition(LaunchConfiguration("slam")),
        parameters=[slam_params, {"use_sim_time": False}],
    )

    # ── 10. RTAB-Map (3-D RGB-D SLAM) ────────────────────────────────────────
    rtabmap = Node(
        package="rtabmap_slam",
        executable="rtabmap",
        name="rtabmap",
        output="screen",
        condition=IfCondition(LaunchConfiguration("slam")),
        parameters=[rtabmap_params, {"use_sim_time": False}],
        remappings=[
            ("rgb/image", "/camera/color/image_raw"),
            ("rgb/camera_info", "/camera/color/camera_info"),
            ("depth/image", "/camera/depth/image_raw"),
            ("depth/camera_info", "/camera/depth/camera_info"),
            ("odom", "/odometry/filtered"),
            ("grid_map", "/rtabmap/grid_map"),
        ],
        arguments=["--delete_db_on_start"],
    )

    # ── 11. OctoMap server ────────────────────────────────────────────────────
    octomap_server = Node(
        package="octomap_server",
        executable="octomap_server_node",
        name="octomap_server",
        output="screen",
        condition=IfCondition(LaunchConfiguration("slam")),
        parameters=[octomap_params, {"use_sim_time": False}],
        remappings=[("cloud_in", "/rtabmap/cloud_map")],
    )

    # ── 12. Nav2 stack ────────────────────────────────────────────────────────
    nav2 = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_nav, "launch", "autonomous_navigation.launch.py")
        ),
        condition=IfCondition(LaunchConfiguration("nav2")),
    )

    # ── 13. cmd_vel mux ───────────────────────────────────────────────────────
    # Routes: nav2 | vla | teleop | rl_nav → /cmd_vel/out → yahboom_controller
    cmd_vel_mux = Node(
        package="omnibot_hybrid",
        executable="cmd_vel_mux",
        name="cmd_vel_mux",
        output="screen",
        parameters=[{"default_mode": "nav2"}],
    )

    # ── 14. arm_cmd_mux ───────────────────────────────────────────────────────
    # Routes: smolvla | rl_arm → /arm/joint_commands/out → arm_driver
    arm_cmd_mux = Node(
        package="omnibot_rl",
        executable="arm_cmd_mux",
        name="arm_cmd_mux",
        output="screen",
        condition=IfCondition(LaunchConfiguration("arm")),
    )

    # ── 15. Mission planner ───────────────────────────────────────────────────
    # Parses /mission/command → Nav2 + VLA + RL goals
    mission_planner = Node(
        package="omnibot_hybrid",
        executable="mission_planner",
        name="mission_planner",
        output="screen",
    )

    # ── 16. Foxglove bridge ───────────────────────────────────────────────────
    foxglove_bridge = TimerAction(
        period=4.0,
        actions=[
            Node(
                package="foxglove_bridge",
                executable="foxglove_bridge",
                name="foxglove_bridge",
                output="screen",
                condition=IfCondition(LaunchConfiguration("foxglove")),
                parameters=[{"port": 8765, "address": "0.0.0.0"}],
            )
        ],
    )

    # ── 17. web_video_server ──────────────────────────────────────────────────
    web_video_server = TimerAction(
        period=4.0,
        actions=[
            Node(
                package="web_video_server",
                executable="web_video_server",
                name="web_video_server",
                output="screen",
                condition=IfCondition(LaunchConfiguration("web_video")),
                parameters=[{"port": 8080, "address": "0.0.0.0"}],
            )
        ],
    )

    # ── 18. ROSBridge WebSocket (Android app) ─────────────────────────────────
    rosbridge = TimerAction(
        period=4.0,
        actions=[
            Node(
                package="rosbridge_server",
                executable="rosbridge_websocket",
                name="rosbridge_websocket",
                output="screen",
                condition=IfCondition(LaunchConfiguration("rosbridge")),
                parameters=[{"port": 9090}],
            )
        ],
    )

    # ── Assemble ──────────────────────────────────────────────────────────────
    return LaunchDescription(
        [
            # Arguments
            declare_depth_camera,
            declare_slam,
            declare_nav2,
            declare_arm,
            declare_foxglove,
            declare_web_video,
            declare_rosbridge,
            declare_cam_front,
            declare_cam_rear,
            declare_cam_left,
            declare_cam_right,
            declare_cam_wrist,
            # Hardware
            robot_state_publisher,
            yahboom_controller,
            arm_driver,
            # Perception
            cam_front,
            cam_rear,
            cam_left,
            cam_right,
            cam_wrist,
            astra_camera,
            depth_to_scan,
            bev_stitcher,
            # State estimation
            ekf,
            object_perception,
            rosbag_recorder,
            # Mapping
            slam_toolbox,
            rtabmap,
            octomap_server,
            # Navigation
            nav2,
            # Control routing
            cmd_vel_mux,
            arm_cmd_mux,
            mission_planner,
            # Connectivity (delayed to let DDS settle)
            foxglove_bridge,
            web_video_server,
            rosbridge,
        ]
    )
