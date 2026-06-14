# OmniBot Digital Twin

A contributor-ready simulation environment for working on OmniBot without physical hardware.

---

## Directory Layout

```
digital_twin/
├── README.md                        # this file
├── worlds/
│   ├── omnibot_lab.sdf              # rich indoor lab (table, shelf, YCB objects)
│   └── objects/                     # reusable SDF object fragments
├── scenarios/
│   ├── pick_and_place.yaml          # VLA / manipulation task definitions
│   └── nav_corridor.yaml            # navigation benchmark scenarios
├── docker/
│   ├── Dockerfile.sim               # Ubuntu 24.04 + ROS Jazzy + Gazebo Harmonic
│   ├── docker-compose.yml           # sim + foxglove services
│   ├── docker-compose.gpu.yml       # NVIDIA GPU override for the sim service
│   └── post_create.sh              # DevContainer postCreate: deps + colcon build
├── configs/
│   ├── rviz/
│   │   ├── perception.rviz          # camera feeds, depth, BEV
│   │   ├── navigation.rviz          # map, costmap, path, odom
│   │   └── manipulation.rviz        # arm joints, TF tree, gripper cam
│   ├── foxglove/
│   │   └── omnibot_layout.json      # Foxglove Studio panel layout
│   └── nav2_sim_params.yaml         # simulation-tuned Nav2 params
└── scripts/
    ├── build_usd.sh                 # automates xacro → URDF → Isaac Sim USD
    └── setup_omnigraph.py           # wires OmniGraph ROS 2 bridge inside Isaac Sim
```

---

## Quick-Start (Gazebo Simulation)

