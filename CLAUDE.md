# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with
code in this repository. Keep it up to date when adding new packages, topics,
parameters, or conventions.

---

## Project Overview

**OmniBot** is a ROS 2 mecanum-wheel mobile-manipulation robot with embodied AI
(OpenVLA / SmolVLA). The repo is a monorepo with these top-level areas:

| Directory | Purpose |
|---|---|
| `robot_ws/` | ROS 2 workspace (Jazzy, Ubuntu 24.04) |
| `packages/` | Standalone Python packages (shared, not ROS-dependent) |
| `vla_engine/` | PyTorch VLA training/inference (no ROS) |
| `data_engine/` | Episode-based dataset collection pipeline |
| `lerobot_engine/` | Direct LeRobot training/recording/inference scripts (no ROS) |
| `rl_engine/` | Isaac Lab RL training + ONNX export for sim-to-real |
| `digital_twin/` | Contributor simulation environment (Gazebo, Isaac Sim, Foxglove) |
| `android_app/` | Kotlin MVVM Android controller (ROSBridge WebSocket) |
| `infra/` | Docker, DevContainers, CI/CD scripts, observability stack |

Hardware platform:
- **Robot brain**: Raspberry Pi 5 8 GB (runs all ROS 2 nodes except VLA)
- **AI brain**: Desktop PC with NVIDIA GPU ≥ 16 GB VRAM (runs VLA nodes)
- **Motor board**: Yahboom ROS Robot Expansion Board (primary)
- **Arm**: SO-101 6-DOF arm with Feetech STS3215 servos (LeRobot)
- See `MIGRATION_GUIDE.md` for the legacy STM32 → Yahboom migration.

---

## Repository Layout

```
Mecanum-Wheel-Robot/
├── robot_ws/
│   └── src/
│       ├── omnibot_bringup/       # Launch files, RViz/Gazebo config
│       ├── omnibot_driver/        # Yahboom serial driver (primary)
│       ├── omnibot_description/   # URDF/xacro, meshes
│       ├── omnibot_navigation/    # SLAM, Nav2, EKF, waypoints
│       ├── omnibot_vla/           # OpenVLA ROS 2 node
│       ├── omnibot_arm/           # SO-101 arm driver (LeRobot)
│       ├── omnibot_hybrid/        # cmd_vel mux + mission planner
│       ├── omnibot_lerobot/       # SmolVLA unified 9-DOF policy
│       ├── omnibot_rl/            # RL inference nodes (nav + arm) + arm_cmd_mux
│       ├── omnibot_orchestration/ # LangGraph AI orchestration (Claude-backed)
│       └── omnibot_firmware/      # Legacy STM32 (not active)
├── packages/
│   ├── yahboom_ros2/              # Pure-Python Yahboom protocol encoder/decoder
│   ├── vla_serve/                 # FastAPI VLA inference server
│   ├── robot_episode_dataset/     # LeRobot-format dataset helpers
│   ├── ros2_bev_stitcher/         # BEV (bird's-eye-view) image stitcher
│   └── mecanum_drive_ros2/        # C++17 + Python mecanum kinematics library
├── vla_engine/
│   ├── inference/server.py        # FastAPI server wrapping OpenVLA
│   ├── models/openvla.py          # OpenVLA model wrapper
│   └── tests/
├── data_engine/
│   ├── schema/constants.py        # Action/state/camera specs
│   ├── ingestion/                 # ROS bag → LeRobot format
│   └── tests/
├── lerobot_engine/
│   ├── train.py                   # Direct LeRobot policy training
│   ├── record.py                  # Direct LeRobot episode recording
│   ├── infer.py                   # Direct LeRobot inference
│   └── requirements.txt
├── rl_engine/
│   ├── requirements.txt           # isaaclab, onnxruntime-gpu, torch
│   ├── config/                    # PPO hyperparams + domain randomization YAMLs
│   ├── envs/                      # Isaac Lab ManagerBasedRLEnvCfg (nav + arm)
│   ├── tasks/mdp/                 # Actions, observations, rewards, terminations
│   ├── export/export_policy.py    # Checkpoint → ONNX opset 17 + TorchScript
│   └── scripts/                   # train_nav.py, train_arm.py
├── digital_twin/
│   ├── worlds/omnibot_lab.sdf     # Rich indoor lab world (table, shelf, YCB objects)
│   ├── scenarios/                 # VLA/nav benchmark scenario definitions
│   ├── docker/                    # Dockerfile.sim + docker-compose.yml
│   ├── configs/                   # RViz configs (perception/navigation/manipulation) + Foxglove layout
│   └── scripts/                   # build_usd.sh (URDF→Isaac Sim USD), setup_omnigraph.py
├── android_app/                   # Kotlin MVVM app (ROSBridge WebSocket)
├── data_engine/
│   └── isaac_sim/                 # Isaac Sim episode collection (Replicator)
├── confirmed_protocol.py          # Yahboom protocol reference (root debug script)
├── deploy.py                      # Deployment mode configurator (single/multi)
├── deployment.env.example         # Template for deployment.env
├── network.env                    # Cross-machine DDS peer IPs (edit before multi-machine use)
├── docker-compose.yml             # Full-stack Docker Compose (robot + vla + rosbridge services)
├── launch_simulation.sh           # Convenience build+launch for Gazebo (single or multi)
├── launch_teleop.sh               # Convenience teleop launcher (sources network.env for DDS peers)
├── launch_rosbridge.sh            # Start ROSBridge WebSocket server for Android app
├── launch_mobile_manipulation.sh  # Mobile manipulation bringup (base + arm + cameras + rosbridge)
├── launch_sim_pc.sh               # PC2 launch script (multi-workstation mode)
└── fuzz_*.py, scan_*.py, ...      # Hardware debug scripts — NOT part of ROS
```

> **Root-level `*.py` files** (`fuzz_*.py`, `scan_*.py`, `test_*.py`,
> `analyze_*.py`, `confirmed_protocol.py`, etc.) are hardware debugging/
> protocol-reverse-engineering scripts. Do not treat them as ROS nodes or tests.
>
> **`deploy.py`** is the exception — it is the deployment configurator and
> should be run as `python deploy.py` before first launch.

---

## Build & Test

```bash
# Build entire ROS 2 workspace
cd robot_ws
colcon build --symlink-install
source install/setup.bash

# Run all tests
colcon test
colcon test-result --verbose

# Single-package test
colcon test --packages-select omnibot_driver
colcon test-result --verbose --test-result-base build/omnibot_driver

# Python standalone packages (from repo root)
pip install -e packages/yahboom_ros2
pip install -e packages/vla_serve

# VLA engine tests
cd vla_engine && pytest tests/

# Data engine tests
cd data_engine && pytest tests/
```

---

## Deployment Configuration

Run once before first launch to set up single vs multi-workstation mode:

