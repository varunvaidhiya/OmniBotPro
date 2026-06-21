"use client";

/*
 * LinkConsole — the OhhO Link application shell.
 *
 * Shows how to connect any AI agent (Claude Desktop, OpenCode, Cursor,
 * Cline, Windsurf, Continue, Zed, Roo Code, Codex, Hermes, Antigravity,
 * or any MCP client) to the OhhO platform.
 *
 * Three sections:
 *   1. Endpoint + API key bar (with copy buttons)
 *   2. AI agent grid — each card expands to show config + setup steps
 *   3. Tool browser — all 91 MCP tools grouped by product, searchable
 */

import { useState, useMemo, useCallback } from "react";
import {
  ArrowLeft,
  Copy,
  Check,
  Search,
  Plug,
  Wrench,
  ExternalLink,
  ChevronDown,
  KeyRound,
  Activity,
  Zap,
} from "lucide-react";

import { AGENTS, ENDPOINT_URL, type AgentConfig } from "@/lib/link/agents";
import { getAllTools, TOOL_COUNT } from "@/lib/mcp/registry";
import PaidFeatureGate from "@/components/auth/PaidFeatureGate";

const CYAN = "#00D4FF";
const CYAN_DIM = "rgba(0,212,255,0.10)";
const GREEN = "#34D399";

