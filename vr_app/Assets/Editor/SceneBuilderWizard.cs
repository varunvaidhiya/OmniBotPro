using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OmniBot.VR.App;
using OmniBot.VR.Core;
using OmniBot.VR.Core.Platform;
using OmniBot.VR.Control;
using OmniBot.VR.UI.Auth;
using OmniBot.VR.UI.Console;
using OmniBot.VR.UI.Garage;
using OmniBot.VR.UI.Selection;

namespace OmniBot.VR.Editor
{
    public class SceneBuilderWizard : ScriptableWizard
    {
        [MenuItem("OmniBot/Generate VR Scene")]
        public static void CreateScene()
        {
            // 1. Create a new Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "OmniBotVR_Main";

            // 2. Try to find and instantiate OVRCameraRig
            GameObject cameraRig = null;
            string[] guids = AssetDatabase.FindAssets("t:Prefab OVRCameraRig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Object prefab = AssetDatabase.LoadAssetAtPath<Object>(path);
                cameraRig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                cameraRig = new GameObject("FallbackCameraRig");
                var cam = new GameObject("Main Camera");
                cam.transform.SetParent(cameraRig.transform);
                cam.AddComponent<Camera>();
            }

            // 3. Create Root Application Object
            GameObject appRoot = new GameObject("[OhhO VR App]");
            var app = appRoot.AddComponent<OhhoVrApp>();

            // Services
            var platform = appRoot.AddComponent<OhhoPlatform>();
            var auth = appRoot.AddComponent<SupabaseAuthService>();
            var garageClient = appRoot.AddComponent<GarageClient>();
            var catalog = appRoot.AddComponent<OhhoCatalog>();
            var connMgr = appRoot.AddComponent<ConnectionManager>();

            // Controllers
            var teleopController = appRoot.AddComponent<TeleopController>();

            // 4. Create World Space UI Canvas Root
            GameObject uiRoot = new GameObject("UI Root");
            var canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            uiRoot.AddComponent<CanvasScaler>();
            uiRoot.AddComponent<GraphicRaycaster>();
            uiRoot.transform.position = new Vector3(0, 1.5f, 2.0f); // 2 meters in front
            uiRoot.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            
            // 5. Create Panels
            GameObject loginPanel = CreateLoginPanel(uiRoot.transform, out LoginPanelController loginController);
            GameObject consolePanel = CreateConsolePanel(uiRoot.transform, out ConsolePanelController consoleController);
            GameObject garagePanel = CreateGaragePanel(uiRoot.transform, out GaragePanelController garageController);

            // 6. Wire up OhhoVrApp fields
            SerializedObject appSo = new SerializedObject(app);
            appSo.FindProperty("loginPanel").objectReferenceValue = loginPanel;
            appSo.FindProperty("consolePanel").objectReferenceValue = consolePanel;
            appSo.FindProperty("garagePanel").objectReferenceValue = garagePanel;

            appSo.FindProperty("platform").objectReferenceValue = platform;
            appSo.FindProperty("auth").objectReferenceValue = auth;
            appSo.FindProperty("garage").objectReferenceValue = garageClient;

            appSo.FindProperty("garagePanelController").objectReferenceValue = garageController;
            appSo.FindProperty("teleopController").objectReferenceValue = teleopController;
            appSo.ApplyModifiedProperties();

            // Setup Console Panel
            SerializedObject consoleSo = new SerializedObject(consoleController);
            consoleSo.FindProperty("app").objectReferenceValue = app;
            consoleSo.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[OmniBot] Successfully generated VR Scene! Please save it and adjust the UI layouts.");
        }

        private static GameObject CreateLoginPanel(Transform parent, out LoginPanelController controller)
        {
            GameObject panel = new GameObject("Login Panel");
            panel.transform.SetParent(parent, false);
            controller = panel.AddComponent<LoginPanelController>();

            var emailField = CreateInputField(panel.transform, "Email Input");
            var sendCodeBtn = CreateButton(panel.transform, "Send Code Button");
            var codeStep = new GameObject("Code Step");
            codeStep.transform.SetParent(panel.transform, false);
            var codeField = CreateInputField(codeStep.transform, "Code Input");
            var verifyBtn = CreateButton(codeStep.transform, "Verify Button");
            var statusText = CreateText(panel.transform, "Status Text");

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("emailField").objectReferenceValue = emailField.GetComponent<TMP_InputField>();
            so.FindProperty("sendCodeButton").objectReferenceValue = sendCodeBtn.GetComponent<Button>();
            so.FindProperty("codeStep").objectReferenceValue = codeStep;
            so.FindProperty("codeField").objectReferenceValue = codeField.GetComponent<TMP_InputField>();
            so.FindProperty("verifyButton").objectReferenceValue = verifyBtn.GetComponent<Button>();
            so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateConsolePanel(Transform parent, out ConsolePanelController controller)
        {
            GameObject panel = new GameObject("Console Panel");
            panel.transform.SetParent(parent, false);
            controller = panel.AddComponent<ConsolePanelController>();

            var grid = new GameObject("Grid");
            grid.transform.SetParent(panel.transform, false);
            grid.AddComponent<GridLayoutGroup>();
            var header = CreateText(panel.transform, "Header Text");

            var dummyPrefab = CreateProductCardPrefab();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("gridParent").objectReferenceValue = grid.transform;
            so.FindProperty("headerText").objectReferenceValue = header.GetComponent<TextMeshProUGUI>();
            so.FindProperty("cardPrefab").objectReferenceValue = dummyPrefab;
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateGaragePanel(Transform parent, out GaragePanelController controller)
        {
            GameObject panel = new GameObject("Garage Panel");
            panel.transform.SetParent(parent, false);
            controller = panel.AddComponent<GaragePanelController>();

            var list = new GameObject("List");
            list.transform.SetParent(panel.transform, false);
            list.AddComponent<VerticalLayoutGroup>();
            
            var status = CreateText(panel.transform, "Status Text");
            var refreshBtn = CreateButton(panel.transform, "Refresh Button");
            var addBtn = CreateButton(panel.transform, "Add Robot Button");

            var selectionPanel = new GameObject("Selection Panel");
            selectionPanel.transform.SetParent(panel.transform, false);
            var selectionController = selectionPanel.AddComponent<RobotSelectionController>();

            var dummyPrefab = CreateRobotCardPrefab();

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("listParent").objectReferenceValue = list.transform;
            so.FindProperty("cardPrefab").objectReferenceValue = dummyPrefab;
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<TextMeshProUGUI>();
            so.FindProperty("refreshButton").objectReferenceValue = refreshBtn.GetComponent<Button>();
            so.FindProperty("addRobotButton").objectReferenceValue = addBtn.GetComponent<Button>();
            so.FindProperty("selectionController").objectReferenceValue = selectionController;
            so.FindProperty("selectionPanel").objectReferenceValue = selectionPanel;
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateInputField(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<TMP_InputField>();
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>();
            go.AddComponent<Button>();
            return go;
        }

        private static GameObject CreateText(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<TextMeshProUGUI>();
            return go;
        }

        private static ProductCardView CreateProductCardPrefab()
        {
            GameObject go = new GameObject("ProductCardPrefab");
            var comp = go.AddComponent<ProductCardView>();
            string path = "Assets/ProductCardPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ProductCardView>();
        }

        private static RobotCardView CreateRobotCardPrefab()
        {
            GameObject go = new GameObject("RobotCardPrefab");
            var comp = go.AddComponent<RobotCardView>();
            string path = "Assets/RobotCardPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<RobotCardView>();
        }
    }
}
