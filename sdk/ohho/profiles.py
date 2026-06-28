"""Hardware deployment profiles — named compute topologies.

A profile answers "which role runs where, on what device" without any component
knowing about machines. Roles: ``control``, ``perception``, ``inference``,
``training``, ``simulation``.

Use ``detect_profile()`` for a best guess on the current machine, or pick one
explicitly (``OHHO_HW_PROFILE`` env var wins). This is a lightweight, stdlib-only
port of ``learning_engine/hardware/profiles.py`` — it reuses the same profile
definitions but resolves devices via :func:`ohho.hardware.resolve_device` so the
base install stays dependency-free.
"""

from __future__ import annotations

import os
import platform
from dataclasses import dataclass, field
from typing import List, Optional

from .hardware import resolve_device


@dataclass
class NodeSpec:
    """One machine in the topology."""

    name: str
    roles: List[str]
    device: str = "auto"
    accelerator: str = ""
    notes: str = ""


@dataclass
class HardwareProfile:
    name: str
    description: str
    nodes: List[NodeSpec] = field(default_factory=list)

    def node_for(self, role: str) -> Optional[NodeSpec]:
        for node in self.nodes:
            if role in node.roles:
                return node
        return None

    def device_for(self, role: str) -> str:
        """Resolved device string for a role on the current machine."""
        node = self.node_for(role)
        return resolve_device(node.device if node else "auto")


BUILTIN_PROFILES: dict[str, HardwareProfile] = {
    "pi_workstation": HardwareProfile(
        name="pi_workstation",
        description=(
            "Raspberry Pi 5 (control+perception) + NVIDIA GPU workstation "
            "(inference+training). deploy.py --mode multi."
        ),
        nodes=[
            NodeSpec("pi5", ["control", "perception"], device="cpu", accelerator="cpu"),
            NodeSpec(
                "workstation",
                ["inference", "training", "simulation"],
                device="cuda",
                accelerator="nvidia_gpu",
            ),
        ],
    ),
    "jetson_single": HardwareProfile(
        name="jetson_single",
        description=(
            "Everything on one Jetson (Orin NX / AGX): control, perception "
            "and on-board VLA/RL inference."
        ),
        nodes=[
            NodeSpec(
                "jetson",
                ["control", "perception", "inference", "training"],
                device="cuda",
                accelerator="jetson",
                notes="prefer TensorRT EPs for ONNX policies",
            ),
        ],
    ),
    "workstation_single": HardwareProfile(
        name="workstation_single",
        description=(
            "Single GPU workstation runs everything (sim-only development, "
            "RTX-class GPU). deploy.py --mode single."
        ),
        nodes=[
            NodeSpec(
                "workstation",
                ["control", "perception", "inference", "training", "simulation"],
                device="cuda",
                accelerator="nvidia_gpu",
            ),
        ],
    ),
    "mac_dev": HardwareProfile(
        name="mac_dev",
        description=(
            "Apple M-series development box: MPS inference/training, CoreML "
            "for ONNX policies."
        ),
        nodes=[
            NodeSpec(
                "mac",
                ["inference", "training"],
                device="mps",
                accelerator="apple_silicon",
                notes="Isaac Lab unavailable; use MuJoCo/ManiSkill envs",
            ),
        ],
    ),
    "edge_cpu": HardwareProfile(
        name="edge_cpu",
        description=(
            "CPU-only edge device (Pi, mini PC, laptop). Control + perception "
            "locally; inference and training off-board."
        ),
        nodes=[
            NodeSpec(
                "edge",
                ["control", "perception"],
                device="cpu",
                accelerator="cpu",
            ),
        ],
    ),
}

_ROLES = ("control", "perception", "inference", "training", "simulation")


def get_profile(name: str) -> HardwareProfile:
    """Look up a built-in profile by name."""
    try:
        return BUILTIN_PROFILES[name]
    except KeyError:
        raise KeyError(
            f"Unknown hardware profile '{name}'. Available: {sorted(BUILTIN_PROFILES)}"
        ) from None


def list_profiles() -> list[str]:
    """Names of all built-in profiles."""
    return sorted(BUILTIN_PROFILES)


def _is_apple_silicon() -> bool:
    return platform.system() == "Darwin" and platform.machine() == "arm64"


def _is_jetson() -> bool:
    try:
        from pathlib import Path

        return Path("/etc/nv_tegra_release").exists()
    except OSError:
        return False


def _has_nvidia_gpu() -> bool:
    return resolve_device("auto") == "cuda" and not _is_jetson()


def _is_raspberry_pi() -> bool:
    try:
        from pathlib import Path

        return (
            "raspberry pi"
            in Path("/proc/device-tree/model").read_text().strip("\x00 \n").lower()
        )
    except OSError:
        return False


def detect_profile() -> HardwareProfile:
    """Best-guess profile for the current machine.

    ``OHHO_HW_PROFILE`` env var overrides; falls back on platform detection.
    """
    forced = os.environ.get("OHHO_HW_PROFILE", "") or os.environ.get(
        "OMNIBOT_HW_PROFILE", ""
    )
    if forced:
        return get_profile(forced)
    if _is_jetson():
        return BUILTIN_PROFILES["jetson_single"]
    if _is_apple_silicon():
        return BUILTIN_PROFILES["mac_dev"]
    if _has_nvidia_gpu():
        return BUILTIN_PROFILES["workstation_single"]
    if _is_raspberry_pi():
        return BUILTIN_PROFILES["pi_workstation"]
    return BUILTIN_PROFILES["edge_cpu"]


def describe() -> dict:
    """Summary of the current machine's compute (for logs, W&B, CLI output)."""
    prof = detect_profile()
    return {
        "platform": f"{platform.system()} {platform.machine()}",
        "profile": prof.name,
        "device": resolve_device("auto"),
        "nodes": [
            {"name": n.name, "roles": n.roles, "device": n.device} for n in prof.nodes
        ],
    }


__all__ = [
    "HardwareProfile",
    "NodeSpec",
    "BUILTIN_PROFILES",
    "get_profile",
    "list_profiles",
    "detect_profile",
    "describe",
]
