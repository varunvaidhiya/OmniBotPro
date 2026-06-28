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
| `sdk/` | **OhhO OS** — open-source robot-agnostic engine (the `ohho` package). Robot abstraction, runtimes (native + ROS 2), adapters (sim, Yahboom, Feetech, Unitree, ROS 2), agent brain, data/train/serve, CLI. Apache-2.0. See `sdk/AGENTS.md`. |
| `packages/` | Standalone Python packages (shared, not ROS-dependent) |
| `vla_engine/` | PyTorch VLA training/inference (no ROS) |
| `data_engine/` | Episode-based dataset collection pipeline |
| `lerobot_engine/` | Direct LeRobot training/recording/inference scripts (no ROS) |
| `rl_engine/` | Isaac Lab RL training + ONNX export for sim-to-real |
| `learning_engine/` | Post-training & continual-learning framework (numpy core, optional torch/VLM) |
| `agent_engine/` | Continuous agent harness — deliberative perceive→reason→act→reflect loop (numpy core, optional Claude/LangGraph) |
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
OmniBot/
├── robot_ws/
│   └── src/
│       ├── omnibot_bringup/       # Launch files, RViz/Gazebo config
│       ├── omnibot_driver/        # Yahboom serial driver (primary)
│       ├── omnibot_description/   # URDF/xacro, meshes
│       ├── omnibot_navigation/    # SLAM, Nav2, EKF, waypoints
│       ├── omnibot_vla/           # OpenVLA ROS 2 node
│       ├── omnibot_arm/           # SO-101 arm driver (LeRobot)
│       ├── omnibot_hybrid/        # cmd_vel mux + mission planner
│       ├── omnibot_lerobot/       # Model-agnostic policy_node (SmolVLA/ACT/diffusion) + recorder + BEV
│       ├── omnibot_rl/            # RL inference nodes (nav + arm) + arm_cmd_mux
│       ├── omnibot_orchestration/ # LangGraph AI orchestration (Claude-backed)
│       ├── omnibot_perception/    # AI perception: object distance + pose (depth cam)
│       ├── omnibot_metrics/       # Prometheus telemetry bridge (observability)
│       ├── omnibot_ota/           # Over-the-air workspace + ONNX model updater
│       ├── omnibot_vr/            # VR teleop/recording bridge (Unity vr_app)
│       ├── ros2_astra_camera/     # Orbbec Astra driver (build-from-source placeholder)
│       └── omnibot_firmware/      # Legacy STM32 (not active)
├── packages/
│   ├── yahboom_ros2/              # Pure-Python Yahboom protocol encoder/decoder
│   ├── vla_serve/                 # FastAPI VLA inference server
│   ├── robot_episode_dataset/     # LeRobot-format dataset helpers
│   ├── ros2_bev_stitcher/         # BEV (bird's-eye-view) image stitcher
│   └── mecanum_drive_ros2/        # C++17 + Python mecanum kinematics library
├── vla_engine/                    # FastAPI server + OpenVLA wrapper + tests
├── data_engine/                   # schema/constants.py, ingestion/, isaac_sim/, tests/
├── lerobot_engine/                # train.py, record.py, infer.py, requirements.txt
├── rl_engine/                     # Isaac Lab envs, mdp tasks, export, train scripts
├── learning_engine/               # Post-training loop: collectors, rewards, replay, trainers, verification
├── agent_engine/                  # Agent harness: core (harness/blackboard/tools), reasoning, memory, reasoners
├── digital_twin/                  # worlds/, scenarios/, docker/, configs/, scripts/
├── android_app/                   # Kotlin MVVM app (ROSBridge WebSocket)
├── confirmed_protocol.py          # Yahboom protocol reference (root debug script)
├── deploy.py                      # Deployment mode configurator (single/multi)
├── deployment.env.example         # Template for deployment.env
├── network.env                    # Cross-machine DDS peer IPs (edit before multi-machine use)
├── docker-compose.yml             # Full-stack Docker Compose (robot + vla + rosbridge services)
├── launch_simulation.sh           # Convenience build+launch for Gazebo (single or multi)
├── launch_teleop.sh               # Convenience teleop launcher (sources network.env for DDS peers)
├── launch_rosbridge.sh            # Start ROSBridge WebSocket server for Android app
└── launch_mobile_manipulation.sh  # Mobile manipulation bringup (base + arm + cameras + rosbridge)
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
ros2 launch omnibot_bringup perception.launch.py          # run on Pi
ros2 launch omnibot_bringup perception.launch.py rviz:=true
ros2 launch omnibot_bringup perception_viewer.launch.py   # workstation viewer

