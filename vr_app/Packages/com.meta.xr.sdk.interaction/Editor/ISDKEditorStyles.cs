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

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    public class ISDKEditorStyles
    {
        public GUIStyle DialogIconStyle => _dialogIconStyle ??= new GUIStyle()
        {
            fixedHeight = 48,
            fixedWidth = 48,
            stretchWidth = false,
            alignment = TextAnchor.MiddleCenter
        };

        public GUIStyle HeaderText => _headerText ??= new GUIStyle(EditorStyles.largeLabel)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            fixedHeight = 48,
            stretchWidth = true,
            padding = new RectOffset(8, 0, 0, 0),
        };

        public GUIStyle ContentArea => _contentArea ??= new GUIStyle(EditorStyles.textArea)
        {
            stretchHeight = true,
            padding = new RectOffset(4, 4, 4, 4),
        };

        public GUIStyle ContentText => _contentText ??= new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            richText = true,
            stretchHeight = true,
            padding = new RectOffset(4, 4, 4, 4),
        };

        public GUIStyle Button => _button ??= new GUIStyle(EditorStyles.miniButton)
        {
            stretchWidth = true,
            fixedHeight = 36,
            richText = true,
        };

        public GUIStyle ContentButton => _contentButton ??= new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            padding = new RectOffset(4, 4, 4, 4),
            richText = true
        };

        public GUIStyle WindowContents => _windowContents ??= new GUIStyle()
        {
            padding = new RectOffset(8, 8, 8, 8)
        };

        private const string META_ICON_NAME = "meta_icon_blue.png";

        private GUIStyle _dialogIconStyle;
        private GUIStyle _headerText;
        private GUIStyle _contentArea;
        private GUIStyle _contentText;
        private GUIStyle _button;
        private GUIStyle _contentButton;
        private GUIStyle _windowContents;

        private static string GetIconPath(string iconName)
        {
            string[] assets = AssetDatabase.FindAssets($"t:Script {nameof(ISDKEditorStyles)}");
            string dir = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(assets[0]));
            string iconPath = Path.Combine(dir, "Icons", iconName);
            return iconPath;
        }

        public void DrawTitle(string title)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    var content = EditorGUIUtility.TrIconContent(GetIconPath(META_ICON_NAME), "Meta Logo");
                    EditorGUILayout.LabelField(content, DialogIconStyle,
                        GUILayout.Width(DialogIconStyle.fixedWidth),
                        GUILayout.Height(DialogIconStyle.fixedHeight));
                    EditorGUILayout.LabelField(title, HeaderText);
                }
            }
        }

        public void DrawContent(string content, System.Action drawExtra = null)
        {
            GUI.enabled = false; // Prevent text area from highlighting on hover
            using (new EditorGUILayout.VerticalScope(ContentArea))
            {
                GUI.enabled = true;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    EditorGUILayout.LabelField(content, ContentText);
                }
                drawExtra?.Invoke();
            }
        }
    }
}
