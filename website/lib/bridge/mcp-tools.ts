/*
 * Bridge MCP tools — protocol adapter catalog and joint maps.
 *
 * Tools:
 *   bridge.listAdapters        — list all protocol adapters
 *   bridge.getAdapter          — get adapter detail by ID
 *   bridge.getJointMap         — get the joint-index map for a robot model
 *   bridge.getTopics           — get ROS 2 topic bridge definitions
 *   bridge.getImpedanceDefaults — get default kp/kd by body region
 *
 * Resources:
 *   ohho://bridge/adapters     — all adapters as JSON
 *   ohho://bridge/jointmap/g1  — G1 joint map as JSON
 */

import {
  type ToolDefinition,
  type ResourceDefinition,
  jsonResult,
  errorResult,
  type InputSchema,
  type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { ADAPTERS, getAdapter, G1_JOINT_MAP, UNITREE_TOPICS } from "@/lib/bridge/adapters";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "bridge.listAdapters",
    description: "List all protocol adapters (Unitree DDS, DJI MAVLink, Modbus, EtherCAT, Yahboom serial) with status, supported models, SDK, and transport info.",
    inputSchema: s({}),
    handler: async () => jsonResult(ADAPTERS),
    product: "bridge",
    readOnly: true,
  },
  {
    name: "bridge.getAdapter",
    description: "Get full detail for a specific protocol adapter by ID.",
    inputSchema: s(
      { adapterId: { type: "string", description: "Adapter ID: unitree-dds, dji-mavlink, modbus-arm, ethercat-arm, yahboom-serial", enum: ["unitree-dds", "dji-mavlink", "modbus-arm", "ethercat-arm", "yahboom-serial"] } },
      ["adapterId"],
    ),
    handler: async (params) => {
      const a = getAdapter(params.adapterId as string);
      if (!a) return errorResult(`Adapter not found: ${params.adapterId}`);
      return jsonResult(a);
    },
    product: "bridge",
    readOnly: true,
  },
  {
    name: "bridge.getJointMap",
    description: "Get the joint-index map for a Unitree robot model. Maps native DDS motor indices to ROS 2 joint names with default impedance gains (kp/kd). Currently supports G1 (27 joints).",
    inputSchema: s(
      { model: { type: "string", description: "Robot model (currently only 'g1' supported)", enum: ["g1"] } },
      ["model"],
    ),
    handler: async (params) => {
      const model = (params.model as string) ?? "g1";
      if (model !== "g1") return errorResult(`Joint map not available for model: ${model}`);
      return jsonResult(G1_JOINT_MAP);
    },
    product: "bridge",
    readOnly: true,
  },
  {
    name: "bridge.getTopics",
    description: "Get the ROS 2 topic bridge definitions — which native DDS topics are translated to which ROS 2 topics, with direction and rate.",
    inputSchema: s({}),
    handler: async () => jsonResult(UNITREE_TOPICS),
    product: "bridge",
    readOnly: true,
  },
];

export const resources: ResourceDefinition[] = [
  {
    uri: "ohho://bridge/adapters",
    name: "All protocol adapters",
    description: "Complete adapter catalog with status, models, SDK, and transport details.",
    mimeType: "application/json",
    product: "bridge",
    read: async () => JSON.stringify(ADAPTERS, null, 2),
  },
  {
    uri: "ohho://bridge/jointmap/g1",
    name: "G1 joint-index map",
    description: "Unitree G1 27-DoF joint-index map: native DDS index → ROS 2 joint name + kp/kd defaults.",
    mimeType: "application/json",
    product: "bridge",
    read: async () => JSON.stringify(G1_JOINT_MAP, null, 2),
  },
];
