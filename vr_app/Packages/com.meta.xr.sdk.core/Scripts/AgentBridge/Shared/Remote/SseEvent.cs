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

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// The set of SSE event types exchanged between the Remote Agent Server (Editor)
    /// and Remote Agent Client (Runtime).
    /// </summary>
    public enum SseEventType
    {
        /// <summary>Sent on initial SSE handshake. Payload: <see cref="RemoteSseConnected"/>.</summary>
        Connected,

        /// <summary>Keepalive ping. Payload: <see cref="RemoteSsePing"/>.</summary>
        Ping,

        /// <summary>A conversation message was added. Payload: <see cref="ConversationMessage"/>.</summary>
        Message,

        /// <summary>Processing state changed (started/stopped). Payload: <see cref="RemoteProcessingState"/>.</summary>
        Status,

        /// <summary>Conversation was cleared. Payload: <see cref="RemoteSseCleared"/>.</summary>
        Cleared,

        /// <summary>An error occurred during processing. Payload: <see cref="RemoteSseError"/>.</summary>
        Error,

        /// <summary>Unrecognized event type received on the wire.</summary>
        Unknown
    }

    /// <summary>
    /// A parsed SSE event. Shared contract between server and client.
    /// </summary>
    public struct SseEvent
    {
        /// <summary>The event type.</summary>
        public SseEventType EventType;

        /// <summary>The event data payload (JSON string).</summary>
        public string Data;

        /// <summary>Wire format name for this event type (e.g. "message", "status").</summary>
        public string WireEventName => ToWireName(EventType);

        /// <summary>Convert an <see cref="SseEventType"/> to its SSE wire format string.</summary>
        public static string ToWireName(SseEventType eventType) => eventType switch
        {
            SseEventType.Connected => "connected",
            SseEventType.Ping => "ping",
            SseEventType.Message => "message",
            SseEventType.Status => "status",
            SseEventType.Cleared => "cleared",
            SseEventType.Error => "error",
            _ => "unknown"
        };

        /// <summary>Parse an SSE wire format string into an <see cref="SseEventType"/>.</summary>
        public static SseEventType ParseWireName(string wireName) => wireName switch
        {
            "connected" => SseEventType.Connected,
            "ping" => SseEventType.Ping,
            "message" => SseEventType.Message,
            "status" => SseEventType.Status,
            "cleared" => SseEventType.Cleared,
            "error" => SseEventType.Error,
            _ => SseEventType.Unknown
        };
    }
}
