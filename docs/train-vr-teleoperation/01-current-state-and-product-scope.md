# 01 — Current State and Product Scope

**Status:** Codebase assessment and v1 boundary  
**Author:** Manus AI  
**Prepared:** 15 August 2026

## 1. Assessment conclusion

The repository already has a meaningful **MR teleoperation prototype** and a meaningful **learning-system architecture**, but neither is yet a product-grade Train platform. The VR side is farther along in direct human control than the Train console is in real training orchestration. The strongest near-term product is therefore not a generic “AI teleoperation platform”; it is a traceable **teleoperation-to-policy iteration workflow** on the single OmniBot reference configuration.

The architectural principle in the supplied design document is correct and should be retained exactly: the real-time control loop is Quest/Unity → ROS 2 → hardware, while the AI/MCP plane operates separately on state, diagnostics, and bounded high-level commands. Any plan that moves an LLM into the 20 Hz command stream is a regression in safety and diagnosability.

## 2. Current capability map

| Area | What is real now | What is representative, incomplete, or unsafe to claim | Strategic implication |
|---|---|---|---|
| **Quest 3 MR client** | A Unity 6.0.5 Quest app with OpenXR, Meta XR packages, passthrough, world-space UI, garage/profile selection, ROSBridge connection, camera feeds, controller base drive, and hand tracking exists.[1] [2] | The project documentation refers to older package versions than the manifest actually contains; its compatibility matrix has not been verified in automated integration tests. | Preserve the working app and remove configuration ambiguity before importing another large Unity project. |
| **Direct base teleoperation** | The profile-driven `TeleopController` reads Quest controller input continuously and dispatches to a drive scheme through ROSBridge.[3] | There is no documented command watchdog, connection-loss test, or end-to-end latency evidence for the Quest-to-actuator path. | Treat direct drive as a safety-critical integration target, not merely an interaction feature. |
| **Hand/arm teleoperation** | The 6-DOF scheme streams either local-IK joint targets or robot-side Cartesian targets at 20 Hz, clamps configured joint limits, and gates arm motion behind explicit enable state.[4] | Headset-side `ArmIKSolver` is SO-101 geometry even when the surrounding profile abstraction implies broader support; collision-aware robot-side IK is only meaningful if the target ROS/MoveIt Servo path is configured and tested. | Support OmniBot/SO-101 only in v1; profile-driven breadth should not be marketed as validated cross-robot control. |
| **Emergency/enable controls** | Both grips implement e-stop input; arm enable requires a held thumbs-up gesture; `StopTeleop()` commands zero/disarm through selected schemes.[1] [3] | Gesture recognition, e-stop propagation, loss of hand tracking, ROSBridge loss, and recovery are not covered by a documented test matrix. | Convert safety behaviours into named requirements with hardware-in-loop evidence. |
| **VR data capture** | `ProfileDrivenRecorder` samples cached arm/base/odometry observations and command topics at 30 Hz into headset-local JSONL, and the app can upload it to the robot-side bridge.[5] | It does not capture camera frames, profile/schema/calibration/build provenance, data-quality metrics, safety events, or content hashes. Callback exceptions are swallowed. | The recorder is a useful prototype but is not yet a training-ready source of truth. |
| **Robot-side VR bridge** | A ROS 2 node provides health/upload endpoints, accepts JSONL, stores it locally, emits recording signals, and mirrors selected ROS state to `/vr/obs`.[6] | Upload uses an unauthenticated HTTP listener on all interfaces, performs no schema validation, does not authenticate robot/project identity, and has no integrity/idempotency/audit contract. | Do not expose this endpoint beyond a trusted development network; replace it with a session-aware ingestion handshake. |
| **Learning/post-training engine** | The learning engine has a pluggable collect → reward → evaluate → store → train → evaluate loop, replay data structures, hardware profiles, benchmarking/reporting, and verification primitives.[7] [8] | Several high-value paths remain architecture-level: replay-to-LeRobot export raises `NotImplementedError`; policy version management is a roadmap item; some heavy training delegates are external. | It is a strong orchestration substrate, but it needs a real run registry and working adapters before it can power a user-facing Train product. |
| **Fine-tuning hand-off** | `lerobot_engine/train.py` accepts explicit model, dataset path, checkpoint, output directory, epochs, and W&B configuration.[9] | `FineTuneTrainer` calls it with `--dataset`, `--policy`, and `--steps`, which are not parameters of that script.[10] | This integration is a P0 blocker: the Train platform cannot claim incremental SmolVLA fine-tuning through the learning engine until fixed and tested. |
| **Policy safety verification** | `InferenceVerifier` samples plans, checks velocity/joint/reachability constraints, and falls back to a zero policy when no candidate is feasible.[11] | It is not a complete physical safety case: it does not replace hardware estop, collision checking, controller watchdogs, perception confidence, or operational procedures. | Use it as a policy-release/runtime defence-in-depth layer, never as a reason to remove deterministic safeguards. |
| **Train web and agent surfaces** | The website presents a coherent configuration, curve, GPU, verification, and export user experience; the MCP registry includes Train tools.[12] [13] | Both are explicitly browser-side/reprsentative simulations. Start/export/stop/sweep tools return success without starting a job, materialising a checkpoint, or deploying a model. | This is a P0 product-trust issue. Replace or label simulation before permitting external users. |
| **AI/MCP foundation** | The VR manifest already includes a Unity MCP package; the codebase has website-side tool registration, and the external ROS 2 MCP project provides diagnostics/introspection ideas.[2] [14] | No audited OmniBot Robotics MCP server or policy-enforced action broker is demonstrated. | Build a narrow, read-only diagnostics capability first; do not connect a generic MCP server directly to actuator topics. |

