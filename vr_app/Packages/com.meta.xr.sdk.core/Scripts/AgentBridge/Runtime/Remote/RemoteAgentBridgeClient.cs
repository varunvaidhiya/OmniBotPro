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
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// HTTP client for communicating with the Remote Agent Server running in the Unity Editor.
    /// Provides the same AI inference capabilities as <see cref="AgentBridgeAPI"/> but over HTTP,
    /// enabling use on remote devices (e.g., Quest headsets) that cannot access Editor-only APIs.
    ///
    /// Consumers create an instance, connect, and subscribe to events:
    /// <code>
    /// var client = new RemoteAgentBridgeClient("192.168.1.100");
    /// client.OnMessageReceived += msg => Debug.Log(msg.Content);
    /// client.OnConnectionStateChanged += connected => Debug.Log($"Connected: {connected}");
    /// await client.ConnectAsync();
    /// // ... later ...
    /// await client.SendPromptAsync(new RemotePromptRequest { Prompt = "Hello", CallerId = "MyApp" });
    /// </code>
    /// </summary>
    public class RemoteAgentBridgeClient : IRemoteAgentBridgeClient
    {
        private const double ReconnectBaseDelaySec = 1.0;
        private const double ReconnectMaxDelaySec = 30.0;
        private const double HealthCheckIntervalSec = 10.0;

        private string _serverUrl;
        private int _port;
        private bool _isConnected;
        private bool _disposed;
        private int _reconnectAttempts;

        private HttpClient _httpClient;
        private CancellationTokenSource? _connectionCts;
        private Task? _sseTask;
        private Task? _healthCheckTask;

        #region Events

        /// <summary>
        /// Fired when a conversation message is received from the server.
        /// </summary>
        public event Action<ConversationMessage>? OnMessageReceived;

        /// <summary>
        /// Fired when the server's processing state changes (true = processing, false = idle).
        /// </summary>
        public event Action<bool>? OnProcessingStateChanged;

        /// <summary>
        /// Fired when the server conversation is cleared.
        /// </summary>
        public event Action? OnConversationCleared;

        /// <summary>
        /// Fired when the connection state to the server changes (true = connected, false = disconnected).
        /// </summary>
        public event Action<bool>? OnConnectionStateChanged;

        /// <summary>
        /// Fired when an error is received from the server during AI processing.
        /// </summary>
        public event Action<RemoteSseError>? OnErrorReceived;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the client is currently connected to the Remote Agent Server.
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// The access token for authenticating with the Remote Agent Server.
        /// Must be set before calling <see cref="ConnectAsync"/>.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// The base URL for the Remote Agent Server (e.g., "192.168.1.100").
        /// Setting this while connected will disconnect and require a new <see cref="ConnectAsync"/>.
        /// </summary>
        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                if (_serverUrl == value) return;
                if (_isConnected) Disconnect();
                _serverUrl = value;
            }
        }

        /// <summary>
        /// The port for the Remote Agent Server.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (_port == value) return;
                if (_isConnected) Disconnect();
                _port = value;
            }
        }

        private string BaseUrl => $"http://{_serverUrl}:{_port}/agentbridge/";

        #endregion

        /// <summary>
        /// Create a new Remote Agent Bridge client.
        /// </summary>
        /// <param name="serverUrl">IP address or hostname of the machine running the Unity Editor (e.g., "192.168.1.100").</param>
        /// <param name="port">Port number matching the Remote Agent Server configuration (default: 48735).</param>
        public RemoteAgentBridgeClient(string serverUrl, int port = 48735)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
            _port = port;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        #region Connection Management

        /// <summary>
        /// Connect to the Remote Agent Server. Starts the SSE stream and health check loop.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>True if connection was successful, false otherwise.</returns>
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RemoteAgentBridgeClient));
            if (_isConnected) return true;

            _reconnectAttempts = 0;
            _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Verify server reachability with a status check
            var (status, error) = await GetStatusAsync(_connectionCts.Token);
            if (status == null)
            {
                Log.Error($"Server not reachable at {BaseUrl}: {error}");
                SetConnected(false);
                return false;
            }

            // Start SSE stream (fire and forget, runs in background)
            _sseTask = RunSseStreamAsync(_connectionCts.Token);

            // Start health check loop (fire and forget, runs in background)
            _healthCheckTask = RunHealthCheckLoopAsync(_connectionCts.Token);

            SetConnected(true);
            return true;
        }

        /// <summary>
        /// Disconnect from the server. Stops SSE stream and health checks.
        /// </summary>
        public void Disconnect()
        {
            _connectionCts?.Cancel();
            _connectionCts?.Dispose();
            _connectionCts = null;
            _sseTask = null;
            _healthCheckTask = null;
            SetConnected(false);
            _reconnectAttempts = 0;
        }

        /// <summary>
        /// Dispose the client and release all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            _httpClient?.Dispose();
            _httpClient = null!;
        }

        #endregion

        #region AI Inference Methods

        /// <summary>
        /// Send a prompt to the Remote Agent Server for AI inference.
        /// The response will arrive asynchronously via <see cref="OnMessageReceived"/> events.
        /// </summary>
        /// <param name="request">The prompt request containing prompt text, caller ID, and optional system prompt.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (success, errorMessage).</returns>
        public async Task<(bool success, string? error)> SendPromptAsync(
            RemotePromptRequest request,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RemoteAgentBridgeClient));

            var (responseBody, error) = await PostRequestAsync("prompt", JsonConvert.SerializeObject(request), cancellationToken);
            if (error != null)
            {
                Log.Error($"SendPrompt failed: {error}");
                return (false, error);
            }

            return ParseOperationResult(responseBody!);
        }

        /// <summary>
        /// Cancel the current AI operation on the server.
        /// </summary>
        /// <param name="request">The caller request identifying who is cancelling.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (success, errorMessage).</returns>
        public async Task<(bool success, string? error)> CancelAsync(
            RemoteCallerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RemoteAgentBridgeClient));

            var (responseBody, error) = await PostRequestAsync("cancel", JsonConvert.SerializeObject(request), cancellationToken);
            if (error != null)
            {
                Log.Error($"Cancel failed: {error}");
                return (false, error);
            }

            return ParseOperationResult(responseBody!);
        }

        /// <summary>
        /// Clear the conversation on the server.
        /// </summary>
        /// <param name="request">The caller request identifying who is clearing the conversation.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (success, errorMessage).</returns>
        public async Task<(bool success, string? error)> ClearConversationAsync(
            RemoteCallerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RemoteAgentBridgeClient));

            var (responseBody, error) = await PostRequestAsync("clear", JsonConvert.SerializeObject(request), cancellationToken);
            if (error != null)
            {
                Log.Error($"Clear failed: {error}");
                return (false, error);
            }

            return ParseOperationResult(responseBody!);
        }

        /// <summary>
        /// Get the current status of the Remote Agent Server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (status, errorMessage). Status is null on failure.</returns>
        public async Task<(RemoteAgentStatus? status, string? error)> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RemoteAgentBridgeClient));

            var url = $"{BaseUrl}status";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ConfigureAuth(request);
                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return (null, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }

                var body = await response.Content.ReadAsStringAsync();
                var status = JsonConvert.DeserializeObject<RemoteAgentStatus>(body);
                return (status, null);
            }
            catch (OperationCanceledException)
            {
                return (null, "Request cancelled");
            }
            catch (Exception ex)
            {
                return (null, $"Failed to get status: {ex.Message}");
            }
        }

        #endregion

        #region SSE Stream

        private async Task RunSseStreamAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await RunSseConnectionAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Log.Error($"SSE stream error: {ex.Message}");
                    }
                }

                if (ct.IsCancellationRequested) break;

                // SSE stream ended — attempt reconnection
                SetConnected(false);
                await ReconnectAsync(ct);
            }
        }

        private async Task RunSseConnectionAsync(CancellationToken ct)
        {
            var url = $"{BaseUrl}messages";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            ConfigureAuth(request);

            using var sseClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };
            using var response = await sseClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            SseEventType currentEventType = SseEventType.Message;
            var currentData = new StringBuilder();
            bool hasData = false;

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break; // Stream ended

                if (string.IsNullOrEmpty(line))
                {
                    // Empty line = dispatch event
                    if (hasData)
                    {
                        OnSseEventReceived(new SseEvent { EventType = currentEventType, Data = currentData.ToString() });
                        currentData.Clear();
                        hasData = false;
                    }
                    currentEventType = SseEventType.Message;
                }
                else if (line.StartsWith("event: "))
                {
                    currentEventType = SseEvent.ParseWireName(line.Substring(7));
                }
                else if (line.StartsWith("data: "))
                {
                    if (hasData) currentData.Append('\n');
                    currentData.Append(line.Substring(6));
                    hasData = true;
                }
            }
        }

        private void OnSseEventReceived(SseEvent sseEvent)
        {
            try
            {
                switch (sseEvent.EventType)
                {
                    case SseEventType.Message:
                        var message = JsonConvert.DeserializeObject<ConversationMessage>(sseEvent.Data);
                        if (message != null)
                        {
                            OnMessageReceived?.Invoke(message);
                        }
                        break;

                    case SseEventType.Status:
                        var processingState = JsonConvert.DeserializeObject<RemoteProcessingState>(sseEvent.Data);
                        if (processingState != null)
                        {
                            OnProcessingStateChanged?.Invoke(processingState.IsProcessing);
                        }
                        break;

                    case SseEventType.Cleared:
                        OnConversationCleared?.Invoke();
                        break;

                    case SseEventType.Error:
                        var error = JsonConvert.DeserializeObject<RemoteSseError>(sseEvent.Data);
                        if (error != null)
                        {
                            OnErrorReceived?.Invoke(error);
                        }
                        break;

                    case SseEventType.Connected:
                    case SseEventType.Ping:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling SSE event '{sseEvent.WireEventName}': {ex.Message}");
            }
        }

        #endregion

        #region Health Check

        private async Task RunHealthCheckLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _isConnected)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(HealthCheckIntervalSec), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (ct.IsCancellationRequested || !_isConnected) break;

                var (status, _) = await GetStatusAsync(ct);
                if (status == null && _isConnected && !ct.IsCancellationRequested)
                {
                    Log.Error("Health check failed — server unreachable");
                    SetConnected(false);
                    // Reconnection is handled by the SSE stream task
                }
            }
        }

        #endregion

        #region Reconnection

        private async Task ReconnectAsync(CancellationToken ct)
        {
            while (!_isConnected && !ct.IsCancellationRequested)
            {
                _reconnectAttempts++;
                var delay = Math.Min(
                    ReconnectBaseDelaySec * Math.Pow(2, _reconnectAttempts - 1),
                    ReconnectMaxDelaySec);

                Log.Info($"Reconnecting in {delay:F1}s (attempt {_reconnectAttempts})...");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (ct.IsCancellationRequested) break;

                var (status, _) = await GetStatusAsync(ct);
                if (status != null)
                {
                    _reconnectAttempts = 0;
                    SetConnected(true);
                    break;
                }
            }
        }

        #endregion

        #region HTTP Helpers

        private async Task<(string? body, string? error)> PostRequestAsync(
            string endpoint,
            string jsonBody,
            CancellationToken ct)
        {
            var url = $"{BaseUrl}{endpoint}";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };
                ConfigureAuth(request);
                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    return (null, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }

                var body = await response.Content.ReadAsStringAsync();
                return (body, null);
            }
            catch (OperationCanceledException)
            {
                return (null, "Request cancelled");
            }
            catch (Exception ex)
            {
                return (null, $"Request failed: {ex.Message}");
            }
        }

        private void ConfigureAuth(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
            }
        }

        #endregion

        #region Response Parsing

        private static (bool success, string? error) ParseOperationResult(string json)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<RemoteOperationResult>(json);
                return (result?.Success ?? false, result?.Error);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to parse response: {ex.Message}");
            }
        }

        #endregion

        #region State Helpers

        private void SetConnected(bool connected)
        {
            if (_isConnected == connected) return;
            _isConnected = connected;
            OnConnectionStateChanged?.Invoke(connected);
        }

        #endregion
    }
}
