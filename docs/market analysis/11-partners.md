# 11 — Partners: What, Why, Who, and the First 10 Outreach Targets

> 📊 Strategy · 💰 Investor — defines what "partner" means for OhhO, the
> screening profile of a good partner, the full list of partners to consider
> (organized by category), and the 10 highest-leverage outreach targets for the
> next 30 days.
>
> Companion to the [12-month plan](./08-12-month-plan.md) — partnerships are
> the lowest-cost, highest-leverage moves in Gate 1 (beachhead), Gate 4
> (design partner), Gate 6 (Bridge + cert), and Gate 7 (Forge revenue funding
> the first hires).

---

## 11.1 — What "partner" means for OhhO (5 types)

When the README and the red-team use "partner," it means one of five formal
relationship types. Today OhhO has zero of any of them, which is why the red-team
flags it as a top-3 risk.

| Type | Definition | Why OhhO needs it |
|---|---|---|
| **1. Design / customer partner** | A paying pilot who lets OhhO use their logo + a case study. Not a free user — a co-development customer. | Fixes the "0 named customers" red-team risk + Forge pipeline |
| **2. Technology integration partner** | A company whose product OhhO integrates with (or vice versa), often with a joint engineering track. | Extends the platform surface without OhhO building it (e.g. Unitree adapter, Scale data feed) |
| **3. Channel / distribution partner** | A reseller, integrator, or OEM who sells OhhO into a vertical OhhO can't reach directly. | Reaches warehouse/industrial buyers without hiring a direct sales team |
| **4. Standards / certification partner** | A cert body (UL, TÜV) or standards org that OhhO co-engages with on compliance. | The Comply-product procurement unlock; nobody in robotics has this software-side |
| **5. Co-marketing / OEM partner** | A brand (often a robot OEM) who co-presents with OhhO at conferences, in blog posts, co-ships pilots. | Top-of-funnel; the closest non-revenue version of a customer partnership |

> Every single one of these is in the **`0 today`** column. The fix is the 10
> outreach targets in §11.4 — not a 100-person partner program.

---

## 11.2 — What a good OhhO partner looks like (the screening profile)

A partner company worth OhhO's limited outbound time should satisfy **at least
3 of these 6 criteria**:

1. **Open ecosystem alignment** — they keep their SDK / firmware / API open, OR
   they're a cert body / standards org whose job is neutrality. (Figure, Tesla,
   1X consumer are excluded by this criterion.)
2. **Adjacent, not competitive** business model — they sell hardware and need a
   brain/ops layer, or they sell data and need a deployment target, or they sell
   cert and need software prep. Not a brain-lab competing with OmniVLA.
3. **A named, reachable decision-maker** a 4–10 person team can actually DM.
   Skip the "validate via portal" Ultrascaleque plays for year 1.
4. **Compounding** — one partnership unlocks reference customers, not just one
   logo. (A Unitree partnership leaks to every G1/H1 owner; a single customer
   pilot does not.)
5. **Budget or equivalent in-kind** on the partner side — they bring engineering
   hours, marketing surface, or co-funding, not just a logo.
6. **Doesn't require OhhO to violate** `website/AGENTS.md` §2 brand-agnostic
   rule — the partner page can stay brand-free even though the partnership
   names the partner.

### Anti-profile (DO NOT pursue)

- **Closed-SDK OEMs** who'd require OhhO to drop the OSS-Switzerland posture:
  Figure, Tesla Optimus, Sanctuary, 1X consumer branch.
- **Well-funded horizontal brain labs** where OhhO's pitch reads as *"we're a
  worse-funded competitor"* rather than *"we're an open operating layer for
  your brain."*
- **Any partner demanding exclusivity** — exclusivity dissolves the "any robot"
  thesis that is OhhO's only structural moat.

---

## 11.3 — The full partner list (by category, ranked inside each)

### A. Robot hardware brand partners (Tier 2A from `05-pmf.md`)

