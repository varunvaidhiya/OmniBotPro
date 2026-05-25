#!/usr/bin/env bash
# build_release_bundle.sh — build a local OTA release bundle for testing.
#
# Usage:
#   VERSION=v0.1.0-test bash infra/ota/build_release_bundle.sh
#
# Output:
#   dist/ros-workspace-<VERSION>.tar.zst
#   dist/manifest.json
#
# To serve locally and test the OTA agent:
#   python3 -m http.server -d dist/ 8765 &
#   ros2 launch omnibot_ota ota.launch.py manifest_url:=http://localhost:8765/manifest.json

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
VERSION="${VERSION:-v0.0.0-dev}"
ARTIFACT="ros-workspace-${VERSION}.tar.zst"
OUTPUT_DIR="${REPO_ROOT}/dist"

echo "=== OmniBot OTA Release Builder ==="
echo "  Version:   ${VERSION}"
echo "  Repo root: ${REPO_ROOT}"
echo "  Output:    ${OUTPUT_DIR}/${ARTIFACT}"
echo ""

# ── 1. Build workspace ────────────────────────────────────────────────────────
echo "[1/4] Building ROS workspace..."
cd "${REPO_ROOT}/robot_ws"
# shellcheck source=/dev/null
source /opt/ros/jazzy/setup.bash
colcon build --symlink-install 2>&1

# Write version marker into install/
echo "${VERSION}" > "${REPO_ROOT}/robot_ws/install/.ota_version"

# ── 2. Package install/ as .tar.zst ──────────────────────────────────────────
echo "[2/4] Packaging install/..."
mkdir -p "${OUTPUT_DIR}"
tar \
  --exclude=./build \
  --exclude=./log \
  -I 'zstd -T0 -15' \
  -cf "${OUTPUT_DIR}/${ARTIFACT}" \
  -C "${REPO_ROOT}/robot_ws" \
  install/

# ── 3. SHA-256 ────────────────────────────────────────────────────────────────
echo "[3/4] Computing SHA-256..."
SHA=$(sha256sum "${OUTPUT_DIR}/${ARTIFACT}" | awk '{print $1}')
echo "  SHA-256: ${SHA}"

# ── 4. manifest.json ─────────────────────────────────────────────────────────
echo "[4/4] Generating manifest.json..."
MANIFEST="${OUTPUT_DIR}/manifest.json"
REPO="${GITHUB_REPOSITORY:-local/omnibot}"

python3 - <<EOF
import json
manifest = {
    "schema_version": 1,
    "version": "${VERSION}",
    "published_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "components": {
        "ros_workspace": {
            "filename": "${ARTIFACT}",
            "sha256": "${SHA}",
            "download_url": "https://github.com/${REPO}/releases/download/${VERSION}/${ARTIFACT}",
            "restart_required": True,
            "restart_service": "omnibot-robot.service"
        },
        "nav_policy_onnx": {
            "filename": "",
            "sha256": "",
            "download_url": "",
            "target_path": "~/models/omnibot_nav_policy.onnx"
        },
        "arm_policy_onnx": {
            "filename": "",
            "sha256": "",
            "download_url": "",
            "target_path": "~/models/omnibot_arm_policy.onnx"
        }
    },
    "min_compatible_version": "v1.0.0",
    "release_notes_url": "https://github.com/${REPO}/releases/tag/${VERSION}"
}
with open("${MANIFEST}", "w") as f:
    json.dump(manifest, f, indent=2)
print("  Manifest written")
EOF

echo ""
echo "=== Done ==="
echo "  Artifact: ${OUTPUT_DIR}/${ARTIFACT}"
echo "  Manifest: ${MANIFEST}"
echo ""
echo "To test the OTA agent locally:"
echo "  python3 -m http.server -d ${OUTPUT_DIR} 8765 &"
echo "  ros2 launch omnibot_ota ota.launch.py manifest_url:=http://localhost:8765/manifest.json check_interval_s:=10"
