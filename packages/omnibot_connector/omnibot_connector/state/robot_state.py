from __future__ import annotations

import asyncio
import copy
import math
import time
from dataclasses import dataclass, field
from typing import Dict, Optional


@dataclass
class RobotState:
    position: Dict = field(default_factory=lambda: {"x": 0.0, "y": 0.0, "theta": 0.0})
    velocity: Dict = field(default_factory=lambda: {"vx": 0.0, "vy": 0.0, "omega": 0.0})
    arm_positions: list = field(default_factory=lambda: [0.0] * 6)
    arm_velocities: list = field(default_factory=lambda: [0.0] * 6)
    control_mode: str = "unknown"
    mission_status: str = "idle"
    ai_status: str = ""
    camera_frames: Dict[str, bytes] = field(default_factory=dict)
    last_updated: float = field(default_factory=time.monotonic)
    rosbridge_connected: bool = False


class RobotStateStore:
    """asyncio-safe state store shared by ROSBridge client and all API handlers."""

    def __init__(self) -> None:
        self._state = RobotState()
        self._lock = asyncio.Lock()

    async def update_odom(self, msg: dict) -> None:
        async with self._lock:
            pose = msg.get("pose", {}).get("pose", {})
            twist = msg.get("twist", {}).get("twist", {})
            pos = pose.get("position", {})
            lin = twist.get("linear", {})
            ang = twist.get("angular", {})
            q = pose.get("orientation", {})
            theta = 2.0 * math.atan2(q.get("z", 0.0), q.get("w", 1.0))
            self._state.position = {
                "x": pos.get("x", 0.0),
                "y": pos.get("y", 0.0),
                "theta": theta,
            }
            self._state.velocity = {
                "vx": lin.get("x", 0.0),
                "vy": lin.get("y", 0.0),
                "omega": ang.get("z", 0.0),
            }
            self._state.last_updated = time.monotonic()

    async def update_arm(self, msg: dict) -> None:
        async with self._lock:
            pos = msg.get("position", [])
            vel = msg.get("velocity", [])
            self._state.arm_positions = list(pos[:6]) + [0.0] * max(0, 6 - len(pos))
            self._state.arm_velocities = list(vel[:6]) + [0.0] * max(0, 6 - len(vel))
            self._state.last_updated = time.monotonic()

    async def update_string(self, field_name: str, value: str) -> None:
        async with self._lock:
            setattr(self._state, field_name, value)
            self._state.last_updated = time.monotonic()

    async def update_camera(self, name: str, jpeg_bytes: bytes) -> None:
        async with self._lock:
            self._state.camera_frames[name] = jpeg_bytes
            self._state.last_updated = time.monotonic()

    async def set_connected(self, connected: bool) -> None:
        async with self._lock:
            self._state.rosbridge_connected = connected

    async def snapshot(self) -> RobotState:
        async with self._lock:
            return copy.deepcopy(self._state)

    async def get_camera(self, name: str) -> Optional[bytes]:
        async with self._lock:
            return self._state.camera_frames.get(name)
