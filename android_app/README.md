# OmniBot Android App

Native Kotlin MVVM controller app for the OmniBot mecanum-wheel mobile-manipulation robot.
Connects over ROSBridge WebSocket (default `ws://192.168.1.100:9090`) and provides full
robot control, 3D visualisation, real-time mapping, and natural-language AI mission control.

<p align="left">
  <img src="https://img.shields.io/badge/Android-SDK_24+-brightgreen"/>
  <img src="https://img.shields.io/badge/Kotlin-2.1.0-purple"/>
  <img src="https://img.shields.io/badge/SceneView-4.8.0-blue"/>
  <img src="https://img.shields.io/badge/Hilt-DI-orange"/>
</p>

---

## Features

| Feature | Status | Notes |
|---------|--------|-------|
| ROSBridge WebSocket connection | ✅ | Auto-reconnect, exponential back-off, 5 retries |
| Connection status indicator | ✅ | Green/orange/red dot, top-right all screens |
| Live camera feed (MJPEG) | ✅ | `web_video_server`, Content-Length + JPEG-marker parser |
| Camera fullscreen dialog | ✅ | Tap fullscreen button; dark overlay with close btn |
| BEV map overlay on camera | ✅ | Toggle BEV icon; robot footprint on camera card |
| Dataset recording button | ✅ | REC badge in camera card; publishes to `/rosbag_recorder/start` and `/rosbag_recorder/stop` |
| Dark mode | ✅ | Force-dark Night mode; `values-night/colors.xml` |
| OmniBot logo in toolbar | ✅ | Custom robot-head vector drawable |
| **Observability tab** | ✅ | Grafana-style health / training / logs / alerts via Prometheus, Loki, AlertManager, W&B HTTP APIs |
| **OTA update tab** | ✅ | Check / apply / rollback robot workspace + models via `/ota/*` ROS services |
| **2D SLAM map tab** | ✅ | Full-screen `SlamMapView` with robot pose overlay |
| SLAM map — robot pose panel | ✅ | X / Y / θ live readout, bottom-left |
| SLAM map — map stats panel | ✅ | Resolution, explored area m², occupied cells, bottom-right |
| SLAM map — zoom hint | ✅ | Pinch/pan hint fades out after 3 s |
| **3D Point Cloud tab** | ✅ | Canvas-based `PointCloudView`, Jet colormap depth render |
| Point cloud — pinch to zoom | ✅ | ScaleGestureDetector, min/max zoom clamped |
| Point cloud — stats overlay | ✅ | Point count, max range, render FPS |
| **3D Robot Viewer tab** | ✅ | SceneView 4.8.0 (Google Filament), loads `robot.glb` |
| Robot viewer — GLB model | ✅ | Named nodes match URDF link names; `tools/urdf_to_glb.py` |
| Robot viewer — live pose | ✅ | Model moves on floor plane from `/odom` |
| Robot viewer — arm animation | ✅ | Per-joint rotation applied from `/arm/joint_states` |
| Robot viewer — tap to navigate | ✅ | Ray-cast vs y=0 plane → Nav2 goal published |
| Robot viewer — no-model banner | ✅ | Instructions shown when `robot.glb` not in assets |
| **AI chat interface** | ✅ | "Hi Varun, what do you want me to do?" greeting |
| AI chat — natural language commands | ✅ | Structured commands → `/mission/command`; free-form → `/ai/command` (LangGraph), with `/mission/command` fallback |
| AI chat — quick chips | ✅ | One-tap: Navigate Home / Pick Up Object / Return to Dock |
| AI chat — mission status updates | ✅ | Robot replies streamed from `/mission/status` |
| AI chat — dataset recording toggle | ✅ | Start/stop recording from chat panel |
| Velocity charts (vx / vy / vz) | ✅ | Rolling line chart (300 points), bottom of Dashboard |
| Odometry readout | ✅ | X / Y / θ on Dashboard |
| Hilt dependency injection | ✅ | All ViewModels inject `RobotRepository` singleton |
| Coroutines + StateFlow | ✅ | All live data as StateFlow |

---

## Navigation Structure

```
MainActivity  (Navigation Component — destinations in mobile_navigation.xml)
├── Home          — landing screen
├── Dashboard     — camera feed, BEV overlay, velocity chart, odometry
├── Map           — ViewPager2 (3 sub-tabs)
│   ├── 2D SLAM   — OccupancyGrid map with live robot pose
│   ├── 3D Cloud  — PointCloud2 depth visualisation
│   └── 3D Robot  — SceneView GLB model + tap-to-navigate
├── AI            — natural language chat + dataset recording
├── Controls      — virtual joystick + arm joint sliders
├── Observability — ViewPager2: Health / Training / Logs / Alerts
├── OTA           — over-the-air workspace + model updates
└── Settings      — robot IP, port, camera URL
```

