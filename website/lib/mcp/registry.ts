/*
 * MCP tool/resource/prompt registry — the single aggregation point.
 *
 * Each product console ships a lib/<product>/mcp-tools.ts file that exports
 * a ToolDefinition[] (and optionally resources/prompts). This file imports
 * them all and exposes one unified registry the MCP server reads from.
 *
 * ── Adding MCP coverage for a new console ──
 *   1. Create lib/<product>/mcp-tools.ts:
 *        export const tools: ToolDefinition[] = [ ... ];
 *        export const resources: ResourceDefinition[] = [ ... ]; // optional
 *   2. Add one import line + one registry entry below.
 *   3. The MCP server auto-exposes the new tools — no other file to touch.
 *
 * Every tool name is namespaced as "<product>.<action>" so there are no
 * collisions across products.
 */

import type { ToolDefinition, ResourceDefinition, PromptDefinition } from "./types";

// ── Product imports ───────────────────────────────────────────────────────────
import { tools as garageTools, resources as garageResources } from "@/lib/garage/mcp-tools";
import { tools as connectTools } from "@/lib/connect/mcp-tools";
import { tools as bridgeTools, resources as bridgeResources } from "@/lib/bridge/mcp-tools";
import { tools as marketTools, resources as marketResources } from "@/lib/market/mcp-tools";
import { tools as twinTools } from "@/lib/twin/mcp-tools";
import { tools as careTools } from "@/lib/care/mcp-tools";
import { tools as fleetTools } from "@/lib/fleet/mcp-tools";
import { tools as serveTools } from "@/lib/serve/mcp-tools";
import { tools as buildTools } from "@/lib/build/mcp-tools";
import { tools as pilotTools } from "@/lib/pilot/mcp-tools";
import { tools as shieldTools } from "@/lib/shield/mcp-tools";
import { tools as complyTools } from "@/lib/comply/mcp-tools";
import { tools as proofTools } from "@/lib/proof/mcp-tools";
import { tools as frameTools } from "@/lib/frame/mcp-tools";
import { tools as trainTools } from "@/lib/train/mcp-tools";
import { tools as dataTools } from "@/lib/data/mcp-tools";
import { tools as viewTools } from "@/lib/view/mcp-tools";
import { tools as autonomyTools } from "@/lib/autonomy/mcp-tools";
import { tools as mindTools } from "@/lib/mind/mcp-tools";
import { tools as benchTools } from "@/lib/bench/mcp-tools";

// ── Registry ──────────────────────────────────────────────────────────────────

const ALL_TOOLS: ToolDefinition[] = [
  ...garageTools,
  ...connectTools,
  ...bridgeTools,
  ...marketTools,
  ...twinTools,
  ...careTools,
  ...fleetTools,
  ...serveTools,
  ...buildTools,
  ...pilotTools,
  ...shieldTools,
  ...complyTools,
  ...proofTools,
  ...frameTools,
  ...trainTools,
  ...dataTools,
  ...viewTools,
  ...autonomyTools,
  ...mindTools,
  ...benchTools,
];

const ALL_RESOURCES: ResourceDefinition[] = [
  ...garageResources,
  ...bridgeResources,
  ...marketResources,
];

const ALL_PROMPTS: PromptDefinition[] = [];

// ── Public API ────────────────────────────────────────────────────────────────

export function getAllTools(): ToolDefinition[] {
  return ALL_TOOLS;
}

export function getAllResources(): ResourceDefinition[] {
  return ALL_RESOURCES;
}

export function getAllPrompts(): PromptDefinition[] {
  return ALL_PROMPTS;
}

export function getTool(name: string): ToolDefinition | undefined {
  return ALL_TOOLS.find((t) => t.name === name);
}

export function getResource(uri: string): ResourceDefinition | undefined {
  return ALL_RESOURCES.find((r) => r.uri === uri);
}

/** Group tools by product for a structured listing. */
export function toolsByProduct(): Record<string, ToolDefinition[]> {
  const groups: Record<string, ToolDefinition[]> = {};
  for (const t of ALL_TOOLS) {
    if (!groups[t.product]) groups[t.product] = [];
    groups[t.product].push(t);
  }
  return groups;
}

/** Total tool count (used in the server info response). */
export const TOOL_COUNT = ALL_TOOLS.length;
export const RESOURCE_COUNT = ALL_RESOURCES.length;