```bash
python deploy.py              # interactive menu
python deploy.py --mode single                          # non-interactive
python deploy.py --mode multi --vla-ip 192.168.1.100 \
    --pi-ip 192.168.1.101 --sim-ip 192.168.1.102       # non-interactive
python deploy.py --show       # print current config
```

Writes `deployment.env` which all launch scripts source automatically.

**Deployment modes:**
- `single` — all nodes on one workstation (e.g. RTX 5090). No DDS peers set.
- `multi` — three machines: Pi (robot) + PC1 (VLA inference) + PC2 (Isaac Sim / Gazebo).

---

## Launch Commands

```bash
# ── Perception (all cameras + BEV + depth→scan) ────────────────────────────
# Run on Pi — starts 5 USB cams, Astra Pro, BEV stitcher, Foxglove bridge
ros2 launch omnibot_bringup perception.launch.py
# With local RViz (Pi has a display)
ros2 launch omnibot_bringup perception.launch.py rviz:=true
# View camera feeds + BEV + point cloud on workstation (ROS_DOMAIN_ID=30 must match)
ros2 launch omnibot_bringup perception_viewer.launch.py

# ── SLAM + 3-D mapping (run after perception.launch.py) ───────────────────
# 2-D slam_toolbox + RTAB-Map 3-D + OctoMap — run on Pi
ros2 launch omnibot_bringup slam_3d_mapping.launch.py
# Headless Pi — view SLAM + 3-D map on workstation
ros2 launch omnibot_bringup slam_3d_mapping.launch.py rviz:=false
ros2 launch omnibot_bringup slam_3d_viewer.launch.py    # workstation
# 2-D SLAM only (no RTAB-Map / OctoMap)
ros2 launch omnibot_bringup slam_3d_mapping.launch.py slam3d:=false
# Localization against a saved map
ros2 launch omnibot_bringup slam_3d_mapping.launch.py slam_mode:=localization map_file:=/path/to/omnibot_map

# ── Basic robot: driver + state publisher
ros2 launch omnibot_bringup robot.launch.py

# Robot + Xbox controller (also available as ./launch_teleop.sh which sources network.env)
ros2 launch omnibot_bringup robot_with_joy.launch.py

# Xbox controller teleoperation only
ros2 launch omnibot_bringup joy_teleop.launch.py

# Gazebo simulation + RViz
./launch_simulation.sh           # convenience: builds, sources, then launches (ROS_DOMAIN_ID=30)
ros2 launch omnibot_bringup simulation.launch.py   # direct

# Mobile manipulation (base + arm + cameras + rosbridge)
./launch_mobile_manipulation.sh  # convenience: auto-builds, checks deps, starts rosbridge
ros2 launch omnibot_bringup mobile_manipulation.launch.py   # direct

# ROSBridge WebSocket (required for Android app, port 9090)
./launch_rosbridge.sh            # convenience: shows local IP for Android config
ros2 launch rosbridge_server rosbridge_websocket_launch.xml port:=9090

# Isaac Sim companion nodes (bev_stitcher + RViz) — Isaac Sim must already be running
ros2 launch omnibot_bringup isaac_sim.launch.py

# PC2 simulation workstation (multi mode) — runs bev_stitcher + RViz
./launch_sim_pc.sh

# Autonomous navigation with SLAM
ros2 launch omnibot_navigation autonomous_robot.launch.py

# Full hybrid robot (driver + SLAM + Nav2 + VLA + mux + mission planner)
ros2 launch omnibot_hybrid hybrid_robot.launch.py

# SmolVLA mobile-manipulation inference
ros2 launch omnibot_lerobot smolvla_inference.launch.py

# OpenVLA inference (NVIDIA GPU, ≥ 16 GB VRAM)
ros2 launch omnibot_vla vla_desktop.launch.py

# SO-101 arm driver
ros2 launch omnibot_arm arm.launch.py

# Send a VLA prompt
ros2 topic pub --once /vla/prompt std_msgs/msg/String "data: 'Find the red cup'"

# Send a mission command (hybrid mode)
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'navigate:kitchen,vla:find the red cup'"

# Full hybrid robot WITH RL inference nodes enabled
ros2 launch omnibot_hybrid hybrid_robot.launch.py use_rl:=true

# RL inference nodes only (4 nodes: rl_nav, rl_arm, arm_cmd_mux, rl_object_pose)
ros2 launch omnibot_rl rl_inference.launch.py

# Train navigation RL policy in Isaac Lab (requires Isaac Sim + Isaac Lab)
python rl_engine/scripts/train_nav.py --num_envs 512

# Train arm RL policy in Isaac Lab
python rl_engine/scripts/train_arm.py --num_envs 256

# Export trained policy to ONNX (nav: 27D obs → 3D act)
python rl_engine/export/export_policy.py \
  --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \
  --output ~/models/omnibot_nav_policy.onnx --type nav

# Switch base to RL navigation mode (short-range goal approach ≤ 3 m)
ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'rl_nav'"
ros2 topic pub --once /rl_nav/goal geometry_msgs/msg/PoseStamped \
  "{header: {frame_id: 'map'}, pose: {position: {x: 1.0, y: 0.0}, orientation: {w: 1.0}}}"

# Switch arm to RL control mode (precision pick/place, triggered by mission planner)
ros2 topic pub --once /arm/cmd_mode std_msgs/msg/String "data: 'rl_arm'"

# Send RL nav + arm mission
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'rl_nav:kitchen,rl_arm:pick up the cup'"
```

---

## Architecture & Data Flow

### High-level diagram

```
Android App  ──ROSBridge ws://robot:9090──►  ROS 2 Network (Raspberry Pi 5)
Xbox Joy     ──/joy──────────────────────►      │
                                                ├─ yahboom_controller_node
                                                │    /cmd_vel → serial → motors
                                                │    encoders/IMU → /odom, /imu/data
                                                │
                                                ├─ cmd_vel_mux  (/cmd_vel/out)
                                                │    modes: nav2 | vla | teleop | rl_nav
                                                │
                                                ├─ arm_cmd_mux  (/arm/joint_commands/out)
                                                │    modes: smolvla (default) | rl_arm
                                                │
                                                ├─ mission_planner
                                                │    parses "navigate:X,vla:Y" commands
                                                │    parses "rl_nav:X,rl_arm:Y" commands
                                                │
                                                ├─ slam_toolbox + Nav2 → /cmd_vel
                                                │
                                                └─ arm_driver_node
                                                     /arm/joint_commands/out → Feetech bus

VLA Desktop (GPU PC)
  OpenVLA node  ← /image_raw + /vla/prompt  →  /cmd_vel/vla
  SmolVLA node  ← /camera/wrist + /camera/base/bev + /arm/joint_states + /odom
               →  /arm/joint_commands + /cmd_vel
  rl_nav_node   ← /odom + /camera/depth/points + /rl_nav/goal
               →  /cmd_vel/rl  (ONNX, 20 Hz, 27D obs → 3D act)
  rl_arm_node   ← /arm/joint_states + /rl_arm/target_pose
               →  /arm/joint_commands/rl  (ONNX, 20 Hz, 30D obs → 6D act)
  rl_object_pose_node ← /camera/wrist/image_raw + /camera/depth/image_raw
               →  /rl_arm/target_pose + /rl_arm/target_detected  (ArUco)
```

