"use client";

/*
 * CameraFeed — an SVG-based simulated playback feed for OhhO Data.
 *
 * Simulates a recorded episode being played back. The crosshair/manipulator
 * moves from a starting position to a target object as the episode progresses,
 * simulating a pick/place task. The background has perspective lines to give
 * a sense of space.
 */

import type { Episode } from "@/lib/data/episodes";

interface Props {
  episode: Episode;
  frame: number;
  activeCamera: string;
}

const W = 560;
const H = 320;

const CYAN = "#00D4FF";
const AMBER = "#FBBF24";

export default function CameraFeed({ episode, frame, activeCamera }: Props) {
  const progress = episode.frames > 1 ? frame / (episode.frames - 1) : 0;

  // Simulate a task: moving to an object, grasping it at 50% progress,
  // and lifting/moving it.
  
  // Target object starts at (x: 0.6, y: 0.55)
  const targetStartX = W * 0.6;
  const targetStartY = H * 0.55;
  const targetLiftY = H * 0.35;
  
  // Manipulator (crosshair) starts at (x: 0.2, y: 0.8)
  const manStartX = W * 0.2;
  const manStartY = H * 0.8;
  
  let manX = manStartX;
  let manY = manStartY;
  let targetX = targetStartX;
  let targetY = targetStartY;
  let grasped = false;

  if (progress < 0.5) {
    // Approach phase
    const t = progress / 0.5; // 0 to 1
    // Easing function for smoother approach
    const ease = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    manX = manStartX + (targetStartX - manStartX) * ease;
    manY = manStartY + (targetStartY - manStartY) * ease;
  } else {
    // Lift/Move phase
    grasped = true;
    const t = (progress - 0.5) / 0.5; // 0 to 1
    const ease = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    
    // Move up and left
    manX = targetStartX - (targetStartX * 0.3 * ease);
    manY = targetStartY - ((targetStartY - targetLiftY) * ease);
    
    // Target moves with manipulator
    targetX = manX;
    targetY = manY;
  }

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      width="100%"
      className="block rounded-xl"
      style={{ background: "#0A0F1C" }}
    >
      {/* Background (Floor & Walls) */}
      <rect x={0} y={H * 0.5} width={W} height={H * 0.5} fill="rgba(255,255,255,0.02)" />
      <line x1={0} y1={H * 0.5} x2={W} y2={H * 0.5} stroke="rgba(255,255,255,0.06)" />
      
      {/* Perspective Grid */}
      {[0, 1, 2, 3].map((i) => (
        <line
          key={`hg${i}`}
          x1={30 - i * 10}
          y1={H * 0.5 + (i + 1) * (H * 0.5) / 5}
          x2={W - 30 + i * 10}
          y2={H * 0.5 + (i + 1) * (H * 0.5) / 5}
          stroke="rgba(255,255,255,0.03)"
        />
      ))}
      {[0, 1, 2, 3, 4, 5, 6].map((i) => {
        const spacing = W / 7;
        const topX = W / 2 + (i - 3) * spacing * 0.4;
        const botX = W / 2 + (i - 3) * spacing;
        return (
          <line
            key={`vg${i}`}
            x1={topX}
            y1={H * 0.5}
            x2={botX}
            y2={H}
            stroke="rgba(255,255,255,0.02)"
          />
        );
      })}

      {/* Target Object */}
      <g transform={`translate(${targetX}, ${targetY})`}>
        <rect x={-20} y={-24} width={40} height={48} rx={4} fill={AMBER} fillOpacity={0.25} stroke={AMBER} strokeWidth={1.4} />
        <rect x={-24} y={-28} width={48} height={56} rx={3} fill="none" stroke={CYAN} strokeWidth={1.6} strokeDasharray="5 3" opacity={grasped ? 1 : 0.4} />
        <text x={-24} y={-34} fontFamily="'JetBrains Mono', monospace" fontSize={9} fill={CYAN} opacity={grasped ? 1 : 0.6}>
          {grasped ? "grasped_obj" : "target_obj"} · {(0.85 + Math.random() * 0.1).toFixed(2)}
        </text>
      </g>

      {/* Secondary Distractor Object */}
      <g transform={`translate(${W * 0.3}, ${H * 0.65})`}>
        <rect x={-15} y={-15} width={30} height={30} rx={4} fill="rgba(255,255,255,0.15)" stroke="rgba(255,255,255,0.3)" strokeWidth={1} />
        <text x={-15} y={-20} fontFamily="'JetBrains Mono', monospace" fontSize={8} fill="rgba(255,255,255,0.5)">
          distractor
        </text>
      </g>

      {/* Manipulator Crosshair */}
      <g transform={`translate(${manX}, ${manY})`}>
        <circle cx={0} cy={0} r={16} fill="none" stroke={grasped ? CYAN : "rgba(255,255,255,0.4)"} strokeWidth={1.2} />
        <line x1={0} y1={-24} x2={0} y2={-12} stroke={grasped ? CYAN : "rgba(255,255,255,0.4)"} strokeWidth={1.2} />
        <line x1={0} y1={12} x2={0} y2={24} stroke={grasped ? CYAN : "rgba(255,255,255,0.4)"} strokeWidth={1.2} />
        <line x1={-24} y1={0} x2={-12} y2={0} stroke={grasped ? CYAN : "rgba(255,255,255,0.4)"} strokeWidth={1.2} />
        <line x1={12} y1={0} x2={24} y2={0} stroke={grasped ? CYAN : "rgba(255,255,255,0.4)"} strokeWidth={1.2} />
      </g>

      {/* HUD Info */}
      <rect x={12} y={12} width={110} height={20} rx={4} fill="rgba(0,0,0,0.4)" />
      <text x={18} y={25} fontFamily="'JetBrains Mono', monospace" fontSize={9} fill={CYAN}>
        ● {activeCamera}_cam
      </text>

      <rect x={W - 140} y={12} width={128} height={20} rx={4} fill="rgba(0,0,0,0.4)" />
      <text x={W - 134} y={25} fontFamily="'JetBrains Mono', monospace" fontSize={9} fill="rgba(255,255,255,0.7)">
        FRAME: {frame.toString().padStart(4, "0")} / {episode.frames}
      </text>
    </svg>
  );
}
