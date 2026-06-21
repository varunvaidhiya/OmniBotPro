"use client";

/*
 * TwinConsole — the OhhO Twin application shell.
 *
 * Three surfaces: sim world + robot position (left), replay timeline +
 * what-if controls (centre), and predictions + sync metrics (right).
 * Consumes the live connection via useRobotConnection() to overlay the
 * real robot's telemetry on the simulated twin.
 */

import Link from "next/link";
import { useState } from "react";
import {
  ArrowLeft,
  Play,
  Pause,
  RotateCcw,
  GitBranch,
  Activity,
  Radio,
  Cpu,
  Zap,
} from "lucide-react";

import { useTwinSimulation, PREDICTIONS, WHATIF_RESULTS, TWIN_STATS, type TwinState } from "@/lib/twin/twin";
import { useRobot } from "@/lib/garage/RobotContext";
import { useRobotConnection } from "@/lib/connect/RobotConnectionProvider";
import { AIRobotPanel } from "@/components/console-kit";
import PaidFeatureGate from "@/components/auth/PaidFeatureGate";

const VIOLET = "#A78BFA";
const VIOLET_DIM = "rgba(167,139,250,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

const W = 220;
const H = 134;

export default function TwinConsole() {
  const { config } = useRobot();
  const conn = useRobotConnection();
  const [running, setRunning] = useState(true);
  const [showWhatIf, setShowWhatIf] = useState(false);
  const twin = useTwinSimulation(running);

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{
          background: "rgba(10,14,26,.86)",
          backdropFilter: "blur(18px)",
          borderColor: "var(--border)",
        }}
      >
        <Link
          href="/products/twin"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: VIOLET }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Twin</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Cpu size={14} /> {TWIN_STATS.simWorld} · live-mirror
        </span>

        <span className="ml-auto flex items-center gap-2">
          <button
            onClick={() => setRunning((v) => !v)}
            className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full transition-colors"
            style={{
              background: running ? "rgba(52,211,153,0.1)" : "rgba(255,255,255,0.03)",
              color: running ? GREEN : "var(--muted)",
              border: `1px solid ${running ? GREEN + "40" : "var(--border)"}`,
            }}
          >
            {running ? <Pause size={11} /> : <Play size={11} />}
            {running ? "LIVE" : "PAUSED"}
          </button>
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: VIOLET_DIM, color: VIOLET, border: `1px solid ${VIOLET}40` }}
          >
            <Activity size={11} />
            {Math.round(TWIN_STATS.matchScore * 100)}% MATCH
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[260px_1fr_300px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Sim world */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="twin" config={config} />
          </div>

          <div className="p-4">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Sim world
            </h2>
            <SimWorld state={twin.state} liveState={twin.liveState} isLive={running && twin.replayIndex === null} connectedRobot={conn} />
          </div>

          <div className="p-4 border-t" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Live telemetry
            </h2>
            <TelemetryGrid state={twin.state} />
          </div>
        </aside>

        {/* CENTER — Replay + what-if */}
        <section className="flex flex-col overflow-y-auto" style={{ background: "var(--bg)" }}>
          {/* Replay timeline */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>
                Replay timeline
              </h2>
              <div className="flex items-center gap-2">
                <span className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>
                  {twin.frameCount} frames
                </span>
                <button
                  onClick={twin.clearReplay}
                  className="inline-flex items-center gap-1 text-[11px] font-mono px-2 py-1 rounded-md"
                  style={{ background: "rgba(255,255,255,0.03)", border: "1px solid var(--border)", color: "var(--muted)" }}
                >
                  <RotateCcw size={11} /> Clear
                </button>
              </div>
            </div>

            {/* Timeline */}
            <div className="relative h-12 rounded-lg overflow-hidden" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
              <div className="absolute inset-0 flex items-center px-3">
                <div className="w-full h-1 rounded-full" style={{ background: "rgba(255,255,255,0.05)" }} />
              </div>
              {twin.frameCount > 0 && (
                <input
                  type="range"
                  min={0}
                  max={twin.frameCount - 1}
                  value={twin.replayIndex ?? twin.frameCount - 1}
                  onChange={(e) => twin.scrubTo(parseInt(e.target.value, 10))}
                  className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                />
              )}
              <div
                className="absolute top-0 bottom-0 w-0.5"
                style={{
                  left: twin.frameCount > 0 ? `${((twin.replayIndex ?? twin.frameCount - 1) / (twin.frameCount - 1 || 1)) * 100}%` : "100%",
                  background: VIOLET,
                  boxShadow: `0 0 8px ${VIOLET}`,
                }}
              />
            </div>
            <div className="flex justify-between mt-2">
              <span className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>
                {twin.replayIndex !== null ? `Frame ${twin.replayIndex}/${twin.frameCount - 1}` : "Live (scrub to replay)"}
              </span>
              <span className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>
                {twin.isRecording ? "● Recording" : "⏸ Paused"}
              </span>
            </div>
          </div>

          {/* What-if */}
          <div className="p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>
                What-if simulation
              </h2>
              <PaidFeatureGate
                feature="cloud-simulation"
                label="Cloud Simulation"
                description="What-if scenarios run on OhhO cloud — requires a plan"
              >
                <button
                  onClick={() => setShowWhatIf((v) => !v)}
                  className="inline-flex items-center gap-1.5 text-[11px] font-semibold px-3 py-1.5 rounded-lg transition-all"
                  style={{ background: VIOLET_DIM, color: VIOLET, border: `1px solid ${VIOLET}40` }}
                >
                  <GitBranch size={12} />
                  {showWhatIf ? "Hide" : "Run what-if"}
                </button>
              </PaidFeatureGate>
            </div>

            {showWhatIf ? (
              <div className="flex flex-col gap-3">
                {WHATIF_RESULTS.map((w) => (
                  <div key={w.scenario} className="px-4 py-3 rounded-lg" style={{ background: VIOLET_DIM, border: `1px solid ${VIOLET}30` }}>
                    <div className="flex items-center gap-2 mb-2">
                      <GitBranch size={13} style={{ color: VIOLET }} />
                      <span className="text-[13px] font-semibold">{w.scenario}</span>
                    </div>
                    <p className="text-[12px] mb-2" style={{ color: "var(--muted)" }}>{w.change}</p>
                    <div className="flex items-center gap-3">
                      <span className="text-[12px]" style={{ color: "rgba(255,255,255,0.8)" }}>{w.outcome}</span>
                      <span className="text-[12px] font-mono" style={{ color: w.successRate > 0.85 ? GREEN : AMBER }}>
                        {Math.round(w.successRate * 100)}% success
                      </span>
                      <span className="text-[11px] font-mono" style={{ color: w.delta > 0 ? GREEN : RED }}>
                        {w.delta > 0 ? "+" : ""}{Math.round(w.delta * 100)}%
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-[13px] leading-[1.6]" style={{ color: "var(--muted)" }}>
                Branch from any recorded state and simulate a different outcome — a different policy, a different grasp, a different speed — without touching the real robot. Scrub the timeline to a moment, then run a what-if.
              </p>
            )}
          </div>
        </section>

        {/* RIGHT — Predictions + sync */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          {/* Sync metrics */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Sim ↔ Real sync
            </h2>
            <div className="flex flex-col items-center mb-4">
              <div className="relative w-28 h-28">
                <svg viewBox="0 0 100 100" className="transform -rotate-90">
                  <circle cx="50" cy="50" r="40" fill="none" stroke="rgba(255,255,255,0.05)" strokeWidth="8" />
                  <circle cx="50" cy="50" r="40" fill="none" stroke={GREEN} strokeWidth="8" strokeDasharray={`${TWIN_STATS.matchScore * 251.2} 251.2`} strokeLinecap="round" />
                </svg>
                <div className="absolute inset-0 flex flex-col items-center justify-center">
                  <span className="text-[20px] font-display font-semibold">{Math.round(TWIN_STATS.matchScore * 100)}%</span>
                  <span className="text-[8px] font-mono tracking-wider" style={{ color: GREEN }}>MATCH</span>
                </div>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Drift</div>
                <div className="text-[14px] font-display font-semibold mt-1">{TWIN_STATS.driftCm} cm</div>
              </div>
              <div>
                <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Storage</div>
                <div className="text-[14px] font-display font-semibold mt-1">{TWIN_STATS.replayStorageMb} GB</div>
              </div>
            </div>
          </div>

          {/* Predictions */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Predictions
            </h2>
            <div className="flex flex-col gap-3">
              {PREDICTIONS.map((p) => {
                const c = p.urgency === "high" ? RED : p.urgency === "medium" ? AMBER : GREEN;
                return (
                  <div key={p.label} className="px-3 py-2.5 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: `1px solid ${c}30` }}>
                    <div className="flex items-center gap-2 mb-1.5">
                      <span className="w-2 h-2 rounded-full" style={{ background: c }} />
                      <span className="text-[12px] font-medium flex-1">{p.label}</span>
                    </div>
                    <div className="flex items-center justify-between text-[11px] font-mono" style={{ color: "var(--muted)" }}>
                      <span>{p.current} → <span style={{ color: c }}>{p.predicted}</span></span>
                    </div>
                    <div className="text-[10px] font-mono mt-1" style={{ color: "var(--faint)" }}>
                      ~{p.hoursToThreshold}h to threshold
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Connection */}
          <div className="p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Live source
            </h2>
            {conn.isConnected && conn.robot ? (
              <div className="flex flex-col gap-2">
                <div className="flex items-center gap-2">
                  <Radio size={14} style={{ color: GREEN }} className="animate-pulse" />
                  <span className="text-[13px] font-semibold">{conn.robot.name}</span>
                </div>
                <div className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
                  Streaming telemetry → twin
                </div>
              </div>
            ) : (
              <Link
                href="/garage"
                className="inline-flex items-center gap-1.5 text-[12px] font-semibold px-3 py-1.5 rounded-lg"
                style={{ background: VIOLET_DIM, color: VIOLET, border: `1px solid ${VIOLET}40` }}
              >
                <Zap size={13} /> Connect a robot
              </Link>
            )}
          </div>
        </aside>
      </div>
    </div>
  );
}

function SimWorld({ state, liveState, isLive, connectedRobot }: {
  state: TwinState;
  liveState: TwinState;
  isLive: boolean;
  connectedRobot: ReturnType<typeof useRobotConnection>;
}) {
  return (
    <div className="relative w-full rounded-xl overflow-hidden" style={{ background: "#0A1424", border: "1px solid var(--border)", aspectRatio: "220/134" }}>
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" height="100%">
        {/* Grid */}
        {[0, 1, 2, 3].map((i) => (
          <line key={`h${i}`} x1={0} y1={i * 35} x2={W} y2={i * 35} stroke="rgba(255,255,255,0.05)" />
        ))}
        {[0, 1, 2, 3, 4, 5].map((i) => (
          <line key={`v${i}`} x1={i * 37} y1={0} x2={i * 37} y2={H} stroke="rgba(255,255,255,0.05)" />
        ))}

        {/* Obstacles */}
        <rect x={30} y={20} width={40} height={8} rx={2} fill="rgba(255,255,255,0.08)" />
        <rect x={150} y={20} width={40} height={8} rx={2} fill="rgba(255,255,255,0.08)" />
        <rect x={30} y={100} width={40} height={8} rx={2} fill="rgba(255,255,255,0.08)" />

        {/* Sim robot (twin) */}
        <g transform={`translate(${state.x} ${state.y}) rotate(${(state.theta * 180) / Math.PI})`}>
          <rect x={-10} y={-10} width={20} height={20} rx={4} fill="rgba(167,139,250,0.2)" stroke={VIOLET} strokeWidth={1.5} />
          <line x1={0} y1={0} x2={0} y2={-14} stroke={VIOLET} strokeWidth={2} strokeLinecap="round" />
        </g>

        {/* Real robot (live overlay) */}
        {isLive && (
          <g transform={`translate(${liveState.x} ${liveState.y}) rotate(${(liveState.theta * 180) / Math.PI})`}>
            <rect x={-10} y={-10} width={20} height={20} rx={4} fill="none" stroke={GREEN} strokeWidth={1.2} strokeDasharray="3 2" />
          </g>
        )}

        {/* Legend */}
        <text x={6} y={128} fontSize={6} fill={GREEN} fontFamily="monospace">● real</text>
        <text x={40} y={128} fontSize={6} fill={VIOLET} fontFamily="monospace">⋯ sim</text>
      </svg>
    </div>
  );
}

function TelemetryGrid({ state }: { state: TwinState }) {
  return (
    <div className="grid grid-cols-2 gap-2">
      <Metric label="X" value={state.x.toFixed(1)} />
      <Metric label="Y" value={state.y.toFixed(1)} />
      <Metric label="θ" value={`${((state.theta * 180) / Math.PI).toFixed(0)}°`} />
      <Metric label="Vx" value={state.vx.toFixed(2)} />
      <Metric label="Battery" value={`${Math.round(state.battery * 100)}%`} color={state.battery > 0.5 ? GREEN : AMBER} />
      <Metric label="Motors" value={`${state.motorTemps.length}`} />
      {state.motorTemps.slice(0, 3).map((mt, i) => (
        <Metric key={i} label={`M${i} temp`} value={`${mt.toFixed(0)}°C`} color={mt > 80 ? RED : mt > 65 ? AMBER : GREEN} />
      ))}
    </div>
  );
}

function Metric({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="px-2.5 py-2 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
      <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>{label}</div>
      <div className="text-[13px] font-display font-semibold mt-0.5 tabular-nums" style={{ color: color ?? "var(--text)" }}>{value}</div>
    </div>
  );
}
