# OhhO Website — Build Instructions for AI Agents

> **Read this first.** This file is the single source of truth for what every
> OhhO website product, console, component, and doc must include. When you
> build or update any part of the website, follow these instructions exactly.
> **Keep it up to date** as new standards, features, or products land — this is
> the file the maintainer edits to post new requirements upfront.
>
> **Location:** `website/AGENTS.md` — AI agents working in `website/` read this
> automatically. The root `AGENTS.md` covers the ROS 2 workspace; this file
> covers the website/Next.js app, the product catalog, the marketing site, and
> every console.

---

## 0. TL;DR — how to use this file

1. **Before building any product console, component, or doc**, scroll to the
   **Per-product build prompts** section and find the product you're building.
   That section tells you exactly which standards, features, and rules apply.
2. **Before writing any marketing copy**, read **The rules** section — it lists
   what you must NEVER do (no brand names, no hardcoded hardware specs).
3. **Before adding a new protocol/standard**, read **Standards reference** —
   it's the complete list of what OhhO supports, grouped by category. If you're
   adding a new one, add it here first, then wire it into the relevant products.
4. **Before adding a new product**, read **Adding a new product** — it's the
   checklist for shipping a new console end-to-end.
5. **Run `npm run test` before committing.** All tests must pass. If you add a
   new adapter/standard, update the test assertions (e.g. adapter count in
   `lib/mcp/mcp.test.ts`).

---

## 1. Core principles (apply to everything)

| Principle | What it means in practice |
|---|---|
| **Brand-agnostic** | Marketing copy never names a specific robot brand (no Yahboom, Unitree, DJI, SO-101, Feetech, UR5e, TurtleBot, OmniBot). Use robot-class language ("DDS-native humanoid", "MAVLink drone", "mecanum mobile manipulator"). The robot *catalog* (Garage/VR) may list real robots users can pick — but product *positioning* stays brand-free. |
| **Hardware-agnostic** | Never hardcode a hardware spec (no "16 GB VRAM", no "NVIDIA GPU", no "Raspberry Pi", no "Meta Quest 3"). Say "your GPU — we help you size it", "any OpenXR headset", "your onboard PC". The user chooses their hardware; OhhO helps them run on it or recommends better. |
| **Standards-based** | Every product speaks the industry standards the target industry already runs on. Don't invent proprietary protocols when CANopen/OPC UA/VDA 5050 exists. See **Standards reference** for the full list. |
| **Open-source, zero lock-in** | MIT/Apache licensed. Self-host everything. Cloud is optional. Every product page states this. |
| **Robot-agnostic** | The platform works with any ROS 2-compatible robot, plus non-ROS robots via Bridge. Capability-typed commands, not robot-specific SDK calls. |

---

## 2. The rules (non-negotiable)

### NEVER do

- **No brand names in marketing/UI strings.** Not in `lib/products.tsx`, not in
  components, not in `app/*/page.tsx`, not in `docs/products/*.md`, not in
  marketing SVGs, not in the VR manifest. Brand names appear ONLY in: the robot
  catalog (`lib/garage/robot-catalog.ts`, `public/vr/catalog.json`), the skills
  marketplace (`lib/market/skills.ts` — real robot compatibility tags), the
  bridge adapter registry (`lib/bridge/adapters.ts` — real SDK names), and the
  technical install docs (`docs/ohho-os/install.md` — pip extras).
- **No hardcoded hardware specs in marketing.** No "16 GB VRAM", no "NVIDIA
  GPU", no "Raspberry Pi 5", no "Meta Quest 3", no "Quest 3S". Use "your GPU",
  "any OpenXR headset", "your onboard PC", "we help you size it".
- **No "Claude-backed" or "Claude tool-calling" in marketing.** Use
  "LLM-backed, your choice of model" or "LLM tool-calling reasoner". Claude is
  an implementation detail, not a product feature.
- **No "car-type" or vendor-specific config terms.** Use "configuration for
  your base", not "car-type set for your base".
- **No proprietary lock-in language.** Every product page must make clear the
  user can self-host, bring their own models/data, and leave.

### ALWAYS do

- **List every industry standard the product supports** in its highlights,
  features, and specs. If a product speaks CANopen, say so. If it speaks OPC UA,
  say so. Standards are a selling point.
- **Update `lib/products.tsx` first** — it's the single source of truth for the
  product catalog. Both the homepage Products grid and the per-product detail
  pages read from it. Then update `docs/products/<slug>.md` to mirror.