### Complete topic map

| Topic | Type | Publisher | Subscriber(s) |
|---|---|---|---|
| `/cmd_vel` | Twist | Nav2, teleop_twist_joy, Android | `yahboom_controller_node`, `cmd_vel_mux` |
| `/cmd_vel/vla` | Twist | `vla_node`, `smolvla_node` | `cmd_vel_mux` |
| `/cmd_vel/teleop` | Twist | `teleop_twist_joy` | `cmd_vel_mux` |
| `/cmd_vel/out` | Twist | `cmd_vel_mux` | `yahboom_controller_node` (via remap) |
| `/control_mode` | String | `mission_planner`, manual pub | `cmd_vel_mux` |
| `/control_mode/active` | String | `cmd_vel_mux` | monitoring |
| `/vla/prompt` | String | Android, `mission_planner` | `vla_node` |
| `/smolvla/task` | String | `mission_planner` | `smolvla_node` |
| `/smolvla/enable` | Bool | `mission_planner` | `smolvla_node` |
| `/mission/command` | String | Android, manual pub | `mission_planner` |
| `/mission/cancel` | String | Android | `mission_planner` |
| `/mission/status` | String | `mission_planner` | Android |
| `/odom` | Odometry | `yahboom_controller_node` | Nav2, `smolvla_node`, Android |
| `/imu/data` | Imu | `yahboom_controller_node` | Nav2 EKF, Android |
| `/map` | OccupancyGrid | `slam_toolbox` | Nav2, Android |
| `/tf` | TFMessage | `robot_state_publisher`, driver | all navigation |
| `/joint_states` | JointState | `robot_state_publisher`, Gazebo | `robot_state_publisher` |
| `/arm/joint_states` | JointState | `arm_driver_node` | `smolvla_node`, Android |
| `/arm/leader_states` | JointState | `arm_driver_node` (teleop) | `teleop_recorder_node` |
| `/arm/joint_commands` | JointState | Android, `smolvla_node` | `arm_cmd_mux` |
| `/arm/joint_commands/rl` | JointState | `rl_arm_node` | `arm_cmd_mux` |
| `/arm/joint_commands/out` | JointState | `arm_cmd_mux` | `arm_driver_node` |
| `/arm/cmd_mode` | String | `mission_planner`, manual pub | `arm_cmd_mux` |
| `/arm/cmd_mode/active` | String | `arm_cmd_mux` | monitoring |
| `/arm/enable` | Bool | Android | `arm_driver_node` |
| `/cmd_vel/rl` | Twist | `rl_nav_node` | `cmd_vel_mux` |
| `/rl_nav/goal` | PoseStamped | `mission_planner`, manual pub | `rl_nav_node` |
| `/rl_arm/target_pose` | PoseStamped | `rl_object_pose_node` | `rl_arm_node` |
| `/rl_arm/target_detected` | Bool | `rl_object_pose_node` | `rl_arm_node` |
| `/emergency_stop` | Bool | Android | `yahboom_controller_node` |
| `/robot_mode` | String | Android | *(monitoring only — does not control mux)* |
| `/control_mode` | String | Android, `mission_planner`, manual pub | `cmd_vel_mux` |
| `/joy` | Joy | `joy_node` | `yahboom_controller_node`, `teleop_recorder_node` |
| `/camera/front/image_raw` | Image | USB camera / Gazebo | `vla_node` (via `/image_raw` remap), `langchain_agent_node` |
| `/camera/wrist/image_raw` | Image | wrist camera | `smolvla_node`, `teleop_recorder_node`, `rl_object_pose_node`, `langchain_agent_node` |
| `/camera/base/bev/image_raw` | Image | `bev_stitcher_node` | `smolvla_node`, `teleop_recorder_node` |
| `/ai/command` | String | Android, manual pub | `langchain_agent_node` |
| `/ai/status` | String | `langchain_agent_node` | Android |
| `/ai/response_needed` | String | `langchain_agent_node` | Android |

**Nav2 action server**: `navigate_to_pose` (NavigateToPose) — used by `mission_planner`.

---

## ROS 2 Packages

### `omnibot_driver`

Primary motor driver. **`yahboom_controller_node.py`** is the active node;
`mecanum_controller_node.py` and `serial_bridge_node.py` are secondary/legacy.

**Declared parameters** (`yahboom_controller_node.py`):

| Parameter | Default | Description |
|---|---|---|
| `wheel_radius` | `0.04` | metres |
| `wheel_separation_width` | `0.215` | left↔right, metres |
| `wheel_separation_length` | `0.165` | front↔rear, metres |
| `serial_port` | `'/dev/ttyUSB0'` | Yahboom USB serial |
| `baud_rate` | `115200` | |
| `debug_serial` | `False` | verbose serial logging |

Safety constants (hardcoded): max linear `0.2 m/s`, ramp step `0.05 m/s`,
update rate `20 Hz`.
Debug log: `/home/varunvaidhiya/yahboom_debug.log` (fails silently if absent).

**`serial_bridge_node.py`** (STM32 legacy bridge, also used for odometry from
encoder packets) — same geometry parameters plus `odom_frame`/`base_frame`.
Encoder packet format: `<ENCODERS,fl,fr,rl,rr>` where values are rad/s.
Publishes `/odom` + broadcasts `odom→base_link` TF.

### `omnibot_bringup`

Launch files only — no Python nodes.

Both `robot.launch.py` and `yahboom_controller_node.py` use `wheel_separation_length` /
`wheel_separation_width` — keep these in sync when either file is changed.

Xbox teleop defaults (`xbox_teleop.yaml`):
- Enable: button 5 (RB), Turbo: button 7 (RT)
- Linear scale: 0.125 m/s normal / 0.25 m/s turbo
- Angular scale: 0.25 rad/s normal / 0.5 rad/s turbo
- Joystick deadzone: 0.1

### `omnibot_navigation`

Nav2 + SLAM + EKF. Config files live in `config/`:
`nav2_params.yaml`, `slam_toolbox_params.yaml`, `robot_localization.yaml`,
`rtabmap_params.yaml`, `octomap_params.yaml`, `waypoints.yaml`.

AMCL uses `OmnidirectionalMotionModel` (correct for mecanum).

### `omnibot_vla`

OpenVLA ROS 2 node. Runs on the desktop GPU machine, not the Pi.

**Declared parameters**:

| Parameter | Default |
|---|---|
| `model_path` | `'openvla/openvla-7b'` |
| `device` | `'cuda'` |
| `load_in_4bit` | `False` |

