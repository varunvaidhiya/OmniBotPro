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
| `digital_twin/` | Contributor simulation environment (Gazebo, Isaac Sim, Foxglove) |
| `android_app/` | Kotlin MVVM Android controller (ROSBridge WebSocket) |
| `infra/` | Docker, DevContainers, CI/CD scripts |

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
                                                │    modes: nav2 | vla | teleop
                                                │
                                                ├─ mission_planner
                                                │    parses "navigate:X,vla:Y" commands
                                                │
                                                ├─ slam_toolbox + Nav2 → /cmd_vel
                                                │
                                                └─ arm_driver_node
                                                     /arm/joint_commands → Feetech bus

VLA Desktop (GPU PC)
  OpenVLA node  ← /image_raw + /vla/prompt  →  /cmd_vel/vla
  SmolVLA node  ← /camera/wrist + /camera/base/bev + /arm/joint_states + /odom
               →  /arm/joint_commands + /cmd_vel
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
| `/arm/joint_commands` | JointState | Android, `smolvla_node` | `arm_driver_node` |
| `/arm/enable` | Bool | Android | `arm_driver_node` |
| `/emergency_stop` | Bool | Android | *(not yet subscribed — known gap)* |
| `/robot_mode` | String | Android | *(monitored externally)* |
| `/joy` | Joy | `joy_node` | `yahboom_controller_node`, `teleop_recorder_node` |
| `/camera/image_raw` | Image | USB camera / Gazebo | `vla_node` |
| `/camera/wrist/image_raw` | Image | wrist camera | `smolvla_node`, `teleop_recorder_node` |
| `/camera/base/bev/image_raw` | Image | `bev_stitcher_node` | `smolvla_node`, `teleop_recorder_node` |

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

**Known parameter mismatch**: `robot.launch.py` passes `wheel_separation_x` /
`wheel_separation_y` but `yahboom_controller_node.py` declares
`wheel_separation_length` / `wheel_separation_width`. These must be kept in
sync when either file is changed.

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
| `joint_names` | `['shoulder_pan','shoulder_lift','elbow_flex','wrist_flex','wrist_roll','gripper']` |
| `motor_ids` | `[1,2,3,4,5,6]` |
| `home_ticks` | `[2048,2048,2048,2048,2048,2048]` |
| `joint_min` | `[-3.14,-1.57,-1.57,-1.57,-3.14,-0.1]` |
| `joint_max` | `[3.14,1.57,1.57,1.57,3.14,0.8]` |

> **Joint name mismatch**: `arm_driver_node.py` defaults use short names
> (`shoulder_pan`, …) but the URDF and Android app use prefixed names
> (`arm_shoulder_pan`, …). Always set `joint_names` explicitly in
> `arm_params.yaml` to the prefixed form.

Falls back to passthrough/simulation mode if `lerobot` is not installed.

### `omnibot_hybrid`

**`cmd_vel_mux.py`** — routes one of three `/cmd_vel` sources to `/cmd_vel/out`.

| Parameter | Default |
|---|---|
| `default_mode` | `'nav2'` |

Valid `control_mode` strings: `"nav2"`, `"vla"`, `"teleop"` (any other value
is silently ignored — the active mode does not change).

**`mission_planner.py`** — high-level state machine.

Command format on `/mission/command`:
```
"navigate:kitchen,vla:find the red cup"   # navigate then VLA
"navigate:kitchen"                         # navigate only
"vla:find the red cup"                     # VLA only
```

Named locations loaded from `config/named_locations.yaml`
(keys: location name → `{x, y, yaw}`).

State machine: `idle → navigating → vla → done → idle`.

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
| `/cmd_vel` | Twist | rate: 20 Hz, clamped ±1.5 m/s / ±2.0 rad/s |
| `/emergency_stop` | Bool | published but not yet consumed on robot side |
| `/robot_mode` | String | |
| `/arm/joint_commands` | JointState | joint names must be `arm_*` prefixed |
| `/arm/enable` | Bool | |
| `/vla/prompt` | String | |

**Subscribed topics** (robot → Android):

| Topic | Type |
|---|---|
| `/odom` | Odometry |
| `/map` | OccupancyGrid |
| `/imu/data` | Imu |
| `/diagnostics` | DiagnosticArray |
| `/arm/joint_states` | JointState |

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

## Known Issues & Mismatches

These are tracked bugs — do not silently work around them, fix them properly:

1. **`robot.launch.py` parameter names** — passes `wheel_separation_x`/`y`
   but the node declares `wheel_separation_length`/`width`. Must be aligned.

2. **Arm joint name prefix** — `arm_driver_node.py` default `joint_names`
   omits the `arm_` prefix (`shoulder_pan` vs `arm_shoulder_pan`). The URDF,
   Android, and `arm_params.yaml` all use the prefixed form. Always override
   the default via the YAML config.

3. **Emergency stop not wired** — Android publishes `/emergency_stop` (Bool)
   but no ROS node subscribes. Adding a subscriber to
   `yahboom_controller_node.py` is the correct fix.

4. **ROSBridge not in launch files** — `rosbridge_server` on port 9090 is
   required for the Android app but is not started by any launch file. Start
   it manually or add it to `robot.launch.py`.

5. **BEV stitcher not in default launch** — `smolvla_node` requires
   `/camera/base/bev/image_raw`, which comes from `bev_stitcher_node`. It is
   not included in any default launch file.

6. **Hardcoded debug log path** — `yahboom_controller_node.py` writes to
   `/home/varunvaidhiya/yahboom_debug.log`. Fails silently on other machines.

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
