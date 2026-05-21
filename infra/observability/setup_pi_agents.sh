#!/usr/bin/env bash
# Install and configure the Pi-side observability agents.
# Run this script ONCE on the Raspberry Pi 5.
#
# Usage:
#   GPU_DESKTOP_IP=192.168.1.100 bash setup_pi_agents.sh

set -euo pipefail

GPU_DESKTOP_IP="${GPU_DESKTOP_IP:-192.168.1.100}"
ARCH="$(uname -m)"   # aarch64 on Pi 5

echo "=== OmniBot Pi Observability Setup ==="
echo "  GPU Desktop IP:  ${GPU_DESKTOP_IP}"
echo "  Architecture:    ${ARCH}"
echo ""

# ── 1. Install prometheus_client for the ROS 2 metrics bridge ────────────
echo "[1/4] Installing prometheus_client..."
pip3 install --break-system-packages "prometheus_client>=0.20"

# ── 2. Download node_exporter (system CPU / RAM / disk metrics) ──────────
echo "[2/4] Downloading node_exporter..."
NODE_EXPORTER_VERSION="1.8.1"
NODE_EXPORTER_URL="https://github.com/prometheus/node_exporter/releases/download/v${NODE_EXPORTER_VERSION}/node_exporter-${NODE_EXPORTER_VERSION}.linux-arm64.tar.gz"

cd /tmp
curl -fsSL "${NODE_EXPORTER_URL}" -o node_exporter.tar.gz
tar xzf node_exporter.tar.gz
sudo mv "node_exporter-${NODE_EXPORTER_VERSION}.linux-arm64/node_exporter" /usr/local/bin/
rm -rf node_exporter.tar.gz "node_exporter-${NODE_EXPORTER_VERSION}.linux-arm64"

# Create systemd service for node_exporter
sudo tee /etc/systemd/system/node_exporter.service > /dev/null <<EOF
[Unit]
Description=Prometheus Node Exporter (OmniBot Pi)
After=network.target

[Service]
User=nobody
ExecStart=/usr/local/bin/node_exporter \
  --collector.filesystem.mount-points-exclude='^/(dev|proc|sys|var/lib/docker/.+)($|/)'
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable --now node_exporter
echo "  node_exporter running on :9100"

# ── 3. Download and configure promtail ───────────────────────────────────
echo "[3/4] Downloading promtail..."
PROMTAIL_VERSION="3.1.0"
PROMTAIL_URL="https://github.com/grafana/loki/releases/download/v${PROMTAIL_VERSION}/promtail-linux-arm64.zip"

cd /tmp
curl -fsSL "${PROMTAIL_URL}" -o promtail.zip
unzip -o promtail.zip
sudo mv promtail-linux-arm64 /usr/local/bin/promtail
rm promtail.zip

# Write promtail config pointing to GPU desktop Loki
OMNIBOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
sudo tee /etc/promtail-omnibot.yml > /dev/null <<EOF
server:
  http_listen_port: 9081
  grpc_listen_port: 0

positions:
  filename: /tmp/promtail_pi_positions.yaml

clients:
  - url: http://${GPU_DESKTOP_IP}:3100/loki/api/v1/push

scrape_configs:
  - job_name: ros2_logs_pi
    static_configs:
      - targets: [localhost]
        labels:
          job: ros2
          machine: raspberry_pi
          __path__: /home/*/.ros/log/**/*.log
    pipeline_stages:
      - regex:
          expression: '^\[(?P<ros_time>[0-9.]+)\] \[(?P<node>[^\]]+)\]: \[(?P<severity>[A-Z]+)\] (?P<message>.+)$'
      - labels:
          node:
          severity:
      - drop:
          expression: '.*\[DEBUG\].*'
          drop_counter_reason: debug_filtered
EOF

sudo tee /etc/systemd/system/promtail-omnibot.service > /dev/null <<EOF
[Unit]
Description=Promtail log shipper (OmniBot Pi → Loki)
After=network.target

[Service]
ExecStart=/usr/local/bin/promtail -config.file=/etc/promtail-omnibot.yml
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable --now promtail-omnibot
echo "  promtail running — shipping logs to ${GPU_DESKTOP_IP}:3100"

# ── 4. Create a helper to start the ROS 2 metrics bridge on boot ─────────
echo "[4/4] Creating metrics bridge launch helper..."
sudo tee /etc/systemd/system/omnibot-metrics-bridge.service > /dev/null <<EOF
[Unit]
Description=OmniBot Prometheus Metrics Bridge (ROS 2)
After=network.target

[Service]
User=${USER:-ubuntu}
Environment="ROS_DOMAIN_ID=30"
ExecStartPre=/bin/bash -c "source /opt/ros/jazzy/setup.bash"
ExecStart=/bin/bash -c "source /opt/ros/jazzy/setup.bash && \
  source ${OMNIBOT_DIR}/robot_ws/install/setup.bash && \
  ros2 launch omnibot_metrics metrics.launch.py machine:=raspberry_pi port:=8888"
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

# Note: don't auto-enable — let the user start it manually after building the workspace
echo ""
echo "=== Setup complete ==="
echo ""
echo "Next steps on the Pi:"
echo "  1. Build the workspace:  cd ${OMNIBOT_DIR}/robot_ws && colcon build --symlink-install"
echo "  2. Start metrics bridge: sudo systemctl enable --now omnibot-metrics-bridge"
echo "  3. Verify metrics:       curl http://localhost:8888/metrics | head -20"
echo ""
echo "On the GPU Desktop:"
echo "  docker compose --profile observability up -d"
echo "  open http://localhost:3000   (admin / omnibot)"
