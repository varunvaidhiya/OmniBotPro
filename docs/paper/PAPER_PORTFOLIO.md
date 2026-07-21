# OmniBot Paper Portfolio — Exploring All Directions

Companion to `PAPER_PLAN.md` (the deep plan for Direction 1, now merged).
This document explores **every paper direction discussed**, grounds each in
code that already exists in this repo, and sequences them into a multi-paper
research agenda for OhhO — so one platform yields a pipeline of publications
instead of a single paper.

**How to read the tables:** *Effort* is time-to-submittable given the code
already exists; *Novelty risk* is how likely reviewers are to say "not novel."
Lower novelty risk = safer for a systems/engineering venue.

---

## 0. The portfolio at a glance

| # | Direction | Core repo evidence | Best venue | Effort | Novelty risk | Order |
|---|-----------|--------------------|------------|--------|--------------|-------|
| D1 | **Affordable distributed embodied-AI platform** (chosen) | `benchmarks/`, `deploy.py`, `vla_serve`, `omnibot_hybrid` | arXiv → RA-L | Med | Low | **1st** |
| D2 | **VLA deployment on consumer robots** (systems) | `vla_engine/trt/`, `vla_serve`, `hardware/profiles.py` | RA-L / workshop | Med | Low-Med | 2nd |
| D3 | **Hybrid classical + foundation** manipulation | muxes, `mission_planner`, `verification/verifier.py` | IROS / RA-L | Med-High | Med | 3rd |
| D4 | **Reproducible benchmarking methodology** for embodied stacks | `benchmarks/` + SLO table + `monitors.py` | workshop / R:SS bench track | Low | Med | fast win |
| D5 | **Inference-time safety verifier** (Best-of-N + hard checks) | `learning_engine/verification/` | workshop → RA-L | Med | Med | 4th |
| D6 | **Low-cost sim→real dataset generation** | `data_engine/isaac_sim/`, LeRobot schema | workshop / dataset track | Med-High | Med | later |
| D7 | **OhhO OS: robot-agnostic open-source engine** (software paper) | `sdk/ohho/` (15+ test modules) | **JOSS** | Low-Med | Low | fast win |
| D8 | **Continual / post-training on a physical robot** | `learning_engine/` PostTrainingLoop | CoRL workshop | High | Med-High | stretch |
| D9 | **Deliberative agent harness for a physical robot** | `agent_engine/` perceive→plan→act→reflect | workshop | High | High | stretch |
| — | *"A Practical Robotics Stack for Physical AI"* (Option 4) | whole repo | — | — | — | **fold into D1** |
| — | *"Lessons Learned deploying OpenVLA"* (Option 5) | `vla_engine/`, `benchmarks/` | — | — | — | **fold into D2** |

**Reading of the original 5 options:** Options 4 and 5 are *framings*, not
distinct contributions — 4 overlaps D1 almost entirely, and 5 is the
experience-report version of D2. Keep them as section framings or blog posts,
not separate papers, or reviewers will see salami-slicing.

**Anti-cannibalization rule:** D1–D9 must each own a *different primary
contribution* so they don't compete. The column that keeps them distinct:
D1 = *the platform + cost*, D2 = *the inference engineering*, D3 = *the
arbitration/autonomy*, D4 = *the methodology*, D5 = *the safety mechanism*,
D6 = *the data*, D7 = *the software abstraction*. Cite each other; don't repeat.

---

## D1 — Affordable Distributed Embodied-AI Platform  *(chosen, planned)*

Fully specified in `PAPER_PLAN.md`. Summary here for completeness.

- **Primary contribution:** a ~$500 end-to-end stack + distributed edge/GPU
  inference + reproducible benchmarks.
- **Evidence:** `README.md` cost table, `deploy.py` (single/multi),
  `packages/vla_serve/`, `omnibot_hybrid/`, `benchmarks/`.
- **Headline experiment:** on-Pi vs distributed inference latency/throughput.
- **Venue:** arXiv immediately → RA-L. **Status:** plan merged; gating task is
  running `benchmarks/run_benchmarks.sh` to fill `benchmarks/results/`.

---

## D2 — Deploying VLA Models on Consumer Robotics Platforms  *(systems / engineering)*

**The pitch:** everyone publishes VLA *models*; almost nobody documents the
*engineering* to run one at interactive rates on cheap hardware. This is the
"how we actually shipped it" paper.

**Primary contribution:** an end-to-end VLA serving pipeline + optimization
study (quantization, TensorRT vision-encoder export, batching, edge/GPU split)
with latency/throughput/accuracy trade-offs on consumer hardware.

**Repo evidence (already real):**
- `vla_engine/trt/encoder_export.py` + `vla_engine/trt/build_engine.py` — a
  TensorRT path for the vision encoder (this is the concrete optimization the
  paper measures).
