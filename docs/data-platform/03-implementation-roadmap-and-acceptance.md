# 03 — Implementation Roadmap and Acceptance Plan

**Status:** Proposed 90-day delivery plan  
**Author:** Manus AI  
**Prepared:** 15 August 2026

## 1. Delivery objective

By the end of 90 days, an external design-partner user should be able to complete one documented **OmniBot Data → Train → Serve golden path**: pass robot preflight, collect or import supervised demonstrations, inspect quality, release an immutable dataset version, train a declared policy, evaluate it on a repeatable task, and deploy or roll it back under operator supervision. The resulting claims must be backed by versioned benchmark and evaluation artifacts, not screenshots or manually assembled logs.[1]

This is a proof-concentration plan. It deliberately avoids generic marketplace, fleet, or industrial adapter development until the team has validated one user outcome.

## 2. Before implementation: three decisions to lock in week 1

| Decision | Recommended answer | Why it must be locked | Owner |
|---|---|---|---|
| **Reference task** | A low-risk, repeatable coloured-cube pick-and-place task with a marked destination and defined success detector. | Determines task language, quality policy, evaluation protocol, and the right dataset composition. | Technical lead + operator |
| **Supported profile** | Yahboom mecanum base + SO-101 arm + front/wrist/BEV vision + Pi 5 ROS 2 + GPU workstation. | This is the only profile with a complete concrete topic/action contract in the repository.[2] | Robotics systems owner |
| **Artifact location for development** | Local filesystem or MinIO-compatible object storage plus a project metadata database; production object store is a deployment choice, not a data-contract change. | Separates product correctness from early cloud-provider lock-in. | Platform owner |

The team should also freeze nonessential product-console work. Priority one is a scope/positioning correction; priority two is the evidence system; priority three is the data-to-policy product. Treating all 19 modules as equally deliverable would undermine this schedule.

## 3. Workstreams and delivery order

| Stream | What it owns | Starts | Completion dependency |
|---|---|---:|---|
| **A. Golden-path contract** | Supported profile, task protocol, quality policy, success metrics, benchmark context schema. | Week 1 | Required before capture UX and marketing copy. |
| **B. Correctness repairs** | Training-loop control-flow repair, count-aware statistics, source-data failure handling, tests. | Week 1 | Required before trustworthy first training run. |
| **C. Data Agent and staging** | Preflight, session lifecycle, local manifest, raw capture, offline queue, ingestion-job trigger. | Week 2 | Requires profile contract and stable recording commands. |
| **D. Catalogue, QA, and versioning** | Persisted entities, artifact ledger, quality reports, review decisions, dataset manifest/splits. | Week 2 | Required before Train launch UI. |
| **E. Train/Serve lineage** | Training-run adapter, policy registry, evaluation evidence, deployment/rollback gate. | Week 5 | Requires immutable dataset version. |
| **F. Evidence and external usability** | Benchmarks, quickstart, reference demo, design-partner run, decision log. | Week 1, continuous | Depends on every actual workflow milestone. |

## 4. 90-day roadmap

### Phase 0 — Weeks 1–2: make the existing loop trustworthy

The first phase intentionally works close to the existing code. It produces a correct baseline before a service architecture obscures defects.

| Deliverable | Detailed tasks | Done only when |
|---|---|---|
| **Supported golden-path specification** | Freeze one robot profile, topics, cameras, 9-D action/state schema, task protocol, workspace layout, safety rules, and holdout conditions. | A person can follow a single document to prepare the robot and knows exactly what is supported. |
| **Training correctness repair** | Move the epoch body inside the epoch loop; make checkpoint frequency real; test a two-epoch mock run; record a structured run manifest. | A test proves that two requested epochs perform two scheduler steps and create expected checkpoint artifacts. |
| **Data writer correctness repair** | Replace episode-weighted stats with per-frame running moments; add frame count to state; add test with unequal episode lengths. | Statistics match an offline reference calculation to a numerical tolerance. |
| **Capture-failure policy** | Stop silently treating absent cameras as valid black frames; emit per-stream quality flags; choose reject/quarantine/explicit-imputation semantics. | An input bag with a missing required camera cannot be silently accepted into an approved dataset. |
| **Baseline evidence skeleton** | Create benchmark context JSON, capture environment versions/config hashes, and run the current benchmark runner in every feasible mode. | `benchmarks/results/` contains dated, attributable baseline JSON or documented hardware-unavailable status.[3] |
| **Public-surface correction** | Label the Data console as demo-only until persistent APIs are connected, or hide unimplemented controls. | A user cannot mistake in-memory sample episodes for a real project dataset. |

### Phase 1 — Weeks 3–4: ship real capture-to-candidate data

