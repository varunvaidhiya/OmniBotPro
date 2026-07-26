# OmniBot Publication Plan — Detailed Master Plan

**Audience constraints (from you):**
- Goal: **visa / hiring / portfolio signal** (citable + peer-reviewed + public repo)
- Timeline: **~4 weeks to first public artifact** (aggressive)
- Hardware available: **full stack** (Pi 5 robot + GPU workstation)
- Scope request: choose the best first paper and plan it in detail

**Sources already in repo:** `docs/paper/PAPER_PLAN.md`, `docs/paper/PAPER_PORTFOLIO.md`, `docs/paper/PRODUCT_DIRECTIONS.md`, `README.md`, `CONTRIBUTING.md`, `CLAUDE.md` / `AGENTS.md`, `benchmarks/`, `learning_engine/`, `agent_engine/`, `sdk/`.

---

## 1. Recommendation: which paper first (and why)

### Decision matrix (your constraints)

| Direction | Visa / hiring signal | Feasibility in 4 weeks with full HW | Novelty risk | Public artifact strength |
|-----------|----------------------|--------------------------------------|--------------|--------------------------|
| **D1 Affordable distributed platform** | **Highest** — systems RA-L + arXiv + demo video | **High** — harness exists; need runs | **Low** | Strong (platform + numbers) |
| D4 Benchmark methodology (workshop) | Medium (supporting) | Very high | Med | Medium unless others adopt |
| D7 OhhO OS (JOSS) | High for OSS credibility | High if SDK polish is small | Low (software review) | Strong cite badge |
| D2 VLA deployment engineering | High | Medium (needs ablation runs) | Low–Med | Strong if numbers clean |
| D3 Hybrid classical + foundation | High impact | **Too tight** for 4 weeks (task trials) | Med | Needs larger N |
| D5 Safety verifier / D8–D9 agent | Stretch | Not 4-week | Med–High | Later |

### Primary paper (ship this)

**D1 — Systems / robotics engineering paper**

**Working title:**
*OmniBot: An Affordable, Distributed Embodied-AI Platform for Mobile Manipulation with Vision-Language-Action Models*

**Paper class:** Systems / engineering — **not** a new-algorithm paper.  
**Novelty frame:** integration + affordability + distributed edge/GPU deployment + hybrid autonomy + reproducible SLO-gated benchmarks.

**Why this wins for visa / hiring:**
1. **Peer-review path is real** — IEEE RA-L accepts systems papers; rolling deadlines.
2. **Immediate public citation** — arXiv `cs.RO` in week 4 regardless of RA-L latency.
3. **Defensible claims** map 1:1 to existing code (`deploy.py`, `vla_serve`, muxes, `benchmarks/`).
4. **Demo + repo + paper** is the strongest portfolio triple (recruiters and GT reviewers understand this).
5. Does **not** compete with DeepMind-scale model papers on SOTA success rate.

### Parallel “fast wins” (optional, only if D1 stays on critical path)

| Artifact | Role for portfolio | Effort if D1 is primary |
|----------|--------------------|-------------------------|
| **arXiv preprint of D1** | Must-ship | Core |
| **RA-L submission of D1** | Peer-reviewed target | Core |
| **D7 JOSS** (OhhO OS software paper) | Permanent “cite this software” badge | Do **after** D1 arXiv, or evenings only in week 4 |
| **D4 methodology workshop** | Extra line on CV | Only if D1 numbers are already in; same data, different framing |

**Anti-scope rule for 4 weeks:** Do **not** start D3 full task suite, D5 Best-of-N paper, D8 continual learning, or D9 agent harness as *separate* papers. Mention them only in Limitations / Future Work of D1.

---

## 2. What the paper claims (must defend)

Each claim is already grounded in the monorepo. Do not invent claims outside this table.