export default function LinkConsole() {
  const [activeAgentId, setActiveAgentId] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [toolSearch, setToolSearch] = useState("");
  const [copied, setCopied] = useState<string | null>(null);

  const copy = useCallback((text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopied(id);
    setTimeout(() => setCopied(null), 2000);
  }, []);

  const activeAgent = AGENTS.find((a) => a.id === activeAgentId);

  const tools = getAllTools();
  const filteredTools = useMemo(() => {
    if (!toolSearch.trim()) return tools;
    const q = toolSearch.toLowerCase();
    return tools.filter(
      (t) => t.name.toLowerCase().includes(q) || t.description.toLowerCase().includes(q) || t.product.toLowerCase().includes(q),
    );
  }, [toolSearch, tools]);

  const toolsByProduct = useMemo(() => {
    const groups: Record<string, typeof tools> = {};
    for (const t of filteredTools) {
      if (!groups[t.product]) groups[t.product] = [];
      groups[t.product].push(t);
    }
    return groups;
  }, [filteredTools]);

  return (
    <div className="min-h-screen relative" style={{ background: "var(--bg)" }}>
      <div className="hero-grid opacity-40" />

      {/* ── Top bar ── */}
      <header
        className="sticky top-0 z-30 flex items-center gap-3 px-4 md:px-6 h-[56px] border-b"
        style={{
          background: "rgba(10,14,26,.86)",
          backdropFilter: "blur(18px)",
          borderColor: "var(--border)",
        }}
      >
        <a
          href="/console"
          className="inline-flex items-center gap-2 text-[12px] font-mono tracking-wider uppercase shrink-0"
          style={{ color: CYAN }}
        >
          <ArrowLeft size={14} />
          <span className="hidden sm:inline">Console</span>
        </a>

        <div className="h-5 w-px mx-1" style={{ background: "var(--border-med)" }} />

        <span className="text-[12.5px] font-mono truncate flex items-center gap-2" style={{ color: "var(--muted)" }}>
          <Plug size={14} /> OhhO Link
        </span>

        <span className="ml-auto flex items-center gap-2">
          <span
            className="hidden sm:inline-flex items-center gap-1.5 text-[11px] font-mono px-2.5 py-1 rounded-full"
            style={{ background: CYAN_DIM, color: CYAN, border: `1px solid ${CYAN}40` }}
          >
            <Wrench size={11} />
            {TOOL_COUNT} TOOLS
          </span>
        </span>
      </header>

      {/* ── Content ── */}
      <div className="relative z-10 max-w-5xl mx-auto px-4 md:px-6 py-8">
        {/* Hero */}
        <div className="mb-8">
          <div className="flex items-center gap-3 mb-4">
            <span
              className="w-12 h-12 rounded-xl flex items-center justify-center"
              style={{ background: `${CYAN}14`, border: `1px solid ${CYAN}40`, color: CYAN }}
            >
              <Plug size={22} />
            </span>
            <div>
              <h1 className="text-[24px] font-display font-bold tracking-tight">OhhO Link</h1>
              <p className="text-[13px] font-mono" style={{ color: "var(--faint)" }}>
                Connect any AI agent to your robots
              </p>
            </div>
          </div>
          <p className="text-[14px] leading-[1.7] max-w-[600px]" style={{ color: "rgba(255,255,255,0.7)" }}>
            OhhO exposes a Model Context Protocol (MCP) server with {TOOL_COUNT} tools spanning every console on the platform.
            Any AI agent that speaks MCP — Claude Desktop, OpenCode, Cursor, Cline, Windsurf, or any other — can query and
            control your robot fleet, browse the skill marketplace, trigger OTA updates, run safety tests, and more.
          </p>
        </div>

        {/* Endpoint + API key bar */}
        <PaidFeatureGate
          feature="mcp-server"
          label="MCP Server Connection"
          description="Hosted MCP server with live robot tools — requires a plan"
        >
        <div
          className="rounded-2xl p-5 mb-8"
          style={{ background: "var(--surf)", border: "1px solid var(--border)" }}
        >
          <div className="flex flex-col sm:flex-row gap-4">
            {/* Endpoint */}
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <Activity size={13} style={{ color: CYAN }} />
                <span className="text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>
                  MCP Endpoint
                </span>
              </div>
              <div className="flex items-center gap-2">
                <code
                  className="flex-1 text-[12.5px] font-mono px-3 py-2 rounded-lg truncate"
                  style={{ background: "rgba(255,255,255,0.04)", border: "1px solid var(--border)", color: "var(--text)" }}
                >
                  {ENDPOINT_URL}
                </code>
                <button
                  onClick={() => copy(ENDPOINT_URL, "endpoint")}
                  className="shrink-0 p-2 rounded-lg transition-colors hover:bg-white/5"
                  style={{ border: "1px solid var(--border)", color: copied === "endpoint" ? GREEN : "var(--muted)" }}
                >
                  {copied === "endpoint" ? <Check size={14} /> : <Copy size={14} />}
                </button>
              </div>
            </div>

            {/* API key */}
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-2">
                <KeyRound size={13} style={{ color: CYAN }} />
                <span className="text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>
                  API Key
                </span>
              </div>
              <div className="flex items-center gap-2">
                <code
                  className="flex-1 text-[12.5px] font-mono px-3 py-2 rounded-lg"
                  style={{ background: "rgba(255,255,255,0.04)", border: "1px solid var(--border)", color: "var(--muted)" }}
                >
                  {process.env.NEXT_PUBLIC_MCP_API_KEY ? "••••••••" : "Set MCP_API_KEY env var"}
                </code>
                <button
                  onClick={() => copy("YOUR-API-KEY", "apikey")}
                  className="shrink-0 p-2 rounded-lg transition-colors hover:bg-white/5"
                  style={{ border: "1px solid var(--border)", color: copied === "apikey" ? GREEN : "var(--muted)" }}
                >
                  {copied === "apikey" ? <Check size={14} /> : <Copy size={14} />}
                </button>
              </div>
            </div>
          </div>

          {/* Test connection */}
          <div className="mt-4 flex items-center gap-3">
            <button
              onClick={async () => {
                try {
                  const res = await fetch("/api/mcp/health");
                  const data = await res.json();
                  copy(JSON.stringify(data, null, 2), "test");
                } catch {
                  /* ignore */
                }
              }}
              className="inline-flex items-center gap-2 text-[12px] font-semibold px-4 py-2 rounded-lg transition-all hover:-translate-y-px"
              style={{ background: CYAN, color: "var(--bg)" }}
            >
              <Zap size={13} />
              Test connection
            </button>
            <span className="text-[11px] font-mono" style={{ color: "var(--faint)" }}>
              GET /api/mcp/health → status check
            </span>
          </div>
        </div>
        </PaidFeatureGate>

        {/* ── AI Agents grid ── */}
        <h2 className="text-[11px] font-mono uppercase tracking-[0.1em] mb-4" style={{ color: "var(--faint)" }}>
          AI Agents
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 mb-10">
          {AGENTS.map((agent) => (
            <AgentCard
              key={agent.id}
              agent={agent}
              active={activeAgentId === agent.id}
              onClick={() => setActiveAgentId(activeAgentId === agent.id ? null : agent.id)}
              onCopy={copy}
              copied={copied}
            />
          ))}
        </div>

        {/* ── Active agent detail ── */}
        {activeAgent && (
          <div className="mb-10 rounded-2xl overflow-hidden" style={{ background: "var(--surf)", border: `1px solid ${activeAgent.accent}40` }}>
            <div className="p-5 border-b" style={{ borderColor: "var(--border)" }}>
              <div className="flex items-center gap-3">
                <span
                  className="w-10 h-10 rounded-xl flex items-center justify-center text-[14px] font-display font-bold"
                  style={{ background: `${activeAgent.accent}14`, border: `1px solid ${activeAgent.accent}40`, color: activeAgent.accent }}
                >
                  {activeAgent.avatar}
                </span>
                <div>
                  <h3 className="text-[16px] font-semibold">{activeAgent.name}</h3>
                  <p className="text-[12px]" style={{ color: "var(--muted)" }}>{activeAgent.tagline}</p>
                </div>
                {activeAgent.docsUrl && (
                  <a
                    href={activeAgent.docsUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="ml-auto inline-flex items-center gap-1 text-[11px] font-mono px-2.5 py-1 rounded-md"
                    style={{ background: "rgba(255,255,255,0.04)", border: "1px solid var(--border)", color: "var(--muted)" }}
                  >
                    Docs <ExternalLink size={10} />
                  </a>
                )}
              </div>
            </div>

            <div className="p-5">
              {/* Config file path */}
              <div className="mb-4">
                <span className="text-[10px] font-mono uppercase tracking-wider block mb-2" style={{ color: "var(--faint)" }}>
                  Config file
                </span>
                <code className="text-[11.5px] font-mono px-3 py-2 rounded-lg block" style={{ background: "rgba(255,255,255,0.03)", border: "1px solid var(--border)", color: activeAgent.accent }}>
                  {activeAgent.configFile}
                </code>
              </div>

              {/* Config snippet */}
              <div className="mb-4">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-[10px] font-mono uppercase tracking-wider" style={{ color: "var(--faint)" }}>
                    Config snippet
                  </span>
                  <button
                    onClick={() => copy(activeAgent.configSnippet, "snippet")}
                    className="inline-flex items-center gap-1 text-[10px] font-mono px-2 py-1 rounded-md transition-colors hover:bg-white/5"
                    style={{ border: "1px solid var(--border)", color: copied === "snippet" ? GREEN : "var(--muted)" }}
                  >
                    {copied === "snippet" ? <><Check size={10} /> Copied</> : <><Copy size={10} /> Copy</>}
                  </button>
                </div>
                <pre
                  className="text-[11.5px] font-mono p-4 rounded-lg overflow-x-auto"
                  style={{ background: "rgba(0,0,0,0.3)", border: "1px solid var(--border)", color: "var(--text)" }}
                >
                  {activeAgent.configSnippet}
                </pre>
              </div>

              {/* Steps */}
              <div>
                <span className="text-[10px] font-mono uppercase tracking-wider block mb-3" style={{ color: "var(--faint)" }}>
                  Setup steps
                </span>
                <ol className="flex flex-col gap-2">
                  {activeAgent.steps.map((step, i) => (
                    <li key={i} className="flex items-start gap-3">
                      <span
                        className="w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-mono font-semibold shrink-0 mt-0.5"
                        style={{ background: `${activeAgent.accent}14`, border: `1px solid ${activeAgent.accent}40`, color: activeAgent.accent }}
                      >
                        {i + 1}
                      </span>
                      <span className="text-[13px] leading-[1.6]" style={{ color: "rgba(255,255,255,0.75)" }}>{step}</span>
                    </li>
                  ))}
                </ol>
              </div>
            </div>
          </div>
        )}

        {/* ── Tool browser ── */}
        <h2 className="text-[11px] font-mono uppercase tracking-[0.1em] mb-4" style={{ color: "var(--faint)" }}>
          Tool Catalog ({TOOL_COUNT} tools)
        </h2>
        <div className="relative mb-4">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "var(--faint)" }} />
          <input
            type="text"
            value={toolSearch}
            onChange={(e) => setToolSearch(e.target.value)}
            placeholder="Search tools by name, product, or description…"
            className="w-full text-[13px] pl-9 pr-3 py-2.5 rounded-lg outline-none"
            style={{ background: "rgba(255,255,255,0.03)", border: "1px solid var(--border)", color: "var(--text)" }}
          />
        </div>

        <div className="flex flex-col gap-4">
          {Object.entries(toolsByProduct).map(([product, productTools]) => (
            <div key={product} className="rounded-xl overflow-hidden" style={{ background: "var(--surf)", border: "1px solid var(--border)" }}>
              <div className="px-4 py-3 flex items-center gap-2 border-b" style={{ borderColor: "var(--border)" }}>
                <span className="w-2 h-2 rounded-full" style={{ background: CYAN }} />
                <span className="text-[13px] font-semibold capitalize">{product}</span>
                <span className="text-[10px] font-mono px-2 py-0.5 rounded-md" style={{ background: CYAN_DIM, color: CYAN }}>
                  {productTools.length}
                </span>
              </div>
              <div className="divide-y" style={{ borderColor: "var(--border)" }}>
                {productTools.map((t) => (
                  <div key={t.name} className="px-4 py-3 flex items-start gap-3" style={{ borderColor: "var(--border)" }}>
                    <code className="text-[11.5px] font-mono shrink-0" style={{ color: CYAN }}>{t.name}</code>
                    <span className="text-[12px] leading-[1.5] flex-1" style={{ color: "rgba(255,255,255,0.6)" }}>{t.description}</span>
                    <span
                      className="text-[9px] font-mono px-1.5 py-0.5 rounded shrink-0"
                      style={{
                        background: t.readOnly ? "rgba(52,211,153,0.1)" : "rgba(251,191,36,0.1)",
                        color: t.readOnly ? GREEN : "#FBBF24",
                      }}
                    >
                      {t.readOnly ? "READ" : "WRITE"}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function AgentCard({
  agent,
  active,
  onClick,
  onCopy,
  copied,
}: {
  agent: AgentConfig;
  active: boolean;
  onClick: () => void;
  onCopy: (text: string, id: string) => void;
  copied: string | null;
}) {
  return (
    <button
      onClick={onClick}
      className="text-left rounded-xl p-4 transition-all"
      style={{
        background: active ? `${agent.accent}10` : "var(--surf)",
        border: active ? `1px solid ${agent.accent}50` : "1px solid var(--border)",
      }}
    >
      <div className="flex items-center gap-3 mb-2">
        <span
          className="w-9 h-9 rounded-lg flex items-center justify-center text-[12px] font-display font-bold shrink-0"
          style={{ background: `${agent.accent}14`, border: `1px solid ${agent.accent}30`, color: agent.accent }}
        >
          {agent.avatar}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="text-[13px] font-semibold truncate">{agent.name}</span>
            {agent.free && (
              <span className="text-[8px] font-mono px-1.5 py-0.5 rounded" style={{ background: "rgba(52,211,153,0.1)", color: GREEN }}>
                FREE
              </span>
            )}
          </div>
          <div className="text-[10px] font-mono" style={{ color: "var(--faint)" }}>{agent.transport}</div>
        </div>
        <ChevronDown
          size={14}
          className="shrink-0 transition-transform"
          style={{ color: "var(--faint)", transform: active ? "rotate(180deg)" : "none" }}
        />
      </div>
      <p className="text-[11.5px] leading-[1.5]" style={{ color: "var(--muted)" }}>{agent.tagline}</p>
    </button>
  );
}
