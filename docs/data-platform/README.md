# OhhO Data Platform: Productisation Plan

**Status:** Proposed implementation plan  
**Author:** Manus AI  
**Prepared:** 15 August 2026  
**Scope:** The practical Data → Train → Serve productisation initiative, with the positioning and benchmark work that must precede or accompany it.

## Executive decision

The strategic review’s first three priorities are **not three independent software products**. They are a sequence of proof-building activities:

| Strategic priority | What it actually is | Required outcome | Relationship to the Data Platform |
|---|---|---|---|
| **1. Narrow the public positioning and active catalogue** | Product-scope and credibility work | The website exposes one precise, supported user outcome and labels incomplete modules honestly. | Defines the promise the data platform must fulfill; it is not a new console. |
| **2. Run and publish benchmark results** | Evidence and reliability work | Versioned results substantiate performance, cost, and reproducibility claims. | Establishes the measurement plane and release gates for the golden path. |
| **3. Productise Data → Train → Serve** | The first actual end-to-end product | An external user completes a documented, safe data-to-deployment loop on supported hardware. | This is the platform build described in the remaining documents. |

The recommended starting product is therefore a **local-first robot data platform**: connect one supported reference robot, record or import demonstrations, validate and curate those episodes, create an immutable dataset version, launch a compatible training run, register the resulting policy, deploy it under a controlled safety envelope, and capture evaluation evidence. The key initial promise is deliberately narrow:

> **On the supported OmniBot mobile-manipulation reference system, a developer can produce a traceable training-ready dataset and a deployable policy from teleoperated demonstrations without manually stitching together ROS bags, scripts, metadata, and model artifacts.**

This is stronger and more credible than claiming generic support for arbitrary robots. The repository already contains the components for the workflow—ROS 2 recording, LeRobot-compatible conversion, model training, VLA serving, synthetic data collection, telemetry, and benchmarks—but they are currently separate tools rather than a durable product workflow.[1] [2] [3] [4]

## Documents in this set

| Document | Purpose | Primary audience |
|---|---|---|
| [01 — Current State and Product Scope](01-current-state-and-product-scope.md) | Establishes exactly what is real in the repository, the leverage to retain, the gaps to close, and the narrow v1 boundary. | Founder and technical lead |
| [02 — Architecture and Feature Plan](02-architecture-and-feature-plan.md) | Specifies the recommended local-first architecture, data contracts, feature releases, security boundaries, and engineering decisions. | Platform and robotics engineers |
| [03 — Implementation Roadmap and Acceptance Plan](03-implementation-roadmap-and-acceptance.md) | Converts the build into a sequenced 90-day plan, backlog, owners, test gates, metrics, and immediate next steps. | Delivery owner and contributors |

## Non-negotiable product principles

| Principle | Decision | Rationale |
|---|---|---|
| **Prove one golden path** | Support the Yahboom mecanum base, SO-101 arm, front/wrist/BEV camera contract, and one repeatable mobile-manipulation task first. | Current code has concrete hardware, ROS topics, and 9-DOF state/action definitions for this configuration; universal robot support now would mask untested assumptions.[1] [5] |
| **Keep the robot usable offline** | Record raw data and show capture health locally; queue sync and cloud-facing operations. | Teleoperation and safety cannot depend on WAN availability or remote service latency. |
| **Treat a dataset as an immutable, attributable asset** | A dataset version requires source episodes, schema version, calibration/configuration, code commit, quality results, and split manifest. | The current writer produces LeRobot files and basic statistics but does not provide a release-level manifest or lineage model.[2] [6] |
| **Never deploy from an unqualified artifact** | A policy must be tied to a dataset version, training run, evaluation report, robot compatibility declaration, and explicit deployment approval. | This makes Train → Serve traceable and prevents “latest checkpoint” deployment. |
| **Benchmark the promise before marketing it** | Measure installation-to-first-policy time, data quality, training, inference, and task success per commit and hardware profile. | The repository includes a benchmark runner and regression checks, but no tracked result artifacts are present.[4] |
| **Make every interface real** | The web Data console and agent-facing tools must read and mutate persisted records, not representative browser-side episode data. | The current Data UI and agent interfaces generate or hold sample episodes in memory; the apparent actions do not change the on-disk dataset.[7] [8] |

## Recommended architecture in one paragraph

The recommended v1 consists of a **robot-side Data Agent** that owns capture sessions, local buffering, provenance collection, and resumable uploads; a **data control plane** that stores projects, episodes, dataset versions, quality decisions, training runs, policy releases, and audit events; an **artifact plane** that stores ROS bag sources, LeRobot Parquet/MP4 materialisations, report files, checkpoints, and evaluation evidence; and adapters that call the existing ingestion, training, serving, benchmark, and observability capabilities. The browser console becomes the workflow controller and reviewer, not the recorder or the source of truth. The resulting architecture preserves the existing local ROS 2 and GPU topology instead of forcing high-bandwidth video or control traffic through a web backend.[1] [2] [3]

## What success means at the end of v1

A successful v1 is not a large dashboard. It is a repeatable, observed workflow with these acceptance conditions:

1. A fresh supported OmniBot installation can start a labelled capture session and save source metadata locally.
2. A completed session can be converted into a LeRobot-format candidate dataset with deterministic validation and a human-visible quality report.
3. A user can approve a named, immutable dataset version whose episode membership, code/configuration, schema, statistics, and train/evaluation split are all recorded.
4. A training run references precisely one dataset version and publishes its checkpoints, metrics, environment, and evaluation result.
5. Only a compatible, evaluated, explicitly approved policy release is deployable; deployment emits a device-side audit record and observable health data.
6. The end-to-end path is benchmarked on documented hardware, with machine-readable results checked into or published from the repository before performance claims are made.[4]

## Explicit exclusions from v1

The following are intentionally excluded until the golden path has external use and measured evidence: arbitrary robot adapters, a multi-tenant marketplace, automatic large-scale dataset labelling, automatic cloud training, multi-site fleet scheduling, industrial VDA 5050/OPC UA support, compliance certification, and fully autonomous policy promotion. The repository may contain related prototypes, but adding these scopes now would recreate the surface-area problem identified in the strategic review.

## References

[1]: ../../AGENTS.md "OmniBot project architecture and hardware/topic contract"
[2]: ../../data_engine/ingestion/bag_to_omnibot.py "ROS 2 bag to LeRobot dataset conversion"
[3]: ../../lerobot_engine/train.py "Policy training entry point"
[4]: ../../benchmarks/run_benchmarks.sh "Benchmark runner and regression workflow"
[5]: ../../data_engine/schema/constants.py "Canonical OmniBot data specifications"
[6]: ../../data_engine/scripts/validate_dataset.py "Current dataset validator"
[7]: ../../website/lib/data/episodes.ts "Browser-side representative Data console episodes"
[8]: ../../website/lib/data/mcp-tools.ts "Representative Data console agent interfaces"
