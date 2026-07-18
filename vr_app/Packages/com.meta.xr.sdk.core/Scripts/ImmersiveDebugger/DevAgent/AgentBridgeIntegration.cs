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
using System;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Integration MonoBehaviour that connects <see cref="RemoteAgentBridgeClient"/>
    /// with the <see cref="ConversationManager"/>. Routes voice input and conversation
    /// events through AgentBridge's HTTP remote client.
    ///
    /// Initialized by <see cref="DevAgentController"/> which provides the
    /// <see cref="ConversationManager"/> reference and optional injected client.
    /// </summary>
    internal class AgentBridgeIntegration : MonoBehaviour
    {
        private const string CallerId = "ImmersiveDebugger.DevAgent";

        private const string DefaultSystemPrompt =
            "You are a Unity VR debugging assistant running on Meta Quest. " +
            "You help users inspect, debug, and understand their Unity VR scene.\n\n" +
            "You have access to MCP tools for finding and inspecting GameObjects, " +
            "reading diagnostics, drawing debug visualizations, and more. " +
            "Use the tools/list to see available tools, and ToolHelp.GetToolHelp() " +
            "to learn how to use a specific tool.";

        private ConversationManager _manager;
        internal ConversationManager ConversationManager => _manager;

        private IRemoteAgentBridgeClient _client;
        internal IRemoteAgentBridgeClient Client => _client;

#if HAS_META_VOICE_SDK
        private DictationController _dictationController;
        private VoiceSetupController _voiceSetupController;
        private PushToTalkController _pushToTalkController;
        private bool _isLiveTranscriptionActive;
#endif

        // Thinking stream state - tracks current streaming entry by MessageId
        private ConversationEntry _currentThinkingEntry;
        private string _currentThinkingMessageId = "";
        private string _accumulatedThinkingContent = "";

        #region Initialization

        /// <summary>
        /// Initialize the integration with a ConversationManager and optional client.
        /// Called by <see cref="DevAgentController"/> during its Awake().
        /// </summary>
        internal void Initialize(ConversationManager manager, IRemoteAgentBridgeClient client = null)
        {
            _manager = manager;
            _client = client;
        }

        /// <summary>
        /// Set the client for testing. Must be called before Start() runs.
        /// </summary>
        internal void SetClient(IRemoteAgentBridgeClient client)
        {
            _client = client;
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
#if HAS_META_VOICE_SDK
            _dictationController = FindFirstObjectByType<DictationController>();

            var settings = RuntimeSettings.Instance;
            var witConfig = settings != null ? settings.WitConfiguration as Meta.WitAi.Data.Configuration.WitConfiguration : null;
            _voiceSetupController = VoiceSetupController.CreateVoiceSetupAsChild(gameObject, witConfig);
            if (_voiceSetupController != null)
            {
                _voiceSetupController.OnDictationControllerReady += OnDictationControllerReady;
            }
#else
            Debug.LogWarning("[AgentBridgeIntegration] Voice SDK (com.meta.xr.sdk.voice) is not installed. " +
                "Voice input is disabled. Install the Voice SDK via Package Manager to enable push-to-talk.");
#endif
        }

        private async void Start()
        {
            // Subscribe to conversation manager events
            if (_manager != null)
            {
                _manager.OnConversationHistoryCleared += OnConversationHistoryCleared;
                _manager.OnConversationCancelled += OnConversationCancelled;
            }

#if HAS_META_VOICE_SDK
            SetupPushToTalk();
#endif

            // Create and connect the AgentBridge remote client (if not already injected)
            if (_client == null)
            {
                var settings = RuntimeSettings.Instance;
                string serverAddress = settings != null ? settings.ServerAddress : "127.0.0.1";
                int serverPort = settings != null ? settings.ServerPort : 48735;
                _client = new RemoteAgentBridgeClient(serverAddress, serverPort);

                // Set access token for authentication
                if (settings != null && !string.IsNullOrEmpty(settings.AccessToken))
                {
                    _client.AccessToken = settings.AccessToken;
                }
            }
            _client.OnMessageReceived += OnMessageReceived;
            _client.OnProcessingStateChanged += OnProcessingStateChanged;
            _client.OnConversationCleared += OnRemoteConversationCleared;
            _client.OnConnectionStateChanged += OnConnectionStateChanged;
            _client.OnErrorReceived += OnErrorReceived;

            // Set initial status
            _manager?.SetConnectionStatus(ConversationManager.ConnectionStatus.Disconnected);

            var connected = await _client.ConnectAsync();
            if (!connected)
            {
                Debug.LogWarning("[AgentBridgeIntegration] Could not connect to Remote Agent Server. " +
                                 $"Ensure the server is running at {_client.ServerUrl}:{_client.Port}");
            }
        }

        private void OnDestroy()
        {
            if (_client != null)
            {
                _client.OnMessageReceived -= OnMessageReceived;
                _client.OnProcessingStateChanged -= OnProcessingStateChanged;
                _client.OnConversationCleared -= OnRemoteConversationCleared;
                _client.OnConnectionStateChanged -= OnConnectionStateChanged;
                _client.OnErrorReceived -= OnErrorReceived;
                _client.Dispose();
                _client = null;
            }

#if HAS_META_VOICE_SDK
            if (_pushToTalkController != null)
            {
                _pushToTalkController.OnButtonPressed -= OnInputButtonPressed;
                _pushToTalkController.OnButtonReleased -= OnInputButtonReleased;
            }
#endif

            if (_manager != null)
            {
                _manager.OnConversationHistoryCleared -= OnConversationHistoryCleared;
                _manager.OnConversationCancelled -= OnConversationCancelled;
            }

#if HAS_META_VOICE_SDK
            if (_dictationController != null)
            {
                _dictationController.OnPartialTranscriptionUpdate -= OnPartialTranscriptionUpdate;
                _dictationController.OnTranscriptionFinalized -= OnTranscriptionFinalized;
            }

            if (_voiceSetupController != null)
            {
                _voiceSetupController.OnDictationControllerReady -= OnDictationControllerReady;
            }
#endif
        }

        internal void OnApplicationPause(bool pauseStatus)
        {
            if (_client == null) return;

            if (pauseStatus)
            {
                // Headset doffed — disconnect to release resources and stop SSE stream
                Debug.Log("[AgentBridgeIntegration] Application paused, disconnecting from server");
                _client.Disconnect();
            }
            else
            {
                // Headset donned — reconnect to server
                Debug.Log("[AgentBridgeIntegration] Application resumed, reconnecting to server");
                _ = ReconnectAsync();
            }
        }

        private async System.Threading.Tasks.Task ReconnectAsync()
        {
            if (_client == null || _client.IsConnected) return;

            var connected = await _client.ConnectAsync();
            if (!connected)
            {
                Debug.LogWarning("[AgentBridgeIntegration] Failed to reconnect after resume");
            }
        }

        #endregion

        #region Connection State

        private void OnConnectionStateChanged(bool connected)
        {
            if (_manager == null) return;

            var status = connected
                ? ConversationManager.ConnectionStatus.Connected
                : ConversationManager.ConnectionStatus.Disconnected;

            _manager.SetConnectionStatus(status);
        }

        private void OnErrorReceived(RemoteSseError error)
        {
            if (_manager == null) return;

            Debug.LogError($"[AgentBridgeIntegration] Server error: [{error.Code}] {error.Message}");
            _manager.AddSystemMessage($"[Error: {error.Message}]");
            _manager.StopProcessing();
        }

        #endregion

        #region Message Handling (from RemoteAgentBridgeClient)

        private void OnMessageReceived(ConversationMessage message)
        {
            if (_manager == null) return;

            switch (message.MessageType?.ToLower())
            {
                case "thinking":
                    HandleThinkingMessage(message);
                    break;
                case "tool_use":
                    FinalizeThinkingStream();
                    _manager.AddToolCallMessage(message.Content);
                    break;
                case "tool_result":
                    _manager.UpdateLastToolStatus(message.Content);
                    break;
                case "assistant":
                    if (message.IsStreaming)
                    {
                        HandleThinkingMessage(message);
                    }
                    else
                    {
                        // Only add a new message if there was no active stream to finalize.
                        // If we just finalized a stream, it already contains the accumulated content.
                        if (!FinalizeThinkingStream())
                        {
                            _manager.AddAssistantMessage(message.Content);
                        }
                    }
                    break;
                case "user":
                    // Ignore user messages from the server - they were already added locally
                    // before sending the prompt. Adding them again would cause duplicates.
                    break;
                default:
                    _manager.AddAssistantMessage(message.Content);
                    break;
            }
        }

        private void OnProcessingStateChanged(bool isProcessing)
        {
            if (isProcessing)
            {
                _manager?.StartProcessing();
            }
            else
            {
                FinalizeThinkingStream();
                _manager?.StopProcessing();
            }
        }

        private void OnRemoteConversationCleared()
        {
            // Server-side clear acknowledged — no additional action needed
        }

        /// <summary>
        /// Handle streaming thinking messages by properly tracking MessageId.
        /// When IsDelta is true, content is appended to accumulated content.
        /// When IsDelta is false, content replaces the accumulated content.
        /// </summary>
        private void HandleThinkingMessage(ConversationMessage message)
        {
            try
            {
                var messageId = message.MessageId ?? "";
                var content = message.Content ?? "";

                // Check if this is a new message stream (different MessageId)
                if (_currentThinkingEntry == null || _currentThinkingMessageId != messageId)
                {
                    // Finalize any existing stream first
                    FinalizeThinkingStream();

                    // Start a new streaming entry
                    _currentThinkingEntry = _manager.AddLiveTranscriptionEntry(ConversationEntry.MessageType.Assistant);
                    _currentThinkingMessageId = messageId;
                    _accumulatedThinkingContent = "";
                }

                // Update content based on IsDelta flag
                if (message.IsDelta)
                {
                    // Delta mode: APPEND content (incremental chunks)
                    _accumulatedThinkingContent += content;
                }
                else
                {
                    // Full text mode: REPLACE content (server sends full accumulated text)
                    _accumulatedThinkingContent = content;
                }

                _manager.UpdateLiveTranscriptionEntry(_accumulatedThinkingContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Error handling thinking message: {ex.Message}");
            }
        }

        private bool FinalizeThinkingStream()
        {
            if (_currentThinkingEntry != null)
            {
                _manager?.FinalizeLiveTranscriptionEntry(_accumulatedThinkingContent);
                _currentThinkingEntry = null;
                _currentThinkingMessageId = "";
                _accumulatedThinkingContent = "";
                return true;
            }
            return false;
        }

        #endregion

        #region Push-to-Talk / Voice

#if HAS_META_VOICE_SDK
        private void SetupPushToTalk()
        {
            _pushToTalkController = FindFirstObjectByType<PushToTalkController>();
            if (_pushToTalkController == null)
            {
                _pushToTalkController = gameObject.AddComponent<PushToTalkController>();
            }

            var settings = RuntimeSettings.Instance;
            if (settings != null)
            {
                _pushToTalkController.InputButton = settings.PushToTalkButton;
                _pushToTalkController.HandGesture = settings.HandPushToTalkGesture;
            }

            _pushToTalkController.OnButtonPressed += OnInputButtonPressed;
            _pushToTalkController.OnButtonReleased += OnInputButtonReleased;
        }

        private void OnDictationControllerReady(DictationController dictationController)
        {
            _dictationController = dictationController;
            _dictationController.OnPartialTranscriptionUpdate += OnPartialTranscriptionUpdate;
            _dictationController.OnTranscriptionFinalized += OnTranscriptionFinalized;
        }

        private void OnInputButtonPressed()
        {
            if (_manager != null && !_isLiveTranscriptionActive)
            {
                _manager.AddLiveTranscriptionEntry();
                _isLiveTranscriptionActive = true;
            }

            if (_dictationController != null)
            {
                _dictationController.Toggle(true);
            }
            else
            {
                Debug.LogWarning("[AgentBridgeIntegration] Cannot start dictation - DictationController is not ready yet.");
            }

            _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Listening);
        }

        private void OnInputButtonReleased()
        {
            _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Processing);

            if (_dictationController != null)
            {
                _dictationController.Toggle(false);
            }
            else
            {
                Debug.LogWarning("[AgentBridgeIntegration] Cannot stop dictation - DictationController is not ready yet.");
            }
        }

        private void OnPartialTranscriptionUpdate(string partialText)
        {
            if (_manager != null && _isLiveTranscriptionActive)
            {
                _manager.UpdateLiveTranscriptionEntry(partialText);
            }
        }

        private void OnTranscriptionFinalized(string finalText)
        {
            if (_manager == null || !_isLiveTranscriptionActive) return;

            _manager.FinalizeLiveTranscriptionEntry(finalText);
            _isLiveTranscriptionActive = false;

            if (!string.IsNullOrEmpty(finalText) && finalText.Trim().Length > 0)
            {
                ProcessTranscription(finalText);
            }
            else
            {
                _manager.SetVoiceStatus(ConversationManager.VoiceStatus.Waiting);
            }
        }
