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

#if ISDK_OPENXR_HAND
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.PoseDetection;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Oculus.Interaction.Editor.AssetListWindow;

namespace Oculus.Interaction.Editor
{
    public class OpenXRMigrationWindow
    {
        private static Type[] _migrableTypes = {
            typeof(HandJoint),
            typeof(HandJointsPose),
            typeof(HandRootOffset),
            typeof(HandPinchOffset),
            typeof(HandGrabPose),
            typeof(HandSphereMap),
            typeof(JointVelocityActiveState),
            typeof(JointRotationActiveState),
            typeof(JointDistanceActiveState),
        };

        private const string OPENXR_DOCS_LINK = "https://developers.meta.com/horizon/documentation/unity/unity-isdk-openxr-hand";

        private static ISDKEditorStyles _styles = new ISDKEditorStyles();

        private static Dictionary<AssetListWindow, Dictionary<int, UnityEditor.Editor>> _cachedEditors
            = new Dictionary<AssetListWindow, Dictionary<int, UnityEditor.Editor>>();

        [MenuItem("Meta/Interaction SDK/OpenXR Migration Tool")]
        public static void ShowWindow()
        {
            //unified entry point for populating the table
            Action<AssetListWindow> handleEnabled = (window) =>
            {
                GetAssets(out List<AssetInfo> assets,
                    out Dictionary<int, UnityEditor.Editor> editors);

                ClearEditors(window);
                _cachedEditors.Add(window, editors);
                window.SetAssets(assets);
            };

            //disabled is called also when changing betweewn play/editor mode
            Action<AssetListWindow> handleDisabled = (window) =>
            {
                ClearEditors(window);
            };

            //teardown references on destroy. Disabled is called always before
            Action<AssetListWindow> handleDestroyed = null;
            handleDestroyed = (window) =>
            {
                window.WhenDestroyed -= handleDestroyed;
                window.WhenEnabled -= handleEnabled;
                window.WhenDisabled -= handleDisabled;
            };

            AssetListWindow window = Show("OpenXR Migration Tool",
                new List<AssetInfo>(), //initialize to empty array
                false,
                DrawHeader,
                DrawFooter,
                DrawContent);

            //force populate from common callback
            handleEnabled(window);

            window.WhenEnabled += handleEnabled;
            window.WhenDisabled += handleDisabled;
            window.WhenDestroyed += handleDestroyed;
        }

        private static void GetAssets(out List<AssetInfo> assets, out Dictionary<int, UnityEditor.Editor> editors)
        {
            List<UnityEngine.Object> foundObjects = FindAllObjects();
            assets = ToAssetInfos(foundObjects);
            editors = CreateEditors(foundObjects);
        }

        internal static List<UnityEngine.Object> FindAllObjects()
        {
            List<UnityEngine.Object> foundComponents = new List<UnityEngine.Object>();
            foreach (Type migrableType in _migrableTypes)
            {
                UnityEngine.Object[] objects =
                    UnityEngine.Object.FindObjectsByType(migrableType, FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                if (objects != null)
                {
                    foundComponents.AddRange(objects);
                }
            }
            return foundComponents;
        }

        private static List<AssetInfo> ToAssetInfos(List<UnityEngine.Object> objects)
        {
            List<AssetInfo> infos = objects.Select(obj =>
            {
                int instanceID = obj.GetHashCode();
                return new AssetInfo(instanceID.ToString(),
                    obj.name,
                    null);
            }).ToList();
            return infos;
        }

        private static Dictionary<int, UnityEditor.Editor> CreateEditors(List<UnityEngine.Object> objects)
        {
            Dictionary<int, UnityEditor.Editor> editors = new Dictionary<int, UnityEditor.Editor>();
            foreach (var obj in objects)
            {
                var editor = UnityEditor.Editor.CreateEditor(obj);
                editors.Add(obj.GetHashCode(), editor);
            }
            return editors;
        }

        private static void ClearEditors(AssetListWindow window)
        {
            if (!_cachedEditors.TryGetValue(window, out var editors))
            {
                return;
            }

            foreach (var editorKvp in editors)
            {
                if (editorKvp.Value is UnityEngine.Object editor)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }
            }
            editors.Clear();
            _cachedEditors.Remove(window);
        }

        private static bool TryGetEditor(AssetListWindow window, UnityEngine.Object obj, out UnityEditor.Editor editor)
        {
            editor = null;
            return (_cachedEditors.TryGetValue(window, out var editors)
                && editors.TryGetValue(obj.GetHashCode(), out editor));
        }

