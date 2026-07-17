using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class PopulateUI
{
    [MenuItem("OmniBot/Populate UI")]
    public static void Populate()
    {
        var root = GameObject.Find("UI Root");
        if (root == null) return;

        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // --- 1. Login Panel (Center) ---
        var login = root.transform.Find("Login Panel");
        if (login != null)
        {
            AddBackground(login.gameObject, new Color(0.1f, 0.1f, 0.15f, 0.85f));
            AddText(login.gameObject, "OhhO VR Login", defaultFont, 48, new Vector2(0, 180));
            
            var userInput = AddInputField(login.gameObject, "Username", defaultFont, new Vector2(0, 50));
            var passInput = AddInputField(login.gameObject, "Password", defaultFont, new Vector2(0, -50));
            var loginBtn = AddButton(login.gameObject, "Login", defaultFont, new Vector2(0, -150));
        }

        // --- 2. Console Panel (Left) ---
        var console = root.transform.Find("Console Panel");
        if (console != null)
        {
            AddBackground(console.gameObject, new Color(0.1f, 0.15f, 0.1f, 0.85f));
            AddText(console.gameObject, "Robot Telemetry & Connection", defaultFont, 42, new Vector2(0, 200));
            
            var ipInput = AddInputField(console.gameObject, "Robot IP (e.g. 192.168.1.100)", defaultFont, new Vector2(0, 100));
            
            var connectBtn = AddButton(console.gameObject, "Connect ROS", defaultFont, new Vector2(-120, 0));
            var disconnectBtn = AddButton(console.gameObject, "Disconnect", defaultFont, new Vector2(120, 0));

            var telemetryBg = new GameObject("TelemetryBg");
            telemetryBg.transform.SetParent(console, false);
            var rect = telemetryBg.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 200);
            rect.anchoredPosition = new Vector2(0, -150);
            var img = telemetryBg.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.5f);

            AddText(telemetryBg, "Status: Disconnected\nBattery: --\nControl Mode: None", defaultFont, 24, Vector2.zero, TextAnchor.UpperLeft);
        }

        // --- 3. Garage Panel (Right) ---
        var garage = root.transform.Find("Garage Panel");
        if (garage != null)
        {
            AddBackground(garage.gameObject, new Color(0.15f, 0.1f, 0.1f, 0.85f));
            AddText(garage.gameObject, "Robot Camera Stream", defaultFont, 42, new Vector2(0, 220));
            
            var viewer = new GameObject("CameraViewer");
            viewer.transform.SetParent(garage, false);
            var rect = viewer.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(640, 360); // 16:9 aspect
            rect.anchoredPosition = new Vector2(0, 0);
            var rawImg = viewer.AddComponent<RawImage>();
            rawImg.color = Color.black;
            
            AddText(viewer, "No Video Signal", defaultFont, 32, Vector2.zero);

            var startRec = AddButton(garage.gameObject, "Start Recording", defaultFont, new Vector2(-150, -220));
            var stopRec = AddButton(garage.gameObject, "Stop Recording", defaultFont, new Vector2(150, -220));
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("UI Successfully Populated!");
    }

    private static void AddBackground(GameObject parent, Color color)
    {
        var img = parent.GetComponent<Image>();
        if (img == null) img = parent.AddComponent<Image>();
        img.color = color;
    }

    private static GameObject AddText(GameObject parent, string txt, Font font, int size, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(600, size * 2);
        
        var textComp = go.AddComponent<Text>();
        textComp.text = txt;
        textComp.font = font;
        textComp.fontSize = size;
        textComp.color = Color.white;
        textComp.alignment = align;
        
        if (align == TextAnchor.UpperLeft)
        {
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(-280, 80); // padding inside bg
        }
        return go;
    }

    private static GameObject AddInputField(GameObject parent, string placeholder, Font font, Vector2 pos)
    {
        var bg = new GameObject("InputField");
        bg.transform.SetParent(parent.transform, false);
        var rect = bg.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 60);
        var img = bg.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.9f);

        var input = bg.AddComponent<InputField>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(bg.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0); textRect.offsetMax = new Vector2(-10, 0);
        var textComp = textGo.AddComponent<Text>();
        textComp.font = font;
        textComp.fontSize = 28;
        textComp.color = Color.black;
        textComp.alignment = TextAnchor.MiddleLeft;

        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(bg.transform, false);
        var phRect = phGo.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 0); phRect.offsetMax = new Vector2(-10, 0);
        var phComp = phGo.AddComponent<Text>();
        phComp.text = placeholder;
        phComp.font = font;
        phComp.fontSize = 28;
        phComp.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        phComp.alignment = TextAnchor.MiddleLeft;

        input.textComponent = textComp;
        input.placeholder = phComp;
        return bg;
    }

    private static GameObject AddButton(GameObject parent, string label, Font font, Vector2 pos)
    {
        var bg = new GameObject("Button_" + label);
        bg.transform.SetParent(parent.transform, false);
        var rect = bg.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(250, 60);
        var img = bg.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.8f, 1f);

        var btn = bg.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(bg.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        var textComp = textGo.AddComponent<Text>();
        textComp.text = label;
        textComp.font = font;
        textComp.fontSize = 28;
        textComp.color = Color.white;
        textComp.alignment = TextAnchor.MiddleCenter;

        return bg;
    }
}