## 3. Advantages to retain

### 3.1 The control path already respects the right separation

The existing VR control path is deterministic and direct: Quest controller/hand input is processed in Unity and forwarded to profile-specific ROS topics. It does not call an LLM per hand movement. The 20 Hz manipulation path, local joint clamping, optional robot-side target mode, arm enable state, and existing ROS muxing are the correct foundation for a teleoperation product.[3] [4]

### 3.2 Training is broader than a single fine-tune script

The Train substrate can collect teleoperation, simulation, and real execution episodes; attach reward terms and evaluation reports; prioritize replay by outcome; invoke trainers; evaluate updated policies; and report metrics. The hardware abstraction supports the existing Pi/workstation deployment as well as Jetson, workstation-only, and Apple development profiles.[7] [8] This is valuable because it allows the platform to become a **policy-iteration system**, not merely a one-click wrapper around model fine-tuning.

### 3.3 The data loop can observe executed behaviour, not just requested behaviour

The learning engine’s ROS execution logger is designed to observe post-mux base and arm commands, state, images, estop, and mission status. That distinction is important: recordings should be able to distinguish what the operator requested from what the deterministic controller actually executed.[7] The Train and Pilot product should preserve both views in its session record.

### 3.4 The repository already has deployment and evidence primitives

Trainers are intended to output ONNX policy artifacts for existing RL nodes or SmolVLA checkpoints for existing policy nodes. The project also contains machine-aware benchmarks and observability foundations. The product needs a release/evidence contract around these primitives rather than a second deployment stack.[7] [11]

## 4. P0 and P1 blockers

| Priority | Blocker | Why it blocks productisation | Required correction |
|---|---|---|---|
| **P0** | Train UI and Train MCP tools simulate execution, metrics, and deployment success. | A user can reasonably infer that GPU training or export occurred when it did not. | Replace handlers with persisted job APIs, or make all simulated controls unmistakably demo-only and non-actionable. |
| **P0** | `FineTuneTrainer` calls the standalone trainer with incompatible arguments. | The claimed learning-engine-to-SmolVLA loop is nonfunctional as written. | Create one typed adapter contract, execute a smoke test on a small known dataset, and register real checkpoints/results. |
| **P0** | `ReplayDataset.export_lerobot()` is unimplemented. | The replay/continual-learning store cannot become a VLA fine-tuning dataset through its advertised path. | Either implement a validated export adapter or make Data Platform LeRobot materialisations the only supported VLA input and remove the false path. |
| **P0** | VR upload endpoint accepts unauthenticated JSONL over HTTP and writes it directly. | It permits untrusted/corrupt data injection on a robot-side machine and offers no traceability. | Require authenticated sessions, TLS/tunnelled transport, content hash, size/type/schema checks, idempotency key, project/profile binding, and audit logging. |
| **P0** | VR JSONL captures no image data or provenance while VLA training expects defined image/state/action modalities. | A headset recording cannot silently stand in for a full LeRobot multimodal demonstration. | Define it as a control-intent sidecar, or fuse it with ROS-side camera/state capture through one shared session ID. |
| **P1** | Safety behaviour is implemented but not verified as a system. | Individual gesture logic is not evidence that e-stop, connection loss, mode transitions, or runaway-command prevention work on hardware. | Add unit, simulation, and hardware-in-loop safety tests with measured reaction times and recorded evidence. |
| **P1** | Unity project and external reference foundation are on different Unity versions and use different ROS transports. | Blind forking would create difficult package, scene, transport, and input-system conflicts. | Treat external code as a selective reference; build a compatibility spike before copying a subsystem. |
| **P1** | Fine-grained data quality, policy release, and rollback models are missing from Train. | Training loss alone does not establish a deployable policy. | Reuse the Data Platform’s immutable dataset, run manifest, evaluation, and policy-release concepts. |
| **P1** | AI/MCP action policy is not formalized. | A broad ROS MCP tool can become an accidental generic actuator interface. | Build an allowlisted capability broker with human confirmation and separate read/write identities. |

