# OhhO Product & Research-for-Growth Directions

Strategic companion to `PAPER_PLAN.md` and `PAPER_PORTFOLIO.md`. Those two
documents plan papers as *publications*; this one plans **product directions**
and the **research that markets them**. Everything here is grounded in what
already exists in this repo and in the public positioning on ohho-robotics.com.

> **Positioning as of this writing** (ohho-robotics.com): *"The open data &
> deployment layer for robots"* — a robot-agnostic, open-core platform with
> **19 modules** across three tiers, monetized as SaaS
> (Free / Builder $49-mo / Fleet $199-mo).
>
> | Tier | Modules |
> |---|---|
> | **Available** | Connect, Data, Train, Serve |
> | **Beta** | Build, Frame, Bench, Bridge, View, Autonomy, Mind, Market, Pilot, Fleet, Twin |
> | **Roadmap** | Care, Comply, Shield, Proof |
>
> **Repo ↔ module map (what's actually backed by code):**
> `sdk/ohho` = OS · `data_engine` = Data · `learning_engine` = Train ·
> `vla_serve`/`vla_engine` = Serve · `benchmarks` = Bench · `agent_engine` = Mind ·
> `infra/observability` = Fleet/View · `vr_app` = Pilot · `digital_twin` = Twin ·
> `learning_engine/verification` = Shield/Proof · `sdk/ohho/market.py` = Market.

---

## Part 0 — The central strategic tension (read first)

**19 modules is the strategy risk, not the strategy.** For a small team that
surface area is unevenly real and impossible to sell as a single thing. Almost
every "new direction" below is really a bet on *which wedge leads* — not a
request to build more. Pick a tip of the spear; let the rest become
*consequences* of winning it, not parallel efforts.

**My read of your center of gravity:** the **data → train → deploy loop on
cheap, robot-agnostic hardware.** The two moves with compounding, defensible
upside are **owning data collection** (start the flywheel) and **Market**
(turn the flywheel into a moat), with **education** as the pragmatic
revenue+funnel bet for your current stage.

---

## Part 1 — Product directions

Each direction: *what to build · why it fits · what you already have · the risk.*
Directions are grouped; **A and B are mutually competing bets — you likely
cannot do both this year.**

### A. Wedge directions (pick the tip of the spear)

#### P1 — Data-collection-as-the-wedge (Pilot + Data)  ⭐ recommended
- **What:** productize the full funnel "plug in any arm → collect teleop
  demos → get a fine-tuned policy." Make first-demo-to-first-policy a
  10-minute experience.
- **Why:** Mobile ALOHA proved the real market pain is *getting
  demonstrations*, not models. This is the entry point that starts the **data
  flywheel** every other module feeds on.
- **Have:** `vr_app` + `launch_vr_teleop.sh` (VR teleop), `data_engine` +
  `lerobot_engine` (LeRobot-format capture), `omnibot_lerobot/teleop_recorder_node.py`.
- **Risk:** teleop UX polish is deceptively hard; hardware variety fragments support.

#### P2 — OhhO Market as the moat (Market)  ⭐ recommended
- **What:** a "Hugging Face Hub for robot skills, policies & datasets" —
  upload, discover, one-click deploy to your robot.
- **Why:** the *only* module on the list with real **network effects**. Every
  upload makes the platform more valuable; this is what makes OhhO a *platform*
  rather than a toolchain.
- **Have:** `sdk/ohho/market.py`, `sdk/ohho/registry.py`, LeRobot dataset
  format, `serve/` for deployment.
- **Risk:** cold-start (no content = no users); needs seeding (see paper G2).

#### P3 — Fleet/observability as the enterprise wedge (Fleet + View)
- **What:** "Datadog for robot fleets" — metrics, logs, traces, alerts,
  OTA updates across a fleet.
- **Why:** a *boring, sellable* B2B product with an existing budget line,
  unlike "AI policies" which buyers can't yet evaluate. Shortest sales cycle.
- **Have:** `infra/observability` (Prometheus/Grafana/Loki/Tempo), `omnibot_metrics`,
  `omnibot_ota`, `learning_engine/benchmarks` telemetry.
- **Risk:** lower ceiling; competes with generic observability if not
  robot-specific enough.

### B. The horizontal-vs-vertical bet (biggest decision)

#### P4 — Go vertical on ONE application
- **What:** pick a single domain and own the full stack: **lab automation**
  (sample/pipette handling — structured envs, high willingness to pay),
  **retail/warehouse** restocking, or **education** (P5).
- **Why:** nobody's job title is "I need a robot platform." Verticals have
  budgets and clear success criteria. Land the niche, then generalize.
- **Have:** the whole mobile-manip stack; `agent_engine` for task logic.
- **Risk:** directly contradicts P6 — this is the point; choose one.

#### P5 — Education / kit business
- **What:** the ~$500 BOM robot + `digital_twin` + a curriculum, sold as a
  teaching kit (universities, bootcamps, hackerspaces) with hosted training seats.
- **Why:** a market that pays for *teachability*, not SOTA — a product **today**.
  Doubles as top-of-funnel for the platform and fits visa/credibility goals.
- **Have:** `README` $500 BOM, `digital_twin`, `docs/`, `sdk` quickstarts.
- **Risk:** hardware logistics/support; margins on kits are thin.

#### P6 — Double down on robot-agnostic breadth (OS)
- **What:** lean all the way into "OhhO runs on *your* robot" — more adapters,
  more robots, one API.
- **Why:** the bet that being the neutral layer across many robots beats owning
  one. The core platform thesis.
- **Have:** adapters for sim/Yahboom/Feetech/Unitree/ROS2 in `sdk/ohho/adapters`.
- **Risk:** slowest to revenue; breadth without depth satisfies no one.

### C. Technical bets that could become the differentiator

#### P7 — On-device / edge VLA (Serve + hardware)
- **What:** run a VLA policy on a $500 robot with a Pi + NPU, no workstation.
- **Why:** genuine technical differentiator most competitors punt on;
  "no $2k GPU needed" is a democratization headline.
- **Have:** `learning_engine/hardware` profiles (DeepX/Jetson/Coral),
  `vla_engine/trt` (TensorRT encoder path), `onnx_providers()` EP selection.
- **Risk:** on-device accuracy/latency may not meet the bar yet — must measure.

#### P8 — Safety/compliance as a standards play (Shield + Comply + Proof)
- **What:** an inference-time safety envelope + a certification/interchange
  standard for "safe robot action."
- **Why:** as learned policies enter regulated/physical spaces, this becomes a
  *requirement*. Whoever defines the standard owns a chokepoint.
- **Have:** `learning_engine/verification/verifier.py` (Best-of-N + hard
  safety/reachability checks with real hardware limits).
- **Risk:** long horizon; standards adoption is a chicken-and-egg slog.

#### P9 — The Mind / agent layer as the product (Mind + Autonomy)
- **What:** sell long-horizon autonomy — the deliberative brain that turns
  commands into multi-step missions.
- **Why:** your most *forward* asset; where the field is heading.
- **Have:** `agent_engine` perceive→plan→act→reflect loop, cloud/local
  reasoning router, `omnibot_orchestration`.
- **Risk:** highest — early, hard to evaluate, hard to price.

### Product recommendation

> **Lead with P1 (own data collection)** to start the flywheel, **P2 (Market)**
> to turn it into a moat, and run **P5 (education)** as the pragmatic
> revenue+funnel bet. Treat P3/P4/P6–P9 as directions you earn the right to
> pursue *after* one of these works — not as parallel tracks.
>
> **Caveat:** this ranking is from code + public positioning, not usage/traction
> data. If you have signal on which modules users actually touch, that overrides it.

---

## Part 2 — Research that develops the product (papers as growth assets)

Different lens from the publication portfolio: here a paper is a **growth
asset** — a trust signal, a citation magnet, or HN/X fuel that pulls users into
a specific module. Each doubles as that module's whitepaper (you already have
`docs/video-prompts/*.md` stubs for `bench`, `serve`, `shield`, etc.).

