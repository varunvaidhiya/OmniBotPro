# OmniBot Paper — Detailed Publication Plan

**Working title:**
*OmniBot: An Affordable, Distributed Embodied-AI Platform for Mobile
Manipulation with Vision-Language-Action Models*

**Paper class:** Systems / robotics engineering (NOT a new-algorithm paper).
The contribution is the *architecture, integration, and reproducible
benchmarks* of a complete low-cost stack — not a new model.

**Timeline:** 1 month to arXiv + a workshop/RA-L submission.

---

## 0. The one thing that decides whether this paper is real

This repo already contains a full benchmark suite
(`benchmarks/`) with an SLO table, per-machine runners (Pi 5 / GPU / CI),
and a regression harness (`benchmarks/compare_baseline.py`).
**But `benchmarks/results/` is empty — no numbers have been collected yet.**

A systems paper *is* its measurements. So the critical path is not writing —
it's **running the existing benchmarks on your real hardware and logging the
JSON results**. Everything else (prose, figures, related work) can be done in
parallel, but without numbers there is no paper.

> **Priority-zero action:** run `benchmarks/run_benchmarks.sh` on the Pi 5 and
> on the GPU workstation, commit the `benchmarks/results/*.json`, and set a
> baseline. Do this in Week 1, day 1–3.

---

## 1. The precise contribution claims (what we assert & must defend)

Each claim is mapped to code that already exists so we don't over-claim.

| # | Claim | Evidence in repo | Experiment needed |
|---|-------|------------------|-------------------|
| C1 | A complete mobile-manipulation stack at **~$500** (vs ~$25–32k platforms) | `README.md` (cost table), `wiki/Bill-of-Materials` | Publish a full BOM table with suppliers/prices |
| C2 | **Distributed edge/GPU inference**: real-time control + perception on Pi 5, VLA inference on a separate GPU workstation | `deploy.py` (single/multi), `packages/vla_serve/`, `learning_engine/hardware/profiles.py` (`pi_workstation`) | Latency of local-Pi vs distributed inference (the headline comparison) |
| C3 | **Hybrid autonomy**: classical Nav2 navigation + VLA/learned manipulation arbitrated by a safety-bounded mux | `omnibot_hybrid/cmd_vel_mux.py`, `arm_cmd_mux.py`, `mission_planner.py` | Task success with/without the hybrid split |
| C4 | **Reproducible, SLO-gated performance methodology** for embodied stacks | `benchmarks/` suite + `benchmarks/conftest.py` SLO table | The full benchmark table across Pi5/GPU |
| C5 | **Hardware-portable deployment** (Pi, Jetson, DeepX NPU, workstation, Apple Silicon) behind one abstraction | `learning_engine/hardware/profiles.py`, `hardware/` device/EP resolution | Optional: same policy, 2–3 targets, latency/power |
| C6 | Learns pick-and-place from **~50 demonstrations** | `README.md`, `lerobot_engine/`, `data_engine/` | Task success vs #demos curve (nice-to-have) |

**Do NOT claim:** a new VLA architecture, SOTA manipulation success, or a new
learning algorithm. Those invite rejection by comparison to DeepMind-scale work.
Frame novelty as *integration + affordability + reproducible benchmarking*.

---

## 2. Literature review → the gap (already scoped)

Six themes (from prior discussion), each anchored to a differentiator:

1. **Low-cost mobile manipulators** — Mobile ALOHA, AhaRobot, SMARTmBOT.
   *Gap we fill:* they optimize teleop/data-collection or are nav-only; none
   document a distributed edge/GPU deployment architecture with benchmarks.
2. **Foundation models for robotics** — RT-1/RT-2, OpenVLA, Octo, π0.
   *Gap:* they provide the model, not a deployable full-robot system.
3. **Embodied datasets** — Open X-Embodiment, DROID, BridgeData V2.
   *Gap:* datasets, not deployment engineering.
4. **ROS 2 infra** — Nav2, ros2_control, MoveIt2, TF.
   *Gap:* no VLA orchestration / distributed inference story.
5. **Edge-AI deployment** — TensorRT, ONNX Runtime, Jetson, quantization.
   *Gap:* optimize single models, not a whole embodied agent split across machines.
6. **Hybrid classical + foundation** — emerging; our arbitrated mux + safety
   verifier is a concrete instance.

**Gap sentence (paste into Intro):**
> While affordable manipulators (Mobile ALOHA, AhaRobot), open VLA models
> (OpenVLA), and large datasets (Open X-Embodiment) each exist in isolation,
> there is little work describing a *reproducible, end-to-end system* that
> unifies low-cost hardware, ROS 2, distributed edge/GPU inference, classical
> navigation, VLA manipulation, safety arbitration, and a benchmarking
> methodology into one open platform for researchers and startups.

