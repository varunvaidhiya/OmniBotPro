"""Hardware deployment profiles — named compute topologies for OmniBot.

A profile answers "which role runs where, on what device" without any
component knowing about machines. Roles:

    control     — drivers, muxes, mission planner (ROS, low compute)
    perception  — cameras, BEV, depth→scan, object perception
    inference   — VLA / RL policy serving
    training    — post-training loop, trainers, judges
    simulation  — Isaac Sim / Gazebo

Use ``detect_profile()`` for a best guess on the current machine, or pick
one explicitly (``OMNIBOT_HW_PROFILE`` env var wins everywhere). The
learning engine consults profiles through ``device_for(role)``; the
existing ``deploy.py`` single/multi modes map onto ``workstation_single``
and ``pi_workstation`` respectively.
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from typing import Dict, List, Optional

from .device import (
    AcceleratorType,
    detect_accelerators,
    is_apple_silicon,
    is_jetson,
    is_raspberry_pi,
    primary_accelerator,
    resolve_device,
)


@dataclass
class NodeSpec:
    """One machine in the topology."""

    name: str
    roles: List[str]
    device: str = "auto"  # torch-style; "auto" resolves on that machine
    accelerator: str = ""  # informational: expected accelerator type
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
        """Resolved device string for a role *on the current machine*.
        (Cross-machine device strings are meaningless — each machine
        resolves its own "auto".)"""
        node = self.node_for(role)
        return resolve_device(node.device if node else "auto")


BUILTIN_PROFILES: Dict[str, HardwareProfile] = {
    # Current production topology
    "pi_workstation": HardwareProfile(
        name="pi_workstation",
        description="Raspberry Pi 5 (control+perception) + NVIDIA GPU "
        "workstation (inference+training). deploy.py --mode multi.",
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
    "pi_accelerator_workstation": HardwareProfile(
        name="pi_accelerator_workstation",
        description="Pi 5 + Hailo/Coral accelerator for on-robot perception "
        "inference; GPU workstation for VLA + training.",
        nodes=[
            NodeSpec(
                "pi5",
                ["control", "perception"],
                device="cpu",
                accelerator="hailo",
                notes="perception models compiled to HEF/Edge-TPU run "
                "on the accelerator; ROS nodes stay on CPU",
            ),
            NodeSpec(
                "workstation",
                ["inference", "training", "simulation"],
                device="cuda",
                accelerator="nvidia_gpu",
            ),
        ],
    ),
    "pi_deepx_workstation": HardwareProfile(
        name="pi_deepx_workstation",
        description="Pi 5 + DeepX NPU (~25 TOPS) running on-robot perception, "
        "VLA and the agent harness's local reasoning fallback; GPU "
        "workstation for cloud-class training + simulation.",
        nodes=[
            NodeSpec(
                "pi5",
                ["control", "perception", "inference"],
                device="cpu",
                accelerator="deepx",
                notes="perception/VLA + small LLM compiled to .dxnn run on the "
                "DeepX NPU via DX-RT; ROS nodes and the harness stay on CPU",
            ),
            NodeSpec(
                "workstation",
                ["training", "simulation"],
                device="cuda",
                accelerator="nvidia_gpu",
            ),
        ],
    ),
    "jetson_single": HardwareProfile(
        name="jetson_single",
        description="Everything on one Jetson (Orin NX / AGX): control, "
        "perception and on-board VLA/RL inference. Training "
        "stays off-robot or runs in low-priority windows.",
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
        description="Single GPU workstation runs everything (sim-only "
        "development, RTX-class GPU). deploy.py --mode single.",
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
        description="Apple M-series development box: MPS inference/training, "
        "CoreML for ONNX policies. No CUDA sim backends.",
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
}


def get_profile(name: str) -> HardwareProfile:
    try:
        return BUILTIN_PROFILES[name]
    except KeyError:
        raise KeyError(
            f"Unknown hardware profile '{name}'. Available: {sorted(BUILTIN_PROFILES)}"
        ) from None


def detect_profile() -> HardwareProfile:
    """Best-guess profile for the current machine. ``OMNIBOT_HW_PROFILE``
    overrides; falls back on accelerator detection."""
    forced = os.environ.get("OMNIBOT_HW_PROFILE", "")
    if forced:
        return get_profile(forced)
    if is_jetson():
        return BUILTIN_PROFILES["jetson_single"]
    if is_apple_silicon():
        return BUILTIN_PROFILES["mac_dev"]
    if is_raspberry_pi():
        if any(a.type is AcceleratorType.DEEPX for a in detect_accelerators()):
            return BUILTIN_PROFILES["pi_deepx_workstation"]
        return BUILTIN_PROFILES["pi_workstation"]
    if primary_accelerator().type is AcceleratorType.NVIDIA_GPU:
        return BUILTIN_PROFILES["workstation_single"]
    return BUILTIN_PROFILES["pi_workstation"]
