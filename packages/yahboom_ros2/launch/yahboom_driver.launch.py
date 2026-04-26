"""Launch the Yahboom driver node with configurable parameters."""

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    return LaunchDescription(
        [
            DeclareLaunchArgument("serial_port", default_value="/dev/ttyUSB0"),
            DeclareLaunchArgument("baud_rate", default_value="115200"),
            DeclareLaunchArgument("wheel_radius", default_value="0.04"),
            DeclareLaunchArgument("sep_width", default_value="0.215"),
            DeclareLaunchArgument("sep_length", default_value="0.165"),
            DeclareLaunchArgument("max_linear", default_value="0.5"),
            DeclareLaunchArgument("max_angular", default_value="2.0"),
            DeclareLaunchArgument("ramp_step", default_value="0.05"),
            DeclareLaunchArgument("update_hz", default_value="20.0"),
            DeclareLaunchArgument("debug_serial", default_value="false"),
            Node(
                package="yahboom_ros2",
                executable="yahboom_driver",
                name="yahboom_driver",
                output="screen",
                parameters=[
                    {
                        "serial_port": LaunchConfiguration("serial_port"),
                        "baud_rate": LaunchConfiguration("baud_rate"),
                        "wheel_radius": LaunchConfiguration("wheel_radius"),
                        "wheel_separation_width": LaunchConfiguration("sep_width"),
                        "wheel_separation_length": LaunchConfiguration("sep_length"),
                        "max_linear_vel": LaunchConfiguration("max_linear"),
                        "max_angular_vel": LaunchConfiguration("max_angular"),
                        "ramp_step": LaunchConfiguration("ramp_step"),
                        "update_hz": LaunchConfiguration("update_hz"),
                        "debug_serial": LaunchConfiguration("debug_serial"),
                    }
                ],
            ),
        ]
    )
