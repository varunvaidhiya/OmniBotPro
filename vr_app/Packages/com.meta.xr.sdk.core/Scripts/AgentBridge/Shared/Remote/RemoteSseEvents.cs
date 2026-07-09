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
    /// Connection confirmation for SSE "connected" events.
    /// Sent to clients when they first connect to the SSE stream.
    /// </summary>
    [Serializable]
    public class RemoteSseConnected
    {
        /// <summary>
        /// Unique identifier assigned to this SSE client connection.
        /// </summary>
        public string ClientId = string.Empty;
    }

    /// <summary>
    /// Keepalive ping for SSE "ping" events.
    /// Sent periodically to keep the SSE connection alive.
    /// </summary>
    [Serializable]
    public class RemoteSsePing
    {
        /// <summary>
        /// Unix timestamp when the ping was sent.
        /// </summary>
        public long Timestamp;
    }

    /// <summary>
    /// Conversation cleared notification for SSE "cleared" events.
    /// Sent when the conversation is cleared on the server.
    /// </summary>
    [Serializable]
    public class RemoteSseCleared
    {
        /// <summary>
        /// Unix timestamp when the conversation was cleared.
        /// </summary>
        public long Timestamp;
    }

    /// <summary>
    /// HTTP error response for 4xx/5xx status codes.
    /// Used for error responses that aren't operation results (e.g., 404, 500).
    /// </summary>
    [Serializable]
    public class RemoteErrorResponse
    {
        /// <summary>
        /// Error message describing what went wrong.
        /// </summary>
        public string Error = string.Empty;

        /// <summary>
        /// Optional path that was requested (for 404 errors).
        /// </summary>
        public string? Path;
    }

    /// <summary>
    /// SSE event payload for error notifications.
    /// Sent when an error occurs during AI processing that the client should be aware of.
    /// </summary>
    [Serializable]
    public class RemoteSseError
    {
        /// <summary>
        /// Error code categorizing the error type (e.g., "SERVICE_ERROR", "TIMEOUT", "CANCELLED", "EXCEPTION").
        /// </summary>
        public string Code = "ERROR";

        /// <summary>
        /// Human-readable error message describing what went wrong.
        /// </summary>
        public string Message = string.Empty;

        /// <summary>
        /// Unix timestamp when the error occurred.
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// CallerId associated with this error, for per-caller filtering.
        /// </summary>
        public string CallerId = string.Empty;
    }
}
