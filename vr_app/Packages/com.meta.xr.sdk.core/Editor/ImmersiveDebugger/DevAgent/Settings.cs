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
using Meta.XR.Editor.Tags;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles;
using static Meta.XR.Editor.UserInterface.Utils;
using AgentBridgeStyles = Meta.XR.AI.AgentBridge.Styles;
using AgentBridgeUtils = Meta.XR.AI.AgentBridge.Utils;

namespace Meta.XR.ImmersiveDebugger.DevAgent.Editor
{
    /// <summary>
    /// Settings UI for ImmersiveDebugger.DevAgent, rendered as "AI Assistant" section
    /// within Immersive Debugger's Project Settings panel.
    /// </summary>
    [InitializeOnLoad]
    internal static class Settings
    {
        static Settings()
        {
            ImmersiveDebugger.Editor.Utils.ToolDescriptor.OnProjectSettingsGUI += OnGUI;

            // Auto-sync client settings from server on editor load to avoid
            // "Token Mismatch" and stale server address when the settings panel is first opened.
            Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(SyncSettingsIfNeeded);
        }

        private static void SyncSettingsIfNeeded()
        {
            if (RuntimeSettings.Instance == null)
                return;

            var dirty = false;

            // Auto-sync token
            if (string.IsNullOrEmpty(RuntimeSettings.Instance.AccessToken))
            {
                var serverToken = RemoteAgentSettings.EnsureToken();
                RuntimeSettings.Instance.SetAccessToken(serverToken);
                dirty = true;
            }

            // Auto-detect server address on first creation only (empty default).
            // After that, the user can click "Detect IP" to update manually.
            if (string.IsNullOrEmpty(RuntimeSettings.Instance.ServerAddress))
            {
                RuntimeSettings.Instance.ServerAddress = NetworkUtilities.GetLocalNetworkAddress();
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(RuntimeSettings.Instance);
                AssetDatabase.SaveAssetIfDirty(RuntimeSettings.Instance);
            }
        }

        #region Setting Definitions

        private static readonly Setting Enabled = new CustomBool
        {
            Uid = nameof(Enabled),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null && RuntimeSettings.Instance.Enabled,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.Enabled = val; },
            Label = "Enable AI Assistant",
            Tooltip = "Enable the AI Assistant panel in Immersive Debugger. When disabled, the panel won't be registered at runtime.",
            SendTelemetry = true
        };

