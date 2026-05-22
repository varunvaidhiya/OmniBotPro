#!/bin/bash
set -e

apt-get update
apt-get install -y python3-colcon-common-extensions python3-rosdep python3-pip git

pip3 install "numpy<2.0" pytest pytest-cov pytest-timeout coverage prometheus_client>=0.20 httpx --break-system-packages

apt-get install -y \
  ros-jazzy-cv-bridge \
  ros-jazzy-tf-transformations \
  ros-jazzy-tf2-ros \
  ros-jazzy-tf2-geometry-msgs \
  ros-jazzy-nav2-msgs \
  ros-jazzy-xacro \
  ros-jazzy-robot-state-publisher \
  ros-jazzy-joint-state-publisher \
  ros-jazzy-joy \
  ros-jazzy-teleop-twist-joy \
  ros-jazzy-ament-cmake-pytest \
  ros-jazzy-ament-copyright \
  ros-jazzy-ament-lint-auto \
  ros-jazzy-ament-lint-common \
  ros-jazzy-diagnostic-msgs \
  ros-jazzy-sensor-msgs \
  ros-jazzy-nav-msgs \
  ros-jazzy-geometry-msgs \
  ros-jazzy-std-msgs \
  python3-serial \
  python3-aiohttp \
  python3-transforms3d \
  libboost-dev

source /opt/ros/jazzy/setup.bash

cd /workspace/robot_ws
git clone https://github.com/RoverRobotics-forks/serial-ros2.git src/serial || true

colcon build

colcon test
colcon test-result --verbose
