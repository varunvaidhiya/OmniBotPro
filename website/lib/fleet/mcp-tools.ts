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
  errorResult,
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
  {
    name: "fleet.getRobot",
    description: "Get a single fleet robot by id, with its status, battery, software version, and position.",
    inputSchema: s({ robotId: { type: "string", description: "Robot id, e.g. 'amr-07'" } }, ["robotId"]),
    handler: async (p) => {
      const robot = generateInitialFleet(24).find((r) => r.id === p.robotId);
      return robot ? jsonResult(robot) : errorResult(`Robot not found: ${p.robotId}`);
    },
    product: "fleet",
    readOnly: true,
  },
  {
    name: "fleet.acknowledgeAlert",
    description: "Acknowledge a fleet alert by id so it stops paging the on-call operator.",
    inputSchema: s({ alertId: { type: "string", description: "Alert id from fleet.getAlerts, e.g. 'a2'" } }, ["alertId"]),
    handler: async (p) => {
      const alert = INITIAL_ALERTS.find((a) => a.id === p.alertId);
      if (!alert) return errorResult(`Alert not found: ${p.alertId}`);
      return jsonResult({ alertId: alert.id, acknowledged: true, message: `Alert '${alert.id}' acknowledged.` });
    },
    product: "fleet",
    readOnly: false,
  },
  {
    name: "fleet.restartRobot",
    description: "Remotely restart a robot's software stack (rosbridge + nodes). Use for a degraded robot before escalating to maintenance.",
    inputSchema: s({ robotId: { type: "string", description: "Robot id to restart" } }, ["robotId"]),
    handler: async (p) => {
      const robot = generateInitialFleet(24).find((r) => r.id === p.robotId);
      if (!robot) return errorResult(`Robot not found: ${p.robotId}`);
      return jsonResult({ robotId: robot.id, restarting: true, message: `Restart command sent to ${robot.id}. It should rejoin the fleet shortly.` });
    },
    product: "fleet",
    readOnly: false,
  },
  {
    name: "fleet.setCanaryPercentage",
    description: "Set the canary rollout percentage — what fraction of healthy robots run the canary version during a staged OTA.",
    inputSchema: s({ percent: { type: "number", description: "Canary percentage, 0–100" } }, ["percent"]),
    handler: async (p) => {
      const pct = Math.max(0, Math.min(100, Number(p.percent)));
      return jsonResult({ canaryPct: pct, targetVersion: VERSIONS.CANARY, message: `Canary rollout set to ${pct}% on ${VERSIONS.CANARY}.` });
    },
    product: "fleet",
    readOnly: false,
  },
];
