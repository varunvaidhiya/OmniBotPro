import os
from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch_ros.actions import Node


def generate_launch_description():
    pkg_omnibot_arm = get_package_share_directory("omnibot_arm")
    arm_params = os.path.join(pkg_omnibot_arm, "config", "arm_params.yaml")

    arm_driver_node = Node(
        package="omnibot_arm",
        executable="arm_driver_node.py",
        name="arm_driver_node",
        output="screen",
        parameters=[arm_params],
    )

    return LaunchDescription(
        [
            arm_driver_node,
        ]
    )
