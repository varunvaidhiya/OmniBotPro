---
trigger: always_on
description: Consult the graphify knowledge graph at graphify-out/ for codebase and architecture questions.
---

## graphify

This project has a graphify knowledge graph at graphify-out/.

Orient with the graph before grepping, in this order of preference. This is a
preference, **not a hard gate** — if none are available, use normal exploration.
See `docs/AI_TOOLING.md` for the full cross-IDE setup.
- **CodeGraphContext MCP** (preferred; portable, works in every IDE) — find
  callers, dependencies, and definitions.
- **`graphify` CLI / MCP** — `graphify query "<question>"` (CLI) or `query_graph`
  (MCP); `graphify path "<A>" "<B>"` / `shortest_path`; `graphify explain
  "<concept>"` / `get_node`. Only if installed locally (private pip package).
- **Read the data directly** — `graphify-out/graph.json`, or
  `graphify-out/GRAPH_REPORT.md` for broad architecture review.

After changing code, refresh via the CodeGraphContext indexer (or `graphify
update .` if installed) to keep the graph current.
