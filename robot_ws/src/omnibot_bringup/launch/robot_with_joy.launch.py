from launch import LaunchDescription
from launch.actions import IncludeLaunchDescription
from launch.launch_description_sources import PythonLaunchDescriptionSource
from ament_index_python.packages import (
    get_package_share_directory,
    PackageNotFoundError,
)
import logging
import os


def generate_launch_description():
    pkg_omnibot_bringup = get_package_share_directory("omnibot_bringup")

    # Auto-detect optional packages so launch never crashes when missing.
    try:
        get_package_share_directory("foxglove_bridge")
        foxglove_available = "true"
    except PackageNotFoundError:
        logging.getLogger("launch").warning(
            "[robot_with_joy] foxglove_bridge not installed — Foxglove disabled. "
            "Run: sudo apt install ros-jazzy-foxglove-bridge"
        )
        foxglove_available = "false"

    try:
        get_package_share_directory("robot_localization")
        ekf_available = "true"
    except PackageNotFoundError:
        logging.getLogger("launch").warning(
            "[robot_with_joy] robot_localization not installed — EKF disabled. "
            "Run: sudo apt install ros-jazzy-robot-localization"
        )
        ekf_available = "false"

    # Launch Robot Driver (Yahboom Node)
    robot_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "robot.launch.py")
        ),
        launch_arguments={
            "use_foxglove": foxglove_available,
            "ekf": ekf_available,
        }.items(),
    )

    # Launch Joystick Teleop
    joy_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "joy_teleop.launch.py")
        )
    )

    return LaunchDescription([robot_launch, joy_launch])
