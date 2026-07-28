# OhhO Robotics — Market Analysis

> Brutal-honesty market analysis of ohho-robotics.com, grounded in primary-source
> research of the `website/` folder (homepage, `/why`, `/os`, `/about`, `/mind`,
> both `/news` articles, all 19 products in `lib/products.tsx`, `Pricing.tsx`,
> `copy.ts`, `AGENTS.md`) plus live triangulation against 9 named competitors
> (Hugging Face LeRobot, Physical Intelligence, Figure, 1X, Formant, Skild,
> Scale AI Robotics, Apptronik, Unitree).
>
> Internal OhhO context (verified with the founder before writing):
> **Bootstrapped · $0 ARR · 0 named customers/partners · 4–10 person engineering
> team · fully-horizontal "any robot / any industry" thesis.**

---

## How to read this folder

Every file is tagged for its primary audience. Read the ones marked for you:

- 📊 **Strategy** — for the founding team making product/investment decisions
- 🛠 **Engineering** — for product/eng leadership deciding what ships next
- 💰 **Investor / partner** — for pitch-deck readiness and external review

Most files include all three tags; the dominant one is listed first.

| # | File | Audience | One-line thesis |
|---|---|---|---|
| 0 | `README.md` *(this file)* | 📊💰 | Brutal verdict up front. |
| 1 | [01-what-ohho-offers.md](./01-what-ohho-offers.md) | 📊🛠 | The 19-product catalog and **honest** ship status. |
| 2 | [02-competitive-landscape.md](./02-competitive-landscape.md) | 📊💰 | Six buyer segments, each threat-rated. Skild + Formant = structural triad. |
| 3 | [03-moats.md](./03-moats.md) | 📊💰 | Audit of the 4 self-claimed moats. Real / thin / missing. |
| 4 | [04-intelligence-vs-hardware-makers.md](./04-intelligence-vs-hardware-makers.md) | 📊🛠 | Structural advantage vs Figure/Unitree/Apptronik — and the reverse. |
| 5 | [05-pmf.md](./05-pmf.md) | 📊💰 | ICP, beachhead, three-tier expansion path. |
| 6 | [06-profitability-positioning.md](./06-profitability-positioning.md) | 💰📊 | Revenue model + math to first $1M ARR. |
| 7 | [07-red-team.md](./07-red-team.md) | 📊 | "OhhO will fail if…" — top failure modes ranked by likelihood. |
| 8 | [08-12-month-plan.md](./08-12-month-plan.md) | 🛠📊 | 7 prioritized gates with kill-criteria. |
| 9 | [09-retention-and-churn.md](./09-retention-and-churn.md) | 📊💰 | Earned vs imposed stickiness — today, in 2 years, at scale. |
| 10 | [10-competitor-teardowns.md](./10-competitor-teardowns.md) | 📊💰 | Per-competitor one-pagers (9 named players). |
| -- | [SOURCES.md](./SOURCES.md) | -- | Citations: every claim links to a `website/` path or external URL. |

---

## Brutal executive verdict

**One paragraph for the top of every deck:**

OhhO has the rarest thing in robotics today: a coherent vision that spans design,
intelligence, deployment, certification, and fleet operations on one open engine,
with standards-native fluency (LeRobot, ROS 2, ONNX, VDA 5050, OPC UA, PROFINET,
EtherNet/IP, URDF, SDF, USD, MCAP, CANopen, MAVLink, Modbus) that no vertically
integrated humanoid maker can match — because integrating with all of those
would dissolve their own lock-in. **But coherence of vision is also an unproven
proof-of-concept:** 4 of 19 products ship, the company is bootstrapped at $0 ARR,
zero customers or partners are named publicly, the team is 4–10 engineers trying
to ship 11 beta and 4 roadmap modules, and the headline moats (data gravity,
operational excellence, "best companies building with us") are **claims, not
assets**. Online competitors (Hugging Face LeRobot, Formant with nearly a decade
in production across 647+ orgs, Skild with Bezos/SoftBank/Amazon backing, and
the same omni-bodied thesis) occupy the exact same strategic seats with
exponentially more runway. **To become profitable**, OhhO must stop selling "19
products and cheap," cut surface to 4 shipping + Bridge + Comply + Market,
land 5 named design partners in the next 90 days, reposition Forge as the actual
revenue engine (managed cloud + named CSM + cert-prep at $25K–$500K ACV), and
seed the cross-brand skills marketplace with a creator-grants program before
HF LeRobot's inventory (already at 188 datasets / 59 models, pi052 published
this hour) makes it uncatchable.

