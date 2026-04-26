from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node
from ament_index_python.packages import get_package_share_directory
from launch.substitutions import Command
from launch_ros.descriptions import ParameterValue
import os


def generate_launch_description():
    use_rosbridge = LaunchConfiguration("use_rosbridge", default="true")
    use_foxglove = LaunchConfiguration("use_foxglove", default="true")

    pkg_omnibot_description = get_package_share_directory("omnibot_description")
    xacro_file = os.path.join(pkg_omnibot_description, "urdf", "omnibot.urdf.xacro")

    robot_description_config = ParameterValue(
        Command(["xacro ", xacro_file]), value_type=str
    )

    start_driver_node = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        parameters=[
            {
                "serial_port": "/dev/ttyUSB0",
                "baud_rate": 115200,
                "wheel_separation_length": 0.165,
                "wheel_separation_width": 0.215,
                "wheel_radius": 0.04,
            }
        ],
    )

    start_robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        parameters=[{"robot_description": robot_description_config}],
    )

    # ROSBridge — Android app (ws://<pi-ip>:9090)
    rosbridge_node = Node(
        package="rosbridge_server",
        executable="rosbridge_websocket",
        name="rosbridge_websocket",
        output="screen",
        parameters=[{"port": 9090}],
        condition=IfCondition(use_rosbridge),
    )

    # Foxglove bridge — browser-based live digital twin (ws://<pi-ip>:8765)
    # Any contributor can open https://app.foxglove.dev and connect to the
    # real robot without installing ROS or a GPU.
    # Disable with: ros2 launch omnibot_bringup robot.launch.py use_foxglove:=false
    foxglove_node = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
        condition=IfCondition(use_foxglove),
    )

    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "use_rosbridge",
                default_value="true",
                description="Start ROSBridge WebSocket server on port 9090 for Android app",
            ),
            DeclareLaunchArgument(
                "use_foxglove",
                default_value="true",
                description="Start Foxglove bridge on port 8765 for browser-based live monitoring",
            ),
            start_driver_node,
            start_robot_state_publisher,
            rosbridge_node,
            foxglove_node,
        ]
    )
