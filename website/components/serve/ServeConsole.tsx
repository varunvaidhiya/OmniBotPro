"use client";

/*
 * ServeConsole — the OhhO Serve application shell.
 *
 * A live inference console for a Vision-Language-Action model server. Three
 * surfaces: deploy config + VRAM fit + generated client code (left), a /predict
 * playground that returns instruction-aware actions (centre), and a live
 * Prometheus-style observability panel (right). Fully client-side; inference is
 * a deterministic simulation that returns the real vla_serve response shape.
 */

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Activity,
  ArrowLeft,
  Check,
  ChevronRight,
  Copy,
  Cpu,
  Gauge,
  Loader2,
  Play,
  Plug,
  Server,
  TriangleAlert,
  Zap,
} from "lucide-react";

import {
  DEFAULT_CONFIG,
  GPUS,
  MODELS,
  effectiveQuant,
  estimate,
  getModel,
  type Backend,
  type ServeConfig,
} from "@/lib/serve/models";
import { SCENES, actionLabels, getScene, predict, type PredictResult } from "@/lib/serve/inference";
import { MetricsSim, type MetricsSnapshot } from "@/lib/serve/metrics";
import { snippets } from "@/lib/serve/client";

const GREEN = "#34D399";
const AMBER = "#FBBF24";
const RED = "#F87171";
const CYAN = "#00D4FF";

interface LogEntry {
  id: number;
  scene: string;
  instruction: string;
  result: PredictResult;
  ok: boolean;
}

