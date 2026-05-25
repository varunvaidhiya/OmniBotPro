#!/usr/bin/env python3
"""
Standalone OTA agent launch.

Usage:
  ros2 launch omnibot_ota ota.launch.py \\
    manifest_url:=https://github.com/varunvaidhiya/omnibot/releases/latest/download/manifest.json

  # Local test server:
  VERSION=v0.1.0-test bash infra/ota/build_release_bundle.sh
  python3 -m http.server -d dist/ 8765 &
  ros2 launch omnibot_ota ota.launch.py \\
    manifest_url:=http://localhost:8765/manifest.json \\
    check_interval_s:=10
"""

import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    pkg = get_package_share_directory("omnibot_ota")

    declare_manifest_url = DeclareLaunchArgument(
        "manifest_url",
        default_value="",
        description="URL to manifest.json — GitHub Releases or local HTTP server",
    )
    declare_check_interval = DeclareLaunchArgument(
        "check_interval_s",
        default_value="3600.0",
        description="Seconds between automatic manifest polls (default 1 h)",
    )
    declare_service_name = DeclareLaunchArgument(
        "service_name",
        default_value="omnibot-robot.service",
        description="systemd service restarted after workspace update",
    )

    ota_node = Node(
        package="omnibot_ota",
        executable="ota_agent",
        name="ota_agent_node",
        output="screen",
        parameters=[
            os.path.join(pkg, "config", "ota_params.yaml"),
            {
                "manifest_url": LaunchConfiguration("manifest_url"),
                "check_interval_s": LaunchConfiguration("check_interval_s"),
                "service_name": LaunchConfiguration("service_name"),
            },
        ],
    )

    return LaunchDescription(
        [
            declare_manifest_url,
            declare_check_interval,
            declare_service_name,
            ota_node,
        ]
    )
