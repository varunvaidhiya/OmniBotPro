"use client";

/*
 * MindConsole — the OhhO Mind application shell.
 *
 * The deliberative brain, live: the agent loop + goal entry (left), the
 * streaming reasoning transcript over a fused world-state snapshot (centre),
 * and the hybrid reasoning router + safety gate + memory/learning (right).
 * Give it a goal, press Run, and watch it perceive → reason → verify → act →
 * monitor → reflect → remember. Toggle offline to see the brain fall back to
 * on-device + NPU, or inject an unsafe action to watch the safety gate stop it.
 */

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import {
  ArrowLeft,
  Brain,
  Play,
  RotateCcw,
  Send,
  Sparkles,
  ShieldCheck,
  Cloud,
  Cpu,
  Wifi,
  WifiOff,
  Database,
  CheckCircle2,
  AlertTriangle,
} from "lucide-react";

import {
  useAgentLoop,
  LOOP_PHASES,
  type Backend,
  type LoopEvent,
  type LoopPhase,
  type Tone,
} from "@/lib/mind/agent";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";

const VIOLET = "#A78BFA";
const CYAN = "#00D4FF";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

const toneColor = (t: Tone): string =>
  t === "accent" ? VIOLET : t === "good" ? GREEN : t === "warn" ? AMBER : t === "bad" ? RED : "rgba(255,255,255,0.7)";

const BACKENDS: { id: Backend; label: string; icon: typeof Cloud }[] = [
  { id: "cloud", label: "cloud · claude", icon: Cloud },
  { id: "on_device", label: "on-device llm", icon: Cpu },
  { id: "deepx", label: "deepx npu", icon: Cpu },
];

