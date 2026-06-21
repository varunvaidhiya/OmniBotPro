/*
 * MCP JSON-RPC server — handles the Model Context Protocol over HTTP.
 *
 * Stateless: each POST is a single JSON-RPC request (or batch). The server
 * doesn't maintain sessions between requests, which makes it compatible
 * with Vercel serverless functions and any MCP client (OpenCode, Claude
 * Desktop, Cursor, etc.).
 *
 * Supported methods:
 *   initialize           → server info + capabilities
 *   tools/list           → all registered tools (grouped by product)
 *   tools/call           → invoke a tool by name with params
 *   resources/list       → all registered resources
 *   resources/read       → read a resource by URI
 *   prompts/list         → all registered prompts
 *   ping                 → keepalive
 */

import {
  type JsonRpcRequest,
  type JsonRpcResponse,
  type JsonRpcError,
  type ToolDefinition,
  SERVER_INFO,
  RPC_ERRORS,
  textResult,
  jsonResult,
  errorResult,
} from "./types";
import { getAllTools, getAllResources, getAllPrompts, getTool, getResource, TOOL_COUNT } from "./registry";

// ── Main entry: process a single JSON-RPC request ─────────────────────────────

export async function handleMcpRequest(req: JsonRpcRequest): Promise<JsonRpcResponse> {
  const { method, params, id } = req;

  try {
    switch (method) {
      case "initialize":
        return ok(id, {
          protocolVersion: SERVER_INFO.protocolVersion,
          capabilities: SERVER_INFO.capabilities,
          serverInfo: {
            name: SERVER_INFO.name,
            version: SERVER_INFO.version,
          },
        });

      case "ping":
        return ok(id, {});

      case "tools/list":
        return ok(id, {
          tools: getAllTools().map(toolToMcp),
        });

      case "tools/call": {
        const p = (params ?? {}) as { name?: string; arguments?: Record<string, unknown> };
        if (!p.name) return rpcError(id, RPC_ERRORS.INVALID_PARAMS, "Missing 'name' parameter.");
        const tool = getTool(p.name);
        if (!tool) return rpcError(id, RPC_ERRORS.METHOD_NOT_FOUND, `Unknown tool: ${p.name}`);

        const args = p.arguments ?? {};
        const result = await tool.handler(args);
        return ok(id, mcpToolResult(result));
      }

      case "resources/list":
        return ok(id, {
          resources: getAllResources().map((r) => ({
            uri: r.uri,
            name: r.name,
            description: r.description,
            mimeType: r.mimeType,
          })),
        });

      case "resources/read": {
        const p = (params ?? {}) as { uri?: string };
        if (!p.uri) return rpcError(id, RPC_ERRORS.INVALID_PARAMS, "Missing 'uri' parameter.");
        const resource = getResource(p.uri);
        if (!resource) return rpcError(id, RPC_ERRORS.METHOD_NOT_FOUND, `Unknown resource: ${p.uri}`);
        const contents = await resource.read();
        return ok(id, {
          contents: [{ uri: p.uri, mimeType: resource.mimeType, text: contents }],
        });
      }

      case "prompts/list":
        return ok(id, {
          prompts: getAllPrompts().map((p) => ({ name: p.name, description: p.description })),
        });

      case "logging/setLevel":
        return ok(id, {});

      default:
        return rpcError(id, RPC_ERRORS.METHOD_NOT_FOUND, `Unknown method: ${method}`);
    }
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    return rpcError(id, RPC_ERRORS.INTERNAL_ERROR, `Internal error: ${message}`);
  }
}

// ── Batch support ──────────────────────────────────────────────────────────────

export async function handleMcpBatch(reqs: JsonRpcRequest[]): Promise<JsonRpcResponse[]> {
  return Promise.all(reqs.map(handleMcpRequest));
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function ok(id: number | string | null, result: unknown): JsonRpcResponse {
  return { jsonrpc: "2.0", id, result };
}

function rpcError(id: number | string | null, code: number, message: string, data?: unknown): JsonRpcResponse {
  const error: JsonRpcError = { code, message };
  if (data !== undefined) error.data = data;
  return { jsonrpc: "2.0", id, error };
}

/** Convert a ToolDefinition to the MCP tools/list format. */
function toolToMcp(t: ToolDefinition) {
  return {
    name: t.name,
    description: t.description,
    inputSchema: t.inputSchema,
    annotations: {
      readOnlyHint: t.readOnly,
      product: t.product,
    },
  };
}

/** Convert our ToolResult to the MCP tools/call response format. */
function mcpToolResult(result: ReturnType<typeof textResult>) {
  return {
    content: result.content.map((c) =>
      c.type === "text"
        ? { type: "text", text: c.text }
        : { type: "text", text: JSON.stringify(c.data, null, 2) },
    ),
    isError: result.isError ?? false,
  };
}
