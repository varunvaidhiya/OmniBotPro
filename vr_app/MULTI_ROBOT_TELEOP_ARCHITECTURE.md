# Multi-Robot VR Teleoperation — Architecture & Build Plan

> Status: **All phases implemented (0a–0c + 1–5)** — full multi-robot VR teleop stack: platform contract, app shell, garage sync, glass UI + passthrough, spatial add-robot flow, profile-driven teleop, generalized drive + manipulation, WebRTC video, profile-driven recording, robot-side IK, per-robot calibration, quick-resume, onboarding, tests
> Target headset: **Meta Quest 3 / 3S** (mixed reality, passthrough)
> Scope: extend `vr_app/` from a single hardcoded OmniBot controller into a
> **catalog-driven, any-robot** mixed-reality teleoperation app whose robot list,
> capability model, branding, **login, product console and saved-robot garage** all
> mirror the OhhO website — **one shared account, no data re-entry**.

### Connected experience — one identity across web, Android and VR

The headset is not a separate silo. A user signs in **once** (the same Supabase
account behind the website and Android app) and everything they already have is
there: the same branding, the same product list, and — critically — the **same
garage of robots**. Add a robot on the website, and it appears in the headset; no
re-typing IPs, models, or names. The shared substrate:

- **Identity** — one Supabase project (auth + RLS). The VR app signs in with the
  same backend; the access token scopes every read to that user.
- **Data** — the `user_robots` table is the single garage. Web, Android and VR are
  all just clients of it.
- **Catalog & branding** — served as static JSON the website generates, so all
  surfaces render robots and UI identically.

### Implemented in this branch (Phase 0a + 0b)

- **Website** — `Product.vr` flag + **OhhO Pilot** tagged; `lib/vr/manifest.ts` →
  `public/vr/manifest.json` (branding + Supabase auth + VR products) and
  `lib/vr/catalog.ts` → `public/vr/catalog.json` (every category + robot type).
  Drift-checked by `manifest.test.ts` + `catalog.test.ts`. _All green: `tsc`
  clean, vitest 107/107._
- **VR — platform** (`Core/Platform/`) — manifest + catalog models, `OhhoPlatform`
  (fetch + offline), `OhhoCatalog` (lazy catalog + id→model resolution),
  `SupabaseAuthService` (email-OTP login), **`GarageClient`** (pulls `user_robots`
  via PostgREST), `Core/OhhoTheme.cs`; bundled `Resources/vr_{manifest,catalog}.json`.
- **VR — app shell** — `App/OhhoVrApp` (login→console→garage flow, configures the
  Supabase clients from the manifest, restores sessions), `UI/Auth/LoginPanelController`,
  `UI/Console/{ConsolePanelController,ProductCardView}`,
  `UI/Garage/{GaragePanelController,RobotCardView}`.
- **VR — design system + MR (0c)** — `Shaders/OhhoGlass.shader` (frosted glass
  card), `UI/Theme/{GlassPanel,ThemedText,AccentButton,OhhoFontSet,ThemeApplier}`
  (branding driven from the manifest tokens), `MR/{PassthroughManager,WorldSpaceUiPlacer}`
  (Quest passthrough + floating panels). Scene/prefab assembly: `vr_app/SCENE_SETUP.md`.

Next (Phase 2): wire a selected garage robot's profile into the control layer
(`IDriveScheme` + hand-IK `IManipulationScheme`) — see §5 and §10. _The VR C# is
not built in CI (no Unity toolchain); UI controllers are the wiring layer for
scene/prefab work in the editor._

### Implemented in this branch (Phase 2 — profile-driven teleop)

- **`RobotProfile`** — runtime capability model (C# mirror of
  `website/lib/garage/robot-config.ts` `RobotConfig`): drive kind, arm DOF,
  joint specs (names + min/max/home), ROS topics, max velocities, hand-workspace
  geometry. Replaces the hardcoded `Core/RobotConfig.cs` constants for the
  control layer.
- **`RobotProfileFactory`** — derives a `RobotProfile` from a `GarageRobot`
  (catalog `VrHardwareModel` → `DriveKind` via `DriveSpecs.ParseDrive`; flagship
  override pins OmniBot to the exact SO-101 6-DOF + mecanum stack). Pure data,
  offline, no AI call.
