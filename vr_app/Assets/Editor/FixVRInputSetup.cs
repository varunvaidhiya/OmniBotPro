using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FixVRInputSetup : MonoBehaviour
{
    [MenuItem("OmniBot/Fix VR Input")]
    public static void FixInput()
    {
        // 1. Get EventSystem and OVRInputModule
        EventSystem es = Object.FindObjectOfType<EventSystem>();
        if (es != null)
        {
            OVRInputModule inputModule = es.GetComponent<OVRInputModule>();
            if (inputModule != null)
            {
                // Find RightControllerAnchor
                GameObject rightAnchor = GameObject.Find("RightControllerAnchor");
                if (rightAnchor != null)
                {
                    inputModule.rayTransform = rightAnchor.transform;
                    Debug.Log("Set OVRInputModule rayTransform to RightControllerAnchor.");
                    
                    // Add a simple laser LineRenderer
                    LineRenderer lr = rightAnchor.GetComponent<LineRenderer>();
                    if (lr == null)
                    {
                        lr = rightAnchor.AddComponent<LineRenderer>();
                    }
                    lr.startWidth = 0.005f;
                    lr.endWidth = 0.005f;
                    lr.useWorldSpace = false;
                    lr.positionCount = 2;
                    lr.SetPosition(0, Vector3.zero);
                    lr.SetPosition(1, new Vector3(0, 0, 1.0f)); // 1 meter forward
                    
                    Material mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = Color.cyan;
                    lr.material = mat;
                    
                    Debug.Log("Added LineRenderer to RightControllerAnchor.");
                }
            }
        }
    }
}
