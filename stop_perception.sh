#!/usr/bin/env bash
# Hard-stop all perception nodes, ensuring USB devices and port 8765 are released
# before relaunching. Usage: ./stop_perception.sh

set -e

echo "[stop_perception] Killing all perception processes..."

pkill -9 -f "astra_camera_node"          2>/dev/null || true
pkill -9 -f "usb_cam_node_exe"           2>/dev/null || true
pkill -9 -f "foxglove_bridge"            2>/dev/null || true
pkill -9 -f "depthimage_to_laserscan"    2>/dev/null || true
pkill -9 -f "bev_stitcher"              2>/dev/null || true
pkill -9 -f "robot_state_publisher.*omnibot" 2>/dev/null || true
pkill -9 -f "ros2 launch omnibot_bringup perception" 2>/dev/null || true

echo "[stop_perception] Waiting 6 s for USB devices and ports to release..."
sleep 6

# Verify
if fuser /dev/bus/usb/*/* 2>/dev/null | grep -q .; then
    echo "[stop_perception] WARNING: USB device still held by: $(fuser /dev/bus/usb/*/* 2>/dev/null)"
else
    echo "[stop_perception] USB devices free."
fi

if ss -tlnp | grep -q 8765; then
    echo "[stop_perception] WARNING: port 8765 still in use."
else
    echo "[stop_perception] Port 8765 free."
fi

echo "[stop_perception] Done. Safe to relaunch."
