# 01 — Current State and Product Scope

**Status:** Codebase assessment and v1 boundary  
**Author:** Manus AI  
**Prepared:** 15 August 2026

## 1. Conclusion

OhhO has enough real robotics and machine-learning substrate to build a compelling **data-to-policy workflow**, but it does **not** yet have a complete data platform. The present repository is best described as an advanced collection of interoperating components: data is captured or converted locally, materialised as a LeRobot-style dataset, handed to a training script, then consumed by a ROS 2 policy node or a small inference service. The missing work is the workflow, provenance, quality control, release management, and real control-plane persistence that turn those components into a product.[1] [2] [3]

This distinction matters. The recommended work is not to replace ROS 2, LeRobot, W&B, or the current GPU-serving path. It is to make the boundaries between them explicit, traceable, safe, and usable by an external developer.

## 2. The actual priority sequence

The supplied strategic review prioritises **focus, proof, then productisation**. Its first two priorities—narrowing the active catalogue and publishing benchmarks—are necessary enabling work rather than standalone products. The actual platform initiative begins with priority three: productising **Data → Train → Serve**. Connect remains a prerequisite and should be presented as the entry point, but v1 should not broaden into all 19 named modules.

| Item | Current recommendation | Implementation consequence |
|---|---|---|
| **Connect** | Treat as the supported-robot onboarding and health gate. | A robot profile, connection test, sensor preflight, and capability declaration must exist before data capture. |
| **Data** | Make this the product wedge. | Build real capture sessions, dataset cataloguing, quality review, immutable versions, and artifacts. |
| **Train** | Preserve existing policy choices and GPU execution. | Add reproducible run manifests, dataset-version binding, checkpoint registration, and evaluation evidence. |
| **Serve** | Keep control-local policy execution separate from product control plane. | Add policy-release compatibility gates, deployment records, health checks, rollback, and live observability links. |
| **Bench** | Do not market it as a broad separate product yet. | Use its runner and result schema as mandatory evidence for the golden path. |

## 3. What is already real and reusable

The codebase has significant advantages. The core recommendation is to **compose these assets**, not rebuild them.

| Existing asset | Evidence in the codebase | Advantage for the product | How v1 should use it |
|---|---|---|---|
| **Reference robot and distributed deployment** | Raspberry Pi 5 runs ROS 2; a GPU workstation runs VLA inference; the Yahboom base and SO-101 arm have concrete topic and physical contracts.[1] | The product can be demonstrated on an actual mobile manipulator rather than a browser mock-up. | Ship this as the single supported reference deployment and hardware compatibility matrix. |
| **Real teleoperation recording** | `teleop_recorder_node` records leader-arm plus base actions; the stack includes Xbox and VR entry points.[1] | Capture can start from real operator behaviour, which is the correct wedge for imitation learning. | Wrap recording in a labelled, preflighted capture session rather than exposing raw node launches. |
| **LeRobot-oriented materialisation** | The converter writes episode Parquet, H.264 MP4, task metadata, episode metadata, and statistics using a 9-DOF mobile-manipulation state/action contract.[2] | The product can interoperate with a recognised training ecosystem rather than inventing a proprietary format. | Retain LeRobot as the canonical materialised dataset format; add a release manifest around it. |
| **Multimodal timestamp synchronisation** | `TopicSynchronizer` aligns streams to a target sampling rate with a configurable tolerance.[4] | The pipeline has a real starting point for producing aligned state, action, and image samples. | Make synchronisation coverage and drift first-class quality metrics. |
| **Dataset structural validator** | The existing validator checks required metadata, episode declarations, Parquet columns/row counts, video presence, and total frame consistency.[5] | Basic corrupt/incomplete dataset rejection already exists. | Preserve it as the first validation stage and add semantic, visual, and policy-safety stages. |
| **Synthetic episode generation** | Isaac Sim collection randomises a scene, records ROS bags, and hands them to the existing ingestion flow.[6] | Sim data can improve iteration speed and support sim/real mix experiments. | Register simulation episodes with a clear source label; do not silently mix them with real data. |
| **Multiple training paths** | The trainer exposes SmolVLA, ACT, Diffusion, and OpenVLA options and optional W&B logging.[3] | Users can compare policy families without switching platforms. | Register a training-run adapter that invokes the existing entry point after hardening it. |
| **Serving and monitoring substrate** | The repository includes a standalone inference service, ROS nodes, Prometheus/Grafana/Loki/Tempo configuration, and a metrics bridge.[1] [7] | Policy lifecycle and evidence collection can be built on an existing runtime and observability stack. | Add policy release/deployment metadata and deep links, not a duplicate telemetry system. |
| **Benchmark harness** | The benchmark runner targets CI, Pi 5, GPU, and ROS contexts and checks for regressions against results.[8] | Product claims can be connected to machine-readable, repeatable evidence. | Use this runner as a release gate for the golden path. |
| **Reusable schema package** | `robot_episode_dataset` defines configurable state, action, camera, and dataset schema primitives.[9] | The future robot-agnostic layer has a real abstraction rather than a marketing-only claim. | Keep the OmniBot profile immutable for v1 and introduce one named schema/profile interface for later adapters. |

