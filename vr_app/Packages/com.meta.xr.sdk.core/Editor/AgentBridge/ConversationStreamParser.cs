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

using Newtonsoft.Json.Linq;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Shared types for parsing conversation content from AI service stream output.
    /// Service-specific parsing logic is co-located with each service:
    /// - ClaudeCodeService: GetConversationContent (for Claude Code CLI)
    /// - DevmateService: GetConversationContentFromJson (for Devmate Unity Bridge)
    /// - AcpParser: GetConversationContentFromAcp (for Gemini/ACP)
    /// </summary>
    public static class ConversationStreamParser
    {
        /// <summary>
        /// Result of conversation content detection.
        /// Used by all AI services to represent parsed message content.
        /// </summary>
        public class ConversationInfo
        {
            public bool HasContent { get; set; }
            public string Content { get; set; } = "";
            public string MessageType { get; set; } = "";
            public string? ToolName { get; set; }
            public string? Method { get; set; }
            public string? Target { get; set; }
            public string? Rationale { get; set; }
            public JObject? AdditionalData { get; set; }
            public string? MessageId { get; set; }
            public bool IsStreaming { get; set; }
            /// <summary>
            /// If true, Content is a delta (append to existing). If false, Content is the full text (replace).
            /// Claude Code CLI sends deltas, while Devmate Unity Bridge sends full accumulated text.
            /// </summary>
            public bool IsDelta { get; set; }
        }
    }
}
