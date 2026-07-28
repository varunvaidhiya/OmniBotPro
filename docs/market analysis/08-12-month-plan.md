# 08 — 12-Month Execution Plan

> 🛠 Engineering · 📊 Strategy — 7 prioritized gates with kill-criteria,
  sequenced to fix the top-4 failure modes from
  [07-red-team.md](./07-red-team.md) in 90 days.

---

## The premise this plan accepts

- Bootstrapped: no new hires without revenue or grant funding them.
- $0 ARR today; 4–10 eng team.
- The plan prioritizes **posture fixes over code volume** — most wins below
  are marketing, GTM, and partnership moves, not new product development.
- Every gate has a **kill-criterion** — a measurable outcome. If a gate fails,
  the company must pivot the play before spending the next quarter's effort.

---

## Gate 0: Brand诚实 posture fix (Day 0–7)

The fastest, cheapest, most impactful single change.

### Actions
1. **Rewrite `website/app/about/page.tsx:30`** — remove *"Some of the best
   companies in the world are building the future of AI in the physical world
   with OhhO."* Replace with:

   > *"We're building the multimodal data stack for embodied AI — for the
   > first builders joining us. If you're operating real robots and want an
   > open, robot-agnostic operating layer, we'd like to talk."*

2. **Rewrite `website/components/Lifecycle.tsx:201`** — replace
   *"The cheapest path from idea to certified fleet — without owing anyone
   your stack"* with *"The most open path from idea to certified fleet —
   without owing anyone your stack."* Drop "cheapest" framing — Red Hat isn't
   cheap. Price leadership creates margin.

3. **Replace the `[ Team Photo Placeholder ]`** in `about/page.tsx:68` with a
   `Join Us` CTA until a real logo grid exists. Stop advertising team-serious
   when no team photo has shipped.

4. **Update the homepage `StatsBar.tsx`** — `$0 platform fee — pay only for
   the cloud you use` and `Any robot — LeRobot · ROS 2 · ONNX compatible` are
   fine. But the existing copy does not surface customer count (because there
   are none). Honest stat labels are good; just don't ship invented numbers.

### Kill-criterion
**Day 7:** the three sentences above are in the marketing; the placeholder
stays or ships a real benchmark; no invented customer count appears anywhere.

### Owner-time: 1 eng-day + 1 founder-day. Not optional.

---

## Gate 1: Industry beachhead announcement (Day 8–30)

**Pick one industry and publish the choice externally.** This is the fix for
failure modes #1 (no beachhead) and partly #9 (brand caution in fundraising).

### Decision matrix for beachhead pick

| Candidate | Why yes | Why no | Verdict |
|---|---|---|---|
| **Warehouse AGV / AMR retrofit** | Bridge VDA 5050 + OPC UA fits; humanoid labs ignore it; integrators have budgets; aging fleet pain; Forge-seat economics make sense | Long sales cycle to enterprise; mapping VDA5050 customers tied to existing WMS is sequential work | ⭐ **Recommended primary** |
| Research / academia ICP | Fast to land (no enterprise sales cycle), top-of-funnel, Kit revenue pays ~$100K/yr, funnels into Forge deals when spinouts fund | Lower ARPA; niche volume | ⭐ **Recommended secondary (kit revenue)** |
| Pharma / lab automation | Pain well-defined; Forge economics very strong; needs Comply + Care + Shield shipped (roadmap) | Long compliance cycle; Comply not shipped | Tertiary (12mo+ to revenue) |
| Humanoid home assistant | Aligns with the fashionable humanoid narrative; "any humanoid brain" position | Battlefield Skild and Figure live on; locked out by their vertical-integration muscle | ❌ Avoid. Concede this vertical. Confession: hardware makers win this. |
| Industrial SCARA / FMS retrofit | Bridge Modbus/EtherCAT/Siemens plays; protectionist industrial market | MoveIt/PickNik already strong here; slower | Tertiary |

### Recommended primary beachhead: **warehouse AGV / AMR retrofit**.

This is a market the humanoid labs don't enter (they don't ship into Linde/
Toyota/MiR/WMS plants directly), with budget line items that already exist, with
VDA 5050 explicitly addressed in the current Bridge module (`website/AGENTS.md`
§3, VDA 5050 listed).

### Actions (Day 8–30)
1. Publish one blog post: *"Why OhhO is picking warehouse AGV retrofit as its
   first industry"* — note the bridge to adjacent verticals explicitly.