| # | Claim | Repo evidence | Experiment / deliverable |
|---|-------|---------------|--------------------------|
| **C1** | Complete mobile-manipulation stack at **~$500** (vs ~$25–32k platforms) | `README.md` cost table; wiki BOM if present | Full BOM table: parts, suppliers, prices, total |
| **C2** | **Distributed edge/GPU inference**: control + perception on Pi 5; VLA on GPU workstation | `deploy.py` single/multi; `packages/vla_serve/`; `learning_engine/hardware/profiles.py` (`pi_workstation`) | **Headline experiment:** on-Pi vs distributed latency, Hz, CPU/GPU/power |
| **C3** | **Hybrid autonomy**: Nav2 + VLA/policy arbitrated by safety-bounded mux | `omnibot_hybrid/cmd_vel_mux.py`, `arm_cmd_mux.py`, `mission_planner.py` | Modest task trials: navigate-then-manipulate; success + interventions |
| **C4** | **Reproducible, SLO-gated** performance methodology | `benchmarks/conftest.py` `SLO_TABLE`, `run_benchmarks.sh`, `compare_baseline.py` | Full micro-benchmark table Pi5 + GPU; JSON in `benchmarks/results/` |
| **C5** | Hardware-portable deployment abstraction (profiles / ONNX EPs) | `learning_engine/hardware/` | Optional one-row stretch if time; else describe + defer numbers |
| **C6** | Learns pick-and-place from **~50 demos** (if you can support it) | `lerobot_engine/`, `data_engine/` | Only claim if you have demos + ≥1 eval set; else soften to “pipeline supports…” |

### Explicit non-claims (protects against rejection)

- No new VLA architecture / SOTA manipulation accuracy.
- No “better than Mobile ALOHA” on success rate without matched evaluation.
- No multi-robot fleet study, no human-subject study, no large-scale dataset release in this paper.

**Gap sentence (Intro):**  
While affordable manipulators (Mobile ALOHA, AhaRobot), open VLA models (OpenVLA, SmolVLA), and large datasets (Open X-Embodiment) each exist in isolation, there is little work describing a *reproducible end-to-end system* that unifies low-cost hardware, ROS 2, distributed edge/GPU inference, classical navigation, VLA manipulation, safety arbitration, and an SLO-gated benchmarking methodology into one open platform.

---

## 3. Critical path: what makes the paper “real”

> **Gating fact from existing plan:** `benchmarks/` suite + SLO table + runners exist, but **`benchmarks/results/` has not been the filled artifact path yet.** A systems paper *is* its measurements.

**Priority-zero (Days 1–3):**

```bash
# On Pi 5
./benchmarks/run_benchmarks.sh pi5
# On GPU workstation
./benchmarks/run_benchmarks.sh gpu
# With ROS stack up (control loop / topic latency)
./benchmarks/run_benchmarks.sh ros
```

Commit / archive:
- `benchmarks/results/*.json` (machine-labelled)
- Baseline via `benchmarks/compare_baseline.py` if baselines exist under `benchmarks/baselines/`
- Exact commit hash → git tag `paper-v1` / `arxiv-YYYY-MM-DD`

Without these numbers, stop expanding scope and do not polish prose alone.

---

## 4. Experiments (mapped to code — run, don’t rebuild)

### 4.1 Component micro-benchmarks → **Table 1**

| Suite | Path | SLOs (from `conftest.py`) | Machines |
|-------|------|---------------------------|----------|
| Yahboom serial TX/RX | `benchmarks/serial/bench_yahboom_protocol.py` | TX ≤5 ms max; RX parse ≤2 ms | Pi5 (+ hardware if port set) |
| Mecanum IK/FK/odom | `benchmarks/kinematics/` | IK/FK ≤0.5 ms; full cycle ≤1 ms | Pi5, GPU, CI |
| BEV stitch | `benchmarks/vision/bench_bev_stitcher.py` | 50 ms max (80 ms Pi override) | Pi5, GPU |
| SmolVLA preprocess | `benchmarks/vision/bench_preprocess.py` | preprocess ≤20 ms | Pi5, GPU |
| Inference | `benchmarks/inference/` | SmolVLA ≤200 ms (GPU tighter); OpenVLA ≤2 s | GPU |
| Control loop | `benchmarks/ros/bench_control_loop.py` | full loop ≤100 ms | ROS up |
| Topic latency | `benchmarks/ros/bench_topic_latency.py` | stamp deltas per SLO | ROS up |
| E2E pipeline | `benchmarks/system/bench_full_pipeline.py` | target interactive control (~5 Hz story) | multi-mode |

