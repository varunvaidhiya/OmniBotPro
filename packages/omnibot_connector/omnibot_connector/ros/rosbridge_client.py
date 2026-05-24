from __future__ import annotations

import asyncio
import json
import logging
from typing import Optional

import websockets
import websockets.exceptions

logger = logging.getLogger(__name__)

OUTBOUND_TOPICS: dict[str, str] = {
    "/mission/command": "std_msgs/String",
    "/mission/cancel": "std_msgs/String",
    "/control_mode": "std_msgs/String",
    "/emergency_stop": "std_msgs/Bool",
    "/arm/joint_commands": "sensor_msgs/JointState",
    "/arm/cmd_mode": "std_msgs/String",
    "/ai/command": "std_msgs/String",
}

INBOUND_TOPICS: dict[str, str] = {
    "/odom": "nav_msgs/Odometry",
    "/arm/joint_states": "sensor_msgs/JointState",
    "/control_mode/active": "std_msgs/String",
    "/mission/status": "std_msgs/String",
    "/ai/status": "std_msgs/String",
    "/camera/front/image_raw": "sensor_msgs/Image",
    "/camera/wrist/image_raw": "sensor_msgs/Image",
    "/camera/base/bev/image_raw": "sensor_msgs/Image",
}

IMAGE_TOPICS: dict[str, str] = {
    "/camera/front/image_raw": "front",
    "/camera/wrist/image_raw": "wrist",
    "/camera/base/bev/image_raw": "bev",
}


class RosBridgeClient:
    def __init__(self, url: str, state_store) -> None:
        self._url = url
        self._state = state_store
        self._ws: Optional[websockets.WebSocketClientProtocol] = None
        self._send_lock = asyncio.Lock()
        self._running = False

    async def start(self) -> None:
        self._running = True
        asyncio.create_task(self._connect_loop())

    async def stop(self) -> None:
        self._running = False
        if self._ws:
            await self._ws.close()

    async def _connect_loop(self) -> None:
        delay = 2.0
        while self._running:
            try:
                async with websockets.connect(self._url) as ws:
                    self._ws = ws
                    await self._state.set_connected(True)
                    logger.info("ROSBridge connected: %s", self._url)
                    delay = 2.0
                    await self._on_connected(ws)
                    await self._receive_loop(ws)
            except (OSError, websockets.exceptions.WebSocketException) as exc:
                logger.warning("ROSBridge error: %s — retry in %.0fs", exc, delay)
            finally:
                self._ws = None
                await self._state.set_connected(False)
            await asyncio.sleep(delay)
            delay = min(delay * 2, 30.0)

    async def _on_connected(self, ws) -> None:
        for topic, msg_type in OUTBOUND_TOPICS.items():
            await self._send_raw(
                ws, {"op": "advertise", "topic": topic, "type": msg_type}
            )
        for topic, msg_type in INBOUND_TOPICS.items():
            await self._send_raw(
                ws, {"op": "subscribe", "topic": topic, "type": msg_type}
            )

    async def _receive_loop(self, ws) -> None:
        async for raw in ws:
            try:
                frame = json.loads(raw)
            except json.JSONDecodeError:
                continue
            if frame.get("op") != "publish":
                continue
            topic = frame.get("topic", "")
            msg = frame.get("msg", {})
            await self._dispatch(topic, msg)

    async def _dispatch(self, topic: str, msg: dict) -> None:
        try:
            if topic == "/odom":
                await self._state.update_odom(msg)
            elif topic == "/arm/joint_states":
                await self._state.update_arm(msg)
            elif topic == "/control_mode/active":
                await self._state.update_string("control_mode", msg.get("data", ""))
            elif topic == "/mission/status":
                await self._state.update_string("mission_status", msg.get("data", ""))
            elif topic == "/ai/status":
                await self._state.update_string("ai_status", msg.get("data", ""))
            elif topic in IMAGE_TOPICS:
                jpeg = _ros_image_to_jpeg(msg)
                if jpeg:
                    await self._state.update_camera(IMAGE_TOPICS[topic], jpeg)
        except Exception as exc:
            logger.debug("Dispatch error for %s: %s", topic, exc)

    async def publish(self, topic: str, msg: dict) -> None:
        if not self._ws:
            raise RuntimeError("ROSBridge not connected")
        async with self._send_lock:
            await self._send_raw(
                self._ws, {"op": "publish", "topic": topic, "msg": msg}
            )

    @staticmethod
    async def _send_raw(ws, payload: dict) -> None:
        await ws.send(json.dumps(payload))


def _ros_image_to_jpeg(msg: dict) -> Optional[bytes]:
    try:
        import io

        import numpy as np
        from PIL import Image

        encoding = msg.get("encoding", "bgr8")
        height = msg.get("height", 0)
        width = msg.get("width", 0)
        data = msg.get("data", [])
        if not data or not height or not width:
            return None
        arr = np.frombuffer(bytes(data), dtype=np.uint8).reshape((height, width, 3))
        if encoding == "bgr8":
            arr = arr[:, :, ::-1]
        img = Image.fromarray(arr, "RGB")
        buf = io.BytesIO()
        img.save(buf, format="JPEG", quality=80)
        return buf.getvalue()
    except Exception:
        return None
