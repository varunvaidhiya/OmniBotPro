/*
 * Twin MCP tools — digital twin state and predictions.
 *
 * Tools:
 *   twin.getState       — get the current twin state (pose, motor temps, battery)
 *   twin.getPredictions — get degradation predictions
 *   twin.getWhatIf      — get what-if simulation results
 *   twin.getSyncMetrics — get sim↔real sync metrics
 */

import {
  type ToolDefinition,
  jsonResult,
  type InputSchema,
  type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { PREDICTIONS, WHATIF_RESULTS, TWIN_STATS } from "@/lib/twin/twin-data";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "twin.getState",
    description: "Get the current digital twin state: robot pose (x, y, theta), velocities, motor temperatures, and battery level.",
    inputSchema: s({}),
    handler: async () => {
      // Return a representative snapshot — the live twin runs in the browser.
      return jsonResult({
        x: 120, y: 70, theta: 0, vx: 0.3, vy: 0, vtheta: 0.02,
        motorTemps: [48, 52, 45, 50, 47, 49],
        battery: 0.82,
        note: "Live twin state is browser-side. This is a representative snapshot. The MCP returns the same data model.",
      });
    },
    product: "twin",
    readOnly: true,
  },
  {
    name: "twin.getPredictions",
    description: "Get degradation predictions for the robot: motor temperature trends, battery depletion, and joint wear with estimated time to threshold.",
    inputSchema: s({}),
    handler: async () => jsonResult(PREDICTIONS),
    product: "twin",
    readOnly: true,
  },
  {
    name: "twin.getWhatIf",
    description: "Get what-if simulation results — alternative outcomes from branching at recorded states with different policies or parameters.",
    inputSchema: s({}),
    handler: async () => jsonResult(WHATIF_RESULTS),
    product: "twin",
    readOnly: true,
  },
  {
    name: "twin.getSyncMetrics",
    description: "Get sim↔real sync metrics: match score, drift, and replay storage.",
    inputSchema: s({}),
    handler: async () => jsonResult(TWIN_STATS),
    product: "twin",
    readOnly: true,
  },
];