- **Update the adapter registry** (`lib/bridge/adapters.ts`) when adding a new
  protocol adapter. The Bridge console reads `ADAPTERS` dynamically — new
  entries render automatically.
- **Update MCP tool enums** (`lib/bridge/mcp-tools.ts`) when adding adapters —
  the `adapterId` enum arrays must include the new IDs (there are 3 of them).
- **Update test assertions** when counts change. `lib/mcp/mcp.test.ts` asserts
  the adapter count — update it when you add adapters.
- **Regenerate the VR manifest** after changing `lib/products.tsx` — run
  `npm run generate:vr` (regenerates `public/vr/manifest.json` from
  `products.tsx`).
- **Run `npm run test`** before committing. 107+ tests must pass.
- **Use the OhhO brand voice:** confident, technical, concrete, no hype. Every
  product has a one-line tag, a hero paragraph, 5-10 highlights, 3 overview
  paragraphs, 5-10 features, 4 how-it-works steps, 6-10 specs, 4 plan rows, 2-5
  FAQ entries.

---

## 3. Standards reference — the complete list

This is the canonical list of every industry standard OhhO supports. When
building a product, include every standard from this list that applies to that
product's category. When adding a new standard, add it here first, then wire it
into the relevant products.

### Communication protocols (15)

| Standard | What it is | Use case | Products |
|---|---|---|---|
| DDS (Cyclone) | Data Distribution Service — pub/sub middleware | Humanoids, legged robots, native SDK translation | Bridge |
| MAVLink | Micro Air Vehicle Link — drone autopilot protocol | Aerial drones, survey, inspection | Bridge |
| CAN bus / CANopen (CiA 402) | Controller Area Network + CANopen device profile (CiA 402 motion) | AGVs, AMRs, mobile robot motor controllers, embedded bases | Bridge |
| Modbus TCP/RTU | Serial communication protocol for PLC-driven arms | Industrial SCARA / 6-DOF arms | Bridge |
| EtherCAT | Ethernet for Control Automation Technology — real-time servo control | High-performance industrial arms | Bridge |
| OPC UA | Open Platform Communications Unified Architecture — Industry 4.0 | Factory cell integration, MES/SCADA, digital twin communication | Bridge, Twin |
| PROFINET | Process Field Net — real-time industrial Ethernet (Siemens ecosystem) | European / German automotive manufacturing | Bridge |
| EtherNet/IP (CIP) | Common Industrial Protocol over Ethernet (Rockwell ecosystem) | North American manufacturing | Bridge |
| MQTT | Message Queuing Telemetry Transport — lightweight IoT pub/sub | Cloud IoT, fleet telemetry, VDA 5050 transport | Bridge, Connect, Fleet |
| VDA 5050 | German Automotive AGV/AMR fleet communication standard (JSON over MQTT) | Warehouse robotics, WMS integration, master control | Bridge, Fleet |
| ROS-Industrial | Consortium driver ecosystem for industrial arms on ROS | Fanuc, ABB, KUKA, Yaskawa, Universal Robots | Bridge |
| IEC 61131-3 / PLCopen | PLC programming standard (ladder logic, structured text, function blocks) | Factory PLC controller integration | Bridge |
| ROS 2 topics | ROS 2 pub/sub message bus — the lingua franca of the platform | All ROS 2-compatible robots | Frame, Connect, all |
| ROSBridge WebSocket | JSON protocol bridging ROS 2 to WebSocket clients | Browser-based robot control, OhhO Connect | Connect |
| Open-RMF | Open Robotics Middleware Framework — multi-robot fleet coordination | Mixed-vendor fleet traffic management, task allocation | Fleet |

### Safety standards (11)

| Standard | What it covers | Products |
|---|---|---|
| EU Machinery Reg (CE) | Essential health & safety requirements for machinery in the EU | Comply |
| ISO 10218-1/2 | Safety requirements for industrial robots and robot systems | Comply |
| ISO 15066 | Collaborative robot safety — power and force limiting | Comply |
| ISO 13849-1 | Safety-related parts of control systems (Performance Level a-e) | Comply |
| ISO 12100 | Risk assessment — hazard identification and risk estimation | Comply |
| ISO 3691-4 | Safety of industrial trucks — driverless (AGVs/AMRs) | Comply |
| ISO 13482 | Safety requirements for personal care robots | Comply |
| ANSI/RIA R15.06 | US industrial robot safety (harmonized with ISO 10218) | Comply |
| ANSI/ITSDF B56.5 | Safety of guided industrial vehicles (AGVs) — US | Comply |
| IEC 61508 | Functional safety of E/E/PE systems (foundational root standard) | Comply |
| UL / IEC 60204-1 | Electrical equipment of machines | Comply |

