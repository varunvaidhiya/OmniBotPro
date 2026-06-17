"use client";

/*
 * FleetConsole — the OhhO Fleet application shell.
 *
 * A simulated mission control dashboard. 
 * Three surfaces: robot roster (left), live map (centre), 
 * and observability/rollout dashboard (right).
 */

import Link from "next/link";
import { useMemo, useState } from "react";
import {
  ArrowLeft,
  Server,
  Activity,
  AlertTriangle,
  Radio,
  Zap,
  DownloadCloud,
} from "lucide-react";

import { VERSIONS, type RobotStatus } from "@/lib/fleet/fleet";
import { useFleetSimulation } from "@/lib/fleet/simulation";
import FleetMap from "./FleetMap";

const CYAN = "#00D4FF";
const CYAN_DIM = "rgba(0,212,255,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function FleetConsole() {
  const { robots, alerts, isRollingOut, triggerOTA } = useFleetSimulation();
  
  const [activeRobotId, setActiveRobotId] = useState<string | null>(null);
  const [filter, setFilter] = useState<RobotStatus | "all">("all");

  const filteredRobots = useMemo(() => {
    if (filter === "all") return robots;
    return robots.filter((r) => r.status === filter);
  }, [robots, filter]);

  const stats = useMemo(() => {
    const total = robots.length;
    const online = robots.filter(r => r.status === "online").length;
    const degraded = robots.filter(r => r.status === "degraded").length;
    const offline = robots.filter(r => r.status === "offline").length;
    const canary = robots.filter(r => r.version === VERSIONS.CANARY).length;
    
    return { total, online, degraded, offline, canary };
  }, [robots]);

  if (robots.length === 0) return null; // loading

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
          href="/products/fleet"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: CYAN }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Fleet</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Server size={14} /> mission-control
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(52,211,153,.1)", color: GREEN, border: `1px solid ${GREEN}40` }}
          >
            <Activity size={11} />
            {stats.online}/{stats.total} ONLINE
          </span>
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(248,113,113,.1)", color: RED, border: `1px solid ${RED}40` }}
          >
            <AlertTriangle size={11} />
            {alerts.filter(a => a.severity === 'critical').length} ALERTS
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Roster */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="px-4 pt-4 pb-2">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>
              Fleet Roster
            </h2>
            <div className="flex flex-wrap gap-1.5">
              {(["all", "online", "degraded", "offline"] as const).map((f) => (
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
            <div className="flex flex-col gap-0.5 mt-2">
              {filteredRobots.map((r) => {
                const on = activeRobotId === r.id;
                const c = r.status === "online" ? GREEN : r.status === "degraded" ? AMBER : RED;
                return (
                  <button
                    key={r.id}
                    onClick={() => setActiveRobotId(r.id)}
                    className="flex items-center px-3 py-2.5 rounded-lg text-left transition-all group"
                    style={{
                      background: on ? CYAN_DIM : "transparent",
                      border: `1px solid ${on ? "rgba(0,212,255,0.3)" : "transparent"}`,
                    }}
                  >
                    <span className="w-2 h-2 rounded-full mr-3 shrink-0" style={{ background: c }} />
                    <span className="flex-1 text-[12px] font-mono" style={{ color: on ? "#fff" : "var(--muted)" }}>
                      {r.id}
                    </span>
                    <span className="text-[10px] font-mono text-right flex flex-col items-end">
                      <span style={{ color: "var(--faint)" }}>{r.battery}%</span>
                      <span style={{ color: r.version === VERSIONS.CANARY ? CYAN : "var(--faint)", opacity: 0.8 }}>{r.version}</span>
                    </span>
                  </button>
                );
              })}
            </div>
          </div>
          
          <div className="p-4 border-t" style={{ borderColor: "var(--border)" }}>
            <button 
              onClick={triggerOTA}
              disabled={isRollingOut}
              className="w-full inline-flex items-center justify-center gap-2 text-[12px] font-semibold py-2.5 rounded-lg transition-all" 
              style={{ background: isRollingOut ? "rgba(0,212,255,0.2)" : CYAN, color: isRollingOut ? CYAN : "var(--bg)", cursor: isRollingOut ? "not-allowed" : "pointer" }}
            >
              <DownloadCloud size={14} />
              {isRollingOut ? "Deploying OTA..." : "Trigger OTA Update"}
            </button>
          </div>
        </aside>

        {/* CENTER — Map */}
        <section className="flex flex-col relative" style={{ background: "var(--bg)" }}>
          <div className="absolute top-4 left-4 z-10 flex items-center gap-2 px-3 py-1.5 rounded-md" style={{ background: "rgba(0,0,0,0.6)", backdropFilter: "blur(4px)" }}>
            <Radio size={12} color={CYAN} className={isRollingOut ? "animate-pulse" : ""} />
            <span className="text-[10px] font-mono tracking-widest text-white">LIVE_TELEMETRY</span>
          </div>
          <FleetMap robots={robots} activeId={activeRobotId} onSelect={setActiveRobotId} />
        </section>

        {/* RIGHT — Observability */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          {/* Health Donut */}
          <div className="p-5 border-b flex flex-col items-center" style={{ borderColor: "var(--border)" }}>
            <h2 className="w-full text-left text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Fleet Health</h2>
            
            <div className="relative w-32 h-32 mb-2">
              <svg viewBox="0 0 100 100" className="transform -rotate-90">
                {/* Background */}
                <circle cx="50" cy="50" r="40" fill="none" stroke="rgba(255,255,255,0.05)" strokeWidth="10" />
                {/* Segments */}
                <circle cx="50" cy="50" r="40" fill="none" stroke={GREEN} strokeWidth="10" strokeDasharray={`${(stats.online / stats.total) * 251.2} 251.2`} />
                <circle cx="50" cy="50" r="40" fill="none" stroke={AMBER} strokeWidth="10" strokeDasharray={`${(stats.degraded / stats.total) * 251.2} 251.2`} strokeDashoffset={-(stats.online / stats.total) * 251.2} />
                <circle cx="50" cy="50" r="40" fill="none" stroke={RED} strokeWidth="10" strokeDasharray={`${(stats.offline / stats.total) * 251.2} 251.2`} strokeDashoffset={-((stats.online + stats.degraded) / stats.total) * 251.2} />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-[20px] font-medium text-white">{Math.round((stats.online / stats.total) * 100)}%</span>
                <span className="text-[9px] font-mono text-emerald-400 tracking-wider">HEALTHY</span>
              </div>
            </div>
            
            <div className="w-full grid grid-cols-3 gap-2 mt-2">
              <div className="text-center">
                <div className="text-[14px] text-emerald-400 font-mono">{stats.online}</div>
                <div className="text-[9px] uppercase text-gray-500 font-mono">Online</div>
              </div>
              <div className="text-center">
                <div className="text-[14px] text-amber-400 font-mono">{stats.degraded}</div>
                <div className="text-[9px] uppercase text-gray-500 font-mono">Degraded</div>
              </div>
              <div className="text-center">
                <div className="text-[14px] text-red-400 font-mono">{stats.offline}</div>
                <div className="text-[9px] uppercase text-gray-500 font-mono">Offline</div>
              </div>
            </div>
          </div>

          {/* Rollout Progress */}
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>OTA Rollout</h2>
            
            <div className="mb-4">
              <div className="flex justify-between text-[10px] font-mono mb-1.5">
                <span style={{ color: CYAN }}>{VERSIONS.CANARY} · canary</span>
                <span className="text-white">{Math.round((stats.canary / stats.total) * 100)}%</span>
              </div>
              <div className="h-2 w-full rounded-full bg-white/5 overflow-hidden">
                <div className="h-full transition-all duration-500" style={{ width: `${(stats.canary / stats.total) * 100}%`, background: CYAN }} />
              </div>
            </div>

            <div>
              <div className="flex justify-between text-[10px] font-mono mb-1.5">
                <span style={{ color: GREEN }}>{VERSIONS.STABLE} · stable</span>
                <span className="text-white">{Math.round(((stats.total - stats.canary) / stats.total) * 100)}%</span>
              </div>
              <div className="h-2 w-full rounded-full bg-white/5 overflow-hidden">
                <div className="h-full transition-all duration-500" style={{ width: `${((stats.total - stats.canary) / stats.total) * 100}%`, background: GREEN }} />
              </div>
            </div>
          </div>

          {/* Alerts Feed */}
          <div className="flex-1 flex flex-col p-4">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-3" style={{ color: "var(--muted)" }}>Live Alerts</h2>
            <div className="flex flex-col gap-2">
              {alerts.map(a => (
                <div key={a.id} className="p-3 rounded-lg border flex flex-col gap-1" style={{ borderColor: "var(--border)", background: "rgba(255,255,255,0.01)" }}>
                  <div className="flex justify-between items-start">
                    <span className="text-[11px] font-mono font-medium" style={{ color: a.severity === 'critical' ? RED : a.severity === 'warning' ? AMBER : CYAN }}>
                      {a.title}
                    </span>
                    <span className="text-[9px] font-mono" style={{ color: "var(--faint)" }}>{a.time}</span>
                  </div>
                  <p className="text-[11px] text-gray-400 leading-snug">{a.description}</p>
                </div>
              ))}
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
