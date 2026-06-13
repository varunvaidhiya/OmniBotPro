"""AI performance benchmarks — same suites on every target hardware.

Each suite produces a ``BenchmarkResult`` bundling:
- AI metrics (latency percentiles, achievable Hz, throughput),
- hardware telemetry captured *during* the run (``resource/*``),
- the full ``SystemInfo`` snapshot,

so a single W&B project can compare Pi vs Jetson vs workstation vs M-series
runs directly. Control-budget verdicts use the same idea as the SLO table
in ``benchmarks/conftest.py``: a policy is deployable when p95 latency fits
the control period (20 Hz RL → 50 ms, 10 Hz SmolVLA → 100 ms, 1 Hz OpenVLA).
"""

from __future__ import annotations

import time
from dataclasses import dataclass, field
from typing import Callable, Dict, List, Optional

import numpy as np

from ..core.interfaces import Policy, PolicyTrainer
from ..core.types import Episode
from ..data import schema
from .monitors import ResourceMonitor
from .system_probe import SystemInfo, probe


@dataclass
class BenchmarkResult:
    name: str
    metrics: Dict[str, float]
    system: SystemInfo
    device: str
    tags: List[str] = field(default_factory=list)
    started_at: float = field(default_factory=time.time)

    def to_dict(self) -> Dict[str, object]:
        return {
            "name": self.name,
            "device": self.device,
            "tags": self.tags,
            "started_at": self.started_at,
            "metrics": self.metrics,
            "system": self.system.to_dict(),
        }


def _latency_stats(samples_ms: List[float]) -> Dict[str, float]:
    arr = np.asarray(samples_ms, dtype=np.float64)
    return {
        "latency_ms_mean": float(arr.mean()),
        "latency_ms_p50": float(np.percentile(arr, 50)),
        "latency_ms_p95": float(np.percentile(arr, 95)),
        "latency_ms_p99": float(np.percentile(arr, 99)),
        "latency_ms_max": float(arr.max()),
        "achievable_hz": float(1000.0 / max(arr.mean(), 1e-9)),
        "iterations": float(len(arr)),
    }


def default_observation(with_images: bool = True) -> Dict[str, np.ndarray]:
    """Synthetic observation matching the canonical schema (used when no
    dataset is supplied)."""
    obs = {schema.OBS_STATE: np.zeros(schema.MOBILE_MANIP_STATE_DIM, dtype=np.float32)}
    if with_images:
        obs[schema.OBS_IMAGE_WRIST] = np.zeros((240, 320, 3), dtype=np.uint8)
        obs[schema.OBS_IMAGE_FRONT] = np.zeros((480, 640, 3), dtype=np.uint8)
        obs[schema.OBS_IMAGE_BEV] = np.zeros((240, 320, 3), dtype=np.uint8)
    return obs


class InferenceBenchmark:
    """Policy inference latency + sustained throughput under telemetry."""

    def __init__(
        self,
        policy: Policy,
        observation_factory: Callable[[], Dict[str, np.ndarray]] = default_observation,
        task: str = "benchmark task",
        n: int = 200,
        warmup: int = 20,
        budget_ms: float = 100.0,  # control-period budget (10 Hz default)
        system: Optional[SystemInfo] = None,
    ) -> None:
        self.policy = policy
        self.observation_factory = observation_factory
        self.task = task
        self.n = n
        self.warmup = warmup
        self.budget_ms = budget_ms
        self.system = system or probe()

    def run(self) -> BenchmarkResult:
        obs = self.observation_factory()

        t0 = time.perf_counter()
        self.policy.predict(obs, task=self.task)
        cold_ms = (time.perf_counter() - t0) * 1000.0

        for _ in range(self.warmup):
            self.policy.predict(obs, task=self.task)

        samples: List[float] = []
        with ResourceMonitor() as mon:
            for _ in range(self.n):
                t0 = time.perf_counter()
                self.policy.predict(obs, task=self.task)
                samples.append((time.perf_counter() - t0) * 1000.0)

        metrics = _latency_stats(samples)
        metrics["cold_latency_ms"] = cold_ms
        metrics["budget_ms"] = self.budget_ms
        metrics["within_budget"] = float(metrics["latency_ms_p95"] <= self.budget_ms)
        metrics.update(mon.summary())
        return BenchmarkResult(
            name=f"inference/{type(self.policy).__name__}",
            metrics=metrics,
            system=self.system,
            device=getattr(self.policy, "device", self.system.torch_device),
            tags=["inference"],
        )


