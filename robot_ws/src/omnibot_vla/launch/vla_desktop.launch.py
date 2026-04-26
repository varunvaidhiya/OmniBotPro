from launch import LaunchDescription
from launch_ros.actions import Node


def generate_launch_description():
    return LaunchDescription(
        [
            Node(
                package="omnibot_vla",
                executable="vla_node",
                name="vla_node",
                output="screen",
                parameters=[
                    {
                        "model_path": "openvla/openvla-7b",
                        "device": "cuda",  # Ensure your PC has NVIDIA GPU
                        "load_in_4bit": False,  # Set True if you have <16GB VRAM
                    }
                ],
            )
        ]
    )
