"""Runtime selection — one API, swappable backends (native / ROS 2)."""

from __future__ import annotations

from .base import Runtime, RuntimeUnavailable, TimerHandle
from .native import NativeRuntime
from .ros2 import Ros2Runtime


def available_runtimes() -> list[str]:
    """Runtimes usable in this environment."""
    out = ["native"]
    if Ros2Runtime.is_available():
        out.append("ros2")
    return out


def get_runtime(name: str = "auto") -> Runtime:
    """Construct a runtime by name.

    ``auto`` prefers ``ros2`` when rclpy is available (the full ROS 2 stack),
    and falls back to ``native`` otherwise — so the same code runs on both
    backends with zero changes.
    """
    if name in (None, "auto"):
        name = "ros2" if Ros2Runtime.is_available() else "native"
    if name == "native":
        return NativeRuntime()
    if name == "ros2":
        return Ros2Runtime()
    raise ValueError(f"unknown runtime '{name}' (choose: auto, native, ros2)")


__all__ = [
    "Runtime",
    "NativeRuntime",
    "Ros2Runtime",
    "TimerHandle",
    "RuntimeUnavailable",
    "get_runtime",
    "available_runtimes",
]