Subscribes `/image_raw` (hardcoded — not parameterised).
Action mapping: `action[0]→linear.x`, `action[1]→linear.y`, `action[5]→angular.z`.
Inference rate: 1 Hz timer.

### `omnibot_arm`

SO-101 6-DOF arm via LeRobot's `FeetechMotorsBus`.

**Declared parameters**:

| Parameter | Default |
|---|---|
| `follower_port` | `'/dev/ttyACM0'` |
| `leader_port` | `'/dev/ttyACM1'` |
| `baudrate` | `1000000` |
| `publish_rate` | `100.0` Hz |
| `teleop_mode` | `False` |
| `ticks_per_rev` | `4096` |
| `joint_names` | `['arm_shoulder_pan','arm_shoulder_lift','arm_elbow_flex','arm_wrist_flex','arm_wrist_roll','arm_gripper']` |
| `motor_ids` | `[1,2,3,4,5,6]` |
| `home_ticks` | `[2048,2048,2048,2048,2048,2048]` |
| `joint_min` | `[-3.14,-1.57,-1.57,-1.57,-3.14,-0.1]` |
| `joint_max` | `[3.14,1.57,1.57,1.57,3.14,0.8]` |

Joint names include the `arm_` prefix in both `arm_driver_node.py` defaults and
`smolvla_node.py` — they are kept in sync. Do not change one without the other.

Falls back to passthrough/simulation mode if `lerobot` is not installed.

### `omnibot_hybrid`

**`cmd_vel_mux.py`** — routes one of four `/cmd_vel` sources to `/cmd_vel/out`.

| Parameter | Default |
|---|---|
| `default_mode` | `'nav2'` |

Valid `control_mode` strings: `"nav2"`, `"vla"`, `"teleop"`, `"rl_nav"` (any
other value is silently ignored — the active mode does not change).

**`mission_planner.py`** — high-level state machine.

Command format on `/mission/command`:
```
"navigate:kitchen,vla:find the red cup"    # navigate (Nav2) then VLA
"navigate:kitchen"                          # navigate only
"vla:find the red cup"                      # VLA only
"rl_nav:kitchen,rl_arm:pick up the cup"    # RL nav then RL arm
"rl_nav:kitchen"                            # RL nav only
```

Named locations loaded from `config/named_locations.yaml`
(keys: location name → `{x, y, yaw}`).

State machine: `idle → navigating → vla → done → idle`
              `idle → rl_navigating → rl_arm → done → idle`

Publishers added for RL: `/rl_nav/goal` (PoseStamped), `/arm/cmd_mode` (String).

### `omnibot_lerobot`

**`smolvla_node.py`** — unified 9-DOF SmolVLA policy (6 arm + 3 base).

| Parameter | Default |
|---|---|
| `checkpoint_path` | `'lerobot/smolvla_base'` |
| `device` | `'cuda'` |
| `policy_hz` | `10.0` |
| `chunk_size` | `50` |
| `state_dim` / `action_dim` | `9` |
| `image_width` / `image_height` | `320` / `240` |
| `task_description` | `'pick up the object and place it'` |
| `base_vel_scale` | `0.3` |

Both `/camera/wrist/image_raw` **and** `/camera/base/bev/image_raw` must be
available — no graceful degradation if either is missing.
`bev_stitcher_node` must be running to provide the BEV topic.

**`teleop_recorder_node.py`** — records leader arm + base for imitation learning.

| Parameter | Default |
|---|---|
| `output_dir` | `'~/datasets/mobile_manipulation'` |
| `repo_id` | `'local/mobile_manipulation'` |
| `record_hz` | `30.0` |
| `episode_timeout_s` | `60.0` |
| `joy_record_button` | `7` (RB) |
| `joy_discard_button` | `6` (LB) |

Max recorded speeds: `MAX_LINEAR=0.2 m/s`, `MAX_ANGULAR=1.0 rad/s`.

### `omnibot_rl`

Four ROS 2 nodes for RL policy inference (ONNX Runtime, 20 Hz).

**`rl_nav_node.py`** — base navigation RL policy.

| Parameter | Default |
|---|---|
| `policy_path` | `'~/models/omnibot_nav_policy.onnx'` |
| `policy_hz` | `20.0` |
| `goal_tolerance` | `0.25` m |
| `max_lin_vel` | `0.20` m/s |
| `max_ang_vel` | `1.00` rad/s |
| `lidar_min` | `0.30` m |
| `lidar_max` | `3.00` m |

Obs (27D): goal_rel_xy (2), base_vel (3), 8-sector lidar (8), prev_action (3),
dist/yaw_error/heading (3), lidar replicated (8).
Action (3D): velocity deltas (Δvx, Δvy, Δω), accumulated and clipped to hardware limits.
Synthesizes 8-sector lidar from `/camera/depth/points` (XY plane, z ∈ [0.05, 1.5] m).
Active only when `/control_mode/active == "rl_nav"`.

**`rl_arm_node.py`** — arm precision control RL policy.

| Parameter | Default |
|---|---|
| `policy_path` | `'~/models/omnibot_arm_policy.onnx'` |
| `policy_hz` | `20.0` |
| `max_delta` | `0.05` rad/step |

Obs (30D): joint_pos_normalized (6), joint_vel (6), ee_pos (3), ee_rot6d (6),
target_pos (3), prev_action (6).
Action (6D): joint position deltas, clipped to ±0.05 rad/step, enforced within URDF limits.
Active only when `/arm/cmd_mode == "rl_arm"`.

**`arm_cmd_mux.py`** — arm command multiplexer (mirrors cmd_vel_mux design).

Valid modes: `"smolvla"` (default, transparent pass-through), `"rl_arm"`.
Inputs: `/arm/joint_commands` (SmolVLA), `/arm/joint_commands/rl` (RL arm node).
Output: `/arm/joint_commands/out` → `arm_driver_node`.
Mode feedback on `/arm/cmd_mode/active` at 1 Hz.

**`rl_object_pose_node.py`** — ArUco marker-based object pose estimator.

| Parameter | Default |
|---|---|
| `aruco_dict_id` | `4` (DICT_4X4_50) |
| `marker_id` | `0` |
| `marker_size_m` | `0.05` |
| `camera_frame` | `'wrist_camera_link'` |

Subscribes `/camera/wrist/image_raw` + `/camera/depth/image_raw`.
Publishes `/rl_arm/target_pose` (PoseStamped in `base_link`) and
`/rl_arm/target_detected` (Bool). Holds last pose for 0.5 s after detection loss.
Uses TF2 to transform wrist_camera_link → base_link.

Config files: `robot_ws/src/omnibot_rl/config/rl_nav_params.yaml`,
`robot_ws/src/omnibot_rl/config/rl_arm_params.yaml`.
Launch: `robot_ws/src/omnibot_rl/launch/rl_inference.launch.py`.

---

## RL Engine (`rl_engine/`)

Isaac Lab training infrastructure for sim-to-real RL. Requires Isaac Sim + Isaac Lab
(not part of the ROS build — run separately on the GPU PC).

