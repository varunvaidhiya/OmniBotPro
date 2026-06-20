/*
 * Shared AI types + sanitizers for the robot enrichment and dynamic
 * console-spec routes. Pure data + validation — no server-only imports — so it
 * is shared by the API routes, the client fetchers and the console-kit renderer.
 *
 * Everything an LLM returns flows through the sanitizers here before it is
 * trusted. The model can only ever choose from fixed vocabularies (panel kinds,
 * sensor kinds) and fill bounded, truncated string/number fields — it can never
 * inject raw markup or unbounded data into a console.
 */

import type { ComputeSpec, SensorKind } from "@/lib/garage/robot-config";

// ── Enrichment (informational; augments, never replaces, the derived config) ──

export interface RobotEnrichment {
  /** One-line plain-language summary of the robot. */
  summary: string;
  /** Detailed factual bullets (SDK, ROS support, real sensor models, etc.). */
  notes: string[];
  /** Refined compute description. */
  compute?: ComputeSpec;
  /** AI/VLA models well-suited to this robot (used by Serve / Train). */
  recommendedModels?: string[];
  /** Example tasks this robot is good at (used by Autonomy / Mind / Data). */
  suggestedTasks?: string[];
}

// ── Dynamic console spec (drives the console-kit renderer) ───────────────────

export const PANEL_KINDS = [
  "metrics",
  "status",
  "sensors",
  "joints",
  "cameras",
  "actions",
  "note",
] as const;
export type PanelKind = (typeof PANEL_KINDS)[number];

export interface ConsolePanel {
  kind: PanelKind;
  title: string;
  /** label/value rows — used by metrics, status, note. */
  items?: { label: string; value?: string }[];
  /** display-only action labels (no handlers) — used by actions. */
  actions?: string[];
}

export interface ConsoleSpec {
  headline: string;
  panels: ConsolePanel[];
}

// ── Limits ───────────────────────────────────────────────────────────────────

const MAX_PANELS = 8;
const MAX_ITEMS = 12;
const MAX_ACTIONS = 8;
const MAX_NOTES = 10;
const MAX_LIST = 12;
const STR = 240;
const SHORT = 80;

const VALID_SENSOR_KINDS: ReadonlySet<SensorKind> = new Set<SensorKind>([
  "rgb_camera", "depth_camera", "stereo_camera", "thermal_camera", "fpv_camera",
  "bev", "lidar_2d", "lidar_3d", "imu", "gps", "sonar", "barometer", "encoder", "force_torque",
]);

function str(v: unknown, max = STR): string {
  return typeof v === "string" ? v.trim().slice(0, max) : "";
}
function strList(v: unknown, maxItems: number, maxLen = STR): string[] {
  if (!Array.isArray(v)) return [];
  return v.map((x) => str(x, maxLen)).filter(Boolean).slice(0, maxItems);
}
function num(v: unknown): number | undefined {
  return typeof v === "number" && Number.isFinite(v) ? v : undefined;
}

/** Validate + clamp anything the model returns for /api/robot/enrich. */
export function sanitizeEnrichment(raw: unknown): RobotEnrichment | null {
  if (!raw || typeof raw !== "object") return null;
  const r = raw as Record<string, unknown>;
  const summary = str(r.summary, 400);
  const notes = strList(r.notes, MAX_NOTES);
  if (!summary && notes.length === 0) return null;

  const out: RobotEnrichment = { summary, notes };

  if (r.compute && typeof r.compute === "object") {
    const c = r.compute as Record<string, unknown>;
    const brain = str(c.brain, SHORT);
    if (brain) {
      out.compute = {
        brain,
        accelerator: str(c.accelerator, SHORT) || undefined,
        vramGb: (() => { const n = num(c.vramGb); return n != null ? Math.max(0, Math.min(512, n)) : undefined; })(),
      };
    }
  }
  const models = strList(r.recommendedModels, MAX_LIST, SHORT);
  if (models.length) out.recommendedModels = models;
  const tasks = strList(r.suggestedTasks, MAX_LIST);
  if (tasks.length) out.suggestedTasks = tasks;

  return out;
}

/** Validate + clamp anything the model returns for /api/robot/console-spec. */
export function sanitizeConsoleSpec(raw: unknown): ConsoleSpec | null {
  if (!raw || typeof raw !== "object") return null;
  const r = raw as Record<string, unknown>;
  const headline = str(r.headline, 160);
  const panelsRaw = Array.isArray(r.panels) ? r.panels : [];
  const validKinds = new Set<string>(PANEL_KINDS);

  const panels: ConsolePanel[] = [];
  for (const p of panelsRaw) {
    if (!p || typeof p !== "object") continue;
    const pr = p as Record<string, unknown>;
    const kind = str(pr.kind, 24) as PanelKind;
    if (!validKinds.has(kind)) continue;
    const panel: ConsolePanel = { kind, title: str(pr.title, SHORT) || kind };

    if (Array.isArray(pr.items)) {
      panel.items = pr.items
        .filter((it): it is Record<string, unknown> => !!it && typeof it === "object")
        .map((it) => ({ label: str(it.label, SHORT), value: str(it.value, SHORT) || undefined }))
        .filter((it) => it.label)
        .slice(0, MAX_ITEMS);
    }
    if (Array.isArray(pr.actions)) {
      panel.actions = strList(pr.actions, MAX_ACTIONS, SHORT);
    }
    panels.push(panel);
    if (panels.length >= MAX_PANELS) break;
  }

  if (!headline && panels.length === 0) return null;
  return { headline, panels };
}

/** Exposed for callers that want to validate sensor kinds (used by enrich). */
export function isValidSensorKind(k: string): k is SensorKind {
  return VALID_SENSOR_KINDS.has(k as SensorKind);
}
