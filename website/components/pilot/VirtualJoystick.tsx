"use client";

/*
 * VirtualJoystick — a circular touch/mouse joystick for the Pilot teleop console.
 *
 * Returns normalised { x, y } in [-1, 1] via onMove. Supports pointer events
 * for cross-device (mouse + touch) compatibility. Visual: outer ring, inner nub
 * that follows the pointer, and a velocity vector line.
 */

import { useCallback, useRef, useState } from "react";

interface Props {
  /** Radius in px. */
  size?: number;
  /** Called continuously while dragging with normalised coords. */
  onMove: (x: number, y: number) => void;
  /** Called when the joystick is released. */
  onRelease: () => void;
  /** Accent colour for the active state. */
  accent?: string;
}

export default function VirtualJoystick({
  size = 140,
  onMove,
  onRelease,
  accent = "#A78BFA",
}: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [pos, setPos] = useState({ x: 0, y: 0 });
  const [active, setActive] = useState(false);
  const activeRef = useRef(false);

  const r = size / 2;
  const nubR = size * 0.18;

  const project = useCallback(
    (clientX: number, clientY: number) => {
      const el = containerRef.current;
      if (!el) return { x: 0, y: 0 };
      const rect = el.getBoundingClientRect();
      const cx = rect.left + rect.width / 2;
      const cy = rect.top + rect.height / 2;
      let dx = clientX - cx;
      let dy = clientY - cy;
      const dist = Math.sqrt(dx * dx + dy * dy);
      const maxDist = r - nubR;
      if (dist > maxDist) {
        dx = (dx / dist) * maxDist;
        dy = (dy / dist) * maxDist;
      }
      return { x: dx / maxDist, y: dy / maxDist };
    },
    [r, nubR],
  );

  const handleStart = useCallback(
    (e: React.PointerEvent) => {
      e.preventDefault();
      (e.target as HTMLElement).setPointerCapture(e.pointerId);
      activeRef.current = true;
      setActive(true);
      const p = project(e.clientX, e.clientY);
      setPos(p);
      onMove(p.x, p.y);
    },
    [project, onMove],
  );

  const handleMove = useCallback(
    (e: React.PointerEvent) => {
      if (!activeRef.current) return;
      const p = project(e.clientX, e.clientY);
      setPos(p);
      onMove(p.x, p.y);
    },
    [project, onMove],
  );

  const handleEnd = useCallback(() => {
    activeRef.current = false;
    setActive(false);
    setPos({ x: 0, y: 0 });
    onRelease();
  }, [onRelease]);

  const nubX = pos.x * (r - nubR);
  const nubY = pos.y * (r - nubR);

  return (
    <div
      ref={containerRef}
      onPointerDown={handleStart}
      onPointerMove={handleMove}
      onPointerUp={handleEnd}
      onPointerCancel={handleEnd}
      className="relative select-none touch-none cursor-grab active:cursor-grabbing"
      style={{ width: size, height: size }}
    >
      {/* outer ring */}
      <svg
        viewBox={`0 0 ${size} ${size}`}
        width={size}
        height={size}
        className="absolute inset-0"
      >
        {/* background circle */}
        <circle
          cx={r}
          cy={r}
          r={r - 2}
          fill="rgba(255,255,255,0.03)"
          stroke={active ? accent : "rgba(255,255,255,0.12)"}
          strokeWidth={2}
        />
        {/* crosshairs */}
        <line
          x1={r}
          y1={8}
          x2={r}
          y2={size - 8}
          stroke="rgba(255,255,255,0.06)"
          strokeWidth={1}
        />
        <line
          x1={8}
          y1={r}
          x2={size - 8}
          y2={r}
          stroke="rgba(255,255,255,0.06)"
          strokeWidth={1}
        />
        {/* velocity vector line */}
        {active && (
          <line
            x1={r}
            y1={r}
            x2={r + nubX}
            y2={r + nubY}
            stroke={accent}
            strokeWidth={2}
            strokeLinecap="round"
            opacity={0.6}
          />
        )}
        {/* nub */}
        <circle
          cx={r + nubX}
          cy={r + nubY}
          r={nubR}
          fill={active ? accent : "rgba(255,255,255,0.15)"}
          stroke={active ? accent : "rgba(255,255,255,0.25)"}
          strokeWidth={1.5}
          style={{ transition: active ? "none" : "all 0.15s ease-out" }}
        />
      </svg>
    </div>
  );
}
