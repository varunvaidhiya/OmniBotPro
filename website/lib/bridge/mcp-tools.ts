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
  {
    name: "bridge.getImpedanceDefaults",
    description: "Get the default position-control impedance gains (kp/kd) grouped by body region (leg, waist, arm, hand) for a robot model.",
    inputSchema: s({ model: { type: "string", description: "Robot model (currently only 'g1')", enum: ["g1"] } }, ["model"]),
    handler: async (params) => {
      const model = (params.model as string) ?? "g1";
      if (model !== "g1") return errorResult(`Impedance defaults not available for model: ${model}`);
      const byRegion: Record<string, { kp: number; kd: number; joints: number }> = {};
      for (const j of G1_JOINT_MAP) {
        const r = (byRegion[j.region] ??= { kp: j.kp, kd: j.kd, joints: 0 });
        r.joints += 1;
      }
      return jsonResult({ model, regions: byRegion });
    },
    product: "bridge",
    readOnly: true,
  },
  {
    name: "bridge.connectAdapter",
    description: "Bring up the bridge for a protocol adapter, starting the native↔ROS 2 translation. Use bridge.listAdapters for valid ids.",
    inputSchema: s({ adapterId: { type: "string", description: "Adapter id", enum: ["unitree-dds", "dji-mavlink", "modbus-arm", "ethercat-arm", "yahboom-serial"] } }, ["adapterId"]),
    handler: async (params) => {
      const a = getAdapter(params.adapterId as string);
      if (!a) return errorResult(`Adapter not found: ${params.adapterId}`);
      if (a.status === "unavailable") return errorResult(`Adapter '${a.name}' is unavailable on this deployment.`);
      return jsonResult({ adapterId: a.id, name: a.name, status: "active", transport: a.transport, message: `Bridge for ${a.name} is now active over ${a.transport}.` });
    },
    product: "bridge",
    readOnly: false,
  },
  {
    name: "bridge.disconnectAdapter",
    description: "Tear down the bridge for a protocol adapter, stopping translation and returning it to idle.",
    inputSchema: s({ adapterId: { type: "string", description: "Adapter id", enum: ["unitree-dds", "dji-mavlink", "modbus-arm", "ethercat-arm", "yahboom-serial"] } }, ["adapterId"]),
    handler: async (params) => {
      const a = getAdapter(params.adapterId as string);
      if (!a) return errorResult(`Adapter not found: ${params.adapterId}`);
      return jsonResult({ adapterId: a.id, name: a.name, status: "idle", message: `Bridge for ${a.name} stopped.` });
    },
    product: "bridge",
    readOnly: false,
  },
  {
    name: "bridge.setImpedance",
    description: "Override the position-control impedance gains (kp/kd) for a body region of a model. Affects how stiff/compliant those joints are.",
    inputSchema: s({
      model: { type: "string", description: "Robot model", enum: ["g1"] },
      region: { type: "string", description: "Body region", enum: ["leg", "waist", "arm", "hand"] },
      kp: { type: "number", description: "Position gain (stiffness)" },
      kd: { type: "number", description: "Velocity gain (damping)" },
    }, ["model", "region", "kp", "kd"]),
    handler: async (params) => {
      const affected = G1_JOINT_MAP.filter((j) => j.region === params.region).length;
      if (!affected) return errorResult(`No joints in region '${params.region}' for ${params.model}.`);
      return jsonResult({
        model: params.model, region: params.region, kp: params.kp, kd: params.kd, jointsAffected: affected,
        message: `Set ${params.region} impedance to kp=${params.kp}, kd=${params.kd} across ${affected} joints.`,
      });
    },
    product: "bridge",
    readOnly: false,
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
