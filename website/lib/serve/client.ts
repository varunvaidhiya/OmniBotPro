/*
 * OhhO Serve — client/deploy snippet generators.
 *
 * Produces copy-pasteable commands that match the REAL packages/vla_serve API
 * and env vars (VLA_MODEL_CLASS, VLA_MODEL_PATH, VLA_LOAD_4BIT, VLA_PORT,
 * VLA_API_KEY) — so what you configure in the console is what you run.
 */

import { effectiveQuant, getModel, BACKEND_MODES, type ServeConfig, type BackendMode } from "./models";

export interface Snippet {
  id: string;
  label: string;
  lang: string;
  code: string;
}

const base = (c: ServeConfig) => c.backendMode === "server" ? c.serverUrl.replace(/\/+$/, "") : `http://localhost:${c.port}`;

function launchCmd(c: ServeConfig): string {
  const model = getModel(c.backend);
  const quant = effectiveQuant(c);
  const lines = [
    "docker run --rm \\",
    c.device === "cuda" ? "  --gpus all \\" : "  # CPU mode — no --gpus \\",
    `  -p ${c.port}:8000 \\`,
    `  -e VLA_MODEL_CLASS=${model.modelClass} \\`,
    `  -e VLA_MODEL_PATH=${c.checkpoint} \\`,
    `  -e VLA_LOAD_4BIT=${quant ? "1" : "0"} \\`,
    "  -e VLA_AUTO_LOAD=1 \\",
    `  -e VLA_RATE_LIMIT=${c.rateLimit} \\`,
  ];
  if (c.apiKey) lines.push(`  -e VLA_API_KEY=${c.apiKey} \\`);
  lines.push("  ohho/serve:latest");
  return lines.join("\n");
}

function curlCmd(c: ServeConfig): string {
  const auth = c.apiKey ? `  -H "X-API-Key: ${c.apiKey}" \\\n` : "";
  return [
    `curl -X POST ${base(c)}/predict \\`,
    `  -H "Content-Type: application/json" \\`,
    auth + `  -d '{`,
    `    "instruction": "pick up the red cup",`,
    `    "image_base64": "'$(base64 -w0 frame.jpg)'"`,
    `  }'`,
  ].join("\n");
}

function pythonClient(c: ServeConfig): string {
  const headers = c.apiKey
    ? `{"Content-Type": "application/json", "X-API-Key": "${c.apiKey}"}`
    : `{"Content-Type": "application/json"}`;
  return [
    "import base64, requests",
    "",
    'with open("frame.jpg", "rb") as f:',
    "    img_b64 = base64.b64encode(f.read()).decode()",
    "",
    `resp = requests.post(`,
    `    "${base(c)}/predict",`,
    `    headers=${headers},`,
    `    json={"instruction": "pick up the red cup", "image_base64": img_b64},`,
    ")",
    'action = resp.json()["action"]["vector"]',
    "print(action)  # send to /arm/joint_commands or /cmd_vel",
  ].join("\n");
}

function healthCmd(c: ServeConfig): string {
  return `curl ${base(c)}/health\n# {"status":"ok","model_loaded":true}`;
}

export function snippets(config: ServeConfig): Snippet[] {
  if (config.backendMode === "server") return serverSnippets(config);
  if (config.backendMode === "webgpu") return webgpuSnippets(config);
  if (config.backendMode === "transformers") return transformersSnippets(config);
  return serverSnippets(config);
}

function serverSnippets(c: ServeConfig): Snippet[] {
  return [
    { id: "launch", label: "Deploy (one command)", lang: "bash", code: launchCmd(c) },
    { id: "curl", label: "Call /predict (curl)", lang: "bash", code: curlCmd(c) },
    { id: "python", label: "Python client", lang: "python", code: pythonClient(c) },
    { id: "health", label: "Health check", lang: "bash", code: healthCmd(c) },
  ];
}

function webgpuSnippets(c: ServeConfig): Snippet[] {
  const model = getModel(c.backend);
  return [
    { id: "install", label: "Install", lang: "bash", code: "npm install onnxruntime-web" },
    { id: "export", label: "Export from rl_engine", lang: "bash", code: [
      "python rl_engine/export/export_policy.py \\",
      `  --checkpoint ~/models/${model.checkpoint.split("/").pop()}.pt \\`,
      `  --output public/models/policy.onnx \\`,
      "  --type arm --opset 17",
    ].join("\n") },
    { id: "load", label: "Load in browser", lang: "typescript", code: [
      "import * as ort from 'onnxruntime-web';",
      "",
      "ort.env.wasm.wasmPaths = '/_next/static/wasm/';",
      `const session = await ort.InferenceSession.create(`,
      `  '${c.serverUrl}/models/policy.onnx',`,
      `  { executionProviders: ['webgpu'] }`,
      ");",
      `const output = await session.run({ input: new ort.Tensor('float32', obs, [1, 27]) });`,
    ].join("\n") },
  ];
}

function transformersSnippets(c: ServeConfig): Snippet[] {
  return [
    { id: "install", label: "Install", lang: "bash", code: "npm install @huggingface/transformers" },
    { id: "load", label: "Load SmolVLA", lang: "typescript", code: [
      "import { pipeline } from '@huggingface/transformers';",
      "",
      `const vla = await pipeline('image-to-text', '${c.checkpoint}', {`,
      "  device: 'webgpu'",
      "});",
      `const result = await vla(cameraFrame, { prompt: "${c.backend === 'smolvla' ? 'pick up the object' : 'find the target'}" });`,
    ].join("\n") },
    { id: "config", label: "Serving config", lang: "typescript", code: [
      `// Add to next.config.js:`,
      `const nextConfig = {`,
      `  experimental: {`,
      `    serverComponentsExternalPackages: ['@huggingface/transformers', 'onnxruntime-web'],`,
      `  },`,
      `};`,
    ].join("\n") },
  ];
}