- **`IDriveScheme` / `MecanumDriveScheme`** — drive strategy extracted from the
  legacy `BaseController.cs` (dead-zone, turbo, e-stop, 20 Hz publish,
  control-mode management), now reading limits + topics from the profile.
- **`IManipulationScheme` / `HandIK6DofScheme`** — hand→arm IK retargeting
  extracted from the legacy `HandTrackingArmController.cs`, wrapping `ArmIKSolver`
  with profile-driven joint names/limits and a thumbs-up arm-enable toggle.
- **`ControlSchemeFactory`** — picks the drive + manipulation schemes from the
  profile (today: mecanum + 6-DOF hand-IK; other drive kinds log a Phase 3 warning).
- **`IRobotLink` / `RosBridgeLink`** — transport abstraction wrapping
  `ROSBridgeClient` (mirrors `RobotTransport` on the website), so schemes are
  testable and the transport is swappable.
- **`TeleopController`** — MonoBehaviour entry point that pumps OVR input +
  hand tracking → schemes → `IRobotLink` every frame. Replaces the legacy
  `BaseController` + `HandTrackingArmController` as the single teleop GameObject.
- **Wiring** — `GaragePanelController.RobotPicked` event → `OhhoVrApp` builds the
  profile → `TeleopController.StartTeleop(profile)` → routes to the teleop view.
  Selecting a robot in the garage now starts driving it.

### Implemented in this branch (Phase 3 — generalized drive + manipulation)

- **`DriveSchemeBase`** — abstract base class extracting the shared e-stop
  (both-grips), turbo (right grip), dead-zone, 20 Hz publish-timing and
  control-mode management from `MecanumDriveScheme`. Every drive scheme
  inherits it and only implements `ComputeVelocity` → `TwistMsg`.
- **`DifferentialDriveScheme`** — `[v, ω]` for differential / skid-steer /
  tracked / rocker-bogie / unknown bases (TurtleBot 4, tracked robots).
- **`AckermannDriveScheme`** — `[v, δ]` for car-like steering.
- **`AerialDriveScheme`** — `[vx, vy, vz, ω]` for quadrotors / hexacopters /
  VTOLs (mode-2 RC mapping: left stick = throttle + yaw, right = translate).
  Idle mode reverts to `"offboard"` instead of `"nav2"`.
- **`ThrusterDriveScheme`** — `[surge, sway, heave, yaw]` for underwater ROVs.
- **`FabrikSolver`** — generic FABRIK IK for arbitrary serial chains, driven by
  `RobotProfile.ArmLinkLengths` + `JointSpec` limits. Used for non-SO-101 arms
  and dual-arm humanoids where the analytic SO-101 solver doesn't apply.
- **`GripperOnlyScheme`** — pinch → gripper for robots with a single-DOF
  gripper and no arm joints (drones, simple AMRs).
- **`DualArmManipulationScheme`** — two hands → two arms for humanoids (Unitree
  G1, Figure 02). Two independent `FabrikSolver` chains, published in one
  `JointState` message with `r_` / `l_` prefixed joint names.
- **`ControlSchemeFactory`** — dispatches every `DriveKind` to its scheme and
  every arm topology (1-DOF gripper / 6-DOF hand-IK / 7+ DOF dual-arm / 2–5 DOF
  generic) to its manipulation scheme.
- **`RobotProfile`** — added `ArmLinkLengths` (link geometry for FABRIK) and
  `IsDualArm` flag; `RobotProfileFactory` sets them for flagship arms (SO-101,
  UR5e, Unitree G1/H1, Figure 02).

---

## 1. Goal

Put on a Quest 3, see your real room through passthrough, pick a robot from the
**same catalog that exists on the website** (`website/lib/garage/`), and teleoperate
it with controls appropriate to that robot:

- **Base / locomotion** — driven by the controllers (e.g. OmniBot's holonomic
  mecanum base from the left thumbstick).
- **Manipulator** — driven by **natural hand motion**: the headset reports a
  full 6-DOF wrist pose, we retarget it to the arm's end-effector and solve
  **inverse kinematics** for the joint angles. The gripper is driven by pinch.

