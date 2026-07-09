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
using Meta.XR.ImmersiveDebugger.Manager;
using Meta.XR.ImmersiveDebugger.UserInterface;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Immersive Debugger Interface Control Tools for MCP Clients
    ///
    /// This tool manages the Immersive Debugger UI interface - panels, blocks, and layout.
    /// It does NOT manipulate Unity GameObjects or Components directly.
    ///
    /// IMPORTANT: Tool Selection Guide
    ///
    /// USE THIS TOOL (ImmersiveDebuggerTools) FOR:
    /// - Opening/closing Immersive Debugger panels
    /// - Managing debugger interface layout
    /// - Showing/hiding debugger UI blocks
    /// - Controlling debugger interface state
    /// - UI-related operations on the debugger itself
    ///
    /// USE SceneObjectsTools INSTEAD FOR:
    /// - Finding GameObjects in the scene
    /// - Inspecting GameObject components
    /// - Modifying component properties/fields
    /// - Calling methods on components
    /// - Adding/removing components
    /// - Any direct Unity scene object manipulation
    ///
    /// These are complementary tools - use ImmersiveDebuggerTools to control the debugger interface,
    /// then use SceneObjectsTools to actually manipulate Unity objects and components.
    /// </summary>
    [Tool(
        "Controls the Immersive Debugger UI interface - panels, blocks, and layout. This tool manages the debugger interface itself, NOT Unity GameObjects.",
        "USE THIS TOOL FOR: Opening/closing Immersive Debugger panels, managing debugger layout, showing/hiding UI blocks, controlling debugger interface state.",
        "USE SceneObjectsTools INSTEAD FOR: Finding GameObjects, inspecting/modifying components, calling methods on components, any direct Unity scene object manipulation."
    )]
    internal class ImmersiveDebuggerTools : SingletonService<ImmersiveDebuggerTools>
    {
        private InspectorPanel _inspectorPanel;
        private Console _console;
        private DebugInterface _debugInterface;

        private bool TryConnectToDebugInterface()
        {
            if (_debugInterface != null) return true;

            _debugInterface = UnityEngine.Object.FindFirstObjectByType<DebugInterface>(FindObjectsInactive.Include);
            if (_debugInterface != null)
            {
                // Get references to panels through the debug interface
                _inspectorPanel = UnityEngine.Object.FindFirstObjectByType<InspectorPanel>(FindObjectsInactive.Include);
                _console = UnityEngine.Object.FindFirstObjectByType<Console>(FindObjectsInactive.Include);
                UnityEngine.Debug.Log("ImmersiveDebuggerTools: Successfully connected to DebugInterface");
                return true;
            }

            return false;
        }

        [Tool(Description = "Show or hide the entire Immersive Debugger interface. This controls the main debug interface visibility.",
            Returns = "Status of the operation and current visibility state.")]
        internal string SetDebuggerVisibility(bool visible)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            if (visible)
            {
                _debugInterface.Show();
                return "Immersive Debugger interface is now visible.";
            }
            else
            {
                _debugInterface.Hide();
                return "Immersive Debugger interface is now hidden.";
            }
        }

        [Tool(Description = "Add UI to the in-headset panel to show user information about the game object's components, properties, methods etc. " +
                            "The membersToInspector parameter should be passed as comma-separated list of member names to inspect, e.g. \"position,rotation,localScale\". " +
                            "If membersToInspector is empty, all public members will be added to UI.",
            Returns = "Information about the operation.")]
        public string ShowObjectComponentInfos(string categoryInPanel, string gameObjectName, string componentName, string membersToInspector = "")
        {
            var result = RuntimeAPIs.AddInspector(categoryInPanel, gameObjectName, componentName, membersToInspector);
            return $"Operation result: {result.Status}. {result.Message}. {result.Context}";
        }

        [Tool(Description = "Toggle the visibility of the entire Immersive Debugger interface.",
            Returns = "Status of the operation and new visibility state.")]
        internal string ToggleDebuggerVisibility()
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            _debugInterface.ToggleVisibility();
            bool isVisible = _debugInterface.Visibility;
            return $"Immersive Debugger interface toggled. Current state: {(isVisible ? "visible" : "hidden")}.";
        }

        [Tool(Description = "Show or hide the Inspector panel specifically. The Inspector panel shows component details and custom inspectors.",
            Returns = "Status of the operation and current panel visibility state.")]
        internal string SetInspectorPanelVisibility(bool visible)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            if (_inspectorPanel == null)
            {
                return "Error: Inspector panel not found.";
            }

            if (visible)
            {
                _inspectorPanel.Show();
                return "Inspector panel is now visible.";
            }
            else
            {
                _inspectorPanel.Hide();
                return "Inspector panel is now hidden.";
            }
        }

        [Tool(Description = "Show or hide the Console panel specifically. The Console panel shows debug logs and messages.",
            Returns = "Status of the operation and current panel visibility state.")]
        internal string SetConsolePanelVisibility(bool visible)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            if (_console == null)
            {
                return "Error: Console panel not found.";
            }

            if (visible)
            {
                _console.Show();
                return "Console panel is now visible.";
            }
            else
            {
                _console.Hide();
                return "Console panel is now hidden.";
            }
        }

        [Tool(Description = "Switch the Inspector panel to hierarchy mode, which allows browsing and selecting objects from the scene hierarchy.",
            Returns = "Status of the operation.")]
        internal string SwitchToHierarchyMode()
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            if (_inspectorPanel == null)
            {
                return "Error: Inspector panel not found.";
            }

            // Use reflection to access the private SelectHierarchyMode method
            try
            {
                var selectHierarchyMethod = typeof(InspectorPanel).GetMethod("SelectHierarchyMode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (selectHierarchyMethod != null)
                {
                    selectHierarchyMethod.Invoke(_inspectorPanel, null);
                    return "Inspector panel switched to hierarchy mode. You can now browse and select objects from the scene hierarchy.";
                }
                else
                {
                    return "Error: Could not access hierarchy mode switch functionality.";
                }
            }
            catch (System.Exception ex)
            {
                return $"Error switching to hierarchy mode: {ex.Message}";
            }
        }

        [Tool(Description = "Switch the Inspector panel to category mode, which shows custom inspectors organized by categories.",
            Returns = "Status of the operation.")]
        internal string SwitchToCategoryMode()
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            if (_inspectorPanel == null)
            {
                return "Error: Inspector panel not found.";
            }

            // Use reflection to access the private SelectCategoryMode method
            try
            {
                var selectCategoryMethod = typeof(InspectorPanel).GetMethod("SelectCategoryMode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (selectCategoryMethod != null)
                {
                    selectCategoryMethod.Invoke(_inspectorPanel, null);
                    return "Inspector panel switched to category mode. Custom inspectors are now organized by categories.";
                }
                else
                {
                    return "Error: Could not access category mode switch functionality.";
                }
            }
            catch (System.Exception ex)
            {
                return $"Error switching to category mode: {ex.Message}";
            }
        }

        [Tool(Description = "Select a GameObject in the hierarchy mode and display its inspector. This intelligently navigates through the hierarchy, starting from the scene level and waiting for each level to populate before proceeding. Ensures all necessary panels are visible.",
            Returns = "Status of the operation and information about the selected object.")]
        internal string SelectObjectInHierarchy(string gameObjectName)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            if (_inspectorPanel == null)
            {
                return "Error: Inspector panel not found.";
            }

            try
            {
                // Step 1: Ensure the main debugger interface is visible
                if (!_debugInterface.Visibility)
                {
                    UnityEngine.Debug.Log("Showing main Immersive Debugger interface");
                    _debugInterface.Show();
                }

                // Step 2: Ensure the inspector panel is visible
                if (!_inspectorPanel.Visibility)
                {
                    UnityEngine.Debug.Log("Showing Inspector panel");
                    _inspectorPanel.Show();
                }

                // Step 3: Switch to hierarchy mode
                UnityEngine.Debug.Log("Switching to hierarchy mode");
                var hierarchyModeResult = SwitchToHierarchyMode();
                if (hierarchyModeResult.Contains("Error"))
                {
                    return $"Failed to switch to hierarchy mode: {hierarchyModeResult}";
                }

                // Step 4: Find the GameObject
                GameObject targetGameObject = GameObject.Find(gameObjectName);
                if (targetGameObject == null)
                {
                    return $"GameObject '{gameObjectName}' not found in scene.";
                }

                // Step 5: Start the intelligent navigation process
                UnityEngine.Debug.Log($"Starting smart hierarchy navigation to: {gameObjectName}");
                StartSmartHierarchyNavigation(targetGameObject);

                // Step 6: Prepare response with object information
                var results = new System.Text.StringBuilder();
                results.AppendLine($"Successfully initiated navigation to GameObject '{gameObjectName}':");
                results.AppendLine($"  Main Debugger Interface: {(_debugInterface.Visibility ? "Visible" : "Hidden")}");
                results.AppendLine($"  Inspector Panel: {(_inspectorPanel.Visibility ? "Visible" : "Hidden")}");
                results.AppendLine($"  Hierarchy Mode: Activated");
                results.AppendLine();
                results.AppendLine($"GameObject Details:");
                results.AppendLine($"  Position: {targetGameObject.transform.position}");
                results.AppendLine($"  Active: {targetGameObject.activeSelf}");
                results.AppendLine($"  Layer: {LayerMask.LayerToName(targetGameObject.layer)}");
                results.AppendLine($"  Scene: {targetGameObject.scene.name}");

                // List components that will be available in the inspector
                Component[] components = targetGameObject.GetComponents<Component>();
                results.AppendLine($"  Components ({components.Length}):");
                foreach (var component in components)
                {
                    if (component != null)
                    {
                        results.AppendLine($"    {component.GetType().Name}");
                    }
                }
                results.AppendLine();
                results.AppendLine("   Navigation process started. The hierarchy will be expanded step by step and the object will be selected automatically.");
                results.AppendLine("   Component inspectors will appear in the Inspector panel once navigation is complete.");

                return results.ToString();
            }
            catch (System.Exception ex)
            {
                return $"Error during navigation setup for GameObject '{gameObjectName}': {ex.Message}";
            }
        }

        private System.Collections.IEnumerator _navigationCoroutine;

        private void StartSmartHierarchyNavigation(GameObject targetGameObject)
        {
            // Stop any existing navigation
            if (_navigationCoroutine != null)
            {
                // For now, we'll handle this synchronously as Unity's coroutines aren't available here
                // but we structure the code for future async support
            }

            // Start the smart navigation process
            PerformSmartNavigation(targetGameObject);
        }

        private void PerformSmartNavigation(GameObject targetGameObject)
        {
            try
            {
                // Step 1: Build the complete hierarchy path
                var hierarchyPath = BuildHierarchyPath(targetGameObject);
                if (hierarchyPath.Count == 0)
                {
                    UnityEngine.Debug.LogError($"Could not build hierarchy path to '{targetGameObject.name}'");
                    return;
                }

                UnityEngine.Debug.Log($"Navigation path: {string.Join(" -> ", hierarchyPath.Select(go => go.name))}");

                // Step 2: Ensure hierarchy manager is refreshed
                var hierarchyManager = Meta.XR.ImmersiveDebugger.Hierarchy.Manager.Instance;
                hierarchyManager?.Refresh();

                // Step 3: Navigate through the path intelligently
                NavigateHierarchyPathSmart(hierarchyPath);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Smart navigation failed: {ex.Message}");
            }
        }

        private void NavigateHierarchyPathSmart(System.Collections.Generic.List<GameObject> hierarchyPath)
        {
            try
            {
                // Get access to the hierarchy items
                var itemsField = typeof(InspectorPanel).GetField("_items",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (itemsField == null)
                {
                    UnityEngine.Debug.LogError("Could not access hierarchy items field");
                    return;
                }

                var items = itemsField.GetValue(_inspectorPanel) as System.Collections.Generic.Dictionary<Meta.XR.ImmersiveDebugger.Hierarchy.Item, HierarchyItemButton>;
                if (items == null)
                {
                    UnityEngine.Debug.LogError("Hierarchy items dictionary not accessible");
                    return;
                }

                // Navigate step by step with proper waiting
                for (int pathIndex = 0; pathIndex < hierarchyPath.Count; pathIndex++)
                {
                    var currentGameObject = hierarchyPath[pathIndex];
                    var isTargetObject = pathIndex == hierarchyPath.Count - 1;

                    UnityEngine.Debug.Log($"Navigating to level {pathIndex}: {currentGameObject.name}");

                    // Step 1: If this is the first level (root GameObjects), make sure scene is expanded
                    if (pathIndex == 0 && currentGameObject.transform.parent == null)
                    {
                        EnsureSceneIsExpanded(currentGameObject, items);
                    }

                    // Step 2: Find the hierarchy button for the current object
                    var currentButton = FindHierarchyButton(currentGameObject, items);
                    if (currentButton == null)
                    {
                        // If we can't find it, try expanding the parent level first
                        if (pathIndex > 0)
                        {
                            var parentGameObject = hierarchyPath[pathIndex - 1];
                            var parentButton = FindHierarchyButton(parentGameObject, items);
                            if (parentButton != null)
                            {
                                ExpandHierarchyItemButton(parentButton);
                                // Try to find the current button again after expansion
                                WaitForHierarchyPopulation();
                                currentButton = FindHierarchyButton(currentGameObject, items);
                            }
                        }

                        if (currentButton == null)
                        {
                            UnityEngine.Debug.LogError($"Could not find hierarchy button for '{currentGameObject.name}' at level {pathIndex}");
                            return;
                        }
                    }

                    // Step 3: Handle the current level
                    if (isTargetObject)
                    {
                        // This is our target - select it
                        UnityEngine.Debug.Log($"Selecting target object: {currentGameObject.name}");
                        SelectHierarchyItemButton(currentButton);
                    }
                    else
                    {
                        // This is an intermediate level - expand it
                        UnityEngine.Debug.Log($"Expanding intermediate object: {currentGameObject.name}");
                        ExpandHierarchyItemButton(currentButton);
                        WaitForHierarchyPopulation();
                    }
                }

                UnityEngine.Debug.Log("Smart hierarchy navigation completed successfully!");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Smart navigation path error: {ex.Message}");
            }
        }

        private void EnsureSceneIsExpanded(GameObject rootGameObject, System.Collections.Generic.Dictionary<Meta.XR.ImmersiveDebugger.Hierarchy.Item, HierarchyItemButton> items)
        {
            // Find the scene item and ensure it's expanded
            var scene = rootGameObject.scene;
            foreach (var kvp in items)
            {
                if (kvp.Key is Meta.XR.ImmersiveDebugger.Hierarchy.SceneItem sceneItem &&
                    sceneItem.TypedOwner.name == scene.name)
                {
                    UnityEngine.Debug.Log($"Expanding scene: {scene.name}");
                    ExpandHierarchyItemButton(kvp.Value);
                    WaitForHierarchyPopulation();
                    break;
                }
            }
        }

        private HierarchyItemButton FindHierarchyButton(GameObject gameObject, System.Collections.Generic.Dictionary<Meta.XR.ImmersiveDebugger.Hierarchy.Item, HierarchyItemButton> items)
        {
            foreach (var kvp in items)
            {
                if (kvp.Key is Meta.XR.ImmersiveDebugger.Hierarchy.GameObjectItem gameObjectItem &&
                    gameObjectItem.TypedOwner == gameObject)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        private void WaitForHierarchyPopulation()
        {
            // Force a hierarchy manager refresh to pick up any new scenes or root objects.
            var hierarchyManager = Meta.XR.ImmersiveDebugger.Hierarchy.Manager.Instance;
            hierarchyManager?.Refresh();

            // No sleep/wait needed: BuildChildren() (called by ExpandHierarchyItemButton)
            // synchronously populates child Items and registers them via Manager.ProcessItem,
            // which calls RegisterInspector on the UI panel — so the items dictionary already
            // contains the new children by the time we return here.
        }

        private System.Collections.Generic.List<GameObject> BuildHierarchyPath(GameObject targetObject)
        {
            var path = new System.Collections.Generic.List<GameObject>();
            Transform current = targetObject.transform;

            // Build path from target to root
            while (current != null)
            {
                path.Insert(0, current.gameObject);
                current = current.parent;
            }

            return path;
        }

        private void ExpandHierarchyItemButton(HierarchyItemButton button)
        {
            if (button?.Foldout != null && !button.Foldout.State)
            {
                UnityEngine.Debug.Log($"Expanding hierarchy item: {button.Item?.Label}");
                button.Foldout.State = true;
                // Trigger the fold/unfold logic which builds children
                button.Item?.BuildChildren();
            }
        }

        private void SelectHierarchyItemButton(HierarchyItemButton button)
        {
            try
            {
                // Use reflection to access the private SelectHierarchyItemButton method
                var selectMethod = typeof(InspectorPanel).GetMethod("SelectHierarchyItemButton",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (selectMethod != null)
                {
                    UnityEngine.Debug.Log($"Selecting hierarchy item: {button.Item?.Label}");
                    selectMethod.Invoke(_inspectorPanel, new object[] { button });
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to select hierarchy item button: {ex.Message}");
            }
        }

        [Tool(Description = "Get the current status of all Immersive Debugger panels and their visibility states.",
            Returns = "Current status of the debugger interface and all panels.")]
        internal string GetDebuggerStatus()
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            var status = new System.Text.StringBuilder();
            status.AppendLine("Immersive Debugger Status:");
            status.AppendLine($"Main Interface: {(_debugInterface.Visibility ? "Visible" : "Hidden")}");

            if (_inspectorPanel != null)
            {
                status.AppendLine($"Inspector Panel: {(_inspectorPanel.Visibility ? "Visible" : "Hidden")}");
            }
            else
            {
                status.AppendLine("Inspector Panel: Not Found");
            }

            if (_console != null)
            {
                status.AppendLine($"Console Panel: {(_console.Visibility ? "Visible" : "Hidden")}");
            }
            else
            {
                status.AppendLine("Console Panel: Not Found");
            }

            // Check DebugManager status
            var debugManager = DebugManager.Instance;
            if (debugManager != null)
            {
                status.AppendLine($"Debug Manager: Active");
                status.AppendLine($"UI Panel Available: {(debugManager.UiPanel != null ? "Yes" : "No")}");
            }
            else
            {
                status.AppendLine("Debug Manager: Not Found");
            }

            return status.ToString();
        }

        [Tool(Description = "Set the transparency/opacity state of the Immersive Debugger panels. When opacity is enabled, panels appear solid. When disabled, panels become transparent.",
            Returns = "Status of the operation and current opacity state.")]
        internal string SetDebuggerOpacity(bool opaque)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            // Use reflection to access the OpacityOverride property
            try
            {
                var opacityProperty = typeof(DebugInterface).GetProperty("OpacityOverride",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (opacityProperty != null)
                {
                    opacityProperty.SetValue(_debugInterface, opaque);
                    return $"Debugger opacity set to {(opaque ? "opaque" : "transparent")}.";
                }
                else
                {
                    return "Error: Could not access opacity control functionality.";
                }
            }
            catch (System.Exception ex)
            {
                return $"Error setting opacity: {ex.Message}";
            }
        }

        [Tool(Description = "Configure the follow behavior of the Immersive Debugger panels. When enabled, panels will follow the user's head movement.",
            Returns = "Status of the operation and current follow state.")]
        internal string SetFollowTranslation(bool followTranslation)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            // Use reflection to access the FollowOverride property
            try
            {
                var followProperty = typeof(DebugInterface).GetProperty("FollowOverride",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (followProperty != null)
                {
                    followProperty.SetValue(_debugInterface, followTranslation);
                    return $"Follow translation set to {(followTranslation ? "enabled" : "disabled")}.";
                }
                else
                {
                    return "Error: Could not access follow translation control functionality.";
                }
            }
            catch (System.Exception ex)
            {
                return $"Error setting follow translation: {ex.Message}";
            }
        }

        [Tool(Description = "Configure the rotation follow behavior of the Immersive Debugger panels. When enabled, panels will follow the user's head rotation.",
            Returns = "Status of the operation and current rotation follow state.")]
        internal string SetFollowRotation(bool followRotation)
        {
            if (!TryConnectToDebugInterface())
            {
                return "Error: Immersive Debugger interface not found. Ensure the Immersive Debugger is enabled in Meta XR settings.";
            }

            // Use reflection to access the RotateOverride property
            try
            {
                var rotateProperty = typeof(DebugInterface).GetProperty("RotateOverride",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (rotateProperty != null)
                {
                    rotateProperty.SetValue(_debugInterface, followRotation);
                    return $"Follow rotation set to {(followRotation ? "enabled" : "disabled")}.";
                }
                else
                {
                    return "Error: Could not access follow rotation control functionality.";
                }
            }
            catch (System.Exception ex)
            {
                return $"Error setting follow rotation: {ex.Message}";
            }
        }
    }
}
