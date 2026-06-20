"use client";

/*
 * FrameConsole — the OhhO Frame application shell.
 *
 * A simulated hardware edge compute dashboard.
 * Three surfaces: hardware telemetry (left), process manager (centre),
 * and network/device config (right).
 */

import Link from "next/link";
import {
  ArrowLeft,
  Cpu,
  Server,
  Activity,
  Play,
  Square,
  RotateCw,
  Network,
  Wifi,
  HardDrive,
} from "lucide-react";

import { useSystemTelemetry } from "@/lib/frame/system";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";
import { useProcessManager } from "@/lib/frame/processes";

const ORANGE = "#F97316";
const GREEN = "#34D399";
const RED = "#F87171";
const AMBER = "#FBBF24";

export default function FrameConsole() {
  const { config } = useRobot();
  const { current, history } = useSystemTelemetry();
  const { processes, toggleProcess, restartProcess } = useProcessManager();

  const renderSparkline = (data: number[], color: string, maxVal: number) => {
    if (data.length === 0) return null;
    const W = 240;
    const H = 40;
    const min = 0;
    const max = maxVal;
    const range = max - min;

    const points = data.map((val, i) => {
      const x = (i / (data.length - 1)) * W;
      const y = H - ((val - min) / range) * H;
      return `${i === 0 ? "M" : "L"}${x} ${y}`;
    }).join(" ");

    const areaPath = points + ` L${W} ${H} L0 ${H} Z`;

    return (
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full h-[40px] mt-2 block">
        <path d={areaPath} fill={color} fillOpacity={0.1} />
        <path d={points} fill="none" stroke={color} strokeWidth={1.5} strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    );
  };

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
          href="/products/frame"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: ORANGE }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Frame</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Server size={14} /> edge-compute-node
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <Activity size={11} color={GREEN} />
            ONLINE
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Hardware Telemetry */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="frame" config={config} />
          </div>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Hardware Telemetry</h2>
            
            <div className="flex flex-col gap-5">
              {/* CPU */}
              <div>
                <div className="flex justify-between items-center mb-1">
                  <span className="text-[11px] font-mono text-gray-400 flex items-center gap-1.5"><Cpu size={12} /> CPU LOAD</span>
                  <span className="text-[14px] font-mono text-white">{current.cpu.toFixed(1)}%</span>
                </div>
                {renderSparkline(history.map(h => h.cpu), ORANGE, 100)}
              </div>

              {/* RAM */}
              <div>
                <div className="flex justify-between items-center mb-1">
                  <span className="text-[11px] font-mono text-gray-400 flex items-center gap-1.5"><HardDrive size={12} /> MEMORY</span>
                  <span className="text-[14px] font-mono text-white">{current.ram.toFixed(1)}%</span>
                </div>
                {renderSparkline(history.map(h => h.ram), ORANGE, 100)}
              </div>

              {/* TEMP */}
              <div>
                <div className="flex justify-between items-center mb-1">
                  <span className="text-[11px] font-mono text-gray-400 flex items-center gap-1.5"><Activity size={12} /> TEMPERATURE</span>
                  <span className="text-[14px] font-mono text-white">{current.temp.toFixed(1)}°C</span>
                </div>
                {renderSparkline(history.map(h => h.temp), current.temp > 75 ? RED : ORANGE, 100)}
              </div>
            </div>
          </div>
        </aside>

        {/* CENTER — Process Manager */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b flex items-center justify-between" style={{ borderColor: "var(--border)" }}>
            <div>
              <h1 className="text-xl font-medium text-white mb-2 flex items-center gap-2">
                <Server size={20} color={ORANGE} />
                Process Manager
              </h1>
              <p className="text-sm text-gray-400">Manage OS daemons, containers, and ROS 2 nodes.</p>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto p-6">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b" style={{ borderColor: "var(--border)" }}>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">Process</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">Status</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">CPU</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">RAM</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal">Uptime</th>
                  <th className="pb-3 text-[10px] font-mono uppercase tracking-wider text-gray-500 font-normal text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="text-[13px]">
                {processes.map(p => (
                  <tr key={p.id} className="border-b transition-colors hover:bg-white/5" style={{ borderColor: "var(--border-med)" }}>
                    <td className="py-4 font-mono text-gray-200">{p.name}</td>
                    <td className="py-4 font-mono">
                      {p.status === "running" && <span className="text-emerald-400 bg-emerald-400/10 px-1.5 py-0.5 rounded text-[10px]">RUNNING</span>}
                      {p.status === "stopped" && <span className="text-gray-400 bg-gray-400/10 px-1.5 py-0.5 rounded text-[10px]">STOPPED</span>}
                      {p.status === "failed" && <span className="text-red-400 bg-red-400/10 px-1.5 py-0.5 rounded text-[10px]">FAILED</span>}
                    </td>
                    <td className="py-4 font-mono text-gray-400">{p.cpu > 0 ? `${p.cpu.toFixed(1)}%` : "-"}</td>
                    <td className="py-4 font-mono text-gray-400">{p.ram > 0 ? `${Math.round(p.ram)} MB` : "-"}</td>
                    <td className="py-4 font-mono text-gray-400">{p.uptime}</td>
                    <td className="py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {p.status === "running" ? (
                          <>
                            <button onClick={() => restartProcess(p.id)} className="p-1.5 rounded hover:bg-white/10 text-gray-400 transition-colors" title="Restart">
                              <RotateCw size={14} />
                            </button>
                            <button onClick={() => toggleProcess(p.id)} className="p-1.5 rounded hover:bg-white/10 text-gray-400 transition-colors" title="Stop">
                              <Square size={14} fill="currentColor" />
                            </button>
                          </>
                        ) : (
                          <button onClick={() => toggleProcess(p.id)} className="p-1.5 rounded hover:bg-white/10 text-emerald-400 transition-colors" title="Start">
                            <Play size={14} fill="currentColor" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* RIGHT — Device Config & Network */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Device Config</h2>
            <div className="flex flex-col gap-3">
              <div className="flex justify-between items-center">
                <span className="text-[11px] text-gray-400">OS Version</span>
                <span className="text-[11px] font-mono text-gray-200">OhhO OS v2.1.4</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-[11px] text-gray-400">Kernel</span>
                <span className="text-[11px] font-mono text-gray-200">6.8.0-1008-raspi</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-[11px] text-gray-400">Architecture</span>
                <span className="text-[11px] font-mono text-gray-200">aarch64</span>
              </div>
            </div>
          </div>

          <div className="flex-1 p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Network Interfaces</h2>
            <div className="flex flex-col gap-4">
              
              <div className="flex items-start gap-3">
                <div className="mt-0.5"><Wifi size={14} color={GREEN} /></div>
                <div className="flex-1">
                  <div className="flex justify-between items-center mb-1">
                    <span className="text-[11px] font-mono text-white">wlan0</span>
                    <span className="text-[9px] font-mono text-emerald-400">CONNECTED</span>
                  </div>
                  <div className="text-[10px] font-mono text-gray-500 mb-0.5">192.168.1.101</div>
                  <div className="text-[10px] font-mono text-gray-500">MAC: e4:5f:01:22:bb:cc</div>
                </div>
              </div>

              <div className="flex items-start gap-3 opacity-60">
                <div className="mt-0.5"><Network size={14} color="var(--muted)" /></div>
                <div className="flex-1">
                  <div className="flex justify-between items-center mb-1">
                    <span className="text-[11px] font-mono text-white">eth0</span>
                    <span className="text-[9px] font-mono text-gray-500">DOWN</span>
                  </div>
                  <div className="text-[10px] font-mono text-gray-500">MAC: e4:5f:01:22:bb:cd</div>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <div className="mt-0.5"><Network size={14} color={ORANGE} /></div>
                <div className="flex-1">
                  <div className="flex justify-between items-center mb-1">
                    <span className="text-[11px] font-mono text-white">tailscale0</span>
                    <span className="text-[9px] font-mono text-orange-400">SECURE</span>
                  </div>
                  <div className="text-[10px] font-mono text-gray-500">100.82.11.45</div>
                </div>
              </div>

            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
