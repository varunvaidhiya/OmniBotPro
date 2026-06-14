# Changelog

All notable changes to OmniBot are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [Unreleased]

### Added
- API key authentication and token-bucket rate limiting on `/predict` and
  `/load_model` endpoints in `vla_serve` (`VLA_API_KEY`, `VLA_RATE_LIMIT`)
- Unit tests for `CmdVelMux` (mode switching, message forwarding)
- Unit tests for `MissionPlanner._parse_command` and `_resolve_location`
- Unit tests for `YahboomControllerNode` (checksum, TX packets, RX odometry,
  IMU parsing, ramp limiting, emergency stop)
- Shared `conftest.py` and RX packet builder helpers for driver tests
- `docker-compose.yml` with `robot`, `vla`, and `rosbridge` services
- `infra/docker/Dockerfile.vla` for standalone VLA inference image
- `.env.example` documenting all environment variables
- `.pre-commit-config.yaml` with ruff (lint + format) and mypy hooks
- ROSBridge WebSocket node (`use_rosbridge` arg, default `true`) to
  `robot.launch.py` and `hybrid_robot.launch.py`
- BEV stitcher node (`use_bev` arg, default `true`) to
  `hybrid_robot.launch.py` — required by `policy_node`
- Lint and coverage CI jobs to `.github/workflows/ros2_ci.yml`
- Separate `vla_serve_test` CI job with 60% coverage gate
- Structured logging (`logging` module, `LOG_LEVEL` env var) in `vla_serve`

### Fixed
- `wheel_separation_x`/`y` → `wheel_separation_length`/`width` in
  `robot.launch.py` and `hybrid_robot.launch.py` (param name mismatch)
- Emergency stop subscriber wired in `yahboom_controller_node.py` — Android
  `/emergency_stop` Bool topic now halts motors immediately
- Hardcoded debug log path `/home/varunvaidhiya/yahboom_debug.log` replaced
  with `~/.ros/omnibot_debug.log`

---

## [0.1.0] — 2026-01-15

### Added — Core robot stack
- Yahboom ROS Robot Expansion Board driver (`yahboom_controller_node.py`)
  — serial protocol, mecanum kinematics, odometry, IMU
- SO-101 6-DOF arm driver via LeRobot `FeetechMotorsBus`
- OpenVLA 7B inference node (GPU desktop, 1 Hz)
- SmolVLA unified 9-DOF mobile-manipulation policy node (10 Hz)
- `cmd_vel_mux` — mode-based velocity multiplexer (nav2 / vla / teleop)
- `mission_planner` — high-level Nav2 + VLA orchestrator with state machine
- SLAM Toolbox + Nav2 + EKF localization stack
- BEV stitcher (`bev_stitcher_node`) for 4-camera bird's-eye-view image
- Teleop recorder for imitation learning dataset collection
- Android MVVM app (ROSBridge WebSocket, Kotlin, Hilt DI)
- `vla_serve` FastAPI inference server (model-agnostic, env-var configured)
- `yahboom_ros2` pure-Python protocol encoder/decoder package
- `robot_episode_dataset` LeRobot-format dataset helpers
- Gazebo Harmonic simulation (`ros_gz_sim` + `ros_gz_bridge`)
- DevContainer + Dockerfile for ROS 2 Jazzy development
- GitHub Actions CI (build, test, bloom-release, PyPI publish)

[Unreleased]: https://github.com/varunvaidhiya/OmniBot/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/varunvaidhiya/OmniBot/releases/tag/v0.1.0