Every robot category on the website (drones, wheeled, legged, humanoid,
mobile-manipulators, marine, industrial arms, …) gets a control scheme that fits
its `DriveKind` and arm DOF. The UI carries the website's visual identity.

### Why this is mostly an *integration* job, not a from-scratch build

Two halves already exist and were designed to meet here:

- **`vr_app/`** already has Unity + Meta XR SDK + ROSBridge + a CCD IK solver +
  hand tracking + dataset recording — but hardcoded to one robot
  (`vr_app/Assets/Scripts/Core/RobotConfig.cs`).
- **`website/lib/garage/`** already has the robot catalog and a **robot-agnostic
  capability model** (`RobotConfig` / `DriveKind`) plus a transport abstraction
  (`RobotTransport`) explicitly built so "a drone, a UR5e arm, a quadruped and
  OmniBot each get a correct, distinct console."

This document defines how the VR app consumes that same model so the headset and
the website agree on *which robots exist* and *how each one is controlled*.

---

## 2. What already exists (starting point)

### 2.1 VR app — `vr_app/` (Unity 2023.3 LTS, Meta XR SDK 60, Quest 3)

| Area | Files | Notes |
|---|---|---|
| Transport | `Core/ROSBridgeClient.cs`, `Core/ConnectionManager.cs` | WebSocket → rosbridge v2 JSON, singletons. |
| Messages | `Core/Messages/*` | Twist, JointState, Odometry, Imu, String/Bool. |
| Base drive | `Input/BaseController.cs` | Thumbsticks → `/cmd_vel/teleop`. **Mecanum only today.** |
| Arm IK | `Input/ArmIKSolver.cs` | CCD, pure C#, 20 Hz, 50 iters, 1 mm tol, 6 joints clamped. **SO-101 geometry hardcoded.** |
| Hand control | `Input/HandTrackingArmController.cs`, `Input/GestureDetector.cs` | Right-hand pose → IK target, pinch → gripper. |
| UI | `UI/HUDManager.cs`, `UI/{Connection,Telemetry,Control,Recording}Panel.cs`, `UI/CameraFeedViewer.cs` | Floating panels; MJPEG camera feed. |
| Recording | `Recording/EpisodeManager.cs`, `Recording/DatasetRecorder.cs` | 30 Hz JSONL, export to the `omnibot_vr` ROS bridge. |
| Constants | `Core/RobotConfig.cs` | **Single-robot constants — the thing this plan generalizes.** |

Current Unity packages (`vr_app/Packages/manifest.json`): OpenXR 1.10, XR
Management, Input System, TextMeshPro, uGUI, Newtonsoft JSON, NativeWebSocket,
Meta XR SDK Core/Interaction/Interaction.OVR 60.

### 2.2 Website — `website/lib/garage/` & `website/lib/connect/`

| File | Provides |
|---|---|
| `garage/types.ts` | 15 `CATEGORIES` (drones, wheeled, legged, humanoid, mobile-manipulator, marine, industrial-arm, tracked, swarm, agricultural, underwater-rov, space, medical, delivery, inspection), each with `icon` + accent `color`. |
| `garage/robot-catalog.ts` | `ROBOT_TYPES[]` → hardware models (OmniBot Pro, Yahboom X3, TurtleBot 4, Unitree Go2/H1/G1, Figure 02, DJI, UR, …). |
| `garage/robot-config.ts` | `DriveKind` (18 kinds), `DRIVE_SPECS` (baseDof, actionLabels, holonomic, max vels), `RobotConfig` (joints, armDof, sensors, rosTopics, capabilities), `deriveRobotConfig()`, `FLAGSHIP_OVERRIDES`, `getRobotConfig()`. |
| `connect/types.ts` | `RobotTransport` interface (`sendVelocity`, `sendJointCommand`, `emergencyStop`, `onTelemetry`) + `Velocity`/`Odometry`/`RobotTelemetry`. |
| `connect/factory.ts` | `createTransport()` — rosbridge / webserial / webbluetooth / simulated. |
| `components/garage/RobotSelector.tsx` | The selection UX to mirror: **categories → types → models → name**. |
| `app/api/robot/console-spec/route.ts` | Already serves a structured `RobotConfig` per hardware model. |

---

## 3. Target architecture

