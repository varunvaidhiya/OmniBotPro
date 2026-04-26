#!/usr/bin/env python3

import os
from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, IncludeLaunchDescription
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import LaunchConfiguration


def generate_launch_description():
    # Get the launch directory
    bringup_dir = get_package_share_directory("omnibot_navigation")
    launch_dir = os.path.join(bringup_dir, "launch")

    # Create the launch configuration variables
    namespace = LaunchConfiguration("namespace")
    use_sim_time = LaunchConfiguration("use_sim_time")
    autostart = LaunchConfiguration("autostart")
    params_file = LaunchConfiguration("params_file")
    use_lifecycle_mgr = LaunchConfiguration("use_lifecycle_mgr")
    use_remappings = LaunchConfiguration("use_remappings")

    # Declare the launch arguments
    declare_namespace_cmd = DeclareLaunchArgument(
        "namespace", default_value="", description="Top-level namespace"
    )

    declare_use_sim_time_cmd = DeclareLaunchArgument(
        "use_sim_time",
        default_value="false",
        description="Use simulation (Gazebo) clock if true",
    )

    declare_params_file_cmd = DeclareLaunchArgument(
        "params_file",
        default_value=os.path.join(bringup_dir, "config", "nav2_params.yaml"),
        description="Full path to the ROS2 parameters file to use",
    )

    declare_autostart_cmd = DeclareLaunchArgument(
        "autostart",
        default_value="true",
        description="Automatically startup the nav2 stack",
    )

    declare_use_lifecycle_mgr_cmd = DeclareLaunchArgument(
        "use_lifecycle_mgr",
        default_value="true",
        description="Whether to launch the lifecycle manager",
    )

    declare_use_remappings_cmd = DeclareLaunchArgument(
        "use_remappings",
        default_value="false",
        description="Arguments to pass to all nodes launched by the file",
    )

    # Specify the actions
    bringup_cmd = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(launch_dir, "nav2_bringup_launch.py")
        ),
        launch_arguments={
            "namespace": namespace,
            "use_sim_time": use_sim_time,
            "autostart": autostart,
            "params_file": params_file,
            "use_lifecycle_mgr": use_lifecycle_mgr,
            "use_remappings": use_remappings,
        }.items(),
    )

    # Create the launch description and populate
    ld = LaunchDescription()

    # Declare the launch options
    ld.add_action(declare_namespace_cmd)
    ld.add_action(declare_use_sim_time_cmd)
    ld.add_action(declare_params_file_cmd)
    ld.add_action(declare_autostart_cmd)
    ld.add_action(declare_use_lifecycle_mgr_cmd)
    ld.add_action(declare_use_remappings_cmd)

    # Add the actions to launch all of the navigation nodes
    ld.add_action(bringup_cmd)

    return ld
