# AI Tooling — Cross-IDE Setup for OmniBot / OhhO

This repo is developed with multiple AI coding agents (Claude Code, Cursor,
Codex, Google Antigravity, Windsurf, Gemini CLI, Copilot). This document is the
**single source of truth** for the shared, portable tooling that makes those
agents fast and accurate on a large polyglot monorepo (ROS 2 Python, C++,
Kotlin, TypeScript/Next.js).

> TL;DR: orient with a **code knowledge graph** before grepping, feed agents
> **live library docs**, give them **semantic edit** tools, and keep one set of
> instructions in `AGENTS.md`. All of it runs over **MCP**, so it works in every
> IDE — not just one.

---

## Background: what happened to `graphify`

`graphify` is a code knowledge-graph generator. Its committed output lives in
`graphify-out/` (`graph.json` = 4,034 nodes / 6,875 links, plus `GRAPH_REPORT.md`).
That data is genuinely useful and stays in the repo.

**But the `graphify` *command* is not portable.** It is a private pip package
installed only on the maintainer's Windows machine
(`.codex/hooks.json` referenced `…\Python312\Scripts\graphify.EXE`). It is **not**
on public PyPI, so it is absent in:

- fresh clones / new contributors,
- CI,
- this repo's cloud dev sessions,
- any non-Windows machine.

That is why agent rules that said *"MANDATORY: you MUST run `graphify query`"*
were failing — they pointed at a command that only exists on one laptop. The
rules have been changed to **prefer** the graph and **degrade gracefully**, and
we added a **portable, MCP-native** code graph (`CodeGraphContext`) that works in
every IDE.

### Three ways to use the graph (in order of preference)

1. **CodeGraphContext MCP** — portable, cross-IDE. Wired in `.mcp.json` /
   `.cursor/mcp.json` / `.codex/config.toml`. Preferred.
2. **`graphify` CLI** — `graphify query "<q>"`, `graphify path "<A>" "<B>"`,
   `graphify explain "<concept>"`, `graphify update .`. Only if installed locally.
3. **Read the data directly** — `graphify-out/graph.json` or `GRAPH_REPORT.md`.
   Always available; no tool required.

If none are available, just use normal Read/Grep/Glob. The graph is an
accelerator, not a gate.

---

## Wired MCP servers

These are defined once and shared across IDEs. None require a paid key
(Context7 has an optional key for higher rate limits).

