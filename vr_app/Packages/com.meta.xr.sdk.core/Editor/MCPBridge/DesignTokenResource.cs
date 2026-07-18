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

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Design token resource for MCP clients to access RLDS (Reality Labs Design System) tokens.
    ///
    /// This tool exposes the RLDS design system tokens for programmatic validation,
    /// allowing AI agents to verify that UI code follows the design system without
    /// relying solely on visual comparisons.
    ///
    /// Key Features:
    /// - Extract color tokens from RLDS.Styles.Colors
    /// - Extract typography specs from RLDS.Styles.Typography
    /// - Extract spacing scale from RLDS.Styles.Spacing
    /// - Extract border radius tokens from RLDS.Styles.Radius
    /// - Extract button variant specs from RLDS.Styles.Buttons
    /// - Combined dump of all tokens for comprehensive validation
    ///
    /// Use Cases:
    /// - Check hardcoded hex values match RLDS tokens
    /// - Verify spacing values against the RLDS scale
    /// - Validate button sizing against design specs
    /// - AI can reference token names instead of arbitrary values
    ///
    /// MCP Client Usage Pattern:
    /// 1. GetAllTokens() for comprehensive design system dump
    /// 2. GetColorTokens() to verify color usage
    /// 3. GetSpacingTokens() to verify spacing values
    /// 4. Compare against captured screenshot for visual validation
    /// </summary>
    [Tool(
        "Tools for accessing RLDS (Reality Labs Design System) design tokens.",
        "WHEN TO USE: When validating UI code against the design system.",
        "WORKFLOW: 1) GetAllTokens() for reference 2) Compare code values to tokens.",
        "IMPORTANT: Use with UIVerificationTools to cross-check visual and code-level design compliance."
    )]
    internal class DesignTokenResource : SingletonService<DesignTokenResource>
    {
        private const string RldsStylesTypeName = "Meta.XR.Editor.UserInterface.RLDS.Styles";

        [Tool(Description = "Get all RLDS color tokens with their hex values for both dark and light modes",
            Returns = "JSON object with color token names and hex values")]
        internal object GetColorTokens()
        {
            var colors = new Dictionary<string, object>();

            var stylesType = FindRldsStylesType();
            if (stylesType == null)
            {
                return new { error = "RLDS Styles type not found", hint = "Ensure UOIAssets is in the project" };
            }

            var colorsType = stylesType.GetNestedType("Colors", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (colorsType == null)
            {
                return new { error = "RLDS Colors class not found" };
            }

            var colorFields = colorsType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.FieldType == typeof(Color));

            foreach (var field in colorFields)
            {
                var color = (Color)field.GetValue(null);
                colors[field.Name] = new
                {
                    hex = ColorToHex(color),
                    rgba = new { r = color.r, g = color.g, b = color.b, a = color.a }
                };
            }

            return new
            {
                type = "colors",
                count = colors.Count,
                tokens = colors
            };
        }

        [Tool(Description = "Get all RLDS spacing tokens with their pixel values",
            Returns = "JSON object with spacing token names and values")]
        internal object GetSpacingTokens()
        {
            var spacing = new Dictionary<string, object>();

            var stylesType = FindRldsStylesType();
            if (stylesType == null)
            {
                return new { error = "RLDS Styles type not found" };
            }

            var spacingType = stylesType.GetNestedType("Spacing", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (spacingType != null)
            {
                ExtractNumericFields(spacingType, spacing);
            }
            else
            {
                spacing["Spacing4XS"] = 2;
                spacing["Spacing3XS"] = 4;
                spacing["Spacing2XS"] = 6;
                spacing["SpacingXS"] = 8;
                spacing["SpacingSM"] = 12;
                spacing["SpacingMD"] = 16;
                spacing["SpacingLG"] = 24;
                spacing["SpacingXL"] = 32;
                spacing["Spacing2XL"] = 48;
                spacing["Spacing3XL"] = 64;
                spacing["Spacing4XL"] = 96;
            }

            return new
            {
                type = "spacing",
                count = spacing.Count,
                tokens = spacing,
                scale = "4xs(2) → 3xs(4) → 2xs(6) → xs(8) → sm(12) → md(16) → lg(24) → xl(32) → 2xl(48) → 3xl(64) → 4xl(96)"
            };
        }

        [Tool(Description = "Get all RLDS radius tokens with their pixel values",
            Returns = "JSON object with radius token names and values")]
        internal object GetRadiusTokens()
        {
            var radii = new Dictionary<string, object>();

            var stylesType = FindRldsStylesType();
            if (stylesType == null)
            {
                return new { error = "RLDS Styles type not found" };
            }

            var radiusType = stylesType.GetNestedType("Radius", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (radiusType != null)
            {
                ExtractNumericFields(radiusType, radii);
            }
            else
            {
                radii["RadiusNone"] = 0;
                radii["Radius2XS"] = 2;
                radii["RadiusXS"] = 4;
                radii["RadiusSM"] = 8;
                radii["RadiusMD"] = 12;
                radii["RadiusLG"] = 16;
                radii["RadiusXL"] = 20;
                radii["Radius2XL"] = 24;
                radii["RadiusRound"] = 9999;
            }

            return new
            {
                type = "radius",
                count = radii.Count,
                tokens = radii
            };
        }

        [Tool(Description = "Get all RLDS typography tokens with font sizes, weights, and line heights",
            Returns = "JSON object with typography token names and specs")]
        internal object GetTypographyTokens()
        {
            var typography = new Dictionary<string, object>();

            var stylesType = FindRldsStylesType();
            if (stylesType == null)
            {
                return new { error = "RLDS Styles type not found" };
            }

            var typographyType = stylesType.GetNestedType("Typography", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (typographyType != null)
            {
                var fields = typographyType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    var value = field.GetValue(null);
                    if (value != null)
                    {
                        var valueType = value.GetType();
                        var spec = new Dictionary<string, object>();

                        var fontSizeField = valueType.GetField("FontSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var fontSizeProperty = fontSizeField == null ? valueType.GetProperty("FontSize") : null;
                        var lineHeightField = valueType.GetField("LineHeight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var lineHeightProperty = lineHeightField == null ? valueType.GetProperty("LineHeight") : null;

                        if (fontSizeField != null)
                            spec["fontSize"] = fontSizeField.GetValue(value);
                        else if (fontSizeProperty != null)
                            spec["fontSize"] = fontSizeProperty.GetValue(value);
                        if (lineHeightField != null)
                            spec["lineHeight"] = lineHeightField.GetValue(value);
                        else if (lineHeightProperty != null)
                            spec["lineHeight"] = lineHeightProperty.GetValue(value);

                        typography[field.Name] = spec.Count > 0 ? spec : value.ToString();
                    }
                }
            }
            else
            {
                typography["Heading1"] = new { fontSize = 24, fontWeight = 700 };
                typography["Heading2"] = new { fontSize = 20, fontWeight = 700 };
                typography["Heading3"] = new { fontSize = 16, fontWeight = 600 };
                typography["Body1"] = new { fontSize = 14, fontWeight = 400 };
                typography["Body1Button"] = new { fontSize = 14, fontWeight = 500 };
                typography["Body2"] = new { fontSize = 12, fontWeight = 400 };
                typography["Body2SmallButton"] = new { fontSize = 12, fontWeight = 500 };
                typography["Caption"] = new { fontSize = 10, fontWeight = 400 };
            }

            return new
            {
                type = "typography",
                count = typography.Count,
                tokens = typography
            };
        }

        [Tool(Description = "Get all RLDS button variant specs with sizing and color information",
            Returns = "JSON object with button variant names and specs")]
        internal object GetButtonTokens()
        {
            var buttons = new Dictionary<string, object>();

            var stylesType = FindRldsStylesType();
            if (stylesType == null)
            {
                return new { error = "RLDS Styles type not found" };
            }

            var buttonsType = stylesType.GetNestedType("Buttons", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (buttonsType != null)
            {
                var fields = buttonsType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    var value = field.GetValue(null);
                    if (value != null)
                    {
                        var spec = ExtractButtonStyle(value);
                        if (spec != null)
                        {
                            buttons[field.Name] = spec;
                        }
                    }
                }
            }
            else
            {
                buttons["Primary"] = new { height = 40, minWidth = 80, cornerRadius = 4, variant = "primary" };
                buttons["Secondary"] = new { height = 40, minWidth = 80, cornerRadius = 4, variant = "secondary" };
                buttons["Tertiary"] = new { height = 40, minWidth = 80, cornerRadius = 4, variant = "tertiary" };
                buttons["PrimarySmall"] = new { height = 32, minWidth = 64, cornerRadius = 4, variant = "primary" };
                buttons["SecondarySmall"] = new { height = 32, minWidth = 64, cornerRadius = 4, variant = "secondary" };
            }

            return new
            {
                type = "buttons",
                count = buttons.Count,
                tokens = buttons
            };
        }

        [Tool(Description = "Get all RLDS icon size tokens",
            Returns = "JSON object with icon size token names and pixel values")]
        internal object GetIconSizeTokens()
        {
            var iconSizes = new Dictionary<string, object>();

            var stylesType = FindRldsStylesType();
            if (stylesType == null)
            {
                return new { error = "RLDS Styles type not found" };
            }

            var iconSizeType = stylesType.GetNestedType("IconSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (iconSizeType != null)
            {
                ExtractNumericFields(iconSizeType, iconSizes);
            }
            else
            {
                iconSizes["IconXS"] = 12;
                iconSizes["IconSM"] = 16;
                iconSizes["IconMD"] = 20;
                iconSizes["IconLG"] = 24;
                iconSizes["IconXL"] = 32;
                iconSizes["Icon2XL"] = 48;
            }

            return new
            {
                type = "iconSize",
                count = iconSizes.Count,
                tokens = iconSizes
            };
        }

        [Tool(Description = "Get all RLDS design tokens in a single combined dump",
            Returns = "JSON object with all token categories: colors, spacing, radius, typography, buttons, iconSize")]
        internal object GetAllTokens()
        {
            var colorResult = GetColorTokens();
            var spacingResult = GetSpacingTokens();
            var radiusResult = GetRadiusTokens();
            var typographyResult = GetTypographyTokens();
            var buttonResult = GetButtonTokens();
            var iconSizeResult = GetIconSizeTokens();

            return new
            {
                designSystem = "RLDS (Reality Labs Design System)",
                version = "Desktop",
                colors = colorResult,
                spacing = spacingResult,
                radius = radiusResult,
                typography = typographyResult,
                buttons = buttonResult,
                iconSize = iconSizeResult,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        private static Type FindRldsStylesType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(RldsStylesTypeName);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }
            return null;
        }

        private static void ExtractNumericFields(Type type, Dictionary<string, object> dict)
        {
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var value = field.GetValue(null);
                if (value is int || value is float || value is double)
                {
                    dict[field.Name] = value;
                }
            }
        }

        private static object ExtractButtonStyle(object buttonStyle)
        {
            var type = buttonStyle.GetType();
            var result = new Dictionary<string, object>();

            var heightProp = type.GetProperty("Height") ?? type.GetField("Height", BindingFlags.Public | BindingFlags.Instance) as MemberInfo;
            var minWidthProp = type.GetProperty("MinWidth") ?? type.GetField("MinWidth", BindingFlags.Public | BindingFlags.Instance) as MemberInfo;
            var cornerRadiusProp = type.GetProperty("CornerRadius") ?? type.GetField("CornerRadius", BindingFlags.Public | BindingFlags.Instance) as MemberInfo;
            var bgColorNormalProp = type.GetProperty("BackgroundColorNormal") ?? type.GetField("BackgroundColorNormal", BindingFlags.Public | BindingFlags.Instance) as MemberInfo;

            if (heightProp is PropertyInfo hpi)
                result["height"] = hpi.GetValue(buttonStyle);
            else if (heightProp is FieldInfo hfi)
                result["height"] = hfi.GetValue(buttonStyle);

            if (minWidthProp is PropertyInfo mwpi)
                result["minWidth"] = mwpi.GetValue(buttonStyle);
            else if (minWidthProp is FieldInfo mwfi)
                result["minWidth"] = mwfi.GetValue(buttonStyle);

            if (cornerRadiusProp is PropertyInfo crpi)
                result["cornerRadius"] = crpi.GetValue(buttonStyle);
            else if (cornerRadiusProp is FieldInfo crfi)
                result["cornerRadius"] = crfi.GetValue(buttonStyle);

            if (bgColorNormalProp is PropertyInfo bcpi)
            {
                var color = (Color)bcpi.GetValue(buttonStyle);
                result["backgroundColorNormal"] = ColorToHex(color);
            }
            else if (bgColorNormalProp is FieldInfo bcfi)
            {
                var color = (Color)bcfi.GetValue(buttonStyle);
                result["backgroundColorNormal"] = ColorToHex(color);
            }

            return result.Count > 0 ? result : null;
        }

        private static string ColorToHex(Color color)
        {
            var r = (int)(color.r * 255);
            var g = (int)(color.g * 255);
            var b = (int)(color.b * 255);

            if (Math.Abs(color.a - 1f) < 0.001f)
            {
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            else
            {
                var a = (int)(color.a * 255);
                return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
            }
        }
    }
}
