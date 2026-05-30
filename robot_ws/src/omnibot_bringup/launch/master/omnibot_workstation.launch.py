#!/usr/bin/env python3
"""
omnibot_workstation.launch.py — Master launch for the GPU workstation.

Run this on the GPU PC alongside omnibot_pi.launch.py on the Pi.
Requires NVIDIA GPU with ≥16 GB VRAM for OpenVLA / SmolVLA.

What this starts:
  ┌─ VLA POLICY ────────────────────────────────────────────────────────────┐
  │  vla_node        OpenVLA 7B — /image_raw + /vla/prompt → /cmd_vel/vla   │
  │  smolvla_node    SmolVLA 9-DOF — wrist+BEV+joints+odom → arm+base cmds  │
  ├─ RL INFERENCE ──────────────────────────────────────────────────────────┤
  │  rl_nav_node         ONNX nav policy → /cmd_vel/rl (20 Hz)              │
  │  rl_arm_node         ONNX arm policy → /arm/joint_commands/rl (20 Hz)   │
  │  rl_object_pose_node ArUco pose → /rl_arm/target_pose                   │
  ├─ AI ORCHESTRATION ──────────────────────────────────────────────────────┤
  │  langchain_agent_node  LangGraph + Claude → /mission/command            │
  └─────────────────────────────────────────────────────────────────────────┘

Usage (after starting Pi):
    ros2 launch omnibot_bringup master/omnibot_workstation.launch.py

Run only SmolVLA (skip OpenVLA and RL):
    ros2 launch omnibot_bringup master/omnibot_workstation.launch.py \
        use_vla:=false use_rl:=false

Run only RL (no VLA):
    ros2 launch omnibot_bringup master/omnibot_workstation.launch.py \
        use_vla:=false use_smolvla:=false

Enable LangGraph AI agent (needs ANTHROPIC_API_KEY env var):
    ros2 launch omnibot_bringup master/omnibot_workstation.launch.py \
        use_langchain:=true

Prerequisites:
    export ROS_DOMAIN_ID=30
    export ROS_STATIC_PEERS=<pi-ip>
    export ANTHROPIC_API_KEY=<key>   # only if use_langchain:=true
"""