# ── SLAM + 3-D mapping (run after perception.launch.py) ───────────────────
ros2 launch omnibot_bringup slam_3d_mapping.launch.py
ros2 launch omnibot_bringup slam_3d_mapping.launch.py rviz:=false   # headless Pi
ros2 launch omnibot_bringup slam_3d_viewer.launch.py                # workstation viewer
ros2 launch omnibot_bringup slam_3d_mapping.launch.py slam3d:=false # 2-D SLAM only
ros2 launch omnibot_bringup slam_3d_mapping.launch.py \
  slam_mode:=localization map_file:=/path/to/omnibot_map

# ── Basic robot, teleop ────────────────────────────────────────────────────
ros2 launch omnibot_bringup robot.launch.py
ros2 launch omnibot_bringup robot_with_joy.launch.py   # + Xbox controller
ros2 launch omnibot_bringup joy_teleop.launch.py       # teleop only

# ── Simulation ────────────────────────────────────────────────────────────
./launch_simulation.sh                               # builds + launches (ROS_DOMAIN_ID=30)
ros2 launch omnibot_bringup simulation.launch.py     # direct
ros2 launch omnibot_bringup simulation.launch.py \
  world:=$(pwd)/digital_twin/worlds/omnibot_lab.sdf  # richer lab world

# ── Mobile manipulation ────────────────────────────────────────────────────
./launch_mobile_manipulation.sh
ros2 launch omnibot_bringup mobile_manipulation.launch.py

# ── ROSBridge (required for Android app, port 9090) ───────────────────────
./launch_rosbridge.sh
ros2 launch rosbridge_server rosbridge_websocket_launch.xml port:=9090

# ── AI & navigation ───────────────────────────────────────────────────────
ros2 launch omnibot_navigation autonomous_robot.launch.py
ros2 launch omnibot_hybrid hybrid_robot.launch.py               # driver+SLAM+Nav2+VLA+mux
ros2 launch omnibot_hybrid hybrid_robot.launch.py use_rl:=true  # + RL nodes
ros2 launch omnibot_hybrid hybrid_robot.launch.py use_langchain:=true
ros2 launch omnibot_lerobot policy_inference.launch.py
ros2 launch omnibot_vla vla_desktop.launch.py    # GPU machine only
ros2 launch omnibot_arm arm.launch.py
ros2 launch omnibot_rl rl_inference.launch.py    # 4 RL nodes standalone
ros2 launch omnibot_bringup isaac_sim.launch.py  # Isaac Sim companion nodes

