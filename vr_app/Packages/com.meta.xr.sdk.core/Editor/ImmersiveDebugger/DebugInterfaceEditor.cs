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

using Meta.XR.Editor.Id;
using Meta.XR.ImmersiveDebugger.UserInterface;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles;
using static Meta.XR.Editor.UserInterface.Styles.Constants;
using static Meta.XR.Editor.UserInterface.Styles.Contents;
using static Meta.XR.Editor.UserInterface.Utils;

namespace Meta.XR.ImmersiveDebugger.Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="DebugInterface"/>.
    /// Provides buttons and controls for testing Immersive Debugger features in Editor Play Mode
    /// without requiring VR controllers or headset.
    /// </summary>
    [CustomEditor(typeof(DebugInterface))]
    public class DebugInterfaceEditor : UnityEditor.Editor
    {
        private DebugInterface _debugInterface;
        private float _dialogNoticeHorizontalPadding;
        private float _dialogNoticeVerticalPadding;
        private GUIContent _dialogNotice;

        private void Awake()
        {
            _dialogNotice = new GUIContent($"<b>{target.GetType().Name}</b> is part of the <b>{Utils.PublicName}</b>." +
                                           $"\nUse the controls below to test the debugger in Play Mode without VR hardware.");

            _dialogNoticeHorizontalPadding = 3
                                             + GUIStyles.DialogIconStyle.fixedWidth
                                             + GUIStyles.DialogBox.padding.left
                                             + GUIStyles.DialogBox.padding.right;

            _dialogNoticeVerticalPadding = GUIStyles.DialogBox.padding.bottom + GUIStyles.DialogBox.padding.top;
        }

        private void OnEnable()
        {
            _debugInterface = (DebugInterface)target;
        }

        public override void OnInspectorGUI()
        {
            ShowHeaderGUI();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use Debug Interface controls.", MessageType.Info);
                return;
            }

            if (_debugInterface == null)
            {
                EditorGUILayout.HelpBox("DebugInterface reference is not available.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(Margin);
            DrawDisplayControls();
            EditorGUILayout.Space(Margin);
            DrawPanelVisibilityControls();
        }

        private void ShowHeaderGUI()
        {
            var currentWidth = EditorGUIUtility.currentViewWidth;
            var expectedButtonHeight = Meta.XR.Editor.ToolingSupport.Styles.Constants.Height;

            GUILayout.BeginArea(new Rect(0, 0, currentWidth, expectedButtonHeight));
            EditorGUILayout.BeginHorizontal();
            Utils.ToolDescriptor.DrawButton(null, false, true, Origins.Component);
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
            GUILayoutUtility.GetRect(currentWidth, expectedButtonHeight);

            var infoWidth = currentWidth - _dialogNoticeHorizontalPadding;
            var expectedInfoHeight = GUIStyles.DialogTextStyle.CalcHeight(_dialogNotice, infoWidth);
            expectedInfoHeight += _dialogNoticeVerticalPadding;
            GUILayout.BeginArea(new Rect(0, expectedButtonHeight, currentWidth, expectedInfoHeight));
            EditorGUILayout.BeginHorizontal(GUIStyles.DialogBox);
            EditorGUILayout.LabelField(DialogIcon, GUIStyles.DialogIconStyle,
                GUILayout.Width(GUIStyles.DialogIconStyle.fixedWidth));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(_dialogNotice, GUIStyles.DialogTextStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayoutUtility.GetRect(currentWidth, expectedInfoHeight);
        }

        private void DrawDisplayControls()
        {
            EditorGUILayout.LabelField("Display Controls", GUIStyles.BoldLabel);

            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);

            DrawToggleRow("Toggle Display", _debugInterface.Visibility, () => _debugInterface.ToggleVisibility());
            DrawToggleRow("Follow Translation", _debugInterface.FollowOverrideState, () => _debugInterface.ToggleFollowTranslation());
            DrawToggleRow("Follow Rotation", _debugInterface.RotateOverrideState, () => _debugInterface.ToggleFollowRotation());
            DrawToggleRow("Opacity", _debugInterface.OpacityOverrideState, () => _debugInterface.ToggleOpacity());

            EditorGUILayout.EndVertical();
        }

        private void DrawPanelVisibilityControls()
        {
            EditorGUILayout.LabelField("Panel Visibility", GUIStyles.BoldLabel);

            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);

            var bar = _debugInterface.Bar;
            if (bar == null || bar.RegisteredPanels == null || bar.RegisteredPanels.Count == 0)
            {
                EditorGUILayout.LabelField("No panels registered.", EditorStyles.label);
            }
            else
            {
                foreach (var panel in bar.RegisteredPanels)
                {
                    if (panel == null) continue;
                    DrawPanelRow(panel);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToggleRow(string label, bool currentState, System.Action onToggle)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            using (new ColorScope(ColorScope.Scope.All, currentState ? Styles.Colors.AccentColorBrighter : Color.white))
            {
                if (GUILayout.Toggle(currentState, "", GUILayout.Width(16)))
                {
                    if (!currentState) onToggle?.Invoke();
                }
                else
                {
                    if (currentState) onToggle?.Invoke();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPanelRow(DebugPanel panel)
        {
            EditorGUILayout.BeginHorizontal();

            var panelName = !string.IsNullOrEmpty(panel.Title) ? panel.Title : panel.name;
            EditorGUILayout.LabelField(panelName, EditorStyles.label, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            using (new ColorScope(ColorScope.Scope.All, panel.Visibility ? Styles.Colors.AccentColorBrighter : Color.white))
            {
                var newState = GUILayout.Toggle(panel.Visibility, "", GUILayout.Width(16));
                if (newState != panel.Visibility)
                {
                    panel.ToggleVisibility();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
