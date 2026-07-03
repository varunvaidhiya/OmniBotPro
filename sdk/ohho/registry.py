"""Robot registry and the ``RobotSpec`` capability manifest.

Built-in robots are defined here as data; users add their own with
``register(...)`` or by loading a manifest file (``load_manifest``). A spec says
what a robot *is* — its category, capabilities, degrees of freedom, limits and
which adapter speaks its native protocol — not how it does any of it.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Union

from . import capabilities as caps


class UnknownRobot(KeyError):
    """Raised when a robot id is not in the registry."""


@dataclass(frozen=True)
class RobotSpec:
    id: str
    name: str
    category: str
    capabilities: tuple[str, ...] = ()
    adapter: str = "sim"
    dof_base: int = 3
    dof_arm: int = 0
    max_lin: float = 0.5
    max_ang: float = 1.0
    joint_names: tuple[str, ...] = ()

    def has(self, capability: str) -> bool:
        return capability in self.capabilities


# OmniBot's SO-101 arm joints (kept in sync with the ROS arm driver defaults).
_OMNIBOT_ARM = (
    "arm_shoulder_pan",
    "arm_shoulder_lift",
    "arm_elbow_flex",
    "arm_wrist_flex",
    "arm_wrist_roll",
    "arm_gripper",
)

_BUILTINS: dict[str, RobotSpec] = {
    "omnibot": RobotSpec(
        id="omnibot",
        name="OmniBot",
        category="mobile-manipulator",
        capabilities=(
            caps.BASE_DRIVE,
            caps.BASE_HOLONOMIC,
            caps.PERCEPTION_RGB,
            caps.PERCEPTION_DEPTH,
            caps.PERCEPTION_SCAN,
            caps.MANIPULATION,
        ),
        adapter="yahboom-serial",
        dof_base=3,
        dof_arm=6,
        max_lin=0.2,
        max_ang=1.0,
        joint_names=_OMNIBOT_ARM,
    ),
    "unitree-go2": RobotSpec(
        id="unitree-go2",
        name="Unitree Go2",
        category="legged",
        capabilities=(
            caps.BASE_DRIVE,
            caps.BASE_HOLONOMIC,
            caps.PERCEPTION_RGB,
            caps.PERCEPTION_DEPTH,
            caps.PERCEPTION_SCAN,
            caps.LEGGED_POSE,
        ),
        adapter="unitree-dds",
        dof_base=3,
        dof_arm=0,
        max_lin=1.5,
        max_ang=2.0,
    ),
    "sim": RobotSpec(
        id="sim",
        name="Generic Sim Bot",
        category="wheeled",
        capabilities=(
            caps.BASE_DRIVE,
            caps.BASE_HOLONOMIC,
            caps.PERCEPTION_RGB,
            caps.PERCEPTION_SCAN,
        ),
        adapter="sim",
        dof_base=3,
        dof_arm=0,
        max_lin=1.0,
        max_ang=2.0,
    ),
}

_REGISTRY: dict[str, RobotSpec] = dict(_BUILTINS)


def register(spec: RobotSpec) -> RobotSpec:
    """Add (or replace) a robot in the registry."""
    _REGISTRY[spec.id] = spec
    return spec


def get_spec(robot_id: str) -> RobotSpec:
    try:
        return _REGISTRY[robot_id]
    except KeyError:
        known = ", ".join(sorted(_REGISTRY))
        raise UnknownRobot(
            f"unknown robot '{robot_id}'. Known robots: {known}"
        ) from None


def list_specs() -> list[RobotSpec]:
    return [_REGISTRY[k] for k in sorted(_REGISTRY)]


def spec_from_dict(d: dict) -> RobotSpec:
    """Build a RobotSpec from a manifest dict (supports nested dof/limits)."""
    dof = d.get("dof", {})
    limits = d.get("limits", {})
    return RobotSpec(
        id=d["id"],
        name=d.get("name", d["id"]),
        category=d.get("category", "unknown"),
        capabilities=tuple(d.get("capabilities", ())),
        adapter=d.get("adapter", "sim"),
        dof_base=int(dof.get("base", d.get("dof_base", 3))),
        dof_arm=int(dof.get("arm", d.get("dof_arm", 0))),
        max_lin=float(limits.get("max_lin", d.get("max_lin", 0.5))),
        max_ang=float(limits.get("max_ang", d.get("max_ang", 1.0))),
        joint_names=tuple(d.get("joint_names", ())),
    )


def load_manifest(path: Union[str, Path], *, register_it: bool = True) -> RobotSpec:
    """Load a robot manifest from JSON (stdlib) or YAML (needs the ``yaml`` extra)."""
    p = Path(path)
    text = p.read_text(encoding="utf-8")
    if p.suffix.lower() in (".yaml", ".yml"):
        try:
            import yaml  # type: ignore
        except Exception as e:  # pragma: no cover - exercised only without pyyaml
            raise RuntimeError(
                "YAML manifests require pyyaml: pip install 'ohho-os[yaml]'"
            ) from e
        data = yaml.safe_load(text)
    else:
        data = json.loads(text)
    spec = spec_from_dict(data)
    if register_it:
        register(spec)
    return spec
