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

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Oculus.Interaction.Editor
{
    public static class OpenXRUpgrade
    {
        public static event Action OnUpdate;
        public static void UpgradeToOpenXR()
        {
            OnUpdate?.Invoke();
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }
    }

    internal class OpenXRUpgradeWindow : EditorWindow
    {
        private const string OPENXR_DOCS_LINK = "https://developers.meta.com/horizon/documentation/unity/unity-isdk-openxr-hand";

        private static ISDKEditorStyles _styles;

        public static OpenXRUpgradeWindow ShowWindow()
        {
            var curWindow = GetWindow<OpenXRUpgradeWindow>(true);
            return curWindow;
        }

        public static string GetIconPath(string iconName)
        {
            var g = AssetDatabase.FindAssets($"t:Script {nameof(PackageUpgradeOpenXR)}");
            return Path.Combine(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(g[0])), "Icons/", iconName);
        }

        private void Awake()
        {
            _styles ??= new ISDKEditorStyles();
            titleContent = new GUIContent("Interaction SDK OpenXR Upgrade");
            minSize = new Vector2(640, 290);
            maxSize = minSize + new Vector2(2, 2);
            EditorApplication.delayCall += () => maxSize = new Vector2(4000, 4000);
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CloseWindow();
            }

            using (new EditorGUILayout.VerticalScope(_styles.WindowContents))
            {
                DrawTitle();
                EditorGUILayout.Space(8);
                DrawContent();
                GUILayout.FlexibleSpace();
                EditorGUILayout.Space(8);
                DrawFooter();
            }
        }

        private void DrawTitle()
        {
            _styles.DrawTitle("Interaction SDK OpenXR Hand Skeleton Upgrade");
        }

        private void DrawContent()
        {
            string content =
                    "As of version 78, Interaction SDK has discontinued support for the OVR hand skeleton." +
                    "\n\n" +
                    "If your project has dependencies on the OVR hand skeleton and needs to be upgraded to OpenXR, " +
                    "tools have been included in the Inspector to upgrade built-in components, though some " +
                    "manual work may be required in certain situations." +
                    "\n\n" +
                    "If OVR compatibility is required in order to perform this upgrade, then it is recommended that you " +
                    "perform the upgrade using a previous version of Interaction SDK.";

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

        private void DrawFooter()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Height(36)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Close", _styles.Button))
                    {
                        CloseWindow();
                    }
                }
            }
        }

        private void CloseWindow()
        {
            Close();
        }
    }

    [InitializeOnLoad]
    internal class PackageUpgradeOpenXR
    {
        private const string MENU_NAME = "Meta/Interaction/Upgrade to OpenXR Hand Skeleton";

        private const string KEY_BASE = "Oculus_Interaction_PackageUpgradeOpenXR";
        private const string KEY_DONTASK = KEY_BASE + "_DontAsk";
        private const string KEY_ASKLATER = KEY_BASE + "_AskLater";

        [MenuItem(MENU_NAME)]
        public static void ShowUpgradeWindow()
        {
            EditorPrefs.DeleteKey(KEY_DONTASK);
            SessionState.EraseBool(KEY_ASKLATER);
            CloseOpenWindows();
            OpenXRUpgradeWindow window = OpenXRUpgradeWindow.ShowWindow();
        }

        private static void CloseOpenWindows()
        {
            if (EditorWindow.HasOpenInstances<OpenXRUpgradeWindow>())
            {
                EditorWindow.GetWindow<OpenXRUpgradeWindow>()?.Close();
            }
        }
    }
}
