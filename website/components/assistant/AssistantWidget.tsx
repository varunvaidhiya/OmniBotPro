"use client";

/*
 * AssistantWidget — the in-console AI assistant.
 *
 * A single floating launcher (same icon, same bottom-right position on EVERY
 * console) that opens a chat panel. The chat talks to /api/assistant, which
 * runs a tool-calling agent over the platform's MCP registry — so a user can
 * ask for anything ("how many robots are degraded?", "roll back the canary",
 * "start a training run") and the assistant calls the matching endpoint.
 *
 * The model is user-selectable (OpenRouter): the picker is populated from
 * GET /api/assistant and the choice persists in localStorage.
 *
 * Rendered by AssistantMount, which mounts it on console routes only.
 */

import { useCallback, useEffect, useRef, useState } from "react";
import {
  Sparkles,
  X,
  Send,
  Loader2,
  Wrench,
  AlertTriangle,
  ChevronDown,
} from "lucide-react";

interface AssistantModel {
  id: string;
  label: string;
  provider: string;
  blurb: string;
}

interface ToolTrace {
  name: string;
  readOnly: boolean;
  args: Record<string, unknown>;
  isError: boolean;
}

interface ChatMessage {
  role: "user" | "assistant";
  content: string;
  toolCalls?: ToolTrace[];
  error?: boolean;
}

const MODEL_STORAGE_KEY = "ohho_assistant_model";

/** A few discoverable starter prompts, lightly tailored per console. */
function starterPrompts(product: string): string[] {
  const byProduct: Record<string, string[]> = {
    fleet: ["How many robots are degraded?", "Show me the latest alerts"],
    care: ["List open work orders", "Which robots need maintenance soon?"],
    twin: ["What's the current twin state?", "Show sync metrics"],
    market: ["Find navigation skills", "What are the most popular skills?"],
    garage: ["List the robots in my garage", "What robot categories exist?"],
    train: ["Show my training runs", "What datasets are available?"],
    autonomy: ["What missions are defined?", "Show the active mission status"],
  };
  return byProduct[product] ?? ["What can you do here?", "Give me a status overview"];
}

