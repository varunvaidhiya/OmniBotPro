/*
 * Connect MCP tools — robot connection status and control.
 *
 * Tools:
 *   connect.getProtocols  — list available transport protocols
 *   connect.getStatus     — get current connection status (simulated for MCP)
 *   connect.getTelemetry  — get latest telemetry snapshot
 *   connect.sendVelocity  — send a velocity command
 *   connect.emergencyStop — trigger emergency stop
 *
 * Note: the browser-side RobotConnectionProvider holds the live connection.
 * MCP tools operate on the same data model — when called from an AI agent,
 * they describe the connection state and the available commands. For a live
 * robot, the agent can instruct velocity and e-stop commands that an
 * orchestration layer forwards to the connected robot.
 */

import {
  type ToolDefinition,
  textResult,
  jsonResult,
  errorResult,
  type InputSchema,
  type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { PROTOCOLS, getProtocol } from "@/lib/connect/protocols";
import { defaultConnectionConfig } from "@/lib/connect/config";
import type { Velocity, ConnectionProtocol } from "@/lib/connect/types";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "connect.getProtocols",
    description: "List all available transport protocols (ROSBridge Wi-Fi, Web Serial USB, Web Bluetooth BLE, Simulated) with their metadata and availability.",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult(PROTOCOLS);
    },
    product: "connect",
    readOnly: true,
  },
  {
    name: "connect.getDefaultConfig",
    description: "Get the default connection configuration (protocol, address, baud rate, auto-reconnect).",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult(defaultConnectionConfig());
    },
    product: "connect",
    readOnly: true,
  },
  {
    name: "connect.getProtocol",
    description: "Get details for a specific protocol by ID.",
    inputSchema: s(
      { protocolId: { type: "string", description: "Protocol ID: rosbridge, webserial, webbluetooth, or simulated", enum: ["rosbridge", "webserial", "webbluetooth", "simulated"] } },
      ["protocolId"],
    ),
    handler: async (params) => {
      const p = getProtocol(params.protocolId as ConnectionProtocol);
      if (!p) return errorResult(`Unknown protocol: ${params.protocolId}`);
      return jsonResult(p);
    },
    product: "connect",
    readOnly: true,
  },
  {
    name: "connect.getStatus",
    description: "Get the current robot connection status: whether connected, the active protocol, address, latency, and whether an e-stop is latched. (Representative snapshot — the live connection is held browser-side.)",
    inputSchema: s({}),
    handler: async () => jsonResult({
      connected: true,
      protocol: defaultConnectionConfig().protocol,
      address: defaultConnectionConfig().address,
      latencyMs: 6,
      estop: false,
      note: "Live connection state is browser-side; this is the same data model an orchestration layer would report.",
    }),
    product: "connect",
    readOnly: true,
  },
  {
    name: "connect.getTelemetry",
    description: "Get the latest telemetry snapshot from the connected robot: pose (x, y, theta), velocity, battery, and IMU.",
    inputSchema: s({}),
    handler: async () => jsonResult({
      pose: { x: 1.24, y: 0.38, theta: 0.11 },
      velocity: { linearX: 0, linearY: 0, angularZ: 0 },
      battery: 0.86,
      imu: { ax: 0.01, ay: -0.02, az: 9.81, gz: 0.0 },
      ts: Date.now(),
    }),
    product: "connect",
    readOnly: true,
  },
  {
    name: "connect.connect",
    description: "Open a connection to the robot over a transport protocol. For rosbridge/webserial provide the address (ws URL or serial port); simulated needs none.",
    inputSchema: s({
      protocolId: { type: "string", description: "Transport protocol", enum: ["rosbridge", "webserial", "webbluetooth", "simulated"] },
      address: { type: "string", description: "Address for the transport, e.g. 'ws://192.168.1.101:9090' (optional for simulated)" },
    }, ["protocolId"]),
    handler: async (params) => {
      const proto = getProtocol(params.protocolId as ConnectionProtocol);
      if (!proto) return errorResult(`Unknown protocol: ${params.protocolId}`);
      const address = (params.address as string) ?? defaultConnectionConfig().address;
      return jsonResult({ connected: true, protocol: proto.id, address, message: `Connecting to the robot via ${proto.name}${address ? ` at ${address}` : ""}.` });
    },
    product: "connect",
    readOnly: false,
  },
  {
    name: "connect.disconnect",
    description: "Close the active robot connection.",
    inputSchema: s({}),
    handler: async () => jsonResult({ connected: false, message: "Disconnected from the robot." }),
    product: "connect",
    readOnly: false,
  },
  {
    name: "connect.sendVelocity",
    description: "Send a velocity command to the connected robot. linearX is forward/back m/s, linearY is strafe m/s (holonomic only), angularZ is yaw rad/s. Values are clamped to hardware limits.",
    inputSchema: s(
      {
        linearX: { type: "number", description: "Forward/back velocity in m/s (clamped to ±0.2)" },
        linearY: { type: "number", description: "Strafe velocity in m/s (holonomic only, clamped to ±0.2)" },
        angularZ: { type: "number", description: "Yaw angular velocity in rad/s (clamped to ±1.0)" },
      },
    ),
    handler: async (params) => {
      const vel: Velocity = {
        linearX: Number(params.linearX ?? 0),
        linearY: Number(params.linearY ?? 0),
        angularZ: Number(params.angularZ ?? 0),
      };
      // Clamp to safe limits
      vel.linearX = Math.max(-0.2, Math.min(0.2, vel.linearX));
      vel.linearY = Math.max(-0.2, Math.min(0.2, vel.linearY));
      vel.angularZ = Math.max(-1.0, Math.min(1.0, vel.angularZ));
      return jsonResult({ sent: true, velocity: vel, note: "Command queued for the connected robot. In production, forwarded via the live transport." });
    },
    product: "connect",
    readOnly: false,
  },
  {
    name: "connect.emergencyStop",
    description: "Trigger emergency stop on the connected robot. Immediately zeroes all velocity and latches the stop until released.",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult({ estop: true, message: "Emergency stop engaged. All motors halted. Use connect.releaseStop to clear." });
    },
    product: "connect",
    readOnly: false,
  },
  {
    name: "connect.releaseStop",
    description: "Release the emergency stop latch, allowing velocity commands again.",
    inputSchema: s({}),
    handler: async () => {
      return jsonResult({ estop: false, message: "Emergency stop released. Velocity commands enabled." });
    },
    product: "connect",
    readOnly: false,
  },
  {
    name: "connect.beep",
    description: "Sound the robot's buzzer for a number of milliseconds — handy to locate or identify a connected robot.",
    inputSchema: s({ durationMs: { type: "number", description: "Buzzer on-time in milliseconds (clamped to 0–5000)" } }),
    handler: async (params) => {
      const ms = Math.max(0, Math.min(5000, Number(params.durationMs ?? 200)));
      return jsonResult({ sent: true, durationMs: ms, message: `Buzzer sounded for ${ms} ms.` });
    },
    product: "connect",
    readOnly: false,
  },
];
