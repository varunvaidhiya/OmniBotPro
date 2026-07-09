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


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight;

namespace Meta.XR.RuntimeOptimizer.Editor
{
    public class RuntimeOptimizerDetailPopup : PopupWindowContent
    {
        public enum PopupForType
        {
            Unknown,
            VertexArray,
            TextureArray,
            MaterialArray
        };
        private Vector2 scrollPosition;
        private float windowWidth = 700;
        private float windowHeight = 250;

        private PopupForType typeOfArray = 0;
        private InsightData insight = null;
        private object selectedObject = null;

        // Dragging functionality
        private bool isDragging = false;
        private Vector2 dragStartPosition;
        private Rect dragHandleRect;
        private static RuntimeOptimizerDetailPopup currentInstance;
        private static Rect currentWindowRect;

        static string GetStringAfterLastSlash(string originalString)
        {
            if (string.IsNullOrEmpty(originalString))
                return string.Empty;
            int lastIndex = originalString.LastIndexOf('/');
            if (lastIndex == -1)
                return originalString;
            return originalString.Substring(lastIndex + 1);
        }
        // Constructor to initialize with items
        public RuntimeOptimizerDetailPopup(InsightData insights, PopupForType arrayToShow = 0, object selectedObject = null)
        {
            // Initialize with some example data if not provided
            this.typeOfArray = arrayToShow;
            this.insight = insights;
            this.selectedObject = selectedObject;

            Debug.Log("Popup window created " + typeOfArray + selectedObject);
        }

        // Called when the popup window is opened
        public override void OnOpen()
        {
            // You can initialize resources here
            Debug.Log("Popup window opened");
        }

        // Called when the popup window is closed
        public override void OnClose()
        {
            // Clean up resources here
            Debug.Log("Popup window closed");
        }

        // Get the window size
        public override Vector2 GetWindowSize()
        {
            return new Vector2(windowWidth, windowHeight);
        }

        // Draw the GUI
        public override void OnGUI(Rect rect)
        {
            // Store the current instance and window rect for dragging
            currentInstance = this;
            currentWindowRect = new Rect(editorWindow.position.x, editorWindow.position.y, windowWidth, windowHeight);

            // Draw drag handle
            dragHandleRect = new Rect(0, 0, rect.width, 20);
            EditorGUI.DrawRect(dragHandleRect, new Color(0.2f, 0.2f, 0.2f, 1f));

            // Handle dragging
            HandleDragging(rect);

            // Draw drag handle text
            GUI.Label(new Rect(5, 2, rect.width - 10, 16), "Asset Detail", EditorStyles.boldLabel);
            EditorGUILayout.Space(20); // Add space after the drag handle


            EditorGUILayout.Space(5);

            // Begin the scrollable area
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Content inside the scroll view
            if (typeOfArray == PopupForType.VertexArray)
            {
                showVertexArray(insight);
            }
            else if (typeOfArray == PopupForType.TextureArray)
            {
                showTextureArray(insight);
            }
            else if (typeOfArray == PopupForType.MaterialArray)
            {
                DrawMaterialSection(insight);
            }

            // End the scrollable area
            EditorGUILayout.EndScrollView();

            // Handle resize
            EditorGUILayout.Space(5);
        }

        private void showVertexArray(InsightData insight)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Object Name", UnitText, GUILayout.Width(240));
            EditorGUILayout.LabelField("Count", UnitText, GUILayout.Width(100));
            EditorGUILayout.LabelField("Path", UnitText, GUILayout.Width(30));
            GUILayout.EndHorizontal();
            foreach (var objectData in insight.meshAssetVertArray)
            {
                var obj = objectData.Item1;
                int totlaVertCount = obj != null ? obj.vertexCount * objectData.Item2.drawCount : objectData.Item2.dynamicVertexCount * objectData.Item2.drawCount;
                var percentAttribution = (totlaVertCount * 100.0) / insight.totalVertexCount;

                string dmeshName = GetStringAfterLastSlash(objectData.Item2.shortPath) + " (dynamic)";

                EditorGUILayout.BeginHorizontal("box");
                if (selectedObject != null)
                {
                    var selectedMesh = selectedObject as MeshRuntimeData;

                    if (selectedMesh.shortPath == objectData.Item2.shortPath)
                    {
                        EditorGUILayout.LabelField("*", GUILayout.Width(10));
                    }
                }


                if (obj != null)
                {
                    EditorGUILayout.ObjectField(obj, typeof(Mesh), false, GUILayout.Width(180));
                    if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        RuntimeOptimizerPlugin.SendEvent("open_mesh", "");
                    }
                }
                else
                {
                    // no asset link for dynamic mesh
                    EditorGUILayout.LabelField(dmeshName, GUILayout.Width(180));
                }

                EditorGUILayout.LabelField("", GUILayout.Width(40));
                EditorGUILayout.LabelField($"{totlaVertCount}({objectData.Item2.drawCount})", GUILayout.Width(80));

                EditorGUILayout.LabelField(objectData.Item2.shortPath, EditorStyles.wordWrappedLabel);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void showTextureArray(InsightData insight)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
            EditorGUILayout.LabelField("Texture Analysis", UnitText, GUILayout.Width(200));

            EditorGUILayout.LabelField("Memory Usage", UnitText, GUILayout.Width(100));