import os

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():

    # ── Launch arguments ──────────────────────────────────────────────────────
    declare_use_vla = DeclareLaunchArgument(
        "use_vla",
        default_value="true",
        description="Start OpenVLA 7B node (needs ≥16 GB VRAM)",
    )
    declare_use_smolvla = DeclareLaunchArgument(
        "use_smolvla",
        default_value="true",
        description="Start SmolVLA unified 9-DOF policy node",
    )
    declare_use_rl = DeclareLaunchArgument(
        "use_rl",
        default_value="true",
        description="Start RL inference nodes (nav + arm + pose, ONNX)",
    )
    declare_use_langchain = DeclareLaunchArgument(
        "use_langchain",
        default_value="false",
        description="Start LangGraph AI orchestration (needs ANTHROPIC_API_KEY)",
    )

    # Model / device params
    declare_vla_model = DeclareLaunchArgument(
        "vla_model",
        default_value="openvla/openvla-7b",
        description="OpenVLA HuggingFace model path or local dir",
    )
    declare_smolvla_checkpoint = DeclareLaunchArgument(
        "smolvla_checkpoint",
        default_value="lerobot/smolvla_base",
        description="SmolVLA checkpoint path or HuggingFace repo",
    )
    declare_device = DeclareLaunchArgument(
        "device",
        default_value="cuda",
        description="Torch device for VLA inference (cuda / cpu)",
    )
    declare_nav_policy = DeclareLaunchArgument(
        "nav_policy",
        default_value="~/models/omnibot_nav_policy.onnx",
        description="Path to RL nav ONNX policy",
    )
    declare_arm_policy = DeclareLaunchArgument(
        "arm_policy",
        default_value="~/models/omnibot_arm_policy.onnx",
        description="Path to RL arm ONNX policy",
    )

    # ── 1. OpenVLA node ───────────────────────────────────────────────────────
    # Subscribes: /image_raw (remapped from /camera/front/image_raw), /vla/prompt
    # Publishes:  /cmd_vel/vla → cmd_vel_mux on Pi
    vla_node = Node(
        package="omnibot_vla",
        executable="vla_node",
        name="vla_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("use_vla")),
        parameters=[
            {
                "model_path": LaunchConfiguration("vla_model"),
                "device": LaunchConfiguration("device"),
                "load_in_4bit": False,
            }
        ],
        remappings=[("/image_raw", "/camera/front/image_raw")],
    )

    # ── 2. SmolVLA unified 9-DOF policy ──────────────────────────────────────
    # Subscribes: /camera/wrist/image_raw, /camera/base/bev/image_raw,
    #             /arm/joint_states, /odom
    # Publishes:  /arm/joint_commands → arm_cmd_mux on Pi
    #             /cmd_vel → cmd_vel_mux on Pi
    smolvla_node = Node(
        package="omnibot_lerobot",
        executable="policy_node",
        name="smolvla_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("use_smolvla")),
        parameters=[
            {
                "checkpoint_path": LaunchConfiguration("smolvla_checkpoint"),
                "device": LaunchConfiguration("device"),
                "policy_hz": 10.0,
                "task_description": "pick up the object and place it",
            }
        ],
    )

    # ── 3. RL navigation node ─────────────────────────────────────────────────
    # Subscribes: /odom, /camera/depth/points, /rl_nav/goal
    # Publishes:  /cmd_vel/rl → cmd_vel_mux on Pi (active when mode==rl_nav)
    rl_nav_node = Node(
        package="omnibot_rl",
        executable="rl_nav_node",
        name="rl_nav_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("use_rl")),
        parameters=[
            {
                "policy_path": LaunchConfiguration("nav_policy"),
                "policy_hz": 20.0,
                "goal_tolerance": 0.25,
                "max_lin_vel": 0.20,
                "max_ang_vel": 1.00,
            }
        ],
    )

    # ── 4. RL arm node ────────────────────────────────────────────────────────
    # Subscribes: /arm/joint_states, /rl_arm/target_pose
    # Publishes:  /arm/joint_commands/rl → arm_cmd_mux on Pi (active when mode==rl_arm)
    rl_arm_node = Node(
        package="omnibot_rl",
        executable="rl_arm_node",
        name="rl_arm_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("use_rl")),
        parameters=[
            {
                "policy_path": LaunchConfiguration("arm_policy"),
                "policy_hz": 20.0,
                "max_delta": 0.05,
            }
        ],
    )

    # ── 5. RL object pose estimator ───────────────────────────────────────────
    # Subscribes: /camera/wrist/image_raw, /camera/depth/image_raw
    # Publishes:  /rl_arm/target_pose, /rl_arm/target_detected
    rl_object_pose_node = Node(
        package="omnibot_rl",
        executable="rl_object_pose_node",
        name="rl_object_pose_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("use_rl")),
        parameters=[
            {
                "aruco_dict_id": 4,
                "marker_id": 0,
                "marker_size_m": 0.05,
                "camera_frame": "wrist_camera_link",
            }
        ],
    )

    # ── 6. LangGraph AI orchestration ────────────────────────────────────────
    # Subscribes: /ai/command, /camera/front/image_raw, /camera/wrist/image_raw
    # Publishes:  /mission/command → mission_planner on Pi
    langchain_agent = Node(
        package="omnibot_orchestration",
        executable="langchain_agent_node",
        name="langchain_agent_node",
        output="screen",
        condition=IfCondition(LaunchConfiguration("use_langchain")),
        parameters=[
            {
                "anthropic_api_key": os.environ.get("ANTHROPIC_API_KEY", ""),
                "vla_serve_url": "http://localhost:8000",
                "use_claude_vision": True,
            }
        ],
    )

    # ── Assemble ──────────────────────────────────────────────────────────────
    return LaunchDescription(
        [
            # Arguments
            declare_use_vla,
            declare_use_smolvla,
            declare_use_rl,
            declare_use_langchain,
            declare_vla_model,
            declare_smolvla_checkpoint,
            declare_device,
            declare_nav_policy,
            declare_arm_policy,
            # VLA policies
            vla_node,
            smolvla_node,
            # RL inference
            rl_nav_node,
            rl_arm_node,
            rl_object_pose_node,
            # AI orchestration
            langchain_agent,
        ]
    )
