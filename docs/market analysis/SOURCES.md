# Sources

Every claim in this market analysis is traceable to either a `website/` file,
a root-repo file, or a public external URL. Reading the source files listed
here is the way to verify any brutal verdict.

---

## OhhO primary sources (from this repository)

### Top-of-tree strategy files

| File | Section reference | Used in |
|---|---|---|
| `website/lib/products.tsx` | `PRODUCT_STATUS` map (lines 109–129); `PRODUCTS` array (lines 145+); individual product entries cited inline | 01, 03, 04, 05, 06, 07, 09 |
| `website/lib/copy.ts` | `HERO_TRUST`, `HERO_COPY`, `WEDGE_ONELINER`, `STATS`, `PRODUCTS_INTRO` | README, 01, 09 |
| `website/AGENTS.md` | §1 core principles; §2 the rules; §3 standards reference (15 comms, 11 safety, 6 data, 3 sim); §4 per-product build prompts; §6 adding a new standard | 01, 03, 04, 09, 10 |
| `website/app/page.tsx` | Home page assembly (Hero → StatsBar → BrandAgnostic → OhhoOS → Lifecycle → Products → Pricing → CtaBanner) | 01 |
| `website/app/about/page.tsx` | "Best companies building with OhhO" sentence (line 30); `[Team Photo Placeholder]` (line 68) | README, 01, 07 |
| `website/app/why/page.tsx` | `MOAT` array (lines 19–44); the "open is the wedge" framing (lines 60–75); the "what you keep and what you lose" callout (lines 134–152) | 03, 09 |
| `website/app/os/page.tsx` | `FEATURE_PILLARS`, `DIFFERENTIATORS`, `RUNTIME_ROWS`, `ROBOT_CATEGORIES`, `ADAPTERS` | 01, 04 |
| `website/app/mind/page.tsx` | Route definition + metadata | 04 |
| `website/app/news/ohho-mind/page.tsx` | Mind launch article; "reflex is not a mind," "two-speed cognition," "delivered over the air" | 04 |
| `website/app/news/omnivla-engine/page.tsx` | OmniVLA open-source launch article (June 7, 2026) | 04 |
| `website/app/why/page.tsx` | Lines 19–44 (claimed moats), 60–75 (the wedge), 134–152 (the moat honest summary) | 03, 09 |
| `website/components/Hero.tsx` | Hero assembly + A/B experiment reference + track() | 01 |
| `website/components/OhhoOS.tsx` | `ROBOT_CHIPS`, `DIFFERENTIATORS`, `PRODUCT_CHIPS` (lines 52–53); the umbrella visualization | 04 |
| `website/components/StatsBar.tsx` | `STATS` content pulled from `lib/copy.ts` | 01 |
| `website/components/BrandAgnostic.tsx` | `LAYERS` (Bring your own ___); `NEVER_LOCKED` strip; the "closed walled-garden aren't the target" line (line 162) | 04, 09 |
| `website/components/Lifecycle.tsx` | `STAGES` array; "cheapest price in robotics" line (line 201) | 01, 06, 07 |
| `website/components/Products.tsx` | `ORDERED_PRODUCTS` sort, `StatusChip`, `ProductCard` | 01 |
| `website/components/Pricing.tsx` | `plans` array (lines 11–92); "Open source platform. Pay only for cloud infrastructure" framing | 01, 06, 09 |
| `website/lib/bridge/mcp-tools.ts` | Bridge MCP tool definitions (referenced from AGENTS.md) | 03, 08 |
| `website/lib/market/mcp-tools.ts` | Market MCP tools (referenced from AGENTS.md) | 03 |
| `website/lib/comply/standards.ts` | Comply standards registry (referenced from AGENTS.md §6) | 03, 07 |
| `docs/paper/PRODUCT_DIRECTIONS.md` | Part 0 ("19 modules is the strategy risk"), P1-P9 product-direction picks, G1-G8 growth paper strategy | 05, 06, 08 |

### Per-product doc files
- `website/docs/products/*.md` — one per `lib/products.tsx` entry; mirror them.
- `website/docs/ohho-os/*.md` — OhhO OS engine docs (setup, runtimes, robots,
  navigation, training, memory, skills, hostname, install, architecture).
