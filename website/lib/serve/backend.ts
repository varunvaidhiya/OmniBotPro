/*
 * OhhO Serve — real backend connectors.
 *
 * Three inference runtimes, one interface. Each backend returns the same
 * PredictResult shape as the simulated engine, so the console UI doesn't
 * know or care which one is active.
 *
 *   • server      — fetch(serverUrl/predict)  → real GPU inference
 *   • webgpu      — ONNX Runtime Web + WebGPU  → browser-GPU (coming soon)
 *   • transformers — Transformers.js            → HuggingFace in-browser (coming soon)
 *
 * When a backend errors or is unavailable, the caller falls back to the
 * deterministic simulator in inference.ts — the playground is never dead.
 */

import type { ServeConfig } from "./models";
import type { PredictRequest, PredictResult } from "./inference";

// ═══════════════════════════════════════════════════════════════════════════
// 1. Server mode — real FastAPI vla_serve call
// ═══════════════════════════════════════════════════════════════════════════

export interface ServerPredictResponse {
  status: "ok" | "error";
  action?: { vector: number[] };
  raw_output?: string;
  latency_ms?: number;
  detail?: string;
}

export async function predictServer(
  config: ServeConfig,
  request: PredictRequest,
): Promise<PredictResult> {
  const url = config.serverUrl.replace(/\/+$/, "") + "/predict";
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (config.apiKey) headers["X-API-Key"] = config.apiKey;

  const body: Record<string, unknown> = {
    instruction: request.instruction,
    scene_id: request.sceneId,
  };

  try {
    const start = performance.now();
    const resp = await fetch(url, {
      method: "POST",
      headers,
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(15000),
    });

    if (!resp.ok) {
      const text = await resp.text().catch(() => "");
      throw new Error(`Server returned ${resp.status}: ${text}`);
    }

    const json = (await resp.json()) as ServerPredictResponse;
    const wallMs = Math.round(performance.now() - start);

    return {
      action: { vector: json.action?.vector ?? [] },
      raw_output:
        json.raw_output ?? JSON.stringify(json.action?.vector ?? []),
      latency_ms: json.latency_ms ?? wallMs,
    };
  } catch (err) {
    throw new Error(
      `vla_serve unreachable at ${config.serverUrl} — is the server running? (${err instanceof Error ? err.message : String(err)})`,
    );
  }
}

// ═══════════════════════════════════════════════════════════════════════════
// 2. WebGPU mode — ONNX Runtime Web (placeholder)
// ═══════════════════════════════════════════════════════════════════════════

let _webGpuWarned = false;

export async function predictWebGPU(
  _config: ServeConfig,
  _request: PredictRequest,
): Promise<PredictResult> {
  if (!_webGpuWarned) {
    console.info(
      "[OhhO Serve] WebGPU backend not yet bundled. Add 'onnxruntime-web' to package.json, " +
        "export your policy from rl_engine/export/export_policy.py, and place the .onnx " +
        "file in /public/models/. Falling back to simulator.",
    );
    _webGpuWarned = true;
  }
  throw new Error(
    "ONNX Runtime Web is not installed. Run `npm install onnxruntime-web` and drop " +
      "your exported .onnx model in public/models/. Then select 'ONNX Runtime WebGPU' " +
      "in the mode picker to enable.",
  );
}

// ═══════════════════════════════════════════════════════════════════════════
// 3. Transformers.js mode (placeholder)
// ═══════════════════════════════════════════════════════════════════════════

let _transformersWarned = false;

export async function predictTransformers(
  _config: ServeConfig,
  _request: PredictRequest,
): Promise<PredictResult> {
  if (!_transformersWarned) {
    console.info(
      "[OhhO Serve] Transformers.js backend not yet bundled. Add '@huggingface/transformers' " +
        "to package.json to enable browser-side HuggingFace model inference.",
    );
    _transformersWarned = true;
  }
  throw new Error(
    "Transformers.js is not installed. Run `npm install @huggingface/transformers` to " +
      "enable browser-side HuggingFace model inference.",
  );
}
