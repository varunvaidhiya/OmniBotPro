# SmolVLA Mobile Manipulation — Training Guide

Complete guide for collecting teleoperation data, converting it to training format, and fine-tuning SmolVLA on the unified mecanum base + SO-101 arm system.

---

## System Overview

The robot is treated as a **single 9-DOF entity**:

| Index | Name | Source | Range |
|-------|------|---------|-------|
| 0 | shoulder_pan | arm encoder | −π … +π rad |
| 1 | shoulder_lift | arm encoder | −π … +π rad |
| 2 | elbow_flex | arm encoder | −π … +π rad |
| 3 | wrist_flex | arm encoder | −π … +π rad |
| 4 | wrist_roll | arm encoder | −π … +π rad |
| 5 | gripper | arm encoder | 0 … 1 (normalised) |
| 6 | base_vx | `/odom` twist | −0.5 … +0.5 m/s |
| 7 | base_vy | `/odom` twist | −0.5 … +0.5 m/s |
| 8 | base_vz | `/odom` twist | −1.5 … +1.5 rad/s |

State = **observation at time t**. Action = **command sent at time t** (arm joint targets + base velocity). SmolVLA predicts `chunk_size = 50` future actions at once.

---

## 1. Hardware Setup

### 1.1 Camera Setup

OmniBot has **6 cameras** fixed in the URDF. No additional setup needed beyond plugging them in.

| # | Camera | Model | Topic | SmolVLA input? |
|---|--------|-------|-------|----------------|
| 1 | Front base | OV9732, 100° FOV | `/camera/front/image_raw` | Via BEV |
| 2 | Rear base | OV9732, 100° FOV | `/camera/rear/image_raw` | Via BEV |
| 3 | Left base | OV9732, 100° FOV | `/camera/left/image_raw` | Via BEV |
| 4 | Right base | OV9732, 100° FOV | `/camera/right/image_raw` | Via BEV |
| 5 | Depth | Orbbec Astra Pro | `/camera/depth/image_raw` `/camera/depth/points` | Nav2 only |
| 6 | Wrist | OV9732, 100° FOV | `/camera/wrist/image_raw` | **Yes** (direct) |

**BEV composite** (`/camera/base/bev/image_raw`): the 4 base cameras are stitched into a single 800×800 top-down image by `ros2_bev_stitcher`. This is what SmolVLA sees as its "environment" view.

**SmolVLA uses:** BEV (global context) + wrist (grasp context). The depth camera feeds Nav2 costmap for obstacle avoidance, not the policy.

**USB device assignment** (verify on your system with `ls /dev/video*`):

| Camera | Device |
|--------|--------|
| Front | `/dev/video0` |
| Rear | `/dev/video2` |
| Left | `/dev/video4` |
| Right | `/dev/video6` |
| Wrist | `/dev/video8` |
| Depth (Orbbec) | USB — auto-detected by `orbbec_camera` driver |

**BEV calibration** — after mounting, run the calibration tool once to compute homography matrices per camera:
```bash
ros2 run ros2_bev_stitcher bev_calibrate
# Follow on-screen prompts to select 4 ground-plane corners per camera
# Saves calibration to ~/.ros/bev_calibration.yaml
```

### 1.2 Teleop Hardware

| Device | Role |
|--------|------|
| Xbox / PS4 controller | Base teleoperation (left stick = vx/vy, right stick = vz) |
| SO-101 leader arm (`/dev/ttyACM1`) | Arm teleoperation (mirrors to follower) |
| SO-101 follower arm (`/dev/ttyACM0`) | Executes mirrored positions |

### 1.3 Verify Before Recording

```bash
# Check USB devices
ls /dev/ttyACM*        # should see ACM0 (follower) + ACM1 (leader)
ls /dev/video*         # one device per USB camera (see §1.1 device table)

# Test cameras individually
ros2 run image_tools showimage --ros-args -r /image:=/camera/front/image_raw
ros2 run image_tools showimage --ros-args -r /image:=/camera/wrist/image_raw

# Verify arm driver is alive
ros2 topic echo /arm/joint_states --once

# Verify odom is publishing
ros2 topic echo /odom --once
```

---

## 2. Arm Calibration (one-time, when arm arrives)