            EditorGUILayout.Space(10);
            GUILayout.EndHorizontal();
            foreach (var objectData in insight.textureArray)
            {
                // const int kTextureLabelWidth = 90;

                EditorGUILayout.BeginHorizontal("box");

                if (selectedObject != null)
                {
                    var selectedTex = selectedObject as Texture;

                    if (selectedTex == objectData.Item1)
                    {
                        EditorGUILayout.LabelField("*", GUILayout.Width(10));
                    }
                }
                double runtimeSize = (double)objectData.Item2.runtimeSize / (1024.0 * 1024.0);
                double percentage = 100.0 * (double)objectData.Item2.runtimeSize / insight.totalTextureMemory;

                string usedLabel = $"({objectData.Item2.usedByCount})";

                string percentageLable = string.Format("{0:0.00}%{1} ", percentage, usedLabel);
                string sizeLable = string.Format("{0:0.00} mb{1}", runtimeSize, usedLabel);
                string fromat = objectData.Item2.format;
                string mipCount = string.Format("Mip:{0}", objectData.Item2.mipCount);
                string dimentionLable = string.Format("W:{0} H:{1}", objectData.Item2.width, objectData.Item2.height);
                var pbb_Comp = UnityInsightsHelper.GetTextureFormatInfo(fromat);
                string pbbAndCompressionStr = string.Format("bpp: {0} {1} {2}", pbb_Comp.Item1, pbb_Comp.Item2 ? "Comp." : "", objectData.Item2.isReadable ? "R/W" : "");

                var textureField = EditorGUILayout.ObjectField(objectData.Item1, typeof(Texture), false, GUILayout.Width(160));
                if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    RuntimeOptimizerPlugin.SendEvent("open_texture", "");
                }
                EditorGUILayout.LabelField("", GUILayout.Width(30));
                EditorGUILayout.LabelField(sizeLable, GUILayout.Width(70));

                EditorGUILayout.LabelField(dimentionLable, GUILayout.Width(100));
                EditorGUILayout.LabelField(mipCount, GUILayout.Width(60));
                EditorGUILayout.LabelField(fromat, GUILayout.Width(90));
                EditorGUILayout.LabelField(pbbAndCompressionStr, GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }
        }

        public static readonly GUIStyle UnitText = new GUIStyle()
        {
            fontSize = 10,
            normal =
            {
                textColor = new Color32(196,196,196,255)
            }
        };

        void DrawMaterialSection(InsightData insight)
        {
            EditorGUILayout.BeginHorizontal();
            int firstLabelWidth = 150;
            EditorGUILayout.LabelField("  Material Analysis", UnitText, GUILayout.Width(firstLabelWidth));

            EditorGUILayout.LabelField("Instructions (PS/VS)", UnitText, GUILayout.Width(140));

            EditorGUILayout.LabelField("fp16 ", UnitText, GUILayout.Width(75));
            EditorGUILayout.LabelField("TexRead", UnitText, GUILayout.Width(95));
            EditorGUILayout.LabelField("Reg ", UnitText, GUILayout.Width(80));

            GUILayout.EndHorizontal();
            foreach (var objectData in insight.materialArray)
            {
                EditorGUILayout.BeginHorizontal("box");

                // EditorGUILayout.LabelField($"Used by {objectData.Item2.usedByCount.ToString()} GOs", GUILayout.Width(160));

                var objectField = EditorGUILayout.ObjectField(objectData.Item1, typeof(Material), false, GUILayout.Width(160));

                if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    RuntimeOptimizerPlugin.SendEvent("open_material", "");
                }

                var defaultColor = GUI.color;
                const int kLabelWidth = 25;
                // 0 instructions, 2 fp16 instructions, 9 texture fetchs, 15 registers
                int[] shaderStatMode = { 0, 2, 9, 15 };
                if (objectData.Item2.shaderStats != null && objectData.Item2.shaderStats.Count > 0)
                {
                    AdrenoOfflineCompilerUtilityRO.ShaderStatInfo stat = objectData.Item2.shaderStats[0];
                    for (int k = 0; k < shaderStatMode.Length; ++k)
                    {
                        int i = shaderStatMode[k];
                        EditorGUILayout.LabelField("", GUILayout.Width(30));
                        GUI.color = AdrenoOfflineCompilerUtilityRO.GetStatColor(stat.fragmentMainStats[i], AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].fragmentMainRed,
                            AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].fragmentMainGreen);
                        EditorGUILayout.LabelField(stat.fragmentMainStats[i] + "", GUILayout.Width(kLabelWidth));

                        GUI.color = AdrenoOfflineCompilerUtilityRO.GetStatColor(stat.vertexStats[i], AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].vertexRed,
                            AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].vertexGreen);
                        EditorGUILayout.LabelField(stat.vertexStats[i] + "", GUILayout.Width(kLabelWidth));
                    }
                }
                GUI.color = defaultColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void HandleDragging(Rect rect)
        {
            Event currentEvent = Event.current;

            // Check if mouse is over the drag handle
            if (currentEvent.type == EventType.MouseDown && dragHandleRect.Contains(currentEvent.mousePosition))
            {
                isDragging = true;
                dragStartPosition = currentEvent.mousePosition;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && isDragging)
            {
                isDragging = false;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && isDragging)
            {
                // Calculate new position
                Vector2 delta = currentEvent.mousePosition - dragStartPosition;
                Vector2 newPosition = new Vector2(
                    editorWindow.position.x + delta.x,
                    editorWindow.position.y + delta.y
                );

                // Update window position
                editorWindow.position = new Rect(newPosition.x, newPosition.y, windowWidth, windowHeight);
                currentEvent.Use();
            }
        }

        /// <summary>
        /// Shows the popup at the specified position
        /// </summary>
        public static void ShowAtPosition(Rect position, InsightData insights, PopupForType arrayToShow)
        {
            PopupWindow.Show(position, new RuntimeOptimizerDetailPopup(insights, arrayToShow));
        }
    }
}
