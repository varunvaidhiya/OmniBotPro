"""
setup_omnigraph.py — Wire OmniBot ROS 2 OmniGraph inside Isaac Sim

Run this script from Isaac Sim's Script Editor (Window → Script Editor)
AFTER the omnibot.usd stage has been loaded.

What it creates:
  - An ActionGraph at /World/OmniBot/ros2_graph
  - Nodes:
      IsaacReadSimulationTime  → ROS2PublishClock
      IsaacComputeOdometry     → ROS2PublishOdometry
      IsaacReadJointState      → ROS2PublishJointState
      IsaacReadIMU             → ROS2PublishImu
      ROS2SubscribeTwist       → IsaacArticulationController  (base /cmd_vel)
      ROS2SubscribeJointState  → IsaacArticulationController  (arm /arm/joint_commands)
      ROS2CameraHelper × 6     (front/rear/left/right/wrist + depth)
      ROS2PublishTransformTree

Assumptions:
  - omnibot.usd is loaded at /World/OmniBot
  - Articulation root is at /World/OmniBot  (set in USD importer)
  - Wheel joints: front_left_wheel_joint, front_right_wheel_joint,
                  rear_left_wheel_joint,  rear_right_wheel_joint
  - Arm joints: arm_shoulder_pan, arm_shoulder_lift, arm_elbow_flex,
                arm_wrist_flex, arm_wrist_roll, arm_gripper
  - Camera prims at /World/OmniBot/front_camera_link/Camera (etc.)

Isaac Sim 4.x API reference:
  https://docs.isaacsim.omniverse.nvidia.com/latest/
"""

import omni.graph.core as og
import omni.usd  # noqa: F401 — required for Isaac Sim USD context initialisation

ROBOT_PRIM = "/World/OmniBot"
GRAPH_PATH = f"{ROBOT_PRIM}/ros2_graph"
ROS_NS = ""  # leave empty for global topics


def _create_graph():
    keys = og.Controller.Keys
    (graph, nodes, _, _) = og.Controller.edit(
        {"graph_path": GRAPH_PATH, "evaluator_name": "execution"},
        {
            keys.CREATE_NODES: [
                # ── Simulation clock ──────────────────────────────────────
                ("on_playback_tick", "omni.graph.action.OnPlaybackTick"),
                ("sim_time", "omni.isaac.core_nodes.IsaacReadSimulationTime"),
                ("publish_clock", "omni.isaac.ros2_bridge.ROS2PublishClock"),
                # ── Odometry ──────────────────────────────────────────────
                ("compute_odom", "omni.isaac.core_nodes.IsaacComputeOdometry"),
                ("publish_odom", "omni.isaac.ros2_bridge.ROS2PublishOdometry"),
                # ── Joint states ──────────────────────────────────────────
                ("read_joint_state", "omni.isaac.core_nodes.IsaacArticulationState"),
                ("publish_joint_state", "omni.isaac.ros2_bridge.ROS2PublishJointState"),
                # ── IMU ────────────────────────────────────────────────────
                ("read_imu", "omni.isaac.sensor.IsaacReadIMU"),
                ("publish_imu", "omni.isaac.ros2_bridge.ROS2PublishImu"),
                # ── cmd_vel subscriber → base controller ──────────────────
                ("sub_cmd_vel", "omni.isaac.ros2_bridge.ROS2SubscribeTwist"),
                (
                    "articulation_ctrl_base",
                    "omni.isaac.core_nodes.IsaacArticulationController",
                ),
                # ── arm/joint_commands subscriber → arm controller ─────────
                ("sub_arm_cmd", "omni.isaac.ros2_bridge.ROS2SubscribeJointState"),
                (
                    "articulation_ctrl_arm",
                    "omni.isaac.core_nodes.IsaacArticulationController",
                ),
                # ── TF tree ───────────────────────────────────────────────
                ("publish_tf", "omni.isaac.ros2_bridge.ROS2PublishTransformTree"),
            ],
            keys.SET_VALUES: [
                # Clock
                ("publish_clock.inputs:topicName", "/clock"),
                # Odometry
                ("compute_odom.inputs:chassisPrim", ROBOT_PRIM),
                ("publish_odom.inputs:topicName", "/odom"),
                ("publish_odom.inputs:frameId", "odom"),
                ("publish_odom.inputs:childFrameId", "base_footprint"),
                # Joint states
                ("read_joint_state.inputs:robotPath", ROBOT_PRIM),
                ("publish_joint_state.inputs:topicName", "/joint_states"),
                # IMU
                ("read_imu.inputs:imuPrim", f"{ROBOT_PRIM}/imu_link/imu_sensor"),
                ("publish_imu.inputs:topicName", "/imu/data"),
                ("publish_imu.inputs:frameId", "imu_link"),
                # cmd_vel
                ("sub_cmd_vel.inputs:topicName", "/cmd_vel"),
                ("articulation_ctrl_base.inputs:robotPath", ROBOT_PRIM),
                (
                    "articulation_ctrl_base.inputs:jointNames",
                    [
                        "front_left_wheel_joint",
                        "front_right_wheel_joint",
                        "rear_left_wheel_joint",
                        "rear_right_wheel_joint",
                    ],
                ),
                ("articulation_ctrl_base.inputs:usePath", True),
                # arm/joint_commands
                ("sub_arm_cmd.inputs:topicName", "/arm/joint_commands"),
                ("articulation_ctrl_arm.inputs:robotPath", ROBOT_PRIM),
                (
                    "articulation_ctrl_arm.inputs:jointNames",
                    [
                        "arm_shoulder_pan",
                        "arm_shoulder_lift",
                        "arm_elbow_flex",
                        "arm_wrist_flex",
                        "arm_wrist_roll",
                        "arm_gripper",
                    ],
                ),
                ("articulation_ctrl_arm.inputs:usePath", True),
                # TF
                ("publish_tf.inputs:targetPrims", [ROBOT_PRIM]),
            ],
            keys.CONNECT: [
                # Tick → everything
                ("on_playback_tick.outputs:tick", "sim_time.inputs:execIn"),
                ("on_playback_tick.outputs:tick", "compute_odom.inputs:execIn"),
                ("on_playback_tick.outputs:tick", "read_joint_state.inputs:execIn"),
                ("on_playback_tick.outputs:tick", "read_imu.inputs:execIn"),
                ("on_playback_tick.outputs:tick", "publish_tf.inputs:execIn"),
                ("on_playback_tick.outputs:tick", "sub_cmd_vel.inputs:execIn"),
                ("on_playback_tick.outputs:tick", "sub_arm_cmd.inputs:execIn"),
                # Clock chain
                ("sim_time.outputs:simulationTime", "publish_clock.inputs:timeStamp"),
                ("on_playback_tick.outputs:tick", "publish_clock.inputs:execIn"),
                # Odometry chain
                ("compute_odom.outputs:execOut", "publish_odom.inputs:execIn"),
                ("compute_odom.outputs:position", "publish_odom.inputs:position"),
                ("compute_odom.outputs:orientation", "publish_odom.inputs:orientation"),
                (
                    "compute_odom.outputs:linearVelocity",
                    "publish_odom.inputs:linearVelocity",
                ),
                (
                    "compute_odom.outputs:angularVelocity",
                    "publish_odom.inputs:angularVelocity",
                ),
                # Joint state chain
                (
                    "read_joint_state.outputs:execOut",
                    "publish_joint_state.inputs:execIn",
                ),
                (
                    "read_joint_state.outputs:jointNames",
                    "publish_joint_state.inputs:jointNames",
                ),
                (
                    "read_joint_state.outputs:jointPositions",
                    "publish_joint_state.inputs:jointPositions",
                ),
                (
                    "read_joint_state.outputs:jointVelocities",
                    "publish_joint_state.inputs:jointVelocities",
                ),
                # IMU chain
                ("read_imu.outputs:execOut", "publish_imu.inputs:execIn"),
                ("read_imu.outputs:angVel", "publish_imu.inputs:angularVelocity"),
                ("read_imu.outputs:linAcc", "publish_imu.inputs:linearAcceleration"),
                ("read_imu.outputs:orientation", "publish_imu.inputs:orientation"),
                # cmd_vel → base articulation
                ("sub_cmd_vel.outputs:execOut", "articulation_ctrl_base.inputs:execIn"),
                (
                    "sub_cmd_vel.outputs:linearVelocity",
                    "articulation_ctrl_base.inputs:targetVelocities",
                ),
                # arm cmd → arm articulation
                ("sub_arm_cmd.outputs:execOut", "articulation_ctrl_arm.inputs:execIn"),
                (
                    "sub_arm_cmd.outputs:positionCommand",
                    "articulation_ctrl_arm.inputs:targetPositions",
                ),
            ],
        },
    )
    return graph


