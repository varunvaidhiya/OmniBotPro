"""Adapter resolution.

Adapters connect the unified Transport interface to a robot's native protocol.
This build bundles the simulator; hardware adapters (Unitree DDS, DJI MAVLink,
Modbus, …) land here behind their extras and register their schemes.
"""

from __future__ import annotations

from typing import Optional

from ..registry import RobotSpec
from ..transport import Transport
from .sim import SimTransport


class AdapterUnavailable(RuntimeError):
    """Raised when an explicitly requested transport scheme isn't installed."""


# Schemes usable in this build.
AVAILABLE = ("sim",)


def available_adapters() -> list[str]:
    return list(AVAILABLE)


def resolve_transport(
    uri: Optional[str] = None,
    spec: Optional[RobotSpec] = None,
    runtime=None,
) -> Transport:
    """Pick a transport for a robot.

    - ``uri="sim://"`` → the simulator.
    - ``uri`` with another scheme → ``AdapterUnavailable`` (honest, explicit error).
    - ``uri=None`` (auto) → the robot's real adapter if bundled, else simulation,
      so any robot can be explored before its hardware adapter is installed.
    """
    explicit = uri is not None
    if uri:
        scheme = uri.split("://", 1)[0].lower()
    elif spec is not None:
        scheme = spec.adapter.lower()
    else:
        scheme = "sim"

    if scheme in ("sim", "simulated"):
        return SimTransport(spec)

    if explicit:
        raise AdapterUnavailable(
            f"Transport '{scheme}' isn't available in this build. "
            f"Install the matching extra (e.g. pip install 'ohho-os[unitree]') "
            f"or use transport='sim://'."
        )

    # auto: no bundled adapter for this robot yet — fall back to simulation.
    return SimTransport(spec)


__all__ = ["SimTransport", "AdapterUnavailable", "resolve_transport", "available_adapters"]
