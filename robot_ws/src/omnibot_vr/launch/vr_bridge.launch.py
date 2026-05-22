"""
Launch file for the VR Recording Bridge node.

Starts the vr_recording_bridge node with configurable upload directory
and HTTP port for receiving JSONL episode uploads from the VR headset.

Usage:
    ros2 launch omnibot_vr vr_bridge.launch.py
    ros2 launch omnibot_vr vr_bridge.launch.py http_port:=8765 upload_dir:=~/datasets/vr_episodes
"""

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    # ── Declare launch arguments ─────────────────────────────────────────────
    upload_dir_arg = DeclareLaunchArgument(
        "upload_dir",
        default_value="~/datasets/vr_episodes",
        description="Directory where uploaded JSONL episode files are saved.",
    )

    http_port_arg = DeclareLaunchArgument(
        "http_port",
        default_value="8765",
        description="TCP port for the aiohttp episode upload HTTP server.",
    )

    # ── Node ─────────────────────────────────────────────────────────────────
    vr_bridge_node = Node(
        package="omnibot_vr",
        executable="vr_recording_bridge",
        name="vr_recording_bridge",
        output="screen",
        emulate_tty=True,
        parameters=[
            {
                "upload_dir": LaunchConfiguration("upload_dir"),
                "http_port": LaunchConfiguration("http_port"),
            }
        ],
    )

    return LaunchDescription(
        [
            upload_dir_arg,
            http_port_arg,
            vr_bridge_node,
        ]
    )
