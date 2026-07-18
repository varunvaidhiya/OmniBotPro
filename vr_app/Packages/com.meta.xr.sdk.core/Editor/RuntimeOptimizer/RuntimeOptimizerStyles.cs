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

#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.RuntimeOptimizer.Editor
{
    public partial class RuntimeOptimizerWindow
    {
        internal static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte r = (byte)(Convert.ToInt32(hex.Substring(0, 2), 16));
            byte g = (byte)(Convert.ToInt32(hex.Substring(2, 2), 16));
            byte b = (byte)(Convert.ToInt32(hex.Substring(4, 2), 16));
            byte a = 255;

            if (hex.Length == 8)
            {
                a = (byte)(Convert.ToInt32(hex.Substring(6, 2), 16));
            }

            return new Color32(r, g, b, a);
        }

        internal static Texture2D CreateTextureFromColor(Color col)
        {
            Color[] pixels = new Color[1 * 1];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = col;
            }

            Texture2D result = new Texture2D(1, 1);
            result.hideFlags = HideFlags.DontSave;
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        internal static class Constants
        {
            public const float ItemHeight = 48.0f;
            public const float MiniIconHeight = 18.0f;
            public const float SmallIconSize = 16.0f;
            public const float LabelWidth = 192.0f;

            public const int Border = 1;
            public const int DoublePadding = 8;
            public const int LargePadding = 6;
            public const int Padding = 4;
            public const int MiniPadding = 2;
            public const int Margin = 8;
            public const int DoubleMargin = 16;
            public const int MiniMargin = 4;
            public const int LargeMargin = 32;
            public const int SpaceForIconMargin = LargeMargin + Margin + MiniPadding;
            public const int TextWidthOffset = 2;
            public const int FoldoutMargin = 14;
        }

        public static class Colors
        {
            public static readonly Color ExperimentalColor = HexToColor("#eba333");
            public static readonly Color NewColor = HexToColor("#ffc75d");
            public static readonly Color DarkBlue = HexToColor("#48484d");
            public static readonly Color BrightGray = HexToColor("#c4c4c4");
            public static readonly Color LightGray = HexToColor("#aaaaaa");
            public static readonly Color DarkerGray = HexToColor("#242424");
            public static readonly Color DarkGray = HexToColor("#3e3e3e");
            public static readonly Color DarkGraySemiTransparent = HexToColor("#3e3e3eaa");
            public static readonly Color DarkGrayHover = HexToColor("#4e4e4e");
            public static readonly Color DarkGrayActive = HexToColor("#5d5d5d");
            public static readonly Color CharcoalGray = HexToColor("#1d1d1d");
            public static readonly Color CharcoalGraySemiTransparent = HexToColor("#1d1d1d80");
            public static readonly Color OffWhite = HexToColor("#dddddd");
            public static readonly Color ErrorColor = HexToColor("ed5757");
            public static readonly Color ErrorColorSemiTransparent = HexToColor("ed575780");
            public static readonly Color WarningColor = HexToColor("e9974e");
            public static readonly Color InfoColor = HexToColor("c4c4c4");
            public static readonly Color SuccessColor = HexToColor("3BC24C");
            public static readonly Color LightMeta = HexToColor("#99c2ff");
            public static readonly Color Meta = HexToColor("#1977f3");
            public static readonly Color Yellow = HexToColor("#ffd74e");
            public static readonly Color SelectedWhite = HexToColor("#f0f0f0");
            public static readonly Color UnselectedWhite = HexToColor("#c4c4c4");
            public static readonly Color LinkColor = HexToColor("#81b3ff");
            public static readonly Color CollectionTagsColor = HexToColor("#eeeeee");
            public static readonly Color InstallationStepPanelBackground = HexToColor("#333333");
            public static readonly Color InstallationStepBackground = HexToColor("#474747");
            public static readonly Color PanelBackground = HexToColor("#383838");
            public static readonly Color DebugColor = HexToColor("#4ed998");
            public static readonly Color UtilityColor = HexToColor("#4ed998");
            public static readonly Color DisabledColor = HexToColor("#808080");
            public static readonly Color InsightToolNote = HexToColor("#7D67AD");
            public static readonly Color GSDToolNote = HexToColor("#4C546F");

        }

        internal static class Styles
        {
            public static readonly GUIContent Description =
            new GUIContent("Run a Bottleneck or What if? Analysis" +
                           "\nto find out how to speed up" +
                           $"\nthe performance of your game." +
                           $"\n\n<b>Tip</b>: Place your headset on a flat surface " +
                           "\nwhen running What If? analysis.");

            public static readonly GUIContent Steps =
            new GUIContent("1. Connect your headset via USB cable" +
                            "\n2. Toggle 'Quest Runtime Optimizer Enabled' to On" +
                            "\n3. Build the APK in Development mode " +
                            "\n4. Add the APK's executable path and click on 'Launch'" +
                            "\n5. Run What if? Analysis () or Bottleneck Analysis");

            public static readonly GUIContent optionsButtonContent = EditorGUIUtility.TrIconContent("_Menu", "Additional Options");
            public static readonly GUIContent helpButtonContent = EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)");
            public static readonly GUIContent preferencesButtonContent = EditorGUIUtility.TrTextContent("Preferences", "Open User Preferences for the Profiler");

            public static readonly GUIContent accessibilityModeLabel = EditorGUIUtility.TrTextContent("Color Blind Mode", "Switch the color scheme to color blind safe colors");
            public static readonly GUIContent showStatsLabelsOnCurrentFrameLabel = EditorGUIUtility.TrTextContent("Show Stats for 'current frame'", "Show stats labels when the 'current frame' toggle is on.");

            public static readonly GUIStyle DocumentationLabelStyle = new()
            {
                normal =
                {
                    textColor = Colors.OffWhite
                },
                padding = new RectOffset(0, 0, 0, 0),
                fontSize = 14
            };

            public static readonly GUIStyle OverviewBox = new GUIStyle()
            {
                padding = new RectOffset(0, Constants.DoubleMargin, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                stretchHeight = false,
            };

            public static readonly GUIStyle DialogTextStyle = new GUIStyle()
            {
                padding = new RectOffset(Constants.MiniPadding, Constants.MiniPadding, Constants.MiniPadding, Constants.MiniPadding),
                stretchWidth = true,
                richText = true,
                fontSize = 11,
                wordWrap = true,
                normal =
                {
                    textColor = Colors.OffWhite
                }
            };

            public static readonly GUIStyle OverviewNoticeBox = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.SpaceForIconMargin, Constants.Margin, Constants.Margin, Constants.Margin),
                stretchWidth = true,
                stretchHeight = false,
                normal =
                {
                    background = CreateTextureFromColor(Colors.CharcoalGraySemiTransparent)
                }
            };

            public static readonly GUIStyle ToolBox = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Margin, Constants.DoubleMargin, Constants.DoubleMargin, Constants.Margin),
                stretchWidth = true,
                stretchHeight = false,
                normal =
                {
                    background = CreateTextureFromColor(Colors.CharcoalGraySemiTransparent)
                }
            };

            public static readonly GUIStyle CaptureBox = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Margin, Constants.Margin, Constants.Margin, Constants.Margin),
                fixedWidth = kSnapshotWidth + Constants.Margin * 2,
                stretchHeight = false,
                normal =
                {
                    background = CreateTextureFromColor(Colors.DarkGraySemiTransparent)
                }
            };

            public static readonly GUIStyle InsightToolNote = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Margin, Constants.DoubleMargin, 2, 0),
                stretchWidth = true,
                stretchHeight = false,
                normal =
                {
                    background = CreateTextureFromColor(Colors.InsightToolNote),
                    textColor = Color.white
                }
            };

            public static readonly GUIStyle GSDToolNote = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Margin, Constants.DoubleMargin, 2, 0),
                stretchWidth = true,
                stretchHeight = false,
                normal =
                {
                    background = CreateTextureFromColor(Colors.GSDToolNote),
                    textColor = Color.white
                }
            };

            public static readonly GUIStyle IconStyle = new(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = Constants.SmallIconSize,
                stretchWidth = false
            };

            public static readonly GUIStyle IconLargeStyle = new(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = Constants.SmallIconSize * 2,
                stretchWidth = false
            };

            public static readonly GUIStyle ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
            };

            public static readonly GUIStyle ButtonMiddleStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            public static readonly GUIStyle UnitText = new GUIStyle()
            {
                fontSize = 10,
                normal =
                {
                    textColor = Colors.InfoColor
                }
            };

            public static readonly GUIStyle CompactMeshText = new GUIStyle()
            {
                fontSize = 8,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = Colors.OffWhite
                }
            };

            public static readonly GUIStyle RightAlignedText = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal =
                {
                    textColor = Colors.OffWhite
                }
            };

            public static readonly GUIStyle QuickPerfBox = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Margin, Constants.Margin, Constants.DoubleMargin, Constants.DoubleMargin),
                stretchWidth = true,
                stretchHeight = false,
                normal = { background = CreateTextureFromColor(Colors.DarkGrayHover) }
            };

            public static readonly GUIStyle QuickPerfSuggestionBox = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Margin, Constants.Margin, Constants.MiniPadding, Constants.MiniPadding),
                stretchWidth = true,
                stretchHeight = false,
                normal = { background = CreateTextureFromColor(Colors.CharcoalGraySemiTransparent) }
            };
        }
    }
}
