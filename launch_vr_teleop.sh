#!/usr/bin/env bash
# ============================================================
#  launch_vr_teleop.sh — Single-click VR mixed-reality teleop
#  OmniBot: base + SO-101 arm + cameras + ROSBridge + VR bridge
#  + web_video_server — everything the Quest 3 OhhO app needs
# ============================================================
#
#  What this starts (one command, Ctrl+C stops all):
#    1. Mobile manipulation bringup  (base driver + arm + cameras)
#    2. ROSBridge WebSocket          (port 9090 — headset connects here)
#    3. VR recording bridge          (port 8765 — episode upload + record sync)
#    4. web_video_server             (port 8080 — MJPEG camera feed in-headset)
#
#  On the Quest: sign in → Pilot → pick the robot → enter this PC's
#  IP + port 9090 → Connect. Recordings export back to port 8765.
# ============================================================
set -eo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE="${SCRIPT_DIR}/robot_ws"

# ---- Tunables (env overrides allowed) ----
ROSBRIDGE_PORT="${ROSBRIDGE_PORT:-9090}"
VR_BRIDGE_PORT="${VR_BRIDGE_PORT:-8765}"
WEB_VIDEO_PORT="${WEB_VIDEO_PORT:-8080}"
UPLOAD_DIR="${UPLOAD_DIR:-~/datasets/vr_episodes}"

# ---- Deployment mode + DDS peer discovery ----
DEPLOY_ENV="${SCRIPT_DIR}/deployment.env"
NETWORK_ENV="${SCRIPT_DIR}/network.env"

if [[ -f "${DEPLOY_ENV}" ]]; then
    source "${DEPLOY_ENV}"
elif [[ -f "${NETWORK_ENV}" ]]; then
    source "${NETWORK_ENV}"
    DEPLOY_MODE="multi"
else
    echo "[WARN] No deployment.env found — run: python deploy.py"
    DEPLOY_MODE="multi"
fi

export ROS_DOMAIN_ID="${ROS_DOMAIN_ID:-30}"

if [[ "${DEPLOY_MODE}" == "single" ]]; then
    unset ROS_STATIC_PEERS
    echo "[INFO] Deployment: single workstation (no DDS peers)"
else
    export ROS_STATIC_PEERS="${VLA_PC_IP:-${WORKSTATION_IP}};${PI_IP};${SIM_PC_IP}"
    echo "[INFO] Deployment: multi  |  DDS peers: ${ROS_STATIC_PEERS}"
fi

# ---- Source ROS 2 base ----
ROS_DISTRO="${ROS_DISTRO:-jazzy}"
ROS_SETUP="/opt/ros/${ROS_DISTRO}/setup.bash"

if [[ ! -f "${ROS_SETUP}" ]]; then
    echo "[ERROR] ROS 2 setup not found at ${ROS_SETUP}."
    echo "        Set ROS_DISTRO env var to your distro (e.g. ROS_DISTRO=iron)."
    exit 1
fi

# shellcheck source=/dev/null
source "${ROS_SETUP}"

# ---- Source workspace overlay (build if needed) ----
WORKSPACE_SETUP="${WORKSPACE}/install/setup.bash"
if [[ ! -f "${WORKSPACE_SETUP}" ]]; then
    echo "[INFO] Workspace not built. Running colcon build first..."
    cd "${WORKSPACE}"
    colcon build --symlink-install
    echo "[INFO] Build complete."
fi

# shellcheck source=/dev/null
source "${WORKSPACE}/install/setup.bash"

# ---- Check required / optional dependencies ----
FATAL=0

if ! ros2 pkg list 2>/dev/null | grep -q "^rosbridge_server$"; then
    echo "[ERROR] rosbridge_server not found. Install it with:"
    echo "        sudo apt install ros-${ROS_DISTRO}-rosbridge-suite"
    FATAL=1
fi

