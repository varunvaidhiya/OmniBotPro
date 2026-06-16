/*
 * dashboardKit — small set of SVG primitives for the product "dashboard"
 * mockups embedded on each Learn-more page.
 *
 * These are pure, presentational SVG (no hooks, no client state) so they render
 * fine inside a server component and export statically. Everything draws inside
 * a 560 × 320 viewBox via <DashboardFrame>.
 *
 * Colours are explicit hex (mirroring the tokens in app/globals.css) so a single
 * mock is self-contained and looks identical wherever it's embedded.
 */

import type { ReactNode } from "react";

export const C = {
  bg: "#0A0E1A",
  surf: "#0F1628",
  surfHi: "#141D35",
  panel: "#131C33",
  panelHi: "#18233f",
  border: "rgba(255,255,255,0.08)",
  borderHi: "rgba(255,255,255,0.16)",
  track: "rgba(255,255,255,0.09)",
  grid: "rgba(255,255,255,0.05)",
  cyan: "#00D4FF",
  violet: "#7C3AED",
  violetLite: "#A78BFA",
  text: "#FFFFFF",
  muted: "rgba(255,255,255,0.56)",
  faint: "rgba(255,255,255,0.30)",
  green: "#34D399",
  amber: "#FBBF24",
  red: "#F87171",
  blue: "#60A5FA",
};

export const MONO = "'JetBrains Mono', ui-monospace, monospace";
export const DISPLAY = "'Space Grotesk', system-ui, sans-serif";
export const BODY = "'Inter', system-ui, sans-serif";

export type Accent = "cyan" | "violet";
export const accentHex = (a: Accent) => (a === "cyan" ? C.cyan : C.violetLite);

