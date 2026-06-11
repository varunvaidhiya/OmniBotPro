"""
perception_pipeline.launch.py

Launches the full omnibot_perception stack:
  1. perception_node         – YOLO detection / tracking / segmentation
  2. distance_estimator_node – depth + ultrasonic fusion
  3. vla_trigger_node        – perception → VLA handoff logic
  4. ultrasonic_driver_node  – HC-SR04 GPIO driver  (opt-in via use_ultrasonic)

Arguments:
  mode              detect | track | segment | vla     (default: track)
  target_class      COCO class name to track            (default: '')
  device            cpu | cuda                          (default: cpu)
  det_model         YOLO weights path/name              (default: yolov8n.pt)
  inference_hz      perception inference rate           (default: 10.0)
  use_ultrasonic    true | false                        (default: false)
  simulate_us       true | false  (simulated HC-SR04)   (default: false)
  auto_vla          true | false  (auto-trigger VLA)    (default: false)
  use_vla_trigger   true | false  (launch trigger node) (default: true)
"""

from launch import LaunchDescription
from launch.actions import DeclareLaunchArgument, OpaqueFunction
from launch.substitutions import LaunchConfiguration
from launch_ros.actions import Node
import os
from ament_index_python.packages import get_package_share_directory


def _make_nodes(context, *args, **kwargs):
    pkg = get_package_share_directory("omnibot_perception")
    cfg = os.path.join(pkg, "config", "perception_params.yaml")

    mode = LaunchConfiguration("mode").perform(context)
    target = LaunchConfiguration("target_class").perform(context)
    device = LaunchConfiguration("device").perform(context)
    det_model = LaunchConfiguration("det_model").perform(context)
    inf_hz = LaunchConfiguration("inference_hz").perform(context)
    use_us = LaunchConfiguration("use_ultrasonic").perform(context).lower() == "true"
    sim_us = LaunchConfiguration("simulate_us").perform(context).lower() == "true"
    auto_vla = LaunchConfiguration("auto_vla").perform(context).lower() == "true"
    use_trigger = (
        LaunchConfiguration("use_vla_trigger").perform(context).lower() == "true"
    )

    nodes = [
        # ── Perception node (detection / tracking / segmentation) ─────────────
        Node(
            package="omnibot_perception",
            executable="perception_node",
            name="perception_node",
            parameters=[
                cfg,
                {
                    "mode": mode,
                    "target_class": target,
                    "device": device,
                    "det_model": det_model,
                    "inference_hz": float(inf_hz),
                },
            ],
            remappings=[],
            output="screen",
        ),
        # ── Distance estimator (depth + optional ultrasonic) ──────────────────
        Node(
            package="omnibot_perception",
            executable="distance_estimator_node",
            name="distance_estimator_node",
            parameters=[cfg],
            output="screen",
        ),
    ]

    # ── VLA trigger (optional) ────────────────────────────────────────────────
    if use_trigger:
        nodes.append(
            Node(
                package="omnibot_perception",
                executable="vla_trigger_node",
                name="vla_trigger_node",
                parameters=[
                    cfg,
                    {
                        "auto_vla": auto_vla,
                        "target_class": target,
                    },
                ],
                output="screen",
            )
        )

    # ── Ultrasonic driver (optional, Pi 5 only) ───────────────────────────────
    if use_us:
        nodes.append(
            Node(
                package="omnibot_perception",
                executable="ultrasonic_driver_node",
                name="ultrasonic_driver_node",
                parameters=[cfg, {"simulate": sim_us}],
                output="screen",
            )
        )

    return nodes


def generate_launch_description():
    return LaunchDescription(
        [
            DeclareLaunchArgument(
                "mode",
                default_value="track",
                description="Perception mode: detect|track|segment|vla",
            ),
            DeclareLaunchArgument(
                "target_class",
                default_value="",
                description="COCO class to focus on (empty = all)",
            ),
            DeclareLaunchArgument(
                "device", default_value="cpu", description="Torch device: cpu or cuda"
            ),
            DeclareLaunchArgument(
                "det_model",
                default_value="yolov8n.pt",
                description="YOLO detection model path/name",
            ),
            DeclareLaunchArgument(
                "inference_hz", default_value="10.0", description="Inference rate in Hz"
            ),
            DeclareLaunchArgument(
                "use_ultrasonic",
                default_value="false",
                description="Launch the HC-SR04 driver node",
            ),
            DeclareLaunchArgument(
                "simulate_us",
                default_value="false",
                description="Simulate ultrasonic readings",
            ),
            DeclareLaunchArgument(
                "auto_vla",
                default_value="false",
                description="Auto-activate VLA on tracking loss",
            ),
            DeclareLaunchArgument(
                "use_vla_trigger",
                default_value="true",
                description="Launch VLA trigger node",
            ),
            OpaqueFunction(function=_make_nodes),
        ]
    )
