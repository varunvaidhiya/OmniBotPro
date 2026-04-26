"""Launch the BEV stitcher node."""

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    return LaunchDescription(
        [
            DeclareLaunchArgument("canvas_size", default_value="800"),
            DeclareLaunchArgument("output_width", default_value="800"),
            DeclareLaunchArgument("output_height", default_value="800"),
            DeclareLaunchArgument("src_width", default_value="640"),
            DeclareLaunchArgument("src_height", default_value="480"),
            DeclareLaunchArgument("publish_hz", default_value="30.0"),
            DeclareLaunchArgument(
                "calibration_file", default_value="~/bev_calibration.npz"
            ),
            DeclareLaunchArgument(
                "output_topic", default_value="/camera/bev/image_raw"
            ),
            Node(
                package="ros2_bev_stitcher",
                executable="bev_stitcher",
                name="bev_stitcher",
                output="screen",
                parameters=[
                    {
                        "canvas_size": LaunchConfiguration("canvas_size"),
                        "output_width": LaunchConfiguration("output_width"),
                        "output_height": LaunchConfiguration("output_height"),
                        "src_width": LaunchConfiguration("src_width"),
                        "src_height": LaunchConfiguration("src_height"),
                        "publish_hz": LaunchConfiguration("publish_hz"),
                        "calibration_file": LaunchConfiguration("calibration_file"),
                        "output_topic": LaunchConfiguration("output_topic"),
                    }
                ],
            ),
        ]
    )
