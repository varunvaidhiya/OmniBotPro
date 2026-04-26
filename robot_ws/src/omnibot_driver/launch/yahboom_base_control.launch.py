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

    wheel_radius_arg = DeclareLaunchArgument(
        "wheel_radius", default_value="0.04", description="Wheel radius in meters"
    )

    wheel_separation_width_arg = DeclareLaunchArgument(
        "wheel_separation_width",
        default_value="0.215",
        description="Distance between left and right wheels in meters",
    )

    wheel_separation_length_arg = DeclareLaunchArgument(
        "wheel_separation_length",
        default_value="0.165",
        description="Distance between front and back wheels in meters",
    )

    # Create the Yahboom controller node
    yahboom_controller_node = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_controller_node",
        output="screen",
        parameters=[
            {
                "serial_port": LaunchConfiguration("serial_port"),
                "baud_rate": int(LaunchConfiguration("baud_rate")),
                "wheel_radius": float(LaunchConfiguration("wheel_radius")),
                "wheel_separation_width": float(
                    LaunchConfiguration("wheel_separation_width")
                ),
                "wheel_separation_length": float(
                    LaunchConfiguration("wheel_separation_length")
                ),
                "max_linear_velocity": 1.0,
                "max_angular_velocity": 2.0,
                "max_pwm_percentage": 100,
                "max_rad_s": 10.0,
            }
        ],
        remappings=[("cmd_vel", "cmd_vel"), ("odom", "odom")],
    )

    # Create robot state publisher node (if you have URDF)
    robot_state_publisher_node = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        name="robot_state_publisher",
        parameters=[
            {"robot_description": "<!-- Robot description will be loaded from URDF -->"}
        ],
    )

    return LaunchDescription(
        [
            # Launch arguments
            serial_port_arg,
            baud_rate_arg,
            wheel_radius_arg,
            wheel_separation_width_arg,
            wheel_separation_length_arg,
            # Nodes
            yahboom_controller_node,
            robot_state_publisher_node,
        ]
    )
