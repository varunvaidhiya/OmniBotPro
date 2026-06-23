/*
 * Serve MCP tools — VLA model registry, GPU targets, and VRAM estimation.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";
import { MODELS, GPUS, DEFAULT_CONFIG, getModel, getGpu, estimate, type Backend } from "@/lib/serve/models";
import { SCENES, predict } from "@/lib/serve/inference";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "serve.listModels",
    description: "List all available VLA model backends (SmolVLA, OpenVLA 7B, ACT, Diffusion, Custom) with checkpoint, VRAM requirements, and latency estimates.",
    inputSchema: s({}),
    handler: async () => jsonResult(MODELS),
    product: "serve", readOnly: true,
  },
  {
    name: "serve.getModel",
    description: "Get details for a specific model backend by ID.",
    inputSchema: s(
      { backend: { type: "string", description: "Model backend ID", enum: ["smolvla", "openvla", "act", "diffusion", "custom"] } },
      ["backend"],
    ),
    handler: async (p) => jsonResult(getModel(p.backend as Backend)),
    product: "serve", readOnly: true,
  },
  {
    name: "serve.listGpus",
    description: "List all supported GPU targets (RTX 4090, A100, L4, T4, Jetson Orin, etc.) with VRAM and performance info.",
    inputSchema: s({}),
    handler: async () => jsonResult(GPUS),
    product: "serve", readOnly: true,
  },
  {
    name: "serve.estimate",
    description: "Estimate VRAM usage, fit, latency, and throughput for a given model + GPU + quantization configuration. Returns whether the model fits on the GPU and the expected latency.",
    inputSchema: s({
      backend: { type: "string", description: "Model backend", enum: ["smolvla", "openvla", "act", "diffusion", "custom"] },
      gpuId: { type: "string", description: "GPU ID (e.g. 'rtx4090', 'a100', 'l4', 't4', 'jetson-orin')" },
      quant4bit: { type: "boolean", description: "Enable 4-bit quantization" },
      batchSize: { type: "number", description: "Batch size (default 1)" },
    }, ["backend", "gpuId"]),
    handler: async (p) => {
      const cfg = { ...DEFAULT_CONFIG, backend: p.backend as Backend, gpuId: p.gpuId as string, quant4bit: Boolean(p.quant4bit), batchSize: Number(p.batchSize ?? 1) };
      return jsonResult(estimate(cfg));
    },
    product: "serve", readOnly: true,
  },
  {
    name: "serve.getDefaultConfig",
    description: "Get the default Serve configuration (model, GPU, port, quantization).",
    inputSchema: s({}),
    handler: async () => jsonResult(DEFAULT_CONFIG),
    product: "serve", readOnly: true,
  },
  {
    name: "serve.listScenes",
    description: "List the built-in test scenes (tabletop, kitchen, etc.) that can be used to try VLA inference.",
    inputSchema: s({}),
    handler: async () => jsonResult(SCENES.map((sc) => ({ id: sc.id, label: sc.label, defaultInstruction: sc.instruction }))),
    product: "serve", readOnly: true,
  },
  {
    name: "serve.predict",
    description: "Run a VLA inference: given a scene and a natural-language instruction, returns the predicted action vector and latency. Uses deterministic simulation in the console.",
    inputSchema: s({
      backend: { type: "string", description: "Model backend", enum: ["smolvla", "openvla", "act", "diffusion", "custom"] },
      sceneId: { type: "string", description: "Scene id from serve.listScenes" },
      instruction: { type: "string", description: "Task instruction, e.g. 'pick up the red cup'" },
      quant4bit: { type: "boolean", description: "Use 4-bit quantization" },
    }, ["sceneId", "instruction"]),
    handler: async (p) => {
      if (!SCENES.some((sc) => sc.id === p.sceneId)) return errorResult(`Unknown scene: ${p.sceneId}. Use serve.listScenes.`);
      const cfg = { ...DEFAULT_CONFIG, backend: (p.backend as Backend) ?? DEFAULT_CONFIG.backend, quant4bit: Boolean(p.quant4bit) };
      const result = predict(cfg, { sceneId: p.sceneId as string, instruction: p.instruction as string });
      return jsonResult({ backend: cfg.backend, sceneId: p.sceneId, instruction: p.instruction, ...result });
    },
    product: "serve", readOnly: false,
  },
  {
    name: "serve.deploy",
    description: "Deploy a model to an inference server on a GPU target. Validates that the model fits in VRAM, then returns the serving endpoint.",
    inputSchema: s({
      backend: { type: "string", description: "Model backend", enum: ["smolvla", "openvla", "act", "diffusion", "custom"] },
      gpuId: { type: "string", description: "GPU target id (see serve.listGpus)" },
      quant4bit: { type: "boolean", description: "Use 4-bit quantization to fit larger models" },
      port: { type: "number", description: "Port to serve on (default 8000)" },
    }, ["backend", "gpuId"]),
    handler: async (p) => {
      if (!GPUS.some((g) => g.id === p.gpuId)) return errorResult(`Unknown GPU: ${p.gpuId}. Use serve.listGpus.`);
      const gpu = getGpu(p.gpuId as string);
      const cfg = { ...DEFAULT_CONFIG, backend: p.backend as Backend, gpuId: p.gpuId as string, quant4bit: Boolean(p.quant4bit), port: Number(p.port ?? 8000) };
      const est = estimate(cfg);
      if (!est.fits) return errorResult(`${getModel(cfg.backend).name} does not fit on ${gpu.name}${cfg.quant4bit ? " even at 4-bit" : " — try quant4bit:true"}.`);
      return jsonResult({
        deployed: true, backend: cfg.backend, gpu: gpu.name, port: cfg.port,
        endpoint: `http://localhost:${cfg.port}/predict`, estimate: est,
        message: `${getModel(cfg.backend).name} deployed on ${gpu.name}, serving on port ${cfg.port}.`,
      });
    },
    product: "serve", readOnly: false,
  },
  {
    name: "serve.stop",
    description: "Stop a running inference server.",
    inputSchema: s({ port: { type: "number", description: "Port of the server to stop (default 8000)" } }),
    handler: async (p) => jsonResult({ stopped: true, port: Number(p.port ?? 8000), message: `Inference server on port ${Number(p.port ?? 8000)} stopped.` }),
    product: "serve", readOnly: false,
  },
];
