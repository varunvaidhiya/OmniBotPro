"""Adapter resolution.

Adapters connect the unified Transport interface to a robot's native protocol.
This build bundles: ``sim`` (always), ``serial`` (Yahboom / OmniBot base, needs
the `[serial]` extra at connect time), and ``dds`` (Unitree Go2, needs `[unitree]`).

Policy: **auto (no explicit transport) runs in simulation** so zero-config use and
tests never touch hardware; pass an explicit transport URI (``serial://…``,
``dds://…``) to engage a real robot.
"""

from __future__ import annotations

import importlib.util
from typing import Optional

from ..registry import RobotSpec
from ..transport import Transport
from .errors import AdapterUnavailable
from .sim import SimTransport
from .unitree import UnitreeDdsTransport
from .yahboom import YahboomTransport

# transport scheme -> hardware adapter class (sim handled separately)
_HARDWARE = {
    "serial": YahboomTransport,
    "yahboom-serial": YahboomTransport,
    "dds": UnitreeDdsTransport,
    "unitree-dds": UnitreeDdsTransport,
}


def available_adapters() -> list[str]:
    """Schemes usable in this environment (their runtime deps are importable)."""
    out = ["sim"]
    if importlib.util.find_spec("serial") is not None:
        out.append("serial")
    if importlib.util.find_spec("cyclonedds") is not None:
        out.append("dds")
    return out


def resolve_transport(
    uri: Optional[str] = None,
    spec: Optional[RobotSpec] = None,
    runtime=None,
) -> Transport:
    """Pick a transport for a robot.

    - ``uri=None`` (auto) → the simulator (explicit URIs engage hardware).
    - ``uri="sim://"`` → the simulator.
    - ``uri="serial://<port>"`` → Yahboom serial adapter.
    - ``uri="dds://<iface>"`` → Unitree DDS adapter.
    - any other scheme → ``AdapterUnavailable``.
    """
    if uri is None:
        return SimTransport(spec)
    scheme, _sep, address = uri.partition("://")
    scheme = scheme.lower()
    if scheme in ("sim", "simulated"):
        return SimTransport(spec)
    cls = _HARDWARE.get(scheme)
    if cls is None:
        raise AdapterUnavailable(
            f"Unknown transport scheme '{scheme}'. Known: sim, serial, dds — "
            f"or omit the transport to run in simulation."
        )
    return cls(spec, address)


__all__ = [
    "SimTransport",
    "YahboomTransport",
    "UnitreeDdsTransport",
    "AdapterUnavailable",
    "resolve_transport",
    "available_adapters",
]
