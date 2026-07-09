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

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meta.MCPBridge.Schemas;
using Meta.XR.AI.MCPBridge.Telemetry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Exception thrown when no Tool Provider is connected.
    /// </summary>
    internal class NoProviderConnectedException : InvalidOperationException
    {
        internal NoProviderConnectedException()
            : base("No Tool Provider connected. Connect a device running the tool implementations.")
        {
        }
    }

    /// <summary>
    /// Manages Tool Provider connections to the MCP server.
    /// Tool Providers (e.g., DevAgent on Quest) connect via SSE to receive requests,
    /// and send results back via HTTP POST. The Tool Provider is the authority for
    /// what tools, resources, and prompts are available.
    /// </summary>
    internal class ToolProviderManager
    {
        private static ToolProviderManager _instance;
        internal static ToolProviderManager Instance => _instance ??= new ToolProviderManager();

        private ConnectedToolProvider _currentProvider;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ResultSchema>> _pendingCalls = new();
        private readonly object _providerLock = new object();

        /// <summary>
        /// The currently connected Tool Provider, or null if no provider is connected.
        /// </summary>
        internal ConnectedToolProvider CurrentProvider
        {
            get { lock (_providerLock) return _currentProvider; }
        }

        /// <summary>
        /// Whether any Tool Provider is currently connected.
        /// </summary>
        internal bool HasConnectedProvider
        {
            get { lock (_providerLock) return _currentProvider != null; }
        }

        /// <summary>
        /// Register a new Tool Provider connection.
        /// </summary>
        internal void RegisterProvider(string providerId, StreamWriter sseWriter)
        {
            lock (_providerLock)
            {
                if (_currentProvider != null)
                {
                    Debug.Log($"[ToolProviderManager] Replacing existing provider {_currentProvider.ProviderId} with {providerId}");
                    _currentProvider.Dispose();
                }

                _currentProvider = new ConnectedToolProvider(providerId, sseWriter);
                Debug.Log($"[ToolProviderManager] Provider registered: {providerId}");

                var isLocal = providerId == LocalToolProvider.LocalProviderId;
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.ProviderConnected,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ProviderId, providerId);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.IsLocal, isLocal);
                    });
            }
        }

        /// <summary>
        /// Unregister a Tool Provider connection.
        /// </summary>
        internal void UnregisterProvider(string providerId)
        {
            lock (_providerLock)
            {
                if (_currentProvider?.ProviderId == providerId)
                {
                    var connectionDuration = (DateTime.UtcNow - _currentProvider.ConnectedAt).TotalMilliseconds;
                    var isLocal = providerId == LocalToolProvider.LocalProviderId;
                    var toolCount = _currentProvider.Capabilities?.Tools?.Length ?? 0;
                    var resourceCount = _currentProvider.Capabilities?.Resources?.Length ?? 0;
                    var promptCount = _currentProvider.Capabilities?.Prompts?.Length ?? 0;

                    _currentProvider.Dispose();
                    _currentProvider = null;
                    Debug.Log($"[ToolProviderManager] Provider unregistered: {providerId}");

                    McpBridgeTelemetry.SendEvent(
                        McpBridgeTelemetryConstants.FalcoEventName.ProviderDisconnected,
                        evt =>
                        {
                            evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ProviderId, providerId);
                            evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, connectionDuration);
                            evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.IsLocal, isLocal);
                            evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ToolCount, toolCount);
                            evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ResourceCount, resourceCount);
                            evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.PromptCount, promptCount);
                        });
                }
            }
        }

        /// <summary>
        /// Check if a specific provider is connected.
        /// </summary>
        internal bool IsProviderConnected(string providerId)
        {
            lock (_providerLock)
            {
                return _currentProvider?.ProviderId == providerId && _currentProvider.IsConnected;
            }
        }

        #region Capability List Operations

        /// <summary>
        /// Get the list of tools from the connected provider's registered capabilities.
        /// </summary>
        internal ResultSchema GetToolList()
        {
            lock (_providerLock)
            {
                if (_currentProvider?.Capabilities?.Tools == null)
                    throw new NoProviderConnectedException();

                return new ToolListResultSchema
                {
                    Tools = _currentProvider.Capabilities.Tools
                };
            }
        }

        /// <summary>
        /// Get the list of resources from the connected provider's registered capabilities.
        /// </summary>
        internal ResultSchema GetResourceList()
        {
            lock (_providerLock)
            {
                if (_currentProvider?.Capabilities?.Resources == null)
                    throw new NoProviderConnectedException();

                return new ResourceListResultSchema
                {
                    Resources = _currentProvider.Capabilities.Resources
                };
            }
        }

        /// <summary>
        /// Get the list of prompts from the connected provider's registered capabilities.
        /// </summary>
        internal ResultSchema GetPromptList()
        {
            lock (_providerLock)
            {
                if (_currentProvider?.Capabilities?.Prompts == null)
                    throw new NoProviderConnectedException();

                return new PromptListResultSchema
                {
                    Prompts = _currentProvider.Capabilities.Prompts
                };
            }
        }

        #endregion

        #region Request Forwarding

        /// <summary>
        /// Forward a request to the connected Tool Provider and await the result.
        /// </summary>
        /// <param name="method">MCP method (e.g., "tools/call", "resources/read", "prompts/get")</param>
        /// <param name="parameters">Request parameters to forward</param>
        /// <param name="timeout">Timeout in milliseconds (default 30 seconds)</param>
        /// <returns>The result from the provider's execution</returns>
        internal async Task<ResultSchema> ForwardRequest(
            string method,
            JObject parameters,
            int timeout = 30000)
        {
            ConnectedToolProvider provider;
            lock (_providerLock)
            {
                provider = _currentProvider;
            }

            if (provider == null)
            {
                throw new NoProviderConnectedException();
            }

            var requestId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<ResultSchema>();

            _pendingCalls[requestId] = tcs;

            try
            {
                var request = new ProviderRequest
                {
                    RequestId = requestId,
                    Method = method,
                    Params = parameters
                };

                await provider.SendRequest(request);

                using var cts = new CancellationTokenSource(timeout);
                cts.Token.Register(() => tcs.TrySetException(
                    new TimeoutException($"Provider request timed out after {timeout}ms")));

                return await tcs.Task;
            }
            finally
            {
                _pendingCalls.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// Forward a tool call to the connected Tool Provider.
        /// If the local provider is connected, executes directly via LocalExecutor.
        /// </summary>
        internal async Task<ResultSchema> ForwardToolCall(string toolName, string methodName, JObject arguments)
        {
            ConnectedToolProvider provider;
            lock (_providerLock)
            {
                provider = _currentProvider;
            }

            if (provider == null)
            {
                throw new NoProviderConnectedException();
            }

            var isLocal = provider.ProviderId == LocalToolProvider.LocalProviderId;
            var stopwatch = Stopwatch.StartNew();

            McpBridgeTelemetry.SendEvent(
                McpBridgeTelemetryConstants.FalcoEventName.ToolInvoked,
                evt =>
                {
                    evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ToolName, toolName);
                    evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.MethodName, methodName);
                    evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.IsLocal, isLocal);
                });

            try
            {
                ResultSchema result;

                // If local provider, execute directly via LocalExecutor
                if (isLocal)
                {
                    result = await LocalExecutor.instance.ExecuteToolCall(toolName, methodName, arguments);
                }
                else
                {
                    // Remote provider - forward via SSE
                    var parameters = new JObject
                    {
                        ["name"] = toolName,
                        ["method"] = methodName,
                        ["arguments"] = arguments
                    };
                    result = await ForwardRequest("tools/call", parameters);
                }

                stopwatch.Stop();
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.ToolCompleted,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ToolName, toolName);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.MethodName, methodName);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, stopwatch.ElapsedMilliseconds);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Success, true);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.IsLocal, isLocal);
                    });

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.ToolCompleted,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ToolName, toolName);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.MethodName, methodName);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, stopwatch.ElapsedMilliseconds);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.IsLocal, isLocal);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorType, ClassifyError(ex));
                    });
                throw;
            }
        }

        /// <summary>
        /// Classifies an exception into an error type for telemetry.
        /// </summary>
        private static string ClassifyError(Exception ex)
        {
            return ex switch
            {
                TimeoutException => McpBridgeTelemetryConstants.ErrorType.Timeout,
                JsonException => McpBridgeTelemetryConstants.ErrorType.Serialization,
                UnauthorizedAccessException => McpBridgeTelemetryConstants.ErrorType.Authentication,
                NoProviderConnectedException => McpBridgeTelemetryConstants.ErrorType.NoProvider,
                _ => McpBridgeTelemetryConstants.ErrorType.Execution
            };
        }

        /// <summary>
        /// Forward a resource read to the connected Tool Provider.
        /// If the local provider is connected, executes directly via LocalExecutor.
        /// </summary>
        internal async Task<ResultSchema> ForwardResourceRead(string uri)
        {
            ConnectedToolProvider provider;
            lock (_providerLock)
            {
                provider = _currentProvider;
            }

            if (provider == null)
            {
                throw new NoProviderConnectedException();
            }

            // If local provider, execute directly via LocalExecutor
            if (provider.ProviderId == LocalToolProvider.LocalProviderId)
            {
                return await LocalExecutor.instance.ExecuteResourceRead(uri);
            }

            // Remote provider - forward via SSE
            var parameters = new JObject
            {
                ["uri"] = uri
            };
            return await ForwardRequest("resources/read", parameters);
        }

        /// <summary>
        /// Forward a prompt get to the connected Tool Provider.
        /// If the local provider is connected, executes directly via LocalExecutor.
        /// </summary>
        internal async Task<ResultSchema> ForwardPromptGet(string name, JObject arguments)
        {
            ConnectedToolProvider provider;
            lock (_providerLock)
            {
                provider = _currentProvider;
            }

            if (provider == null)
            {
                throw new NoProviderConnectedException();
            }

            // If local provider, execute directly via LocalExecutor
            if (provider.ProviderId == LocalToolProvider.LocalProviderId)
            {
                return await LocalExecutor.instance.ExecutePromptGet(name, arguments);
            }

            // Remote provider - forward via SSE
            var parameters = new JObject
            {
                ["name"] = name,
                ["arguments"] = arguments
            };
            return await ForwardRequest("prompts/get", parameters);
        }

        #endregion

        /// <summary>
        /// Receive a result from a provider for a pending request.
        /// </summary>
        internal void ReceiveResult(ProviderResponse result)
        {
            if (_pendingCalls.TryGetValue(result.RequestId, out var tcs))
            {
                if (result.Success)
                {
                    tcs.TrySetResult(result.Result ?? new ResultSchema());
                }
                else
                {
                    tcs.TrySetException(new Exception(result.Error ?? "Unknown error from provider"));
                }
            }
            else
            {
                Debug.LogWarning($"[ToolProviderManager] Received result for unknown request: {result.RequestId}");
            }
        }

        /// <summary>
        /// Update the last heartbeat time for a provider.
        /// </summary>
        internal void UpdateHeartbeat(string providerId)
        {
            lock (_providerLock)
            {
                if (_currentProvider?.ProviderId == providerId)
                {
                    _currentProvider.UpdateHeartbeat();
                }
            }
        }

        /// <summary>
        /// Reset the manager (for testing).
        /// </summary>
        internal void Reset()
        {
            lock (_providerLock)
            {
                _currentProvider?.Dispose();
                _currentProvider = null;
            }
            _pendingCalls.Clear();
        }
    }

    /// <summary>
    /// Represents a connected Tool Provider with its registered capabilities.
    /// </summary>
    internal class ConnectedToolProvider : IDisposable
    {
        internal string ProviderId { get; }
        internal DateTime ConnectedAt { get; }
        internal DateTime LastHeartbeat { get; private set; }
        internal bool IsConnected { get; private set; }

        /// <summary>
        /// Capabilities registered by the Tool Provider (tools, resources, prompts).
        /// </summary>
        internal ProviderCapabilities Capabilities { get; private set; }

        private readonly StreamWriter _sseWriter;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        internal ConnectedToolProvider(string providerId, StreamWriter sseWriter)
        {
            ProviderId = providerId;
            _sseWriter = sseWriter;
            ConnectedAt = DateTime.UtcNow;
            LastHeartbeat = DateTime.UtcNow;
            IsConnected = true;
        }

        internal void SetCapabilities(ProviderCapabilities capabilities)
        {
            Capabilities = capabilities;
            Debug.Log($"[ConnectedToolProvider] Capabilities registered for {ProviderId}: " +
                      $"{capabilities.Tools?.Length ?? 0} tools, " +
                      $"{capabilities.Resources?.Length ?? 0} resources, " +
                      $"{capabilities.Prompts?.Length ?? 0} prompts");
        }

        internal void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
        }

        internal async Task SendRequest(ProviderRequest request)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Provider is not connected");

            await _writeLock.WaitAsync();
            try
            {
                var json = JsonConvert.SerializeObject(request);
                await _sseWriter.WriteLineAsync($"event: request");
                await _sseWriter.WriteLineAsync($"data: {json}");
                await _sseWriter.WriteLineAsync();
                await _sseWriter.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConnectedToolProvider] Failed to send request: {ex.Message}");
                IsConnected = false;
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        internal async Task SendPing()
        {
            if (!IsConnected) return;

            await _writeLock.WaitAsync();
            try
            {
                await _sseWriter.WriteLineAsync($"event: ping");
                await _sseWriter.WriteLineAsync($"data: {{}}");
                await _sseWriter.WriteLineAsync();
                await _sseWriter.FlushAsync();
            }
            catch
            {
                IsConnected = false;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            IsConnected = false;
            _writeLock.Dispose();
        }
    }

    /// <summary>
    /// Request sent to the Tool Provider for execution.
    /// </summary>
    internal class ProviderRequest
    {
        [JsonProperty("requestId")]
        internal string RequestId { get; set; }

        [JsonProperty("method")]
        internal string Method { get; set; }

        [JsonProperty("params")]
        internal JObject Params { get; set; }
    }

    /// <summary>
    /// Response received from the Tool Provider after execution.
    /// </summary>
    internal class ProviderResponse
    {
        [JsonProperty("requestId")]
        internal string RequestId { get; set; }

        [JsonProperty("success")]
        internal bool Success { get; set; }

        [JsonProperty("result")]
        internal ResultSchema Result { get; set; }

        [JsonProperty("error")]
        internal string Error { get; set; }
    }
}
