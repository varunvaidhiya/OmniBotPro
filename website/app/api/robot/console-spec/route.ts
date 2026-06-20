/*
 * Dynamic console spec — POST /api/robot/console-spec
 *   body: { consoleId: string, config: RobotConfig }
 *   200:  { spec: ConsoleSpec }
 *
 * This is the "AI builds the console on demand" route. Given which product
 * console the user is in and the robot they selected, Kimi returns a ConsoleSpec
 * — a list of panels chosen from a FIXED vocabulary (metrics / status / sensors
 * / joints / cameras / actions / note) and filled with bounded label/value data.
 * The console-kit renderer maps those known kinds to known components, so the
 * controls adapt per robot without bespoke code, and a malicious/garbled model
 * response can never inject markup (sanitizeConsoleSpec drops anything else).
 *
 * Kimi key is server-side only. Should re-verify the Supabase session before
 * spending credits once a server Supabase client exists (see Build route TODO).
 */

import { NextResponse } from "next/server";

import { callKimi, parseJsonObject } from "@/lib/ai/kimi";
import { sanitizeConsoleSpec, PANEL_KINDS } from "@/lib/ai/types";
import type { RobotConfig } from "@/lib/garage/robot-config";

export const runtime = "nodejs";

const CONSOLE_PURPOSE: Record<string, string> = {
  view: "perception & sensor streaming (cameras, point clouds, lidar, TF)",
  data: "teleop demonstration recording & dataset review (episodes, state/action telemetry)",
  serve: "VLA model inference served as a REST API (latency, throughput, model backends)",
  pilot: "real-time teleoperation (base driving, arm control, camera feeds, e-stop)",
  autonomy: "SLAM mapping, Nav2 navigation & mission planning",
  mind: "the continuous agent loop (perceive→reason→act→reflect)",
  train: "policy training (BC / VLA / RL) and experiment tracking",
  frame: "the ROS 2 software foundation (nodes, topics, containers)",
  bench: "hardware bring-up, firmware flashing & calibration",
  fleet: "fleet management, OTA updates & observability",
  comply: "safety standards & compliance",
  proof: "test suites & verification",
  shield: "security & attack-surface hardening",
};

interface Body {
  consoleId?: string;
  config?: Partial<RobotConfig>;
}

export async function POST(req: Request) {
  let body: Body;
  try {
    body = (await req.json()) as Body;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const consoleId = (body?.consoleId ?? "").trim().toLowerCase();
  const c = body?.config;
  if (!consoleId || !c || !c.name) {
    return NextResponse.json({ error: "consoleId and a robot config are required." }, { status: 400 });
  }

  const result = await callKimi({
    system: systemPrompt(consoleId),
    user: describe(consoleId, c),
    maxTokens: 1100,
  });
  if (!result.ok) {
    return NextResponse.json({ error: result.error, detail: result.detail }, { status: result.status ?? 502 });
  }

  const spec = sanitizeConsoleSpec(parseJsonObject(result.content!));
  if (!spec) {
    return NextResponse.json({ error: "The model did not return a valid console spec." }, { status: 502 });
  }

  return NextResponse.json({ spec });
}

function systemPrompt(consoleId: string): string {
  const purpose = CONSOLE_PURPOSE[consoleId] ?? "an operator console";
  return [
    `You are OhhO's console designer. The user is in the "${consoleId}" console, which is for ${purpose}.`,
    "Design the robot-specific panels this console should show for the given robot. Tailor the panels to the robot's actual capabilities — e.g. an aerial drone needs flight/GPS panels and NO arm joints; a fixed industrial arm needs joint panels and NO base-velocity panel; a quadruped needs gait/leg panels.",
    "",
    `Each panel.kind MUST be one of: ${PANEL_KINDS.join(", ")}.`,
    "Use 'metrics'/'status'/'note' with items:[{label,value}]; 'actions' with actions:[label,...]; 'joints'/'cameras'/'sensors' to signal the renderer to draw those (items optional, used as captions).",
    "",
    "Return ONLY a JSON object (no prose, no code fence):",
    "{",
    '  "headline": string,                 // one line describing this console for THIS robot',
    '  "panels": [ { "kind": string, "title": string, "items"?: [{"label":string,"value"?:string}], "actions"?: [string] }, ... ]',
    "}",
    "Use at most 6 panels.",
  ].join("\n");
}

function describe(consoleId: string, c: Partial<RobotConfig>): string {
  return [
    `Console: ${consoleId}`,
    `Robot: ${c.name}${c.manufacturer ? ` by ${c.manufacturer}` : ""}`,
    c.category ? `Category: ${c.category}` : "",
    c.driveLabel ? `Locomotion: ${c.driveLabel} (${c.baseDof ?? 0} base DOF)` : "",
    c.hasArm ? `Arm: ${c.armDof}-DOF` : "No arm",
    `Total DOF: ${c.totalDof ?? "?"}`,
    c.capabilities
      ? `Capabilities: ${Object.entries(c.capabilities).filter(([, v]) => v).map(([k]) => k).join(", ")}`
      : "",
    Array.isArray(c.sensors) ? `Sensors: ${c.sensors.map((s) => `${s.label} (${s.kind})`).join(", ")}` : "",
    c.compute?.brain ? `Compute: ${c.compute.brain}${c.compute.accelerator ? ` + ${c.compute.accelerator}` : ""}` : "",
  ].filter(Boolean).join("\n");
}
