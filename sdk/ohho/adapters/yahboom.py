"""Yahboom serial adapter — OmniBot base (Rosmaster X3).

Talks the Yahboom packet protocol over USB serial. Drives the holonomic base and
decodes the board's velocity/IMU telemetry into the unified ``Telemetry`` shape.
pyserial is imported lazily (the `[serial]` extra) so importing this module costs
nothing; a fake serial object can be injected for tests.

Note: the SO-101 arm on OmniBot is a *separate* Feetech bus, not the base board —
so ``send_joint_command`` here is a no-op. Arm control is a future adapter.
"""

from __future__ import annotations

import math
import threading
import time
from typing import Callable, Optional

from ..registry import RobotSpec
from ..schema import ConnectionState, Odometry, Telemetry, TransportStatus, Velocity
from ..transport import BaseTransport
from . import _yahboom_proto as proto
from .errors import AdapterUnavailable

# A serial-like object: write(bytes)->int, read(n)->bytes, close()->None.
SerialFactory = Callable[[], object]


class YahboomTransport(BaseTransport):
    protocol = "serial"

    def __init__(
        self,
        spec: RobotSpec,
        address: str = "",
        *,
        baud: int = 115200,
        serial_factory: Optional[SerialFactory] = None,
    ) -> None:
        super().__init__()
        self.spec = spec
        self.port = address or "/dev/ttyUSB0"
        self.baud = baud
        self._serial_factory = serial_factory
        self._ser: Optional[object] = None
        self._odom = Odometry()
        self._battery: Optional[float] = None
        self._estopped = False
        self._state = ConnectionState.IDLE
        self._connected_since: Optional[float] = None
        self._buf = bytearray()
        self._last_vel_t: Optional[float] = None
        self._thread: Optional[threading.Thread] = None
        self._stop = threading.Event()

    # ── lifecycle ─────────────────────────────────────────────────────────────
    def _open_serial(self) -> object:
        if self._serial_factory is not None:
            return self._serial_factory()
        try:
            import serial  # pyserial, lazy
        except Exception as e:  # pragma: no cover - exercised only without pyserial
            raise AdapterUnavailable(
                "The Yahboom serial adapter needs pyserial: "
                "pip install 'ohho-os[serial]'. Use transport='sim://' to explore "
                "without hardware."
            ) from e
        return serial.Serial(self.port, self.baud, timeout=0.05)

    def connect(self) -> TransportStatus:
        self._ser = self._open_serial()
        # The board wants its car-type set a few times at startup.
        for _ in range(3):
            self._write(proto.packet_set_car_type(proto.CAR_TYPE_MECANUM_X3))
        self._state = ConnectionState.CONNECTED
        self._connected_since = time.time()
        self._start_reader()
        s = self.status()
        self._emit_status(s)
        return s

    def disconnect(self) -> None:
        self._stop_reader()
        if self._ser is not None:
            self._write(proto.packet_motion(0.0, 0.0, 0.0))
            close = getattr(self._ser, "close", None)
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
            label=f"Yahboom serial · {self.port}",
            connected_since=self._connected_since,
        )

    # ── commands ──────────────────────────────────────────────────────────────
    def _write(self, data: bytes) -> None:
        if self._ser is None:
            return
        try:
            self._ser.write(data)  # type: ignore[attr-defined]
        except Exception:
            pass

    def send_velocity(self, vel: Velocity) -> None:
        if self._estopped:
            return
        self._write(proto.packet_motion(vel.linear_x, vel.linear_y, vel.angular_z))

    def send_joint_command(self, name: str, position: float) -> None:
        return  # base board does not control the arm

    def emergency_stop(self) -> None:
        self._estopped = True
        self._write(proto.packet_motion(0.0, 0.0, 0.0))

    def release_stop(self) -> None:
        self._estopped = False

    # ── telemetry ─────────────────────────────────────────────────────────────
    def read(self) -> Telemetry:
        return self._snapshot()

    def _snapshot(self) -> Telemetry:
        o = self._odom
        return Telemetry(
            odom=Odometry(o.x, o.y, o.theta, o.vx, o.vy, o.omega),
            battery=self._battery,
        )

    def _ingest(self, data: bytes) -> None:
        """Feed raw RX bytes: parse, integrate odometry, emit telemetry.

        This is the test seam — feed canned bytes here with no serial port.
        """
        if not data:
            return
        self._buf.extend(data)
        packets, consumed = proto.parse_stream(bytes(self._buf))
        if consumed:
            del self._buf[:consumed]
        if len(self._buf) > 4096:  # bound the buffer against junk
            del self._buf[:-256]
        if not packets:
            return
        now = time.monotonic()
        for pkt in packets:
            if isinstance(pkt, proto.VelocityPacket):
                if self._last_vel_t is not None:
                    dt = now - self._last_vel_t
                    c = math.cos(self._odom.theta)
                    s = math.sin(self._odom.theta)
                    self._odom.x += (self._odom.vx * c - self._odom.vy * s) * dt
                    self._odom.y += (self._odom.vx * s + self._odom.vy * c) * dt
                    self._odom.theta += self._odom.omega * dt
                self._last_vel_t = now
                self._odom.vx, self._odom.vy, self._odom.omega = pkt.vx, pkt.vy, pkt.vz
            elif isinstance(pkt, proto.ImuAttitudePacket):
                self._odom.theta = pkt.yaw
        self._emit_telemetry(self._snapshot())

    def _start_reader(self) -> None:
        if self._thread is not None:
            return
        self._stop.clear()

        def loop() -> None:
            while not self._stop.is_set():
                chunk = b""
                read = getattr(self._ser, "read", None)
                if callable(read):
                    try:
                        chunk = read(64)
                    except Exception:
                        chunk = b""
                if chunk:
                    self._ingest(chunk)
                else:
                    time.sleep(0.005)

        self._thread = threading.Thread(
            target=loop, name="ohho-yahboom-rx", daemon=True
        )
        self._thread.start()

    def _stop_reader(self) -> None:
        self._stop.set()
        t = self._thread
        if t is not None:
            t.join(timeout=1.0)
            self._thread = None
