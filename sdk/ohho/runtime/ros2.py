"""The ROS 2 runtime backend (stub).

Detects rclpy and gives a clear, actionable error until the full backend lands
(see docs/ohho-os/runtimes.md). The point of this stub is that selecting the
runtime is the only thing that changes between no-ROS and ROS 2 — the rest of the
stack already targets the Runtime port.
"""

from __future__ import annotations

import importlib.util

from .base import Runtime, RuntimeUnavailable, TimerHandle


def _rclpy_present() -> bool:
    return importlib.util.find_spec("rclpy") is not None


class Ros2Runtime(Runtime):
    name = "ros2"

    def __init__(self) -> None:
        if not _rclpy_present():
            raise RuntimeUnavailable(
                "The ROS 2 runtime requires rclpy. Source your ROS 2 (Jazzy) setup, "
                "then: pip install 'ohho-os[ros2]'. Until then use runtime='native'."
            )
        super().__init__()
        # Full rclpy-backed implementation arrives in a later milestone.
        raise RuntimeUnavailable(
            "The ROS 2 backend is not implemented in this build yet. "
            "Use runtime='native' (it shares the same API)."
        )

    @classmethod
    def is_available(cls) -> bool:
        """True if rclpy is importable in this environment."""
        return _rclpy_present()

    def now(self) -> float:  # pragma: no cover - unreachable until implemented
        raise NotImplementedError

    def create_timer(self, period_s, callback) -> TimerHandle:  # pragma: no cover
        raise NotImplementedError

    def start(self) -> None:  # pragma: no cover
        raise NotImplementedError

    def stop(self) -> None:  # pragma: no cover
        raise NotImplementedError