# ── Key topic publishes ────────────────────────────────────────────────────
ros2 topic pub --once /vla/prompt std_msgs/msg/String "data: 'Find the red cup'"
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'navigate:kitchen,vla:find the red cup'"
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'rl_nav:kitchen,rl_arm:pick up the cup'"
ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'rl_nav'"
ros2 topic pub --once /arm/cmd_mode std_msgs/msg/String "data: 'rl_arm'"
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
                                                │    modes: policy (default) | rl_arm
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
| `/cmd_vel/vla` | Twist | `vla_node`, `policy_node` | `cmd_vel_mux` |
| `/cmd_vel/teleop` | Twist | `teleop_twist_joy` | `cmd_vel_mux` |
| `/cmd_vel/out` | Twist | `cmd_vel_mux` | `yahboom_controller_node` (via remap) |
| `/control_mode` | String | `mission_planner`, manual pub | `cmd_vel_mux` |
| `/control_mode/active` | String | `cmd_vel_mux` | monitoring |
| `/vla/prompt` | String | Android, `mission_planner` | `vla_node` |
| `/policy/task` | String | `mission_planner` | `policy_node` |
| `/policy/enable` | Bool | `mission_planner` | `policy_node` |
| `/mission/command` | String | Android, manual pub | `mission_planner` |
| `/mission/cancel` | String | Android | `mission_planner` |
| `/mission/status` | String | `mission_planner` | Android |
| `/odom` | Odometry | `yahboom_controller_node` | Nav2, `policy_node`, Android |
| `/imu/data` | Imu | `yahboom_controller_node` | Nav2 EKF, Android |
| `/map` | OccupancyGrid | `slam_toolbox` | Nav2, Android |
| `/tf` | TFMessage | `robot_state_publisher`, driver | all navigation |
| `/joint_states` | JointState | `robot_state_publisher`, Gazebo | `robot_state_publisher` |
| `/arm/joint_states` | JointState | `arm_driver_node` | `policy_node`, Android |
| `/arm/leader_states` | JointState | `arm_driver_node` (teleop) | `teleop_recorder_node` |
| `/arm/joint_commands` | JointState | Android, `policy_node` | `arm_cmd_mux` |
| `/arm/joint_commands/rl` | JointState | `rl_arm_node` | `arm_cmd_mux` |
| `/arm/joint_commands/out` | JointState | `arm_cmd_mux` | `arm_driver_node` |
| `/arm/cmd_mode` | String | `mission_planner`, manual pub | `arm_cmd_mux` |
| `/arm/cmd_mode/active` | String | `arm_cmd_mux` | monitoring |
| `/arm/enable` | Bool | Android | `arm_driver_node` |
| `/cmd_vel/rl` | Twist | `rl_nav_node` | `cmd_vel_mux` |
| `/rl_nav/goal` | PoseStamped | `mission_planner`, manual pub | `rl_nav_node` |
| `/rl_arm/target_pose` | PoseStamped | `rl_object_pose_node` | `rl_arm_node` |
| `/rl_arm/target_detected` | Bool | `rl_object_pose_node` | `rl_arm_node` |
| `/perception/objects` | PoseArray | `object_perception_node` | monitoring, Nav2 tooling |
| `/perception/object_info` | String (JSON) | `object_perception_node` | `langchain_agent_node` |
| `/perception/nearest_distance` | Float32 | `object_perception_node` | mission logic |
| `/perception/query_pixel` | PointStamped | any (VLA / agent) | `object_perception_node` |
| `/perception/query_result` | PoseStamped | `object_perception_node` | requester |
| `/rosbag_recorder/start` | String | Android | `rosbag_recorder` |
| `/rosbag_recorder/stop` | Empty | Android | `rosbag_recorder` |
| `/rosbag_recorder/status` | String | `rosbag_recorder` | Android, monitoring |
| `/emergency_stop` | Bool | Android | `yahboom_controller_node` |
| `/robot_mode` | String | Android | *(monitoring only — does not control mux)* |
| `/joy` | Joy | `joy_node` | `yahboom_controller_node`, `teleop_recorder_node` |
| `/camera/front/image_raw` | Image | USB camera / Gazebo | `vla_node` (via `/image_raw` remap), `langchain_agent_node` |
| `/camera/wrist/image_raw` | Image | wrist camera | `policy_node`, `teleop_recorder_node`, `rl_object_pose_node`, `langchain_agent_node` |
| `/camera/base/bev/image_raw` | Image | `bev_stitcher_node` | `policy_node`, `teleop_recorder_node` |
| `/ai/command` | String | Android, manual pub | `langchain_agent_node` |
| `/ai/status` | String | `langchain_agent_node` | Android |
| `/ai/response_needed` | String | `langchain_agent_node`, `agent_node` | Android |
| `/agent/world_state` | String (JSON) | `world_state_node` | `agent_node` |
| `/agent/goal` | String | Android, manual pub | `agent_node` |
| `/agent/human_response` | String | operator, manual pub | `agent_node` |

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
update rate `20 Hz`. Debug log: `~/.ros/omnibot_debug.log`.

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

**EKF (`robot_localization.yaml`)**: wheel odom fused as VELOCITIES ONLY
(vx, vy, vyaw — mecanum slip makes absolute encoder pose untrustworthy);
IMU fused as differential orientation + angular velocity, linear accel
disabled (noisy MEMS). Exactly ONE odom→base_link TF broadcaster: the EKF.
Every launch that starts both the driver and ekf_node must set the driver's
`publish_tf: False` (robot.launch.py, hybrid_robot.launch.py,
master/omnibot_pi.launch.py all do).

