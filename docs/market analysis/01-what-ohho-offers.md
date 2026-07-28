# 01 — What OhhO Actually Offers

> 📊 Strategy · 🛠 Engineering — the source-of-truth product catalog and **honest
> ship status**, lifted directly from `website/lib/products.tsx:109` so you can see
> what is real and what is a roadmap.

---

## Mission and one-liner

From `lib/copy.ts:48`:

> *"The open data-and-deployment layer for affordable robots — where openness gets
> you in and your data keeps you."*

From `website/AGENTS.md` §1:

> *"Brand-agnostic. Hardware-agnostic. Standards-based. Open-source, zero
> lock-in. Robot-agnostic."*

From `app/about/page.tsx:30`:

> *"We're building the Multimodal Data Stack for embodied AI. Some of the best
> companies in the world are building the future of AI in the physical world with
> OhhO."*

⚠️ The third sentence is **brutally wrong to ship** at $0 ARR with zero named
customers. See [07-red-team.md](./07-red-team.md#best-companies) for the full
takedown.

---

## The 19-console product catalog

Source: `website/lib/products.tsx` (lines 145+). The `accent` colors and
`category` labels are the company's own internal taxonomy. The `Status` column is
the **honest, internal-only** ship status declared at
`lib/products.tsx:109` (`PRODUCT_STATUS`). It is rendered truthfully on the
homepage product grid today; this analysis accuses the *marketing* of
overclaiming, not the catalog data — which is excellent honesty.

### Status key
- 🟢 **Available** — production-shipping (working console + working CLI API)
- 🟡 **Beta** — partially shippable (dashboards/handles exist, full loop not yet
  closed at quality)
- ⚪ **Roadmap** — not yet backed by an end-to-end working feature

### Catalog

| # | Product | Tag | Category | Status | Plan unlock |
|---|---|---|---|:-:|---|
| 1 | **OhhO Build** | Design any robot | Design | 🟡 | Spark → Forge |
| 2 | **OhhO Frame** | Robot stack in an afternoon | Foundation | 🟡 | Every plan |
| 3 | **OhhO Bench** | From parts to powers-on | Foundation | 🟡 | Spark → Forge |
| 4 | **OhhO Connect** | One link · any transport | Foundation | 🟢 | Every plan |
| 5 | **OhhO Bridge** | Connect non-ROS robots | Foundation | 🟡 | Builder (1 adapter) → Forge |
| 6 | **OhhO Serve** | Robot AI inference as an API | Intelligence | 🟢 | Builder → Forge |
| 7 | **OhhO View** | 4 cameras → 1 BEV | Intelligence | 🟡 | Every plan |
| 8 | **OhhO Data** | Collect · Label · Ship | Intelligence | 🟢 | Spark → Forge |
| 9 | **OhhO Train** | Demos → policies | Intelligence | 🟢 | Spark → Forge |
| 10 | **OhhO Autonomy** | Map · Navigate · NL agent | Intelligence | 🟡 | Spark → Forge |
| 11 | **OhhO Mind** | Goal-driven robot brain | Intelligence | 🟡 | Builder → Forge |
| 12 | **OhhO Market** | Download/sell skills | Intelligence | 🟡 | Spark → Forge |
| 13 | **OhhO Pilot** | MR teleop (OpenXR) | Operations | 🟡 | Builder → Forge |
| 14 | **OhhO Fleet** | OTA + observability | Operations | 🟡 | Fleet/Forge only |
| 15 | **OhhO Twin** | Live mirror + replay | Operations | 🟡 | Spark → Forge |
| 16 | **OhhO Care** | Predictive maintenance | Operations | ⚪ | Builder → Forge |
| 17 | **OhhO Comply** | Cert-ready evidence | Trust | ⚪ | Builder → Forge |
| 18 | **OhhO Shield** | Robot security | Trust | ⚪ | Builder → Forge |
| 19 | **OhhO Proof** | Safety scenario testing | Trust | ⚪ | Spark → Forge |

**Honest ratio:** 4 available (21%) · 11 beta (58%) · 4 roadmap (21%). 78% of
the catalog is not yet shippable at quality.

---

## The product lifecycle the marketing tells

Source: `components/Lifecycle.tsx:30` — the homepage "one umbrella" moat section.

```
01 Prototype  → 02 Train → 03 Launch  → 04 Deploy  → 05 Manage → 06 Regulate
   Build,Bench,   Data,      Serve,      Fleet,        Care,Market,  Proof,
   Frame          Train,Twin  Connect,    Bridge,View   Mind,Link     Shield,Comply
                  Pilot       Autonomy

Decisions:
```

This is the most coherent lifecycle narrative in robotics. It is also a
**marketing-level promise of a 19-product stack on a 4–10 person engineering
team** while bootstrapped at $0 ARR with no customers. The narrative is right;
the engineering capacity behind it is not.

> See [08-12-month-plan.md](./08-12-month-plan.md) for the recommended cut.

---

## The intelligence-loop architecture (the smart thing the company says)

Source: `app/news/ohho-mind/page.tsx` (the lunch-time launch article, June 19):

> *"The most important design decision in OhhO Mind is what it doesn't do: it
> never puts a large model in the motor-control loop. The fast reflexive layer —
> navigation, RL and VLA policies running at 10–20 Hz — stays exactly as it is.
> OhhO Mind adds the slow deliberative layer on top, at roughly 1 Hz, where the
> thinking happens. A fast body and a slow mind, wired together carefully."*

The two-speed architecture (VLA reflex at 10–20 Hz, agent loop at ~1 Hz) is the
correct architecture and a **real differentiator** vs single-loop agentic
robotics startups who bolt a fast LLM onto a servo. It is also unproven at
fleet scale.

> See [04-intelligence-vs-hardware-makers.md](./04-intelligence-vs-hardware-makers.md)
> for the structural analysis.

---

## Pricing (the actual revenue model today)

Source: `components/Pricing.tsx:11` — four tiers. The Free tier (lines 12–30)
gives away **all 19 consoles** locally.

| Tier | Price | What you pay for |
|---|---|---|
| Free | $0 | All 19 consoles, local-only, community support |
| Builder | $49 / mo | GPU training (20 hr), Serve 500 calls/day, Mind, Link, Twin, Data 1K |
| **Fleet** | **$199 / mo** | **+ 200 GPU hr, 10K calls/day, Fleet OTA (100 robots), Comply+Shield, Slack** |
| Forge | Custom | Unlimited + on-prem + custom + SSO + white-label |

### The brutal ARPA math nobody on the team has done

- Builder ARPA: $49 × 12 = **$588 ARR per Builder customer**
- Fleet ARPA: $199 × 12 = **$2,388 ARR per Fleet customer**
- For $1M ARR at retail mix (assume 60% Builder / 40% Fleet): **~1,500 paying
  customers.**
- For $1M ARR from Forge-only (no retail): **~1–40 customers at $25K–$1M ACV.**

The self-serve ladder does not get to profitability withoutForge. Forge is the
business; the ladder is the wedge.

> See [06-profitability-positioning.md](./06-profitability-positioning.md) for
> the full revenue model and 18-month math.

---

## Standards coverage (a real differentiator, when shipped)

Source: `website/AGENTS.md` §3. The full standards list:

- **15 communication protocols** (DDS, MAVLink, CANopen, Modbus, EtherCAT, OPC
  UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, ROS-Industrial, IEC 61131-3, ROS 2
  topics, ROSBridge, Open-RMF)
- **11 safety standards** (EU Machinery Reg, ISO 10218-1/2, ISO 15066, ISO
  13849, ISO 12100, ISO 3691-4, ISO 13482, ANSI R15.06, ANSI B56.5, IEC 61508,
  UL/IEC 60204)
- **6 data/description formats** (LeRobot, MCAP, ONNX, URDF, SDF, USD)
- **3 simulation engines** (Gazebo, Isaac Sim, MuJoCo)

This is genuinely rare breadth. **The standards compliance is also a
checklist, not a fortress**: PROFINET, EtherNet/IP, EtherCAT for ROS 2 are *not*
off-the-shelf. Most of those Bridge adapters in `website/lib/bridge/adapters.ts`
need implementation work, and the ones shipped may be reference wrappers rather
than production cert-grade stacks.

> See [03-moats.md](./03-moats.md) for the per-claim audit.

---

## What is actually backed by code today

Cross-referencing `website/lib/products.tsx` with the root repo per
`docs/paper/PRODUCT_DIRECTIONS.md:19`:

| Module | Source code backing | Reality |
|---|---|---|
| Connect | `website/lib/connect/mcp-tools.ts`, Android app ROSBridge | Shipping. |
| Data | `data_engine/`, `lerobot_engine/record.py`, LeRobot HF format | Shipping. |
| Train | `lerobot_engine/train.py`, `rl_engine/`, W&B integration | Shipping. |
| Serve | `vla_serve/`, `vla_engine/` FastAPI + REST | Shipping. |
| Build / Bench / Frame | Scaffolded in `robot_ws/src/`, URDF/`omnibot_description/` | Beta — partial. |
| Bridge | Adapters in `website/lib/bridge/adapters.ts` (12 listed); source code reality unclear for industrial Ethernet (PROFINET/EtherCAT) | Beta — verify depth. |
| View | `packages/ros2_bev_stitcher/` | Beta — appears functional. |
| Autonomy | `robot_ws/src/omnibot_navigation/` (Nav2, SLAM, EKF), `omnibot_hybrid/mission_planner.py` | Beta — full ROS 2 stack exists. |
| Mind | `robot_ws/src/omnibot_orchestration/langchain_agent_node.py` | Beta — node exists; full agent loop unproven. |
| Market | `sdk/ohho/market.py`, `registry.py` (per `PRODUCT_DIRECTIONS.md`) | Beta — directory exists. |
| Pilot | `vr_app/` Unity Quest 3 app | Beta — app exists. |
| Fleet | `infra/observability/`, `omnibot_metrics`, `omnibot_ota` | Beta — infra exists. |
| Twin | `digital_twin/` worlds + scenarios | Beta — scaffolding. |
| Care, Comply, Shield, Proof | `learning_engine/verification/verifier.py` (near Shield/Proof) | Roadmap — only verifier is real. |

> This cross-reference is the most important table in the folder. Most of "beta"
> is closer to "demo or scaffold" than "feature-complete at quality."

---

## What OhhO is NOT selling today

- Not selling BOM (Build is beta, sourcing links unverified)
- Not selling a certified_robot workcell (Comply is roadmap)
- Not selling a security audit (Shield is roadmap)
- Not selling ML-driven failure prediction (Twin prediction roadmap)
- Not selling paid skills (Market catalog empty)
- Not selling IAM/SSO (Forge roadmap)
- Not selling Mind-as-a-service to an external customer (no customers)

That is the honest gap.