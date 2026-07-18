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

using Meta.XR.Editor.ToolingSupport;
using UnityEditor;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Utility class for Agent Bridge with ToolDescriptor integration.
    /// Enables settings to appear in Unity's Edit > Preferences > Meta XR / Agent Bridge menu.
    /// </summary>
    [InitializeOnLoad]
    internal static class Utils
    {
        internal const string PublicName = "AI Agent Bridge";

        public static readonly string Description =
            "AI-powered agent integration for Meta XR SDK tools and SDKs. " +
            "Enables communication with AI assistants for AI inference, " +
            "debugging assistance, and automation within our workflows." +
            "\n\nSelect your preferred AI service and configure service-specific settings below.";

        /// <summary>
        /// Tool descriptor for Meta XR Editor tooling support integration.
        /// Enables Agent Bridge settings in Edit > Preferences > Meta XR / Agent Bridge.
        /// </summary>
        internal static readonly ToolDescriptor ToolDescriptor = new()
        {
            Name = PublicName,
            Description = Description,
            Color = Meta.XR.Editor.UserInterface.Styles.Colors.AI,
            Icon = Styles.Contents.MainIcon,
            Experimental = true,
            AddToStatusMenu = false,
            AddToMenu = false,
            OnClickDelegate = origin => ToolDescriptor?.OpenUserSettings(origin),
            OnUserSettingsGUI = Settings.OnGUI,
            Order = 100,
        };
    }
}
