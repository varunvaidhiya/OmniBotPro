# 01 — Current State and Product Scope

**Status:** Codebase assessment and v1 boundary  
**Author:** Manus AI  
**Prepared:** 15 August 2026

## 1. Assessment conclusion

OmniBotPro contains enough infrastructure to build a credible **robot policy serving platform**, but it does not currently contain a trustworthy public Serve product. The current implementation demonstrates three useful building blocks: a packaged HTTP inference server, ROS-native model inference nodes, and a web console/agent interface. They are not yet joined by the artefact identity, action semantics, deployment control, and runtime safety contract that converts a model checkpoint into safe robot behaviour.

The most important correction is conceptual. **Serve is not a generic VLA API that accepts a free-form model path and returns a vector.** In robotics, action values are inseparable from robot morphology, coordinate frames, joint ordering, units, normalisation statistics, camera preprocessing, control frequency, and safety policy. A response that is syntactically a numeric vector can still be physically incorrect or dangerous. The platform must therefore serve an approved, compatible **policy release**, not anonymous model output.[1] [2]

## 2. Current capability map

| Area | What is real now | What is representative, incomplete, or unsafe to claim | Product implication |
|---|---|---|---|
| **Packaged HTTP server** | `packages/vla_serve` provides FastAPI `/health`, `/load_model`, and `/predict`; it has optional API-key auth, in-process per-key rate limiting, body-size limit, configurable CORS, and optional Prometheus instrumentation.[3] | It maintains one global model in process, has no release registry or digest verification, accepts arbitrary `model_path`, has no model-load job/locking protocol, no readiness distinction, no input/action compatibility contract, and no policy gateway. | Retain the server skeleton but position it behind a managed deployment agent and policy release registry. |
| **OpenVLA adapter** | It detects CUDA, uses Hugging Face `AutoProcessor` and `AutoModelForVision2Seq`, supports optional 4-bit load, and returns latency/raw output.[4] | It calls generic `generate()` then regex-parses numeric strings, rather than using OpenVLA’s action-specific `predict_action()` flow and dataset-statistics unnormalisation key. Action width, semantics, ordering, normalisation, units, and bounds are not validated.[2] [4] | This is the highest-risk technical P0. Replace the adapter before any physical action integration. |
| **Serve container / Compose** | Compose reserves an NVIDIA GPU, mounts Hugging Face cache, passes model/auth/rate-limit environment variables, exposes port 8000, restarts unless stopped, and checks `/health`.[5] | The referenced `Dockerfile.vla` installs only the lightweight server dependencies; OpenVLA dependencies are commented out. The health check treats an unloaded server as healthy, and configuration lacks model registry/artifact mount/identity. | The advertised one-command OpenVLA path cannot be relied on until image and health semantics are corrected. |
| **Parallel `vla_engine`** | A nearly identical FastAPI OpenVLA server exists with similar endpoints, auth, rate limiting, CORS, and image decoding.[6] | It duplicates package/runtime logic and can drift in bug fixes, observability, dependencies, API semantics, and release behaviour. | Choose one canonical server package and deprecate/wrap the other. |
| **ROS-native multi-backend policy node** | `policy_node` loads registered policy adapters, subscribes to camera, arm-state, odometry, task, and enable topics, emits 9-D arm/base outputs at configured rate, clips base commands, and can emit diagnostics.[7] | Loading failure quietly substitutes a zero-action dummy adapter; arm outputs are published without an explicit local action-width/joint-bound/profile contract; camera/state freshness and control-mode ownership are not enforced inside the node. | This is the right place to evolve into a ROS policy gateway, but failures must be visible and fail closed rather than silently imitate a usable runtime. |
| **Base command mux** | `cmd_vel_mux` selects Nav2, VLA, teleop, or RL based on an explicit mode and republishes only the active source.[8] | It is a selection switch, not a safety gateway: it has no authenticated source, lease/owner identity, command timestamp/freshness watchdog, mode-transition zeroing, policy release identity, or incident/audit record. | Preserve the mux but add a dedicated policy input/gateway and watchdog before physical policy activation. |
| **Arm command mux** | `arm_cmd_mux` selects policy or RL arm commands and publishes active mode feedback.[9] | It transparently passes the selected input; it has no action schema/limit freshness validation or output zero/hold policy on source loss/mode change. | Add arm action validation and safe source-loss semantics before autonomous use. |
| **Direct ROS OpenVLA path** | The `omnibot_vla` node supports GPU OpenVLA, image/prompt input, VLA output to `/cmd_vel/vla`, and defined diagnostics/launch path.[10] | It is a separate base-focused inference path from the generic FastAPI package and multi-backend policy node. It is not governed by a common release record. | Fold this adapter under the same release/gateway/observability contract, or make it an explicitly separate legacy/experimental path. |
| **RL deployment path** | RL inference nodes execute ONNX policies at 20 Hz with specified observations and local base/arm output constraints, routed through the same command mux family.[11] | Its artefact lifecycle, deployment ownership, and rollback are not unified with VLA Serve. | Use RL as the first low-latency benchmark adapter for the Serve release contract; do not confuse it with VLA API serving. |
| **Web console** | The console renders a polished configuration, real `/health` probe, generated client snippets, explicit source labels in request logs, and a demo badge.[12] | It is explicitly client-side simulation; “load model” is a timeout, metrics are synthetic, real-server failure may fall back to deterministic simulation, and the model catalogue is wider than the packaged adapters. | Demo mode is useful, but production console actions must bind to persisted deployments/runs and must never silently fall back. |
| **Agent/MCP interface** | Read-only model/GPU/config estimators are available to the product agent.[13] | `serve.predict`, `serve.deploy`, and `serve.stop` return representative/simulated success without a runtime integration. Model/GPU availability is static metadata, not live inventory. | Restrict the initial agent surface to evidence read-only operations; hide write verbs until a capability broker executes real, audited deployments. |
| **Observability** | Prometheus/Grafana/Loki/Tempo infrastructure, request/inference histograms, ROS diagnostics, control/VLA/GPU alerts, and a metric bridge already exist.[14] | Alerts do not prove action contract validity, stale input, action rejection, release drift, model load state, policy/mux ownership, command age, auth misuse, or rollback health. | Retain the stack, then add Serve release/gateway metrics and runbook-linked alerts. |
| **Tests** | Package tests exercise decoding, Pydantic schemas, abstract-model behaviour, fake-model endpoint responses, basic auth, and basic unloaded model response.[15] | There is no real model/container smoke test, upstream action-semantic test, release compatibility test, simultaneous inference/load test, action-gateway test, ROS mux watchdog test, or hardware-in-loop deployment evidence. | Treat current tests as basic API coverage, not proof of deployed robot policy safety. |

