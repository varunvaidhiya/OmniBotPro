"use client";

/*
 * BuildStudio — the OhhO Build application shell.
 *
 * Owns the design state (useReducer over lib/build/design) and lays out the
 * three working surfaces: the parts palette (left), the live 3-D canvas
 * (centre) and the requirements / validation / recommendation / BOM column
 * (right). Everything is client-side: a design round-trips through the URL
 * (?d=) and localStorage so it survives reloads and is shareable on this
 * statically-exported site.
 */

import dynamic from "next/dynamic";
import Link from "next/link";
import { useEffect, useMemo, useReducer, useState } from "react";
import {
  ArrowLeft,
  Check,
  ChevronRight,
  Cpu,
  Download,
  Layers,
  Link2,
  Minus,
  Plus,
  Sparkles,
  TriangleAlert,
  X,
} from "lucide-react";

import {
  CATEGORIES,
  ENVIRONMENTS,
  partsByCategory,
  type Category,
  type Part,
} from "@/lib/build/catalog";
import {
  DEFAULT_DESIGN,
  TEMPLATES,
  decodeDesign,
  encodeDesign,
  isSelected,
  reducer,
  selectedIds,
  type Action,
  type Design,
  type Requirements,
} from "@/lib/build/design";
import {
  computeMetrics,
  overallStatus,
  recommend,
  validate,
  type Check as ValCheck,
  type Recommendation,
  type Status,
} from "@/lib/build/engine";
import { ARTIFACTS, buildBom, downloadFile, slug } from "@/lib/build/exporters";

const BuildCanvas = dynamic(() => import("./BuildCanvas"), { ssr: false, loading: () => <CanvasFallback /> });

const STORAGE_KEY = "ohho.build.design";

const STATUS_COLOR: Record<Status, string> = {
  pass: "#34D399",
  warn: "#FBBF24",
  fail: "#F87171",
};

// ── Requirement slider definitions ────────────────────────────────────────────
const REQ_FIELDS: {
  key: keyof Requirements;
  label: string;
  min: number;
  max: number;
  step: number;
  unit: string;
}[] = [
  { key: "payload", label: "Payload", min: 0, max: 30, step: 0.5, unit: "kg" },
  { key: "reach", label: "Reach", min: 0, max: 1, step: 0.05, unit: "m" },
  { key: "topSpeed", label: "Top speed", min: 0, max: 3, step: 0.1, unit: "m/s" },
  { key: "runtime", label: "Runtime", min: 0, max: 24, step: 0.5, unit: "h" },
  { key: "footprint", label: "Footprint", min: 0.15, max: 1, step: 0.05, unit: "m" },
];