```
┌──────────────────── Meta Quest 3 (mixed reality, passthrough always on) ────────────────────┐
│                                                                                              │
│  RobotCatalogService ──fetch──► website /api/catalog + /api/robot/console-spec + /api/garage  │
│        │   categories→types→models · RobotConfig per model · the user's saved garage           │
│        ▼                                                                                      │
│  Robot Selection (spatial UI, mirrors RobotSelector.tsx)                                      │
│        │   user picks model → RobotProfile { drive, joints, armDof, rosTopics, maxVels … }      │
│        ▼                                                                                      │
│  ControlSchemeFactory(RobotProfile)                                                          │
│        ├── IDriveScheme        ← by DriveKind  (Mecanum / Differential / Ackermann /          │
│        │     Control/Drive/*        Quadrotor / Quadruped / Bipedal / Thruster / FixedBase …)  │
│        └── IManipulationScheme ← by armDof     (HandIK6DOF / DualArm / GripperOnly / None)     │
│              Control/Manip/*                                                                  │
│                                                                                              │
│  Hands ─► retarget ─► IK ─► joint cmds          Thumbsticks/hands ─► drive cmds                │
│        └─────────────────────────┬──────────────────────────────────┘                        │
│                                  ▼                                                            │
│  IRobotLink (ROSBridge today; WebRTC video opt.) ── ws://robot:9090 ──► any ROS 2 robot        │
│        topics from RobotProfile.rosTopics  (cmdVel, jointStates, odom, images)                │
│                                                                                              │
│  OhhoTheme tokens ─► every panel = glass + cyan/violet + Space Grotesk / JetBrains Mono         │
└──────────────────────────────────────────────────────────────────────────────────────────────┘
```

Four new VR abstractions, each mirroring something the website already has:

| New VR abstraction | Mirrors (web) | Replaces / wraps (VR today) |
|---|---|---|
| `RobotProfile` (runtime, fetched) | `RobotConfig` (`robot-config.ts`) | `Core/RobotConfig.cs` constants |
| `IDriveScheme` (strategy per `DriveKind`) | `DRIVE_SPECS` | `Input/BaseController.cs` (mecanum only) |
| `IManipulationScheme` (per arm) | `joints` / `armDof` in `RobotConfig` | `Input/HandTrackingArmController.cs` (SO-101 only) |
| `IRobotLink` | `RobotTransport` (`connect/types.ts`) | `Core/ROSBridgeClient.cs` (keep, wrap) |

**Design rule:** nothing about *which robots exist* or *what DOF a robot has*
lives in C# constants. It comes from the catalog at runtime, with a bundled JSON
fallback for offline.

---

## 4. Tech stack — libraries, frameworks, techniques

### 4.1 Engine & XR

| Concern | Choice | Notes |
|---|---|---|
| Engine | Unity 2023.3 LTS + URP | Already pinned. |
| XR runtime | OpenXR + Meta feature group | Already in manifest. |
| Meta SDK | Meta XR Core/Interaction SDK 60 | Passthrough + hand tracking. |
| **Passthrough MR** | `OVRPassthroughLayer` | Always-on background = "see everything through passthrough." |
| **Scene understanding** | **MRUK** — add `com.meta.xr.mrutilitykit` | Room mesh + spatial anchors → pin the virtual robot workspace to a real surface; world-lock panels. |
| Hand tracking | Meta Hand Tracking (OVRHand); optionally `com.unity.xr.hands` for OpenXR-portable joints | Wrist pose + pinch. |
| UI | uGUI + TextMeshPro (present); world-space curved canvases | Glass shader via URP Shader Graph. |
| Account link (optional) | Meta Platform SDK or device-code flow | Pull the user's garage from their OhhO account. |

### 4.2 Networking / robot link

| Concern | Choice | Notes |
|---|---|---|
| Control + telemetry | rosbridge_suite over NativeWebSocket (present) | Universal ROS 2 contract; topics from `RobotProfile.rosTopics`. |
| Live video | MJPEG via `web_video_server` (present) → upgrade to **WebRTC** (`com.unity.webrtc`) | WebRTC ≈ sub-100 ms first-person telepresence. |
| Non-ROS robots | per-robot **bridge node** on the robot side | Matches the platform's "any robot, any transport" stance; VR app stays ROS-only. |
| JSON | Newtonsoft (present) | rosbridge v2 protocol. |

