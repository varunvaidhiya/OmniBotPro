from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.conditions import IfCondition, UnlessCondition
from launch.substitutions import LaunchConfiguration, Command
from launch_ros.actions import Node
from ament_index_python.packages import get_package_share_directory
import os


def generate_launch_description():
    # Get the launch directory
    pkg_omnibot_description = get_package_share_directory("omnibot_description")
    pkg_omnibot_bringup = get_package_share_directory("omnibot_bringup")

    # Set paths
    default_model_path = os.path.join(
        pkg_omnibot_description, "urdf", "omnibot.urdf.xacro"
    )
    default_rviz_config_path = os.path.join(
        pkg_omnibot_bringup, "config", "omnibot.rviz"
    )

    # Launch configuration variables
    use_gui = LaunchConfiguration("use_gui")
    model = LaunchConfiguration("model")
    rviz_config = LaunchConfiguration("rviz_config")

    # Declare the launch arguments
    declare_model_path_cmd = DeclareLaunchArgument(
        name="model",
        default_value=default_model_path,
        description="Absolute path to robot urdf file",
    )

    declare_use_gui_cmd = DeclareLaunchArgument(
        name="use_gui",
        default_value="true",
        description="Flag to enable joint_state_publisher_gui",
    )

    declare_rviz_config_cmd = DeclareLaunchArgument(
        name="rviz_config",
        default_value=default_rviz_config_path,
        description="Path to RViz config file",
    )

    # Process the URDF file
    robot_description_content = Command(["xacro ", model])

    # Specify the actions
    start_joint_state_publisher_cmd = Node(
        package="joint_state_publisher",
        executable="joint_state_publisher",
        condition=UnlessCondition(use_gui),
    )

    start_joint_state_publisher_gui_cmd = Node(
        package="joint_state_publisher_gui",
        executable="joint_state_publisher_gui",
        condition=IfCondition(use_gui),
    )

    start_robot_state_publisher_cmd = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        parameters=[{"robot_description": robot_description_content}],
    )

    start_rviz_cmd = Node(
        package="rviz2",
        executable="rviz2",
        name="rviz2",
        arguments=["-d", rviz_config],
        output="screen",
    )

    # Create and return launch description
    ld = LaunchDescription()
    ld.add_action(declare_model_path_cmd)
    ld.add_action(declare_use_gui_cmd)
    ld.add_action(declare_rviz_config_cmd)
    ld.add_action(start_joint_state_publisher_cmd)
    ld.add_action(start_joint_state_publisher_gui_cmd)
    ld.add_action(start_robot_state_publisher_cmd)
    ld.add_action(start_rviz_cmd)

    return ld
