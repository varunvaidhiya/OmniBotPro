/*
 * OhhO Serve — simulated inference.
 *
 * The real model runs on a GPU behind `packages/vla_serve`; a static website
 * can't host it. So the console runs a deterministic, instruction-aware mock
 * that returns the SAME response shape as the real /predict endpoint
 * ({ action: { vector }, raw_output, latency_ms }) and reacts to the words in
 * the instruction — so the playground feels live without pretending to be a GPU.
 */

import { estimate, getModel, type ServeConfig } from "./models";
import { predictServer, predictWebGPU, predictTransformers } from "./backend";

// ── Camera scenes the user can "point the robot at" ──────────────────────────

export interface SceneObject {
  shape: "box" | "cyl" | "sphere";
  color: string;
  x: number; // 0..1 across the frame
  y: number; // 0..1 down the frame
  w: number;
  h: number;
  label: string;
}

export interface Scene {
  id: string;
  label: string;
  instruction: string; // a sensible default prompt for this scene
  objects: SceneObject[];
}

export const SCENES: Scene[] = [
  {
    id: "tabletop",
    label: "Tabletop",
    instruction: "pick up the red cup",
    objects: [
      { shape: "cyl", color: "#F87171", x: 0.34, y: 0.52, w: 0.12, h: 0.22, label: "cup" },
      { shape: "box", color: "#60A5FA", x: 0.62, y: 0.58, w: 0.16, h: 0.16, label: "block" },
      { shape: "sphere", color: "#FBBF24", x: 0.5, y: 0.4, w: 0.1, h: 0.1, label: "ball" },
    ],
  },
  {
    id: "shelf",
    label: "Warehouse shelf",
    instruction: "place the box on the left shelf",
    objects: [
      { shape: "box", color: "#A78BFA", x: 0.28, y: 0.46, w: 0.2, h: 0.26, label: "box A" },
      { shape: "box", color: "#34D399", x: 0.6, y: 0.5, w: 0.22, h: 0.24, label: "box B" },
    ],
  },
  {
    id: "bin",
    label: "Parts bin",
    instruction: "grasp the wrench and lift it up",
    objects: [
      { shape: "box", color: "#9CA3AF", x: 0.46, y: 0.6, w: 0.34, h: 0.12, label: "wrench" },
      { shape: "cyl", color: "#F59E0B", x: 0.3, y: 0.42, w: 0.08, h: 0.16, label: "bolt" },
    ],
  },
  {
    id: "kitchen",
    label: "Kitchen counter",
    instruction: "move forward then open the gripper",
    objects: [
      { shape: "cyl", color: "#FFFFFF", x: 0.52, y: 0.5, w: 0.13, h: 0.24, label: "mug" },
      { shape: "box", color: "#F472B6", x: 0.74, y: 0.56, w: 0.14, h: 0.14, label: "carton" },
    ],
  },
];

export function getScene(id: string): Scene {
  return SCENES.find((s) => s.id === id) ?? SCENES[0];
}

// ── Action layout labels per model output dim ────────────────────────────────

const LABELS_7 = ["Δx", "Δy", "Δz", "Δroll", "Δpitch", "Δyaw", "grip"];
const LABELS_9 = ["shoulder", "lift", "elbow", "wrist_flex", "wrist_roll", "grip", "base_vx", "base_vy", "base_ω"];

export function actionLabels(actionDim: number): string[] {
  if (actionDim === 7) return LABELS_7;
  if (actionDim === 9) return LABELS_9;
  return Array.from({ length: actionDim }, (_, i) => `a${i}`);
}

// ── Instruction → intent ─────────────────────────────────────────────────────

interface Intent {
  x: number; // forward(+) / back(-)
  y: number; // left(+) / right(-)
  z: number; // up(+) / down(-)
  yaw: number; // rotate
  grip: number; // close(+1) / open(-1)
  reach: number; // extend the arm
}

function parseIntent(instruction: string): Intent {
  const s = instruction.toLowerCase();
  const has = (...words: string[]) => words.some((w) => s.includes(w));
  const intent: Intent = { x: 0, y: 0, z: 0, yaw: 0, grip: 0, reach: 0 };

  if (has("forward", "ahead", "approach", "toward")) intent.x += 0.6;
  if (has("back", "backward", "retreat", "away")) intent.x -= 0.6;
  if (has("left")) intent.y += 0.6;
  if (has("right")) intent.y -= 0.6;
  if (has("up", "lift", "raise")) intent.z += 0.6;
  if (has("down", "lower", "drop")) intent.z -= 0.5;
  if (has("rotate", "turn", "spin")) intent.yaw += 0.5;
  if (has("pick", "grab", "grasp", "close", "hold")) {
    intent.grip += 1;
    intent.reach += 0.6;
    intent.z -= 0.3; // reach down to the object
  }
  if (has("place", "release", "open", "let go", "put")) {
    intent.grip -= 1;
    intent.reach += 0.4;
  }
  if (has("reach", "extend")) intent.reach += 0.5;
  return intent;
}