- `website/docs/video-prompts/*.md` — marketing video stubs (useful for
  future proof-of-quality evidence).

### Codebase backing (per `docs/paper/PRODUCT_DIRECTIONS.md:19`)
- `sdk/ohho` = OS
- `data_engine` = Data
- `learning_engine` = Train
- `vla_serve` / `vla_engine` = Serve
- `agent_engine` = Mind
- `learning_engine/verification` = Shield / Proof
- `infra/observability` = Fleet / View
- `vr_app` = Pilot
- `digital_twin` = Twin
- `sdk/ohho/market.py` = Market
- `robot_ws/src/omnibot_orchestration/langchain_agent_node.py` = Mind
  orchestrator

---

## External sources (competitor one-pagers in `10-competitor-teardowns.md`)

All fetched July 27–28, 2026.

| # | Competitor | Source URL | Source date | Used in |
|---|---|---|---|---|
| 1 | Hugging Face LeRobot | https://huggingface.co/lerobot | July 28, 2026, 18:00 UTC | 02, 10 |
| 2 | Physical Intelligence | https://www.physicalintelligence.company/blog | July 28, 2026 | 02, 10 |
| 3 | Figure | https://www.figure.ai/ | July 28, 2026 | 02, 10 |
| 4 | 1X (NEO) | https://www.1x.tech | July 28, 2026 | 02, 10 |
| 5 | Formant | https://www.formant.ai | July 27, 2026 | 02, 10 |
| 6 | Skild AI | https://www.skild.ai/ | July 28, 2026 | 02, 10 |
| 7 | Scale AI Robotics | https://scale.com/robotics | July 28, 2026 | 02, 10 |
| 8 | Apptronik | https://apptronik.com/ | July 28, 2026 | 10 |
| 9 | Unitree | https://www.unitree.com/ | July 28, 2026 | 10 |

---

## Methodology ↔ ohho-robotics.com positioning alignment

The marketing analysis in README + 01–09 files is source-checked against
the company's own published positioning. The **internal honesty** of the
catalog (`PRODUCT_STATUS` in `lib/products.tsx:109`) is more conservative
than its **marketing-label outward messages** in `app/about/page.tsx` and
`components/Lifecycle.tsx`. The brutal verdicts in the Red Team largely
target the marketing outward messages, not the internal honest labels
(which are correctly transparent about beta vs planned vs available).

### Forward-looking claims acknowledged as honest company statements
The company explicitly describes its moats as forward-looking in
`app/why/page.tsx:103`:

> *"Come try it — it's safe, you can always leave. Great for getting in the
> door. Terrible as the whole strategy, because it lowers the barrier to leave,
> too."*

This self-awareness is unusually strong for a robotics startup. The Red Team
file [07-red-team.md](./07-red-team.md) takes that self-awareness at face
value and pushes it one step further: the self-aware line is not enough; the
company still ships the incorporating marketing lines that contradict it.
The recommendation is to harmonize marketing copy with the executive's own
admission — not to add more lines.

---

## How to verify any verdict in this folder

1. Open the cited source file in the repo (e.g. `website/lib/products.tsx`).
2. Find the cited line range (e.g. lines 109–129 for `PRODUCT_STATUS`).
3. Compare the cited claim ("4 of 19 products ship") with the data in the
   file.
4. If the claim does not hold, it's a factual error: open an issue or PR
   against this folder; we will correct it.

External competitor citations include the fetch date for the same reason —
competitor sites change continuously. Re-fetched snapshots would be
appreciated as issue reports.

---

## Use and reuse

This document is internal-to-repo analysis. It is not press-release-grade
material:

- Investor pitch deck material: pull from README and 06
- Engineering plan: pull from 08
- Marketing copy revisions: pull from 07 + 09 (specific lines cited)
- Customer-facing partner pitch: pull from 10

If you copy from this folder into external marketing, **remove the brutally
honest verdicts** that contradict current capability (e.g., "4 of 19 ship",
"0 named customers"). Those verdicts are true internally, and they will
stop being true if executed — but they are not yet marketing-grade.