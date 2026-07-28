# 10 — Competitor Teardowns

> 📊 Strategy · 💰 Investor — one-pager per competitor: company / funding /
> current product / threat to OhhO / OhhO's defensible position. Sources
> cited inline; competitor names are unavoidable here (this file is for
> internal strategy, not marketing copy — the brand-agnostic rule in
> `website/AGENTS.md` §2 applies to consumer-facing copy only).

---

## How to read this file

Each competitor is rated on three axes:

- **Strategic seat overlap** (how much of OhhO's claimed surface do they
  cover): 🟢 low / 🟡 medium / 🔴 high
- **Funding / runway advantage vs OhhO** (bootstrapped, $0 ARR, 4–10 eng):
  🟢 similar / 🟡 10× / 🔴 100×+
- **Velocity advantage** (shipping cadence, press cadence, named customers):
  🟢 similar / 🟡 10× / 🔴 100×+

The Teardown verdict for each says: Can OhhO beat them, partner with them, or
concede the battlefield?

---

## 1. Hugging Face LeRobot — the highest *immediate* competitive threat to OhhO Market

**Source:** `huggingface.co/lerobot`, fetched July 28, 2026, 6 PM UTC.

### Snapshot
- **Entity:** LeRobot, hosted on HuggingFace Hub. 13-person team (incl. Clem
  and Thomas Wolf — HF co-founders).
- **Catalog at time of writing:** 188 datasets, 59 models, 11 collections,
  11 Spaces.
- **Velocity signal:** `lerobot/pi052_base` published **one hour before this
  fetch** — weekly cadence maintaining.
- **Standouts:** VLA-JEPA (3B, 713-11K downloads), FastWAM (6B), pi052 (4B),
  OpenHLM-ckpt (4B), lingbot_va (5B). Active Spaces for visualization and
  LeLab (a hosted UI).
- **Distribution surface:** HuggingFace Hub (~1.5M daily users).

