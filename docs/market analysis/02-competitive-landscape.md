# 02 — Competitive Landscape

> 📊 Strategy · 💰 Investor — six buyer-segments, threat-rated. Headline
> finding: **three companies occupy the exact strategic seat OhhO claims**, two
> with 10–100× the fuel (Skild, Formant), one with the marketplace posture
> already winning (Hugging Face LeRobot).

---

## The strategic seat OhhO claims

From `lib/copy.ts:36`: *"The open data & deployment layer for robots."*
From `app/why/page.tsx:63`: *"Open is the wedge. Your data is the moat."*
From `components/Lifecycle.tsx:201`: *"One platform. One bill. Any robot, any
brand, any GPU."*

Three competitors use **near-identical language**:

1. **Formant** — *"The operating layer for physical AI"* — ~10 years, 647
   production orgs (per footer of `formant.com` July 27, 2026).
2. **Skild AI** — *"General purpose robotic intelligence* / *omni-bodied brain to
   control any robot for any task"* — SoftBank + Bezos + Amazon + Sequoia
   + General Catalyst + Felicis + Lightspeed + Coatue backing.
3. **Hugging Face LeRobot** — *"State-of-the-art ML for real-world robotics ·
   lowering the barrier for entry so everyone can contribute and benefit from
   sharing datasets and pretrained models"* — fast-mover's marketplace posture,
   188 datasets + 59 models in catalog, hosted on HuggingFace Hub (~1.5M
   platform daily users).

**OhhO's seat is occupied on three sides by better-funded precedents.** That
is not a death sentence (OSS allows multiple winners; e.g. TensorFlow +
PyTorch). It is the **central strategic fact** that must drive every
positioning decision.

---

## Six buyer segments, threat-rated

| # | Buyer segment | Direct competitor to OhhO | OhhO edge (today) | Threat level |
|---|---|---|---|:-:|
| 1 | **VLA model foundation labs** | Physical Intelligence (π0→π0.7), Skild, Figure Helix, 1X Neo, Apptronik + Google DeepMind, Sanctuary Carbon, Covariant→Scale | OmniVLA is OSS = no closed-model lock-in; but π0 weights are OSS too (`huggingface.co/lerobot/pi0_*`) | 🔴 High |
| 2 | **Robot dev platform buyers** | PickNik MoveIt Studio, Intrinsic (Google), Foxglove, Trianam/Cognite, Hugging Face LeRobot | ROS-optional runtime + 19-product lifecycle depth (incl. CIF/regulate tier that competitors lack) | 🟡 Medium |
| 3 | **Fleet ops / OTA buyers** | Formant, Rapyuta, MVTr_dish, Standard Bots, Bosch IoT | VDA 5050 + Open-RMF + Prometheus stack pre-wired | 🔴 High |
| 4 | **MR/VR teleop buyers** | Pollen Robotics Reach, HaptX, Standard Bots, teleop-as-a-service startups | OpenXR-agnostic + cross-robot catalog + skill marketplace intent | 🟡 Medium |
| 5 | **Data/RLHF service buyers** | Scale AI Robotics (post-Covariant), Dataloop, SkyAlliance | LeRobot-native pipeline *with* the teleop capture (Pilot) — closer to the data source | 🟢 Low (today) |
| 6 | **Trust/cert procurement** | UL, TÜV SÜD, DEKRA, SGS, Bureau Veritas | Pre-built software gates (Comply + Proof) that *lead into* those cert bodies instead of competing | 🟢 Low (today) |

The segments with the lowest threat are also the lowest-revenue ones today
(segments 5 and 6 have not shipped). The high-threat segments (1 and 3) are
where the money is.

> Per-competitor one-pagers in
> [10-competitor-teardowns.md](./10-competitor-teardowns.md).

---

## Structural triad: who beats OhhO if OhhO stays the course

### 🔴 Threat 1: Skild AI — same thesis, infinitely better-funded

**Source:** `skild.ai` (July 2026). Headlines:

- *"General Purpose Robotic Intelligence"*
- *"An AI that truly understands the physical world should not be limited by robot
  or task type"*
