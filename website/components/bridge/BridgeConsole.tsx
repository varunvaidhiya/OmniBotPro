"use client";

/*
 * BridgeConsole — the OhhO Bridge application shell.
 *
 * Three surfaces: adapter catalog (left), bridge detail with joint-index
 * map and ROS 2 topic table (centre), and live bridge status (right).
 * Selecting an adapter shows its models, the DDS↔ROS topic mapping, and
 * the per-joint impedance-gain defaults.
 */

import Link from "next/link";
import { useState } from "react";
import { ArrowLeft, Cpu, Radio, Zap, Activity, CheckCircle2, XCircle } from "lucide-react";

import { ADAPTERS, G1_JOINT_MAP, UNITREE_TOPICS, type BridgeAdapter } from "@/lib/bridge/adapters";
import { useRobot } from "@/lib/garage/RobotContext";
import { useRobotConnection } from "@/lib/connect/RobotConnectionProvider";
import { AIRobotPanel } from "@/components/console-kit";

const VIOLET = "#A78BFA";
const VIOLET_DIM = "rgba(167,139,250,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

const STATUS_COLORS: Record<string, string> = {
  active: GREEN,
  idle: "rgba(255,255,255,0.4)",
  error: RED,
  unavailable: "rgba(255,255,255,0.2)",
};