| Deliverable | Detailed tasks | Done only when |
|---|---|---|
| **Robot profile registry** | Define `omnibot-mobile-manipulator@1.0.0`: schema hash, expected ROS topics, image dimensions/encoding, rate expectations, action limits, calibration/version fields, and safety prerequisites. | Preflight compares a physical robot instance against this declaration and returns structured pass/fail results. |
| **Data Agent MVP** | Implement a local service/CLI that starts capture, writes a local session manifest, records event markers, closes/aborts sessions, survives restart, and queues sync. | Start/stop/abort are idempotent; an offline session persists through agent restart. |
| **Staged ingestion** | Invoke the current bag converter in a per-session staging directory, validate result, compute hashes, and publish a candidate record only on success. | Two concurrent conversions cannot collide on dataset metadata or episode indices. |
| **Candidate catalogue** | Persist sessions, candidate episodes, artifacts, quality reports, and operational logs. | The console reads actual candidates from the catalogue, with no hard-coded representative data. |
| **Quality report v1** | Add duration/frame rate, coverage, timestamp deltas, data ranges, image decode/blank-frame detection, source type, and structural validator result. | A user sees machine-readable reasons for pass, quarantine, or failure. |

### Phase 2 — Weeks 5–7: version data and close the Train/Serve loop

| Deliverable | Detailed tasks | Done only when |
|---|---|---|
| **Curation and review** | Build episode playback from real artifacts, kept/review/rejected status, reason capture, and audit trail. | Review mutations persist and can be reproduced from the audit log. |
| **Dataset composer** | Accept selected candidates, derive session-aware train/validation/test split, calculate count-aware stats, materialise LeRobot layout, and write an immutable `manifest.json`. | A dataset version can be reconstructed exactly from its manifest and hashes. |
| **Training adapter** | Resolve a named dataset version to a local materialisation, invoke an approved policy config, capture stdout/metrics, register checkpoint artifacts and environment. | A training run page shows its exact dataset version, model config, code commit, GPU profile, and artifacts. |
| **Evaluation protocol** | Automate simulation first; define physical supervised runs with success/failure taxonomy and constraints. | A policy release candidate cannot be created without evaluation evidence. |
| **Policy registry and controlled deploy** | Add compatibility checks, approval state, deployment record, health links, and rollback path using existing runtime/observability. | An operator can deploy one approved release to one compatible robot and revert it. |

### Phase 3 — Weeks 8–10: prove external usability and reliability

| Deliverable | Detailed tasks | Done only when |
|---|---|---|
| **Golden-path quickstart** | Write and test the external-user installation, profile registration, capture, curation, training, evaluation, deploy, rollback, and troubleshooting instructions. | A fresh internal tester completes it without creator intervention; elapsed time is recorded. |
| **Benchmark/evidence publication** | Run the full supported matrix; publish configuration, commit, model/dataset version, p50/p95 latency, failures, limitations, and task success results. | Results are machine-readable, reviewed, and linked to the documented workflow. |
| **Design-partner run** | Select one external user/lab, agree task and support boundaries, observe end-to-end run, capture friction/issues, and request pilot/case-study permission. | One external user completes a material part of the workflow; all failures become prioritised issues. |
| **Day-60 commercial gate** | Conduct structured discovery in parallel, compare research/open-hardware and AGV/AMR evidence, and record decision. | The company chooses the next wedge from evidence rather than architectural preference. |

### Phase 4 — Weeks 11–13: turn the proof into a repeatable offer

| Deliverable | Detailed tasks | Done only when |
|---|---|---|
| **Pilot package** | Fix support boundaries, robot count, task, deliverables, acceptance test, data rights, compute terms, and paid engineering scope. | A design partner can sign a bounded statement of work without implied unlimited integration. |
| **Reliability hardening** | Add retry/idempotency tests, degraded-network tests, artifact integrity checks, recovery runbook, and incident taxonomy. | The team can diagnose and recover a failed job/session from logs and documented steps. |
| **Evidence-driven public release** | Publish only claims measured in the golden path; expose capability matrix and known limitations. | Product page, quickstart, benchmark results, and source code agree. |

## 5. First two-week engineering backlog

This is the precise place to start. Every ticket should include an owner, issue ID, test/evidence artifact, and acceptance criterion.

