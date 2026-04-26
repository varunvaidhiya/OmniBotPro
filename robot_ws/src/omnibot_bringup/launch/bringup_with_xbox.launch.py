from launch import LaunchDescription
from launch.actions import IncludeLaunchDescription
from launch.launch_description_sources import PythonLaunchDescriptionSource
from ament_index_python.packages import get_package_share_directory
import os


def generate_launch_description():
    # Get the package directories
    pkg_omnibot_bringup = get_package_share_directory("omnibot_bringup")

    # Define the paths to the other launch files
    robot_launch_path = os.path.join(pkg_omnibot_bringup, "launch", "robot.launch.py")

    joy_teleop_launch_path = os.path.join(
        pkg_omnibot_bringup, "launch", "joy_teleop.launch.py"
    )

    # create launch files
    robot_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(robot_launch_path)
    )

    joy_teleop_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(joy_teleop_launch_path)
    )

    return LaunchDescription([robot_launch, joy_teleop_launch])
