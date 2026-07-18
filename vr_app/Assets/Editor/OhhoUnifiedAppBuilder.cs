using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OmniBot.VR.App;
using OmniBot.VR.Control;
using OmniBot.VR.Core;
using OmniBot.VR.Input;
using OmniBot.VR.MR;
using OmniBot.VR.Recording;
using OmniBot.VR.UI;
using OmniBot.VR.UI.Auth;
using OmniBot.VR.UI.Console;
using OmniBot.VR.UI.Garage;
using OmniBot.VR.Video;

namespace OmniBot.VR.Editor
{
    /// <summary>
    /// Rebuilds the app as ONE unified world-space screen facing the user.
    ///
    /// What it fixes / builds:
    ///   1. Hand tracking — skeleton root pose + scale + bone translations, and
    ///      updateWhenOffscreen on the hand meshes (the misalignment + single-eye
    ///      rendering bugs).
    ///   2. UI interaction — VRDynamicLaserPointer on the EventSystem (the
    ///      OVRInputModule had no ray origin, so nothing was clickable).
    ///   3. ONE screen — deletes the broken UI Root (scale 0) and the outward-curved
    ///      three-panel MR_UI_Root mockup, and builds a single 1600x1000 canvas with
    ///      the full flow inside it: Login → Console → Garage → Teleop. A
    ///      WorldSpaceUiPlacer keeps it parked in front of the user, always facing them.
    ///   4. Wires every controller (OhhoVrApp routing, TeleopController, card prefabs,
    ///      camera feed, recorder, graphics boost).
    /// </summary>
    public static class OhhoUnifiedAppBuilder
    {
        // ── OhhO palette (mirrors website/app/globals.css) ─────────────────────
        private static readonly Color Bg       = new Color(0.039f, 0.055f, 0.102f, 0.96f);
        private static readonly Color Surface  = new Color(0.059f, 0.086f, 0.157f, 0.98f);
        private static readonly Color Surface2 = new Color(0.086f, 0.118f, 0.20f, 1f);
        private static readonly Color Cyan     = new Color(0.000f, 0.831f, 1.000f);
        private static readonly Color Violet   = new Color(0.486f, 0.231f, 0.929f);
        private static readonly Color TextMain = new Color(0.95f, 0.97f, 1.00f);
        private static readonly Color TextMut  = new Color(0.58f, 0.64f, 0.72f);
        private static readonly Color Danger   = new Color(0.85f, 0.25f, 0.25f);

        private const float CanvasW = 1600f;
        private const float CanvasH = 1000f;
        private const float HeaderH = 88f;

        [MenuItem("OmniBot/Rebuild Unified OhhO Screen")]
        public static void Build()
        {
            FixHands();
            FixEventSystem();
            DeleteOldUi();
            BuildCardPrefabs();
            var screen = BuildScreen();
            WireApp(screen);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[OhhO] Unified screen built — ONE panel, facing the user, fully wired.");
        }

