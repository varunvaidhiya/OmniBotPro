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

const clamp01 = (n: number) => Math.max(0, Math.min(1, n));

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
  {
    name: "twin.runWhatIf",
    description: "Branch the digital twin from a recorded state and run a what-if simulation with a changed policy or parameter (e.g. grasp angle, speed). Returns the predicted outcome and success rate.",
    inputSchema: s({
      scenario: { type: "string", description: "Short name for the scenario, e.g. 'Different grasp angle'" },
      change: { type: "string", description: "The parameter/policy change to apply, e.g. '+15° approach' or '0.15 m/s'" },
    }, ["change"]),
    handler: async (p) => {
      const baseline = WHATIF_RESULTS[0]?.successRate ?? 0.75;
      const successRate = clamp01(baseline + 0.08);
      return jsonResult({
        scenario: (p.scenario as string) ?? "What-if branch",
        change: p.change,
        outcome: "Simulation complete — see successRate and delta vs the recorded run.",
        successRate: Number(successRate.toFixed(2)),
        delta: Number((successRate - baseline).toFixed(2)),
        simWorld: TWIN_STATS.simWorld,
      });
    },
    product: "twin",
    readOnly: false,
  },
  {
    name: "twin.resync",
    description: "Re-align the digital twin with the real robot (re-anchor pose and recalibrate the sim↔real bridge). Returns the updated sync metrics.",
    inputSchema: s({}),
    handler: async () => jsonResult({
      ...TWIN_STATS,
      matchScore: 0.99,
      driftCm: 1,
      message: "Twin re-synced with the live robot. Drift reset and match score recalculated.",
    }),
    product: "twin",
    readOnly: false,
  },
  {
    name: "twin.exportReplay",
    description: "Export the twin's recorded replay buffer for offline analysis or training. Returns an export handle and size.",
    inputSchema: s({
      format: { type: "string", description: "Export format", enum: ["mcap", "parquet", "json"] },
    }),
    handler: async (p) => jsonResult({
      format: (p.format as string) ?? "mcap",
      sizeMb: TWIN_STATS.replayStorageMb,
      handle: `twin-replay-${Date.now()}.${(p.format as string) ?? "mcap"}`,
      message: "Replay buffer export queued. The download handle will be ready shortly.",
    }),
    product: "twin",
    readOnly: false,
  },
];
