/*
 * OhhO Assistant API.
 *
 *   GET  /api/assistant
 *     → { configured, models: AssistantModel[], defaultModel }
 *     Lets the in-console widget render its model picker and show a setup hint
 *     when OPENROUTER_API_KEY is not set yet. Never returns the key.
 *
 *   POST /api/assistant
 *     body: { messages: {role,content}[], model?: string, product?: string }
 *     → { reply: string, toolCalls: [...], model: string }
 *     Runs the tool-calling agent loop (lib/assistant/agent.ts), which drives
 *     the same MCP registry the external /api/mcp server exposes.
 *
 * The OpenRouter key is server-side only (OPENROUTER_API_KEY) — it is never
 * exposed to the browser. TODO: re-verify the caller's Supabase session
 * server-side before running [WRITE] tools; the console pages are already gated
 * client-side by ConsoleGate, but this route should re-check auth once a
 * server-side Supabase client is wired up (same TODO as /api/build/generate).
 */

import { NextResponse } from "next/server";

import { runAssistant, type ChatTurn } from "@/lib/assistant/agent";
import { configuredModels, defaultModelId } from "@/lib/assistant/models";
import { openRouterConfigured } from "@/lib/ai/openrouter";

export const runtime = "nodejs";

const MAX_MESSAGES = 40;
const MAX_CHARS = 12000; // total characters across the conversation

export async function GET() {
  return NextResponse.json({
    configured: openRouterConfigured(),
    models: configuredModels(),
    defaultModel: defaultModelId(),
  });
}

interface PostBody {
  messages?: { role?: string; content?: string }[];
  model?: string;
  product?: string;
}

export async function POST(req: Request) {
  let body: PostBody;
  try {
    body = (await req.json()) as PostBody;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const rawMessages = Array.isArray(body.messages) ? body.messages : [];
  if (!rawMessages.length) {
    return NextResponse.json({ error: "At least one message is required." }, { status: 400 });
  }
  if (rawMessages.length > MAX_MESSAGES) {
    return NextResponse.json(
      { error: `Too many messages (max ${MAX_MESSAGES}). Start a new chat.` },
      { status: 400 },
    );
  }

  const messages: ChatTurn[] = [];
  let totalChars = 0;
  for (const m of rawMessages) {
    const role = m.role === "assistant" ? "assistant" : "user";
    const content = typeof m.content === "string" ? m.content : "";
    totalChars += content.length;
    messages.push({ role, content });
  }
  if (totalChars > MAX_CHARS) {
    return NextResponse.json(
      { error: "Conversation is too long. Start a new chat." },
      { status: 400 },
    );
  }

  const result = await runAssistant({
    messages,
    model: typeof body.model === "string" ? body.model : undefined,
    product: typeof body.product === "string" ? body.product : undefined,
  });

  if (!result.ok) {
    return NextResponse.json(
      { error: result.error, detail: result.detail, toolCalls: result.toolCalls ?? [] },
      { status: result.status ?? 502 },
    );
  }

  return NextResponse.json({
    reply: result.reply,
    toolCalls: result.toolCalls ?? [],
    model: result.model,
  });
}