### 4.3 Inverse kinematics (the hand→arm core)

| Option | When | Trade-off |
|---|---|---|
| Analytic 6-DOF IK (closed-form, spherical-wrist arms like SO-101) | flagship arms with known geometry | Fastest, most stable, no jitter. Recommended for OmniBot. |
| FABRIK (free; evolution of the existing CCD) | generic arms, unknown geometry | Cheap, stable, easy per-joint clamping. Good default fallback. |
| **BioIK** (Unity Asset Store) | arbitrary chains, position+orientation+joint limits, dual-arm | Best general solver; paid (~$60). Recommended if one solver should cover *everything*. |
| Existing `ArmIKSolver.cs` (CCD) | already present | Keep as generic fallback; feed it the chain from `RobotProfile`. |

**Recommendation:** hybrid — analytic where the arm is known (SO-101), BioIK or
FABRIK driven by `RobotProfile.joints` everywhere else.

---

## 5. Robot-agnostic control model

### 5.1 Drive schemes — one per `DriveKind`

Port the website's `DRIVE_SPECS` table into a C# strategy. Each scheme maps
controller input → the robot's native action vector and publishes to
`RobotProfile.rosTopics.cmdVel`.

```csharp
public interface IDriveScheme {
    void Configure(RobotProfile p);   // baseDof, actionLabels, holonomic, maxLin/maxAng
    void Tick(XRInputState input);    // thumbsticks + (optional) hands
    void Publish(IRobotLink link);    // → cmdVel (Twist or robot-native)
    string HudHint { get; }           // "Left stick: strafe · Right stick: yaw"
}
```

| `DriveKind` | Left controller | Right controller | Action vector |
|---|---|---|---|
| **mecanum** (OmniBot) | stick XY → vx, vy (holonomic strafe) | stick X → ω | `[vx, vy, ω]` |
| differential / skid / tracked | stick Y → v | stick X → ω | `[v, ω]` |
| ackermann | stick Y → v | stick X → steering δ | `[v, δ]` |
| quadrotor / hexacopter / vtol | stick XY → vx, vy | stick Y → vz, X → yaw | `[vx, vy, vz, ω]` |
| quadruped / bipedal | stick XY → vx, vy | stick X → ω | `[vx, vy, ω]` (+ gait gestures) |
| thruster (underwater) | stick XY → surge, sway | stick Y → heave, X → yaw | `[surge, sway, heave, yaw]` |
| fixed-base (industrial arm) | — (no base) | — | arm-only mode |

All clamped to `maxLinVel` / `maxAngVel` from the profile and ramp-limited to the
robot's own limiter (OmniBot caps 0.2 m/s and 0.05 m/s per 20 Hz tick).

### 5.2 Manipulation schemes — hand → arm via IK retargeting

The Quest reports a full **6-DOF wrist pose** (3 position + 3 orientation), which
is exactly what a 6-DOF arm end-effector needs. So this is a clean
*end-effector pose → joint angles* IK problem; the gripper is a separate channel
driven by pinch strength.

```
1. Quest hand tracking → wrist pose (pos + quat) in world space + pinch [0..1]
2. MRUK anchor "robot workspace origin" (a real spot on the table = virtual arm base)
   → compute hand pose RELATIVE to that origin
3. Scale + clamp into the arm's reachable sphere   (reach from RobotProfile.joints)
4. SOLVE IK  → joint angles, each clamped to JointSpec.min/max
5. Stream  /arm/joint_commands  @ 20 Hz   (names + limits from RobotProfile)
6. pinch → gripper joint (RobotProfile.joints last entry)
```

```csharp
public interface IManipulationScheme {
    void Configure(RobotProfile p);   // joints, armDof, reach, gripper joint
    void Tick(HandPose left, HandPose right);
    void Publish(IRobotLink link);    // → jointStates topic
}
```

Variants selected from `RobotConfig.capabilities` / `armDof`:

