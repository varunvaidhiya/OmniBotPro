/*
 * Market MCP tools — skill marketplace queries.
 *
 * Tools:
 *   market.listSkills      — list all skills
 *   market.searchSkills    — search/filter skills by category, brand, or text
 *   market.getSkill        — get skill detail with Proof verification results
 *   market.listAuthors     — list all skill authors
 *   market.getStats        — get marketplace statistics
 *
 * Resources:
 *   ohho://market/skills   — all skills as JSON
 */

import {
  type ToolDefinition,
  type ResourceDefinition,
  jsonResult,
  errorResult,
  type InputSchema,
  type JsonSchemaProperty,
} from "@/lib/mcp/types";
import { SKILLS, AUTHORS, MARKETPLACE_STATS, getSkill, getAuthor } from "@/lib/market/skills";

const s = (props: Record<string, JsonSchemaProperty>, required: string[] = []): InputSchema => ({ type: "object", properties: props, required });

export const tools: ToolDefinition[] = [
  {
    name: "market.listSkills",
    description: "List all skills in the marketplace with name, robots, success rate, price, and author.",
    inputSchema: s({}),
    handler: async () => jsonResult(SKILLS),
    product: "market",
    readOnly: true,
  },
  {
    name: "market.searchSkills",
    description: "Search and filter skills by category, brand, robot model, or free-text query.",
    inputSchema: s({
      query: { type: "string", description: "Free-text search across name, description, and tags" },
      category: { type: "string", description: "Filter by category", enum: ["humanoid", "quadruped", "wheeled", "arm"] },
      brand: { type: "string", description: "Filter by robot brand (e.g. 'Unitree', 'Universal Robots')" },
      robot: { type: "string", description: "Filter by robot model (e.g. 'G1', 'Go2', 'UR5e')" },
      maxPrice: { type: "number", description: "Maximum price in USD (0 = free only)" },
    }),
    handler: async (params) => {
      let result = SKILLS;
      if (params.category) result = result.filter((s) => s.category === params.category);
      if (params.brand) result = result.filter((s) => s.brands.some((b) => b.toLowerCase() === String(params.brand).toLowerCase()));
      if (params.robot) result = result.filter((s) => s.robots.some((r) => r.toLowerCase() === String(params.robot).toLowerCase()));
      if (params.maxPrice !== undefined) result = result.filter((s) => s.price <= Number(params.maxPrice));
      if (params.query) {
        const q = String(params.query).toLowerCase();
        result = result.filter((s) => s.name.toLowerCase().includes(q) || s.description.toLowerCase().includes(q) || s.tags.some((t) => t.includes(q)));
      }
      return jsonResult(result);
    },
    product: "market",
    readOnly: true,
  },
  {
    name: "market.getSkill",
    description: "Get full detail for a skill, including Proof scenario verification results, author info, and deploy metadata.",
    inputSchema: s(
      { skillId: { type: "string", description: "Skill ID (e.g. 'pick-place-cup', 'patrol-warehouse')" } },
      ["skillId"],
    ),
    handler: async (params) => {
      const skill = getSkill(params.skillId as string);
      if (!skill) return errorResult(`Skill not found: ${params.skillId}`);
      const author = getAuthor(skill.authorId);
      return jsonResult({ skill, author });
    },
    product: "market",
    readOnly: true,
  },
  {
    name: "market.listAuthors",
    description: "List all skill authors with their profile stats (skill count, downloads, revenue, rating).",
    inputSchema: s({}),
    handler: async () => jsonResult(AUTHORS),
    product: "market",
    readOnly: true,
  },
  {
    name: "market.getStats",
    description: "Get marketplace statistics: total skills, total downloads, total authors, platform take-rate.",
    inputSchema: s({}),
    handler: async () => jsonResult(MARKETPLACE_STATS),
    product: "market",
    readOnly: true,
  },
  {
    name: "market.purchaseSkill",
    description: "Purchase a paid skill so it can be installed. Free skills don't need purchasing. Returns a receipt.",
    inputSchema: s({ skillId: { type: "string", description: "Skill id to purchase" } }, ["skillId"]),
    handler: async (params) => {
      const skill = getSkill(params.skillId as string);
      if (!skill) return errorResult(`Skill not found: ${params.skillId}`);
      if (skill.price === 0) return jsonResult({ skillId: skill.id, price: 0, message: `${skill.name} is free — no purchase needed, you can install it directly.` });
      return jsonResult({ skillId: skill.id, name: skill.name, price: skill.price, purchased: true, message: `Purchased ${skill.name} for $${skill.price}.` });
    },
    product: "market",
    readOnly: false,
  },
  {
    name: "market.installSkill",
    description: "Install a marketplace skill onto a robot in the garage. Returns install status. (Paid skills must be purchased first.)",
    inputSchema: s({
      skillId: { type: "string", description: "Skill id to install" },
      robotId: { type: "string", description: "Target robot id from the garage (optional)" },
    }, ["skillId"]),
    handler: async (params) => {
      const skill = getSkill(params.skillId as string);
      if (!skill) return errorResult(`Skill not found: ${params.skillId}`);
      return jsonResult({
        skillId: skill.id, name: skill.name, robotId: (params.robotId as string) ?? "default",
        installed: true,
        message: `Installed ${skill.name}${params.robotId ? ` on ${params.robotId}` : ""}. It's ready to run.`,
      });
    },
    product: "market",
    readOnly: false,
  },
  {
    name: "market.publishSkill",
    description: "Publish a new skill to the marketplace under your author profile. Returns the created skill id.",
    inputSchema: s({
      name: { type: "string", description: "Skill name" },
      category: { type: "string", description: "Skill category", enum: ["humanoid", "quadruped", "wheeled", "arm"] },
      price: { type: "number", description: "Price in USD (0 for free)" },
      description: { type: "string", description: "Short description of what the skill does" },
    }, ["name", "category"]),
    handler: async (params) => {
      const id = String(params.name).toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, "");
      return jsonResult({
        skillId: id, name: params.name, category: params.category, price: Number(params.price ?? 0),
        status: "in-review",
        message: `Skill '${params.name}' submitted for Proof verification before listing.`,
      });
    },
    product: "market",
    readOnly: false,
  },
  {
    name: "market.rateSkill",
    description: "Leave a star rating (1–5) and optional review for a skill you've used.",
    inputSchema: s({
      skillId: { type: "string", description: "Skill id to rate" },
      rating: { type: "number", description: "Star rating 1–5" },
      review: { type: "string", description: "Optional written review" },
    }, ["skillId", "rating"]),
    handler: async (params) => {
      const skill = getSkill(params.skillId as string);
      if (!skill) return errorResult(`Skill not found: ${params.skillId}`);
      const rating = Math.max(1, Math.min(5, Number(params.rating)));
      return jsonResult({ skillId: skill.id, rating, review: (params.review as string) ?? "", message: `Thanks — your ${rating}★ rating for ${skill.name} was recorded.` });
    },
    product: "market",
    readOnly: false,
  },
];

export const resources: ResourceDefinition[] = [
  {
    uri: "ohho://market/skills",
    name: "All marketplace skills",
    description: "Complete skill catalog with Proof verification results, pricing, and author info.",
    mimeType: "application/json",
    product: "market",
    read: async () => JSON.stringify(SKILLS, null, 2),
  },
];