```
rl_engine/
├── requirements.txt          # isaaclab>=1.1.0, onnxruntime-gpu>=1.16, torch>=2.1
├── config/
│   ├── domain_randomization.yaml   # Per-episode randomization ranges
│   ├── nav_train.yaml              # PPO hyperparams + curriculum for nav
│   └── arm_train.yaml              # PPO hyperparams + curriculum for arm
├── envs/
│   ├── omnibot_nav_env.py          # OmnibotNavEnvCfg (27D obs, 3D act, 512 envs)
│   └── omnibot_arm_env.py          # OmnibotArmEnvCfg (30D obs, 6D act, 256 envs)
├── tasks/mdp/
│   ├── actions.py        # MecanumWheelActionTerm, ArmJointDeltaActionTerm
│   ├── observations.py   # LidarSectorObsTerm, GoalRelativeObsTerm, ArmNormJointPos
│   ├── rewards.py        # nav_goal_approach, arm_ee_approach, grasp/lift/place bonuses
│   └── terminations.py   # collision, timeout, goal_reached, object_dropped
├── export/
│   └── export_policy.py  # --checkpoint → ONNX opset 17 or TorchScript
└── scripts/
    ├── train_nav.py       # AppLauncher → OmnibotNavEnvCfg → RSL-RL PPO
    └── train_arm.py       # AppLauncher → OmnibotArmEnvCfg → RSL-RL PPO
```

**Key design decisions:**
- Actions are velocity **deltas** (not absolute), matching Yahboom's 0.05 m/s ramp limiter.
- `MecanumWheelActionTerm` uses exact OmniBot constants: `lx=0.0825`, `ly=0.1075`, `r=0.04`.
- Arm actions are joint position deltas (±0.05 rad/step → maps to STS3215 Goal_Position).
- `ActionDelayTerm` (1–3 steps at 20 Hz = 50–150 ms) models servo bus latency.
- `physics_randomization` in `randomization_config.yaml` is disabled by default;
  set `enabled: true` only during Isaac Lab RL training.

**Training workflow:**
```bash
# 1. Train
python rl_engine/scripts/train_nav.py --num_envs 512 --max_iterations 2000
# 2. Export
python rl_engine/export/export_policy.py \
  --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \
  --output ~/models/omnibot_nav_policy.onnx --type nav
# 3. Deploy: copy .onnx to robot, launch with use_rl:=true
```

---

## Standalone Packages (`packages/`)

### `yahboom_ros2`

Pure-Python, no ROS dependency. Canonical protocol encoder/decoder.

```python
from yahboom_ros2.protocol import packet_motion, packet_beep, packet_set_car_type
```

Key constants: `HEAD_TX=0xFF`, `DEVICE_ID=0xFC`, `HEAD_RX=0xFB`, `CAR_TYPE_MECANUM_X3=1`.
RX type codes: `TYPE_VELOCITY=0x0C`, `TYPE_ACCEL=0x61`, `TYPE_GYRO=0x62`, `TYPE_ATTITUDE=0x63`.

### `vla_serve`

FastAPI VLA inference server. Configured entirely via environment variables:

| Env var | Default |
|---|---|
| `VLA_MODEL_CLASS` | `'OpenVLAModel'` |
| `VLA_MODEL_PATH` | `'openvla/openvla-7b'` |
| `VLA_LOAD_4BIT` | `''` (disabled) |
| `VLA_AUTO_LOAD` | `''` (disabled) |
| `VLA_PORT` | `8000` |

Endpoints: `GET /health`, `POST /load_model`, `POST /predict`.

### `ros2_bev_stitcher`

Stitches 4 base-mounted cameras into a single bird's-eye-view image.
Publishes `/camera/base/bev/image_raw`.
Must be running whenever `smolvla_node` or `teleop_recorder_node` is active.
Config: `packages/ros2_bev_stitcher/config/bev_params.yaml`.

### `mecanum_drive_ros2`

Hardware-agnostic mecanum kinematics — C++17 header-only library
(`include/mecanum_drive_ros2/mecanum_kinematics.hpp`) with a pure Python mirror
(`mecanum_drive_ros2.kinematics`). Provides inverse kinematics (body twist → wheel ω)
and forward kinematics (wheel ω → body twist) using the same physical constants
as the rest of the project.

```python
from mecanum_drive_ros2 import RobotGeometry, inverse_kinematics, forward_kinematics
geom = RobotGeometry(wheel_radius=0.04, wheel_separation_width=0.215,
                     wheel_separation_length=0.165)
fl, fr, bl, br = inverse_kinematics(vx=0.3, vy=0.0, omega=0.0, geom=geom)
```

---

## Digital Twin

`digital_twin/` is a contributor-ready simulation environment — no physical
hardware required.

### Quick-start options

| Option | Command |
|---|---|
| VS Code DevContainer (recommended) | Open folder → **Reopen in Container** |
| Docker Compose (headless) | `docker compose -f digital_twin/docker/docker-compose.yml up` then open Foxglove at `ws://localhost:8765` |
| Native Ubuntu 24.04 | `./launch_simulation.sh` |

### Contributor domain entry points

| Area | Launch | Visualization |
|---|---|---|
| Perception / Cameras | `simulation.launch.py` | `digital_twin/configs/rviz/perception.rviz` |
| SLAM / Mapping | `simulation.launch.py` + `slam_toolbox.launch.py` | `digital_twin/configs/rviz/navigation.rviz` |
| Nav2 | `simulation.launch.py` + `autonomous_robot.launch.py` | `digital_twin/configs/rviz/navigation.rviz` |
| Arm / Manipulation | `simulation.launch.py` | `digital_twin/configs/rviz/manipulation.rviz` |
| VLA / Training data | Isaac Sim + `isaac_sim.launch.py` | Foxglove |

Use `digital_twin/configs/nav2_sim_params.yaml` (not `robot_ws/.../nav2_params.yaml`)
for simulation-tuned Nav2 costmaps:

```bash
ros2 launch omnibot_navigation autonomous_robot.launch.py \
  params_file:=$(pwd)/digital_twin/configs/nav2_sim_params.yaml
```

### Lab world (richer than default)

```bash
ros2 launch omnibot_bringup simulation.launch.py \
  world:=$(pwd)/digital_twin/worlds/omnibot_lab.sdf
```

### Isaac Sim (VLA / training data)

```bash
bash digital_twin/scripts/build_usd.sh          # URDF → USD (run once per URDF change)
# Start Isaac Sim, then in Script Editor:
#   digital_twin/scripts/setup_omnigraph.py
ros2 launch omnibot_bringup isaac_sim.launch.py
python3 data_engine/isaac_sim/collect_episodes.py \
  --config data_engine/isaac_sim/randomization_config.yaml \
  --output ~/datasets/omnibot
```

---

## LeRobot Engine

