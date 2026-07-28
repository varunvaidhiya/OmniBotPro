# 03 — Moats: Real · Thin · Missing

> 📊 Strategy · 💰 Investor — brutal audit of the four self-claimed moats in
> `website/app/why/page.tsx:19-44`, plus the one moat the company doesn't name
> but should be writing on every deck.

---

## The four self-claimed moats (from `app/why/page.tsx:19`)

| # | Claimed moat | Brutal verdict | Status |
|---|---|---|---|
| 01 | **Data gravity** — *"Every demonstration, skill and evaluation you capture on OhhO compounds — your models get better here, and the dataset is yours to take anywhere… earned, not imposed."* | At Builder level, the platform offers **1,000 episode cloud sync** (`components/Pricing.tsx:44`). Real data gravity needs ~10⁵+ episodes per customer fleet and bespoke fine-tunes that **do not transfer** to other toolchains. Today the dataset is portable (`copy.ts:25` → "Your data, portable forever"), which is **the opposite of a moat** — it is the wedge. | **Claimed, unproven, structurally contradicted** |
| 02 | **Best-operated** — *"The canonical managed cloud, run by the people who build the engine. Like Red Hat with Linux or Vercel with Next.js: the code is free and portable — you stay because we run it best and ship fastest."* | Red Hat won on enterprise SLAs, 24×7 support, military-grade cert, and named customers. **OHhO has no enterprise support tier on the Pricing page, no SLA, no SOC 2, no named customer.** It's a strategy — promise of execution — not an earned moat. To get there: 5+ named Forge references + SOC2 + ISO 27001 + named CSM. | **Strategy, not asset — execution required** |
| 03 | **Open-standard interop** — *"Built on LeRobot, ROS 2 and ONNX — we extend the open ecosystem, we don't wall it. Bring any brain, any robot, any runtime."* | Standards compliance is a **checklist, not a fortress** — anyone can list VDA 5050 or OPC UA on their site. Real moat would require: production-grade PROFINET/EtherNet/IP/EtherCAT implementations that nobody else has shipped OSS. The current `website/lib/bridge/adapters.ts` declares 12 adapters; whether the industrial Ethernet ones are working production code or reference wrappers needs verification. | **Real, rare — when shipped at production quality** |
| 04 | **Human-in-the-loop services** — *"Teleoperation data collection, labeling and evaluation at a cost structure most can't match. A business, not a repo — relationships and scale that code can't fork."* | Scale AI Robotics (post-Covariant) is the dominant competitor here, with PI and Cobot listed as customers and the "book a demo" funnel running. OhhO's structural lever: **Pilot MR teleop paired with Data collection** — fewer coordination overhead, the operator IS the labeler. But Scale has more collectors, more environments, SOC 2 Type II + ISO 27001. | **Genuinely underpriced-competitor edge — but small vs Scale** |

---

## Real moat vs claimed moat (one-table summary)

| Claimed moat | Is it real today? | When does it become real? |
|---|:-:|---|
| Data gravity | ❌ (datasets are portable by design) | When customers reach ~10⁵ episode bespoke fine-tunes; will not happen with retail pricing today |
| Best-operated | ❌ (no enterprise tier exists) | When 5+ named Forge customers + SOC 2 + ISO 27001 + named CSM ship |
| Open-standard interop | 🟡 (real on LeRobot/ROS2/ONNX/VDA5050/MQTT; unverified on industrial Ethernet) | Production-grade PROFINET/EtherNet/IP/EtherCAT OSS releases |
| HITL services | 🟡 (structural lever with Pilot+Data; no service team) | Hire a small HITL/delivery team + 3 named customer engagements |

---

## The unclaimed moat OhhO should be writing on every deck

**#05 — Cross-brand skills marketplace** — the only moat no vertically-integrated humanoid maker can copy without dissolving their own lock-in.

Source: `lib/products.tsx:929` (Market) and `lib/market/mcp-tools.ts`.

| OEM skill store (Figure, 1X, Unitree, Apptronik) | OhhO Market |
|---|---|
| Per-robot by construction | Cross-brand by construction |
| Bundled with hardware | Decoupled from hardware |
| Vendor controls the brain | Vendor-agnostic brain |
| Listing revenue stays in-house | OhhO take-rate + author revenue share |

