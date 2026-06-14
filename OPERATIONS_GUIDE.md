# OmniBot Operations Guide

Complete guide for starting the robot, collecting training data, and fine-tuning SmolVLA for unified 9-DOF mobile manipulation control.

---

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Robot Startup Sequence](#robot-startup-sequence)
3. [Simulation Startup](#simulation-startup)
4. [Perception & Mapping](#perception--mapping)
5. [Autonomous Navigation (Nav2)](#autonomous-navigation-nav2)
6. [Hybrid Control Modes](#hybrid-control-modes)
7. [Data Collection for Fine-Tuning](#data-collection-for-fine-tuning)
8. [Fine-Tuning SmolVLA](#fine-tuning-smolvla)
9. [Running the Trained Model](#running-the-trained-model)
10. [Troubleshooting](#troubleshooting)

---

## System Architecture

OmniBot uses a **hybrid architecture** — the active control mode determines which source drives the base and arm:

```
Mission Command (/mission/command)
        │
        ▼
  mission_planner  ──── /control_mode ────► cmd_vel_mux ──► /cmd_vel/out ──► yahboom_driver
        │                                     ▲  ▲  ▲
        │               Nav2 → /cmd_vel ──────┘  │  │
        │          SmolVLA → /cmd_vel/vla ─────────┘  │
        │          Teleop → /cmd_vel/teleop ───────────┘
        │
        ├── /policy/task + /policy/enable ──► policy_node ──► /arm/joint_commands
        │                                                    └──► /cmd_vel/vla
        │
        └── /arm/cmd_mode ──► arm_cmd_mux ──► /arm/joint_commands/out ──► arm_driver_node

Control Modes: "nav2" | "vla" | "teleop" | "rl_nav"
Arm Modes:     "policy" | "rl_arm"
```

**Key rule**: SmolVLA is the unified 9-DOF policy (6 arm + 3 base). It is active when `control_mode = "vla"`. Nav2 handles long-range navigation; SmolVLA handles fine manipulation and short-range tasks.

---

## Robot Startup Sequence

### Prerequisites
- Raspberry Pi 5 running Ubuntu 24.04 + ROS 2 Jazzy
- Desktop PC with NVIDIA GPU ≥ 16 GB VRAM (for SmolVLA inference)
- `ROS_DOMAIN_ID=30` set on all machines
- Edit `network.env` with actual IPs before first run

### Step 1 — Build the workspace (once, or after code changes)

```bash
cd ~/OmniBot/robot_ws
colcon build --symlink-install
source install/setup.bash
```

### Step 2 — Start the robot (Raspberry Pi)

```bash
# Option A: Basic robot + state estimation (no navigation)
ros2 launch omnibot_bringup robot.launch.py

# Option B: Robot + perception (cameras + BEV + depth scan)
ros2 launch omnibot_bringup perception.launch.py

# Option C: Full hybrid robot (driver + SLAM + Nav2 + VLA mux + mission planner)
ros2 launch omnibot_hybrid hybrid_robot.launch.py
```

Parameters for hybrid launch:
```bash
ros2 launch omnibot_hybrid hybrid_robot.launch.py \
  use_rviz:=false \          # headless on Pi
  use_foxglove:=true \       # monitor at ws://pi-ip:8765
  use_slam:=true \           # live SLAM mapping
  use_rosbridge:=true        # Android app at ws://pi-ip:9090
```

### Step 3 — Start SmolVLA inference (Desktop GPU PC)

```bash
cd ~/OmniBot/robot_ws
source install/setup.bash
export ROS_DOMAIN_ID=30
export ROS_STATIC_PEERS=<pi-ip>

ros2 launch omnibot_lerobot policy_inference.launch.py \
  checkpoint:=lerobot/smolvla_base \
  device:=cuda
```

After fine-tuning, replace `checkpoint` with your trained model path:
```bash
ros2 launch omnibot_lerobot policy_inference.launch.py \
  checkpoint:=~/checkpoints/smolvla_mobile/best_model
```

### Step 4 — Start the arm driver (Raspberry Pi, if arm attached)

```bash
ros2 launch omnibot_arm arm.launch.py
```

### Step 5 — Verify all topics are live

```bash
# Check base velocity routing
ros2 topic echo /control_mode/active

# Check SmolVLA is receiving images
ros2 topic hz /camera/wrist/image_raw
ros2 topic hz /camera/base/bev/image_raw

# Check arm
ros2 topic hz /arm/joint_states
```

---

## Simulation Startup

### Gazebo (for development without hardware)

```bash
# Terminal 1: Launch Gazebo + robot + Nav2 + SmolVLA mux
ros2 launch omnibot_bringup simulation.launch.py

# Terminal 2 (Desktop GPU): SmolVLA inference
ros2 launch omnibot_lerobot policy_inference.launch.py

# Terminal 3: Full hybrid mission planner
ros2 launch omnibot_hybrid hybrid_robot.launch.py use_sim_time:=true
```

### Isaac Sim (for VLA data collection)

```bash
# 1. Start Isaac Sim — load the OmniBot USD
bash digital_twin/scripts/build_usd.sh   # once per URDF change
# In Isaac Sim GUI: open omnibot.usd, enable omni.isaac.ros2_bridge, press Play

# 2. Launch ROS 2 companion nodes
ros2 launch omnibot_bringup isaac_sim.launch.py

# 3. (Optional) Collect synthetic episodes
python3 data_engine/isaac_sim/collect_episodes.py \
  --config data_engine/isaac_sim/randomization_config.yaml \
  --output ~/datasets/omnibot_synthetic
```

---

## Perception & Mapping

### View all cameras + BEV (workstation)

```bash
# Pi: start cameras + BEV stitcher
ros2 launch omnibot_bringup perception.launch.py

# Workstation: open RViz with perception config
ros2 launch omnibot_bringup perception_viewer.launch.py
```

### SLAM + 3D mapping

```bash
# Pi: 2D SLAM + RTAB-Map 3D + OctoMap
ros2 launch omnibot_bringup slam_3d_mapping.launch.py

# Workstation: view map
ros2 launch omnibot_bringup slam_3d_viewer.launch.py

# Save the map (2D)
ros2 run nav2_map_server map_saver_cli -f ~/maps/omnibot_map

# Switch to localization mode (existing map)
ros2 launch omnibot_bringup slam_3d_mapping.launch.py \
  slam_mode:=localization map_file:=~/maps/omnibot_map
```

---

## Autonomous Navigation (Nav2)

### Navigation requires a map. Run SLAM first to build one, then:

```bash
# Launch Nav2 stack standalone
ros2 launch omnibot_navigation autonomous_navigation.launch.py

# OR use the hybrid launch which includes Nav2
ros2 launch omnibot_hybrid hybrid_robot.launch.py
```

### Send a navigation goal

```bash
# Via mission planner (recommended — handles mode switching automatically)
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'navigate:kitchen'"

# Via Nav2 action server directly
ros2 action send_goal /navigate_to_pose nav2_msgs/action/NavigateToPose \
  "{pose: {header: {frame_id: 'map'}, pose: {position: {x: 2.0, y: 1.0}, orientation: {w: 1.0}}}}"
```

### Add named locations

Edit `robot_ws/src/omnibot_hybrid/config/named_locations.yaml`:

```yaml
locations:
  kitchen:
    x: 3.5        # from SLAM map — run: ros2 topic echo /amcl_pose --once
    y: 1.2
    yaw: 1.5708   # radians
  table:
    x: 1.8
    y: 0.5
    yaw: 0.0
```

---

## Hybrid Control Modes

### Switch modes manually

```bash
# Nav2 autonomous navigation (default)
ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'nav2'"

# SmolVLA unified 9-DOF control (arm + base)
ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'vla'"

# Keyboard/joystick teleop
ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'teleop'"

# RL navigation policy (short-range ≤ 3 m)
ros2 topic pub --once /control_mode std_msgs/msg/String "data: 'rl_nav'"
```

### Mission commands (auto mode switching)

```bash
# Navigate to kitchen, then run SmolVLA task
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'navigate:kitchen,vla:pick up the red cup'"

# SmolVLA task only (no navigation)
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'vla:open the drawer and place the bottle inside'"

# Navigation only
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'navigate:bedroom'"

# RL pipeline: short-range nav + RL arm precision
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'rl_nav:table,rl_arm:pick up the cube'"

# Cancel any running mission
ros2 topic pub --once /mission/cancel std_msgs/msg/String "data: 'cancel'"
```

### Monitor status

```bash
ros2 topic echo /mission/status
ros2 topic echo /control_mode/active
ros2 topic echo /arm/cmd_mode/active
```

---

## Data Collection for Fine-Tuning

OmniBot records **9-DOF demonstrations**: 6 arm joints + 3 base velocities (vx, vy, ω). The SmolVLA policy is trained on this combined action space.

### Method A — ROS 2 Teleop Recorder (recommended for hardware)

Uses leader arm + Xbox controller simultaneously. Records synchronized arm + base + camera data.

```bash
# Step 1: Start the robot with all sensors
ros2 launch omnibot_bringup perception.launch.py
ros2 launch omnibot_arm arm.launch.py teleop_mode:=true   # enables leader arm

# Step 2: Start the recorder
ros2 launch omnibot_lerobot teleop_record.launch.py \
  output_dir:=~/datasets/mobile_manipulation \
  repo_id:=local/mobile_manipulation \
  record_hz:=30.0

# Controls:
#   Hold RB (button 5)  → record frame
#   Hold LB (button 6)  → discard last episode
#   Move leader arm     → arm teleop
#   Left stick          → base vx/vy
#   Right stick         → base rotation

# Step 3: Monitor recording
ros2 topic echo /teleop_recorder/status
```

### Method B — Standalone Lerobot Recorder (no ROS, hardware only)

```bash
cd ~/OmniBot
pip install -r lerobot_engine/requirements.txt

python lerobot_engine/record.py \
  --task "pick up the red cup and place it on the shelf" \
  --num-episodes 50 \
  --output-dir ~/datasets/mobile_manipulation \
  --follower-port /dev/ttyACM0 \
  --leader-port /dev/ttyACM1
```

### Method C — Isaac Sim synthetic data

```bash
python3 data_engine/isaac_sim/collect_episodes.py \
  --config data_engine/isaac_sim/randomization_config.yaml \
  --output ~/datasets/omnibot_synthetic \
  --num-episodes 200 \
  --task "pick and place"
```

### Dataset structure

After recording, your dataset will look like:

```
~/datasets/mobile_manipulation/
├── meta/
│   └── info.json          # episode count, fps, feature shapes
├── data/
│   └── chunk-000/
│       ├── episode_000000.parquet   # observation.state (9D), action (9D)
│       ├── episode_000001.parquet
│       └── ...
└── videos/
    └── chunk-000/
        ├── observation.images.wrist_episode_000000.mp4
        ├── observation.images.bev_episode_000000.mp4
        └── ...
```

**Recommended dataset size**: 50–200 episodes per task for initial fine-tuning.

---

## Fine-Tuning SmolVLA

SmolVLA controls **both arm and base** with a single 9-DOF action space:
- Actions 0–5: arm joint positions (rad)
- Actions 6–8: base velocities (vx m/s, vy m/s, ω rad/s)

### Step 1 — Install training dependencies

```bash
pip install -r lerobot_engine/requirements.txt
# Key packages: lerobot, torch>=2.1, transformers, huggingface_hub
```

### Step 2 — Fine-tune

```bash
cd ~/OmniBot

python lerobot_engine/train.py \
  --dataset-path ~/datasets/mobile_manipulation \
  --pretrained-model lerobot/smolvla_base \
  --output-dir ~/checkpoints/smolvla_mobile \
  --num-epochs 100 \
  --batch-size 8 \
  --lr 1e-4 \
  --chunk-size 50 \
  --device cuda
```

Key training arguments:

| Argument | Default | Notes |
|---|---|---|
| `--pretrained-model` | `lerobot/smolvla_base` | HF model ID or local path |
| `--num-epochs` | `100` | 50 epochs for quick test, 200+ for production |
| `--batch-size` | `8` | Reduce to 4 if OOM |
| `--lr` | `1e-4` | Lower to `5e-5` for fine-grained tasks |
| `--chunk-size` | `50` | Temporal action horizon — keep at 50 |
| `--save-every` | `10` | Checkpoint frequency |

### Step 3 — Evaluate without hardware

```bash
python lerobot_engine/infer.py \
  --checkpoint ~/checkpoints/smolvla_mobile/best_model \
  --task "pick up the red cup" \
  --camera-id 0          # USB webcam for quick test
```

### Step 4 — Resume interrupted training

```bash
python lerobot_engine/train.py \
  --dataset-path ~/datasets/mobile_manipulation \
  --resume ~/checkpoints/smolvla_mobile/checkpoint_epoch_050 \
  --num-epochs 100
```

### Multi-task training (train on multiple tasks together)

Merge datasets before training:

```bash
# Collect multiple tasks into separate directories, then combine:
python lerobot_engine/train.py \
  --dataset-path ~/datasets/mobile_manipulation \   # contains episodes from all tasks
  --pretrained-model lerobot/smolvla_base \
  --output-dir ~/checkpoints/smolvla_multitask \
  --num-epochs 200 \
  --batch-size 4 \
  --lr 5e-5
```

---

## Running the Trained Model

### Deploy on the robot

```bash
# Desktop GPU PC:
ros2 launch omnibot_lerobot policy_inference.launch.py \
  checkpoint:=~/checkpoints/smolvla_mobile/best_model \
  device:=cuda

# Trigger via mission planner:
ros2 topic pub --once /mission/command std_msgs/msg/String \
  "data: 'navigate:kitchen,vla:pick up the red cup'"
```

### TensorRT acceleration (optional, 2–4× inference speedup)

```bash
# Build TRT engine from checkpoint (once per model)
python -m vla_engine.trt.build_engine \
  --checkpoint ~/checkpoints/smolvla_mobile/best_model \
  --output ~/models/smolvla_mobile.trt

# Launch with TRT encoder
ros2 launch omnibot_lerobot policy_inference.launch.py \
  checkpoint:=~/checkpoints/smolvla_mobile/best_model \
  device:=cuda
# Then set params:
ros2 param set /policy_node use_trt true
ros2 param set /policy_node trt_engine_path ~/models/smolvla_mobile.trt
```

---

## Troubleshooting

### Robot base doesn't move when SmolVLA is active
- Check `control_mode/active` is `"vla"`: `ros2 topic echo /control_mode/active`
- Verify SmolVLA is publishing: `ros2 topic hz /cmd_vel/vla`
- The `cmd_vel_mux` only forwards `/cmd_vel/vla` when mode is `"vla"`

### Nav2 fails to start
- Ensure a map is loaded (SLAM or map_server)
- Check all Nav2 lifecycle nodes are active: `ros2 lifecycle list`
- Verify `/scan` topic is publishing: `ros2 topic hz /scan`

### SmolVLA policy outputs all zeros
- Check both camera topics are live: `ros2 topic hz /camera/wrist/image_raw` and `/camera/base/bev/image_raw`
- SmolVLA requires BOTH wrist and BEV images — run `perception.launch.py` or `bev_stitcher_node`
- Enable the policy: `ros2 topic pub --once /policy/enable std_msgs/msg/Bool "data: true"`

### Arm not responding to SmolVLA commands
- Check arm_cmd_mux mode: `ros2 topic echo /arm/cmd_mode/active` (should be `"policy"`)
- Verify arm_driver is running: `ros2 node list | grep arm_driver`
- Check `/arm/joint_commands/out` is publishing: `ros2 topic hz /arm/joint_commands/out`

### Training loss doesn't decrease
- Reduce learning rate to `5e-5`
- Verify dataset has > 20 episodes
- Check data quality: inspect a random episode with `ros2 bag play` or parquet viewer
- Try `--batch-size 4` if GPU memory is tight

### SLAM map drifts
- Run with a real LiDAR scan (`/scan` must come from `depthimage_to_laserscan` or hardware LiDAR)
- Ensure EKF is running: `ros2 topic hz /odometry/filtered`
- Increase `slam_toolbox` resolution in `slam_toolbox_params.yaml`
