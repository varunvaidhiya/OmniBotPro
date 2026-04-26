import os
from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch_ros.actions import Node


def generate_launch_description():
    pkg_omnibot_lerobot = get_package_share_directory("omnibot_lerobot")
    smolvla_params = os.path.join(pkg_omnibot_lerobot, "config", "smolvla_params.yaml")

    smolvla_node = Node(
        package="omnibot_lerobot",
        executable="smolvla_node",
        name="smolvla_node",
        output="screen",
        parameters=[smolvla_params],
    )

    return LaunchDescription(
        [
            smolvla_node,
        ]
    )
