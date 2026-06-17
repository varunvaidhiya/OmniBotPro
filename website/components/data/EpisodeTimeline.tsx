"use client";

/*
 * EpisodeTimeline — an interactive scrubber and playback control for OhhO Data.
 *
 * Allows playing, pausing, and scrubbing through an episode's frames. Renders
 * keyframe markers on the track.
 */

import { useCallback, useRef } from "react";
import { Pause, Play } from "lucide-react";
import type { Episode } from "@/lib/data/episodes";

interface Props {
  episode: Episode;
  frame: number;
  playing: boolean;
  onTogglePlayback: () => void;
  onSeek: (frame: number) => void;
}

const CYAN = "#00D4FF";

export default function EpisodeTimeline({
  episode,
  frame,
  playing,
  onTogglePlayback,
  onSeek,
}: Props) {
  const trackRef = useRef<HTMLDivElement>(null);
  const progress = episode.frames > 1 ? frame / (episode.frames - 1) : 0;

  const handlePointerEvent = useCallback(
    (e: React.PointerEvent) => {
      if (e.buttons !== 1) return; // Only trigger on left-click drag
      if (!trackRef.current) return;

      const rect = trackRef.current.getBoundingClientRect();
      const x = Math.max(0, Math.min(rect.width, e.clientX - rect.left));
      const percentage = x / rect.width;
      onSeek(Math.floor(percentage * (episode.frames - 1)));
    },
    [episode.frames, onSeek]
  );

  const formatTime = (f: number) => {
    const totalSecs = Math.floor(f / 30);
    const m = Math.floor(totalSecs / 60);
    const s = totalSecs % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  };

  return (
    <div className="flex flex-col px-4 py-4 border-t" style={{ borderColor: "var(--border)", background: "var(--surf)" }}>
      {/* Time and Controls */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-3">
          <button
            onClick={onTogglePlayback}
            className="w-8 h-8 rounded-full flex items-center justify-center transition-colors"
            style={{ background: playing ? "rgba(0,212,255,0.15)" : "rgba(255,255,255,0.05)", color: playing ? CYAN : "#fff" }}
          >
            {playing ? <Pause size={14} fill="currentColor" /> : <Play size={14} fill="currentColor" className="ml-0.5" />}
          </button>
          <span className="text-[12px] font-mono tabular-nums" style={{ color: "var(--muted)" }}>
            <span style={{ color: "#fff" }}>{formatTime(frame)}</span> / {formatTime(episode.frames - 1)}
          </span>
        </div>
        
        <span className="text-[10px] font-mono tracking-wider" style={{ color: "var(--faint)" }}>
          {episode.frames} FRAMES @ 30FPS
        </span>
      </div>

      {/* Scrubber Track */}
      <div
        ref={trackRef}
        onPointerDown={(e) => {
          (e.target as HTMLElement).setPointerCapture(e.pointerId);
          handlePointerEvent(e);
        }}
        onPointerMove={handlePointerEvent}
        className="relative h-6 cursor-pointer touch-none group"
      >
        {/* Background Track */}
        <div className="absolute top-1/2 -translate-y-1/2 left-0 right-0 h-1.5 rounded-full" style={{ background: "rgba(255,255,255,0.08)" }} />
        
        {/* Progress Fill */}
        <div
          className="absolute top-1/2 -translate-y-1/2 left-0 h-1.5 rounded-full pointer-events-none"
          style={{ width: `${progress * 100}%`, background: CYAN }}
        />

        {/* Keyframe Markers */}
        {episode.keyFrames.map((kf, i) => {
          const kfProgress = kf / (episode.frames - 1);
          return (
            <div
              key={i}
              className="absolute top-1/2 -translate-y-1/2 w-1.5 h-1.5 rounded-full pointer-events-none"
              style={{ left: `${kfProgress * 100}%`, background: "#fff", transform: "translate(-50%, -50%)" }}
            />
          );
        })}

        {/* Playhead Thumb */}
        <div
          className="absolute top-1/2 -translate-y-1/2 w-3.5 h-3.5 rounded-full shadow-lg pointer-events-none group-active:scale-125 transition-transform"
          style={{
            left: `${progress * 100}%`,
            background: "#fff",
            border: `2px solid ${CYAN}`,
            transform: "translate(-50%, -50%)",
          }}
        />
      </div>
    </div>
  );
}
