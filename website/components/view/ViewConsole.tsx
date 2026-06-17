"use client";

/*
 * ViewConsole — the OhhO View application shell.
 *
 * A simulated 3D perception and sensor streaming dashboard.
 * Three surfaces: sensor tree (left), 3D canvas (centre),
 * and live 2D streams (right).
 */

import Link from "next/link";
import {
  ArrowLeft,
  Eye,
  Layers,
  Camera,
  Activity,
  Box,
  Map,
  Radio,
  Wifi,
} from "lucide-react";

import { useSensors, type SensorLayer } from "@/lib/view/sensors";

const BLUE = "#3B82F6";
const GREEN = "#34D399";
const CYAN = "#00D4FF";
const RED = "#F87171";

export default function ViewConsole() {
  const { layers, toggleLayer } = useSensors();

  const getIconForType = (type: SensorLayer["type"]) => {
    switch (type) {
      case "pointcloud": return <Box size={14} />;
      case "image": return <Camera size={14} />;
      case "map": return <Map size={14} />;
      case "tf": return <Activity size={14} />;
      case "laserscan": return <Radio size={14} />;
    }
  };

  const isMapVisible = layers.find(l => l.id === "s3")?.visible;
  const isPclVisible = layers.find(l => l.id === "s1")?.visible;
  const isLidarVisible = layers.find(l => l.id === "s2")?.visible;

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
          href="/products/view"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: BLUE }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO View</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Eye size={14} /> perception-engine
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <Wifi size={11} color={GREEN} />
            LIVE STREAM
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[280px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Sensor Tree */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <div className="p-5 border-b flex items-center justify-between" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>Sensor Streams</h2>
            <Layers size={14} color="var(--faint)" />
          </div>

          <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-2">
            {layers.map(l => (
              <button
                key={l.id}
                onClick={() => toggleLayer(l.id)}
                className="flex items-center gap-3 p-3 rounded-lg border transition-colors group text-left"
                style={{ 
                  borderColor: l.visible ? `${BLUE}40` : "var(--border)", 
                  background: l.visible ? "rgba(59,130,246,0.05)" : "transparent" 
                }}
              >
                <div style={{ color: l.visible ? BLUE : "var(--faint)" }}>
                  {getIconForType(l.type)}
                </div>
                <div className="flex-1">
                  <div className="text-[12px] text-gray-200 font-medium mb-0.5">{l.name}</div>
                  <div className="text-[10px] font-mono text-gray-500">{l.topic}</div>
                </div>
                <div className="text-[10px] font-mono text-gray-600">{l.hz} Hz</div>
              </button>
            ))}
          </div>
        </aside>

        {/* CENTER — 3D Viewer */}
        <section className="flex flex-col relative overflow-hidden" style={{ background: "#05080f" }}>
          
          <div className="absolute top-4 left-4 z-10">
            <h1 className="text-xl font-medium text-white mb-1 flex items-center gap-2" style={{ textShadow: "0 2px 4px rgba(0,0,0,0.5)" }}>
              Interactive 3D Canvas
            </h1>
            <p className="text-sm text-gray-400">OmniBot V2 · Local frame: odom</p>
          </div>

          {/* Simulated 3D Environment using SVG */}
          <div className="absolute inset-0 flex items-center justify-center overflow-hidden">
            <svg viewBox="0 0 800 600" className="w-full h-full transform scale-150 opacity-80">
              
              {/* Grid / Map Layer */}
              <g className={`transition-opacity duration-500 ${isMapVisible ? "opacity-100" : "opacity-0"}`}>
                {/* 3D-like perspective grid */}
                <path d="M 0,300 L 800,300" stroke="#ffffff10" strokeWidth="1" />
                <path d="M 0,350 L 800,350" stroke="#ffffff10" strokeWidth="1" />
                <path d="M 0,400 L 800,400" stroke="#ffffff15" strokeWidth="1" />
                <path d="M 0,470 L 800,470" stroke="#ffffff15" strokeWidth="2" />
                <path d="M 0,560 L 800,560" stroke="#ffffff20" strokeWidth="2" />

                <path d="M 400,300 L 400,600" stroke="#ffffff15" strokeWidth="2" />
                <path d="M 400,300 L 100,600" stroke="#ffffff15" strokeWidth="1" />
                <path d="M 400,300 L 700,600" stroke="#ffffff15" strokeWidth="1" />
                <path d="M 400,300 L -100,600" stroke="#ffffff10" strokeWidth="1" />
                <path d="M 400,300 L 900,600" stroke="#ffffff10" strokeWidth="1" />

                {/* Simulated obstacles (SLAM map) */}
                <path d="M 200,450 L 300,420 L 320,470 L 220,500 Z" fill="rgba(255,255,255,0.05)" stroke="rgba(255,255,255,0.2)" strokeWidth="1" />
                <path d="M 550,380 L 650,380 L 650,420 L 550,420 Z" fill="rgba(255,255,255,0.05)" stroke="rgba(255,255,255,0.2)" strokeWidth="1" />
              </g>

              {/* Lidar Scan Layer */}
              <g className={`transition-opacity duration-500 ${isLidarVisible ? "opacity-100" : "opacity-0"}`}>
                <path d="M 400,450 L 250,420 M 400,450 L 300,380 M 400,450 L 350,350 M 400,450 L 400,320 M 400,450 L 450,350 M 400,450 L 500,380 M 400,450 L 580,410" stroke={RED} strokeWidth="1" strokeOpacity="0.4" />
                {/* Lidar hits */}
                <circle cx="250" cy="420" r="2" fill={RED} />
                <circle cx="300" cy="380" r="2" fill={RED} />
                <circle cx="350" cy="350" r="2" fill={RED} />
                <circle cx="400" cy="320" r="2" fill={RED} />
                <circle cx="450" cy="350" r="2" fill={RED} />
                <circle cx="500" cy="380" r="2" fill={RED} />
                <circle cx="580" cy="410" r="2" fill={RED} />
              </g>

              {/* Point Cloud Layer */}
              <g className={`transition-opacity duration-500 ${isPclVisible ? "opacity-100" : "opacity-0"}`}>
                {/* Simulated depth points */}
                <circle cx="380" cy="400" r="1.5" fill={CYAN} />
                <circle cx="385" cy="395" r="1.5" fill={CYAN} />
                <circle cx="390" cy="390" r="1.5" fill={CYAN} />
                <circle cx="395" cy="405" r="1.5" fill={CYAN} />
                <circle cx="405" cy="398" r="1.5" fill={CYAN} />
                <circle cx="410" cy="392" r="1.5" fill={CYAN} />
                <circle cx="415" cy="402" r="1.5" fill={CYAN} />
                <circle cx="420" cy="388" r="1.5" fill={CYAN} />
                
                <circle cx="390" cy="380" r="1.5" fill={BLUE} />
                <circle cx="395" cy="375" r="1.5" fill={BLUE} />
                <circle cx="405" cy="382" r="1.5" fill={BLUE} />
                <circle cx="410" cy="378" r="1.5" fill={BLUE} />
              </g>

              {/* Robot TF (Always visible if layer is on, but we assume it's always on for this demo) */}
              <g>
                {/* Base footprint */}
                <ellipse cx="400" cy="450" rx="30" ry="15" fill="none" stroke={BLUE} strokeWidth="2" strokeDasharray="4 4" />
                {/* Axes */}
                <path d="M 400,450 L 440,450" stroke="#f00" strokeWidth="2" /> {/* X */}
                <path d="M 400,450 L 400,410" stroke="#0f0" strokeWidth="2" /> {/* Y */}
                <path d="M 400,450 L 380,480" stroke="#00f" strokeWidth="2" /> {/* Z */}
              </g>
            </svg>
          </div>
        </section>

        {/* RIGHT — 2D Streams */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          
          <div className="p-5 border-b flex flex-col gap-4" style={{ borderColor: "var(--border)" }}>
            <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>2D Live Feeds</h2>
            
            {/* Feed 1 */}
            <div className="rounded-lg overflow-hidden border relative bg-black aspect-video" style={{ borderColor: "var(--border)" }}>
              <svg viewBox="0 0 320 180" width="100%" height="100%" className="absolute inset-0">
                <rect x="0" y="100" width="320" height="80" fill="rgba(255,255,255,0.03)" />
                <line x1="0" y1="100" x2="320" y2="100" stroke="rgba(255,255,255,0.07)" />
                {[0,1,2].map((i) => (
                  <line key={`fg${i}`} x1={30-i*5} y1={100+(i+1)*20} x2={290+i*5} y2={100+(i+1)*20} stroke="rgba(255,255,255,0.02)" />
                ))}
                {[0,1,2,3,4,5,6].map((i) => {
                  const s = 320/7; const tx = 160+(i-3)*s*0.3; const bx = 160+(i-3)*s;
                  return <line key={`fvg${i}`} x1={tx} y1={100} x2={bx} y2={180} stroke="rgba(255,255,255,0.02)" />;
                })}
                <rect x="120" y="80" width="80" height="50" rx="4" fill="rgba(255,255,255,0.05)" stroke="rgba(0,212,255,0.3)" strokeWidth="1.2" />
                <circle cx="160" cy="105" r="10" fill="none" stroke="rgba(255,255,255,0.3)" strokeWidth="1" />
                <line x1="160" y1="90" x2="160" y2="98" stroke="rgba(255,255,255,0.3)" />
                <line x1="160" y1="112" x2="160" y2="120" stroke="rgba(255,255,255,0.3)" />
                <line x1="144" y1="105" x2="152" y2="105" stroke="rgba(255,255,255,0.3)" />
                <line x1="168" y1="105" x2="176" y2="105" stroke="rgba(255,255,255,0.3)" />
              </svg>
              <div className="absolute top-2 left-2 px-1.5 py-0.5 bg-black/60 rounded text-[9px] font-mono text-white">/camera/front/image_raw</div>
              <div className="absolute top-2 right-2 px-1.5 py-0.5 bg-black/60 rounded text-[9px] font-mono text-emerald-400 flex items-center gap-1">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />30.0 FPS
              </div>
            </div>

            {/* Feed 2 */}
            <div className="rounded-lg overflow-hidden border relative bg-black aspect-video" style={{ borderColor: "var(--border)" }}>
              <svg viewBox="0 0 320 180" width="100%" height="100%" className="absolute inset-0">
                <rect x="0" y="90" width="320" height="90" fill="rgba(124,58,237,0.04)" />
                <line x1="0" y1="90" x2="320" y2="90" stroke="rgba(124,58,237,0.1)" />
                {[0,1,2].map((i) => (
                  <line key={`wg${i}`} x1={20-i*5} y1={90+(i+1)*22} x2={300+i*5} y2={90+(i+1)*22} stroke="rgba(255,255,255,0.02)" />
                ))}
                <rect x="105" y="55" width="110" height="55" rx="5" fill="rgba(124,58,237,0.08)" stroke="rgba(124,58,237,0.3)" strokeWidth="1.2" />
                <ellipse cx="160" cy="82" rx="30" ry="12" fill="none" stroke="rgba(0,212,255,0.3)" strokeWidth="1" strokeDasharray="3 2" />
                <circle cx="160" cy="82" r="4" fill="rgba(0,212,255,0.4)" />
                <rect x="195" y="45" width="16" height="20" rx="2" fill="rgba(251,191,36,0.2)" stroke="rgba(251,191,36,0.4)" strokeWidth="0.8" />
                <text x="195" y="40" fontFamily="monospace" fontSize="6" fill="rgba(251,191,36,0.6)">obj·0.91</text>
              </svg>
              <div className="absolute top-2 left-2 px-1.5 py-0.5 bg-black/60 rounded text-[9px] font-mono text-white">/camera/wrist/image_raw</div>
              <div className="absolute top-2 right-2 px-1.5 py-0.5 bg-black/60 rounded text-[9px] font-mono text-emerald-400 flex items-center gap-1">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />15.2 FPS
              </div>
            </div>
          </div>

          <div className="flex-1 p-5">
            <h2 className="text-[12px] font-mono uppercase tracking-wider mb-4" style={{ color: "var(--muted)" }}>Diagnostics</h2>
            <div className="flex flex-col gap-3">
              <div className="flex justify-between items-center">
                <span className="text-[11px] text-gray-400">Total Bandwidth</span>
                <span className="text-[11px] font-mono text-gray-200">14.2 MB/s</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-[11px] text-gray-400">Latency (WebSocket)</span>
                <span className="text-[11px] font-mono text-gray-200">24 ms</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-[11px] text-gray-400">Dropped Frames</span>
                <span className="text-[11px] font-mono text-gray-200">0.02%</span>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