export default function BuildStudio() {
  const [design, dispatch] = useReducer(reducer, DEFAULT_DESIGN);
  const [active, setActive] = useState<Category>("base");
  const [hydrated, setHydrated] = useState(false);
  const [exportOpen, setExportOpen] = useState(false);
  const [copied, setCopied] = useState(false);

  // ── hydrate from ?d= or localStorage on first mount ──
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const fromUrl = params.get("d");
    const loaded = (fromUrl && decodeDesign(fromUrl)) || loadLocal();
    if (loaded) dispatch({ type: "loadDesign", design: loaded });
    setHydrated(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── autosave to localStorage + reflect into the URL (shareable) ──
  useEffect(() => {
    if (!hydrated) return;
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(design));
    } catch {
      /* storage may be unavailable (private mode) — non-fatal */
    }
    const enc = encodeDesign(design);
    const url = `${window.location.pathname}?d=${enc}`;
    window.history.replaceState(null, "", url);
  }, [design, hydrated]);

  const metrics = useMemo(() => computeMetrics(design), [design]);
  const checks = useMemo(() => validate(design, metrics), [design, metrics]);
  const recs = useMemo(() => recommend(design, metrics), [design, metrics]);
  const status = overallStatus(checks);
  const bom = useMemo(() => buildBom(design), [design]);

  const share = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href);
      setCopied(true);
      setTimeout(() => setCopied(false), 1800);
    } catch {
      /* clipboard blocked — ignore */
    }
  };

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{ background: "rgba(10,14,26,.86)", backdropFilter: "blur(18px)", borderColor: "var(--border)" }}
      >
        <Link href="/products/build" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0" style={{ color: "var(--cyan)" }}>
          <ArrowLeft size={14} /> <span className="hidden sm:inline">OhhO Build</span>
        </Link>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <input
          value={design.name}
          onChange={(e) => dispatch({ type: "setName", name: e.target.value })}
          className="bg-transparent text-[15px] font-display font-semibold outline-none min-w-0 flex-1 px-1 rounded focus:bg-white/5"
          aria-label="Design name"
        />

        <StatusBadge status={status} />

        <button onClick={share} className="hidden sm:inline-flex items-center gap-1.5 text-[12px] font-medium px-3 py-[7px] rounded-lg transition-colors hover:bg-white/[0.06]" style={{ border: "1px solid var(--border-med)", color: "var(--text)" }}>
          {copied ? <Check size={13} /> : <Link2 size={13} />} {copied ? "Copied" : "Share"}
        </button>

        <div className="relative">
          <button onClick={() => setExportOpen((v) => !v)} className="inline-flex items-center gap-1.5 text-[12px] font-semibold px-3.5 py-[7px] rounded-lg transition-all hover:-translate-y-px" style={{ background: "var(--cyan)", color: "var(--bg)" }}>
            <Download size={13} /> Export
          </button>
          {exportOpen && (
            <ExportMenu design={design} onClose={() => setExportOpen(false)} />
          )}
        </div>
      </header>

      {/* ── working area ── */}
      <div className="relative z-10 grid grid-cols-1 lg:grid-cols-[300px_1fr_380px] gap-px" style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}>
        {/* LEFT — parts palette */}
        <PalettePanel design={design} active={active} setActive={setActive} dispatch={dispatch} />

        {/* CENTER — canvas + metrics */}
        <section className="flex flex-col" style={{ background: "var(--bg)" }}>
          <div className="relative flex-1 min-h-[340px] lg:min-h-0">
            <BuildCanvas design={design} highlight={active} />
            <TemplateBar onPick={(id) => dispatch({ type: "loadTemplate", templateId: id })} />
            <div className="absolute bottom-3 left-1/2 -translate-x-1/2 text-[10.5px] font-mono px-2.5 py-1 rounded-full pointer-events-none" style={{ background: "rgba(10,14,26,.7)", color: "var(--faint)", border: "1px solid var(--border)" }}>
              drag to orbit · scroll to zoom
            </div>
          </div>
          <MetricsStrip metrics={metrics} />
        </section>

        {/* RIGHT — requirements / validation / AI / BOM */}
        <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
          <RequirementsPanel design={design} dispatch={dispatch} />
          <RecommendationPanel recs={recs} dispatch={dispatch} />
          <ValidationPanel checks={checks} />
          <BomPanel bom={bom} totalPrice={metrics.totalPrice} maxLead={metrics.maxLeadTimeDays} />
        </aside>
      </div>
    </div>
  );
}

// ── LEFT: parts palette ───────────────────────────────────────────────────────

