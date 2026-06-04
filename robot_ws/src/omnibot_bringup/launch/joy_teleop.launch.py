from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    # Default to js0 (most systems). Override with joy_dev:=/dev/input/js1 if needed.
    joy_dev_arg = DeclareLaunchArgument(
        "joy_dev",
        default_value="/dev/input/js0",
        description="Joystick device path (e.g. /dev/input/js0 or js1)",
    )

    # Only the raw joy publisher — control logic lives in yahboom_controller_node.joy_callback.
    # teleop_twist_joy is NOT launched here; it was overriding direct joy handling.
    joy_node = Node(
        package="joy",
        executable="joy_node",
        name="joy_node",
        output="screen",
        parameters=[
            {
                "dev": LaunchConfiguration("joy_dev"),
                "deadzone": 0.1,
                "autorepeat_rate": 20.0,
            }
        ],
    )

    return LaunchDescription(
        [
            joy_dev_arg,
            joy_node,
        ]
    )
