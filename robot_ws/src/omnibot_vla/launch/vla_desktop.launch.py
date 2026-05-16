from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node


def generate_launch_description():
    image_topic = LaunchConfiguration("image_topic", default="/camera/front/image_raw")

    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "image_topic",
                default_value="/camera/front/image_raw",
                description="Camera topic to feed into OpenVLA (remapped to /image_raw internally)",
            ),
            Node(
                package="omnibot_vla",
                executable="vla_node",
                name="vla_node",
                output="screen",
                parameters=[
                    {
                        "model_path": "openvla/openvla-7b",
                        "device": "cuda",
                        "load_in_4bit": False,
                    }
                ],
                remappings=[
                    ("/image_raw", image_topic),
                ],
            ),
        ]
    )
