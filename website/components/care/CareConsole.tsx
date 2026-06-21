"use client";

/*
 * CareConsole — the OhhO Care application shell.
 *
 * Three surfaces: work orders with urgency filters (left), motor-
 * degradation chart + fleet metrics (centre), and repair log + parts
 * from the Build BOM (right). The degradation chart is live-animated
 * to show the predictive-maintenance signal trending toward threshold.
 */

import Link from "next/link";
import { useState } from "react";
import {
  ArrowLeft,
  Wrench,
  AlertTriangle,
  TrendingUp,
  Package,
  ClipboardCheck,
  Activity,
  ShoppingCart,
} from "lucide-react";

import {
  WORK_ORDERS,
  REPAIR_LOG,
  FLEET_METRICS,
  useDegradationSimulation,
  type Urgency,
  type WorkOrderStatus,
} from "@/lib/care/care";
import { useRobot } from "@/lib/garage/RobotContext";
import { useRobotConnection } from "@/lib/connect/RobotConnectionProvider";
import { AIRobotPanel } from "@/components/console-kit";

const CYAN = "#00D4FF";
const CYAN_DIM = "rgba(0,212,255,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

const URGENCY_COLORS: Record<Urgency, string> = { high: RED, medium: AMBER, low: GREEN };
const STATUS_LABELS: Record<WorkOrderStatus, string> = {
  open: "Open",
  ordered: "Part ordered",
  dispatched: "Tech dispatched",
  repaired: "Repaired",
};

