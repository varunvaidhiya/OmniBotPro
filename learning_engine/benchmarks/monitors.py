"""Hardware telemetry sampled while a benchmark (or the robot) runs.

``ResourceMonitor`` is a context manager running a background sampling
thread; ``summary()`` returns ``resource/*`` metrics (mean/max per channel)
that the reporters publish alongside the AI metrics.

Backends, all optional and auto-selected:
- CPU / RAM     — psutil when present; /proc + load-average fallback
- NVIDIA dGPU   — pynvml (util %, VRAM, power W, temperature)
- Jetson        — tegrastats subprocess parse (GR3D util, RAM, temps)
- Apple Silicon — CPU/RAM only (GPU counters need privileged powermetrics;
                  see ARCHITECTURE.md hardware notes)
"""

from __future__ import annotations

import os
import platform
import re
import subprocess
import threading
from collections import defaultdict
from typing import Dict, List, Optional

from ..hardware import device as hw

try:
    import psutil  # type: ignore

    _HAS_PSUTIL = True
except ImportError:
    _HAS_PSUTIL = False


class _NvmlSampler:
    def __init__(self) -> None:
        import pynvml  # type: ignore

        pynvml.nvmlInit()
        self._nv = pynvml
        self._h = pynvml.nvmlDeviceGetHandleByIndex(0)

    def sample(self) -> Dict[str, float]:
        nv, h = self._nv, self._h
        util = nv.nvmlDeviceGetUtilizationRates(h)
        mem = nv.nvmlDeviceGetMemoryInfo(h)
        out = {
            "gpu_util_pct": float(util.gpu),
            "gpu_mem_used_gb": mem.used / 1024**3,
        }
        try:
            out["gpu_power_w"] = nv.nvmlDeviceGetPowerUsage(h) / 1000.0
            out["gpu_temp_c"] = float(
                nv.nvmlDeviceGetTemperature(h, nv.NVML_TEMPERATURE_GPU)
            )
        except nv.NVMLError:
            pass
        return out


class _TegrastatsSampler:
    """Parses a running `tegrastats` stream (Jetson)."""

    _GR3D = re.compile(r"GR3D_FREQ (\d+)%")
    _RAM = re.compile(r"RAM (\d+)/(\d+)MB")
    _TEMP = re.compile(r"(?:gpu|GPU)@([\d.]+)C")

    def __init__(self, interval_ms: int = 500) -> None:
        self._proc = subprocess.Popen(
            ["tegrastats", "--interval", str(interval_ms)],
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
        )
        self._latest: Dict[str, float] = {}
        self._reader = threading.Thread(target=self._read, daemon=True)
        self._reader.start()

    def _read(self) -> None:
        assert self._proc.stdout is not None
        for line in self._proc.stdout:
            m = self._GR3D.search(line)
            if m:
                self._latest["gpu_util_pct"] = float(m.group(1))
            m = self._RAM.search(line)
            if m:
                self._latest["mem_used_gb"] = float(m.group(1)) / 1024
            m = self._TEMP.search(line)
            if m:
                self._latest["gpu_temp_c"] = float(m.group(1))

    def sample(self) -> Dict[str, float]:
        return dict(self._latest)

    def close(self) -> None:
        self._proc.terminate()


def _cpu_mem_sample() -> Dict[str, float]:
    out: Dict[str, float] = {}
    if _HAS_PSUTIL:
        out["cpu_util_pct"] = psutil.cpu_percent(interval=None)
        vm = psutil.virtual_memory()
        out["mem_used_gb"] = vm.used / 1024**3
        out["mem_used_pct"] = vm.percent
        return out
    try:
        load1 = os.getloadavg()[0]
        out["cpu_load1_per_core"] = load1 / max(os.cpu_count() or 1, 1)
    except (OSError, AttributeError):
        pass
    if platform.system() == "Linux":
        try:
            info = {}
            for line in open("/proc/meminfo"):
                k, v = line.split(":", 1)
                info[k] = float(v.strip().split()[0])  # kB
            out["mem_used_gb"] = (info["MemTotal"] - info["MemAvailable"]) / 1024**2
        except (OSError, KeyError, ValueError):
            pass
    return out


class ResourceMonitor:
    """Background sampler. Usage::

    with ResourceMonitor(interval_s=0.2) as mon:
        run_benchmark()
    metrics.update(mon.summary())   # resource/cpu_util_pct_mean, ...
    """

    def __init__(self, interval_s: float = 0.25) -> None:
        self.interval_s = interval_s
        self._samples: Dict[str, List[float]] = defaultdict(list)
        self._stop = threading.Event()
        self._thread: Optional[threading.Thread] = None
        self._gpu_sampler = self._make_gpu_sampler()

    @staticmethod
    def _make_gpu_sampler():
        if hw.is_jetson():
            try:
                return _TegrastatsSampler()
            except (OSError, FileNotFoundError):
                return None
        try:
            return _NvmlSampler()
        except Exception:
            return None  # no pynvml / no NVIDIA GPU / driver mismatch

    # ----------------------------------------------------------- lifecycle
    def __enter__(self) -> "ResourceMonitor":
        if _HAS_PSUTIL:
            psutil.cpu_percent(interval=None)  # prime the counter
        self._thread = threading.Thread(target=self._run, daemon=True)
        self._thread.start()
        return self

    def __exit__(self, *exc) -> None:
        self._stop.set()
        if self._thread:
            self._thread.join(timeout=2.0)
        sampler = self._gpu_sampler
        if sampler is not None and hasattr(sampler, "close"):
            sampler.close()

    def _run(self) -> None:
        while not self._stop.wait(self.interval_s):
            sample = _cpu_mem_sample()
            if self._gpu_sampler is not None:
                try:
                    sample.update(self._gpu_sampler.sample())
                except Exception:
                    pass
            for k, v in sample.items():
                self._samples[k].append(v)

    # ------------------------------------------------------------- results
    def summary(self) -> Dict[str, float]:
        out: Dict[str, float] = {}
        for k, vals in self._samples.items():
            if not vals:
                continue
            out[f"resource/{k}_mean"] = round(sum(vals) / len(vals), 3)
            out[f"resource/{k}_max"] = round(max(vals), 3)
        out["resource/samples"] = float(
            max((len(v) for v in self._samples.values()), default=0)
        )
        return out