```bash
# Install lerobot tooling
pip install "lerobot[feetech]"

# 1. Configure motor IDs on follower (run once per motor, motors ship as ID=1)
lerobot-setup-motors --robot-type=so101 --robot-port=/dev/ttyACM0 --mock

# 2. Calibrate both arms (move to requested positions, saves ~/.cache/lerobot/)
lerobot-calibrate --robot-type=so101 --robot-port=/dev/ttyACM0  # follower
lerobot-calibrate --robot-type=so101 --robot-port=/dev/ttyACM1  # leader

# 3. Update home_ticks in arm_params.yaml with values from calibration output
nano robot_ws/src/omnibot_arm/config/arm_params.yaml
```

---

## 3. Recording Teleoperation Episodes

### 3.1 Launch Full System

```bash
# Terminal 1 — bring up robot, arm, cameras
./launch_mobile_manipulation.sh

# Terminal 2 — launch teleop recorder node
ros2 launch omnibot_lerobot teleop_record.launch.py \
    output_dir:=/data/episodes \
    task_name:="pick_and_place_cube" \
    fps:=30
```

### 3.2 Recording Controls (Xbox Controller)

| Button | Action |
|--------|--------|
| **RB** (right bumper) | **Toggle recording ON/OFF** |
| **LB** (left bumper) | **Discard current episode** (keeps robot running) |
| Left stick | Base vx / vy (strafe) |
| Right stick X | Base vz (rotation) |
| Left stick click | Emergency stop |

**Leader arm** is always live-mirroring to follower — just move it naturally.

### 3.3 What Gets Recorded Per Frame

The `teleop_recorder_node` captures at `fps` Hz and stores:

```
state  [9D]: current arm joint angles + base odom velocities
action [9D]: arm commands sent + base cmd_vel issued this step
obs/front_image  : 640×480 (CAMERA_FRONT)
obs/wrist_image  : 640×480 (CAMERA_WRIST)
timestamp        : ROS time in nanoseconds
```

Image resolutions come from `data_engine/schema/constants.py` — update that
file if your cameras differ.

Each episode is saved as a **ROS 2 bag** under:
```
/data/episodes/
  pick_and_place_cube_ep000/
  pick_and_place_cube_ep001/
  ...
```

### 3.4 Episode Quality Guidelines

| Metric | Target |
|--------|--------|
| Episode length | 5–20 seconds (150–600 frames @ 30 fps) |
| Episodes per task | ≥ 50 (100+ for robust policies) |
| Task variation | Vary object position ±15 cm, orientation ±45°, lighting |
| Success rate | Record only **successful** episodes — abort with LB if failed |

---

## 4. Converting Bags → Dataset

All data is stored directly in **LeRobot v2.0 format** (Parquet + MP4). No intermediate HDF5 step.

### 4.1 Install Data Engine

```bash
cd data_engine
pip install -e .
```

### 4.2 Single Bag → Dataset Episode

```bash
python -m data_engine.ingestion.bag_to_omnibot \
    --bag /data/episodes/pick_and_place_cube_ep000 \
    --dataset /data/lerobot/pick_and_place_cube \
    --task "pick up the red cube and place it in the bin" \
    --fps 30
```

Each call appends one episode to the dataset. Run again for each bag — `meta/info.json`, `episodes.jsonl`, and `stats.json` are updated automatically.

### 4.3 Batch Convert All Episodes

```bash
python -m data_engine.scripts.ingest_dataset \
    --input-dir /data/episodes \
    --output-dir /data/lerobot/pick_and_place_cube \
    --task "pick up the red cube and place it in the bin" \
    --fps 30
```

### 4.4 Dataset Layout

```
/data/lerobot/pick_and_place_cube/
  meta/
    info.json          ← fps, total_episodes, total_frames, feature shapes
    tasks.jsonl        ← {"task_index":0, "task":"pick up the red cube..."}
    episodes.jsonl     ← {"episode_index":N, "tasks":[0], "length":T}
    stats.json         ← per-feature mean/std/min/max for normalisation
  data/
    chunk-000/
      episode_000000.parquet   ← state[9], action[9], timestamp, indices
      episode_000001.parquet
  videos/
    chunk-000/
      observation.images.front/
        episode_000000.mp4
      observation.images.wrist/
        episode_000000.mp4
```

> The ingestion pipeline (`bag_to_omnibot.py`) writes the **front** and
> **wrist** camera streams. The BEV stream is consumed live by the inference
> node but is not currently exported into the training dataset.

### 4.5 Validate the Dataset