if ! ros2 pkg list 2>/dev/null | grep -q "^omnibot_vr$"; then
    echo "[ERROR] omnibot_vr package not found in the workspace."
    echo "        Rebuild: cd robot_ws && colcon build --packages-select omnibot_vr --symlink-install"
    FATAL=1
fi

if ! python3 -c "import aiohttp" 2>/dev/null; then
    echo "[ERROR] Python module 'aiohttp' not found (needed by the VR bridge)."
    echo "        Install it with: pip install aiohttp"
    FATAL=1
fi

if [[ ${FATAL} -eq 1 ]]; then
    exit 1
fi

if ! ros2 pkg list 2>/dev/null | grep -q "^web_video_server$"; then
    echo "[WARN] web_video_server not found — the in-headset camera feed will show 'No Signal'."
    echo "       Install it with: sudo apt install ros-${ROS_DISTRO}-web-video-server"
    HAS_WEB_VIDEO=0
else
    HAS_WEB_VIDEO=1
fi

# ---- Local IP for the headset ----
LOCAL_IP=$(hostname -I | awk '{print $1}')

echo ""
echo "┌──────────────────────────────────────────────────────────┐"
echo "│   🥽  OmniBot VR Mixed-Reality Teleoperation Stack       │"
echo "│                                                          │"
echo "│   ROS Domain ID: ${ROS_DOMAIN_ID}                                    │"
echo "│   Headset connects to:                                   │"
echo "│     ROSBridge (robot control): ws://${LOCAL_IP}:${ROSBRIDGE_PORT}   │"
echo "│     VR bridge (episodes):      http://${LOCAL_IP}:${VR_BRIDGE_PORT}  │"
echo "│     Camera feed (MJPEG):       port ${WEB_VIDEO_PORT}                     │"
echo "│                                                          │"
echo "│   Quest app: sign in → Pilot → robot → enter IP above    │"
echo "│   Press Ctrl+C to stop all nodes                         │"
echo "└──────────────────────────────────────────────────────────┘"
echo ""

# ---- Launch everything in the background ----
PIDS=()

echo "[1/4] Mobile manipulation bringup (base + arm + cameras)..."
ros2 launch omnibot_bringup mobile_manipulation.launch.py &
PIDS+=($!)
sleep 2

echo "[2/4] ROSBridge WebSocket server (port ${ROSBRIDGE_PORT})..."
ros2 launch rosbridge_server rosbridge_websocket_launch.xml port:="${ROSBRIDGE_PORT}" &
PIDS+=($!)

echo "[3/4] VR recording bridge (port ${VR_BRIDGE_PORT}, uploads → ${UPLOAD_DIR})..."
ros2 launch omnibot_vr vr_bridge.launch.py \
    http_port:="${VR_BRIDGE_PORT}" upload_dir:="${UPLOAD_DIR}" &
PIDS+=($!)

if [[ "${HAS_WEB_VIDEO}" -eq 1 ]]; then
    echo "[4/4] web_video_server (port ${WEB_VIDEO_PORT})..."
    ros2 run web_video_server web_video_server --ros-args -p port:="${WEB_VIDEO_PORT}" &
    PIDS+=($!)
else
    echo "[4/4] web_video_server SKIPPED (not installed)."
fi

echo ""
echo "[INFO] VR teleop stack is up. Put on the Quest and connect."
echo ""

# ---- Cleanup on Ctrl+C / termination ----
cleanup() {
    echo ""
    echo "[INFO] Shutting down VR teleop stack..."
    for pid in "${PIDS[@]}"; do
        kill "${pid}" 2>/dev/null || true
    done
    wait 2>/dev/null || true
    echo "[INFO] All nodes stopped."
    exit 0
}
trap cleanup SIGINT SIGTERM

# ---- Wait for any background job to exit, then tear down ----
wait -n 2>/dev/null || wait
echo "[WARN] A node exited unexpectedly — shutting down the stack."
cleanup
