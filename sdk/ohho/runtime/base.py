"""The Runtime port.

Engines (the agent, training, perception) depend ONLY on this interface — never
on a specific runtime — which is what lets you switch between the native and ROS 2
backends without touching application code. It provides timers, a small pub/sub
bus, and a parameter store.
"""

from __future__ import annotations

import abc
import threading
from typing import Any, Callable


class TimerHandle:
    """Opaque handle for a registered timer; call ``cancel()`` to stop it."""

    def __init__(self) -> None:
        self._cancelled = False

    def cancel(self) -> None:
        self._cancelled = True

    @property
    def cancelled(self) -> bool:
        return self._cancelled


class RuntimeUnavailable(RuntimeError):
    """Raised when a requested runtime backend can't be used here."""


class Runtime(abc.ABC):
    name = "base"

    def __init__(self) -> None:
        self._subs: dict[str, list[Callable[[Any], None]]] = {}
        self._params: dict[str, Any] = {}
        self._lock = threading.RLock()

    # ── pub/sub ──────────────────────────────────────────────────────────────
    def subscribe(self, topic: str, cb: Callable[[Any], None]) -> Callable[[], None]:
        with self._lock:
            self._subs.setdefault(topic, []).append(cb)

        def off() -> None:
            with self._lock:
                try:
                    self._subs.get(topic, []).remove(cb)
                except ValueError:
                    pass

        return off

    def publish(self, topic: str, msg: Any) -> None:
        with self._lock:
            subs = list(self._subs.get(topic, []))
        for cb in subs:
            try:
                cb(msg)
            except Exception:
                pass

    # ── params ───────────────────────────────────────────────────────────────
    def get_param(self, key: str, default: Any = None) -> Any:
        with self._lock:
            return self._params.get(key, default)

    def set_param(self, key: str, value: Any) -> None:
        with self._lock:
            self._params[key] = value

    # ── lifecycle / timers (backend-specific) ────────────────────────────────
    @abc.abstractmethod
    def now(self) -> float: ...

    @abc.abstractmethod
    def create_timer(self, period_s: float, callback: Callable[[], None]) -> TimerHandle: ...

    @abc.abstractmethod
    def start(self) -> None: ...

    @abc.abstractmethod
    def stop(self) -> None: ...
