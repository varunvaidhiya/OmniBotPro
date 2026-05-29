#!/usr/bin/env bash
# OmniBot Pi master launcher — starts the complete robot stack on the Pi.
# Run this ONCE. Run launch_omnibot_workstation.sh on the GPU PC at the same time.
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/robot_ws/install/setup.bash"

LAUNCH_FILE="$SCRIPT_DIR/robot_ws/install/omnibot_bringup/share/omnibot_bringup/launch/master/omnibot_pi.launch.py"

echo "========================================"
echo "  OmniBot Pi — Master Launch"
echo "========================================"
echo ""

exec ros2 launch "$LAUNCH_FILE" "$@"