- `packages/vla_serve/` — FastAPI server (`/predict`, `/load_model`), env-var
  configured, `VLA_LOAD_4BIT` quantization toggle.
- `omnibot_lerobot/policy_node.py` `use_trt` param — the ROS-side hook.
- `learning_engine/hardware/` — `onnx_providers()` EP selection
  (TensorRT/CUDA/CoreML/CPU), `resolve_device()`.
- `benchmarks/inference/bench_vla_inference.py` + `bench_policy_inference.py`.

**Experiments:** OpenVLA-7B and SmolVLA under {fp16, 4-bit, TRT-encoder} ×
{Pi, workstation}; report latency p50/p95, throughput, VRAM, and task-success
delta from quantization. The ablation *is* the paper.

**Venue:** RA-L or an ICRA/IROS deployment/edge-robotics workshop.
**Effort:** medium — code exists; needs runs + one clean ablation table.
**Absorbs Option 5** ("Lessons learned deploying OpenVLA") as the discussion section.

---

## D3 — Hybrid Classical Robotics + Foundation Models

**The pitch:** deterministic Nav2 handles navigation; a learned policy handles
manipulation; a safety-bounded mux arbitrates. The contribution is the
*arbitration architecture + safety envelope*, not either component.

**Repo evidence:**
- `omnibot_hybrid/cmd_vel_mux.py` (4 sources → one `/cmd_vel/out`) and
  `omnibot_rl/arm_cmd_mux.py` (policy vs rl_arm) — the arbitration layer.
- `omnibot_hybrid/mission_planner.py` — the state machine
  (`navigate→vla`, `rl_nav→rl_arm`).
- `learning_engine/verification/verifier.py` — `SafetyCheck` + `ReachabilityCheck`
  as the envelope that gates learned actions with real hardware limits.

**Experiments:** task success + safety interventions for hybrid vs
VLA-only vs classical-only on "navigate then manipulate" tasks; ablate the
safety envelope (interventions, near-limit violations avoided).

**Venue:** IROS or RA-L. **Effort:** medium-high (needs real task trials).
**Novelty framing:** emphasize the *safety-gated* arbitration; "hybrid" alone
is becoming crowded.

---

## D4 — A Reproducible Benchmarking Methodology for Embodied Stacks  *(fast win)*

**The pitch:** embodied-AI systems papers rarely report latency the way
systems papers do. Propose an **SLO-gated, per-machine, regression-checked**
benchmarking methodology and release it as reusable tooling.

**Repo evidence (this is nearly a paper already):**
- `benchmarks/conftest.py` — the `SLO_TABLE` (target/max per metric) with
  per-machine overrides (Pi5/GPU).
- `benchmarks/compare_baseline.py` — regression gate (exits non-zero on
  p95 breach) — the reproducibility contribution.
- `benchmarks/{serial,kinematics,vision,inference,ros,system}/` — the suite.
- `learning_engine/benchmarks/monitors.py` — `ResourceMonitor`
  (CPU/GPU/mem/power/temp), `reporters.py` (W&B/Prometheus/JSON).

**Experiments:** run the suite across ≥2 machines; show SLO pass/fail,
regression detection catching a seeded regression, and telemetry-correlated
latency. Deliverable = methodology + open tool.

**Venue:** an ICRA/IROS/CoRL *workshop* or a benchmarks track. **Effort:**
low — it's mostly writing + running what exists. **Great morale/first-arXiv win.**
**Note:** D1 uses these numbers; D4 is about the *method*, so frame around
methodology + tool release to stay distinct from D1.

---

## D5 — Inference-Time Safety Verification for Learned Robot Policies

**The pitch:** a Best-of-N plan selector with **hard** safety/reachability
rejection using real hardware limits, sitting between a policy and the motors.

**Repo evidence:**
- `learning_engine/verification/verifier.py` — `InferenceVerifier` (Best-of-N),
  `PlanCheck` registry, `SafetyCheck`, `ReachabilityCheck`.
- `agent_engine/integrations/learning_engine.py` — `WorldStateVerifier` wraps
  these as the agent's `VerifierPort` (shows it's wired into a live loop).
- Real limits from `omnibot_arm` joint_min/max, driver 0.2 m/s clamp.

**Experiments:** rate of unsafe actions rejected, task-success impact of
gating, latency overhead of Best-of-N at inference. Compare N∈{1,4,16}.

**Venue:** a safe-robot-learning workshop → RA-L. **Effort:** medium.
**Novelty risk:** medium — position against shielding/CBF literature clearly.

---

## D6 — Low-Cost Sim→Real Dataset Generation for Mobile Manipulation

