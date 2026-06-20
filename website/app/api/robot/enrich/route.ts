/*
 * Robot enrichment — POST /api/robot/enrich
 *   body: { config: RobotConfig }   (the deterministic baseline)
 *   200:  { enrichment: RobotEnrichment }
 *
 * Asks Kimi for richer, robot-specific knowledge (an accurate summary, factual
 * notes, refined compute, recommended models / tasks) to augment the baseline
 * config the consoles already render. Informational only — the structured
 * config (DOF, joints, sensors) stays deterministic; this never overrides it.
 *
 * The Kimi key is server-side only (lib/ai/kimi). Output is run through
 * sanitizeEnrichment() before returning. NOTE: like the Build route, this
 * should re-verify the Supabase session server-side before spending credits
 * once a server Supabase client is wired up.
 */

import { NextResponse } from "next/server";

import { callKimi, parseJsonObject } from "@/lib/ai/kimi";
import { sanitizeEnrichment } from "@/lib/ai/types";
import type { RobotConfig } from "@/lib/garage/robot-config";

export const runtime = "nodejs";

interface Body {
  config?: Partial<RobotConfig>;
}

export async function POST(req: Request) {
  let body: Body;
  try {
    body = (await req.json()) as Body;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const c = body?.config;
  if (!c || !c.name) {
    return NextResponse.json({ error: "A robot config with a name is required." }, { status: 400 });
  }

  const result = await callKimi({
    system: SYSTEM,
    user: describeRobot(c),
    maxTokens: 900,
  });
  if (!result.ok) {
    return NextResponse.json({ error: result.error, detail: result.detail }, { status: result.status ?? 502 });
  }

  const parsed = parseJsonObject(result.content!);
  const enrichment = sanitizeEnrichment(parsed);
  if (!enrichment) {
    return NextResponse.json({ error: "The model did not return valid enrichment." }, { status: 502 });
  }

  return NextResponse.json({ enrichment });
}

const SYSTEM = [
  "You are OhhO's robotics knowledge engine. Given a robot's known specs, return accurate, up-to-date facts about that specific robot to enrich an operator console.",
  "Be precise and factual. If you are unsure of an exact number, describe it qualitatively rather than inventing a figure.",
  "",
  "Return ONLY a JSON object with this exact shape (no prose, no code fence):",
  "{",
  '  "summary": string,                       // one accurate sentence about the robot',
  '  "notes": [string, ...],                  // up to 8 concrete factual bullets: SDK, ROS support, real sensor models, typical deployments',
  '  "compute": { "brain": string, "accelerator"?: string, "vramGb"?: number },',
  '  "recommendedModels": [string, ...],      // AI/VLA or control models suited to this robot (e.g. SmolVLA, ACT, OpenVLA, PX4 offboard)',
  '  "suggestedTasks": [string, ...]          // example jobs this robot is good at',
  "}",
].join("\n");

function describeRobot(c: Partial<RobotConfig>): string {
  const lines = [
    `Robot: ${c.name}${c.manufacturer ? ` by ${c.manufacturer}` : ""}`,
    c.category ? `Category: ${c.category}` : "",
    c.driveLabel ? `Locomotion: ${c.driveLabel} (${c.baseDof ?? "?"} base DOF)` : "",
    c.hasArm ? `Manipulator: ${c.armDof}-DOF arm` : "No manipulator",
    c.totalDof != null ? `Total DOF: ${c.totalDof}` : "",
    c.payloadKg ? `Payload: ${c.payloadKg} kg` : "",
    c.compute?.brain ? `Compute: ${c.compute.brain}` : "",
    c.ros ? `ROS: ${c.ros}` : "",
    Array.isArray(c.sensors) && c.sensors.length
      ? `Sensors: ${c.sensors.map((s) => s.label).join(", ")}`
      : "",
  ].filter(Boolean);
  return lines.join("\n");
}
