# 05 — Product-Market Fit

> 📊 Strategy · 💰 Investor — three-tier ICP, beachhead, and expansion path.
> Harmonizes with `docs/paper/PRODUCT_DIRECTIONS.md` (P1, P5, P6) — does not
> duplicate it.

---

## Current ICP reality

OhhO today:
- **4 shipping modules** (Connect, Data, Train, Serve)
- **0 named customers**
- **0 named partners**
- **4–10 eng team**
- **Bootstrapped, $0 ARR**

This is **not a product-market-fit stage**; this is a *product-build-fit stage*.
PMF requires evidence — the loop is open, but the engine has not run on a real
customer engine.

Honesty first: OhhO has product **concept** fit on a 19-product roadmap, not
product-market fit on a paying customer cohort.

---

## Three-tier ICP (sequential, not parallel)

Each tier is a year of company time. **Do not run them in parallel** — that is
the "fully horizontal" mistake.

### Tier 1 — Today (year 0–1): Frontier research labs + open-source robot hardware brands

| | |
|---|---|
| **ICP** | University labs + frontier AI research teams + open-robot hardware brands (Trossen Community, SO-101 ecosystem, open-source quadruped hackers, Unitree G1 academic users reaching limits of the in-box example SDKs) |
| **Wedge product** | Connect (drive any robot from browser) → Data (record LeRobot episodes) → Train (fine-tune SmolVLA/OpenVLA) → Serve (deploy REST endpoint) — the 4 shipped products |
| **Use case** | "I bought a Unitree G1 + Trossen arm. I want to: collect 100 demos in proper LeRobot format, fine-tune π0/SmolVLA, push the policy back to the robot. Today I assemble this myself from HF LeRobot + Modal + vLLM + LangChain; OhhO gives me a coherent 4-product pipeline" |
| **Plan tier** | Builder ($49/mo) |
| **ARPA** | ~$588 |
| **Volume** | High signal — first 100 paid Builder seats come from this. |
| **Margin** | Negative-zero. Cloud GPU cost at 20 hours/month/customer exceeds $49. **This is intentional** — the wedge is supposed to lose money to acquire users. |
| **PMF signal** | # of Builder seats → Builder→Fleet upgrades/month; # of community PRs against `sdk/ohho`; # of OHhO authors on HF LeRobot writing in the format |
| **Build-now** | The wedge must include OhhO Pilot mobile (free tier today) so researchers can teleop-collect without buying hardware. The Pilot→Data→Train→Serve loop needs to be 10-minutes-to-first-policy, matching `PRODUCT_DIRECTIONS.md` P1. |

### Tier 2 — Next (year 1–2): Open-software humanoid brands + small/mid industrial integrators

This is where revenue starts. Two adjacent customers sit here:

#### 2A: Open-robot hardware brands with weak brains
| | |
|---|---|
| **ICP** | Robot brands whose differentiation is *open-source hardware*, not *brains* — Trossen's wares, open quadrupeds, open-source 6-DOF arms, certain Unitree branches, retrofit providers. Their hardware is competitive vs Figure's; their brains are *not*. They need an intelligence layer partner who will not lock buyers in (Figure/1X would lock them out). |
| **Wedge product** | **Forge white-label** — OhhO Mind + Serve + Connect bundled as the OS layer of the open hardware brand (with OhhO sight unseen). |
| **Deal size** | $50K–$500K ACV per partner (assume 1–3 partners year 1) |
| **PMF signal** | 3 signed Forge partners shipping co-branded OS images |
| **Build-now** | Forge-tier partnerships must include technical co-positioning — the brand's name on the SDK, OhhO underneath. Marketing: "Open-brain stack from [Brand] x OhhO." |
| **Strategic purpose** | These are the partners the horizontal thesis makes economic sense with. They are OhhO's real revenue engine in year 2. |

#### 2B: Mid-market AMR integrators (agriculture, lab automation, warehouse retrofit, inspection)
| | |
|---|---|
| **ICP** | Integrators buying off-the-shelf AGVs but needing to add vision/language/remote-fleet capability. They have VDA 5050 conformance obligations and/or OPC UA cell integrations in their plant customers' floors. |
| **Wedge product** | **Bridge + Fleet + Twin** for mixed-brand AGVs that must speak the customer's VDA 5050 / OPC UA plant. The industrial-Ethernet Bridge is a real wedge nobody in the humanoid coalitions offers. |
| **Deal size** | $5K–$100K ACV (Forge cert engagements + Fleet seat licenses at $199/robot, × ~10–30 robots) |
| **PMF signal** | 2–5 named customer engagements, at least 1 case study, ideally with a named system integrator (Dematic, Stryker, PickNik if they accept partnerships) |
| **Build-now** | Bridge VDA 5050 adapter + OPC UA adapter + IEC 61131-3 mapping must be production-grade. The current listed adapters need code-level verification. |

