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
- Add **`GaragePanelController`**; assign `listParent`, `cardPrefab`, `statusText`,
  `refreshButton`.

## 6. Run

1. Build & Run to the Quest (or Meta XR Simulator).
2. You see your room (passthrough) with the OhhO glass login panel floating ahead.
3. Sign in (email → 6-digit code). On success you land on the **console** showing
   VR products (today **OhhO Pilot**).
4. Open Pilot → the **garage** lists the robots from your OhhO account (pulled from
   Supabase `user_robots`). Empty? Add one on the website/app — it appears here.

Selecting a robot is wired to a hand-off point; the teleop control layer (drive +
hand-IK) lands in **Phase 2** (see `MULTI_ROBOT_TELEOP_ARCHITECTURE.md` §5).

---

## Theme tokens (reference)

`OhhoTheme` mirrors `website/app/globals.css` and is overridden at runtime from
the manifest: bg `#0A0E1A`, surface `#0F1628`, **cyan `#00D4FF`**, **violet
`#7C3AED`** / lite `#A78BFA`. Fonts: Space Grotesk / Inter / JetBrains Mono.
