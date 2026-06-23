/*
 * Build MCP tools — parts catalog, categories, and BOM queries.
 */

import {
  type ToolDefinition, type InputSchema, type JsonSchemaProperty,
  jsonResult, errorResult,
} from "@/lib/mcp/types";
import { PARTS, CATEGORIES, ENVIRONMENTS, getPart, partsByCategory, categoryMeta, type Category } from "@/lib/build/catalog";
import { sanitize, TEMPLATES, type Selection, type Requirements } from "@/lib/build/design";
import { computeMetrics, validate, recommend, overallStatus } from "@/lib/build/engine";

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
  {
    name: "build.listTemplates",
    description: "List the starter robot design templates (e.g. Warehouse AMR, Lab Automation) with their part selection and requirements.",
    inputSchema: s({}),
    handler: async () => jsonResult(TEMPLATES.map((t) => ({ id: t.id, name: t.name, blurb: t.blurb, design: t.design }))),
    product: "build", readOnly: true,
  },
  {
    name: "build.validateDesign",
    description: "Validate a robot design against requirements. Provide a part selection (ids per category) and target requirements; returns derived metrics, pass/warn/fail checks, an overall verdict, and improvement recommendations.",
    inputSchema: s({
      selection: {
        type: "object",
        description: "Selected part ids per category, e.g. {\"base\":[\"base-yahboom-x3\"],\"drive\":[\"drive-mecanum-4\"],\"sensor\":[\"sensor-astra\"]}",
      },
      requirements: {
        type: "object",
        description: "Targets: payload (kg), reach (m), topSpeed (m/s), runtime (h), footprint (m), environment (indoor/outdoor/cleanroom/cold-chain/agriculture). Omitted fields use sensible defaults.",
      },
    }, ["selection"]),
    handler: async (p) => {
      const design = sanitize({
        selection: p.selection as Selection | undefined,
        requirements: p.requirements as Partial<Requirements> | undefined,
      });
      const metrics = computeMetrics(design);
      const checks = validate(design, metrics);
      const recs = recommend(design, metrics);
      const { parts, ...metricSummary } = metrics;
      return jsonResult({
        design,
        status: overallStatus(checks),
        metrics: metricSummary,
        checks,
        recommendations: recs.map((r) => ({ severity: r.severity, title: r.title, detail: r.detail })),
      });
    },
    product: "build", readOnly: true,
  },
  {
    name: "build.estimateBom",
    description: "Estimate the bill of materials for a part selection: line items with price, mass, lead time and supplier, plus totals (cost, mass, longest lead time).",
    inputSchema: s({
      selection: { type: "object", description: "Selected part ids per category (see build.validateDesign)." },
    }, ["selection"]),
    handler: async (p) => {
      const design = sanitize({ selection: p.selection as Selection | undefined });
      const m = computeMetrics(design);
      if (!m.parts.length) return errorResult("No valid parts in the selection. Use build.listParts for valid ids.");
      return jsonResult({
        lineItems: m.parts.map((part) => ({
          id: part.id, name: part.name, category: part.category, brand: part.brand,
          price: part.price, mass: part.mass, leadTimeDays: part.leadTimeDays, supplier: part.supplier,
        })),
        totals: { parts: m.count, totalPrice: m.totalPrice, totalMass: m.totalMass, maxLeadTimeDays: m.maxLeadTimeDays },
      });
    },
    product: "build", readOnly: true,
  },
];
