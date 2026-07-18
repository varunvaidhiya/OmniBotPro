# OhhO VR — Scene Assembly Guide

> **Current state:** the scene is **fully generated** by an editor script.
> `Assets/scene1.unity` already contains everything — you never assemble the
> UI by hand. To regenerate after pulling changes (or if the scene breaks):
> **OmniBot → Rebuild Unified OhhO Screen**. The builder is idempotent,
> re-fixes the hand prefabs, rebuilds the card prefabs, wires every
> controller, and saves the scene.

The app runs **one unified world-space screen** ("OhhO Screen", 1600×1000 at
0.001 scale ≈ 1.6 m wide) floating 1.6 m in front of the user over
passthrough. A `WorldSpaceUiPlacer` keeps it facing the user; the header bar
(OhhO logo + ohho-robotics.com + Open Website button) is always visible, and
the four pages route inside it:

```
┌──────────────── OhhO Screen (one canvas, faces the user) ───────────────┐
│ Header: OHHO · ohho-robotics.com ··················· [Open Website]     │
├──────────────────────────────────────────────────────────────────────────┤
│ Login page      → email OTP sign-in (Supabase, same account as the site) │
│ Console page    → VR products from the manifest (today: OhhO Pilot)      │
│ Garage page     → the user's robots (Supabase user_robots, shared w/web) │
│ Teleop page     → connection + telemetry │ camera feed │ recording/export│
└──────────────────────────────────────────────────────────────────────────┘
```

`OhhoVrApp` routes the pages (`SetActive` one at a time); `ScreenToggle`
hides/shows the whole screen with the **left-controller B** button.

---

## 1. Project Settings

1. **XR Plug-in Management → OpenXR (Android)**: enable, add the **Meta Quest**
   feature group.
2. **OpenXR features**: enable **Passthrough**, **Hand Tracking**, and Meta Quest
   support. Target **Android, ARM64, API 32+**.
3. **Graphics → Always Included Shaders**: add **`OhhO/Glass`** (so
   `Shader.Find("OhhO/Glass")` resolves in a build — `GlassPanel` needs it).
4. **Player → Active Input Handling**: Input System (or Both).

## 2. Fonts (branding parity)

The TMP default font reference (`TextMesh Pro/Resources/TMP Settings.asset`)
must point at **LiberationSans SDF**, and the font materials must use the
**`TextMeshPro/Distance Field`** shader — both were repaired in this branch
(magenta text = broken shader reference; empty text = missing default font).
If you import Space Grotesk / Inter / JetBrains Mono later, create TMP font
assets for them and an **OhhO Font Set** so the `ThemeApplier` can restyle.

## 3. Bootstrap GameObject ("[OhhO VR App]")

One persistent GameObject holding the platform services (each is a singleton):

| Component | Purpose |
|---|---|
| `OhhoPlatform` | fetches `https://ohho-robotics.com/vr/manifest.json`; offline fallback `vr_manifest` |
| `OhhoCatalog` | catalog; offline fallback `vr_catalog` |
| `SupabaseAuthService` | email-OTP sign-in (configured from the manifest) |
| `GarageClient` | pulls `user_robots` via PostgREST |
| `ConnectionManager` | ROSBridge lifecycle |
| `TeleopController` | pumps VR input → drive + hand-IK schemes → ROS |
| `GestureDetector` | hand poses (auto-finds the scene `OVRHand`s) |
| `ProfileDrivenRecorder` | 30 Hz JSONL recorder for the active profile |
| `VRGraphicsBoost` | 1.5× eye texture, MSAA 4×, FFR off, 90 Hz |
| `OhhoVrApp` | routes the four pages; wired to the panels + `TeleopController` |

## 4. Camera rig + passthrough + hands

1. **OVRCameraRig** (Meta prefab) with `OVRPassthroughLayer` (Underlay) +
   `PassthroughManager` — `passthroughLayer` and `hmdCamera` are both assigned
   on the `PassthroughManager` component.