---

## Project Structure

```
app/src/main/
├── kotlin/com/varunvaidhiya/robotcontrol/
│   ├── MainActivity.kt
│   ├── RobotApplication.kt           # @HiltAndroidApp entry point
│   ├── di/                           # Hilt modules: AppModule, NetworkModule, RepositoryModule
│   ├── data/
│   │   ├── models/                   # OdomData, MapData, MotorData, ObservabilityModels, …
│   │   ├── preferences/AppPreferences.kt   # DataStore-backed settings
│   │   ├── remote/                   # Retrofit APIs: Prometheus, Loki, AlertManager, WandB
│   │   └── repository/
│   │       ├── RobotRepository.kt    # Single source of truth, all StateFlows (ROS + OTA)
│   │       └── ObservabilityRepository.kt  # Metrics / logs / alerts / training runs
│   ├── network/
│   │   ├── ROSBridgeManager.kt       # OkHttp WebSocket + ROSBridge v2 JSON (publish/subscribe/callService)
│   │   └── ROSBridgeListener.kt
│   ├── utils/
│   │   ├── Constants.kt              # IP/port defaults, topic + service names, ARM_JOINT_NAMES
│   │   └── DataLogger.kt
│   └── ui/
│       ├── common/                   # BaseFragment, ViewModelFactory
│       ├── home/HomeFragment.kt      # Landing screen (start destination)
│       ├── dashboard/                # DashboardFragment (camera, BEV, charts) + ViewModel
│       ├── camera/CameraViewModel.kt # MJPEG stream URL builder
│       ├── mapping/                  # MapFragment (ViewPager2), PointCloudFragment + ViewModel
│       ├── slam/                     # SlamMapFragment + SlamViewModel
│       ├── viewer/                   # RobotViewerFragment (SceneView 3D) + ViewModel
│       ├── ai/                       # AIFragment (chat + recording), AIViewModel, ChatAdapter
│       ├── controls/                 # ControlsFragment (joystick + arm sliders) + ViewModel
│       ├── observability/            # ObservabilityFragment (ViewPager2) + health/training/logs/alerts
│       ├── ota/                      # OTAFragment + OTAViewModel
│       ├── settings/                 # SettingsFragment + SettingsViewModel
│       └── views/                    # PointCloudView, SlamMapView, MjpegView, VirtualJoystickView,
│                                     # JarvisView, CameraHudOverlay
├── res/
│   ├── layout/                       # All XML layouts
│   ├── drawable/                     # Vector icons + backgrounds
│   ├── values/                       # Colors, strings, themes
│   ├── values-night/colors.xml       # Dark mode color overrides
│   └── navigation/mobile_navigation.xml
└── assets/
    └── robot.glb                     # Generated — see 3D Robot Viewer Setup below (not checked in)
```

---

## 3D Robot Viewer Setup

The **3D Robot** tab loads `robot.glb` from `app/src/main/assets/`. This binary is not
checked in. Generate it from the URDF + STL meshes (run from the repo root):

```bash
pip install trimesh[easy] numpy lxml
python tools/urdf_to_glb.py
# Output: android_app/app/src/main/assets/robot.glb
```

Then rebuild the Android app so assets are re-packaged.

The script expands `omnibot.urdf.xacro`, loads all STL meshes, applies joint origin
transforms, and exports a GLB where every node is named after its URDF link
(e.g. `arm_shoulder_pan`). The viewer finds nodes by name and applies live joint
rotations from `/arm/joint_states`.

If `robot.glb` is absent the viewer still shows live pose and joint angles as text
overlays, and displays setup instructions.

---

## Dataset Recording (from the App)

Tap the **REC** button on the Dashboard camera card, or use the toggle in the AI chat
panel. This publishes a bag name (`std_msgs/String`) to `/rosbag_recorder/start` and an
`std_msgs/Empty` to `/rosbag_recorder/stop`.

On the robot side, the `rosbag_recorder` node (in `omnibot_hybrid`) handles these signals;
it is started by the hybrid bringup:

```bash
ros2 launch omnibot_hybrid hybrid_robot.launch.py
```

It writes a `ros2 bag` to the configured directory when it receives the start signal and
finalises on stop. Status is reported on `/rosbag_recorder/status`.

---

## Build & Run

### Requirements

| Tool | Version |
|------|---------|
| Android Studio | Ladybug (2024.2)+ |
| Minimum SDK | 24 (Android 7.0) |
| Compile / Target SDK | 35 (Android 15) |
| Kotlin | 2.1.0 |
| Android Gradle Plugin | 8.13.2 |
| JDK | 17 |

### Steps

```bash
# Open android_app/ in Android Studio, let Gradle sync, then:
./gradlew build

# Run on device
./gradlew installDebug
```

### Robot-side prerequisites