#endif

        #endregion

        #region Send to AgentBridge

#if UNITY_EDITOR
        /// <summary>
        /// Sends a text message directly to AgentBridge, bypassing voice/PTT.
        /// This is intended for Editor testing only.
        /// </summary>
        /// <param name="message">The text message to send</param>
        internal void SendTextMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("[AgentBridgeIntegration] Cannot send empty message");
                return;
            }

            _manager?.AddUserMessage(message);
            ProcessTranscription(message);
        }
#endif

        internal async void ProcessTranscription(string transcription)
        {
            try
            {
                if (string.IsNullOrEmpty(transcription))
                {
                    Debug.LogWarning("[AgentBridgeIntegration] Cannot process empty transcription");
                    _manager?.StopProcessing();
                    return;
                }

                if (_client == null || !_client.IsConnected)
                {
                    Debug.LogError("[AgentBridgeIntegration] Not connected to Remote Agent Server");
                    _manager?.AddSystemMessage("[Error: Not connected to AgentBridge server]");
                    _manager?.StopProcessing();
                    return;
                }

                var request = new RemotePromptRequest
                {
                    Prompt = transcription,
                    CallerId = CallerId,
                    SystemPrompt = DefaultSystemPrompt
                };

                var (success, error) = await _client.SendPromptAsync(request);
                if (!success)
                {
                    Debug.LogError($"[AgentBridgeIntegration] Failed to send prompt: {error}");
                    _manager?.AddSystemMessage($"[Error: {error}]");
                    _manager?.StopProcessing();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Error in ProcessTranscription: {ex.Message}");
            }
            finally
            {
                _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Waiting);
            }
        }

        private async void OnConversationHistoryCleared()
        {
            try
            {
                if (_client == null || !_client.IsConnected)
                {
                    Debug.LogError("[AgentBridgeIntegration] Not connected to Remote Agent Server");
                    return;
                }

                var request = new RemoteCallerRequest { CallerId = CallerId };
                var (success, error) = await _client.ClearConversationAsync(request);
                if (!success)
                {
                    Debug.LogError($"[AgentBridgeIntegration] Failed to clear conversation: {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Failed to clear conversation: {ex.Message}");
            }
        }

        internal async void OnConversationCancelled()
        {
            try
            {
                if (_client == null || !_client.IsConnected)
                {
                    Debug.LogError("[AgentBridgeIntegration] Not connected to Remote Agent Server");
                    return;
                }

                Debug.Log("[AgentBridgeIntegration] Sending cancellation request");
                var request = new RemoteCallerRequest { CallerId = CallerId };
                var (success, error) = await _client.CancelAsync(request);

                _manager?.AddSystemMessage(
                    success ? "[Interrupted by user]" : $"[Error: Failed to cancel - {error}]");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Failed to send cancellation: {ex.Message}");
                _manager?.AddSystemMessage($"[Error: Cancellation failed - {ex.Message}]");
            }
        }

        #endregion
    }
}
