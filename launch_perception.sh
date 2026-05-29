#!/usr/bin/env bash
# OmniBot Perception Launcher
# Shows a GUI checklist to select which compute-intensive nodes to enable,
# then launches perception.launch.py with the chosen options.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROS_SETUP="$SCRIPT_DIR/robot_ws/install/setup.bash"

# ── Ask user which modules to enable ─────────────────────────────────────────
CHOICES=$(zenity --list \
  --title="OmniBot Perception Launcher" \
  --text="Select modules to enable:\n(uncheck to save CPU on Raspberry Pi)" \
  --checklist \
  --column="Enable" \
  --column="Module" \
  --column="CPU cost" \
  --column="What it does" \
  --width=700 --height=400 \
  TRUE  "depth_camera"  "~69% CPU"  "Astra Pro RGB-D — depth image, point cloud, /scan" \
  TRUE  "bev"           "~65% CPU"  "BEV stitcher — 4 base cams → single fused image for VLA" \
  TRUE  "foxglove"      "~29% CPU"  "Foxglove bridge — ws://pi-ip:8765 (always streaming)" \
  TRUE  "web_video"     "~2% idle"  "web_video_server — http://pi-ip:8080 (only when browser open)" \
  --separator="|" 2>/dev/null) || { echo "Cancelled."; exit 0; }

# ── Build launch arguments from selection ────────────────────────────────────
depth_camera=false
bev=false
foxglove=false
web_video=false

IFS="|" read -ra SELECTED <<< "$CHOICES"
for item in "${SELECTED[@]}"; do
  case "$item" in
    depth_camera) depth_camera=true ;;
    bev)          bev=true ;;
    foxglove)     foxglove=true ;;
    web_video)    web_video=true ;;
  esac
done

# ── Summary ───────────────────────────────────────────────────────────────────
echo "========================================"
echo "  OmniBot Perception"
echo "========================================"
echo "  depth_camera : $depth_camera"
echo "  bev          : $bev"
echo "  foxglove     : $foxglove"
echo "  web_video    : $web_video"
echo ""
if [ "$web_video" = "true" ]; then
  echo "  Browser streams: http://$(hostname -I | awk '{print $1}'):8080"
fi
if [ "$foxglove" = "true" ]; then
  echo "  Foxglove:        ws://$(hostname -I | awk '{print $1}'):8765"
fi
echo "========================================"
echo ""

# ── Stop any existing perception stack ───────────────────────────────────────
if pgrep -f "usb_cam_node_exe\|astra_camera_node\|foxglove_bridge\|web_video_server\|bev_stitcher" > /dev/null 2>&1; then
  echo "Stopping existing perception stack..."
  "$SCRIPT_DIR/stop_perception.sh"
fi

# ── Source ROS and launch ─────────────────────────────────────────────────────
source "$ROS_SETUP"

echo "Launching perception stack..."
echo ""

exec ros2 launch omnibot_bringup perception.launch.py \
  depth_camera:=$depth_camera \
  bev:=$bev \
  foxglove:=$foxglove \
  web_video:=$web_video
