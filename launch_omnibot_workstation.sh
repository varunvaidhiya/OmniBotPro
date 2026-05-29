#!/usr/bin/env bash
# OmniBot Workstation master launcher — starts all GPU AI nodes.
# Run this on the GPU PC after launch_omnibot_pi.sh is up on the Pi.
# Requires: export ROS_DOMAIN_ID=30 && export ROS_STATIC_PEERS=<pi-ip>
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/robot_ws/install/setup.bash"

LAUNCH_FILE="$SCRIPT_DIR/robot_ws/install/omnibot_bringup/share/omnibot_bringup/launch/master/omnibot_workstation.launch.py"

echo "========================================"
echo "  OmniBot Workstation — Master Launch"
echo "========================================"
echo ""

exec ros2 launch "$LAUNCH_FILE" "$@"
