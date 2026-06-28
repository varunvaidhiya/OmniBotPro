"""The native (no-ROS) runtime — a lightweight threaded scheduler.

Pure standard library: a background thread fires registered timers. This is the
default backend and runs anywhere Python runs.
"""

from __future__ import annotations

import threading
import time
from dataclasses import dataclass
from typing import Callable

from .base import Runtime, TimerHandle


@dataclass
class _Timer:
    period: float
    cb: Callable[[], None]
    next_fire: float
    handle: TimerHandle


class NativeRuntime(Runtime):
    name = "native"

    def __init__(self, tick_s: float = 0.005) -> None:
        super().__init__()
        self._timers: list[_Timer] = []
        self._tick = tick_s
        self._thread: threading.Thread | None = None
        self._stop = threading.Event()

    def now(self) -> float:
        return time.monotonic()

    def create_timer(
        self, period_s: float, callback: Callable[[], None]
    ) -> TimerHandle:
        handle = TimerHandle()
        with self._lock:
            self._timers.append(
                _Timer(period_s, callback, self.now() + period_s, handle)
            )
        return handle

    def start(self) -> None:
        if self._thread is not None:
            return
        self._stop.clear()
        self._thread = threading.Thread(
            target=self._spin, name="ohho-native-runtime", daemon=True
        )
        self._thread.start()

    def stop(self) -> None:
        self._stop.set()
        thread = self._thread
        if thread is not None:
            thread.join(timeout=1.0)
            self._thread = None

    def _spin(self) -> None:
        while not self._stop.is_set():
            now = self.now()
            with self._lock:
                timers = list(self._timers)
            for tm in timers:
                if tm.handle.cancelled:
                    with self._lock:
                        if tm in self._timers:
                            self._timers.remove(tm)
                    continue
                if now >= tm.next_fire:
                    tm.next_fire = now + tm.period
                    try:
                        tm.cb()
                    except Exception:
                        pass
            time.sleep(self._tick)
