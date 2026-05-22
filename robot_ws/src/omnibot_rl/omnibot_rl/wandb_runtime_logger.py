#!/usr/bin/env python3
"""
Buffered W&B logger for ROS 2 inference nodes.

Collects metrics at inference frequency (20 Hz), buffers them, and flushes
to W&B in a background thread every `flush_interval_s` seconds. This keeps
the inference loop latency-free while still capturing rich runtime telemetry.

Usage in a ROS 2 node:
    from omnibot_rl.wandb_runtime_logger import WandbRuntimeLogger

    self._wandb = WandbRuntimeLogger(
        project="omnibot_nav",
        run_name="pi5-kitchen-run-1",
        flush_interval_s=5.0,
    )

    # In inference loop:
    self._wandb.log({"inference_ms": latency, "min_lidar": dist})

    # For discrete events:
    self._wandb.log_event("goal_reached")

    # On shutdown:
    self._wandb.finish()
"""

import threading
import time
from collections import defaultdict
from typing import Any

try:
    import wandb

    _WANDB_AVAILABLE = True
except ImportError:
    _WANDB_AVAILABLE = False


class WandbRuntimeLogger:
    """
    Thread-safe, buffered W&B logger for ROS 2 nodes.

    Metrics buffered over each flush window are summarised as mean/min/max
    before being sent, so W&B sees one data-point per flush interval rather
    than one per inference step.
    """

    def __init__(
        self,
        project: str,
        run_name: str | None = None,
        flush_interval_s: float = 5.0,
        tags: list[str] | None = None,
        config: dict | None = None,
    ) -> None:
        self._enabled = _WANDB_AVAILABLE and bool(project)
        self._flush_interval = flush_interval_s
        self._step = 0
        self._buffer: dict[str, list[float]] = defaultdict(list)
        self._lock = threading.Lock()

        if self._enabled:
            wandb.init(
                project=project,
                name=run_name,
                tags=tags or ["runtime", "deployed"],
                config=config or {},
                reinit=True,
            )
            self._thread = threading.Thread(target=self._flush_loop, daemon=True)
            self._thread.start()

    def log(self, metrics: dict[str, Any]) -> None:
        """Buffer scalar metrics (non-blocking, safe to call at 20 Hz)."""
        if not self._enabled:
            return
        with self._lock:
            for k, v in metrics.items():
                try:
                    self._buffer[k].append(float(v))
                except (TypeError, ValueError):
                    pass

    def log_event(self, event: str, value: float = 1.0) -> None:
        """Log a discrete event (e.g. 'goal_reached') as a spike metric."""
        self.log({event: value})

    def finish(self) -> None:
        """Flush remaining buffer and close the W&B run."""
        if not self._enabled:
            return
        self._enabled = False
        self._flush()
        wandb.finish()

    def _flush_loop(self) -> None:
        while self._enabled:
            time.sleep(self._flush_interval)
            self._flush()

    def _flush(self) -> None:
        if not _WANDB_AVAILABLE:
            return
        with self._lock:
            if not self._buffer:
                return
            payload: dict[str, float] = {}
            for k, vals in self._buffer.items():
                if not vals:
                    continue
                payload[f"{k}/mean"] = sum(vals) / len(vals)
                payload[f"{k}/min"] = min(vals)
                payload[f"{k}/max"] = max(vals)
            self._buffer.clear()
            self._step += 1

        if payload:
            wandb.log(payload, step=self._step)