### Data & description formats (6)

| Standard | What it is | Products |
|---|---|---|
| LeRobot (Parquet + MP4) | Hugging Face dataset format for imitation learning demonstrations | Data, Train |
| MCAP | Open-source ROS 2-native bag/recording format | Frame, Data |
| ONNX | Open Neural Network Exchange — cross-platform model format | Train, Fleet, Serve |
| URDF | Unified Robot Description Format — XML robot model | Build, Frame |
| SDF | Simulation Description Format — Gazebo-native world/robot description | Build, Frame |
| USD | Universal Scene Description — Pixar/Omniverse 3D scene format | Frame, Twin |

### Simulation engines (3)

| Standard | What it is | Products |
|---|---|---|
| Gazebo Harmonic | Open-source 3D robot simulator (ROS 2-native) | Frame, Twin, Proof |
| Isaac Sim | NVIDIA Isaac Sim — high-fidelity GPU simulator | Frame, Twin, Train |
| MuJoCo | Multi-Joint dynamics with Contact — physics simulator | Train |

---

## 4. Per-product build prompts

> When building or updating a product, find its section below and include every
> listed standard/feature. Each section is a self-contained "build prompt" an AI
> agent can follow end-to-end.

### OhhO Build — "Design any robot. For any industry."

**When building this product, include:**
- Browser-based 3-D robot designer (WebGL, no install)
- Drag-and-drop parts library (bases, drives, arms, grippers, sensors, compute)
- Requirements-driven validation (payload, torque, reach, stability, power, runtime)
- AI design recommendations (plain-language job → complete starting design)
- Sourced bill of materials (supplier links, lead times, alternates)
- **Exports URDF + SDF + USD** (all three robot/scene description standards)
- Exports a deployment profile for OhhO Frame
- Industry templates (warehouse AMRs, lab automation, agriculture, inspection, education)
- Plan-gated: Spark (1 design), Builder (10 designs), Fleet (unlimited + live sourcing), Forge (white-label catalog)
- **Standards to mention:** URDF, SDF, USD, industry templates for AGV/AMR (ISO 3691-4), service robots (ISO 13482)

### OhhO Frame — "Your robot stack, ready in one afternoon."

**When building this product, include:**
- ROS 2 Jazzy workspace, pre-structured (Ubuntu 24.04)
- Docker + DevContainer build (containerized everything)
- **Gazebo Harmonic + Isaac Sim** simulation, wired to the same topics as the real robot
- **Scene formats: URDF, SDF, USD** (Isaac Sim / Omniverse)
- **MCAP recording** (ROS 2-native bag format — not a proprietary format)
- CI/CD out of the box (GitHub Actions — build + colcon test)
- Single or multi-machine deploy (DDS peer auto-config, ROS_DOMAIN_ID 30)
- Reference drivers as a starting point (swappable, not locked)
- Plan-gated: Spark (simulation only), Builder (+ hardware), Fleet (multi-machine), Forge (on-prem)
- **Standards to mention:** ROS 2 Jazzy, URDF, SDF, USD, MCAP, DDS

### OhhO Bench — "From a box of parts to a robot that powers on."

**When building this product, include:**
- Step-by-step assembly from your OhhO Build BOM
- Wiring & port map (serial / power / bus — which controller, which port, which baud)
- One-click firmware flashing (protocol-aware, no hand-edited config)
- Hardware self-test (spin each motor, read encoders/IMU, sweep arm servos)
- Guided calibration (odometry geometry, IMU bias, camera intrinsics, BEV rig, arm homing)
- Writes the deployment profile for Frame/View/Autonomy
- Plan-gated: Spark (assembly guide + self-test), Builder (+ firmware + calibration), Fleet (batch bring-up), Forge (contract-mfg handoff)
- **Standards to mention:** CAN bus (for motor controller bring-up), CiA 402 (CANopen motion profile)

### OhhO Connect — "One link. Any robot. Any transport."

**When building this product, include:**
- **Five transports, one API:** ROSBridge WebSocket (Wi-Fi), Web Serial (USB), Web Bluetooth (BLE), **MQTT (IoT/cloud fleet)**, built-in simulator
- Robot-agnostic (generic velocity and joint commands, not robot-specific SDK calls)
- Browser-native, no install (Web Serial + Web Bluetooth in Chromium)
- Mixed-content aware (HTTPS-to-ws mismatch guidance)
- Persistent per-robot config (Supabase + localStorage)
- Simulator always on (deterministic in-browser telemetry)
- Plan-gated: included on every plan (foundational)
- **Standards to mention:** ROSBridge, Web Serial, Web Bluetooth (Nordic UART), MQTT, ROS 2 topics

