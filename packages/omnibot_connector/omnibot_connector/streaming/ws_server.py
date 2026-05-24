"""WebSocket push server (port 8082) — streams robot state and camera frames to agents."""
from __future__ import annotations

import asyncio
import base64
import json
import logging
import time

import websockets
import websockets.exceptions

logger = logging.getLogger(__name__)

_state_store = None
_cfg = None

# Audio events pushed here by the voice pipeline
_audio_events: asyncio.Queue = asyncio.Queue()


def init(state_store, cfg) -> None:
    global _state_store, _cfg
    _state_store = state_store
    _cfg = cfg


async def handler(websocket, path: str) -> None:
    logger.info("WS stream client connected: %s", path)
    try:
        if path == "/state":
            await _stream_state(websocket)
        elif path.startswith("/camera/"):
            camera = path.split("/")[-1]
            await _stream_camera(websocket, camera)
        elif path == "/audio":
            await _stream_audio(websocket)
        else:
            await websocket.send(json.dumps({"error": f"Unknown stream path: {path}"}))
    except websockets.exceptions.ConnectionClosed:
        pass
    except Exception as exc:
        logger.debug("WS handler error (%s): %s", path, exc)


async def _stream_state(websocket) -> None:
    while True:
        if _state_store is None:
            await asyncio.sleep(0.5)
            continue
        snap = await _state_store.snapshot()
        data = {
            "position": snap.position,
            "velocity": snap.velocity,
            "arm_positions": snap.arm_positions,
            "control_mode": snap.control_mode,
            "mission_status": snap.mission_status,
            "rosbridge_connected": snap.rosbridge_connected,
            "timestamp": time.time(),
        }
        await websocket.send(json.dumps(data))
        await asyncio.sleep(0.2)  # 5 Hz


async def _stream_camera(websocket, camera: str) -> None:
    valid = {"front", "wrist", "bev"}
    if camera not in valid:
        await websocket.send(json.dumps({"error": f"Unknown camera '{camera}'. Use: {valid}"}))
        return
    fps = _cfg.camera_fps if _cfg else 2
    interval = 1.0 / max(1, fps)
    last_sent: bytes = b""
    while True:
        if _state_store is None:
            await asyncio.sleep(interval)
            continue
        jpeg = await _state_store.get_camera(camera)
        if jpeg and jpeg != last_sent:
            b64 = base64.b64encode(jpeg).decode()
            await websocket.send(
                json.dumps({"camera": camera, "image_base64": b64, "timestamp": time.time()})
            )
            last_sent = jpeg
        await asyncio.sleep(interval)


async def _stream_audio(websocket) -> None:
    while True:
        event = await _audio_events.get()
        await websocket.send(json.dumps(event))


def push_audio_event(event: dict) -> None:
    try:
        _audio_events.put_nowait(event)
    except asyncio.QueueFull:
        pass


async def serve(host: str = "0.0.0.0", port: int = 8082) -> None:
    logger.info("WebSocket stream server starting on ws://%s:%d", host, port)
    async with websockets.serve(handler, host, port):
        await asyncio.Future()


def main() -> None:
    from omnibot_connector.config import get_config
    from omnibot_connector.state import RobotStateStore

    cfg = get_config()
    store = RobotStateStore()
    init(store, cfg)
    asyncio.run(serve(port=cfg.stream_port))
