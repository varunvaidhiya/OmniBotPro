# OmniBot First Paper: Research Design and Evidence Plan

## 1. Confirmed first publication

The documents consistently select the first paper as a **systems and robotics-engineering manuscript**, not a new-policy or new-algorithm paper:

> **OmniBot: An Affordable, Distributed Embodied-AI Platform for Mobile Manipulation with Vision-Language-Action Models**

The intended publication path is an arXiv `cs.RO` preprint followed by an IEEE Robotics and Automation Letters submission. The governing research question is whether an open, low-cost mobile manipulator can provide a **measured and reproducible** edge/GPU deployment path for VLA-enabled mobile manipulation. This question is narrower and more defensible than claiming a first low-cost robot, a new VLA, or state-of-the-art task success.

| Item | Evidence from repository | Interpretation for the manuscript |
|---|---|---|
| Platform | Mecanum base, SO-101 arm, six-camera sensing, Raspberry Pi 5 and separate NVIDIA GPU workstation | The physical/system subject under evaluation |
| Policy options | SmolVLA, OpenVLA and model-agnostic policy hooks | Enabling policies; **not** algorithmic contributions |
| Classical subsystem | ROS 2 Jazzy, Nav2, SLAM and odometry | Baseline navigation services |
| Integration mechanism | Command multiplexer, arm multiplexer and mission planner | The architectural boundary to describe and test |
| Deployment mechanism | Single/multi-machine `deploy.py`, `vla_serve` and hardware profiles | The core edge/GPU systems contribution |
| Measurement mechanism | SLO table, per-machine benchmark runners, JSON results and resource monitor | The reproducibility contribution, conditional on real results |

## 2. Literature-grounded positioning

Low-cost mobile manipulators, including Mobile ALOHA, AhaRobot and TidyBot++, already show that open hardware, teleoperation and learned mobile-manipulation policies are active research directions. Mobile ALOHA demonstrates low-cost whole-body teleoperation with on-board laptop compute; AhaRobot foregrounds a low-cost dual-arm platform, precision compensation and remote teleoperation; and TidyBot++ presents an open holonomic mobile manipulator for imitation-learning data collection [L1] [L11] [L12]. Therefore **affordability, openness, holonomy, teleoperation, and a learned-policy demonstration cannot individually be presented as OmniBot’s novelty**.

At the model layer, RT-2 established the VLA formulation, while OpenVLA and SmolVLA provide open models, fine-tuning paths and increasingly accessible consumer-grade deployment [L2] [L5] [L6]. Large-scale data resources including Open X-Embodiment, BridgeData V2 and DROID provide broad datasets and data-collection infrastructure [L4] [L9] [L10]. ROS 2, Nav2 and RobotPerf provide mature navigation and performance-evaluation foundations [L7] [L8]. The paper must identify the integration and measurement gap that remains after acknowledging these capabilities.

| Theme | What the literature already establishes | What OmniBot must contribute or test |
|---|---|---|
| Affordable mobile manipulators | Open low-cost mobile manipulation and teleoperation are feasible | A clearly costed single-arm, mecanum alternative; no priority claim |
| VLA models | Open VLA models, consumer-GPU adaptation and compact policies exist | Operational deployment behaviour across Pi edge control, LAN serving and physical actuation |
| Datasets | Large, open robot datasets and low-cost collection systems exist | Optional local data-collection interoperability only; do not claim dataset-scale contribution |
| ROS 2 navigation | Nav2 supplies configurable mobile navigation and velocity commands | The handover from deterministic navigation to learned manipulation/control sources |
| Benchmarking | RobotPerf supports reproducible ROS 2 computing benchmarks | Platform-specific, SLO-gated full-pipeline protocol with raw artefacts and task linkage |
| Safe learning | Formal shielding has explicit specifications and corrective monitoring | A bounded mux/e-stop implementation; call it a shield only after formal verification work |

## 3. Research gap and problem statement

### 3.1 Defensible research gap

> **Gap.** Existing work separately provides low-cost mobile-manipulation hardware, open VLA policies, robot-learning datasets, ROS 2 navigation, and component-level robotics benchmarks. There is limited open, reproducible systems evidence for an end-to-end configuration in which low-latency control and perception run on a constrained robot computer, VLA/policy inference runs on a networked consumer GPU, classical navigation and learned commands are explicitly arbitrated, and latency/resource/task measurements are released together with the platform configuration.

This is a qualified gap, not a claim of absence. It must be supported by a systematic comparison table containing the closest papers and by the released OmniBot protocol/data.

### 3.2 Problem statement