### OhhO Bridge — "Connect any robot. Even the ones that don't speak ROS."

**When building this product, include ALL of these adapters:**
- **DDS-native humanoid adapter** (LowCmd/LowState ↔ ROS 2, per-model joint-index maps)
- **MAVLink drone adapter** (heartbeat, attitude, position, manual control → ROS 2)
- **CAN bus / CANopen adapter** (CiA 402, SDO/PDO object dictionary → ROS 2) — THE AGV/AMR protocol
- **OPC UA adapter** (browse address space, map nodes → ROS 2) — Industry 4.0
- **PROFINET adapter** (Siemens / European manufacturing)
- **EtherNet/IP adapter** (Rockwell / North American manufacturing)
- **MQTT adapter** (IoT, cloud telemetry, VDA 5050 transport)
- **VDA 5050 adapter** (AGV/AMR fleet standard — Linde, Toyota, MiR, KION interop)
- **Modbus TCP/RTU adapter** (PLC-driven arms)
- **EtherCAT adapter** (real-time servo control)
- **ROS-Industrial compatibility** (Fanuc, ABB, KUKA, Yaskawa, UR)
- **IEC 61131-3 / PLCopen** (PLC signal exchange — ladder logic, structured text)
- Impedance-gain defaults (kp/kd per joint)
- Browser-side codecs (Web Serial firmware-direct)
- Community-extensible (standalone adapter modules)
- **Update `lib/bridge/adapters.ts`** — add new adapter entries to the `ADAPTERS` array (the Bridge console renders them dynamically)
- **Update `lib/bridge/mcp-tools.ts`** — add new IDs to all 3 `adapterId` enum arrays
- **Update `lib/mcp/mcp.test.ts`** — update the adapter count assertion
- Plan-gated: Spark (not included), Builder (1 adapter), Fleet (all adapters), Forge (custom)
- **Standards to mention:** DDS, MAVLink, CANopen, OPC UA, PROFINET, EtherNet/IP, MQTT, VDA 5050, Modbus, EtherCAT, ROS-Industrial, IEC 61131-3

### OhhO Serve — "Robot AI inference, as an API."

**When building this product, include:**
- One-command model deploy (REST /health, /load_model, /predict)
- Pluggable backends (OpenVLA, SmolVLA, ACT, diffusion, custom — by config, not code)
- Optional 4-bit quantization ("fits your hardware — we help you size it", NOT "16 GB VRAM")
- Hot loading & health (zero-downtime model swap)
- Prometheus metrics (latency, throughput, GPU utilization)
- **ONNX export** support for Fleet OTA
- Hardware: "your GPU (desktop, server or cloud) — we help you size it" (NOT "NVIDIA GPU, 16 GB+")
- Plan-gated: Spark (not included), Builder (500 calls/day), Fleet (10K/day), Forge (on-prem unlimited)
- **Standards to mention:** ONNX, REST, Prometheus, LeRobot (model checkpoints)

### OhhO View — "Four cameras. One smart view."

