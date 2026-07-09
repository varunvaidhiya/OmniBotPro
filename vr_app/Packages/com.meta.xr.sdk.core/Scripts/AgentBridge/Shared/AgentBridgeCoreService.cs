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
using System.Threading;
using System.Threading.Tasks;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Core service that bridges Runtime and Editor assemblies using delegates.
    /// The Editor assembly (AgentBridgeManager) sets up these delegates at initialization,
    /// and the Runtime assembly (AgentBridgeAPI) calls through these delegates.
    /// This enables Runtime code to invoke Editor functionality without direct assembly references.
    /// </summary>
    public static class AgentBridgeCoreService
    {
        // Delegates for all operations
        public static Func<string, CallerIdentity?, List<ImageAttachment>?, CancellationToken, string?, Task<bool>>? SendPromptAsyncDelegate;
        public static Func<List<ConversationMessage>>? GetConversationHistoryDelegate;
        public static Func<CallerIdentity?, List<ConversationMessage>>? GetConversationHistoryForCallerDelegate;
        public static Action<CallerIdentity>? ClearConversationDelegate;
        public static Func<CallerIdentity, Task>? CancelCurrentOperationAsyncDelegate;
        public static Func<bool>? IsProcessingDelegate;
        public static Func<bool>? HasActiveSessionDelegate;
        public static Func<string?>? GetLastErrorDelegate;
        public static Func<string>? GetCurrentServiceNameDelegate;
        public static Action? EnsureServiceInitializedDelegate;
        public static Action<CallerIdentity>? ClearErrorDelegate;

        /// <summary>
        /// Send a prompt to the currently configured AI service.
        /// </summary>
        public static async Task<bool> SendPromptAsync(string prompt, CallerIdentity? caller, List<ImageAttachment>? images = null, CancellationToken cancellationToken = default, string? systemPrompt = null)
        {
            if (SendPromptAsyncDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return false;
            }
            return await SendPromptAsyncDelegate(prompt, caller, images, cancellationToken, systemPrompt);
        }

        /// <summary>
        /// Get the conversation history as a list of messages.
        /// </summary>
        public static List<ConversationMessage> GetConversationHistory()
        {
            if (GetConversationHistoryDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return new List<ConversationMessage>();
            }
            return GetConversationHistoryDelegate();
        }

        /// <summary>
        /// Get the conversation history for a specific caller as a list of messages.
        /// </summary>
        public static List<ConversationMessage> GetConversationHistoryForCaller(CallerIdentity? caller)
        {
            if (GetConversationHistoryForCallerDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return new List<ConversationMessage>();
            }
            return GetConversationHistoryForCallerDelegate(caller);
        }

        /// <summary>
        /// Clear the current conversation and start fresh.
        /// </summary>
        public static void ClearConversation(CallerIdentity caller)
        {
            if (ClearConversationDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return;
            }
            ClearConversationDelegate(caller);
        }

        /// <summary>
        /// Cancel the current AI operation if one is in progress.
        /// </summary>
        public static async Task CancelCurrentOperationAsync(CallerIdentity caller)
        {
            if (CancelCurrentOperationAsyncDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return;
            }
            await CancelCurrentOperationAsyncDelegate(caller);
        }

        /// <summary>
        /// Check if an AI agent is currently processing a request.
        /// </summary>
        public static bool IsProcessing()
        {
            if (IsProcessingDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return false;
            }
            return IsProcessingDelegate();
        }

        /// <summary>
        /// Check if there is an active conversation session.
        /// </summary>
        public static bool HasActiveSession()
        {
            if (HasActiveSessionDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return false;
            }
            return HasActiveSessionDelegate();
        }

        /// <summary>
        /// Get the last error message if any occurred.
        /// </summary>
        public static string? GetLastError()
        {
            if (GetLastErrorDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return null;
            }
            return GetLastErrorDelegate();
        }

        /// <summary>
        /// Clear the last error message.
        /// </summary>
        public static void ClearError(CallerIdentity caller)
        {
            if (ClearErrorDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return;
            }
            ClearErrorDelegate(caller);
        }

        /// <summary>
        /// Get the name of the currently active AI service.
        /// </summary>
        public static string GetCurrentServiceName()
        {
            if (GetCurrentServiceNameDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return "None";
            }
            return GetCurrentServiceNameDelegate();
        }

        /// <summary>
        /// Ensure the AI service is initialized based on current settings.
        /// This can be called to explicitly initialize the service without sending a prompt.
        /// </summary>
        public static void EnsureServiceInitialized()
        {
            if (EnsureServiceInitializedDelegate == null)
            {
                UnityEngine.Debug.LogError("[AgentBridge] Core service not initialized. Make sure you are running in Unity Editor.");
                return;
            }
            EnsureServiceInitializedDelegate();
        }
    }
}
