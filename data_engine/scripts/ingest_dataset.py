"""
ingest_dataset.py
-----------------
Batch-convert a directory of ROS 2 bags into a single LeRobot v2.0 dataset.

Usage:
    python -m data_engine.scripts.ingest_dataset \\
        --input-dir /data/bags/pick_place \\
        --output-dir /data/lerobot/pick_place \\
        --task "pick up the red cube and place it in the bin" \\
        --fps 30

NOTE: Use --workers 1 (default) to avoid concurrent writes to meta/*.jsonl.
"""

from __future__ import annotations

import multiprocessing as mp
from pathlib import Path

import click

from data_engine.ingestion.bag_to_omnibot import bag_to_omnibot


def _worker(args: tuple) -> None:
    bag_path, dataset_root, task, fps = args
    try:
        bag_to_omnibot(bag_path, dataset_root, task, fps)
    except Exception as e:
        print(f"[ERROR] {bag_path}: {e}")


@click.command()
@click.option(
    "--input-dir",
    required=True,
    type=click.Path(exists=True),
    help="Directory containing ROS 2 bag folders",
)
@click.option(
    "--output-dir", required=True, help="LeRobot dataset root to write episodes to"
)
@click.option("--task", required=True, help="Natural language task description")
@click.option("--fps", default=30, show_default=True, help="Target frame rate")
@click.option(
    "--workers",
    default=1,
    show_default=True,
    help="Parallel workers (keep at 1 to avoid meta write conflicts)",
)
@click.option(
    "--pattern",
    default="*",
    show_default=True,
    help="Glob pattern to match bag directories (e.g. 'ep*')",
)
def main(
    input_dir: str, output_dir: str, task: str, fps: int, workers: int, pattern: str
) -> None:

    input_path = Path(input_dir)
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)

    bags = sorted(p for p in input_path.glob(pattern) if p.is_dir())
    if not bags:
        click.echo(
            f"[WARN] No bag directories found in {input_dir} matching '{pattern}'"
        )
        return

    click.echo(f"Found {len(bags)} bag(s). Writing to {output_dir} ...")

    job_args = [(str(b), str(output_path), task, fps) for b in bags]

    if workers > 1:
        with mp.Pool(processes=workers) as pool:
            pool.map(_worker, job_args)
    else:
        for args in job_args:
            _worker(args)

    click.echo(f"Done. Dataset: {output_dir}")


if __name__ == "__main__":
    main()
