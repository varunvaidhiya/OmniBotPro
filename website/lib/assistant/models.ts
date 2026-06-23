/*
 * OhhO Assistant — the catalog of selectable AI models (via OpenRouter).
 *
 * The in-console assistant talks to OpenRouter (https://openrouter.ai), which
 * exposes every major model behind one OpenAI-compatible API and one key. This
 * file is the single source of truth for which models the model-picker offers.
 *
 * Pure data only — safe to import from both client and server. The actual key
 * (OPENROUTER_API_KEY) lives only in the server environment; the browser never
 * sees it. The client learns the available models from GET /api/assistant,
 * which calls configuredModels() below.
 *
 * ── Customising the list ──
 *   • Edit DEFAULT_MODELS to change the built-in catalog, OR
 *   • Set OPENROUTER_MODELS in the environment to override it at runtime, as a
 *     comma-separated list of "id|Label" pairs, e.g.
 *       OPENROUTER_MODELS="anthropic/claude-sonnet-4|Claude Sonnet 4,openai/gpt-4o|GPT-4o"
 *   • Set OPENROUTER_DEFAULT_MODEL to pick which one is selected first.
 *
 * Every model listed here supports tool/function calling, which the assistant
 * relies on to drive the MCP endpoints.
 */

export interface AssistantModel {
  /** OpenRouter model id, e.g. "anthropic/claude-sonnet-4". */
  id: string;
  /** Short label shown in the picker. */
  label: string;
  /** Provider family, for grouping/badges. */
  provider: string;
  /** One-line "when to use this" hint. */
  blurb: string;
}

/**
 * Curated, cross-provider default catalog. All support function calling.
 * Model ids follow OpenRouter's "<provider>/<model>" convention; adjust to
 * taste once your OpenRouter key is in place.
 */
export const DEFAULT_MODELS: AssistantModel[] = [
  {
    id: "anthropic/claude-sonnet-4",
    label: "Claude Sonnet 4",
    provider: "Anthropic",
    blurb: "Best all-round reasoning and tool use. Recommended default.",
  },
  {
    id: "anthropic/claude-3.7-sonnet",
    label: "Claude 3.7 Sonnet",
    provider: "Anthropic",
    blurb: "Strong, slightly cheaper Claude for everyday console tasks.",
  },
  {
    id: "openai/gpt-4o",
    label: "GPT-4o",
    provider: "OpenAI",
    blurb: "Fast, reliable multimodal model with solid tool calling.",
  },
  {
    id: "openai/gpt-4.1",
    label: "GPT-4.1",
    provider: "OpenAI",
    blurb: "OpenAI's deeper reasoning model for complex multi-step jobs.",
  },
  {
    id: "google/gemini-2.5-pro",
    label: "Gemini 2.5 Pro",
    provider: "Google",
    blurb: "Long-context analysis across large fleets and datasets.",
  },
  {
    id: "google/gemini-2.0-flash-001",
    label: "Gemini 2.0 Flash",
    provider: "Google",
    blurb: "Very fast and cheap — great for quick status questions.",
  },
  {
    id: "meta-llama/llama-3.3-70b-instruct",
    label: "Llama 3.3 70B",
    provider: "Meta",
    blurb: "Open-weight model, good value for routine automation.",
  },
  {
    id: "deepseek/deepseek-chat",
    label: "DeepSeek V3",
    provider: "DeepSeek",
    blurb: "Capable open model, economical for high-volume use.",
  },
];

/** Fallback default when OPENROUTER_DEFAULT_MODEL is not set. */
export const DEFAULT_MODEL_ID = DEFAULT_MODELS[0].id;

/**
 * The models the server actually offers, honouring the OPENROUTER_MODELS env
 * override when present. Server-side only (reads process.env), but falls back
 * to DEFAULT_MODELS so it is harmless to call anywhere.
 */
export function configuredModels(): AssistantModel[] {
  const raw = typeof process !== "undefined" ? process.env?.OPENROUTER_MODELS : undefined;
  if (!raw || !raw.trim()) return DEFAULT_MODELS;

  const parsed: AssistantModel[] = [];
  for (const entry of raw.split(",")) {
    const [id, label] = entry.split("|").map((s) => s.trim());
    if (!id) continue;
    parsed.push({
      id,
      label: label || id,
      provider: id.split("/")[0] ?? "openrouter",
      blurb: "Configured via OPENROUTER_MODELS.",
    });
  }
  return parsed.length ? parsed : DEFAULT_MODELS;
}

/** The id selected first in the UI (env override → catalog default). */
export function defaultModelId(): string {
  const override =
    typeof process !== "undefined" ? process.env?.OPENROUTER_DEFAULT_MODEL : undefined;
  if (override && override.trim()) return override.trim();
  const models = configuredModels();
  return models[0]?.id ?? DEFAULT_MODEL_ID;
}

/** Whether the given model id is one the server is willing to call. */
export function isModelAllowed(id: string): boolean {
  return configuredModels().some((m) => m.id === id);
}