**Three sentences that should be on the founder's wall:**

1. **4 of 19 shipped, 0 customers named.** Either ship the missing 15 or shrink
   the marketing to match what works — don't sell a roadmap like a product.
2. **The moats are forward-looking claims.** Real moats compound without
   permission; today, a customer can leave OhhO in an afternoon — including
   with the dataset (good for trust, bad for defensibility).
3. **Horizontal with no wedge is a death sentence** against Bezos/SoftBank-funded
   clones. Pick a beachhead industry in 30 days, or accept that OhhO becomes a
   research tool that nobody pays for.

---

## The single most under-appreciated asset

The **cross-brand skills marketplace** (`lib/products.tsx:929`) is the only moat
no vertically-integrated humanoid maker can copy without dissolving their own
lock-in. Figure, 1X, Unitree, Apptronik all sell the body+brain together — their
skill store is per-OEM by construction. OhhO Market is cross-brand by
construction. **Hugging Face LeRobot is the competitive equivalent, with 188
datasets + 59 models already indexed.** Either OhhO ships Market with seeded
inventory in 90 days, or this moat becomes someone else's moat.

> See [03-moats.md](./03-moats.md) and
> [10-competitor-teardowns.md](./10-competitor-teardowns.md) for the full audit.

---

## The single most damaging marketing line

> *"Some of the best companies in the world are building the future of AI in the
> physical world with OhhO."* — `website/app/about/page.tsx:30`

At **$0 ARR with 0 named customers**, this is a credibility landmine. Any
serious investor or enterprise buyer's first DD call will ask "great — who?"
and the only truthful answer is "we can't say" — but the question itself reads
as "we don't have any." **Either remove the line, win one anchor customer in
60 days, or rewrite it.** Leaving it as-is costs OhhO the trust of every
experienced operator who reads the about page.

> See [07-red-team.md](./07-red-team.md) for the ranked failure modes.

---

## Decision matrix (what to do this week)

| Decision | Recommended | Why |
|---|---|---|
| Drop "cheapest price in robotics" framing | ✅ Yes | Red Hat (the cited model) didn't win on cheap. Replace with "most open at any price." Source: `components/Lifecycle.tsx:201`. |
| Cut catalog from 19 to 7 active | ✅ Yes | 4–10 eng team cannot ship 19 quality products. Pause 12 of them. |
| Remove "best companies building with OhhO" line | ✅ Yes | Untrue at $0 ARR / 0 named customers. |
| Pick one wedge industry in 30 days | ✅ Yes | "Fully horizontal" against Skild + Formant funding = death sentence. |
| Reposition Forge to managed cloud + cert at $25K+ ACV | ✅ Yes | Today Forge is described as "on-prem white-label." Pivot it to managed cloud + SLA + named CSM = the real revenue. |
| Seed Marketplace with 20–30 skills via $1K–$5K creator grants | ✅ Yes | Cold-start costs less than one engineer-month; HF LeRobot won't wait. |
| Partner with UL or TÜV SÜD for cert brand-moat | ✅ Yes | Forge-hours of integration = enterprise procurement unlock. |
| Hire head of solutions engineering (ex-red-hat-muscle) | ✅ Yes | OSS-to-enterprise GTM is a discipline; current open-source hobbyist traffic won't convert at $100K ACV. |
| Build new product (20th) | ❌ No | Don't extend catalog until 12+ of existing 19 ship at quality. |
| Avoid naming competitors in marketing | ❌ No | The AGENTS rule (`website/AGENTS.md` §2) is good for *hardware* but leaves competitive positioning unsaid. Reverse this for B2B pages only. |

---

## Changelog

| Date | Author | Note |
|---|---|---|
| 2026-07-28 | Opencode market analysis (graphify-assisted) | Initial creation. 11 files + SOURCES. |

Found a factual error? Open an issue or PR — every claim is sourced to either
a `website/` file path or an external URL listed in
[SOURCES.md](./SOURCES.md).