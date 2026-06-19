"""Hardware abstraction — one place that knows what compute is available.

Everything else in the codebase asks two questions and stays
hardware-agnostic:

- ``resolve_device("auto")``  → a torch-style device string
  (``cuda`` / ``mps`` / ``cpu``) for any component that takes ``device=``.
- ``onnx_providers()``        → the ordered ONNX Runtime execution-provider
  list for this machine (TensorRT on Jetson, CUDA on dGPU, CoreML on
  Apple Silicon, CPU everywhere).

Supported targets today: Raspberry Pi 5 (CPU), NVIDIA dGPU workstation,
NVIDIA Jetson (Orin/Xavier — CUDA + TensorRT on aarch64), Apple M-series
(MPS/CoreML), Hailo, Coral and DeepX (DX-RT, e.g. the 25-TOPS DX-M1 on the
Pi) USB/M.2 accelerators (detected when their runtimes are installed). New
targets = extend ``detect_accelerators`` and ``_ONNX_PROVIDER_PREFERENCE`` —
nothing else changes.
"""

from __future__ import annotations

import functools
import platform
import shutil
import subprocess
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Dict, List, Optional


class AcceleratorType(str, Enum):
    NVIDIA_GPU = "nvidia_gpu"  # discrete GPU workstation
    JETSON = "jetson"  # NVIDIA Jetson (integrated GPU, aarch64)
    APPLE_SILICON = "apple_silicon"  # M-series (MPS / CoreML / ANE)
    HAILO = "hailo"  # Hailo-8 accelerator (e.g. Pi AI Kit)
    CORAL = "coral"  # Google Coral Edge TPU
    DEEPX = "deepx"  # DeepX NPU (e.g. DX-M1, ~25 TOPS) via the DX-RT runtime
    CPU = "cpu"


@dataclass
class Accelerator:
    type: AcceleratorType
    name: str = ""
    memory_gb: float = 0.0  # dedicated (dGPU) or unified (Jetson/Apple)
    extra: Dict[str, str] = field(default_factory=dict)


# ---------------------------------------------------------------------------
# Platform probes (no heavy imports; each guarded and cached)
# ---------------------------------------------------------------------------


@functools.lru_cache(maxsize=None)
def is_jetson() -> bool:
    if Path("/etc/nv_tegra_release").exists():
        return True
    model = _device_tree_model()
    return "jetson" in model.lower() or "tegra" in model.lower()


@functools.lru_cache(maxsize=None)
def jetson_model() -> str:
    return _device_tree_model() if is_jetson() else ""


@functools.lru_cache(maxsize=None)
def is_apple_silicon() -> bool:
    return platform.system() == "Darwin" and platform.machine() == "arm64"


@functools.lru_cache(maxsize=None)
def apple_chip() -> str:
    if not is_apple_silicon():
        return ""
    try:
        return subprocess.run(
            ["sysctl", "-n", "machdep.cpu.brand_string"],
            capture_output=True,
            text=True,
            timeout=5,
        ).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        return "Apple Silicon"


@functools.lru_cache(maxsize=None)
def is_raspberry_pi() -> bool:
    return "raspberry pi" in _device_tree_model().lower()


def _device_tree_model() -> str:
    try:
        return Path("/proc/device-tree/model").read_text().strip("\x00 \n")
    except OSError:
        return ""


def _nvidia_smi_gpu() -> Optional[Accelerator]:
    if not shutil.which("nvidia-smi"):
        return None
    try:
        out = subprocess.run(
            [
                "nvidia-smi",
                "--query-gpu=name,memory.total",
                "--format=csv,noheader,nounits",
            ],
            capture_output=True,
            text=True,
            timeout=10,
        )
        if out.returncode != 0 or not out.stdout.strip():
            return None
        name, mem_mb = (s.strip() for s in out.stdout.splitlines()[0].split(",")[:2])
        return Accelerator(
            AcceleratorType.NVIDIA_GPU,
            name=name,
            memory_gb=round(float(mem_mb) / 1024, 1),
        )
    except (OSError, ValueError, subprocess.SubprocessError):
        return None


# ---------------------------------------------------------------------------
# Detection
# ---------------------------------------------------------------------------


@functools.lru_cache(maxsize=None)
def detect_accelerators() -> tuple:
    """All accelerators on this machine, best first. Cached per process."""
    found: List[Accelerator] = []
    if is_jetson():
        found.append(
            Accelerator(
                AcceleratorType.JETSON, name=jetson_model(), memory_gb=_total_ram_gb()
            )
        )
    else:
        gpu = _nvidia_smi_gpu()
        if gpu:
            found.append(gpu)
    if is_apple_silicon():
        found.append(
            Accelerator(
                AcceleratorType.APPLE_SILICON,
                name=apple_chip(),
                memory_gb=_total_ram_gb(),
            )
        )
    # USB/M.2 inference accelerators — present iff their runtime imports.
    for module, accel_type, name in (
        ("hailo_platform", AcceleratorType.HAILO, "Hailo-8"),
        ("pycoral", AcceleratorType.CORAL, "Coral Edge TPU"),
    ):
        try:
            __import__(module)
            found.append(Accelerator(accel_type, name=name))
        except ImportError:
            pass
    # DeepX NPU (DX-M1 / DX-H1) — present iff the DX-RT python runtime imports.
    # The binding name has varied across SDK releases, so probe a few.
    for module in ("dx_engine", "dxrt", "dx_rt"):
        try:
            __import__(module)
            found.append(Accelerator(AcceleratorType.DEEPX, name="DeepX NPU"))
            break
        except ImportError:
            continue
    if not found:
        found.append(
            Accelerator(
                AcceleratorType.CPU, name=platform.processor() or platform.machine()
            )
        )
    return tuple(found)


