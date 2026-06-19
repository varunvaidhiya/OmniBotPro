/*
 * OhhO Build — natural-language design generation.
 *
 * POST /api/build/generate
 *   body: { prompt: string, requirements?: Partial<Requirements> }
 *   200:  { design: Design }
 *
 * Calls the Moonshot / Kimi API (OpenAI-compatible) to turn a plain-language
 * job description into a complete robot design that uses only real part ids
 * from lib/build/catalog. The result is run through sanitize() so unknown ids
 * and malformed requirements are dropped before it reaches the studio.
 *
 * The API key is server-side only (MOONSHOT_API_KEY) — it is never exposed to
 * the browser. TODO: re-verify the caller's Supabase session server-side before
 * spending Kimi credits; the /build page is already gated client-side by
 * ConsoleGate, but this route should re-check auth once a server-side Supabase
 * client is wired up.
 */

import { NextResponse } from "next/server";

import { PARTS, ENVIRONMENTS, CATEGORIES, type Part } from "@/lib/build/catalog";
import { sanitize, type Design, type Requirements } from "@/lib/build/design";

export const runtime = "nodejs";

const MOONSHOT_URL = "https://api.moonshot.ai/v1/chat/completions";
const DEFAULT_MODEL = "moonshot-v1-128k";
const MAX_PROMPT = 2000;

interface GenerateBody {
  prompt?: string;
  requirements?: Partial<Requirements>;
}

export async function POST(req: Request) {
  const apiKey = process.env.MOONSHOT_API_KEY;
  if (!apiKey) {
    return NextResponse.json(
      { error: "Moonshot API key not configured. Set MOONSHOT_API_KEY in the website environment." },
      { status: 500 },
    );
  }

  let body: GenerateBody;
  try {
    body = (await req.json()) as GenerateBody;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const prompt = (body?.prompt ?? "").trim();
  if (!prompt) return NextResponse.json({ error: "A prompt is required." }, { status: 400 });
  if (prompt.length > MAX_PROMPT) {
    return NextResponse.json({ error: `Prompt is too long (max ${MAX_PROMPT} chars).` }, { status: 400 });
  }

  const model = process.env.MOONSHOT_MODEL || DEFAULT_MODEL;

  const payload: Record<string, unknown> = {
    model,
    messages: [
      { role: "system", content: systemPrompt(body.requirements) },
      { role: "user", content: prompt },
    ],
    response_format: { type: "json_object" },
    max_completion_tokens: 1200,
  };
  // temperature is only a documented field for the moonshot-v1 family; the
  // kimi-k2.* models ignore/forbid it.
  if (model.startsWith("moonshot-v1")) payload.temperature = 0.3;
  // kimi-k2.5/2.6 accept an explicit thinking toggle — disable it for fast
  // structured output. kimi-k2.7-code and moonshot-v1-* don't take this field.
  if (model === "kimi-k2.5" || model === "kimi-k2.6") payload.thinking = { type: "disabled" };

  let upstream: Response;
  try {
    upstream = await fetch(MOONSHOT_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${apiKey}` },
      body: JSON.stringify(payload),
    });
  } catch {
    return NextResponse.json({ error: "Could not reach the Moonshot API." }, { status: 502 });
  }

  if (!upstream.ok) {
    const text = await upstream.text().catch(() => "");
    return NextResponse.json(
      { error: `Moonshot API error (${upstream.status}).`, detail: text.slice(0, 500) },
      { status: 502 },
    );
  }

  const data = await upstream.json().catch(() => null);
  const content: string | undefined = data?.choices?.[0]?.message?.content;
  if (!content) {
    return NextResponse.json({ error: "The model returned an empty response." }, { status: 502 });
  }

  const parsed = parseDesign(content);
  if (!parsed) {
    return NextResponse.json({ error: "The model did not return a valid design." }, { status: 502 });
  }

  return NextResponse.json({ design: sanitize(parsed) });
}

/** Pull a JSON object out of the model output, tolerating code fences / prose. */
function parseDesign(content: string): Design | null {
  let text = content.trim();
  if (text.startsWith("```")) {
    text = text.replace(/^```[a-zA-Z]*\n?/, "").replace(/```$/, "").trim();
  }
  try {
    return JSON.parse(text) as Design;
  } catch {
    const start = text.indexOf("{");
    const end = text.lastIndexOf("}");
    if (start >= 0 && end > start) {
      try {
        return JSON.parse(text.slice(start, end + 1)) as Design;
      } catch {
        return null;
      }
    }
    return null;
  }
}

function systemPrompt(reqHint?: Partial<Requirements>): string {
  const catalog = PARTS.map(partLine).join("\n");
  const cats = CATEGORIES.map(
    (c) => `${c.key}: ${c.single ? "single (exactly 1)" : "multi (0 or more)"}${c.required ? ", required" : ""}`,
  ).join("\n");
  const envs = ENVIRONMENTS.map((e) => e.key).join(", ");
  const hint = reqHint
    ? `\nThe user has also set these requirement hints (respect them unless the prompt contradicts): ${JSON.stringify(reqHint)}`
    : "";
  return [
    "You are OhhO Build, an expert robot design assistant.",
    "Given a plain-language description of a robot job, choose real parts from the catalog below and set target requirements, returning a complete robot design as JSON.",
    "",
    "CATALOG (id | category | name | brand | key specs):",
    catalog,
    "",
    "CATEGORIES:",
    cats,
    "",
    `ENVIRONMENTS (pick one): ${envs}`,
    "",
    "RULES:",
    "- Pick part ids ONLY from the catalog above.",
    "- For single categories (base, drive, power, compute) choose exactly one id; arm and gripper are optional but a gripper requires an arm.",
    "- sensor is multi-select: pick 0 or more (cameras, lidar, imu) that fit the job.",
    "- Set requirements (payload kg, reach m, topSpeed m/s, runtime h, footprint m, environment) to match the described job.",
    "- Give the design a short, descriptive name.",
    "",
    "Return ONLY a JSON object with this exact shape (no prose, no code fence):",
    "{",
    '  "name": string,',
    '  "selection": { "base": [id], "drive": [id], "power": [id], "compute": [id], "arm": [id]?, "gripper": [id]?, "sensor": [id, ...] },',
    '  "requirements": { "payload": number, "reach": number, "topSpeed": number, "runtime": number, "footprint": number, "environment": string }',
    "}",
    hint,
  ].join("\n");
}

function partLine(p: Part): string {
  const specs: string[] = [`${p.mass}kg`, `$${p.price}`];
  if (p.payload != null) specs.push(`${p.payload}kg payload`);
  if (p.reach != null) specs.push(`${p.reach}m reach`);
  if (p.topSpeed != null) specs.push(`${p.topSpeed}m/s`);
  if (p.capacityWh != null) specs.push(`${p.capacityWh}Wh`);
  if (p.tops != null && p.tops > 0) specs.push(`${p.tops}TOPS`);
  if (p.dof != null) specs.push(`${p.dof}dof`);
  if (p.range != null && p.range > 0) specs.push(`${p.range}m range`);
  specs.push(`env:${p.env.join("/")}`);
  return `${p.id} | ${p.category} | ${p.name} | ${p.brand} | ${specs.join(", ")}`;
}
