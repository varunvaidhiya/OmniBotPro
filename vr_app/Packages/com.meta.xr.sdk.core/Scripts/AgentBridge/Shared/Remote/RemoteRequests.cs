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
    /// Request body for the POST /prompt endpoint.
    /// Sent by the client to request AI inference.
    /// </summary>
    [Serializable]
    public class RemotePromptRequest
    {
        /// <summary>The text prompt to send to the AI service.</summary>
        public string Prompt = string.Empty;

        /// <summary>Identifier for the caller (e.g., "ImmersiveDebugger.DevAgent").</summary>
        public string CallerId = "RemoteClient";

        /// <summary>Optional system prompt for additional context.</summary>
        public string? SystemPrompt;
    }

    /// <summary>
    /// Request body for the POST /cancel and POST /clear endpoints.
    /// Identifies which caller is making the request.
    /// </summary>
    [Serializable]
    public class RemoteCallerRequest
    {
        /// <summary>Identifier for the caller.</summary>
        public string CallerId = "RemoteClient";
    }
}