> *"Cross-brand skill marketplace. Cross-brand, not per-robot. Unlike OEM app
> stores, Market spans every robot the platform supports. A skill tagged for
> 'any mecanum base' works on a mecanum manipulator, a research robot and a
> custom AMR alike."* — `lib/products.tsx:957-958`

### BUT — this moat belongs to whoever ships the catalog first

Hugging Face LeRobot has **188 datasets + 59 models + 11 collections** in their
org page at the time of writing (`huggingface.co/lerobot`, July 28, 2026), with
`lerobot/pi052_base` published **one hour ago** (yes, this hour).

The LeRobot catalog format today is `(dataset, model_checkpoint)` — not the
OhhO bundle `(signed_policy_package, Proof_scenario_results, Shield_signature,
marketplace_listing)`. **That bundle format is OhhO's actual moat** — but only
if it ships with real inventory before LeRobot extends their format to include
scenarios + signatures.

### Why this is OhhO's strongest moat

1. **Network-effect compounding:** every skill on Market makes Market more
   valuable; this is the textbook platform-moat.
2. **Bundled format is differentiated:** Proof + Shield + signed package is
   not what HF LeRobot publishes today.
3. **Cross-brand robot support** is the structural feature OEM stores can't
   match.

### Why this moat is fragile today

1. **Catalog inventory is the moat; format is not.** HF LeRobot has the
   inventory; OhhO has the format.
2. **Cold-start costs ~$50K–$100K** (20–30 paid skills at $1K–$5K each), less
   than one engineer-month of script-writing time per skill author. But the time
   cost of NOT seeding is "lose to HF LeRobot inside 6 months."
3. **Take-rate is two-sided:** 15-25% take-rate at zero velocity today; it
   becomes real only when buyers + sellers both commit. **OHhO must seed both
   sides** — first 20–30 skills as grants (no fee), first 50 paid buyers via
   Forge partnership pipelines.

---

## Missing moats (the ones OhhO does not have today and competitors do)

| Missing moat | Competitor with it | What it would take to build |
|---|---|---|
| Production customer references | Formant (~10 years), Apptronik (Mercedes, GXO, Jabil), Figure (Helix), 1X (NEO HUD) | Land 1–3 named design partners in 90 days, ship a case study on the homepage (`about/page.tsx` should have `Case Studies` section like formant.com) |
| Enterprise certifications | Scale AI (SOC2 Type II + ISO 27001); Formant (10 years compliance); Apptronik (multi-tier partnerships) | SOC 2 Type II in 6-9 months; ISO 27001 in 12 months; named security/compliance owner on hiring roadmap |
| Distribution-scale foundation model | Skild, PI, Figure Helix, Apptronik×DeepMind, 1X (NEO) | Structural gap; cannot close from bootstrapped base. Concede this battlefield; partner with one (Skild, PI, Figure open branch) |
| Catalog inventory | HF LeRobot (188 datasets shipped) | Seed Marketplace in 90 days; partner w/ at least 1 HF LeRobot publisher |
| Public GTM muscle | Apptronik (CNBC Disruptor 50, Forbes, The Information); Scale AI (white glove service); Formant (boot camp forward-deployed engineers) | Hire 1 ex-Red-Hat-style head of solutions engineering with named enterprise OSS GTM practiced |

---

## Brutal final moat ranking

If OhhO executes the [08-12-month-plan.md](./08-12-month-plan.md) cleanly, in
24 months these are the ONLY moats that will be **real and earned**:

1. 🥇 **Cross-brand Market bundle format with inventory** — the OSS Switzerland
   moat (Skild/PI/HF LeRobot brains run through the OhhO operational funnel).
2. 🥈 **Forge-as-managed-cloud + named Forge customers** — the Red Hat
   earned-credibility moat.
3. 🥉 **Bridge industrial-Ethernet depth** — the Bosch/Siemens/Rockwell-agnostic
   moat nobody in the humanoid race has yet.
4. **Comply-as-cert-prep-with-named-UL-or-TÜV partnership** — the regulatory
   chokepoint moat.
5. **HITL teleop-as-data-pipeline (Pilot+Data)** — the moat that competitors
   (Scale) lack because their data is abstracted from operator context.

Everything else today — including all four self-claimed moats on `app/why` — is
a promise, not an asset.

> See [07-red-team.md](./07-red-team.md) for the conditions under which even
> these real moats fail to compound.