## 4. Material gaps and risks

The following are concrete findings from the repository. They should be treated as work items, not as reasons to abandon the approach.

| Severity | Finding | Evidence | Product risk | Required remediation before external golden-path testing |
|---|---|---|---|---|
| **P0** | The Data web console generates browser-side representative episode/state data and exports a CSV blob despite its `exportParquet` name. | The episode catalog uses generated states and the browser export uses `text/csv`.[10] | The UI can imply that real data is being reviewed or exported when no persisted dataset was read. | Replace sample-only flows with a clearly labelled demo mode; make production UI call persisted control-plane APIs. |
| **P0** | The agent-facing Data actions operate on a representative in-memory episode list; mutation, export, and Hub-push responses are not durable actions. | Data tool handlers use `EPISODES` and return synthetic handles/results.[11] | External agents could report a successful data operation without touching a dataset. | Bind each tool to authenticated backend APIs and an audit event; return real job IDs and artifact URIs. |
| **P0** | The training loop has an indentation defect: the epoch-local work is outside the epoch loop, so only one pass is performed and its printed epoch is the final number. | `epoch_loss`, batch iteration, evaluation, and checkpointing are aligned outside `for epoch`.[3] | Reported training duration/metrics and checkpoint cadence cannot be trusted. | Correct control flow; add a two-epoch unit/integration test that asserts two scheduler steps and expected checkpoint creation. |
| **P0** | Dataset writer metadata is mutable and has no dataset-level release manifest. | The writer appends to `info.json`, JSONL files, and cumulative statistics in place.[2] | A training run cannot reliably prove exactly which episode set, calibration, commit, or validation result it used. | Introduce immutable dataset versions and a canonical manifest; never train from a mutable “current” directory. |
| **P1** | Dataset statistics are merged by episode count rather than frame count. | The statistical merge assigns `n_new = 1` per episode, regardless of its length.[2] | Normalisation statistics can be materially biased when episodes have different lengths. | Replace with count-aware running moments (per sample, per feature); cover mixed-length cases in tests. |
| **P1** | Missing camera data is silently replaced with black frames. | The writer appends zero-valued images when front or wrist frames are absent.[2] | A visually invalid episode can look structurally valid and poison a training run. | Record per-stream missingness; reject or quarantine episodes beyond a policy threshold. |
| **P1** | Synchronisation has only a basic happy-path test. | There is one synthetic synchroniser test; semantic capture quality is not validated.[4] [12] | Drift, duplicate frames, stalls, schema mismatch, calibration mismatch, and safety events are not surfaced to users. | Add deterministic unit tests and a capture-quality report with hard release thresholds. |
| **P1** | Batch ingestion warns that concurrent workers conflict on shared metadata. | The ingestion wrapper documents `--workers 1` as safe because of metadata write conflicts.[13] | Scaling capture processing risks corrupt metadata or duplicate indices. | Use a per-job staging area plus a single transactional catalog/commit step. |
| **P1** | No tracked benchmark evidence is present. | The runner reports no-results state when `benchmarks/results/*.json` is absent; the tracked results inventory is empty.[8] | Performance and time-to-policy claims cannot be substantiated. | Establish baseline reports on the supported Pi, GPU workstation, and ROS deployment before claims. |
| **P2** | Training hard-stops without the real LeRobot dependency even though a lightweight loader fallback is imported. | The main entry point returns when `LEROBOT_AVAILABLE` is false.[3] | Local setup can fail after users believe a fallback is supported. | Either remove the fallback implication or make an explicitly supported minimal training/evaluation path. |

## 5. Product boundary for v1

### 5.1 Supported environment

V1 supports exactly one declared robot profile: **OmniBot mobile manipulator with Yahboom mecanum base, SO-101 arm, front/wrist/BEV camera contract, Raspberry Pi 5 ROS 2 edge node, and GPU workstation for model execution**. The canonical policy state and action space is 9-D—six arm dimensions plus planar mecanum velocity commands.[1] [14]