**rosbag_recorder** (`omnibot_hybrid`): remote-controlled `ros2 bag record`
for the Android app. `/rosbag_recorder/start` (String bag name),
`/rosbag_recorder/stop` (Empty), status on `/rosbag_recorder/status`.
Records a curated telemetry set by default (`record_all:=true` for -a).

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
`policy_node.py` — they are kept in sync. Do not change one without the other.

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

**`policy_node.py`** — model-agnostic unified 9-DOF policy (6 arm + 3 base).
The model backend is selected by the `model_type` parameter (registry names:
`smolvla` | `act` | `diffusion` | `openvla`); SmolVLA is the default.
Config: `config/policy_params.yaml`. Launch: `policy_inference.launch.py`
(`model_type:=`, `checkpoint:=`, `device:=`, `include_arm_mux:=`).

| Parameter | Default |
|---|---|
| `model_type` | `'smolvla'` |
| `checkpoint_path` | `'lerobot/smolvla_base'` |
| `device` | `'cuda'` |
| `policy_hz` | `10.0` |
| `chunk_size` | `50` |
| `state_dim` / `action_dim` | `9` |
| `image_width` / `image_height` | `320` / `240` |
| `task_description` | `'pick up the object and place it'` |
| `base_vel_scale` | `0.3` |
| `use_trt` | `False` (TRT vision-encoder patch) |

Control topics: `/policy/task` (String, update task description) and
`/policy/enable` (Bool, enable/disable inference).
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

Valid modes: `"policy"` (default, transparent pass-through), `"rl_arm"`.
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

### `omnibot_perception`

Modular AI perception. **`object_perception_node.py`** measures object
distance + estimates pose from the Astra depth camera. Two backends chosen
automatically: YOLO (`ultralytics`, optional) with depth-fused 3-D positions,
or dependency-free depth-band clustering (connected components + PCA yaw).
Publishes `/perception/objects` (PoseArray, base_link), `/perception/object_info`
(JSON), `/perception/nearest_distance` (Float32), `/perception/markers` (RViz).
Answers pixel queries: `/perception/query_pixel` → `/perception/query_result`.
Launched by `perception.launch.py` (`ai_perception:=true` default) or
standalone: `ros2 launch omnibot_perception perception_ai.launch.py`.
Config: `robot_ws/src/omnibot_perception/config/perception_params.yaml`.
`langchain_agent_node` consumes `/perception/object_info` and appends metric
detections to `observe_node` scene descriptions.

### BEV stitcher (geometric IPM)

`omnibot_lerobot/bev_stitcher_node.py` now computes geometric IPM
homographies from URDF camera poses when no calibration file exists —
producing a true fused top-down BEV (no more 2×2 tiling). Params:
`bev_range_m` (2.0), `camera_hfov` (1.745), `camera_height` (0.0885),
`cam_{front,rear,left,right}_pose` [x, y, yaw]. A calibration file
(`~/omnibot_bev_calibration.npz`) still takes priority when present.

### Nav2 motion limits (must match driver)

`nav2_params.yaml` velocities are capped at 0.20 m/s and accelerations at
1.0 m/s² to match the Yahboom driver's hardcoded clamp (0.2 m/s) and ramp
limiter (0.05 m/s per 20 Hz tick). `min_vel_x` is −0.20 (omnidirectional
reverse allowed), `vy_samples: 10`. `depthimage_to_laserscan` must use
`output_frame: depth_camera_link` (x-forward), never the optical frame.

---

## RL Engine (`rl_engine/`)

Isaac Lab training infrastructure for sim-to-real RL. Requires Isaac Sim + Isaac Lab
(not part of the ROS build — run separately on the GPU PC).
Requirements: `isaaclab>=1.1.0`, `onnxruntime-gpu>=1.16`, `torch>=2.1`.

**Key design decisions:**
- Actions are velocity **deltas** (not absolute), matching Yahboom's 0.05 m/s ramp limiter.
- `MecanumWheelActionTerm` uses exact OmniBot constants: `lx=0.0825`, `ly=0.1075`, `r=0.04`.
- Arm actions are joint position deltas (±0.05 rad/step → maps to STS3215 Goal_Position).
- `ActionDelayTerm` (1–3 steps at 20 Hz = 50–150 ms) models servo bus latency.
- `physics_randomization` in `randomization_config.yaml` is disabled by default;
  set `enabled: true` only during Isaac Lab RL training.

