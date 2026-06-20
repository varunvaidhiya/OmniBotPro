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
import { callKimi, parseJsonObject } from "@/lib/ai/kimi";

export const runtime = "nodejs";

const MAX_PROMPT = 2000;

interface GenerateBody {
  prompt?: string;
  requirements?: Partial<Requirements>;
}

export async function POST(req: Request) {
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

  const result = await callKimi({
    system: systemPrompt(body.requirements),
    user: prompt,
    maxTokens: 1200,
  });
  if (!result.ok) {
    return NextResponse.json({ error: result.error, detail: result.detail }, { status: result.status ?? 502 });
  }

  const parsed = parseJsonObject<Design>(result.content!);
  if (!parsed) {
    return NextResponse.json({ error: "The model did not return a valid design." }, { status: 502 });
  }

  return NextResponse.json({ design: sanitize(parsed) });
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
