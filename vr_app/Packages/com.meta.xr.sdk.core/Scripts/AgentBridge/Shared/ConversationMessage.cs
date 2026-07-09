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

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Represents a single message in the conversation history.
    /// </summary>
    [Serializable]
    public class ConversationMessage
    {
        /// <summary>
        /// Unique identifier for this message (used for streaming updates).
        /// For streaming messages, this is the message_id from Claude.
        /// For other messages, it's auto-generated.
        /// </summary>
        public string MessageId = string.Empty;

        /// <summary>
        /// Type of message: "thinking", "tool_use", "tool_result", "assistant", etc.
        /// </summary>
        public string MessageType = string.Empty;

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Content = string.Empty;

        /// <summary>
        /// Name of the tool being called (for tool_use messages).
        /// </summary>
        public string ToolName = string.Empty;

        /// <summary>
        /// HTTP method for tool calls (e.g., "GET", "POST").
        /// </summary>
        public string Method = string.Empty;

        /// <summary>
        /// Target/endpoint for the tool call.
        /// </summary>
        public string Target = string.Empty;

        /// <summary>
        /// Rationale or reason for the action.
        /// </summary>
        public string Rationale = string.Empty;

        /// <summary>
        /// Unix timestamp when the message was created.
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// Whether this is a streaming message that can be updated.
        /// </summary>
        public bool IsStreaming = false;

        /// <summary>
        /// Whether the content is a delta (append) or full text (replace).
        /// Claude Code sends deltas, while Devmate sends full accumulated text.
        /// </summary>
        public bool IsDelta = false;

        /// <summary>
        /// Identifier for the caller that initiated this message's conversation.
        /// Used for per-caller message filtering in multi-client scenarios (e.g., multiple Quest headsets).
        /// Callers must set this explicitly via CallerIdentity.
        /// </summary>
        public string CallerId = string.Empty;
    }
}