### Tier 3 — Later (year 2–3): Enterprise mixed-fleet operators

| | |
|---|---|
| **ICP** | Logistics/warehouse 3PL with mixed AGV fleets from 5+ brands; pharma manufacturing that needs predict-maintenance across heterogeneous arms; facilities with existing Formant fleets ready to embrace OhhO's OSS Bridge; hospitality/cleaning/inspection rollouts |
| **Wedge product** | **Care + Comply + Shield + Proof** — full lifecycle trust stack |
| **Deal size** | $50K–$1M ACV with recurring managed cloud |
| **PMF signal** | 1 named Win (Win = Fortune 500 logos + 2-3 named vertical case studies + journal-quality penetration) |
| **Build-now** | Plans Tier 2 + named UL/TÜV SÜD partnership on Comply; named cybersecurity advisor on Shield; named standards partner on Proof |

---

## Beachhead choice (the one "fully-horizontal" decision to flip in 30 days)

Picking a beachhead forces the user's "fully horizontal any industry" thesis to
become reality grounded in one industry that has dollars-and-use-case. Same
construct as Red Hat winning one vertical (banking/telecoms) before going
horizontal.

**Recommended primary beachhead: warehouse AGV/AMR retrofit.**

Why:
1. The customer base has dollars and a pain point (aging AGV fleets, modern WMS
   expectations, compliance tightening).
2. Bridge + Fleet + Twin directly maps to it (VDA 5050, OPC UA, MQTT).
3. It is *the* market the humanoid labs are ignoring (they don't ship into
   Linde/Toyota/MiR warehouses today — those run Cisco/Rockwell/Siemens
   ecosystems).
4. The retail $199 Fleet plan fits a small AGV fleet operator; the Forge plan
   fits a mid-market integrator or the customer directly.
5. It is laterally adjacent to **open-robot hardware brands** (Tier 2A) and
   extends to **lab automation** (pharma labs with AGVs + Modbus arms) without
   re-positioning.

**Alternative beachhead if AGV proves too slow to land:** Research-and-academia
kit business (or-equivalent to `PRODUCT_DIRECTIONS.md` P5 教育) with named 10–20
university customers on the kit (≈$500 BOM robot + Frame + Pilot + Train). Less
revenue; faster reference-able customer acquisition; stronger OSS-funnel PEM.

> ⚠️ **BRUTAL:** The current "fully horizontal any robot / any industry" thesis
> is the highest-risk profile thesis possible at this stage. The chance
> OhhO gets a single named Forge customer while maintaining that thesis is
> less than 20%. The chance improves to ~60% with a published beachhead and a
> "we'll expand from this vertical" admission externally.

---

## PMF proof points to track monthly

| # | Signal | Tier 1 threshold | Tier 2 threshold | Tier 3 threshold |
|---|---|---|---|---|
| 1 | Builder seats → Fleet upgrades per month | 100 → 5/mo | 500 → 25/mo | 1K+ → 50+/mo |
| 2 | Skills published on Market that are **not built by OhhO** | 0 today (target 10 in Q1, 50 in Q2) | — | — |
| 3 | Robots enrolled in Fleet OTA (the data-gravity proxy) | 5 | 50 | 500+ |
| 4 | Time-to-first-Mind-loop on a **third-party robot** | <60 min | <30 min | <15 min (the marketing claim) |
| 5 | GitHub stars on `ohho-os` repo | 500 | 5,000 | 20,000+ |
| 6 | Named design partner logos on homepage | 0 today | 3 in 6 mo | 8+ in 18 mo |
| 7 | GitHub PRs from outside the founding team | 5/mo | 30/mo | 100/mo |
| 8 | Builder→Fleet conversion rate | 5% (today unclear) | 15% | 25% |

**This table is the monthly heartbeat report.** If the founders enforce nothing
else from this analysis, enforce this table.

---

## What PMF looks like for the customer (concrete customer journey)

This is the Tier 1 → Tier 2 customer journey as the customer experiences it:

```
Day 0: A Unitree G1 developer hears about OhhO OS on HN.
       pip install 'ohho-os[base]'  → drives the robot from the browser
Day 1: Realizes the Go-2 quadruped also works through the same console.
       Records 100 demos in LeRobot format via Pilot mobile (free).
Week 2: Fine-tunes SmolVLA via Train → deploys via Serve at $49/mo Builder.
Month 2: Wants VR teleop → upgrades to Fleet plan ($199/mo).
         Also recruits 2 lab mates; pays for 3 Builder seats = $147/mo
         + 1 Fleet = $346/mo ARR for OhhO from this one lab.
Year 1-3: Lab spinout → opens a startup; funding reaches seed.
          Upgrades to Forge for managed cloud + cert-prep = $50K ACV.
```

That is the wedge: **a research lab → startup → managed cloud.** The escalation
path hooks the Tier 1 customer economically when their funding arrives.

**This story is the pitch deck slide you do not yet have.**