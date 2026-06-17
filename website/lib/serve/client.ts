/*
 * OhhO Serve — client/deploy snippet generators.
 *
 * Produces copy-pasteable commands that match the REAL packages/vla_serve API
 * and env vars (VLA_MODEL_CLASS, VLA_MODEL_PATH, VLA_LOAD_4BIT, VLA_PORT,
 * VLA_API_KEY) — so what you configure in the console is what you run.
 */

import { effectiveQuant, getModel, type ServeConfig } from "./models";

export interface Snippet {
  id: string;
  label: string;
  lang: string;
  code: string;
}

const base = (c: ServeConfig) => `http://localhost:${c.port}`;

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
  return [
    { id: "launch", label: "Deploy (one command)", lang: "bash", code: launchCmd(config) },
    { id: "curl", label: "Call /predict (curl)", lang: "bash", code: curlCmd(config) },
    { id: "python", label: "Python client", lang: "python", code: pythonClient(config) },
    { id: "health", label: "Health check", lang: "bash", code: healthCmd(config) },
  ];
}
