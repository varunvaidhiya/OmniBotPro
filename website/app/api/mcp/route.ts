/*
 * MCP HTTP endpoint — POST /api/mcp
 *
 * Implements the Model Context Protocol (MCP) over HTTP using JSON-RPC 2.0.
 * Stateless: each request is processed independently, making it compatible
 * with Vercel serverless functions and any MCP client.
 *
 * Authentication:
 *   X-API-Key: <key>       — MCP convention
 *   Authorization: Bearer  — standard HTTP
 *   (No MCP_API_KEY env var = open access in development)
 *
 * Supported JSON-RPC methods:
 *   initialize, ping, tools/list, tools/call, resources/list, resources/read,
 *   prompts/list, logging/setLevel
 *
 * Client configuration (OpenCode, Claude Desktop, Cursor, etc.):
 *   URL: https://ohho-robotics.com/api/mcp
 *   Header: X-API-Key: <your-key>
 *   Transport: streamable-http
 *
 * See MCP_SETUP.md for full setup instructions.
 */

import { handleMcpRequest, handleMcpBatch } from "@/lib/mcp/server";
import { isAuthorized, unauthorizedResponse } from "@/lib/mcp/auth";
import { SERVER_INFO, type JsonRpcRequest, type JsonRpcResponse } from "@/lib/mcp/types";
import { getAllTools, getAllResources, TOOL_COUNT, RESOURCE_COUNT } from "@/lib/mcp/registry";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

// ── POST: JSON-RPC request ───────────────────────────────────────────────────

export async function POST(req: Request) {
  if (!isAuthorized(req)) return unauthorizedResponse();

  let body: unknown;
  try {
    body = await req.json();
  } catch {
    return Response.json({
      jsonrpc: "2.0",
      id: null,
      error: { code: -32700, message: "Parse error: invalid JSON." },
    } as JsonRpcResponse, { status: 400 });
  }

  // Batch request (array of JSON-RPC calls)
  if (Array.isArray(body)) {
    if (body.length === 0) {
      return Response.json({
        jsonrpc: "2.0",
        id: null,
        error: { code: -32600, message: "Invalid request: empty batch." },
      } as JsonRpcResponse, { status: 400 });
    }
    const responses = await handleMcpBatch(body as JsonRpcRequest[]);
    return Response.json(responses as JsonRpcResponse[]);
  }

  // Single request
  const rpcReq = body as JsonRpcRequest;
  if (!rpcReq.jsonrpc || !rpcReq.method) {
    return Response.json({
      jsonrpc: "2.0",
      id: rpcReq.id ?? null,
      error: { code: -32600, message: "Invalid request: missing jsonrpc or method." },
    } as JsonRpcResponse, { status: 400 });
  }

  const response = await handleMcpRequest(rpcReq);
  return Response.json(response as JsonRpcResponse);
}

// ── GET: server info + capability discovery ──────────────────────────────────

export async function GET(req: Request) {
  if (!isAuthorized(req)) return unauthorizedResponse();

  return Response.json({
    server: SERVER_INFO,
    endpoint: "/api/mcp",
    transport: "json-rpc-2.0-over-http",
    toolCount: TOOL_COUNT,
    resourceCount: RESOURCE_COUNT,
    products: Array.from(new Set(getAllTools().map((t) => t.product))),
    auth: process.env.MCP_API_KEY ? "enabled (X-API-Key or Bearer)" : "disabled (set MCP_API_KEY env var)",
    usage: {
      method: "POST",
      url: "/api/mcp",
      headers: { "Content-Type": "application/json", "X-API-Key": "<your-key>" },
      example: {
        jsonrpc: "2.0",
        id: 1,
        method: "tools/list",
      },
    },
  });
}

// ── OPTIONS: CORS preflight ──────────────────────────────────────────────────

export async function OPTIONS() {
  return new Response(null, {
    status: 204,
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "POST, GET, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type, X-API-Key, Authorization",
    },
  });
}