- **HandIK6DOF** — one hand → one 6-DOF arm (OmniBot, UR5e). Default.
- **DualArm** — two hands → two arms (Unitree H1, Figure 02); BioIK solves both chains.
- **GripperOnly** — pinch → open/close, no arm pose.
- **None** — drones / AMRs with no manipulator (manipulation UI hidden automatically).

**Where to solve IK — two valid designs (v1 = headset-side):**

- **Headset-side (recommended):** solve in Unity, stream joint angles. Lowest
  perceived latency, works with any joint-position robot, matches the existing
  `ArmIKSolver` design.
- **Robot-side:** stream the Cartesian target pose, let **MoveIt 2 Servo** solve
  on the robot (collision-aware). Expose as a per-profile toggle
  (`ikLocation: headset | robot`).

### 5.3 Worked example — OmniBot (the reference)

1. Selection → **OmniBot Pro** → `RobotProfile { drive: "mecanum", holonomic: true, armDof: 6, joints: SO101_JOINTS, rosTopics.cmdVel: "/cmd_vel/teleop", … }`.
2. Factory → `MecanumDriveScheme` + `HandIK6DOF` (analytic SO-101 solver).
3. **Base:** left stick → vx/vy strafe, right stick X → yaw → `/cmd_vel/teleop` @ 20 Hz.
4. **Arm:** right-hand pose → analytic IK → 6 joints → `/arm/joint_commands` @ 20 Hz; pinch → `arm_gripper`. Left-hand thumbs-up toggles arm enable; both grips = e-stop.
5. All in passthrough: a translucent twin of the SO-101 overlays the real table so the operator previews motion before the real arm catches up.

---

## 6. Mixed reality (passthrough) approach

- **Always-on passthrough** (`OVRPassthroughLayer`) as the scene background.
- **MRUK** room scan once → a **spatial anchor** for the "robot workspace origin."
  Hand→arm mapping is relative to this anchor, so the virtual arm sits on the real
  desk and stays put as the operator walks.
- **Three world-locked glass surfaces** in the room: (a) live robot **camera feed**
  (telepresence), (b) **telemetry / HUD**, (c) **virtual robot twin** mirroring the
  commanded base/joint state (visual confirmation + recording preview).
- Passthrough MR has no vection → minimal sim-sickness even during fast base driving.

---

## 7. Platform contract — single source of truth (static export!)

The website is canonical; the VR app fetches, never re-enters data in C#. One
constraint shapes *how*: **the site is a static export** (`vercel.json` →
`outputDirectory: website/out`) with **client-side Supabase auth**. There is no
runtime server for the headset to call, so the VR app uses two channels the
static site already supports:

