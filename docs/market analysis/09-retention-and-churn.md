# 09 — Retention & Churn Analysis

> 📊 Strategy · 💰 Investor — classified stickiness model. The brutally honest
> answer to the user's "why would people stick with the platform or not" —
> including the one mismatch between today's marketing claim and today's
> stickiness reality.

---

## The stickiness classifications

Three flavors of stickiness. The brutal accounting only respects the first
two:

| Class | Definition | Example |
|---|---|---|
| **Earned** | The customer stays because the platform got better with use | Compounding data gravity, trust compound |
| **Imposed** | The customer stays because leaving means losing work | Migration cost, certification a medio |
| **Hoped** | The customer is expected to stay for soft reasons; not measurable | Voice, brand affinity, "good taste" |

> Marketing copy from `app/why/page.tsx:23` literally promises only
> *earned* stickiness: *"the one switching cost that's earned, not imposed."*
> The brutal truth: today, most "stickiness" is *imposed* by Comply
> audit-trail mappings or *hoped* by voice — see §3 below.

---

## 09.1 — Why people will **stay** (earned switching costs, real)

### Earned stickiness #1: Multi-product integration stickiness

Once a customer wires `BuildURDF → Bench calibration → Frame workspace → View
BEV → Serve endpoint → Fleet OTA`, ripping out any one piece breaks the chain.

| Integration point | Cost to undo if customer leaves |
|---|---|
| Records format (LeRobot HF + MCAP) | Re-export to competitor format |
| Adapter (Bridge / Connect) | Re-write the protocol mapping |
| Tracker (Fleet observability stack) | Lose dashboards + alerting history |
| Skills purchased in Market | Re-buy or rewrite skills they no longer own |
| Audit evidence in Comply | Migrate audit trail; potentially recertify |

The catch: **this stickiness only fires after install.** At 0 named
customers today, integration stickiness is purely future-value.

> Today: not yet real. Two years from now *if executed*: medium-real.

### Earned stickiness #2: Dataset compounding

After a customer has ~10K LeRobot-format episodes fine-tuned against OhhO's
training schemas, re-formatting into a competitor's pipeline (HF standalone,
Scale Data engine, internal pipeline) is non-trivial.

> *"Your data, portable forever."* — `lib/copy.ts:28`. That's the wedge.

**Note:** this is not OpenAI-grade data gravity. OpenAI's gravity is in
*their* weights from *your* data; your data is gone the moment you paid for
the API call. OhhO's gravity lives in the *portable artifacts* (datasets,
policies, audit trails). That's a smaller, more honest gravity.

The customer retention pitch simplifies to: *"the cost of LeRobot-format →
competitor-format re-export is real work; staying compounds it."*

> Today: low. Targets: medium at 12-month customer age.

### Earned stickiness #3: Cross-brand skills they bought on Market

A skill tagged for "any mecanum base" runs on the OhhO runtime. If a customer
changes runtimes, the skills they bought run nowhere else. (Or: a competitor's
version of "OhhO Market" accepts the same signed package but rarely ships on
day one.) **Maintenance gotchas for repeat buying compound.**

