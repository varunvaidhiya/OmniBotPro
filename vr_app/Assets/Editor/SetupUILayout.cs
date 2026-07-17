using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class SetupUILayout
{
    [MenuItem("OmniBot/Setup UI Layout")]
    public static void Setup()
    {
        var root = GameObject.Find("UI Root");
        if (root == null) { Debug.Log("UI Root not found"); return; }
        
        root.transform.position = new Vector3(0, 1.2f, 1.5f);
        root.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        root.transform.rotation = Quaternion.identity;
        
        var login = root.transform.Find("Login Panel");
        if (login != null)
        {
            login.localPosition = Vector3.zero;
            login.localRotation = Quaternion.identity;
            login.localScale = Vector3.one;
        }

        var console = root.transform.Find("Console Panel");
        if (console != null)
        {
            // Moved further left, pushed back slightly on Z to form curve, rotated inward (negative Y)
            console.localPosition = new Vector3(-1100f, 0f, 300f);
            console.localEulerAngles = new Vector3(0, -35f, 0);
            console.localScale = Vector3.one;
        }

        var garage = root.transform.Find("Garage Panel");
        if (garage != null)
        {
            // Moved further right, pushed back slightly on Z to form curve, rotated inward (positive Y)
            garage.localPosition = new Vector3(1100f, 0f, 300f);
            garage.localEulerAngles = new Vector3(0, 35f, 0);
            garage.localScale = Vector3.one;
        }

        var canvas = root.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("UI Layout Setup Completed successfully.");
    }
}