function PalettePanel({
  design,
  active,
  setActive,
  dispatch,
}: {
  design: Design;
  active: Category;
  setActive: (c: Category) => void;
  dispatch: React.Dispatch<Action>;
}) {
  const meta = CATEGORIES.find((c) => c.key === active)!;
  const parts = partsByCategory(active);

  return (
    <section className="flex flex-col" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
      {/* category rail */}
      <div className="flex lg:flex-col gap-1 p-2 overflow-x-auto lg:overflow-x-visible border-b lg:border-b-0 lg:border-r-0" style={{ borderColor: "var(--border)" }}>
        <div className="hidden lg:block px-2 pt-1 pb-2 text-[10px] font-mono tracking-widest uppercase" style={{ color: "var(--faint)" }}>
          Parts
        </div>
        {CATEGORIES.map((c) => {
          const n = selectedIds(design, c.key).length;
          const on = active === c.key;
          const missing = c.required && n === 0;
          return (
            <button
              key={c.key}
              onClick={() => setActive(c.key)}
              className="flex items-center gap-2 px-2.5 py-2 rounded-lg text-left text-[13px] font-medium transition-colors shrink-0 lg:w-full"
              style={{
                background: on ? "var(--cyan-dim)" : "transparent",
                color: on ? "#fff" : "var(--muted)",
                border: `1px solid ${on ? "rgba(0,212,255,.28)" : "transparent"}`,
              }}
            >
              <span className="font-display flex-1">{c.label}</span>
              {missing ? (
                <span className="w-1.5 h-1.5 rounded-full" style={{ background: STATUS_COLOR.fail }} title="required" />
              ) : n > 0 ? (
                <span className="text-[10px] font-mono px-1.5 py-0.5 rounded-full" style={{ background: "rgba(0,212,255,.14)", color: "var(--cyan)" }}>{n}</span>
              ) : null}
            </button>
          );
        })}
      </div>

      {/* parts in the active category */}
      <div className="flex-1 overflow-y-auto p-2.5 lg:border-t" style={{ borderColor: "var(--border)" }}>
        <div className="px-1 pb-2 text-[11px]" style={{ color: "var(--muted)" }}>
          {meta.blurb}
          {!meta.single && <span className="ml-1" style={{ color: "var(--faint)" }}>· pick several</span>}
        </div>
        <div className="flex flex-col gap-2">
          {parts.map((p) => (
            <PartCard
              key={p.id}
              part={p}
              selected={isSelected(design, active, p.id)}
              onToggle={() =>
                dispatch(
                  meta.single
                    ? { type: "selectSingle", category: active, partId: p.id }
                    : { type: "togglePart", category: active, partId: p.id },
                )
              }
            />
          ))}
        </div>
      </div>
    </section>
  );
}

function PartCard({ part, selected, onToggle }: { part: Part; selected: boolean; onToggle: () => void }) {
  return (
    <button
      onClick={onToggle}
      className="text-left p-3 rounded-xl transition-all group"
      style={{
        background: selected ? "var(--cyan-dim)" : "rgba(255,255,255,.02)",
        border: `1px solid ${selected ? "rgba(0,212,255,.4)" : "var(--border)"}`,
      }}
    >
      <div className="flex items-start gap-2">
        <div className="flex-1 min-w-0">
          <div className="text-[13px] font-semibold font-display leading-tight">{part.name}</div>
          <div className="text-[10.5px] font-mono mt-0.5" style={{ color: "var(--cyan)" }}>{part.brand}</div>
        </div>
        <span
          className="shrink-0 w-5 h-5 rounded-md flex items-center justify-center transition-colors"
          style={{
            background: selected ? "var(--cyan)" : "rgba(255,255,255,.04)",
            color: selected ? "var(--bg)" : "var(--muted)",
            border: selected ? "none" : "1px solid var(--border-med)",
          }}
        >
          {selected ? <Check size={12} strokeWidth={3} /> : <Plus size={12} strokeWidth={2.5} />}
        </span>
      </div>
      <p className="text-[11.5px] leading-[1.5] mt-1.5" style={{ color: "var(--muted)" }}>{part.desc}</p>
      <div className="flex flex-wrap gap-1.5 mt-2">
        <Spec label={`$${part.price}`} />
        <Spec label={`${part.mass} kg`} />
        {part.payload != null && <Spec label={`${part.payload} kg payload`} />}
        {part.reach != null && <Spec label={`${part.reach} m reach`} />}
        {part.topSpeed != null && <Spec label={`${part.topSpeed} m/s`} />}
        {part.capacityWh != null && <Spec label={`${part.capacityWh} Wh`} />}
        {part.tops != null && part.tops > 0 && <Spec label={`${part.tops} TOPS`} />}
        {part.range != null && part.range > 0 && <Spec label={`${part.range} m range`} />}
        <Spec label={`${part.leadTimeDays}d lead`} dim />
      </div>
    </button>
  );
}

function Spec({ label, dim }: { label: string; dim?: boolean }) {
  return (
    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded" style={{ background: "rgba(255,255,255,.04)", color: dim ? "var(--faint)" : "var(--muted)" }}>
      {label}
    </span>
  );
}

// ── CENTER overlays ───────────────────────────────────────────────────────────

