"use client";

/*
 * DataConsole — the OhhO Data application shell.
 *
 * A simulated dataset viewer for reviewing teleoperated episodes. 
 * Three surfaces: episode list (left), playback viewer + timeline (centre), 
 * and 9-DOF state telemetry plots + actions (right).
 */

import Link from "next/link";
import { useMemo, useState } from "react";
import {
  ArrowLeft,
  CheckCircle2,
  Database,
  Eye,
  FileCode2,
  Filter,
  LineChart,
  ListVideo,
  UploadCloud,
  XCircle,
} from "lucide-react";

import { EPISODES, getEpisode, type Episode, type EpisodeStatus } from "@/lib/data/episodes";
import { usePlayback } from "@/lib/data/playback";
import CameraFeed from "./CameraFeed";
import EpisodeTimeline from "./EpisodeTimeline";

const CYAN = "#00D4FF";
const CYAN_DIM = "rgba(0,212,255,0.10)";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function DataConsole() {
  const [activeEpisodeId, setActiveEpisodeId] = useState(EPISODES[0].id);
  const [filter, setFilter] = useState<EpisodeStatus | "all">("all");
  const [activeCamera, setActiveCamera] = useState("wrist");

  const episode = useMemo(() => getEpisode(activeEpisodeId), [activeEpisodeId]);
  const playback = usePlayback(episode);

  const filteredEpisodes = useMemo(() => {
    if (filter === "all") return EPISODES;
    return EPISODES.filter((e) => e.status === filter);
  }, [filter]);

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
          href="/products/data"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: CYAN }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">OhhO Data</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate" style={{ color: "var(--muted)" }}>
          local/mobile_manipulation
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <Database size={11} />
            LeRobot Parquet
          </span>
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}
          >
            <ListVideo size={11} />
            {EPISODES.length} EPS
          </span>
        </span>
      </header>

      {/* ── Working area ── */}
      <div
        className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_320px] gap-px"
        style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}
      >
        {/* LEFT — Dataset & Episodes */}
        <DatasetPanel
          episodes={filteredEpisodes}
          activeId={activeEpisodeId}
          onSelect={setActiveEpisodeId}
          filter={filter}
          onFilter={setFilter}
        />

        {/* CENTER — Viewer & Timeline */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          {/* Camera tabs */}
          <div className="flex items-center gap-1.5 px-4 py-2 border-b" style={{ borderColor: "var(--border)" }}>
            {episode.cameras.map((cam) => (
              <button
                key={cam}
                onClick={() => setActiveCamera(cam)}
                className="text-[11px] font-mono uppercase tracking-wider px-2.5 py-1 rounded-lg transition-colors"
                style={{
                  background: activeCamera === cam ? CYAN_DIM : "transparent",
                  color: activeCamera === cam ? "#fff" : "var(--muted)",
                  border: `1px solid ${activeCamera === cam ? "rgba(0,212,255,.4)" : "transparent"}`,
                }}
              >
                {cam}
              </button>
            ))}
            <span className="ml-auto text-[10px] font-mono truncate max-w-[200px]" style={{ color: "var(--faint)" }}>
              {episode.instruction}
            </span>
          </div>

          {/* Camera feed */}
          <div className="relative flex-1 min-h-[240px] lg:min-h-0 p-3">
            <CameraFeed episode={episode} frame={playback.frame} activeCamera={activeCamera} />
          </div>

          {/* Timeline Scrubber */}
          <EpisodeTimeline
            episode={episode}
            frame={playback.frame}
            playing={playback.playing}
            onTogglePlayback={playback.togglePlayback}
            onSeek={playback.seek}
          />
        </section>

        {/* RIGHT — Telemetry & Actions */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <TelemetryPanel episode={episode} frame={playback.frame} />
          <ActionsPanel episode={episode} />
        </aside>
      </div>
    </div>
  );
}

// ── Left panel ────────────────────────────────────────────────────────────────

