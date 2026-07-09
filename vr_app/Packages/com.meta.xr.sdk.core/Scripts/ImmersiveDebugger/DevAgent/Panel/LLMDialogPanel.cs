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

using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Meta.XR.ImmersiveDebugger.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// This is a <see cref="MonoBehaviour"/> for the LLM Dialog panel of Immersive Debugger.
    /// Provides an in-headset UI for LLM conversations, showing conversation history and tool usage.
    /// Integrates with VoiceDebugAssistant for voice input and LLM interactions.
    /// For more info about Immersive Debugger, check out the [official doc](https://developer.oculus.com/documentation/unity/immersivedebugger-overview)
    /// </summary>
    internal class LLMDialogPanel : UserInterface.DebugPanel
    {
        private const int NumberOfLines = 10;
        private const int FullConversationPanelBottomMargin = 80;
        private const int ContractedConversationPanelBottomMargin = 180;

        // Re-export status enums for backward compatibility
        internal enum ConnectionStatus { Disconnected, Connected }
        internal enum VoiceStatus { Waiting, Listening, Processing }

        internal static event Action OnLLMDialogPanelReady;

        // Forward events from ConversationManager (with null checks for test scenarios where Setup() isn't called)
        internal event Action OnConversationHistoryCleared
        {
            add { if (_conversationManager != null) _conversationManager.OnConversationHistoryCleared += value; }
            remove { if (_conversationManager != null) _conversationManager.OnConversationHistoryCleared -= value; }
        }

        internal event Action OnConversationCancelled
        {
            add { if (_conversationManager != null) _conversationManager.OnConversationCancelled += value; }
            remove { if (_conversationManager != null) _conversationManager.OnConversationCancelled -= value; }
        }

        internal bool Dirty { get; set; }

        // Controller that owns ConversationManager and AgentBridgeIntegration
        private DevAgentController _controller;

        // Conversation manager - owned by controller
        private ConversationManager _conversationManager;
        internal ConversationManager ConversationManager => _conversationManager;

        // UI elements
        private ScrollView _conversationScrollView;
        private ScrollView _scrollViewMessageDetails;
        private Flex _conversationFlex;
        private Flex _controlsContainer;
        private Flex _leftStatusContainer;
        private ButtonWithIcon _clearButton;
        private Toggle _cancelButton;
        private Toggle _autoScrollToggle;

        // Status pill components - AgentBridge
        private Flex _agentBridgeStatusContainer;
        private Toggle _agentBridgeStatusPill;
        private Label _agentBridgeStatusLabel;

        // Status pill components - MCP Bridge
        private Flex _mcpBridgeStatusContainer;
        private Toggle _mcpBridgeStatusPill;
        private Label _mcpBridgeStatusLabel;

        // Status pill components - Voice
        private Flex _voiceStatusContainer;
        private Toggle _voiceStatusPill;
        private Label _voiceStatusLabel;

        // Message details panel components
        private Label _messageDetailLabel;
        private ButtonWithIcon _messageDetailPaneCloseBtn;
        private Background _messageDetailPaneBackground;
        private ImageStyle _messageDetailPaneBackgroundImageStyle;

        // UI element tracking for conversation entries
        private readonly Dictionary<ConversationEntry, ConversationLine> _entryUIElements = new();

        // Auto-scroll state tracking
        private bool _autoScrollEnabled = true;

        // Thinking animation controller
        private ThinkingAnimationController _thinkingAnimationController;

        // Welcome watermark overlay
        private Flex _welcomeOverlay;

        internal ImageStyle MessageDetailBackgroundStyle
        {
            set
            {
                _messageDetailPaneBackground.Sprite = value.sprite;
                _messageDetailPaneBackground.Color = value.color;
                _messageDetailPaneBackground.PixelDensityMultiplier = value.pixelDensityMultiplier;
            }
        }

        // Property to expose IsConversationActive from the manager
        internal bool IsConversationActive
        {
            get => _conversationManager?.IsConversationActive ?? false;
            set
            {
                if (_conversationManager != null)
                {
                    if (value)
                        _conversationManager.StartProcessing();
                    else
                        _conversationManager.StopProcessing();
                }
            }
        }

        protected override void Setup(Controller owner)
        {
            base.Setup(owner);

            // Initialize DevAgentController (creates ConversationManager + AgentBridgeIntegration)
            _controller = gameObject.AddComponent<DevAgentController>();
            _controller.Initialize();
            _conversationManager = _controller.ConversationManager;
            SubscribeToManagerEvents();

            // Main container for the panel content
            var mainFlex = Append<Flex>("main");
            mainFlex.LayoutStyle = Style.Load<LayoutStyle>("ConsoleFlex");

            // Controls section at the top
            _controlsContainer = mainFlex.Append<Flex>("controls");
            _controlsContainer.LayoutStyle = Style.Load<LayoutStyle>("StatusBarControls");

            // Left container for status pills (takes remaining space)
            _leftStatusContainer = _controlsContainer.Append<Flex>("leftStatus");
            _leftStatusContainer.LayoutStyle = Style.Load<LayoutStyle>("LeftStatusContainer");

            // AgentBridge connection status pill (in left container)
            CreateAgentBridgeStatusPill();

            // MCP Bridge connection status pill (in left container)
            CreateMcpBridgeStatusPill();

            // Voice status pill (in left container)
            CreateVoiceStatusPill();

            // Clear conversation button (rightmost in main container)
            _clearButton = _controlsContainer.Append<ButtonWithIcon>("clearButton");
            _clearButton.LayoutStyle = Style.Load<LayoutStyle>("ConsoleButton");
            _clearButton.Icon = Resources.Load<Texture2D>("Textures/bin_icon");
            _clearButton.IconStyle = Style.Load<ImageStyle>("BinIcon");
            _clearButton.Callback = OnClearButtonClicked;

            // Auto-scroll toggle button (left of cancel button)
            _autoScrollToggle = _controlsContainer.Append<Toggle>("autoScrollToggle");
            _autoScrollToggle.LayoutStyle = Style.Load<LayoutStyle>("ConsoleButton");
            _autoScrollToggle.Icon = Resources.Load<Texture2D>("Textures/scroll_to_bottom");
            _autoScrollToggle.IconStyle = Style.Load<ImageStyle>("ScrollToBottom");
            _autoScrollToggle.Callback = OnAutoScrollToggleClicked;
            _autoScrollToggle.State = _autoScrollEnabled;

            // Cancel button (left of clear button)
            _cancelButton = _controlsContainer.Append<Toggle>("cancelButton");
            _cancelButton.LayoutStyle = Style.Load<LayoutStyle>("ConsoleButton");
            _cancelButton.Icon = Resources.Load<Texture2D>("Textures/square-circle");
            _cancelButton.IconStyle = Style.Load<ImageStyle>("StopIcon");
            _cancelButton.Callback = OnCancelButtonClicked;
            _cancelButton.State = false;

            // Conversation history scroll view with regular Flex (no virtualization)
            _conversationScrollView = mainFlex.Append<ScrollView>("conversation");
            _conversationScrollView.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLogsScrollView");
            _conversationScrollView.Flex.LayoutStyle = Style.Load<LayoutStyle>("ConversationLogs");

            // Use the scroll view's flex directly instead of ProxyFlex to avoid virtualization issues
            _conversationFlex = _conversationScrollView.Flex;

            // Initialize thinking animation controller
            _thinkingAnimationController = new ThinkingAnimationController(this, _conversationFlex);

            // Welcome watermark overlay (centered in the scroll view, disappears on first interaction)
            CreateWelcomeWatermark();

            // Message detail panel (similar to Console's log detail panel)
            _messageDetailPaneBackground = Append<Background>("messageDetailBackground");
            _messageDetailPaneBackground.LayoutStyle = Style.Load<LayoutStyle>("LogDetailsPaneBackground");
            _messageDetailPaneBackgroundImageStyle = Style.Load<ImageStyle>("LogDetailPaneBackground");
            MessageDetailBackgroundStyle = _messageDetailPaneBackgroundImageStyle;

            _scrollViewMessageDetails = Append<ScrollView>("messageDetails");
            _scrollViewMessageDetails.LayoutStyle = Style.Load<LayoutStyle>("LogDetailsScrollView");
            _scrollViewMessageDetails.Flex.LayoutStyle = Style.Load<LayoutStyle>("ConsoleLogDetails");

            _messageDetailLabel = _scrollViewMessageDetails.Flex.Append<Label>("messageEntry");
            _messageDetailLabel.LayoutStyle = Style.Load<LayoutStyle>("ConsoleLineLogDetailsLabel");
            _messageDetailLabel.TextStyle = Style.Load<TextStyle>("ConsoleLogDetailsLabel");
            _messageDetailLabel.Text.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Message detail panel close button
            _messageDetailPaneCloseBtn = Append<ButtonWithIcon>("closeMessageDetail");
            _messageDetailPaneCloseBtn.LayoutStyle = Style.Load<LayoutStyle>("LogDetailPaneCloseButton");
            _messageDetailPaneCloseBtn.Icon = Resources.Load<Texture2D>("Textures/close_icon");
            _messageDetailPaneCloseBtn.IconStyle = Style.Load<ImageStyle>("LogDetailPaneCloseButton");
            _messageDetailPaneCloseBtn.Callback = HideMessageDetailsPanel;

            HideMessageDetailsPanel();

            // Set up the click handler for conversation entries
            ConversationEntry.OnDisplayDetails = OnConversationLineClicked;

            OnLLMDialogPanelReady?.Invoke();
        }

        private void SubscribeToManagerEvents()
        {
            _conversationManager.OnEntryAdded += OnEntryAdded;
            _conversationManager.OnEntryUpdated += OnEntryUpdated;
            _conversationManager.OnEntryRemoved += OnEntryRemoved;
            _conversationManager.OnEntriesCleared += OnEntriesCleared;
            _conversationManager.OnConnectionStatusChanged += OnConnectionStatusChanged;
            _conversationManager.OnVoiceStatusChanged += OnVoiceStatusChanged;
            _conversationManager.OnConversationActiveChanged += OnConversationActiveChanged;

            // Subscribe to MCP Bridge connection state changes
            if (_controller?.McpIntegration != null)
            {
                _controller.McpIntegration.OnConnectionStateChanged += OnMcpBridgeConnectionStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (_conversationManager != null)
            {
                _conversationManager.OnEntryAdded -= OnEntryAdded;
                _conversationManager.OnEntryUpdated -= OnEntryUpdated;
                _conversationManager.OnEntryRemoved -= OnEntryRemoved;
                _conversationManager.OnEntriesCleared -= OnEntriesCleared;
                _conversationManager.OnConnectionStatusChanged -= OnConnectionStatusChanged;
                _conversationManager.OnVoiceStatusChanged -= OnVoiceStatusChanged;
                _conversationManager.OnConversationActiveChanged -= OnConversationActiveChanged;
            }

            // Unsubscribe from MCP Bridge connection state changes
            if (_controller?.McpIntegration != null)
            {
                _controller.McpIntegration.OnConnectionStateChanged -= OnMcpBridgeConnectionStateChanged;
            }
        }

        #region Event Handlers from ConversationManager

        private void OnEntryAdded(ConversationEntry entry)
        {
            HideWelcomeWatermark();

            CreateConversationLineUI(entry);
            ScrollToBottom();
        }

        private void OnEntryUpdated(ConversationEntry entry)
        {
            if (_entryUIElements.TryGetValue(entry, out var uiElement))
            {
                uiElement.RefreshContent();
                ScrollToBottom();
            }
        }

        private void OnEntryRemoved(ConversationEntry entry)
        {
            if (_entryUIElements.TryGetValue(entry, out var uiElement))
            {
                _conversationFlex.Remove(uiElement, true);
                _entryUIElements.Remove(entry);
            }
        }

        private void OnEntriesCleared()
        {
            ClearConversationUI();
            _autoScrollEnabled = true;
            if (_autoScrollToggle != null)
            {
                _autoScrollToggle.State = true;
            }
        }

        private void OnConnectionStatusChanged(ConversationManager.ConnectionStatus status)
        {
            UpdateAgentBridgeStatusPill(status);
        }

        private void OnMcpBridgeConnectionStateChanged(bool isConnected)
        {
            UpdateMcpBridgeStatusPill(isConnected);
        }

        private void OnVoiceStatusChanged(ConversationManager.VoiceStatus status)
        {
            UpdateVoiceStatusPill(status);
        }

        private void OnConversationActiveChanged(bool isActive)
        {
            if (_cancelButton != null)
            {
                _cancelButton.State = isActive;
            }

            if (isActive)
            {
                _thinkingAnimationController?.StartAnimation();
                ScrollToBottom();
            }
            else
            {
                _thinkingAnimationController?.StopAnimation();
            }
        }

        #endregion

        #region Public API - Delegation to ConversationManager

        /// <summary>
        /// Start a live transcription entry that can be updated with partial transcription
        /// </summary>
        /// <returns>The conversation entry for live transcription updates</returns>
        internal ConversationEntry AddLiveTranscriptionEntry()
        {
            return _conversationManager?.AddLiveTranscriptionEntry();
        }

        /// <summary>
        /// Start a live transcription entry with specified message type that can be updated with partial transcription
        /// </summary>
        /// <param name="messageType">The type of message (User for right-aligned, Assistant for left-aligned)</param>
        /// <returns>The conversation entry for live transcription updates</returns>
        internal ConversationEntry AddLiveTranscriptionEntry(ConversationEntry.MessageType messageType)
        {
            return _conversationManager?.AddLiveTranscriptionEntry(messageType);
        }

        /// <summary>
        /// Update the live transcription entry with partial transcription text
        /// </summary>
        /// <param name="partialText">The partial transcription text to display</param>
        internal void UpdateLiveTranscriptionEntry(string partialText)
        {
            _conversationManager?.UpdateLiveTranscriptionEntry(partialText);
        }

        /// <summary>
        /// Finalize the live transcription entry with the complete transcription
        /// </summary>
        /// <param name="finalText">The final transcription text</param>
        internal void FinalizeLiveTranscriptionEntry(string finalText)
        {
            _conversationManager?.FinalizeLiveTranscriptionEntry(finalText);
        }

        /// <summary>
        /// Add a user message to the conversation display
        /// </summary>
        /// <param name="message">The user's message</param>
        internal void AddUserMessage(string message)
        {
            _conversationManager?.AddUserMessage(message);
        }

        /// <summary>
        /// Add an assistant message to the conversation display
        /// </summary>
        /// <param name="message">The assistant's message</param>
        internal void AddAssistantMessage(string message)
        {
            _conversationManager?.AddAssistantMessage(message);
        }

        /// <summary>
        /// Add a system message to the conversation display
        /// </summary>
        /// <param name="message">The system message</param>
        internal void AddSystemMessage(string message)
        {
            _conversationManager?.AddSystemMessage(message);
        }

        /// <summary>
        /// Add a tool call message to the conversation display
        /// </summary>
        /// <param name="toolCallsInfo">Tool calls information to display</param>
        internal void AddToolCallMessage(string toolCallsInfo)
        {
            _conversationManager?.AddToolCallMessage(toolCallsInfo);
        }

        /// <summary>
        /// Update the last tool entry status (for tool_result handling)
        /// </summary>
        /// <param name="resultContent">The tool result content to check for errors</param>
        public void UpdateLastToolStatus(string resultContent)
        {
            _conversationManager?.UpdateLastToolStatus(resultContent);
        }

        /// <summary>
        /// Add a complete message to signal the end of thinking/processing
        /// </summary>
        /// <param name="message">The completion message (can be empty)</param>
        internal void AddCompleteMessage(string message = "")
        {
            _conversationManager?.AddCompleteMessage(message);
        }

        /// <summary>
        /// Set the connection status to the AgentBridge Remote Server.
        /// </summary>
        /// <param name="status">The connection status</param>
        internal void SetConnectionStatus(ConnectionStatus status)
        {
            _conversationManager?.SetConnectionStatus((ConversationManager.ConnectionStatus)status);
        }

        /// <summary>
        /// Set the Voice system status
        /// </summary>
        /// <param name="status">The Voice status</param>
        internal void SetVoiceStatus(VoiceStatus status)
        {
            _conversationManager?.SetVoiceStatus((ConversationManager.VoiceStatus)status);
        }

        /// <summary>
        /// Stop the thinking animation
        /// </summary>
        internal void StopThinkingAnimation()
        {
            _conversationManager?.StopProcessing();
        }

        #endregion

        #region Welcome Watermark

        private void CreateWelcomeWatermark()
        {
            // Create a label that stretches across the scroll view area with centered text.
            // We bypass the layout system's anchor/offset calculations by directly configuring
            // the RectTransform to stretch-fill the parent after setting the LayoutStyle.
            _welcomeOverlay = _conversationScrollView.Append<Flex>("welcomeOverlay");
            var overlayStyle = Style.Instantiate<LayoutStyle>("Fill");
            overlayStyle.ignoreFlexLayout = true;
            _welcomeOverlay.LayoutStyle = overlayStyle;
            // Override RectTransform to stretch-fill the scroll view
            _welcomeOverlay.RectTransform.anchorMin = Vector2.zero;
            _welcomeOverlay.RectTransform.anchorMax = Vector2.one;
            _welcomeOverlay.RectTransform.offsetMin = Vector2.zero;
            _welcomeOverlay.RectTransform.offsetMax = Vector2.zero;

            var label = _welcomeOverlay.Append<Label>("welcomeLabel");
            label.LayoutStyle = Style.Instantiate<LayoutStyle>("Fill");
            label.TextStyle = Style.Load<TextStyle>("ConversationLineLabel");
            // Override RectTransform to fill the overlay
            label.RectTransform.anchorMin = Vector2.zero;
            label.RectTransform.anchorMax = Vector2.one;
            label.RectTransform.offsetMin = Vector2.zero;
            label.RectTransform.offsetMax = Vector2.zero;

            var gestureName = GetGestureDisplayName(RuntimeSettings.Instance.HandPushToTalkGesture);
            var buttonName = GetButtonDisplayName(RuntimeSettings.Instance.PushToTalkButton);
            label.Content =
                "[Experimental] AI Assistant\n\n" +
                $"Connect to AI Agents on your Unity Editor,\npinch with <b>{gestureName}</b> " +
                $"or press controller <b>{buttonName}</b> button to talk";
            label.Text.alignment = TextAnchor.MiddleCenter;
            label.Text.color = new Color(1f, 1f, 1f, 0.25f);
            label.Text.fontSize = 18;
            label.Text.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.Text.verticalOverflow = VerticalWrapMode.Overflow;
            label.Text.supportRichText = true;
        }

        private static string GetGestureDisplayName(HandPinchGesture gesture)
        {
            return gesture switch
            {
                HandPinchGesture.MiddleFingerPinch => "middle finger",
                HandPinchGesture.RingFingerPinch => "ring finger",
                HandPinchGesture.PinkyFingerPinch => "pinky finger",
                HandPinchGesture.IndexFingerPinch => "index finger",
                _ => "middle finger"
            };
        }

        private static string GetButtonDisplayName(OVRInput.Button button)
        {
            return button switch
            {
                OVRInput.Button.One => "A/X",
                OVRInput.Button.Two => "B/Y",
                OVRInput.Button.PrimaryThumbstick => "thumbstick",
                _ => "A/X"
            };
        }

        private void ShowWelcomeWatermark()
        {
            _welcomeOverlay?.Show();
        }

        private void HideWelcomeWatermark()
        {
            _welcomeOverlay?.Hide();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update method called every frame
        /// </summary>
        private void Update()
        {
            if (Dirty)
            {
                RefreshAllEntries();
                Dirty = false;
            }

            // Force scroll to bottom every frame if auto-scroll is enabled
            if (_autoScrollEnabled && _conversationScrollView != null && _conversationScrollView.Progress > 0.01f)
            {
                _conversationScrollView.Progress = 0.0f;
            }
        }

        private void ScrollToBottom()
        {
            if (_autoScrollEnabled && _conversationScrollView != null)
            {
                _conversationScrollView.Progress = 0.0f;
            }
        }

        private void CreateConversationLineUI(ConversationEntry entry)
        {
            var conversationLine = _conversationFlex.Append<ConversationLine>($"entry_{_conversationManager.Entries.Count}");
            conversationLine.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLine");
            conversationLine.Entry = entry;
            entry.UIElement = conversationLine;
            _entryUIElements[entry] = conversationLine;
        }

        private void ClearConversationUI()
        {
            foreach (var kvp in _entryUIElements)
            {
                _conversationFlex.Remove(kvp.Value, true);
                kvp.Key.UIElement = null;
            }
            _entryUIElements.Clear();
        }

        private void RefreshAllEntries()
        {
            float savedScrollProgress = 0f;
            if (!_autoScrollEnabled && _conversationScrollView != null)
            {
                savedScrollProgress = _conversationScrollView.Progress;
            }

            ClearConversationUI();

            foreach (var entry in _conversationManager.Entries)
            {
                CreateConversationLineUI(entry);
            }

            if (!_autoScrollEnabled && _conversationScrollView != null)
            {
                StartCoroutine(RestoreScrollPositionCoroutine(savedScrollProgress));
            }
        }

        private IEnumerator RestoreScrollPositionCoroutine(float targetProgress)
        {
            yield return null;

            if (_conversationScrollView != null && !_autoScrollEnabled)
            {
                _conversationScrollView.Progress = targetProgress;
            }
        }

        #endregion

        #region Status Pill UI

        private void CreateAgentBridgeStatusPill()
        {
            _agentBridgeStatusContainer = _leftStatusContainer.Append<Flex>("agentBridgeStatusContainer");
            _agentBridgeStatusContainer.LayoutStyle = Style.Load<LayoutStyle>("StatusPillContainer");

            _agentBridgeStatusPill = _agentBridgeStatusContainer.Append<Toggle>("agentBridgeStatusPill");
            _agentBridgeStatusPill.LayoutStyle = Style.Load<LayoutStyle>("StatusPill");

            _agentBridgeStatusLabel = _agentBridgeStatusContainer.Append<Label>("agentBridgeStatusLabel");
            _agentBridgeStatusLabel.LayoutStyle = Style.Load<LayoutStyle>("StatusPillLabel");
            _agentBridgeStatusLabel.TextStyle = Style.Load<TextStyle>("StatusPillText");
            _agentBridgeStatusLabel.Content = "AgentBridge: Disconnected";

            UpdateAgentBridgeStatusPill(_conversationManager?.CurrentConnectionStatus ?? ConversationManager.ConnectionStatus.Disconnected);
        }

        private void CreateMcpBridgeStatusPill()
        {
            _mcpBridgeStatusContainer = _leftStatusContainer.Append<Flex>("mcpBridgeStatusContainer");
            _mcpBridgeStatusContainer.LayoutStyle = Style.Load<LayoutStyle>("StatusPillContainer");

            _mcpBridgeStatusPill = _mcpBridgeStatusContainer.Append<Toggle>("mcpBridgeStatusPill");
            _mcpBridgeStatusPill.LayoutStyle = Style.Load<LayoutStyle>("StatusPill");

            _mcpBridgeStatusLabel = _mcpBridgeStatusContainer.Append<Label>("mcpBridgeStatusLabel");
            _mcpBridgeStatusLabel.LayoutStyle = Style.Load<LayoutStyle>("StatusPillLabel");
            _mcpBridgeStatusLabel.TextStyle = Style.Load<TextStyle>("StatusPillText");
            _mcpBridgeStatusLabel.Content = "MCPBridge: Disconnected";

            UpdateMcpBridgeStatusPill(_controller?.McpIntegration?.IsConnected ?? false);
        }

        private void CreateVoiceStatusPill()
        {
            _voiceStatusContainer = _leftStatusContainer.Append<Flex>("voiceStatusContainer");
            _voiceStatusContainer.LayoutStyle = Style.Load<LayoutStyle>("StatusPillContainer");

            _voiceStatusPill = _voiceStatusContainer.Append<Toggle>("voiceStatusPill");
            _voiceStatusPill.LayoutStyle = Style.Load<LayoutStyle>("StatusPill");

            _voiceStatusLabel = _voiceStatusContainer.Append<Label>("voiceStatusLabel");
            _voiceStatusLabel.LayoutStyle = Style.Load<LayoutStyle>("StatusPillLabel");
            _voiceStatusLabel.TextStyle = Style.Load<TextStyle>("StatusPillText");
            _voiceStatusLabel.Content = "Voice: Waiting";

            UpdateVoiceStatusPill(_conversationManager?.CurrentVoiceStatus ?? ConversationManager.VoiceStatus.Waiting);
        }

        private void UpdateAgentBridgeStatusPill(ConversationManager.ConnectionStatus status)
        {
            if (_agentBridgeStatusPill == null || _agentBridgeStatusLabel == null) return;

            var isConnected = status == ConversationManager.ConnectionStatus.Connected;
            var pillStyle = Style.Load<ImageStyle>("StatusPill");

            _agentBridgeStatusPill.IconStyle = pillStyle;
            _agentBridgeStatusPill.Icon = pillStyle.icon;
            _agentBridgeStatusPill.State = isConnected;

            _agentBridgeStatusLabel.Content = $"AgentBridge: {status}";
        }

        private void UpdateMcpBridgeStatusPill(bool isConnected)
        {
            if (_mcpBridgeStatusPill == null || _mcpBridgeStatusLabel == null) return;

            var pillStyle = Style.Load<ImageStyle>("StatusPill");

            _mcpBridgeStatusPill.IconStyle = pillStyle;
            _mcpBridgeStatusPill.Icon = pillStyle.icon;
            _mcpBridgeStatusPill.State = isConnected;

            _mcpBridgeStatusLabel.Content = $"MCPBridge: {(isConnected ? "Connected" : "Disconnected")}";
        }

        private void UpdateVoiceStatusPill(ConversationManager.VoiceStatus status)
        {
            if (_voiceStatusPill == null || _voiceStatusLabel == null) return;

            var isActive = status == ConversationManager.VoiceStatus.Listening;
            var pillStyle = Style.Load<ImageStyle>("VoicePill");

            _voiceStatusPill.IconStyle = pillStyle;
            _voiceStatusPill.Icon = pillStyle.icon;
            _voiceStatusPill.State = isActive;

            _voiceStatusLabel.Content = $"Voice: {status}";
        }

        #endregion

        #region Button Callbacks

        private void OnClearButtonClicked()
        {
            _conversationManager.ClearConversation();
        }

        private void OnAutoScrollToggleClicked()
        {
            _autoScrollEnabled = !_autoScrollEnabled;

            if (_autoScrollToggle != null)
            {
                _autoScrollToggle.State = _autoScrollEnabled;
            }

            if (_autoScrollEnabled)
            {
                ScrollToBottom();
            }
        }

        private void OnCancelButtonClicked()
        {
            _conversationManager.CancelConversation();
        }

        #endregion

        #region Message Detail Panel

        /// <summary>
        /// Handle conversation line click to show full message details
        /// </summary>
        /// <param name="entry">The conversation entry that was clicked</param>
        private void OnConversationLineClicked(ConversationEntry entry)
        {
            ShowMessageDetailsPanel();

            _messageDetailLabel.Content = entry.FullMessage;
            _messageDetailLabel.SetHeight(_messageDetailLabel.Text.preferredHeight + 20);
            _messageDetailLabel.RefreshLayout();

            _scrollViewMessageDetails.Progress = 1.0f;
        }

        /// <summary>
        /// Show the message details panel
        /// </summary>
        private void ShowMessageDetailsPanel()
        {
            if (_scrollViewMessageDetails.Visibility) return;

            _scrollViewMessageDetails.Show();
            _messageDetailPaneCloseBtn.Show();
            _messageDetailPaneBackground.Show();
            _conversationScrollView.LayoutStyle.bottomRightMargin.y = ContractedConversationPanelBottomMargin;
            _conversationScrollView.RefreshLayout();
        }

        /// <summary>
        /// Hide the message details panel
        /// </summary>
        private void HideMessageDetailsPanel()
        {
            if (!_scrollViewMessageDetails.Visibility) return;

            _scrollViewMessageDetails.Hide();
            _messageDetailPaneCloseBtn.Hide();
            _messageDetailPaneBackground.Hide();
            _conversationScrollView.LayoutStyle.bottomRightMargin.y = FullConversationPanelBottomMargin;
            _conversationScrollView.RefreshLayout();
        }

        /// <summary>
        /// Handle transparency changes for the details panel background
        /// </summary>
        protected override void OnTransparencyChanged()
        {
            base.OnTransparencyChanged();
            if (_messageDetailPaneBackground != null && _messageDetailPaneBackgroundImageStyle != null)
            {
                _messageDetailPaneBackground.Color = Transparent ? _messageDetailPaneBackgroundImageStyle.colorOff : _messageDetailPaneBackgroundImageStyle.color;
            }
        }

        #endregion
    }
}