`lerobot_engine/` provides standalone scripts for direct LeRobot interaction
without ROS. Install deps: `pip install -r lerobot_engine/requirements.txt`.

```bash
# Record demonstrations
python lerobot_engine/record.py

# Train a policy
python lerobot_engine/train.py

# Run inference
python lerobot_engine/infer.py
```

---

## Multi-Machine Networking

Edit `network.env` with the actual IPs before running cross-machine:

```bash
WORKSTATION_IP=192.168.1.100   # GPU desktop running VLA
PI_IP=192.168.1.101            # Raspberry Pi 5 running ROS
```

`launch_teleop.sh` and the other convenience scripts source this file and set
`ROS_STATIC_PEERS` automatically. `ROS_DOMAIN_ID=30` must match on all machines.

---

## Yahboom Serial Protocol

The Rosmaster serial protocol was reverse-engineered. `confirmed_protocol.py`
and `packages/yahboom_ros2/protocol.py` are the sources of truth.

```
TX packet: [0xFF, 0xFC, LEN, FUNC, PAYLOAD..., CHECKSUM]
  LEN      = 1 (device ID byte) + 1 (LEN byte) + 1 (FUNC byte) + N (payload bytes)
  CHECKSUM = (sum(all_packet_bytes) + 5) & 0xFF
             (5 = 257 - 0xFC, compensates for the header complement)
```

| Function | Code | Payload |
|---|---|---|
| `FUNC_BEEP` | `0x02` | `struct.pack('<H', ms)` — buzzer on-time in ms |
| `FUNC_MOTOR` | `0x10` | 4 × `int16` direct wheel velocities |
| `FUNC_MOTION` | `0x12` | `struct.pack('<bhhh', CAR_TYPE, vx×1000, vy×1000, w×1000)` |
| `FUNC_SET_CAR_TYPE` | `0x15` | `[CAR_TYPE]` — send once on startup (X3 = 1) |

RX packets start with `0xFB`. Parse by type code at byte index 3.

---

## `omnibot_orchestration`

LangGraph AI orchestration layer — converts natural language into structured
robot missions using Claude. Runs on the AI desktop PC alongside the VLA nodes.

**Node**: `langchain_agent_node` (launch: `langchain_agent.launch.py`)

**Declared parameters**:

| Parameter | Default |
|---|---|
| `anthropic_api_key` | `''` (reads `ANTHROPIC_API_KEY` env var) |
| `vla_serve_url` | `'http://localhost:8000'` |
| `locations_yaml` | `''` (auto-discovers from `omnibot_hybrid` share) |
| `entity_memory_path` | `'~/.omnibot/entity_memory.json'` |
| `use_claude_vision` | `True` |
| `langchain_tracing_v2` | `False` |

**Subscribes**: `/ai/command` (String), `/mission/status` (String),
`/camera/front/image_raw`, `/camera/wrist/image_raw`

**Publishes**: `/mission/command` → `mission_planner`, `/mission/cancel`,
`/ai/status` → Android, `/ai/response_needed` → Android

**Launch**:
```bash
export ANTHROPIC_API_KEY=sk-ant-...
# Standalone (alongside hybrid_robot.launch.py)
ros2 launch omnibot_orchestration langchain_agent.launch.py
# Or integrated via hybrid_robot:
ros2 launch omnibot_hybrid hybrid_robot.launch.py use_langchain:=true
```

**LangGraph tools**: `navigate_to_location`, `navigate_then_execute`,
`execute_vla_task`, `get_robot_status`, `cancel_current_mission`,
`list_available_locations`, `describe_current_scene`, `ask_human_for_clarification`

---

## Gazebo Simulation

- Simulator: Gazebo Harmonic (`ros_gz_sim` + `ros_gz_bridge`)
- Robot spawned as entity `'mecanum_bot'`
- Arm driver launches with a 4-second delay after the base
- `ROS_DOMAIN_ID=30` aligns with physical robot (set by `launch_simulation.sh`)
- Bridged topics: `/cmd_vel`, `/odom`, `/tf`, `/imu/data`, full `/camera/*` family,
  `/camera/depth/points`, `/joint_states`

---

## Android App

MVVM architecture with Hilt DI, OkHttp3 WebSocket, Kotlin coroutines.

**Connection**: `ws://<robot_ip>:9090` (ROSBridge v2 JSON protocol)
**Defaults**: IP `192.168.1.100`, port `9090`, timeout `10 000 ms`

**Published topics** (Android → robot):

| Topic | Type | Notes |
|---|---|---|
| `/cmd_vel/teleop` | Twist | rate: 20 Hz, clamped ±1.5 m/s / ±2.0 rad/s. Set `/control_mode` to `"teleop"` first. |
| `/control_mode` | String | set via `sendMode()` or `sendControlMode()` — drives `cmd_vel_mux` |
| `/emergency_stop` | Bool | zerors velocity immediately; hold Bool=false to clear |
| `/robot_mode` | String | monitoring only; does not control `cmd_vel_mux` |
| `/arm/joint_commands` | JointState | joint names must be `arm_*` prefixed; routes via `arm_cmd_mux` |
| `/arm/enable` | Bool | |
| `/vla/prompt` | String | |
| `/mission/command` | String | structured commands (`navigate:X,vla:Y`) |
| `/ai/command` | String | natural language → LangGraph agent (requires `use_langchain:=true`) |

**Subscribed topics** (robot → Android):

| Topic | Type |
|---|---|
| `/odom` | Odometry |
| `/map` | OccupancyGrid |
| `/imu/data` | Imu |
| `/diagnostics` | DiagnosticArray |
| `/arm/joint_states` | JointState |
| `/mission/status` | String |
| `/ai/status` | String (from `langchain_agent_node` when running) |
| `/ai/response_needed` | String (clarification requests from AI agent) |

Arm joint names in `Constants.kt`:
```kotlin
ARM_JOINT_NAMES = ["arm_shoulder_pan", "arm_shoulder_lift", "arm_elbow_flex",
                   "arm_wrist_flex", "arm_wrist_roll", "arm_gripper"]
```

ROSBridge reconnect policy: max 5 attempts, 1 s base delay (exponential back-off).

**MJPEG camera view** (`MjpegView.kt`): connects to `web_video_server`, uses
`Content-Length`-based multipart parsing with a JPEG-marker fallback.

---

## Data Engine & Dataset Format

Episodes are stored in LeRobot HF dataset format (Parquet + MP4), not rosbags.

**Canonical specs** (`data_engine/schema/constants.py`):

| Spec | Dimensions | Description |
|---|---|---|
| `STATE_SPEC` (base) | 10-D | x, y, θ, vel_x, vel_y, ω, motor ×4 |
| `ACTION_SPEC` (base) | 3-D | linear_x, linear_y, angular_z |
| `MOBILE_MANIP_STATE_SPEC` | 9-D | arm ×6 + base ×3 |
| `MOBILE_MANIP_ACTION_SPEC` | 9-D | arm ×6 + base ×3 |
| `CAMERA_FRONT` | 480×640, 30 fps | `/camera/front/image_raw`, bgr8 |
| `CAMERA_WRIST` | 240×320, 30 fps | `/camera/wrist/image_raw`, bgr8 |