## 5. V1 product boundary

### 5.1 Supported configuration

The first release supports **one Quest 3/3S client and one OmniBot mobile manipulator**: Yahboom mecanum base, SO-101 arm, front/wrist/BEV camera contract, Pi 5 ROS 2 edge process, and GPU workstation. The reference task should be a low-speed, supervised pick-and-place task in a bounded, marked workspace. The user remains present with an accessible physical e-stop and controlled reset procedure.

### 5.2 User journey

| Step | Operator sees | System records |
|---|---|---|
| **1. Connect and preflight** | Robot/profile match, ROS topic freshness, camera availability, battery/arm state, safety state, network condition, and clear workspace confirmation. | Profile version, robot identity, software/calibration/config versions, preflight result, operator acknowledgement. |
| **2. Enter teleop mode** | Selected drive/manipulation mode, speed/workspace limits, arm disabled by default, camera feed, and clear “recording off” state. | Mode transition, limit policy, control owner, connection/session state. |
| **3. Teach demonstration** | Task label, recording status, quality hints, camera/robot feedback, manual pause/abort, and conspicuous e-stop. | A shared session ID; requested commands, executed commands, state, image streams, timestamps, safety events, and XR interaction metadata. |
| **4. Review and release data** | Episode playback, quality result, reason for keep/reject, and linkage to task/profile. | Candidate episode, artifacts, provenance manifest, review/audit event, dataset-version membership. |
| **5. Train/evaluate** | Real queued/running/completed job status, logs, metrics, input dataset version, model config, hardware profile, and evaluation report. | Immutable run manifest, artifacts/checkpoints, benchmark context, policy candidate. |
| **6. Approve/deploy/rollback** | Exact release, compatibility, evaluation evidence, safety limitations, approval prompt, live health, and rollback action. | Policy release record, deployment event, approval, health/incident history, rollback target. |
| **7. Ask the AI** | Read-only diagnosis and evidence-grounded recommendations; named capability requests require confirmation. | Read set, model/agent identity, recommendation, approval, invoked capability, result. |

### 5.3 Defer deliberately

Do not build generic robot profiles, world-space scene editing, autonomous manipulation by language instruction, multi-user remote operations, cloud-hosted real-time teleoperation, public marketplace publication, or broad ROS MCP control in v1. These create a surface area and safety burden far larger than the current proof base supports.

## References

[1]: ../../vr_app/README.md "Quest 3 application architecture, controls, workspace, recording, and bridge"
[2]: ../../vr_app/Packages/manifest.json "Unity 6, Meta XR, OpenXR, WebSocket, and Unity MCP dependencies"
[3]: ../../vr_app/Assets/Scripts/Control/TeleopController.cs "Profile-driven Quest input and ROS control routing"
[4]: ../../vr_app/Assets/Scripts/Control/Manip/HandIK6DofScheme.cs "20 Hz arm teleoperation, profile limits, enable gate, and robot-side IK option"
[5]: ../../vr_app/Assets/Scripts/Recording/ProfileDrivenRecorder.cs "Headset-local JSONL capture implementation"
[6]: ../../robot_ws/src/omnibot_vr/omnibot_vr/vr_recording_bridge.py "ROS recording signals, state mirror, and HTTP upload server"
[7]: ../../learning_engine/ARCHITECTURE.md "Learning architecture, ROS integration, training, verification, and roadmap"
[8]: ../../learning_engine/loop/post_training_loop.py "Training-loop orchestration"
[9]: ../../lerobot_engine/train.py "Standalone VLA training interface"
[10]: ../../learning_engine/policies/trainers.py "Fine-tuning subprocess adapter"
[11]: ../../learning_engine/verification/verifier.py "Inference safety/reachability verification"
[12]: ../../website/components/train/TrainConsole.tsx "Train console interface"
[13]: ../../website/lib/train/mcp-tools.ts "Train MCP tool implementation"
[14]: https://github.com/LCAS/ros2_mcp "ROS2 MCP reference project"
