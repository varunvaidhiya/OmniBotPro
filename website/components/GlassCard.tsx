"use client";

/*
 * GlassCard — an Apple-style "liquid glass" tile.
 *
 *   • frosted, translucent surface (backdrop blur + saturation) so the live
 *     3-D OmniBot behind the page shows softly through it
 *   • a specular highlight that tracks the cursor (the "liquid" sheen)
 *   • an interactive 3-D tilt (perspective + rotateX/rotateY) with depth
 *   • a scroll-reveal entrance, staggered via `delay`
 *
 * The visual styling lives in app/globals.css (.glass-* classes); this
 * component only wires up the pointer-driven CSS variables.
 */

import { useRef } from "react";
import { useScrollReveal } from "@/hooks/useScrollReveal";

const MAX_TILT = 6; // degrees of rotation at the card edges

type Accent = "none" | "cyan" | "violet";

export default function GlassCard({
  children,
  className = "",
  accent = "none",
  featured = false,
  interactive = true,
  delay = 0,
  padding = "28px",
  radius = 20,
  style,
}: {
  children: React.ReactNode;
  className?: string;
  accent?: Accent;
  featured?: boolean;
  interactive?: boolean;
  delay?: number;
  padding?: string | number;
  radius?: number;
  style?: React.CSSProperties;
}) {
  const { ref: revealRef, inView } = useScrollReveal();
  const tiltRef = useRef<HTMLDivElement>(null);

  const onMove = (e: React.MouseEvent<HTMLDivElement>) => {
    const el = tiltRef.current;
    if (!el) return;
    if (
      typeof window !== "undefined" &&
      window.matchMedia("(prefers-reduced-motion: reduce)").matches
    ) {
      return;
    }
    const r = el.getBoundingClientRect();
    const px = (e.clientX - r.left) / r.width; // 0..1 across the card
    const py = (e.clientY - r.top) / r.height; // 0..1 down the card
    el.style.setProperty("--mx", `${(px * 100).toFixed(1)}%`);
    el.style.setProperty("--my", `${(py * 100).toFixed(1)}%`);
    if (interactive) {
      el.style.setProperty("--rx", `${((0.5 - py) * 2 * MAX_TILT).toFixed(2)}deg`);
      el.style.setProperty("--ry", `${((px - 0.5) * 2 * MAX_TILT).toFixed(2)}deg`);
    }
  };

  const onLeave = () => {
    const el = tiltRef.current;
    if (!el) return;
    el.style.setProperty("--rx", "0deg");
    el.style.setProperty("--ry", "0deg");
    el.style.setProperty("--mx", "50%");
    el.style.setProperty("--my", "-20%");
  };

  const accentClass =
    accent === "cyan" ? "glass-cyan" : accent === "violet" ? "glass-violet" : "";

  return (
    <div
      ref={revealRef}
      className="glass-reveal h-full"
      style={{
        opacity: inView ? 1 : 0,
        transform: inView ? "translateY(0)" : "translateY(24px)",
        transition: "opacity .65s ease, transform .65s ease",
        transitionDelay: `${delay}s`,
      }}
    >
      <div
        ref={tiltRef}
        onMouseMove={onMove}
        onMouseLeave={onLeave}
        className={`glass-tile h-full ${accentClass} ${
          featured ? "glass-featured" : ""
        } ${className}`}
        style={{ padding, borderRadius: radius, ...style }}
      >
        {children}
      </div>
    </div>
  );
}
