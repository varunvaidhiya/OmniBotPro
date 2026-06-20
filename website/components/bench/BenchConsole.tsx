"use client";

/*
 * BenchConsole — the OhhO Bench application shell.
 *
 * A simulated hardware bring-up cockpit: assembly checklist (left), the
 * firmware flash + hardware self-test board (centre), and the wiring/port
 * map + calibration routines (right). When every stage is green, Bench
 * "writes the deployment profile" the rest of the stack consumes.
 */

import Link from "next/link";
import {
  ArrowLeft,
  Wrench,
  Cpu,
  CircuitBoard,
  Check,
  Loader2,
  Play,
  Zap,
  Crosshair,
  CheckCircle2,
} from "lucide-react";

import { useBringup, type Status } from "@/lib/bench/bringup";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";

const CYAN = "#00D4FF";
const GREEN = "#34D399";
const AMBER = "#FBBF24";

function dot(status: Status): string {
  return status === "done"
    ? GREEN
    : status === "active"
      ? CYAN
      : status === "fail"
        ? "#F87171"
        : "rgba(255,255,255,0.28)";
}

function StatusGlyph({ status }: { status: Status }) {
  if (status === "done") return <Check size={13} color={GREEN} />;
  if (status === "active") return <Loader2 size={13} color={CYAN} className="animate-spin" />;
  return <span className="w-[13px] h-[13px] rounded-full border" style={{ borderColor: dot(status) }} />;
}

