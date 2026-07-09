using UnityEngine;

namespace OmniBot.VR
{
    public class VRLaserPointer : MonoBehaviour
    {
        private LineRenderer _line;

        void Start()
        {
            _line = gameObject.GetComponent<LineRenderer>();
            if (_line == null)
            {
                _line = gameObject.AddComponent<LineRenderer>();
            }
            
            _line.startWidth = 0.005f;
            _line.endWidth = 0.005f;
            
            // Use an unlit material
            Material unlit = new Material(Shader.Find("Hidden/Internal-Colored"));
            unlit.hideFlags = HideFlags.HideAndDontSave;
            unlit.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            unlit.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            unlit.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            unlit.SetInt("_ZWrite", 0);
            
            _line.material = unlit;
            _line.startColor = Color.cyan;
            _line.endColor = Color.cyan;
            _line.positionCount = 2;
        }

        void Update()
        {
            _line.SetPosition(0, transform.position);
            
            // Just shoot the laser forward 10 meters so we can see it
            // OVRInputModule handles the actual UI clicking internally!
            _line.SetPosition(1, transform.position + transform.forward * 10f);
        }
    }
}
