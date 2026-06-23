import { describe, it, expect } from "vitest";

import {
  encodeToolName,
  decodeToolName,
  mcpToolsToOpenRouter,
  dispatchToolCall,
} from "@/lib/assistant/tools";
import {
  DEFAULT_MODELS,
  configuredModels,
  defaultModelId,
  isModelAllowed,
} from "@/lib/assistant/models";
import { getAllTools } from "@/lib/mcp/registry";

describe("assistant tool-name mapping", () => {
  it("round-trips dotted MCP names through OpenAI-safe names", () => {
    for (const t of getAllTools()) {
      const encoded = encodeToolName(t.name);
      expect(encoded).not.toContain(".");
      // OpenAI/OpenRouter function-name constraint.
      expect(encoded).toMatch(/^[a-zA-Z0-9_-]{1,64}$/);
      expect(decodeToolName(encoded)).toBe(t.name);
    }
  });
});

describe("mcpToolsToOpenRouter", () => {
  const tools = mcpToolsToOpenRouter();

  it("exposes every registry tool as a function", () => {
    expect(tools.length).toBe(getAllTools().length);
    for (const t of tools) {
      expect(t.type).toBe("function");
      expect(typeof t.function.name).toBe("string");
      expect(typeof t.function.description).toBe("string");
      expect(t.function.parameters).toBeTruthy();
    }
  });

  it("flags write tools in the description so the model is cautious", () => {
    const writeTool = getAllTools().find((t) => !t.readOnly);
    expect(writeTool).toBeTruthy();
    const mapped = tools.find((t) => t.function.name === encodeToolName(writeTool!.name));
    expect(mapped!.function.description.startsWith("[WRITE]")).toBe(true);
  });

  it("produces unique function names", () => {
    const names = tools.map((t) => t.function.name);
    expect(new Set(names).size).toBe(names.length);
  });
});

describe("dispatchToolCall", () => {
  it("runs a real read-only tool from the registry", async () => {
    // fleet.getHealth takes no args and uses in-memory data — safe to call.
    const res = await dispatchToolCall(encodeToolName("fleet.getHealth"), "{}");
    expect(res.name).toBe("fleet.getHealth");
    expect(res.isError).toBe(false);
    expect(res.output).toContain("total");
  });

  it("returns an error result for an unknown tool (never throws)", async () => {
    const res = await dispatchToolCall("nope__doesNotExist", "{}");
    expect(res.isError).toBe(true);
    expect(res.output).toContain("Unknown tool");
  });

  it("handles malformed JSON arguments gracefully", async () => {
    const res = await dispatchToolCall(encodeToolName("fleet.getHealth"), "{not json");
    expect(res.isError).toBe(true);
    expect(res.output).toContain("Invalid JSON");
  });
});

describe("assistant models", () => {
  it("ships a non-empty default catalog with valid ids", () => {
    expect(DEFAULT_MODELS.length).toBeGreaterThan(0);
    for (const m of DEFAULT_MODELS) {
      expect(m.id).toMatch(/.+\/.+/); // provider/model
      expect(m.label.length).toBeGreaterThan(0);
    }
  });

  it("falls back to the default catalog and a valid default id", () => {
    const models = configuredModels();
    expect(models.length).toBeGreaterThan(0);
    expect(isModelAllowed(defaultModelId())).toBe(true);
  });

  it("rejects models not in the catalog", () => {
    expect(isModelAllowed("totally/made-up-model")).toBe(false);
  });
});