| Server | What it does | Why it matters here | Prereq | API key |
|---|---|---|---|---|
| **codegraphcontext** | Indexes the repo into a local graph DB; serves structural queries (callers, dependencies, definitions) over MCP. Portable `graphify` replacement. | Orient before grepping across ROS 2 + Kotlin + TS without burning tokens. | `pip install codegraphcontext` (embedded DB, no Neo4j needed) | none |
| **context7** | Injects up-to-date, version-specific library docs into the prompt. | Stops hallucinated APIs for fast-moving deps: ROS 2 Jazzy, Nav2, LeRobot/SmolVLA, Isaac Lab, `transformers`, Next.js 15. | Node (`npx`) | optional `CONTEXT7_API_KEY` |
| **serena** | LSP-based **semantic** code retrieval *and editing* at the symbol level. | Safe cross-language refactors instead of text-munging. | [`uv`](https://github.com/astral-sh/uv) | none |
| **playwright** | Drives a real browser (navigate, click, screenshot). | Test the Next.js site + the OhhO product dashboards end-to-end. | Node (`npx`) | none |

### One-time prerequisites

```bash
pip install codegraphcontext          # code graph
# uv (for serena):  https://docs.astral.sh/uv/getting-started/installation/
# Node 18+ (for npx-based servers): https://nodejs.org
```

### First index (CodeGraphContext)

```bash
codegraphcontext mcp setup     # optional: auto-writes IDE config
# then, from your agent, ask it to index the repo once; re-index after big changes
```

---

## Enabling per IDE

| IDE | How it picks up the servers |
|---|---|
| **Claude Code** | Auto-loads `.mcp.json` at repo root (this file is committed). Approve servers on first run. |
| **Cursor** | Auto-loads `.cursor/mcp.json` (committed). Enable in Settings → MCP. |
| **Codex** | Reads `~/.codex/config.toml` (per-user). Copy the `[mcp_servers.*]` blocks from the committed `.codex/config.toml`. |
| **Google Antigravity** | Add the same server commands via its MCP config (Settings → MCP / config file). Antigravity gained MCP support in early 2026. |
| **Windsurf / VS Code (Copilot, Cline)** | Add the same `mcpServers` block to the client's MCP settings. |

Instructions for all of them live in **`AGENTS.md`** (the cross-tool standard,
read by 30+ agents). `CLAUDE.md`, `GEMINI.md`, `.github/copilot-instructions.md`,
`.cursor/rules/`, and `.codex/` mirror it for their respective tools.

---

## Optional add-ons (not wired by default)

- **Shared memory** (`agentmemory` or **Mem0** MCP) — one persistent project
  memory across agents, so decisions made in Claude are remembered in Antigravity.
  Mem0 needs an API key; add it if cross-tool memory is worth the dependency.
- **Spec-driven development** (**GitHub Spec Kit**, **BMAD-METHOD**) — Specify →
  Plan → Tasks → Implement. Best path to build the not-yet-implemented Trust
  products (Comply, Shield, Proof) without "vibe coding."
- **AI code review** — **CodeRabbit** (free tier, PR-level) or **Greptile**
  (full-codebase context, highest measured bug-catch). Worth it for
  safety-critical robot control code.
- **UI generation** — **v0** (Vercel) / **Magic MCP (21st.dev)** for shadcn-based
  components — useful for OhhO Build's designer and the product/dashboard pages.

---

## How the tools map to the OhhO products

| Product (repo area) | Tools that accelerate it |
|---|---|
| **Build** — 3-D web designer (`website/`) | v0 / Magic MCP (UI), Context7 (react-three-fiber/Next), CodeGraphContext (URDF/`omnibot_description`) |
| **Frame** — ROS 2/Docker/sim foundation (`robot_ws`, `infra`, `digital_twin`) | CodeGraphContext + Serena (navigate workspace), Context7 (ROS 2 Jazzy/Nav2/colcon) |
| **Serve** — VLA REST API (`vla_serve`, `vla_engine`) | Context7 (FastAPI/transformers), Serena (refactor backends), code review |
| **View** — BEV fusion (`packages/ros2_bev_stitcher`) | CodeGraphContext (trace stitcher), Context7 (OpenCV) |
| **Data** — LeRobot pipeline (`data_engine`, `lerobot_engine`) | Context7 (LeRobot/HF datasets), Serena |
| **Pilot** — VR + Android teleop (`android_app`, `vr_app`) | CodeGraphContext (cross Kotlin↔ROS), Context7 (ROSBridge/Unity) |
| **Fleet** — OTA + observability (`omnibot_ota`, `omnibot_metrics`, `infra/observability`) | Context7 (Prometheus/Grafana), GitHub MCP, code review |
| **Comply** — safety standards → docs | Spec Kit / BMAD (standards as specs), Context7 |
| **Shield** — robot security | Greptile + CodeRabbit + SBOM/secret scanning |
| **Proof** — sim testing / safety case | Playwright MCP (web flows), Spec Kit (scenario specs), CodeGraphContext |

---

## Security note (on-brand: you ship OhhO Shield)

Treat third-party MCP servers as supply chain. Independent scans have found
issues in a majority of popular MCP servers, and some UI-generation servers have
had unpatched prompt-injection advisories. Therefore:

- Prefer **local-first / vendor-maintained** servers (the four wired here are
  local or widely-used: CodeGraphContext, Serena, Context7, Playwright).
- **Pin versions** rather than floating `@latest` once you settle on a setup.
- Review a server's source/permissions before adding it, especially anything
  that can write files or reach the network.