**Meta-principle:** the highest-ROI paper isn't the one people *read* — it's the
one people *use and cite*. A benchmark or dataset others build on compounds
(backlinks, citations, network effects); a "we built X" paper is a single spike.

### Tier 1 — Flywheel papers (compounding: citations + network effects + SEO)

#### G1 — A public benchmark + leaderboard for low-cost mobile manipulation ⭐
- **Serves:** Bench + Market. Every external submission = a backlink, a
  citation, a reason to be on-platform.
- **Build:** standardize `benchmarks/` into an open benchmark others submit to
  ("the MLPerf/COCO of affordable embodied AI"); host a live leaderboard on the site.
- **Grounding:** `benchmarks/conftest.py` SLO table, `compare_baseline.py`,
  `learning_engine/benchmarks`.
- **Why #1:** owning the benchmark owns the conversation — the single best
  moat-builder here.

#### G2 — A released dataset (Open-X-style, for cheap mobile manipulation) ⭐
- **Serves:** Data + Market (seeds the cold-start of P2).
- **Build:** publish a real LeRobot-format dataset collected on the $500 robot
  + a short data paper + a HuggingFace dataset card linking to Market.
- **Grounding:** `data_engine`, `lerobot_engine`, `packages/robot_episode_dataset`.
- **Why:** datasets get cited far more than systems papers; durable SEO.

