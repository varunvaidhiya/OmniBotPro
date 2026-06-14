# OmniBot Reusable Packages

Standalone packages extracted from the OmniBot monorepo, each independently
releasable to the broader robotics community.

| Package | Language | Registry | Description |
|---|---|---|---|
| [`yahboom_ros2`](yahboom_ros2/) | Python | ROS index | ROS 2 driver for Yahboom expansion board — includes the **reverse-engineered serial protocol** |
| [`mecanum_drive_ros2`](mecanum_drive_ros2/) | C++17 + Python | ROS index | Generic mecanum wheel kinematics (header-only C++ + Python mirror) |
| [`ros2_bev_stitcher`](ros2_bev_stitcher/) | Python | ROS index + PyPI | Multi-camera Bird's Eye View compositor with calibration tool |
| [`vla_serve`](vla_serve/) | Python | PyPI | Model-agnostic VLA inference REST server (FastAPI + Docker) |
| [`robot_episode_dataset`](robot_episode_dataset/) | Python | PyPI | LeRobot-compatible episode dataset library for imitation learning |
| [`rosbridge-android`](rosbridge-android/) | Kotlin | JitPack | Android ROSBridge client library + custom views (joystick, SLAM map) |

## Quick Install

### ROS 2 packages (Jazzy)
```bash
cd ~/ros2_ws/src
# Symlink the packages you want
ln -s /path/to/OmniBotPro/packages/yahboom_ros2 .
ln -s /path/to/OmniBotPro/packages/mecanum_drive_ros2 .
ln -s /path/to/OmniBotPro/packages/ros2_bev_stitcher .
cd ~/ros2_ws && colcon build
```

### Python packages (PyPI)
```bash
pip install vla-serve robot-episode-dataset
```

### Android (JitPack)
```gradle
implementation 'com.github.varunvaidhiya.OmniBotPro:lib:1.0.0'
```

## Release Status

| Package | Version | Status |
|---|---|---|
| `yahboom_ros2` | 1.0.0 | Ready for bloom-release |
| `mecanum_drive_ros2` | 1.0.0 | Ready for bloom-release |
| `ros2_bev_stitcher` | 1.0.0 | Ready for bloom-release + PyPI |
| `vla_serve` | 1.0.0 | Ready for PyPI (`python -m build && twine upload`) |
| `robot_episode_dataset` | 1.0.0 | Ready for PyPI |
| `rosbridge-android` | 1.0.0 | Ready for JitPack (tag v1.0.0) |
