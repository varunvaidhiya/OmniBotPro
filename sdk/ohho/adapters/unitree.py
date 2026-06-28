"""Unitree DDS adapter — Go2 / quadrupeds.

Bridges the unified ``Robot`` API to Unitree's high-level sport client over
CycloneDDS (``unitree_sdk2py``). The SDK + cyclonedds are heavy and imported
lazily inside ``connect()`` (the ``[unitree]`` extra), so importing this module is
free and the pure mapping functions below are unit-testable without any DDS.

What's complete: the structure, the protocol mapping (velocity → Move,
SportModeState → Telemetry), and the **DDS state subscription** — a reader thread
polls the sport client's state at ~20 Hz and feeds ``_on_state``, which maps the
raw message to ``Telemetry`` and emits it. A ``state_factory`` can be injected for
tests (returns a callable that yields state dicts — no DDS, no hardware).
"""

from __future__ import annotations

import threading
import time
from typing import Callable, Optional, Tuple

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


StateFactory = Callable[[], Callable[[], Optional[dict]]]


class UnitreeDdsTransport(BaseTransport):
    protocol = "dds"

    def __init__(
        self,
        spec: RobotSpec,
        address: str = "",
        *,
        iface: str = "",
        state_factory: Optional[StateFactory] = None,
    ) -> None:
        super().__init__()
        self.spec = spec
        self.address = address  # DDS network interface / host hint
        self._iface = iface
        self._state_factory = state_factory
        self._client: Optional[object] = None
        self._state_getter: Optional[Callable[[], Optional[dict]]] = None
        self._latest = Telemetry(odom=Odometry())
        self._estopped = False
        self._state = ConnectionState.IDLE
        self._connected_since: Optional[float] = None
        self._thread: Optional[threading.Thread] = None
        self._stop = threading.Event()

    # ── lifecycle ─────────────────────────────────────────────────────────────
    def connect(self) -> TransportStatus:
        if self._state_factory is not None:
            self._state_getter = self._state_factory()
        else:
            self._connect_dds()
        self._state = ConnectionState.CONNECTED
        self._connected_since = time.time()
        self._start_reader()
        s = self.status()
        self._emit_status(s)
        return s

    def _connect_dds(self) -> None:
        try:
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
        self._state_getter = self._make_sport_state_getter(client)

    def _make_sport_state_getter(self, client: object) -> Callable[[], Optional[dict]]:
        """Build a callable that polls the SportClient for a state dict.

        Uses ``client.GetState(SportModeState)`` if available (SDK version
        dependent); falls back to attribute-sniffing on the client itself.
        """

        def getter() -> Optional[dict]:
            try:
                from unitree_sdk2py.idl.go2.sportmode import SportModeState  # type: ignore

                msg = getattr(client, "GetState", None)
                if callable(msg):
                    state = msg(SportModeState)
                else:
                    state = getattr(client, "_state", None)
                if state is None:
                    return None
                return self._sportstate_to_dict(state)
            except Exception:
                return None

        return getter

    @staticmethod
    def _sportstate_to_dict(msg: object) -> dict:
        imu = getattr(msg, "imu_state", None)
        return {
            "position": list(getattr(msg, "position", [0.0, 0.0, 0.0])),
            "velocity": list(getattr(msg, "velocity", [0.0, 0.0, 0.0])),
            "yaw_speed": float(getattr(msg, "yaw_speed", 0.0)),
            "imu_rpy": list(getattr(imu, "rpy", [0.0, 0.0, 0.0])),
            "battery": getattr(msg, "battery_soc", None),
        }

    def _on_state(self, state: dict) -> None:
        """Bridge a state dict to telemetry and emit."""
        self._latest = sportstate_to_telemetry(state)
        self._emit_telemetry(self._latest)

    def disconnect(self) -> None:
        self._stop_reader()
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

    # ── reader thread ─────────────────────────────────────────────────────────
    def _start_reader(self, hz: float = 20.0) -> None:
        if self._thread is not None:
            return
        self._stop.clear()
        period = 1.0 / hz

        def loop() -> None:
            while not self._stop.is_set():
                if self._state_getter is not None:
                    state = self._state_getter()
                    if state is not None:
                        self._on_state(state)
                time.sleep(period)

        self._thread = threading.Thread(
            target=loop, name="ohho-unitree-rx", daemon=True
        )
        self._thread.start()

    def _stop_reader(self) -> None:
        self._stop.set()
        t = self._thread
        if t is not None:
            t.join(timeout=1.0)
            self._thread = None

    # ── commands / telemetry ──────────────────────────────────────────────────
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
