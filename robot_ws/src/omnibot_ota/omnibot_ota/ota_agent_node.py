#!/usr/bin/env python3
"""
OmniBot OTA Update Agent.

Polls a remote manifest.json for new versions, downloads and verifies
the ROS workspace tarball and ONNX model weights, and installs them
atomically with rollback support.

Topics published:
  /ota/status   (std_msgs/String) — JSON: state, workspace, models info
  /ota/progress (std_msgs/String) — JSON: {"percent": 0-100, "component": "..."}

Services:
  /ota/check             (std_srvs/Trigger) — force manifest fetch
  /ota/apply_workspace   (std_srvs/Trigger) — apply pending ROS workspace update
  /ota/apply_models      (std_srvs/Trigger) — apply pending ONNX model update
  /ota/rollback_workspace (std_srvs/Trigger) — restore install.backup/ → install/

Parameters:
  manifest_url     (string) — URL to manifest.json; set via deployment.env
  check_interval_s (float)  — polling interval in seconds; default 3600
  workspace_root   (string) — path to robot_ws; default ~/OmniBot/robot_ws
  models_dir       (string) — path to models directory; default ~/models
  service_name     (string) — systemd service restarted after workspace update
"""

import hashlib
import json
import os
import shutil
import subprocess
import tarfile
import tempfile
import threading
import urllib.request
from pathlib import Path
from typing import Optional

import rclpy
from rclpy.node import Node
from std_msgs.msg import String
from std_srvs.srv import Trigger