def primary_accelerator() -> Accelerator:
    return detect_accelerators()[0]


def _total_ram_gb() -> float:
    try:
        import os

        pages = os.sysconf("SC_PHYS_PAGES")
        page_size = os.sysconf("SC_PAGE_SIZE")
        return round(pages * page_size / 1024**3, 1)
    except (ValueError, OSError, AttributeError):
        return 0.0


# ---------------------------------------------------------------------------
# Device resolution (torch-style strings)
# ---------------------------------------------------------------------------


def resolve_device(device: str = "auto") -> str:
    """Map ``"auto"`` to the best torch device on this machine; pass
    explicit strings through untouched. Honest about availability: returns
    ``cpu`` when torch is missing even on accelerated hardware."""
    if device != "auto":
        return device
    try:
        import torch
    except ImportError:
        return "cpu"
    if torch.cuda.is_available():  # dGPU and Jetson both surface as cuda
        return "cuda"
    mps = getattr(torch.backends, "mps", None)
    if mps is not None and mps.is_available():
        return "mps"
    return "cpu"


# Ordered EP preference per accelerator; filtered by what this ORT build has.
#
# TensorRT vs CUDA is a deployment-vs-development call, not "newer is better":
# the TensorRT EP rebuilds an engine on first inference (slow, per-input-shape,
# needs an engine cache to amortize), so it only pays off when you bake once
# and run many times.
#   - Jetson is a deployment edge target → TensorRT first (worth the bake).
#   - A discrete-GPU workstation is a dev/training/sim box → CUDA first (fast
#     session init, no engine-build penalty polluting the dev loop or the
#     cold_latency_ms benchmark). Opt into TensorRT explicitly when
#     benchmarking a deployment build: OnnxPolicy(providers=[...]) or
#     onnx_providers(prefer_tensorrt=True).
_ONNX_PROVIDER_PREFERENCE: Dict[AcceleratorType, List[str]] = {
    AcceleratorType.JETSON: [
        "TensorrtExecutionProvider",
        "CUDAExecutionProvider",
        "CPUExecutionProvider",
    ],
    AcceleratorType.NVIDIA_GPU: ["CUDAExecutionProvider", "CPUExecutionProvider"],
    AcceleratorType.APPLE_SILICON: ["CoreMLExecutionProvider", "CPUExecutionProvider"],
    AcceleratorType.HAILO: ["CPUExecutionProvider"],  # Hailo runs via HEF, not ORT
    AcceleratorType.CORAL: ["CPUExecutionProvider"],  # Coral runs via tflite
    # DeepX runs compiled .dxnn graphs via the DX-RT runtime; the DeepX ORT EP
    # is promoted here when a build exposes it, else ORT falls back to CPU.
    AcceleratorType.DEEPX: ["DeepXExecutionProvider", "CPUExecutionProvider"],
    AcceleratorType.CPU: ["CPUExecutionProvider"],
}


def onnx_providers(
    accelerator: Optional[AcceleratorType] = None, prefer_tensorrt: bool = False
) -> List[str]:
    """Execution providers for ONNX Runtime sessions on this machine,
    best-first, restricted to providers actually available in the installed
    onnxruntime build.

    ``prefer_tensorrt=True`` promotes the TensorRT EP to the front on any
    NVIDIA accelerator — use it when benchmarking/serving a baked deployment
    engine on a discrete GPU (TensorRT is already first on Jetson)."""
    accel = accelerator or primary_accelerator().type
    preferred = list(_ONNX_PROVIDER_PREFERENCE.get(accel, ["CPUExecutionProvider"]))
    if prefer_tensorrt and accel in (
        AcceleratorType.NVIDIA_GPU,
        AcceleratorType.JETSON,
    ):
        trt = "TensorrtExecutionProvider"
        preferred = [trt] + [p for p in preferred if p != trt]
    try:
        import onnxruntime as ort

        available = set(ort.get_available_providers())
    except ImportError:
        return ["CPUExecutionProvider"]
    picked = [p for p in preferred if p in available]
    return picked or ["CPUExecutionProvider"]


def describe() -> Dict[str, object]:
    """One-line-able description of this machine's compute (for logs,
    benchmark metadata and W&B configs)."""
    accels = detect_accelerators()
    return {
        "platform": f"{platform.system()} {platform.machine()}",
        "accelerators": [
            {"type": a.type.value, "name": a.name, "memory_gb": a.memory_gb}
            for a in accels
        ],
        "torch_device": resolve_device("auto"),
        "onnx_providers": onnx_providers(),
        "is_raspberry_pi": is_raspberry_pi(),
    }
