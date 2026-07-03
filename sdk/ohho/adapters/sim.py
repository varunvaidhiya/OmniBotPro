"""Deterministic in-process simulator.

Implements the Transport interface with a holonomic base model and simple joint
servoing, so every console and the agent can run with no hardware on the bench.
``step(dt)`` advances physics deterministically (used by tests); ``start_sim()``
runs it on a background thread and streams telemetry (used live).
"""

from __future__ import annotations

import math
import threading
import time
from dataclasses import dataclass

from ..registry import RobotSpec
from ..schema import (
    ConnectionState,
    JointReading,
    Odometry,
    Scan,
    Telemetry,
    TransportStatus,
    Velocity,
)
from ..transport import BaseTransport


@dataclass
class SimObject:
    """A labeled circular object in the sim world (obstacle + perceivable)."""

    x: float
    y: float
    radius: float
    label: str = "obstacle"


def demo_world() -> list[SimObject]:
    """A small furnished room — obstacles for nav + labeled objects to perceive."""
    return [
        SimObject(1.5, 0.0, 0.30, "chair"),
        SimObject(2.5, 1.2, 0.45, "table"),
        SimObject(0.0, 2.0, 0.35, "plant"),
        SimObject(-1.5, -1.0, 0.40, "box"),
        SimObject(3.5, -1.5, 0.25, "cup"),
    ]


class SimTransport(BaseTransport):
    protocol = "simulated"

    def __init__(
        self,
        spec: RobotSpec,
        joint_rate: float = 2.0,
        objects: list[SimObject] | None = None,
        scan_rays: int = 72,
        scan_range: float = 4.0,
    ) -> None:
        super().__init__()
        self.spec = spec
        self._objects: list[SimObject] = list(objects or [])
        self._scan_rays = scan_rays
        self._scan_range = scan_range
        self._odom = Odometry()
        self._cmd = Velocity()
        self._joints: dict[str, float] = {n: 0.0 for n in spec.joint_names}
        self._joint_targets: dict[str, float] = dict(self._joints)
        self._joint_rate = joint_rate  # rad/s
        self._battery = 1.0
        self._estopped = False
        self._state = ConnectionState.IDLE
        self._connected_since: float | None = None
        self._thread: threading.Thread | None = None
        self._stop = threading.Event()

    # ── lifecycle ─────────────────────────────────────────────────────────────
    def connect(self) -> TransportStatus:
        self._state = ConnectionState.CONNECTED
        self._connected_since = time.time()
        s = self.status()
        self._emit_status(s)
        return s

    def disconnect(self) -> None:
        self.stop_sim()
        self._state = ConnectionState.DISCONNECTED
        self._emit_status(self.status())

    def status(self) -> TransportStatus:
        return TransportStatus(
            protocol=self.protocol,
            state=self._state,
            label=f"Simulator · {self.spec.id}",
            connected_since=self._connected_since,
        )

    # ── commands ──────────────────────────────────────────────────────────────
    def send_velocity(self, vel: Velocity) -> None:
        if self._estopped:
            return
        self._cmd = vel

    def send_joint_command(self, name: str, position: float) -> None:
        if name in self._joint_targets:
            self._joint_targets[name] = position

    def emergency_stop(self) -> None:
        self._estopped = True
        self._cmd = Velocity()

    def release_stop(self) -> None:
        self._estopped = False

    # ── world (obstacles / perceivable objects) ──────────────────────────────
    def add_object(
        self, x: float, y: float, radius: float, label: str = "obstacle"
    ) -> None:
        """Place a labeled circular object in the world (obstacle + perceivable)."""
        self._objects.append(SimObject(x, y, radius, label))

    def world_objects(self) -> list[SimObject]:
        """The labeled objects in the sim world (used by the sim perceptor)."""
        return list(self._objects)

    def _ray_cast(self) -> Scan:
        """Synthesize a planar range scan from the pose and the circle world."""
        n = self._scan_rays
        angle_min = -math.pi
        inc = 2.0 * math.pi / n
        px, py, th = self._odom.x, self._odom.y, self._odom.theta
        ranges: list[float] = []
        for k in range(n):
            a = th + angle_min + k * inc
            dx, dy = math.cos(a), math.sin(a)
            best = self._scan_range
            for ob in self._objects:
                mx, my = px - ob.x, py - ob.y
                b = dx * mx + dy * my
                c0 = mx * mx + my * my - ob.radius * ob.radius
                disc = b * b - c0
                if disc < 0.0:
                    continue
                t = -b - math.sqrt(disc)
                if 0.0 < t < best:
                    best = t
            ranges.append(best)
        return Scan(
            angle_min=angle_min,
            angle_increment=inc,
            ranges=ranges,
            range_max=self._scan_range,
        )

    # ── telemetry ─────────────────────────────────────────────────────────────
    def read(self) -> Telemetry:
        return self._snapshot()

    def _snapshot(self) -> Telemetry:
        joints = [JointReading(name=n, position=p) for n, p in self._joints.items()]
        odom = Odometry(
            self._odom.x,
            self._odom.y,
            self._odom.theta,
            self._odom.vx,
            self._odom.vy,
            self._odom.omega,
        )
        scan = self._ray_cast() if self._objects else None
        return Telemetry(odom=odom, joints=joints, battery=self._battery, scan=scan)

    # ── physics ───────────────────────────────────────────────────────────────
    def step(self, dt: float) -> Telemetry:
        """Advance the simulation by ``dt`` seconds and return the snapshot."""
        c = math.cos(self._odom.theta)
        s = math.sin(self._odom.theta)
        vx, vy, w = self._cmd.linear_x, self._cmd.linear_y, self._cmd.angular_z
        # integrate world-frame pose from body-frame command
        self._odom.x += (vx * c - vy * s) * dt
        self._odom.y += (vx * s + vy * c) * dt
        self._odom.theta += w * dt
        self._odom.vx, self._odom.vy, self._odom.omega = vx, vy, w

        # solid obstacles: project the pose back to the surface (slide along it)
        for ob in self._objects:
            dx = self._odom.x - ob.x
            dy = self._odom.y - ob.y
            d = math.hypot(dx, dy)
            if d < ob.radius:
                if d < 1e-9:
                    dx, dy, d = 1e-9, 0.0, 1e-9
                scale = ob.radius / d
                self._odom.x = ob.x + dx * scale
                self._odom.y = ob.y + dy * scale

        # joints servo toward targets at a bounded rate
        max_step = self._joint_rate * dt
        for name, target in self._joint_targets.items():
            cur = self._joints[name]
            delta = target - cur
            if delta > max_step:
                delta = max_step
            elif delta < -max_step:
                delta = -max_step
            self._joints[name] = cur + delta

        moving = (abs(vx) + abs(vy) + abs(w)) > 1e-6
        self._battery = max(0.0, self._battery - (0.0005 if moving else 0.0001) * dt)
        return self._snapshot()

    def start_sim(self, hz: float = 50.0, emit_hz: float = 10.0) -> None:
        if self._thread is not None:
            return
        self._stop.clear()
        period = 1.0 / hz
        emit_period = 1.0 / emit_hz

        def loop() -> None:
            last_emit = 0.0
            while not self._stop.is_set():
                snap = self.step(period)
                now = time.monotonic()
                if now - last_emit >= emit_period:
                    self._emit_telemetry(snap)
                    last_emit = now
                time.sleep(period)

        self._thread = threading.Thread(target=loop, name="ohho-sim", daemon=True)
        self._thread.start()

    def stop_sim(self) -> None:
        self._stop.set()
        thread = self._thread
        if thread is not None:
            thread.join(timeout=1.0)
            self._thread = None
