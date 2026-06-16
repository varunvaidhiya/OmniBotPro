## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community
structure, and cross-file relationships. See `docs/AI_TOOLING.md` for the full
cross-IDE setup.

Orient with the graph before grepping, in this order of preference (a preference,
**not a hard gate** — fall back to normal exploration if none are available):
- **CodeGraphContext MCP** (preferred; portable, works in every IDE).
- **`graphify` CLI** — `graphify query "<question>"`, `graphify path "<A>" "<B>"`,
  `graphify explain "<concept>"`. Only if installed locally (private pip package).
- **Read the data directly** — `graphify-out/graph.json`, or
  `graphify-out/GRAPH_REPORT.md` for broad architecture review.

After changing code, refresh via the CodeGraphContext indexer (or `graphify
update .` if installed).
