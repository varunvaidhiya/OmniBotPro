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
    /// Event broker for conversation updates. Bridges Runtime and Editor assemblies.
    /// This class is in the Shared folder so both Runtime and Editor code can access it.
    /// The Editor assembly (ConversationManager) raises events through this broker,
    /// and the Runtime assembly (AgentBridgeAPI) subscribes to these events.
    /// </summary>
    public static class ConversationEventBroker
    {
        /// <summary>
        /// Event fired when a new message is added to the conversation.
        /// </summary>
        public static event Action<ConversationMessage>? MessageAdded;

        /// <summary>
        /// Event fired when the conversation is cleared.
        /// </summary>
        public static event Action? ConversationCleared;

        /// <summary>
        /// Event fired when processing state changes.
        /// </summary>
        public static event Action<bool>? ProcessingStateChanged;

        /// <summary>
        /// Raise the MessageAdded event. Called by ConversationManager (Editor).
        /// </summary>
        public static void RaiseMessageAdded(ConversationMessage message)
        {
            MessageAdded?.Invoke(message);
        }

        /// <summary>
        /// Raise the ConversationCleared event. Called by ConversationManager (Editor).
        /// </summary>
        public static void RaiseConversationCleared()
        {
            ConversationCleared?.Invoke();
        }

        /// <summary>
        /// Raise the ProcessingStateChanged event. Called by ConversationManager (Editor).
        /// </summary>
        public static void RaiseProcessingStateChanged(bool isProcessing)
        {
            ProcessingStateChanged?.Invoke(isProcessing);
        }

        /// <summary>
        /// Clear all event subscriptions. Used for test cleanup.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            MessageAdded = null;
            ConversationCleared = null;
            ProcessingStateChanged = null;
        }
    }
}