2. Update the homepage's `BrandAgnostic.tsx` hero to surface the AGV-retrofit
   use case **concretely** — not just generic "any robot." Add one
   industry-tagged card.
3. Reach out to 30 named robotics decision-makers (the list from Red Team
   Gate 4 mitigation): 5 brand makers, 10 integrators, 10 academic leads, 5
   open-robot hardware brands. The founder personally DMs each. Goal: 5
   design-partner conversations. 2 paid pilots in 60 days.
4. Freeze new product marketing copy on the planned products — Comply, Proof,
   Shield, Care — and add "Roadmap: Q4" syllabi to their landing pages so the
   cut catalog decision (Gate 4) is visible externally.

### Kill-criterion (Day 30)
- **NO** industry announcement published → kill the play; revise to a
  fallback "lab/hobbyist" identity and start over (with revenue scars locked
  in, this is fine but slower).
- Published but **0** design-partner conversations booked → repeat outreach
  with revised pitch based on what didn't land.

---

## Gate 2: Catalog cut (Day 8–30, parallel with Gate 1)

Cut the visible catalog breadth to match the engineering capacity.

### Recommended active set (7 products)
- Connect (Foundation, shipping)
- Data (Intelligence, shipping)
- Train (Intelligence, shipping)
- Serve (Intelligence, shipping)
- Bridge (beta, critical for AGV beachhead)
- Market (beta, cold-start Gate 3 below)
- Mind (beta, the headline Mind-tier claim — must keep its claim alive to
  pitch Forge Identity-tier)

### Pause / freeze marketing pages for 12 of 19
- Build, Frame, Bench, View, Autonomy, Pilot, Fleet, Twin, Care, Comply,
  Shield, Proof
- Either: (a) rework those pages to be "Coming Q4" placeholders, or (b) drop
  them from the top Nav (`components/Nav.tsx` `MARKETING_LINKS`) entirely
  while keeping the `app/<slug>/page.tsx` route alive at the slug for SEO.

**Why this is a feature not a bug**: the site already accurately shows `Roadmap`
status chips in `lib/products.tsx:142`. Pause-marketing is the orthodox move;
the company just hasn't done it because the catalog count is part of the brand
story ("19 products riding on one engine" is a seductive tagline). But a
better tagline is *"7 products today; 19 the open destination"* — and the
founder gets to ship more things without apology.

### Owner-time
- 1 eng-day to update `components/Nav.tsx` link list
- 1 founder-day on the messaging decision

### Kill-criterion (Day 30)
- 7-product "active" + 12-product "roadmap" division visible on the site
  → pass.
- 19-product active mark preserved → repeat conversation.

---

## Gate 3: Marketplace cold-start (Day 14–90)

Fix failure mode #5 (HF LeRobot out-catalogs).

### Actions
1. **Day 14:** ship the OhhO Market **bundle format spec**:
   `(signed_policy_package, Proof_scenario_results, Shield_signature,
   marketplace_listing)` — published as a server-time JSON schema in the `ohho-os`
   repo.
2. **Day 30:** port 5 internal policies (patrol, pick-place-tier-1, wave, stop,
   status from the SDK built-in skills per `ohho-os` `@skill` decorator) into
   the bundle format with dummy-Proof scenarios. Ship them as the first
   listings in Market.
3. **Day 45:** launch the **$25K Maker Grants** program — 25 grants of $1K each
   for creators who publish a verified skill in the bundle format by Day 75.
   Target outcomes: ≥15 verified skills from at least 10 distinct external
   authors.
4. **Day 75:** first paid skill sale. Update the homepage hero to surface the
   marketplace ("X verified skills, Y creators, Z paid deployments").
5. **Day 90:** publish a blog "OhhO Market 30-day launch report"
   (skills/authors/commercial use).

### Kill-criterion (Day 90)
- **≥20 verified skills live + ≥10 distinct external authors** → marketplace
  moat is real, continue to Gate 5.
- **<10 verified skills or <5 external authors** → cold-start failed.
  Mitigation: increase bounty to $5K/grant, pay Scale AI to source 10 skilled
  authors, OR (worse) admit that LeRobot's catalog wins and partner with HF
  rather than compete. **Conceding HF LeRobot = decision point — not worse
  than the next quarter spending; it's the platform play.**

### Owner-time
1 senior eng × 6 weeks; ~$25K grant budget; ~$5K bounty pool; founder time on
the 30-author outreach.