export default function MindConsole() {
  const { config } = useRobot();
  const a = useAgentLoop();
  const [draft, setDraft] = useState(a.goal);
  const logRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    logRef.current?.scrollTo({ top: logRef.current.scrollHeight, behavior: "smooth" });
  }, [a.events.length]);

  const statusText =
    a.phase === "running" ? "THINKING" : a.phase === "done" ? "GOAL COMPLETE" : "IDLE";
  const statusColor = a.phase === "running" ? VIOLET : a.phase === "done" ? GREEN : "var(--muted)";

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{ background: "rgba(10,14,26,.86)", backdropFilter: "blur(18px)", borderColor: "var(--border)" }}
      >
        <Link href="/products/mind" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0" style={{ color: VIOLET }}>
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Mind</span>
        </Link>
        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />
        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Brain size={14} /> agent loop
        </span>
        <span className="ml-auto flex items-center gap-2">
          <span
            className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{
              background: a.phase === "running" ? "rgba(124,58,237,.16)" : "rgba(255,255,255,.04)",
              color: statusColor,
              border: `1px solid ${a.phase === "running" ? "rgba(124,58,237,.4)" : "var(--border)"}`,
            }}
          >
            <span className="badge-dot" style={{ background: statusColor, width: 6, height: 6 }} />
            {statusText}
          </span>
        </span>
      </header>

      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_290px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — loop + goal */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="mind" config={config} />
          </div>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Agent loop</h2>
            <div className="flex flex-col">
              {LOOP_PHASES.map((p, i) => (
                <PhaseRow key={p} phase={p} active={a.currentPhase === p} last={i === LOOP_PHASES.length - 1} />
              ))}
            </div>
            <p className="text-[10.5px] mt-3 font-mono" style={{ color: "var(--faint)" }}>
              monitor ↻ perceive · loops until the goal is met
            </p>
          </div>

          <div className="p-5 mt-auto">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3 flex items-center gap-2" style={{ color: VIOLET }}>
              <Sparkles size={13} /> Goal
            </h2>
            <textarea
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              rows={3}
              className="w-full text-[12.5px] rounded-lg p-3 resize-none outline-none"
              style={{ background: "var(--bg)", border: "1px solid var(--border-med)", color: "#fff" }}
              placeholder="Give the robot a goal, not a script…"
            />
            <button
              onClick={() => { a.submit(draft); }}
              className="mt-2 w-full inline-flex items-center justify-center gap-2 text-[12px] font-semibold px-4 py-2 rounded-lg transition-all hover:bg-white/[0.05]"
              style={{ border: `1px solid ${VIOLET}`, color: VIOLET }}
            >
              <Send size={13} /> Set goal
            </button>

            {/* demo toggles */}
            <div className="mt-4 flex flex-col gap-2">
              <Toggle
                on={a.online}
                onClick={() => a.setOnline(!a.online)}
                onIcon={<Wifi size={13} />}
                offIcon={<WifiOff size={13} />}
                label={a.online ? "online · cloud brain" : "offline · on-device brain"}
              />
              <Toggle
                on={a.unsafe}
                danger
                onClick={() => a.setUnsafe(!a.unsafe)}
                onIcon={<AlertTriangle size={13} />}
                offIcon={<ShieldCheck size={13} />}
                label={a.unsafe ? "inject unsafe action" : "actions safe"}
              />
            </div>
          </div>
        </aside>

        {/* CENTER — world state + reasoning stream */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b flex items-center justify-between flex-wrap gap-3" style={{ borderColor: "var(--border)" }}>
            <div>
              <h1 className="text-xl font-medium text-white mb-1 flex items-center gap-2">
                <Brain size={20} color={VIOLET} /> Reasoning stream
              </h1>
              <p className="text-sm text-gray-400">The deliberative loop, grounded in the live world state.</p>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={a.start}
                disabled={a.phase === "running"}
                className="inline-flex items-center gap-2 text-[13px] font-semibold px-4 py-2 rounded-lg transition-all disabled:opacity-40"
                style={{ background: VIOLET, color: "var(--bg)" }}
              >
                <Play size={15} /> {a.phase === "done" ? "Run again" : "Run agent"}
              </button>
              <button
                onClick={a.reset}
                className="inline-flex items-center gap-2 text-[12px] font-semibold px-3 py-2 rounded-lg transition-all hover:bg-white/[0.05]"
                style={{ border: "1px solid var(--border-med)", color: "var(--muted)" }}
              >
                <RotateCcw size={13} />
              </button>
            </div>
          </div>

          {/* world-state snapshot */}
          <div className="px-6 pt-5">
            <div className="rounded-xl border p-4" style={{ borderColor: "var(--border)", background: "var(--surf)" }}>
              <div className="text-[10.5px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--faint)" }}>
                World state · /agent/world_state
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <Cell label="base" value={a.world.basePose} />
                <Cell label="arm" value={a.world.arm} />
                <Cell label="nearest" value={a.world.nearest} />
                <Cell label="mission" value={a.world.missionPhase} accent />
              </div>
            </div>
          </div>

          {/* transcript */}
          <div ref={logRef} className="flex-1 px-6 py-5 overflow-y-auto" style={{ maxHeight: "calc(100vh - 320px)" }}>
            {a.events.length === 0 ? (
              <div className="h-full min-h-[180px] flex flex-col items-center justify-center text-center gap-2" style={{ color: "var(--faint)" }}>
                <Brain size={26} />
                <p className="text-[13px] font-mono">Press “Run agent” to watch OhhO Mind think.</p>
                <p className="text-[11.5px] max-w-[320px]">Give it a goal; it perceives, reasons, verifies every action against hardware limits, acts, and reflects — on a loop.</p>
              </div>
            ) : (
              <div className="flex flex-col gap-2">
                {a.events.map((e) => (
                  <EventRow key={e.id} event={e} />
                ))}
              </div>
            )}
          </div>
        </section>

        {/* RIGHT — router + safety + memory */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--muted)" }}>Reasoning router</h2>
            <p className="text-[11px] mb-4" style={{ color: "var(--muted)" }}>hybrid · best available wins</p>
            <div className="flex flex-col gap-2">
              {BACKENDS.map((b) => {
                const isActive = a.activeBackend === b.id;
                const offlineCloud = b.id === "cloud" && !a.online;
                const Icon = b.icon;
                return (
                  <div
                    key={b.id}
                    className="flex items-center justify-between px-3 py-2.5 rounded-lg transition-all"
                    style={{
                      background: isActive ? "rgba(124,58,237,.14)" : "rgba(255,255,255,.02)",
                      border: `1px solid ${isActive ? VIOLET : "var(--border)"}`,
                      opacity: offlineCloud ? 0.45 : 1,
                    }}
                  >
                    <span className="text-[12.5px] font-mono flex items-center gap-2" style={{ color: isActive ? "#fff" : "var(--muted)" }}>
                      <Icon size={13} />
                      {b.label}
                    </span>
                    <span className="text-[10px] font-mono" style={{ color: isActive ? VIOLET : "var(--faint)" }}>
                      {isActive ? "active" : offlineCloud ? "offline" : "ready"}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4 flex items-center gap-2" style={{ color: "var(--muted)" }}>
              <ShieldCheck size={13} /> Safety gate
            </h2>
            <Row label="status" value={a.blocked > 0 ? "blocking unsafe" : "verifying"} color={a.blocked > 0 ? RED : GREEN} />
            <Row label="base limit" value="≤ 0.20 m/s" />
            <Row label="arm limit" value="≤ 0.05 rad" />
            <Row label="blocked" value={`${a.blocked}`} color={a.blocked > 0 ? RED : "var(--muted)"} />
          </div>

          <div className="p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4 flex items-center gap-2" style={{ color: "var(--muted)" }}>
              <Database size={13} /> Memory & learning
            </h2>
            <Row label="episodes" value={`${a.episodes}`} color={VIOLET} />
            <div className="mt-3 text-[10.5px] font-mono uppercase tracking-wider mb-2" style={{ color: "var(--faint)" }}>
              remembered
            </div>
            <div className="flex flex-col gap-1.5">
              {a.memory.slice(0, 5).map((fact) => (
                <div key={fact} className="text-[12px] font-mono px-2.5 py-1.5 rounded-md" style={{ background: "rgba(255,255,255,.03)", border: "1px solid var(--border)", color: "rgba(255,255,255,.78)" }}>
                  {fact}
                </div>
              ))}
            </div>
          </div>

          {a.phase === "done" && (
            <div className="p-5 mt-auto">
              <div className="rounded-xl border p-4 flex items-center gap-3" style={{ borderColor: "rgba(52,211,153,.4)", background: "rgba(52,211,153,.08)" }}>
                <CheckCircle2 size={18} color={GREEN} />
                <span className="text-[12px]" style={{ color: GREEN }}>Goal complete — episode stored & memory updated.</span>
              </div>
            </div>
          )}
        </aside>
      </div>
    </div>
  );
}

function PhaseRow({ phase, active, last }: { phase: LoopPhase; active: boolean; last: boolean }) {
  const color = active ? VIOLET : "rgba(255,255,255,0.28)";
  return (
    <div className="flex gap-3">
      <div className="flex flex-col items-center">
        <span
          className="w-3.5 h-3.5 rounded-full mt-1 flex items-center justify-center transition-all"
          style={{ background: active ? VIOLET : "transparent", border: `2px solid ${color}`, boxShadow: active ? `0 0 10px ${VIOLET}` : "none" }}
        />
        {!last && <span className="w-px flex-1 my-1" style={{ background: "var(--border-med)" }} />}
      </div>
      <div className="pb-3.5">
        <div className="text-[13px] font-mono" style={{ color: active ? "#fff" : "var(--muted)" }}>{phase}</div>
        {active && <div className="text-[10px] font-mono mt-0.5" style={{ color: VIOLET }}>now</div>}
      </div>
    </div>
  );
}

function EventRow({ event }: { event: LoopEvent }) {
  const c = toneColor(event.tone);
  return (
    <div className="flex items-start gap-3">
      <span
        className="shrink-0 mt-[3px] text-[9px] font-mono uppercase tracking-wider px-1.5 py-0.5 rounded"
        style={{ color: c, background: "rgba(255,255,255,.03)", border: `1px solid ${c}`, minWidth: 64, textAlign: "center" }}
      >
        {event.phase}
      </span>
      <span className="text-[12.5px] leading-[1.55] font-mono" style={{ color: event.tone === "info" ? "rgba(255,255,255,.7)" : c }}>
        {event.text}
      </span>
    </div>
  );
}

function Cell({ label, value, accent }: { label: string; value: string; accent?: boolean }) {
  return (
    <div>
      <div className="text-[9.5px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--faint)" }}>{label}</div>
      <div className="text-[12.5px] font-mono truncate" style={{ color: accent ? VIOLET : "#fff" }}>{value}</div>
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

function Toggle({
  on,
  onClick,
  onIcon,
  offIcon,
  label,
  danger,
}: {
  on: boolean;
  onClick: () => void;
  onIcon: React.ReactNode;
  offIcon: React.ReactNode;
  label: string;
  danger?: boolean;
}) {
  const activeColor = danger ? RED : CYAN;
  return (
    <button
      onClick={onClick}
      className="flex items-center justify-between px-3 py-2 rounded-lg transition-all hover:bg-white/[0.04]"
      style={{ background: on ? (danger ? "rgba(248,113,113,.12)" : "rgba(0,212,255,.10)") : "rgba(255,255,255,.02)", border: `1px solid ${on ? activeColor : "var(--border)"}` }}
    >
      <span className="text-[11.5px] font-mono flex items-center gap-2" style={{ color: on ? "#fff" : "var(--muted)" }}>
        {on ? onIcon : offIcon}
        {label}
      </span>
      <span
        className="w-7 h-4 rounded-full relative transition-all"
        style={{ background: on ? activeColor : "var(--border-med)" }}
      >
        <span className="absolute top-0.5 w-3 h-3 rounded-full bg-white transition-all" style={{ left: on ? 14 : 2 }} />
      </span>
    </button>
  );
}
