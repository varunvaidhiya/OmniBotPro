import os

from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, IncludeLaunchDescription, TimerAction
from launch.conditions import IfCondition
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import Command, LaunchConfiguration
from launch_ros.actions import Node
from launch_ros.descriptions import ParameterValue


def generate_launch_description():
    pkg_bringup = get_package_share_directory("omnibot_bringup")
    pkg_desc = get_package_share_directory("omnibot_description")
    pkg_arm = get_package_share_directory("omnibot_arm")
    pkg_rl = get_package_share_directory("omnibot_rl")

    xacro_file = os.path.join(pkg_desc, "urdf", "omnibot.urdf.xacro")
    robot_description = ParameterValue(Command(["xacro ", xacro_file]), value_type=str)
    arm_params = os.path.join(pkg_arm, "config", "arm_params.yaml")
    rl_arm_params = os.path.join(pkg_rl, "config", "rl_arm_params.yaml")

    use_rosbridge = LaunchConfiguration("use_rosbridge", default="true")
    use_foxglove = LaunchConfiguration("use_foxglove", default="true")

    # ------------------------------------------------------------------
    # 1. Yahboom base driver — remapped so cmd_vel_mux controls it.
    #    Driver reads /cmd_vel/out (mux output) instead of plain /cmd_vel.
    #    EKF owns odom→base_link TF so publish_tf is disabled here.
    # ------------------------------------------------------------------
    driver_node = Node(
        package="omnibot_driver",
        executable="yahboom_controller_node.py",
        name="yahboom_driver",
        output="screen",
        parameters=[
            {
                "serial_port": "/dev/ttyUSB0",
                "baud_rate": 115200,
                "wheel_separation_length": 0.165,
                "wheel_separation_width": 0.215,
                "wheel_radius": 0.04,
                "publish_tf": False,
                "publish_diagnostics": True,
            }
        ],
        remappings=[("/cmd_vel", "/cmd_vel/out")],
    )

    # ------------------------------------------------------------------
    # 2. cmd_vel Mux — selects among nav2 / vla / teleop / rl_nav sources.
    #    In mobile manipulation mode default is "vla" so SmolVLA drives the base.
    # ------------------------------------------------------------------
    cmd_vel_mux_node = Node(
        package="omnibot_hybrid",
        executable="cmd_vel_mux",
        name="cmd_vel_mux",
        output="screen",
        parameters=[{"default_mode": "vla"}],
    )

    # ------------------------------------------------------------------
    # 3. Robot state publisher
    # ------------------------------------------------------------------
    robot_state_publisher = Node(
        package="robot_state_publisher",
        executable="robot_state_publisher",
        output="screen",
        parameters=[{"robot_description": robot_description}],
    )

    # ------------------------------------------------------------------
    # 4. EKF — fuses /odom + /imu/data → /odometry/filtered + odom→base_link TF
    # ------------------------------------------------------------------
    ekf = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_bringup, "launch", "state_estimation.launch.py")
        ),
    )

    # ------------------------------------------------------------------
    # 5. Arm driver (delayed 2 s to let base come up first).
    #    Subscribes to /arm/joint_commands/out (routed via arm_cmd_mux).
    # ------------------------------------------------------------------
    arm_driver_node = Node(
        package="omnibot_arm",
        executable="arm_driver_node.py",
        name="arm_driver_node",
        output="screen",
        parameters=[arm_params, {"publish_diagnostics": True}],
    )
    arm_driver_delayed = TimerAction(period=2.0, actions=[arm_driver_node])

    # ------------------------------------------------------------------
    # 6. Arm Command Mux — bridges /arm/joint_commands (SmolVLA / Android)
    #    and /arm/joint_commands/rl → /arm/joint_commands/out → arm_driver.
    #    Default mode "smolvla" is transparent: SmolVLA commands pass straight
    #    through without the RL nodes being needed.
    # ------------------------------------------------------------------
    arm_cmd_mux_node = Node(
        package="omnibot_rl",
        executable="arm_cmd_mux",
        name="arm_cmd_mux",
        output="screen",
        parameters=[rl_arm_params],
    )

    # ------------------------------------------------------------------
    # 7. Base cameras (4 × USB MJPEG)
    #    Device paths verified via v4l2-ctl --list-devices.
    # ------------------------------------------------------------------
    def _base_camera_node(name, device):
        return Node(
            package="usb_cam",
            executable="usb_cam_node_exe",
            name=f"{name}_camera",
            output="screen",
            parameters=[
                {
                    "video_device": device,
                    "image_width": 640,
                    "image_height": 480,
                    "framerate": 30.0,
                    "pixel_format": "mjpeg2rgb",
                    "camera_name": f"{name}_camera",
                    "camera_frame_id": f"{name}_camera_link",
                }
            ],
            remappings=[
                ("image_raw", f"/camera/{name}/image_raw"),
                ("camera_info", f"/camera/{name}/camera_info"),
            ],
        )

    front_camera_node = _base_camera_node("front", "/dev/video1")
    rear_camera_node = _base_camera_node("rear", "/dev/video3")
    left_camera_node = _base_camera_node("left", "/dev/video5")
    right_camera_node = _base_camera_node("right", "/dev/video7")

    # ------------------------------------------------------------------
    # 8. Wrist camera
    # ------------------------------------------------------------------
    wrist_camera_node = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="wrist_camera",
        output="screen",
        parameters=[
            {
                "video_device": "/dev/video11",
                "image_width": 640,
                "image_height": 480,
                "framerate": 30.0,
                "pixel_format": "mjpeg2rgb",
                "camera_name": "wrist_camera",
                "camera_frame_id": "wrist_camera_link",
            }
        ],
        remappings=[
            ("image_raw", "/camera/wrist/image_raw"),
            ("camera_info", "/camera/wrist/camera_info"),
        ],
    )

    # ------------------------------------------------------------------
    # 9. Orbbec Astra Pro depth camera
    # ------------------------------------------------------------------
    orbbec_camera_node = Node(
        package="orbbec_camera",
        executable="orbbec_camera_node",
        name="orbbec_astra_pro",
        output="screen",
        parameters=[
            {
                "camera_name": "depth",
                "depth_registration": True,
                "enable_point_cloud": True,
                "point_cloud_qos": "default",
                "depth_width": 640,
                "depth_height": 480,
                "depth_fps": 30,
                "color_width": 640,
                "color_height": 480,
                "color_fps": 30,
                "depth_optical_frame": "depth_camera_optical_frame",
                "color_optical_frame": "depth_camera_optical_frame",
            }
        ],
        remappings=[
            ("depth/image_raw", "/camera/depth/image_raw"),
            ("depth/camera_info", "/camera/depth/camera_info"),
            ("color/image_raw", "/camera/depth/color/image_raw"),
            ("depth/points", "/camera/depth/points"),
        ],
    )

    # ------------------------------------------------------------------
    # 10. BEV stitcher — combines 4 base cameras → /camera/base/bev/image_raw
    #     Required by smolvla_node and teleop_recorder_node.
    # ------------------------------------------------------------------
    bev_stitcher_node = Node(
        package="omnibot_lerobot",
        executable="bev_stitcher_node",
        name="bev_stitcher_node",
        output="screen",
        parameters=[
            {
                "canvas_size": 800,
                "output_width": 800,
                "output_height": 800,
                "src_width": 640,
                "src_height": 480,
                "publish_hz": 30.0,
            }
        ],
    )

    # ------------------------------------------------------------------
    # 11. ROSBridge WebSocket (Android app — ws://<pi>:9090)
    # ------------------------------------------------------------------
    rosbridge_node = Node(
        package="rosbridge_server",
        executable="rosbridge_websocket",
        name="rosbridge_websocket",
        output="screen",
        parameters=[{"port": 9090}],
        condition=IfCondition(use_rosbridge),
    )

    # ------------------------------------------------------------------
    # 12. Foxglove bridge (browser monitoring — ws://<pi>:8765)
    # ------------------------------------------------------------------
    foxglove_node = Node(
        package="foxglove_bridge",
        executable="foxglove_bridge",
        name="foxglove_bridge",
        output="screen",
        parameters=[{"port": 8765, "address": "0.0.0.0"}],
        condition=IfCondition(use_foxglove),
    )

    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "use_rosbridge",
                default_value="true",
                description="Start ROSBridge WebSocket server on port 9090 for Android app",
            ),
            DeclareLaunchArgument(
                "use_foxglove",
                default_value="true",
                description="Start Foxglove bridge on port 8765 for browser monitoring",
            ),
            # Base
            driver_node,
            cmd_vel_mux_node,
            robot_state_publisher,
            ekf,
            # Arm
            arm_driver_delayed,
            arm_cmd_mux_node,
            # Cameras
            front_camera_node,
            rear_camera_node,
            left_camera_node,
            right_camera_node,
            wrist_camera_node,
            orbbec_camera_node,
            bev_stitcher_node,
            # Connectivity
            rosbridge_node,
            foxglove_node,
        ]
    )
