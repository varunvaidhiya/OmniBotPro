"""OhhO OS — the open-source, robot-agnostic engine for any robot.

Public surface:

    from ohho import Robot, connect
    bot = Robot.connect("omnibot")
"""

from __future__ import annotations

__version__ = "1.1.0"

from .schema import (
    ConnectionState,
    JointReading,
    Odometry,
    Scan,
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
    "Scan",
    "Telemetry",
    "TransportStatus",
    "ConnectionState",
]