## 3. Advantages to retain

### 3.1 The repository already separates model inference from motor drivers

The ROS architecture routes policy outputs through base and arm muxes before they reach the Yahboom and Feetech drivers. That is materially better than a web server publishing directly to hardware. The next step is to make the missing gateway and ownership rules explicit, not to bypass existing ROS pathways for a faster product demo.[7] [8] [9]

### 3.2 There is a legitimate multi-runtime future

The project already supports conceptually different runtime classes: low-rate OpenVLA, multi-modal SmolVLA-style policy nodes, and low-latency ONNX RL policies. The right Serve design can normalize their **release, compatibility, deployment, metrics, and rollback** lifecycle while leaving model-specific inference behind adapters. This avoids falsely forcing every policy into one REST request shape.

### 3.3 Existing operational tools are valuable

GPU reservation in Compose, container restart policy, cache mounting, ROS diagnostics, and Prometheus/Grafana/Alertmanager provide a useful foundation. It is cheaper and less risky to extend these with proper deployment/release semantics than to introduce a second operations stack.[5] [14]

### 3.4 The client console can become the real operational interface

The website already understands model configurations and can report whether a request was server- or simulator-sourced. Its current demo mechanics should become a clearly separated **sandbox**, while a new production view consumes the same deployment/release records as the agent and runtime. This preserves visual investment without overstating functionality.[12]

## 4. P0 blockers

| Priority | Blocker | Why it blocks a real Serve product | Required correction |
|---|---|---|---|
| **P0** | OpenVLA output is obtained by free-text generation and regex numeric extraction. | It discards the model’s intended action interface and does not bind action unnormalisation to the train dataset. Numeric tokens are not evidence of valid robot action. | Implement `OpenVLAAdapter.predict_candidate()` with the upstream action API; require a release-declared normalisation/unnormalisation mapping and validate final action contract. |
| **P0** | The packaged Serve image does not install the optional OpenVLA dependencies it configures by default. | A `docker compose up vla` instance cannot be assumed to load the advertised default model. | Build versioned runtime images per validated adapter; add GPU/model-load smoke tests and attach their results to the release. |
| **P0** | No policy release registry, immutable artifact verification, or target compatibility check exists. | Arbitrary model paths/classes can be loaded without train lineage, digest, robot schema, calibration, or rollback. | Create a release manifest and registry; deployment accepts only an approved `policy_release_id`. |
| **P0** | HTTP `/predict` returns unconstrained `Dict[str, Any>` action output. | The consumer cannot reliably determine width, type, units, frame, normalisation, time, model version, or safety status. | Replace with typed candidate-action envelope and reject/record malformed output. |
| **P0** | ROS policy/mux path lacks independent source freshness and explicit action-gateway validation. | A selected but stale, mismatched, or invalid source may be forwarded/handled without a dedicated safety decision. | Insert a deterministic gateway that validates action, timestamps, mode owner, release, and limits; outputs safe command/hold/zero only. |
| **P0** | UI and agent deployment verbs simulate success. | Users can incorrectly believe a model is loaded, deployed, stopped, or inferred. | Hide/label demo functions; replace write verbs with real signed deployment tasks and receipts. |
| **P0** | Authentication is optional, single-key, and lacks identities/roles; CORS defaults to `*` while allowing credentials. | A deployment or prediction endpoint is not safe to expose beyond a trusted development network. | Use local-only binding or mutually authenticated private networking in v1; add service identity, scoped tokens, TLS/tunnel, deny-by-default CORS, and audit. |