### Option A — VS Code DevContainer (recommended)
1. Install [VS Code](https://code.visualstudio.com/) and the
   [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).
2. `git clone https://github.com/varunvaidhiya/OmniBot && cd OmniBot`
3. Open VS Code → **Reopen in Container**. The root `.devcontainer/devcontainer.json`
   builds `digital_twin/docker/Dockerfile.sim` and runs `docker/post_create.sh`,
   which installs deps and runs `colcon build` automatically.
4. Inside the container:
   ```bash
   source robot_ws/install/setup.bash
   ros2 launch omnibot_bringup simulation.launch.py
   ```
   The DevContainer enables GPU passthrough via `--gpus=all` on Linux hosts;
   remove those `runArgs` on Windows/Mac (see the comment in `devcontainer.json`).

### Option B — Docker Compose (headless)
```bash
# Gazebo renders off-screen, Foxglove serves in browser
docker compose -f digital_twin/docker/docker-compose.yml up
# Open https://app.foxglove.dev → Connect → ws://localhost:8765

# NVIDIA host (GPU-accelerated Gazebo) — layer the GPU override:
docker compose \
  -f digital_twin/docker/docker-compose.yml \
  -f digital_twin/docker/docker-compose.gpu.yml up
```

The compose `sim` service launches `simulation.launch.py rviz:=false foxglove:=false`
and runs `foxglove_bridge` as a separate service; ROSBridge (port 9090) is exposed
for the Android app.

### Option C — Native Ubuntu 24.04
```bash
# Prerequisites: ROS 2 Jazzy, Gazebo Harmonic, foxglove_bridge
sudo apt install ros-jazzy-desktop ros-jazzy-ros-gz ros-jazzy-foxglove-bridge
cd robot_ws && colcon build --symlink-install && source install/setup.bash
ros2 launch omnibot_bringup simulation.launch.py
```

---

## Contributor Domain Guide

| Your Area | Launch Command | Visualization |
|-----------|---------------|---------------|
| **Perception / Cameras** | `simulation.launch.py` | Foxglove or `perception.rviz` |
| **SLAM / Mapping** | `simulation.launch.py` then `slam_toolbox.launch.py` | `navigation.rviz` |
| **Nav2 Navigation** | `simulation.launch.py` then `autonomous_robot.launch.py` | `navigation.rviz` |
| **VLA / SmolVLA** | Isaac Sim + `isaac_sim.launch.py` | Foxglove |
| **Arm / Manipulation** | `simulation.launch.py` | `manipulation.rviz` |
| **Android App** | `simulation.launch.py` (ROSBridge on port 9090 auto-starts) | Android app |
| **Dataset Collection** | Isaac Sim + `data_engine/isaac_sim/collect_episodes.py` | Isaac Sim GUI |

---

## Visualization Options

### Foxglove Studio (browser — no GPU needed)
```bash
# Foxglove bridge starts automatically with simulation.launch.py
# Open:
https://app.foxglove.dev
# Connect: ws://localhost:8765
# Import layout: digital_twin/configs/foxglove/omnibot_layout.json
```

### RViz 2 (desktop)
```bash
# Per-domain configs:
rviz2 -d digital_twin/configs/rviz/perception.rviz
rviz2 -d digital_twin/configs/rviz/navigation.rviz
rviz2 -d digital_twin/configs/rviz/manipulation.rviz
```

---

## Simulation Worlds

| World | Use Case | File |
|-------|----------|------|
| `omnibot_world.sdf` (default) | Basic physics + sensor test | `robot_ws/src/omnibot_bringup/worlds/` |
| `omnibot_lab.sdf` | Manipulation + navigation with objects | `digital_twin/worlds/` |

To launch with the lab world (run from the repo root):
```bash
ros2 launch omnibot_bringup simulation.launch.py \
  world:=$(pwd)/digital_twin/worlds/omnibot_lab.sdf
```

---

## Isaac Sim Setup (VLA / Training Data)

Prerequisites:
- NVIDIA GPU with ≥ 16 GB VRAM
- [NVIDIA Omniverse Launcher](https://www.nvidia.com/en-us/omniverse/) with Isaac Sim 4.x installed
- ROS 2 Jazzy sourced in the same shell

```bash
# 1. Generate the USD asset (run once per URDF change)
bash digital_twin/scripts/build_usd.sh

# 2. Start Isaac Sim with ROS 2 bridge enabled (via GUI or:)
#    ~/.local/share/ov/pkg/isaac-sim-*/isaac-sim.sh --enable omni.isaac.ros2_bridge

# 3. In Isaac Sim Script Editor, run:
#    digital_twin/scripts/setup_omnigraph.py

# 4. Launch ROS 2 side
ros2 launch omnibot_bringup isaac_sim.launch.py

# 5. Collect training episodes
python3 data_engine/isaac_sim/collect_episodes.py \
  --config data_engine/isaac_sim/randomization_config.yaml \
  --output ~/datasets/omnibot
```

---

## Key Topics (all domains)

| Topic | Type | Notes |
|-------|------|-------|
| `/scan` | LaserScan | synthesized from depth via `depthimage_to_laserscan` (no physical 2D lidar) — SLAM, Nav2 |
| `/camera/front/image_raw` | Image | 640×480, 30 Hz |
| `/camera/base/bev/image_raw` | Image | 800×800 BEV composite |
| `/camera/wrist/image_raw` | Image | 320×240 gripper cam |
| `/camera/depth/points` | PointCloud2 | Astra Pro — Nav2 voxel layer |
| `/odom` | Odometry | Fused wheel odometry |
| `/map` | OccupancyGrid | SLAM Toolbox 2D map |
| `/arm/joint_states` | JointState | 6-DOF arm feedback |
| `/arm/joint_commands` | JointState | 6-DOF arm commands |
| `/mission/command` | String | e.g. `"navigate:kitchen,vla:find cup"` |

---

## Nav2 Simulation Parameters

The default `nav2_params.yaml` is tuned for hardware. Use the sim-specific
overrides for cleaner costmaps in Gazebo:

```bash
ros2 launch omnibot_navigation autonomous_robot.launch.py \
  params_file:=$(pwd)/digital_twin/configs/nav2_sim_params.yaml
```
