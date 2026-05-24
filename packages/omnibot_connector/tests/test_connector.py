"""Comprehensive tests for omnibot_connector."""

from __future__ import annotations

import asyncio
import json
import os
import tempfile
from pathlib import Path
from typing import Any
import pytest
import yaml
from fastapi import HTTPException, Request
from fastapi.testclient import TestClient
from pydantic import BaseModel


# ---------------------------------------------------------------------------
# Helpers / fixtures
# ---------------------------------------------------------------------------


class FakeRos:
    """Fake ROSBridge client that records published messages."""

    def __init__(self):
        self.published: list[tuple[str, dict]] = []

    async def publish(self, topic: str, msg: dict) -> None:
        self.published.append((topic, msg))

    def last(self, topic: str) -> dict | None:
        for t, m in reversed(self.published):
            if t == topic:
                return m
        return None


@pytest.fixture()
def fake_ros():
    return FakeRos()


@pytest.fixture()
def cfg():
    from omnibot_connector.config import ConnectorConfig

    return ConnectorConfig()


@pytest.fixture()
def state_store():
    from omnibot_connector.state import RobotStateStore

    return RobotStateStore()


# ---------------------------------------------------------------------------
# 1. ConnectorConfig — env var overrides and defaults
# ---------------------------------------------------------------------------


def test_config_defaults():
    from omnibot_connector.config import ConnectorConfig

    c = ConnectorConfig()
    assert c.rosbridge_url == "ws://localhost:9090"
    assert c.rest_port == 8080
    assert c.mcp_port == 8081
    assert c.stream_port == 8082
    assert c.max_lin_vel == pytest.approx(0.20)
    assert c.max_ang_vel == pytest.approx(1.00)
    assert c.camera_fps == 2
    assert c.whisper_model == "base.en"
    assert c.whisper_device == "cpu"


def test_config_env_override(monkeypatch):
    from omnibot_connector import config as cfg_mod

    monkeypatch.setenv("ROSBRIDGE_URL", "ws://robot:9090")
    monkeypatch.setenv("CONNECTOR_REST_PORT", "9999")
    monkeypatch.setenv("MAX_LIN_VEL", "0.5")
    monkeypatch.setenv("CAMERA_STREAM_FPS", "5")
    cfg_mod._cfg = None  # reset singleton
    c = cfg_mod.get_config()
    assert c.rosbridge_url == "ws://robot:9090"
    assert c.rest_port == 9999
    assert c.max_lin_vel == pytest.approx(0.5)
    assert c.camera_fps == 5
    cfg_mod._cfg = None  # cleanup singleton


def test_config_singleton():
    from omnibot_connector import config as cfg_mod

    cfg_mod._cfg = None
    a = cfg_mod.get_config()
    b = cfg_mod.get_config()
    assert a is b
    cfg_mod._cfg = None


# ---------------------------------------------------------------------------
# 2. RobotStateStore — concurrent updates and snapshot
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_state_store_odom_update(state_store):
    msg = {
        "pose": {
            "pose": {
                "position": {"x": 1.0, "y": 2.0, "z": 0.0},
                "orientation": {"x": 0.0, "y": 0.0, "z": 0.707, "w": 0.707},
            }
        },
        "twist": {
            "twist": {
                "linear": {"x": 0.1, "y": 0.0, "z": 0.0},
                "angular": {"x": 0.0, "y": 0.0, "z": 0.5},
            }
        },
    }
    await state_store.update_odom(msg)
    snap = await state_store.snapshot()
    assert snap.position["x"] == pytest.approx(1.0)
    assert snap.position["y"] == pytest.approx(2.0)
    assert snap.velocity["vx"] == pytest.approx(0.1)
    assert snap.velocity["omega"] == pytest.approx(0.5)


