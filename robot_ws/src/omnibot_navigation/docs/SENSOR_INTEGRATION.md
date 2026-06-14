# Sensor Integration Guide

How the OmniBot navigation stack consumes its sensors, and how to bring up the
depth camera driver that is not vendored in this repo.

## Sensor overview

| Sensor | Purpose | Topics | Frame |
|---|---|---|---|
| Orbbec Astra Pro (RGB-D) | Mapping, obstacle avoidance, 3-D SLAM | `/camera/color/image_raw`, `/camera/color/camera_info`, `/camera/depth/image_raw`, `/camera/depth/camera_info`, `/camera/depth/points` | `depth_camera_link` |
| IMU (on the Yahboom board) | Orientation + angular velocity for the EKF | `/imu/data` | `imu_link` |
| Wheel encoders (Yahboom board) | Wheel odometry for the EKF | `/odom` | `odom` → `base_link` |

There is no separate USB webcam or external IMU in the active stack — the IMU
and encoders are read by `omnibot_driver` over the Yahboom serial link, and the
Astra Pro provides both colour and depth.

## Depth camera: Orbbec Astra Pro

The Astra driver (`ros2_astra_camera`) is **not on apt and is not vendored** —
it must be built from source.

```bash
sudo apt install libgflags-dev ros-jazzy-image-geometry \
    ros-jazzy-camera-info-manager ros-jazzy-image-transport \
    ros-jazzy-image-publisher libgoogle-glog-dev libusb-1.0-0-dev libeigen3-dev
cd ~/OmniBotPro/robot_ws/src
git clone https://github.com/orbbec/ros2_astra_camera --depth 1
cd ~/OmniBotPro/robot_ws && colcon build --packages-select astra_camera
```

`rtabmap.launch.py` starts the `astra_camera` driver automatically on the
physical robot (and skips it under `use_sim_time:=true`, where Gazebo's RGBD
sensor + `ros_gz_bridge` publish the same `/camera/*` topics).

## Depth → LaserScan (for 2-D costmaps)

Nav2's local costmap consumes a laser scan. `depthimage_to_laserscan` converts
the Astra depth image to `/scan`:

```bash
sudo apt install ros-jazzy-depthimage-to-laserscan
```

Use `output_frame: depth_camera_link` (the x-forward camera link), **never** the
optical frame.

## EKF fusion

The EKF (`config/robot_localization.yaml`, launched via
`omnibot_bringup/state_estimation.launch.py`) fuses:

- **`/odom`** — wheel odometry, velocities only (`vx, vy, vyaw`); mecanum slip
  makes absolute encoder pose untrustworthy.
- **`/imu/data`** — differential orientation + angular velocity; linear
  acceleration is disabled (noisy MEMS).

The EKF is the only `odom→base_link` TF broadcaster, so the driver runs with
`publish_tf: False`.

## Config templates

`config/sensor_integration.yaml` holds per-sensor frame/topic/covariance
templates. (It still references generic placeholder hardware names; the
authoritative topic/frame map is the table above.)

## Quick checks

```bash
ros2 topic echo /camera/depth/image_raw --once   # depth flowing?
ros2 topic echo /camera/depth/points --once      # point cloud?
ros2 topic echo /scan --once                      # depth→laserscan?
ros2 topic echo /imu/data --once                  # IMU?
ros2 topic echo /odometry/filtered --once         # EKF output?
```
