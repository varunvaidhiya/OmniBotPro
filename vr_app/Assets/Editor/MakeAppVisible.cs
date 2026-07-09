using UnityEditor;
using UnityEngine;

public class MakeAppVisible
{
    [MenuItem("OmniBot/Add Test Objects to Scene")]
    public static void AddTestObjects()
    {
        // Create a giant cube 3 meters in front of the origin
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "VR Test Cube";
        cube.transform.position = new Vector3(0, 1.5f, 3f);
        cube.transform.localScale = new Vector3(1f, 1f, 1f); // 1 meter wide cube
        
        // Make it Red
        var renderer = cube.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = Color.red;
        renderer.material = mat;

        // Create a floor plane
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "VR Test Floor";
        plane.transform.position = new Vector3(0, 0, 0);
        plane.transform.localScale = new Vector3(2f, 2f, 2f); // 20m x 20m floor

        Debug.Log("[OmniBot] Test objects added! Look for a giant red cube 3 meters in front of you.");
    }
}
