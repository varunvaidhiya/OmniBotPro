"""
Launch the Prometheus metrics bridge node.

Usage:
  # On Raspberry Pi
  ros2 launch omnibot_metrics metrics.launch.py machine:=raspberry_pi port:=8888

  # On GPU Desktop (alongside VLA/RL nodes)
  ros2 launch omnibot_metrics metrics.launch.py machine:=gpu_desktop port:=8889
"""

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    declare_port = DeclareLaunchArgument(
        "port",
        default_value="8888",
        description="HTTP port Prometheus will scrape for /metrics",
    )
    declare_machine = DeclareLaunchArgument(
        "machine",
        default_value="unknown",
        description="Machine label added to all metrics (e.g. raspberry_pi, gpu_desktop)",
    )

    metrics_bridge = Node(
        package="omnibot_metrics",
        executable="metrics_bridge",
        name="ros2_prometheus_bridge",
        output="screen",
        parameters=[
            {
                "metrics_port": LaunchConfiguration("port"),
                "machine_label": LaunchConfiguration("machine"),
            }
        ],
    )

    return LaunchDescription(
        [
            declare_port,
            declare_machine,
            metrics_bridge,
        ]
    )
