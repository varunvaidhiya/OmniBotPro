"""Feetech arm adapter — SO-101 6-DOF arm (OmniBot manipulation).

The SO-101 arm lives on a *separate* Feetech (STS3215) bus from the Yahboom base
board — typically ``/dev/ttyACM0`` at 1 000 000 baud. This adapter speaks to it
via LeRobot's ``FeetechMotorsBus`` (lazy import, the ``[arm]`` extra).

- ``send_velocity`` is a no-op (the arm has no base).
- ``send_joint_command`` writes ``Goal_Position`` to the named servo.
- ``read`` returns the present joint positions as ``Telemetry``.
- ``emergency_stop`` disables torque; ``release_stop`` re-enables it.

A fake bus can be injected (``bus_factory=``) so the adapter is fully testable
with no hardware and no lerobot install. The rad↔tick conversion and clamping
logic is pure and unit-tested without any bus at all.

Constants mirror the ROS arm driver (``robot_ws/src/omnibot_arm``) and
``AGENTS.md`` §"Key Physical Constants": 4096 ticks/rev, home 2048, baud 1 Mbps.
"""

from __future__ import annotations

import math
import threading
import time
from typing import Callable, Optional, Sequence

from ..registry import RobotSpec
from ..schema import (
    ConnectionState,
    JointReading,
    Odometry,
    Telemetry,
    TransportStatus,
    Velocity,
)
from ..transport import BaseTransport
from .errors import AdapterUnavailable

TICKS_PER_REV: int = 4096
HOME_TICKS: int = 2048
TICKS_PER_RAD: float = TICKS_PER_REV / (2.0 * math.pi)
DEFAULT_BAUD: int = 1_000_000
DEFAULT_PORT: str = "/dev/ttyACM0"
MOTOR_MODEL: str = "sts3215"
DEFAULT_MOTOR_IDS: tuple[int, ...] = (1, 2, 3, 4, 5, 6)
DEFAULT_JOINT_MIN: tuple[float, ...] = (-3.14, -1.57, -1.57, -1.57, -3.14, -0.1)
DEFAULT_JOINT_MAX: tuple[float, ...] = (3.14, 1.57, 1.57, 1.57, 3.14, 0.8)

BusFactory = Callable[[], object]


def ticks_to_radians(ticks: Sequence[int], home: int = HOME_TICKS) -> list[float]:
    """Convert raw servo ticks to joint angles in radians."""
    return [(t - home) / TICKS_PER_RAD for t in ticks]


def radians_to_ticks(radians: Sequence[float], home: int = HOME_TICKS) -> list[int]:
    """Convert joint angles in radians to servo ticks (rounded)."""
    return [int(round(r * TICKS_PER_RAD + home)) for r in radians]


def clamp_joints(
    radians: Sequence[float],
    joint_min: Sequence[float] = DEFAULT_JOINT_MIN,
    joint_max: Sequence[float] = DEFAULT_JOINT_MAX,
) -> list[float]:
    """Clamp each joint angle to its declared limits."""
    return [max(mn, min(mx, r)) for r, mn, mx in zip(radians, joint_min, joint_max)]


