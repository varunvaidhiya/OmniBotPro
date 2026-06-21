/*
 * MCP types — the shared contracts for the OhhO MCP server.
 *
 * These mirror the MCP (Model Context Protocol) specification's tool,
 * resource and prompt definitions, but are self-contained (no external
 * SDK dependency) so the server runs in a Vercel serverless function
 * with zero install.
 *
 * To add MCP coverage for a new product console:
 *   1. Create lib/<product>/mcp-tools.ts exporting a ToolDefinition[]
 *   2. Import it in lib/mcp/registry.ts
 *   3. That's it — the MCP server auto-exposes the new tools.
 */

// ── JSON-RPC 2.0 ──────────────────────────────────────────────────────────────

export interface JsonRpcRequest {
  jsonrpc: "2.0";
  id: number | string | null;
  method: string;
  params?: Record<string, unknown> | null;
}

export interface JsonRpcResponse {
  jsonrpc: "2.0";
  id: number | string | null;
  result?: unknown;
  error?: JsonRpcError;
}

export interface JsonRpcError {
  code: number;
  message: string;
  data?: unknown;
}

// ── MCP Tool ──────────────────────────────────────────────────────────────────

/** A simplified JSON Schema for tool parameters (the subset MCP uses). */
export interface InputSchema {
  type: "object";
  properties: Record<string, JsonSchemaProperty>;
  required?: string[];
}

export interface JsonSchemaProperty {
  type: "string" | "number" | "boolean" | "array" | "object";
  description?: string;
  enum?: (string | number)[];
  items?: JsonSchemaProperty;
  properties?: Record<string, JsonSchemaProperty>;
  required?: string[];
  default?: unknown;
}

export interface ToolDefinition {
  /** Dotted name, e.g. "garage.listRobots". Must be unique across all products. */
  name: string;
  /** Human-readable description — the LLM reads this to decide when to call the tool. */
  description: string;
  /** JSON Schema for the tool's parameters. */
  inputSchema: InputSchema;
  /** The handler invoked when an AI agent calls this tool. */
  handler: ToolHandler;
  /** Which product this tool belongs to (for grouping in listings). */
  product: string;
  /** Read-only tools are safe to call without confirmation; write tools mutate state. */
  readOnly: boolean;
}

export type ToolHandler = (params: Record<string, unknown>) => Promise<ToolResult>;

export interface ToolResult {
  /** Whether the tool call succeeded. */
  isError?: boolean;
  /** The content blocks returned to the LLM. */
  content: ToolContent[];
}

export type ToolContent =
  | { type: "text"; text: string }
  | { type: "json"; data: unknown };

// ── MCP Resource ──────────────────────────────────────────────────────────────

export interface ResourceDefinition {
  /** URI scheme, e.g. "ohho://garage/robots". */
  uri: string;
  /** Human-readable name. */
  name: string;
  description: string;
  /** MIME type of the content. */
  mimeType: string;
  /** The product this resource belongs to. */
  product: string;
  /** Returns the resource content as a string. */
  read: () => Promise<string>;
}

// ── MCP Prompt ────────────────────────────────────────────────────────────────

export interface PromptDefinition {
  name: string;
  description: string;
  product: string;
  /** Returns the prompt text. */
  build: (params: Record<string, unknown>) => Promise<string>;
}

// ── MCP Server Info ───────────────────────────────────────────────────────────

export interface ServerInfo {
  name: string;
  version: string;
  protocolVersion: string;
  capabilities: {
    tools: { listChanged: boolean };
    resources: { listChanged: boolean; subscribe: boolean };
    prompts: { listChanged: boolean };
  };
}

export const SERVER_INFO: ServerInfo = {
  name: "ohho-robotics-platform",
  version: "1.0.0",
  protocolVersion: "2025-06-18",
  capabilities: {
    tools: { listChanged: false },
    resources: { listChanged: false, subscribe: false },
    prompts: { listChanged: false },
  },
};

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Build a successful text result. */
export function textResult(text: string): ToolResult {
  return { content: [{ type: "text", text }] };
}

/** Build a successful JSON result (serialized for the LLM). */
export function jsonResult(data: unknown): ToolResult {
  return { content: [{ type: "json", data }] };
}

/** Build an error result. */
export function errorResult(message: string): ToolResult {
  return { isError: true, content: [{ type: "text", text: message }] };
}

/** Standard JSON-RPC error codes. */
export const RPC_ERRORS = {
  PARSE_ERROR: -32700,
  INVALID_REQUEST: -32600,
  METHOD_NOT_FOUND: -32601,
  INVALID_PARAMS: -32602,
  INTERNAL_ERROR: -32603,
} as const;
