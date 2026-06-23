/*
 * Mind MCP tools — the deliberative agent brain.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
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
  {
    name: "mind.setBackend",
    description: "Switch the agent's reasoning backend (cloud Claude, on-device LLM, or DeepX NPU). Fails if the backend is unavailable on this deployment.",
    inputSchema: s({ backendId: { type: "string", description: "Backend id", enum: ["cloud", "on_device", "deepx"] } }, ["backendId"]),
    handler: async (p) => {
      const backend = BACKENDS.find((b) => b.id === p.backendId);
      if (!backend) return errorResult(`Unknown backend: ${p.backendId}`);
      if (!backend.available) return errorResult(`Backend '${backend.label}' is not available on this deployment.`);
      return jsonResult({ activeBackend: backend.id, label: backend.label, message: `Reasoning backend set to ${backend.label}.` });
    },
    product: "mind", readOnly: false,
  },
  {
    name: "mind.stopLoop",
    description: "Stop the agent loop and clear the current goal. The agent returns to idle.",
    inputSchema: s({}),
    handler: async () => jsonResult({ stopped: true, phase: "idle", message: "Agent loop stopped and goal cleared." }),
    product: "mind", readOnly: false,
  },
  {
    name: "mind.respondToHuman",
    description: "Provide a human response when the agent is waiting for clarification (the WAIT_HUMAN interrupt). Resumes the loop with the answer.",
    inputSchema: s({ response: { type: "string", description: "Your answer to the agent's question" } }, ["response"]),
    handler: async (p) => jsonResult({ delivered: true, response: p.response, message: "Response delivered to the agent; the loop will resume." }),
    product: "mind", readOnly: false,
  },
  {
    name: "mind.addMemory",
    description: "Store a memory for the agent (an object, place, or fact) so it can be recalled in future reasoning.",
    inputSchema: s({
      type: { type: "string", description: "Memory type", enum: ["object", "place", "fact"] },
      key: { type: "string", description: "Short key, e.g. 'red_cup' or 'charging-dock'" },
      value: { type: "string", description: "The remembered detail" },
    }, ["type", "key", "value"]),
    handler: async (p) => jsonResult({ stored: true, type: p.type, key: p.key, value: p.value, message: `Remembered ${p.type} '${p.key}'.` }),
    product: "mind", readOnly: false,
  },
];