export default function ServeConsole() {
  const [config, setConfig] = useState<ServeConfig>(DEFAULT_CONFIG);
  const [loaded, setLoaded] = useState(false);
  const [loading, setLoading] = useState(false);
  const [sceneId, setSceneId] = useState(SCENES[0].id);
  const [instruction, setInstruction] = useState(SCENES[0].instruction);
  const [predicting, setPredicting] = useState(false);
  const [log, setLog] = useState<LogEntry[]>([]);
  const [snap, setSnap] = useState<MetricsSnapshot | null>(null);

  const sim = useRef<MetricsSim>();
  const logId = useRef(0);

  const model = getModel(config.backend);
  const est = useMemo(() => estimate(config), [config]);

  // ── metrics loop ──
  useEffect(() => {
    sim.current = new MetricsSim(config);
    setSnap(sim.current.snapshot());
    const iv = setInterval(() => {
      if (!sim.current) return;
      sim.current.tick();
      setSnap(sim.current.snapshot());
    }, 900);
    return () => clearInterval(iv);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // keep the sim's config/loaded in sync
  useEffect(() => {
    sim.current?.setConfig(config, loaded);
    setSnap(sim.current?.snapshot() ?? null);
  }, [config, loaded]);

  // ── config helpers (model-affecting changes require a reload) ──
  const patch = useCallback((p: Partial<ServeConfig>, requiresReload = false) => {
    setConfig((c) => ({ ...c, ...p }));
    if (requiresReload) setLoaded(false);
  }, []);

  const chooseBackend = (b: Backend) => {
    const m = getModel(b);
    setConfig((c) => ({ ...c, backend: b, checkpoint: m.checkpoint, quant4bit: c.quant4bit && m.supports4bit }));
    setLoaded(false);
  };

  const loadModel = () => {
    if (loading) return;
    setLoading(true);
    // bigger models "load" slower — purely cosmetic
    const ms = 500 + Math.min(1600, model.vramFp16 * 110);
    setTimeout(() => {
      setLoading(false);
      setLoaded(true);
    }, ms);
  };

  const sendPredict = () => {
    if (!loaded || predicting || !instruction.trim()) return;
    setPredicting(true);
    const result = predict(config, { sceneId, instruction });
    const wait = Math.min(1600, Math.max(120, result.latency_ms));
    setTimeout(() => {
      sim.current?.record(result.latency_ms);
      setSnap(sim.current?.snapshot() ?? null);
      setLog((l) => [{ id: logId.current++, scene: sceneId, instruction, result, ok: true }, ...l].slice(0, 8));
      setPredicting(false);
    }, wait);
  };

  const latest = log[0];

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* top bar */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{ background: "rgba(10,14,26,.86)", backdropFilter: "blur(18px)", borderColor: "var(--border)" }}
      >
        <Link href="/products/serve" className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0" style={{ color: CYAN }}>
          <ArrowLeft size={14} /> <span className="hidden sm:inline">OhhO Serve</span>
        </Link>
        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />
        <span className="text-[12.5px] font-mono truncate" style={{ color: "var(--muted)" }}>
          http://localhost:{config.port}
        </span>
        <span className="ml-auto flex items-center gap-2">
          <HealthPill loaded={loaded} />
          <span className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full" style={{ background: "rgba(255,255,255,.04)", color: "var(--muted)", border: "1px solid var(--border)" }}>
            <span className="badge-dot" style={{ background: CYAN }} /> demo
          </span>
        </span>
      </header>

      {/* working area */}
      <div className="relative z-10 grid grid-cols-1 lg:grid-cols-[320px_1fr_340px] gap-px" style={{ background: "var(--border)", minHeight: "calc(100vh - 56px)" }}>
        {/* LEFT — deploy config */}
        <ConfigPanel
          config={config}
          model={model}
          est={est}
          loaded={loaded}
          loading={loading}
          onBackend={chooseBackend}
          onPatch={patch}
          onLoad={loadModel}
        />

        {/* CENTER — predict playground */}
        <section className="flex flex-col overflow-y-auto" style={{ background: "var(--bg)", maxHeight: "calc(100vh - 56px)" }}>
          <EndpointBar loaded={loaded} />
          <div className="p-4 flex flex-col gap-4">
            <ScenePicker sceneId={sceneId} onPick={(id) => { setSceneId(id); setInstruction(getScene(id).instruction); }} />

            <div>
              <label className="text-[11px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>Instruction</label>
              <div className="flex gap-2 mt-1.5">
                <input
                  value={instruction}
                  onChange={(e) => setInstruction(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && sendPredict()}
                  placeholder="e.g. pick up the red cup"
                  className="flex-1 bg-transparent text-[13.5px] px-3 py-2.5 rounded-lg outline-none focus:border-cyan"
                  style={{ border: "1px solid var(--border-med)", color: "#fff" }}
                />
                <button
                  onClick={sendPredict}
                  disabled={!loaded || predicting || !instruction.trim()}
                  className="inline-flex items-center gap-1.5 text-[13px] font-semibold px-4 py-2.5 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed enabled:hover:-translate-y-px shrink-0"
                  style={{ background: CYAN, color: "var(--bg)" }}
                >
                  {predicting ? <Loader2 size={14} className="animate-spin" /> : <Play size={13} />}
                  {predicting ? "Running" : "POST /predict"}
                </button>
              </div>
              {!loaded && (
                <p className="text-[11.5px] mt-2 flex items-center gap-1.5" style={{ color: AMBER }}>
                  <TriangleAlert size={12} /> Load a model first — POST /load_model (left panel).
                </p>
              )}
            </div>

            <ActionResponse latest={latest} model={model} />

            {log.length > 0 && (
              <div>
                <div className="text-[11px] font-mono uppercase tracking-wider mb-2" style={{ color: "var(--faint)" }}>Recent requests</div>
                <div className="flex flex-col gap-1">
                  {log.map((e) => (
                    <div key={e.id} className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-[11.5px]" style={{ background: "rgba(255,255,255,.02)", border: "1px solid var(--border)" }}>
                      <span className="w-1.5 h-1.5 rounded-full shrink-0" style={{ background: GREEN }} />
                      <span className="font-mono shrink-0" style={{ color: GREEN }}>200</span>
                      <span className="truncate flex-1" style={{ color: "var(--muted)" }}>{e.instruction}</span>
                      <span className="font-mono tabular-nums shrink-0" style={{ color: "var(--faint)" }}>{e.result.latency_ms} ms</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </section>

        {/* RIGHT — observability */}
        <MetricsPanel snap={snap} config={config} est={est} />
      </div>
    </div>
  );
}

// ── LEFT: config ──────────────────────────────────────────────────────────────

function ConfigPanel({
  config,
  model,
  est,
  loaded,
  loading,
  onBackend,
  onPatch,
  onLoad,
}: {
  config: ServeConfig;
  model: ReturnType<typeof getModel>;
  est: ReturnType<typeof estimate>;
  loaded: boolean;
  loading: boolean;
  onBackend: (b: Backend) => void;
  onPatch: (p: Partial<ServeConfig>, requiresReload?: boolean) => void;
  onLoad: () => void;
}) {
  const quantOn = effectiveQuant(config);
  return (
    <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
      <SectionHead icon={<Server size={15} />} title="Model backend" />
      <div className="px-3 flex flex-col gap-2">
        {MODELS.map((m) => {
          const on = config.backend === m.id;
          return (
            <button key={m.id} onClick={() => onBackend(m.id)} className="text-left p-2.5 rounded-xl transition-all"
              style={{ background: on ? "var(--cyan-dim)" : "rgba(255,255,255,.02)", border: `1px solid ${on ? "rgba(0,212,255,.4)" : "var(--border)"}` }}>
              <div className="flex items-center gap-2">
                <span className="text-[13px] font-semibold font-display flex-1">{m.name}</span>
                <span className="text-[10px] font-mono px-1.5 py-0.5 rounded" style={{ background: "rgba(255,255,255,.05)", color: "var(--muted)" }}>{m.params}</span>
              </div>
              <p className="text-[11px] leading-[1.45] mt-1" style={{ color: "var(--muted)" }}>{m.desc}</p>
            </button>
          );
        })}
      </div>

      <SectionHead icon={<Cpu size={15} />} title="Deploy config" />
      <div className="px-4 pb-2 flex flex-col gap-3">
        <Field label="Checkpoint (VLA_MODEL_PATH)">
          <input value={config.checkpoint} onChange={(e) => onPatch({ checkpoint: e.target.value }, true)}
            className="w-full bg-transparent text-[12px] font-mono px-2.5 py-2 rounded-lg outline-none focus:border-cyan" style={{ border: "1px solid var(--border-med)", color: "#fff" }} />
        </Field>

        <Field label="Device">
          <div className="flex gap-1.5">
            {(["cuda", "cpu"] as const).map((d) => (
              <button key={d} onClick={() => onPatch({ device: d }, true)} className="flex-1 text-[12px] font-medium py-1.5 rounded-lg transition-colors"
                style={{ background: config.device === d ? "var(--cyan-dim)" : "rgba(255,255,255,.02)", color: config.device === d ? "#fff" : "var(--muted)", border: `1px solid ${config.device === d ? "rgba(0,212,255,.4)" : "var(--border)"}` }}>
                {d}
              </button>
            ))}
          </div>
        </Field>

        {config.device === "cuda" && (
          <Field label="GPU">
            <div className="grid grid-cols-2 gap-1.5">
              {GPUS.map((g) => (
                <button key={g.id} onClick={() => onPatch({ gpuId: g.id }, true)} className="text-[11px] font-medium py-1.5 px-2 rounded-lg transition-colors text-left"
                  style={{ background: config.gpuId === g.id ? "var(--cyan-dim)" : "rgba(255,255,255,.02)", color: config.gpuId === g.id ? "#fff" : "var(--muted)", border: `1px solid ${config.gpuId === g.id ? "rgba(0,212,255,.4)" : "var(--border)"}` }}>
                  {g.name}<span className="block text-[9.5px] font-mono" style={{ color: "var(--faint)" }}>{g.vramGb} GB</span>
                </button>
              ))}
            </div>
          </Field>
        )}

        <button onClick={() => model.supports4bit && onPatch({ quant4bit: !config.quant4bit }, true)} disabled={!model.supports4bit}
          className="flex items-center justify-between px-3 py-2 rounded-lg transition-colors disabled:opacity-45"
          style={{ background: "rgba(255,255,255,.02)", border: "1px solid var(--border)" }}>
          <span className="text-[12.5px]" style={{ color: "var(--muted)" }}>4-bit quantization {model.supports4bit ? "" : "· n/a"}</span>
          <span className="w-9 h-5 rounded-full relative transition-colors" style={{ background: quantOn ? CYAN : "rgba(255,255,255,.12)" }}>
            <span className="absolute top-0.5 w-4 h-4 rounded-full transition-all" style={{ background: "var(--bg)", left: quantOn ? "18px" : "2px" }} />
          </span>
        </button>

        <Field label={`Batch size · ${config.batchSize}`}>
          <input type="range" min={1} max={16} step={1} value={config.batchSize} onChange={(e) => onPatch({ batchSize: parseInt(e.target.value) })} className="ohho-range w-full" />
        </Field>
      </div>

      {/* VRAM fit */}
      <div className="px-4 pb-3">
        <VramMeter est={est} device={config.device} />
        <button onClick={onLoad} disabled={loading || (config.device === "cuda" && !est.fits)}
          className="w-full mt-3 inline-flex items-center justify-center gap-2 text-[13px] font-semibold py-2.5 rounded-lg transition-all disabled:opacity-40 disabled:cursor-not-allowed enabled:hover:-translate-y-px"
          style={{ background: loaded ? "rgba(52,211,153,.16)" : CYAN, color: loaded ? GREEN : "var(--bg)", border: loaded ? `1px solid ${GREEN}66` : "none" }}>
          {loading ? <Loader2 size={14} className="animate-spin" /> : loaded ? <Check size={14} /> : <Plug size={14} />}
          {loading ? "Loading model…" : loaded ? "Model loaded" : "POST /load_model"}
        </button>
      </div>

      <ClientCode config={config} />
    </aside>
  );
}

function VramMeter({ est, device }: { est: ReturnType<typeof estimate>; device: string }) {
  if (device === "cpu") {
    return (
      <div className="p-3 rounded-xl" style={{ background: "rgba(255,255,255,.02)", border: "1px solid var(--border)" }}>
        <div className="flex justify-between text-[11.5px]"><span style={{ color: "var(--muted)" }}>System RAM</span><span className="font-mono" style={{ color: "#fff" }}>{est.totalVram} GB</span></div>
        <p className="text-[10.5px] mt-1" style={{ color: "var(--faint)" }}>CPU mode — slower, but no GPU required.</p>
      </div>
    );
  }
  const frac = Math.min(1, est.totalVram / est.gpuVram);
  const color = est.fits ? (frac > 0.85 ? AMBER : GREEN) : RED;
  return (
    <div className="p-3 rounded-xl" style={{ background: est.fits ? "rgba(255,255,255,.02)" : "rgba(248,113,113,.08)", border: `1px solid ${est.fits ? "var(--border)" : RED + "66"}` }}>
      <div className="flex items-center justify-between text-[11.5px] mb-1.5">
        <span style={{ color: "var(--muted)" }}>VRAM</span>
        <span className="font-mono tabular-nums" style={{ color }}>{est.totalVram} / {est.gpuVram} GB</span>
      </div>
      <div className="h-2 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,.08)" }}>
        <div className="h-full rounded-full transition-all" style={{ width: `${Math.max(4, frac * 100)}%`, background: color }} />
      </div>
      <p className="text-[10.5px] mt-1.5" style={{ color: est.fits ? "var(--faint)" : RED }}>
        {est.fits ? `Fits with ${est.headroom} GB headroom.` : `Over budget by ${Math.abs(est.headroom)} GB — enable 4-bit or pick a bigger GPU.`}
      </p>
    </div>
  );
}

function ClientCode({ config }: { config: ServeConfig }) {
  const snips = useMemo(() => snippets(config), [config]);
  const [active, setActive] = useState(snips[0].id);
  const [copied, setCopied] = useState(false);
  const current = snips.find((s) => s.id === active) ?? snips[0];

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(current.code);
      setCopied(true);
      setTimeout(() => setCopied(false), 1600);
    } catch {
      /* clipboard blocked */
    }
  };

  return (
    <div className="border-t mt-auto" style={{ borderColor: "var(--border)" }}>
      <SectionHead icon={<Zap size={15} />} title="Deploy & client" />
      <div className="px-3 pb-4">
        <div className="flex gap-1 mb-2 flex-wrap">
          {snips.map((s) => (
            <button key={s.id} onClick={() => setActive(s.id)} className="text-[10.5px] font-medium px-2 py-1 rounded-md transition-colors"
              style={{ background: active === s.id ? "var(--cyan-dim)" : "transparent", color: active === s.id ? "#fff" : "var(--muted)", border: `1px solid ${active === s.id ? "rgba(0,212,255,.3)" : "var(--border)"}` }}>
              {s.label}
            </button>
          ))}
        </div>
        <div className="relative rounded-lg overflow-hidden" style={{ background: "#0A0F1C", border: "1px solid var(--border)" }}>
          <button onClick={copy} className="absolute top-2 right-2 z-10 inline-flex items-center gap-1 text-[10px] font-mono px-1.5 py-1 rounded transition-colors hover:bg-white/10" style={{ color: copied ? GREEN : "var(--muted)" }}>
            {copied ? <Check size={11} /> : <Copy size={11} />} {copied ? "copied" : "copy"}
          </button>
          <pre className="text-[10.5px] font-mono p-3 pr-14 overflow-x-auto leading-[1.55]" style={{ color: "rgba(255,255,255,.8)" }}>{current.code}</pre>
        </div>
      </div>
    </div>
  );
}

// ── CENTER ────────────────────────────────────────────────────────────────────

function EndpointBar({ loaded }: { loaded: boolean }) {
  const eps = [
    { m: "GET", p: "/health", ok: true, color: "#60A5FA" },
    { m: "POST", p: "/load_model", ok: loaded, color: AMBER },
    { m: "POST", p: "/predict", ok: loaded, color: GREEN },
  ];
  return (
    <div className="flex items-center gap-2 px-4 py-2.5 border-b overflow-x-auto" style={{ borderColor: "var(--border)", background: "var(--surf)" }}>
      {eps.map((e) => (
        <div key={e.p} className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg shrink-0" style={{ background: "rgba(255,255,255,.02)", border: "1px solid var(--border)" }}>
          <span className="w-1.5 h-1.5 rounded-full" style={{ background: e.ok ? GREEN : "var(--faint)" }} />
          <span className="text-[9.5px] font-mono px-1 rounded" style={{ color: e.color }}>{e.m}</span>
          <span className="text-[11.5px] font-mono" style={{ color: "var(--muted)" }}>{e.p}</span>
        </div>
      ))}
    </div>
  );
}

function ScenePicker({ sceneId, onPick }: { sceneId: string; onPick: (id: string) => void }) {
  return (
    <div>
      <div className="text-[11px] font-mono uppercase tracking-wider mb-2" style={{ color: "var(--faint)" }}>Camera input</div>
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
        {SCENES.map((s) => (
          <button key={s.id} onClick={() => onPick(s.id)} className="rounded-xl overflow-hidden transition-all text-left"
            style={{ border: `1px solid ${sceneId === s.id ? CYAN : "var(--border)"}`, boxShadow: sceneId === s.id ? "0 0 0 1px " + CYAN : "none" }}>
            <SceneThumb id={s.id} />
            <div className="px-2 py-1.5 text-[10.5px]" style={{ background: "var(--surf)", color: sceneId === s.id ? "#fff" : "var(--muted)" }}>{s.label}</div>
          </button>
        ))}
      </div>
    </div>
  );
}

function SceneThumb({ id }: { id: string }) {
  const scene = getScene(id);
  return (
    <svg viewBox="0 0 100 64" width="100%" style={{ display: "block", background: "#0B1322" }}>
      <rect x="0" y="40" width="100" height="24" fill="rgba(255,255,255,.04)" />
      <line x1="0" y1="40" x2="100" y2="40" stroke="rgba(255,255,255,.08)" />
      {scene.objects.map((o, i) => {
        const x = o.x * 100, y = o.y * 64, w = o.w * 100, h = o.h * 64;
        if (o.shape === "cyl") return <rect key={i} x={x - w / 2} y={y - h / 2} width={w} height={h} rx={w / 2} fill={o.color} fillOpacity={0.85} />;
        if (o.shape === "sphere") return <circle key={i} cx={x} cy={y} r={w / 2} fill={o.color} fillOpacity={0.85} />;
        return <rect key={i} x={x - w / 2} y={y - h / 2} width={w} height={h} rx={2} fill={o.color} fillOpacity={0.85} />;
      })}
    </svg>
  );
}

function ActionResponse({ latest, model }: { latest?: LogEntry; model: ReturnType<typeof getModel> }) {
  const [showJson, setShowJson] = useState(false);
  if (!latest) {
    return (
      <div className="rounded-xl p-6 text-center" style={{ background: "rgba(255,255,255,.02)", border: "1px dashed var(--border-med)" }}>
        <Activity size={22} className="mx-auto mb-2" style={{ color: "var(--faint)" }} />
        <p className="text-[12.5px]" style={{ color: "var(--muted)" }}>Send an instruction to get an action vector back.</p>
      </div>
    );
  }
  const vector = latest.result.action.vector;
  const labels = actionLabels(model.actionDim);
  return (
    <div className="rounded-xl p-4" style={{ background: "rgba(0,212,255,.04)", border: "1px solid rgba(0,212,255,.22)" }}>
      <div className="flex items-center justify-between mb-3">
        <span className="text-[11px] font-mono uppercase tracking-wider" style={{ color: CYAN }}>action · {model.actionDim}-D</span>
        <span className="text-[11px] font-mono" style={{ color: GREEN }}>200 OK · {latest.result.latency_ms} ms</span>
      </div>
      <div className="flex flex-col gap-1.5">
        {vector.map((v, i) => (
          <div key={i} className="flex items-center gap-2">
            <span className="text-[11px] font-mono w-20 shrink-0 truncate" style={{ color: "var(--muted)" }}>{labels[i] ?? `a${i}`}</span>
            <div className="flex-1 h-3 rounded relative" style={{ background: "rgba(255,255,255,.05)" }}>
              <div className="absolute top-0 bottom-0 w-px" style={{ left: "50%", background: "var(--border-med)" }} />
              <div className="absolute top-0 bottom-0 rounded" style={{
                background: v >= 0 ? CYAN : "#A78BFA",
                left: v >= 0 ? "50%" : `${50 + v * 50}%`,
                width: `${Math.abs(v) * 50}%`,
              }} />
            </div>
            <span className="text-[10.5px] font-mono tabular-nums w-12 text-right shrink-0" style={{ color: "#fff" }}>{v.toFixed(2)}</span>
          </div>
        ))}
      </div>
      <button onClick={() => setShowJson((v) => !v)} className="mt-3 inline-flex items-center gap-1 text-[11px] font-mono transition-colors" style={{ color: "var(--muted)" }}>
        <ChevronRight size={12} className="transition-transform" style={{ transform: showJson ? "rotate(90deg)" : "none" }} /> response JSON
      </button>
      {showJson && (
        <pre className="mt-2 text-[10.5px] font-mono p-3 rounded-lg overflow-x-auto leading-[1.5]" style={{ background: "#0A0F1C", color: "rgba(255,255,255,.75)" }}>
{JSON.stringify({ action: latest.result.action, raw_output: latest.result.raw_output, latency_ms: latest.result.latency_ms }, null, 2)}
        </pre>
      )}
    </div>
  );
}

// ── RIGHT: metrics ────────────────────────────────────────────────────────────

function MetricsPanel({ snap, config, est }: { snap: MetricsSnapshot | null; config: ServeConfig; est: ReturnType<typeof estimate> }) {
  const isCpu = config.device === "cpu";
  const utilLabel = isCpu ? "CPU" : "GPU";
  const rateFrac = snap ? Math.min(1, snap.throughput / config.rateLimit) : 0;
  return (
    <aside className="flex flex-col overflow-y-auto" style={{ background: "var(--surf)", maxHeight: "calc(100vh - 56px)" }}>
      <SectionHead icon={<Activity size={15} />} title="Latency · p50" hint="live" />
      <div className="px-4 pb-3">
        <div className="flex items-baseline gap-2">
          <span className="text-[26px] font-display font-bold tabular-nums">{snap?.p50 ?? 0}</span>
          <span className="text-[12px]" style={{ color: "var(--muted)" }}>ms</span>
          <span className="ml-auto text-[11px] font-mono" style={{ color: "var(--faint)" }}>p95 {snap?.p95 ?? 0} ms</span>
        </div>
        <Sparkline values={snap?.latencyHistory ?? []} />
      </div>

      <SectionHead icon={<Zap size={15} />} title="Throughput" />
      <div className="px-4 pb-3">
        <div className="flex items-baseline gap-2">
          <span className="text-[22px] font-display font-bold tabular-nums" style={{ color: CYAN }}>{snap?.throughput ?? 0}</span>
          <span className="text-[12px]" style={{ color: "var(--muted)" }}>req/s</span>
        </div>
        <div className="flex justify-between text-[11px] mt-2 mb-1"><span style={{ color: "var(--muted)" }}>rate limit</span><span className="font-mono" style={{ color: "var(--faint)" }}>{config.rateLimit} req/s</span></div>
        <Bar frac={rateFrac} color={rateFrac > 0.9 ? AMBER : GREEN} />
        <p className="text-[10.5px] mt-2" style={{ color: "var(--faint)" }}>{snap?.reqToday ?? 0} requests this session · batch {config.batchSize}</p>
      </div>

      <SectionHead icon={<Gauge size={15} />} title={utilLabel} />
      <div className="px-4 pb-4">
        <div className="flex items-center gap-4">
          <Ring frac={(snap?.gpuUtil ?? 0) / 100} label={`${snap?.gpuUtil ?? 0}%`} sub="UTIL" />
          <div className="flex-1 flex flex-col gap-2.5">
            <div>
              <div className="flex justify-between text-[11px] mb-1"><span style={{ color: "var(--muted)" }}>{isCpu ? "RAM" : "VRAM"}</span><span className="font-mono tabular-nums" style={{ color: "#fff" }}>{snap?.vramUsed ?? 0} / {snap?.vramTotal ?? 0} GB</span></div>
              <Bar frac={snap ? Math.min(1, snap.vramUsed / snap.vramTotal) : 0} color={CYAN} />
            </div>
            <div>
              <div className="flex justify-between text-[11px] mb-1"><span style={{ color: "var(--muted)" }}>Temp</span><span className="font-mono tabular-nums" style={{ color: "#fff" }}>{snap?.temp ?? 0}°C</span></div>
              <Bar frac={snap ? Math.min(1, (snap.temp - 30) / 60) : 0} color={(snap?.temp ?? 0) > 75 ? AMBER : GREEN} />
            </div>
          </div>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-4">
          <Pill label={snap?.loaded ? (effectiveQuant(config) ? "4-bit · loaded" : "loaded") : "not loaded"} color={snap?.loaded ? GREEN : "var(--faint)"} />
          <Pill label="healthy" color={CYAN} />
          <Pill label="prometheus" color="#A78BFA" />
        </div>
      </div>
    </aside>
  );
}

// ── shared atoms ──────────────────────────────────────────────────────────────

function SectionHead({ icon, title, hint }: { icon: React.ReactNode; title: string; hint?: string }) {
  return (
    <div className="flex items-center gap-2 px-4 pt-4 pb-2">
      <span style={{ color: CYAN }}>{icon}</span>
      <h2 className="text-[12px] font-mono uppercase tracking-wider" style={{ color: "var(--muted)" }}>{title}</h2>
      {hint && <span className="ml-auto inline-flex items-center gap-1 text-[10px] font-mono" style={{ color: "var(--faint)" }}><span className="badge-dot" style={{ background: GREEN }} /> {hint}</span>}
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="text-[11px] font-mono uppercase tracking-wider block mb-1.5" style={{ color: "var(--faint)" }}>{label}</label>
      {children}
    </div>
  );
}

function HealthPill({ loaded }: { loaded: boolean }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full" style={{ background: `${(loaded ? GREEN : AMBER)}1a`, color: loaded ? GREEN : AMBER, border: `1px solid ${(loaded ? GREEN : AMBER)}66` }}>
      <span className="w-1.5 h-1.5 rounded-full" style={{ background: loaded ? GREEN : AMBER }} /> {loaded ? "ready" : "no model"}
    </span>
  );
}

function Pill({ label, color }: { label: string; color: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-[10.5px] font-mono px-2 py-1 rounded-full" style={{ background: "rgba(255,255,255,.04)", color, border: `1px solid ${color === "var(--faint)" ? "var(--border)" : color + "55"}` }}>
      <span className="w-1.5 h-1.5 rounded-full" style={{ background: color }} /> {label}
    </span>
  );
}

function Bar({ frac, color }: { frac: number; color: string }) {
  return (
    <div className="h-2 rounded-full overflow-hidden" style={{ background: "rgba(255,255,255,.08)" }}>
      <div className="h-full rounded-full transition-all duration-500" style={{ width: `${Math.max(2, Math.min(1, frac) * 100)}%`, background: color }} />
    </div>
  );
}

function Ring({ frac, label, sub }: { frac: number; label: string; sub: string }) {
  const r = 30, c = 2 * Math.PI * r;
  const f = Math.max(0, Math.min(1, frac));
  return (
    <svg width="78" height="78" viewBox="0 0 78 78" className="shrink-0">
      <circle cx="39" cy="39" r={r} fill="none" stroke="rgba(255,255,255,.09)" strokeWidth="8" />
      <circle cx="39" cy="39" r={r} fill="none" stroke={CYAN} strokeWidth="8" strokeLinecap="round"
        strokeDasharray={`${(f * c).toFixed(1)} ${c.toFixed(1)}`} transform="rotate(-90 39 39)" className="transition-all duration-500" />
      <text x="39" y="38" textAnchor="middle" className="font-display" fontSize="15" fontWeight="700" fill="#fff">{label}</text>
      <text x="39" y="50" textAnchor="middle" fontFamily="monospace" fontSize="7.5" fill="rgba(255,255,255,.3)">{sub}</text>
    </svg>
  );
}

function Sparkline({ values }: { values: number[] }) {
  if (values.length < 2) {
    return <div className="h-12 mt-2 flex items-center justify-center text-[10.5px] font-mono" style={{ color: "var(--faint)" }}>no requests yet</div>;
  }
  const w = 280, h = 48;
  const min = Math.min(...values), max = Math.max(...values);
  const span = max - min || 1;
  const pts = values.map((v, i) => {
    const x = (i / (values.length - 1)) * w;
    const y = h - ((v - min) / span) * (h - 6) - 3;
    return `${i === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(1)}`;
  });
  const d = pts.join(" ");
  const area = `${d} L${w} ${h} L0 ${h} Z`;
  return (
    <svg viewBox={`0 0 ${w} ${h}`} width="100%" height="48" className="mt-2" preserveAspectRatio="none">
      <path d={area} fill={CYAN} fillOpacity="0.1" />
      <path d={d} fill="none" stroke={CYAN} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" vectorEffect="non-scaling-stroke" />
    </svg>
  );
}