        // ════════════════════════════════════════════════════════════════════
        // 1. Hands
        // ════════════════════════════════════════════════════════════════════
        private static void FixHands()
        {
            foreach (var sk in Object.FindObjectsOfType<OVRCustomSkeleton>())
            {
                var so = new SerializedObject(sk);
                so.FindProperty("_updateRootPose").boolValue = true;
                so.FindProperty("_updateRootScale").boolValue = true;
                var abt = so.FindProperty("_applyBoneTranslations");
                if (abt != null) abt.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(sk);
            }
            foreach (var smr in Object.FindObjectsOfType<SkinnedMeshRenderer>())
            {
                if (smr.name == "l_handMeshNode" || smr.name == "r_handMeshNode")
                {
                    smr.updateWhenOffscreen = true;
                    EditorUtility.SetDirty(smr);
                }
            }
            var rig = GameObject.Find("OVRCameraRig");
            if (rig != null)
            {
                var mgr = rig.GetComponent<OVRManager>();
                if (mgr != null) mgr.trackingOriginType = OVRManager.TrackingOrigin.Stage;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 2. Event system / pointer
        // ════════════════════════════════════════════════════════════════════
        private static void FixEventSystem()
        {
            var es = GameObject.Find("EventSystem");
            if (es == null) return;
            if (es.GetComponent<VRDynamicLaserPointer>() == null)
                es.AddComponent<VRDynamicLaserPointer>();

            var module = es.GetComponent<UnityEngine.EventSystems.OVRInputModule>();
            var rightAnchor = GameObject.Find("RightControllerAnchor");
            if (module != null && rightAnchor != null && module.rayTransform == null)
                module.rayTransform = rightAnchor.transform;
        }

        // ════════════════════════════════════════════════════════════════════
        // 3. Remove the broken / mockup UI
        // ════════════════════════════════════════════════════════════════════
        private static void DeleteOldUi()
        {
            var uiRoot = GameObject.Find("UI Root");
            if (uiRoot != null) Object.DestroyImmediate(uiRoot);
            var mrRoot = GameObject.Find("MR_UI_Root");
            if (mrRoot != null) Object.DestroyImmediate(mrRoot);
            // Idempotent rebuilds: remove the previous unified screen too.
            var screen = GameObject.Find("OhhO Screen");
            if (screen != null) Object.DestroyImmediate(screen);
        }

        // ════════════════════════════════════════════════════════════════════
        // 4. Card prefabs (visuals + wiring)
        // ════════════════════════════════════════════════════════════════════
        private static void BuildCardPrefabs()
        {
            BuildRobotCardPrefab("Assets/RobotCardPrefab.prefab");
            BuildProductCardPrefab("Assets/ProductCardPrefab.prefab");
        }

        private static void BuildRobotCardPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (Transform child in root.transform) Object.DestroyImmediate(child.gameObject);

            var rt = root.GetComponent<RectTransform>();
            if (rt == null) rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1420, 92);

            var bg = root.GetComponent<Image>();
            if (bg == null) bg = root.AddComponent<Image>();
            bg.color = Surface2;
            var btn = root.GetComponent<Button>();
            if (btn == null) btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;

            var dot = MakeImage("CategoryDot", root.transform, new Color(0, 0.83f, 1f));
            Stretch(dot.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 0), new Vector2(10, 0));

            var name = MakeText("NameText", root.transform, "Robot", 30, TextMain, TextAlignmentOptions.MidlineLeft);
            Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(1300, 44), new Vector2(44, 20));
            var model = MakeText("ModelText", root.transform, "Model", 22, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(model.rectTransform, new Vector2(0, 0.5f), new Vector2(1300, 36), new Vector2(44, -20));

            var view = root.GetComponent<RobotCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("modelText").objectReferenceValue = model;
            so.FindProperty("categoryDot").objectReferenceValue = dot;
            so.FindProperty("button").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void BuildProductCardPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            foreach (Transform child in root.transform) Object.DestroyImmediate(child.gameObject);

            var rt = root.GetComponent<RectTransform>();
            if (rt == null) rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480, 560);

            var bg = root.GetComponent<Image>();
            if (bg == null) bg = root.AddComponent<Image>();
            bg.color = Surface2;
            var btn = root.GetComponent<Button>();
            if (btn == null) btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;

