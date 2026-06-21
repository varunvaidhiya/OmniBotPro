/*
 * Frame MCP tools — ROS 2 process management and system telemetry.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult,
} from "@/lib/mcp/types";

// Static process data (the live process manager runs in the browser).
const PROCESSES = [
  { id: "p1", name: "omnivla_node", status: "running", cpu: 45.2, ram: 2048, uptime: "4h 12m" },
  { id: "p2", name: "nav2_stack", status: "running", cpu: 12.4, ram: 512, uptime: "4h 12m" },
  { id: "p3", name: "yahboom_driver", status: "running", cpu: 3.1, ram: 128, uptime: "4h 12m" },
  { id: "p4", name: "rosbridge_ws", status: "running", cpu: 2.0, ram: 256, uptime: "4h 12m" },
  { id: "p5", name: "bev_stitcher", status: "running", cpu: 18.5, ram: 1024, uptime: "4h 12m" },
  { id: "p6", name: "slam_toolbox", status: "stopped", cpu: 0, ram: 0, uptime: "-" },
  { id: "p7", name: "teleop_recorder", status: "failed", cpu: 0, ram: 0, uptime: "-" },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "frame.listProcesses",
    description: "List all ROS 2 processes in the Frame workspace (omnivla_node, nav2_stack, yahboom_driver, rosbridge_ws, bev_stitcher, slam_toolbox, teleop_recorder) with status, CPU, RAM, and uptime.",
    inputSchema: s({
      status: { type: "string", description: "Filter by status", enum: ["running", "stopped", "failed"] },
    }),
    handler: async (p) => p.status ? jsonResult(PROCESSES.filter((proc) => proc.status === p.status)) : jsonResult(PROCESSES),
    product: "frame", readOnly: true,
  },
  {
    name: "frame.getProcess",
    description: "Get a specific ROS 2 process by ID with full status, resource usage, and uptime.",
    inputSchema: s(
      { processId: { type: "string", description: "Process ID (e.g. 'p1', 'p2', 'p3')" } },
      ["processId"],
    ),
    handler: async (p) => jsonResult(PROCESSES.find((proc) => proc.id === p.processId) ?? { error: "not found" }),
    product: "frame", readOnly: true,
  },
  {
    name: "frame.getSystemStatus",
    description: "Get the overall Frame workspace status: total processes, running count, stopped count, failed count, and total CPU/RAM usage.",
    inputSchema: s({}),
    handler: async () => {
      const running = PROCESSES.filter((p) => p.status === "running");
      return jsonResult({
        total: PROCESSES.length,
        running: running.length,
        stopped: PROCESSES.filter((p) => p.status === "stopped").length,
        failed: PROCESSES.filter((p) => p.status === "failed").length,
        totalCpu: Math.round(running.reduce((a, p) => a + p.cpu, 0) * 10) / 10,
        totalRamMb: running.reduce((a, p) => a + p.ram, 0),
      });
    },
    product: "frame", readOnly: true,
  },
];