**The pitch:** a pipeline that generates LeRobot-format episodes in Isaac Sim
with domain randomization, aligned to a real robot's schema, for cheap.

**Repo evidence:**
- `data_engine/isaac_sim/collect_episodes.py` + `randomization_config.yaml`.
- `data_engine/schema/constants.py` — canonical state/action specs
  (`MOBILE_MANIP_STATE_SPEC` 9-D) that keep sim and real aligned.
- `packages/robot_episode_dataset/`, `data_engine/ingestion/`,
  `TopicSynchronizer` (0.05 s alignment).

**Experiments:** train SmolVLA on N sim episodes, measure real-robot
success vs #demos and vs sim/real mix (this also feeds D1's C6 curve).

**Venue:** dataset/sim workshop. **Effort:** medium-high (needs training runs +
real eval). **Later** — depends on D1/D2 infrastructure being measured first.

---

## D7 — OhhO OS: A Robot-Agnostic Open-Source Embodied-AI Engine  *(software paper, fast win)*

**The pitch:** a JOSS-style software paper on `sdk/ohho` — the robot-agnostic
engine (abstraction + runtimes + adapters + agent brain + data/train/serve/CLI).
JOSS reviews *software*, not novelty, so acceptance hinges on tests/docs/design
— which already exist.

**Repo evidence:**
- `sdk/ohho/` — `robot.py`, `runtime/`, `adapters/` (sim/Yahboom/Feetech/
  Unitree/ROS2), `brains.py`, `agent.py`, `serve/`, `train/`, `cli.py`,
  `registry.py`, Apache-2.0.
- `sdk/tests/` — 15+ test modules (`test_robot`, `test_adapters`, `test_ros2`,
  `test_agent`, HIL tests) — JOSS wants exactly this.
- `sdk/README.md`, `sdk/CONTRIBUTING.md`, `sdk/AGENTS.md`.

**"Experiments":** JOSS needs a statement of need + working examples + tests
passing, not benchmarks. Mostly packaging + a short paper.md.
**Venue:** **JOSS**. **Effort:** low-medium. **Strong for open-source credibility
and contributor growth** — complements D1 perfectly (D1 = the robot, D7 = the engine).

---

## D8 / D9 — Stretch directions (real code, but paper-scale work)

- **D8 Continual / post-training on a physical robot** — `learning_engine/`
  `PostTrainingLoop` + `ContinualLearningScheduler` (triggers: new episodes /
  tasks / staleness), BC/AWR trainers, PER replay. A real continual-learning
  paper needs sustained real-robot runs → high effort, higher novelty bar.
  CoRL workshop if pursued.
- **D9 Deliberative agent harness** — `agent_engine/` perceive→plan→act→
  monitor→reflect→remember state machine with a cloud/local reasoning router.
  Novel but hard to evaluate rigorously on one robot; workshop-only unless a
  strong task suite is built. **Highest novelty risk.**

Keep D8/D9 as *future work* sections in D1/D3 until the platform papers land.

---

## Recommended sequencing (maps onto the 1-month + beyond)

```
Month 1     D1 (arXiv + RA-L)         ← already planned; run benchmarks
 ‖ parallel D4 (methodology, workshop) ← cheap, reuses D1's runs
 ‖ parallel D7 (JOSS software paper)   ← cheap, independent of hardware runs
Month 2-3   D2 (VLA deployment, RA-L)  ← inference ablations
Month 3-4   D3 (hybrid+safety, IROS)   ← task trials
Later       D5, D6 → D8/D9             ← as data/eval matures
```

Three submittable artifacts in month one (D1 paper, D4 workshop, D7 JOSS)
because D4 and D7 barely need new experiments — they package what exists.

---

## What to decide before starting (open questions for you)

1. **Breadth vs depth this month:** go deep on D1 only, or fire D1+D4+D7 in
   parallel for three artifacts? (D4/D7 are cheap; the risk is split focus.)
2. **JOSS now or later:** D7 needs the SDK's docs/tests presentable — is
   `sdk/ohho` ready to show reviewers, or does it need a cleanup pass first?
3. **Which second paper:** D2 (safer, systems) vs D3 (higher-impact, harder).
4. **Author/venue constraints:** any co-authors, or venue ties from your
   Global-Talent-Visa / hiring goals that should steer venue choice?

I can turn any of D2–D7 into a full `PAPER_PLAN`-style deep plan (contribution
table, experiments mapped to files, figures, schedule) on request — say which.

---

*Every direction above is anchored to code present in this repo as of the
merge of `PAPER_PLAN.md`. Nothing here assumes work that doesn't exist; the
recurring gating task across D1/D2/D4/D6 is generating measurements from the
benchmark and inference suites that are already built.*
