/*
 * OhhO Serve — live metrics simulator.
 *
 * Drives the observability panel (latency, throughput, GPU util, VRAM, temp)
 * the way the real Prometheus `/metrics` endpoint would, but in the browser.
 * Calm when idle; it comes alive when the user fires /predict — so cause and
 * effect are honest, not faked background traffic.
 */

import { estimate, getGpu, type ServeConfig } from "./models";

export interface MetricsSnapshot {
  p50: number;
  p95: number;
  throughput: number; // req/s over the recent window
  gpuUtil: number; // %
  vramUsed: number; // GB
  vramTotal: number; // GB
  temp: number; // °C
  reqToday: number;
  latencyHistory: number[]; // raw ms, oldest → newest
  loaded: boolean;
}

const HISTORY = 48;
const THROUGHPUT_WINDOW_MS = 5000;

export class MetricsSim {
  private latencies: number[] = [];
  private reqTimes: number[] = [];
  private util = 4;
  private temp = 38;
  private reqToday = 0;
  private loaded = false;
  private vramUsed = 0;
  private vramTotal = 24;

  constructor(config: ServeConfig) {
    this.setConfig(config, false);
  }

  setConfig(config: ServeConfig, loaded: boolean): void {
    const est = estimate(config);
    this.loaded = loaded;
    this.vramTotal = config.device === "cpu" ? 64 : getGpu(config.gpuId).vramGb;
    this.vramUsed = loaded ? est.totalVram : config.device === "cpu" ? 0 : est.overheadVram;
    if (loaded && this.latencies.length === 0) {
      // seed the chart with the modelled p50 so it reads as a baseline
      this.latencies = [est.latencyMs, est.latencyMs, est.latencyMs];
    }
    if (!loaded) {
      this.latencies = [];
      this.reqTimes = [];
    }
  }

  /** Record a completed /predict at the given wall-clock time. */
  record(latencyMs: number, now: number = Date.now()): void {
    this.latencies.push(latencyMs);
    if (this.latencies.length > HISTORY) this.latencies.shift();
    this.reqTimes.push(now);
    this.reqToday += 1;
    this.util = Math.min(97, this.util + 42); // load pulse
  }

  /** Advance the simulation one frame toward its steady state. */
  tick(now: number = Date.now()): void {
    this.reqTimes = this.reqTimes.filter((t) => now - t <= THROUGHPUT_WINDOW_MS);
    const baseline = this.loaded ? 11 : 3;
    this.util += (baseline - this.util) * 0.16;
    const targetTemp = 38 + this.util * 0.33;
    this.temp += (targetTemp - this.temp) * 0.08;
  }

  snapshot(now: number = Date.now()): MetricsSnapshot {
    const recent = this.reqTimes.filter((t) => now - t <= THROUGHPUT_WINDOW_MS);
    const throughput = round(recent.length / (THROUGHPUT_WINDOW_MS / 1000), 1);
    return {
      p50: percentile(this.latencies, 50),
      p95: percentile(this.latencies, 95),
      throughput,
      gpuUtil: Math.round(this.util),
      vramUsed: round(this.vramUsed, 1),
      vramTotal: this.vramTotal,
      temp: Math.round(this.temp),
      reqToday: this.reqToday,
      latencyHistory: [...this.latencies],
      loaded: this.loaded,
    };
  }
}

export function percentile(values: number[], p: number): number {
  if (values.length === 0) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const idx = Math.min(sorted.length - 1, Math.floor((p / 100) * sorted.length));
  return Math.round(sorted[idx]);
}

function round(v: number, dp: number): number {
  const f = 10 ** dp;
  return Math.round(v * f) / f;
}
