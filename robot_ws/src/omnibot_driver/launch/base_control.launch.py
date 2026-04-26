from launch.launch_description import LaunchDescription
from launch_ros.actions import Node
from launch.substitutions import LaunchConfiguration
from launch.actions import DeclareLaunchArgument


def generate_launch_description():
    # Launch arguments
    serial_port = LaunchConfiguration("serial_port", default="/dev/ttyACM0")
    baud_rate = LaunchConfiguration("baud_rate", default="115200")
    wheel_radius = LaunchConfiguration("wheel_radius", default="0.05")
    wheel_separation_width = LaunchConfiguration(
        "wheel_separation_width", default="0.3"
    )
    wheel_separation_length = LaunchConfiguration(
        "wheel_separation_length", default="0.3"
    )

    # Declare launch arguments
    declare_serial_port_arg = DeclareLaunchArgument(
        "serial_port",
        default_value="/dev/ttyACM0",
        description="Serial port for communication with the STM32",
    )

    declare_baud_rate_arg = DeclareLaunchArgument(
        "baud_rate",
        default_value="115200",
        description="Baud rate for serial communication",
    )

    declare_wheel_radius_arg = DeclareLaunchArgument(
        "wheel_radius", default_value="0.05", description="Wheel radius in meters"
    )

    declare_wheel_separation_width_arg = DeclareLaunchArgument(
        "wheel_separation_width",
        default_value="0.3",
        description="Distance between left and right wheels in meters",
    )

    declare_wheel_separation_length_arg = DeclareLaunchArgument(
        "wheel_separation_length",
        default_value="0.3",
        description="Distance between front and back wheels in meters",
    )

    # Create node for serial bridge
    serial_bridge_node = Node(
        package="omnibot_driver",
        executable="serial_bridge_node.py",
        name="serial_bridge_node",
        parameters=[
            {"serial_port": serial_port},
            {"baud_rate": baud_rate},
            {"read_timeout": 0.1},
        ],
        output="screen",
    )

    # Create node for mecanum controller
    mecanum_controller_node = Node(
        package="omnibot_driver",
        executable="mecanum_controller_node.py",
        name="mecanum_controller_node",
        parameters=[
            {"serial_port": serial_port},
            {"baud_rate": baud_rate},
            {"wheel_radius": wheel_radius},
            {"wheel_separation_width": wheel_separation_width},
            {"wheel_separation_length": wheel_separation_length},
        ],
        output="screen",
    )

    return LaunchDescription(
        [
            declare_serial_port_arg,
            declare_baud_rate_arg,
            declare_wheel_radius_arg,
            declare_wheel_separation_width_arg,
            declare_wheel_separation_length_arg,
            serial_bridge_node,
            mecanum_controller_node,
        ]
    )