2. **Tracking origin: Stage** (`OVRManager._trackingOriginType = 2`) — required
   for correct mixed-reality alignment.
3. **Hands** — `OVRCustomHandPrefab_L/R` under the hand anchors with:
   - `_updateRootPose = true`, `_updateRootScale = true`,
     `_applyBoneTranslations = true` → the hand mesh exactly follows the
     tracked wrist (fixes the hand-mesh offset).
   - `updateWhenOffscreen = true` on `l_handMeshNode` / `r_handMeshNode` →
     prevents per-eye frustum culling (fixes hands visible in only one eye).
   - Android manifest: `com.oculus.permission.HAND_TRACKING` +
     `com.oculus.handtracking.frequency = HIGH`.
4. **EventSystem** — `OVRInputModule` + `VRDynamicLaserPointer` (sets
   `rayTransform` from the active hand/controller and draws the cyan laser).
   Without it, nothing on the screen is clickable.
5. **HandWorkspaceOrigin** — transform at the virtual arm base (0, 1.0, 0.5);
   the IK scheme anchors hand retargeting to it.

## 5. The unified screen (generated)

`OhhoUnifiedAppBuilder.Build()` creates:

- **OhhO Screen** — world-space `Canvas` (1600×1000, scale 0.001),
  `CanvasScaler` (`dynamicPixelsPerUnit = 2` for crisp text), `OVRRaycaster`,
  `WorldSpaceUiPlacer` (distance 1.6, billboard on), `ScreenToggle`.
- **Header** — logo, URL, Open Website button → `OhhoVrApp.OpenWebsite()`.
- **Login / Console / Garage / Teleop pages** — each with its controller
  (`LoginPanelController`, `ConsolePanelController`, `GaragePanelController`,
  `TeleopHudController`) and every serialized field wired.
- **Card prefabs** — `RobotCardPrefab` / `ProductCardPrefab` contents +
  `RobotCardView` / `ProductCardView` wiring.
- **TeleopController wiring** — `gestureDetector`, `handWorkspaceOrigin`,
  `cameraFeed` (the teleop page's `CameraFeedController`), `recorder`.

### 5a. Teleop page layout

| Column | Contents |
|---|---|
| Left | Robot name, connection (IP/port/Connect/Disconnect/status dot), telemetry (pose, velocity, arm, mode), control hints, **< Garage** |
| Center | Camera feed (`CameraFeedController` → MJPEG/WebRTC), camera name, **Next Camera** |
| Right | Dataset recording (Start / Stop & Save / Discard, live status), **Export to Robot** (POSTs JSONL to the VR bridge at `:8765`) |

---

## 6. Run

1. Build & Run to the Quest (or Meta XR Simulator).
2. You see your room (passthrough) with the OhhO glass screen floating ahead.
3. Sign in (email → 6-digit code). On success you land on the **console** showing
   VR products (today **OhhO Pilot**).
4. Open Pilot → the **garage** lists the robots from your OhhO account (pulled from
   Supabase `user_robots`). Empty? Add one on the website/app — it appears here.
5. **Select a robot** → `OhhoVrApp` builds its `RobotProfile` from the catalog and
   hands it to `TeleopController`. The teleop page opens.
   - **Base:** left stick strafe (vx/vy), right stick X yaw, right grip turbo,
     both grips e-stop.
   - **Arm:** right-hand pose → IK → `/arm/joint_commands` @ 20 Hz; pinch →
     gripper; left-hand thumbs-up (hold 0.5 s) toggles arm enable.
   - All topics, joint names and limits come from the profile (OmniBot = SO-101
     6-DOF + mecanum, identical to the old hardcoded constants).

---

## Theme tokens (reference)

`OhhoTheme` mirrors `website/app/globals.css` and is overridden at runtime from
the manifest: bg `#0A0E1A`, surface `#0F1628`, **cyan `#00D4FF`**, **violet
`#7C3AED`** / lite `#A78BFA`. Fonts: Space Grotesk / Inter / JetBrains Mono.
