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

using UnityEditor;
using UnityEngine;
using static Oculus.Interaction.UITheme;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(UITheme))]
    public class UIThemeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            UITheme selectedTheme = (UITheme)target;

            // Save Theme File button
            GUILayout.Space(20);
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 20;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            GUI.backgroundColor = Color.green;
            GUIContent buttonContent = new GUIContent("Save Theme File", "Click to save the current theme file and update the animation clips.");
            if (GUILayout.Button(buttonContent, buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(60)))
            {
                UpdateAnimationClips(selectedTheme);
            }
        }

        private void UpdateClipColors(AnimationClip[] clips, ElementColors colors, string prefix)
        {
            foreach (var clip in clips)
            {
                if (clip.name == $"{prefix}Normal")
                {
                    ColorCurve(clip, colors.normal, "m_Color");
                }
                else if (clip.name == $"{prefix}Highlighted")
                {
                    ColorCurve(clip, colors.highlighted, "m_Color");
                }
                else if (clip.name == $"{prefix}Pressed")
                {
                    ColorCurve(clip, colors.pressed, "m_Color");
                }
                else if (clip.name == $"{prefix}Selected")
                {
                    ColorCurve(clip, colors.selected, "m_Color");
                }
                else if (clip.name == $"{prefix}Disabled")
                {
                    ColorCurve(clip, colors.disabled, "m_Color");
                }
                EditorUtility.SetDirty(clip);
            }
        }

        private void UpdateToggleClips(AnimationClip[] clips, ElementColors unselectedColors, ElementColors selectedColors)
        {
            UpdateClipColors(clips, unselectedColors, "");
            UpdateClipColors(clips, selectedColors, "Selected");
        }

        private void UpdateAnimationClips(UITheme selectedTheme)
        {
            UpdateClipColors(selectedTheme.acPrimaryButton.animationClips, selectedTheme.primaryButton, "");
            UpdateClipColors(selectedTheme.acSecondaryButton.animationClips, selectedTheme.secondaryButton, "");
            UpdateClipColors(selectedTheme.acBorderlessButton.animationClips, selectedTheme.borderlessButton, "");
            UpdateClipColors(selectedTheme.acDestructiveButton.animationClips, selectedTheme.destructiveButton, "");

            UpdateClipColors(selectedTheme.acTextInputField.animationClips, selectedTheme.secondaryButton, "");

            UpdateToggleClips(selectedTheme.acToggleButton.animationClips, selectedTheme.secondaryButton, selectedTheme.primaryButton);
            UpdateToggleClips(selectedTheme.acToggleBorderlessButton.animationClips, selectedTheme.borderlessButton, selectedTheme.primaryButton);
            UpdateToggleClips(selectedTheme.acToggleSwitch.animationClips, selectedTheme.secondaryButton, selectedTheme.primaryButton);
            UpdateToggleClips(selectedTheme.acToggleCheckboxRadio.animationClips, selectedTheme.secondaryButton, selectedTheme.primaryButton);

            // Writes all unsaved asset changes to disk
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Theme and animation clips updated.");
        }

        private void ColorCurve(AnimationClip clip, Color targetColor, string propName)
        {
            UITheme selectedTheme = (UITheme)target;

            var channelName = new char[] { 'r', 'g', 'b', 'a' };
            for (int i = 0; i < 4; i++)
            {
                var value = targetColor[i];
                var c = channelName[i];
                var colorCurveChannel = new AnimationCurve();
                colorCurveChannel.AddKey(0.0f, value);
                clip.SetCurve(selectedTheme.colorPath, typeof(UnityEngine.UI.Image), $"{propName}.{c}", colorCurveChannel);
            }
        }
    }
}
