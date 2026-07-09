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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Manages conversation state and logic for the LLM Dialog.
    /// This MonoBehaviour handles all conversation entries, status tracking, and events
    /// without any UI dependencies, making it testable independently of ImmersiveDebugger.
    /// </summary>
    internal class ConversationManager : MonoBehaviour
    {
        private const int MaxConversationEntries = 50;

        // Status enums
        internal enum ConnectionStatus { Disconnected, Connected }
        internal enum VoiceStatus { Waiting, Listening, Processing }

        // Events for UI to subscribe to
        internal event Action<ConversationEntry> OnEntryAdded;
        internal event Action<ConversationEntry> OnEntryUpdated;
        internal event Action<ConversationEntry> OnEntryRemoved;
        internal event Action OnEntriesCleared;
        internal event Action<ConnectionStatus> OnConnectionStatusChanged;
        internal event Action<VoiceStatus> OnVoiceStatusChanged;
        internal event Action<bool> OnConversationActiveChanged;
        internal event Action OnConversationHistoryCleared;
        internal event Action OnConversationCancelled;

        // Conversation entries - accessible for UI rendering
        private readonly List<ConversationEntry> _entries = new();
        internal IReadOnlyList<ConversationEntry> Entries => _entries;

        // Live transcription tracking
        private ConversationEntry _liveTranscriptionEntry;

        // Last tool entry tracking (for updating status on tool_result)
        private ConversationEntry _lastToolEntry;

        // Status states
        private ConnectionStatus _currentConnectionStatus = ConnectionStatus.Disconnected;
        internal ConnectionStatus CurrentConnectionStatus => _currentConnectionStatus;

        private VoiceStatus _currentVoiceStatus = VoiceStatus.Waiting;
        internal VoiceStatus CurrentVoiceStatus => _currentVoiceStatus;

        // Conversation state tracking
        private bool _isConversationActive;
        internal bool IsConversationActive
        {
            get => _isConversationActive;
            set
            {
                if (_isConversationActive == value) return;
                _isConversationActive = value;
                OnConversationActiveChanged?.Invoke(_isConversationActive);
            }
        }

        #region Public API - Entry Management

        /// <summary>
        /// Start a live transcription entry that can be updated with partial transcription.
        /// </summary>
        /// <returns>The conversation entry for live transcription updates</returns>
        internal ConversationEntry AddLiveTranscriptionEntry()
        {
            return AddLiveTranscriptionEntry(ConversationEntry.MessageType.User);
        }

        /// <summary>
        /// Start a live transcription entry with specified message type.
        /// </summary>
        /// <param name="messageType">The type of message</param>
        /// <returns>The conversation entry for live transcription updates</returns>
        internal ConversationEntry AddLiveTranscriptionEntry(ConversationEntry.MessageType messageType)
        {
            EnforceMaxEntries();

            var entry = CreateEntry("", messageType);
            entry.SetLiveTranscription(true);
            _liveTranscriptionEntry = entry;

            OnEntryAdded?.Invoke(entry);
            return entry;
        }

        /// <summary>
        /// Update the live transcription entry with partial transcription text.
        /// </summary>
        /// <param name="partialText">The partial transcription text to display</param>
        internal void UpdateLiveTranscriptionEntry(string partialText)
        {
            if (_liveTranscriptionEntry != null)
            {
                _liveTranscriptionEntry.UpdateMessage(partialText);
                OnEntryUpdated?.Invoke(_liveTranscriptionEntry);
            }
        }

        /// <summary>
        /// Finalize the live transcription entry with the complete transcription.
        /// </summary>
        /// <param name="finalText">The final transcription text</param>
        internal void FinalizeLiveTranscriptionEntry(string finalText)
        {
            if (_liveTranscriptionEntry != null)
            {
                _liveTranscriptionEntry.UpdateMessage(finalText);
                _liveTranscriptionEntry.SetLiveTranscription(false);
                OnEntryUpdated?.Invoke(_liveTranscriptionEntry);
                _liveTranscriptionEntry = null;

                StartProcessing();
            }
        }

        /// <summary>
        /// Add a user message to the conversation.
        /// </summary>
        /// <param name="message">The user's message</param>
        internal ConversationEntry AddUserMessage(string message)
        {
            var entry = AddConversationEntry(message, ConversationEntry.MessageType.User);
            StartProcessing();
            return entry;
        }

        /// <summary>
        /// Add an assistant message to the conversation.
        /// </summary>
        /// <param name="message">The assistant's message</param>
        internal ConversationEntry AddAssistantMessage(string message)
        {
            return AddConversationEntry(message, ConversationEntry.MessageType.Assistant);
        }

        /// <summary>
        /// Add a system message to the conversation.
        /// </summary>
        /// <param name="message">The system message</param>
        internal ConversationEntry AddSystemMessage(string message)
        {
            return AddConversationEntry(message, ConversationEntry.MessageType.System);
        }

        /// <summary>
        /// Add a tool call message to the conversation.
        /// </summary>
        /// <param name="toolCallsInfo">Tool calls information to display</param>
        internal ConversationEntry AddToolCallMessage(string toolCallsInfo)
        {
            _lastToolEntry = AddConversationEntry(toolCallsInfo, ConversationEntry.MessageType.Tool);
            return _lastToolEntry;
        }

        /// <summary>
        /// Update the last tool entry status (for tool_result handling).
        /// </summary>
        /// <param name="resultContent">The tool result content to check for errors</param>
        internal void UpdateLastToolStatus(string resultContent)
        {
            if (_lastToolEntry == null) return;

            var isFailure = DetectToolFailure(resultContent);
            var status = isFailure
                ? ConversationEntry.ToolStatus.Failure
                : ConversationEntry.ToolStatus.Success;

            _lastToolEntry.UpdateToolStatus(status);
            OnEntryUpdated?.Invoke(_lastToolEntry);
            _lastToolEntry = null;
        }

        /// <summary>
        /// Add a complete message to signal the end of thinking/processing.
        /// </summary>
        /// <param name="message">The completion message (can be empty)</param>
        internal void AddCompleteMessage(string message = "")
        {
            StopProcessing();

            if (!string.IsNullOrEmpty(message))
            {
                AddConversationEntry(message, ConversationEntry.MessageType.Complete);
            }
        }

        /// <summary>
        /// Clear all conversation entries.
        /// </summary>
        internal void ClearConversation()
        {
            foreach (var entry in _entries)
            {
                OnEntryRemoved?.Invoke(entry);
            }
            _entries.Clear();
            _liveTranscriptionEntry = null;
            _lastToolEntry = null;

            OnEntriesCleared?.Invoke();
            OnConversationHistoryCleared?.Invoke();

            if (IsConversationActive)
            {
                OnConversationCancelled?.Invoke();
            }

            StopProcessing();
        }

        /// <summary>
        /// Cancel the current conversation processing.
        /// </summary>
        internal void CancelConversation()
        {
            if (IsConversationActive)
            {
                StopProcessing();
                OnConversationCancelled?.Invoke();
            }
        }

        #endregion

        #region Public API - Status Management

        /// <summary>
        /// Set the connection status.
        /// </summary>
        /// <param name="status">The connection status</param>
        internal void SetConnectionStatus(ConnectionStatus status)
        {
            if (_currentConnectionStatus == status) return;
            _currentConnectionStatus = status;
            OnConnectionStatusChanged?.Invoke(status);
        }

        /// <summary>
        /// Set the voice status.
        /// </summary>
        /// <param name="status">The voice status</param>
        internal void SetVoiceStatus(VoiceStatus status)
        {
            if (_currentVoiceStatus == status) return;
            _currentVoiceStatus = status;
            OnVoiceStatusChanged?.Invoke(status);
        }

        #endregion

        #region Public API - Processing State

        /// <summary>
        /// Signal that processing has started (e.g., waiting for AI response).
        /// </summary>
        internal void StartProcessing()
        {
            IsConversationActive = true;
        }

        /// <summary>
        /// Signal that processing has stopped.
        /// </summary>
        internal void StopProcessing()
        {
            IsConversationActive = false;
        }

        #endregion

        #region Private Helpers

        private ConversationEntry AddConversationEntry(string message, ConversationEntry.MessageType type)
        {
            EnforceMaxEntries();
            var entry = CreateEntry(message, type);
            OnEntryAdded?.Invoke(entry);
            return entry;
        }

        private ConversationEntry CreateEntry(string message, ConversationEntry.MessageType type)
        {
            var entry = new ConversationEntry();
            entry.Setup(message, type);
            _entries.Add(entry);
            return entry;
        }

        private void EnforceMaxEntries()
        {
            if (_entries.Count >= MaxConversationEntries)
            {
                RemoveOldestEntry();
            }
        }

        private void RemoveOldestEntry()
        {
            if (_entries.Count > 0)
            {
                var oldestEntry = _entries[0];
                _entries.RemoveAt(0);
                OnEntryRemoved?.Invoke(oldestEntry);
            }
        }

        private bool DetectToolFailure(string resultContent)
        {
            if (string.IsNullOrEmpty(resultContent)) return false;

            var lowerContent = resultContent.ToLower();

            // Look for actual failure patterns
            var isFailure = lowerContent.Contains("exception:") ||
                           lowerContent.Contains("exception occurred") ||
                           lowerContent.Contains("error:") ||
                           lowerContent.Contains("failed to") ||
                           lowerContent.Contains("could not") ||
                           lowerContent.Contains("unable to") ||
                           lowerContent.Contains("failure:") ||
                           lowerContent.Contains("not found") ||
                           (lowerContent.StartsWith("error") && !lowerContent.StartsWith("error count")) ||
                           lowerContent.StartsWith("exception");

            // Exclude false positives
            if (isFailure)
            {
                if (lowerContent.Contains("no error") ||
                    lowerContent.Contains("no errors") ||
                    lowerContent.Contains("without error") ||
                    lowerContent.Contains("without errors") ||
                    lowerContent.Contains("error count: 0") ||
                    lowerContent.Contains("0 errors") ||
                    lowerContent.Contains("successfully") ||
                    lowerContent.Contains("completed") ||
                    lowerContent.Contains("found 0 errors"))
                {
                    isFailure = false;
                }
            }

            return isFailure;
        }

        #endregion
    }
}
