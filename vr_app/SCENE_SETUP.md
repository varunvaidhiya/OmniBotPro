# OhhO VR — Scene Assembly Guide

How to wire the Phase 0a–0c scripts into a working Quest 3 mixed-reality scene.
Scenes, prefabs, materials and TMP font assets are binary Unity assets created in
the editor; this guide is the spec for building them from the committed C# +
shader. Once assembled, the app runs the flow **sign in → console → garage**, all
in passthrough, themed like the website.

> Prerequisite: open `vr_app/` in Unity 2023.3 LTS and let packages resolve
> (`Packages/manifest.json`: Meta XR SDK 60, OpenXR, TMP, Newtonsoft, NativeWebSocket).

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

1. Import **Space Grotesk**, **Inter**, **JetBrains Mono** (TTFs) and make a
   **TMP Font Asset** for each (Window → TextMeshPro → Font Asset Creator).
2. **Create → OhhO → Font Set** → assign display = Space Grotesk, body = Inter,
   mono = JetBrains Mono. This is the `OhhoFontSet` the `ThemeApplier` publishes.

## 3. Bootstrap GameObject ("OhhoApp")

One persistent GameObject holding the platform services (each is a singleton):

| Component | Inspector wiring |
|---|---|
| `OhhoPlatform` | `platformBaseUrl = https://ohho-robotics.com`; offline resource `vr_manifest` |
| `OhhoCatalog` | offline resource `vr_catalog` |
| `SupabaseAuthService` | (configured at runtime from the manifest) |
| `GarageClient` | (configured at runtime from the manifest) |
| `OhhoVrApp` | assign `loginPanel`, `consolePanel`, `garagePanel` (§5) + the services above |

`OhhoVrApp` configures `SupabaseAuthService`/`GarageClient` from the manifest on
load and restores a saved session, so returning users skip login.

## 4. Camera rig + passthrough

1. Add the Meta **OVRCameraRig** (or XR Origin) — this provides the HMD camera and
   hands.
2. Add a child with the Meta **OVRPassthroughLayer** component (Underlay).
3. Add **`PassthroughManager`**: assign `hmdCamera` = CenterEye camera and
   `passthroughLayer` = the OVRPassthroughLayer (a Behaviour). On start it makes
   the camera transparent and enables passthrough.

## 5. UI panels (world-space, glass, themed)

Make three **world-space Canvas** panels: **Login**, **Console**, **Garage**.
On each panel root:

- **`WorldSpaceUiPlacer`** (`hmdCamera` = CenterEye) — floats it in front of the user.
- A background **Image** + **`GlassPanel`** (accent Cyan) — the frosted card.
- **`ThemeApplier`** at the UI root (assign the `OhhoFontSet`) — restyles all
  themed widgets and re-applies when the manifest theme loads.

Label/text elements use **`ThemedText`** with a role:
`Heading` (Space Grotesk), `Body`/`Muted` (Inter), `Label` (cyan JetBrains Mono
micro-labels). Primary buttons use **`AccentButton`** (PrimaryCyan).

### 5a. Login panel
- Email `TMP_InputField`, "Send code" `Button`, code `TMP_InputField` (+ its
  parent `codeStep` GameObject, hidden initially), "Verify" `Button`, status `TMP_Text`.
- Add **`LoginPanelController`** and wire those fields.

### 5b. Console panel
- A grid (`GridLayoutGroup`/`HorizontalLayoutGroup`) as `gridParent`, a header `TMP_Text`.
- **Product card prefab**: a `GlassPanel` card with name/tag/desc `ThemedText` +
  an accent `Image` + a `Button`; add **`ProductCardView`** and wire them.
- Add **`ConsolePanelController`**; assign `app` (OhhoVrApp), `gridParent`,
  `cardPrefab`, `headerText`.

### 5c. Garage panel
- A list container `listParent`, status `TMP_Text`, optional "Refresh" `Button`.
- **Robot card prefab**: name + model `ThemedText`, a category `Image` dot, a
  `Button`; add **`RobotCardView`**.
- Add **`GaragePanelController`** (or **`FleetPanelController`** for the enhanced
  fleet view with quick-resume + status badges); assign `listParent`, `cardPrefab`,
  `statusText`, `refreshButton`.
- An **"Add Robot"** `Button` — assign `addRobotButton`. Opens the selection flow.

### 5c-bis. Add-robot selection flow (Phase 1)
A five-step spatial wizard mirroring the website's `RobotSelector`:
**categories → types → models → name → confirm**. One world-space panel with five
child step panels, three card prefabs, and a `RobotSelectionController`.