class OtaAgentNode(Node):
    """Over-the-Air update agent for OmniBot."""

    def __init__(self):
        super().__init__("ota_agent_node")

        # ── Parameters ────────────────────────────────────────────────────────
        self.declare_parameter("manifest_url", "")
        self.declare_parameter("check_interval_s", 3600.0)
        self.declare_parameter(
            "workspace_root",
            str(Path.home() / "OmniBot" / "robot_ws"),
        )
        self.declare_parameter("models_dir", str(Path.home() / "models"))
        self.declare_parameter("service_name", "omnibot-robot.service")

        self._manifest_url: str = self.get_parameter("manifest_url").value
        self._check_interval: float = self.get_parameter("check_interval_s").value
        ws_root = self.get_parameter("workspace_root").value
        self._workspace_root = Path(ws_root) if ws_root else Path.home() / "OmniBot" / "robot_ws"
        models_dir = self.get_parameter("models_dir").value
        self._models_dir = Path(models_dir) if models_dir else Path.home() / "models"
        self._service_name: str = self.get_parameter("service_name").value

        # Derived paths
        self._install_dir = self._workspace_root / "install"
        self._backup_dir = self._workspace_root / "install.backup"
        self._cache_dir = Path(tempfile.gettempdir()) / "omnibot_ota_cache"
        self._cache_dir.mkdir(parents=True, exist_ok=True)

        # ── State ──────────────────────────────────────────────────────────────
        self._ws_state = "IDLE"
        self._model_state = "IDLE"
        self._pending_workspace: Optional[dict] = None
        self._pending_models: Optional[dict] = None
        self._ws_version: Optional[str] = self._read_ws_version()
        self._model_version: Optional[str] = self._read_model_version()
        self._lock = threading.Lock()

        # ── Publishers ─────────────────────────────────────────────────────────
        self._status_pub = self.create_publisher(String, "/ota/status", 10)
        self._progress_pub = self.create_publisher(String, "/ota/progress", 10)

        # ── Services ───────────────────────────────────────────────────────────
        self.create_service(Trigger, "/ota/check", self._check_cb)
        self.create_service(Trigger, "/ota/apply_workspace", self._apply_workspace_cb)
        self.create_service(Trigger, "/ota/apply_models", self._apply_models_cb)
        self.create_service(Trigger, "/ota/rollback_workspace", self._rollback_workspace_cb)

        # ── Timer ──────────────────────────────────────────────────────────────
        self.create_timer(self._check_interval, self._check_updates)

        self._publish_status()
        self.get_logger().info(
            f"OTA agent ready — manifest_url={self._manifest_url!r} "
            f"interval={self._check_interval}s "
            f"ws_version={self._ws_version!r}"
        )

    # ── Service callbacks ─────────────────────────────────────────────────────

    def _check_cb(
        self, req: Trigger.Request, resp: Trigger.Response
    ) -> Trigger.Response:
        manifest = self._fetch_manifest()
        if manifest is None:
            resp.success = False
            resp.message = "Failed to fetch manifest — check manifest_url parameter"
            return resp

        components = manifest.get("components", {})
        remote_ws = manifest.get("version", "")
        remote_nav = components.get("nav_policy_onnx", {}).get("sha256", "")
        remote_arm = components.get("arm_policy_onnx", {}).get("sha256", "")

        with self._lock:
            if remote_ws and remote_ws != self._ws_version:
                self._pending_workspace = manifest
                self._ws_state = "UPDATE_AVAILABLE"
            if (remote_nav or remote_arm) and self._models_changed(components):
                self._pending_models = manifest
                self._model_state = "UPDATE_AVAILABLE"

        self._publish_status()
        ws_msg = (
            f"workspace {self._ws_version} → {remote_ws}"
            if remote_ws != self._ws_version
            else f"workspace up to date ({self._ws_version})"
        )
        model_msg = (
            "model update available"
            if self._model_state == "UPDATE_AVAILABLE"
            else "models up to date"
        )
        resp.success = True
        resp.message = f"{ws_msg}; {model_msg}"
        return resp

    def _apply_workspace_cb(
        self, req: Trigger.Request, resp: Trigger.Response
    ) -> Trigger.Response:
        with self._lock:
            manifest = self._pending_workspace
        if manifest is None:
            manifest = self._fetch_manifest()
        if manifest is None:
            resp.success = False
            resp.message = "No pending workspace update and manifest fetch failed"
            return resp
        if manifest.get("version") == self._ws_version:
            resp.success = True
            resp.message = f"Workspace already up to date ({self._ws_version})"
            return resp

        threading.Thread(
            target=self._apply_workspace, args=(manifest,), daemon=True
        ).start()
        resp.success = True
        resp.message = f"Workspace update to {manifest.get('version')} started"
        return resp

    def _apply_models_cb(
        self, req: Trigger.Request, resp: Trigger.Response
    ) -> Trigger.Response:
        with self._lock:
            manifest = self._pending_models
        if manifest is None:
            manifest = self._fetch_manifest()
        if manifest is None:
            resp.success = False
            resp.message = "No pending model update and manifest fetch failed"
            return resp

        threading.Thread(
            target=self._apply_models, args=(manifest,), daemon=True
        ).start()
        resp.success = True
        resp.message = "Model update started"
        return resp

    def _rollback_workspace_cb(
        self, req: Trigger.Request, resp: Trigger.Response
    ) -> Trigger.Response:
        if not self._backup_dir.exists():
            resp.success = False
            resp.message = "No backup snapshot found — cannot rollback"
            return resp

        threading.Thread(target=self._perform_rollback, daemon=True).start()
        resp.success = True
        resp.message = "Workspace rollback started"
        return resp

    # ── Timer callback ────────────────────────────────────────────────────────

    def _check_updates(self) -> None:
        if not self._manifest_url:
            return
        manifest = self._fetch_manifest()
        if manifest is None:
            return
        components = manifest.get("components", {})
        remote_ws = manifest.get("version", "")
        with self._lock:
            if remote_ws and remote_ws != self._ws_version:
                self._pending_workspace = manifest
                self._ws_state = "UPDATE_AVAILABLE"
                self.get_logger().info(
                    f"[OTA] Workspace update: {self._ws_version} → {remote_ws}"
                )
            if self._models_changed(components):
                self._pending_models = manifest
                self._model_state = "UPDATE_AVAILABLE"
                self.get_logger().info("[OTA] Model update available")
        self._publish_status()

    # ── Core workspace pipeline ───────────────────────────────────────────────

    def _apply_workspace(self, manifest: dict) -> None:
        try:
            comp = manifest.get("components", {}).get("ros_workspace", {})
            url = comp.get("download_url", "")
            expected_sha = comp.get("sha256", "")
            service = comp.get("restart_service", self._service_name)

            if not url or not expected_sha:
                raise ValueError("manifest missing ros_workspace download_url or sha256")

            self._set_ws_state("DOWNLOADING")
            artifact = self._cache_dir / Path(url).name
            self._download(url, artifact, component="ros_workspace")

            self._set_ws_state("VERIFYING")
            actual = self._sha256(artifact)
            if actual != expected_sha:
                raise ValueError(f"SHA-256 mismatch: expected {expected_sha} got {actual}")
            self.get_logger().info("[OTA] Workspace SHA-256 OK")

            self._set_ws_state("INSTALLING")
            self._snapshot_install()
            shutil.rmtree(self._install_dir, ignore_errors=True)
            self._install_dir.mkdir(parents=True, exist_ok=True)
            with tarfile.open(artifact, "r:*") as tf:
                tf.extractall(path=self._workspace_root)

            version = manifest.get("version", "unknown")
            (self._workspace_root / "install" / ".ota_version").write_text(version)
            artifact.unlink(missing_ok=True)

            self._set_ws_state("RESTARTING")
            self._ws_version = version
            with self._lock:
                self._pending_workspace = None

            self.get_logger().info(f"[OTA] Restarting {service}...")
            subprocess.run(
                ["sudo", "systemctl", "restart", service],
                check=True,
                timeout=60,
            )
            self._set_ws_state("IDLE")

        except Exception as exc:
            self.get_logger().error(f"[OTA] Workspace update failed: {exc}")
            self._set_ws_state("ERROR")
            self._perform_rollback()

    # ── Core model pipeline ───────────────────────────────────────────────────

    def _apply_models(self, manifest: dict) -> None:
        try:
            components = manifest.get("components", {})
            self._models_dir.mkdir(parents=True, exist_ok=True)

            for key, target_name in [
                ("nav_policy_onnx", "omnibot_nav_policy.onnx"),
                ("arm_policy_onnx", "omnibot_arm_policy.onnx"),
            ]:
                comp = components.get(key, {})
                url = comp.get("download_url", "")
                expected_sha = comp.get("sha256", "")
                if not url or not expected_sha:
                    continue

                self._set_model_state("DOWNLOADING")
                artifact = self._cache_dir / Path(url).name
                self._download(url, artifact, component=key)

                self._set_model_state("VERIFYING")
                actual = self._sha256(artifact)
                if actual != expected_sha:
                    raise ValueError(
                        f"{key} SHA-256 mismatch: expected {expected_sha} got {actual}"
                    )
                self.get_logger().info(f"[OTA] {key} SHA-256 OK")

                target = self._models_dir / target_name
                shutil.move(str(artifact), str(target))
                self.get_logger().info(f"[OTA] {key} installed to {target}")

            version = manifest.get("version", "unknown")
            (self._models_dir / ".ota_model_version").write_text(version)
            self._model_version = version
            with self._lock:
                self._pending_models = None
            self._set_model_state("IDLE")

        except Exception as exc:
            self.get_logger().error(f"[OTA] Model update failed: {exc}")
            self._set_model_state("ERROR")

    # ── Rollback ──────────────────────────────────────────────────────────────

    def _perform_rollback(self) -> None:
        try:
            self._set_ws_state("ROLLING_BACK")
            if not self._backup_dir.exists():
                raise FileNotFoundError("No backup snapshot found")
            shutil.rmtree(self._install_dir, ignore_errors=True)
            shutil.move(str(self._backup_dir), str(self._install_dir))
            self._ws_version = self._read_ws_version()
            self.get_logger().info(
                f"[OTA] Rollback complete — restored version={self._ws_version}"
            )
            subprocess.run(
                ["sudo", "systemctl", "restart", self._service_name],
                check=True,
                timeout=60,
            )
            self._set_ws_state("IDLE")
        except Exception as exc:
            self.get_logger().error(f"[OTA] Rollback failed: {exc}")
            self._set_ws_state("ERROR")

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _fetch_manifest(self) -> Optional[dict]:
        if not self._manifest_url:
            return None
        try:
            self._set_ws_state("CHECKING")
            with urllib.request.urlopen(self._manifest_url, timeout=15) as resp:
                data = json.loads(resp.read().decode())
            self._set_ws_state("IDLE")
            return data
        except Exception as exc:
            self.get_logger().error(f"[OTA] Manifest fetch failed: {exc}")
            self._set_ws_state("IDLE")
            return None

    def _download(self, url: str, dest: Path, component: str = "") -> None:
        def reporthook(block: int, block_size: int, total: int) -> None:
            if total > 0:
                pct = min(100, block * block_size * 100 // total)
                self._publish_progress(pct, component)

        urllib.request.urlretrieve(url, str(dest), reporthook=reporthook)
        self._publish_progress(100, component)
        self.get_logger().info(f"[OTA] Downloaded: {dest.name}")

    @staticmethod
    def _sha256(path: Path) -> str:
        h = hashlib.sha256()
        with open(path, "rb") as f:
            for chunk in iter(lambda: f.read(65536), b""):
                h.update(chunk)
        return h.hexdigest()

    def _snapshot_install(self) -> None:
        if self._install_dir.exists():
            if self._backup_dir.exists():
                shutil.rmtree(self._backup_dir)
            shutil.copytree(str(self._install_dir), str(self._backup_dir))
            self.get_logger().info(f"[OTA] Snapshot saved to {self._backup_dir}")

    def _models_changed(self, components: dict) -> bool:
        nav = components.get("nav_policy_onnx", {})
        arm = components.get("arm_policy_onnx", {})
        return bool(nav.get("download_url") or arm.get("download_url"))

    def _read_ws_version(self) -> Optional[str]:
        marker = self._workspace_root / "install" / ".ota_version"
        return marker.read_text().strip() if marker.exists() else None

    def _read_model_version(self) -> Optional[str]:
        marker = self._models_dir / ".ota_model_version"
        return marker.read_text().strip() if marker.exists() else None

    def _set_ws_state(self, state: str) -> None:
        self._ws_state = state
        self._publish_status()

    def _set_model_state(self, state: str) -> None:
        self._model_state = state
        self._publish_status()

    def _publish_status(self) -> None:
        payload = json.dumps(
            {
                "workspace": {
                    "state": self._ws_state,
                    "current": self._ws_version,
                    "pending": (
                        self._pending_workspace.get("version")
                        if self._pending_workspace
                        else None
                    ),
                    "backup_available": self._backup_dir.exists(),
                },
                "models": {
                    "state": self._model_state,
                    "current": self._model_version,
                    "pending": (
                        self._pending_models.get("version")
                        if self._pending_models
                        else None
                    ),
                },
            }
        )
        msg = String()
        msg.data = payload
        self._status_pub.publish(msg)

    def _publish_progress(self, pct: int, component: str = "") -> None:
        msg = String()
        msg.data = json.dumps({"percent": pct, "component": component})
        self._progress_pub.publish(msg)


def main(args=None):
    rclpy.init(args=args)
    node = OtaAgentNode()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()


if __name__ == "__main__":
    main()
