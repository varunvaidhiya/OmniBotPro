# OmniBot Autonomous Navigation

Autonomous navigation for the OmniBot mecanum-wheel robot, built on the ROS 2
Nav2 stack with `slam_toolbox` 2-D SLAM and an optional RTAB-Map + OctoMap
3-D RGB-D pipeline.

## Features

- **2-D SLAM** with `slam_toolbox`
- **3-D RGB-D SLAM** (optional) with RTAB-Map + OctoMap from the Orbbec Astra
  depth camera
- **Nav2 path planning + obstacle avoidance** (omnidirectional, mecanum)
- **EKF state estimation** via `robot_localization` (fuses wheel odom + IMU)
- **Waypoint following** (`waypoint_navigator`)

## Package Structure

```
omnibot_navigation/
├── config/
│   ├── nav2_params.yaml          # Nav2 parameters (velocity caps match the driver)
│   ├── robot_localization.yaml   # EKF state estimation
│   ├── slam_toolbox_params.yaml  # 2-D SLAM
│   ├── rtabmap_params.yaml       # RTAB-Map 3-D RGB-D SLAM
│   ├── octomap_params.yaml       # OctoMap server (cloud → 2-D projected map)
│   ├── sensor_integration.yaml   # Sensor frame/topic templates
│   └── waypoints.yaml            # Default waypoints
├── docs/
│   └── SENSOR_INTEGRATION.md     # Sensor integration guide
├── launch/
│   ├── autonomous_robot.launch.py        # Top-level: base + description + EKF + SLAM + Nav2 (+ RViz)
│   ├── autonomous_navigation.launch.py   # Nav2 stack only
│   ├── autonomous_with_waypoints.launch.py
│   ├── slam_toolbox.launch.py            # 2-D SLAM
│   └── rtabmap.launch.py                 # Astra driver + RTAB-Map + OctoMap (3-D)
├── scripts/
│   ├── waypoint_navigator.py     # Waypoint navigation node
│   └── test_autonomous.py        # Autonomous test sequence
└── maps/                         # Saved maps (created at runtime)
```

This is an `ament_cmake` package (`CMakeLists.txt` + `package.xml`); the Python
scripts are installed as ROS 2 executables.

## Build

```bash
cd ~/OmniBotPro/robot_ws
colcon build --packages-select omnibot_navigation
source install/setup.bash
```

## Launch

### Full autonomous robot (base + description + EKF + SLAM + Nav2)

```bash
ros2 launch omnibot_navigation autonomous_robot.launch.py
```

`autonomous_robot.launch.py` includes the Yahboom base control
(`omnibot_driver/yahboom_base_control.launch.py`), the robot description, the
shared EKF state estimation (`omnibot_bringup/state_estimation.launch.py`),
`slam_toolbox`, the Nav2 stack, and RViz.

Key launch arguments (defaults):

| Argument | Default | Description |
|---|---|---|
| `use_sim_time` | `false` | Use the Gazebo clock |
| `params_file` | `config/nav2_params.yaml` | Nav2 parameters |
| `autostart` | `true` | Autostart the Nav2 lifecycle |
| `use_slam` | `true` | Run `slam_toolbox` 2-D SLAM |
| `use_3d_mapping` | `false` | Also launch RTAB-Map + OctoMap (requires depth camera) |
| `use_rviz` | `true` | Start RViz |
| `rviz_config_file` | `omnibot_description/config/omnibot_navigation.rviz` | RViz config |

Set navigation goals from RViz with the **2D Nav Goal** tool, or send a
`navigate_to_pose` action goal.

### Waypoint following

```bash
ros2 launch omnibot_navigation autonomous_with_waypoints.launch.py
```

The `waypoint_navigator` node loads `config/waypoints.yaml` and drives the robot
through the listed poses via the Nav2 `navigate_to_pose` /
`navigate_through_poses` actions.

| `waypoint_navigator` param | Default |
|---|---|
| `waypoint_file` | `waypoints.yaml` |
| `loop_waypoints` | `false` |
| `waypoint_timeout` | `30.0` s |

### 2-D SLAM only

```bash
ros2 launch omnibot_navigation slam_toolbox.launch.py
# Drive around, then save the map:
ros2 run nav2_map_server map_saver_cli -f my_map
```

### 3-D RGB-D SLAM (RTAB-Map + OctoMap)

```bash
ros2 launch omnibot_navigation rtabmap.launch.py                 # physical robot
ros2 launch omnibot_navigation rtabmap.launch.py use_sim_time:=true   # Gazebo
```

On the physical robot this starts the Orbbec Astra driver, RTAB-Map RGB-D SLAM,
and an OctoMap server. In simulation the Astra driver is skipped (Gazebo's RGBD
sensor + `ros_gz_bridge` provide the same topics). See the file header of
`launch/rtabmap.launch.py` for the topic layout and Orbbec driver install steps.

## Configuration

- **`nav2_params.yaml`** — velocity caps (0.20 m/s, 1.0 m/s² accel) and
  `min_vel_x: -0.20` match the Yahboom driver's clamp/ramp limiter. AMCL uses
  the `OmnidirectionalMotionModel`.
- **`robot_localization.yaml`** — EKF fuses wheel odom as velocities only and
  the IMU as differential orientation + angular velocity (linear accel
  disabled). It is the single `odom→base_link` TF broadcaster, so the driver
  must run with `publish_tf: False`.
- **`rtabmap_params.yaml` / `octomap_params.yaml`** — consumed by
  `rtabmap.launch.py`.

## Hardware

| Component | Notes |
|---|---|
| Compute | Raspberry Pi 5 (8 GB) |
| Motor board | Yahboom ROS Robot Expansion Board (USB serial) |
| Depth camera | Orbbec Astra Pro (`/camera/color/*`, `/camera/depth/*`) |
| IMU | provided by the Yahboom board → `/imu/data` |
| Odometry | wheel encoders via the Yahboom board → `/odom` |

The Orbbec Astra driver (`ros2_astra_camera`) is built from source, not apt —
see `launch/rtabmap.launch.py` and `docs/SENSOR_INTEGRATION.md`.

## Troubleshooting

```bash
ros2 node list | grep nav2          # Nav2 nodes up?
ros2 topic echo /map                # SLAM producing a map?
ros2 topic echo /odom               # odometry flowing?
ros2 topic echo /imu/data           # IMU flowing?
ros2 topic echo /navigate_to_pose/_action/status   # navigation status
```
