"""Agent harness launch — OmniBot's continuous deliberative brain.

Starts ``world_state_node`` (fuses robot state → /agent/world_state) and
``agent_node`` (runs the AgentHarness loop). Run alongside the base stack
(hybrid_robot.launch.py) and, on the GPU/cloud side, vla_serve.

Prereqs:
  pip install -e agent_engine            # pure-Python harness core
  pip install -e learning_engine          # optional: safety verify + reflect + learn
  export ANTHROPIC_API_KEY=sk-ant-...      # optional: cloud reasoning (else offline)

Quick-start:
  ros2 launch omnibot_orchestration agent.launch.py
  ros2 topic pub --once /ai/command std_msgs/msg/String \\
    "data: 'Find the red cup in the kitchen and bring it back'"
  ros2 topic echo /ai/status
  ros2 topic echo /agent/world_state
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import EnvironmentVariable, LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg = get_package_share_directory("omnibot_orchestration")
    config_file = os.path.join(pkg, "config", "agent_params.yaml")

    use_sim_time = LaunchConfiguration("use_sim_time", default="false")

    world_state_node = Node(
        package="omnibot_orchestration",
        executable="world_state_node",
        name="world_state_node",
        output="screen",
        parameters=[config_file, {"use_sim_time": use_sim_time}],
    )

    agent_node = Node(
        package="omnibot_orchestration",
        executable="agent_node",
        name="agent_node",
        output="screen",
        parameters=[
            config_file,
            {
                "use_sim_time": use_sim_time,
                "anthropic_api_key": EnvironmentVariable(
                    "ANTHROPIC_API_KEY", default_value=""
                ),
            },
        ],
    )

    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "use_sim_time",
                default_value="false",
                description="Use simulation clock (set true when running with Gazebo)",
            ),
            world_state_node,
            agent_node,
        ]
    )