A VLA-capable policy can demand more memory and compute than an inexpensive on-robot processor can reliably offer, while mobile manipulation remains safety- and latency-sensitive. A practical developer needs to decide how to split perception, control, policy serving, and classical navigation between an edge computer and a GPU workstation without obscuring network delay, resource cost, reproducibility, or failure handling. Current documentation alone cannot answer whether the proposed split provides an adequate control rate, tail latency, resource envelope, and mission-level outcome on a physical low-cost platform.

**Problem statement:**

> Design and empirically evaluate an open mobile-manipulation reference platform that separates robot-resident real-time I/O and perception from GPU-resident VLA/policy inference, coordinates learned and classical control sources through explicit interfaces, and reports reproducible end-to-end performance, resource, cost, and task-level evidence.

## 4. Research questions and hypotheses

| ID | Research question | Testable hypothesis | Primary outcomes | Decision rule |
|---|---|---|---|---|
| RQ1 | Does the Pi-edge/GPU-server split improve policy responsiveness relative to a local Pi configuration? | **H1:** The distributed configuration has lower median and p95 policy-to-action latency and a higher sustainable action rate for the same policy/configuration. | Inference p50/p95, end-to-end p50/p95, Hz, deadline miss rate | Claim support only with paired configuration, repeated runs and raw traces |
| RQ2 | What is the systems cost of the split? | **H2:** The split places policy compute on the GPU while keeping Pi CPU/RAM, network bandwidth and control-loop latency within declared envelopes. | CPU/RAM/GPU util, power, temperature, network bytes/s, dropped/late actions | Report actual values and SLO status; do not infer safety from thresholds |
| RQ3 | Can the platform compose deterministic navigation and learned manipulation without changing the individual subsystems? | **H3:** A staged navigation-then-manipulation mission is executable through explicit command-source transitions, and its failure modes can be observed in logs. | Stage success, mission success, completion time, intervention and abort counts | Exploratory unless each condition has pre-registered trials and an appropriate baseline |
| RQ4 | Are OmniBot’s measurements reproducible and useful for regression detection? | **H4:** Repeated runs reproduce summary metrics within reported uncertainty, and a deliberately seeded regression is detected by the published SLO gate. | Per-run distribution, p95 SLO outcomes, regression-detection outcome | Release environment, command, commit, raw JSON, and regression patch/protocol |
| RQ5 | What is the actual affordability boundary of the platform? | **H5:** The robot-only BOM is lower than selected comparators under a consistent cost definition. | Three-part cost table, BOM date/currency/suppliers | No cross-paper claim unless accounting boundary is matched or differences are explicit |

## 5. Contribution claims: claim only after evidence exists

| Claim ID | Proposed claim wording | Minimum supporting evidence | Current status |
|---|---|---|---|
| C1 | OmniBot is an open, low-cost mobile-manipulation reference platform with a documented BOM. | Dated BOM with part numbers, quantity, supplier, currency, three cost totals and license/repository tag. | Architecture exists; BOM and total must be frozen. |
| C2 | A Pi-edge/GPU-server configuration enables measured VLA/policy deployment characteristics relative to a local edge configuration. | Matched-mode latency/throughput/resource data, network topology, model/checkpoint version, repeated runs. | **Unmeasured.** This is the headline experiment. |
| C3 | OmniBot composes Nav2 and learned-policy control through explicit mission and action-arbitration interfaces. | System diagram, source code release, transition logs and limited task-study results. | Architecture exists; physical evidence required. |
| C4 | OmniBot releases a reproducible, SLO-aware, platform-specific performance protocol. | SLO rationale, runners, raw JSON, complete machine spec, runs/repeats and a seeded-regression demonstration. | Harness exists; results and reproducibility study missing. |
| C5 | The deployment abstraction supports multiple target profiles. | At least two actual target results using the same policy/configuration. | Optional. Describe as designed support if unmeasured. |
| C6 | The system learns a task with approximately 50 demonstrations. | Dataset card, protocol, held-out physical trials, task definition and uncertainty. | **Do not claim** until the experiment exists. |

The paper should use **“safety-bounded command mux”** for the current documented implementation. The term **“safety shield”** requires a formal safety specification, explicit monitor semantics, evidence of corrective intervention, and an appropriate guarantee; source-selection and fixed limits alone do not establish that claim [L3].

## 6. System under test and comparison conditions

### 6.1 Fixed system description

The paper should document the Raspberry Pi 5 (8 GB) edge node, GPU workstation, Raspberry Pi operating system/version, GPU model and driver, ROS 2 Jazzy version, network transport, wired/wireless network properties, camera resolutions/frame rates, policy model/checkpoint/hash, quantization mode, action chunk size, control period, and relevant software commit. The fixed base configuration is a 9-dimensional action vector: six arm joint commands plus planar base commands `(v_x, v_y, \omega_z)`.

