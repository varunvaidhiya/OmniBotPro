"""Reporters — publish benchmark results and loop metrics automatically.

| Reporter            | Destination                                          |
|---------------------|------------------------------------------------------|
| JsonFileReporter    | results dir (one JSON per run; CI artifacts)         |
| WandbReporter       | Weights & Biases (system info → run config)          |
| PrometheusReporter  | existing Grafana/Prometheus stack, two modes:        |
|                     |  • HTTP /metrics endpoint (add a scrape target)      |
|                     |  • node_exporter textfile collector (.prom file)     |

All reporters also implement ``log_metrics`` so the PostTrainingLoop can
stream per-iteration metrics through the same channels.
"""

from __future__ import annotations

import abc
import json
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any, Dict, Optional, Sequence

from .ai_benchmark import BenchmarkResult


class Reporter(abc.ABC):
    @abc.abstractmethod
    def publish(self, result: BenchmarkResult) -> None: ...

    def log_metrics(
        self,
        metrics: Dict[str, float],
        step: Optional[int] = None,
        context: str = "loop",
    ) -> None:
        """Stream scalar metrics (e.g. learning-loop iterations)."""

    def close(self) -> None:
        pass


class JsonFileReporter(Reporter):
    def __init__(self, output_dir: str = "~/benchmarks/learning_engine") -> None:
        self.dir = Path(output_dir).expanduser()
        self.dir.mkdir(parents=True, exist_ok=True)

    def publish(self, result: BenchmarkResult) -> None:
        slug = result.name.replace("/", "_")
        stamp = time.strftime("%Y%m%d_%H%M%S", time.localtime(result.started_at))
        path = self.dir / f"{stamp}_{slug}_{result.system.label()}.json"
        path.write_text(json.dumps(result.to_dict(), indent=1))

    def log_metrics(self, metrics, step=None, context="loop") -> None:
        path = self.dir / f"{context}_metrics.jsonl"
        with path.open("a") as f:
            f.write(json.dumps({"step": step, "time": time.time(), **metrics}) + "\n")


class WandbReporter(Reporter):
    """One W&B run per reporter lifetime. Benchmark results are logged with
    their suite name as the metric prefix; the machine's SystemInfo becomes
    the run config, so W&B's grouping/filtering compares hardware directly.
    """

    def __init__(
        self,
        project: str = "omnibot_benchmarks",
        entity: str = "",
        run_name: str = "",
        tags: Sequence[str] = (),
        group: str = "",
    ) -> None:
        try:
            import wandb
        except ImportError as e:
            raise RuntimeError("WandbReporter requires `pip install wandb`") from e
        self._wandb = wandb
        from .system_probe import probe

        system = probe()
        self.run = wandb.init(
            project=project,
            entity=entity or None,
            name=run_name or f"{system.label()}-{time.strftime('%m%d-%H%M')}",
            tags=[*tags, system.accelerator_type, system.hw_profile],
            group=group or system.hw_profile,
            config=system.to_dict(),
            reinit=True,
        )

    def publish(self, result: BenchmarkResult) -> None:
        self.run.log({f"{result.name}/{k}": v for k, v in result.metrics.items()})
        self.run.summary.update(
            {f"{result.name}/{k}": v for k, v in result.metrics.items()}
        )

    def log_metrics(self, metrics, step=None, context="loop") -> None:
        self.run.log({f"{context}/{k}": v for k, v in metrics.items()}, step=step)

    def close(self) -> None:
        self.run.finish()


class PrometheusReporter(Reporter):
    """Feeds the existing infra/observability stack.

    HTTP mode (``port=8890``): exposes the latest values on ``/metrics`` —
    add a scrape target to infra/observability/prometheus/prometheus.yml::

        - job_name: omnibot_benchmarks
          static_configs:
            - targets: ["<machine-ip>:8890"]

    Textfile mode (``textfile=".../bench.prom"``): writes the node_exporter
    textfile-collector format (the Pi's node_exporter from
    setup_pi_agents.sh picks it up with --collector.textfile.directory).
    """

    def __init__(
        self,
        port: Optional[int] = None,
        textfile: str = "",
        prefix: str = "omnibot_bench",
    ) -> None:
        """``port=None`` disables the HTTP server; ``port=0`` binds an
        ephemeral port (see ``.port``)."""
        if port is None and not textfile:
            raise ValueError("PrometheusReporter needs port and/or textfile")
        self.prefix = prefix
        self.textfile = Path(textfile).expanduser() if textfile else None
        self._gauges: Dict[str, float] = {}
        self._labels: Dict[str, Dict[str, str]] = {}
        self._lock = threading.Lock()
        self._server: Optional[ThreadingHTTPServer] = None
        if port is not None:
            self._start_server(port)

    # -------------------------------------------------------------- publish
    def publish(self, result: BenchmarkResult) -> None:
        labels = {
            "benchmark": result.name,
            "host": result.system.hostname,
            "accelerator": result.system.accelerator_type,
            "device": result.device,
            "hw_profile": result.system.hw_profile,
        }
        self._set_many(result.metrics, labels)

    def log_metrics(self, metrics, step=None, context="loop") -> None:
        self._set_many({f"{context}_{k}": v for k, v in metrics.items()}, {})

    def _set_many(self, metrics: Dict[str, float], labels: Dict[str, str]) -> None:
        with self._lock:
            for k, v in metrics.items():
                if not isinstance(v, (int, float)):
                    continue
                name = _sanitize(f"{self.prefix}_{k}")
                self._gauges[name] = float(v)
                self._labels[name] = labels
        if self.textfile:
            self._write_textfile()

    # -------------------------------------------------------------- render
    def render(self) -> str:
        lines = []
        with self._lock:
            for name in sorted(self._gauges):
                labels = self._labels.get(name) or {}
                label_str = ",".join(f'{k}="{v}"' for k, v in sorted(labels.items()))
                lines.append(f"# TYPE {name} gauge")
                lines.append(
                    f"{name}{{{label_str}}} {self._gauges[name]}"
                    if label_str
                    else f"{name} {self._gauges[name]}"
                )
        return "\n".join(lines) + "\n"

    def _write_textfile(self) -> None:
        assert self.textfile is not None
        self.textfile.parent.mkdir(parents=True, exist_ok=True)
        tmp = self.textfile.with_suffix(".prom.tmp")
        tmp.write_text(self.render())
        tmp.replace(self.textfile)  # atomic — node_exporter never sees a partial file

    # --------------------------------------------------------------- server
    def _start_server(self, port: int) -> None:
        reporter = self

        class Handler(BaseHTTPRequestHandler):
            def do_GET(self) -> None:  # noqa: N802 — http.server API
                if self.path.rstrip("/") not in ("", "/metrics"):
                    self.send_response(404)
                    self.end_headers()
                    return
                body = reporter.render().encode()
                self.send_response(200)
                self.send_header("Content-Type", "text/plain; version=0.0.4")
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)

            def log_message(self, *args: Any) -> None:  # silence access log
                pass

        self._server = ThreadingHTTPServer(("0.0.0.0", port), Handler)
        threading.Thread(target=self._server.serve_forever, daemon=True).start()

    @property
    def port(self) -> int:
        return self._server.server_address[1] if self._server else 0

    def close(self) -> None:
        if self._server:
            self._server.shutdown()
            self._server = None


def _sanitize(name: str) -> str:
    out = "".join(c if (c.isalnum() or c == "_") else "_" for c in name)
    return out if not out[0].isdigit() else f"_{out}"