```bash
python -m data_engine.scripts.validate_dataset \
    --dataset /data/lerobot/pick_and_place_cube \
    --verbose

# Visually play back episode 3
python -m data_engine.visualization.visualize_episode \
    --dataset /data/lerobot/pick_and_place_cube \
    --episode 3
```

---

## 5. Training SmolVLA

### 5.1 Prerequisites

```bash
cd lerobot_engine
pip install -r requirements.txt
# Requires: torch>=2.1, CUDA 12+, ~8 GB VRAM minimum (fp16)
# Recommended: RTX 3080 / 4070 or better, or a cloud GPU (Colab A100, RunPod)
```

### 5.2 Run Fine-Tuning

`lerobot_engine/train.py` selects the policy from a registry (`--model`); the
default is `smolvla`.

```bash
python train.py \
    --model smolvla \
    --dataset-path /data/lerobot/pick_and_place_cube \
    --output-dir /data/checkpoints/smolvla_manip_v1 \
    --num-epochs 100 \
    --batch-size 16 \
    --lr 1e-4 \
    --chunk-size 50 \
    --wandb-project omnibot-smolvla   # optional, requires wandb login

python train.py --list-models           # show all registered policy types
```

### 5.3 Key Training Arguments

| Flag | Default | Notes |
|------|---------|-------|
| `--model` | `smolvla` | also: `act`, `diffusion`, `openvla` |
| `--checkpoint` | (model default) | HF hub ID or local path to start from / resume |
| `--num-epochs` | 100 | |
| `--batch-size` | 8 | lower if you hit GPU OOM |
| `--lr` | 1e-4 | |
| `--chunk-size` | 50 | predicted future steps per query |
| `--save-every` | 10 | checkpoint every N epochs |
| `--device` | cuda | |

Per-model architecture/config files live in
`lerobot_engine/configs/models/` (`smolvla.yaml`, `act.yaml`, `diffusion.yaml`)
and the robot/feature config in
`lerobot_engine/configs/robot/omnibot_mobile_manip.yaml`. State and action are
the fixed 9-D mobile-manipulation spec (6 arm + 3 base).

### 5.4 Training on a Cloud GPU (if no local GPU)

```bash
# Option A: HuggingFace Spaces / AutoTrain (easiest)
# Upload /data/lerobot_dataset/ to HF Hub, then use lerobot CLI:
huggingface-cli upload <your-org>/omnibot-pick-place /data/lerobot_dataset/

lerobot train \
    --policy.type=smolvla \
    --dataset.repo_id=<your-org>/omnibot-pick-place \
    --output_dir=outputs/smolvla_v1 \
    --training.num_epochs=100

# Option B: Google Colab / RunPod — rsync dataset + run train.py
```

### 5.5 Monitoring Training

```
outputs/smolvla_v1/
  checkpoints/
    epoch_010/model.pt
    epoch_020/model.pt
    ...
    best/model.pt        ← saved when val loss improves
  train_log.json
```

Key metrics to watch:
- **action_loss** (L1 on predicted vs actual actions) — should decrease steadily
- **val_action_loss** — if this diverges from train loss, you need more data or more regularisation
- Target: val_action_loss < 0.05 for reliable policy

---

## 6. Running Inference

### 6.1 Update Checkpoint Path

```yaml
# robot_ws/src/omnibot_lerobot/config/policy_params.yaml
policy_node:
  ros__parameters:
    model_type: "smolvla"        # smolvla | act | diffusion | openvla
    checkpoint_path: "/data/checkpoints/smolvla_manip_v1/best"
    task_description: "pick up the red cube and place it in the bin"
    base_vel_scale: 0.3          # scales base velocity output (start low)
    policy_hz: 10.0
```

### 6.2 Launch Inference

```bash
# Terminal 1 — full system
./launch_mobile_manipulation.sh

# Terminal 2 — policy inference (smolvla by default)
ros2 launch omnibot_lerobot policy_inference.launch.py
# or override the checkpoint / model:
ros2 launch omnibot_lerobot policy_inference.launch.py \
    model_type:=smolvla checkpoint:=/data/checkpoints/smolvla_manip_v1/best

# Terminal 3 — enable the policy (robot will start moving)
ros2 topic pub /policy/enable std_msgs/Bool "data: true" --once

# To change task at runtime:
ros2 topic pub /policy/task std_msgs/String \
    "data: 'pick up the blue block'" --once
```