---

## Gate 4: Mind-loop demo + 1 named design partner (Day 30–120)

Fix failure mode #11 (Mind unproven at fleet scale) and shore up failure mode
#4 (Formant closes Forge pipeline).

### Actions
1. **Day 30–60:** ship Mind closed loop on OmniBot reference robot + one
   **hetero-brand** robot (Unitree G1, Trossen arm, or similar). Verify the
   full loop: goal in plain language → Mind reason → safety gate verify →
   Autonomy execute → Data record → Train offline schedule. The ChooseOrLose
   reference implementation lives at:
   `sdk/ohho/adapters` + `agent_engine/integrations` + `learning_engine/`.
2. **Day 60:** publish the operational report (success rate, recovery rate,
   safety-gate triggers, latency overhead). This becomes the Mind pitch asset.
3. **Day 60–90:** sign 1 paid design partner (likely a Tier 1 research lab or
   small integrator)	Port their first Mind experiment to the Forge TF engagement
   at $25K+ ACV. They get a real implementation; OhhO gets a logo.
4. **Day 90–120:** ship one case study with the named partner visible on the
   homepage. Now the "best companies building with us" line — if any future
   version ships — has a real answer.

### Kill-criterion (Day 120)
- **1 named customer logo + 1 published operational report + Mind closed
  loop demonstrable on two hetero-brand robots** → pass; promote to Gate 5.
- 0 named customers by Day 120 → the Forge pipeline reset → pivot to academic
  kit business (P5 from `PRODUCT_DIRECTIONS.md`) as primary revenue, accepting
  lower ACV; defer Forge to year 2.

### Owner-time
2 senior eng × 8 weeks; 1 founder × 30 days of GTM work; ~$5K infrastructure
cost.

---

## Gate 5: Forge repositioning + 1–2 Forge customers (Day 60–180)

Reposition Forge from "on-prem white-label" to "managed cloud + named CSM +
cert-prep + SLA at published ACVs." (See [06-profitability-positioning.md](./06-profitability-positioning.md).)

### Actions
1. **Day 60:** update `components/Pricing.tsx:73-91` — replace *"Custom"* with
   a published 3-tier Forge ladder:
   - Team — $25K/yr (~10 robots, named CSM, email SLA)
   - Studio — $50K/yr (~50 robots, hybrid Mind, chat SLA, Comply self-service)
   - Enterprise — $250K+/yr (custom integrations, cert-prep hours,
     dedicated AE)
2. **Day 90:** begin Forge pipeline outreach — direct cold-DM 50 named
   decision-makers in the beachhead industry (warehouse AGV retrofit). The
   founder or the GTM hire owns this.
3. **Day 120–180:** close 1–2 Forge customers. The first signs deal with
   flexibility on price; demand named case-study usage rights.

### Kill-criterion (Day 180)
- 1 Forge deal signed + 2 in late pipeline → pass.
- 0 Forge deals signed at Day 180 → restructure: drop Forge sales motion
  for the quarter, double down on paid symposium / academic-Consortium
  arrangement (the P5 education wedge from `PRODUCT_DIRECTIONS.md`) to generate
  $100K ARR in quarter 3 money to fund the next Forge hire.

### Owner-time
1 founder × 12 weeks of Forge sales motion + 0.5 eng × 12 weeks of
implementation overrides at customer demand. ~$15K sales budget.

---

## Gate 6: Bridge industrial-Ethernet verification + UL/TÜV partnership (Day 60–180)

Fix failure mode #8 (Bridge ind-Eth adapter quality) and lay the foundation
for failure mode #4 (Forge pipeline credibility).

### Actions
1. **Day 60–120:** verify the PROFINET, EtherNet/IP, EtherCAT, VDA 5050, OPC
   UA adapters in `website/lib/bridge/adapters.ts` actually work at
   production-grade against test harnesses from:
   - Hilscher netTAP for PROFINET/EtherCAT
   - Softing for EtherNet/IP
   - Open-RMF reference test fleet for VDA 5050
2. **Day 90:** ship a blog/demos: *"OhhO Bridge talks to Siemens S7-1500 over
   PROFINET, end-to-end, no ROS adapter hacks"* — proof-of-quality that
   differentiates the platform from any humanoid-vendor OS.