The reference task should be one repeatable, low-risk mobile-manipulation task, such as *pick a coloured cube and place it in a marked bin*. It must be executable in simulation and on the physical reference robot with a defined success detector. This gives the product a measurable outcome without falsely implying generic scene understanding, universal hardware support, or unsupervised autonomy.

### 5.2 In-scope user workflow

| Stage | User outcome | Product capability |
|---|---|---|
| **Connect and preflight** | The user knows that the selected robot profile, ROS topics, camera streams, calibration IDs, storage, and safety state are fit for recording. | Robot registration, capability profile, diagnostic probes, capture readiness report, and explicit operator acknowledgement. |
| **Capture** | The user creates an unambiguous session with task text and data-source declaration. | Local recording controller, session metadata, event markers, storage accounting, pause/abort, and resumable upload queue. |
| **Ingest and validate** | Raw sessions become reviewable candidate episodes with reproducible quality results. | Staged conversion, structural validation, synchronisation/missingness checks, semantic labels, thumbnails, and quarantine reason. |
| **Curate and version** | The user selects kept episodes and creates a frozen dataset version. | Review state, dataset membership, split manifest, quality gate, statistics, release notes, and immutable artifact identifiers. |
| **Train** | The user starts or observes a compatible training run and can reproduce its inputs. | Dataset-version selection, model configuration, runtime profile, job state, checkpoints, metrics, and run manifest. |
| **Evaluate and release** | The user can decide whether a model is safe enough for supervised test deployment. | Scenario result capture, success/failure taxonomy, regression comparison, compatibility declaration, and explicit approval. |
| **Deploy and observe** | The user deploys or rolls back a named policy release to a named robot and sees its status. | Deployment record, health/latency link, policy enable state, rollback, and event/audit trail. |

### 5.3 Out of scope

V1 must not promise general robot onboarding, arbitrary camera configurations, automated semantic labelling, cloud-only capture, non-supervised physical rollout, managed multi-tenant compute, a public dataset marketplace, or industrial protocol integration. Those are potential later products; they are not dependencies for proving the Data → Train → Serve loop.

## 6. Primary personas and jobs to be done

| Persona | Job to be done | Evidence of success |
|---|---|---|
| **Robotics researcher / lab engineer** | “I want to turn repeated teleoperation attempts into a model-ready, auditable dataset without manually reconciling bags, videos, and actions.” | They complete a capture-to-version workflow unaided and can reproduce it from the manifest. |
| **Embodied-AI developer** | “I want to train and compare a policy from a known dataset, then know which checkpoint is safe to try on the robot.” | They can choose a model, inspect training/evaluation data, and deploy a named release with rollback. |
| **OhhO operator / design partner** | “I need confidence that a demonstrated task is real, measured, and recoverable.” | They can inspect dataset origin, test evidence, telemetry, and failure reports for a policy release. |

## References

[1]: ../../AGENTS.md "Architecture, hardware topology, ROS topics, recording, and observability"
[2]: ../../data_engine/ingestion/bag_to_omnibot.py "Bag conversion, metadata, video writing, and statistics"
[3]: ../../lerobot_engine/train.py "Training entry point, model registry, LeRobot dependency, and training-loop control flow"
[4]: ../../data_engine/ingestion/sync_topics.py "TopicSynchronizer implementation"
[5]: ../../data_engine/scripts/validate_dataset.py "Structural dataset validation"
[6]: ../../data_engine/isaac_sim/collect_episodes.py "Isaac Sim episode collection"
[7]: ../../packages/vla_serve/vla_serve/inference/server.py "Standalone VLA serving API"
[8]: ../../benchmarks/run_benchmarks.sh "Machine-aware benchmark execution and missing-results handling"
[9]: ../../packages/robot_episode_dataset/robot_episode_dataset/schema.py "Configurable robot episode schema"
[10]: ../../website/lib/data/episodes.ts "Client-side generated episode catalog and CSV browser export"
[11]: ../../website/lib/data/mcp-tools.ts "Representative agent-facing Data interface"
[12]: ../../data_engine/tests/test_ingestion.py "Current synchronisation test coverage"
[13]: ../../data_engine/scripts/ingest_dataset.py "Batch ingestion concurrency limitation"
[14]: ../../data_engine/schema/constants.py "9-D mobile manipulation state/action data contract"