### Threat rating
- Strategic seat overlap: 🔴 **High** (cross-brand model + dataset distribution
  = OhhO Market's exact seat)
- Funding advantage: 🔴 **Massive** (HF hosted; HF Series-D at $100M+)
- Velocity advantage: 🔴 **Massive** (publishing weekly; OhhO publishes 0
  marketplace skills)

### Why the high rating
HF LeRobot is the **canonical cross-brand robot-skill marketplace today**.
Their format is `(dataset, checkpoint)` not OhhO's
`(signed_policy, Proof_scenarios, Shield_signature, marketplace_listing)` —
but the catalog exists; the format can be extended.

### OhhO's defensible position
- Format extension: HF LeRobot doesn't yet ship scenario-verified signed
  packages; that's OhhO's spec.
- Deployment: HF LeRobot hosts artifacts; it does not deploy them. Bridge
  + Fleet OTA + Serve + Mind's agent loop = deployment depth HF doesn't have.
- Bridging format: ship 5 internal skills (patrol, wave, stop, status, pick)
  in the OhhO bundle format referencing HF dataset cards, proving the bundle
  format interoperable with LeRobot, not anti-LeRobot.

### Teardown verdict
**Partner, don't compete on catalog.** OhhO Market should explicitly publish
HF-compatible bundle format; cross-list HF datasets as training provenance in
OhhO Market listings. Co-existence: HF is the dataset hub; OhhO is the
deployment + verification hub.

> Failure mode #5 in `07-red-team.md` fires if this partnership is not made
> in 90 days.

---

## 2. Physical Intelligence (π) — the highest *foundation-model* threat

**Source:** `physicalintelligence.company/blog`, fetched July 28, 2026.

### Snapshot
- **Latest:** π0.7 "a Steerable Model with Emergent Capabilities" (April 16,
  2026). Cadence: π0 → π0.5 (April 2025) → π*0.6 RL-learned (Nov 2025) →
  π0.7 (April 2026). All in 18 months.
- **Research stack:** Real-time chunking, multi-scale embodied memory
  (MEM, March 2026), precise online RL (March 2026), human-to-robot transfer
  (Dec 2025), FAST tokenizer (Jan 2025), HiRobot think-step-by-step (Feb
  2025).
- **Open source posture:** Open-sourced π0 weights (Feb 2025) — published on
  HuggingFace LeRobot.
- **Customer-style commentary:** "PI partners are already solving real-world
  problems" (Feb 2026 blog post).

### Threat rating
- Strategic seat overlap: 🟡 **Medium**. PI sells the brain layer, not the
  operating layer. They could partner with an OS layer (Formant or OhhO).
- Funding advantage: 🔴 **100×+** (founded by Chelsea Finn, Karol Hausman,
  Sergey Lavine-backed; raised $400M at $2.4B valuation in 2024).
- Velocity advantage: 🔴 **Massive** (monthly papers + model releases)

### Why the medium rating
PI's surface is the foundation model + researchIP. They don't currently
claim the operating-layer / fleet / cert / marketplace seat. Their open-weights
posture means they NEED a deployment ecosystem — and they currently use HF
LeRobot as that ecosystem.

### OhhO's defensible position
- PI's brain runs through OhhO's runtime (OhhO Serve supports OpenVLA-style
  model plug-ins; pi0 weights no different). OhhO can be the **operating
  partner for a PI brain on a customer's fleet** — without owning the brain.
- Achieving this requires explicit coordination with PI; no marketing can
  substitute for a real partnership.

### Teardown verdict
**Partner explicitly.** Big win: "OhhO Fleet + Mind orchestrates a π0.7
policy deployed to a heterogeneous fleet." There is no marketing-side pivot
that achieves this; founder-to-founder introduction with PI is the only path.

---

## 3. Figure — direct defense impossible; play complementarity

**Source:** `figure.ai`, fetched July 28, 2026.

### Snapshot
- **Product:** Figure 03 humanoid robot for the home; Helix as the in-house
  AI brain.
- **News flow:** Figure 03 announced; Helix featured; "team bringing
  impossible ideas to life."
- **Funding advantage:** Series C+ rounds; OpenAI and Microsoft historically
  involved; partnership with BMW for automotive use cases.
- **Velocity:** Heavy press cadence; YouTube demo with household navigation.

### Threat rating
- Strategic seat overlap: 🟡 **Medium**. Figure is in the "own brain on own
  body" vertical; doesn't compete on the operating-layer seat directly. But
  any Figure customer is locked out of OhhO by definition.
- Funding advantage: 🔴 **100×+**
- Velocity advantage: 🔴 **Massive** (public demos; Helix brand-recognition)

### Why the medium rating
Figure doesn't sell the operating layer; they sell a robot. **But the
moment a customer has a Figure-03 humanoid, OhhO cannot serve it** (Figure's
closed SDK; AGENTS.md §2 properly notes "closed walled-garden platforms
aren't the target").

### OhhO's defensible position
None directly. OhhO's structural lever (from
[04-intelligence-vs-hardware-makers.md](./04-intelligence-vs-hardware-makers.md))
is the open-Switzerland seat, which Figure structurally cannot occupy.

**The honest play:** concede this vertical externally. *"We don't serve
Figure-03 owners; we serve everyone else."* Make that an explicit talking
point, not an embarrassing constraint.

### Teardown verdict
**Concede the Figure vertical publicly.** Don't pitch Figure owners; pitch
everybody who bought the wrong humanoid (Unitree G1, open quadrupeds, AGV
retrofits) and now needs an OSS brain.

---

## 4. 1X (NEO) — home robot; same concession play as Figure

**Source:** `1x.tech`, fetched July 28, 2026.

### Snapshot
- **Product:** NEO home humanoid; $200 deposit to order; AI / Factory / Stories
  subdivisions; Investor Relations page.
- **Velocity:** NEO named-career track (66 open roles July 2026).
- **Funding advantage:** OpenAI-backed; significant runway.
- **OS layer:** Not publicly disclosed; deep vertical play.

### Threat rating
- Strategic seat overlap: 🟡 **Medium** (same Figure-style vertical;
  consumer home humanoid = vertical not in OhhO's
  AGV/industrial/open-hardware beachhead)
- Funding advantage: 🔴 **100×+**
- Velocity advantage: 🟡 **Moderate** (consumer marketing ≠ B2B sales cycle)

### OhhO's defensible position
NEO owners are locked into 1X. But NEO is shipping to consumers; their plant
isn't OhhO's anyway. Concede the consumer-home-humanoid vertical; focus on
industrial + research.

### Teardown verdict
**Concede consumer-home vertical.** Don't pitch NEO owners. Pitch their
buyers before they buy NEO (research labs at universities that already have
a G1 + want to keep their data).

---

## 5. Formant — the highest *operating-layer* threat to OhhO

**Source:** `formant.com`, fetched July 27, 2026.

### Snapshot
- **Brand claim:** *"We make physical AI succeed in the real world — beyond
  the demo."* / *"The operating layer for physical AI."* —
  the same seat OhhO claims in `lib/copy.ts:36`.
- **Production reality:** 644+ production organizations cumulative (footer
  self-report, May 2026); 1.98M operating hours; "9 yrs" device activity;
  events identified 32M; missions tracked; autonomous miles; ~10 years of
  recorded video.
- **Customer proof:** Named case studies published — BP, Burro, Hullbot, Cala.
- **Funding advantage:** Bootstrapped → venture-backed; last disclosed Seed
  ~$10M+ from Eclipse, etc.
- **Strategy:** *"Pilots rarely fail on capability. They fail on contact with
  unstructured reality — the edge cases no demo contains. That's exactly what
  we've accumulated."* This is the credibility moat OhhO claims under
  "Best-operated" without having accumulated it.
- **GTm model:** Forward-deployed engineers ("you get outcomes, not a tool
  you're left to run alone"). The Red Hat muscle OhhO needs to hire.

### Threat rating
- Strategic seat overlap: 🔴 **High**. Formant IS the operating layer; OhhO
  aspires to be.
- Funding advantage: 🟡 **10×** — bootstrapped-to-Seed difference;
  not catastrophic, but real.
- Velocity advantage: 🔴 **Massive** (10 years of customer data; named case
  studies; published book camp methodology).

### OhhO's defensible position
- **OSS wedge.** Formant is proprietary; OhhO's Apache-2.0 engine can be
  extended by any customer or partner. Formant cannot be self-hosted at scale.
- **Brain-agnosticism.** Formant integrates with brains but doesn't claim
  brain-agnostic. OhhO explicitly accepts "bring any brain — Skild, PI, your
  own." Formant has narrower explicit brain integration.
- **Industrial-Ethernet OSS depth.** Formant focuses on operation; has less
  on Bridge PROFINET / EtherNet/IP / VDA 5050.

### Teardown verdict
**Differentiate on openness + industrial protocol depth.** Pitch: *"If
you're ready to deploy on Formant, do it. If you want OSS, self-hostable
control, and industrial protocols, OhhO is your alternative."*

Risk: Formant announces VDA 5050 / OPC UA support; closes the differentiator.
Mitigation: ship production-grade Bridge depth before Formant announces that.

---

## 6. Skild AI — the highest *free-foundation* competitive threat

**Source:** `skild.ai`, fetched July 28, 2026.

### Snapshot
- **Brand claim:** *"General Purpose Robotic Intelligence"* / *"Omni-bodied
  brain to control any robot for any task)."*
- **Thesis:** *"Physical AI should be omni-bodied"* — same OhhO thesis.
- **Customer play:** Security/Inspection Robot Platform; Mobile Manipulation;
  Autonomous Packing — real-world problem-verticals.
- **Funding stack:** SoftBank, Bezos (Amazon), Sequoia, General Catalyst,
  Felicis, Lightspeed, Coatue, CRV, SV Angel — multiple $100M+ rounds.
- **Velocity signal:** Recent blog on "Learning by Watching human videos"
  — scalable-data approach the founder explicitly compared to LLM data scale.

### Threat rating
- Strategic seat overlap: 🔴 **High** — *"omni-bodied brain"* = OhhO's
  cross-brand brain thesis
- Funding advantage: 🔴 **100×+** (SoftBank + Bezos + Amazon = capital
  stack that can fund true OA-scale data collection)
- Velocity advantage: 🔴 **Massive**

### Why the high rating
Skild is racing OhhO's exact strategic seat with infinitely more fuel. They
solve "robot data problem" via "learning by watching human videos"; this
side-steps the teleop UX bottleneck OhhO faces.

### OhhO's defensible position
**Skild's brain is closed weights.** Their data flywheel is theirs. **OhhO's
operational layer** (Bridge → Frame → Fleet → Market + Comply + Shield + Proof)
is the operating layer, not the brain layer. Skild must plug into an
operating layer to deploy; OhhO wants to be that layer.

**But:** Skild has not yet announced that operating layer partnership.
Formant or HF LeRobot could sign it. That's the 6-12 month window from
`07-red-team.md` failure mode #6.

### Teardown verdict
**Coordinate, don't compete.** Founder-to-founder introduction with Skild
inside 90 days. The pitch: *"Skild makes a great brain; OhhO deploys it
across heterogeneous fleets with cert and OTA. Use us as the OSS operating
layer for Skild deployment."*

This is the single highest-leverage outreach in the [08-12-month-plan.md](./08-12-month-plan.md)
Gate 4 sequence.

---

## 7. Scale AI (Robotics Data Engine) — the HITL services threat

**Source:** `scale.com/robotics`, fetched July 28, 2026.

### Snapshot
- **Customer proof:** "Trusted by the world's most ambitious AI teams."
  Names: Physical Intelligence, Generalist, Cobot (Brad Porter — patient), Dyna.
- **Service stack:** Global Collection Network (data factories, distributed
  collectors, partners); 1000+ hours of demo data ingested daily.
- **Embodiments:** Bimanual manipulators, Scale Harness (robotless), bespoke
  customer platforms.
- **Environments:** Data Factories (high-volume), Residential, Commercial and
  Industrial.
- **Differentiators:** SOC 2 Type II, ISO 27001, GDPR/CCPA, White Glove
  Service, internal roboticists.

### Threat rating
- Strategic seat overlap: 🟡 **Medium** (data engine overlaps OhhO Data;
  Scale doesn't claim OS / Fleet / Mind-sofa)
- Funding advantage: 🔴 **100×+** (Scale AI went public at >$10B)
- Velocity advantage: 🔴 **Massive**

### OhhO's defensible position
- OhhO Pilot teleop-as-data-pipeline is structurally different — the
  operator IS the labeler, the recording IS LeRobot format. Scale operates
  its data factories at scale but is decoupled from operator context.
- OSS: LeRobot format openly interchangeable, not Scale's internal tooling.

### Teardown verdict
**Differentiate but **don't compete head-on**.** Scale is the pile-up
$200K-$5M engagement for enterprise brain-makers; OhhO is the OSS alternative
for research labs and hardware brands doing it themselves.

---

## 8. Apptronik — vertical humanoid; concession battlefield

**Source:** `apptronik.com`, fetched July 28, 2026.

### Snapshot
- **Product:** Apollo 2 humanoid robot; bipedal + wheeled mobility options.
- **Customer verticals:** Manufacturing, Warehouse (3PL), Retail — well
  segmented.
- **Customer logos on site:** Google DeepMind, NVIDIA, GXO, Mercedes Benz,
  Jabil, Synology.
- **Recognition:** 2025 Best Workplace Innovators, 2025 Forbes Innovative
  Companies, 2026 CNBC Disruptor 50.
- **Vertical integration:** "platform for scalable humanoid deployment."
- **Funding:** Series A and beyond; multiple disclosed partners.

### Threat rating
- Strategic seat overlap: 🟡 **Medium** (Apollo is a vertical humanoid; their
  warehouse positioning overlaps OhhO's recommended AGV-beachhead, but their
  Apollo costs much more per-unit)
- Funding advantage: 🔴 **100×+**
- Velocity advantage: 🔴 **Massive**

### OhhO's defensible position
Apollo costs hundreds of thousands of dollars per unit. AGV retrofits cost
$5K-$30K per unit. **Different price points = different market segments.**
Apollo is at Jabil/Mercedes/GXO; OhhO's AGV beachhead is at the mid-tier
3PL that can't afford Apptronik.

### Teardown verdict
**Acknowledge; don't compete head-on in 3PL.** Differentiation in price +
open-stack. OhhO's GTM pitch: *"Apollo is the Cadillac; we're the open-source
Linux."*

---

## 9. Unitree — hardware-only OEM; potential partner

**Source:** `unitree.com`, fetched July 28, 2026.

### Snapshot
- **Catalog:** As2, Go2 (consumer/ed), A2/B2 (industry), H1/H2/R1/G1
  (humanoids), Dex2-5 / Dex1-1 / Dex5-1 / Dex3-1 (hands), Z1 / D1-T (arms),
  L1/L2 (4D LiDAR). Plus IM6014 motors, bionic joint motors, app ecosystem.
- **Open source posture:** Has `/opensource` page (Unitree OS), `/developer`
  and `/github.com/unitreerobotics` resource center, plus a customer service
  console global-serviceconsole.unitree.com.
- **Brand prominence:** CCTV Spring Gala appearances (秧Bot, kung-fu-horse);
  global visibility.

### Threat rating
- Strategic seat overlap: 🟢 **Low** — Unitree is hardware-first; they sell
  bodies, not brains or operators.
- Funding advantage: 🟡 **Moderate** (hardware company; self-funded+investor
  mix; not venture-exploded like Skild but well-funded for hardware line)
- Velocity advantage: 🟡 **Moderate** (consumer marketing velocity, B2B sales
  slower)

### OhhO's defensible position
**Unitree makes the bodies; OhhO runs the brains.** G1 + H1 + R1 are popular
in research but the in-box demos are sparse. **OhhO Adapter for Unitree
SDK** is the obvious Tier 2A design-partner move from
[05-pmf.md](./05-pmf.md).

### Teardown verdict
**Prime partnership candidate.** Founder-to-founder intro with Unitree;
co-positioning: *"Unitree's G1 + H1 + R1 ... with OhhO OS as the open
operating layer."* A co-marketed "Unitree + OhhO Mind" reference build would
deliver the first production-grade Mind-tier reference implementation on
hetero-brand hardware (Gate 4 of the 12-month plan).

---

## Summary ledger — battlefields and verdicts

| # | Competitor | Battlefield | OhhO play |
|---|---|---|---|
| 1 | HF LeRobot | Cross-brand catalog | Partner in 90 days; ship bundle-format compatible with HF datasets |
| 2 | Physical Intelligence | Foundation model | Partner; pitch OhhO as operating layer for π0.x deployment |
| 3 | Figure | Closed humanoid vertical | Concede; pitch "anyone who didn't buy Figure" |
| 4 | 1X | Consumer home humanoid | Concede; pitch research labs that need their data |
| 5 | Formant | Operating layer | Differentiate on OSS + industrial-Ethernet; pitch Forge as alternative |
| 6 | Skild | Omni-bodied brain foundation | Coordinate; founder-to-founder intro in 90 days |
| 7 | Scale AI | HITL data engine | Differentiate via Pilot teleop-as-data-pipeline; don't compete head-on |
| 8 | Apptronik | Vertical humanoid | Concede; differentiate on price + open stack |
| 9 | Unitree | Hardware body | **Prime partnership candidate**; co-position |

### The的合作 partnership matrix toppling

Of the nine named competitors:
- **2 are partnership-of-record candidates** (Unitree, + PI).
- **2 are coordination candidates** (HF LeRobot, Skild).
- **1 is differentiation candidate** (Formant by OSS + Bridge).
- **1 is market-segment alternative** (Scale AI).
- **3 are concede-the-battlefield** (Figure, 1X, Apptronik).

**Translation:** OhhO cannot win foundation-model war or operating-layer
battlefield alone. It can win the **OSS coordination play** — the same model
that moved Red Hat, Vercel, and HashiCorp from pre-revenue OSS to credibility.

> See [03-moats.md](./03-moats.md) for the moats audit that backs this
> partnership matrix, and [08-12-month-plan.md](./08-12-month-plan.md) for
> the 90-day action sequence.