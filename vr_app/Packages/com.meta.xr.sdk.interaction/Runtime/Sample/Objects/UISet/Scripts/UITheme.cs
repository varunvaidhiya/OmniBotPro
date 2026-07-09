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

using UnityEngine;
using TMPro;
using System;

namespace Oculus.Interaction
{
    /// <summary>
    /// Style definition for the UI components. These properties are assigned to UI components by QDSUIThemeManager.
    /// </summary>
    public class UITheme : ScriptableObject
    {
        private const int CurrentThemeVersion = 2;
        [SerializeField]
        [HideInInspector]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private int _themeVersion = CurrentThemeVersion;
#pragma warning restore CS0414 // Field is assigned but its value is never used
        public int ThemeVersion { get { return _themeVersion; } }

        public Color backplateColor;
        public Material backplateGradientMaterial;
        public Color buttonPlateColor;
        public Color sectionPlateColor;
        public Color tooltipColor;
        public Color rippleCursorColor = Color.white;

        [Header("Shared")]
        public Color textPrimaryColor;
        public Color textSecondaryColor;
        public Color textPrimaryInvertedColor;
        public Color textSecondaryInvertedColor;

        [Serializable]
        public struct ElementColors
        {
            public Color normal;
            public Color highlighted;
            public Color pressed;
            public Color selected;
            public Color disabled;
        }

        [Header("Per Element Type Color")]
        public ElementColors primaryButton;
        public ElementColors secondaryButton;
        public ElementColors borderlessButton;
        public ElementColors destructiveButton;

        [Header("Fonts")]
        public TMP_FontAsset textFontBold;
        public TMP_FontAsset textFontMedium;
        public TMP_FontAsset textFontRegular;

        [Header("Animators")]
        public RuntimeAnimatorController acPrimaryButton;
        public RuntimeAnimatorController acSecondaryButton;
        public RuntimeAnimatorController acBorderlessButton;
        public RuntimeAnimatorController acDestructiveButton;
        public RuntimeAnimatorController acToggleButton;
        public RuntimeAnimatorController acToggleBorderlessButton;
        public RuntimeAnimatorController acToggleSwitch;
        public RuntimeAnimatorController acToggleCheckboxRadio;
        public RuntimeAnimatorController acToggleThumbnail;
        public RuntimeAnimatorController acTextInputField;

        [Space(10)]
        public string colorPath = "Content/Background";
    }
}