export default function BenchConsole() {
  const { config } = useRobot();
  const {
    assembly,
    subsystems,
    calibrations,
    firmware,
    testing,
    ready,
    progress,
    toggleStep,
    flashFirmware,
    runSelfTest,
    runCalibration,
  } = useBringup(config);

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{ background: "rgba(10,14,26,.86)", backdropFilter: "blur(18px)", borderColor: "var(--border)" }}
      >
        <Link href="/products/bench" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0" style={{ color: CYAN }}>
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Bench</span>
        </Link>
        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />
        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Wrench size={14} /> warehouse-amr · bring-up
        </span>
        <span className="ml-auto flex items-center gap-2">
          <span
            className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{
              background: ready ? "rgba(52,211,153,.12)" : "rgba(255,255,255,.04)",
              color: ready ? GREEN : "var(--muted)",
              border: `1px solid ${ready ? "rgba(52,211,153,.4)" : "var(--border)"}`,
            }}
          >
            {ready ? <CheckCircle2 size={11} /> : <Cpu size={11} />}
            {ready ? "DEPLOY PROFILE WRITTEN" : "BRING-UP IN PROGRESS"}
          </span>
        </span>
      </header>

      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Assembly checklist */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="bench" config={config} />
          </div>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--muted)" }}>Assembly</h2>
            <p className="text-[11px] mb-4" style={{ color: "var(--muted)" }}>From your OhhO Build BOM · tap to toggle</p>
            <Meter label="Assembled" frac={progress.assembly} />
          </div>
          <div className="flex flex-col">
            {assembly.map((s) => (
              <button
                key={s.id}
                onClick={() => toggleStep(s.id)}
                className="text-left px-5 py-3.5 border-b transition-colors hover:bg-white/[0.03] flex items-start gap-3"
                style={{ borderColor: "var(--border)" }}
              >
                <span className="mt-0.5"><StatusGlyph status={s.status} /></span>
                <span className="flex-1">
                  <span className="block text-[13px]" style={{ color: s.status === "pending" ? "var(--muted)" : "#fff" }}>{s.label}</span>
                  <span className="block text-[10.5px] font-mono mt-0.5" style={{ color: "var(--muted)" }}>{s.detail}</span>
                </span>
              </button>
            ))}
          </div>
        </aside>

        {/* CENTER — Firmware + self-test */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b flex items-center justify-between flex-wrap gap-3" style={{ borderColor: "var(--border)" }}>
            <div>
              <h1 className="text-xl font-medium text-white mb-1 flex items-center gap-2">
                <CircuitBoard size={20} color={CYAN} /> Firmware &amp; self-test
              </h1>
              <p className="text-sm text-gray-400">Flash the controllers, then prove every subsystem before any autonomy runs.</p>
            </div>
            <button
              onClick={runSelfTest}
              disabled={testing || firmware !== "done"}
              className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2 rounded-lg transition-all disabled:opacity-40"
              style={{ background: CYAN, color: "var(--bg)" }}
            >
              {testing ? <Loader2 size={15} className="animate-spin" /> : <Play size={15} />}
              Run self-test
            </button>
          </div>

          <div className="flex-1 overflow-y-auto p-6 flex flex-col gap-6">
            {/* Firmware flash */}
            <div className="rounded-xl border p-5" style={{ borderColor: "var(--border)", background: "var(--surf)" }}>
              <div className="flex items-center justify-between mb-3">
                <span className="text-[12px] font-mono uppercase tracking-wider flex items-center gap-2" style={{ color: "var(--muted)" }}>
                  <Zap size={13} /> Firmware
                </span>
                <span className="text-[11px] font-mono" style={{ color: dot(firmware) }}>
                  {firmware === "done" ? "FLASHED" : firmware === "active" ? "FLASHING…" : "NOT FLASHED"}
                </span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <div className="text-[12px] font-mono" style={{ color: "var(--muted)" }}>
                  motor board · car-type X3 · MCU app v2.1
                </div>
                <button
                  onClick={flashFirmware}
                  disabled={firmware !== "pending"}
                  className="inline-flex items-center gap-2 text-[12px] font-semibold px-3.5 py-1.5 rounded-lg transition-all disabled:opacity-40"
                  style={{ border: `1px solid ${CYAN}`, color: CYAN }}
                >
                  {firmware === "active" ? <Loader2 size={13} className="animate-spin" /> : <Zap size={13} />}
                  Flash
                </button>
              </div>
            </div>

            {/* Self-test grid */}
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>Hardware self-test</span>
                <Meter label="" frac={progress.tests} compact />
              </div>
              <div className="grid sm:grid-cols-2 gap-3">
                {subsystems.map((s) => (
                  <div
                    key={s.id}
                    className="rounded-xl border p-4 flex items-center gap-3 transition-colors"
                    style={{
                      borderColor: s.status === "done" ? "rgba(52,211,153,.4)" : s.status === "active" ? CYAN : "var(--border)",
                      background: "var(--surf)",
                    }}
                  >
                    <span className="w-9 h-9 rounded-lg flex items-center justify-center" style={{ background: "rgba(255,255,255,.04)", border: "1px solid var(--border)" }}>
                      <StatusGlyph status={s.status} />
                    </span>
                    <div className="flex-1">
                      <div className="flex items-center justify-between">
                        <span className="text-[13px] text-white">{s.label}</span>
                        <span className="text-[10px] font-mono" style={{ color: dot(s.status) }}>
                          {s.status === "done" ? "OK" : s.status === "active" ? "…" : "—"}
                        </span>
                      </div>
                      <div className="text-[10.5px] font-mono mt-0.5" style={{ color: "var(--muted)" }}>{s.port}</div>
                      <div className="text-[10.5px] mt-0.5" style={{ color: "var(--muted)" }}>{s.detail}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>

        {/* RIGHT — Wiring + calibration */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Wiring · port map</h2>
            <div className="flex flex-col gap-2.5">
              {[
                { l: "motor board", p: "USB0 · 115200" },
                { l: "arm bus", p: "ACM0 · 1 Mbd" },
                { l: "cameras", p: "uvc · /dev/video*" },
                { l: "battery", p: "12 V · power rail" },
              ].map((w) => (
                <div key={w.l} className="flex items-center justify-between text-[11.5px]">
                  <span className="font-mono flex items-center gap-2 text-white">
                    <span className="w-1.5 h-1.5 rounded-full" style={{ background: CYAN }} />
                    {w.l}
                  </span>
                  <span className="font-mono" style={{ color: "var(--muted)" }}>{w.p}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-[12px] font-mono uppercase tracking-wider flex items-center gap-2" style={{ color: "var(--muted)" }}>
                <Crosshair size={13} /> Calibration
              </h2>
              <span className="text-[10px] font-mono" style={{ color: progress.cal === 1 ? GREEN : AMBER }}>
                {Math.round(progress.cal * 100)}%
              </span>
            </div>
            <div className="flex flex-col gap-2.5">
              {calibrations.map((c) => (
                <div key={c.id} className="flex items-center gap-3">
                  <span><StatusGlyph status={c.status} /></span>
                  <div className="flex-1">
                    <div className="text-[12px]" style={{ color: c.status === "pending" ? "var(--muted)" : "#fff" }}>{c.label}</div>
                    <div className="text-[10px] font-mono" style={{ color: "var(--muted)" }}>{c.result}</div>
                  </div>
                  <button
                    onClick={() => runCalibration(c.id)}
                    disabled={c.status !== "pending"}
                    className="text-[10.5px] font-mono px-2.5 py-1 rounded-md transition-all disabled:opacity-40"
                    style={{ border: `1px solid ${CYAN}`, color: CYAN }}
                  >
                    {c.status === "done" ? "✓" : c.status === "active" ? "…" : "run"}
                  </button>
                </div>
              ))}
            </div>
          </div>

          <div className="p-5">
            <div
              className="rounded-xl border p-4 text-center"
              style={{
                borderColor: ready ? "rgba(52,211,153,.4)" : "var(--border)",
                background: ready ? "rgba(52,211,153,.08)" : "rgba(255,255,255,.02)",
              }}
            >
              <div className="text-[12px] font-mono uppercase tracking-wider mb-1" style={{ color: ready ? GREEN : "var(--muted)" }}>
                Deployment profile
              </div>
              <div className="text-[12px]" style={{ color: "var(--muted)" }}>
                {ready
                  ? "Written — Frame, View & Autonomy can come up on calibrated hardware."
                  : "Completes when assembly, firmware, self-test and calibration are all green."}
              </div>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}

function Meter({ label, frac, compact }: { label: string; frac: number; compact?: boolean }) {
  return (
    <div className={compact ? "w-28" : ""}>
      {label && (
        <div className="flex justify-between text-[10px] font-mono mb-1" style={{ color: "var(--muted)" }}>
          <span>{label}</span>
          <span>{Math.round(frac * 100)}%</span>
        </div>
      )}
      <div className="h-1.5 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,.09)" }}>
        <div className="h-full rounded-full transition-all duration-500" style={{ width: `${frac * 100}%`, background: CYAN }} />
      </div>
    </div>
  );
}
