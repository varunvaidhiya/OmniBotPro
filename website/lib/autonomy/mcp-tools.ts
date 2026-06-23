/*
 * Autonomy MCP tools — mission planning, navigation, and named locations.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult,
} from "@/lib/mcp/types";

const NAMED_LOCATIONS = ["kitchen", "bench", "dock", "shelf-3", "lab"];

const MISSION_STEPS = [
  { id: "s1", label: "navigate · kitchen", kind: "navigate", status: "done" },
  { id: "s2", label: "detect · red cup", kind: "perceive", status: "running" },
  { id: "s3", label: "pick · cup", kind: "manipulate", status: "queued" },
  { id: "s4", label: "navigate · bench", kind: "navigate", status: "queued" },
];

const CONTROL_MODES = [
  { id: "nav2", label: "Nav2", desc: "Autonomous navigation" },
  { id: "vla", label: "VLA", desc: "Vision-Language-Action policy" },
  { id: "rl_nav", label: "RL Nav", desc: "Reinforcement learning navigation" },
  { id: "teleop", label: "Teleop", desc: "Human teleoperation" },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "autonomy.listLocations",
    description: "List all named locations the mission planner and NL agent can navigate to (kitchen, bench, dock, shelf-3, lab).",
    inputSchema: s({}),
    handler: async () => jsonResult(NAMED_LOCATIONS.map((l) => ({ name: l }))),
    product: "autonomy", readOnly: true,
  },
  {
    name: "autonomy.getMission",
    description: "Get the current mission state: steps, phase (idle/running/done), active control mode, and progress.",
    inputSchema: s({}),
    handler: async () => jsonResult({
      steps: MISSION_STEPS,
      phase: "running",
      mode: "nav2",
      progress: 0.5,
      distToGoal: "2.4 m",
      eta: "9 s",
      instruction: "take the red cup to the bench",
    }),
    product: "autonomy", readOnly: true,
  },
  {
    name: "autonomy.listControlModes",
    description: "List the available control modes the autonomy mux can switch between (nav2, vla, rl_nav, teleop).",
    inputSchema: s({}),
    handler: async () => jsonResult(CONTROL_MODES),
    product: "autonomy", readOnly: true,
  },
  {
    name: "autonomy.submitMission",
    description: "Submit a natural-language mission instruction to the autonomy agent. The agent parses it into a sequence of navigate/perceive/manipulate steps.",
    inputSchema: s(
      { instruction: { type: "string", description: "Natural-language instruction, e.g. 'take the red cup to the bench'" } },
      ["instruction"],
    ),
    handler: async (p) => jsonResult({
      accepted: true,
      instruction: p.instruction,
      parsedSteps: [
        { kind: "navigate", target: "kitchen" },
        { kind: "perceive", target: "red cup" },
        { kind: "manipulate", action: "pick up" },
        { kind: "navigate", target: "bench" },
      ],
      message: `Mission accepted: "${p.instruction}". The agent will plan and execute the steps.`,
    }),
    product: "autonomy", readOnly: false,
  },
  {
    name: "autonomy.setMode",
    description: "Set the active control mode for the robot (nav2, vla, rl_nav, or teleop). This switches the cmd_vel mux.",
    inputSchema: s(
      { mode: { type: "string", description: "Control mode to activate", enum: ["nav2", "vla", "rl_nav", "teleop"] } },
      ["mode"],
    ),
    handler: async (p) => jsonResult({ mode: p.mode, active: true, message: `Control mode set to '${p.mode}'.` }),
    product: "autonomy", readOnly: false,
  },
  {
    name: "autonomy.navigateTo",
    description: "Send the robot to a named location using Nav2. Use autonomy.listLocations to see valid names.",
    inputSchema: s({ location: { type: "string", description: "Named location, e.g. 'kitchen'" } }, ["location"]),
    handler: async (p) => {
      const known = NAMED_LOCATIONS.includes(String(p.location));
      return jsonResult({
        location: p.location,
        accepted: known,
        mode: "nav2",
        message: known
          ? `Navigating to '${p.location}'.`
          : `'${p.location}' is not a known location. Known: ${NAMED_LOCATIONS.join(", ")}.`,
      });
    },
    product: "autonomy", readOnly: false,
  },
  {
    name: "autonomy.cancelMission",
    description: "Cancel the currently running mission and stop the robot. The autonomy state returns to idle.",
    inputSchema: s({}),
    handler: async () => jsonResult({ cancelled: true, phase: "idle", message: "Mission cancelled and the robot has been stopped." }),
    product: "autonomy", readOnly: false,
  },
  {
    name: "autonomy.addLocation",
    description: "Save a new named location (the robot's current pose, or an explicit x/y/yaw) so future missions can navigate to it by name.",
    inputSchema: s({
      name: { type: "string", description: "Name for the location, e.g. 'charging-dock'" },
      x: { type: "number", description: "X in metres (optional; defaults to current pose)" },
      y: { type: "number", description: "Y in metres (optional)" },
      yaw: { type: "number", description: "Heading in radians (optional)" },
    }, ["name"]),
    handler: async (p) => jsonResult({
      name: p.name,
      x: typeof p.x === "number" ? p.x : 0,
      y: typeof p.y === "number" ? p.y : 0,
      yaw: typeof p.yaw === "number" ? p.yaw : 0,
      saved: true,
      message: `Location '${p.name}' saved to the map.`,
    }),
    product: "autonomy", readOnly: false,
  },
];
