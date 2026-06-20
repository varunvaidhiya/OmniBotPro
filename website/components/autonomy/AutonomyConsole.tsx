"use client";

/*
 * AutonomyConsole — the OhhO Autonomy application shell.
 *
 * Mission state machine + NL agent (left), the live SLAM map with the robot
 * driving a planned Nav2 path (centre), and the control-mode mux + Nav2
 * status (right). Type an instruction, hit go, watch it execute.
 */

import { useState, useEffect } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  Map as MapIcon,
  Play,
  RotateCcw,
  Sparkles,
  Send,
  Navigation,
  CheckCircle2,
} from "lucide-react";

import { useMission, PATH, type ControlMode, type MissionStep } from "@/lib/autonomy/mission";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";

const VIOLET = "#A78BFA";
const CYAN = "#00D4FF";
const GREEN = "#34D399";
const AMBER = "#FBBF24";

const MODES: { id: ControlMode; label: string }[] = [
  { id: "nav2", label: "nav2" },
  { id: "vla", label: "vla" },
  { id: "rl_nav", label: "rl_nav" },
  { id: "teleop", label: "teleop" },
];

// map drawing area in SVG user units
const MW = 460;
const MH = 360;

export default function AutonomyConsole() {
  const { config } = useRobot();
  const m = useMission(config);
  const [draft, setDraft] = useState(m.instruction);

  // Keep the input in sync with the robot-derived default instruction.
  useEffect(() => setDraft(m.instruction), [m.instruction]);

  const rx = m.robot.x * MW;
  const ry = m.robot.y * MH;
  const pathD = PATH.map((p, i) => `${i === 0 ? "M" : "L"}${(p.x * MW).toFixed(1)} ${(p.y * MH).toFixed(1)}`).join(" ");

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{ background: "rgba(10,14,26,.86)", backdropFilter: "blur(18px)", borderColor: "var(--border)" }}
      >
        <Link href="/products/autonomy" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0" style={{ color: VIOLET }}>
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Autonomy</span>
        </Link>
        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />
        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <MapIcon size={14} /> mission control
        </span>
        <span className="ml-auto flex items-center gap-2">
          <span
            className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{
              background: m.phase === "running" ? "rgba(124,58,237,.16)" : "rgba(255,255,255,.04)",
              color: m.phase === "running" ? VIOLET : m.phase === "done" ? GREEN : "var(--muted)",
              border: `1px solid ${m.phase === "running" ? "rgba(124,58,237,.4)" : "var(--border)"}`,
            }}
          >
            <span className="badge-dot" style={{ background: m.phase === "running" ? VIOLET : m.phase === "done" ? GREEN : "var(--muted)", width: 6, height: 6 }} />
            {m.phase === "running" ? "EXECUTING" : m.phase === "done" ? "MISSION COMPLETE" : "READY · LOCALIZED"}
          </span>
        </span>
      </header>

      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_290px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Mission + agent */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Mission</h2>
            <div className="flex flex-col">
              {m.steps.map((s, i) => (
                <StepRow key={s.id} step={s} last={i === m.steps.length - 1} />
              ))}
            </div>
          </div>

          <div className="p-5 mt-auto">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3 flex items-center gap-2" style={{ color: VIOLET }}>
              <Sparkles size={13} /> Agent · Claude
            </h2>
            <textarea
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              rows={3}
              className="w-full text-[12.5px] rounded-lg p-3 resize-none outline-none"
              style={{ background: "var(--bg)", border: "1px solid var(--border-med)", color: "#fff" }}
              placeholder="Tell the robot what to do…"
            />
            <button
              onClick={() => m.submit(draft)}
              className="mt-2 w-full inline-flex items-center justify-center gap-2 text-[12px] font-semibold px-4 py-2 rounded-lg transition-all hover:bg-white/[0.05]"
              style={{ border: `1px solid ${VIOLET}`, color: VIOLET }}
            >
              <Send size={13} /> Plan mission
            </button>
            <p className="text-[10.5px] mt-2" style={{ color: "var(--muted)" }}>
              Turns plain language into a navigate → perceive{config.capabilities.canManipulate ? " → manipulate" : ""} plan.
            </p>
          </div>

          <div className="p-4 border-t mt-auto" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="autonomy" config={config} />
          </div>
        </aside>

        {/* CENTER — SLAM map */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b flex items-center justify-between flex-wrap gap-3" style={{ borderColor: "var(--border)" }}>
            <div>
              <h1 className="text-xl font-medium text-white mb-1 flex items-center gap-2">
                <Navigation size={20} color={VIOLET} /> SLAM map · Nav2
              </h1>
              <p className="text-sm text-gray-400">Planned path over the costmap; the robot drives it autonomously.</p>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={m.start}
                disabled={m.phase === "running"}
                className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2 rounded-lg transition-all disabled:opacity-40"
                style={{ background: VIOLET, color: "var(--bg)" }}
              >
                <Play size={15} /> {m.phase === "done" ? "Run again" : "Start mission"}
              </button>
              <button
                onClick={m.reset}
                className="inline-flex items-center gap-2 text-[12px] font-semibold px-3 py-2 rounded-lg transition-all hover:bg-white/[0.05]"
                style={{ border: "1px solid var(--border-med)", color: "var(--muted)" }}
              >
                <RotateCcw size={13} />
              </button>
            </div>
          </div>

          <div className="flex-1 p-6 flex items-center justify-center">
            <div className="w-full max-w-[560px] rounded-2xl border overflow-hidden" style={{ borderColor: "var(--border)", background: "#0A1424" }}>
              <svg viewBox={`0 0 ${MW} ${MH}`} className="w-full block">
                {/* grid */}
                {Array.from({ length: 10 }).map((_, i) => (
                  <line key={`h${i}`} x1={0} y1={(MH / 10) * i} x2={MW} y2={(MH / 10) * i} stroke="rgba(255,255,255,0.05)" />
                ))}
                {Array.from({ length: 13 }).map((_, i) => (
                  <line key={`v${i}`} x1={(MW / 13) * i} y1={0} x2={(MW / 13) * i} y2={MH} stroke="rgba(255,255,255,0.05)" />
                ))}
                {/* occupancy walls */}
                <g fill="rgba(255,255,255,0.08)">
                  <rect x={40} y={40} width={230} height={20} rx={4} />
                  <rect x={360} y={40} width={20} height={150} rx={4} />
                  <rect x={40} y={290} width={150} height={20} rx={4} />
                </g>
                {/* costmap obstacle */}
                <circle cx={MW * 0.52} cy={MH * 0.56} r={26} fill={AMBER} fillOpacity={0.16} stroke={AMBER} strokeDasharray="4 4" />
                {/* planned path */}
                <path d={pathD} fill="none" stroke={VIOLET} strokeWidth={3} strokeDasharray="7 5" strokeLinecap="round" />
                {/* waypoints */}
                {PATH.slice(1, -1).map((p, i) => (
                  <circle key={i} cx={p.x * MW} cy={p.y * MH} r={3.5} fill={VIOLET} />
                ))}
                {/* goal */}
                <g>
                  <circle cx={PATH[PATH.length - 1].x * MW} cy={PATH[PATH.length - 1].y * MH} r={9} fill="none" stroke={GREEN} strokeWidth={2} />
                  <circle cx={PATH[PATH.length - 1].x * MW} cy={PATH[PATH.length - 1].y * MH} r={3} fill={GREEN} />
                </g>
                {/* robot */}
                <g style={{ transition: "transform 120ms linear" }} transform={`translate(${rx} ${ry})`}>
                  <circle r={16} fill={VIOLET} fillOpacity={0.14} />
                  <rect x={-10} y={-10} width={20} height={20} rx={4} fill="#18233f" stroke={VIOLET} strokeWidth={2} />
                  <circle r={3} fill={VIOLET} />
                </g>
              </svg>
            </div>
          </div>
        </section>

        {/* RIGHT — Mode mux + Nav2 */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--muted)" }}>Control mode</h2>
            <p className="text-[11px] mb-4" style={{ color: "var(--muted)" }}>mux arbitrates the wheels</p>
            <div className="flex flex-col gap-2">
              {MODES.map((mode) => {
                const active = m.mode === mode.id;
                return (
                  <button
                    key={mode.id}
                    onClick={() => m.setMode(mode.id)}
                    className="flex items-center justify-between px-3 py-2.5 rounded-lg transition-all"
                    style={{
                      background: active ? "rgba(124,58,237,.14)" : "rgba(255,255,255,.02)",
                      border: `1px solid ${active ? VIOLET : "var(--border)"}`,
                    }}
                  >
                    <span className="text-[12.5px] font-mono flex items-center gap-2" style={{ color: active ? "#fff" : "var(--muted)" }}>
                      <span className="w-2 h-2 rounded-full" style={{ background: active ? VIOLET : "var(--muted)" }} />
                      {mode.label}
                    </span>
                    {active && <span className="text-[10px] font-mono" style={{ color: VIOLET }}>active</span>}
                  </button>
                );
              })}
            </div>
          </div>

          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4 flex items-center gap-2" style={{ color: "var(--muted)" }}>
              <Navigation size={13} /> Nav2
            </h2>
            <Row label="status" value={m.phase === "running" ? "navigating" : m.phase === "done" ? "arrived" : "idle"} color={m.phase === "running" ? GREEN : "var(--muted)"} />
            <Row label="dist to goal" value={`${m.distToGoal.toFixed(1)} m`} />
            <Row label="ETA" value={m.phase === "done" ? "0 s" : `${m.eta} s`} />
            <Row label="progress" value={`${Math.round(m.progress * 100)}%`} color={VIOLET} />
            <div className="mt-3 h-1.5 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,.09)" }}>
              <div className="h-full rounded-full transition-all duration-150" style={{ width: `${m.progress * 100}%`, background: VIOLET }} />
            </div>
          </div>

          {m.phase === "done" && (
            <div className="p-5">
              <div className="rounded-xl border p-4 flex items-center gap-3" style={{ borderColor: "rgba(52,211,153,.4)", background: "rgba(52,211,153,.08)" }}>
                <CheckCircle2 size={18} color={GREEN} />
                <span className="text-[12px]" style={{ color: GREEN }}>Mission complete — all steps executed.</span>
              </div>
            </div>
          )}

          <div className="p-5 mt-auto">
            <Row label="localization" value="EKF · odom+IMU" />
            <Row label="map" value="2-D + 3-D SLAM" />
          </div>
        </aside>
      </div>
    </div>
  );
}

function StepRow({ step, last }: { step: MissionStep; last: boolean }) {
  const color = step.status === "done" ? GREEN : step.status === "running" ? VIOLET : "rgba(255,255,255,0.28)";
  return (
    <div className="flex gap-3">
      <div className="flex flex-col items-center">
        <span className="w-3.5 h-3.5 rounded-full mt-1.5 flex items-center justify-center" style={{ background: step.status === "done" ? GREEN : "transparent", border: `2px solid ${color}` }} />
        {!last && <span className="w-px flex-1 my-1" style={{ background: "var(--border-med)" }} />}
      </div>
      <div className="pb-4">
        <div className="text-[13px] font-mono" style={{ color: step.status === "queued" ? "var(--muted)" : "#fff" }}>{step.label}</div>
        <div className="text-[10px] font-mono mt-0.5" style={{ color }}>
          {step.status === "running" ? "running" : step.status === "done" ? "done" : "queued"} · {step.kind}
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="flex justify-between items-center py-1.5 text-[12px]">
      <span className="font-mono" style={{ color: "var(--muted)" }}>{label}</span>
      <span className="font-mono" style={{ color: color ?? "#fff" }}>{value}</span>
    </div>
  );
}