export default function BridgeConsole() {
  const { config } = useRobot();
  const conn = useRobotConnection();
  const [activeAdapterId, setActiveAdapterId] = useState("unitree-dds");

  const activeAdapter = ADAPTERS.find((a) => a.id === activeAdapterId) ?? ADAPTERS[0];

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
          href="/products/bridge"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: VIOLET }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Bridge</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Cpu size={14} /> protocol-adapters
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(52,211,153,.1)", color: GREEN, border: `1px solid ${GREEN}40` }}
          >
            <Activity size={11} />
            {ADAPTERS.filter((a) => a.status === "active").length}/{ADAPTERS.length} ACTIVE
          </span>
          {conn.isConnected && (
            <span
              className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
              style={{ background: VIOLET_DIM, color: VIOLET, border: `1px solid ${VIOLET}40` }}
            >
              <Radio size={11} className="animate-pulse" />
              {conn.robot?.name}
            </span>
          )}
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[260px_1fr_300px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Adapter catalog */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="bridge" config={config} />
          </div>
          <div className="px-4 pt-4 pb-2">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Adapters
            </h2>
          </div>
          <div className="flex-1 overflow-y-auto px-2 pb-4">
            <div className="flex flex-col gap-0.5 mt-1">
              {ADAPTERS.map((a) => {
                const on = activeAdapterId === a.id;
                const c = STATUS_COLORS[a.status];
                return (
                  <button
                    key={a.id}
                    onClick={() => setActiveAdapterId(a.id)}
                    className="flex items-start px-3 py-3 rounded-lg text-left transition-all"
                    style={{
                      background: on ? VIOLET_DIM : "transparent",
                      border: `1px solid ${on ? "rgba(167,139,250,0.3)" : "transparent"}`,
                    }}
                  >
                    <span className="w-2 h-2 rounded-full mr-3 mt-1.5 shrink-0" style={{ background: c }} />
                    <span className="flex-1 min-w-0">
                      <span className="text-[12px] font-mono block" style={{ color: on ? "#fff" : "var(--muted)" }}>
                        {a.name}
                      </span>
                      <span className="text-[10px] font-mono block" style={{ color: "var(--faint)" }}>
                        {a.protocol} · {a.models.length} models
                      </span>
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
        </aside>

        {/* CENTER — Bridge detail */}
        <section className="flex flex-col overflow-y-auto" style={{ background: "var(--bg)" }}>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="flex items-center gap-3 mb-3">
              <span
                className="w-10 h-10 rounded-xl flex items-center justify-center"
                style={{ background: `${activeAdapter.accent}14`, border: `1px solid ${activeAdapter.accent}40`, color: activeAdapter.accent }}
              >
                <Cpu size={18} />
              </span>
              <div>
                <h1 className="text-[16px] font-semibold">{activeAdapter.name}</h1>
                <p className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>
                  {activeAdapter.sdk} · {activeAdapter.transport}
                </p>
              </div>
            </div>
            <p className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.7)" }}>
              {activeAdapter.blurb}
            </p>
          </div>

          {/* Supported models */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Supported models
            </h2>
            <div className="flex flex-wrap gap-2">
              {activeAdapter.models.map((m) => (
                <span
                  key={m}
                  className="text-[11px] font-mono px-2.5 py-1.5 rounded-md"
                  style={{ background: `${activeAdapter.accent}10`, border: `1px solid ${activeAdapter.accent}30`, color: activeAdapter.accent }}
                >
                  {m}
                </span>
              ))}
            </div>
          </div>

          {/* ROS 2 topic mapping */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              ROS 2 topic bridge
            </h2>
            <div className="overflow-hidden rounded-lg" style={{ border: "1px solid var(--border)" }}>
              <table className="w-full text-[12px]">
                <thead>
                  <tr style={{ background: "rgba(255,255,255,0.02)" }}>
                    <th className="text-left px-3 py-2 text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Topic</th>
                    <th className="text-left px-3 py-2 text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Type</th>
                    <th className="text-left px-3 py-2 text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Direction</th>
                    <th className="text-right px-3 py-2 text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Rate</th>
                  </tr>
                </thead>
                <tbody>
                  {UNITREE_TOPICS.map((t, i) => (
                    <tr key={t.name} style={{ borderTop: i === 0 ? "none" : "1px solid var(--border)" }}>
                      <td className="px-3 py-2 font-mono" style={{ color: activeAdapter.accent }}>{t.name}</td>
                      <td className="px-3 py-2 font-mono text-[11px]" style={{ color: "var(--muted)" }}>{t.type}</td>
                      <td className="px-3 py-2">
                        <span
                          className="text-[10px] font-mono px-2 py-0.5 rounded"
                          style={{
                            background: t.direction === "native-to-ros" ? "rgba(52,211,153,0.12)" : "rgba(167,139,250,0.12)",
                            color: t.direction === "native-to-ros" ? GREEN : VIOLET,
                          }}
                        >
                          {t.direction === "native-to-ros" ? "→ ROS" : "→ native"}
                        </span>
                      </td>
                      <td className="px-3 py-2 text-right font-mono text-[11px]" style={{ color: "var(--muted)" }}>{t.rate}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Joint-index map (G1) */}
          <div className="p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Joint-index map · Unitree G1 (29 DoF)
            </h2>
            <div className="overflow-hidden rounded-lg max-h-[300px] overflow-y-auto" style={{ border: "1px solid var(--border)" }}>
              <table className="w-full text-[11px]">
                <thead className="sticky top-0" style={{ background: "var(--surf)" }}>
                  <tr>
                    <th className="text-left px-3 py-2 text-[9px] font-mono uppercase" style={{ color: "var(--faint)" }}>#</th>
                    <th className="text-left px-3 py-2 text-[9px] font-mono uppercase" style={{ color: "var(--faint)" }}>Native name</th>
                    <th className="text-left px-3 py-2 text-[9px] font-mono uppercase" style={{ color: "var(--faint)" }}>ROS name</th>
                    <th className="text-right px-3 py-2 text-[9px] font-mono uppercase" style={{ color: "var(--faint)" }}>kp</th>
                    <th className="text-right px-3 py-2 text-[9px] font-mono uppercase" style={{ color: "var(--faint)" }}>kd</th>
                    <th className="text-left px-3 py-2 text-[9px] font-mono uppercase" style={{ color: "var(--faint)" }}>Region</th>
                  </tr>
                </thead>
                <tbody>
                  {G1_JOINT_MAP.map((j, i) => (
                    <tr key={j.index} style={{ borderTop: i === 0 ? "none" : "1px solid var(--border)" }}>
                      <td className="px-3 py-1.5 font-mono" style={{ color: "var(--faint)" }}>{j.index}</td>
                      <td className="px-3 py-1.5 font-mono" style={{ color: "var(--muted)" }}>{j.nativeName}</td>
                      <td className="px-3 py-1.5 font-mono" style={{ color: activeAdapter.accent }}>{j.rosName}</td>
                      <td className="px-3 py-1.5 text-right font-mono" style={{ color: "var(--muted)" }}>{j.kp}</td>
                      <td className="px-3 py-1.5 text-right font-mono" style={{ color: "var(--muted)" }}>{j.kd}</td>
                      <td className="px-3 py-1.5">
                        <span
                          className="text-[9px] font-mono px-1.5 py-0.5 rounded"
                          style={{
                            background: j.region === "leg" ? "rgba(0,212,255,0.1)" : j.region === "arm" ? "rgba(167,139,250,0.1)" : "rgba(251,191,36,0.1)",
                            color: j.region === "leg" ? "#00D4FF" : j.region === "arm" ? VIOLET : AMBER,
                          }}
                        >
                          {j.region}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </section>

        {/* RIGHT — Live bridge status */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <BridgeStatusPanel adapter={activeAdapter} />
          <ImpedanceDefaults adapter={activeAdapter} />
          <ConnectionStatePanel />
        </aside>
      </div>
    </div>
  );
}

function BridgeStatusPanel({ adapter }: { adapter: BridgeAdapter }) {
  const active = adapter.status === "active";
  const c = STATUS_COLORS[adapter.status];

  return (
    <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
      <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
        Bridge status
      </h2>
      <div className="flex items-center gap-3 mb-4">
        <span className="w-3 h-3 rounded-full" style={{ background: c, boxShadow: active ? `0 0 12px ${c}` : "none" }} />
        <span className="text-[14px] font-semibold capitalize">{adapter.status}</span>
      </div>
      {active ? (
        <div className="grid grid-cols-3 gap-3">
          <div>
            <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Latency</div>
            <div className="text-[18px] font-display font-semibold mt-1" style={{ color: adapter.latencyMs > 5 ? AMBER : GREEN }}>
              {adapter.latencyMs.toFixed(1)} ms
            </div>
          </div>
          <div>
            <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Jitter</div>
            <div className="text-[18px] font-display font-semibold mt-1" style={{ color: "var(--text)" }}>
              {adapter.jitterMs.toFixed(1)} ms
            </div>
          </div>
          <div>
            <div className="text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Msg/s</div>
            <div className="text-[18px] font-display font-semibold mt-1" style={{ color: adapter.accent }}>
              {adapter.msgRate}
            </div>
          </div>
        </div>
      ) : (
        <p className="text-[12px] leading-[1.6]" style={{ color: "var(--muted)" }}>
          Adapter is {adapter.status}. Select a compatible robot in the garage and connect to activate this bridge.
        </p>
      )}
    </div>
  );
}

function ImpedanceDefaults({ adapter }: { adapter: BridgeAdapter }) {
  const regions = [
    { name: "Leg", kp: 80, kd: 3, color: "#00D4FF" },
    { name: "Waist", kp: 60, kd: 3, color: AMBER },
    { name: "Arm", kp: 40, kd: 2, color: VIOLET },
    { name: "Wrist", kp: 20, kd: 1, color: "#34D399" },
  ];
  return (
    <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
      <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
        Impedance defaults
      </h2>
      <p className="text-[11px] mb-3" style={{ color: "var(--faint)" }}>
        Default kp/kd applied when translating ROS JointState commands to {adapter.name} motor commands.
      </p>
      <div className="flex flex-col gap-2">
        {regions.map((r) => (
          <div key={r.name} className="flex items-center gap-3 px-3 py-2 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
            <span className="w-2 h-2 rounded-full" style={{ background: r.color }} />
            <span className="text-[12px] font-medium flex-1">{r.name}</span>
            <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>kp {r.kp}</span>
            <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>kd {r.kd}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function ConnectionStatePanel() {
  const conn = useRobotConnection();
  return (
    <div className="p-5">
      <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
        Live connection
      </h2>
      {conn.isConnected && conn.robot ? (
        <div className="flex flex-col gap-2">
          <div className="flex items-center gap-2">
            <CheckCircle2 size={14} style={{ color: GREEN }} />
            <span className="text-[13px] font-semibold">{conn.robot.name}</span>
          </div>
          <div className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
            Protocol: {conn.protocol}
          </div>
          <div className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
            Latency: {conn.status?.latencyMs ?? 0} ms
          </div>
          {conn.telemetry?.joints && (
            <div className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
              Joints: {conn.telemetry.joints.length}
            </div>
          )}
        </div>
      ) : (
        <div className="flex flex-col items-start gap-2">
          <div className="flex items-center gap-2">
            <XCircle size={14} style={{ color: "var(--faint)" }} />
            <span className="text-[13px]" style={{ color: "var(--muted)" }}>No robot connected</span>
          </div>
          <Link
            href="/garage"
            className="text-[12px] font-semibold inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg"
            style={{ background: VIOLET_DIM, color: VIOLET, border: `1px solid ${VIOLET}40` }}
          >
            <Zap size={13} /> Connect a robot
          </Link>
        </div>
      )}
    </div>
  );
}