**When building this product, include:**
- 4-camera surround BEV (front / rear / left / right)
- Geometric IPM from URDF camera poses (true top-down, not tiled mosaic)
- CPU-only (doesn't compete with GPU for VLA inference)
- Calibration UI + optional calibration file
- ROS 2 Image topic output (model-ready)
- Any camera layout (configurable poses, FoV, mounting height)
- Plan-gated: open source, included on every plan
- **Standards to mention:** ROS 2 Image topics, URDF (camera poses), OpenCV (IPM)

### OhhO Data — "Collect. Label. Ship."

**When building this product, include:**
- Teleop episode recording (leader arm + base velocity + multi-camera)
- **Training format: LeRobot HF dataset (Parquet + MP4)** — no bespoke converters
- **ROS 2 recording: MCAP** (ROS 2-native bag format) — interops with the ROS 2 ecosystem
- Multi-camera time sync (~50 ms tolerance)
- Episode viewer (scrub, inspect, keep-or-discard)
- One schema end-to-end (9-DOF state/action matches recorder, trainer, policy)
- CLI-first (scriptable record / inspect / push)
- Records from real robot, Gazebo, and Isaac Sim
- Plan-gated: Spark (local), Builder (cloud sync 1K episodes), Fleet (unlimited + annotation), Forge (managed)
- **Standards to mention:** LeRobot, MCAP, Parquet, MP4, HuggingFace

### OhhO Train — "Turn demonstrations into policies."

**When building this product, include:**
- Many methods, one engine: BC, VLA fine-tune (SmolVLA/ACT/diffusion/OpenVLA), offline + online RL
- Trains from OhhO Data (LeRobot) or in simulation (Gazebo, Isaac Sim, MuJoCo)
- Domain randomization for sim-to-real
- Continual learning (re-train as new episodes arrive, prioritized replay)
- Multi-objective rewards + AI-judged self-evaluation
- Weights & Biases tracking + Bayesian sweeps
- **Export: Serve checkpoint + ONNX for Fleet OTA** (hardware-aware execution providers)
- Built on the open OmniVLA engine
- Hardware: "your GPU — we help you size it or recommend a rig" (NOT "NVIDIA GPU")
- Plan-gated: Spark (local), Builder (cloud + tracking), Fleet (sweeps + continual), Forge (on-prem cluster)
- **Standards to mention:** LeRobot, ONNX, Gazebo, Isaac Sim, MuJoCo, W&B

### OhhO Autonomy — "Map it, navigate it, command it in plain language."

**When building this product, include:**
- 2-D & 3-D SLAM mapping (mapping + localization modes)
- Nav2 path planning + obstacle avoidance (fused costmap)
- EKF localization (wheel odometry + IMU fusion)
- Mission planner (navigate → manipulate state machine)
- **Natural-language agent (LLM-backed, your choice of model)** — NOT "Claude-backed"
- Safe control-mode mux (nav / AI / teleop)
- Named locations (kitchen, dock, bench)
- Plan-gated: Spark (sim SLAM), Builder (+ mission planner on hardware), Fleet (+ NL agent), Forge (custom + on-prem agent)
- **Standards to mention:** ROS 2 Nav2, SLAM, EKF, Open-RMF (fleet coordination)

### OhhO Mind — "Give your robot a mind of its own."

**When building this product, include:**
- Continuous perceive → reason → verify → act → monitor → reflect → remember loop
- Goals in plain language (not scripted commands)
- Hardware-safety gate on every action (velocity, joint deltas, reach — reject, don't clip)
- **Hybrid reasoning: cloud LLM + on-device LLM + NPU** — degrades gracefully offline (NOT "NPU" as the only option, NOT "Claude" as the only model)
- Memory that compounds (objects, places, past-attempt outcomes)
- Learns from experience (judged episodes → OhhO Train)
- Delivered as OhhO Fleet OTA update
- Plan-gated: Spark (not included), Builder (single-robot), Fleet (hybrid + memory + continual), Forge (on-prem brain + custom skills)
- **Standards to mention:** ONNX (for on-device), NPU runtime, ROS 2 topics

### OhhO Market — "Download a skill. Or sell one."

**When building this product, include:**
- Cross-brand skill marketplace (NOT per-robot OEM store)
- Verified, signed policies (OhhO Proof scenario suites — measured success rate, not marketing claim)
- Signed with OhhO Shield supply-chain keys (tamper-proof)
- Tagged by robot model, task, success rate
- One-click deploy via OhhO Serve or Fleet OTA
- Sell or share (free + paid, platform take-rate)
- Training-to-market loop (Train → Proof → Shield → Market)
- Plan-gated: Spark (browse + free), Builder (paid skills), Fleet (sell + team licenses), Forge (private marketplace)
- **Standards to mention:** ONNX (skill package format), LeRobot (training data provenance)

### OhhO Pilot — "Operate any robot. From anywhere. In mixed reality."

**When building this product, include:**
- **Any OpenXR mixed-reality headset** (NOT "Meta Quest 3" — say "any OpenXR headset")
- Mixed-reality passthrough (see real room + floating glass panels)
- Catalog-driven (any robot, one tap to drive — shared Supabase account)
- Hand-tracking arm IK (analytic for reference 6-DOF arm, FABRIK for generic, dual-arm for humanoids, MoveIt Servo for robot-side)
- WebRTC telepresence video (sub-100 ms) + MJPEG fallback
- Per-robot calibration + quick-resume
- Profile-driven demo recording (30 Hz JSONL, LeRobot-compatible)
- Safety: deadman grip, both-grips e-stop, velocity/joint clamps, connection-loss watchdog
- Plan-gated: Spark (not included), Builder (mobile, 3 robots), Fleet (VR + unlimited), Forge (white-label)
- **Standards to mention:** OpenXR, WebRTC, ROSBridge, LeRobot (recording), MJPEG

### OhhO Fleet — "Update 50 robots like you update an app."

**When building this product, include:**
- Fleet health dashboard (status, version, battery, last-seen)
- Signed OTA updates (ROS 2 workspace + ONNX policy models)
- Staged / canary rollouts
- **VDA 5050 warehouse integration** (AGV/AMR fleet standard over MQTT — interop with Linde, Toyota, MiR, KION, WMS)
- **Open-RMF multi-robot coordination** (traffic management, task allocation, conflict-free navigation across mixed-vendor fleets)
- Observability: Prometheus + Grafana + Loki + Tempo + AlertManager (pre-provisioned)
- Alerting (offline robots, latency spikes, resource exhaustion → email/Slack)
- Plan-gated: Spark (not included), Builder (not included), Fleet (100 robots + OTA), Forge (unlimited + on-prem)
- **Standards to mention:** VDA 5050, MQTT, Open-RMF, ONNX (OTA), Prometheus, Grafana

### OhhO Twin — "Your real robot. Mirrored in simulation. Live."

**When building this product, include:**
- Live telemetry → persistent sim (Gazebo or Isaac Sim)
- Replay + scrub (every telemetry frame recorded, per-frame inspection)
- What-if simulation (branch from any recorded state)
- Prediction (motor temperature, battery, trajectory projection)
- **Scene formats: USD, SDF, URDF** (shared with Frame)
- **OPC UA factory integration** (digital twin ↔ factory cell/MES/SCADA — Industry 4.0)
- Fleet-scale (mirror one robot or a hundred)
- Feeds OhhO Proof (near-misses → training scenarios) and OhhO Care (degradation → maintenance)
- Plan-gated: Spark (simulated twin), Builder (single live twin + replay), Fleet (multi-robot + what-if), Forge (prediction APIs)
- **Standards to mention:** USD, SDF, URDF, OPC UA, Gazebo, Isaac Sim

### OhhO Care — "Fix it before it breaks."

**When building this product, include:**
- Predictive maintenance alerts (degradation signals from Fleet + Twin → structured work orders)
- Auto-sourced parts from OhhO Build BOM (supplier links, lead times)
- Technician dispatch + scheduling
- Repair log → OhhO Comply audit trail
- Downtime + MTBF + MTTR metrics (per robot + fleet)
- Plan-gated: Spark (not included), Builder (basic scheduling), Fleet (predictive + parts), Forge (SLA tracking)
- **Standards to mention:** ISO 3691-4 (AGV safety), ISO 10218 (industrial robot safety), IEC 61508 (functional safety)

### OhhO Comply — "Ship robots the regulators will pass."

**When building this product, include ALL of these standards:**
- **EU Machinery Reg (CE)** — essential health & safety for machinery
- **ISO 10218-1/2** — industrial robot safety
- **ISO 15066** — collaborative robot safety (power/force limiting)
- **ISO 13849-1** — safety-related control systems (PL a-e)
- **ISO 12100** — risk assessment (hazard identification)
- **UL / IEC 60204-1** — electrical equipment of machines
- **ISO 3691-4** — AGV/AMR safety (driverless industrial trucks) — speed, zones, obstacle detection, e-stop
- **ANSI/ITSDF B56.5** — US guided industrial vehicles
- **ISO 13482** — personal care / service robot safety
- **ANSI/RIA R15.06** — US industrial robot safety (harmonized with ISO 10218)
- **IEC 61508** — foundational functional safety (root standard)
- Applicability engine (reads robot design + use case → which standards apply)
- Guided checklists (per-standard requirement lists with evidence, owner, status)
- Risk assessment templates (ISO 12100 + ISO 13849 PL determination)
- Document generation (technical file, risk assessment, Declaration of Conformity)
- Immutable audit trail
- Stays in sync with OhhO Build design changes
- **Update `lib/comply/standards.ts`** — add new `Standard` entries to `INITIAL_STANDARDS` with representative requirements
- Plan-gated: Spark (not included), Builder (self-assessment), Fleet (full + docs), Forge (custom + cert partner)
- Regions: EU (CE), North America (UL/ANSI), extensible

### OhhO Shield — "Security for robots that touch the real world."

**When building this product, include:**
- Hardware-rooted device identity (keys + X.509 certs)
- Mutually-authenticated encrypted links (TLS / encrypted DDS)
- Secure boot + signed OTA
- SBOM + CVE monitoring (per-robot, continuous)
- Zero-trust access (RBAC + audit log, SSO on Forge)
- Fleet-wide risk posture (live security score per robot + fleet)
- Plan-gated: Spark (not included), Builder (encrypted + signed), Fleet (+ identity + SBOM), Forge (+ secure boot + SSO + on-prem)
- **Standards to mention:** X.509, TLS, DDS security, SBOM (SPDX/CycloneDX)

### OhhO Proof — "Prove the robot is safe before it ships."

**When building this product, include:**
- Scenario-based simulation testing (thousands of randomized scenarios)
- Pass/fail criteria (task success, collisions, safety zones, timing)
- Regression tracking (vs last known-good build)
- Coverage map (condition-space — speeds, payloads, lighting, layouts)
- Fault injection (sensor dropouts, latency, actuator faults)
- Versioned safety-case report (feeds OhhO Comply)
- Runs across Gazebo + Isaac Sim
- Plan-gated: Spark (single scenario), Builder (suites + regression), Fleet (large-batch + coverage + fault injection), Forge (custom + sign-off)
- **Standards to mention:** Gazebo, Isaac Sim, ISO 12100 (risk assessment), ISO 13849 (PL verification)

---

## 5. Adding a new product

When shipping a new product console end-to-end, follow this checklist:

1. **Add the product to `lib/products.tsx`** — the `PRODUCTS` array. Include:
   `slug`, `name`, `tag`, `desc`, `accent` (cyan/violet), `category`
   (Design/Foundation/Intelligence/Operations/Trust), `icon` (SVG), `hero`,
   `highlights[]`, `overview[]`, `features[]`, `how[]`, `specs[]`, `plans[]`,
   `recommendedPlan`, `planRationale`, `faq[]`, `related[]`, `dashboardCaption`,
   and optionally `app: { href, label }` and `vr: true`.
2. **Create the app route** — `app/<slug>/page.tsx` with metadata + ConsoleGate +
   the console component.
3. **Create the console component** — `components/<slug>/<Name>Console.tsx`.
4. **Create the MCP tools** — `lib/<slug>/mcp-tools.ts` (follow the convention in
   `AGENTS.md` root: "Every time you add a new feature or option to any console,
   you MUST also add an MCP tool for it"). Register in `lib/mcp/registry.ts`.
5. **Create the product doc** — `docs/products/<slug>.md` mirroring the
   products.tsx entry.
6. **Add the product to `app/standards/page.tsx`** — if it supports any industry
   standard, list it in the `STANDARDS` array.
7. **Regenerate the VR manifest** — `npm run generate:vr` (if the product sets
   `vr: true`).
8. **Run `npm run test`** — all tests must pass. Add new tests for the console.
9. **Update this file** — add a **Per-product build prompt** section for the new
   product, listing every standard/feature to include.

---

## 6. Adding a new industry standard

When adding a new protocol/safety standard/data format:

1. **Add it to the Standards reference section above** (section 3) — with name,
   what it is, use case, and which products it applies to.
2. **If it's a communication protocol** → add an adapter entry to
   `lib/bridge/adapters.ts` (`ADAPTERS` array), update the `adapterId` enums in
   `lib/bridge/mcp-tools.ts` (3 places), update the adapter-count assertion in
   `lib/mcp/mcp.test.ts`, and add it to the Bridge product in `lib/products.tsx`
   (highlights, features, specs).
3. **If it's a safety standard** → add a `Standard` entry to
   `lib/comply/standards.ts` (`INITIAL_STANDARDS` array) with representative
   requirements, and add it to the Comply product in `lib/products.tsx`
   (highlights, features, specs, FAQ).
4. **If it's a fleet standard** → add it to the Fleet product in
   `lib/products.tsx` (highlights, features, specs, FAQ) and
   `docs/products/fleet.md`.
5. **Add it to `app/standards/page.tsx`** — the `STANDARDS` array.
6. **Update the OS page adapter chips** (`app/os/page.tsx` `ADAPTERS` array) if
   it's a communication protocol.
7. **Update the relevant `docs/products/<slug>.md`** to mirror.
8. **Run `npm run test`** — all tests must pass.

---

## 7. File map — where things live

| File | Purpose |
|---|---|
| `lib/products.tsx` | Single source of truth for the product catalog (19 products). Both homepage + detail pages read from it. |
| `lib/bridge/adapters.ts` | Bridge adapter registry (12 adapters). The Bridge console renders `ADAPTERS` dynamically. |
| `lib/bridge/mcp-tools.ts` | Bridge MCP tools — `adapterId` enums in 3 places must stay in sync. |
| `lib/comply/standards.ts` | Comply standards registry (8 standards). The Comply console renders `INITIAL_STANDARDS` dynamically. |
| `lib/market/skills.ts` | Marketplace skill listings (tagged with real robot models — brand names OK here). |
| `lib/garage/robot-catalog.ts` | Robot catalog (real robots users can pick — brand names OK here). |
| `lib/vr/manifest.ts` | VR manifest generator — reads from `products.tsx`. Run `npm run generate:vr` to regenerate `public/vr/manifest.json`. |
| `lib/site.ts` | Central link definitions (add new page hrefs here). |
| `lib/mcp/registry.ts` | MCP tool registry — aggregate all product MCP tools. |
| `lib/mcp/mcp.test.ts` | MCP server tests — adapter count assertion lives here. |
| `app/standards/page.tsx` | The dedicated standards page — `STANDARDS` array lists every standard. |
| `app/os/page.tsx` | The OS marketing page — `ADAPTERS` array lists protocol chips. |
| `docs/products/<slug>.md` | Per-product docs — mirror the `products.tsx` entry. |
| `components/OhhoOS.tsx` | Homepage OS section — `ROBOT_CHIPS` + `DIFFERENTIATORS`. |
| `components/Products.tsx` | Homepage products grid — intro paragraph. |
| `components/Nav.tsx` | Top nav — `MARKETING_LINKS` array. |
| `components/Footer.tsx` | Footer — `ALL_LINKS` array. |
| `public/vr/manifest.json` | VR app manifest — regenerated from `products.tsx` via `npm run generate:vr`. |
| `public/docs/ohho-os/*.svg` | Marketing diagrams — brand-name labels must stay generic. |

---

## 7a. HDR highlights convention

The site uses HDR (high dynamic range) colour to make the *important* things
pop like a neon sign on HDR-capable displays — starting with the logo. **All
HDR styling is gated behind `@media (dynamic-range: high)`** in
`app/globals.css`, so non-HDR devices keep the base sRGB palette untouched and
the site looks exactly as before. Graceful degradation is built in — never
apply an HDR colour outside that media query.

**How it works:**
- HDR accent tokens live in `:root`: `--cyan-hdr`, `--cyan-hdr-bright`,
  `--violet-hdr` — expressed in the `rec2100-pq` (HDR10) colour space so they
  render brighter than SDR white on capable displays.
- **Layer 1 (subtle):** inside the media query, `--cyan` / `--violet` (and
  their glow variants) are re-pointed at the HDR tokens, so every accent
  site-wide gets a gentle richness lift on HDR.
- **Layer 2 (strong, targeted):** utility classes for the few elements that
  should truly glow. Apply these sparingly to preserve hierarchy:
  - `.hdr-logo-glow` — the cyan "O" glyphs of the wordmark (Nav + Hero).
    Persistent neon glow that exceeds SDR white.
  - `.hdr-cta` — primary cyan calls-to-action (Hero "Start Free", Nav
    "Get Started"/"Upgrade", Pricing "Start Building", CtaBanner).
  - `.hdr-cta-violet` — violet CTAs (Pricing "Deploy Your Fleet").
  - `.glass-featured` tiles get an always-on neon rim on HDR.

**When adding a new important/highlighted element:** prefer applying one of
these utility classes over inventing a new HDR colour. If you must add a new
HDR token, define it in `:root` next to the existing `--*-hdr` tokens and only
*use* it inside `@media (dynamic-range: high)`. Always keep the sRGB
`--cyan`/`--violet` baseline intact for non-HDR devices.

---

## 8. Build & test commands

```bash
# Run all tests (107+ tests, must pass before commit)
cd website && npm run test

# Regenerate the VR manifest after changing products.tsx
cd website && npm run generate:vr

# Lint
cd website && npm run lint

# Typecheck
cd website && npx tsc --noEmit

# Dev server
cd website && npm run dev

# Production build
cd website && npm run build
```

---

## 9. Changelog

| Date | Change |
|---|---|
| 2026-07-06 | Initial creation. 35 standards documented across 4 categories. 19 product build prompts. Brand-agnostic + hardware-agnostic rules. |
| 2026-08-21 | Added HDR highlights convention (§7a). `rec2100-pq` accent tokens + `.hdr-logo-glow` / `.hdr-cta` / `.hdr-cta-violet` utilities, all gated by `@media (dynamic-range: high)` so non-HDR devices keep the sRGB palette. |

<!-- 
  ════════════════════════════════════════════════════════════════════════════
  MAINTAINER NOTES — add new requirements here as you think of them.
  The AI agent reads this entire file before building, so anything you add
  below will be picked up automatically. Format: date + requirement.
  ════════════════════════════════════════════════════════════════════════════
-->
