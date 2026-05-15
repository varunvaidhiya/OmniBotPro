import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, IncludeLaunchDescription
from launch.conditions import IfCondition, UnlessCondition
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import Command, LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue


def generate_launch_description():
    pkg_desc = get_package_share_directory("omnibot_description")
    pkg_bringup = get_package_share_directory("omnibot_bringup")

    xacro_file = os.path.join(pkg_desc, "urdf", "omnibot.urdf.xacro")
    robot_description_config = ParameterValue(
        Command(["xacro ", xacro_file]), value_type=str
    )

    # ── Launch arguments ──────────────────────────────────────────────────────
    declare_use_rosbridge = DeclareLaunchArgument(
        "use_rosbridge", default_value="true",
        description="Start ROSBridge WebSocket server on port 9090 for Android app",
    )
    declare_use_foxglove = DeclareLaunchArgument(
        "use_foxglove", default_value="true",
        description="Start Foxglove bridge on port 8765 for browser-based live monitoring",
    )
    declare_ekf = DeclareLaunchArgument(
        "ekf", default_value="true",
        description="Run robot_localization EKF (fuses /odom + /imu/data → /odometry/filtered)",
    )

    use_rosbridge = LaunchConfiguration("use_rosbridge")
    use_foxglove = LaunchConfiguration("use_foxglove")
    use_ekf = LaunchConfiguration("ekf")

    # ── Yahboom driver ────────────────────────────────────────────────────────
    # publish_tf is disabled when EKF is running — the EKF owns odom→base_link.
    # When ekf:=false the driver broadcasts the TF itself (legacy behaviour).
    driver_with_ekf = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        condition=IfCondition(use_ekf),
        parameters=[{
            "serial_port": "/dev/ttyUSB0",
            "baud_rate": 115200,
            "wheel_separation_length": 0.165,
            "wheel_separation_width": 0.215,
            "wheel_radius": 0.04,
            "publish_tf": False,   # EKF owns odom→base_link TF
        }],
    )

    driver_without_ekf = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        condition=UnlessCondition(use_ekf),
        parameters=[{
            "serial_port": "/dev/ttyUSB0",
            "baud_rate": 115200,
            "wheel_separation_length": 0.165,
            "wheel_separation_width": 0.215,
            "wheel_radius": 0.04,
            "publish_tf": True,    # driver broadcasts TF when EKF is off
        }],
    )

    # ── Robot state publisher ─────────────────────────────────────────────────
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[{"robot_description": robot_description_config}],
    )

    # ── EKF — state estimation ────────────────────────────────────────────────
    # Fuses /odom (wheel dead-reckoning) + /imu/data (Yahboom IMU)
    # → /odometry/filtered + odom→base_link TF
    ekf = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_bringup, "launch", "state_estimation.launch.py")
        ),
        condition=IfCondition(use_ekf),
    )

    # ── ROSBridge — Android app (ws://<pi-ip>:9090) ───────────────────────────
    rosbridge_node = Node(
        package="rosbridge_server",
        executable="rosbridge_websocket",
        name="rosbridge_websocket",
        output="screen",
        parameters=[{"port": 9090}],
        condition=IfCondition(use_rosbridge),
    )

    # ── Foxglove bridge — browser-based live digital twin (ws://<pi-ip>:8765) ─
    foxglove_node = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
        condition=IfCondition(use_foxglove),
    )

    return LaunchDescription([
        declare_use_rosbridge,
        declare_use_foxglove,
        declare_ekf,
        driver_with_ekf,
        driver_without_ekf,
        robot_state_publisher,
        ekf,
        rosbridge_node,
        foxglove_node,
    ])
