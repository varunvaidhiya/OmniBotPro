# 06 — Profitability Positioning

> 💰 Investor · 📊 Strategy — line-by-line revenue recommendations and the math
> from $0 to first $1M ARR. Brutal pill: **the self-serve pricing cannot do
> it alone; Forge must become the business.**

---

## The central brutal truth about pricing today

Current pricing (`components/Pricing.tsx:11`):

| Tier | ARPA | Notes on revenue |
|---|---|---|
| **Free** | $0 | All 19 consoles locally. Freeloaders think they are customers but they aren't. |
| **Builder** | $588/yr | GPU training (20 hr) costs more than $49/mo retail for almost any real workload. **Margin per seat is negative.** |
| **Fleet** | $2,388/yr | Marginal only if customer uses minimally; actual customers running 10K Serve calls/day + 200 GPU hr / mo on managed cloud cost ~$200–$2,000 in infragrams alone. **Margin per seat is negative at typical use.** |
| **Forge** | Custom | "On-prem white-label" — true enterprise-priced but positioned narrowly as "for hardware OEMs." |

**The current retail ladder is structurally margin-negative at average use.**
This is normal for OSS-SaaS startups (Vercel, Supabase, HF Pro etc. all run
margin-negative on lower tiers), but **only viable if there is a Forge (or
equivalent enterprise tier) that funds the leak.** Without a high-ACV tier,
this is a scale-of-users business with no revenue scaling.

**Profitability therefore requires Forge to become the actual business.** Today
Forge is positioned narrowly as "on-prem white-label" — that positions OhhO as
an OEM-enabling vendor, not an enterprise managed-cloud vendor. **This must
flip.**

---

## The Forge revenue repositioning (the single biggest revenue unlock)

### Today's Forge positioning
From `Pricing.tsx:73-91`:

> *"Enterprise / white-label. Everything in Fleet. Unlimited GPU & API usage.
> On-prem OhhO Serve license. Custom robot integrations. Custom standards +
> cert partner. Secure boot + SSO. Private marketplace + white-label.
> Dedicated SLA + onboarding. White-label OhhO Pilot."*

This is fine for hardware OEM customers (Trossen, a future open-brands cluster)
but **leaves the largest enterprise digital-primary segment unaddressed**: the
enterprise managed-cloud category dominated by Red Hat OpenShift, HashiCorp
Cloud, Vercel for Enterprise, etc.

### Recommended Forge repositioning

| Feature | Today | Recommended |
|---|---|---|
| Positioning | "On-prem white-label" | "Fully managed OhhO Cloud + named CSM + SLA + cert-prep engagements" |
| Pricing | "Custom" (Vague) | **$25K–$1M ACV** published on a 3-tier ladder (Team/Studio/Enterprise) for predictability |
| Differentiator | White-label Pilot, custom adapters | **SOC 2 + ISO 27001 + named CSM + 99.5% SLA + Forge-only Comply bundleship on top |
| Target buyer | Hardware makers | Enterprise digital-primary + integrators + open-brands + anyone who has mixed-brand fleets they cannot update themselves |
| Sales motion | Inbound only | Direct sales (1 enterprise AEs) + 1 solutions engineer (the Red-Hat-muscle hire) |

### Economics of the Forge pivot

For $1M ARR run rate, the path is:

| Customer type | Deal size | Count | % of ARR | Probability of close in 12 mo |
|---|---|---|---|---|
| Open-robot hardware brand (Forge white-label) | $25K–$500K ACV | 2–5 | 40% | High if Tier 2A target lands |
| Mid-market warehouse AMR integrator (Forge cert-prep + Fleet) | $25K–$100K ACV | 5–10 | 30% | Medium if beachhead announcement hits the right buyers |
| Pharma/lab automation director (Forge full lifecycle) | $50K–$1M ACV | 1–3 | 20% | Lower; referral-led |
| Education kit partnership (Forge white-label + academic seats) | $5K–$50K ACV | 10–20 | 5% | High if 10 university partnerships signed |
| Self-serve Fleet seats ($199/mo) | ~$2.4K ACV | 100–500 | 5% | Adds volume but mostly funnel to Forge |

That distribution makes $1M ARR credible at 5–25 Forge customers in 18 months.
**Not a forecast — a calibration of what *each tier closes* with one full-time
AE + one full-time SE**. The 4–10 eng team cannot hire both in 6 months; the
plan does it in 9–12.

> See [08-12-month-plan.md](./08-12-month-plan.md) for the hiring sequence.

---

## Revenue streams recommended (ranked by ROI)

### 1. Forge-as-managed-cloud — ⭐ primary revenue engine
- ACM + named CSM + SLA + cert-prep engagements
- ACV $25K–$500K per customer per year
- Pipeline tied to Forge-direct AE + partner-OEM inflow
- **Target: $400K–$700K ARR share of $1M goal (40–70%)**

### 2. Mind-as-a-Service subscription — ⭐ first "Mind-tier recurring revenue" stream
- Per-robot / per-month $50–$150 fees for iterative Mind over-the-air improvement
- Especially strong for an AGV fleet customer paying per-robot (retrofit revenue)
- **Target: $150K–$300K ARR share of $1M goal (15–30%)**

### 3. Comply-as-cert-prep couplet — ⭐ first regulatory revenue
- Partner arrangement reselling cert body services (UL/TÜV/SGS) at margin
- Day-rate engagement $200–$500/hr; SAF-style 1–3 week engagements
- OhhO's role: software compliance prep + technical-file generation + evidence
- Each 3-week engagement ≈ $30K–$50K
- **Target: $50K–$150K ARR share of $1M goal (5–15%)**

