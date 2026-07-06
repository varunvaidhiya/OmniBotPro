"use client";

/*
 * TrainConsole — the OhhO Train application shell.
 *
 * Run config (left), live loss / success-rate curves (centre), and GPU
 * telemetry + verify/export (right). Start a run and the metrics animate;
 * the export-to-Serve step unlocks once the eval passes.
 */

import { useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  FlaskConical,
  Play,
  Pause,
  RotateCcw,
  Cpu,
  CheckCircle2,
  Rocket,
  ShieldCheck,
} from "lucide-react";

import { useTraining, METHODS } from "@/lib/train/training";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";
import PaidFeatureGate from "@/components/auth/PaidFeatureGate";

const CYAN = "#00D4FF";
const VIOLET = "#A78BFA";
const GREEN = "#34D399";
const AMBER = "#FBBF24";

export default function TrainConsole() {
  const { config } = useRobot();
  const t = useTraining();
  const [exported, setExported] = useState(false);

  const stepPct = t.step / t.totalSteps;

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{ background: "rgba(10,14,26,.86)", backdropFilter: "blur(18px)", borderColor: "var(--border)" }}
      >
        <Link href="/products/train" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0" style={{ color: CYAN }}>
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Train</span>
        </Link>
        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />
        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <FlaskConical size={14} /> {t.config.method}-ft
        </span>
        <span className="ml-auto flex items-center gap-2">
          <span
            className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{
              background: t.state === "running" ? "rgba(0,212,255,.12)" : "rgba(255,255,255,.04)",
              color: t.state === "running" ? CYAN : t.state === "done" ? GREEN : "var(--muted)",
              border: `1px solid ${t.state === "running" ? "rgba(0,212,255,.4)" : "var(--border)"}`,
            }}
          >
            <span className="badge-dot" style={{ background: t.state === "running" ? CYAN : t.state === "done" ? GREEN : "var(--muted)", width: 6, height: 6 }} />
            {t.state.toUpperCase()}
          </span>
        </span>
      </header>

      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_300px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Run config */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="train" config={config} />
          </div>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Run config</h2>

            <label className="block text-[11px] font-mono mb-1.5" style={{ color: "var(--muted)" }}>Method</label>
            <div className="flex flex-wrap gap-1.5 mb-4">
              {METHODS.map((m) => (
                <button
                  key={m}
                  onClick={() => t.setConfig((c) => ({ ...c, method: m }))}
                  disabled={t.state === "running"}
                  className="text-[11px] font-mono px-2.5 py-1 rounded-md transition-all disabled:opacity-40"
                  style={
                    t.config.method === m
                      ? { background: CYAN, color: "var(--bg)" }
                      : { border: "1px solid var(--border-med)", color: "var(--muted)" }
                  }
                >
                  {m}
                </button>
              ))}
            </div>

            {[
              { l: "dataset", v: config.datasetName },
              { l: "epochs", v: String(t.config.epochs) },
              { l: "lr", v: t.config.lr },
              { l: "device", v: t.config.device },
            ].map((row) => (
              <div key={row.l} className="flex justify-between items-center py-2 border-t text-[12px]" style={{ borderColor: "var(--border)" }}>
                <span className="font-mono" style={{ color: "var(--muted)" }}>{row.l}</span>
                <span className="font-mono text-white text-right max-w-[170px] truncate">{row.v}</span>
              </div>
            ))}

            <div className="mt-4 rounded-lg p-3 text-center text-[11px]" style={{ background: "rgba(124,58,237,.1)", border: "1px solid rgba(124,58,237,.3)", color: VIOLET }}>
              built on the open OmniVLA engine
            </div>
          </div>

          <div className="p-5 flex flex-col gap-2.5">
            <PaidFeatureGate
              feature="gpu-training"
              label="GPU Training"
              description="Runs on OhhO cloud GPUs — requires a plan"
            >
              <button
                onClick={t.state === "running" ? t.pause : t.start}
                className="inline-flex items-center justify-center gap-2 text-[13px] font-semibold px-4 py-2.5 rounded-lg transition-all"
                style={{ background: CYAN, color: "var(--bg)" }}
              >
                {t.state === "running" ? <Pause size={15} /> : <Play size={15} />}
                {t.state === "running" ? "Pause run" : t.state === "done" ? "Train again" : t.state === "paused" ? "Resume" : "Start training"}
              </button>
            </PaidFeatureGate>
            <button
              onClick={() => { t.reset(); setExported(false); }}
              className="inline-flex items-center justify-center gap-2 text-[12px] font-semibold px-4 py-2 rounded-lg transition-all hover:bg-white/[0.05]"
              style={{ border: "1px solid var(--border-med)", color: "var(--muted)" }}
            >
              <RotateCcw size={13} /> Reset
            </button>
          </div>
        </aside>

        {/* CENTER — Live curves */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b" style={{ borderColor: "var(--border)" }}>
            <h1 className="text-xl font-medium text-white mb-1 flex items-center gap-2">
              <FlaskConical size={20} color={CYAN} /> Training run
            </h1>
            <p className="text-sm text-gray-400">Loss and task success stream live; verified checkpoints export to OhhO Serve.</p>
          </div>

          <div className="flex-1 overflow-y-auto p-6 flex flex-col gap-6">
            {/* step / epoch progress */}
            <div className="grid grid-cols-3 gap-3">
              <Stat label="step" value={`${(t.step / 1000).toFixed(1)}k`} sub={`/ ${t.totalSteps / 1000}k`} color={CYAN} />
              <Stat label="epoch" value={`${t.epoch}`} sub={`/ ${t.config.epochs}`} color={CYAN} />
              <Stat label="loss" value={t.lossNow.toFixed(3)} sub="train" color={VIOLET} />
            </div>
            <div className="h-1.5 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,.09)" }}>
              <div className="h-full rounded-full transition-all duration-300" style={{ width: `${stepPct * 100}%`, background: CYAN }} />
            </div>

            {/* loss chart */}
            <Chart title="Loss" valueLabel={t.lossNow.toFixed(3)} data={t.loss} color={CYAN} domainMax={1} />

            {/* success chart */}
            <Chart
              title="Task success rate"
              valueLabel={`${Math.round(t.successNow * 100)}%`}
              data={t.success}
              color={GREEN}
              domainMax={1}
              threshold={0.8}
            />
          </div>
        </section>

        {/* RIGHT — GPU + verify/export */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4 flex items-center gap-2" style={{ color: "var(--muted)" }}>
              <Cpu size={13} /> GPU 0
            </h2>
            <Gauge label="Utilization" value={t.gpu.util} unit="%" max={100} color={CYAN} />
            <Gauge label="VRAM" value={t.gpu.vram} unit=" GB" max={16} color={t.gpu.vram > 15 ? AMBER : CYAN} />
            <Gauge label="Temp" value={t.gpu.temp} unit="°C" max={100} color={t.gpu.temp > 80 ? "#F87171" : GREEN} />
          </div>

          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3 flex items-center gap-2" style={{ color: "var(--muted)" }}>
              <ShieldCheck size={13} /> Verify
            </h2>
            <div className="flex items-center justify-between text-[12px] mb-2">
              <span style={{ color: "var(--muted)" }}>Best-of-N · safety checks</span>
              <span className="font-mono" style={{ color: t.canExport ? GREEN : AMBER }}>
                {t.canExport ? "PASSED" : "pending"}
              </span>
            </div>
            <div className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
              hard reachability + velocity limits enforced
            </div>
          </div>

          <div className="p-5">
            <button
              onClick={() => setExported(true)}
              disabled={!t.canExport || exported}
              className="w-full inline-flex items-center justify-center gap-2 text-[13px] font-semibold px-4 py-3 rounded-lg transition-all disabled:opacity-40"
              style={{ background: t.canExport ? CYAN : "rgba(255,255,255,.06)", color: t.canExport ? "var(--bg)" : "var(--muted)" }}
            >
              {exported ? <CheckCircle2 size={15} /> : <Rocket size={15} />}
              {exported ? "Exported to Serve" : "Export → OhhO Serve"}
            </button>
            {exported && (
              <div className="mt-3 rounded-lg p-3 text-[11px] font-mono" style={{ background: "rgba(52,211,153,.08)", border: "1px solid rgba(52,211,153,.3)", color: GREEN }}>
                checkpoint_0050.safetensors → registry · ONNX exported for Fleet OTA
              </div>
            )}
            <div className="mt-3 text-[11px]" style={{ color: "var(--muted)" }}>
              Export unlocks at ≥ 80% success.
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}

