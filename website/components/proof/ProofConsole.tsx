"use client";

/*
 * ProofConsole — the OhhO Proof application shell.
 *
 * A simulated testing and validation dashboard.
 * Three surfaces: test suites (left), scenario coverage heatmap (centre),
 * and safety case / regression (right).
 */

import Link from "next/link";
import {
  ArrowLeft,
  TestTube2,
  Play,
  CheckCircle2,
  Target,
  BarChart4,
} from "lucide-react";

import { useProofSimulation, type HeatmapValue } from "@/lib/proof/testing";
import { useRobot } from "@/lib/garage/RobotContext";
import { AIRobotPanel } from "@/components/console-kit";

const VIOLET = "#A78BFA";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function ProofConsole() {
  const { config } = useRobot();
  const { suites, coverage, regression, isRunning, runFullSuite, overallVerdict } = useProofSimulation();

  const heatColor = (v: HeatmapValue) => {
    if (v === 2) return GREEN;
    if (v === 1) return AMBER;
    return RED;
  };

  const renderSparkline = () => {
    const min = Math.min(...regression) - 0.02;
    const max = Math.max(...regression) + 0.02;
    const range = max - min;
    const W = 280;
    const H = 70;

    const points = regression.map((val, i) => {
      const x = (i / (regression.length - 1)) * W;
      const y = H - ((val - min) / range) * H;
      return `${i === 0 ? "M" : "L"}${x} ${y}`;
    }).join(" ");

    const areaPath = points + ` L${W} ${H} L0 ${H} Z`;

    return (
      <svg viewBox={`0 0 ${W} ${H}`} className="w-full h-[70px] mt-2 block">
        <path d={areaPath} fill={VIOLET} fillOpacity={0.1} />
        <path d={points} fill="none" stroke={VIOLET} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
        <circle cx={W} cy={H - ((regression[regression.length - 1] - min) / range) * H} r={3} fill={VIOLET} />
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
          href="/products/proof"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: VIOLET }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Proof</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <TestTube2 size={14} /> release-candidate-validation
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <BarChart4 size={11} />
            12,480 RUNS
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Test Suites */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-4 border-b" style={{ borderColor: "var(--border)" }}>
            <AIRobotPanel consoleId="proof" config={config} />
          </div>
          <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Test Suites</h2>
            
            <div className="flex flex-col gap-4">
              {suites.map((s) => (
                <div key={s.id}>
                  <div className="flex justify-between text-[11px] font-mono mb-1.5">
                    <span className="text-gray-300">{s.name}</span>
                    <span className="text-white">{Math.round(s.passRate * 100)}%</span>
                  </div>
                  <div className="h-2 w-full rounded-full bg-white/5 overflow-hidden">
                    <div className="h-full transition-all duration-300" style={{ width: `${s.passRate * 100}%`, background: s.passRate > 0.9 ? GREEN : s.passRate > 0.8 ? AMBER : RED }} />
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="p-5 mt-auto">
            <button 
              onClick={runFullSuite}
              disabled={isRunning}
              className="w-full inline-flex items-center justify-center gap-2 text-[12px] font-semibold py-2.5 rounded-lg transition-all" 
              style={{ background: isRunning ? "rgba(167,139,250,0.2)" : VIOLET, color: isRunning ? VIOLET : "var(--bg)", cursor: isRunning ? "wait" : "pointer" }}
            >
              <Play size={12} fill="currentColor" />
              {isRunning ? "Executing..." : "Run Full Regression Suite"}
            </button>
          </div>
        </aside>

        {/* CENTER — Coverage Heatmap */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="p-6 border-b flex items-center justify-between" style={{ borderColor: "var(--border)" }}>
            <div>
              <h1 className="text-xl font-medium text-white mb-2 flex items-center gap-2">
                <Target size={20} color={VIOLET} />
                Scenario Coverage
              </h1>
              <p className="text-sm text-gray-400">Parameter space execution map: speed × payload × layout × lighting.</p>
            </div>
          </div>

          <div className="flex-1 flex items-center justify-center p-6">
            <svg viewBox="0 0 400 240" className="w-full max-w-2xl">
              {coverage.map((row, r) =>
                row.map((v, c) => (
                  <rect
                    key={`${r}-${c}`}
                    x={40 + c * 42}
                    y={40 + r * 30}
                    width={36}
                    height={22}
                    rx={4}
                    fill={heatColor(v)}
                    fillOpacity={v === 0 ? 0.4 : 0.15}
                    stroke={heatColor(v)}
                    strokeWidth={1.5}
                    strokeOpacity={0.6}
                    className="transition-all duration-500"
                  />
                ))
              )}
            </svg>
          </div>
        </section>

        {/* RIGHT — Verdict & Trends */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          <div className="p-5 border-b flex flex-col items-center" style={{ borderColor: "var(--border)" }}>
            <h2 className="w-full text-left text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Overall Verdict</h2>
            
            <div className="relative w-32 h-32 mb-4">
              <svg viewBox="0 0 100 100" className="transform -rotate-90">
                <circle cx="50" cy="50" r="40" fill="none" stroke="rgba(255,255,255,0.05)" strokeWidth="10" />
                <circle cx="50" cy="50" r="40" fill="none" stroke={GREEN} strokeWidth="10" strokeDasharray={`${overallVerdict * 251.2} 251.2`} className="transition-all duration-500" />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-[20px] font-medium text-white">{Math.round(overallVerdict * 100)}%</span>
              </div>
            </div>

            <div className="w-full p-3 rounded-lg flex items-center justify-between" style={{ background: "rgba(52,211,153,.1)", border: `1px solid ${GREEN}40` }}>
              <span className="text-[11px] font-mono text-emerald-400">Safety Case</span>
              <span className="flex items-center gap-1 text-[11px] font-mono text-emerald-400 font-bold"><CheckCircle2 size={12} /> READY TO SHIP</span>
            </div>
          </div>

          <div className="flex-1 p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Sim-to-Real Regression</h2>
            {renderSparkline()}
            <div className="flex justify-between mt-2">
              <span className="text-[10px] font-mono text-gray-500">build 0142</span>
              <span className="text-[10px] font-mono text-emerald-400">build 0152 · +6%</span>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
