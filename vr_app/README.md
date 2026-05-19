# OmniBot VR Controller

Unity-based VR application for controlling the OmniBot mecanum-wheel robot
and SO-101 arm. Communicates with the robot via ROSBridge WebSocket (v2 JSON
protocol). Primary target: **Meta Quest 3 / 3S**.

---

## 1. Prerequisites

| Tool | Version |
|---|---|
| Unity Editor | **2023.3.0f1 LTS** (exact) |
| Meta XR SDK Core | 60.0.0 (via OpenUPM scoped registry) |
| Meta XR Interaction SDK | 60.0.0 |
| Meta XR Interaction SDK OVR | 60.0.0 |
| NativeWebSocket | upm branch (auto-fetched via git URL) |
| Newtonsoft JSON | 3.2.1 (via Unity NuGet) |
| Android Build Support | Installed via Unity Hub |
| Android SDK / NDK | API level 32+ (included with Unity Android module) |

Unity packages are declared in `vr_app/Packages/manifest.json` and are
automatically resolved on first open.

---

## 2. Platform Targets

### Meta Quest 3 / 3S (primary)

- OpenXR backend via `com.unity.xr.openxr` 1.10.0
- Meta OpenXR feature set (hand tracking, passthrough)
- Android API target: 32+, ARM64

### Apple Vision Pro (roadmap)

visionOS support is planned but not yet implemented. Architecture notes:

- The ROS/ROSBridge layer is identical on the robot side — no changes needed.
- A separate **SwiftUI + RealityKit** application will replace the Unity app.
- Hand tracking will use **ARKit HandAnchor** (visionOS 1.1+) instead of OVR Hand API.
- IK solver logic (`ArmIKSolver.cs`) can be ported directly to Swift with no
  algorithmic changes — only the Unity `Vector3`/`Quaternion` types need
  substitution with `simd_float3`/`simd_quatf`.
- WebSocket connection to ROSBridge uses `URLSessionWebSocketTask`.

---

## 3. Build Steps for Quest

1. Open `vr_app/` as a Unity project (Unity Hub → Add project from disk).
2. Wait for package resolution (first open takes ~2–5 minutes).
3. Go to **Edit → Project Settings → XR Plug-in Management**.
   - Enable **OpenXR** under the Android tab.
   - Under OpenXR features, enable **Meta Quest support**, **Hand Tracking**, and **Passthrough**.
4. Go to **File → Build Settings**.
   - Switch platform to **Android**.
   - Enable **Development Build** for first deployment (optional).
5. Connect Quest via USB, enable Developer Mode on the headset.
6. Click **Build and Run** (or **Build** to produce an `.apk`).

---

## 4. Setup: Enter Robot IP

1. Put on the headset and launch the app.
2. The **Connection Panel** appears automatically.
3. Enter the robot's IP address (default: `192.168.1.101`) and port (default: `9090`).
4. Toggle **Auto Connect** to connect automatically on future launches.
5. Press **Connect**. The status indicator turns green when connected.

The robot must have ROSBridge running:
```bash
./launch_rosbridge.sh   # or:
ros2 launch rosbridge_server rosbridge_websocket_launch.xml port:=9090
```

---

## 5. Controls Reference

### Base Movement

| Input | Action |
|---|---|
| Left thumbstick Y | Forward / backward (linear.x) |
| Left thumbstick X | Strafe left / right (linear.y) |
| Right thumbstick X | Rotate left / right (angular.z) |
| Right grip (hold) | Turbo mode (2× speed, clamped to 0.2 m/s) |
| **Both grips simultaneously** | **Emergency stop** (held = stop, release = clear) |
| Left controller B | Toggle main HUD panel |
| Right controller A | Cycle through HUD panels |
| Right thumbstick press | Cycle camera feed |

### Hand Tracking Arm Control

| Gesture | Action |
|---|---|
| Right hand position (in workspace) | IK target for arm end-effector |
| Right hand rotation | Wrist orientation target |
| Right index–thumb pinch | Gripper close (pinch=1 → closed, pinch=0 → open) |
| Left hand thumbs-up (hold 0.5 s) | Toggle arm enable/disable |

---

## 6. Hand Tracking Arm Workspace

The arm workspace maps the right hand's position within a 0.40 m radius sphere
(centered on `handWorkspaceOrigin`) to the SO-101 arm's reachable space.

```
              [Headset view — looking down]

        ┌────────────────────────────────────┐
        │                                    │
        │       Hand workspace sphere        │
        │         radius = 0.40 m            │
        │                                    │
        │    [O] = workspace origin           │
        │       (arm base in VR scene)        │
        │                                    │
        │   Move right hand within sphere    │
        │   → IK drives arm to match         │
        │                                    │
        │   Arm reach: L1+L2+L3 = 0.33 m    │
        │     L1 = 0.117 m (upper arm)       │
        │     L2 = 0.133 m (forearm)         │
        │     L3 = 0.080 m (wrist)           │
        └────────────────────────────────────┘
```

