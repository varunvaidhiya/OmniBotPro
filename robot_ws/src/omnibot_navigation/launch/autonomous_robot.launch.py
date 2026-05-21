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
    omnibot_bringup_dir = get_package_share_directory("omnibot_bringup")
    omnibot_driver_dir = get_package_share_directory("omnibot_driver")
    omnibot_description_dir = get_package_share_directory("omnibot_description")

    # Create the launch configuration variables
    namespace = LaunchConfiguration("namespace")
    use_sim_time = LaunchConfiguration("use_sim_time")
    autostart = LaunchConfiguration("autostart")
    params_file = LaunchConfiguration("params_file")
    use_lifecycle_mgr = LaunchConfiguration("use_lifecycle_mgr")
    use_remappings = LaunchConfiguration("use_remappings")
    use_slam = LaunchConfiguration("use_slam")
    use_3d_mapping = LaunchConfiguration("use_3d_mapping")
    use_rviz = LaunchConfiguration("use_rviz")
    rviz_config_file = LaunchConfiguration("rviz_config_file")

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

    declare_use_slam_cmd = DeclareLaunchArgument(
        "use_slam", default_value="true", description="Whether to use SLAM for mapping"
    )

    declare_use_3d_mapping_cmd = DeclareLaunchArgument(
        "use_3d_mapping",
        default_value="false",
        description="Launch RTAB-Map + OctoMap 3-D mapping (requires depth camera)",
    )

    declare_use_rviz_cmd = DeclareLaunchArgument(
        "use_rviz", default_value="true", description="Whether to start RViz"
    )

    declare_rviz_config_file_cmd = DeclareLaunchArgument(
        "rviz_config_file",
        default_value=os.path.join(
            omnibot_description_dir, "config", "omnibot_navigation.rviz"
        ),
        description="Full path to the RVIZ config file to use",
    )

    # Include robot base control
    robot_base_control = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(omnibot_driver_dir, "launch", "yahboom_base_control.launch.py")
        ),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # Include robot description
    robot_description = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(omnibot_description_dir, "launch", "display.launch.py")
        ),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # Include RTAB-Map 3-D mapping (conditional)
    rtabmap_3d = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(bringup_dir, "launch", "rtabmap.launch.py")
        ),
        condition=IfCondition(use_3d_mapping),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # Include SLAM toolbox
    slam_toolbox = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(bringup_dir, "launch", "slam_toolbox.launch.py")
        ),
        condition=IfCondition(use_slam),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
    )

    # Include autonomous navigation
    autonomous_navigation = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(bringup_dir, "launch", "autonomous_navigation.launch.py")
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

    # RViz
    rviz_node = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        output="screen",
        arguments=["-d", rviz_config_file],
        parameters=[{"use_sim_time": use_sim_time}],
        condition=IfCondition(use_rviz),
    )

    # State estimation — shared launch so EKF config stays in one place
    robot_localization_node = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(omnibot_bringup_dir, "launch", "state_estimation.launch.py")
        ),
        launch_arguments={"use_sim_time": use_sim_time}.items(),
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
    ld.add_action(declare_use_slam_cmd)
    ld.add_action(declare_use_3d_mapping_cmd)
    ld.add_action(declare_use_rviz_cmd)
    ld.add_action(declare_rviz_config_file_cmd)

    # Add the actions to launch all of the navigation nodes
    ld.add_action(robot_base_control)
    ld.add_action(robot_description)
    ld.add_action(robot_localization_node)
    ld.add_action(slam_toolbox)
    ld.add_action(rtabmap_3d)
    ld.add_action(autonomous_navigation)
    ld.add_action(rviz_node)

    return ld
