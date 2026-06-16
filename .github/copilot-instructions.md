## graphify

For any question about this repo's architecture, structure, components, or how to add/modify/find
code, orient with the code knowledge graph before grepping — preferring, in order (this is a
preference, **not a hard gate**; fall back to normal search if none are available):

1. **CodeGraphContext MCP** — portable, works in every IDE. Preferred.
2. **`graphify` CLI** — `graphify query "<question>"`, `graphify path "<A>" "<B>"`,
   `graphify explain "<concept>"`. Only if installed locally (private pip package; absent in CI,
   fresh clones, and non-Windows machines).
3. **Read the data directly** — `graphify-out/graph.json`, or `graphify-out/GRAPH_REPORT.md`
   for broad architecture review.

Triggers: "how do I…", "where is…", "what does … do", "add/modify a <component>",
"explain the architecture", or anything that depends on how files or classes relate.

See `docs/AI_TOOLING.md` for the full cross-IDE tooling setup. Read source files directly when
(a) modifying/debugging specific code, (b) the graph lacks the needed detail, or (c) no graph
tool is available.