        private static readonly Setting ServerAddress = new CustomString
        {
            Uid = nameof(ServerAddress),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.ServerAddress : NetworkUtilities.GetLocalNetworkAddress(),
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.ServerAddress = val; },
            Label = "Server Address",
            Tooltip = "IP address of the Unity Editor running AgentBridge. " +
                "Auto-detected from this machine's local network. " +
                "At build time, the current IP is automatically injected into the build.",
            SendTelemetry = true
        };

        private static readonly Setting ServerPort = new CustomInt
        {
            Uid = nameof(ServerPort),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.ServerPort : 48735,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.ServerPort = val; },
            Label = "Server Port",
            Tooltip = "Port number for the AgentBridge Remote Server.",
            SendTelemetry = true
        };

        private static readonly Setting AccessToken = new CustomString
        {
            Uid = nameof(AccessToken),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.AccessToken : "",
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.SetAccessToken(val); },
            Label = "Access Token",
            Tooltip = "The access token for authenticating with the Remote Agent Server.",
            SendTelemetry = false
        };

        private static readonly Setting McpServerPort = new CustomInt
        {
            Uid = nameof(McpServerPort),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.McpServerPort : 8090,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.McpServerPort = val; },
            Label = "MCP Bridge Port",
            Tooltip = "Port number for the MCP Bridge HTTP server. This should match the port configured in MCPBridge settings.",
            SendTelemetry = true
        };

        private static readonly Setting PushToTalkButton = new CustomFlags<OVRInput.Button>
        {
            Uid = nameof(PushToTalkButton),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.PushToTalkButton : OVRInput.Button.One,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.PushToTalkButton = val; },
            Label = "Controller Push-To-Talk Button",
            Tooltip = "Controller button used for push-to-talk voice input when using controllers.",
            SendTelemetry = true
        };

        private static readonly Setting HandPushToTalkGesture = new CustomEnum<HandPinchGesture>
        {
            Uid = nameof(HandPushToTalkGesture),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.HandPushToTalkGesture : HandPinchGesture.MiddleFingerPinch,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.HandPushToTalkGesture = val; },
            Label = "Hand Push-To-Talk Gesture",
            Tooltip = "Hand pinch gesture used for push-to-talk when using hand tracking. " +
                "Middle Finger Pinch is recommended to avoid conflict with the index pinch used for UI interaction.",
            SendTelemetry = true
        };

        private static readonly Setting EnableDelayedRelease = new CustomBool
        {
            Uid = nameof(EnableDelayedRelease),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.EnableDelayedRelease : true,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.EnableDelayedRelease = val; },
            Label = "Enable Delayed Release",
            Tooltip = "Enable delayed release for better dictation timing.",
            SendTelemetry = true
        };

        private static readonly Setting ReleaseDelay = new CustomFloat
        {
            Uid = nameof(ReleaseDelay),
            Owner = ImmersiveDebugger.Editor.Utils.ToolDescriptor,
            Get = () => RuntimeSettings.Instance != null ? RuntimeSettings.Instance.ReleaseDelay : 0.8f,
            Set = val => { if (RuntimeSettings.Instance != null) RuntimeSettings.Instance.ReleaseDelay = val; },
            Label = "Release Delay",
            Tooltip = "Delay in seconds before releasing push-to-talk.",
            SendTelemetry = true
        };

        #endregion

        #region UI Drawing

        private const string Description =
            "The <b>AI Assistant</b> allows you to interact with an AI agent directly from your VR headset using voice commands. " +
            "It connects to the <b>AgentBridge</b> service running in the Unity Editor and enables natural language debugging, " +
            "scene manipulation, and code assistance while immersed in your application.";

        private static void Draw(this Setting setting, Origins origin)
        {
            setting.DrawForGUI(origin, ImmersiveDebugger.Editor.Utils.ToolDescriptor, SetDirty);
        }

        private static void SetDirty()
        {
            if (RuntimeSettings.Instance != null)
            {
                EditorUtility.SetDirty(RuntimeSettings.Instance);
            }
        }

        public static void OnGUI(Origins origin, string searchContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI Assistant", GUIStyles.BoldLabel);

            // Experimental notice
            EditorGUILayout.BeginHorizontal(GUIStyles.ExperimentalNoticeBox);
            var experimentalTag = new Tag("Experimental");
            experimentalTag.Draw();
            EditorGUILayout.LabelField(
                "<b>AI Assistant</b> is currently an experimental feature.",
                GUIStyles.ExperimentalNoticeTextStyle);
            EditorGUILayout.EndHorizontal();

            // Description notice
            DrawDescriptionNotice();

            Enabled.Draw(origin);

            // Auto-sync token if empty (before status check to avoid false "Token Mismatch")
            if (RuntimeSettings.Instance != null && string.IsNullOrEmpty(RuntimeSettings.Instance.AccessToken))
            {
                RuntimeSettings.Instance.SetAccessToken(RemoteAgentSettings.EnsureToken());
                SetDirty();
            }

            EditorGUILayout.Space();

            // AgentBridge status
            DrawAgentBridgeStatus();

            EditorGUILayout.Space();

            if (Foldout("AgentBridge Remote Server", "AgentBridge Remote Server"))
            {
                using (new IndentScope(EditorGUI.indentLevel + 1))
                {
                    // Server address with "Detect IP" button
                    EditorGUILayout.BeginHorizontal();
                    ServerAddress.Draw(origin);
                    if (GUILayout.Button("Detect IP", GUILayout.Width(80)))
                    {
                        var detectedIp = NetworkUtilities.GetLocalNetworkAddress();
                        if (RuntimeSettings.Instance != null)
                        {
                            RuntimeSettings.Instance.ServerAddress = detectedIp;
                            SetDirty();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.HelpBox(
                        "This address is auto-detected from your local network and injected into builds automatically. " +
                        "Quest headsets will use it to connect back to this editor over the local network.\n\n" +
                        "If you are running in XR Simulator or Play Mode locally, use 127.0.0.1 instead.",
                        MessageType.Info);

                    ServerPort.Draw(origin);
                    McpServerPort.Draw(origin);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        "Authentication prevents unauthorized devices on the same network from sending AI requests to the server. " +
                        "The token is generated by AgentBridge and must match between client and server.",
                        EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.Space();

                    // Token field with "Use Project Token" button
                    EditorGUILayout.BeginHorizontal();
                    AccessToken.Draw(origin);
                    if (GUILayout.Button("Use Project Token", GUILayout.Width(130)))
                    {
                        RuntimeSettings.Instance.SetAccessToken(RemoteAgentSettings.AccessToken.Value);
                        SetDirty();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (Foldout("Voice Input", "Voice Input"))
            {
                using (new IndentScope(EditorGUI.indentLevel + 1))
                {
#if HAS_META_VOICE_SDK
                    // Wit.ai Configuration
                    DrawWitConfigurationField();

                    PushToTalkButton.Draw(origin);
                    HandPushToTalkGesture.Draw(origin);
                    EnableDelayedRelease.Draw(origin);
                    ReleaseDelay.Draw(origin);
#else
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(
                        "Voice SDK (com.meta.xr.sdk.voice) is not installed. " +
                        "Voice input is disabled. Install the Voice SDK to use the AI Assistant.",
                        EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField(
                        "To enable voice input:\n" +
                        "1. Open Window > Package Manager\n" +
                        "2. Add the Meta XR Voice SDK package\n" +
                        "3. Reimport to activate push-to-talk and dictation",
                        EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndVertical();
#endif
                }
            }
        }

        /// <summary>
        /// Draw the description notice box explaining what AI Assistant is.
        /// </summary>
        private static void DrawDescriptionNotice()
        {
            var descriptionBoxStyle = new GUIStyle(GUIStyles.DialogBox)
            {
                padding = new RectOffset(
                    Constants.Margin + Constants.Padding,
                    Constants.Margin,
                    Constants.Margin,
                    Constants.Margin)
            };

            EditorGUILayout.BeginHorizontal(descriptionBoxStyle);
            using (new ColorScope(ColorScope.Scope.Content, Colors.AI))
            {
                GUILayout.Label(AgentBridgeStyles.Contents.MainIcon, GUIStyles.DialogIconStyle);
            }
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(Description, GUIStyles.DialogTextStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the AgentBridge status section showing validation state and settings button.
        /// </summary>
        private static void DrawAgentBridgeStatus()
        {
            // Get the current AgentBridge service and its validation status
            var service = AgentBridgeManager.GetCurrentService();
            var validationService = service as IServiceValidation;
            var isRemoteServerRunning = RemoteAgentServer.IsRunning;
            var selectedServiceId = AI.AgentBridge.Settings.SelectedServiceId.Value;

            // Check token validity - must match server's token
            var serverToken = RemoteAgentSettings.AccessToken.Value;
            var clientToken = RuntimeSettings.Instance?.AccessToken ?? "";
            var tokenValid = !string.IsNullOrEmpty(clientToken) && clientToken == serverToken;

            var statusBoxStyle = new GUIStyle(GUIStyles.DialogBox)
            {
                padding = new RectOffset(
                    Constants.Margin + Constants.Padding,
                    Constants.Margin,
                    Constants.Margin,
                    Constants.Margin)
            };

            EditorGUILayout.BeginHorizontal(statusBoxStyle);

            // Determine overall status based on both AI service and Remote Server
            GUIContent statusIcon;
            Color statusColor;
            string statusTitle;
            string statusMessage;

            // Build service info string
            var serviceName = service?.ServiceName ?? selectedServiceId ?? "None";

            // Token validation takes priority
            if (!AI.AgentBridge.Settings.Enabled)
            {
                statusIcon = Contents.ErrorIcon;
                statusColor = Colors.WarningColor;
                statusTitle = "AI Agent Bridge Disabled";
                statusMessage = $"AI Agent Bridge is not enabled, this is required for any AI capabilities within Meta XR SDK Tools.";
            }
            else if (!tokenValid)
            {
                statusIcon = Contents.ErrorIcon;
                statusColor = Colors.WarningColor;
                statusTitle = "Token Mismatch";
                statusMessage = "Access token doesn't match the server. Click 'Use Project Token' to sync.";
            }
            else if (validationService != null)
            {
                var result = validationService.CurrentValidationResult;

                if (result.Status == ValidationStatus.Valid && isRemoteServerRunning)
                {
                    // Both AI service and Remote Server are ready
                    statusIcon = Contents.CheckIcon;
                    statusColor = Colors.AI;
                    statusTitle = "AI Assistant Ready";
                    statusMessage = $"Service: {serviceName} • Remote Server: Running (port {RemoteAgentSettings.Port.Value})";
                }
                else if (result.Status == ValidationStatus.Valid && !isRemoteServerRunning)
                {
                    // AI service is valid but Remote Server is not running
                    statusIcon = Contents.InfoIcon;
                    statusColor = Colors.LightGray;
                    statusTitle = "Remote Server Not Running";
                    statusMessage = $"Service: {serviceName} is ready, but the Remote Server must be started for Quest connectivity.";
                }
                else if (result.Status == ValidationStatus.Validating)
                {
                    statusIcon = Contents.InfoIcon;
                    statusColor = Colors.LightGray;
                    statusTitle = "Validating AgentBridge...";
                    statusMessage = $"Service: {serviceName} • Checking configuration...";
                }
                else
                {
                    // AI service has issues
                    (statusIcon, statusColor) = GetValidationStatusVisuals(result.Status);
                    statusTitle = GetValidationStatusTitle(result.Status);
                    statusMessage = $"Service: {serviceName} • {result.Message}";
                }
            }
            else if (service != null)
            {
                // Service exists but doesn't support validation
                if (isRemoteServerRunning)
                {
                    statusIcon = Contents.CheckIcon;
                    statusColor = Colors.AI;
                    statusTitle = "AI Assistant Ready";
                    statusMessage = $"Service: {serviceName} • Remote Server: Running (port {RemoteAgentSettings.Port.Value})";
                }
                else
                {
                    statusIcon = Contents.InfoIcon;
                    statusColor = Colors.LightGray;
                    statusTitle = "Remote Server Not Running";
                    statusMessage = $"Service: {serviceName} is ready, but the Remote Server must be started for Quest connectivity.";
                }
            }
            else
            {
                // No service initialized
                statusIcon = Contents.InfoIcon;
                statusColor = Colors.LightGray;
                statusTitle = "AgentBridge Not Initialized";
                statusMessage = $"Selected Service: {serviceName} • Open AgentBridge preferences to configure and start the Remote Server.";
            }

            using (new ColorScope(ColorScope.Scope.Content, statusColor))
            {
                GUILayout.Label(statusIcon, GUIStyles.DialogIconStyle);
            }

            // Title and message
            EditorGUILayout.BeginVertical();
            var titleStyle = new GUIStyle(GUIStyles.BoldLabel)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            var subtextStyle = new GUIStyle(GUIStyles.DialogTextStyle)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Constants.Padding, 0, 0, 0)
            };
            EditorGUILayout.LabelField(statusTitle, titleStyle);
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.LabelField(statusMessage, subtextStyle);
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Open AgentBridge Settings button
            if (GUILayout.Button("Open AgentBridge Settings"))
            {
                AgentBridgeUtils.ToolDescriptor.OpenUserSettings(Origins.ProjectSettings);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the Wit.ai configuration field with explanation of demo vs custom config.
        /// </summary>
#if HAS_META_VOICE_SDK
        private static void DrawWitConfigurationField()
        {
            var settings = RuntimeSettings.Instance;
            if (settings == null) return;

            EditorGUI.BeginChangeCheck();
            var current = settings.WitConfiguration as Meta.WitAi.Data.Configuration.WitConfiguration;
            var newConfig = EditorGUILayout.ObjectField(
                new GUIContent("Wit Configuration",
                    "Assign a WitConfiguration asset for custom Wit.ai app settings. " +
                    "If left empty, a built-in demo app is used with limited quotas."),
                current,
                typeof(Meta.WitAi.Data.Configuration.WitConfiguration),
                false) as Meta.WitAi.Data.Configuration.WitConfiguration;
            if (EditorGUI.EndChangeCheck())
            {
                settings.WitConfiguration = newConfig;
                SetDirty();
            }

            if (current == null)
            {
                EditorGUILayout.HelpBox(
                    "Using built-in demo Wit.ai app (speech-to-text only).\n" +
                    "• Limited request quota shared across all demo users\n" +
                    "• No custom intents, entities, or trained models\n\n" +
                    "For more heavy and customized usage, create a free Wit.ai app at https://wit.ai, " +
                    "then create a WitConfiguration asset (Assets > Create > Wit > Configuration) " +
                    "and assign it above for higher rate limits and custom voice commands.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
        }
#endif

        /// <summary>
        /// Get the visual representation for a validation status.
        /// </summary>
        private static (GUIContent icon, Color color) GetValidationStatusVisuals(ValidationStatus status)
        {
            return status switch
            {
                ValidationStatus.Valid => (Contents.CheckIcon, Colors.AI),
                ValidationStatus.Invalid => (Contents.ErrorIcon, Colors.ErrorColor),
                ValidationStatus.Error => (Contents.ErrorIcon, Colors.ErrorColor),
                ValidationStatus.Validating => (Contents.InfoIcon, Colors.LightGray),
                _ => (Contents.InfoIcon, Colors.LightGray)
            };
        }

        /// <summary>
        /// Get the title text for a validation status.
        /// </summary>
        private static string GetValidationStatusTitle(ValidationStatus status)
        {
            return status switch
            {
                ValidationStatus.Valid => "AgentBridge Ready",
                ValidationStatus.Invalid => "AgentBridge Configuration Invalid",
                ValidationStatus.Error => "AgentBridge Validation Error",
                ValidationStatus.Validating => "Validating AgentBridge...",
                _ => "AgentBridge Validation Required"
            };
        }

        #endregion
    }
}
