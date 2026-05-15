from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node
from ament_index_python.packages import get_package_share_directory
import os

def generate_launch_description():
    pkg_omnibot_bringup = get_package_share_directory('omnibot_bringup')

    config_file = os.path.join(
        pkg_omnibot_bringup,
        'config',
        'xbox_teleop.yaml'
    )

    # Default to js0 (most systems). Override with joy_dev:=/dev/input/js1 if needed.
    joy_dev_arg = DeclareLaunchArgument(
        'joy_dev',
        default_value='/dev/input/js0',
        description='Joystick device path (e.g. /dev/input/js0 or js1)',
    )

    joy_node = Node(
        package='joy',
        executable='joy_node',
        name='joy_node',
        output='screen',
        parameters=[{
            'dev': LaunchConfiguration('joy_dev'),
            'deadzone': 0.1,
            'autorepeat_rate': 20.0,
        }]
    )

    teleop_node = Node(
        package='teleop_twist_joy',
        executable='teleop_node',
        name='teleop_twist_joy_node',
        output='screen',
        parameters=[config_file],
        remappings=[('/cmd_vel', '/cmd_vel')],
    )

    return LaunchDescription([
        joy_dev_arg,
        joy_node,
        teleop_node,
    ])