@pytest.mark.asyncio
async def test_state_store_arm_update(state_store):
    msg = {"position": [0.1, 0.2, 0.3, 0.4, 0.5, 0.6], "velocity": [0.01] * 6}
    await state_store.update_arm(msg)
    snap = await state_store.snapshot()
    assert snap.arm_positions == pytest.approx([0.1, 0.2, 0.3, 0.4, 0.5, 0.6])
    assert snap.arm_velocities == pytest.approx([0.01] * 6)


@pytest.mark.asyncio
async def test_state_store_arm_short_message(state_store):
    msg = {"position": [1.0, 2.0], "velocity": []}
    await state_store.update_arm(msg)
    snap = await state_store.snapshot()
    assert len(snap.arm_positions) == 6
    assert snap.arm_positions[0] == pytest.approx(1.0)
    assert snap.arm_positions[2] == pytest.approx(0.0)


@pytest.mark.asyncio
async def test_state_store_string_update(state_store):
    await state_store.update_string("control_mode", "nav2")
    snap = await state_store.snapshot()
    assert snap.control_mode == "nav2"


@pytest.mark.asyncio
async def test_state_store_camera_buffer(state_store):
    jpeg = b"\xff\xd8\xff\xe0fake_jpeg_data"
    await state_store.update_camera("front", jpeg)
    result = await state_store.get_camera("front")
    assert result == jpeg


@pytest.mark.asyncio
async def test_state_store_camera_missing(state_store):
    result = await state_store.get_camera("nonexistent")
    assert result is None


@pytest.mark.asyncio
async def test_state_store_connected(state_store):
    await state_store.set_connected(True)
    snap = await state_store.snapshot()
    assert snap.rosbridge_connected is True
    await state_store.set_connected(False)
    snap = await state_store.snapshot()
    assert snap.rosbridge_connected is False


@pytest.mark.asyncio
async def test_state_store_snapshot_is_copy(state_store):
    await state_store.update_string("control_mode", "nav2")
    snap1 = await state_store.snapshot()
    await state_store.update_string("control_mode", "vla")
    snap2 = await state_store.snapshot()
    assert snap1.control_mode == "nav2"
    assert snap2.control_mode == "vla"


@pytest.mark.asyncio
async def test_state_store_concurrent_updates(state_store):
    async def updater(i):
        await state_store.update_string("control_mode", f"mode_{i}")

    await asyncio.gather(*[updater(i) for i in range(20)])
    snap = await state_store.snapshot()
    assert snap.control_mode.startswith("mode_")


