/*
 * Shared OpenRouter client — SERVER-ONLY.
 *
 * OpenRouter (https://openrouter.ai) is an OpenAI-compatible gateway in front
 * of every major model (Anthropic, OpenAI, Google, Meta, DeepSeek, …) behind a
 * single key. The OhhO in-console assistant uses it so a user can pick whichever
 * model they like while we keep one integration.
 *
 * This module is the only place the platform talks to OpenRouter. It exposes a
 * single tool-aware chat call; the agent loop (lib/assistant/agent.ts) layers
 * the MCP tool dispatch on top.
 *
 * NEVER import this from a client component — it reads OPENROUTER_API_KEY from
 * the server environment. Import it only inside route handlers (app/api/**).
 */

const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

/** Is an OpenRouter key configured on the server? */
export function openRouterConfigured(): boolean {
  return !!process.env.OPENROUTER_API_KEY;
}

// ── OpenAI-compatible message + tool shapes ───────────────────────────────────

export interface ORToolCall {
  id: string;
  type: "function";
  function: { name: string; arguments: string };
}

export interface ORMessage {
  role: "system" | "user" | "assistant" | "tool";
  content: string | null;
  /** Present on assistant messages that request tool calls. */
  tool_calls?: ORToolCall[];
  /** Present on tool-result messages (role: "tool"). */
  tool_call_id?: string;
  /** Optional tool name on tool-result messages. */
  name?: string;
}

export interface ORTool {
  type: "function";
  function: {
    name: string;
    description: string;
    parameters: unknown;
  };
}

export interface OpenRouterResult {
  ok: boolean;
  /** The assistant message returned by the model (may include tool_calls). */
  message?: ORMessage;
  /** Human-readable error, safe to surface to the client. */
  error?: string;
  /** HTTP status to return to the caller. */
  status?: number;
  /** Upstream detail, truncated — for logs only. */
  detail?: string;
}

export interface OpenRouterOptions {
  model: string;
  messages: ORMessage[];
  tools?: ORTool[];
  /** "auto" (default) lets the model decide; "none" forbids tool calls. */
  toolChoice?: "auto" | "none";
  maxTokens?: number;
  temperature?: number;
}

/**
 * One chat round-trip to OpenRouter. Returns the raw assistant message so the
 * caller can inspect tool_calls and run the agent loop. All failures collapse
 * into `{ ok: false, error, status }` so callers map straight to a response.
 */
export async function callOpenRouter(opts: OpenRouterOptions): Promise<OpenRouterResult> {
  const apiKey = process.env.OPENROUTER_API_KEY;
  if (!apiKey) {
    return {
      ok: false,
      error:
        "OpenRouter API key not configured. Set OPENROUTER_API_KEY in the website environment.",
      status: 503,
    };
  }

  const payload: Record<string, unknown> = {
    model: opts.model,
    messages: opts.messages,
    max_tokens: opts.maxTokens ?? 1024,
    temperature: opts.temperature ?? 0.4,
  };
  if (opts.tools && opts.tools.length) {
    payload.tools = opts.tools;
    payload.tool_choice = opts.toolChoice ?? "auto";
  }

  // OpenRouter recommends (but does not require) attribution headers; they let
  // the OhhO app show up in your OpenRouter dashboard. Both are optional.
  const referer = process.env.OPENROUTER_SITE_URL || "https://ohho-robotics.com";
  const title = process.env.OPENROUTER_APP_NAME || "OhhO Console Assistant";

  let upstream: Response;
  try {
    upstream = await fetch(OPENROUTER_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${apiKey}`,
        "HTTP-Referer": referer,
        "X-Title": title,
      },
      body: JSON.stringify(payload),
    });
  } catch {
    return { ok: false, error: "Could not reach the OpenRouter API.", status: 502 };
  }

  if (!upstream.ok) {
    const text = await upstream.text().catch(() => "");
    return {
      ok: false,
      error: `OpenRouter API error (${upstream.status}).`,
      status: upstream.status === 429 ? 429 : 502,
      detail: text.slice(0, 500),
    };
  }

  const data = await upstream.json().catch(() => null);
  const message: ORMessage | undefined = data?.choices?.[0]?.message;
  if (!message) {
    return { ok: false, error: "The model returned an empty response.", status: 502 };
  }

  return { ok: true, message };
}