**Action:** build a `related_work.bib` of 40–70 refs. Use Google Scholar +
Semantic Scholar; grab the 6-theme seed papers first, then snowball citations.

---

## 3. Experiments — exactly what to run (mapped to existing code)

All of these already have harness code. The work is *running and logging*, not
building.

### 3.1 Component micro-benchmarks (already coded — `benchmarks/`)
Run `./benchmarks/run_benchmarks.sh pi5` and `... gpu`. Produces p50/p95 for:

- **Serial protocol** encode/decode — `benchmarks/serial/bench_yahboom_protocol.py`
  (SLOs: tx ≤5 ms, rx parse ≤2 ms)
- **Mecanum kinematics** IK/FK/odometry — `benchmarks/kinematics/`
- **BEV stitch** per frame — `benchmarks/vision/bench_bev_stitcher.py`
  (SLO 33 ms target / 50 ms max; 80 ms on Pi)
- **SmolVLA preprocess** — `benchmarks/vision/bench_preprocess.py`
- **SmolVLA / OpenVLA inference** — `benchmarks/inference/` (GPU only)
- **Full control loop** — `benchmarks/ros/bench_control_loop.py` (SLO 50/100 ms)
- **ROS topic latency** (stamp→arrival) — `benchmarks/ros/bench_topic_latency.py`
- **End-to-end pipeline** (frame→BEV→preprocess→inference→action) —
  `benchmarks/system/bench_full_pipeline.py` (target <200 ms for ≥5 Hz)

→ **Table 1** in the paper is literally the SLO table (`conftest.py:SLO_TABLE`)
with measured p50/p95 columns filled in per machine.

### 3.2 The headline experiment — distributed vs. on-robot inference (C2)
The paper's money figure. Configure both modes with `deploy.py`:

- **Mode A (on-Pi):** attempt VLA inference on the Pi 5 alone.
- **Mode B (distributed):** control+perception on Pi, VLA on GPU workstation
  via `vla_serve` (FastAPI `/predict`), `deploy.py --mode multi`.

Measure, for each: inference latency, end-to-end action latency, achievable
control rate (Hz), Pi CPU%/RAM, GPU util, network bandwidth (`/predict`
payload size × rate), power draw. `learning_engine/benchmarks/monitors.py`
(`ResourceMonitor`: CPU/GPU/mem/power/temp via psutil/pynvml/tegrastats)
already collects the telemetry — reuse it.

→ **Figure (bar/latency breakdown)** + **Table 2**. Expected story: on-Pi VLA is
infeasible/too slow; distributed hits interactive rates. That *is* the argument
for the architecture.

### 3.3 Hybrid autonomy value (C3)
Task-level: run N trials of "navigate to X then pick Y" via `mission_planner`
(`navigate:kitchen,vla:pick the cup`). Compare:
- Nav2-only navigation success (reach pose within tolerance)
- VLA-only manipulation success (grasp) at the goal
- Full hybrid mission success + safety-clamp interventions logged.

Report success rate, time-to-complete, and #safety interventions
(velocity clamp at 0.2 m/s, e-stop). Keep N modest (e.g. 10–20 trials/task,
2–3 tasks) — enough for means + error bars, honest about scale.

### 3.4 Portability (C5, optional/stretch)
Same ONNX policy on ≥2 targets (workstation CUDA, Pi CPU, and if available
Jetson/DeepX) via `learning_engine/hardware/` EP resolution. One latency/power
row per target. Only include if time permits.

### 3.5 Reproducibility protocol
State exactly: commit hash, `run_benchmarks.sh` invocation, machine labels,
#trials, and that raw JSON is in `benchmarks/results/`. This *is* contribution C4.

---

## 4. Figures (target 8–10)

| Fig | Content | Source |
|-----|---------|--------|
| F1 | Hero photo of the physical robot + labelled hardware | new photo |
| F2 | System architecture (Pi ↔ GPU, node/topic graph) | adapt CLAUDE.md high-level diagram |
| F3 | Distributed inference dataflow (camera→vla_serve→action) | new, from `vla_serve` + topic map |
| F4 | Hybrid autonomy state machine + mux arbitration | `mission_planner.py` / `cmd_vel_mux.py` |
| F5 | Latency breakdown: on-Pi vs distributed (stacked bar) | §3.2 data |
| F6 | End-to-end pipeline latency distribution (violin/CDF) | `bench_full_pipeline` |
| F7 | Resource use (CPU/GPU/power) per mode | `monitors.py` telemetry |
| F8 | Task success + safety interventions bar chart | §3.3 data |
| F9 | Cost comparison table/plot vs Mobile ALOHA etc. | README cost table |
| F10 | (opt) Portability latency across targets | §3.4 |