| Order | Ticket | Files/components affected | Acceptance criterion |
|---:|---|---|---|
| 1 | **Define `omnibot-mobile-manipulator@1.0.0` profile** | New `data_engine/profiles/omnibot_mobile_manipulator.yaml`; reuse schema constants. | Profile declares topic/image/action/state/calibration/safety contract and validates against known system docs.[2] [4] |
| 2 | **Repair and test training epoch loop** | `lerobot_engine/train.py`; new unit test. | A deterministic two-epoch test observes two training iterations, correct scheduler steps, and expected checkpoints.[5] |
| 3 | **Correct streaming statistics** | `data_engine/ingestion/bag_to_omnibot.py`; tests. | Stats are sample-weighted across unequal episode lengths, with explicit count persisted in metadata.[6] |
| 4 | **Replace silent missing-image substitution** | Converter and validator/quality module. | Required-stream loss produces a failed/quarantined candidate with coverage metrics; any future imputation is explicit metadata. |
| 5 | **Create the dataset manifest contract** | New `data_engine/schema/manifest.py` and JSON Schema; tests. | A dataset version manifest validates and captures source, split, quality, configuration, and artifact hashes. |
| 6 | **Add job staging and transactional commit** | New `data_engine/jobs/` or service module; ingestion wrapper. | Parallel conversions write independent staging directories and one catalog commit path; no shared JSONL race.[7] |
| 7 | **Publish benchmark context/result standard** | `benchmarks/results/README.md`, result JSON schema, runner integration. | Every result includes commit, runner version, profile, hardware, OS, model/dataset version, config, timestamp, and failure fields. |
| 8 | **Run the baseline benchmark matrix** | Existing `benchmarks/run_benchmarks.sh`; hardware setup. | CI baseline passes; Pi/GPU/ROS results are captured or explicitly blocked with evidence. |
| 9 | **Design the persisted Data API** | API schema/document; map current console fields to real entities. | API supports list/get session/episode, review decision, create dataset version, job status; no fake mutation path. |
| 10 | **Update user-facing copy** | Website Data console / docs. | The product makes no false claim that sample data actions are persisted or that the first-policy target is already achieved. |

## 6. Benchmark and evidence plan

### 6.1 Evidence hierarchy

| Layer | Required measurements | Existing leverage |
|---|---|---|
| **Data capture** | Preflight pass rate; episode duration/frame count; camera/action/state coverage; p95 stream time delta; missingness; disk throughput; operator aborts. | Existing topic contract and synchroniser.[2] [8] |
| **Ingestion** | Source-to-candidate time; conversion failure rate; video decoding failures; structural validation rate; artifact bytes per minute of recording. | Existing converter/validator.[6] [9] |
| **Training** | Dataset version; split; model/config; GPU/VRAM; wall time; loss; checkpoint size; training/evaluation errors. | Existing multi-model trainer and optional W&B logging.[5] |
| **Serving** | Model load time; p50/p95 prediction latency; GPU/CPU/RAM; error rate; end-to-end policy frequency; control health. | Existing inference service and observability stack.[10] [11] |
| **Task outcome** | Physical/simulation success rate; failures by taxonomy; human interventions; reset time; conditions used. | New fixed task protocol and evidence record. |
| **Product usability** | Time from fresh install to first candidate, first dataset version, first train, first evaluated deployment; support interventions; completion rate. | New quickstart instrumented through job/session events. |

### 6.2 Required result context

Every benchmark, evaluation, and published demo must record: git commit, dirty-tree status, agent/service version, robot profile version, hardware/firmware, OS/driver/CUDA versions, network configuration, source data version, policy/model artifact hash, configuration hash, task/scenario definition, operator, run timestamp, and known deviations. A result with missing context is a diagnostic clue, not marketing evidence.

### 6.3 Release gates

| Gate | Minimum evidence | Blocking condition |
|---|---|---|
| **Capture feature merge** | Automated tests plus a recorded reference session with manifest and quality report. | Required-stream health or restart recovery is untested. |
| **Dataset version release** | Structural + quality gates, immutable manifest, session-aware splits, count-aware stats, reviewer approval. | Any artifact hash, source session, schema/profile, or quality policy is missing. |
| **Training-run completion** | Dataset manifest, config/environment record, logs, checkpoints, and evaluation attempt. | Training loop/correctness test failed or run uses mutable/unidentified input. |
| **Policy release candidate** | Compatibility check and scenario evidence. | Missing rollback artifact, unverifiable model hash, or incompatible profile/schema. |
| **Physical test deployment** | Human operator approval, estop/control path confirmed, recent preflight, supervised task plan. | Any safety/health gate fails. |
| **Public claim** | Reviewed, reproducible benchmark/evaluation artifact and documented limitation. | A number, target, or capability cannot be traced to a result. |

## 7. Success metrics and decision gates

The strategic review’s operating scorecard should be specialised to the golden path. These are decision metrics, not vanity metrics.

