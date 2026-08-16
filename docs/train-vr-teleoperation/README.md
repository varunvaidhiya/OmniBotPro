# OhhO Train + Pilot: Mixed-Reality Teleoperation Productisation Plan

**Status:** Proposed implementation plan  
**Author:** Manus AI  
**Prepared:** 15 August 2026  
**Scope:** Productise the Train platform and the Quest 3 mixed-reality teleoperation application as one closed, safe **teach → train → evaluate → deploy** loop.

## Executive decision

The right product is **not** “an LLM that drives a robot from Unity.” It is a supervised **Mixed-Reality Teach–Train Studio** in which a human uses Quest 3 to teleoperate and collect high-quality demonstrations, the Train platform turns approved data into evaluated policy candidates, and the existing ROS 2 serving/control stack deploys only a compatible, approved release.

> **Hard safety boundary:** Unity/OpenXR and ROS 2 deterministic controllers own live teleoperation. AI/MCP observes, explains, proposes, and invokes a small set of human-confirmed, high-level operations. It must never be inserted into the 20 Hz arm/base command path or be granted an arbitrary ROS publish tool.[1]

This is a natural extension of the Data Platform plan. **Pilot/VR becomes the highest-quality demonstration source; Train becomes the reproducible learning and release system; Serve remains the controlled runtime.** The product promise for the first supported configuration should be narrow:

> **A trained operator can use a Meta Quest 3 to collect a labelled OmniBot mobile-manipulation demonstration, review its quality, train an identified policy version, evaluate it on a fixed task, and deploy it under supervision with rollback.**

## Why this is a credible starting point

The repository already contains unusually strong ingredients: a Quest 3 Unity app with ROSBridge, Meta XR/OpenXR packages, profile-driven drive and arm control, 20 Hz hand-IK streaming, emergency-stop gesture, head-mounted recording, a robot-side upload bridge, replay/continual-learning primitives, verification checks, hardware-aware benchmarks, ROS execution logging, and established VLA/ONNX deployment paths.[2] [3] [4] [5]

The current gap is product integration and trustworthiness. The Train console and its agent-facing tools deliberately simulate live runs, GPU metrics, exports, and training state in the browser. The VR app records JSONL files without a dataset-release contract; its upload endpoint is unauthenticated and only saves bytes. The learning-engine fine-tuning bridge invokes the standalone training script with incompatible command-line arguments, while replay-to-LeRobot export is explicitly unimplemented.[6] [7] [8] [9] [10]

## Documents in this set

| Document | Purpose | Primary audience |
|---|---|---|
| [01 — Current State and Product Scope](01-current-state-and-product-scope.md) | Separates real capabilities from scaffolding, identifies blockers, and defines the first safe product boundary. | Founder, product lead, robotics lead |
| [02 — Reference Architecture and Safety Model](02-reference-architecture-and-safety-model.md) | Defines the closed-loop technical architecture, AI/MCP guardrails, data contracts, release gates, and reusable external foundations. | Robotics, XR, ML, and platform engineers |
| [03 — Implementation Roadmap and Test Plan](03-implementation-roadmap-and-test-plan.md) | Provides the 90-day sequence, immediate backlog, validation protocols, ownership model, and evidence scorecard. | Delivery owner and contributors |

## The four product lanes

| Lane | User outcome | Existing leverage | First product increment |
|---|---|---|---|
| **Pilot: deterministic MR teleoperation** | The operator drives base/arm and sees camera/status safely in the headset. | Quest 3 Unity app, ROSBridge, profile-driven control, 20 Hz hand-IK, emergency stop.[2] | Make the supported OmniBot profile and control state explicit, observable, and testable. |
| **Teach: demonstration capture** | A session becomes a traceable, quality-scored candidate episode. | Headset JSONL recorder, robot-side bridge, ROS recorder, Data Platform ingestion.[3] [11] | Replace opaque JSONL upload with session manifest, preflight, artifact integrity, and provenance. |
| **Train: policy iteration** | An approved dataset version produces a reproducible, evaluated candidate policy. | Learning loop, existing model trainer, simulators, reward/evaluation primitives, hardware benchmarks.[4] [5] | Turn in-process components into a job/run/policy-release contract and repair broken integration points. |
| **Assist: AI/MCP copilot** | The operator understands robot state, diagnoses faults, and requests high-level actions safely. | Existing product MCP registry plus a candidate ROS 2 diagnostics foundation.[12] [13] | Ship read-only diagnostics first, then human-confirmed capability commands—not actuator control. |

## Immediate product boundaries

### In scope for v1

V1 targets a single physical configuration: OmniBot mecanum base, SO-101 arm, front/wrist/BEV vision contract, Raspberry Pi ROS 2 edge node, GPU workstation, and Quest 3/3S. It supports supervised direct teleoperation, headset-side or robot-side IK as explicitly selected by profile, labelled capture sessions, offline data transfer/recovery, versioned training jobs, fixed evaluation scenarios, policy release approval, controlled deployment, and read-only AI diagnostics.[2] [4]

### Explicitly out of scope

V1 excludes arbitrary robot onboarding, autonomous LLM control, non-supervised policy rollouts, arbitrary ROS publish/parameter tools, automated scene editing during live operation, public marketplace publishing, multi-robot fleet scheduling, and generic cloud training. Those are potential follow-ons after the reference loop has external evidence.

## Non-negotiable rules

| Rule | Product decision |
|---|---|
| **The deterministic path wins** | Command rate, latency, deadman/estop, workspace, joint/velocity limits, and controller state remain inside Unity/ROS control software. |
| **The AI is capability-scoped** | It starts read-only. Any write operation is a named capability with typed parameters, preconditions, policy enforcement, user confirmation, and audit event. |
| **Data provenance follows the demonstration** | A VR session records selected robot profile, calibration, topic schema, app/build version, XR mode, timestamps, network quality, safety events, and artifact hashes. |
| **Training consumes frozen data** | Every run references an immutable dataset version and declares code, environment, model, hyperparameters, and hardware. |
| **Deployment requires evidence** | Model artifact, compatibility, benchmark/evaluation results, safety policy, human approval, and a rollback target are mandatory. |
| **Interface truthfulness matters** | UI cards and agent calls use persisted jobs and artifacts. Simulated charts/actions may remain only in an explicit demo mode. |

## References

[1]: Supplied document: *AI-Native Mixed-Reality Robot Teleoperation with Unity + MCP*.
[2]: ../../vr_app/README.md "Quest 3 VR application, controls, recording, and robot-side integration"
[3]: ../../vr_app/Assets/Scripts/Recording/ProfileDrivenRecorder.cs "Profile-driven JSONL episode recorder"
[4]: ../../learning_engine/ARCHITECTURE.md "Train platform architecture and deployment path"
[5]: ../../learning_engine/loop/post_training_loop.py "Pluggable post-training orchestration loop"
[6]: ../../website/lib/train/training.ts "Client-side simulated Train state"
[7]: ../../website/lib/train/mcp-tools.ts "Representative Train MCP tool handlers"
[8]: ../../robot_ws/src/omnibot_vr/omnibot_vr/vr_recording_bridge.py "VR HTTP upload bridge"
[9]: ../../learning_engine/policies/trainers.py "FineTuneTrainer integration contract"
[10]: ../../learning_engine/data/replay_dataset.py "Unimplemented ReplayDataset LeRobot export"
[11]: ../data-platform/README.md "Existing Data Platform productisation plan"
[12]: https://github.com/LCAS/ros2_mcp "ROS2 MCP project"
[13]: https://github.com/leggedrobotics/unity_ros_teleoperation "Unity ROS Teleoperation project"