def _add_camera_publishers():
    """Add ROS2CameraHelper nodes for each camera in the robot."""
    cameras = [
        (
            "front_cam",
            f"{ROBOT_PRIM}/front_camera_link",
            "/camera/front/image_raw",
            "rgb",
        ),
        ("rear_cam", f"{ROBOT_PRIM}/rear_camera_link", "/camera/rear/image_raw", "rgb"),
        ("left_cam", f"{ROBOT_PRIM}/left_camera_link", "/camera/left/image_raw", "rgb"),
        (
            "right_cam",
            f"{ROBOT_PRIM}/right_camera_link",
            "/camera/right/image_raw",
            "rgb",
        ),
        (
            "wrist_cam",
            f"{ROBOT_PRIM}/wrist_camera_link",
            "/camera/wrist/image_raw",
            "rgb",
        ),
        ("depth_cam", f"{ROBOT_PRIM}/depth_camera_link", "/camera/depth", "depth"),
    ]
    keys = og.Controller.Keys
    for name, prim, topic, cam_type in cameras:
        og.Controller.edit(
            GRAPH_PATH,
            {
                keys.CREATE_NODES: [
                    (f"cam_helper_{name}", "omni.isaac.ros2_bridge.ROS2CameraHelper"),
                ],
                keys.SET_VALUES: [
                    (f"cam_helper_{name}.inputs:cameraPrim", prim),
                    (f"cam_helper_{name}.inputs:topicName", topic),
                    (f"cam_helper_{name}.inputs:type", cam_type),
                    (f"cam_helper_{name}.inputs:frameId", prim.split("/")[-1]),
                ],
                keys.CONNECT: [
                    (
                        "on_playback_tick.outputs:tick",
                        f"cam_helper_{name}.inputs:execIn",
                    ),
                ],
            },
        )


if __name__ == "__main__":
    print("[setup_omnigraph] Creating ROS 2 OmniGraph for OmniBot ...")
    graph = _create_graph()
    _add_camera_publishers()
    print(f"[setup_omnigraph] Graph created at: {GRAPH_PATH}")
    print("[setup_omnigraph] Press Play in Isaac Sim to start publishing topics.")