### 6.2 Primary modes

| Mode | Pi 5 responsibilities | GPU workstation responsibilities | Purpose |
|---|---|---|---|
| M0: component baseline | Run the selected component benchmark | None unless the component needs GPU | Isolate protocol, kinematics, perception and local preprocessing costs |
| M1: local/edge policy | Edge I/O, perception, preprocessing and attempted policy execution | None | Establish feasible local policy behaviour or a documented resource/latency limit |
| M2: distributed policy | Edge I/O, sensor capture, required preprocessing, action execution and timing stamps | Policy/VLA service, model inference, server timing and GPU telemetry | Primary Pi-to-GPU comparison |
| M3: navigation-only | Nav2 and robot edge stack | None | Stage-level navigation baseline |
| M4: manipulation-at-goal | Fixed base pose, policy action path | Policy service as applicable | Separates grasp/manipulation outcomes from navigation |
| M5: hybrid mission | Nav2 followed by learned-policy stage through mission/mux transition | Policy service as applicable | Exploratory integrated mobile-manipulation evaluation |

A local Pi condition must run the **same policy class, checkpoint, input tensors, action representation, frame rate and warm-up protocol** as far as feasible. If a model cannot load on the Pi, report this as a deployability result with its memory/error evidence; do not substitute a different small model and label it a direct latency comparison.

## 7. Measurement and experimental methodology

### 7.1 Timing model

Instrument every relevant transition with monotonic clocks and unique request/action identifiers:

\[
L_{\mathrm{e2e}} = t_{\mathrm{actuation\_publish}} - t_{\mathrm{frame\_capture}}.
\]

The paper should also decompose this total when clocks are comparable or offsets are measured:

\[
L_{\mathrm{e2e}} = L_{\mathrm{capture}} + L_{\mathrm{preprocess}} + L_{\mathrm{uplink}} + L_{\mathrm{queue}} + L_{\mathrm{inference}} + L_{\mathrm{downlink}} + L_{\mathrm{mux}} + L_{\mathrm{actuation}}.
\]

Cross-host timing requires clock synchronization. Use chrony/PTP/NTP discipline, record offset and jitter before every block, and prefer one-way segment timings only when the synchronization error is materially smaller than the reported effect. Otherwise publish client round-trip time and server-side inference time separately. **Never sum unsynchronized wall-clock timestamps.**

### 7.2 Benchmark protocol

| Benchmark class | Example implemented metric | Minimum repetitions | Reporting |
|---|---|---:|---|
| Motor/protocol | Yahboom TX/RX packet encode/decode | 1,000 after 50 warm-up iterations | mean, p50, p95, p99, max, SD and SLO state |
| Kinematics | Inverse/forward kinematics and complete cycle | 1,000 after 50 warm-up iterations | Same summary, CPU pinning/governor state |
| Perception | BEV stitch, warp/blend, VLA preprocessing | 1,000 after 50 warm-up iterations | Input resolution/frame source and p95 |
| Policy service | Client RTT, queue time, server inference time, output serialization | At least 30 independent short runs; 100+ requests/run | Per-run and pooled distributions; cold/warm state separate |
| Full pipeline | Capture-to-actuation latency and achievable control rate | At least 30 independent short runs per mode | CDF/violin plus deadline miss rate |
| Resource/power | CPU, RAM, GPU util/VRAM, temperature, power and network rate | Sample at fixed rate throughout every run | Time series and run-level median/p95/peak |
| Regression | Intentional added delay or degraded code path | At least one clean and one seeded-regression run | Detection rule and pass/fail result |

The existing `TimingHarness` computes p50, p95, p99 and summary statistics. Its default warm-up and repetition counts must be stated. Values in the existing SLO table are engineering targets and gates; they are **not experimental results** and must appear in the paper only as predeclared thresholds until measurements are collected.

### 7.3 Task-study protocol

The task study should remain deliberately modest. Choose two or three fixed tasks that expose both navigation and manipulation, such as a named-location approach followed by a single-object pick. Pre-register: workspace layout, object type/pose distribution, navigation tolerance, grasp success definition, task time limit, reset procedure, model version, operator involvement, termination rules, and whether an e-stop counts as a failure.

For each task and mode, use 15–20 independent trials when feasible. Report the numerator and denominator, Wilson 95% confidence interval for binary success, median and IQR for skewed completion time, and every failure category. A useful taxonomy is: localization/planning failure, perception failure, network/service timeout, policy error, grasp/arm execution failure, mux/transition failure, and manual/e-stop intervention. Do not add a VLA-only mobile-navigation baseline unless it is a runnable, predefined system; an absent or incomparable baseline is more honest than a weak strawman.