## 5. P1 gaps

| Gap | Product consequence | v1 design response |
|---|---|---|
| Duplicate FastAPI implementations | Security/bug fixes and API semantics may drift. | Declare `packages/vla_serve` canonical; migrate reusable TensorRT/adapter code deliberately; deprecate duplicate entry point. |
| One global model and `/load_model` mutation | Inference races, response/model ambiguity, and disruptive loads are possible. | One immutable release per runtime instance. Deploy a new revision alongside; warm/verify; atomically route or restart under mode-disabled state. |
| Liveness only | `status: ok, model_loaded: false` satisfies Compose health despite being unable to infer. | Separate `/livez`, `/readyz`, `/release`, `/metrics`; deploy orchestration uses readiness, not liveness. |
| Unbounded request semantics | Image, task, preprocessing, and configuration could be incompatible across robot/model pairs. | Use typed observation references/encoded payload with size/schema/model-profile checks; expose selected profile/preprocessor in response. |
| Missing policy action audit | Cannot diagnose whether a model, gateway, mode, sensor, or driver caused an incident. | Add correlation ID from runtime request through gateway/mux/driver/episode record. |
| Static VRAM/latency catalogue | Browser estimates can be confused with live capacity/performance. | Query node/GPU/runtime inventory and benchmark evidence; label estimates as planning only. |
| Incomplete action safety case | VLA and policy node arm output lacks a demonstrated final validation chain. | Have gateway convert/reject candidate actions before mux; retain driver constraints and physical e-stop. |

## 6. V1 product boundary

### 6.1 Reference deployment

The first supported runtime is a **single GPU desktop policy runtime** connected to the Pi-hosted ROS 2 graph. One model family is selected from real Train output only after it passes the release smoke test. The initial task is the same bounded mobile-manipulation task used by the Pilot/Train reference flow, with an operator physically present and separate emergency-stop procedure.

### 6.2 In scope

V1 includes a policy release manifest, target compatibility preflight, immutable adapter image, one registered runtime, action gateway, exclusive mode lease, controlled model activation, runtime health/readiness, release and action observability, supervised deployment, one-click rollback to known-good release, and real production console/agent read views.

### 6.3 Explicitly deferred

Do not offer arbitrary Hugging Face model paths, arbitrary Python adapter imports, public inbound prediction API, multiple tenants, self-service cloud GPU selection, WebGPU/Transformers browser runtime, auto-scaling, fleet-wide OTA, broad model catalogues, autonomous LLM task execution, or remote physical operation in V1.

## References

[1]: https://raw.githubusercontent.com/openvla/openvla/main/vla-scripts/deploy.py "Upstream OpenVLA action inference example"
[2]: ../../packages/vla_serve/vla_serve/models/openvla.py "Current OpenVLA adapter"
[3]: ../../packages/vla_serve/vla_serve/inference/server.py "Current FastAPI Serve runtime"
[4]: ../../packages/vla_serve/vla_serve/inference/schema.py "Current inference API schema"
[5]: ../../docker-compose.yml "Compose VLA deployment"
[6]: ../../vla_engine/inference/server.py "Parallel VLA engine server"
[7]: ../../robot_ws/src/omnibot_lerobot/omnibot_lerobot/policy_node.py "ROS policy inference node"
[8]: ../../robot_ws/src/omnibot_hybrid/omnibot_hybrid/cmd_vel_mux.py "Base velocity mux"
[9]: ../../robot_ws/src/omnibot_rl/omnibot_rl/arm_cmd_mux.py "Arm command mux"
[10]: ../../robot_ws/src/omnibot_vla/README.md "OpenVLA ROS node"
[11]: ../../robot_ws/src/omnibot_rl/omnibot_rl/rl_nav_node.py "RL navigation inference path"
[12]: ../../website/components/serve/ServeConsole.tsx "Serve console demonstration implementation"
[13]: ../../website/lib/serve/mcp-tools.ts "Serve agent handlers"
[14]: ../../infra/observability/prometheus/alerts/omnibot_alerts.yml "Operational alert rules"
[15]: ../../packages/vla_serve/tests/test_vla_serve.py "Current package test coverage"