- *"a unified, omni-bodied brain to control any robot for any task"*
- *"Learning by watching human videos"* — scalable-data approach
- Funded by SoftBank, Bezos, Amazon, Sequoia, General Catalyst, Felicis, Lightspeed,
  Coatue, CRV, SV Angel → capital stack that can fund true OA-scale data collection.

**Why Skild is the strongest free-foundation competitive threat to OhhO:**
- Identical "any robot any task" thesis to OhhO's "any robot" thesis
- Their brain = their proprietary foundation model (walled garden on the model side)
- "Scalable solution to the robotics data problem" — solves the bottleneck OhhO
  is also tied to, with no teleop UX burden
- 100–100× OhhO's capital → can do whatever OhhO can do, faster

**OhhO's structural lever vs Skild:** Skild's brain is closed weights. Their data
flywheel is theirs. OhhO's *operational* layer (Bridge → Frame → Fleet → Market
+ Comply + Shield + Proof) is the **operating layer**, not the *brain* layer.
Skild must plug into an operating layer to deploy; OhhO wants to be that layer.

**But:** Skild has not yet announced that operating layer partnership. The moment
they do — with Formant, or with HF LeRobot, or with Apptronik — OhhO's window
closes. **OhhO has ~6–12 months to be the named operating partner of choice.**

### 🔴 Threat 2: Formant — already shipping the operating layer

**Source:** `formant.com` (July 27, 2026). Headlines:

- *"We make physical AI succeed in the real world — beyond the demo."*
- *"The operating layer for physical AI"* — same seat as OhhO
- *"Cumulative across 644 production organizations"* — 647+ verified in their
  footer; case studies published (BP, Burro, Hullbot, Cala)
- *"Pilots rarely fail on capability. They fail on contact with unstructured
  reality"* — creates the credibility moat OhhO's "best companies building with
  us" line destroys.
- 1.98M operating hours, 32M+ events identified, autonomous miles tracked — real
  production telemetry at scale.
- "Forward-deployed engineers" leg in their service model = Red-Hat-style GTM
  muscle executed correctly.

**Why Formant is the strongest operating-layer competitive threat to OhhO:**
- Has been doing what OhhO is *planning* for ~10 years.
- Has named case studies while OhhO has zero named customers.
- Has a real "we run it best" provenance (the Red Hat analogy OhhO claims).
- Direct competitor to OhhO Fleet + Care + Twin.

**OhhO's structural lever vs Formant:** Formant is proprietary OS-as-a-service;
it's not OSS. Their integration is "we'll do it for you" (services-led), not
self-serve OSS that an open-robot community can extend freely. **The OSS wedge
is OhhO's only durable defense** — and only if OhhO defends it with code
velocity and a community, not marketing language.

### 🔴 Threat 3: Hugging Face LeRobot — the marketplace OhhO hasn't built yet

**Source:** `huggingface.co/lerobot` (July 28, 2026). Headlines:

- 188 datasets, 59 models, 11 collections in their org page
- **`lerobot/pi052_base` published an hour ago** at time of writing (July 28,
  2026 6 PM UTC) — HF LeRobot has been the omni-VLA-weights distribution point
  for Physical Intelligence's π0 family for two years
- Hosted on HuggingFace Hub — the largest open ML community on the planet (~1.5M
  daily users, hundreds of thousands of robotics-curious ML grads)
- Includes VLA-JEPA, FastWAM, pi052, OpenHLM, lingbot — a working "cross-brand
  skill / model / dataset" marketplace **already in production**
- 13 staff (well-resourced for the mission); Mishig, Clem (HF co-founder),
  Remi, Thomas Wolf involved → organizational priority, not side project

**Why LeRobot is the most immediate threat to OhhO Market:**
- OhhO's `lib/market/mcp-tools.ts` lives at the SKU-listing layer. HF LeRobot
  *is* the actual cross-brand robot-skill marketplace today.
- HF is "dataset first → checkpoint first"; OhhO Market is `policy + Proof
  scenario results + Shield signature` bundle. **Both formats are needed.**
