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

using Meta.XR.Editor.Id;
using Meta.XR.Editor.PlayCompanion;
using Meta.XR.Editor.UserInterface;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Unity Editor toolbar integration for AgentBridge. Provides a status button with a
    /// colored indicator reflecting composite service/remote status, and opens settings on click.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    internal static class ToolbarItem
    {
        /// <summary>Play Companion toolbar item integration.</summary>
        internal static readonly Item PlayCompanionItem = new()
        {
            Order = 100,
            Name = Utils.PublicName,
            Tooltip = "AI Agent Bridge\nClick to open settings",
            Icon = Styles.Contents.MainIcon,
            Color = Meta.XR.Editor.UserInterface.Styles.Colors.AI,
            Show = true,
            IsButton = true,
            OnSelect = () => Utils.ToolDescriptor.OpenUserSettings(Origins.Toolbar),
            TintColor = GetStatusColor
        };

        private static bool _registered;

        static ToolbarItem()
        {
            // Only register the toolbar item if the master toggle is enabled.
            if (Settings.Enabled.Value)
            {
                Register();
            }
        }

        private static void Register()
        {
            if (_registered) return;
            _registered = true;
            Manager.RegisterItem(PlayCompanionItem);
        }

        /// <summary>
        /// Ensure the toolbar item is registered. Called when the user enables AI packages.
        /// </summary>
        internal static void EnsureRegistered()
        {
            Register();
        }

        /// <summary>
        /// Returns a color reflecting composite status from AI service and remote server state.
        /// Green: service valid, Red: service invalid/error, Gray: unknown or no service.
        /// </summary>
        private static Color GetStatusColor()
        {
            var service = AgentBridgeManager.GetCurrentService();

            if (service is IServiceValidation validation)
            {
                var result = validation.CurrentValidationResult;
                return result.Status switch
                {
                    ValidationStatus.Valid => Meta.XR.Editor.UserInterface.Styles.Colors.SuccessColor,
                    ValidationStatus.Invalid => Meta.XR.Editor.UserInterface.Styles.Colors.ErrorColor,
                    ValidationStatus.Error => Meta.XR.Editor.UserInterface.Styles.Colors.ErrorColor,
                    _ => Meta.XR.Editor.UserInterface.Styles.Colors.DisabledColor
                };
            }

            if (service != null)
            {
                return Meta.XR.Editor.UserInterface.Styles.Colors.SuccessColor;
            }

            return Meta.XR.Editor.UserInterface.Styles.Colors.DisabledColor;
        }
    }
}
