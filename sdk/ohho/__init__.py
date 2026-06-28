"""OhhO OS — the open-source, robot-agnostic engine for any robot.

Public surface:

    from ohho import Robot, connect
    bot = Robot.connect("omnibot")
"""

from __future__ import annotations

__version__ = "0.1.0"

from .schema import (
    ConnectionState,
    JointReading,
    Odometry,
    Telemetry,
    TransportStatus,
    Velocity,
)
from .robot import Robot, connect
from .registry import RobotSpec, get_spec, list_specs, register

__all__ = [
    "__version__",
    "Robot",
    "connect",
    "RobotSpec",
    "get_spec",
    "list_specs",
    "register",
    "Velocity",
    "Odometry",
    "JointReading",
    "Telemetry",
    "TransportStatus",
    "ConnectionState",
]
