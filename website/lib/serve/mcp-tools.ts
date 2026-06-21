/*
 * Serve MCP tools — VLA model registry, GPU targets, and VRAM estimation.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";
import { MODELS, GPUS, DEFAULT_CONFIG, getModel, getGpu, estimate, type Backend } from "@/lib/serve/models";

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
];
