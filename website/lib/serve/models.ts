/*
 * OhhO Serve — model registry + deploy estimators.
 *
 * Mirrors the real `packages/vla_serve` FastAPI server: the same pluggable
 * backends, 4-bit option, GPU targets and env-var configuration. Pure data +
 * math so it can drive the console UI, the VRAM "does it fit" check and the
 * generated client snippets without any backend.
 */

export type Backend = "openvla" | "smolvla" | "act" | "diffusion" | "custom";
export type Device = "cuda" | "cpu";

export interface ModelSpec {
  id: Backend;
  name: string;
  /** Default HuggingFace id / path, matching VLA_MODEL_PATH. */
  checkpoint: string;
  /** Python class path used for VLA_MODEL_CLASS. */
  modelClass: string;
  params: string;
  /** Approx. weight memory at fp16 / 4-bit, in GB. */
  vramFp16: number;
  vram4bit: number;
  supports4bit: boolean;
  /** Reference per-inference compute on an RTX 4090 (batch 1), ms. */
  baseLatencyMs: number;
  /** Output action dimensionality. */
  actionDim: number;
  desc: string;
}

export const MODELS: ModelSpec[] = [
  {
    id: "smolvla",
    name: "SmolVLA",
    checkpoint: "lerobot/smolvla_base",
    modelClass: "vla_serve.models.smolvla.SmolVLAModel",
    params: "0.45B",
    vramFp16: 1.8,
    vram4bit: 0.9,
    supports4bit: true,
    baseLatencyMs: 38,
    actionDim: 9,
    desc: "Compact mobile-manipulation policy — OmniBot's on-robot default.",
  },
  {
    id: "openvla",
    name: "OpenVLA 7B",
    checkpoint: "openvla/openvla-7b",
    modelClass: "vla_serve.models.openvla.OpenVLAModel",
    params: "7B",
    vramFp16: 14.2,
    vram4bit: 5.6,
    supports4bit: true,
    baseLatencyMs: 215,
    actionDim: 7,
    desc: "Large generalist VLA. Needs a 16 GB+ GPU, or 4-bit to fit smaller cards.",
  },
  {
    id: "act",
    name: "ACT",
    checkpoint: "lerobot/act_base",
    modelClass: "vla_serve.models.act.ACTModel",
    params: "80M",
    vramFp16: 0.4,
    vram4bit: 0.4,
    supports4bit: false,
    baseLatencyMs: 11,
    actionDim: 9,
    desc: "Action-Chunking Transformer — tiny and fast, great for high-rate control.",
  },
  {
    id: "diffusion",
    name: "Diffusion Policy",
    checkpoint: "lerobot/diffusion_base",
    modelClass: "vla_serve.models.diffusion.DiffusionModel",
    params: "260M",
    vramFp16: 0.9,
    vram4bit: 0.9,
    supports4bit: false,
    baseLatencyMs: 64,
    actionDim: 9,
    desc: "Iterative denoising policy — smooth trajectories, heavier compute.",
  },
  {
    id: "custom",
    name: "Custom fine-tune",
    checkpoint: "your-org/your-vla",
    modelClass: "your_package.YourModel",
    params: "—",
    vramFp16: 8,
    vram4bit: 3.2,
    supports4bit: true,
    baseLatencyMs: 120,
    actionDim: 7,
    desc: "Any VLAModel subclass loaded by config — your OhhO Data fine-tune drops in here.",
  },
];

export interface Gpu {
  id: string;
  name: string;
  vramGb: number;
  /** Relative throughput, 1.0 = RTX 4090. */
  perf: number;
}

export const GPUS: Gpu[] = [
  { id: "rtx4090", name: "RTX 4090", vramGb: 24, perf: 1.0 },
  { id: "rtx4080", name: "RTX 4080", vramGb: 16, perf: 0.78 },
  { id: "a100", name: "A100 40 GB", vramGb: 40, perf: 1.35 },
  { id: "l4", name: "NVIDIA L4", vramGb: 24, perf: 0.6 },
  { id: "t4", name: "NVIDIA T4", vramGb: 16, perf: 0.35 },
  { id: "orin", name: "Jetson AGX Orin", vramGb: 32, perf: 0.18 },
];

export interface ServeConfig {
  backend: Backend;
  checkpoint: string;
  device: Device;
  gpuId: string;
  quant4bit: boolean;
  batchSize: number;
  port: number;
  apiKey: string;
  rateLimit: number;
}

export const DEFAULT_CONFIG: ServeConfig = {
  backend: "smolvla",
  checkpoint: "lerobot/smolvla_base",
  device: "cuda",
  gpuId: "rtx4090",
  quant4bit: false,
  batchSize: 1,
  port: 8000,
  apiKey: "",
  rateLimit: 10,
};

export function getModel(backend: Backend): ModelSpec {
  return MODELS.find((m) => m.id === backend) ?? MODELS[0];
}

export function getGpu(id: string): Gpu {
  return GPUS.find((g) => g.id === id) ?? GPUS[0];
}

/** Effective 4-bit: only when the model supports it AND it's enabled. */
export function effectiveQuant(config: ServeConfig): boolean {
  return config.quant4bit && getModel(config.backend).supports4bit;
}

export interface Estimate {
  weightsVram: number; // GB
  activationVram: number; // GB
  overheadVram: number; // GB (CUDA context)
  totalVram: number; // GB
  gpuVram: number; // GB
  fits: boolean;
  headroom: number; // GB (negative = over budget)
  latencyMs: number; // p50 per /predict
  throughput: number; // req/s at this batch size
}

const CUDA_CONTEXT_GB = 0.8;

export function estimate(config: ServeConfig): Estimate {
  const model = getModel(config.backend);
  const gpu = getGpu(config.gpuId);
  const quant = effectiveQuant(config);

  const weightsVram = quant ? model.vram4bit : model.vramFp16;
  // activation memory scales with batch and (loosely) model size
  const perSample = Math.max(0.15, model.vramFp16 * 0.05);
  const activationVram = round(perSample * config.batchSize, 2);
  const overheadVram = config.device === "cpu" ? 0 : CUDA_CONTEXT_GB;
  const totalVram = round(weightsVram + activationVram + overheadVram, 2);

  const gpuVram = config.device === "cpu" ? Infinity : gpu.vramGb;
  const fits = config.device === "cpu" ? true : totalVram <= gpuVram;
  const headroom = config.device === "cpu" ? Infinity : round(gpuVram - totalVram, 2);

  // latency: reference compute scaled by device/quant, with sub-linear batching
  const deviceFactor = config.device === "cpu" ? 26 : 1 / gpu.perf;
  const quantFactor = quant ? 1.12 : 1;
  const compute = model.baseLatencyMs * deviceFactor * quantFactor;
  const batchTime = compute * (1 + 0.22 * (config.batchSize - 1));
  const latencyMs = round(batchTime * (1 + 0.08 * (config.batchSize - 1)), 0);
  const throughput = round((config.batchSize / batchTime) * 1000, 1);

  return { weightsVram, activationVram, overheadVram, totalVram, gpuVram, fits, headroom, latencyMs, throughput };
}

function round(v: number, dp: number): number {
  const f = 10 ** dp;
  return Math.round(v * f) / f;
}