Place the `handWorkspaceOrigin` transform in your Unity scene at the robot
arm's base position (0.35 m above the robot base by default).

CCD IK runs at 20 Hz with max 50 iterations and 1 mm tolerance.
All 6 joints are clamped to their configured limits at every step.

---

## 7. Dataset Recording Workflow

1. Open the **Recording Panel** (cycle with right A button).
2. The episode name is auto-filled with a Unix timestamp (`ep_1234567890`).
   Edit it if desired.
3. Press **Start Recording**. The pulsing red dot indicates active recording.
4. Perform the demonstration — all arm states, odometry, and commands are
   captured at 30 Hz locally on the headset.
5. Press **Stop & Save** to finalize. The JSONL file is written to
   `<persistentDataPath>/recordings/<episodeName>.jsonl`.
6. Press **Discard** to throw away the current episode without saving.

### Exporting to the Robot

After recording, export episodes to the robot for training:

1. Ensure the robot VR bridge is running (see section 8).
2. Press **Export to Robot** in the Recording Panel.
3. Each JSONL file is uploaded via HTTP POST to `http://<robotIp>:8765/upload_episode`.
4. Files are saved to `~/datasets/vr_episodes/` on the robot PC.

---

## 8. Launching the ROS VR Bridge

On the robot PC (or VLA desktop):

```bash
# Install dependency (if not already present)
pip install aiohttp

# Build the workspace (first time)
cd robot_ws
colcon build --packages-select omnibot_vr --symlink-install
source install/setup.bash

# Launch
ros2 launch omnibot_vr vr_bridge.launch.py

# Custom upload directory and port
ros2 launch omnibot_vr vr_bridge.launch.py \
    upload_dir:=~/datasets/vr_episodes \
    http_port:=8765
```

The bridge provides:
- `GET  http://<robot>:8765/health` — health check
- `POST http://<robot>:8765/upload_episode` — multipart form upload (field: `file`)

It also mirrors robot sensor data to `/vr/obs` (JSON string) for any
external VR monitoring tools.

---

## 9. Apple Vision Pro Roadmap

The visionOS port is planned for a future release. Key design decisions:

- **No ROS changes required.** The ROSBridge WebSocket protocol and all
  published/subscribed topics are identical.
- **Separate Swift app** using `URLSessionWebSocketTask` for ROSBridge,
  ARKit `HandAnchor` for hand tracking, and RealityKit for scene rendering.
- The `ArmIKSolver` CCD algorithm ports 1:1 to Swift (`simd` math library).
- Hand gesture detection (`GestureDetector`) maps to ARKit skeleton joints.
- Timeline: dependent on Apple shipping `HandAnchor` API stability in
  visionOS 2.x and RealityKit spatial computing API maturity.

---

## 10. Project Structure

```
vr_app/
├── Assets/Scripts/
│   ├── Core/
│   │   ├── Messages/           # ROSBridge JSON message types
│   │   │   ├── ROSBridgeMessage.cs
│   │   │   ├── TwistMsg.cs
│   │   │   ├── JointStateMsg.cs
│   │   │   ├── OdometryMsg.cs
│   │   │   ├── ImuMsg.cs
│   │   │   └── StringMsg.cs    # includes BoolMsg
│   │   ├── ROSBridgeClient.cs  # WebSocket client (singleton)
│   │   ├── ConnectionManager.cs # Connection lifecycle (singleton)
│   │   └── RobotConfig.cs      # All constants
│   ├── Input/
│   │   ├── ArmIKSolver.cs      # CCD IK solver (pure C#)
│   │   ├── GestureDetector.cs  # OVR hand tracking
│   │   ├── HandTrackingArmController.cs
│   │   └── BaseController.cs   # Thumbstick → cmd_vel
│   ├── UI/
│   │   ├── HUDManager.cs       # Floating panel manager
│   │   ├── ConnectionPanel.cs
│   │   ├── TelemetryPanel.cs
│   │   ├── CameraFeedViewer.cs # MJPEG stream viewer
│   │   ├── ControlPanel.cs
│   │   └── RecordingPanel.cs
│   └── Recording/
│       ├── EpisodeManager.cs   # Episode state machine (singleton)
│       └── DatasetRecorder.cs  # 30 Hz local JSONL recorder
├── Packages/manifest.json
└── ProjectSettings/ProjectVersion.txt
```

---

## 11. Troubleshooting

| Problem | Solution |
|---|---|
| Status indicator stays red | Check robot IP, verify ROSBridge is running on port 9090, check `ROS_DOMAIN_ID=30` |
| Hand tracking not working | Ensure Hand Tracking is enabled in Quest developer settings and in XR Plug-in Management |
| Camera feed shows "No Signal" | Ensure `web_video_server` is running: `ros2 run web_video_server web_video_server` |
| Export fails | Check VR bridge is running (`ros2 launch omnibot_vr vr_bridge.launch.py`) and firewall allows port 8765 |
| Arm not moving | Toggle arm enable with left-hand thumbs-up, check `/arm/enable` topic |
