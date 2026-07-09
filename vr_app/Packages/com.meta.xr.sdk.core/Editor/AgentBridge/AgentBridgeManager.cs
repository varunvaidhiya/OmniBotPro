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
using Meta.XR.AI.AgentBridge.Telemetry;
using Meta.XR.Editor.Id;
using UnityEngine;
using UnityEditor;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Central manager for AI service access and lifecycle management.
    /// Provides a unified API for interacting with different AI services.
    /// Wires up delegates in AgentBridgeCoreService to bridge Runtime and Editor assemblies.
    /// Thread-safe: All public methods can be called from any thread.
    /// </summary>
    [InitializeOnLoad]
    public static class AgentBridgeManager
    {
        private static IAIService? _currentService;
        private static string? _currentServiceId;
        private static readonly object _serviceLock = new object();

        /// <summary>
        /// Static constructor called when Unity Editor loads.
        /// Wires up all delegates in AgentBridgeCoreService.
        /// </summary>
        static AgentBridgeManager()
        {
            // Wire up all delegates to bridge Runtime and Editor assemblies.
            // These are lightweight pointer assignments — safe to do immediately.
            AgentBridgeCoreService.SendPromptAsyncDelegate = SendPromptAsync;
            AgentBridgeCoreService.GetConversationHistoryDelegate = GetConversationHistory;
            AgentBridgeCoreService.GetConversationHistoryForCallerDelegate = GetConversationHistoryForCaller;
            AgentBridgeCoreService.ClearConversationDelegate = ClearConversation;
            AgentBridgeCoreService.CancelCurrentOperationAsyncDelegate = CancelCurrentOperationAsync;
            AgentBridgeCoreService.IsProcessingDelegate = IsProcessing;
            AgentBridgeCoreService.HasActiveSessionDelegate = HasActiveSession;
            AgentBridgeCoreService.GetLastErrorDelegate = GetLastError;
            AgentBridgeCoreService.GetCurrentServiceNameDelegate = GetCurrentServiceName;
            AgentBridgeCoreService.EnsureServiceInitializedDelegate = EnsureServiceInitialized;
            AgentBridgeCoreService.ClearErrorDelegate = ClearError;

            // Register cleanup handlers
            AssemblyReloadEvents.beforeAssemblyReload += OnShutdown;
            EditorApplication.quitting += OnShutdown;

            // Defer heavier initialization (EditorPrefs access, server startup)
            // until after all assets are post-processed.
            // Only register if the master toggle is enabled; otherwise stays dormant.
            if (Settings.Enabled.Value)
            {
                Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(InitializeDeferred);
            }
        }

        /// <summary>
        /// Initialize the manager if the master toggle is enabled.
        /// Called from Settings.InitializeAllServices() when the user enables AI packages.
        /// </summary>
        public static void InitializeIfEnabled()
        {
            if (Settings.Enabled.Value)
            {
                InitializeDeferred();
            }
        }

        private static void InitializeDeferred()
        {
            Log.VerboseLogging = Settings.VerboseLogging.Value;
            RemoteAgentServer.Initialize();
            Log.Info("Manager initialized and delegates wired up");
        }

        private static void OnShutdown()
        {
            Cleanup();
            RemoteAgentServer.Stop();
        }

        /// <summary>
        /// Get the current AI service, creating it if necessary based on settings.
        /// Uses AIServiceRegistry for service creation to support third-party services.
        /// Thread-safe: Uses lock to prevent race conditions during service switching.
        /// </summary>
        private static IAIService GetOrCreateService()
        {
            lock (_serviceLock)
            {
                // Read desired service ID from settings
                var desiredServiceId = Settings.SelectedServiceId.Value;

                // If service changed or doesn't exist, create new one
                if (_currentService == null || _currentServiceId != desiredServiceId)
                {
                    var isServiceSwitch = _currentServiceId != null && _currentServiceId != desiredServiceId;
                    Log.Info($"Switching to service: {desiredServiceId}");

                    // Dispose old service before creating new one
                    _currentService?.Dispose();
                    _currentService = null;
                    _currentServiceId = null;

                    // Clear all conversation states when switching services — session IDs are
                    // service-specific and cannot be reused across providers.
                    if (isServiceSwitch)
                    {
                        ConversationManager.ClearAll();
                    }

                    // Create new service via registry
                    if (AIServiceRegistry.IsServiceRegistered(desiredServiceId))
                    {
                        _currentService = AIServiceRegistry.CreateService(desiredServiceId);
                        _currentServiceId = desiredServiceId;
                    }
                    else
                    {
                        // Fall back to default service if requested one is not registered
                        var defaultServiceId = AIServiceRegistry.GetDefaultServiceId();
                        if (!string.IsNullOrEmpty(defaultServiceId))
                        {
                            Log.Warning($"Service '{desiredServiceId}' not available, falling back to '{defaultServiceId}'");
                            _currentService = AIServiceRegistry.CreateService(defaultServiceId!);
                            _currentServiceId = defaultServiceId;
                            Settings.SelectedServiceId.SetValue(defaultServiceId, Origins.Self, Utils.ToolDescriptor);
                        }
                        else
                        {
                            Log.Error("No AI services available");
                            throw new InvalidOperationException("No AI services available");
                        }
                    }
                }

                return _currentService!;
            }
        }

        /// <summary>
        /// Get the current AI service instance (for settings UI).
        /// </summary>
        internal static IAIService? GetCurrentService()
        {
            return _currentService;
        }

        /// <summary>
        /// Send a prompt to the currently configured AI service.
        /// </summary>
        /// <param name="prompt">The text prompt to send</param>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <param name="images">Optional list of image attachments</param>
        /// <param name="cancellationToken">Optional cancellation token for async operation</param>
        /// <param name="systemPrompt">Contextual information for AI</param>
        /// <returns>True if prompt was sent successfully, false otherwise</returns>
        public static async Task<bool> SendPromptAsync(string prompt, CallerIdentity? caller, List<ImageAttachment>? images = null, CancellationToken cancellationToken = default, string? systemPrompt = null)
        {
            var startTime = DateTime.UtcNow;
            var success = false;

            try
            {
                // Send telemetry before processing
                SendPromptTelemetry(prompt, images, caller, isStart: true);

                var service = GetOrCreateService();
                await service.ProcessUserInputAsync(prompt, caller, images, cancellationToken, systemPrompt);

                success = true;
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    ConversationManager.SetError($"{ex.Message} ({ex.GetType().Name})");
                });
                return false;
            }
            finally
            {
                // Send completion telemetry with duration
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                SendPromptTelemetry(prompt, images, caller, isStart: false, duration, success);
            }
        }

        /// <summary>
        /// Send prompt telemetry event to track usage metrics.
        /// </summary>
        private static void SendPromptTelemetry(string prompt, List<ImageAttachment>? images,
            CallerIdentity? caller, bool isStart, double duration = 0, bool success = false,
            int? inputTokens = null, int? outputTokens = null)
        {
            try
            {
                var sessionId = ConversationManager.GetSessionId();
                var serviceName = _currentService?.ServiceName ?? "None";
                var eventName = isStart
                    ? AgentBridgeTelemetryConstants.FalcoEventName.PromptSent
                    : AgentBridgeTelemetryConstants.FalcoEventName.PromptCompleted;
                // Send prompt sent event (when we send)
                AgentBridgeTelemetry.SendEvent(eventName,
                    eventData =>
                    {
                        eventData.SetMetadata(AgentBridgeTelemetryConstants.AnnotationType.SessionId, sessionId);
                        eventData.SetMetadata(AgentBridgeTelemetryConstants.AnnotationType.ServiceType, serviceName);
                        eventData.SetMetadata(AgentBridgeTelemetryConstants.AnnotationType.ImageCount,
                            images?.Count ?? 0);
                        eventData.SetMetadata(AgentBridgeTelemetryConstants.AnnotationType.Duration, duration);
                        eventData.SetMetadata(AgentBridgeTelemetryConstants.AnnotationType.Success, success);
                        eventData.SetMetadata(AgentBridgeTelemetryConstants.AnnotationType.CallerId,
                            caller?.Id);
                    });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Clear the conversation for the specified caller.
        /// </summary>
        public static void ClearConversation(CallerIdentity caller)
        {
            Log.Info($"Clearing conversation for caller: {caller.Id}");
            _currentService?.ClearSession();
            ConversationManager.ClearForCaller(caller);
        }

        /// <summary>
        /// Cancel the current AI operation if one is in progress.
        /// </summary>
        public static async Task CancelCurrentOperationAsync(CallerIdentity caller)
        {
            if (_currentService != null)
            {
                Log.Info("Cancelling current operation");
                await _currentService.CancelCurrentOperationAsync();
            }
        }

        /// <summary>
        /// Get all messages in the current conversation.
        /// </summary>
        public static List<ConversationMessage> GetConversationHistory()
        {
            return ConversationManager.GetMessages();
        }

        /// <summary>
        /// Get messages for a specific caller.
        /// </summary>
        public static List<ConversationMessage> GetConversationHistoryForCaller(CallerIdentity? caller)
        {
            return ConversationManager.GetMessagesForCaller(caller);
        }

        /// <summary>
        /// Check if an AI operation is currently in progress.
        /// </summary>
        public static bool IsProcessing()
        {
            return ConversationManager.IsActive;
        }

        /// <summary>
        /// Get the last error message if any occurred.
        /// </summary>
        public static string? GetLastError()
        {
            var error = ConversationManager.GetLastError();
            return string.IsNullOrEmpty(error) ? null : error;
        }

        /// <summary>
        /// Clear the last error message.
        /// </summary>
        public static void ClearError(CallerIdentity caller)
        {
            ConversationManager.ClearError();
        }

        /// <summary>
        /// Check if there is an active conversation session.
        /// </summary>
        public static bool HasActiveSession()
        {
            return _currentService?.HasActiveSession ?? false;
        }

        /// <summary>
        /// Get the name of the currently active AI service.
        /// </summary>
        public static string GetCurrentServiceName()
        {
            return _currentService?.ServiceName ?? "None";
        }

        /// <summary>
        /// Switch to a different AI service by ID.
        /// This will dispose the current service and create a new one.
        /// </summary>
        /// <param name="serviceId">The service ID to switch to</param>
        public static void SwitchService(string serviceId)
        {
            // Check if we're already using the requested service ID (early exit optimization)
            if (_currentServiceId == serviceId)
            {
                return;
            }

            if (!AIServiceRegistry.IsServiceRegistered(serviceId))
            {
                UnityEngine.Debug.LogWarning($"[AgentBridge] Service '{serviceId}' is not registered");
                return;
            }

            Log.Info($"Switching to service: {serviceId}");

            // Update settings
            Settings.SelectedServiceId.SetValue(serviceId, Origins.Self, Utils.ToolDescriptor);

            // Dispose old service before creating new one
            _currentService?.Dispose();
            _currentService = null;
            _currentServiceId = null;

            // Clear all conversation states — session IDs are service-specific
            // and cannot be reused across providers.
            ConversationManager.ClearAll();

            // This will create the new service on next use
            GetOrCreateService();
        }

        /// <summary>
        /// Ensure the service is initialized based on current settings.
        /// This can be called to explicitly initialize the service without sending a prompt.
        /// </summary>
        public static void EnsureServiceInitialized()
        {
            Log.Info("Explicitly initializing service...");
            GetOrCreateService();
        }

        /// <summary>
        /// Internal cleanup method for disposing services.
        /// Called during domain reload and app shutdown.
        /// </summary>
        internal static void Cleanup()
        {
            _currentService?.Dispose();
            _currentService = null;
            _currentServiceId = null;
        }
    }
}
