#!/usr/bin/env python3
"""
benchmarks/compare_baseline.py
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Compare latest benchmark results against a stored baseline.

Usage:
    # Check for regressions (exits 1 on failure):
    python benchmarks/compare_baseline.py

    # Update baseline with current results:
    python benchmarks/compare_baseline.py --update-baseline

    # Specify custom paths:
    python benchmarks/compare_baseline.py \
        --results benchmarks/results/ \
        --baseline benchmarks/baselines/baseline_ci.json \
        --threshold 0.20

Exit codes:
    0 — No regressions detected, all SLOs met
    1 — One or more p95 values exceed SLO max or regressed >threshold vs baseline
"""

from __future__ import annotations

import argparse
import json
import platform
import sys
from datetime import datetime, timezone
from pathlib import Path

_REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(_REPO))

from benchmarks.conftest import SLO_TABLE, get_slo

_RESULTS_DIR = Path(__file__).parent / "results"
_BASELINES_DIR = Path(__file__).parent / "baselines"


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _machine_type() -> str:
    import os

    if os.environ.get("OMNIBOT_MACHINE"):
        return os.environ["OMNIBOT_MACHINE"].lower()
    try:
        import torch

        if torch.cuda.is_available():
            return "gpu"
    except ImportError:
        pass
    if platform.machine() == "aarch64":
        return "pi5"
    return "ci"


def _load_latest_results(results_dir: Path) -> dict[str, dict]:
    """Load all result JSON files, returning the latest per metric name."""
    if not results_dir.exists():
        print(f"Results directory not found: {results_dir}")
        return {}

    all_metrics: dict[str, dict] = {}
    for jf in sorted(results_dir.glob("*.json")):
        try:
            data = json.loads(jf.read_text())
            metrics = data.get("metrics", {})
            for name, stats in metrics.items():
                # Later files (sorted alphabetically = chronologically) win
                all_metrics[name] = stats
        except Exception as e:
            print(f"  WARN  Could not load {jf.name}: {e}")

    return all_metrics


def _baseline_path(baselines_dir: Path) -> Path:
    machine = _machine_type()
    return baselines_dir / f"baseline_{machine}.json"


def _load_baseline(path: Path) -> dict[str, dict]:
    if not path.exists():
        return {}
    try:
        data = json.loads(path.read_text())
        return data.get("metrics", {})
    except Exception as e:
        print(f"  WARN  Could not load baseline {path}: {e}")
        return {}


# ---------------------------------------------------------------------------
# Comparison logic
# ---------------------------------------------------------------------------


def compare(
    current: dict[str, dict],
    baseline: dict[str, dict],
    threshold: float = 0.20,
) -> tuple[list[str], list[str], list[str]]:
    """
    Compare current metrics against baseline and SLOs.

    Returns:
        (failures, warnings, improvements)
    """
    failures: list[str] = []
    warnings: list[str] = []
    improvements: list[str] = []

    all_metrics = set(current.keys()) | set(SLO_TABLE.keys())

    for name in sorted(all_metrics):
        if name not in current:
            continue

        stats = current[name]
        p95 = stats.get("p95_ms", stats.get("p95", None))
        if p95 is None:
            continue

        slo = get_slo(name)
        slo_target = slo["target"]
        slo_max = slo["max"]

        # SLO check
        if p95 > slo_max:
            failures.append(f"  FAIL  {name}: p95={p95:.2f}ms > SLO max={slo_max}ms")
        elif p95 > slo_target:
            warnings.append(
                f"  WARN  {name}: p95={p95:.2f}ms > SLO target={slo_target}ms "
                f"(max={slo_max}ms)"
            )

        # Regression check vs baseline
        if name in baseline:
            b_stats = baseline[name]
            b_p95 = b_stats.get("p95_ms", b_stats.get("p95", None))
            if b_p95 is not None and b_p95 > 0:
                change = (p95 - b_p95) / b_p95
                if change > threshold:
                    failures.append(
                        f"  REGR  {name}: p95={p95:.2f}ms vs baseline={b_p95:.2f}ms "
                        f"(+{change * 100:.0f}% > threshold {threshold * 100:.0f}%)"
                    )
                elif change < -0.10:
                    improvements.append(
                        f"  IMPR  {name}: p95={p95:.2f}ms vs baseline={b_p95:.2f}ms "
                        f"({change * 100:.0f}%)"
                    )

    return failures, warnings, improvements


def write_baseline(current: dict[str, dict], path: Path) -> None:
    """Write current results as new baseline."""
    path.parent.mkdir(exist_ok=True)
    machine = _machine_type()
    payload = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "platform": platform.platform(),
        "machine": machine,
        "python_version": sys.version,
        "metrics": {
            name: {
                "p50": stats.get("median_ms", 0.0),
                "p95": stats.get("p95_ms", 0.0),
                "p99": stats.get("p99_ms", 0.0),
                "mean": stats.get("mean_ms", 0.0),
            }
            for name, stats in current.items()
        },
    }
    path.write_text(json.dumps(payload, indent=2))
    print(f"Baseline written → {path}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare benchmark results vs baseline"
    )
    parser.add_argument(
        "--results",
        type=Path,
        default=_RESULTS_DIR,
        help="Directory with benchmark result JSON files",
    )
    parser.add_argument(
        "--baseline",
        type=Path,
        default=None,
        help="Baseline JSON path (default: baselines/baseline_<machine>.json)",
    )
    parser.add_argument(
        "--threshold",
        type=float,
        default=0.20,
        help="Regression threshold as fraction (default: 0.20 = 20%%)",
    )
    parser.add_argument(
        "--update-baseline",
        action="store_true",
        help="Write current results as new baseline and exit",
    )
    args = parser.parse_args()

    baseline_path = args.baseline or _baseline_path(_BASELINES_DIR)
    current = _load_latest_results(args.results)

    if not current:
        print(f"No result JSON files found in {args.results}")
        print("Run benchmarks first: ./benchmarks/run_benchmarks.sh")
        return 1

    if args.update_baseline:
        write_baseline(current, baseline_path)
        return 0

    baseline = _load_baseline(baseline_path)
    if not baseline:
        print(
            f"No baseline found at {baseline_path}. "
            "Run with --update-baseline after a clean run."
        )

    print("\n" + "=" * 70)
    print(f"OmniBot Performance Comparison ({_machine_type()})")
    print(f"Results:  {args.results}")
    print(f"Baseline: {baseline_path}")
    print(f"Regression threshold: {args.threshold * 100:.0f}%")
    print("=" * 70)

    failures, warnings, improvements = compare(current, baseline, args.threshold)

    if improvements:
        print("\n[Improvements]")
        for msg in improvements:
            print(msg)

    if warnings:
        print("\n[Warnings]")
        for msg in warnings:
            print(msg)

    if failures:
        print("\n[Failures]")
        for msg in failures:
            print(msg)
        print(f"\n{len(failures)} failure(s) detected. Fix before merging.\n")
        return 1

    print(
        f"\nAll checks passed. ({len(warnings)} warnings, {len(improvements)} improvements)\n"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
