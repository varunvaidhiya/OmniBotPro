/*
 * Mind MCP tools — the deliberative agent brain.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult,
} from "@/lib/mcp/types";

const LOOP_PHASES = ["perceive", "reason", "verify", "act", "monitor", "reflect", "remember"];

const BACKENDS = [
  { id: "cloud", label: "Cloud · Claude", desc: "Cloud-class reasoning via Claude", available: true },
  { id: "on_device", label: "On-device LLM", desc: "Local LLM (Ollama/llama.cpp)", available: true },
  { id: "deepx", label: "DeepX NPU", desc: "Hardware NPU accelerator", available: false },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "mind.getLoopPhases",
    description: "List the 7 phases of the agent loop: perceive → reason → verify → act → monitor → reflect → remember.",
    inputSchema: s({}),
    handler: async () => jsonResult(LOOP_PHASES.map((p, i) => ({ index: i, phase: p }))),
    product: "mind", readOnly: true,
  },
  {
    name: "mind.getStatus",
    description: "Get the current agent loop status: current phase, goal, world state, reasoning backend, safety gate state, and episode count.",
    inputSchema: s({}),
    handler: async () => jsonResult({
      phase: "reason",
      goal: "find the red cup and bring it back",
      worldState: { basePose: "1.8, 0.4, 0°", arm: "home · open", nearest: "red_cup 0.42 m", missionPhase: "active" },
      activeBackend: "cloud",
      safetyGate: "verified",
      episodes: 128,
      memory: 2,
      online: true,
    }),
    product: "mind", readOnly: true,
  },
  {
    name: "mind.listBackends",
    description: "List the available reasoning backends (cloud Claude, on-device LLM, DeepX NPU) with availability and descriptions.",
    inputSchema: s({}),
    handler: async () => jsonResult(BACKENDS),
    product: "mind", readOnly: true,
  },
  {
    name: "mind.submitGoal",
    description: "Submit a natural-language goal to the agent brain. The agent will run the perceive→reason→verify→act→monitor→reflect→remember loop until the goal is met.",
    inputSchema: s(
      { goal: { type: "string", description: "Natural-language goal, e.g. 'find the red cup and bring it back'" } },
      ["goal"],
    ),
    handler: async (p) => jsonResult({
      accepted: true,
      goal: p.goal,
      loopStarted: true,
      message: `Goal accepted: "${p.goal}". The agent loop is running. The agent will reason, verify, act, and reflect until the goal is met.`,
    }),
    product: "mind", readOnly: false,
  },
  {
    name: "mind.getMemory",
    description: "Get the agent's stored memories (objects, places, and past attempt outcomes).",
    inputSchema: s({}),
    handler: async () => jsonResult([
      { type: "object", key: "red_cup", value: "located in kitchen, 0.42 m from base", lastSeen: "2 min ago" },
      { type: "place", key: "bench", value: "x=3.2, y=1.1, yaw=0", lastVisited: "5 min ago" },
    ]),
    product: "mind", readOnly: true,
  },
];