class TrainingBenchmark:
    """Trainer throughput (transitions/sec) under telemetry."""

    def __init__(
        self,
        trainer: PolicyTrainer,
        dataset,
        policy: Optional[Policy] = None,
        system: Optional[SystemInfo] = None,
    ) -> None:
        self.trainer = trainer
        self.dataset = dataset
        self.policy = policy
        self.system = system or probe()

    def run(self) -> BenchmarkResult:
        with ResourceMonitor() as mon:
            t0 = time.perf_counter()
            result = self.trainer.train(self.dataset, self.policy)
            wall_s = time.perf_counter() - t0

        transitions = result.metrics.get("transitions", 0.0)
        metrics = {
            "wall_s": round(wall_s, 3),
            "train_steps": float(result.steps),
            "final_loss": result.final_loss,
            "transitions": transitions,
            "transitions_per_s": round(transitions / wall_s, 1) if wall_s else 0.0,
            "steps_per_s": round(result.steps / wall_s, 2) if wall_s else 0.0,
        }
        metrics.update(mon.summary())
        return BenchmarkResult(
            name=f"training/{self.trainer.name}",
            metrics=metrics,
            system=self.system,
            device=getattr(self.trainer, "device", self.system.torch_device),
            tags=["training"],
        )


class DatasetIOBenchmark:
    """ReplayDataset write/read throughput — storage matters on SD-card
    targets (Pi, Jetson) far more than on the workstation."""

    def __init__(
        self,
        root: str,
        episodes: int = 10,
        steps: int = 100,
        with_images: bool = True,
        system: Optional[SystemInfo] = None,
    ) -> None:
        self.root = root
        self.episodes = episodes
        self.steps = steps
        self.with_images = with_images
        self.system = system or probe()

    def run(self) -> BenchmarkResult:
        from ..core.types import Episode as Ep, EpisodeMeta, Step
        from ..data.replay_dataset import ReplayDataset

        ds = ReplayDataset(self.root, store_images=self.with_images)

        def make_ep() -> Ep:
            return Ep(
                meta=EpisodeMeta(task_instruction="io benchmark"),
                steps=[
                    Step(
                        observation=default_observation(self.with_images),
                        action=np.zeros(
                            schema.MOBILE_MANIP_ACTION_DIM, dtype=np.float32
                        ),
                        timestamp=float(t),
                    )
                    for t in range(self.steps)
                ],
            )

        episodes = [make_ep() for _ in range(self.episodes)]
        with ResourceMonitor() as mon:
            t0 = time.perf_counter()
            ids = [ds.add_episode(ep) for ep in episodes]
            write_s = time.perf_counter() - t0
            t0 = time.perf_counter()
            for eid in ids:
                ds.get(eid)
            read_s = time.perf_counter() - t0

        metrics = {
            "write_eps_per_s": round(self.episodes / write_s, 2),
            "read_eps_per_s": round(self.episodes / read_s, 2),
            "write_steps_per_s": round(self.episodes * self.steps / write_s, 1),
            "read_steps_per_s": round(self.episodes * self.steps / read_s, 1),
        }
        metrics.update(mon.summary())
        for eid in ids:
            ds.remove(eid)
        return BenchmarkResult(
            name="dataset_io/replay_dataset",
            metrics=metrics,
            system=self.system,
            device="disk",
            tags=["dataset_io"],
        )


def episodes_for_training(n_episodes: int = 20, n_steps: int = 50) -> List[Episode]:
    """Synthetic state-only episodes for trainer benchmarks."""
    from ..core.types import Episode as Ep, EpisodeMeta, Step

    rng = np.random.default_rng(0)
    out = []
    for _ in range(n_episodes):
        steps = [
            Step(
                observation={
                    schema.OBS_STATE: rng.normal(
                        size=schema.MOBILE_MANIP_STATE_DIM
                    ).astype(np.float32)
                },
                action=rng.normal(size=schema.MOBILE_MANIP_ACTION_DIM).astype(
                    np.float32
                ),
                timestamp=float(t),
            )
            for t in range(n_steps)
        ]
        out.append(
            Ep(meta=EpisodeMeta(task_instruction="train benchmark"), steps=steps)
        )
    return out
