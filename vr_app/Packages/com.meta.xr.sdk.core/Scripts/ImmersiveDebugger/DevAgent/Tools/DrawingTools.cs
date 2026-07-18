/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using Meta.XR.ImmersiveDebugger;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Drawing tools for visualizing GameObjects with axis and bounding box gizmos
    /// through the Immersive Debugger's existing gizmo system.
    ///
    /// This tool creates MonoBehaviour components that provide real-time gizmo data
    /// for GameObjects by name, working properly with both visible and invisible objects.
    /// Uses RuntimeAPIs.AddInspector to register components with the Immersive Debugger.
    /// </summary>
    [Tool(
        Description = "Drawing tools for visualizing GameObjects with axis and bounding box gizmos using the Immersive Debugger's native gizmo system. " +
                      "All methods automatically shows the Immersive Debugger Inspector panel for gizmo visibility control. " +
                      "Works with both visible and invisible GameObjects - can highlight hidden objects. " +
                      "Users should check the Inspector panel under 'Drawing Tools' category to toggle gizmo visibility."
    )]
    internal class DrawingTools : SingletonService<DrawingTools>
    {
        // Track active drawings for management
        private readonly Dictionary<string, List<GameObject>> _activeDrawings = new Dictionary<string, List<GameObject>>();
        private GameObject _drawingContainer;

        private const string DRAWING_CATEGORY = "Drawing Tools";

        private void EnsureDrawingContainer()
        {
            if (_drawingContainer == null)
            {
                _drawingContainer = new GameObject("[DrawingTools Container]");
                // Make it persist across scene loads
                UnityEngine.Object.DontDestroyOnLoad(_drawingContainer);
            }
        }

        [Tool(Description = "Draw axis gizmo to show the pose (position + rotation) of a GameObject by name." +
                            "Use this to visualize hidden objects or objects without any Renderer or Collider",
              Returns = "Status message with operation result.")]
        public string DrawAxis(string gameObjectName)
        {
            try
            {
                // Check if target object exists (including inactive ones)
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                GameObject targetObject = allObjects.FirstOrDefault(obj => obj.name == gameObjectName);
                if (targetObject == null)
                {
                    return $"Error: GameObject '{gameObjectName}' not found in scene. Make sure the name is correct and the object exists (including inactive objects).";
                }

                // Ensure inspector panel is visible for gizmo rendering
                EnsureInspectorPanelVisible();

                EnsureDrawingContainer();

                // Create a gizmo provider component for this axis
                GameObject gizmoObject = new GameObject($"AxisGizmo_{gameObjectName}");
                gizmoObject.transform.SetParent(_drawingContainer.transform);

                var axisProvider = gizmoObject.AddComponent<AxisGizmoProvider>();
                axisProvider.Setup(gameObjectName);

                // Register the component with the Immersive Debugger Inspector
                var registrationResult = RuntimeAPIs.AddInspectorItemWithGizmo(
                    DRAWING_CATEGORY,
                    "Axis",
                    "white", // doesn't matter
                    gizmoObject.name,
                    nameof(AxisGizmoProvider),
                    "AxisTransform"
                );

                // Track the active drawing
                TrackDrawing(gameObjectName, gizmoObject);

                if (registrationResult.Status == RuntimeAPIResult.Success)
                {
                    return $"Successfully added axis gizmo for '{gameObjectName}'. " +
                           $"Gizmo is ENABLED BY DEFAULT and should be immediately visible in scene view. " +
                           $"Check Inspector under '{DRAWING_CATEGORY}' category to toggle on/off. " +
                           $"Works even if object becomes invisible. Registration: {registrationResult.Message}";
                }
                else
                {
                    return $"Successfully created axis gizmo for '{gameObjectName}', but Inspector registration had issues: " +
                           $"{registrationResult.Message}. The gizmo may still work but might not appear in Inspector. " +
                           $"Try refreshing the Inspector or restarting the Immersive Debugger.";
                }
            }
            catch (Exception ex)
            {
                return $"Error creating axis gizmo for '{gameObjectName}': {ex.Message}";
            }
        }

        [Tool(Description = "Draw bounding box gizmo around a GameObject's bounds with customizable color (with color name or hex code)." +
                            "The object should ideally have Renderer or Collider to calculate the bounds.",
              Returns = "Status message with operation result.")]
        public string DrawBoundingBox(string gameObjectName, string color = "red")
        {
            try
            {
                // Check if target object exists (including inactive ones)
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                GameObject targetObject = allObjects.FirstOrDefault(obj => obj.name == gameObjectName);
                if (targetObject == null)
                {
                    return $"Error: GameObject '{gameObjectName}' not found in scene. Make sure the name is correct and the object exists (including inactive objects).";
                }

                // Ensure inspector panel is visible for gizmo rendering
                EnsureInspectorPanelVisible();

                EnsureDrawingContainer();

                // Create a gizmo provider component for this bounding box
                GameObject gizmoObject = new GameObject($"BoundingBoxGizmo_{gameObjectName}");
                gizmoObject.transform.SetParent(_drawingContainer.transform);

                var boxProvider = gizmoObject.AddComponent<BoundingBoxGizmoProvider>();
                boxProvider.Setup(gameObjectName);

                // Register the component with the Immersive Debugger Inspector
                var registrationResult = RuntimeAPIs.AddInspectorItemWithGizmo(
                    DRAWING_CATEGORY,
                    "Box",
                    color,
                    gizmoObject.name,
                    nameof(BoundingBoxGizmoProvider),
                    "BoundingBoxData"
                );

                // Track the active drawing
                TrackDrawing(gameObjectName, gizmoObject);

                if (registrationResult.Status == RuntimeAPIResult.Success)
                {
                    return $"Successfully added bounding box gizmo for '{gameObjectName}'. " +
                           $"Color: {color}. Gizmo is ENABLED BY DEFAULT and should be immediately visible in scene view. " +
                           $"Check Inspector under '{DRAWING_CATEGORY}' category to toggle on/off. " +
                           $"Works even if object becomes invisible. Registration: {registrationResult.Message}";
                }
                else
                {
                    return $"Successfully created bounding box gizmo for '{gameObjectName}', but Inspector registration had issues: " +
                           $"{registrationResult.Message}. The gizmo may still work but might not appear in Inspector. " +
                           $"Try refreshing the Inspector or restarting the Immersive Debugger.";
                }
            }
            catch (Exception ex)
            {
                return $"Error creating bounding box gizmo for '{gameObjectName}': {ex.Message}";
            }
        }

        [Tool(Description = "Remove all drawings for a specific GameObject, or all drawings if no GameObject specified.",
              Returns = "Status message with cleanup results.")]
        public string ClearDrawings(string gameObjectName = null)
        {
            try
            {
                int removedCount = 0;
                var keysToRemove = new List<string>();

                if (string.IsNullOrEmpty(gameObjectName))
                {
                    // Clear all drawings
                    foreach (var kvp in _activeDrawings)
                    {
                        foreach (var gizmoObject in kvp.Value)
                        {
                            if (gizmoObject != null)
                            {
                                UnityEngine.Object.DestroyImmediate(gizmoObject);
                                removedCount++;
                            }
                        }
                        keysToRemove.Add(kvp.Key);
                    }
                }
                else
                {
                    // Clear drawings for specific GameObject
                    if (_activeDrawings.ContainsKey(gameObjectName))
                    {
                        foreach (var gizmoObject in _activeDrawings[gameObjectName])
                        {
                            if (gizmoObject != null)
                            {
                                UnityEngine.Object.DestroyImmediate(gizmoObject);
                                removedCount++;
                            }
                        }
                        keysToRemove.Add(gameObjectName);
                    }
                }

                // Clean up tracking
                foreach (var key in keysToRemove)
                {
                    _activeDrawings.Remove(key);
                }

                if (removedCount > 0)
                {
                    return $"Successfully removed {removedCount} drawing(s). " +
                           $"Gizmos have been destroyed and will no longer appear in the Inspector.";
                }
                else
                {
                    return gameObjectName == null ?
                        "No active drawings found to clear." :
                        $"No active drawings found for GameObject '{gameObjectName}'.";
                }
            }
            catch (Exception ex)
            {
                return $"Error clearing drawings: {ex.Message}";
            }
        }

        [Tool(Description = "Get the current status of all active drawings and their Inspector entries.",
              Returns = "List of currently active drawings and their states.")]
        public string GetDrawingStatus()
        {
            try
            {
                if (_activeDrawings.Count == 0)
                {
                    return "No active drawings found.";
                }

                var status = new System.Text.StringBuilder();
                status.AppendLine("Active Drawing Tools:");
                status.AppendLine();

                foreach (var kvp in _activeDrawings)
                {
                    string objectName = kvp.Key;
                    var gizmoObjects = kvp.Value;

                    status.AppendLine($"  {objectName}:");

                    // List gizmo types
                    var gizmoTypes = new List<string>();
                    foreach (var gizmoObj in gizmoObjects)
                    {
                        if (gizmoObj != null)
                        {
                            if (gizmoObj.GetComponent<AxisGizmoProvider>() != null)
                                gizmoTypes.Add("axis");
                            if (gizmoObj.GetComponent<BoundingBoxGizmoProvider>() != null)
                                gizmoTypes.Add("boundingbox");
                        }
                    }
                    status.AppendLine($"    Active gizmos: {string.Join(", ", gizmoTypes)}");

                    // Check if target object still exists
                    GameObject targetObj = GameObject.Find(objectName);
                    status.AppendLine($"    Target object exists: {targetObj != null}");

                    if (targetObj != null)
                    {
                        status.AppendLine($"    Target object active: {targetObj.activeInHierarchy}");
                        status.AppendLine($"    Position: {targetObj.transform.position}");
                        status.AppendLine($"    Rotation: {targetObj.transform.rotation.eulerAngles}");
                    }
                    status.AppendLine();
                }

                status.AppendLine($"Total drawings: {_activeDrawings.Values.SelectMany(list => list).Count()}");
                status.AppendLine();
                status.AppendLine("Note: Use the Immersive Debugger Inspector to toggle gizmo visibility on/off.");
                status.AppendLine("Gizmos work in real-time and will track objects even if they become invisible.");

                return status.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting drawing status: {ex.Message}";
            }
        }

        /// <summary>
        /// Ensures the Immersive Debugger inspector panel is visible for gizmo rendering
        /// </summary>
        private void EnsureInspectorPanelVisible()
        {
            try
            {
                var debuggerTools = ImmersiveDebuggerTools.instance;
                if (debuggerTools != null)
                {
                    // Show the main debugger interface
                    debuggerTools.SetDebuggerVisibility(true);

                    // Ensure the inspector panel is visible
                    debuggerTools.SetInspectorPanelVisibility(true);

                    Debug.Log("[DrawingTools] Inspector panel visibility ensured for gizmo rendering");
                }
                else
                {
                    Debug.LogWarning("[DrawingTools] Could not access ImmersiveDebuggerTools - gizmos may not be visible until inspector panel is manually opened");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DrawingTools] Failed to ensure inspector panel visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Track a drawing for management purposes
        /// </summary>
        private void TrackDrawing(string gameObjectName, GameObject gizmoObject)
        {
            if (!_activeDrawings.ContainsKey(gameObjectName))
                _activeDrawings[gameObjectName] = new List<GameObject>();

            _activeDrawings[gameObjectName].Add(gizmoObject);
        }
    }
}