export default function AssistantWidget({
  product,
  productLabel,
}: {
  product: string;
  productLabel: string;
}) {
  const [open, setOpen] = useState(false);
  const [models, setModels] = useState<AssistantModel[]>([]);
  const [model, setModel] = useState<string>("");
  const [configured, setConfigured] = useState<boolean | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [busy, setBusy] = useState(false);

  const scrollRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const loadedRef = useRef(false);

  // Load model catalog + configured flag the first time the panel opens.
  useEffect(() => {
    if (!open || loadedRef.current) return;
    loadedRef.current = true;
    fetch("/api/assistant")
      .then((r) => r.json())
      .then((data: { configured: boolean; models: AssistantModel[]; defaultModel: string }) => {
        setConfigured(!!data.configured);
        setModels(data.models ?? []);
        const saved = typeof window !== "undefined" ? localStorage.getItem(MODEL_STORAGE_KEY) : null;
        const ids = (data.models ?? []).map((m) => m.id);
        setModel(saved && ids.includes(saved) ? saved : data.defaultModel || ids[0] || "");
      })
      .catch(() => setConfigured(false));
  }, [open]);

  // Keep the transcript scrolled to the bottom.
  useEffect(() => {
    if (scrollRef.current) scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
  }, [messages, busy]);

  // Focus the input when opening; close on Escape.
  useEffect(() => {
    if (open) setTimeout(() => inputRef.current?.focus(), 60);
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape" && open) setOpen(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);

  const onModelChange = (id: string) => {
    setModel(id);
    if (typeof window !== "undefined") localStorage.setItem(MODEL_STORAGE_KEY, id);
  };

  const send = useCallback(
    async (text: string) => {
      const trimmed = text.trim();
      if (!trimmed || busy) return;

      const history = [...messages, { role: "user" as const, content: trimmed }];
      setMessages(history);
      setInput("");
      setBusy(true);

      try {
        const res = await fetch("/api/assistant", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            messages: history.map((m) => ({ role: m.role, content: m.content })),
            model,
            product,
          }),
        });
        const data = await res.json();
        if (!res.ok) {
          setMessages((prev) => [
            ...prev,
            {
              role: "assistant",
              content: data?.error ?? "Something went wrong. Please try again.",
              toolCalls: data?.toolCalls,
              error: true,
            },
          ]);
        } else {
          setMessages((prev) => [
            ...prev,
            { role: "assistant", content: data.reply ?? "Done.", toolCalls: data.toolCalls },
          ]);
        }
      } catch {
        setMessages((prev) => [
          ...prev,
          { role: "assistant", content: "Network error — couldn't reach the assistant.", error: true },
        ]);
      } finally {
        setBusy(false);
      }
    },
    [busy, messages, model, product],
  );

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    void send(input);
  };

  const onKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      void send(input);
    }
  };

  return (
    <>
      {/* ── launcher (identical icon + position on every console) ── */}
      {!open && (
        <button
          type="button"
          aria-label="Open OhhO Assistant"
          onClick={() => setOpen(true)}
          className="fixed bottom-5 right-5 z-[95] flex items-center gap-2 rounded-full pl-3.5 pr-4 py-3 font-semibold text-[13px] shadow-lg transition-all duration-200 hover:-translate-y-0.5"
          style={{
            background: "var(--cyan)",
            color: "var(--bg)",
            boxShadow: "0 8px 30px rgba(0,212,255,0.35)",
          }}
        >
          <Sparkles size={17} strokeWidth={2.4} />
          Ask OhhO
        </button>
      )}

      {/* ── chat panel ── */}
      {open && (
        <div
          className="fixed bottom-5 right-5 z-[95] flex flex-col rounded-2xl overflow-hidden"
          style={{
            width: "min(420px, 94vw)",
            height: "min(640px, 82vh)",
            background: "rgba(10,14,26,0.92)",
            border: "1px solid rgba(255,255,255,0.12)",
            backdropFilter: "blur(20px)",
            boxShadow: "0 24px 70px -18px rgba(0,0,0,0.7)",
          }}
          role="dialog"
          aria-label="OhhO Assistant"
        >
          {/* header */}
          <div
            className="flex items-center gap-2.5 px-4 py-3 shrink-0"
            style={{ borderBottom: "1px solid rgba(255,255,255,0.08)" }}
          >
            <div
              className="w-7 h-7 rounded-lg flex items-center justify-center shrink-0"
              style={{ color: "var(--cyan)", background: "rgba(0,212,255,0.12)", border: "1px solid rgba(0,212,255,0.24)" }}
            >
              <Sparkles size={15} />
            </div>
            <div className="min-w-0 flex-1">
              <div className="text-[13px] font-semibold leading-tight">OhhO Assistant</div>
              <div className="text-[10.5px] font-mono truncate" style={{ color: "var(--faint)" }}>
                {productLabel}
              </div>
            </div>

            {/* model picker */}
            <div className="relative">
              <select
                value={model}
                onChange={(e) => onModelChange(e.target.value)}
                aria-label="AI model"
                className="appearance-none text-[11px] font-mono rounded-lg pl-2.5 pr-6 py-1.5 cursor-pointer focus:outline-none"
                style={{
                  background: "rgba(255,255,255,0.06)",
                  border: "1px solid rgba(255,255,255,0.12)",
                  color: "rgba(255,255,255,0.8)",
                  maxWidth: 150,
                }}
              >
                {models.length === 0 && <option value="">default</option>}
                {models.map((m) => (
                  <option key={m.id} value={m.id} style={{ background: "#0A0E1A" }}>
                    {m.label}
                  </option>
                ))}
              </select>
              <ChevronDown
                size={13}
                className="absolute right-1.5 top-1/2 -translate-y-1/2 pointer-events-none"
                style={{ color: "var(--faint)" }}
              />
            </div>

            <button
              type="button"
              aria-label="Close assistant"
              onClick={() => setOpen(false)}
              className="w-7 h-7 rounded-lg flex items-center justify-center transition-colors hover:bg-white/10"
              style={{ color: "var(--faint)" }}
            >
              <X size={16} />
            </button>
          </div>

          {/* transcript */}
          <div ref={scrollRef} className="flex-1 overflow-y-auto px-4 py-4 space-y-3">
            {configured === false && (
              <Notice
                icon={<AlertTriangle size={14} />}
                text="The assistant isn't configured yet. Add OPENROUTER_API_KEY to the website environment to enable it."
              />
            )}

            {messages.length === 0 && configured !== false && (
              <div className="space-y-3">
                <p className="text-[12.5px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.6)" }}>
                  Hi! I can operate the {productLabel} and any other OhhO console for you — ask a
                  question or tell me what to do.
                </p>
                <div className="flex flex-col gap-2">
                  {starterPrompts(product).map((p) => (
                    <button
                      key={p}
                      type="button"
                      onClick={() => void send(p)}
                      className="text-left text-[12px] px-3 py-2 rounded-lg transition-colors hover:bg-white/5"
                      style={{ background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", color: "rgba(255,255,255,0.75)" }}
                    >
                      {p}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {messages.map((m, i) => (
              <MessageBubble key={i} message={m} />
            ))}

            {busy && (
              <div className="flex items-center gap-2 text-[12px]" style={{ color: "var(--faint)" }}>
                <Loader2 size={14} className="animate-spin" />
                <span className="font-mono">working…</span>
              </div>
            )}
          </div>

          {/* composer */}
          <form
            onSubmit={onSubmit}
            className="shrink-0 p-3"
            style={{ borderTop: "1px solid rgba(255,255,255,0.08)" }}
          >
            <div
              className="flex items-end gap-2 rounded-xl px-3 py-2"
              style={{ background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.12)" }}
            >
              <textarea
                ref={inputRef}
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={onKeyDown}
                rows={1}
                placeholder={configured === false ? "Set OPENROUTER_API_KEY to chat…" : `Ask about ${productLabel}…`}
                disabled={busy}
                className="flex-1 resize-none bg-transparent text-[13px] leading-[1.5] focus:outline-none disabled:opacity-50"
                style={{ color: "var(--text)", maxHeight: 120 }}
              />
              <button
                type="submit"
                aria-label="Send"
                disabled={busy || !input.trim()}
                className="w-8 h-8 rounded-lg flex items-center justify-center shrink-0 transition-all disabled:opacity-30"
                style={{ background: "var(--cyan)", color: "var(--bg)" }}
              >
                <Send size={15} strokeWidth={2.4} />
              </button>
            </div>
            <p className="text-[10px] mt-1.5 px-1 font-mono" style={{ color: "var(--faint)" }}>
              Enter to send · Shift+Enter for a new line
            </p>
          </form>
        </div>
      )}
    </>
  );
}

function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === "user";
  return (
    <div className={`flex ${isUser ? "justify-end" : "justify-start"}`}>
      <div className="max-w-[88%]">
        {/* tool-call trace (assistant only) */}
        {!isUser && message.toolCalls && message.toolCalls.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mb-1.5">
            {message.toolCalls.map((t, i) => (
              <span
                key={i}
                title={Object.keys(t.args).length ? JSON.stringify(t.args) : undefined}
                className="inline-flex items-center gap-1 text-[10px] font-mono px-1.5 py-0.5 rounded"
                style={{
                  background: t.isError
                    ? "rgba(255,80,80,0.12)"
                    : t.readOnly
                      ? "rgba(255,255,255,0.06)"
                      : "rgba(124,58,237,0.16)",
                  color: t.isError ? "#ff8a8a" : t.readOnly ? "rgba(255,255,255,0.6)" : "var(--violet-lite)",
                  border: "1px solid rgba(255,255,255,0.08)",
                }}
              >
                <Wrench size={9} />
                {t.name}
                {!t.readOnly && <span className="opacity-80">·write</span>}
              </span>
            ))}
          </div>
        )}
        <div
          className="text-[13px] leading-[1.55] px-3 py-2 rounded-xl whitespace-pre-wrap break-words"
          style={
            isUser
              ? { background: "var(--cyan)", color: "var(--bg)", borderBottomRightRadius: 4 }
              : {
                  background: message.error ? "rgba(255,80,80,0.10)" : "rgba(255,255,255,0.05)",
                  color: message.error ? "#ffb3b3" : "var(--text)",
                  border: "1px solid rgba(255,255,255,0.08)",
                  borderBottomLeftRadius: 4,
                }
          }
        >
          {message.content}
        </div>
      </div>
    </div>
  );
}

function Notice({ icon, text }: { icon: React.ReactNode; text: string }) {
  return (
    <div
      className="flex items-start gap-2 text-[12px] leading-[1.5] px-3 py-2.5 rounded-lg"
      style={{ background: "rgba(245,170,60,0.10)", border: "1px solid rgba(245,170,60,0.22)", color: "#f5c97a" }}
    >
      <span className="shrink-0 mt-0.5">{icon}</span>
      <span>{text}</span>
    </div>
  );
}