**Paper Table 1** = SLO target/max + measured p50/p95 per machine.

### 4.2 Headline experiment — distributed vs on-robot (C2) → **Figure 5 + Table 2**

| Mode | Configuration | Measure |
|------|---------------|---------|
| **A On-Pi** | Attempt VLA/policy path on Pi alone (or document failure/too-slow) | inference latency, achievable Hz, CPU/RAM, power |
| **B Distributed** | `python deploy.py --mode multi …`; Pi control/perception; GPU `vla_serve` `/predict` | e2e action latency, network payload×rate, Pi load, GPU util |

**Telemetry:** reuse `learning_engine/benchmarks/monitors.py` (`ResourceMonitor`: CPU/GPU/mem/power/temp).

**Story you want reviewers to take away:** interactive VLA on a $500 robot is impractical *without* the distributed split; the architecture is the contribution.

### 4.3 Hybrid autonomy value (C3) — keep N honest for 4 weeks

Tasks via `mission_planner` command style, e.g.:
```text
navigate:<named_location>,vla:pick the cup
```
(from `omnibot_hybrid` + `named_locations.yaml`)

**Compare (small but real):**
- Nav2-only reach success
- Policy/VLA-only manipulation at fixed pose (if time)
- Full hybrid mission success
- Safety interventions (0.2 m/s clamp, e-stop counts)

**Scale for 4 weeks:** 2–3 tasks × **10–15 trials** each (means + std or Wilson CI). Do **not** claim large-scale evaluation.

### 4.4 Optional stretch (only if Days 1–10 finish early)

- C5: same ONNX policy latency on workstation CUDA vs Pi CPU (one table row).
- C6: success vs #demos only if demos already collected; do not start a 50-demo collection campaign mid-month unless already underway.

### 4.5 Reproducibility protocol (section + appendix)

State in paper:
- Git commit / tag
- Ubuntu 24.04 + ROS 2 Jazzy; Pi 5 8 GB; GPU model + VRAM
- Exact `run_benchmarks.sh` invocations
- Trial counts; raw JSON location
- Network topology for multi-machine (`network.env`, `ROS_DOMAIN_ID=30`)

---

## 5. Paper structure (IEEE RA-L / conference style, ~6–8 pages)

| § | Content | Source material |
|---|---------|-----------------|
| 0 Abstract | Problem, $500 distributed stack, **fill key numbers after Week 1** | Results |
| 1 Introduction | Cost/access gap; bullets C1–C4 (C5–C6 if supported); contributions list | This plan + README |
| 2 Related Work | 6 themes (below) + gap sentence | New `related_work.bib` |
| 3 System Overview | BOM, holonomic base, 6-cam array, Pi + GPU topology | README hardware section |
| 4 Software Architecture | ROS graph, muxes, mission planner, policy/VLA nodes, BEV IPM | CLAUDE.md topic map |
| 5 Distributed Deployment | `deploy.py`, DDS peers, `vla_serve`, profiles | `deploy.py`, `packages/vla_serve/` |
| 6 Methodology | SLO table, machines, protocol, ethical/safety notes | `benchmarks/conftest.py` |
| 7 Results | Tables 1–2, latency bars, hybrid success, cost plot | Logged JSON + trials |
| 8 Limitations | Small N, single robot, rear-only depth FOV, coverage | Honest list |
| 9 Future Work | On-device VLA (DeepX/Jetson), larger study, safety verifier (D5), agent (D9) | Portfolio D2–D9 |
| 10 Conclusion | Platform thesis + open release | Short |