            var accent = MakeImage("AccentBar", root.transform, Cyan);
            Stretch(accent.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 6), Vector2.zero);

            var name = MakeText("NameText", root.transform, "Product", 38, TextMain, TextAlignmentOptions.TopLeft);
            Place(name.rectTransform, new Vector2(0, 1), new Vector2(432, 90), new Vector2(24, -20));
            var tag = MakeText("TagText", root.transform, "tag", 24, Cyan, TextAlignmentOptions.TopLeft);
            Place(tag.rectTransform, new Vector2(0, 1), new Vector2(432, 44), new Vector2(24, -118));
            var desc = MakeText("DescText", root.transform, "description", 22, TextMut, TextAlignmentOptions.TopLeft);
            desc.enableWordWrapping = true;
            Place(desc.rectTransform, new Vector2(0, 0), new Vector2(432, 390), new Vector2(24, 24));

            var view = root.GetComponent<ProductCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("tagText").objectReferenceValue = tag;
            so.FindProperty("descText").objectReferenceValue = desc;
            so.FindProperty("accentBar").objectReferenceValue = accent;
            so.FindProperty("button").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ════════════════════════════════════════════════════════════════════
        // 5. The unified screen
        // ════════════════════════════════════════════════════════════════════
        private static GameObject BuildScreen()
        {
            var screen = new GameObject("OhhO Screen", typeof(RectTransform));
            screen.transform.position = new Vector3(0f, 1.4f, 1.6f);
            screen.transform.localScale = Vector3.one * 0.001f;

            var canvas = screen.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = screen.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 2f; // crisper text in world space
            screen.AddComponent<OVRRaycaster>();

            var rootRt = (RectTransform)screen.transform;
            rootRt.sizeDelta = new Vector2(CanvasW, CanvasH);

            var placer = screen.AddComponent<WorldSpaceUiPlacer>();
            var cam = GameObject.Find("CenterEyeAnchor")?.GetComponent<Camera>();
            var pso = new SerializedObject(placer);
            pso.FindProperty("hmdCamera").objectReferenceValue = cam;
            pso.FindProperty("distance").floatValue = 1.6f;
            pso.FindProperty("verticalOffset").floatValue = -0.1f;
            pso.FindProperty("placeOnEnable").boolValue = true;
            pso.FindProperty("billboard").boolValue = true;
            pso.ApplyModifiedPropertiesWithoutUndo();

            // B button (left controller) toggles screen visibility — readme controls.
            screen.AddComponent<ScreenToggle>();

            // Background card
            var bg = screen.AddComponent<Image>();
            bg.color = Bg;

            BuildHeader(screen.transform);
            var login = BuildLoginPage(screen.transform);
            var console = BuildConsolePage(screen.transform);
            var garage = BuildGaragePage(screen.transform);
            var teleop = BuildTeleopPage(screen.transform);

            console.SetActive(false);
            garage.SetActive(false);
            teleop.SetActive(false);

            screen.tag = "Untagged";
            screen.SetActive(true);
            return screen;
        }

        private static void BuildHeader(Transform parent)
        {
            var header = MakePanel("Header", parent, Surface);
            Stretch(header.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, HeaderH), Vector2.zero);

            var logo = MakeText("Logo", header.transform, "OHHO", 46, Cyan, TextAlignmentOptions.MidlineLeft);
            logo.fontStyle = FontStyles.Bold;
            Stretch(logo.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(240, 0), new Vector2(36, 0));

            var url = MakeText("Url", header.transform, "ohho-robotics.com", 24, TextMut, TextAlignmentOptions.MidlineLeft);
            Stretch(url.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(400, 0), new Vector2(280, 0));

            var websiteBtn = MakeButton("WebsiteButton", header.transform, "Open Website", 24, Cyan, Color.black);
            Stretch(websiteBtn.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(240, 56), new Vector2(-20, 0));
            // wired to OhhoVrApp.OpenWebsite in WireApp
        }

        // ── Login ─────────────────────────────────────────────────────────────
        private static GameObject BuildLoginPage(Transform parent)
        {
            var page = MakePage("Login Page", parent);

            var title = MakeText("Title", page.transform, "Sign in to OhhO", 44, TextMain, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 70), new Vector2(0, -60));

            var sub = MakeText("Subtitle", page.transform, "One account across web, Android and VR — your garage is already here.", 24, TextMut, TextAlignmentOptions.Center);
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(1100, 40), new Vector2(0, -140));

            var email = MakeInputField("Email Input", page.transform, "you@example.com", 640, 64);
            Place(email.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(640, 64), new Vector2(0, -260));

            var send = MakeButton("Send Code Button", page.transform, "Send Code", 26, Cyan, Color.black);
            Place(send.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(320, 64), new Vector2(0, -360));

            var codeStep = new GameObject("Code Step", typeof(RectTransform));
            codeStep.transform.SetParent(page.transform, false);
            Stretch((RectTransform)codeStep.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var code = MakeInputField("Code Input", codeStep.transform, "6-digit code", 640, 64);
            Place(code.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(640, 64), new Vector2(0, -480));
            var verify = MakeButton("Verify Button", codeStep.transform, "Verify", 26, Cyan, Color.black);
            Place(verify.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(320, 64), new Vector2(0, -580));

            var status = MakeText("Status Text", page.transform, "Sign in to OhhO", 24, TextMut, TextAlignmentOptions.Center);
            Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(1200, 44), new Vector2(0, 60));

            var ctl = page.AddComponent<LoginPanelController>();
            var so = new SerializedObject(ctl);
            so.FindProperty("emailField").objectReferenceValue = email;
            so.FindProperty("sendCodeButton").objectReferenceValue = send;
            so.FindProperty("codeStep").objectReferenceValue = codeStep;
            so.FindProperty("codeField").objectReferenceValue = code;
            so.FindProperty("verifyButton").objectReferenceValue = verify;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // ── Console ───────────────────────────────────────────────────────────
        private static GameObject BuildConsolePage(Transform parent)
        {
            var page = MakePage("Console Page", parent);

            var header = MakeText("Header Text", page.transform, "Your VR consoles", 40, TextMain, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 60), new Vector2(0, -50));

            var grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(page.transform, false);
            var grt = (RectTransform)grid.transform;
            Place(grt, new Vector2(0.5f, 0.5f), new Vector2(1500, 600), Vector2.zero);
            var hlg = grid.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 32;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var cardPrefab = AssetDatabase.LoadAssetAtPath<ProductCardView>("Assets/ProductCardPrefab.prefab");
            var ctl = page.AddComponent<ConsolePanelController>();
            var so = new SerializedObject(ctl);
            so.FindProperty("gridParent").objectReferenceValue = grid.transform;
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("headerText").objectReferenceValue = header;
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // ── Garage ────────────────────────────────────────────────────────────
        private static GameObject BuildGaragePage(Transform parent)
        {
            var page = MakePage("Garage Page", parent);

            var title = MakeText("Title", page.transform, "Your garage", 40, TextMain, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(800, 60), new Vector2(60, -44));

            var refresh = MakeButton("Refresh Button", page.transform, "Refresh", 24, Surface2, TextMain);
            Place(refresh.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(220, 56), new Vector2(-60, -44));

            var status = MakeText("Status Text", page.transform, "Loading your garage…", 24, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(status.rectTransform, new Vector2(0, 1), new Vector2(1400, 40), new Vector2(60, -120));

            var hint = MakeText("Hint", page.transform, "Add robots on ohho-robotics.com or the Android app — they appear here automatically.", 20, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(hint.rectTransform, new Vector2(0, 1), new Vector2(1400, 34), new Vector2(60, -164));

            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(page.transform, false);
            var lrt = (RectTransform)list.transform;
            Place(lrt, new Vector2(0.5f, 0f), new Vector2(1480, 660), new Vector2(0, 40));
            var vlg = list.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var cardPrefab = AssetDatabase.LoadAssetAtPath<RobotCardView>("Assets/RobotCardPrefab.prefab");
            var ctl = page.AddComponent<GaragePanelController>();
            var so = new SerializedObject(ctl);
            so.FindProperty("listParent").objectReferenceValue = list.transform;
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("refreshButton").objectReferenceValue = refresh;
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // ── Teleop ────────────────────────────────────────────────────────────
        private static GameObject BuildTeleopPage(Transform parent)
        {
            var page = MakePage("Teleop Page", parent);

            // ── Left column: robot + connection + telemetry ──
            var left = MakePanel("Left Column", page.transform, new Color(0, 0, 0, 0));
            Place(left.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(500, 880), new Vector2(20, 0));

            var robotName = MakeText("Robot Name", left.transform, "Robot", 36, Cyan, TextAlignmentOptions.MidlineLeft);
            robotName.fontStyle = FontStyles.Bold;
            Place(robotName.rectTransform, new Vector2(0, 1), new Vector2(480, 50), new Vector2(20, -30));

            var connTitle = MakeText("Conn Title", left.transform, "CONNECTION", 20, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(connTitle.rectTransform, new Vector2(0, 1), new Vector2(480, 30), new Vector2(20, -100));

            var ip = MakeInputField("IP Field", left.transform, "192.168.1.101", 300, 56);
            Place(ip.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(300, 56), new Vector2(20, -150));
            var port = MakeInputField("Port Field", left.transform, "9090", 140, 56);
            Place(port.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(140, 56), new Vector2(340, -150));

            var connect = MakeButton("Connect Button", left.transform, "Connect", 24, Cyan, Color.black);
            Place(connect.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(230, 56), new Vector2(20, -225));
            var disconnect = MakeButton("Disconnect Button", left.transform, "Disconnect", 24, Surface2, TextMain);
            Place(disconnect.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(230, 56), new Vector2(270, -225));

            var dot = MakeImage("Connection Dot", left.transform, Danger);
            Place(dot.rectTransform, new Vector2(0, 1), new Vector2(22, 22), new Vector2(24, -300));
            var connStatus = MakeText("Connection Status", left.transform, "Disconnected", 22, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(connStatus.rectTransform, new Vector2(0, 1), new Vector2(400, 30), new Vector2(60, -296));

            var telTitle = MakeText("Tel Title", left.transform, "TELEMETRY", 20, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(telTitle.rectTransform, new Vector2(0, 1), new Vector2(480, 30), new Vector2(20, -360));
            var pose = MakeText("Pose Text", left.transform, "x —   y —", 22, TextMain, TextAlignmentOptions.MidlineLeft);
            Place(pose.rectTransform, new Vector2(0, 1), new Vector2(480, 30), new Vector2(20, -400));
            var vel = MakeText("Velocity Text", left.transform, "vx —   vy —   ω —", 22, TextMain, TextAlignmentOptions.MidlineLeft);
            Place(vel.rectTransform, new Vector2(0, 1), new Vector2(480, 30), new Vector2(20, -440));
            var arm = MakeText("Arm Text", left.transform, "arm: —", 22, TextMain, TextAlignmentOptions.MidlineLeft);
            Place(arm.rectTransform, new Vector2(0, 1), new Vector2(480, 30), new Vector2(20, -480));
            var mode = MakeText("Mode Text", left.transform, "mode: —", 22, TextMain, TextAlignmentOptions.MidlineLeft);
            Place(mode.rectTransform, new Vector2(0, 1), new Vector2(480, 30), new Vector2(20, -520));

            var hints = MakeText("Hints Text", left.transform, "", 19, TextMut, TextAlignmentOptions.TopLeft);
            hints.enableWordWrapping = true;
            Place(hints.rectTransform, new Vector2(0, 0), new Vector2(480, 180), new Vector2(20, 120));

            var back = MakeButton("Back Button", left.transform, "< Garage", 24, Violet, TextMain);
            Place(back.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(220, 56), new Vector2(20, 20));

            // ── Center column: camera feed ──
            var center = MakePanel("Center Column", page.transform, new Color(0, 0, 0, 0));
            Place(center.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(600, 880), Vector2.zero);

            var camPanel = MakePanel("Camera Panel", center.transform, Color.black);
            Place(camPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(600, 420), new Vector2(0, 40));
            var feed = new GameObject("Camera Feed", typeof(RectTransform));
            feed.transform.SetParent(camPanel.transform, false);
            var feedRt = (RectTransform)feed.transform;
            Stretch(feedRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var raw = feed.AddComponent<RawImage>();
            raw.color = new Color(0.02f, 0.03f, 0.06f, 1f); // dark until the stream starts

            var camName = MakeText("Camera Name", center.transform, "camera", 20, TextMut, TextAlignmentOptions.Center);
            Place(camName.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600, 30), new Vector2(0, 270));
            var cycle = MakeButton("Cycle Camera Button", center.transform, "Next Camera", 22, Surface2, TextMain);
            Place(cycle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(240, 52), new Vector2(0, -230));

            var feedCtl = center.AddComponent<CameraFeedController>();
            var fso = new SerializedObject(feedCtl);
            fso.FindProperty("displayImage").objectReferenceValue = raw;
            fso.FindProperty("cameraNameOverlay").objectReferenceValue = camName;
            fso.ApplyModifiedPropertiesWithoutUndo();

            // ── Right column: recording ──
            var right = MakePanel("Right Column", page.transform, new Color(0, 0, 0, 0));
            Place(right.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(400, 880), new Vector2(-20, 0));

            var recTitle = MakeText("Rec Title", right.transform, "DATASET RECORDING", 20, TextMut, TextAlignmentOptions.MidlineLeft);
            Place(recTitle.rectTransform, new Vector2(0, 1), new Vector2(380, 30), new Vector2(20, -30));

            var recDot = MakeImage("Recording Dot", right.transform, new Color(0.45f, 0.45f, 0.5f));
            Place(recDot.rectTransform, new Vector2(0, 1), new Vector2(22, 22), new Vector2(22, -84));
            var recStatus = MakeText("Recording Status", right.transform, "Idle", 22, TextMain, TextAlignmentOptions.MidlineLeft);
            Place(recStatus.rectTransform, new Vector2(0, 1), new Vector2(340, 30), new Vector2(58, -80));

            var startRec = MakeButton("Start Recording", right.transform, "Start Recording", 24, Cyan, Color.black);
            Place(startRec.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(360, 60), new Vector2(20, -140));
            var stopSave = MakeButton("Stop & Save", right.transform, "Stop & Save", 24, Surface2, TextMain);
            Place(stopSave.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(360, 60), new Vector2(20, -215));
            var discard = MakeButton("Discard", right.transform, "Discard", 24, Danger, TextMain);
            Place(discard.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(360, 60), new Vector2(20, -290));

            var export = MakeButton("Export Button", right.transform, "Export to Robot", 24, Surface2, TextMain);
            Place(export.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(360, 60), new Vector2(20, -365));
            var exportStatus = MakeText("Export Status", right.transform, "", 19, TextMut, TextAlignmentOptions.TopLeft);
            exportStatus.enableWordWrapping = true;
            Place(exportStatus.rectTransform, new Vector2(0, 1), new Vector2(360, 60), new Vector2(20, -440));
            exportStatus.gameObject.SetActive(false);

            var recHint = MakeText("Rec Hint", right.transform, "Episodes save to the headset and export to the robot for training.", 19, TextMut, TextAlignmentOptions.TopLeft);
            recHint.enableWordWrapping = true;
            Place(recHint.rectTransform, new Vector2(0, 1), new Vector2(360, 120), new Vector2(20, -520));

            // ── TeleopHudController wiring ──
            var hud = page.AddComponent<TeleopHudController>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("ipField").objectReferenceValue = ip;
            hso.FindProperty("portField").objectReferenceValue = port;
            hso.FindProperty("connectButton").objectReferenceValue = connect;
            hso.FindProperty("disconnectButton").objectReferenceValue = disconnect;
            hso.FindProperty("connectionDot").objectReferenceValue = dot;
            hso.FindProperty("connectionStatusText").objectReferenceValue = connStatus;
            hso.FindProperty("poseText").objectReferenceValue = pose;
            hso.FindProperty("velocityText").objectReferenceValue = vel;
            hso.FindProperty("armText").objectReferenceValue = arm;
            hso.FindProperty("modeText").objectReferenceValue = mode;
            hso.FindProperty("cycleCameraButton").objectReferenceValue = cycle;
            hso.FindProperty("cameraFeed").objectReferenceValue = feedCtl;
            hso.FindProperty("startRecordingButton").objectReferenceValue = startRec;
            hso.FindProperty("stopSaveButton").objectReferenceValue = stopSave;
            hso.FindProperty("discardButton").objectReferenceValue = discard;
            hso.FindProperty("recordingStatusText").objectReferenceValue = recStatus;
            hso.FindProperty("recordingDot").objectReferenceValue = recDot;
            hso.FindProperty("exportButton").objectReferenceValue = export;
            hso.FindProperty("exportStatusText").objectReferenceValue = exportStatus;
            hso.FindProperty("robotNameText").objectReferenceValue = robotName;
            hso.FindProperty("hintsText").objectReferenceValue = hints;
            hso.FindProperty("backButton").objectReferenceValue = back;
            hso.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // ════════════════════════════════════════════════════════════════════
        // 6. Wire the app bootstrap
        // ════════════════════════════════════════════════════════════════════
        private static void WireApp(GameObject screen)
        {
            var bootstrap = GameObject.Find("[OhhO VR App]");
            if (bootstrap == null)
            {
                Debug.LogError("[OhhO] '[OhhO VR App]' bootstrap not found — cannot wire.");
                return;
            }

            // Services that must exist
            if (bootstrap.GetComponent<GestureDetector>() == null) bootstrap.AddComponent<GestureDetector>();
            if (bootstrap.GetComponent<ProfileDrivenRecorder>() == null) bootstrap.AddComponent<ProfileDrivenRecorder>();
            if (bootstrap.GetComponent<VRGraphicsBoost>() == null) bootstrap.AddComponent<VRGraphicsBoost>();

            // Hand workspace origin (arm IK anchor — readme §6: 0.35 m above robot base)
            var workspace = GameObject.Find("HandWorkspaceOrigin");
            if (workspace == null)
            {
                workspace = new GameObject("HandWorkspaceOrigin");
                workspace.transform.position = new Vector3(0f, 1.0f, 0.5f);
            }

            var login = screen.transform.Find("Login Page")?.gameObject;
            var console = screen.transform.Find("Console Page")?.gameObject;
            var garage = screen.transform.Find("Garage Page")?.gameObject;
            var teleop = screen.transform.Find("Teleop Page")?.gameObject;

            var app = bootstrap.GetComponent<OhhoVrApp>();
            var appSo = new SerializedObject(app);
            appSo.FindProperty("loginPanel").objectReferenceValue = login;
            appSo.FindProperty("consolePanel").objectReferenceValue = console;
            appSo.FindProperty("garagePanel").objectReferenceValue = garage;
            appSo.FindProperty("teleopPanel").objectReferenceValue = teleop;
            appSo.FindProperty("garagePanelController").objectReferenceValue =
                garage != null ? garage.GetComponent<GaragePanelController>() : null;
            appSo.ApplyModifiedPropertiesWithoutUndo();

            // Console controller needs the app reference
            var consoleCtl = console != null ? console.GetComponent<ConsolePanelController>() : null;
            if (consoleCtl != null)
            {
                var cso = new SerializedObject(consoleCtl);
                cso.FindProperty("app").objectReferenceValue = app;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }

            // Website button in the header → ohho-robotics.com
            var websiteBtn = screen.transform.Find("Header/WebsiteButton")?.GetComponent<Button>();
            if (websiteBtn != null)
            {
                websiteBtn.onClick.RemoveAllListeners();
                websiteBtn.onClick.AddListener(app.OpenWebsite);
                EditorUtility.SetDirty(websiteBtn);
            }

            // TeleopController wiring
            var teleopCtl = bootstrap.GetComponent<TeleopController>();
            var tso = new SerializedObject(teleopCtl);
            tso.FindProperty("gestureDetector").objectReferenceValue = bootstrap.GetComponent<GestureDetector>();
            tso.FindProperty("handWorkspaceOrigin").objectReferenceValue = workspace.transform;
            tso.FindProperty("cameraFeed").objectReferenceValue =
                teleop != null ? teleop.GetComponentInChildren<CameraFeedController>(true) : null;
            tso.FindProperty("recorder").objectReferenceValue = bootstrap.GetComponent<ProfileDrivenRecorder>();
            tso.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(bootstrap);
        }

        // ════════════════════════════════════════════════════════════════════
        // UI construction helpers
        // ════════════════════════════════════════════════════════════════════
        private static GameObject MakePage(string name, Transform parent)
        {
            var page = new GameObject(name, typeof(RectTransform));
            page.transform.SetParent(parent, false);
            var rt = (RectTransform)page.transform;
            Stretch(rt, Vector2.zero, Vector2.one, new Vector2(0, -HeaderH), Vector2.zero);
            return page;
        }

        private static GameObject MakePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = color.a > 0.01f;
            return go;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static Button MakeButton(string name, Transform parent, string label, float fontSize, Color bgColor, Color labelColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var txt = MakeText("Text", go.transform, label, fontSize, labelColor, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return btn;
        }

        private static TMP_InputField MakeInputField(string name, Transform parent, string placeholder, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width, height);
            var img = go.AddComponent<Image>();
            img.color = Surface2;
            var field = go.AddComponent<TMP_InputField>();

            var viewport = new GameObject("Text Area", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var vrt = (RectTransform)viewport.transform;
            Stretch(vrt, Vector2.zero, Vector2.one, new Vector2(-16, -6), new Vector2(16, 6));
            viewport.AddComponent<RectMask2D>();

            var text = MakeText("Text", viewport.transform, "", 24, TextMain, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var ph = MakeText("Placeholder", viewport.transform, placeholder, 24, TextMut, TextAlignmentOptions.MidlineLeft);
            ph.fontStyle = FontStyles.Italic;
            Stretch(ph.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            field.textViewport = vrt;
            field.textComponent = text;
            field.placeholder = ph;
            return field;
        }

        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            // Pivot follows the anchor point on non-stretched axes so `pos` is the
            // element's edge offset from the anchor (no center-pivot overflow).
            rt.pivot = new Vector2(min.x == max.x ? min.x : 0.5f, min.y == max.y ? min.y : 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            // Pivot == anchor: `pos` is measured from the anchor corner to the
            // element's matching corner — left-anchored content never overflows left.
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