### 4. Marketplace take-rate — ⭐ the long-term main event
- 15-25% take-rate on paid skills
- Only viable after catalog seeding (20–30 grants in 90 days)
- Minimal year 1 ($5K–$50K); significant at 18–24 months after cold start (**This
  is the bet that determines OhhO's enterprise valuation, not the year-1 ARR
  number** because it is the only moat that compounds at platform-grade)

### 5. Pilot teleop-as-a-service — pragmatic revenue during autonomy maturity
- Per-shift / per-robot hourly teleop charges during the gap between "robot
  purchased" and "robot autonomous" (when Mind is not yet flying autonomously)
- **Target: $20–$50/shift/robot contracted monthly**
- Boutique revenue today (10s–100s of $K/yr); **adopts Scale-AI-style
  managed-services volume into year 2–3**

### 6. Builder/Fleet retail seats — intentional loss leader
- Lose $10–$50/mo per Builder customer; lose $5–$50/mo per Fleet customer
  depending on usage; **must be free platform-funnel for Forge**
- **Target: ARR-$150K→-$300K range; breakeven-wise profitable only after a
  $1M+ customer mix absorbs the cost**

### 7. Education kit (research-grade recurring) — ⭐ funnel + revenue
- Kit + Forge tier bundle sold directly to universities: kit ($1K one-time)
  + 5 Fleet seats ($1K per year x 5) + Lab access; ~$5K per institution
- ~20 institutions in year 1 = ~$100K ARR share; **bonus**: top of academic
  funnel that converts to research → startup (Tier 2 & 3 path)

### 8. Custom Bridge adapters / professional services — overflow
- Custom adapters for proprietary protocols (Forge-only); ~$50–$500/hr at
  senior-eng day rate; high margin if scoped tightly

---

## Realistic ARR path (18-month model, no rose-tint)

| Quarter | Builder ARR | Fleet ARR | Forge ARR | Marketplace | Pilot service | Total ARR | Notes |
|---|---|---|---|---|---|---|---|
| Q0 today | ~$0 | ~$0 | ~$0 | ~$0 | ~$0 | ~$0 | Starting point — brutal honesty only |
| Q1 | $2K | $3K | $25K | $0 | $0 | ~$30K | 5 Builder, 0 Fleet, 1 Forge pipeline (~10x in pipeline) |
| Q2 | $6K | $15K | $75K | $1K | $0 | ~$100K | 10 Builder, 5 Fleet, 1 Forge signed |
| Q3 | $15K | $50K | $150K | $5K | $5K | ~$225K | 25 Builder, 15-20 Fleet, 2 Forge, marketplace seed launched |
| Q4 | $30K | $100K | $300K | $15K | $20K | ~$465K | 50 Builder, 40 Fleet, 3 Forge, marketplace 50 skills live |
| Q5 (12 mo) | $50K | $180K | $450K | $30K | $30K | ~$740K | 80 Builder, 70 Fleet, 4-5 Forge, marketplace 100+ skills |
| Q6 (15 mo) | $80K | $260K | $550K | $50K | $40K | ~$980K | ~$1M ARR hit |
| Q7 (18 mo) | $120K | $350K | $700K | $75K | $50K | ~$1.3M | IF the Plan A beachhead lands |

The Q5→Q6 eccentricity is **Forge conversion**: 1–2 Forge deals close from
the pipeline built in Q3–Q4 and take ARR over $1M.

**What can break this model (highlights):**
- Forge sales cycle > 6 mo without CSM muscle hired Q2 → 75% probability estimator
- Marketplace cold-start fails (cold kill) → caps customer retention in Tier 2
- Ind-Eth Bridge unmaintained → AGV beachhead stories don't land → Forge pipeline goes to zero
- One VP-brand competitor (Formant or one with deeper pockets) hires our 1 enterprise AEM

> See [07-red-team.md](./07-red-team.md) for the full failure-mode table.

---

## What NOT to do (Preserve constraints below this resistance line)

1. **Don't raise Builder/Fleet pricing yet** to chase margin on loss-leader
   seats. Pricing power comes from retention character, not from squeezing the
   loss leader. *When Mind + Fleet reduces churn (verifiable retention >24 mo
   at avg LTV of >$1,500 per seat) THEN raise Builder/Fleet pricing by ~30%*.
2. **Don't go commodity-cheap on the marketing** ("cheapest price in robotics"
   in `Lifecycle.tsx:201`). Replace with *"most open at any price."* Price
   leadership creates margin; price-following destroys it on day one.
3. **Don't pitch Forge as "custom"** — "(Custom)" in the price column telegraphs
   "we don't know either." Publish a 3-tier Forge ladder (Team $25K,
   Studio $50K, Enterprise $250K+).
4. **Don't avoid named references** — at any point a customer case study is more
   worth than $50K revenue. So even if you "pass down" revenue for a named
   case-study Forge partner, do that.
5. **Don't ship new product #20** until 12+ of the existing 19 are beta-quality
   or higher; adding catalog SPA surface at this stage burns runway.

---

## The bottom line for the founder

Profitability **is** possible from bootstrapped. **It is not possible from the
current pricing alone.** Profitability requires:

1. Forge flipped from "on-prem white-label" → "managed cloud at $25K+ ACV." **Today.**
2. Marketplace cold-start seeded with 20–30 creator grants in 90 days.
3. A beachhead industry named by Day 30 (recommended: warehouse AGV retrofit).
4. Hire 1 enterprise AE + 1 solutions engineer (the Red Hat muscle) within
   Q2–Q3 of this plan.
5. Hold marketing honestly: replace *"best companies building with
   us"*🖌 with *"first companies joining us"* and put a customer logo
   on the homepage.

**That is the entire profitability thesis.** Revenue math: yes. Engineering
math: in the [08-12-month-plan.md](./08-12-month-plan.md).