"""Benchmark CLI — run the same suite on any target hardware and publish.

Examples::

    # Plumbing check anywhere (random policy, JSON results only)
    python3 -m learning_engine.benchmarks.run --suite inference

    # ONNX RL policy on Jetson, publish to W&B + Prometheus :8890
    python3 -m learning_engine.benchmarks.run --suite inference \
        --policy onnx --policy-kwargs '{"model_path": "~/models/omnibot_nav_policy.onnx"}' \
        --budget-ms 50 --wandb-project omnibot_benchmarks --prom-port 8890

    # SmolVLA on the workstation / Apple M-series (device auto-resolves)
    python3 -m learning_engine.benchmarks.run --suite inference \
        --policy smolvla --budget-ms 100 --wandb-project omnibot_benchmarks

    # Trainer + dataset I/O throughput
    python3 -m learning_engine.benchmarks.run --suite training,dataset
"""

from __future__ import annotations

import argparse
import json
import sys
import tempfile
from typing import List

from .. import policies  # noqa: F401 — populates POLICIES/TRAINERS registries
from ..core.registry import POLICIES, TRAINERS
from .ai_benchmark import (
    BenchmarkResult,
    DatasetIOBenchmark,
    InferenceBenchmark,
    TrainingBenchmark,
    episodes_for_training,
)
from .reporters import JsonFileReporter, PrometheusReporter, Reporter, WandbReporter
from .system_probe import probe


def build_reporters(args: argparse.Namespace) -> List[Reporter]:
    reporters: List[Reporter] = [JsonFileReporter(args.output_dir)]
    if args.wandb_project:
        reporters.append(
            WandbReporter(
                project=args.wandb_project, entity=args.wandb_entity, tags=["benchmark"]
            )
        )
    if args.prom_port or args.prom_textfile:
        reporters.append(
            PrometheusReporter(port=args.prom_port or None, textfile=args.prom_textfile)
        )
    return reporters


def main(argv: List[str] = None) -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    ap.add_argument(
        "--suite",
        default="inference",
        help="comma-separated: inference,training,dataset",
    )
    ap.add_argument(
        "--policy", default="random", help=f"policy registry name ({POLICIES.names()})"
    )
    ap.add_argument("--policy-kwargs", default="{}", help="JSON constructor kwargs")
    ap.add_argument(
        "--trainer",
        default="behavior_cloning",
        help=f"trainer registry name ({TRAINERS.names()})",
    )
    ap.add_argument("--trainer-kwargs", default="{}", help="JSON constructor kwargs")
    ap.add_argument("--n", type=int, default=200, help="inference iterations")
    ap.add_argument(
        "--budget-ms",
        type=float,
        default=100.0,
        help="control-period budget for the within_budget verdict",
    )
    ap.add_argument("--task", default="pick up the red cup")
    ap.add_argument("--output-dir", default="~/benchmarks/learning_engine")
    ap.add_argument("--wandb-project", default="", help="publish to this W&B project")
    ap.add_argument("--wandb-entity", default="")
    ap.add_argument(
        "--prom-port",
        type=int,
        default=0,
        help="serve /metrics on this port (existing Prometheus scrapes it)",
    )
    ap.add_argument(
        "--prom-textfile",
        default="",
        help="write node_exporter textfile-collector .prom file here",
    )
    ap.add_argument(
        "--hold",
        action="store_true",
        help="keep the process alive after the run (for /metrics scraping)",
    )
    args = ap.parse_args(argv)

    system = probe()
    print(f"machine : {system.hostname} — {system.cpu}")
    print(
        f"accel   : {system.accelerator_type} ({system.accelerator_name}, "
        f"{system.accelerator_memory_gb} GB)"
    )
    print(f"device  : {system.torch_device}   onnx EPs: {system.onnx_providers}")
    print(f"profile : {system.hw_profile}\n")

    suites = [s.strip() for s in args.suite.split(",") if s.strip()]
    reporters = build_reporters(args)
    results: List[BenchmarkResult] = []

    try:
        if "inference" in suites:
            policy = POLICIES.create(args.policy, **json.loads(args.policy_kwargs))
            results.append(
                InferenceBenchmark(
                    policy,
                    task=args.task,
                    n=args.n,
                    budget_ms=args.budget_ms,
                    system=system,
                ).run()
            )
        if "training" in suites:
            trainer = TRAINERS.create(args.trainer, **json.loads(args.trainer_kwargs))
            results.append(
                TrainingBenchmark(
                    trainer,
                    episodes_for_training(),
                    system=system,
                ).run()
            )
        if "dataset" in suites:
            with tempfile.TemporaryDirectory() as tmp:
                results.append(DatasetIOBenchmark(tmp, system=system).run())

        for result in results:
            print(f"── {result.name}  [{result.device}]")
            for k, v in sorted(result.metrics.items()):
                print(
                    f"   {k:32s} {v:>12.3f}"
                    if isinstance(v, float)
                    else f"   {k:32s} {v}"
                )
            for reporter in reporters:
                reporter.publish(result)

        verdicts = [r for r in results if "within_budget" in r.metrics]
        failed = [r for r in verdicts if not r.metrics["within_budget"]]
        if failed:
            print(f"\nBUDGET EXCEEDED: {[r.name for r in failed]}")
        if args.hold:
            print("\nholding for Prometheus scrapes — Ctrl-C to exit")
            try:
                import time

                while True:
                    time.sleep(60)
            except KeyboardInterrupt:
                pass
        return 1 if failed else 0
    finally:
        for reporter in reporters:
            reporter.close()


if __name__ == "__main__":
    sys.exit(main())
