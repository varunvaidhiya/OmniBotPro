import { describe, it, expect } from "vitest";

import { handleMcpRequest } from "@/lib/mcp/server";
import { getAllTools, getTool, getAllResources, getResource, TOOL_COUNT } from "@/lib/mcp/registry";
import type { JsonRpcRequest } from "@/lib/mcp/types";

// ── Registry ──────────────────────────────────────────────────────────────────

describe("MCP registry", () => {
  it("has tools from multiple products", () => {
    const tools = getAllTools();
    expect(tools.length).toBeGreaterThan(20);
    const products = new Set(tools.map((t) => t.product));
    expect(products.size).toBeGreaterThanOrEqual(6);
    expect(products.has("garage")).toBe(true);
    expect(products.has("bridge")).toBe(true);
    expect(products.has("market")).toBe(true);
  });

  it("every tool has a dotted name, description, and schema", () => {
    for (const t of getAllTools()) {
      expect(t.name).toMatch(/^[a-z]+\.[a-zA-Z]+$/);
      expect(t.description.length).toBeGreaterThan(10);
      expect(t.inputSchema.type).toBe("object");
      expect(t.product).toBeTruthy();
    }
  });

  it("tool names are unique", () => {
    const names = getAllTools().map((t) => t.name);
    expect(new Set(names).size).toBe(names.length);
  });

  it("getTool finds a tool by name", () => {
    expect(getTool("garage.listRobots")?.product).toBe("garage");
    expect(getTool("bridge.listAdapters")?.product).toBe("bridge");
    expect(getTool("market.listSkills")?.product).toBe("market");
    expect(getTool("nonexistent.tool")).toBeUndefined();
  });

  it("has resources", () => {
    const resources = getAllResources();
    expect(resources.length).toBeGreaterThan(0);
    expect(resources.some((r) => r.uri.startsWith("ohho://"))).toBe(true);
  });

  it("getResource finds a resource by URI", () => {
    expect(getResource("ohho://bridge/adapters")?.product).toBe("bridge");
    expect(getResource("ohho://nonexistent")).toBeUndefined();
  });
});

// ── JSON-RPC server ───────────────────────────────────────────────────────────

async function rpc(method: string, params?: Record<string, unknown>, id: number | string = 1) {
  const req: JsonRpcRequest = { jsonrpc: "2.0", id, method, params: params ?? null };
  return handleMcpRequest(req);
}

describe("MCP server — initialize", () => {
  it("returns server info and capabilities", async () => {
    const res = await rpc("initialize");
    expect(res.error).toBeUndefined();
    const result = res.result as Record<string, unknown>;
    expect(result.protocolVersion).toBe("2025-06-18");
    expect((result.serverInfo as Record<string, string>).name).toBe("ohho-robotics-platform");
    expect((result.capabilities as Record<string, unknown>).tools).toBeDefined();
  });
});

describe("MCP server — ping", () => {
  it("returns an empty result", async () => {
    const res = await rpc("ping");
    expect(res.error).toBeUndefined();
    expect(res.result).toEqual({});
  });
});

describe("MCP server — tools/list", () => {
  it("returns all registered tools with MCP-format fields", async () => {
    const res = await rpc("tools/list");
    expect(res.error).toBeUndefined();
    const result = res.result as { tools: Record<string, unknown>[] };
    expect(result.tools.length).toBe(TOOL_COUNT);
    const first = result.tools[0];
    expect(first.name).toBeDefined();
    expect(first.description).toBeDefined();
    expect(first.inputSchema).toBeDefined();
  });
});

describe("MCP server — tools/call", () => {
  it("calls a read-only tool and returns content", async () => {
    const res = await rpc("tools/call", { name: "bridge.listAdapters", arguments: {} });
    expect(res.error).toBeUndefined();
    const result = res.result as { content: { type: string; text: string }[]; isError: boolean };
    expect(result.isError).toBe(false);
    expect(result.content.length).toBeGreaterThan(0);
    // The text should be valid JSON (the bridge adapter list)
    const parsed = JSON.parse(result.content[0].text);
    expect(Array.isArray(parsed)).toBe(true);
    expect(parsed.length).toBe(12); // 12 protocol adapters (DDS, MAVLink, Modbus, EtherCAT, serial, CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, ROS-Industrial)
  });

  it("calls a tool with parameters", async () => {
    const res = await rpc("tools/call", { name: "market.searchSkills", arguments: { category: "humanoid" } });
    expect(res.error).toBeUndefined();
    const result = res.result as { content: { type: string; text: string }[] };
    const parsed = JSON.parse(result.content[0].text);
    expect(Array.isArray(parsed)).toBe(true);
    expect(parsed.every((s: Record<string, unknown>) => s.category === "humanoid")).toBe(true);
  });

  it("returns an error for an unknown tool", async () => {
    const res = await rpc("tools/call", { name: "nonexistent.tool", arguments: {} });
    expect(res.error).toBeDefined();
    expect(res.error?.code).toBe(-32601);
  });

  it("returns an error for missing name parameter", async () => {
    const res = await rpc("tools/call", { arguments: {} });
    expect(res.error).toBeDefined();
    expect(res.error?.code).toBe(-32602);
  });
});

describe("MCP server — resources/list", () => {
  it("returns all resources with URI, name, and mimeType", async () => {
    const res = await rpc("resources/list");
    expect(res.error).toBeUndefined();
    const result = res.result as { resources: Record<string, unknown>[] };
    expect(result.resources.length).toBeGreaterThan(0);
    expect(result.resources[0].uri).toBeDefined();
  });
});

describe("MCP server — resources/read", () => {
  it("reads a resource by URI and returns text content", async () => {
    const res = await rpc("resources/read", { uri: "ohho://bridge/adapters" });
    expect(res.error).toBeUndefined();
    const result = res.result as { contents: { uri: string; text: string; mimeType: string }[] };
    expect(result.contents[0].uri).toBe("ohho://bridge/adapters");
    expect(result.contents[0].mimeType).toBe("application/json");
    // The text should be valid JSON
    const parsed = JSON.parse(result.contents[0].text);
    expect(Array.isArray(parsed)).toBe(true);
  });

  it("returns an error for an unknown resource URI", async () => {
    const res = await rpc("resources/read", { uri: "ohho://nonexistent" });
    expect(res.error).toBeDefined();
  });
});

describe("MCP server — unknown method", () => {
  it("returns a method-not-found error", async () => {
    const res = await rpc("completely/invalid");
    expect(res.error).toBeDefined();
    expect(res.error?.code).toBe(-32601);
  });
});
