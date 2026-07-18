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

using Meta.XR.ImmersiveDebugger.Utils;
using System;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Represents a conversation entry in the LLM Dialog Panel.
    /// Similar to LogEntry but for conversation messages.
    /// </summary>
    internal class ConversationEntry
    {
        internal enum MessageType
        {
            User,
            Assistant,
            System,
            Tool,
            Complete
        }

        internal enum ToolStatus
        {
            Pending,
            Success,
            Failure
        }

        public static Action<ConversationEntry> OnDisplayDetails { get; set; }

        internal string Message { get; private set; }
        internal string FullMessage { get; private set; }
        internal MessageType Type { get; private set; }
        internal ToolStatus Status { get; private set; } = ToolStatus.Pending;
        internal ConversationLine UIElement { get; set; }
        internal bool Shown => UIElement != null;
        internal bool ShouldShow => true; // Always show all entries since we're not using virtualization

        /// <summary>
        /// Indicates if this message is currently being live-transcribed
        /// </summary>
        internal bool IsLiveTranscription { get; private set; }

        private const int MaxDisplayCharacterSize = 50000; // Much higher limit for multiline conversation support

        internal void Setup(string message, MessageType type)
        {
            FullMessage = message;
            // For conversation messages, we want to show much more content directly in the line
            // Only clamp extremely long messages to maintain performance
            Message = UserInterface.Utils.ClampText(message, MaxDisplayCharacterSize);
            Type = type;
        }

        internal void DisplayDetails() => OnDisplayDetails?.Invoke(this);

        internal bool HasTruncatedContent => FullMessage.Length > MaxDisplayCharacterSize;

        /// <summary>
        /// Update the message content (for live transcription updates)
        /// </summary>
        /// <param name="newMessage">The updated message content</param>
        internal void UpdateMessage(string newMessage)
        {
            FullMessage = newMessage;
            Message = UserInterface.Utils.ClampText(newMessage, MaxDisplayCharacterSize);
        }

        /// <summary>
        /// Set whether this message is currently being live transcribed
        /// </summary>
        /// <param name="isLive">True if live transcription is active, false when finalized</param>
        internal void SetLiveTranscription(bool isLive)
        {
            IsLiveTranscription = isLive;
        }

        internal void Reset()
        {
            Message = string.Empty;
            FullMessage = string.Empty;
            Type = MessageType.System;
            UIElement = null;
        }

        /// <summary>
        /// Update the status of a tool message (for tool_result handling)
        /// </summary>
        public void UpdateToolStatus(ToolStatus newStatus)
        {
            Status = newStatus;
            // Trigger UI update if element exists
            UIElement?.UpdateDisplay();
        }
    }
}