        private static bool TryGetInstanceId(AssetInfo info, out int instanceId)
        {
            return int.TryParse(info.AssetPath, out instanceId);
        }

        private static void DrawContent(AssetListWindow window, int index, AssetInfo assetInfo)
        {
            using (new EditorGUILayout.HorizontalScope(_styles.ContentArea))
            {
                bool success = TryNeedsConversion(window, assetInfo, out bool needsConversion);
                string color = success ? needsConversion ? "orange" : "lime" : "grey";
                string subtitle = success ? needsConversion ? "(needs convertion)"
                    : "(converted)" : "(can't convert)";
                string name = $"<color={color}>{assetInfo.DisplayName}</color> {subtitle}";
                EditorGUILayout.LabelField(name, _styles.ContentText);

                Rect labelRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(labelRect, string.Empty, GUIStyle.none))
                {
                    SelectAsset(assetInfo);
                }
                if (GUILayout.Button("Convert to OpenXR"))
                {
                    Convert(window, GetObject(assetInfo));
                }
            }
        }

        private static void SelectAsset(AssetInfo info)
        {
            if (TryGetInstanceId(info, out int instanceID))
            {
#if UNITY_6000_3_OR_NEWER
                Selection.activeEntityId = instanceID;
#else
                Selection.activeInstanceID = instanceID;
#endif
            }
        }

        private static UnityEngine.Object GetObject(AssetInfo info)
        {
            if (TryGetInstanceId(info, out int instanceID))
            {
#if UNITY_6000_3_OR_NEWER
                return EditorUtility.EntityIdToObject(instanceID);
#else
                return EditorUtility.InstanceIDToObject(instanceID);
#endif
            }
            return null;
        }


        private static void Convert(AssetListWindow window, UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!TryGetEditor(window, obj, out var editor)
                || editor is not IOpenXRMigrableEditor migrableEditor)
            {
                return;
            }

            migrableEditor.Convert(editor.serializedObject);
            editor.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(obj);
        }


        private static bool TryNeedsConversion(AssetListWindow window, AssetInfo info, out bool needsConversion)
        {
            UnityEngine.Object obj = GetObject(info);
            if (obj != null)
            {
                return TryNeedsConversion(window, obj, out needsConversion);
            }

            needsConversion = false;
            return false;
        }

        private static bool TryNeedsConversion(AssetListWindow window, UnityEngine.Object obj, out bool needsConversion)
        {
            if (obj == null)
            {
                needsConversion = false;
                return false;
            }

            if (!TryGetEditor(window, obj, out var editor)
                || editor is not IOpenXRMigrableEditor migrableEditor)
            {
                needsConversion = false;
                return false;
            }

            needsConversion = migrableEditor.NeedsConversion(editor.serializedObject);
            return true;
        }

        private static void DrawHeader(AssetListWindow window)
        {
            string content = "Use this tool to instantly <b>locate and convert</b> the components " +
                "in the scene from having <b>OVR</b>-centric parameters to <b>OpenXR</b>-centric parameters. " +
                "This includes hand-space offsets, hand joint identifiers and others. Note that you can also convert " +
                "the components one by one by using the <b>Convert</b> button in the inspector.\n" +

                "\n- Click in any row to highlight the component in the inspector.\n" +
                "- Click the row button to convert the component parameters from OVR to OpenXR.\n" +
                "- Click the footer button to convert all remaining components from OVR to OpenXR.\n" +

                "\nThis tool upgrades only <b>Interaction SDK</b> components, if you have created your " +
                "own OVR-centric assets, please refer to the documentation to migrate them manually.";

            using (new EditorGUILayout.VerticalScope(_styles.WindowContents))
            {
                _styles.DrawTitle("Interaction SDK OpenXR Migration tool");
                _styles.DrawContent(content, () =>
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(
                        "For more details please see the <color=#6060ee>Interaction SDK documentation.</color>",
                        _styles.ContentButton))
                    {
                        Application.OpenURL(OPENXR_DOCS_LINK);
                    }
                });
            }
        }

        private static void DrawFooter(AssetListWindow window)
        {
            using (new EditorGUILayout.VerticalScope(_styles.WindowContents))
            {
                if (GUILayout.Button("Convert missing to OpenXR", _styles.Button))
                {
                    foreach (AssetInfo asset in window.AssetInfos)
                    {
                        UnityEngine.Object obj = GetObject(asset);
                        if (obj == null)
                        {
                            continue;
                        }

                        if (TryNeedsConversion(window, obj, out bool needsConversion)
                            && needsConversion)
                        {
                            Convert(window, obj);
                        }
                    }
                }
            }
        }

    }
}
#endif