/** Window chrome that frames every dashboard mockup. */
export function DashboardFrame({
  title,
  accent = "cyan",
  tools = [],
  children,
}: {
  title: string;
  accent?: Accent;
  tools?: string[];
  children: ReactNode;
}) {
  const a = accentHex(accent);
  return (
    <svg
      viewBox="0 0 560 320"
      width="100%"
      role="img"
      aria-label={`${title} interface mockup`}
      style={{ display: "block" }}
    >
      <defs>
        <linearGradient id={`fr-bg-${accent}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor={C.surf} />
          <stop offset="1" stopColor={C.bg} />
        </linearGradient>
        <linearGradient id={`fr-glow-${accent}`} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor={a} stopOpacity="0.16" />
          <stop offset="0.5" stopColor={a} stopOpacity="0" />
        </linearGradient>
      </defs>

      {/* board */}
      <rect x="1" y="1" width="558" height="318" rx="16" fill={`url(#fr-bg-${accent})`} stroke={C.borderHi} />
      <rect x="1" y="1" width="558" height="318" rx="16" fill={`url(#fr-glow-${accent})`} />

      {/* title bar */}
      <rect x="1" y="1" width="558" height="36" rx="16" fill={C.surfHi} />
      <rect x="1" y="20" width="558" height="17" fill={C.surfHi} />
      <line x1="1" y1="37" x2="559" y2="37" stroke={C.border} />
      <circle cx="22" cy="19" r="4" fill="#FF5F57" />
      <circle cx="38" cy="19" r="4" fill="#FEBC2E" />
      <circle cx="54" cy="19" r="4" fill="#28C840" />
      <text x="78" y="23" fontFamily={MONO} fontSize="11" fill={C.muted}>
        {title}
      </text>

      {/* tool pills, right-aligned */}
      {tools.map((t, i) => {
        const w = t.length * 6 + 16;
        const x = 544 - w - tools.slice(0, i).reduce((s, p) => s + p.length * 6 + 16 + 6, 0);
        return (
          <g key={t}>
            <rect x={x} y={11} width={w} height={16} rx={8} fill="rgba(255,255,255,0.05)" stroke={C.border} />
            <text x={x + w / 2} y={22} fontFamily={MONO} fontSize="9" fill={C.muted} textAnchor="middle">
              {t}
            </text>
          </g>
        );
      })}

      {/* content area */}
      <g transform="translate(0,37)">{children}</g>
    </svg>
  );
}

/** A titled panel rectangle inside the content area. */
export function Panel({
  x,
  y,
  w,
  h,
  label,
  accent,
}: {
  x: number;
  y: number;
  w: number;
  h: number;
  label?: string;
  accent?: string;
}) {
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx="9" fill={C.panel} stroke={C.border} />
      {label && (
        <text x={x + 12} y={y + 17} fontFamily={MONO} fontSize="8.5" fill={C.faint} style={{ letterSpacing: "0.08em" }}>
          {label.toUpperCase()}
        </text>
      )}
      {accent && <rect x={x} y={y} width="3" height={h} rx="1.5" fill={accent} />}
    </g>
  );
}

/** Circular progress ring with optional centered label. */
export function Ring({
  cx,
  cy,
  r,
  frac,
  color,
  width = 8,
  label,
  sub,
}: {
  cx: number;
  cy: number;
  r: number;
  frac: number;
  color: string;
  width?: number;
  label?: string;
  sub?: string;
}) {
  const circ = 2 * Math.PI * r;
  const f = Math.max(0, Math.min(1, frac));
  return (
    <g>
      <circle cx={cx} cy={cy} r={r} fill="none" stroke={C.track} strokeWidth={width} />
      <circle
        cx={cx}
        cy={cy}
        r={r}
        fill="none"
        stroke={color}
        strokeWidth={width}
        strokeLinecap="round"
        strokeDasharray={`${(f * circ).toFixed(1)} ${circ.toFixed(1)}`}
        transform={`rotate(-90 ${cx} ${cy})`}
      />
      {label && (
        <text x={cx} y={cy + (sub ? 1 : 4)} fontFamily={DISPLAY} fontSize="15" fontWeight="700" fill={C.text} textAnchor="middle">
          {label}
        </text>
      )}
      {sub && (
        <text x={cx} y={cy + 14} fontFamily={MONO} fontSize="7.5" fill={C.faint} textAnchor="middle">
          {sub}
        </text>
      )}
    </g>
  );
}

/** Horizontal progress / meter bar. */
export function Bar({
  x,
  y,
  w,
  frac,
  color,
  h = 6,
}: {
  x: number;
  y: number;
  w: number;
  frac: number;
  color: string;
  h?: number;
}) {
  const f = Math.max(0, Math.min(1, frac));
  return (
    <g>
      <rect x={x} y={y} width={w} height={h} rx={h / 2} fill={C.track} />
      <rect x={x} y={y} width={Math.max(h, f * w)} height={h} rx={h / 2} fill={color} />
    </g>
  );
}

/** Build an SVG path string for a sparkline from values in [0,1]. */
export function sparkPath(values: number[], x: number, y: number, w: number, h: number, close = false): string {
  const n = values.length;
  const pts = values.map((v, i) => {
    const px = x + (i / (n - 1)) * w;
    const py = y + h - Math.max(0, Math.min(1, v)) * h;
    return `${i === 0 ? "M" : "L"}${px.toFixed(1)} ${py.toFixed(1)}`;
  });
  let d = pts.join(" ");
  if (close) d += ` L${(x + w).toFixed(1)} ${(y + h).toFixed(1)} L${x.toFixed(1)} ${(y + h).toFixed(1)} Z`;
  return d;
}

/** Small status pill with a colored dot. */
export function StatusPill({
  x,
  y,
  label,
  color,
}: {
  x: number;
  y: number;
  label: string;
  color: string;
}) {
  const w = label.length * 5.6 + 22;
  return (
    <g>
      <rect x={x} y={y} width={w} height={16} rx={8} fill="rgba(255,255,255,0.04)" stroke={color} strokeOpacity={0.4} />
      <circle cx={x + 11} cy={y + 8} r="2.6" fill={color} />
      <text x={x + 19} y={y + 11} fontFamily={MONO} fontSize="8.5" fill={C.text} fillOpacity={0.85}>
        {label}
      </text>
    </g>
  );
}

export function Dot({ cx, cy, r = 3, color }: { cx: number; cy: number; r?: number; color: string }) {
  return <circle cx={cx} cy={cy} r={r} fill={color} />;
}
