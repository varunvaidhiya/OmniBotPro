#!/usr/bin/env python3
"""
Hybrid Robot Launch — Nav2 + VLA with cmd_vel mux and mission planner.

┌──────────────────────────────────────────────────────────────────────┐
│  MISSION LAYER                                                        │
│  /mission/command  →  mission_planner  →  /control_mode + /vla/prompt│
└──────────────────────────────────────┬───────────────────────────────┘
                                       │
          ┌────────────────────────────▼────────────────────────────┐
          │                     cmd_vel_mux                          │
          │  /cmd_vel       ← Nav2 (native output)                  │
          │  /cmd_vel/vla   ← VLA inference node                    │
          │  /cmd_vel/teleop← keyboard / joystick                   │
          │                     selects by /control_mode            │
          │                   → /cmd_vel/out                        │
          └────────────────────────────┬────────────────────────────┘
                                       │
          ┌────────────────────────────▼────────────────────────────┐
          │  yahboom_driver  (remapped: /cmd_vel ← /cmd_vel/out)    │
          └─────────────────────────────────────────────────────────┘

Launching this file starts the complete hybrid system:
  • Robot driver        (RPi 5 — Yahboom board)
  • Robot state publisher + EKF localization
  • SLAM Toolbox        (live mapping)
  • Nav2 autonomy       (path planning + obstacle avoidance)
  • VLA inference node  (Desktop GPU — semantic tasks)
  • cmd_vel_mux         (mode-based multiplexer)
  • mission_planner     (high-level orchestrator)
  • RViz                (optional visualisation)

Quick-start after launch
────────────────────────
  # Full hybrid mission
  ros2 topic pub --once /mission/command std_msgs/msg/String \\
    "data: 'navigate:kitchen,vla:find the red cup'"

  # Force a specific mode
  ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'vla'"
  ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'nav2'"
  ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'teleop'"

  # Cancel running mission
  ros2 topic pub --once /mission/cancel std_msgs/msg/String "data: 'cancel'"

  # Watch status
  ros2 topic echo /mission/status
  ros2 topic echo /control_mode/active
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, IncludeLaunchDescription
from launch.conditions import IfCondition
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import Command, LaunchConfiguration, PythonExpression
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue

# RL inference nodes are included when use_rl:=true.
# arm_driver reads /arm/joint_commands/out (routed via arm_cmd_mux)
# instead of /arm/joint_commands so the RL arm policy can be interleaved.


def generate_launch_description():
    pkg_navigation = get_package_share_directory("omnibot_navigation")
    pkg_description = get_package_share_directory("omnibot_description")
    pkg_rl = get_package_share_directory("omnibot_rl")
    pkg_lerobot = get_package_share_directory("omnibot_lerobot")
    pkg_orchestration = get_package_share_directory("omnibot_orchestration")

    xacro_file = os.path.join(pkg_description, "urdf", "omnibot.urdf.xacro")
    robot_description = ParameterValue(Command(["xacro ", xacro_file]), value_type=str)

    # ── Launch arguments ──────────────────────────────────────────────────────
    use_sim_time = LaunchConfiguration("use_sim_time", default="false")
    use_rviz = LaunchConfiguration("use_rviz", default="true")
    use_slam = LaunchConfiguration("use_slam", default="true")
    vla_device = LaunchConfiguration("vla_device", default="cuda")
    vla_4bit = LaunchConfiguration("vla_4bit", default="false")
    vla_image_topic = LaunchConfiguration(
        "vla_image_topic", default="/camera/front/image_raw"
    )
    use_rosbridge = LaunchConfiguration("use_rosbridge", default="true")
    use_ota = LaunchConfiguration("use_ota", default="false")
    ota_manifest_url = LaunchConfiguration("ota_manifest_url", default="")
    use_foxglove = LaunchConfiguration("use_foxglove", default="true")
    use_bev = LaunchConfiguration("use_bev", default="true")
    use_rl = LaunchConfiguration("use_rl", default="false")
    use_policy = LaunchConfiguration("use_policy", default="false")
    policy_model_type = LaunchConfiguration("policy_model_type", default="smolvla")
    policy_checkpoint = LaunchConfiguration(
        "policy_checkpoint", default="lerobot/smolvla_base"
    )
    use_langchain = LaunchConfiguration("use_langchain", default="false")

    # ── Robot driver ──────────────────────────────────────────────────────────
    # Remapped: driver reads /cmd_vel/out (mux output) instead of /cmd_vel.
    # This keeps Nav2 writing to /cmd_vel natively, while the mux selects
    # the correct source and forwards it to /cmd_vel/out.
    driver_node = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        parameters=[
            {
                "serial_port": "/dev/ttyUSB0",
                "baud_rate": 115200,
                "wheel_separation_length": 0.165,
                "wheel_separation_width": 0.215,
                "wheel_radius": 0.04,
                "use_sim_time": use_sim_time,
                # EKF owns odom->base_link TF -- never run two broadcasters
                # on the same transform (causes TF jitter -> SLAM/Nav2 drift).
                "publish_tf": False,
                # Android app subscribes /diagnostics — publish driver health
                "publish_diagnostics": True,
            }
        ],
        remappings=[("/cmd_vel", "/cmd_vel/out")],
    )

    # -- Remote rosbag recorder (Android app record button) -----------------
    rosbag_recorder_node = Node(
        package="omnibot_hybrid",
        executable="rosbag_recorder",
        name="rosbag_recorder",
        output="screen",
    )

    # ── Arm Command Mux (always required) ────────────────────────────────────
    # Bridges /arm/joint_commands (SmolVLA / Android) → /arm/joint_commands/out
    # → arm_driver_node. Also routes /arm/joint_commands/rl from rl_arm_node
    # when use_rl:=true. Started unconditionally so the arm works in every mode.
    arm_cmd_mux_node = Node(
        package="omnibot_rl",
        executable="arm_cmd_mux",
        name="arm_cmd_mux",
        output="screen",
        parameters=[
            os.path.join(pkg_rl, "config", "rl_arm_params.yaml"),
            {"use_sim_time": use_sim_time},
        ],
    )

    # ── RL Inference nodes (optional, use_rl:=true) ───────────────────────────
    # Includes: rl_nav_node, rl_arm_node, rl_object_pose_node.
    # arm_cmd_mux is already started above, so pass include_arm_mux:=false to
    # avoid duplicate nodes.
    rl_inference = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_rl, "launch", "rl_inference.launch.py")
        ),
        condition=IfCondition(use_rl),
        launch_arguments={
            "use_sim_time": use_sim_time,
            "include_arm_mux": "false",
        }.items(),
    )

    # ── Visuomotor policy (optional, use_policy:=true) ────────────────────────
    # Model selected via policy_model_type. Requires BEV stitcher and wrist camera.
    # arm_cmd_mux above routes /arm/joint_commands to the arm driver.
    policy_inference = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_lerobot, "launch", "policy_inference.launch.py")
        ),
        condition=IfCondition(use_policy),
        launch_arguments={
            "model_type": policy_model_type,
            "checkpoint": policy_checkpoint,
            "include_arm_mux": "false",  # arm_cmd_mux already started above
        }.items(),
    )

    # ── LangGraph AI orchestration (optional, use_langchain:=true) ───────────
    # Requires ANTHROPIC_API_KEY env var. Subscribes /ai/command, publishes
    # to /mission/command (consumed by mission_planner).
    langchain_agent = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_orchestration, "launch", "langchain_agent.launch.py")
        ),
        condition=IfCondition(use_langchain),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # ── Robot state publisher ─────────────────────────────────────────────────
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[
            {
                "robot_description": robot_description,
                "use_sim_time": use_sim_time,
            }
        ],
    )

    # ── EKF localisation ──────────────────────────────────────────────────────
    ekf_node = Node(
        package="robot_localization",
        executable="ekf_node",
        name="ekf_filter_node",
        output="screen",
        parameters=[
            os.path.join(pkg_navigation, "config", "robot_localization.yaml"),
            {"use_sim_time": use_sim_time},
        ],
    )

    # ── SLAM Toolbox ──────────────────────────────────────────────────────────
    slam_toolbox = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_navigation, "launch", "slam_toolbox.launch.py")
        ),
        condition=IfCondition(use_slam),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # ── Nav2 (outputs to /cmd_vel natively) ───────────────────────────────────
    # The mux subscribes to /cmd_vel as the "nav2" source.
    autonomous_navigation = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_navigation, "launch", "autonomous_navigation.launch.py")
        ),
        launch_arguments={
            "use_sim_time": use_sim_time,
            "params_file": os.path.join(pkg_navigation, "config", "nav2_params.yaml"),
        }.items(),
    )

    # ── VLA node (publishes to /cmd_vel/vla) ──────────────────────────────────
    # Remap /image_raw to the configured camera topic so the node receives
    # real frames. Without this remap vla_node.py's hardcoded /image_raw
    # subscription receives nothing.
    vla_node = Node(
        package="omnibot_vla",
        executable="vla_node",
        name="vla_node",
        output="screen",
        parameters=[
            {
                "model_path": "openvla/openvla-7b",
                "device": vla_device,
                "load_in_4bit": vla_4bit,
                "use_sim_time": use_sim_time,
            }
        ],
        remappings=[
            ("/image_raw", vla_image_topic),
        ],
    )

    # ── cmd_vel Mux ───────────────────────────────────────────────────────────
    # Subscribes: /cmd_vel (nav2), /cmd_vel/vla, /cmd_vel/teleop
    # Publishes:  /cmd_vel/out  →  driver
    cmd_vel_mux_node = Node(
        package="omnibot_hybrid",
        executable="cmd_vel_mux",
        name="cmd_vel_mux",
        output="screen",
        parameters=[
            {
                "default_mode": "nav2",
                "use_sim_time": use_sim_time,
            }
        ],
    )

    # ── Mission Planner ───────────────────────────────────────────────────────
    mission_planner_node = Node(
        package="omnibot_hybrid",
        executable="mission_planner",
        name="mission_planner",
        output="screen",
        parameters=[{"use_sim_time": use_sim_time}],
    )

    # ── BEV stitcher (required by policy_node) ───────────────────────────────
    # Publishes /camera/base/bev/image_raw from 4 base-mounted cameras.
    # Enabled when use_bev:=true OR use_policy:=true.
    _bev_enabled = PythonExpression(
        [
            "'true' if '",
            use_bev,
            "' == 'true' or '",
            use_policy,
            "' == 'true' else 'false'",
        ]
    )
    bev_stitcher_node = Node(
        package="omnibot_lerobot",
        executable="bev_stitcher_node",
        name="bev_stitcher_node",
        output="screen",
        parameters=[{"use_sim_time": use_sim_time}],
        condition=IfCondition(_bev_enabled),
    )

    # ── ROSBridge WebSocket server ────────────────────────────────────────────
    rosbridge_node = Node(
        package="rosbridge_server",
        executable="rosbridge_websocket",
        name="rosbridge_websocket",
        output="screen",
        parameters=[{"port": 9090}],
        condition=IfCondition(use_rosbridge),
    )

    # ── Foxglove bridge — browser-based live monitoring ───────────────────────
    # Open https://app.foxglove.dev → Connect → ws://<robot-ip>:8765
    # Shows: 3D model, camera feeds, map, costmap, arm joints, mission status.
    # Disable with: hybrid_robot.launch.py use_foxglove:=false
    foxglove_node = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
        condition=IfCondition(use_foxglove),
    )

    # ── RViz (optional) ───────────────────────────────────────────────────────
    rviz_config = os.path.join(pkg_description, "config", "omnibot_navigation.rviz")
    rviz_node = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        output="screen",
        arguments=["-d", rviz_config],
        parameters=[{"use_sim_time": use_sim_time}],
        condition=IfCondition(use_rviz),
    )

    return LaunchDescription(
        [
            # ── Arguments ─────────────────────────────────────────────────────────
            DeclareLaunchArgument(
                "use_sim_time",
                default_value="false",
                description="Use Gazebo simulation clock",
            ),
            DeclareLaunchArgument(
                "use_rviz",
                default_value="true",
                description="Launch RViz for visualisation",
            ),
            DeclareLaunchArgument(
                "use_slam",
                default_value="true",
                description="Launch SLAM Toolbox for live mapping",
            ),
            DeclareLaunchArgument(
                "vla_device",
                default_value="cuda",
                description="PyTorch device for VLA inference (cuda / cpu)",
            ),
            DeclareLaunchArgument(
                "vla_4bit",
                default_value="false",
                description="Load VLA model in 4-bit quantisation (needs <16 GB VRAM)",
            ),
            DeclareLaunchArgument(
                "use_rosbridge",
                default_value="true",
                description="Start ROSBridge WebSocket server on port 9090 for Android app",
            ),
            DeclareLaunchArgument(
                "use_foxglove",
                default_value="true",
                description="Start Foxglove bridge on port 8765 for browser-based monitoring",
            ),
            DeclareLaunchArgument(
                "use_bev",
                default_value="true",
                description="Start BEV stitcher node (required by SmolVLA)",
            ),
            DeclareLaunchArgument(
                "use_rl",
                default_value="false",
                description="Start Isaac Lab RL inference nodes (rl_nav, rl_arm)",
            ),
            DeclareLaunchArgument(
                "use_policy",
                default_value="false",
                description="Start visuomotor policy node (9-DOF arm+base). Set policy_model_type to choose model.",
            ),
            DeclareLaunchArgument(
                "policy_model_type",
                default_value="smolvla",
                description="Policy model type: smolvla | act | diffusion | openvla",
            ),
            DeclareLaunchArgument(
                "policy_checkpoint",
                default_value="lerobot/smolvla_base",
                description="HuggingFace hub ID or local checkpoint path for the policy model",
            ),
            DeclareLaunchArgument(
                "vla_image_topic",
                default_value="/camera/front/image_raw",
                description="Camera topic fed into OpenVLA (remapped to /image_raw internally)",
            ),
            DeclareLaunchArgument(
                "use_langchain",
                default_value="false",
                description=(
                    "Start LangGraph AI orchestration node. "
                    "Requires ANTHROPIC_API_KEY env var."
                ),
            ),
            DeclareLaunchArgument(
                "use_ota",
                default_value="false",
                description="Start OTA update agent — exposes /ota/* services for Android app",
            ),
            DeclareLaunchArgument(
                "ota_manifest_url",
                default_value="",
                description="URL to OTA manifest.json (GitHub Releases)",
            ),
            # ── Nodes ─────────────────────────────────────────────────────────────
            driver_node,
            arm_cmd_mux_node,
            rl_inference,
            policy_inference,
            langchain_agent,
            robot_state_publisher,
            ekf_node,
            slam_toolbox,
            autonomous_navigation,
            vla_node,
            cmd_vel_mux_node,
            rosbag_recorder_node,
            mission_planner_node,
            bev_stitcher_node,
            rosbridge_node,
            foxglove_node,
            rviz_node,
            Node(
                package="omnibot_ota",
                executable="ota_agent",
                name="ota_agent_node",
                output="screen",
                parameters=[{"manifest_url": ota_manifest_url}],
                condition=IfCondition(use_ota),
            ),
        ]
    )
