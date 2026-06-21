/*
 * Build MCP tools — parts catalog, categories, and BOM queries.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";
import { PARTS, CATEGORIES, ENVIRONMENTS, getPart, partsByCategory, categoryMeta, type Category } from "@/lib/build/catalog";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema =>
  ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "build.listParts",
    description: "List all parts in the Build catalog (bases, drives, power, compute, arms, grippers, sensors) with real specs, pricing, and supplier links.",
    inputSchema: s({
      category: { type: "string", description: "Filter by category", enum: ["base", "drive", "power", "compute", "arm", "gripper", "sensor"] },
    }),
    handler: async (p) => p.category ? jsonResult(partsByCategory(p.category as Category)) : jsonResult(PARTS),
    product: "build", readOnly: true,
  },
  {
    name: "build.getPart",
    description: "Get full details for a specific part by ID, including mass, price, lead time, supplier, and specs.",
    inputSchema: s(
      { partId: { type: "string", description: "Part ID (e.g. 'base-mecanum', 'arm-so101', 'compute-orin')" } },
      ["partId"],
    ),
    handler: async (p) => {
      const part = getPart(p.partId as string);
      if (!part) return errorResult(`Part not found: ${p.partId}`);
      return jsonResult(part);
    },
    product: "build", readOnly: true,
  },
  {
    name: "build.listCategories",
    description: "List all part categories (base, drive, power, compute, arm, gripper, sensor) with metadata — which are required, which allow multiple selections.",
    inputSchema: s({}),
    handler: async () => jsonResult(CATEGORIES),
    product: "build", readOnly: true,
  },
  {
    name: "build.listEnvironments",
    description: "List all operating environments (indoor, outdoor, cleanroom, cold-chain, agriculture) that Build can validate designs for.",
    inputSchema: s({}),
    handler: async () => jsonResult(ENVIRONMENTS),
    product: "build", readOnly: true,
  },
  {
    name: "build.partsByCategory",
    description: "List all parts in a specific category with full specs.",
    inputSchema: s(
      { category: { type: "string", description: "Category to filter by", enum: ["base", "drive", "power", "compute", "arm", "gripper", "sensor"] } },
      ["category"],
    ),
    handler: async (p) => jsonResult(partsByCategory(p.category as Category)),
    product: "build", readOnly: true,
  },
];
