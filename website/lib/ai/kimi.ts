/*
 * Shared Kimi / Moonshot client — SERVER-ONLY.
 *
 * The single place the whole platform talks to the Kimi (Moonshot) chat API.
 * Holds none of the prompt logic — callers pass system + user text and get
 * back the raw model content (or a structured error). This was generalized
 * from app/api/build/generate/route.ts so every AI route (robot enrichment,
 * dynamic console specs, Build design generation) shares one battle-tested
 * implementation with the model-specific quirk handling in one place.
 *
 * NEVER import this from a client component — it reads MOONSHOT_API_KEY from
 * the server environment. Import it only inside route handlers (app/api/**).
 */

const MOONSHOT_URL = "https://api.moonshot.ai/v1/chat/completions";
const DEFAULT_MODEL = "moonshot-v1-128k";

/** Is a Kimi API key configured on the server? */
export function kimiConfigured(): boolean {
  return !!process.env.MOONSHOT_API_KEY;
}

export interface KimiResult {
  ok: boolean;
  content?: string;
  /** Human-readable error (safe to surface to the client). */
  error?: string;
  /** HTTP status to return to the caller. */
  status?: number;
  /** Upstream detail, truncated — for logs / debugging only. */
  detail?: string;
}

export interface KimiOptions {
  system: string;
  user: string;
  /** Defaults to 1200. */
  maxTokens?: number;
  /** Request a strict JSON object response_format. Defaults to true. */
  jsonObject?: boolean;
}

/**
 * Call the Kimi chat API. Returns `{ ok: true, content }` on success or
 * `{ ok: false, error, status }` on any failure (missing key, network,
 * upstream error, empty completion) — callers map this straight to a response.
 */
export async function callKimi(opts: KimiOptions): Promise<KimiResult> {
  const apiKey = process.env.MOONSHOT_API_KEY;
  if (!apiKey) {
    return {
      ok: false,
      error: "Moonshot API key not configured. Set MOONSHOT_API_KEY in the website environment.",
      status: 500,
    };
  }

  const model = process.env.MOONSHOT_MODEL || DEFAULT_MODEL;

  const payload: Record<string, unknown> = {
    model,
    messages: [
      { role: "system", content: opts.system },
      { role: "user", content: opts.user },
    ],
    max_completion_tokens: opts.maxTokens ?? 1200,
  };
  if (opts.jsonObject !== false) payload.response_format = { type: "json_object" };
  // temperature is only a documented field for the moonshot-v1 family; the
  // kimi-k2.* models ignore/forbid it.
  if (model.startsWith("moonshot-v1")) payload.temperature = 0.3;
  // kimi-k2.5/2.6 accept an explicit thinking toggle — disable it for fast
  // structured output. kimi-k2.7-code and moonshot-v1-* don't take this field.
  if (model === "kimi-k2.5" || model === "kimi-k2.6") payload.thinking = { type: "disabled" };

  let upstream: Response;
  try {
    upstream = await fetch(MOONSHOT_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${apiKey}` },
      body: JSON.stringify(payload),
    });
  } catch {
    return { ok: false, error: "Could not reach the Moonshot API.", status: 502 };
  }

  if (!upstream.ok) {
    const text = await upstream.text().catch(() => "");
    return {
      ok: false,
      error: `Moonshot API error (${upstream.status}).`,
      status: 502,
      detail: text.slice(0, 500),
    };
  }

  const data = await upstream.json().catch(() => null);
  const content: string | undefined = data?.choices?.[0]?.message?.content;
  if (!content) {
    return { ok: false, error: "The model returned an empty response.", status: 502 };
  }

  return { ok: true, content };
}

/**
 * Pull a JSON object out of model output, tolerating code fences / prose.
 * Generalized from build/generate's parseDesign so every AI route parses the
 * same way.
 */
export function parseJsonObject<T = unknown>(content: string): T | null {
  let text = content.trim();
  if (text.startsWith("```")) {
    text = text.replace(/^```[a-zA-Z]*\n?/, "").replace(/```$/, "").trim();
  }
  try {
    return JSON.parse(text) as T;
  } catch {
    const start = text.indexOf("{");
    const end = text.lastIndexOf("}");
    if (start >= 0 && end > start) {
      try {
        return JSON.parse(text.slice(start, end + 1)) as T;
      } catch {
        return null;
      }
    }
    return null;
  }
}