#### G3 — Data-efficiency scaling study: "how few demos to a competent policy?"
- **Serves:** the Data → Train → Serve loop (the flywheel's sales pitch).
- **Build:** success-rate vs #demos vs sim/real mix on $500 hardware; prove the
  README's "~50 demos" claim rigorously.
- **Grounding:** `lerobot_engine`, `data_engine`, `learning_engine`.
- **Why:** the one chart every landing page and pitch deck needs — tells users
  their data collection will pay off.

### Tier 2 — Trust papers (unlock enterprise + reliability positioning)

#### G4 — Inference-time safety verification for learned policies
- **Serves:** Shield / Comply / Proof — the modules enterprises need to say yes.
- **Build:** Best-of-N plan selection with hard safety/reachability gates on
  real hardware limits; report unsafe-action rejection rate, task-success
  impact, latency overhead (N ∈ {1,4,16}).
- **Grounding:** `learning_engine/verification/verifier.py`,
  `agent_engine/integrations/learning_engine.py` (`WorldStateVerifier`).
- **Why:** "certified-safe action selection" is a procurement unlock.

#### G5 — Reproducible-benchmarking methodology ("how we measure")
- **Serves:** reliability-as-marketing; the rigor signal that makes skeptics
  trust an open-source tool.
- **Build:** the SLO-gated, per-machine, regression-checked *method* itself
  (distinct from G1's leaderboard).
- **Grounding:** `benchmarks/conftest.py` SLO table + overrides,
  `benchmarks/compare_baseline.py` regression gate.
- **Why:** "we hold ourselves to published SLOs" differentiates from hobby projects.

### Tier 3 — Attention papers (HN front page / X threads / top-of-funnel)

#### G6 — "Run a VLA on a Raspberry Pi + NPU" (edge deployment)
- **Serves:** Serve (drives signups); supports product direction P7.
- **Build:** OpenVLA/SmolVLA under {fp16, 4-bit, TRT-encoder} × {Pi+NPU,
  workstation}; latency/VRAM/accuracy trade-offs.
- **Grounding:** `learning_engine/hardware` (DeepX/Jetson/Coral), `vla_engine/trt`.
- **Why:** "no $2k GPU needed" is developer catnip that spreads.

#### G7 — Cross-embodiment: "one stack, many robots"
- **Serves:** the OhhO OS "works on your robot" claim (P6), made credible.
- **Build:** run the same pipeline across ≥3 robots; publish a compatibility
  matrix + video montage.
- **Grounding:** `sdk/ohho/adapters` (sim/Yahboom/Feetech/Unitree/ROS2),
  `sdk/tests/test_adapters.py`, `test_ros2.py`.
- **Why:** expands addressable users beyond OmniBot owners.

#### G8 — OhhO OS software paper (JOSS)
- **Serves:** academic adoption funnel — a citeable handle for the platform.
- **Build:** the engine reviewed as software; DOI + "cite us" badge on the repo.
- **Grounding:** `sdk/ohho` (robot.py, runtimes, adapters, agent, serve, train,
  CLI), `sdk/tests` (15+ modules), Apache-2.0.
- **Why:** low effort (tests/docs exist), high compounding value — every paper
  that uses OhhO cites it.

### Growth sequencing (optimize for growth, not paper count)

| Now (weeks) | Next (1–2 mo) | Later |
|---|---|---|
| **G8 JOSS** (cheap, citeable) + **G6 Edge VLA** (attention spike) | **G1 Benchmark+leaderboard** + **G3 data-efficiency chart** | **G2 Dataset**, **G4 Safety**, **G7 Cross-embodiment** |

> **The strategic core:** **G1 (benchmark) and G2 (dataset) are the only two
> that create compounding, defensible growth** — they make *other people's work*
> point back at OhhO. Everything else is trust (G4/G5) or attention (G6/G7/G8),
> which spike and fade. If you do only two, do those.

---

## Part 3 — How product directions and papers reinforce each other

| Product wedge | Paper that markets it | Compounding effect |
|---|---|---|
| P1 Data collection | G3 data-efficiency, G2 dataset | proves ROI → more collectors → more data |
| P2 Market | G1 leaderboard, G2 dataset | submissions/citations seed content → network effects |
| P3 Fleet | G5 reproducible methodology | rigor signal → enterprise trust |
| P5 Education | G8 JOSS, G2 dataset | citeable + teachable → academic funnel |
| P7 Edge VLA | G6 "VLA on a Pi" | HN attention → Serve signups |
| P8 Safety | G4 safety verifier | procurement unlock → enterprise |
| P6 Cross-embodiment | G7 one-stack-many-robots | breadth proof → wider TAM |

**The loop:** win a wedge → publish the paper that proves it → the paper draws
users and citations → those feed the data/Market flywheel → which funds the next
wedge.

---

## Open decisions for you

1. **Horizontal vs vertical** (P6 vs P4/P5) — the biggest fork; everything else
   follows from it.
2. **Which wedge leads** — P1 (data), P2 (Market), or P3 (Fleet)?
3. **Two growth papers first** — confirm G1 + G2, or substitute based on what's
   fastest to ship on your hardware.
4. **Traction signal** — is there usage data that should override the
   code-and-positioning-based ranking here?

*Next step options: I can deep-plan any single wedge (build + GTM) or any single
paper (contribution table, experiments mapped to files, figures, schedule) in
the style of `PAPER_PLAN.md`. The highest-leverage single artifact is a deep
plan for G1 (the benchmark + leaderboard).*
