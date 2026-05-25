"""
Unit tests for OtaAgentNode — pure logic, no hardware or network required.

Tests exercise SHA-256 helpers, state transitions, service callbacks,
snapshot/restore logic, and model apply path via internal methods.
"""

import hashlib
from unittest.mock import MagicMock, patch

import pytest
from std_srvs.srv import Trigger

from omnibot_ota.ota_agent_node import OtaAgentNode


@pytest.fixture()
def tmp_ws(tmp_path):
    install = tmp_path / "install"
    install.mkdir()
    (install / ".ota_version").write_text("v0.1.0")
    return tmp_path


@pytest.fixture()
def node(tmp_ws):
    n = OtaAgentNode()
    n._workspace_root = tmp_ws
    n._install_dir = tmp_ws / "install"
    n._backup_dir = tmp_ws / "install.backup"
    n._models_dir = tmp_ws / "models"
    n._models_dir.mkdir(exist_ok=True)
    n._cache_dir = tmp_ws / "cache"
    n._cache_dir.mkdir(exist_ok=True)
    n._status_pub = MagicMock()
    n._progress_pub = MagicMock()
    n._ws_version = "v0.1.0"
    yield n
    n.destroy_node()


def _req():
    return Trigger.Request()


def _resp():
    return Trigger.Response()


# ── SHA-256 ────────────────────────────────────────────────────────────────────


class TestSha256:
    def test_known_hash(self, tmp_path):
        f = tmp_path / "data.bin"
        f.write_bytes(b"omnibot")
        expected = hashlib.sha256(b"omnibot").hexdigest()
        assert OtaAgentNode._sha256(f) == expected

    def test_empty_file(self, tmp_path):
        f = tmp_path / "empty"
        f.write_bytes(b"")
        expected = hashlib.sha256(b"").hexdigest()
        assert OtaAgentNode._sha256(f) == expected


# ── Snapshot ───────────────────────────────────────────────────────────────────


class TestSnapshot:
    def test_copies_install_to_backup(self, node, tmp_ws):
        (tmp_ws / "install" / "sentinel.txt").write_text("data")
        node._snapshot_install()
        assert (tmp_ws / "install.backup" / "sentinel.txt").exists()
        # original still present
        assert (tmp_ws / "install" / "sentinel.txt").exists()

    def test_overwrites_old_backup(self, node, tmp_ws):
        backup = tmp_ws / "install.backup"
        backup.mkdir()
        (backup / "stale").write_text("old")
        (tmp_ws / "install" / "fresh").write_text("new")
        node._snapshot_install()
        assert not (backup / "stale").exists()
        assert (backup / "fresh").exists()


# ── Rollback ───────────────────────────────────────────────────────────────────


class TestRollback:
    def test_fails_gracefully_with_no_backup(self, node):
        resp = node._rollback_workspace_cb(_req(), _resp())
        assert resp.success is False
        assert "No backup" in resp.message

    def test_restores_backup_and_restarts(self, node, tmp_ws):
        # Create backup with a known marker
        backup = tmp_ws / "install.backup"
        backup.mkdir()
        (backup / ".ota_version").write_text("v0.0.9")
        (backup / "lib").mkdir()

        with patch("subprocess.run") as mock_run:
            mock_run.return_value = MagicMock(returncode=0)
            node._perform_rollback()

        # install.backup should be gone, install should be restored
        assert not backup.exists()
        assert (tmp_ws / "install" / ".ota_version").exists()
        mock_run.assert_called_once()


# ── Check service ──────────────────────────────────────────────────────────────


class TestCheckService:
    def test_empty_url_returns_failure(self, node):
        node._manifest_url = ""
        resp = node._check_cb(_req(), _resp())
        assert resp.success is False

    def test_manifest_fetch_failure_returns_failure(self, node):
        node._manifest_url = "http://127.0.0.1:1/manifest.json"
        with patch.object(node, "_fetch_manifest", return_value=None):
            resp = node._check_cb(_req(), _resp())
        assert resp.success is False

    def test_up_to_date_when_versions_match(self, node):
        node._ws_version = "v1.0.0"
        mock_manifest = {"version": "v1.0.0", "components": {}}
        with patch.object(node, "_fetch_manifest", return_value=mock_manifest):
            resp = node._check_cb(_req(), _resp())
        assert resp.success is True
        assert "up to date" in resp.message
        assert node._pending_workspace is None

    def test_sets_pending_when_new_version(self, node):
        node._ws_version = "v1.0.0"
        mock_manifest = {"version": "v1.1.0", "components": {}}
        with patch.object(node, "_fetch_manifest", return_value=mock_manifest):
            resp = node._check_cb(_req(), _resp())
        assert resp.success is True
        assert node._pending_workspace is not None
        assert node._ws_state == "UPDATE_AVAILABLE"


# ── Apply workspace ────────────────────────────────────────────────────────────


class TestApplyWorkspace:
    def test_apply_already_current(self, node):
        node._ws_version = "v1.0.0"
        node._pending_workspace = {"version": "v1.0.0", "components": {}}
        resp = node._apply_workspace_cb(_req(), _resp())
        assert resp.success is True
        assert "up to date" in resp.message

    def test_apply_starts_thread_when_update_available(self, node):
        node._ws_version = "v1.0.0"
        node._pending_workspace = {
            "version": "v1.1.0",
            "components": {
                "ros_workspace": {
                    "download_url": "http://example.com/ws.tar.zst",
                    "sha256": "abc",
                    "restart_service": "omnibot-robot.service",
                }
            },
        }
        with patch("threading.Thread") as mock_thread:
            mock_thread.return_value = MagicMock()
            resp = node._apply_workspace_cb(_req(), _resp())
        assert resp.success is True
        mock_thread.assert_called_once()


# ── Apply models ───────────────────────────────────────────────────────────────


class TestApplyModels:
    def test_model_apply_writes_to_models_dir(self, node, tmp_ws):
        models_dir = tmp_ws / "models"
        node._models_dir = models_dir

        fake_bytes = b"\x00" * 16
        expected_sha = hashlib.sha256(fake_bytes).hexdigest()

        manifest = {
            "version": "v1.1.0",
            "components": {
                "nav_policy_onnx": {
                    "download_url": "http://example.com/omnibot-nav-policy-v1.1.0.onnx",
                    "sha256": expected_sha,
                    "target_path": "~/models/omnibot_nav_policy.onnx",
                },
                "arm_policy_onnx": {
                    "download_url": "",
                    "sha256": "",
                    "target_path": "",
                },
            },
        }

        def fake_download(url, dest, component=""):
            dest.write_bytes(fake_bytes)

        with patch.object(node, "_download", side_effect=fake_download):
            node._apply_models(manifest)

        assert (models_dir / "omnibot_nav_policy.onnx").exists()
        assert node._model_state == "IDLE"
        assert node._model_version == "v1.1.0"
