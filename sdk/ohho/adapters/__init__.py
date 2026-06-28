"""Adapter resolution.

Adapters connect the unified Transport interface to a robot's native protocol.
This build bundles: ``sim`` (always), ``serial`` (Yahboom / OmniBot base, needs
the ``[serial]`` extra at connect time), ``feetech`` (SO-101 arm, needs ``[arm]``),
``composite`` (base+arm merge for mobile-manipulators), and ``dds`` (Unitree Go2,
needs ``[unitree]``).

Policy: **auto (no explicit transport) runs in simulation** so zero-config use and
tests never touch hardware; pass an explicit transport URI (``serial://…``,
``dds://…``, ``feetech://…``) to engage a real robot. For a mobile-manipulator
that has ``manipulation`` capability, ``serial://<base_port>,<arm_port>``
auto-composes a composite (Yahboom base + Feetech arm).
"""

from __future__ import annotations

import importlib.util
from typing import Optional

from .. import capabilities as caps
from ..registry import RobotSpec
from ..transport import Transport
from .composite import CompositeTransport
from .errors import AdapterUnavailable
from .feetech import DEFAULT_PORT as DEFAULT_ARM_PORT
from .feetech import FeetechTransport
from .ros2 import Ros2Transport
from .sim import SimTransport
from .unitree import UnitreeDdsTransport
from .yahboom import YahboomTransport

# transport scheme -> hardware adapter class (sim/composite handled separately)
_HARDWARE = {
    "serial": YahboomTransport,
    "yahboom-serial": YahboomTransport,
    "feetech": FeetechTransport,
    "dds": UnitreeDdsTransport,
    "unitree-dds": UnitreeDdsTransport,
    "ros2": Ros2Transport,
}


def available_adapters() -> list[str]:
    """Schemes usable in this environment (their runtime deps are importable)."""
    out = ["sim"]
    if importlib.util.find_spec("serial") is not None:
        out.append("serial")
    if importlib.util.find_spec("lerobot") is not None:
        out.append("feetech")
    if importlib.util.find_spec("cyclonedds") is not None:
        out.append("dds")
    if importlib.util.find_spec("rclpy") is not None:
        out.append("ros2")
    return out


def _split_ports(address: str) -> tuple[str, str]:
    """Split ``base_port,arm_port`` into parts; arm defaults to the Feetech port."""
    if "," in address:
        base, arm = address.split(",", 1)
        return base.strip(), arm.strip()
    return address.strip(), DEFAULT_ARM_PORT


def resolve_transport(
    uri: Optional[str] = None,
    spec: Optional[RobotSpec] = None,
    runtime=None,
) -> Transport:
    """Pick a transport for a robot.

    - ``uri=None`` (auto) → the simulator (explicit URIs engage hardware).
    - ``uri="sim://"`` → the simulator.
    - ``uri="serial://<port>"`` → Yahboom serial adapter. If the robot has
      ``manipulation`` capability, ``<port>`` may be ``base_port,arm_port`` and
      a composite (Yahboom base + Feetech arm) is returned.
    - ``uri="feetech://<port>"`` → Feetech arm adapter (arm only).
    - ``uri="dds://<iface>"`` → Unitree DDS adapter.
    - ``uri="ros2://<namespace>"`` → ROS 2 transport (bridges to /cmd_vel, /odom,
      /arm/joint_states; requires ``runtime="ros2"``).
    - any other scheme → ``AdapterUnavailable``.
    """
    if uri is None:
        return SimTransport(spec)
    scheme, _sep, address = uri.partition("://")
    scheme = scheme.lower()
    if scheme in ("sim", "simulated"):
        return SimTransport(spec)

    if (
        scheme in ("serial", "yahboom-serial")
        and spec is not None
        and spec.has(caps.MANIPULATION)
    ):
        base_port, arm_port = _split_ports(address)
        base = YahboomTransport(spec, base_port)
        arm = FeetechTransport(spec, arm_port)
        return CompositeTransport(base, arm)

    if scheme == "ros2":
        return Ros2Transport(spec, address, runtime=runtime)

    cls = _HARDWARE.get(scheme)
    if cls is None:
        raise AdapterUnavailable(
            f"Unknown transport scheme '{scheme}'. Known: sim, serial, feetech, "
            f"dds, ros2 — or omit the transport to run in simulation."
        )
    return cls(spec, address)


__all__ = [
    "SimTransport",
    "YahboomTransport",
    "FeetechTransport",
    "UnitreeDdsTransport",
    "Ros2Transport",
    "CompositeTransport",
    "AdapterUnavailable",
    "resolve_transport",
    "available_adapters",
]
