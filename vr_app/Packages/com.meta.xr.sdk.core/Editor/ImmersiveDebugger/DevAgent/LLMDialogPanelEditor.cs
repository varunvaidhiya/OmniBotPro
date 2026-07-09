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

using Meta.XR.Editor.Id;
using Meta.XR.ImmersiveDebugger.Editor;
using Meta.XR.ImmersiveDebugger.UserInterface;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles;
using static Meta.XR.Editor.UserInterface.Styles.Constants;
using static Meta.XR.Editor.UserInterface.Styles.Contents;
using static Meta.XR.Editor.UserInterface.Utils;
using IDUtils = Meta.XR.ImmersiveDebugger.Editor.Utils;

namespace Meta.XR.ImmersiveDebugger.DevAgent.Editor
{
    /// <summary>
    /// Custom Unity Editor inspector for <see cref="LLMDialogPanel"/>.
    /// Provides buttons and controls for testing AI Assistant features in Editor Play Mode
    /// without requiring VR controllers or voice input.
    /// </summary>
    [CustomEditor(typeof(LLMDialogPanel))]
    public class LLMDialogPanelEditor : UnityEditor.Editor
    {
        private LLMDialogPanel _panel;
        private DebugInterface _debugInterface;
        private string _testMessage = "";

        private float _dialogNoticeHorizontalPadding;
        private float _dialogNoticeVerticalPadding;
        private GUIContent _dialogNotice;

        private void Awake()
        {
            _dialogNotice = new GUIContent($"<b>{target.GetType().Name}</b> is part of the <b>{IDUtils.PublicName}</b>." +
                                           $"\nUse the controls below to test the AI Assistant in Play Mode without voice input.");

            _dialogNoticeHorizontalPadding = 3
                                             + GUIStyles.DialogIconStyle.fixedWidth
                                             + GUIStyles.DialogBox.padding.left
                                             + GUIStyles.DialogBox.padding.right;

            _dialogNoticeVerticalPadding = GUIStyles.DialogBox.padding.bottom + GUIStyles.DialogBox.padding.top;
        }

        private void OnEnable()
        {
            _panel = (LLMDialogPanel)target;
        }

        public override void OnInspectorGUI()
        {
            ShowHeaderGUI();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use AI Assistant controls.", MessageType.Warning);
                return;
            }