1. **A static manifest** — `website/lib/vr/manifest.ts` → committed
   `website/public/vr/manifest.json`, served at `https://ohho-robotics.com/vr/manifest.json`.
   It carries the **branding tokens**, the **Supabase auth config** (public anon
   key, identical to the site's), and the **VR-headset products** (`Product.vr === true`).
   A `vitest` (`manifest.test.ts`) drift-checks the committed copy; the VR app
   also bundles a fallback at `Resources/vr_manifest.json`. _(Implemented — Phase 0.)_
2. **Direct Supabase** — the headset talks to GoTrue (`/auth/v1`) and PostgREST
   (`/rest/v1/user_robots`) with the same anon key + the user's session token,
   exactly as the website's browser client does. No bespoke backend.

The robot catalog/garage follows the same pattern next: extend the manifest (or a
sibling static JSON) with `CATEGORIES` + `ROBOT_TYPES`, and read the user's saved
robots straight from the `user_robots` table via PostgREST (the `RobotSelector`
flow: categories → types → models → confirm).

### App shell — mirrors the website: login → console → product → teleop

The headset reproduces the website's own flow:

```
Sign in to OhhO   →   Console (product grid)   →   open a product   →   (Pilot) teleop
(Supabase email       shows ONLY products            today: Pilot          robot select +
 OTP, in-headset)     where vr === true              (teleoperation)       drive + hand-IK
```

- **Login** mirrors `app/login/page.tsx` (Supabase). The site uses an email magic
  link; the headset uses the **email OTP** path of the *same* GoTrue backend (a
  short code is typeable in VR), implemented in `SupabaseAuthService`.
- **Console** mirrors `app/console` + the homepage product grid, but filtered to
  VR products from the manifest — so today the user sees **OhhO Pilot**, and any
  future headset products appear automatically when tagged `vr` on the website.
- Picking **Pilot** drops into the teleop flow (robot selection + the control
  model in §5).

---

## 8. UI & branding — mirror the website

Pull tokens straight from `website/app/globals.css` into a Unity `OhhoTheme`
ScriptableObject:

| Token | Value | VR usage |
|---|---|---|
| `--bg` | `#0A0E1A` | panel base / fade |
| `--cyan` | `#00D4FF` | primary accent, active states |
| `--violet` / lite | `#7C3AED` / `#A78BFA` | secondary accent, gradients |
| `--surf` | `#0F1628` | glass tint |
| Display font | Space Grotesk 700 | headings |
| Body font | Inter | body |
| Mono font | JetBrains Mono | cyan uppercase micro-labels |
| Glass | gradient border cyan→violet, frosted | URP glass shader on every panel |

Build a **VR design-system prefab kit** mirroring the web components:
`GlassPanel` (≈ `GlassCard`), `MonoLabel`, `AccentButton`, `StatTile`
(≈ `StatsBar`), `CategoryChip`, `Nav`. Import the **lucide** icons referenced by
the catalog as an SVG/sprite atlas so VR and web share iconography.

---

## 9. Safety, latency, comfort (non-negotiable)

- **Deadman:** hold a grip to drive; release → zero velocity.
- **E-stop:** both grips (already implemented) → `/emergency_stop`; latch until cleared.
- **Clamping:** every command clamped to `RobotProfile.maxLinVel/maxAngVel` and `JointSpec.min/max`; ramp-limit to the robot's own limiter.
- **Connection-loss watchdog:** no telemetry for N ms → auto-stop + red HUD.
- **Rate:** 20 Hz command; IK solve budgeted < 5 ms/frame.
- **Workspace clamp:** IK targets clamped to the reachable sphere.

---

## 10. Phased roadmap

| Phase | Deliverable | Key files |
|---|---|---|
| **0a — Platform contract** ✅ | `Product.vr` tag + `OhhO Pilot`; static `vr/manifest.json` (branding + Supabase auth + VR products) + vitest drift check; VR `OhhoTheme`, manifest models, `OhhoPlatform` fetch, `SupabaseAuthService` (email OTP) | `website/lib/vr/*`, `vr_app/.../Core/OhhoTheme.cs`, `Core/Platform/*` |
| **0b — App shell + garage sync** ✅ | Login panel (Supabase OTP), Console product grid (VR-filtered), Garage panel pulling the user's `user_robots` from Supabase; static `vr/catalog.json` (categories + robot types) + drift check; `OhhoCatalog` id→model resolution; `GarageClient` (PostgREST) | `website/lib/vr/catalog.*`, VR `App/OhhoVrApp`, `UI/Auth/`, `UI/Console/`, `UI/Garage/`, `Core/Platform/{OhhoCatalog,GarageClient,*Models}` |
| **0c — Glass design system + MR** ✅ | `OhhO/Glass` shader + `OhhoTheme`-driven components (GlassPanel/ThemedText/AccentButton/ThemeApplier + OhhoFontSet), passthrough bootstrap + floating world-space panels; scene-assembly guide | VR `Shaders/OhhoGlass.shader`, `UI/Theme/`, `MR/`, `SCENE_SETUP.md` |
| **1 — Robot selection (add)** ✅ | spatial "add robot" flow mirroring `RobotSelector` (categories → types → models → name → confirm); `RobotSelectionController` state machine + `CategoryCardView`/`TypeCardView`/`ModelCardView`; writes back to `user_robots` via `GarageClient.AddUserRobot`; wired into garage + fleet panels via Add-Robot button | `UI/Selection/{SelectionState,RobotSelectionController,CategoryCardView,TypeCardView,ModelCardView}.cs`, `UI/Garage/{GaragePanelController,FleetPanelController}.cs` |
| **2 — Profile + OmniBot end-to-end** ✅ | `RobotProfile` from catalog (`RobotProfileFactory`); `MecanumDriveScheme` + `HandIK6DofScheme`; full OmniBot drive+arm in MR; `IRobotLink` transport abstraction; `TeleopController` + garage→teleop wiring | `Control/RobotProfile.cs`, `Control/RobotProfileFactory.cs`, `Control/Drive/*`, `Control/Manip/*`, `Control/{IRobotLink,ControlSchemeFactory,TeleopController}.cs`, `App/OhhoVrApp.cs`, `UI/Garage/GaragePanelController.cs` |
| **3 — Generalize** ✅ | `DriveSchemeBase` shared infrastructure + `DifferentialDriveScheme`, `AckermannDriveScheme`, `AerialDriveScheme`, `ThrusterDriveScheme`; legged reuses mecanum (gait gestures = Phase 5); `FabrikSolver` generic IK; `GripperOnlyScheme`, `DualArmManipulationScheme`; `ControlSchemeFactory` dispatches all drive kinds + arm topologies; `ArmLinkLengths` + `IsDualArm` on `RobotProfile` | `Control/Drive/{DriveSchemeBase,Differential,Ackermann,Aerial,Thruster}DriveScheme.cs`, `Control/Manip/{FabrikSolver,GripperOnly,DualArm}*.cs`, `Control/ControlSchemeFactory.cs`, `Control/RobotProfile.cs`, `Control/RobotProfileFactory.cs` |
| **4 — Telepresence & data** ✅ | `IVideoSource` abstraction + `MjpegVideoSource` (extracted from `CameraFeedViewer`) + `WebRtcVideoSource` (sub-100 ms, graceful fallback); profile-driven `CameraFeedController` (reads `RobotProfile.Topics.Images`); `ProfileDrivenRecorder` (profile-aware topics + dims, replaces hardcoded `DatasetRecorder`); `IkLocation` enum + `PoseStampedMsg` for robot-side IK via MoveIt Servo; `HandIK6DofScheme` publishes Cartesian target when `IkLocation == Robot` | `Video/{IVideoSource,MjpegVideoSource,WebRtcVideoSource,CameraFeedController}.cs`, `Core/Messages/PoseStampedMsg.cs`, `Recording/ProfileDrivenRecorder.cs`, `Control/RobotProfile.cs`, `Control/RobotProfileFactory.cs`, `Control/Manip/HandIK6DofScheme.cs`, `Control/TeleopController.cs` |
| **5 — Polish** ✅ | `LastRobotStore` (quick-resume last robot), `FleetPanelController` (status badges + quick-resume banner), `RobotCalibration` + `CalibrationManager` (per-robot workspace offsets, IK location + velocity overrides, persisted in PlayerPrefs), `OnboardingController` (first-run control hints), edit-mode unit tests (DriveSpec parsing, ControlSchemeFactory dispatch, FabrikSolver convergence/limits, CalibrationManager round-trip) | `Control/{LastRobotStore,CalibrationManager}.cs`, `UI/Garage/FleetPanelController.cs`, `UI/OnboardingController.cs`, `Tests/{DriveSpecParsingTests,ControlSchemeFactoryTests,FabrikSolverTests,CalibrationManagerTests}.cs` |

**First running milestone: Phase 2** — OmniBot, but fully profile-driven, in MR,
and reskinned. It proves the whole spine without breaking the existing teleop.

---

## 11. Key decisions (recommendations)

1. **IK location:** headset-side joint streaming for v1; robot-side MoveIt Servo as an opt-in per profile.
2. **IK solver:** **FABRIK (free)** as the universal generic solver for non-SO-101 arms,
   driven by `RobotProfile.joints`; analytic IK for SO-101. _(Decided.)_
3. **Platform contract:** static `vr/manifest.json` + direct Supabase (the site is a
   static export). Never duplicate website data in C#. _(Decided / implemented.)_
4. **Transport:** ROSBridge as the universal contract; non-ROS robots get a bridge node on their side.
5. **Video:** ship MJPEG (present), upgrade to WebRTC in Phase 4.

---

## 12. Open questions

- ~~Account-linked garage sync in v1?~~ **Decided: yes** — the headset pulls the
  user's `user_robots` from Supabase (`GarageClient`), one shared account across
  web/Android/VR, no re-entry. _(Implemented in Phase 0b.)_
- Which non-ROS robot families to bridge first (drones via MAVLink, Unitree SDK, …)?