```bash
# ROSBridge WebSocket (port 9090)
./launch_rosbridge.sh
# or:
ros2 launch rosbridge_server rosbridge_websocket_launch.xml port:=9090

# web_video_server for MJPEG (optional but needed for camera feed)
ros2 run web_video_server web_video_server
```

---

## Key Dependencies

| Library | Purpose |
|---------|---------|
| `io.github.sceneview:sceneview:4.8.0` | Filament 3D rendering for robot viewer |
| `com.squareup.okhttp3:okhttp:4.12.0` | WebSocket (ROSBridge) |
| `com.github.PhilJay:MPAndroidChart:v3.1.0` | Velocity charts |
| `com.google.code.gson:gson:2.11.0` | JSON parsing (ROSBridge + OTA payloads) |
| `androidx.viewpager2:viewpager2:1.1.0` | Map + Observability tab pagers |
| `androidx.recyclerview:recyclerview:1.4.0` | Chat / logs / runs lists |
| `androidx.datastore:datastore-preferences:1.1.1` | Persisted settings |
| `com.google.dagger:hilt-android:2.57.2` | Dependency injection |
| `com.jakewharton.timber:timber:5.0.1` | Logging |

---

## ROS Topics

All topic and service names live in `utils/Constants.kt`.

### Published by the app (Android → robot)

| Topic | Type | Notes |
|-------|------|-------|
| `/cmd_vel/teleop` | Twist | 20 Hz, clamped ±1.5 m/s / ±2.0 rad/s. Routed by `cmd_vel_mux`; set `/control_mode` to `"teleop"` first |
| `/control_mode` | String | `"nav2"` / `"vla"` / `"teleop"` / `"rl_nav"` — drives `cmd_vel_mux` |
| `/robot_mode` | String | monitoring only; does not control the mux |
| `/arm/joint_commands` | JointState | joint names must be `arm_*` prefixed |
| `/arm/enable` | Bool | |
| `/mission/command` | String | structured (`navigate:X,vla:Y`) or natural language |
| `/ai/command` | String | natural language → LangGraph agent (requires `omnibot_orchestration`) |
| `/rosbag_recorder/start` | String | dataset recording start (bag name) |
| `/rosbag_recorder/stop` | Empty | dataset recording stop |
| `/emergency_stop` | Bool | |

### Subscribed by the app (robot → Android)

| Topic | Type | Notes |
|-------|------|-------|
| `/odom` | Odometry | |
| `/map` | OccupancyGrid | |
| `/imu/data` | Imu | |
| `/arm/joint_states` | JointState | |
| `/camera/depth/points` | PointCloud2 | base64-decoded for the 3D cloud view |
| `/mission/status` | String | |
| `/ai/status` | String | from `langchain_agent_node` when running |
| `/ai/response_needed` | String | clarification requests from the AI agent |
| `/diagnostics` | DiagnosticArray | |
| `/ota/status` | String (JSON) | from `omnibot_ota` node when running |
| `/ota/progress` | String (JSON) | OTA download/apply progress |

### ROS services called by the app (OTA)

| Service | Purpose |
|---------|---------|
| `/ota/check` | Query for an available update |
| `/ota/apply_workspace` | Apply a new ROS workspace bundle |
| `/ota/apply_models` | Apply new model artifacts |
| `/ota/rollback_workspace` | Roll back to the previous workspace |

> The app also declares legacy custom topics in `Constants.kt`
> (`/robot_status`, `/wheel_speeds`, `/motor_pwm`) that have no publisher in the
> current robot stack — they are subscribed but never receive data.

---

## Future Work

- [ ] **ARCore mode** — AR overlay of robot model on live camera using ARCore + SceneView AR session.
- [ ] **Voice input** — Android SpeechRecognizer feeding the AI chat interface.
- [ ] **Multi-camera switcher** — Swipe between wrist / front / BEV camera on Dashboard.
- [ ] **Foxglove debug panel** — Embedded Foxglove WebView for raw topic inspection.
- [ ] **Unit tests for RobotRepository** — Refactor singleton to constructor DI first.
- [ ] **CI GLB generation** — GitHub Actions step to run `tools/urdf_to_glb.py` and upload `robot.glb` as a release asset.

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Gradle sync failed | Check internet; **File > Invalidate Caches > Restart** |
| Build errors | Ensure JDK 17; **Build > Clean Project** |
| WebSocket not connecting | Verify same network; check `./launch_rosbridge.sh` is running |
| No camera image | Confirm `web_video_server` is running; check camera URL in Settings |
| 3D viewer shows banner | Run `python tools/urdf_to_glb.py` then rebuild app |
| Robot model doesn't move | Verify `/odom` and `/arm/joint_states` are publishing |

---

## License

MIT

## Contact

Varun Vaidhiya — varun.vaidhiya@gmail.com
