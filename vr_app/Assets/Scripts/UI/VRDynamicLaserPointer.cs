using UnityEngine;
using UnityEngine.EventSystems;

public class VRDynamicLaserPointer : MonoBehaviour
{
    public OVRInputModule inputModule;
    public Transform rightControllerAnchor;
    public Transform rightHandAnchor;
    private LineRenderer lineRenderer;

    void Start()
    {
        if (inputModule == null && EventSystem.current != null)
        {
            inputModule = EventSystem.current.GetComponent<OVRInputModule>();
        }

        GameObject rightController = GameObject.Find("RightControllerAnchor");
        if (rightController != null) rightControllerAnchor = rightController.transform;

        GameObject rightHand = GameObject.Find("RightHandAnchor");
        if (rightHand != null) rightHandAnchor = rightHand.transform;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.003f;
        lineRenderer.endWidth = 0.003f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0, 1, 1, 0.5f); // Semi-transparent cyan
        lineRenderer.material = mat;
    }

    void Update()
    {
        if (inputModule == null) return;
        
        // Determine if we are using hands or controllers
        bool usingHands = OVRInput.IsControllerConnected(OVRInput.Controller.Hands) && OVRInput.GetActiveController() == OVRInput.Controller.Hands;
        Transform activeAnchor = usingHands ? rightHandAnchor : rightControllerAnchor;

        if (activeAnchor == null) return;

        inputModule.rayTransform = activeAnchor;

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, activeAnchor.position);
            lineRenderer.SetPosition(1, activeAnchor.position + activeAnchor.forward * 1.5f);
        }
    }
}