**Train → Export workflow:**
```bash
python rl_engine/scripts/train_nav.py --num_envs 512 --max_iterations 2000
python rl_engine/export/export_policy.py \
  --checkpoint ~/logs/omnibot_nav/checkpoints/model_2000.pt \
  --output ~/models/omnibot_nav_policy.onnx --type nav
# Copy .onnx to robot, then launch with use_rl:=true
```

---

## Learning Engine (`learning_engine/`)

Modular post-training & continual-learning framework — see
`learning_engine/ARCHITECTURE.md` for the full design. Architecture-first:
every layer is a pluggable component behind `learning_engine/core/interfaces.py`,
registered by name in `core/registry.py` and assembled from config
(`configs/learning_loop.yaml`).

Layers: data collection (teleop/sim/real/rosbag collectors → on-disk
`ReplayDataset`, npz+json), simulation adapters (Isaac Lab, MuJoCo, ManiSkill,
Gazebo) + curriculum/domain randomization, multi-objective `RewardEngine`
(task/dense/safety/efficiency/smoothness terms), vision rewards & AI judges
(`VLMClient`, Claude by default), self-evaluation (reflection + heuristic
fallback), experience replay (PER + outcome-stratified episodic store),
interchangeable trainers (BC, offline RL/AWR; `online_rl` delegates to
`rl_engine`, `finetune_smolvla` delegates to `lerobot_engine`),
`PostTrainingLoop` + `ContinualLearningScheduler` (triggers: new episodes /
new tasks / staleness), and `InferenceVerifier` (Best-of-N with hard
safety/reachability checks using the real hardware limits).

Key conventions:
- Observations are `Dict[str, np.ndarray]` with keys in
  `learning_engine/data/schema.py` (`state` 9-D = arm ×6 + base ×3) — must
  stay aligned with `data_engine/schema/constants.py`.
- Core imports need only numpy; torch/lerobot/isaaclab/anthropic/rclpy are
  optional and imported lazily inside the components that need them.
- ROS integration: `learning_engine/ros2/episode_logger_node.py` runs on the
  Pi, records post-mux commands + observations, segments episodes via
  `/learning/episode/start|stop` or `/mission/status`; topic names live in
  `learning_engine/ros2/topics.py` and must mirror this file's topic map.

**Hardware portability** (`learning_engine/hardware/`): all hardware
knowledge is isolated here — every component takes `device="auto"` (resolved
by `resolve_device()` → cuda/mps/cpu) and ONNX policies call
`onnx_providers()` (TensorRT-first on Jetson; CUDA on the dGPU workstation
with TensorRT opt-in via `prefer_tensorrt=True`; CoreML on Apple Silicon;
CPU elsewhere). Named deployment topologies in `hardware/profiles.py`:
`pi_workstation` (= deploy.py multi), `workstation_single` (= deploy.py
single), `jetson_single`, `pi_accelerator_workstation` (Hailo/Coral),
`pi_deepx_workstation` (Pi 5 + DeepX NPU ~25 TOPS for on-robot
perception/VLA/local reasoning), `mac_dev`. `OMNIBOT_HW_PROFILE` env var forces
a profile; else `detect_profile()` guesses (a DeepX NPU on a Pi auto-selects
`pi_deepx_workstation`). Add a target = one row in `detect_accelerators()`
+ one EP-preference row + optionally one profile.

**Benchmarking** (`learning_engine/benchmarks/`): `run.py` CLI runs the same
inference/training/dataset suites on any target under a `ResourceMonitor`
(CPU/GPU util, mem, power, temp via psutil/pynvml/tegrastats), bundling AI
metrics + hardware telemetry + a `SystemInfo` snapshot. Reporters publish to
W&B (`WandbReporter` — SystemInfo → run config, grouped by hw profile),
Prometheus (`PrometheusReporter` — `/metrics` HTTP on port 8890, already a
scrape target in `infra/observability/prometheus/prometheus.yml`; or
node_exporter textfile on the Pi), and JSON artifacts. `PostTrainingLoop`
accepts the same `reporters=[...]` so continual-learning metrics stream too.

```bash
# Tests (stdlib unittest, no optional deps needed)
python3 -m unittest discover -s learning_engine/tests -t .
# Benchmark this machine (auto-detects accelerator + profile)
python3 -m learning_engine.benchmarks.run --suite inference,training,dataset
```

