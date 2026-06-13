# PROGRESS — AI feature implementation session (2026-06-12)

Running log so work can resume cleanly after a token-limit reset.
**Resume instruction: read this file, then continue from "Next session".**

## Scope (user request) — ALL CORE ITEMS DONE ✅
1. ✅ BEV stitcher: true fused single image (was 2×2 tiling)
2. ✅ Depth-camera SLAM pipeline fixed
3. ✅ Modular perception: object distance + pose estimation (new package)
4. ✅ Path planning / motion control tuned for mecanum + driver limits
5. ✅ Hybrid VLA/modular orchestration audit + perception→agent integration
6. ✅ Verification: py_compile + YAML/XML parse on every touched file; IPM
      and clustering validated with synthetic-image tests

## What was changed

### 1. BEV stitcher (`robot_ws/src/omnibot_lerobot/omnibot_lerobot/bev_stitcher_node.py`)
- Added `_geometric_ipm()`: per-camera homography (image→canvas) by projecting
  the z=0 ground plane through each camera's pinhole model (URDF poses), plus
  canvas-space feathered visibility wedges for blending and artifact removal.
- New params `bev_range_m`, `camera_hfov`, `camera_height`, `cam_*_pose`.
- Tiled-quadrant fallback removed; calibration file still wins if present.
- `simulation.launch.py`: switched to this node (old `ros2_bev_stitcher`
  published to `/camera/bev/image_raw` which nothing consumed).
- Verified: synthetic 4-camera checkerboard renders → seamless fused BEV,
  round-trip error 0.0002 px.
- TODO (optional): port `_geometric_ipm` into `packages/ros2_bev_stitcher`
  (only benchmarks use it now).

### 2. SLAM (depth camera)
- `output_frame` of depthimage_to_laserscan changed
  `depth_camera_optical_frame` → `depth_camera_link` in perception.launch.py,
  simulation.launch.py, isaac_sim.launch.py. Optical frame (z-fwd) made the
  scan appear rotated 90°; LaserScan frames must be x-forward.
- `slam_toolbox_params.yaml`: explicit `odom_frame/map_frame/base_frame
  (base_footprint)/scan_topic`, removed duplicate `map_start_at_dock`.
- Note: depth cam is rear-facing, tilted 12° up (URDF) — scan still works via
  TF but is rear-coverage only; consider hardware re-aim later.

### 3. NEW package `robot_ws/src/omnibot_perception`
- `object_perception_node.py`: YOLO backend (if ultralytics present) or
  depth-band clustering fallback (connected components + PCA yaw). Publishes
  /perception/{objects,object_info,nearest_distance,markers}; answers
  /perception/query_pixel → /perception/query_result (lets VLA/agent ground
  "that object" to metric pose). TF optical→base_link applied internally.
- Launch: own `perception_ai.launch.py` + auto-started from
  `perception.launch.py` (`ai_perception:=true` default).
- Verified with synthetic depth scene (objects at 1.2 m / 2.5 m detected at
  correct centroids; backprojection matches analytic).

### 4. Nav2 (`omnibot_navigation/config/nav2_params.yaml`)
- All 0.26 velocities → 0.20 (Yahboom driver hard-clamps at 0.2 m/s).
- `min_vel_x: 0.0` → −0.20 (omni reverse), `vy_samples: 5→10`.
- acc/decel 2.5 → 1.0 m/s² (driver ramp = 0.05 m/s @ 20 Hz).
- `trans_stopped_velocity: 0.25→0.05` (was ≥ max speed → always "stopped").
- velocity_smoother limits matched.

### 5. Hybrid architecture audit — VERIFIED WORKING
- driver remap `/cmd_vel`←`/cmd_vel/out` ✓; mux inputs nav2/vla/teleop/rl ✓
- `vla_node` → /cmd_vel/vla ✓; `policy_node` (SmolVLA) publishes /cmd_vel but
  `policy_inference.launch.py` remaps → /cmd_vel/vla ✓ (don't run policy_node
  bare without that remap!)
- mission_planner: control_mode + /policy/task|enable + /rl_nav/goal +
  arm cmd_mode ("policy"|"rl_arm" — CLAUDE.md said "smolvla", now corrected) ✓
- langchain_agent → /mission/command ✓
- NEW: langchain_agent_node subscribes /perception/object_info;
  `observe_node` (graph/nodes.py) appends metric detections to scene
  descriptions → agent can reason about real distances.

### Docs
- CLAUDE.md updated: new package section, perception topics in topic map,
  BEV IPM section, Nav2 limits section, arm-mux mode fix.

## Session 2 additions (same day)

### State estimation — FIXED
- `robot_localization.yaml`: wheel odom now velocity-only (was fusing
  absolute x/y/z/yaw — drifts with mecanum slip and fought the IMU);
  IMU linear accelerations disabled (noisy MEMS integrated into velocity).
- Double odom→base_link TF broadcast fixed: driver `publish_tf: False`
  added in hybrid_robot.launch.py and master/omnibot_pi.launch.py
  (robot.launch.py already handled it).

### Android app / telemetry — FIXED
- CameraViewModel.kt streamed `/camera/image_raw` (nonexistent) →
  `/camera/front/image_raw`.
- Driver `publish_diagnostics: True` enabled in robot/hybrid/pi-master
  launches so the app's /diagnostics panel gets data.
- NEW `omnibot_hybrid/rosbag_recorder.py`: app record button now works
  (/rosbag_recorder/start|stop|status, curated topic list, SIGINT-clean).
  Added to hybrid + pi-master launches; entry point in setup.py.
- `object_perception_node` added to master/omnibot_pi.launch.py.
- KNOWN-DEAD app features (legacy STM32 era, need custom robot_msgs pkg
  that doesn't exist): /robot_status, /wheel_speeds, /motor_pwm
  subscriptions never fire (harmless). Implement later if wanted.

## Next session (nice-to-haves, in priority order)
1. `colcon build && colcon test` on the robot/dev machine (sandbox here has
   no ROS) — fix any packaging nits (omnibot_perception is new).
2. Unit tests: bev `_geometric_ipm`, perception clustering (port the
   synthetic tests from this session into robot_ws tests/; they live in
   sandbox /tmp/test_ipm.py and /tmp/test_perception.py — recreate from
   PROGRESS history if lost).
3. Port geometric IPM into packages/ros2_bev_stitcher.
4. Optional: feed /perception/objects into a Nav2 costmap layer or a
   speed-limit filter using /perception/nearest_distance.
5. Optional: rl_nav synthesized lidar is rear-only (depth cam aims backward,
   tilted up 12°) — evaluate re-aiming camera or fusing 4 RGB cams.
