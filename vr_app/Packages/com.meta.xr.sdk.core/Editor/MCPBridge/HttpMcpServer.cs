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
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Lightweight HTTP server implementing the MCP JSON-RPC 2.0 protocol.
    /// Acts as a pure router between MCP clients (AI agents like Claude Code, DevMate)
    /// and the connected Tool Provider (device running tool implementations).
    ///
    /// When no Tool Provider is connected, all capability requests return an error
    /// instructing the user to connect a device.
    /// </summary>
    internal class HttpMcpServer : IDisposable
    {
        private static HttpMcpServer _instance;
        internal static HttpMcpServer Instance => _instance ??= new HttpMcpServer();

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Thread _listenerThread;
        private bool _isRunning;
        private Stopwatch _serverUptime;
        private bool _autoStarted;

        // Thread-safe token cache - EditorPrefs can only be accessed from the main thread,
        // but ValidateToken is called from background HTTP listener threads.
        private static volatile string _cachedAccessToken;

        internal bool IsRunning => _isRunning;
        internal int Port { get; private set; }

        /// <summary>
        /// Gets the cached access token. Only valid when server is running.
        /// </summary>
        internal static string CachedAccessToken => _cachedAccessToken;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // Register cleanup handlers immediately — lightweight event subscriptions.
            EditorApplication.quitting += () => Instance.Stop();
            AssemblyReloadEvents.beforeAssemblyReload += () => Instance.Stop();


            // Defer server startup until after all assets are post-processed.
            // The callback fires before the editor UI is interactive, so the server
            // is still ready well before any play mode client could try to connect.
            // Only start if the AI Agent Bridge toggle is enabled (dormant by default).
            Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(() =>
            {
                if (Meta.XR.AI.AgentBridge.Settings.Enabled.Value)
                {
                    StartIfEnabled();
                }
            });

            // When the user enables AI Agent Bridge mid-session, initialize MCPBridge too.
            Meta.XR.AI.AgentBridge.Settings.Activated += StartIfEnabled;
        }

        /// <summary>
        /// Initialize the MCPBridge service registry and start the server if AutoStart is enabled.
        /// </summary>
        internal static void StartIfEnabled()
        {
            Meta.MCPBridge.Services.Registry.Initialize();

            if (McpBridgeSettings.AutoStart.Value)
            {
                StartMcpBridgeServer();
            }
        }

        private static void StartMcpBridgeServer()
        {
            Instance.Start(McpBridgeSettings.Port.Value, () =>
            {
                LocalToolProvider.Register();
                ExternalDiscovery.WriteDiscoveryFiles();
            });
        }

        internal void Start(int port, Action onStarted = null)
        {
            if (_isRunning)
                return;

            // Cache the access token BEFORE starting the listener to avoid race conditions.
            // EditorPrefs can only be accessed from the main thread, but ValidateToken runs
            // on background HTTP listener threads. We must cache before accepting requests.
            _cachedAccessToken = McpBridgeSettings.EnsureToken();

            Port = port;
            _cts = new CancellationTokenSource();
            _autoStarted = McpBridgeSettings.AutoStart.Value;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/mcpbridge/");

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpMcpServer] Failed to start on port {port}: {ex.GetType().Name}: {ex.Message}");
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.Error,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorType,
                            McpBridgeTelemetryConstants.ErrorType.Execution);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorMessage,
                            $"Failed to start on port {port}: {ex.GetType().Name}");
                    });

                try { _listener?.Close(); }
                catch (Exception closeEx) { Debug.LogWarning($"[HttpMcpServer] Error closing listener during cleanup: {closeEx.Message}"); }
                _listener = null;
                _cts?.Dispose();
                _cts = null;
                return;
            }

            _isRunning = true;
            _serverUptime = Stopwatch.StartNew();

            _listenerThread = new Thread(ListenLoop) { IsBackground = true, Name = "HttpMcpServer" };
            _listenerThread.Start();

            Debug.Log($"[HttpMcpServer] Started on port {port}");

            McpBridgeTelemetry.SendEvent(
                            McpBridgeTelemetryConstants.FalcoEventName.ServerStarted,
                            evt =>
                            {
                                evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Port, port);
                                evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.AutoStart, _autoStarted);
                            },
                            isEssential: true);

            onStarted?.Invoke();
        }

        internal void Stop()
        {
            if (!_isRunning)
                return;

            var cleanShutdown = true;
            var uptimeMs = _serverUptime?.ElapsedMilliseconds ?? 0;

            _isRunning = false;
            _cts?.Cancel();

            // Stop the listener first to unblock GetContext() on the background thread.
            try
            {
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                cleanShutdown = false;
                Debug.LogWarning($"[HttpMcpServer] Error stopping listener: {ex.Message}");
            }

            // Wait for the background thread to exit before closing the listener.
            // Without this, Close() can race with the thread still in GetContext(),
            // leaving the HTTP.sys registration orphaned.
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _listenerThread.Join(TimeSpan.FromSeconds(2));
            }

            try
            {
                _listener?.Close();
            }
            catch (Exception ex)
            {
                cleanShutdown = false;
                Debug.LogWarning($"[HttpMcpServer] Error closing listener: {ex.Message}");
            }

            _listener = null;
            _listenerThread = null;
            _cts?.Dispose();
            _cts = null;
            _serverUptime?.Stop();
            _serverUptime = null;

            Debug.Log("[HttpMcpServer] Stopped");

            McpBridgeTelemetry.SendEvent(
                            McpBridgeTelemetryConstants.FalcoEventName.ServerStopped,
                            evt =>
                            {
                                evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, uptimeMs);
                                evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.CleanShutdown, cleanShutdown);
                            },
                            isEssential: true);
        }

        public void Dispose()
        {
            Stop();
        }

        private void ListenLoop()
        {
            var cts = _cts;
            var listener = _listener;

            if (cts == null || listener == null)
                return;

            while (_isRunning && !cts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = listener.GetContext();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (HttpListenerException) when (cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Debug.LogError($"[HttpMcpServer] Listener error: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;
            var path = context.Request.Url.AbsolutePath;
            var requestStopwatch = Stopwatch.StartNew();

            // Handle Tool Provider connection endpoints
            if (path.StartsWith("/mcpbridge/provider/"))
            {
                await HandleProviderEndpoint(context, path);
                return;
            }

            // Standard MCP JSON-RPC handling
            response.ContentType = "application/json";
            AddCorsHeaders(context.Request, response, "POST, OPTIONS");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            if (context.Request.HttpMethod != "POST")
            {
                await WriteJsonRpcError(response, null, -32600, "Only POST requests are accepted");
                return;
            }

            // Validate token for MCP client requests
            if (!ValidateToken(context.Request))
            {
                Debug.LogWarning($"[HttpMcpServer] Unauthorized MCP client request");
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.Error,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorType,
                            McpBridgeTelemetryConstants.ErrorType.Authentication);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorMessage,
                            "Unauthorized MCP client request");
                    });
                await WriteJsonRpcError(response, null, -32001, "Unauthorized: invalid or missing access token");
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

            RequestSchema request;
            try
            {
                request = JsonConvert.DeserializeObject<RequestSchema>(body);
            }
            catch (Exception ex)
            {
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.Error,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorType,
                            McpBridgeTelemetryConstants.ErrorType.Serialization);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorMessage,
                            $"Parse error: {ex.GetType().Name}");
                    });
                await WriteJsonRpcError(response, null, -32700, $"Parse error: {ex.Message}");
                return;
            }

            if (request == null)
            {
                await WriteJsonRpcError(response, null, -32600, "Invalid request");
                return;
            }

            McpBridgeTelemetry.SendEvent(
                McpBridgeTelemetryConstants.FalcoEventName.RequestReceived,
                evt =>
                {
                    evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Method, request.Method ?? "unknown");
                });

            try
            {
                var result = await DispatchMethod(request);
                requestStopwatch.Stop();

                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.RequestCompleted,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Method, request.Method ?? "unknown");
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, requestStopwatch.ElapsedMilliseconds);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Success, true);
                    });

                await WriteJsonRpcResult(response, request.Id, result);
            }
            catch (NoProviderConnectedException)
            {
                requestStopwatch.Stop();
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.RequestCompleted,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Method, request.Method ?? "unknown");
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, requestStopwatch.ElapsedMilliseconds);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorType,
                            McpBridgeTelemetryConstants.ErrorType.NoProvider);
                    });
                await WriteJsonRpcError(response, request.Id, -32001,
                    "No Tool Provider connected. Connect a device running the tool implementations.");
            }
            catch (Exception ex)
            {
                requestStopwatch.Stop();
                McpBridgeTelemetry.SendEvent(
                    McpBridgeTelemetryConstants.FalcoEventName.RequestCompleted,
                    evt =>
                    {
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Method, request.Method ?? "unknown");
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Duration, requestStopwatch.ElapsedMilliseconds);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorType, ClassifyError(ex));
                        evt.SetMetadata(McpBridgeTelemetryConstants.AnnotationType.ErrorMessage, ex.GetType().Name);
                    });
                await WriteJsonRpcError(response, request.Id, -32603, ex.Message);
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

        #region Tool Provider Endpoints

        private async Task HandleProviderEndpoint(HttpListenerContext context, string path)
        {
            var response = context.Response;
            AddCorsHeaders(context.Request, response, "GET, POST, OPTIONS");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            // Validate token for all provider endpoints except heartbeat (allows reachability check)
            if (path != "/mcpbridge/provider/heartbeat" && !ValidateToken(context.Request))
            {
                Debug.LogWarning($"[HttpMcpServer] Unauthorized provider request to {path}");
                response.StatusCode = 401;
                response.ContentType = "application/json";
                await WriteSimpleResponse(response, "{\"error\":\"Unauthorized\"}");
                return;
            }

            try
            {
                switch (path)
                {
                    case "/mcpbridge/provider/connect":
                        await HandleProviderConnect(context);
                        break;
                    case "/mcpbridge/provider/register":
                        await HandleProviderRegister(context);
                        break;
                    case "/mcpbridge/provider/result":
                        await HandleProviderResult(context);
                        break;
                    case "/mcpbridge/provider/heartbeat":
                        await HandleProviderHeartbeat(context);
                        break;
                    default:
                        response.StatusCode = 404;
                        await WriteSimpleResponse(response, "Not found");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpMcpServer] Provider endpoint error: {ex.Message}");
                response.StatusCode = 500;
                await WriteSimpleResponse(response, ex.Message);
            }
        }

        /// <summary>
        /// Handle Tool Provider SSE connection.
        /// </summary>
        private async Task HandleProviderConnect(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "GET")
            {
                context.Response.StatusCode = 405;
                await WriteSimpleResponse(context.Response, "Method not allowed. Use GET for SSE connection.");
                return;
            }

            var providerId = context.Request.QueryString["providerId"] ?? Guid.NewGuid().ToString();
            var response = context.Response;

            response.ContentType = "text/event-stream";
            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("Connection", "keep-alive");
            response.StatusCode = 200;

            var outputStream = response.OutputStream;
            var writer = new StreamWriter(outputStream, Encoding.UTF8) { AutoFlush = true };

            await writer.WriteLineAsync("event: connected");
            await writer.WriteLineAsync($"data: {{\"providerId\":\"{providerId}\"}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            ToolProviderManager.Instance.RegisterProvider(providerId, writer);

            Debug.Log($"[HttpMcpServer] Tool Provider connected via SSE: {providerId}");

            try
            {
                while (_isRunning && ToolProviderManager.Instance.IsProviderConnected(providerId))
                {
                    await Task.Delay(30000); // 30 second ping interval

                    if (ToolProviderManager.Instance.IsProviderConnected(providerId))
                    {
                        var provider = ToolProviderManager.Instance.CurrentProvider;
                        if (provider != null && provider.ProviderId == providerId)
                        {
                            await provider.SendPing();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[HttpMcpServer] Provider {providerId} disconnected: {ex.Message}");
            }
            finally
            {
                ToolProviderManager.Instance.UnregisterProvider(providerId);
            }
        }

        /// <summary>
        /// Handle Tool Provider capability registration.
        /// </summary>
        private async Task HandleProviderRegister(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                await WriteSimpleResponse(context.Response, "Method not allowed. Use POST.");
                return;
            }

            var providerId = context.Request.QueryString["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                context.Response.StatusCode = 400;
                await WriteSimpleResponse(context.Response, "Missing providerId query parameter");
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

            var capabilities = JsonConvert.DeserializeObject<ProviderCapabilities>(body);
            if (capabilities == null)
            {
                context.Response.StatusCode = 400;
                await WriteSimpleResponse(context.Response, "Invalid capabilities payload");
                return;
            }

            var provider = ToolProviderManager.Instance.CurrentProvider;
            if (provider != null && provider.ProviderId == providerId)
            {
                provider.SetCapabilities(capabilities);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await WriteSimpleResponse(context.Response, "{\"status\":\"ok\"}");
            }
            else
            {
                context.Response.StatusCode = 404;
                await WriteSimpleResponse(context.Response, "Provider not found or not connected");
            }
        }

        /// <summary>
        /// Handle execution result from Tool Provider.
        /// </summary>
        private async Task HandleProviderResult(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                await WriteSimpleResponse(context.Response, "Method not allowed. Use POST.");
                return;
            }

            string body;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

            var result = JsonConvert.DeserializeObject<ProviderResponse>(body);
            if (result == null || string.IsNullOrEmpty(result.RequestId))
            {
                context.Response.StatusCode = 400;
                await WriteSimpleResponse(context.Response, "Invalid result message");
                return;
            }

            ToolProviderManager.Instance.ReceiveResult(result);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await WriteSimpleResponse(context.Response, "{\"status\":\"ok\"}");
        }

        /// <summary>
        /// Handle heartbeat from Tool Provider.
        /// GET: Used by provider to check server reachability before connecting.
        /// POST: Used by connected provider to send periodic heartbeats.
        /// </summary>
        private async Task HandleProviderHeartbeat(HttpListenerContext context)
        {
            var method = context.Request.HttpMethod;

            if (method == "GET")
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await WriteSimpleResponse(context.Response, "{\"status\":\"ok\"}");
                return;
            }

            if (method == "POST")
            {
                var providerId = context.Request.QueryString["providerId"];
                if (!string.IsNullOrEmpty(providerId))
                {
                    ToolProviderManager.Instance.UpdateHeartbeat(providerId);
                }

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await WriteSimpleResponse(context.Response, "{\"status\":\"ok\"}");
                return;
            }

            context.Response.StatusCode = 405;
            await WriteSimpleResponse(context.Response, "Method not allowed. Use GET or POST.");
        }

        private static async Task WriteSimpleResponse(HttpListenerResponse response, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        #endregion

        private async Task<object> DispatchMethod(RequestSchema request)
        {
            var providerManager = ToolProviderManager.Instance;

            switch (request.Method)
            {
                case "initialize":
                    return new InitializationSchema { Id = request.Id };

                case "notifications/initialized":
                    return new ResultSchema();

                case "tools/list":
                    return providerManager.GetToolList();

                case "resources/list":
                    return providerManager.GetResourceList();

                case "prompts/list":
                    return providerManager.GetPromptList();

                case "tools/call":
                    return await ForwardToolCallToProvider(providerManager, request.Parameters);

                case "resources/read":
                    return await providerManager.ForwardResourceRead(request.Parameters.Uri);

                case "prompts/get":
                    return await providerManager.ForwardPromptGet(
                        request.Parameters.Name, request.Parameters.Arguments);

                default:
                    throw new InvalidOperationException($"Unknown method: {request.Method}");
            }
        }

        /// <summary>
        /// Forward a tool call to the connected Tool Provider.
        /// </summary>
        private async Task<ResultSchema> ForwardToolCallToProvider(
            ToolProviderManager providerManager, RequestParametersSchema parameters)
        {
            var methodName = parameters.Arguments?["method"]?.ToString();
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException("method", "Method name is required in arguments");
            }

            Debug.Log($"[HttpMcpServer] Forwarding tool call to provider: {parameters.Name}.{methodName}");

            return await providerManager.ForwardToolCall(
                parameters.Name,
                methodName,
                parameters.Arguments);
        }

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private static async Task WriteJsonRpcResult(HttpListenerResponse response, string id, object result)
        {
            var responseObj = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id
            };

            if (result is InitializationSchema initSchema)
            {
                var serialized = JObject.FromObject(initSchema, JsonSerializer.CreateDefault(SerializerSettings));
                var resultObj = new JObject
                {
                    ["serverInfo"] = serialized["serverInfo"],
                    ["protocolVersion"] = serialized["protocolVersion"],
                    ["capabilities"] = serialized["capabilities"]
                };
                responseObj["result"] = resultObj;
            }
            else
            {
                responseObj["result"] = JObject.FromObject(result, JsonSerializer.CreateDefault(SerializerSettings));
            }

            response.StatusCode = 200;
            await WriteResponse(response, responseObj.ToString(Formatting.None));
        }

        private static async Task WriteJsonRpcError(HttpListenerResponse response, string id, int code, string message)
        {
            var responseObj = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = message
                }
            };

            response.StatusCode = 200;
            await WriteResponse(response, responseObj.ToString(Formatting.None));
        }

        private static async Task WriteResponse(HttpListenerResponse response, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        #region CORS

        /// <summary>
        /// Adds CORS headers only for trusted localhost origins.
        /// Prevents the "localhost service CSRF" attack vector where a malicious website
        /// could make cross-origin requests to this localhost server.
        /// </summary>
        private static void AddCorsHeaders(HttpListenerRequest request, HttpListenerResponse response, string methods)
        {
            var origin = request.Headers["Origin"];
            if (!string.IsNullOrEmpty(origin) && IsAllowedOrigin(origin))
            {
                response.AddHeader("Access-Control-Allow-Origin", origin);
                response.AddHeader("Access-Control-Allow-Methods", methods);
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
            }
        }

        /// <summary>
        /// Returns true for localhost/loopback origins only.
        /// Accepts http://localhost[:port], http://127.0.0.1[:port], and http://[::1][:port].
        /// </summary>
        internal static bool IsAllowedOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return false;

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                return false;

            var host = uri.Host;
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(host, "127.0.0.1", StringComparison.Ordinal)
                   || string.Equals(host, "[::1]", StringComparison.Ordinal)
                   || string.Equals(host, "::1", StringComparison.Ordinal);
        }

        #endregion

        #region Token Validation

        /// <summary>
        /// Validates the access token from the request's Authorization header.
        /// Expected format: "Bearer {token}"
        /// </summary>
        private static bool ValidateToken(HttpListenerRequest request)
        {
            var authHeader = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var providedToken = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(providedToken))
            {
                return false;
            }

            // Use the cached token - EditorPrefs (used by AccessToken.Value) can only be
            // accessed from the main thread, but this method runs on HTTP listener threads.
            var expectedToken = _cachedAccessToken;
            if (string.IsNullOrEmpty(expectedToken))
            {
                // No token configured/cached - reject all requests
                return false;
            }

            return string.Equals(providedToken, expectedToken, StringComparison.Ordinal);
        }

        #endregion
    }
}
