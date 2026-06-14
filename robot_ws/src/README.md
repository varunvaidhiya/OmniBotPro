# OmniBot ROS 2 Packages

This directory contains the ROS 2 packages for the OmniBot mecanum-wheel
mobile-manipulation robot (ROS 2 Jazzy, Ubuntu 24.04).

## Packages

| Package | Purpose |
|---|---|
| `omnibot_bringup` | Launch files, RViz/Gazebo config (no Python nodes) |
| `omnibot_driver` | Yahboom serial motor driver (`yahboom_controller_node`) |
| `omnibot_description` | URDF/xacro, meshes, USD assets for RViz/Gazebo/Isaac Sim |
| `omnibot_navigation` | Nav2, SLAM (slam_toolbox), RTAB-Map 3-D mapping, EKF, waypoints |
| `omnibot_vla` | OpenVLA Vision-Language-Action node (desktop GPU) |
| `omnibot_arm` | SO-101 6-DOF arm driver (LeRobot / Feetech STS3215) |
| `omnibot_hybrid` | cmd_vel mux + arm cmd mux + mission planner + rosbag recorder |
| `omnibot_lerobot` | SmolVLA unified 9-DOF policy + teleop recorder + BEV stitcher |
| `omnibot_rl` | RL inference nodes (nav + arm) + ArUco object pose + arm_cmd_mux |
| `omnibot_orchestration` | LangGraph AI orchestration (Claude-backed natural language) |
| `omnibot_perception` | AI perception: object distance + pose from depth camera |
| `omnibot_metrics` | Prometheus metrics bridge (ROS 2 topics → `/metrics`) |
| `omnibot_ota` | Over-the-air update agent (workspace + ONNX model updates) |
| `omnibot_vr` | VR control bridge (Meta Quest episode upload + recording signals) |
| `omnibot_firmware` | Legacy STM32 firmware — **not a ROS package, not active** |
| `ros2_astra_camera` | Placeholder for the Orbbec Astra driver (built from source) |

> `ros2_astra_camera/` is an empty placeholder directory. The Orbbec Astra
> driver is not vendored — clone and build it from
> <https://github.com/orbbec/ros2_astra_camera> (see
> `omnibot_navigation/launch/rtabmap.launch.py`).

See the top-level `CLAUDE.md` and each package's own README for details on
nodes, parameters, topics, and launch commands.
