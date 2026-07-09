using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OmniBot.VR.Editor
{
    public class VRInteractionFixer
    {
        [MenuItem("OmniBot/Fix VR UI Interaction")]
        public static void Fix()
        {
            // 1. Swap StandaloneInputModule to OVRInputModule
            var es = Object.FindObjectOfType<EventSystem>();
            if (es != null) 
            {
                var old = es.GetComponent<StandaloneInputModule>();
                if (old) Object.DestroyImmediate(old);
                
                var ovrInput = es.GetComponent<OVRInputModule>();
                if (ovrInput == null) ovrInput = es.gameObject.AddComponent<OVRInputModule>();
                
                // JoyPadClickButton is usually One (A button) or PrimaryIndexTrigger
                ovrInput.joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger;
                
                // 2. Find RightControllerAnchor to act as the pointer
                var rightAnchor = GameObject.Find("RightControllerAnchor");
                if (rightAnchor != null) 
                {
                    ovrInput.rayTransform = rightAnchor.transform;
                    
                    if (rightAnchor.GetComponent<VRLaserPointer>() == null)
                        rightAnchor.AddComponent<VRLaserPointer>();
                }
                else
                {
                    Debug.LogWarning("[OmniBot] Could not find RightControllerAnchor. Make sure OVRCameraRig is in the scene!");
                }
            }
            
            // 3. Swap GraphicRaycaster to OVRRaycaster on the UI Root Canvas
            var canvases = Object.FindObjectsOfType<Canvas>();
            foreach(var c in canvases) 
            {
                var oldRay = c.GetComponent<GraphicRaycaster>();
                if (oldRay) Object.DestroyImmediate(oldRay);
                
                if (c.GetComponent<OVRRaycaster>() == null) 
                {
                    c.gameObject.AddComponent<OVRRaycaster>();
                }
            }
            
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[OmniBot] Success! VR UI Interaction fixed. Rebuild the app!");
        }
    }
}