- The minute HF LeRobot ships a "skill = dataset + checkpoint + scenarios"
  bundled format, OhhO Market loses its distinguishing format. LeRobot has the
  catalog; OhhO has the format spec but no catalog.

**OhhO's structural lever vs LeRobot:** LeRobot hosts artifacts, it does not
deploy them. **The combined list+deploy story — Train → Proof → Shield → Market
→ Serve → Fleet OTA — is the only durable marketplace format LeRobot hasn't
shipped.** OhhO has ~90 days to either ship that bundle format with real
inventory or partner/risk-concede with HuggingFace.

> See [03-moats.md](./03-moats.md) for the marketplace moat audit.

---

## The forces that compound AGAINST a horizontal-startup strategy

Read these in order. They are why "fully-horizontal" against this triad is a
death-sentence posture unless OhhO is willing to be the OSS Switzerland that
all three of Formant / Skild / HF LeRobot cite but can't beat.

1. **Foundation-model quality compounds with data scale.** Skild + PI + Scale
   are racing the curve; OhhO is not on it. OhhO cannot close this gap from a
   bootstrapped base.
2. **Operating-layer credibility compounds with years in production.** Formant
   has ~10. OhhO has zero reported.
3. **Marketplace gravity compounds with catalog inventory.** HF LeRobot has 188
   datasets / 59 models **and publishes more every week**. OhhO Market has ~0%
   public inventory.
4. **Open-OSS landings compound with the project winning the open ecosystem.**
   HF LeRobot + PickNik + Foxglove + MoveIt + Open-RMF are all OSS neighbors
   that already have community stars. OhhO's GitHub stars count is what
   investors and developers will check first.

---

## The horizontal thesis verdict

**Staying "fully horizontal — any robot / any industry" with 4–10 engineers
and $0 ARR against Skild + Formant + HF LeRobot funded as they are is a thought
experiment, not a strategy.**

Three outcomes are possible from this position:

| Outcome | Conditions | Probability (12 mo) |
|---|---|---|
| A — OhhO becomes the OSS operating-partner-of-choice for Skild-class brains + Formant-class ops | OhhO ships the 4-product wedge end-to-end; announces one named design partner doing this in 60 days; Market gets cold-start seed in 90 days. | **~15%** |
| B — OhhO becomes a research tool with NPM-grade popularity andGithub starsbut no revenue | OSS hobbyist traffic in voice continues; no enterprise OSS conversion; Forge remains "on-prem white-label" named only | **~50%** |
| C — OhhO fails to differentiate and is forked-aside | Skild partners with Formant; HF LeRobot launches bundling format; OhhO's 4-shipping-product wedge gets stale | **~35%** |

**Outcome A is the only profitable one** and requires the playbook in
[08-12-month-plan.md](./08-12-month-plan.md). Outcome B is the most likely today
and matches many bootstrapped OSS projects (great code, no revenue). Outcome C
is the default if OhhO does not change posture in 30–60 days.

---

## Where OhhO has gaps competitors don't have to fill

These are the *adjacent markets* OhhO uniquely covers but competitors don't:

- **Regulatory certification prep** (Comply + Proof): nobody in the VLA /
  operating-layer stack sells this. UL/TÜV are cert *issuers*, not software.
- **Mixed-brand fleet lifecycle** (Care + Bench + Build): Formant is
  multi-robot but does not sell robot design or assembly. PickNik does motion
  planning only.
- **Industrial Ethernet adapters** (Bridge PROFINET/EtherNet/IP/EtherCAT):
  nobody in the open AI-robotics stack supports these — the humanoid labs
  simply don't ship into the factories that run Siemens/Rockwell.
- **MR/VR teleop-as-data-pipeline** (Pilot → Data): Formant does remote
  operation, not LeRobot-format demonstration capture. Pilot's MR recording is
  unique.

These four gaps are OhhO's defensible periphery. **They are not on the
main battlefield (foundation models + OS layer + marketplace)** and therefore
are not enough alone. They are why OhhO has *something* to add to a partnership
that none of the three threat players can match.

> See [04-intelligence-vs-hardware-makers.md](./04-intelligence-vs-hardware-makers.md)
> for the structural-edge analysis.