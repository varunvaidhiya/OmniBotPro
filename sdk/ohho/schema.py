"""Core data types shared across OhhO OS.

Ported from the TypeScript transport layer (website/lib/connect/types.ts) so the
SDK and the web consoles speak the same shapes. Pure standard library — no
third-party dependencies, importable anywhere.
"""

from __future__ import annotations

import time
from dataclasses import dataclass, field
from enum import Enum
from typing import Optional


class ConnectionState(str, Enum):
    """Lifecycle of a transport link."""

    IDLE = "idle"
    CONNECTING = "connecting"
    CONNECTED = "connected"
    RECONNECTING = "reconnecting"
    ERROR = "error"
    DISCONNECTED = "disconnected"


@dataclass
class Velocity:
    """A base velocity command. linear_y is honoured only on holonomic bases."""

    linear_x: float = 0.0
    linear_y: float = 0.0
    angular_z: float = 0.0


@dataclass
class Odometry:
    """Robot pose + body-frame velocity estimate."""

    x: float = 0.0
    y: float = 0.0
    theta: float = 0.0
    vx: float = 0.0
    vy: float = 0.0
    omega: float = 0.0


@dataclass
class JointReading:
    name: str
    position: float
    velocity: Optional[float] = None


@dataclass
class Scan:
    """A planar range scan in the robot frame (lidar-like, sim or synthesized).

    Ray ``k`` points at ``angle_min + k * angle_increment`` radians relative to
    the robot's heading; ``ranges[k]`` is the hit distance in metres, or
    ``range_max`` when nothing was hit.
    """

    angle_min: float
    angle_increment: float
    ranges: list[float] = field(default_factory=list)
    range_max: float = 4.0


@dataclass
class Telemetry:
    """One best-effort snapshot of a robot's state. All fields optional."""

    odom: Optional[Odometry] = None
    joints: list[JointReading] = field(default_factory=list)
    battery: Optional[float] = None  # 0..1 fraction when known
    scan: Optional[Scan] = None  # planar range scan, when the robot has one
    custom: dict = field(default_factory=dict)
    timestamp: float = field(default_factory=time.time)


@dataclass
class TransportStatus:
    """Live status of a transport link."""

    protocol: str
    state: ConnectionState
    label: str = ""
    latency_ms: float = 0.0
    msg_rate: float = 0.0
    error: Optional[str] = None
    connected_since: Optional[float] = None


def clamp(value: float, lo: float, hi: float) -> float:
    """Clamp ``value`` into the inclusive range [lo, hi]."""
    if value < lo:
        return lo
    if value > hi:
        return hi
    return value
