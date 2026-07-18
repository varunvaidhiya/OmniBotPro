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

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using RO = Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight;

[assembly: InternalsVisibleTo("Meta.XR.RuntimeOptimizer.Editor.Tests")]

namespace Meta.XR.RuntimeOptimizer.Editor
{
    public class GameObjectSelectionWindow : EditorWindow
    {
        internal class TreeNode
        {
            public string Name;
            public string FullPath;
            public TreeNode? Parent;
            public List<TreeNode> Children = new List<TreeNode>();
            public bool IsExpanded = true;
            public int Depth = 0;
            public string UniqueId;
            private static int _nextId = 0;

            public TreeNode(string name, string fullPath, TreeNode? parent = null)
            {
                Name = name;
                FullPath = fullPath;
                Parent = parent;
                Depth = parent != null ? parent.Depth + 1 : 0;
                UniqueId = $"{fullPath}_{_nextId++}";
            }

            public bool HasChildren => Children.Count > 0;
        }

        private List<string> allGameObjects = new List<string>();

        /// <summary>
        /// Creates a GUIContent with the specified display text and tooltip.
        /// Used to show full paths when hovering over truncated GameObject names.
        /// </summary>
        internal static GUIContent CreateTooltipContent(string displayText, string tooltipText)
        {
            return new GUIContent(displayText, tooltipText);
        }

        private List<string> filteredGameObjects = new List<string>();
        private HashSet<string> selectedGameObjects = new HashSet<string>();
        private Dictionary<string, TreeNode> selectedNodesByUniqueId = new Dictionary<string, TreeNode>();
        private string searchText = "";
        private Vector2 scrollPosition;
        private Vector2 selectedScrollPosition;
        private RuntimeOptimizerWindow? parentWindow;
        private bool selectAll = false;
        private bool runAsGroup = false;
        private TreeNode? rootNode;
        private Dictionary<string, TreeNode> pathToNodeMap = new Dictionary<string, TreeNode>();
        private bool searchWasUsed = false;
        private bool toggleWasChanged = false;
        private const int kMaxSelectableGameObjects = 200;

        public static void ShowWindow(RuntimeOptimizerWindow parent)
        {
            var window = GetWindow<GameObjectSelectionWindow>(true, "Select Game Objects to Test", true);
            window.minSize = new Vector2(600, 700);
            window.maxSize = new Vector2(800, 700);
            window.parentWindow = parent;
            window.Initialize();
            window.Show();
        }

        public static void ShowWindow(RuntimeOptimizerWindow parent, List<string> gameObjectPaths)
        {
            var window = GetWindow<GameObjectSelectionWindow>(true, "Select Game Objects to Test", true);
            window.minSize = new Vector2(500, 770);
            window.maxSize = new Vector2(700, 850);
            window.parentWindow = parent;
            window.InitializeWithPaths(gameObjectPaths);
            window.Show();
        }

        private void Initialize()
        {
            // Get GameObjects from the most recent capture
            // We'll need to read the most recent _unity.json file
            string[] captures = CaptureTool.GetCapturedList();
            if (captures.Length == 0)
            {
                RO.Util.DebugLogError("No captures found for GameObject selection");
                Close();
                return;
            }

            // Get the most recent capture
            string mostRecentCapture = System.IO.Path.GetFileNameWithoutExtension(captures[0]);

            // Read the unity JSON file
            string unityJsonPath = $"{CaptureTool.GetOutputDirectory()}/{mostRecentCapture}_unity.json";
            if (!System.IO.File.Exists(unityJsonPath))
            {
                RO.Util.DebugLogError($"Unity JSON file not found: {unityJsonPath}");
                Close();
                return;
            }

            try
            {
                string jsonContent = System.IO.File.ReadAllText(unityJsonPath);
                var jsonNode = JSONObject.Parse(jsonContent) as JSONObject;

                if (jsonNode != null && jsonNode["items"] is JSONArray)
                {
                    JSONArray itemArray = (JSONArray)jsonNode["items"];
                    foreach (var item in itemArray)
                    {
                        string parentName = item.Value["parentName"]?.Value ?? "";
                        string name = item.Value["name"]?.Value ?? "";
                        string fullPath = string.IsNullOrEmpty(parentName) ? name : $"{parentName}{name}";

                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            allGameObjects.Add(fullPath);
                        }
                    }
                }

                filteredGameObjects = new List<string>(allGameObjects);
                BuildTree();
                RO.Util.DebugLog($"Loaded {allGameObjects.Count} GameObjects for selection");
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLogError($"Failed to parse Unity JSON: {ex.Message}");
                Close();
            }
        }

