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

using Meta.XR.AI.AgentBridge;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// EditorPrefs-backed settings for the MCPBridge HTTP server.
    /// Follows the same pattern as AgentBridge's RemoteAgentSettings.
    /// Settings appear in Edit > Preferences > Meta XR / AI Tools Bridge.
    /// </summary>
    internal static class McpBridgeSettings
    {
        private static readonly IIdentified Owner = Utils.ToolDescriptor;

        /// <summary>
        /// The port number for the MCP HTTP server.
        /// </summary>
        public static readonly UserInt Port = new UserInt
        {
            Uid = "McpBridge_Port",
            Owner = Owner,
            Default = 48736,
            Label = "Port",
            Tooltip = "The port number for the MCP HTTP server. " +
                      "AI agents (Claude Code, DevMate) connect to this port.",
            SendTelemetry = false
        };

        /// <summary>
        /// Whether to automatically start the HTTP server when Unity Editor loads.
        /// </summary>
        public static readonly UserBool AutoStart = new UserBool
        {
            Uid = "McpBridge_AutoStart",
            Owner = Owner,
            Default = true,
            Label = "Auto-Start",
            Tooltip = "Automatically start the MCP HTTP server when Unity Editor loads. " +
                      "When disabled, the server must be started manually from settings.",
            SendTelemetry = false
        };

        /// <summary>
        /// Ensures an access token exists. Delegates to RemoteAgentSettings to share
        /// the same token between AgentBridge and MCPBridge.
        /// </summary>
        public static string EnsureToken()
        {
            return RemoteAgentSettings.EnsureToken();
        }

        /// <summary>
        /// Gets the current access token value. Shared with AgentBridge.
        /// </summary>
        public static string AccessTokenValue => RemoteAgentSettings.AccessToken.Value;

        /// <summary>
        /// Draw the MCP Bridge settings UI in the preferences panel.
        /// </summary>
        public static void OnGUI(Origins origin, string searchContext)
        {
            // HTTP Server section
            EditorGUILayout.LabelField("HTTP Server", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Runs a lightweight HTTP server implementing the MCP JSON-RPC 2.0 protocol. " +
                "AI agents connect here to discover and call Meta XR tools.",
                EditorStyles.wordWrappedMiniLabel);

            // Server status and Start/Stop button on the same line
            var isRunning = HttpMcpServer.Instance.IsRunning;

            EditorGUILayout.BeginHorizontal();
            var statusLabel = isRunning
                ? $"● Running (port {Port.Value})"
                : "○ Stopped";
            var statusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = isRunning ? Colors.SuccessColor : Colors.DisabledColor }
            };
            EditorGUILayout.LabelField("Status", statusLabel, statusStyle);
            if (isRunning)
            {
                if (GUILayout.Button("Stop Server", GUILayout.Width(120)))
                {
                    HttpMcpServer.Instance.Stop();
                }
            }
            else
            {
                if (GUILayout.Button("Start Server", GUILayout.Width(120)))
                {
                    HttpMcpServer.Instance.Start(Port.Value);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Port setting
            EditorGUI.BeginChangeCheck();
            Port.DrawForGUI(origin, Owner);
            if (EditorGUI.EndChangeCheck() && isRunning)
            {
                if (EditorUtility.DisplayDialog(
                    "Restart MCP Server?",
                    "The port has been changed. The MCP server must be restarted for the change to take effect.",
                    "Restart", "Later"))
                {
                    HttpMcpServer.Instance.Stop();
                    HttpMcpServer.Instance.Start(Port.Value);
                }
            }

            // Auto-start setting
            AutoStart.DrawForGUI(origin, Owner);

            EditorGUILayout.Space();

            // Authentication section (shared with AgentBridge)
            RemoteAgentSettings.DrawAuthenticationUI();

            EditorGUILayout.Space();

            // MCP Registration section
            EditorGUILayout.LabelField("MCP Registration", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Register the 'meta-xr-unity-runtime' MCP server with AI agent CLIs " +
                "so they can discover and call Meta XR tools.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(
                $"Important: Registration uses the current port ({Port.Value}). " +
                "If you change the port, you must re-register.",
                EditorStyles.wordWrappedMiniLabel);

            // Claude Code status and registration
            var claudeStatus = McpRegistration.GetClaudeCodeStatus(Port.Value);
            EditorGUILayout.BeginHorizontal();
            var claudeStatusLabel = claudeStatus switch
            {
                McpRegistrationStatus.Checking => "○ Checking...",
                McpRegistrationStatus.Registered => "● Registered",
                McpRegistrationStatus.NotRegistered => "○ Not Registered",
                McpRegistrationStatus.NotInstalled => "✕ Not Installed",
                _ => "? Unknown"
            };
            var claudeStatusStyle = new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = claudeStatus switch
                    {
                        McpRegistrationStatus.Checking => Colors.DisabledColor,
                        McpRegistrationStatus.Registered => Colors.SuccessColor,
                        McpRegistrationStatus.NotRegistered => Colors.DisabledColor,
                        McpRegistrationStatus.NotInstalled => Colors.ErrorColor,
                        _ => Colors.DisabledColor
                    }
                }
            };
            EditorGUILayout.LabelField("Claude Code", claudeStatusLabel, claudeStatusStyle);
            GUI.enabled = claudeStatus != McpRegistrationStatus.Checking;
            if (GUILayout.Button("↻", GUILayout.Width(24)))
            {
                McpRegistration.RefreshClaudeCodeStatus(Port.Value);
            }
            GUI.enabled = claudeStatus != McpRegistrationStatus.NotInstalled &&
                          claudeStatus != McpRegistrationStatus.Checking;
            if (GUILayout.Button("Register", GUILayout.Width(80)))
            {
                McpRegistration.RegisterWithClaudeCodeAsync(Port.Value);
            }
            if (GUILayout.Button("Unregister", GUILayout.Width(80)))
            {
                McpRegistration.UnregisterFromClaudeCodeAsync(Port.Value);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Reset button (matching AgentBridge layout)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Reset all settings to their defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            Port.Reset();
            AutoStart.Reset();
        }
    }
}