function Stat({ label, value, sub, color }: { label: string; value: string; sub: string; color: string }) {
  return (
    <div className="rounded-xl border p-3" style={{ borderColor: "var(--border)", background: "var(--surf)" }}>
      <div className="text-[10px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--muted)" }}>{label}</div>
      <div className="flex items-baseline gap-1">
        <span className="text-[20px] font-semibold" style={{ color }}>{value}</span>
        <span className="text-[10px] font-mono" style={{ color: "var(--muted)" }}>{sub}</span>
      </div>
    </div>
  );
}

function Gauge({ label, value, unit, max, color }: { label: string; value: number; unit: string; max: number; color: string }) {
  const frac = Math.max(0, Math.min(1, value / max));
  return (
    <div className="mb-4">
      <div className="flex justify-between items-center mb-1">
        <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>{label}</span>
        <span className="text-[12px] font-mono text-white">{value.toFixed(1)}{unit}</span>
      </div>
      <div className="h-1.5 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,.09)" }}>
        <div className="h-full rounded-full transition-all duration-500" style={{ width: `${frac * 100}%`, background: color }} />
      </div>
    </div>
  );
}

function Chart({
  title,
  valueLabel,
  data,
  color,
  domainMax,
  threshold,
}: {
  title: string;
  valueLabel: string;
  data: number[];
  color: string;
  domainMax: number;
  threshold?: number;
}) {
  const W = 520;
  const H = 130;
  const pts = data.length < 2 ? [] : data.map((v, i) => {
    const x = (i / (data.length - 1)) * W;
    const y = H - (Math.max(0, Math.min(domainMax, v)) / domainMax) * H;
    return `${i === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(1)}`;
  });
  const line = pts.join(" ");
  const area = pts.length ? `${line} L${W} ${H} L0 ${H} Z` : "";
  const thrY = threshold != null ? H - (threshold / domainMax) * H : null;

  return (
    <div className="rounded-xl border p-4" style={{ borderColor: "var(--border)", background: "var(--surf)" }}>
      <div className="flex items-center justify-between mb-2">
        <span className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>{title}</span>
        <span className="text-[16px] font-semibold" style={{ color }}>{valueLabel}</span>
      </div>
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full block" style={{ height: 130 }}>
        {[0.25, 0.5, 0.75].map((g) => (
          <line key={g} x1={0} y1={H * g} x2={W} y2={H * g} stroke="rgba(255,255,255,0.05)" />
        ))}
        {thrY != null && (
          <line x1={0} y1={thrY} x2={W} y2={thrY} stroke="#34D399" strokeOpacity={0.5} strokeDasharray="5 4" />
        )}
        {area && <path d={area} fill={color} fillOpacity={0.1} />}
        {line && <path d={line} fill="none" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />}
      </svg>
    </div>
  );
}
