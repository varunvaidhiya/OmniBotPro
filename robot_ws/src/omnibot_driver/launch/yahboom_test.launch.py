#!/usr/bin/env python3

from launch import LaunchDescription
from launch_ros.actions import Node
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration


def generate_launch_description():
    # Declare launch arguments
    serial_port_arg = DeclareLaunchArgument(
        "serial_port",
        default_value="/dev/ttyUSB0",
        description="Serial port for Yahboom board communication",
    )

    baud_rate_arg = DeclareLaunchArgument(
        "baud_rate",
        default_value="115200",
        description="Baud rate for serial communication",
    )

    # Create the Yahboom test node
    yahboom_test_node = Node(
        package="omnibot_driver",
        executable="yahboom_test_node.py",
        name="yahboom_test_node",
        output="screen",
        parameters=[
            {
                "serial_port": LaunchConfiguration("serial_port"),
                "baud_rate": int(LaunchConfiguration("baud_rate")),
            }
        ],
    )

    return LaunchDescription(
        [
            # Launch arguments
            serial_port_arg,
            baud_rate_arg,
            # Nodes
            yahboom_test_node,
        ]
    )
