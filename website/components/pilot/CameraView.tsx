"use client";

/*
 * CameraView — an SVG-based simulated camera feed for OhhO Pilot.
 *
 * Renders a first-person robot view with: perspective floor grid, obstacles,
 * bounding boxes with confidence labels, a centre crosshair, and HUD overlays
 * (REC indicator, hand-tracking hint). All drawing is pure SVG — same technique
 * as the product dashboard mockups in dashboardKit.tsx, but animated via
 * requestAnimationFrame.
 */

import { useEffect, useRef, useState } from "react";
import {
  type ScenePreset,
  type SceneState,
  initSceneState,
  tickScene,
} from "@/lib/pilot/scene";

interface Props {
  scene: ScenePreset;
  /** Currently active camera label. */
  activeCamera: string;
  /** Whether connected to the robot. */
  connected: boolean;
  /** Whether e-stop is engaged. */
  eStop: boolean;
}

const W = 560;
const H = 320;

const VIOLET = "#A78BFA";
const AMBER = "#FBBF24";
const RED = "#F87171";
const GREEN = "#34D399";

export default function CameraView({ scene, activeCamera, connected, eStop }: Props) {
  const [state, setState] = useState<SceneState>(() => initSceneState(scene));
  const sceneRef = useRef(scene);
  const rafRef = useRef<number>(0);
  const stateRef = useRef(state);

  // Reset when scene changes
  useEffect(() => {
    sceneRef.current = scene;
    const fresh = initSceneState(scene);
    stateRef.current = fresh;
    setState(fresh);
  }, [scene]);

  // Animation loop
  useEffect(() => {
    if (!connected) return;
    let frame = 0;
    const loop = () => {
      frame++;
      // Tick at ~15 fps (every 4th rAF at 60fps)
      if (frame % 4 === 0) {
        const next = tickScene(stateRef.current, sceneRef.current);
        stateRef.current = next;
        setState(next);
      }
      rafRef.current = requestAnimationFrame(loop);
    };
    rafRef.current = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(rafRef.current);
  }, [connected]);

  const { primary, secondary } = state;

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      width="100%"
      className="block rounded-xl"
      style={{ background: "#0A1322" }}
    >
      {/* Horizon line */}
      <rect x={0} y={H * 0.47} width={W} height={H * 0.53} rx={0} fill="rgba(255,255,255,0.02)" />
      <line x1={0} y1={H * 0.47} x2={W} y2={H * 0.47} stroke="rgba(255,255,255,0.06)" />

      {/* Perspective floor grid */}
      {[0, 1, 2, 3].map((i) => (
        <line
          key={`hg${i}`}
          x1={40 - i * 10}
          y1={H * 0.47 + (i + 1) * (H * 0.53) / 5}
          x2={W - 40 + i * 10}
          y2={H * 0.47 + (i + 1) * (H * 0.53) / 5}
          stroke="rgba(255,255,255,0.04)"
        />
      ))}
      {[0, 1, 2, 3, 4, 5, 6].map((i) => {
        const spacing = W / 7;
        const topX = W / 2 + (i - 3) * spacing * 0.3;
        const botX = W / 2 + (i - 3) * spacing;
        return (
          <line
            key={`vg${i}`}
            x1={topX}
            y1={H * 0.47}
            x2={botX}
            y2={H}
            stroke="rgba(255,255,255,0.03)"
          />
        );
      })}

      {/* Static obstacles */}
      {scene.obstacles.map((obs, i) => (
        <rect
          key={`obs${i}`}
          x={obs.x * W}
          y={obs.y * H}
          width={obs.w * W}
          height={obs.h * H}
          rx={4}
          fill={obs.color}
        />
      ))}

      {/* Secondary detected objects */}
      {secondary.map((obj, i) => (
        <g key={`sec${i}`}>
          <rect
            x={(obj.x - obj.hw) * W}
            y={(obj.y - obj.hh) * H}
            width={obj.hw * 2 * W}
            height={obj.hh * 2 * H}
            rx={3}
            fill={obj.color}
            fillOpacity={0.15}
            stroke={obj.color}
            strokeWidth={1}
            strokeOpacity={0.5}
          />
          <text
            x={(obj.x - obj.hw) * W}
            y={(obj.y - obj.hh) * H - 4}
            fontFamily="'JetBrains Mono', monospace"
            fontSize={9}
            fill={obj.color}
            fillOpacity={0.7}
          >
            {obj.label} · {obj.confidence.toFixed(2)}
          </text>
        </g>
      ))}

      {/* Primary detected object with dashed bbox */}
      <rect
        x={(primary.x - primary.hw) * W}
        y={(primary.y - primary.hh) * H}
        width={primary.hw * 2 * W}
        height={primary.hh * 2 * H}
        rx={4}
        fill={primary.color}
        fillOpacity={0.2}
        stroke={primary.color}
        strokeWidth={1.4}
      />
      <rect
        x={(primary.x - primary.hw - 0.01) * W}
        y={(primary.y - primary.hh - 0.015) * H}
        width={(primary.hw * 2 + 0.02) * W}
        height={(primary.hh * 2 + 0.03) * H}
        rx={4}
        fill="none"
        stroke={VIOLET}
        strokeWidth={1.6}
        strokeDasharray="5 3"
      />
      <text
        x={(primary.x - primary.hw - 0.01) * W}
        y={(primary.y - primary.hh - 0.025) * H}
        fontFamily="'JetBrains Mono', monospace"
        fontSize={10}
        fill={VIOLET}
      >
        {primary.label} · {primary.confidence.toFixed(2)}
      </text>

      {/* Centre crosshair */}
      <circle cx={W / 2} cy={H * 0.42} r={18} fill="none" stroke="rgba(255,255,255,0.35)" strokeWidth={1.2} />
      <line x1={W / 2} y1={H * 0.42 - 24} x2={W / 2} y2={H * 0.42 - 14} stroke="rgba(255,255,255,0.35)" strokeWidth={1.2} />
      <line x1={W / 2} y1={H * 0.42 + 14} x2={W / 2} y2={H * 0.42 + 24} stroke="rgba(255,255,255,0.35)" strokeWidth={1.2} />
      <line x1={W / 2 - 24} y1={H * 0.42} x2={W / 2 - 14} y2={H * 0.42} stroke="rgba(255,255,255,0.35)" strokeWidth={1.2} />
      <line x1={W / 2 + 14} y1={H * 0.42} x2={W / 2 + 24} y2={H * 0.42} stroke="rgba(255,255,255,0.35)" strokeWidth={1.2} />

      {/* REC indicator */}
      {connected && (
        <g>
          <rect x={12} y={12} width={90} height={20} rx={10} fill="rgba(248,113,113,0.18)" stroke={RED} strokeOpacity={0.6} />
          <circle cx={26} cy={22} r={3} fill={RED}>
            <animate attributeName="opacity" values="1;0.3;1" dur="1.6s" repeatCount="indefinite" />
          </circle>
          <text x={35} y={26} fontFamily="'JetBrains Mono', monospace" fontSize={9} fill="rgba(255,255,255,0.85)">
            REC · {activeCamera}
          </text>
        </g>
      )}

      {/* Hand tracking hint */}
      {connected && (
        <text
          x={W - 12}
          y={26}
          fontFamily="'JetBrains Mono', monospace"
          fontSize={9}
          fill={VIOLET}
          textAnchor="end"
        >
          ✋ hand-tracking
        </text>
      )}

      {/* E-STOP overlay */}
      {eStop && (
        <g>
          <rect x={0} y={0} width={W} height={H} fill="rgba(248,113,113,0.08)" />
          <rect x={W / 2 - 80} y={H / 2 - 20} width={160} height={40} rx={8} fill="rgba(248,113,113,0.2)" stroke={RED} strokeWidth={2} />
          <text
            x={W / 2}
            y={H / 2 + 6}
            fontFamily="'Space Grotesk', sans-serif"
            fontSize={18}
            fontWeight={700}
            fill={RED}
            textAnchor="middle"
          >
            E-STOP ACTIVE
          </text>
        </g>
      )}

      {/* Not connected overlay */}
      {!connected && (
        <g>
          <rect x={0} y={0} width={W} height={H} fill="rgba(10,14,26,0.7)" />
          <text
            x={W / 2}
            y={H / 2 - 6}
            fontFamily="'Space Grotesk', sans-serif"
            fontSize={16}
            fontWeight={600}
            fill="rgba(255,255,255,0.5)"
            textAnchor="middle"
          >
            No connection
          </text>
          <text
            x={W / 2}
            y={H / 2 + 16}
            fontFamily="'JetBrains Mono', monospace"
            fontSize={10}
            fill="rgba(255,255,255,0.3)"
            textAnchor="middle"
          >
            Connect to a robot to start the feed
          </text>
        </g>
      )}
    </svg>
  );
}