function TemplateBar({ onPick }: { onPick: (id: string) => void }) {
  return (
    <div className="absolute top-3 left-1/2 -translate-x-1/2 flex items-center gap-1.5 px-1.5 py-1.5 rounded-xl flex-wrap justify-center max-w-[92%]" style={{ background: "rgba(10,14,26,.72)", backdropFilter: "blur(12px)", border: "1px solid var(--border)" }}>
      <span className="text-[10px] font-mono uppercase tracking-wider px-1.5 hidden sm:inline" style={{ color: "var(--faint)" }}>Templates</span>
      {TEMPLATES.map((t) => (
        <button
          key={t.id}
          onClick={() => onPick(t.id)}
          title={t.blurb}
          className="text-[11px] font-medium px-2.5 py-1 rounded-lg transition-colors hover:bg-white/[0.08]"
          style={{ color: "var(--muted)", border: "1px solid transparent" }}
        >
          {t.name}
        </button>
      ))}
    </div>
  );
}

function MetricsStrip({ metrics }: { metrics: ReturnType<typeof computeMetrics> }) {
  const items = [
    { label: "Mass", value: `${metrics.totalMass} kg` },
    { label: "Payload", value: `${metrics.usablePayload} kg` },
    { label: "Reach", value: metrics.reach ? `${metrics.reach} m` : "—" },
    { label: "Top speed", value: metrics.topSpeed ? `${metrics.topSpeed} m/s` : "—" },
    { label: "Runtime", value: metrics.runtime ? `${metrics.runtime} h` : "—" },
    { label: "Power draw", value: metrics.avgPowerDraw ? `${metrics.avgPowerDraw} W` : "—" },
    { label: "Compute", value: metrics.tops ? `${metrics.tops} TOPS` : "CPU" },
    { label: "Cost", value: `$${metrics.totalPrice.toLocaleString()}` },
  ];
  // gap-px over a border-coloured background draws crisp 1px dividers between
  // cells without any per-cell border maths (mirrors the outer layout).
  return (
    <div className="grid grid-cols-4 sm:grid-cols-8 gap-px border-t" style={{ borderColor: "var(--border)", background: "var(--border)" }}>
      {items.map((it) => (
        <div key={it.label} className="px-3 py-2.5" style={{ background: "var(--surf)" }}>
          <div className="text-[9.5px] font-mono uppercase tracking-wider truncate" style={{ color: "var(--faint)" }}>{it.label}</div>
          <div className="text-[14px] font-display font-semibold mt-0.5 tabular-nums">{it.value}</div>
        </div>
      ))}
    </div>
  );
}

// ── RIGHT panels ──────────────────────────────────────────────────────────────

function PanelHead({ icon, title, hint }: { icon: React.ReactNode; title: string; hint?: string }) {
  return (
    <div className="flex items-center gap-2 px-4 pt-4 pb-2">
      <span style={{ color: "var(--cyan)" }}>{icon}</span>
      <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>{title}</h2>
      {hint && <span className="ml-auto text-[10.5px]" style={{ color: "var(--faint)" }}>{hint}</span>}
    </div>
  );
}

