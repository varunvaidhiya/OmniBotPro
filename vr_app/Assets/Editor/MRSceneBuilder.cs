using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using OmniBot.VR.MR;
using OmniBot.VR.Core;

namespace OmniBot.VR.Editor
{
    public class MRSceneBuilder : EditorWindow
    {
        [MenuItem("OmniBot/Generate Mixed Reality App Scene")]
        public static void GenerateScene()
        {
            // 1. High Resolution Settings for Quest 3
            QualitySettings.antiAliasing = 4;
            if (UnityEngine.XR.XRSettings.enabled)
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f;

            // 2. OVR Camera Rig & Passthrough Setup
            var rig = GameObject.Find("OVRCameraRig");
            if (rig == null)
            {
                var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
                if (rigPrefab != null) {
                    rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
                } else {
                    rig = new GameObject("OVRCameraRig");
                    var camera = new GameObject("TrackingSpace");
                    camera.transform.SetParent(rig.transform);
                    var centerEye = new GameObject("CenterEyeAnchor");
                    centerEye.transform.SetParent(camera.transform);
                    centerEye.AddComponent<Camera>();
                }
            }

            var cam = rig.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0);
            }

            // OVRManager Passthrough Support
            var manager = rig.GetComponent<OVRManager>();
            if (manager != null)
            {
                manager.isInsightPassthroughEnabled = true;
            }

            // OVRPassthroughLayer
            var ptLayer = rig.GetComponent<OVRPassthroughLayer>();
            if (ptLayer == null) ptLayer = rig.AddComponent<OVRPassthroughLayer>();
            ptLayer.projectionSurfaceType = OVRPassthroughLayer.ProjectionSurfaceType.Reconstructed;
            ptLayer.overlayType = OVROverlay.OverlayType.Underlay;

            if (!rig.GetComponent<PassthroughManager>()) rig.AddComponent<PassthroughManager>();

            // 3. Setup Hands and Controllers
            var leftHandAnchor = rig.transform.Find("TrackingSpace/LeftHandAnchor");
            var rightHandAnchor = rig.transform.Find("TrackingSpace/RightHandAnchor");

            if (leftHandAnchor != null && rightHandAnchor != null)
            {
                // Instantiate OVRControllerPrefab
                var controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRControllerPrefab.prefab");
                if (controllerPrefab != null)
                {
                    if (leftHandAnchor.Find("OVRControllerPrefab") == null) {
                        var lc = (GameObject)PrefabUtility.InstantiatePrefab(controllerPrefab, leftHandAnchor);
                        lc.GetComponent<OVRControllerHelper>().m_controller = OVRInput.Controller.LTouch;
                    }
                    if (rightHandAnchor.Find("OVRControllerPrefab") == null) {
                        var rc = (GameObject)PrefabUtility.InstantiatePrefab(controllerPrefab, rightHandAnchor);
                        rc.GetComponent<OVRControllerHelper>().m_controller = OVRInput.Controller.RTouch;
                    }
                }

                // Instantiate OVRCustomHandPrefab
                var leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCustomHandPrefab_L.prefab");
                var rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCustomHandPrefab_R.prefab");
                
                if (leftHandPrefab != null && leftHandAnchor.Find(leftHandPrefab.name) == null) {
                    PrefabUtility.InstantiatePrefab(leftHandPrefab, leftHandAnchor);
                }
                if (rightHandPrefab != null && rightHandAnchor.Find(rightHandPrefab.name) == null) {
                    PrefabUtility.InstantiatePrefab(rightHandPrefab, rightHandAnchor);
                }
            }