        private void InitializeWithPaths(List<string> gameObjectPaths)/////
        {
            // Initialize with GameObject paths received from the runtime
            allGameObjects = new List<string>(gameObjectPaths);
            filteredGameObjects = new List<string>(allGameObjects);
            BuildTree();

            RO.Util.DebugLog($"Initialized GameObject selection with {allGameObjects.Count} objects from frustrum");

            // Telemetry: Window opened
            var windowOpenedEventData = new
            {
                totalGameObjects = allGameObjects.Count,
                source = "frustrum_query",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string windowOpenedJson = InsightEventDataStr.ToJsonStr(UnityEngine.JsonUtility.ToJson(windowOpenedEventData));
            RuntimeOptimizerPlugin.SendEvent("gameobject_selection_window_opened", windowOpenedJson);
        }

        private void OnGUI()
        {
            if (allGameObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No GameObjects found. Please run a capture first.", MessageType.Warning);
                if (GUILayout.Button("Close"))
                {
                    Close();
                }
                return;
            }

            EditorGUILayout.Space(10);

            // Title
            EditorGUILayout.LabelField("Select Game Objects to Test", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Instructions
            EditorGUILayout.HelpBox(
                $"Found {allGameObjects.Count} GameObjects in the scene. " +
                "Select one or more GameObjects to run performance analysis on.\n\n" +
                "Use the search bar to filter GameObjects by name.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Search bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            string newSearchText = EditorGUILayout.TextField(searchText);
            if (newSearchText != searchText)
            {
                // Telemetry: Search usage
                bool wasEmpty = string.IsNullOrEmpty(searchText);
                bool isNowEmpty = string.IsNullOrEmpty(newSearchText);

                searchText = newSearchText;
                FilterGameObjects();

                if (!isNowEmpty)
                {
                    searchWasUsed = true;
                    var searchEventData = new
                    {
                        searchLength = searchText.Length,
                        resultsCount = filteredGameObjects.Count,
                        totalCount = allGameObjects.Count,
                        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    string searchJson = InsightEventDataStr.ToJsonStr(UnityEngine.JsonUtility.ToJson(searchEventData));
                    RuntimeOptimizerPlugin.SendEvent("gameobject_selection_search_used", searchJson);
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                // Fix for T257512937: Clear button should clear ALL selections
                // and reset the selection state completely
                searchText = "";
                GUI.FocusControl(null);
                selectedNodesByUniqueId.Clear();
                selectedGameObjects.Clear();
                selectAll = false;

                // Rebuild filtered list and tree (without restoring any selections)
                filteredGameObjects = new List<string>(allGameObjects);
                BuildTree();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Select All checkbox
            EditorGUILayout.BeginHorizontal();
            bool newSelectAll = EditorGUILayout.Toggle("Select all game objects", selectAll);
            if (newSelectAll != selectAll)
            {
                selectAll = newSelectAll;
                if (selectAll)
                {
                    if (filteredGameObjects.Count > kMaxSelectableGameObjects)
                    {
                        EditorUtility.DisplayDialog(
                            "Too Many GameObjects",
                            $"Cannot select all — there are {filteredGameObjects.Count} GameObjects but the maximum is {kMaxSelectableGameObjects}.\n\n" +
                            "Please use the search bar to filter to a smaller subset first.",
                            "OK");
                        selectAll = false;
                    }
                    else
                    {
                        // Select all filtered GameObjects by traversing the tree
                        SelectAllNodesRecursive(rootNode);
                        foreach (var go in filteredGameObjects)
                        {
                            selectedGameObjects.Add(go);
                        }

                        // Telemetry: Select All used
                        var selectAllEventData = new
                        {
                            selectionMethod = "select_all",
                            selectedCount = selectedGameObjects.Count,
                            filteredCount = filteredGameObjects.Count,
                            totalCount = allGameObjects.Count,
                            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        string selectAllJson = InsightEventDataStr.ToJsonStr(UnityEngine.JsonUtility.ToJson(selectAllEventData));
                        RuntimeOptimizerPlugin.SendEvent("gameobject_selection_method_used", selectAllJson);
                    }
                }
                else
                {
                    // Deselect all
                    selectedNodesByUniqueId.Clear();
                    selectedGameObjects.Clear();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"({filteredGameObjects.Count} shown, {selectedNodesByUniqueId.Count} selected)", EditorStyles.miniLabel, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();

            // Run Selected as Group checkbox
            EditorGUILayout.BeginHorizontal();
            bool newRunAsGroup = EditorGUILayout.Toggle("Run Selected as Group", runAsGroup);
            if (newRunAsGroup != runAsGroup)
            {
                toggleWasChanged = true;
                runAsGroup = newRunAsGroup;
            }
            EditorGUILayout.EndHorizontal();

            if (runAsGroup)
            {
                EditorGUILayout.HelpBox(
                    "Group mode: All selected GameObjects will be disabled together in a single pass. " +
                    "Results will show the total GPU cost for the entire group.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // GameObject list
            EditorGUILayout.LabelField("Available Game Objects:", EditorStyles.boldLabel);

            // Create a darker background box for the available game objects list
            Color originalBgColor2 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUIStyle boxStyle2 = new GUIStyle(GUI.skin.box);

            EditorGUILayout.BeginVertical(boxStyle2);
            GUI.backgroundColor = originalBgColor2;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            // Draw hierarchical tree view
            bool anyChanges = false;
            if (rootNode != null)
            {
                DrawTreeNode(rootNode, ref anyChanges);
            }
            else
            {
                // Fallback to flat list if tree hasn't been built
                foreach (var gameObject in filteredGameObjects)
                {
                    EditorGUILayout.BeginHorizontal();
                    bool isSelected = selectedGameObjects.Contains(gameObject);

                    // Make checkbox darker
                    Color originalColor = GUI.color;
                    GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    GUI.color = originalColor;

                    if (newSelected != isSelected)
                    {
                        if (newSelected)
                        {
                            selectedGameObjects.Add(gameObject);
                        }
                        else
                        {
                            selectedGameObjects.Remove(gameObject);
                            selectAll = false;
                        }
                    }

                    GUIContent gameObjectContent = CreateTooltipContent(gameObject, gameObject);
                    EditorGUILayout.LabelField(gameObjectContent);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Selected GameObjects section
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Selected Game Objects ({selectedNodesByUniqueId.Count}):", EditorStyles.boldLabel);

            // Warning for large selections - inline
            if (selectedNodesByUniqueId.Count > kMaxSelectableGameObjects)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"⚠ Exceeds limit of {kMaxSelectableGameObjects} GOs", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            // Create a darker background box for the staging area
            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);

            EditorGUILayout.BeginVertical(boxStyle);
            GUI.backgroundColor = originalBgColor;

            if (selectedNodesByUniqueId.Count > 0)
            {
                selectedScrollPosition = EditorGUILayout.BeginScrollView(selectedScrollPosition, GUILayout.Height(135));

                var selectedList = selectedNodesByUniqueId.Values.ToList();
                for (int i = 0; i < selectedList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                    // Show index
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));

                    // Show GameObject name with tooltip showing full path
                    GUIContent nameContent = CreateTooltipContent(selectedList[i].FullPath, selectedList[i].FullPath);
                    EditorGUILayout.LabelField(nameContent, GUILayout.ExpandWidth(true));

                    // Remove button
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        TreeNode nodeToRemove = selectedList[i];
                        selectedNodesByUniqueId.Remove(nodeToRemove.UniqueId);

                        // Check if we need to remove the path from selectedGameObjects
                        // Only remove if no other selected nodes have the same path
                        bool hasOtherNodesWithSamePath = selectedNodesByUniqueId.Values
                            .Any(n => n.FullPath == nodeToRemove.FullPath);
                        if (!hasOtherNodesWithSamePath)
                        {
                            selectedGameObjects.Remove(nodeToRemove.FullPath);
                        }

                        selectAll = false;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Box("", GUILayout.Height(135), GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndVertical();

            // Push button to bottom
            GUILayout.FlexibleSpace();

            // Bottom button - pinned to bottom
            EditorGUILayout.BeginHorizontal();

            // Enable Scan button only if at least one GameObject is selected
            GUI.enabled = selectedNodesByUniqueId.Count > 0 && selectedNodesByUniqueId.Count <= kMaxSelectableGameObjects;
            if (GUILayout.Button("Scan Selected GameObjects", GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                StartScan();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private void BuildTree()
        {
            rootNode = new TreeNode("Root", "", null);
            pathToNodeMap.Clear();
            pathToNodeMap[""] = rootNode;

            foreach (var path in filteredGameObjects)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                string[] parts = path.Split('/');
                TreeNode currentParent = rootNode;
                string currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (string.IsNullOrEmpty(part))
                        continue;

                    string newPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

                    // For leaf nodes (last part of the path), always create a new node to handle duplicates
                    bool isLeafNode = (i == parts.Length - 1);

                    if (isLeafNode)
                    {
                        // Always create a new leaf node even if the path exists
                        // This handles cases where multiple GameObjects have the same full path
                        TreeNode newNode = new TreeNode(part, newPath, currentParent);
                        currentParent.Children.Add(newNode);
                        // Don't add leaf nodes to pathToNodeMap to allow duplicates
                    }
                    else if (!pathToNodeMap.ContainsKey(newPath))
                    {
                        // For intermediate nodes, create only if it doesn't exist
                        TreeNode newNode = new TreeNode(part, newPath, currentParent);
                        currentParent.Children.Add(newNode);
                        pathToNodeMap[newPath] = newNode;
                        currentParent = newNode;
                        currentPath = newPath;
                    }
                    else
                    {
                        // Reuse existing intermediate node
                        currentParent = pathToNodeMap[newPath];
                        currentPath = newPath;
                    }
                }
            }

            // Sort children alphabetically at each level
            SortTreeRecursive(rootNode);
        }

        private void SortTreeRecursive(TreeNode node)
        {
            if (node.Children.Count > 0)
            {
                node.Children.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
                foreach (var child in node.Children)
                {
                    SortTreeRecursive(child);
                }
            }
        }

        private void FilterGameObjects()
        {
            // Preserve selections by FullPath before rebuilding tree
            // BuildTree() creates new TreeNode objects with new UniqueIds
            var previouslySelectedPaths = new HashSet<string>(
                selectedNodesByUniqueId.Values.Select(n => n.FullPath)
            );

            // Clear stale node references before rebuilding
            selectedNodesByUniqueId.Clear();
            selectedGameObjects.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                filteredGameObjects = new List<string>(allGameObjects);
            }
            else
            {
                filteredGameObjects = allGameObjects
                    .Where(go => go.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            BuildTree();

            // Restore selections by finding new nodes that match previously selected paths
            if (previouslySelectedPaths.Count > 0)
            {
                RestoreSelectionsByPath(rootNode, previouslySelectedPaths);
            }
        }

        private void RestoreSelectionsByPath(TreeNode? node, HashSet<string> selectedPaths)
        {
            if (node == null)
                return;

            // For leaf nodes, check if their path was previously selected
            if (!node.HasChildren && selectedPaths.Contains(node.FullPath))
            {
                selectedNodesByUniqueId[node.UniqueId] = node;
                // Also add to selectedGameObjects to keep collections in sync
                selectedGameObjects.Add(node.FullPath);
            }

            foreach (var child in node.Children)
            {
                RestoreSelectionsByPath(child, selectedPaths);
            }
        }

        private void DrawTreeNode(TreeNode node, ref bool anyChanges)
        {
            if (node == rootNode)
            {
                // Draw root's children directly
                foreach (var child in node.Children)
                {
                    DrawTreeNode(child, ref anyChanges);
                }
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Only show checkbox for leaf nodes (nodes without children)
            if (!node.HasChildren)
            {
                // Checkbox - always left aligned, no indentation
                bool wasSelected = selectedNodesByUniqueId.ContainsKey(node.UniqueId);

                // Make checkbox darker
                Color originalColor = GUI.color;
                GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                bool isSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(20));
                GUI.color = originalColor;
                if (isSelected != wasSelected)
                {
                    anyChanges = true;
                    if (isSelected)
                    {
                        if (selectedNodesByUniqueId.Count >= kMaxSelectableGameObjects)
                        {
                            EditorUtility.DisplayDialog(
                                "Selection Limit Reached",
                                $"Maximum GameObjects that can be selected at once is {kMaxSelectableGameObjects}.\n\n" +
                                "Please deselect some GameObjects or use the search bar to narrow your selection.",
                                "OK");
                        }
                        else
                        {
                            selectedNodesByUniqueId[node.UniqueId] = node;
                            selectedGameObjects.Add(node.FullPath);
                        }
                    }
                    else
                    {
                        selectedNodesByUniqueId.Remove(node.UniqueId);
                        selectedGameObjects.Remove(node.FullPath);
                        selectAll = false;
                    }
                }

                // Reduced spacing between checkbox and hierarchy
                GUILayout.Space(2);
            }
            else
            {
                // No checkbox for parent nodes, just add spacing to align with leaf nodes
                GUILayout.Space(22);
            }

            // Hierarchy section - increased indentation based on depth
            GUILayout.Space((node.Depth - 1) * 25);

            // Foldout with label for nodes with children (not bold)
            if (node.HasChildren)
            {
                node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, node.Name, true);
            }
            else
            {
                // Leaf node - show name with tooltip for full path
                GUIContent leafContent = CreateTooltipContent(node.Name, node.FullPath);
                EditorGUILayout.LabelField(leafContent);
            }

            EditorGUILayout.EndHorizontal();

            // Draw children if expanded
            if (node.IsExpanded && node.HasChildren)
            {
                foreach (var child in node.Children)
                {
                    DrawTreeNode(child, ref anyChanges);
                }
            }
        }

        private void SelectAllNodesRecursive(TreeNode? node)
        {
            if (node == null)
                return;

            if (!node.HasChildren)
            {
                selectedNodesByUniqueId[node.UniqueId] = node;
            }

            foreach (var child in node.Children)
            {
                SelectAllNodesRecursive(child);
            }
        }

        private void OnDestroy()
        {
            parentWindow?.NotifyChildWindowClosed();

            // Send telemetry on window close
            var windowCloseEventData = new
            {
                searchWasUsed = searchWasUsed,
                toggleWasChanged = toggleWasChanged,
                finalToggleState = runAsGroup,
                totalSelected = selectedNodesByUniqueId.Count,
                totalAvailable = allGameObjects.Count,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string windowCloseJson = InsightEventDataStr.ToJsonStr(UnityEngine.JsonUtility.ToJson(windowCloseEventData));
            RuntimeOptimizerPlugin.SendEvent("gameobject_selection_window_closed", windowCloseJson);

            // Clear all selected GameObjects when the window is closed
            selectedNodesByUniqueId.Clear();
            selectedGameObjects.Clear();
        }

        private void StartScan()
        {
            if (selectedNodesByUniqueId.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one GameObject to test.", "OK");
                return;
            }

            if (selectedNodesByUniqueId.Count > kMaxSelectableGameObjects)
            {
                EditorUtility.DisplayDialog(
                    "Too Many GameObjects Selected",
                    $"Maximum GameObjects that can be selected at once is {kMaxSelectableGameObjects}. " +
                    $"You have {selectedNodesByUniqueId.Count} selected.\n\n" +
                    "Please use the search bar to filter and select a smaller subset of GameObjects you are interested in.",
                    "OK");
                return;
            }

            if (parentWindow == null)
            {
                EditorUtility.DisplayDialog("Error", "Parent window reference lost.", "OK");
                Close();
                return;
            }
            // Log telemetry with detailed information including grouping toggle state
            var scanEventData = new
            {
                selectedCount = selectedNodesByUniqueId.Count,
                totalAvailable = allGameObjects.Count,
                groupMode = runAsGroup,
                groupingToggled = runAsGroup,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string scanJson = InsightEventDataStr.ToJsonStr(UnityEngine.JsonUtility.ToJson(scanEventData));
            RuntimeOptimizerPlugin.SendEvent("scan_selected_gameobjects_clicked", scanJson);

            // Start the selective What If analysis directly
            // Use the full list of selected paths (may contain duplicates if multiple nodes have the same path)
            var selectedPaths = selectedNodesByUniqueId.Values.Select(node => node.FullPath).ToList();
            parentWindow.StartSelectiveWhatIfAnalysis(selectedPaths, runAsGroup);
            Close();
        }
    }
}
