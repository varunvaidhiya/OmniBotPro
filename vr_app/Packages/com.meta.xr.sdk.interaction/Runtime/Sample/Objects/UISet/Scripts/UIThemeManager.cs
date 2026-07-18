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

using Oculus.Interaction.Samples;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Interaction
{
    /// <summary>
    /// Find and assign style properties defined by UITheme using tags.
    /// </summary>
    public class UIThemeManager : MonoBehaviour
    {
        [SerializeField]
        private UITheme[] _themes;
        public UITheme[] Themes => _themes;

        [SerializeField]
        private int _currentThemeIndex = -1;
        public int CurrentThemeIndex => _currentThemeIndex;


        void Awake()
        {
        }

        protected virtual void Start()
        {
            ApplyTheme(_currentThemeIndex);
        }


        public void ApplyCurrentTheme()
        {
            ApplyTheme(_currentThemeIndex);
        }

        /// <summary>
        /// Re-applies animator controllers to all buttons under this manager.
        /// Call this after dynamically creating or destroying UI elements at runtime.
        /// </summary>
        public void RefreshAnimators()
        {
            if (_themes == null || _currentThemeIndex < 0 || _currentThemeIndex >= _themes.Length)
            {
                return;
            }

            ApplyAnimators(_themes[_currentThemeIndex]);
        }


        public void ResetIndex()
        {
            _currentThemeIndex = -1;
        }

        public void ApplyTheme(int index)
        {
            if (_themes == null || _themes.Length == 0)
            {
                return;
            }

            if (index < 0 || index >= _themes.Length)
            {
                Debug.LogError("Theme index out of range.");
                return;
            }

            if (_themes[index] == null)
            {
                return;
            }

            _currentThemeIndex = index;
            UITheme selectedTheme = _themes[index];


            ApplyAnimators(selectedTheme);
            ApplyCursor(selectedTheme);

            // Apply color to all image objects under the canvas. For interactable elements, updates default normal state for editor view.
            Image[] images = GetComponentsInChildren<Image>();

            foreach (var image in images)
            {
                if (image.CompareTag("QDSUIIcon"))
                {
                    // Apply the text color to the icons as well
                    image.color = selectedTheme.textPrimaryColor;
                }
                else if (image.CompareTag("QDSUIAccentColor"))
                {
                    // Apply the accent color
                    image.color = selectedTheme.primaryButton.normal;
                }
                else if (image.CompareTag("QDSUISharedThemeColor") || image.CompareTag("QDSUIDestructiveButton") || image.CompareTag("QDSUIBorderlessButton") || image.CompareTag("QDSUIToggleBorderlessButton"))
                {
                    // Same color scheme for both themes, No changes. Or apply any future theme adjustments here.
                }
                else if (image.CompareTag("QDSUISecondaryButton") || image.CompareTag("QDSUIToggleButton"))
                {
                    // Colors are applied though animation clips.
                    // Apply the backplate color of the button for plated button.
                    // image.color = selectedTheme.buttonPlateColor;
                }
                else if (image.CompareTag("QDSUISection"))
                {
                    // Apply the section plate color.
                    image.color = selectedTheme.sectionPlateColor;
                }
                else if (image.CompareTag("QDSUITooltip"))
                {
                    // Apply the backplate color of the button for plated button.
                    image.color = selectedTheme.tooltipColor;
                }
                else if (image.CompareTag("QDSUITextInputField"))
                {
                    // Colors are applied though animation clips.
                }
                else if (image.CompareTag("QDSUILineDrawing"))
                {
                    image.color = selectedTheme.textPrimaryColor;
                }
                else if (selectedTheme.ThemeVersion < 2)
                {
                    image.color = selectedTheme.backplateColor;
                }
                else if (selectedTheme.ThemeVersion == 2)
                {
                    if (image.CompareTag("QDSUIBackplate"))
                    {
                        image.color = selectedTheme.backplateColor;
                    }
                    // v2 visual design, optional: Apply the gradient material to the backplate
                    else if (image.CompareTag("QDSUIBackplateGradient"))
                    {
                        image.color = selectedTheme.backplateColor;
                        image.material = selectedTheme.backplateGradientMaterial;
                    }
                    // v2 visual design, handle inverted colors on images
                    else if (image.CompareTag("QDSUITextInvertedColor"))
                    {
                        image.color = selectedTheme.textPrimaryInvertedColor;
                    }
                }
            }

            SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

            foreach (var spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = selectedTheme.tooltipColor;
            }

            // Apply color and font to all TextMeshProUGUI components under the canvas
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                text.font = selectedTheme.textFontMedium; // Set the font

                if (text.CompareTag("QDSUISharedThemeColor") || text.CompareTag("QDSUIDestructiveButton"))
                {
                    // Same color scheme for both themes, No changes. Or apply any future theme adjustments here.
                }
                else if (text.CompareTag("QDSUITextSecondaryColor"))
                {
                    text.color = selectedTheme.textSecondaryColor;
                }
                else
                {
                    // Apply the primary text color as default
                    text.color = selectedTheme.textPrimaryColor;
                }

                // v2 visual design, handle inverted text colors on buttons
                if (selectedTheme.ThemeVersion == 2)
                {
                    if (text.CompareTag("QDSUITextInvertedColor"))
                    {
                        text.color = selectedTheme.textPrimaryInvertedColor;
                    }

                    if (text.CompareTag("QDSUITextSecondaryInvertedColor"))
                    {
                        text.color = selectedTheme.textSecondaryInvertedColor;
                    }
                }
            }
        }

        private void ApplyAnimators(UITheme theme)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (var animator in animators)
            {
                RuntimeAnimatorController controller = GetControllerForTag(animator.tag, theme);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    if (animator.gameObject.activeInHierarchy)
                    {
                        animator.Update(0);
                    }
                }
            }
        }

        private void ApplyCursor(UITheme theme)
        {
            RippleCursorEffectManager[] rippleCursorEffectManagers = FindObjectsByType<RippleCursorEffectManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var rippleCursorEffectManager in rippleCursorEffectManagers)
            {
                rippleCursorEffectManager.SetThemeRippleColor(theme.rippleCursorColor);
            }
        }

        private RuntimeAnimatorController GetControllerForTag(string tag, UITheme theme)
        {

            return tag switch
            {
                "QDSUIPrimaryButton" => theme.acPrimaryButton,
                "QDSUISecondaryButton" => theme.acSecondaryButton,
                "QDSUIBorderlessButton" => theme.acBorderlessButton,
                "QDSUIDestructiveButton" => theme.acDestructiveButton,
                "QDSUIToggleButton" => theme.acToggleButton,
                "QDSUIToggleBorderlessButton" => theme.acToggleBorderlessButton,
                "QDSUIToggleSwitch" => theme.acToggleSwitch,
                "QDSUIToggleCheckboxRadio" => theme.acToggleCheckboxRadio,
                "QDSUIToggleThumbnail" => theme.acToggleThumbnail,
                "QDSUITextInputField" => theme.acTextInputField,
                _ => null
            };
        }
    }
}