### 7.4 Statistical analysis

The primary inferential unit for M1 versus M2 is the **independent run**, not an individual frame/request, because sequential requests are autocorrelated. For each paired configuration, report the estimated difference in median run-level latency and in sustainable action rate with a 95% bootstrap confidence interval. If using a null-hypothesis test, use a paired nonparametric test only for prespecified hypotheses and report effect size plus uncertainty; do not treat a large request count inside one run as a large sample size. Report raw runs in CSV/JSON so reviewers can reanalyse.

For task success, give Wilson intervals and an exact or Fisher test only where a prespecified comparison and sufficient trials exist. With a single robot and small N, the result is a platform demonstration, not a population-level generalization claim.

### 7.5 Cost accounting protocol

| Cost view | Include | Exclude only if explicitly justified |
|---|---|---|
| Robot-only BOM | base, arm, motor board, sensors, battery, structural parts, cables, storage, on-robot Pi | Remote workstation, but label this clearly |
| On-robot operational system | Robot-only BOM plus on-robot compute and required cooling/power | Remote training/inference workstation |
| Full experimental stack | On-robot operational system plus GPU workstation, networking, displays/teleop peripherals and shared power equipment | Nothing material to demonstrating the reported configuration |

Report source URL/supplier, date, currency, quantity, shipping/tax convention and total. Convert currency with a dated exchange rate and preserve original currency. Existing papers vary in whether they include compute, so the comparison table needs a separate “cost boundary” column rather than a single rank ordering.

## 8. Figures, tables and release package

| ID | Artifact | Evidence required before inclusion |
|---|---|---|
| Figure 1 | Annotated physical robot and compute topology | Original photo, actual BOM labels and compute-boundary caption |
| Figure 2 | ROS 2 / service architecture diagram | Exact topic and service interfaces from the released commit |
| Figure 3 | Timestamped distributed-inference sequence diagram | Implemented event names and clock-synchronization note |
| Figure 4 | Median/p95 latency breakdown by mode | Raw repeated-run traces; no simulated values |
| Figure 5 | Full-pipeline latency distribution / deadline misses | Per-run data and threshold definition |
| Figure 6 | CPU/GPU/RAM/power/network time series | Telemetry logs and sampling rate |
| Figure 7 | Task-stage success and failure taxonomy | Trial log with N and confidence intervals |
| Table I | BOM and cost-accounting boundaries | Dated supplier evidence |
| Table II | SLO table plus Pi/GPU measured results | Results JSON, target and maximum columns clearly separated |
| Table III | Local versus distributed deployment | Matched configurations, model version and CIs |
| Table IV | Related-work comparison | Paper-specific evidence; “not reported” rather than inferred no |

The public release must contain: exact git tag and lockfiles; BOM CSV; network topology; model/checkpoint provenance and licence; all benchmark invocations; raw machine-labelled JSON/CSV; analysis script/notebook; trial log; source-data schema; a versioned protocol; and a short video showing both normal operation and at least one logged failure/abort path.

## 9. Limits and threats to validity

The first paper will be constrained by one robot, a small number of task trials, a limited camera/depth geometry, local-LAN-specific network performance, and one or a small number of GPU workstation configurations. It cannot establish broad generalization to arbitrary robots, homes, networks, policy checkpoints or safety-critical environments. A multiplexed command interface and velocity clamps do not prove safety. Performance SLOs selected by the system designer are engineering criteria, not externally validated guarantees. The manuscript should state these points plainly.

## 10. Immediate execution order

1. Freeze a paper commit/tag and create the BOM with the three cost boundaries.
2. Verify network clock synchronization and record host/GPU/software specifications.
3. Run the existing micro-benchmark suites on Pi and GPU; retain every raw JSON result.
4. Instrument request/action IDs and timestamps for M1/M2; pilot the local versus distributed comparison.
5. Run repeated M1/M2 blocks and generate the latency/resource analysis from raw data.
6. Validate the mission/mux path with a written task protocol before collecting the small task study.
7. Perform and document a seeded regression test for the benchmark-gate claim.
8. Draft the results only after measured values exist; retain placeholder labels in the manuscript until then.

## 11. Source key

The literature source keys refer to `LITERATURE_REVIEW_NOTES.md`: L1 Mobile ALOHA; L2 OpenVLA; L3 Safe RL via Shielding; L4 Open X-Embodiment; L5 SmolVLA; L6 RT-2; L7 RobotPerf; L8 Nav2; L9 DROID; L10 BridgeData V2; L11 AhaRobot; L12 TidyBot++.
