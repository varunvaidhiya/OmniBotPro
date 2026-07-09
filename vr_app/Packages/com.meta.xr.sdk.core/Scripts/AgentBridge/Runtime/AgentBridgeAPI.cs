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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Public API for external tools (like Building Blocks) to interact with AI agents.
    /// This is the primary integration point for other Unity packages.
    /// </summary>
    public static class AgentBridgeAPI
    {
        /// <summary>
        /// Event fired when a new message is added to the conversation.
        /// Subscribe to this to receive real-time conversation updates.
        /// </summary>
        public static event System.Action<ConversationMessage>? OnMessageAdded
        {
            add => ConversationEventBroker.MessageAdded += value;
            remove => ConversationEventBroker.MessageAdded -= value;
        }

        /// <summary>
        /// Event fired when the conversation is cleared.
        /// </summary>
        public static event System.Action? OnConversationCleared
        {
            add => ConversationEventBroker.ConversationCleared += value;
            remove => ConversationEventBroker.ConversationCleared -= value;
        }

        /// <summary>
        /// Event fired when processing state changes (true = started processing, false = stopped).
        /// Subscribe to this instead of polling IsProcessing().
        /// </summary>
        public static event System.Action<bool>? OnProcessingStateChanged
        {
            add => ConversationEventBroker.ProcessingStateChanged += value;
            remove => ConversationEventBroker.ProcessingStateChanged -= value;
        }

        /// <summary>
        /// Send a text prompt to the selected AI agent.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the AI</param>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <param name="systemPrompt">Contextual information for AI</param>
        /// <returns>True if prompt was sent successfully, false otherwise</returns>
        /// <example>
        /// <code>
        /// var caller = new CallerIdentity("MyTool");
        /// bool success = await AgentBridgeAPI.SendPromptAsync("Add a VR locomotion system to the scene", caller);
        /// if (!success)
        /// {
        ///     Debug.LogError($"Failed to send prompt: {AgentBridgeAPI.GetLastError()}");
        /// }
        /// </code>
        /// </example>
        public static async Task<bool> SendPromptAsync(string prompt, CallerIdentity caller, string? systemPrompt = null)
        {
            return await AgentBridgeCoreService.SendPromptAsync(prompt, caller, null, systemPrompt: systemPrompt);
        }

        /// <summary>
        /// Send a prompt with image attachments to the selected AI agent.
        /// </summary>
        /// <param name="prompt">The text prompt to send to the AI</param>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <param name="images">List of image attachments (base64-encoded)</param>
        /// <returns>True if prompt was sent successfully, false otherwise</returns>
        /// <example>
        /// <code>
        /// var images = new List&lt;ImageAttachment&gt;
        /// {
        ///     new ImageAttachment
        ///     {
        ///         Filename = "screenshot.png",
        ///         MediaType = "image/png",
        ///         Data = Convert.ToBase64String(imageBytes)
        ///     }
        /// };
        /// var caller = new CallerIdentity("MyTool");
        /// bool success = await AgentBridgeAPI.SendPromptAsync("What's in this image?", caller, images);
        /// </code>
        /// </example>
        public static async Task<bool> SendPromptAsync(string prompt, CallerIdentity caller, List<ImageAttachment> images)
        {
            return await AgentBridgeCoreService.SendPromptAsync(prompt, caller, images);
        }

        /// <summary>
        /// Get the conversation history as a list of messages.
        /// </summary>
        /// <returns>List of conversation messages</returns>
        /// <example>
        /// <code>
        /// var messages = AgentBridgeAPI.GetConversationHistory();
        /// foreach (var msg in messages)
        /// {
        ///     Debug.Log($"[{msg.MessageType}] {msg.Content}");
        /// }
        /// </code>
        /// </example>
        public static List<ConversationMessage> GetConversationHistory()
        {
            return AgentBridgeCoreService.GetConversationHistory();
        }

        /// <summary>
        /// Get the conversation history for a specific caller as a list of messages.
        /// </summary>
        /// <param name="caller">The caller to get history for</param>
        /// <returns>List of conversation messages for the specified caller</returns>
        /// <example>
        /// <code>
        /// var caller = new CallerIdentity("MyTool");
        /// var messages = AgentBridgeAPI.GetConversationHistoryForCaller(caller);
        /// foreach (var msg in messages)
        /// {
        ///     Debug.Log($"[{msg.MessageType}] {msg.Content}");
        /// }
        /// </code>
        /// </example>
        public static List<ConversationMessage> GetConversationHistoryForCaller(CallerIdentity caller)
        {
            return AgentBridgeCoreService.GetConversationHistoryForCaller(caller);
        }

        /// <summary>
        /// Clear the current conversation and start fresh.
        /// This will reset the conversation history and end the current session.
        /// </summary>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <example>
        /// <code>
        /// var caller = new CallerIdentity("MyTool");
        /// AgentBridgeAPI.ClearConversation(caller);
        /// Debug.Log("Conversation cleared, starting fresh");
        /// </code>
        /// </example>
        public static void ClearConversation(CallerIdentity caller)
        {
            AgentBridgeCoreService.ClearConversation(caller);
        }

        /// <summary>
        /// Cancel the current AI operation if one is in progress.
        /// </summary>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <example>
        /// <code>
        /// var caller = new CallerIdentity("MyTool");
        /// if (AgentBridgeAPI.IsProcessing())
        /// {
        ///     await AgentBridgeAPI.CancelCurrentOperationAsync(caller);
        ///     Debug.Log("AI operation cancelled");
        /// }
        /// </code>
        /// </example>
        public static async Task CancelCurrentOperationAsync(CallerIdentity caller)
        {
            await AgentBridgeCoreService.CancelCurrentOperationAsync(caller);
        }

        /// <summary>
        /// Check if an AI agent is currently processing a request.
        /// </summary>
        /// <returns>True if processing, false otherwise</returns>
        /// <example>
        /// <code>
        /// if (AgentBridgeAPI.IsProcessing())
        /// {
        ///     Debug.Log("Please wait, AI is thinking...");
        /// }
        /// </code>
        /// </example>
        public static bool IsProcessing()
        {
            return AgentBridgeCoreService.IsProcessing();
        }

        /// <summary>
        /// Check if there is an active conversation session.
        /// </summary>
        /// <returns>True if there's an active session, false otherwise</returns>
        /// <example>
        /// <code>
        /// if (AgentBridgeAPI.HasActiveSession())
        /// {
        ///     Debug.Log("Continuing existing conversation");
        /// }
        /// else
        /// {
        ///     Debug.Log("Starting new conversation");
        /// }
        /// </code>
        /// </example>
        public static bool HasActiveSession()
        {
            return AgentBridgeCoreService.HasActiveSession();
        }

        /// <summary>
        /// Get the last error message if any occurred.
        /// Returns null if no error.
        /// </summary>
        /// <returns>Error message string or null</returns>
        /// <example>
        /// <code>
        /// var caller = new CallerIdentity("MyTool");
        /// bool success = await AgentBridgeAPI.SendPromptAsync("Hello", caller);
        /// if (!success)
        /// {
        ///     string? error = AgentBridgeAPI.GetLastError();
        ///     Debug.LogError($"AI Error: {error ?? "Unknown error"}");
        /// }
        /// </code>
        /// </example>
        public static string? GetLastError()
        {
            return AgentBridgeCoreService.GetLastError();
        }

        /// <summary>
        /// Clear the last error message.
        /// </summary>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <example>
        /// <code>
        /// var caller = new CallerIdentity("MyTool");
        /// AgentBridgeAPI.ClearError(caller);
        /// </code>
        /// </example>
        public static void ClearError(CallerIdentity caller)
        {
            AgentBridgeCoreService.ClearError(caller);
        }

        /// <summary>
        /// Get the name of the currently active AI service (e.g., "Claude Code", "Devmate").
        /// </summary>
        /// <returns>Service name string</returns>
        /// <example>
        /// <code>
        /// string serviceName = AgentBridgeAPI.GetCurrentServiceName();
        /// Debug.Log($"Using AI service: {serviceName}");
        /// </code>
        /// </example>
        public static string GetCurrentServiceName()
        {
            return AgentBridgeCoreService.GetCurrentServiceName();
        }

        /// <summary>
        /// Create an image attachment from a Unity Texture2D.
        /// </summary>
        /// <param name="texture">The texture to convert</param>
        /// <param name="filename">Optional filename (defaults to "image.png")</param>
        /// <returns>ImageAttachment ready to send to AI</returns>
        /// <example>
        /// <code>
        /// Texture2D screenshot = GetScreenshot();
        /// ImageAttachment imageAttachment = AgentBridgeAPI.CreateImageFromTexture(screenshot, "screenshot.png");
        /// await AgentBridgeAPI.SendPromptAsync("Analyze this screenshot", new List&lt;ImageAttachment&gt; { imageAttachment });
        /// </code>
        /// </example>
        public static ImageAttachment CreateImageFromTexture(UnityEngine.Texture2D texture, string filename = "image.png")
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID
            var imageBytes = UnityEngine.ImageConversion.EncodeToPNG(texture);
            var base64Data = System.Convert.ToBase64String(imageBytes);

            return new ImageAttachment
            {
                Filename = filename,
                Data = base64Data,
                MediaType = "image/png"
            };
#else
            throw new System.NotSupportedException("Image encoding is not supported on this platform.");
#endif
        }

        /// <summary>
        /// Ensure the AI service is initialized based on current settings.
        /// This can be called to explicitly initialize the service without sending a prompt.
        /// </summary>
        /// <example>
        /// <code>
        /// AgentBridgeAPI.EnsureServiceInitialized();
        /// Debug.Log($"Service: {AgentBridgeAPI.GetCurrentServiceName()}");
        /// </code>
        /// </example>
        public static void EnsureServiceInitialized()
        {
            AgentBridgeCoreService.EnsureServiceInitialized();
        }
    }
}