*The "open-software brands welcome" segment from `BrandAgnostic.tsx`. Each one
is a potential paid Forge partner.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Trossen Robotics** (SO-101 arm, multi-arm rigs) | LeRobot-native, OSS heritage, the SO-101 ecosystem the OmniBot repo is built on | ⭐⭐⭐ | Founder DM — they may want to co-brand an "Open Brain Stack" |
| 2 | **K Scale Labs** (open-source humanoid) | Pure OSS humanoid; underfunded brain; OhhO = their ops layer for free | ⭐⭐⭐ | GitHub-based intro — they live on Discord/HN |
| 3 | **Pollen Robotics** (Reachy) | Open robot arm + Poppy ecosystem; French; aligned with LeRobot (HF Films robotics) | ⭐⭐⭐ | Founder DM (French OSS robotics is small) |
| 4 | **Standard Bots** (RO1, Parker, catories cobot arms) | US-made open-software cobots; strong product, weaker AI story; perfect complement | ⭐⭐ | Sales-led intro via their BD team (NY-based) |
| 5 | **Reach Robotics** (open underwater ROVs) | Diversifies OhhO's "any robot" demonstration; OpenSDK underwater | ⭐⭐ | Conference-side intro (Oceanology / BLUE) |
| 6 | **Unitree** (G1, H1, Go2) | OhhO adapters already support them; huge research reach; semi-open SDK | ⭐⭐ | Co-marketing MoU; pitch "Unitree + OhhO OS research bundle" |
| 7 | **Awake Robotics** | Open manipulation SDK, India-based, fast-mover | ⭐ | GitHub outreach |
| 8 | **Solomon / healeur AI** | 3D vision + arms, Asia markets | ⭐ | Conference intro |
| 9 | **Ghost Robotics** (Spirit 40 / 45 quadruped) | Open-API quadruped, DoD-adjacent, US | ⭐⭐ | Via defense-robotics intros |
| 10 | **Open Source Robotics Foundation** (ROS 2 steward) | Not a hardware OEM but the canonical strategic OSS robotics partner | ⭐⭐⭐ | Apply to ROS 2 Industrial Consortium; sponsor a working group |

### B. Foundation-model / brain-lab partners (coordinate, don't compete)

*The "Switzerland seat" plays. Pitch: "we deploy your brain across hetero-brand
fleets with cert + OTA."*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Hugging Face / LeRobot** | They have the catalog (188 datasets / 59 models); OhhO has the deploy bundle format. Lose-this-or-die partner. | ⭐⭐⭐ | Founder-to-Clem or Remi intro this quarter; propose deployment-format partnership |
| 2 | **Physical Intelligence (π)** | Open-weights π0/π.5/π0.7 on HF; needs an operating partner for fleet deploy | ⭐⭐⭐ | Founder-to-founder intro; ship a "π0.5 deployed via OhhO Serve + Fleet" demo |
| 3 | **Skild AI** | Same omni-bodied thesis; closed brain; needs deploy layer | ⭐⭐ | Cold founder-email with the OSS-Switzerland pitch deck |
| 4 | **Toyota Research Institute** | Large-scale funder of open robotics research; not a brain for sale, but a research partner | ⭐⭐ | TRI academic outreach program |
| 5 | **NVIDIA Isaac / Isaac Lab** | Isaac Sim already in `frame/twin`; co-marketing partner; NVIDIA Inception program | ⭐⭐⭐ | Apply to NVIDIA Inception (free GPU credits + co-marketing) |
| 6 | **Microsoft (ONNX Runtime)** | ONNX is core to OhhO Train/Serve/Fleet export | ⭐⭐ | Microsoft AI for Good / Open Source Ambassador program |
| 7 | **Intel (OpenVINO)** | ONNX execution provider; edge inference | ⭐ | Intel Edge Insight partner program |
| 8 | **Google DeepMind** | RT-X, Gemini Robotics; potential research-collab partner | ⭐ | Academic-track intro |

### C. Industrial / factory-floor / VDA 5050 partners