Synchronization: `TopicSynchronizer` aligns streams with `sync_tolerance=0.05 s`.

---

## CI/CD

GitHub Actions (`.github/workflows/ros2_ci.yml`) on `ubuntu-24.04` + ROS 2 Jazzy:

1. Clone `serial-ros2` (RoverRobotics fork) via FetchContent — not a system package
2. `pip install numpy<2.0 torch opencv-python accelerate transformers`
3. `rosdep install --from-paths robot_ws/src --ignore-src -y`
4. `colcon build --symlink-install`
5. `colcon test`
6. `colcon test-result --verbose`

**Known gaps** (do not fix silently — open an issue first):
- Linting disabled in all `package.xml` (`ament_lint_auto` commented out)
- No coverage collection or minimum threshold
- No Gazebo integration tests

---

## Key Physical Constants

| Constant | Value | Where used |
|---|---|---|
| Wheel radius | 0.04 m | All kinematics |
| Wheel sep (width, L↔R) | 0.215 m | Odometry, mecanum IK |
| Wheel sep (length, F↔R) | 0.165 m | Odometry, mecanum IK |
| Servo ticks/rev | 4096 | Arm joint conversion |
| Arm baud rate | 1 000 000 bps | `/dev/ttyACM0` |
| Yahboom baud rate | 115 200 bps | `/dev/ttyUSB0` |
| VRAM requirement | ≥ 16 GB | OpenVLA (7B), desktop only |
| ROS Domain ID | 30 | Cross-machine DDS |

Mecanum forward kinematics (wheel ω in rad/s → body velocity):
```
vx    = (r/4)           × (fl + fr + rl + rr)
vy    = (r/4)           × (−fl + fr + rl − rr)
omega = r/(4×(lx+ly))  × (−fl + fr − rl + rr)
```
where `lx = wheel_separation_length/2`, `ly = wheel_separation_width/2`.

---

## Observability Stack

Grafana + Prometheus + Loki + Tempo — deployed on the GPU desktop via Docker Compose.

### Quick start (GPU Desktop)

```bash
# Start the full observability stack
docker compose --profile observability up -d

# Grafana dashboards: http://localhost:3000  (admin / omnibot)
# Prometheus:         http://localhost:9090
# AlertManager:       http://localhost:9093
```

### Pi setup (one-time)

```bash
GPU_DESKTOP_IP=192.168.1.100 bash infra/observability/setup_pi_agents.sh
# Then after building the workspace:
sudo systemctl enable --now omnibot-metrics-bridge
```

### Enable metrics bridge in launch files

```bash
# Pi (alongside the robot stack)
ros2 launch omnibot_bringup robot.launch.py use_metrics:=true

# GPU Desktop (alongside VLA nodes — uses port 8889)
ros2 launch omnibot_metrics metrics.launch.py machine:=gpu_desktop port:=8889
```

### Architecture

| Component | Runs on | Port | Purpose |
|---|---|---|---|
| `ros2_prometheus_bridge` | Pi + GPU Desktop | 8888 / 8889 | Converts ROS 2 topics → Prometheus metrics |
| `prometheus_fastapi_instrumentator` | GPU Desktop (VLA FastAPI) | 8000/metrics | HTTP request metrics for vla_serve |
| `node_exporter` | Pi + GPU Desktop | 9100 | CPU, RAM, disk metrics |
| `dcgm_exporter` | GPU Desktop | 9400 | GPU VRAM, utilization, temperature |
| `promtail` | Pi + GPU Desktop | — | Ships `~/.ros/log/**` to Loki |
| `Prometheus` | GPU Desktop | 9090 | Scrapes all exporters, 30-day retention |
| `Loki` | GPU Desktop | 3100 | Log aggregation from all machines |
| `Tempo` | GPU Desktop | 4317 (OTLP) | Distributed traces from LangGraph agent |
| `Grafana` | GPU Desktop | 3000 | Dashboards + alerting UI |
| `AlertManager` | GPU Desktop | 9093 | Alert routing (email/Slack) |

### Metrics exposed by `ros2_prometheus_bridge`

| Metric | Source |
|---|---|
| `omnibot_node_cycle_p50/p95/max_ms{node, machine}` | `/diagnostics` (all nodes) |
| `omnibot_vla_inference_ms` | `/diagnostics` (vla_node) |
| `omnibot_vla_preprocess_ms` | `/diagnostics` (vla_node) |
| `omnibot_rl_inference_ms{policy}` | `/diagnostics` (rl_nav/arm nodes) |
| `omnibot_robot_vx/vy/omega` | `/odom` |
| `omnibot_arm_joint_pos_rad{joint}` | `/arm/joint_states` |
| `omnibot_arm_joint_vel_rads{joint}` | `/arm/joint_states` |
| `omnibot_missions_total{type}` | `/mission/command` |
| `omnibot_missions_done_total{type, result}` | `/mission/status` |
| `omnibot_estop_active` | `/emergency_stop` |
| `omnibot_control_mode_info{mode}` | `/control_mode/active` |
| `omnibot_ai_commands_total` | `/ai/command` |

### Dashboards (auto-provisioned)

| Dashboard UID | Title | Key panels |
|---|---|---|
| `omnibot-robot-health` | OmniBot — Robot Health | E-stop, control mode, driver cycle times, velocity, arm joints |
| `omnibot-ai-performance` | OmniBot — AI Performance | VLA latency, mission success rate, LangGraph traces, agent logs |
| `omnibot-system-resources` | OmniBot — System Resources | Pi CPU/RAM/disk, GPU VRAM/utilization/temperature |

### Alert rules (`infra/observability/prometheus/alerts/omnibot_alerts.yml`)

- `ControlLoopSlow` — driver P95 > 55 ms (warning)
- `ControlLoopCritical` — driver P95 > 100 ms (critical)
- `EmergencyStopActive` — `/emergency_stop` is true (critical, immediate)
- `VLAInferenceSlow` — VLA > 2 s (warning)
- `VLAInferenceCritical` — VLA > 5 s (critical)
- `GPUVRAMCritical` — VRAM > 95% (critical)
- `GPUVRAMHigh` — VRAM > 85% (warning)
- `PiCPUHigh` / `PiCPUCritical` — Pi CPU > 85% / 95%
- `PiDiskFull` — Pi root disk < 10% free
- `MissionFailureRateHigh` — > 30% failure rate over 10 min
- `MetricsBridgeDown` — bridge unreachable

### Key files

