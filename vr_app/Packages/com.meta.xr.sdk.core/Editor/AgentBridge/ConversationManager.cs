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
using System.Collections.Generic;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Manages the conversation lifecycle, including adding messages and managing state.
    /// Events are raised through ConversationEventBroker to bridge Runtime and Editor assemblies.
    /// Supports per-caller state isolation for multi-client scenarios.
    /// </summary>
    public static class ConversationManager
    {
        /// <summary>
        /// Add a message to the conversation history or update an existing streaming message.
        /// Thread-safe: Can be called from any thread.
        /// The message's CallerId is used for per-caller state isolation.
        /// </summary>
        /// <param name="message">The message to add. Must have CallerId set at construction time.</param>
        public static void AddMessage(ConversationMessage message)
        {
            // Get the caller-specific state from the message's CallerId
            var state = ConversationPersistence.GetStateForCaller(message.CallerId);

            // If this message has an ID, try to find and update an existing message with the same ID
            // This handles streaming updates where multiple chunks have the same messageId
            if (!string.IsNullOrEmpty(message.MessageId))
            {
                var existingMessage = state.Messages.Find(m => m.MessageId == message.MessageId);

                if (existingMessage != null)
                {
                    // Update existing message
                    if (message.IsDelta)
                    {
                        // Delta mode: APPEND content (Claude Code sends incremental chunks)
                        existingMessage.Content = (existingMessage.Content ?? "") + (message.Content ?? "");
                    }
                    else
                    {
                        // Full text mode: REPLACE content (Devmate sends full accumulated text)
                        existingMessage.Content = message.Content ?? "";
                    }
                    existingMessage.IsStreaming = message.IsStreaming;
                    // Always set IsDelta = false when broadcasting because Content is now the
                    // FULL accumulated text. If we kept IsDelta = true, remote clients would
                    // try to append again, causing double accumulation.
                    existingMessage.IsDelta = false;
                    existingMessage.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    ConversationPersistence.Save();

                    // Notify subscribers through event broker
                    ConversationEventBroker.RaiseMessageAdded(existingMessage);
                    return;
                }
            }

            // Deduplication for user messages from server:
            // When the server echoes back a user message, we may have already added it locally
            // (for low-latency UI). Deduplicate by checking if there's already a user message
            // with the same content that was recently added (within the last few seconds).
            if (message.MessageType == "user" && !string.IsNullOrEmpty(message.Content))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var recentThresholdSeconds = 30; // User messages added within 30 seconds are considered duplicates

                var existingUserMessage = state.Messages.Find(m =>
                    m.MessageType == "user" &&
                    m.Content == message.Content &&
                    (now - m.Timestamp) < recentThresholdSeconds);

                if (existingUserMessage != null)
                {
                    // Already have this user message locally - update the MessageId to the server's
                    // authoritative ID (if provided) but don't add a duplicate
                    if (!string.IsNullOrEmpty(message.MessageId))
                    {
                        existingUserMessage.MessageId = message.MessageId;
                        ConversationPersistence.Save();
                    }
                    // Skip adding duplicate - server confirmation received
                    return;
                }
            }

            // If this is a final "assistant" message, convert any streaming "thinking" messages to final
            if (!message.IsStreaming && message.MessageType == "assistant" && !string.IsNullOrEmpty(message.MessageId))
            {
                // Find the most recent streaming "thinking" message and convert it
                for (int i = state.Messages.Count - 1; i >= 0; i--)
                {
                    var msg = state.Messages[i];
                    if (msg.IsStreaming && msg.MessageType == "thinking")
                    {
                        // Convert streaming message to final assistant message
                        msg.MessageType = "assistant";
                        msg.IsStreaming = false;
                        msg.MessageId = message.MessageId;
                        ConversationPersistence.Save();

                        // Notify subscribers through event broker
                        ConversationEventBroker.RaiseMessageAdded(msg);
                        return;
                    }
                }
            }

            // Otherwise, add new message
            ConversationPersistence.AddMessageForCaller(message.CallerId, message);

            // Notify subscribers through event broker
            ConversationEventBroker.RaiseMessageAdded(message);
        }

        /// <summary>
        /// Add a message with specific details to the conversation history.
        /// Every caller must provide a CallerIdentity for per-caller state isolation.
        /// </summary>
        public static void AddMessage(string messageType, string content, string toolName = "",
            string method = "", string target = "", string rationale = "",
            CallerIdentity? caller = null)
        {
            var message = new ConversationMessage
            {
                MessageType = messageType,
                Content = content,
                ToolName = toolName,
                Method = method,
                Target = target,
                Rationale = rationale,
                CallerId = caller?.Id ?? string.Empty,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            AddMessage(message);
        }

        /// <summary>
        /// Get all messages in the default conversation history.
        /// </summary>
        public static List<ConversationMessage> GetMessages()
        {
            return GetMessagesForCaller((CallerIdentity?)null);
        }

        /// <summary>
        /// Get all messages for a specific caller's conversation history.
        /// </summary>
        public static List<ConversationMessage> GetMessagesForCaller(CallerIdentity? caller)
        {
            return new List<ConversationMessage>(ConversationPersistence.GetStateForCaller(caller?.Id).Messages);
        }

        /// <summary>
        /// Get all messages for a specific caller's conversation history (by string ID).
        /// </summary>
        public static List<ConversationMessage> GetMessagesForCaller(string? callerId)
        {
            return new List<ConversationMessage>(ConversationPersistence.GetStateForCaller(callerId).Messages);
        }

        /// <summary>
        /// Get the current session ID (for default state).
        /// </summary>
        public static string GetSessionId()
        {
            return GetSessionIdForCaller((CallerIdentity?)null);
        }

        /// <summary>
        /// Get the session ID for a specific caller.
        /// </summary>
        public static string GetSessionIdForCaller(CallerIdentity? caller)
        {
            var state = ConversationPersistence.GetStateForCaller(caller?.Id);
            return state.SessionId;
        }

        /// <summary>
        /// Get the session ID for a specific caller (by string ID).
        /// </summary>
        public static string GetSessionIdForCaller(string? callerId)
        {
            var state = ConversationPersistence.GetStateForCaller(callerId);
            return state.SessionId;
        }

        /// <summary>
        /// Set the session ID for the default conversation.
        /// </summary>
        public static void SetSessionId(string sessionId)
        {
            SetSessionIdForCaller((CallerIdentity?)null, sessionId);
        }

        /// <summary>
        /// Set the session ID for a specific caller's conversation.
        /// </summary>
        public static void SetSessionIdForCaller(CallerIdentity? caller, string sessionId)
        {
            var state = ConversationPersistence.GetStateForCaller(caller?.Id);
            state.SessionId = sessionId;
            ConversationPersistence.Save();
        }

        /// <summary>
        /// Set the session ID for a specific caller's conversation (by string ID).
        /// </summary>
        public static void SetSessionIdForCaller(string? callerId, string sessionId)
        {
            var state = ConversationPersistence.GetStateForCaller(callerId);
            state.SessionId = sessionId;
            ConversationPersistence.Save();
        }

        /// <summary>
        /// Check if a request is currently active (for default state).
        /// </summary>
        public static bool IsActive
        {
            get => IsActiveForCaller((CallerIdentity?)null);
            set => SetActiveForCaller((CallerIdentity?)null, value);
        }

        /// <summary>
        /// Check if a request is currently active for a specific caller.
        /// </summary>
        public static bool IsActiveForCaller(CallerIdentity? caller)
        {
            return ConversationPersistence.GetStateForCaller(caller?.Id).IsActive;
        }

        /// <summary>
        /// Check if a request is currently active for a specific caller (by string ID).
        /// </summary>
        public static bool IsActiveForCaller(string? callerId)
        {
            return ConversationPersistence.GetStateForCaller(callerId).IsActive;
        }

        /// <summary>
        /// Set the active state for a specific caller.
        /// </summary>
        public static void SetActiveForCaller(CallerIdentity? caller, bool value)
        {
            var state = ConversationPersistence.GetStateForCaller(caller?.Id);
            var oldValue = state.IsActive;
            state.IsActive = value;
            ConversationPersistence.Save();

            // Notify subscribers if state changed through event broker
            if (oldValue != value)
            {
                ConversationEventBroker.RaiseProcessingStateChanged(value);
            }
        }

        /// <summary>
        /// Set the active state for a specific caller (by string ID).
        /// </summary>
        public static void SetActiveForCaller(string? callerId, bool value)
        {
            var state = ConversationPersistence.GetStateForCaller(callerId);
            var oldValue = state.IsActive;
            state.IsActive = value;
            ConversationPersistence.Save();

            // Notify subscribers if state changed through event broker
            if (oldValue != value)
            {
                ConversationEventBroker.RaiseProcessingStateChanged(value);
            }
        }

        /// <summary>
        /// Clear the entire default conversation state.
        /// </summary>
        public static void Clear()
        {
            ClearForCaller((CallerIdentity?)null);
        }

        /// <summary>
        /// Clear all conversation states (default and all per-caller states).
        /// </summary>
        public static void ClearAll()
        {
            ConversationPersistence.ClearAll();

            // Notify subscribers through event broker
            ConversationEventBroker.RaiseConversationCleared();
        }

        /// <summary>
        /// Clear the conversation state for a specific caller.
        /// </summary>
        public static void ClearForCaller(CallerIdentity? caller)
        {
            ConversationPersistence.ClearForCaller(caller?.Id);

            // Notify subscribers through event broker
            ConversationEventBroker.RaiseConversationCleared();
        }

        /// <summary>
        /// Clear the conversation state for a specific caller (by string ID).
        /// </summary>
        public static void ClearForCaller(string? callerId)
        {
            ConversationPersistence.ClearForCaller(callerId);

            // Notify subscribers through event broker
            ConversationEventBroker.RaiseConversationCleared();
        }

        /// <summary>
        /// Set an error message for the default conversation.
        /// </summary>
        public static void SetError(string error)
        {
            SetErrorForCaller((CallerIdentity?)null, error);
        }

        /// <summary>
        /// Set an error message for a specific caller's conversation.
        /// </summary>
        public static void SetErrorForCaller(CallerIdentity? caller, string error)
        {
            var state = ConversationPersistence.GetStateForCaller(caller?.Id);
            state.LastError = error;
            ConversationPersistence.Save();
        }

        /// <summary>
        /// Set an error message for a specific caller's conversation (by string ID).
        /// </summary>
        public static void SetErrorForCaller(string? callerId, string error)
        {
            var state = ConversationPersistence.GetStateForCaller(callerId);
            state.LastError = error;
            ConversationPersistence.Save();
        }

        /// <summary>
        /// Get the last error message if any occurred (for default state).
        /// </summary>
        public static string? GetLastError()
        {
            return GetLastErrorForCaller((CallerIdentity?)null);
        }

        /// <summary>
        /// Get the last error message for a specific caller.
        /// </summary>
        public static string? GetLastErrorForCaller(CallerIdentity? caller)
        {
            return ConversationPersistence.GetStateForCaller(caller?.Id).LastError;
        }

        /// <summary>
        /// Get the last error message for a specific caller (by string ID).
        /// </summary>
        public static string? GetLastErrorForCaller(string? callerId)
        {
            return ConversationPersistence.GetStateForCaller(callerId).LastError;
        }

        /// <summary>
        /// Clear the last error message (for default state).
        /// </summary>
        public static void ClearError()
        {
            ClearErrorForCaller((CallerIdentity?)null);
        }

        /// <summary>
        /// Clear the last error message for a specific caller.
        /// </summary>
        public static void ClearErrorForCaller(CallerIdentity? caller)
        {
            var state = ConversationPersistence.GetStateForCaller(caller?.Id);
            state.LastError = null;
            ConversationPersistence.Save();
        }

        /// <summary>
        /// Clear the last error message for a specific caller (by string ID).
        /// </summary>
        public static void ClearErrorForCaller(string? callerId)
        {
            var state = ConversationPersistence.GetStateForCaller(callerId);
            state.LastError = null;
            ConversationPersistence.Save();
        }
    }
}
