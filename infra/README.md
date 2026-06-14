# Infrastructure & Tooling

Configuration for development environments, containerization, CI, observability,
over-the-air (OTA) updates, and device udev rules.

## Directory Structure

| Path | Purpose |
|------|---------|
| `docker/Dockerfile` | Multi-stage build (base ROS 2 Jazzy → `dev` target with tools). Used by the dev container. |
| `docker/Dockerfile.vla` | Slim `python:3.11` image for the FastAPI VLA inference server (`packages/vla_serve`). |
| `devcontainers/devcontainer.json` | VS Code Dev Container that builds `docker/Dockerfile` (`dev` target). |
| `ci/test_all.sh` | Runs `colcon build` + `colcon test` + `colcon test-result` in `robot_ws`. |
| `observability/` | Prometheus + Loki + Tempo + Grafana + Alertmanager stack (see below). |
| `ota/` | OTA release bundler + systemd unit for the robot stack. |
| `udev/99-obsensor-libusb.rules` | udev rules for the Orbbec / Astra (ObSensor) depth camera. |

> Note: the repo's primary Dev Container is the root `.devcontainer/devcontainer.json`
> (ROS 2 Jazzy + Gazebo Harmonic, builds `digital_twin/docker/Dockerfile.sim` and
> runs `colcon build` via `post_create.sh`). The `infra/devcontainers/devcontainer.json`
> here is a lighter ROS-only alternative without the simulation toolchain.

## Getting Started (DevContainer)

1. Install Docker and VS Code with the "Dev Containers" extension.
2. Open this project in VS Code.
3. Run "Dev Containers: Reopen in Container" from the command palette.

## Running Tests Locally

```bash
bash infra/ci/test_all.sh
```

## Camera udev Rules

Install the ObSensor depth-camera rules on the Pi so the camera enumerates without
root and gets stable `/dev` symlinks:

```bash
sudo cp infra/udev/99-obsensor-libusb.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules && sudo udevadm trigger
```

## Observability Stack

Prometheus + Loki + Tempo + Grafana + Alertmanager, deployed on the GPU desktop.

```bash
# Run on the GPU desktop (it has the most resources), from the repo root:
PI_IP=192.168.1.101 docker compose \
  -f docker-compose.yml \
  -f infra/observability/docker-compose.observability.yml \
  up -d
```

| Service (container) | Port | URL / Notes |
|---------------------|------|-------------|
| Grafana | 3000 | http://localhost:3000 (admin / omnibot — override via `GRAFANA_ADMIN_*`) |
| Prometheus | 9090 | http://localhost:9090 |
| Alertmanager | 9093 | http://localhost:9093 |
| Loki | 3100 | log aggregation |
| Tempo | 4317 / 4318 / 3200 | OTLP gRPC / OTLP HTTP / query |
| Node Exporter (GPU) | 9100 | host system metrics |
| DCGM Exporter | 9400 | NVIDIA GPU metrics |
| Promtail (GPU) | — | ships `~/.ros/log` to Loki |

### Prometheus scrape targets (`prometheus/prometheus.yml`)

| Job | Target | Layer |
|-----|--------|-------|
| `omnibot_ros2_pi` | `${PI_IP}:8888` | ROS metrics bridge on the Pi |
| `omnibot_ros2_gpu` | `localhost:8889` | ROS metrics bridge on the GPU desktop |
| `vla_serve` | `localhost:8000` | VLA FastAPI `/metrics` |
| `node_exporter_pi` | `${PI_IP}:9100` | Pi system metrics |
| `node_exporter_gpu` | `localhost:9100` | GPU desktop system metrics |
| `dcgm_gpu` | `localhost:9400` | NVIDIA GPU metrics |
| `omnibot_benchmarks` | `localhost:8890` | `learning_engine` benchmark reporter |

### Pi-side agents

The Pi runs its agents natively (no Docker). Configure them once:

```bash
GPU_DESKTOP_IP=192.168.1.100 bash infra/observability/setup_pi_agents.sh
```

This installs node_exporter (port 9100) + promtail, and points the ROS metrics
bridge / promtail at the Grafana/Loki host. See `setup_pi_agents.sh` for details.

Dashboards under `observability/grafana/dashboards/` (robot health, AI
performance, system resources) and datasources/dashboard providers under
`observability/grafana/provisioning/` are auto-provisioned on Grafana startup.
Alert rules live in `observability/prometheus/alerts/omnibot_alerts.yml`;
routing in `observability/alertmanager/alertmanager.yml`.

## OTA Updates

`ota/build_release_bundle.sh` builds a versioned workspace bundle for the
`omnibot_ota` ROS node to fetch and apply (Android app drives this via the
`/ota/*` services).

```bash
# Build a local test bundle
VERSION=v0.1.0-test bash infra/ota/build_release_bundle.sh
# Output: dist/ros-workspace-<VERSION>.tar.zst + dist/manifest.json

# Serve it locally and point the OTA agent at it
python3 -m http.server -d dist/ 8765 &
ros2 launch omnibot_ota ota.launch.py manifest_url:=http://localhost:8765/manifest.json
```

`ota/omnibot-robot@.service` is a systemd template unit that launches the robot
stack with `use_ota:=true` and sources `~/OmniBot/deployment.env`:

```bash
sudo cp infra/ota/omnibot-robot@.service /etc/systemd/system/
sudo systemctl enable --now omnibot-robot@$USER
```