This becomes *real earned stickiness* — but only if the customer bought at
least one paid skill. Today, paid skill catalog is empty, so this gravity is
forward-looking. (See [03-moats.md](./03-moats.md#unclaimed-moat).)

---

## 09.2 — Why people will **leave** (churn risks, ranked)

### Churn risk #1: Closed humanoid maker releases a comparable brain

A Figure Helix user will leave OhhO if Figure's in-house brain beats Mind on
Figure hardware. **OhhO's open-source value to them is zero on that fight**,
because they're not buying the brain; they're buying the platform. If the
platform (Mind-on-Figure) has worse success rate than the in-house brain
(Helix-on-Figure), every Figure owner churns.

→ **Treat as "feature-overspecialized churn"**: compete with structured
brains by orchestrating them; concede narrow wins gracefully.

### Churn risk #2: Build/Comply/Shield/Proof stays unshipped too long

Right now 4 of 19 are roadmap (`PRODUCT_STATUS`). A customer who chose OhhO
**for the lifecycle** leaves if the lifecycle doesn't materialize within ~12–18
months.

The current `lib/products.tsx:109` honesty mitigates this risk on the website
surface (statuses are labeled truthfully). But it's still possible for a paying
customer at year-2 to see 4 of 19 still unshipped and decide to revert to
Formant + manual certs.

→ **Mitigation already partly done; preserve the `PRODUCT_STATUS` accuracy.**

### Churn risk #3: GitHub OSS movement allows ambitious teams to DIY

PickNik + Intrinsic + Foxglove + HF LeRobot + Open-RMF + MoveIt are ALL OSS.
A well-resourced enterprise team can DIY the entire OhhO stack from those
components — at the cost of integration engineer-feels-of-the-quarter. They
will leave OhhO once they realize they can bootstrap.

This is the **classic "OEM moat is the others' engine" risk** for OSS
platforms. Mitigations:
- Maintain velocity advantage (most OSS neighbors move slow)
- Offer Forge value (managed cloud, CSM, SLA) at a price cheaper than
  a full eng-team DIY

### Churn risk #4: Foundation lab releases free humanoid policy

Mind's agent layer depends on a cloud LLM. If OpenAI / Anthropic give away a
robotics-aware reasoning model with teleop-coded function calls, Mind's RTE
proposition weakens. A paying customer can run the BaseModel-with-free-LangSmith
because OpenAI released it.

→ **Mitigation**: the agent loop is the moat, not the LLM. If a foundation
lab releases the LLM behavior, OhhO packages that — Mind is the orchestration
loop, not the underlying LLM.

### Churn risk #5: Brand-agnostic copy pushes away closed-sdk OEMs

The site explicitly states (`BrandAgnostic.tsx:162`): *"Closed, walled-garden
platforms aren't the target — but if a vendor opens up, OhhO's adapters plug
them straight in."*

This is the correct wedge; it also means **Figure / Unitree / Tesla / 1X /
Apptronik will (~99%) never become partners.**  Some VC investors may see this
as locking out the hottest names; accept that trade — OhhO cannot win on
Figure's home turf anyway.

→ The flip: **accept the trade externally.** Make the *brand-agnostic quote*
pitch deck-front-and-center instead of hiding it in the AGENTS.md rules.

---

## 09.3 — The most dangerous churn risk: today's Trust-Row mismatch

The homepage trust row (`copy.ts:24`):

```
"Apache-2.0 engine",
"Works with LeRobot, ROS 2 & ONNX",
"Runs on a Pi, a laptop, or your cloud",
"Your data, portable forever",
```

The pricing page Fleet plan (`Pricing.tsx:53-71`):

> "OhhO Fleet — OTA updates, up to 100 robots
>  ...
>  Comply + Shield — compliance & CVE watch"

Comply + Shield are roadmap (`PRODUCT_STATUS` = "planned"). Comply makes audit
trails (the *imposed* stickiness moat); Shield makes signed OTA (the security
moat). **Mixing these on a Fleet plan feature list reads like products shipping
when they don't ship.**

When a buyer subscribes Fleet **today and discovers Comply doesn't exist**,
the discrepancy hits in the first 30 days. Churn risk is high; trustworthiness
brand scar is permanent.

### Recommended fix (this week)
- Replace "OhhO Comply + Shield — compliance & CVE watch" wording on the Fleet
  plan card with: *"Roadmap in 2026: Comply + Shield — join the waitlist."*
- Add a "Waitlist" CTA so the early access is opt-in. Pricing transparency
  becomes a feature, not a complaint fax.

### What this fixes
- A buyer today signs Fleet knowing what's *not* included. Renewal next year
  is a contract upgrade, not a recrimination.

### Survival rating without this fix
- Customers WILL ask in customer calls, *"...so where's Comply?"*. Answer:
  *"we have it on the roadmap."* Quote: **75% probability of canceling Fleet
  plan in first month, 40% probability of public trust loss within short
  customer LTV.**

---

## 09.4 — The brutal verdict timeline

### Today
- **Earned stickiness: low.** Install depth is shallow; Marketplace is empty;
  audit trails don't exist; cert evidence doesn't exist.
- **Imposed stickiness: low.** Apache-2.0 engine, portable datasets, ONNX
  exports — by design, leaving is easy. **That's the wedge, not the moat.**
- **Hoped stickiness: brand affinity; flavor; OSS community.** Real but
  soft.

### Two years from now *if execution ships*
- Earned stickiness moves to **medium**:
  - Compounding flight-of-skills data on Market
  - Compounding LeRobot-format episodes per customer (~10K+)
  - Audit trail locked into Comply + Proof evidence
- Imposed stickiness: medium (audit-trail migration cost is real; republishing
  Forge custom adapters takes engineering)
- Hoped stickiness remains soft

### Becoming Red Hat of robotics (year 4)
- Earned stickiness: **high** (LTS releases; brand-eyes; customer community)
- Imposed stickiness: high (Comply audit-trail lock at enterprise contracts
  becomes expensive to migrate..dressing up as open)
- This is exactly the OSS-to-enterprise pattern; Red Hat earned it after 10+
  years.

### Probability today: low → becoming medium by year 2
- Requires execution of [08-12-month-plan.md](./08-12-month-plan.md), not
  narrative.
- A slow drift along the current trajectory keeps current retention: low
  stickiness, low churn numbers (because zero customers means zero churn).

---

## 09.5 — One last honest line

Open in this category: **None of the marketing moats in
`app/why/page.tsx:19-44` are alive today**. They are forward-looking claims.
The company says so itself (`why/page.tsx:103`):

> *"Come try it — it's safe, you can always leave. Great for getting in the
> door. Terrible as the whole strategy, because it lowers the barrier to leave,
> too."*

This is unusually self-aware marketing. The point of this section: **self-aware
marketing is not the same as having the products that make it true.** Until
Comply, Shield, Proof, Marketplace inventory, and Mind closed-loop ship
end-to-end on a real customer, today's stickiness is *hoped* — not earned, and
one of the in-the-door customers could walk in an afternoon with everything
they made.

That's the trade the founder chose. The job is to make it less than fully
true by shipping the missing 4–7 products in 12 months — not by adding new
ones.

> See [08-12-month-plan.md](./08-12-month-plan.md) for the ship sequence that
> converts the stickiness from *hoped* to *earned*.