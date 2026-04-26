#!/usr/bin/env python3

import os
from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, IncludeLaunchDescription
from launch.conditions import IfCondition
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    # Get the launch directory
    bringup_dir = get_package_share_directory("omnibot_navigation")

    # Create the launch configuration variables
    use_sim_time = LaunchConfiguration("use_sim_time")
    use_waypoints = LaunchConfiguration("use_waypoints")
    waypoint_file = LaunchConfiguration("waypoint_file")
    loop_waypoints = LaunchConfiguration("loop_waypoints")

    # Declare the launch arguments
    declare_use_sim_time_cmd = DeclareLaunchArgument(
        "use_sim_time",
        default_value="false",
        description="Use simulation (Gazebo) clock if true",
    )

    declare_use_waypoints_cmd = DeclareLaunchArgument(
        "use_waypoints",
        default_value="true",
        description="Whether to use waypoint navigation",
    )

    declare_waypoint_file_cmd = DeclareLaunchArgument(
        "waypoint_file",
        default_value="waypoints.yaml",
        description="Waypoint file to use for navigation",
    )

    declare_loop_waypoints_cmd = DeclareLaunchArgument(
        "loop_waypoints",
        default_value="true",
        description="Whether to loop through waypoints continuously",
    )

    # Include autonomous robot launch
    autonomous_robot = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(bringup_dir, "launch", "autonomous_robot.launch.py")
        ),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # Waypoint navigator node
    waypoint_navigator = Node(
        package="omnibot_navigation",
        executable="waypoint_navigator.py",
        name="waypoint_navigator",
        output="screen",
        parameters=[
            {
                "use_sim_time": use_sim_time,
                "waypoint_file": waypoint_file,
                "loop_waypoints": loop_waypoints,
            }
        ],
        condition=IfCondition(use_waypoints),
    )

    # Create the launch description and populate
    ld = LaunchDescription()

    # Declare the launch options
    ld.add_action(declare_use_sim_time_cmd)
    ld.add_action(declare_use_waypoints_cmd)
    ld.add_action(declare_waypoint_file_cmd)
    ld.add_action(declare_loop_waypoints_cmd)

    # Add the actions to launch all of the navigation nodes
    ld.add_action(autonomous_robot)
    ld.add_action(waypoint_navigator)

    return ld