| File | Purpose |
|---|---|
| `infra/observability/docker-compose.observability.yml` | Standalone compose for observability services only |
| `infra/observability/prometheus/prometheus.yml` | Scrape targets (Pi :8888, GPU :8889, VLA :8000, DCGM :9400) |
| `infra/observability/prometheus/alerts/omnibot_alerts.yml` | Alert rules |
| `infra/observability/grafana/dashboards/*.json` | Pre-built dashboards (auto-provisioned) |
| `infra/observability/alertmanager/alertmanager.yml` | Alert routing — add email/Slack config here |
| `infra/observability/setup_pi_agents.sh` | One-time Pi setup (node_exporter + promtail + metrics bridge) |
| `robot_ws/src/omnibot_metrics/` | `ros2_prometheus_bridge` ROS 2 package |
| `packages/vla_serve/vla_serve/inference/server.py` | Exposes `/metrics` via prometheus-fastapi-instrumentator |
| `robot_ws/src/omnibot_orchestration/omnibot_orchestration/langchain_agent_node.py` | OTEL traces exported to Tempo via gRPC :4317 |

---

## Weights & Biases (W&B) Integration

All three training pipelines log to W&B when a project name is provided. One-time setup:

```bash
pip install wandb>=0.16.0
wandb login   # paste API key from wandb.ai/authorize
```

### Training — LeRobot / SmolVLA

```bash
python lerobot_engine/train.py \
  --model smolvla \
  --dataset-path ~/datasets/mobile_manipulation \
  --output-dir ~/checkpoints/smolvla_run1 \
  --wandb-project omnibot_smolvla \
  --wandb-run-name "smolvla-lr1e-4-bs8"
```

Logs per-step loss + grad norm every 50 batches, per-epoch train/eval loss + LR curve, and uploads checkpoint artifacts (tagged `best` and `epoch-XXXX`) to the W&B artifact registry.

### Training — RL Navigation (Isaac Lab)

```bash
python rl_engine/scripts/train_nav.py \
  --num_envs 512 \
  --wandb_project omnibot_nav   # default; pass "" to use TensorBoard instead
```

RSL-RL logs PPO metrics (mean reward, episode length, value loss, surrogate loss, entropy) automatically. Full YAML hyperparameter config is attached to the run.

### Training — RL Arm (Isaac Lab)

```bash
python rl_engine/scripts/train_arm.py \
  --num_envs 256 \
  --wandb_project omnibot_arm
```

### ONNX Export — Artifact Tracking

```bash
python rl_engine/export/export_policy.py \
  --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \
  --output ~/models/omnibot_nav_policy.onnx \
  --type nav \
  --wandb_project omnibot_nav \
  --wandb_source_run <run_id>   # links artifact back to the training run
```

### Hyperparameter Sweeps

```bash
# Navigation RL sweep (Bayesian, ~20 runs recommended)
wandb sweep rl_engine/config/wandb_sweep_nav.yaml
wandb agent <sweep_id>

# Arm RL sweep
wandb sweep rl_engine/config/wandb_sweep_arm.yaml
wandb agent <sweep_id>

# SmolVLA sweep
wandb sweep lerobot_engine/wandb_sweep_smolvla.yaml
wandb agent <sweep_id>
```

### Runtime Monitoring (Deployed Robot)

Enable by setting `wandb_project` in the RL node params YAML:

```yaml
# robot_ws/src/omnibot_rl/config/rl_nav_params.yaml
rl_nav_node:
  ros__parameters:
    wandb_project: "omnibot_nav"   # streams runtime metrics at 5s intervals
```

Logged runtime metrics: `inference_ms`, `min_lidar_dist`, `cmd_vx/vy/omega`,
`dist_to_goal`, `goal_reached` events.

### W&B Files

| File | Purpose |
|---|---|
| `lerobot_engine/wandb_sweep_smolvla.yaml` | Bayesian sweep for SmolVLA fine-tuning |
| `rl_engine/config/wandb_sweep_nav.yaml` | Bayesian sweep for nav PPO |
| `rl_engine/config/wandb_sweep_arm.yaml` | Bayesian sweep for arm PPO |
| `robot_ws/src/omnibot_rl/omnibot_rl/wandb_runtime_logger.py` | Buffered runtime logger |

---

## Known Issues & Mismatches

No open issues. All previously tracked mismatches have been resolved.

**Fixed (no longer open):**
- *`robot.launch.py` parameter names* — both `robot.launch.py` and
  `yahboom_controller_node.py` now use `wheel_separation_length`/`width` consistently.
- *Emergency stop not wired* — `yahboom_controller_node.py` subscribes to
  `/emergency_stop` (Bool); activating it zeroes velocity immediately and holds
  until cleared (Bool=false).
- *Hardcoded debug log path* — changed from `/home/varunvaidhiya/yahboom_debug.log`
  to `~/.ros/omnibot_debug.log` (works on any machine).
- *Arm joint name prefix* — `smolvla_node.py` `JOINT_NAMES` and `arm_driver_node.py`
  default `joint_names` both now use the `arm_` prefix (`arm_shoulder_pan`, …).
- *arm_cmd_mux missing from default launches* — now started unconditionally in
  `hybrid_robot.launch.py`, `mobile_manipulation.launch.py`, and
  `smolvla_inference.launch.py`.
- *OpenVLA `/image_raw` subscription* — `vla_desktop.launch.py` and
  `hybrid_robot.launch.py` now remap `/image_raw` → `/camera/front/image_raw`.
- *ROSBridge and BEV stitcher not in launch files* — both are included in
  `robot.launch.py`, `hybrid_robot.launch.py`, and `mobile_manipulation.launch.py`.
- *Android cmd_vel on wrong topic* — now publishes to `/cmd_vel/teleop` (teleop
  slot) instead of the nav2 slot. Call `sendControlMode("teleop")` before driving.
- *Android sendMode not updating cmd_vel_mux* — `sendMode` now also publishes
  the correct mode string to `/control_mode`.

---

## Test Coverage

Overall coverage is very low (~5%). Priority areas:

| File | Coverage | Priority |
|---|---|---|
| `yahboom_controller_node.py` | 0% | Critical — checksum, packet encoding, odometry |
| `cmd_vel_mux.py` | 0% | High — pure logic, easy to test |
| `serial_bridge_node.py` | 0% | High — mecanum kinematics just added |
| `mission_planner.py` | 0% | High — command parser, state machine |
| `arm_driver_node.py` | 0% | High — tick↔rad conversion, clamping |
| `smolvla_node.py` | 0% | High — action mapping, image preprocessing |
| `openvla.py` | ~30% | Medium — only tested via mocks |
| Android (all Kotlin) | ~1% | High — repository layer untestable due to singleton |

When adding tests:
- Mock `serial.Serial` in a shared `conftest.py` fixture for all driver tests.
- For ROS nodes, use `rclpy` test utilities; spin in a thread and test via
  topic publish/subscribe.
- The Android `RobotRepository` singleton pattern must be refactored to
  constructor DI before it can be properly unit tested.
- Run `pytest --cov` and check the HTML report before opening a PR.