3. **Day 120–180:** sign a partnership MoU with UL or TÜV SÜD for cert-prep
   engagement (referred from OhhO Comply engagements). This is the cert
   chokepoint moat from [03-moats.md](./03-moats.md).

### Kill-criterion (Day 180)
- Verified production-grade adapters + signed MoU → pass.
- Adapters are reference-quality only → don't market Bridge to Siemens/Plant
  buyers until the team ships production code. Do not over-promise on
  industrial-Ethernet depth until verified.

### Owner-time
1 senior eng × 8 weeks of work on adapter verification; 1 founder × 60 days
of partnership outreach to UL/TÜV SÜD.

---

## Gate 7: First hires (Day 90–270, conditional on Forge cash)

If Gate 5 lands 1–2 Forge deals, the revenue should fund the Red-Hat-muscle
hire. Without it, OhhO's enterprise motion stays founder-led — which is
single-threaded.

### Hires in order

1. **Head of Solutions Engineering (Day 90–180)** — ex-Red Hat / HashiCorp /
   Vercel / AWS Solutions org. Profile: OSS-to-enterprise GTM practiced.
   Compensation: ~$180K–$250K base + options; funded from Forge ACV.
2. **Enterprise AE / Forge lead (Day 180–270)** — ex-Formant or ex-Apptronik
   or ex-Standard Bots. Same comp band.
3. **(Optional) Pilot teleop service technician (Day 180–270)** — for
   Pilot-aaS revenue during autonomy-maturity gap; a part-time role.

### Funding source
- 1 Forge deal at $50K ACV →月中旬 tenance:* to all hires are funded from
  Forgeェstmix: ~each hire ~$80–$100K/yr fully loaded at pre-revenue, the
  first Forge deal at $50K ACV plus first Marketpaid flows at ~$30K/qtr
  barely cover the first hire; needs 2nd + 3rd Forge deal to reach
  profitability-of-hire.

### Kill-criterion (Day 270)
- Hires complete + at least 1 Forge deal contributed by the new hire not
  from founder → pass.
- Hires not made or revenue < $250K ARR → defer Forge hires indefinitely;
  remain founder-led for engineering + GTM; lower valuation ceiling is
  acceptable.

---

## What this 12-month plan does and doesn't deliver

### What it delivers by Day 365 (when fully executed)

- 7-product active catalog + 12-product honest roadmap
- 1 named Forge-logo customer + 2 design partners
- 20–30 verified skills on Market from 10+ external creators
- Mind closed loop demo on 2 hetero-brand robots with operational report
- 1 UL/TÜV partnership on cert-prep MoU
- Production-grade Bridge VDA5050 / OPC UA / PROFINET / EtherNet/IP / EtherCAT
- 1–2 Forge revenue deals ≈ $50K–$150K ACV
- Builder retail funnel ~80 seats → 20 Fleet → 3 Forge conversions
- ~$250K–$500K ARR real (neding paid Forge to fund first GTM hire)
- A realistic Forge runway Pablo with credible distribution curve

### What it does NOT deliver

- New product modules (no new #20)
- $1M ARR (path is Q6–Q9)
- Mind fully validated at scale across multiple fleets (year 2)
- Marketplace take-rate at scale (year 2)
- Anything resembling "best companies in the world are building with us"
  language — that line stays killed

### What it does NOT require

- External funding (bootstrapped path; Forge revenue funds the first hires)
- Founder giving up control (no preferred equity)
- New product development other than Bridge Verification + Market cold-start
- Hiring an engineering army (3–5 eng focus stays)

---

## The pattern this plan reflects

For bootstrapped OSS projects at pre-revenue, the winning pattern is:

> 1. Cut the visible surface to what ships.
> 2. Pick one wedge market and publish it.
> 3. Seed the network-effect product with cash grants before competitors
>    out-catalog.
> 4. Prove the headline claim on real hetero-brand hardware.
> 5. Repackage the enterprise tier as managed cloud at published ACVs.
> 6. Form ONE institutional partnership (regulatory, OEM, or academic
>    consortium).
> 7. Use the first Forge revenue to fund the GTM hire.

This is the playbook that moved Redis, Vercel, Supabase, HashiCorp, and (yes)
Red Hat from pre-revenue OSS to sustainable ARR. None of these had to invent a
moat that doesn't exist; they had to *act on the posture*.

> Next read: [09-retention-and-churn.md](./09-retention-and-churn.md) for why
> customers will stay or defect once Gate 5 brings them into the funnel.