---

## Agent Engine (`agent_engine/`)

Continuous **agent harness** — the deliberative "brain" that turns OmniBot from
a one-shot command executor into a continuously-operating physical agent. Same
convention as `learning_engine`: a pure-Python core (numpy only, fully
unit-testable) with adapters at the edges (cloud Claude, ROS, learning-engine
reuse) imported lazily. See `agent_engine/ARCHITECTURE.md`.

`AgentHarness` (`core/harness.py`) runs a state machine, one transition per
`tick()` (ROS-timer- or test-friendly):
`IDLE → PERCEIVE → PLAN → ACT → MONITOR → (loop to PERCEIVE) → REFLECT →
REMEMBER → IDLE`, with `WAIT_HUMAN` and e-stop as first-class interrupts and a
per-goal step budget. The harness depends only on structural `Protocol` **ports**
(`core/interfaces.py`): `Perceptor`, `Reasoner`, `MemoryPort`, `Reflector`,
`VerifierPort`, `ReasoningBackend`, `EntityStore`.

| Component | Role |
|---|---|
| `core/blackboard.py` `WorldState` | one fused snapshot; `to_observation()`→9-D `{"state"}` for the verifier; `to_dict()` round-trips `/agent/world_state` |
| `core/tools.py` `ToolRegistry` | actuators as callable tools + Claude tool-use schemas; `low_level=True` tools must pass the verifier |
| `reasoning/router.py` `ReasoningRouter` | hybrid brain — first available of cloud Claude / on-device LLM / DeepX per call kind (`deliberate`/`reactive`) |
| `reasoning/cloud_claude.py` | `CloudClaudeBackend` (`claude-sonnet-4-6`, lazy `anthropic`, `ANTHROPIC_API_KEY`) |
| `reasoning/local_llm.py` | `LocalLLMBackend` (Ollama/llama.cpp/DeepX-compiled fallback; available only when a runner is wired) |
| `memory/working_memory.py` `WorkingMemory` | injects long-term (`EntityMemory`) + short-term (recent outcomes) + episodic (`ReplayDataset`) into prompts; writes grounded objects back |
| `reasoners/claude_tool_caller.py` `ClaudeToolCallingReasoner` | LLM brain for PLAN: backend-agnostic JSON tool-calling via `ReasoningRouter`, tools from `ToolRegistry.to_anthropic_schema()` |
| `reasoners/scripted.py` `ScriptedReasoner` | deterministic reasoner for tests/sim |
| `reasoning/factory.py` `build_reasoning_router` | assembles cloud+local+echo backends in deliberate/reactive preference order |

Reuse-by-adapter (`integrations/learning_engine.py`, lazy import): `WorldStateVerifier`
wraps `SafetyCheck`/`ReachabilityCheck` (real hw limits) as the `VerifierPort`;
`EvaluatorReflector` runs the `LanguageGoalEvaluator`→`ReflectionEvaluator`→
`HeuristicSelfEvaluator` chain and persists labelled episodes to `ReplayDataset`;
`ReplayMemorySource` feeds episodic recall; `ContinualLearningClosure` fires the
`ContinualLearningScheduler` (reflect→learn).

ROS edge (`omnibot_orchestration`, needs `pip install -e agent_engine`):
`world_state_node` fuses `/odom`+`/arm/joint_states`+`/perception/object_info`+
`/mission/status`+`/emergency_stop` → `/agent/world_state` (JSON String, 5 Hz);
`agent_node` drives `harness.tick()` from a timer (worker thread; one tick in
flight), goals on `/ai/command`+`/agent/goal`, tools publish `/mission/command`+
`/control_mode`+`/arm/cmd_mode`+`/mission/cancel`+`/ai/response_needed`. Launch:
`agent.launch.py` (config `config/agent_params.yaml`).

