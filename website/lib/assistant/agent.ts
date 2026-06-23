/*
 * OhhO Assistant — the agent loop. SERVER-ONLY.
 *
 * Given a conversation and a chosen model, this drives a tool-calling loop:
 *   1. Send the messages + every MCP tool to the model via OpenRouter.
 *   2. If the model asks to call tools, dispatch each into the MCP registry,
 *      append the results, and loop.
 *   3. When the model returns a plain answer (no tool calls), stop and return
 *      it, along with a trace of every tool that ran (for the UI).
 *
 * The same MCP registry that powers the external /api/mcp server backs this, so
 * "any task in the chat maps to the respective endpoint" is true by construction.
 */

import {
  callOpenRouter,
  openRouterConfigured,
  type ORMessage,
} from "@/lib/ai/openrouter";
import { isModelAllowed, defaultModelId } from "@/lib/assistant/models";
import {
  mcpToolsToOpenRouter,
  dispatchToolCall,
  type ToolDispatchResult,
} from "@/lib/assistant/tools";
import { getAllTools, toolsByProduct } from "@/lib/mcp/registry";

/** Hard ceiling on tool round-trips, so a confused model can't loop forever. */
const MAX_STEPS = 6;

export interface ChatTurn {
  role: "user" | "assistant";
  content: string;
}

export interface AssistantRequest {
  messages: ChatTurn[];
  /** OpenRouter model id; falls back to the configured default. */
  model?: string;
  /** Current console the user is on (e.g. "fleet") — focuses the system prompt. */
  product?: string;
}

export interface AssistantToolTrace {
  name: string;
  readOnly: boolean;
  args: Record<string, unknown>;
  isError: boolean;
}

export interface AssistantResponse {
  ok: boolean;
  reply?: string;
  /** Every tool the assistant invoked, in order, for display in the UI. */
  toolCalls?: AssistantToolTrace[];
  model?: string;
  error?: string;
  status?: number;
  detail?: string;
}

export async function runAssistant(req: AssistantRequest): Promise<AssistantResponse> {
  if (!openRouterConfigured()) {
    return {
      ok: false,
      status: 503,
      error:
        "The AI assistant is not configured yet. Add OPENROUTER_API_KEY to the website environment to enable it.",
    };
  }

  const model = req.model && isModelAllowed(req.model) ? req.model : defaultModelId();

  if (!req.messages?.length) {
    return { ok: false, status: 400, error: "No messages provided." };
  }

  const tools = mcpToolsToOpenRouter(getAllTools());
  const messages: ORMessage[] = [
    { role: "system", content: systemPrompt(req.product) },
    ...req.messages.map((m) => ({ role: m.role, content: m.content })),
  ];

  const trace: AssistantToolTrace[] = [];

  for (let step = 0; step < MAX_STEPS; step++) {
    const isLastStep = step === MAX_STEPS - 1;
    const result = await callOpenRouter({
      model,
      messages,
      tools,
      // On the final allowed step, forbid further tool calls so the model is
      // forced to produce a written answer with what it already gathered.
      toolChoice: isLastStep ? "none" : "auto",
    });

    if (!result.ok || !result.message) {
      return {
        ok: false,
        status: result.status ?? 502,
        error: result.error ?? "The model failed to respond.",
        detail: result.detail,
        toolCalls: trace,
      };
    }

    const msg = result.message;
    const calls = msg.tool_calls ?? [];

    if (!calls.length) {
      return {
        ok: true,
        reply: (msg.content ?? "").trim() || "Done.",
        toolCalls: trace,
        model,
      };
    }

    // Record the assistant's tool-call turn verbatim so the follow-up tool
    // results line up with their call ids.
    messages.push({ role: "assistant", content: msg.content ?? null, tool_calls: calls });

    // Run each requested tool and append its result.
    const results: ToolDispatchResult[] = await Promise.all(
      calls.map((c) => dispatchToolCall(c.function.name, c.function.arguments)),
    );
    results.forEach((r, i) => {
      trace.push({ name: r.name, readOnly: r.readOnly, args: r.args, isError: r.isError });
      messages.push({
        role: "tool",
        tool_call_id: calls[i].id,
        name: calls[i].function.name,
        content: r.output,
      });
    });
  }

  // Exhausted the step budget without a final answer.
  return {
    ok: true,
    reply:
      "I gathered some information but ran out of steps before finishing. Try narrowing the request.",
    toolCalls: trace,
    model,
  };
}

/** Build the system prompt, focusing on the current console when known. */
function systemPrompt(product?: string): string {
  const groups = toolsByProduct();
  const productList = Object.keys(groups).sort().join(", ");
  const focus =
    product && groups[product]
      ? `The user is currently in the OhhO ${product} console, so prefer ${product}.* tools when relevant, but you may use any tool to complete the task.`
      : "The user may ask about any console.";

  return [
    "You are the OhhO Assistant, the built-in AI for the OhhO robotics platform.",
    "OhhO is an open platform to design, build, train, simulate and operate robots and fleets.",
    "You operate the platform by calling tools. Each tool maps to a real platform endpoint (an MCP tool).",
    `Available product areas: ${productList}.`,
    focus,
    "",
    "Guidelines:",
    "- To answer questions about robots, fleets, missions, training, telemetry, etc., CALL the relevant tool rather than guessing. Many tools are read-only and safe.",
    "- Tools whose description starts with [WRITE] change state (create/delete/update, trigger rollouts, send commands, e-stop). Only call a [WRITE] tool when the user clearly asked for that action. Briefly confirm what you did afterwards.",
    "- You can chain tools: gather with read tools, then act. Keep going until the task is done.",
    "- Be concise. Summarise tool results in plain language; show key numbers. Don't dump raw JSON unless asked.",
    "- If a tool returns an error, explain it plainly and suggest a fix.",
    "- If the user's request is ambiguous or could be destructive, ask a short clarifying question before acting.",
  ].join("\n");
}