Draw architecture diagrams in draw.io / TikZ; keep them vector (PDF) for camera-ready.

---

## 5. Paper structure (IEEE 6–8 pages)

1. **Abstract** — problem, $500 distributed stack, key numbers (fill after §3).
2. **Introduction** — cost/accessibility gap; contributions C1–C6 as a bullet list.
3. **Related Work** — the 6 themes + gap sentence (§2).
4. **System Overview** — hardware (BOM), compute topology (`profiles.py`).
5. **Software Architecture** — ROS 2 graph, distributed inference (`vla_serve`,
   `deploy.py`), hybrid mux + safety.
6. **Deployment & Engineering** — networking (DDS peers, `network.env`),
   observability (Prometheus/Grafana in `infra/`), OTA, portability abstraction.
7. **Experimental Methodology** — the SLO/benchmark framework (C4), machines, protocol.
8. **Results** — Tables 1–2, Figs 5–9.
9. **Limitations** — small trial counts, single robot, rear-only depth cam,
   coverage gaps (be honest — reviewers reward it).
10. **Future Work** — larger user study, on-device VLA via DeepX/Jetson, RL.
11. **Conclusion.**

---

## 6. Four-week schedule

**Week 1 — Freeze scope + collect data (critical path)**
- Freeze the feature set at a git tag (`paper-v1`).
- Run `benchmarks/run_benchmarks.sh` on Pi5 + GPU; commit `results/*.json`; set baseline.
- Run §3.2 distributed-vs-Pi experiment; log telemetry.
- Photograph robot; finalize BOM table.
- Start `related_work.bib`.

**Week 2 — Draft core + more experiments**
- Write Intro, Related Work, System Overview, Software Architecture.
- Run §3.3 hybrid task trials; (opt) §3.4 portability.
- Build F2–F4 architecture diagrams.

**Week 3 — Results + writing**
- Generate F5–F9 from logged data; write Methodology + Results.
- Write Deployment/Engineering, Limitations, Future Work, Conclusion.

**Week 4 — Polish + submit**
- Internal review vs reproducibility checklist; proofread; finalize bib.
- Format in IEEE `conference` template (Overleaf).
- Upload to **arXiv** (cs.RO). Submit to target venue (below).

---

## 7. Where to submit (realistic in 1 month)

Ranked by fit + feasibility:

1. **arXiv (cs.RO)** — do this regardless, immediately. Citable, public, dated.
2. **IEEE RA-L** (Robotics & Automation Letters) — rolling submission, no fixed
   deadline, systems/engineering papers welcome, ~few-month review. **Best
   peer-reviewed target for this work.** Can be presented at ICRA/IROS if accepted.
3. **A workshop at ICRA/IROS/CoRL** — check open CFPs for the current cycle;
   easier acceptance, fast, good visibility. Great first venue in parallel to RA-L.
4. **JOSS** — only if you reframe around the open-source *software* platform
   (the SDK / `ohho` package) with a short paper + software review.
5. **Tier-1 full conf (ICRA/IROS/CoRL/RSS)** — do NOT gate the month on these;
   fixed deadlines, months of review. Target a *later* cycle with this same work.

**Recommendation:** arXiv (week 4) + RA-L submission, and simultaneously submit
the short version to an open ICRA/IROS workshop.

---

## 8. Reproducibility checklist (reviewers will look for this)

- [ ] Public repo + exact commit/tag referenced in paper.
- [ ] BOM with part numbers, suppliers, prices, total.
- [ ] `benchmarks/results/*.json` committed; `run_benchmarks.sh` command shown.
- [ ] Machine specs (Pi 5 8 GB; GPU model + VRAM; ROS 2 Jazzy / Ubuntu 24.04).
- [ ] #trials, seeds, and error bars for every reported number.
- [ ] Honest limitations section.
- [ ] Video/figures of real robot doing the tasks.

---

## 9. How this compounds for OhhO / career

One paper → arXiv link → GitHub README badge → portfolio → X thread → recruiter
signal → contributor/investor credibility → foundation for follow-up papers
(benchmarking deep-dive, on-device VLA, safety verifier, dataset generation).
"Documented in a peer-reviewed paper with reproducible benchmarks" is a much
stronger claim than "we built a robot."

---

*This plan is grounded in the current codebase: `benchmarks/` (suite + SLO
table + baseline harness), `deploy.py` (single/multi), `packages/vla_serve/`,
`omnibot_hybrid/` (muxes + mission planner), `learning_engine/hardware/`
(profiles + device resolution) and `learning_engine/benchmarks/monitors.py`
(resource telemetry). The gating task is generating results — the
infrastructure to measure them already exists.*