            if (_panel == null)
            {
                EditorGUILayout.HelpBox("LLMDialogPanel reference is not available.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(Margin);
            DrawStatus();
            EditorGUILayout.Space(Margin);
            DrawSendTestMessage();
            EditorGUILayout.Space(Margin);
            DrawControls();
        }

        private void ShowHeaderGUI()
        {
            var currentWidth = EditorGUIUtility.currentViewWidth;
            var expectedButtonHeight = Meta.XR.Editor.ToolingSupport.Styles.Constants.Height;

            GUILayout.BeginArea(new Rect(0, 0, currentWidth, expectedButtonHeight));
            EditorGUILayout.BeginHorizontal();
            IDUtils.ToolDescriptor.DrawButton(null, false, true, Origins.Component);
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
            GUILayoutUtility.GetRect(currentWidth, expectedButtonHeight);

            var infoWidth = currentWidth - _dialogNoticeHorizontalPadding;
            var expectedInfoHeight = GUIStyles.DialogTextStyle.CalcHeight(_dialogNotice, infoWidth);
            expectedInfoHeight += _dialogNoticeVerticalPadding;
            GUILayout.BeginArea(new Rect(0, expectedButtonHeight, currentWidth, expectedInfoHeight));
            EditorGUILayout.BeginHorizontal(GUIStyles.DialogBox);
            EditorGUILayout.LabelField(DialogIcon, GUIStyles.DialogIconStyle,
                GUILayout.Width(GUIStyles.DialogIconStyle.fixedWidth));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(_dialogNotice, GUIStyles.DialogTextStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUILayoutUtility.GetRect(currentWidth, expectedInfoHeight);
        }

        private void DrawStatus()
        {
            EditorGUILayout.LabelField("Status", GUIStyles.BoldLabel);

            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);

            var conversationManager = _panel.ConversationManager;
            if (conversationManager != null)
            {
                // AgentBridge connection status
                var connectionStatus = conversationManager.CurrentConnectionStatus;
                var isConnected = connectionStatus == ConversationManager.ConnectionStatus.Connected;
                DrawStatusRow("AgentBridge", isConnected ? "Connected" : "Disconnected", isConnected);

                // MCP Bridge connection status
                var controller = _panel.GetComponent<DevAgentController>();
                if (controller?.McpIntegration != null)
                {
                    var mcpConnected = controller.McpIntegration.IsConnected;
                    DrawStatusRow("MCP Bridge", mcpConnected ? "Connected" : "Disconnected", mcpConnected);
                }
                else
                {
                    DrawStatusRow("MCP Bridge", "Not available", false);
                }

                // Voice status
                var voiceStatus = conversationManager.CurrentVoiceStatus;
                var isListening = voiceStatus == ConversationManager.VoiceStatus.Listening;
                DrawStatusRow("Voice", voiceStatus.ToString(), isListening);

                // Processing status
                var isProcessing = _panel.IsConversationActive;
                DrawStatusRow("Processing", isProcessing ? "Active" : "Idle", isProcessing);

                // Entry count
                var entryCount = conversationManager.Entries?.Count ?? 0;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Entries", EditorStyles.label, GUILayout.Width(150));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(entryCount.ToString(), EditorStyles.label, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("ConversationManager not available", EditorStyles.label);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(string label, string value, bool isActive)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            using (new ColorScope(ColorScope.Scope.Content, isActive ? Styles.Colors.AccentColorBrighter : Color.white))
            {
                EditorGUILayout.LabelField(value, EditorStyles.label, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSendTestMessage()
        {
            EditorGUILayout.LabelField("Send Test Message", GUIStyles.BoldLabel);

            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);

            _testMessage = EditorGUILayout.TextField(_testMessage);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_testMessage)))
            {
                if (GUILayout.Button("Send", GUILayout.Height(24)))
                {
                    SendTestMessage();
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(24)))
            {
                _testMessage = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Visibility", GUIStyles.BoldLabel);

            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);

            // Find DebugInterface if not cached (include inactive since debugger might be hidden)
            if (_debugInterface == null)
            {
                _debugInterface = FindAnyObjectByType<DebugInterface>(FindObjectsInactive.Include);
            }

            // Overall Immersive Debugger visibility toggle
            if (_debugInterface != null)
            {
                DrawToggleRow("Immersive Debugger", _debugInterface.Visibility, () => _debugInterface.ToggleVisibility());
            }

            // AI Assistant panel visibility toggle
            DrawToggleRow("AI Assistant Panel", _panel.Visibility, () => _panel.ToggleVisibility());

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(Margin);
            EditorGUILayout.LabelField("Actions", GUIStyles.BoldLabel);

            EditorGUILayout.BeginVertical(GUIStyles.ContentBox);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Conversation", GUILayout.Height(24)))
            {
                _panel.ConversationManager?.ClearConversation();
            }

            using (new EditorGUI.DisabledScope(!_panel.IsConversationActive))
            {
                if (GUILayout.Button("Cancel", GUILayout.Height(24)))
                {
                    _panel.ConversationManager?.CancelConversation();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToggleRow(string label, bool currentState, System.Action onToggle)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            using (new ColorScope(ColorScope.Scope.All, currentState ? Styles.Colors.AccentColorBrighter : Color.white))
            {
                if (GUILayout.Toggle(currentState, "", GUILayout.Width(16)))
                {
                    if (!currentState) onToggle?.Invoke();
                }
                else
                {
                    if (currentState) onToggle?.Invoke();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SendTestMessage()
        {
            if (string.IsNullOrWhiteSpace(_testMessage)) return;

            var controller = _panel.GetComponent<DevAgentController>();
            if (controller != null && controller.Integration != null)
            {
                controller.Integration.SendTextMessage(_testMessage);
                _testMessage = "";
                GUI.FocusControl(null);
            }
            else
            {
                Debug.LogWarning("[LLMDialogPanelEditor] Could not find DevAgentController or AgentBridgeIntegration");
            }
        }
    }
}
