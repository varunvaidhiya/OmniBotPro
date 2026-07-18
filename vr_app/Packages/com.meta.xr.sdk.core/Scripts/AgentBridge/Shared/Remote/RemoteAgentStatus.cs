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
    /// Status information for the Remote Agent Server's /status endpoint.
    /// Shared between server (serialization) and client (deserialization).
    /// </summary>
    [Serializable]
    public class RemoteAgentStatus
    {
        /// <summary>
        /// Whether the server is currently processing an AI request.
        /// </summary>
        public bool IsProcessing;

        /// <summary>
        /// Whether there is an active conversation session on the server.
        /// </summary>
        public bool HasActiveSession;

        /// <summary>
        /// Name of the currently active AI service (e.g., "Claude Code", "Devmate").
        /// </summary>
        public string ServiceName = string.Empty;

        /// <summary>
        /// The last error message from the server, if any. Null when no error.
        /// </summary>
        public string? LastError;

        /// <summary>
        /// Number of SSE clients currently connected to the server.
        /// </summary>
        public int ConnectedClients;
    }
}