### Related-work themes (seed the bibliography)

1. Low-cost mobile manipulators — Mobile ALOHA, AhaRobot, SMARTmBOT, Hello Stretch  
2. Foundation models for robotics — RT-1/RT-2, OpenVLA, Octo, π0, SmolVLA  
3. Embodied datasets — Open X-Embodiment, DROID, BridgeData V2  
4. ROS 2 navigation / control — Nav2, ros2_control, MoveIt  
5. Edge AI deployment — TensorRT, ONNX Runtime, Jetson quantization papers  
6. Hybrid classical + learned control — emerging mux / shielding literature  

**Target bib size:** 40–70 entries in `docs/paper/related_work.bib` (or Overleaf).

---

## 6. Figures & tables checklist

| ID | Content | How to produce |
|----|---------|----------------|
| F1 | Hero photo of physical robot + labels | New photos this week |
| F2 | System architecture (Pi ↔ GPU, nodes) | draw.io / TikZ from CLAUDE.md diagram |
| F3 | Distributed inference dataflow | From `vla_serve` + topic map |
| F4 | Hybrid state machine + mux arbitration | `mission_planner` / `cmd_vel_mux` |
| F5 | Latency: on-Pi vs distributed (stacked bar) | §4.2 data |
| F6 | E2E pipeline latency CDF/violin | `bench_full_pipeline` |
| F7 | Resource use CPU/GPU/power | `ResourceMonitor` |
| F8 | Task success + safety interventions | §4.3 |
| F9 | Cost comparison vs Mobile ALOHA / Stretch | README table |
| T1 | SLO micro-benchmarks | `SLO_TABLE` + results JSON |
| T2 | Distributed vs local summary metrics | §4.2 |
| T3 | BOM excerpt | Wiki / spreadsheet |

Also produce a **2–3 min demo video** (MP4) for arXiv/supplement and README — high ROI for hiring/visa reviewers.

---

## 7. Four-week schedule (aggressive, day-level)

### Week 1 — Freeze + measure (critical path)

| Day | Tasks |
|-----|--------|
| **1** | Tag repo `paper-v1`; freeze features for the paper (no new subsystems). Confirm Pi + GPU networking (`network.env`, `deploy.py --show`). |
| **2** | Run `run_benchmarks.sh pi5` + `gpu`; save JSON under `benchmarks/results/`. Fix any broken harness issues *only* if they block numbers. |
| **3** | Run `ros` suite with stack up; start §4.2 distributed vs on-Pi experiment + telemetry. |
| **4** | Finish §4.2; photograph robot; draft BOM spreadsheet. |
| **5** | Start hybrid task protocol (2–3 tasks); begin `related_work.bib` (20 seed papers). |
| **6–7** | Continue hybrid trials to N≈10/task; build F2–F4 architecture diagrams. |

**Week 1 exit criteria:** results JSON committed; headline latency numbers known; BOM draft; ≥ half of hybrid trials done.

### Week 2 — Draft architecture + finish experiments

- Write: Introduction, Related Work, System Overview, Software Architecture, Deployment.
- Finish hybrid trials; generate draft F5–F8 from logs.
- Soften or drop C6 if demos are incomplete.
- Optional evenings: JOSS readiness scan of `sdk/` (do not derail D1).

**Week 2 exit criteria:** full first draft of §§1–5; all experimental data collected.

### Week 3 — Results + complete manuscript

- Write Methodology, Results, Limitations, Future Work, Conclusion, Abstract (numbers first).
- Camera-ready figures (vector PDF); tables in IEEE format.
- Internal reproducibility checklist pass (Section 9 below).
- Record demo video of real robot + architecture walkthrough.

**Week 3 exit criteria:** complete draft in IEEE template on Overleaf.