*These unlock the recommended AGV-retrofit beachhead from `05-pmf.md`. The
Bridge speaks their protocols (`website/AGENTS.md` §3).*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **MiR** (Mobile Industrial Robots / Teradyne) | VDA 5050 AGV vendor; biggest AMR OEM behind Geeks+ | ⭐⭐⭐ | BD intro; pitch OhhO Fleet as VDA 5050 fleet-manager |
| 2 | **Toyota Industries** (BT, Vanderlande) | Largest forklift/AGV maker globally; VDA 5050 ecosystem | ⭐⭐ | Via industrial partner channel (TTI Corp Dev) |
| 3 | **KION Group** (Dematic, Linde, Still) | Major warehouse automation; VDA 5050 commitments | ⭐⭐ | Dematic integration partner program |
| 4 | **Linde Material Handling** | VDA 5050 forklift leadership; KION subsidiary | ⭐ | Via KION |
| 5 | **Geek+** | Large warehouse AMR; China-based; less aligned | ⭐ | China partner via OEM |
| 6 | **Locus Robotics**, **6 River Systems** | Goods-to-person AMRs; REST APIs available | ⭐⭐ | Direct BD intro |
| 7 | **AutoStore** | Cube-style warehouse automation | ⭐ | Partner program |
| 8 | **Siemens** (PROFINET, Xcelerator) | Bridge PROFINET adapter = Siemens ecosystem unlock; Xcelerator partner program | ⭐⭐⭐ | Apply to Siemens Xcelerator (open ecosystem) |
| 9 | **Rockwell Automation** (EtherNet/IP, PartnerNetwork) | EtherNet/IP = Rockwell; PartnerNetwork is open | ⭐⭐⭐ | Apply to Rockwell PartnerNetwork "Design Partner" |
| 10 | **Bosch** (Bosch.IO, Industry 4.0) | Active in NN/edge manufacturing; open API | ⭐⭐ | Bosch Startup Innovation program |
| 11 | **KUKA / ABB / Fanuc / Yaskawa / Universal Robots** | ROS-Industrial compatibility already claimed; depth varies | ⭐⭐ | Adopt ROS-Industrial Consortium leverage |
| 12 | **Dematic / SSI Schaefer / Knapp** | Warehouse system integrators — *the channel partners* for AGV retrofit | ⭐⭐⭐ | SI partnership outreach (they need software integrators of OhhO's size) |
| 13 | **Open Robotics + Open-RMF** | Open-RMF is in Fleet already; formalize the partnership | ⭐⭐⭐ | Contribute upstream; sponsor a working group |

### D. Simulation / digital-twin partners

*Already-used tools that should be formal partnerships, not just dependencies.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Open Robotics** (Gazebo, Open-RMF) | The ROS 2 community home; OhhO built on it | ⭐⭐⭐ | Sponsor OSRF; join Board-tier if possible |
| 2 | **NVIDIA Isaac Sim** | Already in the stack; NVIDIA Inception co-marketing | ⭐⭐⭐ | NVIDIA Inception application (free GPU + co-marketing + Isaac compat badge) |
| 3 | **Foxglove Studio** | Open-source robotics visualization; already on the Pi (`AGENTS.md` mentions Foxglove) | ⭐⭐ | Co-marketing; propose reciprocal blog post |
| 4 | **PickNik Robotics** | MoveIt Studio open-source motion planning | ⭐⭐ | Co-marketing with coordination pitch |
| 5 | **Dassault Systèmes / 3DEXPERIENCE** | Industrial digital twin; USD-native (OhhO Twin uses USD) | ⭐ | Enterprise partner program (slow) |
| 6 | **Ansys / Siemens Process Simulate** | Industrial simulators | ⭐ | Slow cycle; defer |

### E. Data / HITL / labeling-service partners

*For the Data + Train flywheel.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Scale AI Robotics** | The category leader; competitor in data engine but partner on integration | ⭐⭐⭐ | Discuss via Scale BD; pitch LeRobot-format outflow |
| 2 | **Dataloop, Labelbox, Encord, V7 Labs** | Labeling tools — integrate via API as annotation source for `Data` | ⭐⭐ | API partnerships |
| 3 | **Turing.com, Sky Alliance, Alegion** | HITL labor at scale; possible channel into enterprise data teams | ⭐ | Outreach on cold-mode |
| 4 | **Mozilla Common Voice analog for robotics** | No robotics-data-cooperative yet; opportunity | ⭐⭐ | Could be an own-initiative play |

### F. Certification / regulatory partners (the *real* moat-creators)

*From `lib/comply/standards.ts` — no robotics operating layer currently partners
with these. This is OhhO's defensible periphery.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **UL** (Underwriters Laboratories) | US safety cert; most recognized; opens US enterprise procurement | ⭐⭐⭐ | Cold outreach to UL Digital Partner team |
| 2 | **TÜV SÜD** | German cert; opens German automotive + manufacturing | ⭐⭐⭐ | Cold outreach to TÜV SÜD Digital Hub |
| 3 | **TÜV Rheinland, TÜV NORD, DEKRA** | German/EU cert bodies | ⭐⭐ | Bundled with TÜV SÜD pitch |
| 4 | **SGS** | Global cert leader | ⭐⭐ | Geneva-based outreach |
| 5 | **Bureau Veritas, Intertek** | Global cert | ⭐⭐ | Partner programs |
| 6 | **NSF International** | US testing + cert | ⭐ | Outreach after UL land |
| 7 | **NIST (Cybersecurity Framework, AI RMF)** | Standards body, not cert; Bridge Shield pairs with NIST CSF | ⭐⭐ | NIST AI Safety Institute Consortium (AISIC) — apply |
| 8 | **ISO (TC 299 robotics, TC 184 industrial automation)** | Standards coordination | ⭐⭐ | Become a participating member via national body |
| 9 | **ANSI / RIA R15.06** | US robotics standards; refer Comply to `app/standards/page.tsx` | ⭐⭐ | A3 / RIA membership |

### G. Cyber / security partners (for Shield)

*Shield is roadmap but Forge SSO + secure boot requires partner brand.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Trail of Bits / NCC Group** | OSS security audits; EH dev partner for Shield roadmap | ⭐⭐⭐ | Engagement for an initial OhhO OS security audit (de-risking asset + marketing) |
| 2 | **Chainguard** | OSS container security; SBOM pro | ⭐⭐ | Co-marketing; SBOM tooling |
| 3 | **Snyk, Sonatype, Anchore** | SBOM tooling vendors | ⭐⭐ | Integrations into Shield `docs/ohho-os/install.md` |
| 4 | **Synopsys (Black Duck / Code Sight)** | SBOM | ⭐ | Enterprise track |
| 5 | **Wiz** | Cloud security posture | ⭐ | Late-stage once managed cloud ships |

### H. Cloud / infrastructure / distribution partners

*GPUs are OhhO's biggest variable cost; managed-cloud partner credits matter
more than any cert at this stage.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **AWS Activate** | $1K–$100K credits, marketplace distribution | ⭐⭐⭐ | AWS Activate application — fastest free win |
| 2 | **NVIDIA Inception** | Free GPU credits, Isaac compat, co-marketing, technical support | ⭐⭐⭐ | NVIDIA Inception application — same week |
| 3 | **Microsoft for Startups Founders Hub** | Azure credits, ONNX partnership leverage | ⭐⭐⭐ | Founders Hub application |
| 4 | **Google for Startups Cloud Program** | GCP credits + HF LeRobot bundling | ⭐⭐ | Application after first Forge customer |
| 5 | **Modal, RunPod, Lambda Labs, CoreWeave** | GPU-on-demand; alternative to AWS for Train/Serve | ⭐⭐ | Integration partnerships; pricing leverage |
| 6 | **Vercel** (currently hosting the website) | Forge-as-managed-cloud co-hosting partner | ⭐⭐ | Vercel for Startups program; upgrade tier |
| 7 | **Supabase** (already used for auth/DB) | Already a dependency; extend to Forge-managed tenant DB | ⭐⭐ | Supabase partnerships |
| 8 | **Fly.io** | Global edge deploy; alternative Forge host | ⭐ | Trial |
| 9 | **DigitalOcean for Startups** | Affordable cloud; small-team friendly | ⭐⭐ | Apply (fast, easy) |

### I. VR / MR / teleop partners

*Pilot uses OpenXR; co-marketing unlocks the MR-teleop narrative.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Meta (Reality Labs, Quest)** | Quest 3 is the runtime platform; Meta already participates in OSS (Android) | ⭐⭐⭐ | Meta Reality Labs ISV program (Pilot VR app originator program) |
| 2 | **HTC Vive** | OpenXR headsets; enterprise XR focus | ⭐⭐ | ISV partner program |
| 3 | **ByteDance Pico** | OpenXR headset; cheaper global | ⭐⭐ | ISV partner program (sizable consumer reach) |
| 4 | **Ultraleap (Leap Motion)** | Hand tracking IP — Pilot hand-IK leverages them | ⭐⭐ | Co-marketing; technical alignment |
| 5 | **Varjo** | Enterprise XR; high-fidelity for commercial teleops | ⭐ | Niche; later |
| 6 | **Microsoft Mixed Reality (HoloLens, Mesh)** | Enterprise MR; slower-moving but powerful | ⭐ | Long cycle; defer |
| 7 | **OpenXR Working Group (Khronos)** | Open specification steward | ⭐⭐⭐ | Join Khronos adopter program |

### J. Academic / research consortium partners

*Top-of-funnel for the Tier-1 ICP, and Forge-pipeline primer via founders
spinning out labs.*

| # | Institution | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **CMU Robotics Institute** | Top robotics school; many grad-student potential users | ⭐⭐⭐ | Cold-email robotics faculty; offer $25K "OhhO Academic Pack" |
| 2 | **MIT CSAIL / Biomimetics** | Same | ⭐⭐⭐ | Same outreach |
| 3 | **Stanford SAIL / Iliad Lab** | Imitation learning pioneer; aligned with LeRobot work | ⭐⭐⭐ | Same |
| 4 | **UC Berkeley (BAIR / Pi-Research)** | Robotics + AI cross-flow | ⭐⭐⭐ | Same |
| 5 | **Georgia Tech / IRIM** | Manufacturing + robots | ⭐⭐ | Same |
| 6 | **ETH Zurich (RPG, ASL, Robotic Systems Lab)** | European OS robotics; aligned with LeRobot | ⭐⭐⭐ | Same |
| 7 | **KAIST, Imperial, TU Munich, Oxford, KAUST** | International academic presence | ⭐⭐ | Same template |
| 8 | **ROS 2 Industrial Consortium** | Industry-OS coordination forum | ⭐⭐⭐ | Apply as a member (~$2K-$5K/yr) |
| 9 | **MLCommons / MLPerf** | Robotics benchmarks are forming; seize the seat | ⭐⭐⭐ | MLPerf Robotics submission (cited as G1 in `PRODUCT_DIRECTIONS.md`) |
| 10 | **OpenAI Academic program** / **Anthropic Academic** | LLM API credits for Mind demos | ⭐⭐ | Apply (frequently free for verified labs) |

### K. GTM / channel / distribution partners

*For the AGV-retrofit beachhead — sell through, not around.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Dematic** (KION) | World's #2 warehouse integrator; directly aligned with Bridge VDA 5050 | ⭐⭐⭐ | SI partnership outreach |
| 2 | **SSI Schaefer, Knapp, Witron, Vanderlande** | Major warehouse integrators | ⭐⭐ | Partner program outreach |
| 3 | **Arrow Electronics / Avnet** | Components distribution + services integrator channel | ⭐⭐ | Arrow IoT partner program |
| 4 | **Digi-Key, Mouser, RS Components** | Build BOM supplier links (`website/AGENTS.md` §4) | ⭐⭐⭐ | Affiliate / supplier link programs (easy free-win for Build sourcing) |
| 5 | **PTC (Vuforia, ThingWorx)** | AR / industrial IoT platform | ⭐ | Longer cycle |
| 6 | **Bosch.IO** | Multi-vendor IoT integrator | ⭐⭐ | Bosch open-source partnerships |

### L. Observability / DevOps partners

*Fleet already uses Prometheus/Grafana/Loki/Tempo — formalize what's already in
`infra/observability/`.*

| # | Company | Why fit | Priority | First move |
|---|---|---|:-:|---|
| 1 | **Grafana Labs** | Direct dep stack overlap | ⭐⭐⭐ | Grafana Startup program + co-marketing blog |
| 2 | **Honeycomb** | Observability for ML pipelines | ⭐⭐ | Startup program; integration |
| 3 | **Weights & Biases** | Already integrated; deepen to W&B Academic + Startup tier | ⭐⭐⭐ | W&B for Startups (free tiers); co-author benchmark blog |
| 4 | **Sentry** | Error tracking for launched websites/pilots | ⭐ | Free tier |

### M. Funding / accelerator partners (only if/when raising)

*Bootstrapped today; list ready if raising becomes needed.*

| # | Investor / program | Fit | Priority if raising |
|---|---|---|:-:|
| 1 | **NVIDIA Inception** | Free GPU + co-marketing; close to "no equity" | Highest first stop |
| 2 | **AWS Activate / Microsoft Founders Hub** | Cloud credits, no equity | Highest first stop |
| 3 | **HAX (SOSV)** | Hardware accelerator, equity-stage | If hardware productization contemplated |
| 4 | **Eclipse Ventures** | Funded Formant; robotics-specialist VC | Series-A stage |
| 5 | **Lux Capital, Playground Global** | Deep-tech VC | Series-A stage |
| 6 | **Initialized Capital, Boost VC** | Early deep tech | Pre-seed stage if you decide to raise |
| 7 | **Refactor Capital** | Deep-tech early | Pre-seed |
| 8 | **YC / On Deck / Techstars AI** | Accelerator track | If accelerator-style runway helps |

---

## 11.4 — The first 10 outreach targets (do these in 30 days)

If you have bandwidth for only 10 founder-DMs in the next 30 days, here is the
ranking I'd execute. **Each one directly enacts a top failure-mode fix from
`07-red-team.md`:**

| # | Target | Type | Red-team risk it fixes | Time-to-pitch |
|---|---|---|---|---|
| 1 | **Hugging Face LeRobot** (Clement / Mishig / Remi) | Tech integration | #5 (Market catalog eaten by HF) | 1 week |
| 2 | **Trossen Robotics** | Customer / OEM | #1, #2 (no beachhead, no customer logo) | 1 week |
| 3 | **NVIDIA Inception** | Cloud / marketing | #7 (margin-negative cloud cost) | Apply in 24h |
| 4 | **AWS Activate** | Cloud credits | #7 | Apply in 24h |
| 5 | **K Scale Labs** | Customer / OEM | #1 | GitHub/Discord intro |
| 6 | **Meta Reality Labs ISV (for Pilot)** | VR partnership | #4 Formant closes Pilot tier | 2 weeks |
| 7 | **MiR (Teradyne BD)** | Channel / customer | #1, #8 (Bridge ind-Eth depth) | 2 weeks |
| 8 | **UL Digital Partner team** | Cert partner | #4 (Forge credibility) | 4 weeks |
| 9 | **CMU + Stanford + MIT + Berkeley robotics faculty** (4 cold emails) | Academic design partners | #2, #10 (mind-loop demo) | 1 week each |
| 10 | **Scale AI Robotics** | Tech integration / data engine partner | #3 (data flywheel impossible alone) | 4 weeks |

### Why this ordering

1. **HF LeRobot is the single highest-leverage move** — without it, by the time
   Forge lands its first customer, LeRobot's catalog will have 300+ skills and
   OhhO Market will have ~10. **Founder-to-founder intro to HF this week is
   non-negotiable** per the red-team verdict in `07-red-team.md` (failure mode
   #5).
2. **Trossen + K Scale** unlock the "named customer" fix — and Trossen is
   already in OhhO's own stack (SO-101 is the reference arm in `AGENTS.md`),
   so the pitch is "ship what we already built together." K Scale is
   OSS-native and reachable on GitHub/Discord.
3. **NVIDIA Inception + AWS Activate** are applications, not DMs — file them
   the same day. They unlock free GPU credits that turn the margin-negative
   Builder/Fleet retail ladder into a temporarily margin-neutral funnel.
4. **Meta Reality Labs** is the only partner who can dissolve Pilot's
   distribution risk; if Formant or another ops competitor signs them first,
   OhhO's MR-teleop narrative gets ceded.
5. **MiR + UL** are the beachhead (warehouse AGV) + the trust machette (cert).
   Both have formal partner channels, both are slow, so start now.
6. **CMU/Stanford/MIT/Berkeley cold emails** are the cheapest possible Forge
   pipeline primer — 4 emails cost nothing and may produce 1–2 paid pilots
   when those labs get funded.
7. **Scale AI** is harder but compounds: if they accept LeRobot-format data
   inflow, OhhO becomes a data-feed into their enterprise pipeline — and they
   have the SOC 2 / ISO 27001 / global labor pool that OhhO structurally
   lacks today.

### Why this list is **not** 100 long

The screening profile (§11.2) rules out most "interesting" names:

- **Figure, Tesla Optimus, Sanctuary, 1X consumer arm** — closed SDK + vertical
  brain; OhhO cannot partner without dropping the OSS-Switzerland posture.
- **Apptronik, Agility, Fulbit** — closed humanoid OEMs with named
  customer logos now; they don't need OhhO, and OhhO can't offer them
  anything they don't already have.
- **OpenAI, Anthropic, Google DeepMind foundation model partnerships** —
  these are foundation labs, not partner-OS deployments. They are users
  of HF LeRobot, not OS deployments of OhhO.
- **Stripe / Notion / Linear / Vercel-level "startup partner programs"** —
  these are excellent for credits/distribution but they're not partners of
  OhhO in particular; they accept anyone.

The 10 above *are* OhhO-specific moves. They are not interchangeable with
anyone else's partner playbook. They use OhhO's structural opening (OSS
Switzerland seat, LeRobot-format native, OpenXR-agnostic Pilot, Bridge
ind-Ethernet depth, AGV-retrofit beachhead) that no other robotics OS
competitor can offer in the same bundle.

---

## 11.5 — How each partner type fixes a red-team risk

A one-page cross-reference between partner tiers and `07-red-team.md` failure
modes:

| Failure mode (# from red-team) | Partner tier this file fixes it |
|---|---|
| #1 — No beachhead | A (robot OEMs), K (warehouse SIs), J (academic) |
| #2 — "Best companies" credibility cliff | A (Trossen, K Scale), J (academic logos) |
| #3 — 4 eng can't ship 19 | B (Hugging Face LeRobot catalog), H (NVIDIA Inception) |
| #4 — Formant closes Forge pipeline | A (named OEM partner), F (UL/TÜV), I (Meta Reality Labs) |
| #5 — HF LeRobot out-catalogs | B-1 (Hugging Face LeRobot) — the partnership itself |
| #6 — Skild+Scale+Formant consortium | B (Skild / PI / HF / NVIDIA consortium) |
| #7 — Margin-negative retail | H (AWS Activate, NVIDIA Inception, MS Founders Hub) |
| #8 — Bridge ind-Eth reference-only | C-8 (Siemens Xcelerator), C-9 (Rockwell PartnerNetwork) |
| #9 — Brand caution kills fundraising | L (W&B), J-9 (MLCommons), F (NIST AISIC) — credibility building blocks |
| #10 — Eng team burn-out | J (academic co-development), B-2 (PI partnership), B-6 (Microsoft ONNX) |
| #11 — Mind unproven at scale | A (Unitree + Trossen co-build), J (CMU/Stanford/MIT/Berkeley pilots) |

The pattern: **no single partner fixes more than 2 failure modes; but the top
10 partners together cover all 11.** That's why the 10 list is not optional.

---

## 11.6 — Companion documents needed next

To make the partner list actionable (not just a spreadsheet), the following
files would complete it:

| File | What it adds |
|---|---|
| `12-outreach-sequence.md` | Email / LinkedIn templates for the top 10 DMs; weekly cadence templates; meeting agenda for each partner type; what to ask, what to offer |
| `13-partner-program-policy.md` | Internal rules: the contract template, what's exclusive and what isn't, the logo-usage policy, the launch co-marketing obligations |
| `14-partner-OKRs.md` | Quarterly OKRs by partner type (X design partners / Y tech-integration partnerships / 1 signed cert-partner MoU / 2 academic pilots) — ties partnerships into the 12-month plan |

If useful, the next step is writing `12-outreach-sequence.md` with executable
message templates for the top 10 outreach targets — say the word.

---

## Cross-references

- [07-red-team.md](./07-red-team.md) — the 11 failure modes, mapped to partner tiers in §11.5 above.
- [08-12-month-plan.md](./08-12-month-plan.md) — the partnerships are part of Gates 1, 4, 6, and 7.
- [10-competitor-teardowns.md](./10-competitor-teardowns.md) — Skild, Formant, HF LeRobot rated; the partnerships here are how OhhO turns competitor pressure into coordination advantage.
- [05-pmf.md](./05-pmf.md) — the three-tier ICP those partnerships feed into (research → lab spinout → Forge middle-market and warehouse AGV retrofit).
- [03-moats.md](./03-moats.md) — the cross-brand skills marketplace moat lives or dies on #1 (Hugging Face LeRobot) in §11.4.
- `website/components/BrandAgnostic.tsx` — the brand-agnostic rule that gates which partners OhhO can name publicly on the homepage vs internally.