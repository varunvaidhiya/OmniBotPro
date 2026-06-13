"""System probe — a full snapshot of the machine a benchmark ran on.

Attached to every BenchmarkResult and logged as the W&B run config, so
results from the Pi, Jetson, the workstation and a Mac are directly
comparable in one dashboard.
"""

from __future__ import annotations

import os
import platform
import socket
import time
from dataclasses import asdict, dataclass, field
from typing import Dict, Optional

from ..hardware import device as hw


@dataclass
class SystemInfo:
    hostname: str
    os: str
    machine: str  # arm64 / aarch64 / x86_64
    cpu: str
    cpu_cores: int
    ram_gb: float
    python: str
    accelerator_type: str
    accelerator_name: str
    accelerator_memory_gb: float
    torch_version: str = ""
    torch_device: str = "cpu"
    onnxruntime_version: str = ""
    onnx_providers: str = ""
    jetson_model: str = ""
    hw_profile: str = ""
    probed_at: float = field(default_factory=time.time)

    def to_dict(self) -> Dict[str, object]:
        return asdict(self)

    def label(self) -> str:
        """Short id for filenames and Prometheus labels,
        e.g. ``m4-max-mps`` or ``orin-nx-cuda``."""
        name = (self.accelerator_name or self.accelerator_type).lower()
        slug = "".join(c if c.isalnum() else "-" for c in name).strip("-")
        while "--" in slug:
            slug = slug.replace("--", "-")
        return f"{slug[:24]}-{self.torch_device}"


def _cpu_model() -> str:
    if platform.system() == "Darwin":
        return hw.apple_chip() or platform.processor()
    try:
        for line in open("/proc/cpuinfo"):
            if line.lower().startswith(("model name", "hardware", "model")):
                return line.split(":", 1)[1].strip()
    except OSError:
        pass
    return platform.processor() or platform.machine()


def probe(profile_name: Optional[str] = None) -> SystemInfo:
    accel = hw.primary_accelerator()
    torch_version = ""
    try:
        import torch

        torch_version = torch.__version__
    except ImportError:
        pass
    ort_version = ""
    try:
        import onnxruntime

        ort_version = onnxruntime.__version__
    except ImportError:
        pass

    if profile_name is None:
        from ..hardware.profiles import detect_profile

        profile_name = detect_profile().name

    return SystemInfo(
        hostname=socket.gethostname(),
        os=f"{platform.system()} {platform.release()}",
        machine=platform.machine(),
        cpu=_cpu_model(),
        cpu_cores=os.cpu_count() or 1,
        ram_gb=hw._total_ram_gb(),
        python=platform.python_version(),
        accelerator_type=accel.type.value,
        accelerator_name=accel.name,
        accelerator_memory_gb=accel.memory_gb,
        torch_version=torch_version,
        torch_device=hw.resolve_device("auto"),
        onnxruntime_version=ort_version,
        onnx_providers=",".join(hw.onnx_providers()),
        jetson_model=hw.jetson_model(),
        hw_profile=profile_name,
    )