1. Create a world-space Canvas **"SelectionPanel"** (hidden by default) with five
   child GameObjects: `CategoryStep`, `TypeStep`, `ModelStep`, `NameStep`,
   `ConfirmStep`. Each has a list container (`Transform`) for its cards.
2. Add **`RobotSelectionController`** and assign:
   - The five step panels + their list containers (`categoryListParent`,
     `typeListParent`, `modelListParent`).
   - Three card prefabs: **`CategoryCardView`** (label + blurb + color dot),
     **`TypeCardView`** (name + tagline), **`ModelCardView`** (name +
     manufacturer + specs summary).
   - `backButton`, `cancelButton`, `stepHeader` (TMP_Text).
   - Name step: `nameInput` (TMP_InputField) + `nameNextButton`.
   - Confirm step: `confirmSummary` (TMP_Text) + `confirmButton`.
   - `statusText` for errors/success.
3. On the **garage/fleet panel**, assign `selectionController` → this controller
   and `selectionPanel` → the SelectionPanel. The Add-Robot button opens it;
   `OnRobotAdded` closes it and reloads the garage.

On confirm, `RobotSelectionController` calls `GarageClient.AddUserRobot`, which
writes a row to Supabase `user_robots` — the same table the website/app use — so
the new robot immediately appears in the garage ready to teleoperate.

### 5d. Teleop controller (Phase 2)
The teleop control layer — receives the selected robot's profile and pumps VR
input → drive + hand-IK → ROS every frame.

1. Create a persistent GameObject **"TeleopController"** (child of the OhhoApp
   bootstrap, or its own DontDestroyOnLoad object).
2. Add **`TeleopController`** and wire:
   - `gestureDetector` → the scene `GestureDetector` (auto-found if omitted).
   - `handWorkspaceOrigin` → a Transform at the robot arm's base position
     (0.35 m above the robot base by default). The IK scheme anchors hand
     retargeting to this point.
   - `linkProvider` → optional; leave null to use the `RosBridgeLink` singleton
     (wraps `ROSBridgeClient`).
 3. On the **`OhhoVrApp`** bootstrap component, assign the new fields:
   - `garagePanelController` → the scene `GaragePanelController` (or `fleetPanelController` for the enhanced fleet view with quick-resume).
   - `teleopController` → the `TeleopController` above.
   - `onboarding` → the scene `OnboardingController` (optional; shows first-run hints).
4. **Camera feed** — add a `CameraFeedController` to a world-space panel with a
   `RawImage` + `TMP_Text` overlay. Assign `displayImage` + `cameraNameOverlay`.
   On `TeleopController`, assign the `cameraFeed` field so it gets configured
   with the robot's camera topics on `StartTeleop`.
5. **Recording** — add a `ProfileDrivenRecorder` MonoBehaviour. Assign it on
   `TeleopController.recorder` so it subscribes to the profile's topics.
6. **Onboarding panel** — a world-space Canvas with drive/manip/safety `TMP_Text`
   fields + the `OnboardingController` component. Dismissed by B button or a
   dismiss button; only shows once (reset via `OnboardingController.Reset()`).

`OhhoVrApp` subscribes to `GaragePanelController.RobotPicked`: when the user
selects a robot it builds a `RobotProfile` (`RobotProfileFactory.FromGarageRobot`
— drive kind + arm DOF + joint limits + ROS topics from the catalog), applies
per-robot calibration (`CalibrationManager.Load` + `ApplyTo`), saves it as the
last robot (`LastRobotStore.Save` for quick-resume), calls
`TeleopController.StartTeleop(profile)`, routes to the teleop view, and shows
the onboarding hints on the first session.

The legacy `Input/BaseController.cs` + `Input/HandTrackingArmController.cs`
MonoBehaviours are superseded by `TeleopController` — remove them from the scene
(or disable them) to avoid double-publishing `/cmd_vel/teleop` and
`/arm/joint_commands`.

## 6. Run

1. Build & Run to the Quest (or Meta XR Simulator).
2. You see your room (passthrough) with the OhhO glass login panel floating ahead.
3. Sign in (email → 6-digit code). On success you land on the **console** showing
   VR products (today **OhhO Pilot**).
4. Open Pilot → the **garage** lists the robots from your OhhO account (pulled from
   Supabase `user_robots`). Empty? Add one on the website/app — it appears here.
5. **Select a robot** → `OhhoVrApp` builds its `RobotProfile` from the catalog and
   hands it to `TeleopController`. The app-shell panels hide; the HUD takes over.
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
