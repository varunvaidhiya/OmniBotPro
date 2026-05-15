from launch import LaunchDescription
from launch.actions import IncludeLaunchDescription
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import LaunchConfiguration
from ament_index_python.packages import get_package_share_directory, PackageNotFoundError
import os


def generate_launch_description():
    pkg_omnibot_bringup = get_package_share_directory("omnibot_bringup")

    # Auto-detect whether foxglove_bridge is installed so launch never crashes
    # when the package is missing. Install with:
    #   sudo apt install ros-jazzy-foxglove-bridge
    try:
        get_package_share_directory("foxglove_bridge")
        foxglove_available = "true"
    except PackageNotFoundError:
        import logging
        logging.getLogger("launch").warning(
            "[robot_with_joy] foxglove_bridge not installed — Foxglove disabled. "
            "Run: sudo apt install ros-jazzy-foxglove-bridge"
        )
        foxglove_available = "false"

    # Launch Robot Driver (Yahboom Node) — disable foxglove if not installed
    robot_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "robot.launch.py")
        ),
        launch_arguments={"use_foxglove": foxglove_available}.items(),
    )

    # Launch Joystick Teleop
    joy_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "joy_teleop.launch.py")
        )
    )

    return LaunchDescription([robot_launch, joy_launch])
