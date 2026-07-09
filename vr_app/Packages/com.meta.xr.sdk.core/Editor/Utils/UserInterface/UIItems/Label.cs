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
using Meta.XR.Editor.Id;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2023_1_OR_NEWER
using UnityEngine.UIElements.Experimental;
#endif
using static Meta.XR.Editor.UserInterface.Styles.Constants;

namespace Meta.XR.Editor.UserInterface
{
    /// <summary>
    /// Make a read-only label.
    /// </summary>
    internal class Label : IUserInterfaceItem, IDynamicColorItem
    {
        private VisualElement _visualElement;
        private readonly string _typography;
        private UnityEngine.UIElements.Label _uiLabel;

        public bool Hide { get; set; }
        public GUIContent LabelContent { get; set; }
        public readonly GUIStyle GUIStyle;
        private readonly GUILayoutOption[] _options;
        public Func<IDynamicColorItem, Color> FetchDynamicColor { get; set; }

        /// <summary>
        /// Origin context for link click telemetry. Set this to indicate where the label is displayed.
        /// </summary>
        public Origins LinkOrigin { get; set; } = Origins.Unknown;

        /// <summary>
        /// Origin data for link click telemetry. Typically the parent IIdentified object (e.g., the Guide).
        /// </summary>
        public IIdentified LinkOriginData { get; set; }

        public Label(string label, params GUILayoutOption[] options) : this(label, UIStyles.GUIStyles.Label, options)
        {
        }

        public Label(string label, GUIStyle style, params GUILayoutOption[] options)
        {
            LabelContent = new GUIContent(label);
            GUIStyle = new GUIStyle(style);
            _options = options;
        }

        /// <summary>
        /// Constructor to use in UIToolkit based environment
        /// </summary>
        /// <param name="label">Label text to show</param>
        /// <param name="typography"><see cref="RLDS.Props.Typography"/> for the typographic variants</param>
        public Label(string label, string typography)
        {
            LabelContent = new GUIContent(label);
            _typography = typography;
        }

        public void Draw()
        {
            if (FetchDynamicColor != null)
            {
                var expectedColor = FetchDynamicColor?.Invoke(this) ?? Color.white;
                // Splitting the behaviour to not play with color scope in case no color was set at all
                using (new Utils.ColorScope(Utils.ColorScope.Scope.Content, expectedColor))
                {
                    EditorGUILayout.LabelField(LabelContent.text, GUIStyle, _options);
                }
            }
            else
            {
                EditorGUILayout.LabelField(LabelContent.text, GUIStyle, _options);
            }

        }

        public float GetHeight(float contentWidth = UIStyles.Constants.DefaultWidth - LargeMargin) => GUIStyle.CalcHeight(LabelContent, contentWidth);
        public float GetWidth() => GUIStyle.CalcSize(LabelContent).x;

        /// <summary>
        /// Creates a UIToolkit Label element with RLDS styling applied.
        /// This method provides an alternative to the IMGUI Draw() method for UIToolkit-based workflows.
        /// </summary>
        /// <returns>A VisualElement containing the styled label</returns>
        public VisualElement Build()
        {
            if (_visualElement != null)
            {
                return _visualElement;
            }
            _visualElement = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = RLDS.Styles.Spacing.Space4XS,
                    marginBottom = RLDS.Styles.Spacing.Space4XS
                }
            };

            _uiLabel = new UnityEngine.UIElements.Label(LabelContent.text);
            if (!string.IsNullOrEmpty(_typography))
            {
                _uiLabel.AddToClassList(_typography);
            }
            _uiLabel.style.whiteSpace = WhiteSpace.Normal;
            _uiLabel.enableRichText = true;

            if (GUIStyle != null)
            {
                ApplyStyleToLabel(GUIStyle);
            }

            if (FetchDynamicColor != null)
            {
                _uiLabel.style.color = FetchDynamicColor.Invoke(this);
            }

            // Handle link clicks with telemetry (Unity 2023.1+ feature)
#if UNITY_2023_1_OR_NEWER
            _uiLabel.RegisterCallback<PointerUpLinkTagEvent>(evt => HandleLinkClick(evt.linkID, evt.linkText));
#endif
            _visualElement.Add(_uiLabel);

            return _visualElement;
        }

        /// <summary>
        /// Handles link clicks by creating a UrlLinkDescription with telemetry and opening the URL.
        /// Extracted as internal method to enable unit testing.
        /// </summary>
        /// <param name="linkUrl">The URL to open (from link href)</param>
        /// <param name="linkText">The display text of the link</param>
        internal void HandleLinkClick(string linkUrl, string linkText)
        {
            if (string.IsNullOrEmpty(linkUrl))
            {
                return;
            }

            var linkDescription = new UrlLinkDescription
            {
                Content = new GUIContent(linkText),
                URL = linkUrl,
                Origin = LinkOrigin,
                OriginData = LinkOriginData
            };
            linkDescription.Click();
        }

        /// <summary>
        /// Bridges IMGUI styling to UIToolkit by transferring relevant GUIStyle properties.
        /// This enables labels created with IMGUI GUIStyle constructors to render consistently
        /// when displayed via UIToolkit's Build() method, preserving font size, style, and margins.
        /// </summary>
        /// <param name="style">The GUIStyle to apply to the UIToolkit label</param>
        private void ApplyStyleToLabel(GUIStyle style)
        {
            if (style.fontSize > 0)
            {
                _uiLabel.style.fontSize = style.fontSize;
            }

            if (style.fontStyle == FontStyle.Bold || style.fontStyle == FontStyle.BoldAndItalic)
            {
                _uiLabel.style.unityFontStyleAndWeight = style.fontStyle == FontStyle.BoldAndItalic
                    ? FontStyle.BoldAndItalic
                    : FontStyle.Bold;
            }

            if (style.fontStyle == FontStyle.Italic || style.fontStyle == FontStyle.BoldAndItalic)
            {
                _uiLabel.style.unityFontStyleAndWeight = style.fontStyle == FontStyle.BoldAndItalic
                    ? FontStyle.BoldAndItalic
                    : FontStyle.Italic;
            }

            if (style.margin.bottom > 0)
            {
                _visualElement.style.marginBottom = style.margin.bottom;
            }
        }
    }
}