export default function CareConsole() {
  const { config } = useRobot();
  const conn = useRobotConnection();
  const [filter, setFilter] = useState<Urgency | "all">("all");
  const [activeWOId, setActiveWOId] = useState("wo-001");
  const degradation = useDegradationSimulation(true);

  const filteredWOs = filter === "all" ? WORK_ORDERS : WORK_ORDERS.filter((w) => w.urgency === filter);
  const activeWO = WORK_ORDERS.find((w) => w.id === activeWOId) ?? WORK_ORDERS[0];

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
          href="/products/care"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: CYAN }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Care</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Wrench size={14} /> predictive-maintenance
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(248,113,113,.1)", color: RED, border: `1px solid ${RED}40` }}
          >
            <AlertTriangle size={11} />
            {FLEET_METRICS.openWorkOrders} OPEN
          </span>
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: CYAN_DIM, color: CYAN, border: `1px solid ${CYAN}40` }}
          >
            <Activity size={11} />
            {FLEET_METRICS.robotsMonitored} ROBOTS
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_300px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Work orders */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="care" config={config} />
          </div>

          <div className="px-4 pt-4 pb-2">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Work orders
            </h2>
            <div className="flex flex-wrap gap-1.5">
              {(["all", "high", "medium", "low"] as const).map((f) => (
                <button
                  key={f}
                  onClick={() => setFilter(f)}
                  className="text-[10.5px] font-mono uppercase px-2 py-1.5 rounded-md transition-colors"
                  style={{
                    background: filter === f ? CYAN_DIM : "rgba(255,255,255,0.02)",
                    color: filter === f ? "#fff" : "var(--muted)",
                    border: `1px solid ${filter === f ? "rgba(0,212,255,0.3)" : "var(--border)"}`,
                  }}
                >
                  {f}
                </button>
              ))}
            </div>
          </div>

          <div className="flex-1 overflow-y-auto px-2 pb-4">
            <div className="flex flex-col gap-1 mt-2">
              {filteredWOs.map((w) => {
                const on = activeWOId === w.id;
                const c = URGENCY_COLORS[w.urgency];
                return (
                  <button
                    key={w.id}
                    onClick={() => setActiveWOId(w.id)}
                    className="flex items-start px-3 py-3 rounded-lg text-left transition-all"
                    style={{
                      background: on ? CYAN_DIM : "transparent",
                      border: `1px solid ${on ? "rgba(0,212,255,0.3)" : "transparent"}`,
                    }}
                  >
                    <span className="w-2 h-2 rounded-full mr-3 mt-1.5 shrink-0" style={{ background: c }} />
                    <span className="flex-1 min-w-0">
                      <span className="text-[12px] font-mono block truncate" style={{ color: on ? "#fff" : "var(--muted)" }}>
                        {w.robotId}
                      </span>
                      <span className="text-[10px] font-mono block truncate" style={{ color: "var(--faint)" }}>
                        {w.component}
                      </span>
                      <span className="text-[10px] font-mono block" style={{ color: c }}>
                        {w.urgency.toUpperCase()} · {w.daysToFailure}d
                      </span>
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
        </aside>

        {/* CENTER — Degradation chart + metrics */}
        <section className="flex flex-col overflow-y-auto" style={{ background: "var(--bg)" }}>
          {/* Active work order header */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="flex items-center gap-3 mb-2">
              <span
                className="text-[10px] font-mono font-semibold px-2.5 py-1 rounded"
                style={{ background: `${URGENCY_COLORS[activeWO.urgency]}1a`, color: URGENCY_COLORS[activeWO.urgency], border: `1px solid ${URGENCY_COLORS[activeWO.urgency]}40` }}
              >
                {activeWO.urgency.toUpperCase()}
              </span>
              <h1 className="text-[16px] font-semibold">{activeWO.robotId} · {activeWO.component}</h1>
            </div>
            <p className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.7)" }}>
              {activeWO.symptom}
            </p>
            <div className="flex items-center gap-3 mt-3">
              <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
                Status: <span style={{ color: "var(--text)" }}>{STATUS_LABELS[activeWO.status]}</span>
              </span>
              <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
                Est. failure: <span style={{ color: URGENCY_COLORS[activeWO.urgency] }}>{activeWO.daysToFailure} days</span>
              </span>
              <span className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>Created {activeWO.createdAt}</span>
            </div>
          </div>

          {/* Degradation chart */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>
                Motor temperature trend
              </h2>
              <div className="flex items-center gap-3">
                <span className="text-[20px] font-display font-bold" style={{ color: degradation.currentTemp > 80 ? RED : degradation.currentTemp > 65 ? AMBER : GREEN }}>
                  {degradation.currentTemp.toFixed(0)}°C
                </span>
                <span className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>
                  threshold: {degradation.threshold}°C
                </span>
              </div>
            </div>

            <DegradationChart temps={degradation.temps} threshold={degradation.threshold} />

            <div className="flex justify-between mt-2">
              <span className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>
                {degradation.elapsed > 0 ? `${degradation.elapsed.toFixed(0)}s elapsed` : "monitoring…"}
              </span>
              <span className="text-[10px] font-mono" style={{ color: degradation.currentTemp > degradation.threshold ? RED : AMBER }}>
                {degradation.currentTemp > degradation.threshold
                  ? "THRESHOLD EXCEEDED — service now"
                  : `~${degradation.timeToThreshold.toFixed(1)}min to threshold`}
              </span>
            </div>
          </div>

          {/* Fleet metrics */}
          <div className="p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Fleet maintenance metrics
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              <MetricCard label="MTBF" value={`${FLEET_METRICS.mtbfHours}h`} icon={<TrendingUp size={14} />} />
              <MetricCard label="MTTR" value={`${FLEET_METRICS.mttrHours}h`} icon={<Wrench size={14} />} />
              <MetricCard label="Downtime (mo)" value={`${FLEET_METRICS.downtimeMonthHours}h`} color={AMBER} />
              <MetricCard label="Predictive" value={`${FLEET_METRICS.predictiveAlerts}`} color={RED} icon={<AlertTriangle size={14} />} />
            </div>
          </div>
        </section>

        {/* RIGHT — Parts + repair log */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          {/* Parts from BOM */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Replacement part
            </h2>
            <div className="px-3 py-3 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
              <div className="flex items-center gap-2 mb-3">
                <Package size={15} style={{ color: CYAN }} />
                <span className="text-[13px] font-semibold">{activeWO.partName}</span>
              </div>
              <div className="grid grid-cols-2 gap-2 text-[11px] font-mono">
                <div>
                  <span style={{ color: "var(--faint)" }}>Cost</span>
                  <div style={{ color: "var(--text)" }}>${activeWO.partCost}</div>
                </div>
                <div>
                  <span style={{ color: "var(--faint)" }}>Lead time</span>
                  <div style={{ color: "var(--text)" }}>{activeWO.partLeadTimeDays} days</div>
                </div>
              </div>
              <div className="mt-3">
                <span
                  className="inline-flex items-center gap-1.5 text-[10px] font-mono px-2 py-1 rounded"
                  style={{
                    background: activeWO.partInStock ? "rgba(52,211,153,0.12)" : "rgba(251,191,36,0.12)",
                    color: activeWO.partInStock ? GREEN : AMBER,
                  }}
                >
                  {activeWO.partInStock ? "● In stock" : "○ Order required"}
                </span>
              </div>
            </div>
            <button
              className="w-full mt-3 inline-flex items-center justify-center gap-2 text-[12px] font-semibold py-2.5 rounded-lg transition-all hover:-translate-y-px"
              style={{ background: CYAN, color: "var(--bg)" }}
            >
              <ShoppingCart size={14} />
              Order from Build BOM
            </button>
          </div>

          {/* Repair log */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Repair log
            </h2>
            <div className="flex flex-col gap-2">
              {REPAIR_LOG.map((r) => (
                <div key={r.id} className="flex items-start gap-2.5 px-3 py-2.5 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
                  <ClipboardCheck size={13} style={{ color: GREEN, marginTop: 1 }} />
                  <div className="flex-1 min-w-0">
                    <div className="text-[11.5px] font-mono" style={{ color: "var(--text)" }}>
                      {r.robotId} · {r.component}
                    </div>
                    <div className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>
                      {r.technician} · {r.timestamp}
                    </div>
                    {r.auditLogged && (
                      <div className="text-[9px] font-mono mt-0.5" style={{ color: GREEN }}>
                        → Comply audit trail
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Connection */}
          <div className="p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>
              Monitored robot
            </h2>
            {conn.isConnected && conn.robot ? (
              <div className="flex flex-col gap-2">
                <span className="text-[13px] font-semibold">{conn.robot.name}</span>
                <span className="text-[11px] font-mono" style={{ color: "var(--muted)" }}>
                  Telemetry feeding degradation models
                </span>
              </div>
            ) : (
              <Link
                href="/garage"
                className="inline-flex items-center gap-1.5 text-[12px] font-semibold px-3 py-1.5 rounded-lg"
                style={{ background: CYAN_DIM, color: CYAN, border: `1px solid ${CYAN}40` }}
              >
                Connect a robot
              </Link>
            )}
          </div>
        </aside>
      </div>
    </div>
  );
}

function DegradationChart({ temps, threshold }: { temps: number[]; threshold: number }) {
  const W = 400;
  const H = 120;
  const PAD = 8;
  const minT = 40;
  const maxT = 95;
  const n = temps.length;

  const x = (i: number) => PAD + (i / Math.max(n - 1, 1)) * (W - 2 * PAD);
  const y = (v: number) => PAD + (1 - (v - minT) / (maxT - minT)) * (H - 2 * PAD);

  const path = temps.map((v, i) => `${i === 0 ? "M" : "L"}${x(i).toFixed(1)} ${y(v).toFixed(1)}`).join(" ");
  const areaPath = path + ` L${x(n - 1).toFixed(1)} ${H - PAD} L${x(0).toFixed(1)} ${H - PAD} Z`;
  const thresholdY = y(threshold);

  return (
    <div className="w-full overflow-hidden rounded-lg" style={{ border: "1px solid var(--border)" }}>
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" height={120}>
        <defs>
          <linearGradient id="deg-area" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor={RED} stopOpacity="0.2" />
            <stop offset="1" stopColor={RED} stopOpacity="0" />
          </linearGradient>
        </defs>

        {/* Grid lines */}
        {[0, 1, 2, 3].map((i) => (
          <line key={i} x1={PAD} y1={PAD + (i * (H - 2 * PAD)) / 3} x2={W - PAD} y2={PAD + (i * (H - 2 * PAD)) / 3} stroke="rgba(255,255,255,0.05)" />
        ))}

        {/* Threshold line */}
        <line x1={PAD} y1={thresholdY} x2={W - PAD} y2={thresholdY} stroke={RED} strokeWidth={1} strokeDasharray="4 3" strokeOpacity={0.4} />
        <text x={PAD + 4} y={thresholdY - 3} fontSize={8} fill={RED} fontFamily="monospace" opacity={0.7}>threshold</text>

        {/* Area fill */}
        {n > 1 && <path d={areaPath} fill="url(#deg-area)" />}

        {/* Line */}
        {n > 1 && <path d={path} fill="none" stroke={RED} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />}

        {/* Current point */}
        {n > 0 && <circle cx={x(n - 1)} cy={y(temps[n - 1])} r={3.5} fill={RED} />}
      </svg>
    </div>
  );
}

function MetricCard({ label, value, color, icon }: { label: string; value: string; color?: string; icon?: React.ReactNode }) {
  return (
    <div className="px-3 py-3 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
      <div className="flex items-center gap-1.5 text-[9px] font-mono uppercase tracking-wider mb-1" style={{ color: "var(--faint)" }}>
        {icon}
        {label}
      </div>
      <div className="text-[18px] font-display font-semibold" style={{ color: color ?? "var(--text)" }}>{value}</div>
    </div>
  );
}
