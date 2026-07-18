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

using Meta.XR.Editor.ToolingSupport;
using UnityEditor;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Utility class for MCPBridge with ToolDescriptor integration.
    /// Enables settings to appear in Unity's Edit > Preferences > Meta XR / MCP Bridge menu.
    /// </summary>
    [InitializeOnLoad]
    internal static class Utils
    {
        internal const string PublicName = "AI Tools Bridge";

        public static readonly string Description =
            "Exposes Unity tools, resources, and prompts to AI agents via the Model Context Protocol (MCP). " +
            "Runs a lightweight HTTP server that AI agents (Claude Code, DevMate) connect to for tool discovery and execution." +
            "\n\nConfigure the HTTP server port and MCP registration below.";

        /// <summary>
        /// Tool descriptor for Meta XR Editor tooling support integration.
        /// Enables MCPBridge settings in Edit > Preferences > Meta XR / MCP Bridge.
        /// </summary>
        internal static readonly ToolDescriptor ToolDescriptor = new()
        {
            Name = PublicName,
            Description = Description,
            Color = Meta.XR.Editor.UserInterface.Styles.Colors.AI,
            Icon = Meta.XR.Editor.UserInterface.Styles.Contents.ConfigIcon,
            Experimental = true,
            AddToStatusMenu = false,
            AddToMenu = false,
            OnClickDelegate = origin => ToolDescriptor?.OpenUserSettings(origin),
            OnUserSettingsGUI = McpBridgeSettings.OnGUI,
            Order = 101,
        };
    }
}
