"use client";

/*
 * FleetMap — an SVG-based live map for OhhO Fleet.
 *
 * Renders a warehouse grid and places robot pins based on their simulated
 * coordinates. Highlights the active robot if selected.
 */

import type { Robot } from "@/lib/fleet/fleet";

interface Props {
  robots: Robot[];
  activeId: string | null;
  onSelect: (id: string) => void;
}

const W = 400;
const H = 300;

const CYAN = "#00D4FF";
const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";

export default function FleetMap({ robots, activeId, onSelect }: Props) {
  // SVG grid lines
  const vLines = [];
  for (let x = 0; x <= W; x += 40) {
    vLines.push(<line key={`v${x}`} x1={x} y1={0} x2={x} y2={H} stroke="rgba(255,255,255,0.03)" />);
  }
  const hLines = [];
  for (let y = 0; y <= H; y += 40) {
    hLines.push(<line key={`h${y}`} x1={0} y1={y} x2={W} y2={y} stroke="rgba(255,255,255,0.03)" />);
  }

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      width="100%"
      height="100%"
      className="block"
      style={{ background: "#0A0F1C" }}
    >
      <defs>
        <pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse">
          <path d="M 40 0 L 0 0 0 40" fill="none" stroke="rgba(255,255,255,0.02)" strokeWidth="1" />
        </pattern>
        <pattern id="dots" width="10" height="10" patternUnits="userSpaceOnUse">
          <circle cx="1" cy="1" r="0.5" fill="rgba(255,255,255,0.04)" />
        </pattern>
      </defs>

      {/* Floors */}
      <rect width={W} height={H} fill="url(#dots)" />
      {vLines}
      {hLines}

      {/* Mock Static Structures */}
      <rect x={40} y={40} width={80} height={40} fill="rgba(255,255,255,0.02)" stroke="rgba(255,255,255,0.05)" />
      <rect x={40} y={120} width={80} height={40} fill="rgba(255,255,255,0.02)" stroke="rgba(255,255,255,0.05)" />
      <rect x={280} y={80} width={40} height={160} fill="rgba(255,255,255,0.02)" stroke="rgba(255,255,255,0.05)" />
      <text x={45} y={55} fill="rgba(255,255,255,0.2)" fontSize={8} fontFamily="monospace">ZONE_A</text>
      <text x={285} y={95} fill="rgba(255,255,255,0.2)" fontSize={8} fontFamily="monospace">CHARGERS</text>

      {/* Robots */}
      {robots.map((r) => {
        const c = r.status === "online" ? GREEN : r.status === "degraded" ? AMBER : RED;
        const isActive = activeId === r.id;

        return (
          <g
            key={r.id}
            transform={`translate(${r.x}, ${r.y})`}
            onClick={() => onSelect(r.id)}
            className="cursor-pointer"
            style={{ transition: "transform 0.05s linear" }} // smooth the Raf steps
          >
            {/* Ping animation if active or moving */}
            {isActive && (
              <circle cx={0} cy={0} r={16} fill={CYAN} fillOpacity={0.15}>
                <animate attributeName="r" values="8;20" dur="1.5s" repeatCount="indefinite" />
                <animate attributeName="opacity" values="1;0" dur="1.5s" repeatCount="indefinite" />
              </circle>
            )}
            
            <circle cx={0} cy={0} r={r.status === "online" ? 10 : 8} fill={c} fillOpacity={isActive ? 0.3 : 0.15} />
            <circle cx={0} cy={0} r={isActive ? 4 : 3} fill={isActive ? CYAN : c} />
            
            {/* Label */}
            {isActive && (
              <g transform="translate(12, 4)">
                <rect x={0} y={-10} width={42} height={14} rx={2} fill="rgba(0,0,0,0.6)" stroke={CYAN} strokeWidth={0.5} />
                <text x={4} y={0} fill={CYAN} fontSize={8} fontFamily="monospace">{r.id}</text>
              </g>
            )}
          </g>
        );
      })}
    </svg>
  );
}
