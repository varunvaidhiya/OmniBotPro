"""The transport interface — one shape every robot link implements.

Ported from the web Connect layer (website/lib/connect/types.ts). A transport
opens a link to a robot and exposes a uniform surface: drive, command joints,
e-stop, and read/stream telemetry. Adapters (sim, Unitree DDS, DJI MAVLink, …)
each implement this so every higher layer is protocol-agnostic.
"""

from __future__ import annotations

import abc
from typing import Callable

from .schema import Telemetry, TransportStatus, Velocity

Unsubscribe = Callable[[], None]


class Transport(abc.ABC):
    """Abstract robot link."""

    protocol: str = "base"

    @abc.abstractmethod
    def connect(self) -> TransportStatus: ...

    @abc.abstractmethod
    def disconnect(self) -> None: ...

    @abc.abstractmethod
    def status(self) -> TransportStatus: ...

    @abc.abstractmethod
    def read(self) -> Telemetry:
        """Return the current telemetry snapshot synchronously."""

    @abc.abstractmethod
    def send_velocity(self, vel: Velocity) -> None: ...

    @abc.abstractmethod
    def send_joint_command(self, name: str, position: float) -> None: ...

    @abc.abstractmethod
    def emergency_stop(self) -> None: ...

    @abc.abstractmethod
    def release_stop(self) -> None: ...

    @abc.abstractmethod
    def on_telemetry(self, cb: Callable[[Telemetry], None]) -> Unsubscribe: ...

    @abc.abstractmethod
    def on_status(self, cb: Callable[[TransportStatus], None]) -> Unsubscribe: ...


class BaseTransport(Transport):
    """Transport with callback bookkeeping done for you.

    Concrete adapters subclass this and implement the connect/read/command
    methods, calling ``_emit_telemetry`` / ``_emit_status`` to notify subscribers.
    """

    def __init__(self) -> None:
        self._tele_subs: list[Callable[[Telemetry], None]] = []
        self._status_subs: list[Callable[[TransportStatus], None]] = []

    def on_telemetry(self, cb: Callable[[Telemetry], None]) -> Unsubscribe:
        self._tele_subs.append(cb)

        def off() -> None:
            try:
                self._tele_subs.remove(cb)
            except ValueError:
                pass

        return off

    def on_status(self, cb: Callable[[TransportStatus], None]) -> Unsubscribe:
        self._status_subs.append(cb)

        def off() -> None:
            try:
                self._status_subs.remove(cb)
            except ValueError:
                pass

        return off

    def _emit_telemetry(self, t: Telemetry) -> None:
        for cb in list(self._tele_subs):
            try:
                cb(t)
            except Exception:
                pass

    def _emit_status(self, s: TransportStatus) -> None:
        for cb in list(self._status_subs):
            try:
                cb(s)
            except Exception:
                pass
