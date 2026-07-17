using UnityEngine;
using UnityEditor;

public class FixVRPrefabs
{
    [MenuItem("OmniBot/Fix VR Prefabs")]
    public static void FixPrefabs()
    {
        var leftAnchor = GameObject.Find("LeftHandAnchor");
        var rightAnchor = GameObject.Find("RightHandAnchor");

        if (leftAnchor == null || rightAnchor == null)
        {
            Debug.LogError("Could not find Hand Anchors!");
            return;
        }

        // Clean up broken/bad prefabs
        Object.DestroyImmediate(leftAnchor.transform.Find("OVRCustomHandPrefab_L")?.gameObject);
        Object.DestroyImmediate(rightAnchor.transform.Find("OVRCustomHandPrefab_R")?.gameObject);
        Object.DestroyImmediate(leftAnchor.transform.Find("OVRControllerPrefab")?.gameObject);
        Object.DestroyImmediate(rightAnchor.transform.Find("OVRControllerPrefab")?.gameObject);

        // Find standard Meta prefabs
        string[] handGuids = AssetDatabase.FindAssets("OVRHandPrefab t:Prefab");
        string[] controllerGuids = AssetDatabase.FindAssets("OVRControllerPrefab t:Prefab");

        if (handGuids.Length > 0 && controllerGuids.Length > 0)
        {
            var handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(handGuids[0]));
            var controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(controllerGuids[0]));

            // Setup Left Hand
            var leftHand = PrefabUtility.InstantiatePrefab(handPrefab, leftAnchor.transform) as GameObject;
            leftHand.name = "OVRHandPrefab";
            var ovrHandL = leftHand.GetComponent<OVRHand>();
            var ovrSkeletonL = leftHand.GetComponent<OVRSkeleton>();
            var ovrMeshL = leftHand.GetComponent<OVRMesh>();
            if (ovrHandL != null) { var so = new SerializedObject(ovrHandL); so.FindProperty("HandType").enumValueIndex = 1; so.ApplyModifiedProperties(); }
            if (ovrSkeletonL != null) { var so = new SerializedObject(ovrSkeletonL); so.FindProperty("_skeletonType").enumValueIndex = 1; so.ApplyModifiedProperties(); }
            if (ovrMeshL != null) { var so = new SerializedObject(ovrMeshL); so.FindProperty("_meshType").enumValueIndex = 1; so.ApplyModifiedProperties(); }

            var leftController = PrefabUtility.InstantiatePrefab(controllerPrefab, leftAnchor.transform) as GameObject;
            leftController.name = "OVRControllerPrefab";
            var ovrHelperL = leftController.GetComponent<OVRControllerHelper>();
            if (ovrHelperL != null) ovrHelperL.m_controller = OVRInput.Controller.LTouch;

            // Setup Right Hand
            var rightHand = PrefabUtility.InstantiatePrefab(handPrefab, rightAnchor.transform) as GameObject;
            rightHand.name = "OVRHandPrefab";
            var ovrHandR = rightHand.GetComponent<OVRHand>();
            var ovrSkeletonR = rightHand.GetComponent<OVRSkeleton>();
            var ovrMeshR = rightHand.GetComponent<OVRMesh>();
            if (ovrHandR != null) { var so = new SerializedObject(ovrHandR); so.FindProperty("HandType").enumValueIndex = 2; so.ApplyModifiedProperties(); }
            if (ovrSkeletonR != null) { var so = new SerializedObject(ovrSkeletonR); so.FindProperty("_skeletonType").enumValueIndex = 2; so.ApplyModifiedProperties(); }
            if (ovrMeshR != null) { var so = new SerializedObject(ovrMeshR); so.FindProperty("_meshType").enumValueIndex = 2; so.ApplyModifiedProperties(); }

            var rightController = PrefabUtility.InstantiatePrefab(controllerPrefab, rightAnchor.transform) as GameObject;
            rightController.name = "OVRControllerPrefab";
            var ovrHelperR = rightController.GetComponent<OVRControllerHelper>();
            if (ovrHelperR != null) ovrHelperR.m_controller = OVRInput.Controller.RTouch;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Successfully replaced Hand and Controller prefabs with high-quality Meta models.");
        }
        else
        {
            Debug.LogError("Could not find standard OVRHandPrefab or OVRControllerPrefab in the project.");
        }
    }
}
