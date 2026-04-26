import os
from ament_index_python.packages import get_package_share_directory
from launch import LaunchDescription
from launch.actions import IncludeLaunchDescription, TimerAction
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch_ros.actions import Node


def generate_launch_description():
    # Package share directories
    pkg_omnibot_bringup = get_package_share_directory("omnibot_bringup")
    pkg_omnibot_arm = get_package_share_directory("omnibot_arm")

    # Config files
    arm_params = os.path.join(pkg_omnibot_arm, "config", "arm_params.yaml")

    # ------------------------------------------------------------------
    # 1. Base robot (yahboom driver + robot state publisher)
    # ------------------------------------------------------------------
    robot_launch = IncludeLaunchDescription(
        PythonLaunchDescriptionSource(
            os.path.join(pkg_omnibot_bringup, "launch", "robot.launch.py")
        )
    )

    # ------------------------------------------------------------------
    # 2. ARM driver (delayed 2s to let base come up first)
    # ------------------------------------------------------------------
    arm_driver_node = Node(
        package="omnibot_arm",
        executable="arm_driver_node.py",
        name="arm_driver_node",
        output="screen",
        parameters=[arm_params],
    )

    arm_driver_delayed = TimerAction(
        period=2.0,
        actions=[arm_driver_node],
    )

    # ------------------------------------------------------------------
    # Base cameras (4x OV9732, 100° FOV, MJPEG to stay within USB bandwidth)
    # Device assignment (even-numbered video devices; odd are metadata):
    #   front  → /dev/video0
    #   rear   → /dev/video2
    #   left   → /dev/video4
    #   right  → /dev/video6
    # ------------------------------------------------------------------

    def _base_camera_node(name, device):
        """Helper: create a usb_cam node for one base camera."""
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
                    "pixel_format": "mjpeg2rgb",  # MJPEG keeps USB bandwidth low
                    "camera_name": f"{name}_camera",
                    "camera_frame_id": f"{name}_camera_link",
                }
            ],
            remappings=[
                ("image_raw", f"/camera/{name}/image_raw"),
                ("camera_info", f"/camera/{name}/camera_info"),
            ],
        )

    front_camera_node = _base_camera_node("front", "/dev/video0")
    rear_camera_node = _base_camera_node("rear", "/dev/video2")
    left_camera_node = _base_camera_node("left", "/dev/video4")
    right_camera_node = _base_camera_node("right", "/dev/video6")

    # ------------------------------------------------------------------
    # 7. Wrist camera — /dev/video8  (arm end-effector)
    # ------------------------------------------------------------------
    wrist_camera_node = Node(
        package="usb_cam",
        executable="usb_cam_node_exe",
        name="wrist_camera",
        output="screen",
        parameters=[
            {
                "video_device": "/dev/video8",
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
    # 8. Orbbec Astra Pro depth camera — rear-facing on top plate
    #    ROS 2 driver: https://github.com/orbbec/OrbbecSDK_ROS2
    #    Topics published: /camera/depth/image_raw, /camera/depth/camera_info,
    #                      /camera/color/image_raw, /camera/depth/points
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
    # 9. BEV stitcher — combines 4 base cameras into one top-down image
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

    return LaunchDescription(
        [
            robot_launch,
            arm_driver_delayed,
            front_camera_node,
            rear_camera_node,
            left_camera_node,
            right_camera_node,
            wrist_camera_node,
            orbbec_camera_node,
            bev_stitcher_node,
        ]
    )