### Week 4 — Polish + submit

| Day | Tasks |
|-----|--------|
| **1–2** | Co-author / peer proofread; fix overclaims; tighten abstract. |
| **3** | Format final; check bib; embed video link / supplementary. |
| **4** | **arXiv upload** (`cs.RO`, optionally `cs.LG` cross-list). |
| **5** | **RA-L submission** (or concurrent workshop short paper if CFP open). |
| **6–7** | Update GitHub README badge + one-pager; LinkedIn/X thread; portfolio PDF with arXiv ID. |

**Week 4 exit criteria:** arXiv ID live; RA-L (or workshop) submitted; public repo points at paper.

---

## 8. Venue strategy (visa / hiring optimized)

| Priority | Venue | Why | When |
|----------|-------|-----|------|
| **P0** | **arXiv cs.RO** | Instant citable handle; dates the work | End of Week 4 |
| **P1** | **IEEE RA-L** | Rolling; systems/engineering welcome; can present at ICRA/IROS if accepted | Submit with arXiv |
| **P2** | **ICRA/IROS/CoRL workshop** | Faster accept; visibility | If open CFP matches timing |
| **P3** | **JOSS (D7)** | Software citation for `sdk/ohho` | After D1 arXiv (Week 5+) |
| **Defer** | Full ICRA/IROS/RSS main track | Fixed deadlines, higher bar | Next cycle *with* RA-L/arXiv citation already in hand |

**Do not** gate the month on a single annual conference deadline. For visa/portfolio, **arXiv + RA-L + open repo + demo video** is the optimal bundle.

---

## 9. Reproducibility & integrity checklist

- [ ] Public repo + exact commit/tag cited in paper  
- [ ] BOM with part numbers, suppliers, prices, total  
- [ ] `benchmarks/results/*.json` archived; commands in paper  
- [ ] Machine specs listed (Pi 5 8 GB; GPU model + VRAM; ROS 2 Jazzy / Ubuntu 24.04)  
- [ ] Trial counts, seeds/protocol, error bars or CIs  
- [ ] Honest Limitations (rear depth FOV, scale of N, single platform)  
- [ ] Real-robot video  
- [ ] No fabricated / extrapolated benchmarks (enforced by `CODE_OF_CONDUCT.md`)  

---

## 10. Writing assets & workspace layout (proposed)

Create under `docs/paper/` (when implementation starts — **not** required before you approve this plan):

```text
docs/paper/
  PAPER_PLAN.md          # already exists — keep as living short plan
  PAPER_PORTFOLIO.md     # already exists
  PRODUCT_DIRECTIONS.md  # already exists
  manuscript/            # optional local mirror of Overleaf
    main.tex
    sections/
    figures/
    tables/
    related_work.bib
  results/               # copies or links to benchmarks/results for paper freeze
  notes/
    claims.md            # freeze of C1–C6 wording
    experiment_log.md    # trial-by-trial notes
    bom.csv
```

Prefer **Overleaf + IEEE RA-L / conference template** as source of truth for `.tex`; keep raw numbers in git.

---

## 11. Author / narrative package for visa & hiring

Beyond the PDF:

1. **One-page research summary** (problem → method → 3 numbers → impact)  
2. **README badges:** arXiv, DOI (when available), license, ROS 2  
3. **2–3 min demo video** + 30 s silent loop for portfolio  
4. **Talk track** (5 min): cost gap → architecture diagram → latency figure → hybrid mux → open release  
5. **Citation ready:** BibTeX on arXiv page + “If you use OmniBot, cite …” in README  

This package matters as much as the paper for Global Talent / hiring committees.

---

## 12. Multi-paper portfolio (after month 1 — do not start early)

Sequence already designed in `PAPER_PORTFOLIO.md` / growth papers in `PRODUCT_DIRECTIONS.md`:

```text
Month 1     D1 arXiv + RA-L          ← THIS PLAN
Optional    D4 workshop / D7 JOSS    ← only if D1 is solid
Month 2–3   D2 VLA deployment ablations (TRT / 4-bit / latency)
Month 3–4   D3 hybrid + safety envelope (larger N)
Later       G1 public leaderboard, G2 dataset paper, D5 verifier, D6 sim→real data
Stretch     D8 continual learning, D9 deliberative agent
```

**Anti-cannibalization:** each paper owns one primary contribution (platform / inference eng / arbitration / methodology / safety / data / software). Cite earlier papers; do not re-slice the same figure set.

---

## 13. Risks & mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| No benchmark JSON / broken harness | No paper | Days 1–3 only fix harness; freeze features |
| On-Pi VLA path not runnable | Weak C2 | Still publish “infeasible / >X s” with honest method; distributed still wins |
| Hybrid trials unstable | Weak C3 | Reduce to 1–2 tasks; report failures; emphasize systems metrics |
| Overclaiming vs Mobile ALOHA | Rejection | Cost + architecture comparison only; no SOTA success claims |
| Scope creep (agent, RL, market) | Miss deadline | Future Work only |
| Networking flaky multi-machine | Bad latency numbers | Document topology; multiple runs; report p50/p95 |
| Time for writing | Incomplete draft | Architecture sections can be written in parallel with Day 1 measurements |

---

## 14. Week-1 action list (start immediately after approval)

1. Tag `paper-v1` and open `docs/paper/notes/experiment_log.md`.  
2. On Pi: `./benchmarks/run_benchmarks.sh pi5` (+ `ros` with stack).  
3. On GPU: `./benchmarks/run_benchmarks.sh gpu`.  
4. Run distributed vs local protocol; log with `ResourceMonitor`.  
5. Photograph robot; start BOM CSV.  
6. Seed `related_work.bib` (40+ papers over week).  
7. Draft F2 architecture diagram (draw.io).  
8. Create Overleaf IEEE project and paste skeleton sections.

---

## 15. Success definition (end of 4 weeks)

You have succeeded if **all** of the following are true:

1. **arXiv preprint** with abstract that contains **real measured numbers** (latency, cost, success rates).  
2. **RA-L or workshop submission** filed.  
3. **Public git tag** matching the paper.  
4. **Demo video** of the physical robot.  
5. **README** links arXiv + explains how to reproduce Table 1.

That bundle is the visa/hiring signal; peer-review acceptance can trail by months and still count once the preprint is public.

---

## 16. Alignment with existing repo docs

| Existing doc | Role relative to this plan |
|--------------|----------------------------|
| `docs/paper/PAPER_PLAN.md` | Short form of D1; this master plan **extends** it for visa timeline + day-level ops |
| `docs/paper/PAPER_PORTFOLIO.md` | Follow-on papers D2–D9 after month 1 |
| `docs/paper/PRODUCT_DIRECTIONS.md` | Growth papers (G1–G8); use for product strategy, not for week-1 scope |
| `CONTRIBUTING.md` research section | How external contributors add benchmarks later |
| `benchmarks/` | Measurement backbone |
| `CODE_OF_CONDUCT.md` | Scientific integrity bar for results |

---

## 17. Open items (non-blocking; resolve during Week 1)

1. Exact **author list** and affiliations on the title page.  
2. Whether a **workshop CFP** is open in your current conference cycle (opportunistic P2).  
3. Whether any **pretrained checkpoints** used require third-party citation/license notes (OpenVLA, SmolVLA, LeRobot).  
4. Confirm BOM currency (2026 prices) for C1 table.  
5. Preferred IEEE template: RA-L vs ICRA dual-submission style (RA-L first recommended).

---

*This plan is grounded in the current OmniBot monorepo: full ROS 2 mobile-manipulation stack, distributed VLA serving, hybrid muxes, SLO-gated benchmarks, learning/agent engines, and OhhO OS SDK. The critical path is measurement + writing under a systems framing — not inventing a new model.*