            // 3. Event System
            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var eventObj = new GameObject("EventSystem");
                es = eventObj.AddComponent<EventSystem>();
                eventObj.AddComponent<OVRInputModule>();
            }

            // 4. Managers
            var managerRoot = new GameObject("AppManagers");
            managerRoot.AddComponent<ROSConnectionWrapper>();
            managerRoot.AddComponent<DirectConnectionWrapper>();

            // 5. Curved UI Layout
            var uiRoot = new GameObject("MR_UI_Root");
            uiRoot.transform.position = new Vector3(0, 1.2f, 1.5f); // 1.5m in front of user

            // Center Panel (Web Mockup)
            var centerPanel = CreateUIPanel("Center_WebMockup", uiRoot.transform, new Vector3(0, 0, 0), new Vector3(0, 0, 0), 800, 600);
            AddTextToPanel(centerPanel, "OhhO Robotics (Web Portal)", 32, true);
            AddMockupBrowser(centerPanel);
            
            // Left Panel (Connection & Telemetry)
            var leftPanel = CreateUIPanel("Left_RobotControl", uiRoot.transform, new Vector3(-0.9f, 0, 0.2f), new Vector3(0, 25, 0), 600, 600);
            AddTextToPanel(leftPanel, "Robot Control", 28, true);
            AddConnectionUI(leftPanel);

            // Right Panel (Recording & Camera)
            var rightPanel = CreateUIPanel("Right_Dataset", uiRoot.transform, new Vector3(0.9f, 0, 0.2f), new Vector3(0, -25, 0), 600, 600);
            AddTextToPanel(rightPanel, "Dataset & Camera", 28, true);
            AddRecordingUI(rightPanel);

            // Attach OVRRaycaster to all canvases
            foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            {
                if (!canvas.GetComponent<OVRRaycaster>()) canvas.gameObject.AddComponent<OVRRaycaster>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[OmniBot MR] Mixed Reality Spatial Scene Generated Successfully!");
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Vector3 localPos, Vector3 eulerAngles, float width, float height)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            panel.transform.localPosition = localPos;
            panel.transform.localEulerAngles = eulerAngles;
            panel.transform.localScale = Vector3.one * 0.002f; // Scale down to VR size

            var canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            panel.AddComponent<CanvasScaler>();
            
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f); // Dark translucent glass

            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            // Layout Group
            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;

            return panel;
        }

        private static void AddTextToPanel(GameObject panel, string text, int fontSize, bool isHeader = false)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isHeader ? Color.white : new Color(0.8f, 0.8f, 0.8f);
        }

        private static void AddMockupBrowser(GameObject panel)
        {
            var browserBg = new GameObject("BrowserBG");
            browserBg.transform.SetParent(panel.transform, false);
            var img = browserBg.AddComponent<Image>();
            img.color = Color.white;
            var rect = browserBg.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(760, 480);
            
            var text = new GameObject("BrowserText");
            text.transform.SetParent(browserBg.transform, false);
            var tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "<b>www.ohhorobotics.com</b>\n\nLogin to Access Cloud Services\n[ Username ]\n[ Password ]\n\n(Select ROS or Direct Connection below)";
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            
            var toggleRoot = new GameObject("ToggleGroup");
            toggleRoot.transform.SetParent(panel.transform, false);
            var hl = toggleRoot.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 20;
            hl.childAlignment = TextAnchor.MiddleCenter;
            
            AddButton(toggleRoot, "Use ROS 2 Mode");
            AddButton(toggleRoot, "Use Direct Mode");
        }

        private static void AddConnectionUI(GameObject panel)
        {
            AddTextToPanel(panel, "IP Address: 192.168.1.101", 24);
            AddButton(panel, "Connect");
            AddButton(panel, "Disconnect");
            
            AddTextToPanel(panel, "\nTelemetry", 24, true);
            AddTextToPanel(panel, "Battery: 100%\nSpeed: 0.0 m/s\nStatus: Idle", 20);
        }

        private static void AddRecordingUI(GameObject panel)
        {
            var camBox = new GameObject("CameraView");
            camBox.transform.SetParent(panel.transform, false);
            var img = camBox.AddComponent<Image>();
            img.color = Color.gray;
            camBox.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 315);
            AddTextToPanel(camBox, "Camera Feed\n(No Signal)", 24);

            AddTextToPanel(panel, "\nDataset Recording", 24, true);
            AddButton(panel, "Start Recording");
            AddButton(panel, "Stop & Save");
        }

        private static void AddButton(GameObject parent, string text)
        {
            var btnObj = new GameObject("Button_" + text);
            btnObj.transform.SetParent(parent.transform, false);
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.8f);
            var btn = btnObj.AddComponent<Button>();
            
            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }
    }
}
