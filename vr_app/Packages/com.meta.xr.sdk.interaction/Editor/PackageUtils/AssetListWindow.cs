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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oculus.Interaction.Editor
{
    public class AssetListWindow : EditorWindow
    {
        public class AssetInfo
        {
            public readonly string AssetPath;
            public readonly string DisplayName;
            public readonly Action ClickAction;

            public AssetInfo(string assetPath) : this(assetPath, assetPath) { }

            public AssetInfo(string assetPath, string displayName, Action clickAction) :
                this(assetPath, displayName)
            {
                ClickAction = clickAction;
            }

            public AssetInfo(string assetPath, string displayName)
            {
                AssetPath = assetPath;
                DisplayName = displayName;
                ClickAction = () => PingObject(assetPath);
            }
        }

        private List<AssetInfo> _assetInfos;
        private Vector2 _scrollPos;

        private Action<AssetListWindow, int, AssetInfo> _contentDrawer;
        private Action<AssetListWindow> _headerDrawer;
        private Action<AssetListWindow> _footerDrawer;

        public event Action<AssetListWindow> WhenEnabled = delegate { };
        public event Action<AssetListWindow> WhenDisabled = delegate { };
        public event Action<AssetListWindow> WhenDestroyed = delegate { };

        public IReadOnlyList<AssetInfo> AssetInfos => _assetInfos;

        public static AssetListWindow Show(
            string title,
            IEnumerable<string> assetPaths,
            bool modal = false,
            Action<AssetListWindow> headerDrawer = null,
            Action<AssetListWindow> footerDrawer = null,
            Action<AssetListWindow, int, AssetInfo> contentDrawer = null)
        {
            List<AssetInfo> assetInfos = new List<AssetInfo>();
            foreach (var path in assetPaths)
            {
                assetInfos.Add(new AssetInfo(path));
            }
            return Show(title, assetInfos, modal, headerDrawer, footerDrawer, contentDrawer);
        }

        public static AssetListWindow Show(
            string title,
            IEnumerable<AssetInfo> assetInfos,
            bool modal = false,
            Action<AssetListWindow> headerDrawer = null,
            Action<AssetListWindow> footerDrawer = null,
            Action<AssetListWindow, int, AssetInfo> contentDrawer = null)
        {
            AssetListWindow window = GetWindow<AssetListWindow>(true);
            window.SetTitle(title);
            window.SetAssets(assetInfos);
            window.SetHeader(headerDrawer);
            window.SetFooter(footerDrawer);
            window.SetContentDrawer(contentDrawer);

            if (modal)
            {
                window.ShowModalUtility();
            }
            else
            {
                window.ShowUtility();
            }

            return window;
        }

        public static void CloseAll()
        {
            if (HasOpenInstances<AssetListWindow>())
            {
                AssetListWindow window = GetWindow<AssetListWindow>(true);
                window.Close();
            }
        }

        public void SetTitle(string title)
        {
            titleContent = new GUIContent(title);
        }

        public void SetAssets(IEnumerable<AssetInfo> assetInfos)
        {
            _assetInfos = new List<AssetInfo>(assetInfos);
        }

        public void SetHeader(Action<AssetListWindow> headerDrawer)
        {
            _headerDrawer = headerDrawer;
        }

        public void SetFooter(Action<AssetListWindow> footerDrawer)
        {
            _footerDrawer = footerDrawer;
        }

        public void SetContentDrawer(Action<AssetListWindow, int, AssetInfo> contentDrawer)
        {
            _contentDrawer = contentDrawer;
        }

        private void OnGUI()
        {
            DrawHeader();
            if (_assetInfos != null && _assetInfos.Count > 0)
            {
                DrawContent();
            }
            DrawFooter();
        }

        private void DrawHeader()
        {
            if (_headerDrawer == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            _headerDrawer.Invoke(this);
            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            if (_footerDrawer == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            _footerDrawer.Invoke(this);
            EditorGUILayout.EndVertical();
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginVertical();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _assetInfos.Count; i++)
            {
                AssetInfo assetInfo = _assetInfos[i];
                if (_contentDrawer != null)
                {
                    _contentDrawer.Invoke(this, i, assetInfo);
                }
                else
                {
                    DrawAssetInfo(assetInfo);
                }

            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void DrawAssetInfo(AssetInfo assetInfo)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            if (GUI.Button(rect, "", GUIStyle.none))
            {
                assetInfo.ClickAction.Invoke();
            }
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            EditorGUILayout.LabelField(assetInfo.DisplayName, style);
            EditorGUILayout.EndHorizontal();
        }

        private static void PingObject(string assetPath)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(
                 assetPath, typeof(Object));

            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
            }
        }

        private void OnEnable()
        {
            WhenEnabled.Invoke(this);
        }

        private void OnDisable()
        {
            WhenDisabled.Invoke(this);
        }

        private void OnDestroy()
        {
            WhenDestroyed.Invoke(this);
        }
    }
}
