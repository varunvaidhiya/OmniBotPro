/*
 * View MCP tools — sensor layer management for the BEV perception viewer.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult,
} from "@/lib/mcp/types";

const LAYERS = [
  { id: "front-cam", name: "Front camera", topic: "/camera/front/image_raw", type: "image", visible: true, hz: 30 },
  { id: "wrist-cam", name: "Wrist camera", topic: "/camera/wrist/image_raw", type: "image", visible: true, hz: 30 },
  { id: "bev", name: "Bird's-eye-view", topic: "/camera/base/bev/image_raw", type: "image", visible: true, hz: 15 },
  { id: "depth", name: "Depth points", topic: "/camera/depth/points", type: "pointcloud", visible: true, hz: 15 },
  { id: "map", name: "Occupancy map", topic: "/map", type: "map", visible: true, hz: 1 },
  { id: "tf", name: "TF tree", topic: "/tf", type: "tf", visible: true, hz: 50 },
  { id: "scan", name: "Laser scan", topic: "/scan", type: "laserscan", visible: false, hz: 10 },
];

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "view.listLayers",
    description: "List all sensor visualization layers (cameras, BEV, depth, map, TF, laser scan) with their ROS 2 topics, types, visibility, and refresh rates.",
    inputSchema: s({}),
    handler: async () => jsonResult(LAYERS),
    product: "view", readOnly: true,
  },
  {
    name: "view.getLayer",
    description: "Get a specific sensor layer by ID with its topic, type, visibility, and refresh rate.",
    inputSchema: s(
      { layerId: { type: "string", description: "Layer ID (e.g. 'front-cam', 'wrist-cam', 'bev', 'depth', 'map', 'tf', 'scan')" } },
      ["layerId"],
    ),
    handler: async (p) => jsonResult(LAYERS.find((l) => l.id === p.layerId) ?? { error: "not found" }),
    product: "view", readOnly: true,
  },
  {
    name: "view.toggleLayer",
    description: "Toggle the visibility of a sensor layer in the perception viewer.",
    inputSchema: s(
      { layerId: { type: "string", description: "Layer ID to toggle" } },
      ["layerId"],
    ),
    handler: async (p) => {
      const layer = LAYERS.find((l) => l.id === p.layerId);
      if (!layer) return jsonResult({ error: `Layer not found: ${p.layerId}` });
      return jsonResult({ ...layer, visible: !layer.visible, toggled: true });
    },
    product: "view", readOnly: false,
  },
];
