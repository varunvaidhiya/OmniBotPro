using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OmniBot.VR.Editor
{
    public class BuildAutomator
    {
        [MenuItem("OmniBot/1-Click Save & Add To Build")]
        public static void SaveAndAddToBuild()
        {
            var scene = EditorSceneManager.GetActiveScene();
            
            // If the scene hasn't been saved yet, save it to Assets/
            string path = scene.path;
            if (string.IsNullOrEmpty(path))
            {
                bool saved = EditorSceneManager.SaveScene(scene, "Assets/OmniBotVR_Main.unity");
                if (saved) path = "Assets/OmniBotVR_Main.unity";
                else 
                {
                    Debug.LogError("Failed to save scene!");
                    return;
                }
            }
            
            // Add the current scene to the Build Settings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(path, true)
            };
            
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[OmniBot] Success! The scene was saved to " + path + " and added to your Build Settings.");
            
            // Bring up the build window so they can just hit Build
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }
    }
}