function RequirementsPanel({ design, dispatch }: { design: Design; dispatch: React.Dispatch<Action> }) {
  const r = design.requirements;
  return (
    <div className="border-b" style={{ borderColor: "var(--border)" }}>
      <PanelHead icon={<Sparkles size={15} />} title="Requirements" hint="what it must do" />
      <div className="px-4 pb-4 flex flex-col gap-3.5">
        {REQ_FIELDS.map((f) => {
          const value = r[f.key] as number;
          return (
            <div key={f.key}>
              <div className="flex items-baseline justify-between mb-1.5">
                <label className="text-[12.5px]" style={{ color: "var(--muted)" }}>{f.label}</label>
                <span className="text-[12.5px] font-mono tabular-nums" style={{ color: "#fff" }}>
                  {value}
                  <span style={{ color: "var(--faint)" }}> {f.unit}</span>
                </span>
              </div>
              <input
                type="range"
                min={f.min}
                max={f.max}
                step={f.step}
                value={value}
                onChange={(e) => dispatch({ type: "setRequirement", key: f.key, value: parseFloat(e.target.value) })}
                className="ohho-range w-full"
              />
            </div>
          );
        })}
        <div>
          <label className="text-[12.5px] block mb-1.5" style={{ color: "var(--muted)" }}>Environment</label>
          <div className="flex flex-wrap gap-1.5">
            {ENVIRONMENTS.map((e) => {
              const on = r.environment === e.key;
              return (
                <button
                  key={e.key}
                  onClick={() => dispatch({ type: "setRequirement", key: "environment", value: e.key })}
                  className="text-[11.5px] font-medium px-2.5 py-1.5 rounded-lg transition-colors"
                  style={{
                    background: on ? "var(--cyan-dim)" : "rgba(255,255,255,.02)",
                    color: on ? "#fff" : "var(--muted)",
                    border: `1px solid ${on ? "rgba(0,212,255,.4)" : "var(--border)"}`,
                  }}
                >
                  {e.label}
                </button>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}

function RecommendationPanel({ recs, dispatch }: { recs: Recommendation[]; dispatch: React.Dispatch<Action> }) {
  return (
    <div className="border-b" style={{ borderColor: "var(--border)", background: "rgba(124,58,237,.06)" }}>
      <PanelHead icon={<Sparkles size={15} />} title="AI Recommendations" />
      <div className="px-4 pb-4 flex flex-col gap-2.5">
        {recs.map((rec) => {
          const color = rec.severity === "fix" ? STATUS_COLOR.fail : rec.severity === "improve" ? "#A78BFA" : STATUS_COLOR.pass;
          return (
            <div key={rec.id} className="p-3 rounded-xl" style={{ background: "rgba(255,255,255,.025)", border: `1px solid ${rec.severity === "ok" ? "rgba(52,211,153,.3)" : "var(--border-med)"}` }}>
              <div className="flex items-center gap-2 mb-1">
                <span className="w-1.5 h-1.5 rounded-full" style={{ background: color }} />
                <span className="text-[10px] font-mono uppercase tracking-wider" style={{ color }}>
                  {rec.severity === "fix" ? "Fix" : rec.severity === "improve" ? "Improve" : "Valid"}
                </span>
              </div>
              <div className="text-[13px] font-semibold font-display">{rec.title}</div>
              <p className="text-[11.5px] leading-[1.5] mt-1" style={{ color: "var(--muted)" }}>{rec.detail}</p>
              {rec.action && (
                <button
                  onClick={() => dispatch(rec.action!.payload)}
                  className="mt-2 inline-flex items-center gap-1.5 text-[11.5px] font-semibold px-2.5 py-1.5 rounded-lg transition-all hover:-translate-y-px"
                  style={{ background: "#A78BFA", color: "var(--bg)" }}
                >
                  {rec.action.label} <ChevronRight size={12} strokeWidth={2.5} />
                </button>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function ValidationPanel({ checks }: { checks: ValCheck[] }) {
  return (
    <div className="border-b" style={{ borderColor: "var(--border)" }}>
      <PanelHead icon={<Check size={15} />} title="Validation" hint={`${checks.length} checks`} />
      <div className="px-4 pb-4 flex flex-col gap-1.5">
        {checks.length === 0 && <p className="text-[12px]" style={{ color: "var(--faint)" }}>Add parts to start validating.</p>}
        {checks.map((c) => (
          <div key={c.id} className="flex items-start gap-2.5 py-1.5">
            <span className="shrink-0 mt-0.5 w-4 h-4 rounded-full flex items-center justify-center" style={{ background: `${STATUS_COLOR[c.status]}22`, border: `1px solid ${STATUS_COLOR[c.status]}` }}>
              {c.status === "pass" ? <Check size={9} strokeWidth={3} style={{ color: STATUS_COLOR.pass }} /> : c.status === "warn" ? <Minus size={9} strokeWidth={3} style={{ color: STATUS_COLOR.warn }} /> : <X size={9} strokeWidth={3} style={{ color: STATUS_COLOR.fail }} />}
            </span>
            <div className="flex-1 min-w-0">
              <div className="text-[12.5px] font-medium">{c.label}</div>
              <div className="text-[11px] leading-[1.45]" style={{ color: "var(--muted)" }}>{c.detail}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function BomPanel({ bom, totalPrice, maxLead }: { bom: ReturnType<typeof buildBom>; totalPrice: number; maxLead: number }) {
  return (
    <div>
      <PanelHead icon={<Layers size={15} />} title="Bill of Materials" hint={`${bom.length} lines`} />
      <div className="px-4 pb-4">
        <div className="rounded-xl overflow-hidden" style={{ border: "1px solid var(--border)" }}>
          {bom.length === 0 && <div className="px-3 py-4 text-[12px]" style={{ color: "var(--faint)" }}>No parts selected yet.</div>}
          {bom.map((row, i) => (
            <a
              key={row.name + i}
              href={row.supplierUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-2 px-3 py-2 transition-colors hover:bg-white/[0.03]"
              style={{ borderTop: i === 0 ? "none" : "1px solid var(--border)" }}
            >
              <span className="text-[9px] font-mono uppercase w-12 shrink-0" style={{ color: "var(--cyan)" }}>{row.category}</span>
              <span className="flex-1 min-w-0 text-[12px] truncate">{row.name}{row.qty > 1 && <span style={{ color: "var(--faint)" }}> ×{row.qty}</span>}</span>
              <span className="text-[11.5px] font-mono tabular-nums" style={{ color: "var(--muted)" }}>${row.lineTotal.toLocaleString()}</span>
            </a>
          ))}
          {bom.length > 0 && (
            <div className="flex items-center gap-2 px-3 py-2.5" style={{ borderTop: "1px solid var(--border-med)", background: "rgba(0,212,255,.05)" }}>
              <span className="flex-1 text-[12px] font-semibold">Total</span>
              <span className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>{maxLead}d lead</span>
              <span className="text-[14px] font-display font-bold tabular-nums" style={{ color: "var(--cyan)" }}>${totalPrice.toLocaleString()}</span>
            </div>
          )}
        </div>
        <p className="text-[10.5px] mt-2 leading-[1.5]" style={{ color: "var(--faint)" }}>
          Rows link to suppliers. Export a costed CSV, plus a matching URDF, sim world and OhhO Frame deployment profile.
        </p>
      </div>
    </div>
  );
}

// ── shared bits ───────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: Status }) {
  const map = {
    pass: { label: "Valid", color: STATUS_COLOR.pass, icon: <Check size={12} strokeWidth={3} /> },
    warn: { label: "Warnings", color: STATUS_COLOR.warn, icon: <TriangleAlert size={12} /> },
    fail: { label: "Issues", color: STATUS_COLOR.fail, icon: <X size={12} strokeWidth={3} /> },
  }[status];
  return (
    <span className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full shrink-0" style={{ background: `${map.color}1a`, color: map.color, border: `1px solid ${map.color}66` }}>
      {map.icon} {map.label}
    </span>
  );
}

function ExportMenu({ design, onClose }: { design: Design; onClose: () => void }) {
  const s = slug(design.name);
  return (
    <>
      <div className="fixed inset-0 z-30" onClick={onClose} />
      <div className="absolute right-0 top-[calc(100%+8px)] z-40 w-[260px] p-1.5 rounded-xl" style={{ background: "var(--surf-hi)", border: "1px solid var(--border-med)", boxShadow: "0 18px 40px -16px rgba(0,0,0,.7)" }}>
        <div className="px-2.5 py-1.5 text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Download artifacts</div>
        {ARTIFACTS.map((a) => (
          <button
            key={a.id}
            onClick={() => {
              downloadFile(a.filename(s), a.build(design), a.mime);
              onClose();
            }}
            className="w-full flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-left text-[12.5px] transition-colors hover:bg-white/[0.06]"
          >
            <Download size={13} style={{ color: "var(--cyan)" }} />
            {a.label}
          </button>
        ))}
      </div>
    </>
  );
}

function CanvasFallback() {
  return (
    <div className="absolute inset-0 flex items-center justify-center">
      <div className="flex flex-col items-center gap-3" style={{ color: "var(--faint)" }}>
        <Cpu size={28} className="animate-pulse" />
        <span className="text-[12px] font-mono">loading 3-D studio…</span>
      </div>
    </div>
  );
}

function loadLocal(): Design | null {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as Design;
  } catch {
    return null;
  }
}
