# OhhO Serve: Policy Runtime and Release Platform Plan

**Status:** Proposed implementation plan  
**Author:** Manus AI  
**Prepared:** 15 August 2026  
**Scope:** Productise OmniBot model serving, policy deployment, runtime verification, observability, and rollback as the final **Train → Serve → robot → Data** link.

## Executive decision

**OhhO Serve should not be an API playground that returns plausible action vectors.** It should be the controlled runtime and release system that turns a Train-approved policy artifact into supervised robot behaviour with an explicit compatibility contract, deterministic action gateway, operational evidence, and rollback.

The repository has three related but currently divergent serving paths: the `packages/vla_serve` FastAPI model server, the near-duplicate `vla_engine` FastAPI server, and ROS-native policy nodes for OpenVLA, SmolVLA/ACT/Diffusion, and ONNX RL.[1] [2] [3] The v1 product must define one **canonical policy-release and inference contract** above those adapters, rather than treating each endpoint or launch file as an independent product.

> **Hard safety boundary:** A model server produces a candidate action in a model-specific representation. A separate, deterministic policy gateway validates release identity, robot/profile compatibility, observation freshness, action schema, units, normalisation, limits, mode ownership, and command age before an action can reach a ROS command mux. The HTTP model server, website console, and AI tools never directly actuate the robot.[4] [5]

## The product promise

For the initial OmniBot configuration, a user should be able to select an approved policy release, confirm its compatibility with the connected robot profile, deploy it to the registered GPU runtime, observe readiness and evidence, activate it only in a supervised control mode, monitor live health and latency, and roll back to a known-good release in one controlled operation.

This product relies on the preceding Data, Train, and Pilot plans:

| Upstream product | Required input to Serve | What Serve contributes |
|---|---|---|
| **Data Platform** | Immutable approved dataset versions and their profile/schema provenance. | Maintains the data lineage in the policy release record. |
| **Train** | Real model artifact, run manifest, evaluation results, compatibility declaration, reviewer approval. | Refuses an artifact that lacks complete evidence or a compatible target. |
| **Pilot / VR** | Supervised teleop and autonomous-runtime observations, incident records, rollback needs. | Supplies runtime policy identity, health, intervention, and failure evidence back to Data/Train. |
| **Serve** | An approved release plus a live, verified runtime target. | Delivers a bounded policy output to the existing ROS control and driver safety chain. |

## What is genuinely reusable today

The repository already contains a model-agnostic FastAPI skeleton with optional API-key authentication, token-bucket rate limiting, request-size limit, optional Prometheus instrumentation, and Docker/Compose wiring.[1] [6] It also contains ROS-native policy execution, base/arm command muxes, model runtime diagnostics, hardware-aware benchmarking, and Prometheus/Grafana/Alertmanager infrastructure.[3] [4] [7]

These assets accelerate the build, but they are **components**, not a coherent Serve product. The current FastAPI OpenVLA adapter invokes generic text generation and regex-parses numbers, the container lacks the optional OpenVLA dependencies it advertises, model loads are unconstrained by a registry or artifact digest, and the ROS muxes select modes but do not independently enforce source freshness or policy identity.[1] [5] [8]

## Documents in this set

| Document | Purpose | Primary audience |
|---|---|---|
| [01 — Current State and Product Scope](01-current-state-and-product-scope.md) | Separates working assets from scaffolding and identifies the P0 blockers to a trustworthy Serve product. | Founder, robotics/ML/platform leads |
| [02 — Reference Architecture and Safety Model](02-reference-architecture-and-safety-model.md) | Defines the policy release contract, canonical runtime, action gateway, deployment plane, security model, and observability architecture. | Robotics, ML platform, infrastructure engineers |
| [03 — Implementation Roadmap and Acceptance Plan](03-implementation-roadmap-and-acceptance.md) | Provides the 90-day execution sequence, immediate backlog, verification ladder, runtime test matrix, and evidence scorecard. | Delivery owner and contributors |

## V1 boundary

V1 supports one managed target: the GPU workstation paired with the OmniBot reference robot, `omnibot-quest3-so101@1.0.0`, and selected model/runtime combinations that pass a real smoke test. It begins with a single action-policy family and a single task class—not the full advertised model catalogue. SmolVLA or another adapter should be selected only after the completed Train smoke run determines which artifact can be evaluated end-to-end.

V1 includes release registration, immutable artifact verification, deployment to a named runtime, readiness/health/metrics, a deterministic ROS-side action gateway, exclusive control-mode activation, request/action audit, runtime safety tests, supervised deployment, and rollback. It excludes multitenant public hosting, arbitrary Hugging Face model loading, generic customer APIs, browser-side execution, fleet-wide OTA campaigns, generic auto-scaling, and fully autonomous high-risk behaviours.

## Non-negotiable rules

| Rule | Product decision |
|---|---|
| **Serve a release, not a path** | Users choose a registered `policy_release_id`; raw `model_path` and arbitrary Python class names are not production inputs. |
| **The model is not the controller** | Model output remains a candidate until a deterministic policy gateway validates it and hands it to the existing driver/mux safety chain. |
| **Readiness is separate from liveness** | `/healthz` may report process liveness; `/readyz` must report loaded release, dependency, GPU, contract, and gateway readiness. |
| **No silent fallback** | Demo UI simulation, dummy policy, empty action, or request failure must never masquerade as a live deployment or a valid robot action. |
| **Mode ownership is explicit** | A policy cannot publish when teleop, Nav2, RL, or another policy owns the relevant base/arm channel. |
| **Every action is attributable** | Session, request, model/release, input version, action hash, gateway decision, control mode, and outcome share correlation IDs. |
| **Rollback is a first-class release action** | Each release has a known-good compatible predecessor and a tested, audited rollback path. |

## Recommended first step

Do not start with cloud deployment, WebGPU, new model cards, or an LLM control endpoint. First create the **policy release manifest** and a **single action-adapter smoke test** that proves a frozen Train artifact can be loaded inside an image that actually contains its dependencies, emits an explicit 9-D OmniBot candidate action, and is rejected by the gateway when profile/schema/normalisation/age checks fail. That artifact is the first true Serve evidence.

## References

[1]: ../../packages/vla_serve/README.md "Current packaged VLA Serve contract"
[2]: ../../vla_engine/README.md "Parallel VLA engine"
[3]: ../../robot_ws/src/omnibot_lerobot/omnibot_lerobot/policy_node.py "ROS-native model policy execution"
[4]: ../../robot_ws/src/omnibot_hybrid/omnibot_hybrid/cmd_vel_mux.py "Base command selection"
[5]: ../../robot_ws/src/omnibot_rl/omnibot_rl/arm_cmd_mux.py "Arm command selection"
[6]: ../../docker-compose.yml "VLA Serve container topology"
[7]: ../../infra/observability/prometheus/alerts/omnibot_alerts.yml "Existing control, inference, and hardware alerts"
[8]: https://raw.githubusercontent.com/openvla/openvla/main/vla-scripts/deploy.py "Upstream OpenVLA REST deployment example"
