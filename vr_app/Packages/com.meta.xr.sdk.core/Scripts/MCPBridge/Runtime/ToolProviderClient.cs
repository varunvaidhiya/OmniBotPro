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
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meta.MCPBridge.Schemas;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Meta.MCPBridge.Runtime
{
    /// <summary>
    /// Client that connects to the MCP server as a Tool Provider, registering its
    /// capabilities (tools, resources, prompts) and handling execution requests.
    ///
    /// Tool Providers are the authority for what capabilities are available. On connect,
    /// the client discovers local capabilities via <see cref="LocalExecutor"/> and registers
    /// them with the server. The server then serves these capabilities to MCP clients
    /// (AI agents) and forwards execution requests back to this provider.
    ///
    /// <code>
    /// var client = new ToolProviderClient("192.168.1.100", 8090);
    /// client.OnRequestExecuted += (method, success) => Debug.Log($"Request: {method}");
    /// await client.ConnectAsync();
    /// // ... later ...
    /// client.Disconnect();
    /// </code>
    /// </summary>
    public class ToolProviderClient : IDisposable
    {
        private const float ReconnectBaseDelaySec = 1f;
        private const float ReconnectMaxDelaySec = 30f;

        private string _serverAddress;
        private int _serverPort;
        private string _providerId;
        private string _accessToken;
        private HttpClient _httpClient;
        private CancellationTokenSource _connectionCts;
        private Task _sseTask;
        private bool _isConnected;
        private bool _disposed;
        private int _reconnectAttempts;

        #region Events

        /// <summary>
        /// Fired when a request has been executed (success or failure).
        /// Parameters: method, success
        /// </summary>
        public event Action<string, bool> OnRequestExecuted;

        /// <summary>
        /// Fired when the connection state changes.
        /// </summary>
        public event Action<bool> OnConnectionStateChanged;

        /// <summary>
        /// Fired when an error occurs during execution or connection.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Fired when capabilities have been successfully registered with the server.
        /// </summary>
        public event Action OnCapabilitiesRegistered;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the client is currently connected to the MCP server.
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// The server address this client connects to.
        /// </summary>
        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                if (_serverAddress == value) return;
                if (_isConnected) Disconnect();
                _serverAddress = value;
            }
        }

        /// <summary>
        /// The server port this client connects to.
        /// </summary>
        public int ServerPort
        {
            get => _serverPort;
            set
            {
                if (_serverPort == value) return;
                if (_isConnected) Disconnect();
                _serverPort = value;
            }
        }

        /// <summary>
        /// The full server URL.
        /// </summary>
        public string ServerUrl => $"http://{_serverAddress}:{_serverPort}";

        /// <summary>
        /// The access token for authenticating with the MCP Bridge server.
        /// Must be set before calling <see cref="ConnectAsync"/>.
        /// </summary>
        public string AccessToken
        {
            get => _accessToken;
            set
            {
                if (_accessToken == value) return;
                if (_isConnected) Disconnect();
                _accessToken = value;
            }
        }

        #endregion

        /// <summary>
        /// Create a new Tool Provider client.
        /// </summary>
        /// <param name="serverAddress">IP address or hostname of the MCP server (e.g., "192.168.1.100").</param>
        /// <param name="serverPort">Port number for the MCP HTTP server (default: 8090).</param>
        public ToolProviderClient(string serverAddress, int serverPort = 8090)
        {
            _serverAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
            _serverPort = serverPort;
            _providerId = Guid.NewGuid().ToString();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        /// <summary>
        /// Adds the Authorization header to the HttpClient if access token is configured.
        /// </summary>
        private void ConfigureHttpClientAuth(HttpClient client)
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(_accessToken))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            }
        }

        #region Connection Management

        /// <summary>
        /// Connect to the MCP server and start listening for requests.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>True if connection was successful, false otherwise.</returns>
        public virtual async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ToolProviderClient));
            if (_isConnected) return true;

            _reconnectAttempts = 0;
            _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var reachable = await CheckServerReachableAsync(_connectionCts.Token);
            if (!reachable)
            {
                Debug.LogError($"[ToolProviderClient] Server not reachable at {ServerUrl}");
                SetConnected(false);
                return false;
            }

            _sseTask = RunSseStreamAsync(_connectionCts.Token);

            SetConnected(true);
            return true;
        }

        private async Task<bool> CheckServerReachableAsync(CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ServerUrl}/mcpbridge/provider/heartbeat", ct);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ToolProviderClient] Server reachability check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from the MCP server.
        /// </summary>
        public virtual void Disconnect()
        {
            _connectionCts?.Cancel();
            _connectionCts?.Dispose();
            _connectionCts = null;
            _sseTask = null;
            SetConnected(false);
            _reconnectAttempts = 0;
        }

        /// <summary>
        /// Dispose the client and release all resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            _httpClient?.Dispose();
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
                        Debug.LogError($"[ToolProviderClient] SSE stream error: {ex.Message}");
                        OnError?.Invoke(ex.Message);
                    }
                }

                if (ct.IsCancellationRequested) break;

                SetConnected(false);
                await ReconnectAsync(ct);
            }
        }

        private async Task RunSseConnectionAsync(CancellationToken ct)
        {
            var sseUrl = $"{ServerUrl}/mcpbridge/provider/connect?providerId={_providerId}";
            Debug.Log($"[ToolProviderClient] Connecting to {sseUrl}");

            using var request = new HttpRequestMessage(HttpMethod.Get, sseUrl);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            // Add authorization header if access token is configured
            if (!string.IsNullOrEmpty(_accessToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            }

            using var sseClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };
            using var response = await sseClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string currentEvent = null;

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                if (line.StartsWith("event: "))
                {
                    currentEvent = line.Substring(7);
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    await HandleSseEvent(currentEvent, data);
                    currentEvent = null;
                }
            }
        }

        private async Task HandleSseEvent(string eventType, string data)
        {
            switch (eventType)
            {
                case "connected":
                    Debug.Log($"[ToolProviderClient] Connected to MCP server: {data}");
                    SetConnected(true);
                    _reconnectAttempts = 0;
                    await RegisterCapabilitiesAsync();
                    break;

                case "request":
                    var providerRequest = JsonConvert.DeserializeObject<ProviderRequestMessage>(data);
                    if (providerRequest != null)
                    {
                        await HandleRequest(providerRequest);
                    }
                    break;

                case "ping":
                    break;

                default:
                    Debug.Log($"[ToolProviderClient] Unknown event: {eventType}");
                    break;
            }
        }

        #endregion

        #region Capability Registration

        /// <summary>
        /// Build capabilities from local registries and register them with the server.
        /// </summary>
        private async Task RegisterCapabilitiesAsync()
        {
            try
            {
                var capabilities = LocalExecutor.instance.GetCapabilities();

                var json = JsonConvert.SerializeObject(capabilities);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Configure auth header for this request
                ConfigureHttpClientAuth(_httpClient);

                var registerUrl = $"{ServerUrl}/mcpbridge/provider/register?providerId={_providerId}";
                var response = await _httpClient.PostAsync(registerUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[ToolProviderClient] Capabilities registered: " +
                              $"{capabilities.Tools?.Length ?? 0} tools, " +
                              $"{capabilities.Resources?.Length ?? 0} resources, " +
                              $"{capabilities.Prompts?.Length ?? 0} prompts");
                    OnCapabilitiesRegistered?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[ToolProviderClient] Failed to register capabilities: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ToolProviderClient] Failed to register capabilities: {ex.Message}");
                OnError?.Invoke($"Failed to register capabilities: {ex.Message}");
            }
        }

        #endregion

        #region Request Handling

        private async Task HandleRequest(ProviderRequestMessage request)
        {
            Debug.Log($"[ToolProviderClient] Handling request: {request.Method} (id={request.RequestId})");

            try
            {
                ResultSchema result;

                switch (request.Method)
                {
                    case "tools/call":
                        result = await HandleToolCall(request.Params);
                        break;
                    case "resources/read":
                        result = await HandleResourceRead(request.Params);
                        break;
                    case "prompts/get":
                        result = await HandlePromptGet(request.Params);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown request method: {request.Method}");
                }

                await SendResultAsync(request.RequestId, true, result, null);
                OnRequestExecuted?.Invoke(request.Method, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ToolProviderClient] Request execution failed: {ex.Message}");
                OnError?.Invoke($"Request execution failed: {ex.Message}");

                await SendResultAsync(request.RequestId, false, null, ex.Message);
                OnRequestExecuted?.Invoke(request.Method, false);
            }
        }

        private async Task<ResultSchema> HandleToolCall(JObject parameters)
        {
            var toolName = parameters["name"]?.ToString();
            var methodName = parameters["method"]?.ToString()
                             ?? parameters["arguments"]?["method"]?.ToString();
            var arguments = parameters["arguments"] as JObject ?? new JObject();

            if (string.IsNullOrEmpty(toolName))
                throw new ArgumentNullException("name", "Tool name is required");
            if (string.IsNullOrEmpty(methodName))
                throw new ArgumentNullException("method", "Method name is required");

            Debug.Log($"[ToolProviderClient] Executing tool: {toolName}.{methodName}");
            return await LocalExecutor.instance.ExecuteToolCall(toolName, methodName, arguments);
        }

        private async Task<ResultSchema> HandleResourceRead(JObject parameters)
        {
            var uri = parameters["uri"]?.ToString();
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentNullException("uri", "Resource URI is required");

            Debug.Log($"[ToolProviderClient] Reading resource: {uri}");
            return await LocalExecutor.instance.ExecuteResourceRead(uri);
        }

        private Task<ResultSchema> HandlePromptGet(JObject parameters)
        {
            var name = parameters["name"]?.ToString();
            var arguments = parameters["arguments"] as JObject ?? new JObject();

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name", "Prompt name is required");

            Debug.Log($"[ToolProviderClient] Getting prompt: {name}");
            return LocalExecutor.instance.ExecutePromptGet(name, arguments);
        }

        private async Task SendResultAsync(string requestId, bool success, ResultSchema result, string error)
        {
            try
            {
                var payload = new ProviderResponseMessage
                {
                    RequestId = requestId,
                    Success = success,
                    Result = result,
                    Error = error
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Configure auth header for this request
                ConfigureHttpClientAuth(_httpClient);

                var response = await _httpClient.PostAsync(
                    $"{ServerUrl}/mcpbridge/provider/result",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"[ToolProviderClient] Failed to send result: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ToolProviderClient] Failed to send result: {ex.Message}");
            }
        }

        #endregion

        #region Reconnection

        private async Task ReconnectAsync(CancellationToken ct)
        {
            while (!_isConnected && !ct.IsCancellationRequested)
            {
                _reconnectAttempts++;
                var delay = Mathf.Min(
                    ReconnectBaseDelaySec * Mathf.Pow(2, _reconnectAttempts - 1),
                    ReconnectMaxDelaySec);

                Debug.Log($"[ToolProviderClient] Reconnecting in {delay:F1}s (attempt {_reconnectAttempts})...");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (ct.IsCancellationRequested) break;

                break;
            }
        }

        #endregion

        #region State Helpers

        protected void SetConnected(bool connected)
        {
            if (_isConnected == connected) return;
            _isConnected = connected;
            OnConnectionStateChanged?.Invoke(connected);
        }

        /// <summary>
        /// Raise the OnRequestExecuted event. For use by derived classes in testing.
        /// </summary>
        protected void RaiseRequestExecuted(string method, bool success)
        {
            OnRequestExecuted?.Invoke(method, success);
        }

        /// <summary>
        /// Raise the OnCapabilitiesRegistered event. For use by derived classes in testing.
        /// </summary>
        protected void RaiseCapabilitiesRegistered()
        {
            OnCapabilitiesRegistered?.Invoke();
        }

        /// <summary>
        /// Raise the OnError event. For use by derived classes in testing.
        /// </summary>
        protected void RaiseError(string error)
        {
            OnError?.Invoke(error);
        }

        #endregion
    }

    #region Request/Response Types

    /// <summary>
    /// Request received from the MCP server for execution by the Tool Provider.
    /// </summary>
    public class ProviderRequestMessage
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JObject Params { get; set; }
    }

    /// <summary>
    /// Response sent from the Tool Provider back to the MCP server.
    /// </summary>
    public class ProviderResponseMessage
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    #endregion
}