class FeetechTransport(BaseTransport):
    protocol = "feetech"

    def __init__(
        self,
        spec: RobotSpec,
        address: str = "",
        *,
        baud: int = DEFAULT_BAUD,
        bus_factory: Optional[BusFactory] = None,
        motor_ids: Optional[Sequence[int]] = None,
        joint_min: Optional[Sequence[float]] = None,
        joint_max: Optional[Sequence[float]] = None,
    ) -> None:
        super().__init__()
        self.spec = spec
        self.port = address or DEFAULT_PORT
        self.baud = baud
        self._bus_factory = bus_factory
        self._bus: Optional[object] = None
        self._joint_names = tuple(spec.joint_names)
        self._motor_ids = tuple(
            motor_ids or DEFAULT_MOTOR_IDS[: len(self._joint_names)]
        )
        self._joint_min = tuple(
            joint_min or DEFAULT_JOINT_MIN[: len(self._joint_names)]
        )
        self._joint_max = tuple(
            joint_max or DEFAULT_JOINT_MAX[: len(self._joint_names)]
        )
        self._positions: dict[str, float] = {n: 0.0 for n in self._joint_names}
        self._torque_on: bool = True
        self._estopped: bool = False
        self._state = ConnectionState.IDLE
        self._connected_since: Optional[float] = None
        self._thread: Optional[threading.Thread] = None
        self._stop = threading.Event()

    # ── lifecycle ─────────────────────────────────────────────────────────────
    def _motors_dict(self) -> dict[str, tuple[int, str]]:
        return {
            name: (mid, MOTOR_MODEL)
            for name, mid in zip(self._joint_names, self._motor_ids)
        }

    def _open_bus(self) -> object:
        if self._bus_factory is not None:
            return self._bus_factory()
        try:
            from lerobot.common.robot_devices.motors.feetech import (  # type: ignore
                FeetechMotorsBus,
            )
        except Exception as e:
            raise AdapterUnavailable(
                "The Feetech arm adapter needs lerobot: "
                "pip install 'ohho-os[arm]'. Use transport='sim://' to explore "
                "without hardware."
            ) from e
        return FeetechMotorsBus(port=self.port, motors=self._motors_dict())

    def connect(self) -> TransportStatus:
        self._bus = self._open_bus()
        connect = getattr(self._bus, "connect", None)
        if callable(connect):
            connect()
        self._state = ConnectionState.CONNECTED
        self._connected_since = time.time()
        self._start_reader()
        s = self.status()
        self._emit_status(s)
        return s

    def disconnect(self) -> None:
        self._stop_reader()
        if self._bus is not None:
            if self._torque_on:
                self._disable_torque()
            close = getattr(self._bus, "disconnect", None)
            if callable(close):
                try:
                    close()
                except Exception:
                    pass
        self._state = ConnectionState.DISCONNECTED
        self._emit_status(self.status())

    def status(self) -> TransportStatus:
        return TransportStatus(
            protocol=self.protocol,
            state=self._state,
            label=f"Feetech arm · {self.port}",
            connected_since=self._connected_since,
        )

    # ── commands ──────────────────────────────────────────────────────────────
    def send_velocity(self, vel: Velocity) -> None:
        return  # arm has no base

    def send_joint_command(self, name: str, position: float) -> None:
        if self._estopped or not self._torque_on or self._bus is None:
            return
        if name not in self._positions:
            return
        idx = self._joint_names.index(name)
        clamped = max(self._joint_min[idx], min(self._joint_max[idx], position))
        tick = int(round(clamped * TICKS_PER_RAD + HOME_TICKS))
        write = getattr(self._bus, "write", None)
        if callable(write):
            try:
                write("Goal_Position", {name: tick})
            except Exception:
                pass

    def emergency_stop(self) -> None:
        self._estopped = True
        self._disable_torque()

    def release_stop(self) -> None:
        self._estopped = False
        self._enable_torque()

    def _enable_torque(self) -> None:
        if self._bus is None:
            return
        self._torque_on = True
        write = getattr(self._bus, "write", None)
        if callable(write):
            try:
                write("Torque_Enable", {n: 1 for n in self._joint_names})
            except Exception:
                pass

    def _disable_torque(self) -> None:
        if self._bus is None:
            return
        self._torque_on = False
        write = getattr(self._bus, "write", None)
        if callable(write):
            try:
                write("Torque_Enable", {n: 0 for n in self._joint_names})
            except Exception:
                pass

    # ── telemetry ─────────────────────────────────────────────────────────────
    def read(self) -> Telemetry:
        return self._snapshot()

    def _snapshot(self) -> Telemetry:
        joints = [JointReading(name=n, position=p) for n, p in self._positions.items()]
        return Telemetry(odom=Odometry(), joints=joints)

    def _read_positions(self) -> dict[str, float]:
        if self._bus is None:
            return dict(self._positions)
        read = getattr(self._bus, "read", None)
        if not callable(read):
            return dict(self._positions)
        try:
            ticks_dict = read("Present_Position")
        except Exception:
            return dict(self._positions)
        out: dict[str, float] = {}
        for name in self._joint_names:
            tick = ticks_dict.get(name, HOME_TICKS)
            out[name] = (tick - HOME_TICKS) / TICKS_PER_RAD
        return out

    def _poll_once(self) -> None:
        self._positions = self._read_positions()
        self._emit_telemetry(self._snapshot())

    def _start_reader(self, hz: float = 50.0) -> None:
        if self._thread is not None:
            return
        self._stop.clear()
        period = 1.0 / hz

        def loop() -> None:
            while not self._stop.is_set():
                self._poll_once()
                time.sleep(period)

        self._thread = threading.Thread(
            target=loop, name="ohho-feetech-rx", daemon=True
        )
        self._thread.start()

    def _stop_reader(self) -> None:
        self._stop.set()
        t = self._thread
        if t is not None:
            t.join(timeout=1.0)
            self._thread = None
