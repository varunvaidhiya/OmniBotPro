"""The Robot Abstraction Layer — the public API.

``Robot`` is what your code, the agent, and the training pipelines talk to. It is
backend- and protocol-agnostic: the same calls work on any robot and either
runtime, with capabilities gating what actually happens (a robot that can't do
something turns the call into a safe no-op rather than crashing).
"""

from __future__ import annotations

from typing import Iterable, Optional

from . import capabilities as caps
from .adapters import resolve_transport
from .registry import RobotSpec, get_spec
from .runtime import Runtime, get_runtime
from .schema import Telemetry, TransportStatus, Velocity, clamp
from .transport import Transport


class Robot:
    def __init__(self, spec: RobotSpec, transport: Transport, runtime: Runtime) -> None:
        self.spec = spec
        self._t = transport
        self._rt = runtime
        self._latest: Optional[Telemetry] = None
        self._unsub = transport.on_telemetry(self._on_tele)

    @classmethod
    def connect(
        cls,
        robot_id: str,
        transport: Optional[str] = None,
        runtime: str = "auto",
    ) -> "Robot":
        """Connect to a robot and start its runtime.

        ``transport`` is a URI like ``"sim://"`` or ``"dds://192.168.1.10"``; when
        omitted, the robot's declared adapter is used (falling back to simulation
        if it isn't bundled). ``runtime`` is ``"auto"`` | ``"native"`` | ``"ros2"``.
        """
        spec = get_spec(robot_id)
        rt = get_runtime(runtime)
        tp = resolve_transport(transport, spec, rt)
        tp.connect()
        bot = cls(spec, tp, rt)
        # live backends that own a loop start it here
        start_sim = getattr(tp, "start_sim", None)
        if callable(start_sim):
            start_sim()
        rt.start()
        return bot

    # ── introspection ─────────────────────────────────────────────────────────
    @property
    def simulated(self) -> bool:
        return self._t.protocol == "simulated"

    @property
    def runtime(self) -> Runtime:
        return self._rt

    @property
    def transport(self) -> Transport:
        return self._t

    def has(self, capability: str) -> bool:
        return self.spec.has(capability)

    # ── control ───────────────────────────────────────────────────────────────
    def drive(self, vx: float = 0.0, vy: float = 0.0, w: float = 0.0) -> "Robot":
        """Drive the base. Clamped to the robot's limits; ``vy`` ignored on
        non-holonomic bases; a no-op if the robot can't drive."""
        if not self.has(caps.BASE_DRIVE):
            return self
        if not self.has(caps.BASE_HOLONOMIC):
            vy = 0.0
        vx = clamp(vx, -self.spec.max_lin, self.spec.max_lin)
        vy = clamp(vy, -self.spec.max_lin, self.spec.max_lin)
        w = clamp(w, -self.spec.max_ang, self.spec.max_ang)
        self._t.send_velocity(Velocity(vx, vy, w))
        return self

    def stop(self) -> "Robot":
        self._t.send_velocity(Velocity())
        return self

    def move_joints(self, positions: Iterable[float]) -> "Robot":
        """Command arm joints (in spec order). No-op if the robot has no arm."""
        if not self.has(caps.MANIPULATION):
            return self
        for name, p in zip(self.spec.joint_names, positions):
            self._t.send_joint_command(name, float(p))
        return self

    def emergency_stop(self) -> "Robot":
        self._t.emergency_stop()
        return self

    def release_stop(self) -> "Robot":
        self._t.release_stop()
        return self

    # ── state ─────────────────────────────────────────────────────────────────
    def telemetry(self) -> Telemetry:
        if self._latest is not None:
            return self._latest
        return self._t.read()

    def status(self) -> TransportStatus:
        return self._t.status()

    def _on_tele(self, t: Telemetry) -> None:
        self._latest = t

    # ── teardown ──────────────────────────────────────────────────────────────
    def disconnect(self) -> None:
        try:
            self._unsub()
        except Exception:
            pass
        stop_sim = getattr(self._t, "stop_sim", None)
        if callable(stop_sim):
            stop_sim()
        try:
            self._t.disconnect()
        finally:
            self._rt.stop()

    def __enter__(self) -> "Robot":
        return self

    def __exit__(self, *exc) -> None:
        self.disconnect()

    def __repr__(self) -> str:
        kind = "sim" if self.simulated else self._t.protocol
        return f"<Robot {self.spec.id} via {kind} on {self._rt.name}>"


def connect(robot_id: str, transport: Optional[str] = None, runtime: str = "auto") -> Robot:
    """Module-level shortcut for :meth:`Robot.connect`."""
    return Robot.connect(robot_id, transport=transport, runtime=runtime)
