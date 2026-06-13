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
        "use_rosbridge",
        default_value="true",
        description="Start ROSBridge WebSocket server on port 9090 for Android app",
    )
    declare_use_foxglove = DeclareLaunchArgument(
        "use_foxglove",
        default_value="true",
        description="Start Foxglove bridge on port 8765 for browser-based live monitoring",
    )
    declare_ekf = DeclareLaunchArgument(
        "ekf",
        default_value="true",
        description="Run robot_localization EKF (fuses /odom + /imu/data → /odometry/filtered)",
    )
    declare_use_metrics = DeclareLaunchArgument(
        "use_metrics",
        default_value="false",
        description="Start Prometheus metrics bridge (port 8888) for Grafana observability",
    )
    declare_metrics_port = DeclareLaunchArgument(
        "metrics_port",
        default_value="8888",
        description="HTTP port the Prometheus metrics bridge listens on",
    )
    declare_use_ota = DeclareLaunchArgument(
        "use_ota",
        default_value="false",
        description="Start OTA update agent (requires OTA_MANIFEST_URL in deployment.env)",
    )
    declare_ota_manifest_url = DeclareLaunchArgument(
        "ota_manifest_url",
        default_value="",
        description="URL to OTA manifest.json — overrides deployment.env OTA_MANIFEST_URL",
    )

    use_rosbridge = LaunchConfiguration("use_rosbridge")
    use_foxglove = LaunchConfiguration("use_foxglove")
    use_ekf = LaunchConfiguration("ekf")
    use_metrics = LaunchConfiguration("use_metrics")
    metrics_port = LaunchConfiguration("metrics_port")
    use_ota = LaunchConfiguration("use_ota")
    ota_manifest_url = LaunchConfiguration("ota_manifest_url")

    # ── Yahboom driver ────────────────────────────────────────────────────────
    # publish_tf is disabled when EKF is running — the EKF owns odom→base_link.
    # When ekf:=false the driver broadcasts the TF itself (legacy behaviour).
    driver_with_ekf = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        condition=IfCondition(use_ekf),
        parameters=[
            {
                "serial_port": "/dev/ttyUSB0",
                "baud_rate": 115200,
                "wheel_separation_length": 0.165,
                "wheel_separation_width": 0.215,
                "wheel_radius": 0.04,
                "publish_tf": False,  # EKF owns odom→base_link TF
                "publish_diagnostics": True,  # Android app /diagnostics panel
            }
        ],
    )

    driver_without_ekf = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        condition=UnlessCondition(use_ekf),
        parameters=[
            {
                "serial_port": "/dev/ttyUSB0",
                "baud_rate": 115200,
                "wheel_separation_length": 0.165,
                "wheel_separation_width": 0.215,
                "wheel_radius": 0.04,
                "publish_tf": True,  # driver broadcasts TF when EKF is off
                "publish_diagnostics": True,  # Android app /diagnostics panel
            }
        ],
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

    # ── Prometheus metrics bridge — Grafana observability ─────────────────────
    # Exposes /metrics on <metrics_port> for Prometheus to scrape.
    # Enable with: ros2 launch omnibot_bringup robot.launch.py use_metrics:=true
    metrics_bridge_node = Node(
        package="omnibot_metrics",
        executable="metrics_bridge",
        name="ros2_prometheus_bridge",
        output="screen",
        parameters=[
            {
                "metrics_port": metrics_port,
                "machine_label": "raspberry_pi",
            }
        ],
        condition=IfCondition(use_metrics),
    )

    # ── OTA update agent ──────────────────────────────────────────────────────
    # Polls manifest.json for updates; Android app calls /ota/* services.
    # Enable with: ros2 launch omnibot_bringup robot.launch.py use_ota:=true ota_manifest_url:=<URL>
    ota_agent_node = Node(
        package="omnibot_ota",
        executable="ota_agent",
        name="ota_agent_node",
        output="screen",
        parameters=[{"manifest_url": ota_manifest_url}],
        condition=IfCondition(use_ota),
    )

    return LaunchDescription(
        [
            declare_use_rosbridge,
            declare_use_foxglove,
            declare_ekf,
            declare_use_metrics,
            declare_metrics_port,
            declare_use_ota,
            declare_ota_manifest_url,
            driver_with_ekf,
            driver_without_ekf,
            robot_state_publisher,
            ekf,
            rosbridge_node,
            foxglove_node,
            metrics_bridge_node,
            ota_agent_node,
        ]
    )