```bash
# Core tests (stdlib unittest; numpy; learning_engine adapters covered too)
python3 -m unittest discover -s agent_engine/tests -t .
# Run the loop on the robot / in Gazebo (after pip install -e agent_engine)
ros2 launch omnibot_orchestration agent.launch.py
ros2 topic pub --once /ai/command std_msgs/msg/String "data: 'find the red cup'"
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
Must be running whenever `policy_node` or `teleop_recorder_node` is active.
Config: `packages/ros2_bev_stitcher/config/bev_params.yaml`.

### `mecanum_drive_ros2`

Hardware-agnostic mecanum kinematics — C++17 header-only library
(`include/mecanum_drive_ros2/mecanum_kinematics.hpp`) with a pure Python mirror
(`mecanum_drive_ros2.kinematics`).

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
for simulation-tuned Nav2 costmaps.

### Isaac Sim (VLA / training data)

```bash
bash digital_twin/scripts/build_usd.sh   # URDF → USD (run once per URDF change)
# Start Isaac Sim, run digital_twin/scripts/setup_omnigraph.py in Script Editor
ros2 launch omnibot_bringup isaac_sim.launch.py
python3 data_engine/isaac_sim/collect_episodes.py \
  --config data_engine/isaac_sim/randomization_config.yaml \
  --output ~/datasets/omnibot
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
Also hosts the **agent harness ROS edge** (`world_state_node`, `agent_node` —
launch `agent.launch.py`), which runs the continuous `agent_engine` loop; see
the **Agent Engine** section above.

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
| `/emergency_stop` | Bool | zeroes velocity immediately; hold Bool=false to clear |
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

```bash
docker compose --profile observability up -d
# Grafana: http://localhost:3000  (admin / omnibot)
# Prometheus: http://localhost:9090   AlertManager: http://localhost:9093

# Pi (alongside robot stack)
ros2 launch omnibot_bringup robot.launch.py use_metrics:=true
# GPU Desktop (alongside VLA nodes)
ros2 launch omnibot_metrics metrics.launch.py machine:=gpu_desktop port:=8889
```

| File | Purpose |
|---|---|
| `infra/observability/docker-compose.observability.yml` | Standalone compose for observability services only |
| `infra/observability/prometheus/prometheus.yml` | Scrape targets (Pi :8888, GPU :8889, VLA :8000, DCGM :9400) |
| `infra/observability/prometheus/alerts/omnibot_alerts.yml` | Alert rules |
| `infra/observability/grafana/dashboards/*.json` | Pre-built dashboards (auto-provisioned) |
| `infra/observability/alertmanager/alertmanager.yml` | Alert routing — add email/Slack config here |
| `infra/observability/setup_pi_agents.sh` | One-time Pi setup (node_exporter + promtail + metrics bridge) |
| `robot_ws/src/omnibot_metrics/` | `ros2_prometheus_bridge` ROS 2 package |

---

## Weights & Biases (W&B) Integration

All three training pipelines log to W&B. Setup: `pip install wandb>=0.16.0 && wandb login`.

- **SmolVLA**: `lerobot_engine/train.py --wandb-project omnibot_smolvla`
- **RL nav**: `rl_engine/scripts/train_nav.py --wandb_project omnibot_nav`
- **RL arm**: `rl_engine/scripts/train_arm.py --wandb_project omnibot_arm`

Enable runtime monitoring on the deployed robot by setting `wandb_project: "omnibot_nav"`
in `robot_ws/src/omnibot_rl/config/rl_nav_params.yaml` (streams metrics at 5 s intervals).

| File | Purpose |
|---|---|
| `lerobot_engine/wandb_sweep_smolvla.yaml` | Bayesian sweep for SmolVLA fine-tuning |
| `rl_engine/config/wandb_sweep_nav.yaml` | Bayesian sweep for nav PPO |
| `rl_engine/config/wandb_sweep_arm.yaml` | Bayesian sweep for arm PPO |
| `robot_ws/src/omnibot_rl/omnibot_rl/wandb_runtime_logger.py` | Buffered runtime logger |

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
| `policy_node.py` | 0% | High — action mapping, image preprocessing |
| `openvla.py` | ~30% | Medium — only tested via mocks |
| Android (all Kotlin) | ~1% | High — repository layer untestable due to singleton |

When adding tests:
- Mock `serial.Serial` in a shared `conftest.py` fixture for all driver tests.
- For ROS nodes, use `rclpy` test utilities; spin in a thread and test via
  topic publish/subscribe.
- The Android `RobotRepository` singleton pattern must be refactored to
  constructor DI before it can be properly unit tested.
- Run `pytest --cov` and check the HTML report before opening a PR.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
