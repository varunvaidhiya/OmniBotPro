from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, IncludeLaunchDescription, TimerAction
from launch.conditions import IfCondition
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import Command, LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue
from launch_ros.substitutions import FindPackageShare
import os


def generate_launch_description():
    pkg_ros_gz_sim = FindPackageShare(package="ros_gz_sim").find("ros_gz_sim")
    pkg_omnibot_description = FindPackageShare(package="omnibot_description").find(
        "omnibot_description"
    )
    pkg_omnibot_bringup = FindPackageShare(package="omnibot_bringup").find(
        "omnibot_bringup"
    )
    pkg_omnibot_arm = FindPackageShare(package="omnibot_arm").find("omnibot_arm")

    xacro_file = os.path.join(pkg_omnibot_description, "urdf", "omnibot.urdf.xacro")
    rviz_config_path = os.path.join(pkg_omnibot_bringup, "config", "omnibot.rviz")
    bridge_config = os.path.join(pkg_omnibot_bringup, "config", "ros_gz_bridge.yaml")
    world_file = os.path.join(pkg_omnibot_bringup, "worlds", "omnibot_world.sdf")
    arm_params = os.path.join(pkg_omnibot_arm, "config", "arm_params.yaml")

    robot_desc = ParameterValue(Command(["xacro ", xacro_file]), value_type=str)

    # ── Launch arguments ──────────────────────────────────────────────────────
    rviz_arg = DeclareLaunchArgument(
        "rviz",
        default_value="true",
        description="Launch RViz2 (set false for headless / CI)",
    )

    bev_arg = DeclareLaunchArgument(
        "bev",
        default_value="true",
        description="Launch BEV stitcher (requires 4 base cameras in Gazebo)",
    )

    rosbridge_arg = DeclareLaunchArgument(
        "rosbridge",
        default_value="true",
        description="Launch ROSBridge on port 9090 (needed for Android app)",
    )

    foxglove_arg = DeclareLaunchArgument(
        "foxglove",
        default_value="true",
        description="Launch Foxglove bridge on ws://localhost:8765",
    )

    world_arg = DeclareLaunchArgument(
        "world", default_value=world_file, description="Path to Gazebo world SDF"
    )

    # ── 1. Gazebo Sim (-r = run unpaused) ────────────────────────────────────
    gazebo = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_ros_gz_sim, "launch", "gz_sim.launch.py")
        ),
        launch_arguments={"gz_args": ["-r ", LaunchConfiguration("world")]}.items(),
    )

    # ── 2. Robot State Publisher ──────────────────────────────────────────────
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[{"robot_description": robot_desc, "use_sim_time": True}],
    )

    # ── 3. Spawn robot — wait 3 s for RSP to publish /robot_description ──────
    spawn_entity = TimerAction(
        period=3.0,
        actions=[
            Node(
                package="ros_gz_sim",
                executable="create",
                arguments=[
                    "-name",
                    "mecanum_bot",
                    "-topic",
                    "/robot_description",
                    "-z",
                    "0.1",
                ],
                output="screen",
            )
        ],
    )

    # ── 4. ROS ↔ GZ bridge (/cmd_vel, /odom, /tf, /imu, /camera/*) ──────────
    bridge = Node(
        package="ros_gz_bridge",
        executable="parameter_bridge",
        parameters=[{"config_file": bridge_config}],
        output="screen",
    )

    # ── 5. Arm driver — simulation passthrough mode ───────────────────────────
    # NO remapping: arm_driver publishes to /arm/joint_states so the Android app
    # can subscribe to that topic directly via ROSBridge.  A relay node below
    # forwards /arm/joint_states → /joint_states so robot_state_publisher also
    # receives arm poses for the full TF tree.
    arm_driver = TimerAction(
        period=4.0,
        actions=[
            Node(
                package="omnibot_arm",
                executable="arm_driver_node.py",
                name="arm_driver_node",
                output="screen",
                parameters=[arm_params],
            )
        ],
    )

    # Relay arm joint states into the shared /joint_states topic so
    # robot_state_publisher can update arm-link TF alongside wheel joints.
    arm_joint_state_relay = Node(
        package="topic_tools",
        executable="relay",
        name="arm_joint_state_relay",
        output="screen",
        arguments=["/arm/joint_states", "/joint_states"],
        parameters=[{"use_sim_time": True}],
    )

    # ── 6. depthimage_to_laserscan — virtual /scan from Astra Pro depth image ──
    # OmniBot has NO 2D lidar.  slam_toolbox and Nav2 expect /scan (LaserScan).
    # This node converts /camera/depth/image_raw → /scan at the same height
    # slice as the Astra Pro, matching real hardware behaviour where
    # nav2_params.yaml drives slam_toolbox from the depth camera.
    # scan_height=1 takes the single middle row; range_min/max match Astra Pro.
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
                "use_sim_time": True,
            }
        ],
        remappings=[
            ("depth", "/camera/depth/image_raw"),
            ("depth_camera_info", "/camera/depth/camera_info"),
            ("scan", "/scan"),
        ],
    )

    # ── 8. BEV stitcher — composites 4 base cameras into /camera/base/bev ────
    # Required by smolvla_node; safe to disable with bev:=false for quick tests.
    bev_stitcher = TimerAction(
        period=5.0,
        actions=[
            Node(
                # omnibot_lerobot stitcher: geometric-IPM fused BEV, publishes
                # /camera/base/bev/image_raw (required by smolvla_node).
                # (ros2_bev_stitcher defaulted to /camera/bev/image_raw,
                # which nothing subscribes to.)
                package="omnibot_lerobot",
                executable="bev_stitcher_node",
                name="bev_stitcher_node",
                output="screen",
                condition=IfCondition(LaunchConfiguration("bev")),
            )
        ],
    )

    # ── 9a. web_video_server — MJPEG stream for Android CameraView ──────────────
    # Android MjpegView connects to http://<ip>:8080/stream?topic=/camera/image_raw
    # This node must be running for the camera feed to appear in the Android app.
    web_video_server = Node(
        package="web_video_server",
        executable="web_video_server",
        name="web_video_server",
        output="screen",
        parameters=[{"port": 8080, "use_sim_time": True}],
        condition=IfCondition(LaunchConfiguration("rosbridge")),
    )

    # ── 9. ROSBridge — WebSocket on port 9090 (Android app + web clients) ────
    rosbridge = Node(
        package="rosbridge_server",
        executable="rosbridge_websocket",
        name="rosbridge_websocket",
        output="screen",
        parameters=[{"port": 9090}],
        condition=IfCondition(LaunchConfiguration("rosbridge")),
    )

    # ── 10. Foxglove bridge — WebSocket on port 8765 (browser visualisation) ──
    # Open https://app.foxglove.dev → Connect → ws://localhost:8765
    foxglove_bridge = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
        condition=IfCondition(LaunchConfiguration("foxglove")),
    )

    # ── 11. RViz ───────────────────────────────────────────────────────────────
    rviz = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        output="screen",
        arguments=["-d", rviz_config_path],
        condition=IfCondition(LaunchConfiguration("rviz")),
    )

    return LaunchDescription(
        [
            rviz_arg,
            bev_arg,
            rosbridge_arg,
            foxglove_arg,
            world_arg,
            gazebo,
            robot_state_publisher,
            spawn_entity,
            bridge,
            arm_driver,
            arm_joint_state_relay,
            depth_to_scan,
            bev_stitcher,
            web_video_server,
            rosbridge,
            foxglove_bridge,
            rviz,
        ]
    )
