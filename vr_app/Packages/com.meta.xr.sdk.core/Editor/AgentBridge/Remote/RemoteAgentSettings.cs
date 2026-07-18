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
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Settings for the Remote Agent Server.
    /// Controls the HTTP server that enables remote AI inference from devices (e.g., Quest headsets).
    /// Settings are stored in EditorPrefs (user-specific, not version controlled).
    /// </summary>
    internal static class RemoteAgentSettings
    {
        private static readonly IIdentified Owner = Utils.ToolDescriptor;

        /// <summary>
        /// The port number for the Remote Agent HTTP server.
        /// </summary>
        public static readonly UserInt Port = new UserInt
        {
            Uid = "RemoteServer_Port",
            Owner = Owner,
            Default = 48735,
            Label = "Port",
            Tooltip = "The port number for the Remote Agent HTTP server. " +
                      "Remote clients (e.g., Quest headsets) will connect to this port.",
            SendTelemetry = false
        };

        /// <summary>
        /// Whether to automatically start the Remote Agent server when Unity Editor loads.
        /// </summary>
        public static readonly UserBool AutoStart = new UserBool
        {
            Uid = "RemoteServer_AutoStart",
            Owner = Owner,
            Default = false,
            Label = "Auto-Start",
            Tooltip = "Automatically start the Remote Agent server when Unity Editor loads. " +
                      "When disabled, the server must be started manually from settings.",
            SendTelemetry = false
        };

        /// <summary>
        /// The access token for authenticating remote clients.
        /// Generated automatically on first access. Stored in EditorPrefs (user-specific).
        /// </summary>
        public static readonly UserString AccessToken = new UserString
        {
            Uid = "RemoteServer_AccessToken",
            Owner = Owner,
            Default = "",
            Label = "Access Token",
            Tooltip = "Unique token for this Editor instance. Automatically injected into builds at build time. " +
                      "Only builds from this project can connect to this server.",
            SendTelemetry = false
        };

        /// <summary>
        /// Ensures an access token exists. Generates a new GUID-based token if empty.
        /// </summary>
        /// <returns>The current access token.</returns>
        public static string EnsureToken()
        {
            if (string.IsNullOrEmpty(AccessToken.Value))
            {
                var newToken = Guid.NewGuid().ToString("N"); // 32-char hex, no dashes
                AccessToken.SetValue(newToken, Origins.Self, Owner);
                Log.Info("Generated new access token for Remote Agent Server");
            }
            return AccessToken.Value;
        }

        /// <summary>
        /// Regenerates the access token. This invalidates all existing builds.
        /// </summary>
        /// <returns>The newly generated token.</returns>
        public static string RegenerateToken()
        {
            var newToken = Guid.NewGuid().ToString("N");
            AccessToken.SetValue(newToken, Origins.Self, Owner);
            Log.Info("Regenerated access token for Remote Agent Server");
            return newToken;
        }

        /// <summary>
        /// Draw the Remote Server settings section in the preferences panel.
        /// </summary>
        public static void DrawSettingsUI(Origins origin, IIdentified owner)
        {
            EditorGUILayout.LabelField("Remote Server", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Allows devices on the local network (e.g., Quest headsets) to send AI inference requests to this Unity Editor instance.",
                EditorStyles.wordWrappedMiniLabel);

            // Server status and Start/Stop button on the same line
            var isRunning = RemoteAgentServer.IsRunning;
            var clientCount = RemoteAgentServer.ConnectedClientCount;

            EditorGUILayout.BeginHorizontal();
            var localIp = NetworkUtilities.GetLocalNetworkAddress();
            var statusLabel = isRunning
                ? $"● Running on {localIp}:{Port.Value} ({clientCount} client{(clientCount != 1 ? "s" : "")})"
                : "○ Stopped";
            var statusStyle = new UnityEngine.GUIStyle(EditorStyles.label)
            {
                normal = { textColor = isRunning ? Colors.SuccessColor : Colors.DisabledColor }
            };
            EditorGUILayout.LabelField("Status", statusLabel, statusStyle);
            if (isRunning)
            {
                if (UnityEngine.GUILayout.Button("Stop Server", UnityEngine.GUILayout.Width(120)))
                {
                    RemoteAgentServer.Stop();
                }
            }
            else
            {
                if (UnityEngine.GUILayout.Button("Start Server", UnityEngine.GUILayout.Width(120)))
                {
                    RemoteAgentServer.Start();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Port setting
            EditorGUI.BeginChangeCheck();
            Port.DrawForGUI(origin, owner);
            if (EditorGUI.EndChangeCheck() && isRunning)
            {
                // Port changed while server is running — prompt restart
                if (EditorUtility.DisplayDialog(
                    "Restart Remote Server?",
                    "The port has been changed. The Remote Agent server must be restarted for the change to take effect.",
                    "Restart", "Later"))
                {
                    RemoteAgentServer.Restart();
                }
            }

            // Auto-start setting
            AutoStart.DrawForGUI(origin, owner);

            // Access Token section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Authentication prevents unauthorized devices on the same network from sending AI requests to this server. " +
                "A unique token is generated here and automatically injected into builds at build time, ensuring only apps built from this project can connect.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            // Ensure token exists
            var token = EnsureToken();

            // Display token in a disabled text field (greyed out)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Access Token");
            GUI.enabled = false;
            EditorGUILayout.TextField(token);
            GUI.enabled = true;
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = token;
                Log.Info("Access token copied to clipboard");
            }
            if (GUILayout.Button("Regenerate", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog(
                    "Regenerate Access Token?",
                    "This will invalidate all existing builds. You will need to rebuild and redeploy your app to Quest.",
                    "Regenerate", "Cancel"))
                {
                    RegenerateToken();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Team Setup: If multiple developers need to connect to this server, " +
                "share this token via version control or team documentation. " +
                "Developers can enable 'Use Manual Token' in their DevAgentSettings asset " +
                "and enter the shared server's token manually.",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// Draw only the Authentication section. Can be called from other settings panels
        /// (e.g., MCPBridge) to show the shared token UI without duplicating code.
        /// </summary>
        public static void DrawAuthenticationUI()
        {
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Authentication prevents unauthorized devices on the same network from sending requests to this server. " +
                "A unique token is generated here and automatically injected into builds at build time, ensuring only apps built from this project can connect. " +
                "This token is shared between AgentBridge and MCPBridge.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            // Ensure token exists
            var token = EnsureToken();

            // Display token in a disabled text field (greyed out)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Access Token");
            GUI.enabled = false;
            EditorGUILayout.TextField(token);
            GUI.enabled = true;
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = token;
                Log.Info("Access token copied to clipboard");
            }
            if (GUILayout.Button("Regenerate", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog(
                    "Regenerate Access Token?",
                    "This will invalidate all existing builds and MCP registrations. You will need to rebuild and redeploy your app, and re-register with AI agents.",
                    "Regenerate", "Cancel"))
                {
                    RegenerateToken();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Team Setup: If multiple developers need to connect to this server, " +
                "share this token via version control or team documentation. " +
                "Developers can enable 'Use Manual Token' in their DevAgentSettings asset " +
                "and enter the shared server's token manually.",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// Reset all remote server settings to their defaults.
        /// </summary>
        public static void ResetToDefaults()
        {
            Port.Reset();
            AutoStart.Reset();
        }
    }
}
