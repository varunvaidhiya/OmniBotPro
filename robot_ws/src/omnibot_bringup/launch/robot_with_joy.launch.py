from launch import LaunchDescription
from launch.actions import IncludeLaunchDescription
from launch.launch_description_sources import PythonLaunchDescriptionSource
from ament_index_python.packages import get_package_share_directory
import os


def generate_launch_description():
    pkg_omnibot_bringup = get_package_share_directory("omnibot_bringup")

    # Launch Robot Driver (Yahboom Node)
    robot_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "robot.launch.py")
        )
    )

    # Launch Joystick Teleop
    joy_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "joy_teleop.launch.py")
        )
    )

    return LaunchDescription([robot_launch, joy_launch])