function DatasetPanel({
  episodes,
  activeId,
  onSelect,
  filter,
  onFilter,
}: {
  episodes: Episode[];
  activeId: string;
  onSelect: (id: string) => void;
  filter: string;
  onFilter: (f: EpisodeStatus | "all") => void;
}) {
  return (
    <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
      {/* Filters */}
      <div className="px-4 pt-4 pb-2">
        <div className="flex items-center gap-2 mb-3">
          <Filter size={15} color={CYAN} />
          <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>
            Episodes
          </h2>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {(["all", "kept", "discard", "review"] as const).map((f) => (
            <button
              key={f}
              onClick={() => onFilter(f)}
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

      {/* Episode List */}
      <div className="flex-1 overflow-y-auto px-2 pb-4">
        <div className="flex px-3 pb-2 text-[9px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>
          <span className="w-16">ID</span>
          <span className="flex-1">Task</span>
          <span className="w-12 text-right">Frames</span>
          <span className="w-16 text-right">Status</span>
        </div>
        
        <div className="flex flex-col gap-0.5">
          {episodes.map((ep) => {
            const on = activeId === ep.id;
            const c = ep.status === "kept" ? GREEN : ep.status === "discard" ? RED : AMBER;
            return (
              <button
                key={ep.id}
                onClick={() => onSelect(ep.id)}
                className="flex items-center px-3 py-2.5 rounded-lg text-left transition-all group"
                style={{
                  background: on ? CYAN_DIM : "transparent",
                  border: `1px solid ${on ? "rgba(0,212,255,0.3)" : "transparent"}`,
                }}
              >
                <span className="w-16 text-[11px] font-mono" style={{ color: on ? "#fff" : "var(--muted)" }}>
                  {ep.id}
                </span>
                <span className="flex-1 text-[11.5px] truncate mr-2" style={{ color: on ? "rgba(255,255,255,0.9)" : "var(--faint)" }}>
                  {ep.instruction}
                </span>
                <span className="w-12 text-[10px] font-mono text-right" style={{ color: "var(--faint)" }}>
                  {ep.frames}
                </span>
                <span className="w-16 flex justify-end items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full" style={{ background: c }} />
                  <span className="text-[10px] font-mono" style={{ color: c }}>
                    {ep.status}
                  </span>
                </span>
              </button>
            );
          })}
        </div>
      </div>
    </aside>
  );
}

// ── Right panel: Telemetry ────────────────────────────────────────────────────

function TelemetryPanel({ episode, frame }: { episode: Episode; frame: number }) {
  const progress = episode.frames > 1 ? frame / (episode.frames - 1) : 0;

  // Simple SVG sparkline generator for timeseries data
  const renderPlot = (data: number[][], height: number, color: string) => {
    const W = 280;
    const H = height;
    
    // We plot the first dimension (e.g. first arm joint or forward velocity)
    const series = data.map(d => d[0]);
    
    const max = Math.max(...series, 1);
    const min = Math.min(...series, 0);
    const range = max - min || 1;

    const points = series
      .map((val, i) => {
        const x = (i / (series.length - 1)) * W;
        const y = H - ((val - min) / range) * H * 0.8 - H * 0.1;
        return `${i === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(1)}`;
      })
      .join(" ");

    const areaPath = points + ` L${W} ${H} L0 ${H} Z`;
    
    const playheadX = progress * W;

    return (
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" height={H} className="block mt-2 rounded-md overflow-hidden bg-black/20">
        <path d={areaPath} fill={color} fillOpacity={0.1} />
        <path d={points} fill="none" stroke={color} strokeWidth={1.5} strokeLinecap="round" strokeLinejoin="round" />
        {/* Playhead indicator on the chart */}
        <line x1={playheadX} y1={0} x2={playheadX} y2={H} stroke="#fff" strokeWidth={1} strokeDasharray="3 2" opacity={0.5} />
        <circle cx={playheadX} cy={H - ((series[frame] - min) / range) * H * 0.8 - H * 0.1} r={3} fill="#fff" stroke={color} strokeWidth={1.5} />
      </svg>
    );
  };

  return (
    <div className="border-b" style={{ borderColor: "var(--border)" }}>
      <div className="flex items-center gap-2 px-4 pt-4 pb-2">
        <LineChart size={15} color={CYAN} />
        <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>
          9-DOF State
        </h2>
      </div>

      <div className="px-4 pb-4">
        <div className="mb-4">
          <div className="flex justify-between items-baseline">
            <span className="text-[10px] font-mono uppercase" style={{ color: "var(--faint)" }}>Arm Joints (rad)</span>
            <span className="text-[11px] font-mono tabular-nums text-white">
              {episode.states.arm[frame][0].toFixed(2)}
            </span>
          </div>
          {renderPlot(episode.states.arm, 70, "#A78BFA")}
        </div>

        <div>
          <div className="flex justify-between items-baseline">
            <span className="text-[10px] font-mono uppercase" style={{ color: "var(--faint)" }}>Base Velocity (m/s)</span>
            <span className="text-[11px] font-mono tabular-nums text-white">
              {episode.states.base[frame][0].toFixed(2)}
            </span>
          </div>
          {renderPlot(episode.states.base, 70, CYAN)}
        </div>
      </div>
    </div>
  );
}

// ── Right panel: Actions ────────────────────────────────────────────────────

function ActionsPanel({ episode }: { episode: Episode }) {
  const statusColor = episode.status === "kept" ? GREEN : episode.status === "discard" ? RED : AMBER;
  const StatusIcon = episode.status === "kept" ? CheckCircle2 : episode.status === "discard" ? XCircle : Eye;

  return (
    <div className="p-4 flex flex-col gap-3">
      <div className="flex items-center justify-between p-3 rounded-lg" style={{ background: "rgba(255,255,255,0.02)", border: "1px solid var(--border)" }}>
        <span className="text-[11.5px] font-medium" style={{ color: "var(--muted)" }}>Status</span>
        <span className="flex items-center gap-1.5 text-[11px] font-mono uppercase px-2 py-1 rounded bg-black/20" style={{ color: statusColor }}>
          <StatusIcon size={12} />
          {episode.status}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-2 mt-2">
        <button className="flex flex-col items-center justify-center py-3 rounded-lg border transition-colors hover:bg-white/5" style={{ borderColor: "var(--border)" }}>
          <CheckCircle2 size={16} className="mb-1" color={GREEN} />
          <span className="text-[10px] font-mono text-gray-400">Mark Kept</span>
        </button>
        <button className="flex flex-col items-center justify-center py-3 rounded-lg border transition-colors hover:bg-white/5" style={{ borderColor: "var(--border)" }}>
          <XCircle size={16} className="mb-1" color={RED} />
          <span className="text-[10px] font-mono text-gray-400">Mark Discard</span>
        </button>
      </div>

      <button className="mt-4 w-full inline-flex items-center justify-center gap-2 text-[13px] font-semibold py-2.5 rounded-lg transition-all hover:-translate-y-px" style={{ background: CYAN, color: "var(--bg)" }}>
        <UploadCloud size={14} />
        Push to Hugging Face
      </button>

      <button className="mt-1 w-full inline-flex items-center justify-center gap-2 text-[12px] font-medium py-2 rounded-lg transition-all hover:bg-white/5" style={{ color: "var(--muted)" }}>
        <FileCode2 size={13} />
        Export Parquet
      </button>
    </div>
  );
}
