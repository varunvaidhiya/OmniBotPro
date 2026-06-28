"""Unitree DDS adapter — Go2 / quadrupeds.

Bridges the unified ``Robot`` API to Unitree's high-level sport client over
CycloneDDS (``unitree_sdk2py``). The SDK + cyclonedds are heavy and imported
lazily inside ``connect()`` (the `[unitree]` extra), so importing this module is
free and the pure mapping functions below are unit-testable without any DDS.

What's complete here: the structure and the protocol mapping (velocity → Move,
SportModeState → Telemetry). The actual on-robot DDS subscription is wired during
M1 hardware bring-up; see ``sdk/AGENTS.md`` §8/§9.
"""

from __future__ import annotations

import time
from typing import Optional, Tuple

from ..registry import RobotSpec
from ..schema import ConnectionState, Odometry, Telemetry, TransportStatus, Velocity
from ..transport import BaseTransport
from .errors import AdapterUnavailable


# ── pure, testable protocol mapping ────────────────────────────────────────────
def velocity_to_move(vel: Velocity) -> Tuple[float, float, float]:
    """Map a unified ``Velocity`` to Unitree ``SportClient.Move(vx, vy, vyaw)``."""
    return (vel.linear_x, vel.linear_y, vel.angular_z)


def sportstate_to_telemetry(state: dict) -> Telemetry:
    """Map a SportModeState-like dict to ``Telemetry``.

    Expected keys (all optional): ``position`` [x,y,z], ``velocity`` [vx,vy,vz],
    ``yaw_speed`` (rad/s), ``imu_rpy`` [roll,pitch,yaw], ``battery`` (0..1).
    """
    pos = state.get("position") or [0.0, 0.0, 0.0]
    vel = state.get("velocity") or [0.0, 0.0, 0.0]
    rpy = state.get("imu_rpy") or [0.0, 0.0, 0.0]
    odom = Odometry(
        x=float(pos[0]),
        y=float(pos[1]),
        theta=float(rpy[2]),
        vx=float(vel[0]),
        vy=float(vel[1]),
        omega=float(state.get("yaw_speed", 0.0)),
    )
    return Telemetry(odom=odom, battery=state.get("battery"))


class UnitreeDdsTransport(BaseTransport):
    protocol = "dds"

    def __init__(self, spec: RobotSpec, address: str = "", *, iface: str = "") -> None:
        super().__init__()
        self.spec = spec
        self.address = address  # DDS network interface / host hint
        self._iface = iface
        self._client: Optional[object] = None
        self._latest = Telemetry(odom=Odometry())
        self._estopped = False
        self._state = ConnectionState.IDLE
        self._connected_since: Optional[float] = None

    def connect(self) -> TransportStatus:
        try:  # heavy SDK + DDS — lazy
            from unitree_sdk2py.core.channel import (  # type: ignore
                ChannelFactoryInitialize,
            )
            from unitree_sdk2py.go2.sport.sport_client import SportClient  # type: ignore
        except Exception as e:
            raise AdapterUnavailable(
                "The Unitree DDS adapter needs unitree_sdk2py + cyclonedds: "
                "pip install 'ohho-os[unitree]' (with a working CycloneDDS). "
                "Use transport='sim://' to explore without hardware."
            ) from e
        ChannelFactoryInitialize(0, self.address or self._iface or "")
        client = SportClient()
        client.Init()
        self._client = client
        self._state = ConnectionState.CONNECTED
        self._connected_since = time.time()
        s = self.status()
        self._emit_status(s)
        return s

    def _on_state(self, msg: object) -> None:
        """Bridge a raw SportModeState message to telemetry (wired in bring-up)."""
        imu = getattr(msg, "imu_state", None)
        state = {
            "position": list(getattr(msg, "position", [0.0, 0.0, 0.0])),
            "velocity": list(getattr(msg, "velocity", [0.0, 0.0, 0.0])),
            "yaw_speed": float(getattr(msg, "yaw_speed", 0.0)),
            "imu_rpy": list(getattr(imu, "rpy", [0.0, 0.0, 0.0])),
        }
        self._latest = sportstate_to_telemetry(state)
        self._emit_telemetry(self._latest)

    def disconnect(self) -> None:
        if self._client is not None:
            stop = getattr(self._client, "StopMove", None)
            if callable(stop):
                try:
                    stop()
                except Exception:
                    pass
        self._state = ConnectionState.DISCONNECTED
        self._emit_status(self.status())

    def status(self) -> TransportStatus:
        return TransportStatus(
            protocol=self.protocol,
            state=self._state,
            label=f"Unitree DDS · {self.address or 'default'}",
            connected_since=self._connected_since,
        )

    def read(self) -> Telemetry:
        return self._latest

    def send_velocity(self, vel: Velocity) -> None:
        if self._estopped or self._client is None:
            return
        vx, vy, vyaw = velocity_to_move(vel)
        move = getattr(self._client, "Move", None)
        if callable(move):
            try:
                move(vx, vy, vyaw)
            except Exception:
                pass

    def send_joint_command(self, name: str, position: float) -> None:
        return  # Go2 has no arm; low-level joint control is a future adapter

    def emergency_stop(self) -> None:
        self._estopped = True
        if self._client is not None:
            stop = getattr(self._client, "StopMove", None)
            if callable(stop):
                try:
                    stop()
                except Exception:
                    pass

    def release_stop(self) -> None:
        self._estopped = False