### 6.3 Safety During Inference

- Keep Xbox controller in hand — **left stick click = emergency stop** via `/emergency_stop`
- Start with a low `base_vel_scale` (e.g. 0.3) until confident
- Run in a clear 2m × 2m area

---

## 7. Iterative Improvement

```
Record 50 demos → Train → Test → Identify failures
         ↑                              |
         └──── Record 20 more demos ←──┘
               focused on failure cases
```

### 7.1 Dataset Expansion

Just run the ingest script again pointing at new bags — it appends to the existing dataset:

```bash
python -m data_engine.scripts.ingest_dataset \
    --input-dir /data/episodes_v2 \
    --output-dir /data/lerobot/pick_and_place_cube \
    --task "pick up the red cube and place it in the bin"

# Continue training from a previous checkpoint
python lerobot_engine/train.py \
    --model smolvla \
    --dataset-path /data/lerobot/pick_and_place_cube \
    --output-dir /data/checkpoints/smolvla_manip_v2 \
    --checkpoint /data/checkpoints/smolvla_manip_v1/best \
    --num-epochs 50
```

### 7.2 Multi-Task Training

Point multiple ingest runs at the same output directory with different `--task` strings:

```bash
python -m data_engine.scripts.ingest_dataset \
    --input-dir /data/bags/pick_place \
    --output-dir /data/lerobot/multi_task \
    --task "pick up the red cube and place it in the bin"

python -m data_engine.scripts.ingest_dataset \
    --input-dir /data/bags/push_button \
    --output-dir /data/lerobot/multi_task \
    --task "push the green button"
```

SmolVLA conditions on the task string at inference time, so multi-task training works out of the box.

---

## 8. File Reference

| File | Purpose |
|------|---------|
| `lerobot_engine/record.py` | Standalone recorder (no ROS, for bench testing) |
| `lerobot_engine/train.py` | Fine-tune a registry policy (smolvla/act/diffusion/openvla) |
| `lerobot_engine/infer.py` | Standalone inference test (no robot) |
| `lerobot_engine/configs/models/smolvla.yaml` | SmolVLA architecture/config |
| `lerobot_engine/configs/robot/omnibot_mobile_manip.yaml` | Robot feature/normalisation config |
| `robot_ws/src/omnibot_lerobot/omnibot_lerobot/teleop_recorder_node.py` | ROS 2 recorder (live robot) |
| `robot_ws/src/omnibot_lerobot/omnibot_lerobot/policy_node.py` | ROS 2 inference node (multi-backend) |
| `robot_ws/src/omnibot_lerobot/config/policy_params.yaml` | Inference node parameters |
| `robot_ws/src/omnibot_arm/config/arm_params.yaml` | Arm motor IDs, ports, home ticks |
| `data_engine/ingestion/bag_to_omnibot.py` | ROS bag → LeRobot Parquet + MP4 |
| `data_engine/ingestion/ros_parser.py` | Low-level ROS bag parsing (cameras, odom, cmd_vel) |
| `data_engine/ingestion/sync_topics.py` | Multi-modal timestamp synchronisation |
| `data_engine/schema/constants.py` | 9D state/action spec, camera configs |
| `data_engine/loader/dataset.py` | Lightweight PyTorch Dataset over Parquet files |
| `data_engine/scripts/ingest_dataset.py` | Batch convert bags → dataset |
| `data_engine/scripts/validate_dataset.py` | Validate LeRobot dataset integrity |
| `data_engine/visualization/visualize_episode.py` | Play back episodes with OpenCV overlay |

---

## 9. Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| Base moves too fast during inference | `base_vel_scale` too high | Lower it in `policy_params.yaml` |
| Base drifts unexpectedly | Odom not calibrated | Check wheel geometry in `omnibot_driver` params |
| `chunk_size` mismatch error | Config vs checkpoint mismatch | Ensure same `chunk_size=50` at train + infer |
| Low val accuracy despite many demos | Insufficient task variation | Vary object position, lighting, start pose |
| GPU OOM during training | Batch too large | Reduce `--batch-size` |
| Camera topics missing at record time | USB cam not detected | Check `ls /dev/video*`, update device in `mobile_manipulation.launch.py` |
| Dataset validation fails (`frame_count mismatch`) | Recording dropped frames | Reduce `fps` to 15 or improve USB bandwidth (dedicated USB controller) |
