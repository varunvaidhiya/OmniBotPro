/*
 * MCP health check — GET /api/mcp/health
 *
 * A lightweight endpoint that returns 200 if the MCP server is operational.
 * Used by monitoring, load balancers, and MCP clients to verify availability.
 */

import { TOOL_COUNT, RESOURCE_COUNT } from "@/lib/mcp/registry";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function GET() {
  return Response.json({
    status: "ok",
    server: "ohho-robotics-platform",
    tools: TOOL_COUNT,
    resources: RESOURCE_COUNT,
    timestamp: new Date().toISOString(),
  });
}