# ---------------------------------------------------------------------------
# 3. Manipulation — joint limit validation
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_move_arm_valid(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import move_arm

    positions = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]
    result = await move_arm(positions, fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    assert fake_ros.last("/arm/joint_commands") is not None


@pytest.mark.asyncio
async def test_move_arm_wrong_count(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import move_arm

    with pytest.raises(ValueError, match="exactly 6"):
        await move_arm([0.0, 0.0, 0.0], fake_ros, state_store, cfg)


@pytest.mark.asyncio
async def test_move_arm_out_of_limit_high(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import move_arm

    with pytest.raises(ValueError, match="out of limits"):
        await move_arm([99.0, 0.0, 0.0, 0.0, 0.0, 0.0], fake_ros, state_store, cfg)


@pytest.mark.asyncio
async def test_move_arm_out_of_limit_low(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import move_arm

    with pytest.raises(ValueError, match="out of limits"):
        await move_arm([0.0, 0.0, 0.0, 0.0, 0.0, -1.0], fake_ros, state_store, cfg)


@pytest.mark.asyncio
async def test_move_arm_boundary_values(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import JOINT_MAX, JOINT_MIN, move_arm

    result = await move_arm(JOINT_MIN, fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    result = await move_arm(JOINT_MAX, fake_ros, state_store, cfg)
    assert result["status"] == "sent"


@pytest.mark.asyncio
async def test_execute_manipulation(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import execute_manipulation

    result = await execute_manipulation("pick up the cup", fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    msg = fake_ros.last("/mission/command")
    assert msg["data"] == "vla:pick up the cup"


# ---------------------------------------------------------------------------
# 4. Tool registry — all 13 tools present with required fields
# ---------------------------------------------------------------------------


def test_registry_tool_count():
    from omnibot_connector.tools import TOOLS

    assert len(TOOLS) == 13


def test_registry_all_tools_have_required_fields():
    from omnibot_connector.tools import TOOLS

    required_names = {
        "navigate_to_location",
        "navigate_to_pose",
        "navigate_then_manipulate",
        "execute_manipulation",
        "move_arm",
        "set_control_mode",
        "emergency_stop",
        "cancel_mission",
        "get_robot_state",
        "get_camera_image",
        "list_locations",
        "send_ai_command",
        "describe_scene",
    }
    for t in TOOLS:
        assert "name" in t, f"Tool missing 'name': {t}"
        assert "description" in t, f"Tool {t.get('name')} missing 'description'"
        assert "parameters" in t, f"Tool {t.get('name')} missing 'parameters'"
        assert isinstance(t["description"], str) and t["description"]
    assert {t["name"] for t in TOOLS} == required_names


def test_registry_dispatch_unknown_tool():
    from omnibot_connector.tools import dispatch

    async def run():
        await dispatch("nonexistent_tool", {}, None, None, None)

    with pytest.raises(ValueError, match="Unknown tool"):
        asyncio.get_event_loop().run_until_complete(run())


# ---------------------------------------------------------------------------
# 5. list_locations — parses yaml, handles missing file
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_list_locations_with_yaml(fake_ros, state_store):
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.tools.system import list_locations

    locs = {
        "locations": {
            "kitchen": {"x": 1.0, "y": 2.0, "yaw": 0.0},
            "home": {"x": 0.0, "y": 0.0, "yaw": 0.0},
        }
    }
    with tempfile.NamedTemporaryFile(mode="w", suffix=".yaml", delete=False) as f:
        yaml.safe_dump(locs, f)
        path = f.name
    try:
        cfg = ConnectorConfig()
        cfg.locations_yaml = path
        result = await list_locations(fake_ros, state_store, cfg)
        assert "kitchen" in result["locations"]
        assert "home" in result["locations"]
        assert result["details"]["kitchen"]["x"] == pytest.approx(1.0)
    finally:
        os.unlink(path)


@pytest.mark.asyncio
async def test_list_locations_missing_file(fake_ros, state_store):
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.tools.system import list_locations

    cfg = ConnectorConfig()
    cfg.locations_yaml = "/nonexistent/path/locations.yaml"
    result = await list_locations(fake_ros, state_store, cfg)
    assert result["locations"] == []
    assert "error" in result


# ---------------------------------------------------------------------------
# 6. set_control_mode — rejects invalid modes
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_set_control_mode_valid(fake_ros, state_store, cfg):
    from omnibot_connector.tools.system import set_control_mode

    for mode in ("nav2", "vla", "teleop", "rl_nav"):
        result = await set_control_mode(mode, fake_ros, state_store, cfg)
        assert result["status"] == "sent"
        assert result["mode"] == mode
        assert fake_ros.last("/control_mode")["data"] == mode


@pytest.mark.asyncio
async def test_set_control_mode_invalid(fake_ros, state_store, cfg):
    from omnibot_connector.tools.system import set_control_mode

    with pytest.raises(ValueError, match="mode must be one of"):
        await set_control_mode("fly", fake_ros, state_store, cfg)


@pytest.mark.asyncio
async def test_emergency_stop_activate(fake_ros, state_store, cfg):
    from omnibot_connector.tools.system import emergency_stop

    result = await emergency_stop(True, fake_ros, state_store, cfg)
    assert result["active"] is True
    assert fake_ros.last("/emergency_stop")["data"] is True


@pytest.mark.asyncio
async def test_emergency_stop_clear(fake_ros, state_store, cfg):
    from omnibot_connector.tools.system import emergency_stop

    result = await emergency_stop(False, fake_ros, state_store, cfg)
    assert result["active"] is False
    assert fake_ros.last("/emergency_stop")["data"] is False


@pytest.mark.asyncio
async def test_cancel_mission(fake_ros, state_store, cfg):
    from omnibot_connector.tools.system import cancel_mission

    result = await cancel_mission(fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    assert fake_ros.last("/mission/cancel") is not None


# ---------------------------------------------------------------------------
# 7. move_arm — additional coverage
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_move_arm_publishes_joint_names(fake_ros, state_store, cfg):
    from omnibot_connector.tools.manipulation import JOINT_NAMES, move_arm

    await move_arm([0.0] * 6, fake_ros, state_store, cfg)
    msg = fake_ros.last("/arm/joint_commands")
    assert msg["name"] == JOINT_NAMES
    assert len(msg["position"]) == 6


# ---------------------------------------------------------------------------
# 8. Navigation tools
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_navigate_to_location_no_yaml(fake_ros, state_store):
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.tools.navigation import navigate_to_location

    cfg = ConnectorConfig()
    cfg.locations_yaml = "/nonexistent/locations.yaml"
    result = await navigate_to_location("kitchen", fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    assert "navigate:kitchen" in result["command"]


@pytest.mark.asyncio
async def test_navigate_to_pose(fake_ros, state_store, cfg):
    from omnibot_connector.tools.navigation import navigate_to_pose

    result = await navigate_to_pose(1.5, -2.0, 1.57, fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    msg = fake_ros.last("/mission/command")
    assert "navigate_pose" in msg["data"]
    assert "1.500" in msg["data"]


@pytest.mark.asyncio
async def test_navigate_then_manipulate(fake_ros, state_store, cfg):
    from omnibot_connector.tools.navigation import navigate_then_manipulate

    cfg.locations_yaml = "/nonexistent/locations.yaml"
    result = await navigate_then_manipulate(
        "kitchen", "pick up the cup", fake_ros, state_store, cfg
    )
    assert result["status"] == "sent"
    msg = fake_ros.last("/mission/command")
    assert "navigate:kitchen" in msg["data"]
    assert "vla:pick up the cup" in msg["data"]


# ---------------------------------------------------------------------------
# 9. Perception tools
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_get_robot_state(fake_ros, state_store, cfg):
    from omnibot_connector.tools.perception import get_robot_state

    result = await get_robot_state(fake_ros, state_store, cfg)
    assert "position" in result
    assert "velocity" in result
    assert "arm_positions" in result
    assert "control_mode" in result
    assert "rosbridge_connected" in result


@pytest.mark.asyncio
async def test_get_camera_image_no_frame(fake_ros, state_store, cfg):
    from omnibot_connector.tools.perception import get_camera_image

    result = await get_camera_image("front", fake_ros, state_store, cfg)
    assert result["image_base64"] is None
    assert "error" in result


@pytest.mark.asyncio
async def test_get_camera_image_with_frame(fake_ros, state_store, cfg):
    import base64

    from omnibot_connector.tools.perception import get_camera_image

    jpeg = b"\xff\xd8\xff\xe0test_image_bytes"
    await state_store.update_camera("wrist", jpeg)
    result = await get_camera_image("wrist", fake_ros, state_store, cfg)
    assert result["image_base64"] == base64.b64encode(jpeg).decode()


@pytest.mark.asyncio
async def test_get_camera_image_invalid_camera(fake_ros, state_store, cfg):
    from omnibot_connector.tools.perception import get_camera_image

    with pytest.raises(ValueError, match="camera must be one of"):
        await get_camera_image("rear", fake_ros, state_store, cfg)


@pytest.mark.asyncio
async def test_describe_scene_no_frame(fake_ros, state_store, cfg):
    from omnibot_connector.tools.perception import describe_scene

    result = await describe_scene("front", fake_ros, state_store, cfg)
    assert "No camera frame" in result["description"]


@pytest.mark.asyncio
async def test_describe_scene_no_api(fake_ros, state_store, cfg):
    from omnibot_connector.tools.perception import describe_scene

    jpeg = b"\xff\xd8\xff\xe0fake"
    await state_store.update_camera("front", jpeg)
    cfg.anthropic_api_key = ""
    cfg.vla_serve_url = ""
    result = await describe_scene("front", fake_ros, state_store, cfg)
    assert "No vision API" in result["description"]


# ---------------------------------------------------------------------------
# 10. FastAPI REST API — TestClient tests
# ---------------------------------------------------------------------------


class _ToolCallRequest(BaseModel):
    tool: str
    arguments: dict[str, Any] = {}


def _make_test_app(state_store, fake_ros, cfg):
    """Build a FastAPI app with mocked lifespan state."""
    from contextlib import asynccontextmanager

    from fastapi import FastAPI

    @asynccontextmanager
    async def lifespan(app):
        from omnibot_connector.episodes import EpisodeRecorder

        with tempfile.TemporaryDirectory() as tmpdir:
            cfg.episode_dir = tmpdir
            recorder = EpisodeRecorder(cfg)
            await recorder.start()
            app.state.ros = fake_ros
            app.state.state_store = state_store
            app.state.cfg = cfg
            app.state.recorder = recorder
            yield

    test_app = FastAPI(lifespan=lifespan)

    @test_app.get("/health")
    async def health(request: Request):
        snap = await request.app.state.state_store.snapshot()
        return {"status": "ok", "rosbridge_connected": snap.rosbridge_connected}

    @test_app.get("/v1/tools")
    async def list_tools_ep():
        from omnibot_connector.tools import TOOLS

        return {"tools": TOOLS}

    @test_app.post("/v1/tools/call")
    async def call_tool_ep(body: _ToolCallRequest, request: Request):
        import time

        from omnibot_connector.tools import dispatch

        t0 = time.monotonic()
        try:
            result = await dispatch(
                body.tool,
                body.arguments,
                request.app.state.ros,
                request.app.state.state_store,
                request.app.state.cfg,
            )
            await request.app.state.recorder.record_tool_call(
                body.tool, body.arguments, result
            )
            return {
                "result": result,
                "latency_ms": round((time.monotonic() - t0) * 1000, 1),
            }
        except ValueError as exc:
            raise HTTPException(status_code=400, detail=str(exc))
        except RuntimeError as exc:
            raise HTTPException(status_code=503, detail=str(exc))

    @test_app.get("/v1/observe")
    async def observe(request: Request):
        from omnibot_connector.tools.perception import get_robot_state

        return await get_robot_state(
            request.app.state.ros,
            request.app.state.state_store,
            request.app.state.cfg,
        )

    return test_app


def test_rest_health():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.state import RobotStateStore

    ss = RobotStateStore()
    fr = FakeRos()
    cfg = ConnectorConfig()
    app = _make_test_app(ss, fr, cfg)
    with TestClient(app) as client:
        resp = client.get("/health")
        assert resp.status_code == 200
        data = resp.json()
        assert data["status"] == "ok"
        assert data["rosbridge_connected"] is False


def test_rest_list_tools():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.state import RobotStateStore

    ss = RobotStateStore()
    fr = FakeRos()
    cfg = ConnectorConfig()
    app = _make_test_app(ss, fr, cfg)
    with TestClient(app) as client:
        resp = client.get("/v1/tools")
        assert resp.status_code == 200
        tools = resp.json()["tools"]
        assert len(tools) == 13


def test_rest_call_tool_success():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.state import RobotStateStore

    ss = RobotStateStore()
    fr = FakeRos()
    cfg = ConnectorConfig()
    cfg.locations_yaml = "/nonexistent/locations.yaml"
    app = _make_test_app(ss, fr, cfg)
    with TestClient(app) as client:
        resp = client.post(
            "/v1/tools/call",
            json={"tool": "navigate_to_location", "arguments": {"location": "kitchen"}},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert data["result"]["status"] == "sent"
        assert "latency_ms" in data


def test_rest_call_tool_invalid_tool():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.state import RobotStateStore

    ss = RobotStateStore()
    fr = FakeRos()
    cfg = ConnectorConfig()
    app = _make_test_app(ss, fr, cfg)
    with TestClient(app) as client:
        resp = client.post(
            "/v1/tools/call",
            json={"tool": "does_not_exist", "arguments": {}},
        )
        assert resp.status_code == 400
        assert "Unknown tool" in resp.json()["detail"]


def test_rest_call_tool_invalid_arguments():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.state import RobotStateStore

    ss = RobotStateStore()
    fr = FakeRos()
    cfg = ConnectorConfig()
    app = _make_test_app(ss, fr, cfg)
    with TestClient(app) as client:
        resp = client.post(
            "/v1/tools/call",
            json={"tool": "set_control_mode", "arguments": {"mode": "warp_drive"}},
        )
        assert resp.status_code == 400


def test_rest_observe():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.state import RobotStateStore

    ss = RobotStateStore()
    fr = FakeRos()
    cfg = ConnectorConfig()
    app = _make_test_app(ss, fr, cfg)
    with TestClient(app) as client:
        resp = client.get("/v1/observe")
        assert resp.status_code == 200
        data = resp.json()
        assert "position" in data
        assert "arm_positions" in data


# ---------------------------------------------------------------------------
# 11. Episode recorder
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_episode_recorder_records_and_flushes():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.episodes import EpisodeRecorder

    with tempfile.TemporaryDirectory() as tmpdir:
        cfg = ConnectorConfig()
        cfg.episode_dir = tmpdir
        recorder = EpisodeRecorder(cfg)
        await recorder.start()
        await recorder.record_tool_call(
            "navigate_to_location", {"location": "kitchen"}, {"status": "sent"}
        )
        files = list(Path(tmpdir).glob("episode_*.json"))
        assert len(files) == 1
        with open(files[0]) as f:
            data = json.load(f)
        assert data["events"][0]["tool"] == "navigate_to_location"
        assert data["events"][0]["result"]["status"] == "sent"


@pytest.mark.asyncio
async def test_episode_recorder_no_flush_on_non_mission_tool():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.episodes import EpisodeRecorder

    with tempfile.TemporaryDirectory() as tmpdir:
        cfg = ConnectorConfig()
        cfg.episode_dir = tmpdir
        recorder = EpisodeRecorder(cfg)
        await recorder.start()
        await recorder.record_tool_call("get_robot_state", {}, {"position": {}})
        files = list(Path(tmpdir).glob("episode_*.json"))
        assert len(files) == 0


@pytest.mark.asyncio
async def test_episode_recorder_creates_dir():
    from omnibot_connector.config import ConnectorConfig
    from omnibot_connector.episodes import EpisodeRecorder

    with tempfile.TemporaryDirectory() as tmpdir:
        episode_dir = os.path.join(tmpdir, "deep", "nested", "dir")
        cfg = ConnectorConfig()
        cfg.episode_dir = episode_dir
        EpisodeRecorder(cfg)
        assert os.path.isdir(episode_dir)


# ---------------------------------------------------------------------------
# 12. send_ai_command
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_send_ai_command(fake_ros, state_store, cfg):
    from omnibot_connector.tools.system import send_ai_command

    result = await send_ai_command("find the red cup", fake_ros, state_store, cfg)
    assert result["status"] == "sent"
    assert fake_ros.last("/ai/command")["data"] == "find the red cup"
