/*
 * OhhO Assistant — the bridge from MCP tools to OpenRouter function calling.
 *
 * Every console already exposes its capabilities as MCP ToolDefinitions in
 * lib/<product>/mcp-tools.ts, aggregated by lib/mcp/registry.ts. The assistant
 * reuses that exact registry: it advertises every MCP tool to the model as an
 * OpenAI-style "function", and when the model calls one it dispatches straight
 * to the same handler the external MCP server uses. So the in-console AI and an
 * external agent (Claude Desktop, Cursor, …) drive identical endpoints — and
 * any new MCP tool is picked up automatically with zero extra wiring.
 *
 * Name mapping: MCP tool names are dotted ("fleet.listRobots"), but OpenAI /
 * OpenRouter function names must match ^[a-zA-Z0-9_-]{1,64}$ — no dots. We map
 * "." → "__" on the way out and reverse it on dispatch. No product or action
 * contains "__", so the round-trip is lossless.
 */

import type { ORTool } from "@/lib/ai/openrouter";
import { getAllTools, getTool } from "@/lib/mcp/registry";
import type { ToolDefinition, ToolResult } from "@/lib/mcp/types";

const SEP = "__";

/** Encode a dotted MCP name as an OpenAI-safe function name. */
export function encodeToolName(mcpName: string): string {
  return mcpName.replace(/\./g, SEP);
}

/** Decode an OpenAI-safe function name back to its dotted MCP name. */
export function decodeToolName(fnName: string): string {
  return fnName.replace(new RegExp(SEP, "g"), ".");
}

/** Convert the MCP registry (or a subset) to OpenRouter tool definitions. */
export function mcpToolsToOpenRouter(tools: ToolDefinition[] = getAllTools()): ORTool[] {
  return tools.map((t) => ({
    type: "function",
    function: {
      name: encodeToolName(t.name),
      // Prefix write tools so the model knows they mutate state.
      description: t.readOnly ? t.description : `[WRITE] ${t.description}`,
      parameters: t.inputSchema,
    },
  }));
}

export interface ToolDispatchResult {
  /** Dotted MCP tool name that ran. */
  name: string;
  /** Whether this tool is read-only (vs a state mutation). */
  readOnly: boolean;
  /** The arguments the model supplied. */
  args: Record<string, unknown>;
  /** Flat text the model receives back as the tool result. */
  output: string;
  /** True if the tool reported an error (still fed back to the model). */
  isError: boolean;
}

/**
 * Run a single tool call coming from the model. Never throws — any failure is
 * captured and returned as an error result so the agent loop can feed it back
 * to the model and keep going.
 */
export async function dispatchToolCall(
  fnName: string,
  rawArgs: string,
): Promise<ToolDispatchResult> {
  const name = decodeToolName(fnName);
  let args: Record<string, unknown> = {};
  if (rawArgs && rawArgs.trim()) {
    try {
      args = JSON.parse(rawArgs) as Record<string, unknown>;
    } catch {
      return {
        name,
        readOnly: true,
        args: {},
        output: `Invalid JSON arguments for ${name}.`,
        isError: true,
      };
    }
  }

  const tool = getTool(name);
  if (!tool) {
    return { name, readOnly: true, args, output: `Unknown tool: ${name}.`, isError: true };
  }

  let result: ToolResult;
  try {
    result = await tool.handler(args);
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return { name, readOnly: tool.readOnly, args, output: `Tool error: ${msg}`, isError: true };
  }

  return {
    name,
    readOnly: tool.readOnly,
    args,
    output: flattenResult(result),
    isError: result.isError ?? false,
  };
}

/** Collapse a ToolResult's content blocks into a single string for the model. */
function flattenResult(result: ToolResult): string {
  const parts = result.content.map((c) =>
    c.type === "text" ? c.text : JSON.stringify(c.data, null, 2),
  );
  const text = parts.join("\n").trim();
  return text || "(no output)";
}