| Metric | Week-4 target | Day-60 target | Day-90 target | Decision it informs |
|---|---:|---:|---:|---|
| **Sessions that pass preflight** | Baseline measured | ≥80% of internal attempts | ≥90% after documented setup | Whether onboarding and hardware contract are viable. |
| **Candidates passing data-quality gate** | Baseline measured | ≥70% of intended episodes | ≥80% with reason taxonomy | Whether teleop/capture UX and instrumentation are usable. |
| **Reproducible dataset versions** | 1 internal | ≥3 task iterations | Each released model has one | Whether data provenance is real. |
| **Successful evaluated policy deployments** | 1 supervised internal | ≥3 repeat runs | ≥1 external user run | Whether Data → Train → Serve works as a product. |
| **Median install-to-first-evaluated-policy time** | Baseline measured | Downward trend tracked | Published only if repeatable | Whether the core promise is credible. |
| **Benchmark result coverage** | CI/GPU baseline | Pi + ROS context added | Complete golden-path matrix | Whether technical claims are defensible. |
| **External completion / design-partner evidence** | 0–1 observation | 2 users or one committed pilot | One paid/committed partner | Whether research/open-hardware is the immediate wedge. |

No numerical external-facing target should be set before Phase 0/1 measurements establish a baseline. In particular, the historical 10-minute first-policy ambition should remain an internal north star, not a product claim, until it is measured across named hardware, model, dataset size, and task conditions.

## 8. Ownership model

| Role | Accountable area | Cadence |
|---|---|---|
| **Founder / product lead** | Scope discipline, design-partner discovery, public positioning, weekly decision review. | Weekly evidence review and user calls. |
| **Robotics systems engineer** | Supported profile, agent/ROS integration, capture preflight, physical task/evaluation, safety and rollback. | Daily during physical test windows. |
| **Data/ML engineer** | Ingestion correctness, quality policies, immutable versioning, training adapter, evaluation analysis. | Continuous, with every run traceable. |
| **Platform/full-stack engineer** | Control-plane data model, artifact store, API, real console integration, identity/audit surface. | Two-week vertical slices. |
| **Optional developer-relations/research support** | Quickstart, benchmark report, dataset card, user feedback synthesis. | Only after the workflow produces real evidence. |

With a very small team, roles may be combined, but the **golden-path owner** must be named and should have authority to reject adjacent feature requests that threaten the end-to-end delivery date.

## 9. Risks and mitigations

| Risk | Likelihood | Impact | Mitigation and trigger |
|---|---:|---:|---|
| Hardware/camera instability creates poor data | Medium | High | Gate capture with preflight and coverage checks; if pass rate is below target for two weeks, prioritise hardware/instrumentation over UI. |
| Inconsistent or poor task demonstrations fail to train | High | High | Use one task protocol, operator guide, episode review, failure labels, and an evaluation holdout; do not conflate training loss with task success. |
| GPU/dependency friction delays external setup | Medium | High | Pin environment, surface the LeRobot requirement clearly, offer one tested runtime profile, and keep first run local before managed cloud training. |
| Control plane distracts from real robot integration | Medium | High | Deliver vertical slices where every API/UI feature is exercised on a real session; reject dashboard-only tickets. |
| Network loss corrupts/repeats uploads | Medium | Medium | Local staging, content hashes, resumable transfers, idempotency keys, and atomic catalog commits. |
| Premature generic robot support | High | High | Add a new profile only after the reference profile meets success/evidence gates and an external user provides a concrete hardware need. |
| Benchmark work becomes a paper-only branch | Medium | Medium | Use the same artifacts for product release gates, quickstart, and any future paper. |

## 10. Immediate operating instructions

Start tomorrow with tickets 1–4 in the first two-week backlog. In parallel, schedule one fixed physical task session to create the baseline capture artifact and one GPU workstation session to establish the current training/inference baseline. Do not begin a marketplace, new robot adapter, or industrial-protocol expansion during this period. The evidence from this first vertical slice should determine the next implementation choice.

## References

[1]: Supplied document: OhhO Robotics Strategic Review and Recommended Next Steps (15 August 2026).
[2]: ../../AGENTS.md "Supported hardware, ROS topics, teleoperation, and 9-D policy context"
[3]: ../../benchmarks/run_benchmarks.sh "Existing benchmark runner and result location"
[4]: ../../data_engine/schema/constants.py "OmniBot data dimensions, topics, cameras, and ranges"
[5]: ../../lerobot_engine/train.py "Training entry point and current training-loop structure"
[6]: ../../data_engine/ingestion/bag_to_omnibot.py "Dataset materialisation and statistics implementation"
[7]: ../../data_engine/scripts/ingest_dataset.py "Batch ingestion and shared metadata concurrency limitation"
[8]: ../../data_engine/ingestion/sync_topics.py "Current timestamp alignment"
[9]: ../../data_engine/scripts/validate_dataset.py "Existing structural validation"
[10]: ../../packages/vla_serve/vla_serve/inference/server.py "Serving interface and metrics"
[11]: ../../infra/observability/ "Existing observability configuration"
