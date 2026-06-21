/*
 * Fleet MCP tools — fleet management, OTA, and observability.
 *
 * Tools:
 *   fleet.listRobots    — list all fleet robots with status and version
 *   fleet.getHealth     — get fleet health summary (online/degraded/offline counts)
 *   fleet.getAlerts     — get live alerts feed
 *   fleet.getVersions   — get current stable and canary version info
 *   fleet.triggerOTA    — trigger a canary OTA rollout
 *   fleet.rollbackOTA   — rollback canary robots to stable
 */

import {
  type ToolDefinition,
  jsonResult,
  type InputSchema,
  type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { generateInitialFleet, INITIAL_ALERTS, VERSIONS, type RobotStatus } from "@/lib/fleet/fleet";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "fleet.listRobots",
    description: "List all robots in the fleet with their status (online/degraded/offline), battery, software version, and position.",
    inputSchema: s({
      status: { type: "string", description: "Filter by status", enum: ["online", "degraded", "offline"] },
    }),
    handler: async (params) => {
      const robots = generateInitialFleet(24);
      let result = robots;
      if (params.status) result = result.filter((r) => r.status === params.status);
      return jsonResult(result);
    },
    product: "fleet",
    readOnly: true,
  },
  {
    name: "fleet.getHealth",
    description: "Get fleet health summary: total robots, online count, degraded count, offline count, and healthy percentage.",
    inputSchema: s({}),
    handler: async () => {
      const robots = generateInitialFleet(24);
      const total = robots.length;
      const online = robots.filter((r) => r.status === "online").length;
      const degraded = robots.filter((r) => r.status === "degraded").length;
      const offline = robots.filter((r) => r.status === "offline").length;
      const canary = robots.filter((r) => r.version === VERSIONS.CANARY).length;
      return jsonResult({ total, online, degraded, offline, canary, healthyPct: Math.round((online / total) * 100) });
    },
    product: "fleet",
    readOnly: true,
  },
  {
    name: "fleet.getAlerts",
    description: "Get the live alerts feed with severity (critical/warning/info), title, description, and timestamp.",
    inputSchema: s({}),
    handler: async () => jsonResult(INITIAL_ALERTS),
    product: "fleet",
    readOnly: true,
  },
  {
    name: "fleet.getVersions",
    description: "Get current software version info: stable version, canary version, and rollout percentage.",
    inputSchema: s({}),
    handler: async () => {
      const robots = generateInitialFleet(24);
      const canaryCount = robots.filter((r) => r.version === VERSIONS.CANARY).length;
      return jsonResult({
        stable: VERSIONS.STABLE,
        canary: VERSIONS.CANARY,
        canaryPct: Math.round((canaryCount / robots.length) * 100),
        totalRobots: robots.length,
      });
    },
    product: "fleet",
    readOnly: true,
  },
  {
    name: "fleet.triggerOTA",
    description: "Trigger a canary OTA rollout to push the new software version to healthy robots. Returns a confirmation with the target version.",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult({
        triggered: true,
        targetVersion: VERSIONS.CANARY,
        message: `OTA rollout initiated. Pushing ${VERSIONS.CANARY} to healthy robots with staged canary rollout.`,
      });
    },
    product: "fleet",
    readOnly: false,
  },
  {
    name: "fleet.rollbackOTA",
    description: "Rollback all canary robots to the stable version.",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult({
        triggered: true,
        targetVersion: VERSIONS.STABLE,
        message: `Rollback initiated. Reverting all canary robots to ${VERSIONS.STABLE}.`,
      });
    },
    product: "fleet",
    readOnly: false,
  },
];
