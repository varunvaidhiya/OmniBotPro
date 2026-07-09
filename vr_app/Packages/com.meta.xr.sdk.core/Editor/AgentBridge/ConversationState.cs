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
    /// Represents the complete state of a conversation session.
    /// </summary>
    [Serializable]
    public class ConversationState
    {
        /// <summary>
        /// Unique identifier for the conversation session.
        /// </summary>
        public string SessionId = string.Empty;

        /// <summary>
        /// List of all messages in the conversation history.
        /// </summary>
        public List<ConversationMessage> Messages = new List<ConversationMessage>();

        /// <summary>
        /// Indicates whether a request is currently being processed.
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// Stores the last error message if any occurred.
        /// </summary>
        public string? LastError = null;
    }
}