// ── Deterministic RNG (so the same scene+prompt always returns the same act) ──

function hash(str: string): number {
  let h = 5381;
  for (let i = 0; i < str.length; i++) h = ((h << 5) + h + str.charCodeAt(i)) | 0;
  return h >>> 0;
}

function mulberry32(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const clamp = (v: number, lo = -1, hi = 1) => Math.max(lo, Math.min(hi, v));

function toAction(actionDim: number, intent: Intent, rng: () => number): number[] {
  const noise = () => (rng() - 0.5) * 0.12;
  const out = new Array(actionDim).fill(0).map(noise);

  if (actionDim === 7) {
    out[0] = clamp(intent.x + noise());
    out[1] = clamp(intent.y + noise());
    out[2] = clamp(intent.z + noise());
    out[4] = clamp(intent.reach * 0.2 + noise());
    out[5] = clamp(intent.yaw + noise());
    out[6] = clamp(intent.grip > 0 ? 0.9 : intent.grip < 0 ? -0.9 : noise());
  } else if (actionDim === 9) {
    out[0] = clamp(intent.reach * 0.5 + noise()); // shoulder
    out[1] = clamp(intent.z * -0.5 + noise()); // lift (down → +lift toward object)
    out[2] = clamp(intent.reach * 0.45 + noise()); // elbow
    out[4] = clamp(intent.yaw * 0.3 + noise()); // wrist_roll
    out[5] = clamp(intent.grip > 0 ? 0.9 : intent.grip < 0 ? -0.9 : noise()); // grip
    out[6] = clamp(intent.x + noise()); // base_vx
    out[7] = clamp(intent.y + noise()); // base_vy
    out[8] = clamp(intent.yaw + noise()); // base_ω
  } else {
    for (let i = 0; i < actionDim; i++) out[i] = clamp(out[i] + (intent.x + intent.y) * 0.1);
  }
  return out.map((v) => Math.round(v * 1000) / 1000);
}

// ── Public API — mirrors the real /predict response ──────────────────────────

export interface PredictRequest {
  sceneId: string;
  instruction: string;
}

export interface PredictResult {
  action: { vector: number[] };
  raw_output: string;
  latency_ms: number;
}

/** Result from a real backend call, or null if it fell back to simulation. */
import type { ServerPredictResponse } from "./backend";

export interface PredictMeta {
  result: PredictResult;
  /** "server" | "webgpu" | "transformers" | "simulated" */
  source: "server" | "webgpu" | "transformers" | "simulated";
  /** The raw server response if source === "server". */
  serverResponse?: ServerPredictResponse;
  /** Error message if the real backend failed (fell back to sim). */
  error?: string;
}

/**
 * Try the real backend first; fall back to deterministic simulation on failure.
 * Returns both the result and metadata about which backend produced it.
 */
export async function predictWithMeta(
  config: ServeConfig,
  req: PredictRequest,
): Promise<PredictMeta> {
  // ── try real backend ──
  try {
    if (config.backendMode === "server") {
      const result = await predictServer(config, req);
      return { result, source: "server" };
    }
    if (config.backendMode === "webgpu") {
      const result = await predictWebGPU(config, req);
      return { result, source: "webgpu" };
    }
    if (config.backendMode === "transformers") {
      const result = await predictTransformers(config, req);
      return { result, source: "transformers" };
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.warn(`[OhhO Serve] ${config.backendMode} backend failed:`, msg);
    // fall through to simulation
  }

  // ── deterministic simulation (always works) ──
  return { result: simulatePredict(config, req), source: "simulated" };
}

/** Deterministic simulated inference — original behaviour. */
export function predict(config: ServeConfig, req: PredictRequest): PredictResult {
  return simulatePredict(config, req);
}

function simulatePredict(config: ServeConfig, req: PredictRequest): PredictResult {
  const model = getModel(config.backend);
  const intent = parseIntent(req.instruction);
  const rng = mulberry32(hash(`${req.sceneId}|${req.instruction}|${config.backend}|${config.quant4bit}`));
  const vector = toAction(model.actionDim, intent, rng);

  const est = estimate(config);
  // add a little realistic jitter around the modelled p50
  const jitter = 1 + (rng() - 0.5) * 0.25;
  const latency_ms = Math.max(2, Math.round(est.latencyMs * jitter));

  const raw_output = model.id === "openvla" ? `[${vector.map((v) => v.toFixed(3)).join(", ")}]` : `action=${JSON.stringify(vector)}`;
  return { action: { vector }, raw_output, latency_ms };
}
