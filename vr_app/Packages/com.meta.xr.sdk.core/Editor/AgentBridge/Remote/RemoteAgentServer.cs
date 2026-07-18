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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// HTTP server that enables remote AI inference via AgentBridge.
    /// Runs in the Unity Editor and accepts requests from remote clients (e.g., Quest headsets).
    /// Exposes REST endpoints for sending prompts, cancelling, clearing, and status queries.
    /// Streams conversation updates to connected clients via Server-Sent Events (SSE).
    /// Lifecycle is managed by AgentBridgeManager — do not add [InitializeOnLoad] here.
    /// </summary>
    internal static class RemoteAgentServer
    {
        private const string LogPrefix = "Remote:";
        private const string BasePath = "/agentbridge/";
        private const int SsePingIntervalMs = 15000;

        private static HttpListener? _listener;
        private static CancellationTokenSource? _cts;
        private static bool _isRunning;

        // Thread-safe token cache - EditorPrefs can only be accessed from the main thread,
        // but ValidateToken is called from background HTTP listener threads.
        private static volatile string? _cachedAccessToken;

        private static readonly ConcurrentDictionary<Guid, SseClient> _sseClients = new();

        /// <summary>
        /// Initialize the remote server based on settings.
        /// Called by AgentBridgeManager during editor initialization.
        /// </summary>
        public static void Initialize()
        {
            if (RemoteAgentSettings.AutoStart.Value)
            {
                Start();
            }
        }

        /// <summary>
        /// Whether the server is currently running and accepting connections.
        /// </summary>
        public static bool IsRunning => _isRunning;

        /// <summary>
        /// Number of currently connected SSE clients.
        /// </summary>
        public static int ConnectedClientCount => _sseClients.Count;

        /// <summary>
        /// Start the HTTP server on the configured port.
        /// </summary>
        public static void Start()
        {
            if (_isRunning)
            {
                Log.Info($"{LogPrefix} Server is already running");
                return;
            }

            var port = RemoteAgentSettings.Port.Value;

            try
            {
                // Cache the access token BEFORE starting the listener to avoid race conditions.
                // EditorPrefs can only be accessed from the main thread, but ValidateToken runs
                // on background HTTP listener threads. We must cache before accepting requests.
                _cachedAccessToken = RemoteAgentSettings.EnsureToken();

                _cts = new CancellationTokenSource();
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://*:{port}{BasePath}");
                _listener.Start();
                _isRunning = true;

                // Subscribe to conversation events for SSE broadcasting
                ConversationEventBroker.MessageAdded += OnMessageAdded;
                ConversationEventBroker.ProcessingStateChanged += OnProcessingStateChanged;
                ConversationEventBroker.ConversationCleared += OnConversationCleared;

                // Start accepting requests on a background thread
                Task.Run(() => AcceptRequestsAsync(_cts.Token));

                var localIp = NetworkUtilities.GetLocalNetworkAddress();
                Log.Info($"{LogPrefix} Server started on port {port} (accessible at {localIp}:{port})");
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix} Failed to start server on port {port}: {ex.Message}");
                _isRunning = false;
                _listener?.Close();
                _listener = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Stop the HTTP server and disconnect all SSE clients.
        /// </summary>
        public static void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            // Unsubscribe from conversation events
            ConversationEventBroker.MessageAdded -= OnMessageAdded;
            ConversationEventBroker.ProcessingStateChanged -= OnProcessingStateChanged;
            ConversationEventBroker.ConversationCleared -= OnConversationCleared;

            // Cancel pending async operations
            try { _cts?.Cancel(); }
            catch (Exception ex) { Log.Warning($"{LogPrefix} Error cancelling operations: {ex.Message}"); }

            // Close all SSE clients
            foreach (var kvp in _sseClients)
            {
                try { kvp.Value.Close(); }
                catch (Exception ex) { Log.Warning($"{LogPrefix} Error closing SSE client {kvp.Key}: {ex.Message}"); }
            }
            _sseClients.Clear();

            // Stop the listener first to unblock GetContextAsync(), then close.
            // Separating Stop/Close prevents a race where Close() disposes resources
            // while pending I/O is still being cancelled by Stop().
            try { _listener?.Stop(); }
            catch (Exception ex) { Log.Warning($"{LogPrefix} Error stopping listener: {ex.Message}"); }

            try { _listener?.Close(); }
            catch (Exception ex) { Log.Warning($"{LogPrefix} Error closing listener: {ex.Message}"); }
            _listener = null;

            try { _cts?.Dispose(); }
            catch (Exception ex) { Log.Warning($"{LogPrefix} Error disposing CTS: {ex.Message}"); }
            _cts = null;

            Log.Info($"{LogPrefix} Server stopped");
        }

        /// <summary>
        /// Restart the server (e.g., after port change).
        /// </summary>
        public static void Restart()
        {
            Stop();
            Start();
        }

        #region Request Handling

        private static async Task AcceptRequestsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    // Handle each request independently (don't await — fire and forget)
                    _ = Task.Run(() => HandleRequestAsync(context, ct), ct);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Log.Error($"{LogPrefix} Error accepting request: {ex.Message}");
                    }
                }
            }
        }

        private static async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
        {
            var request = context.Request;
            var response = context.Response;

            // Add CORS headers for trusted localhost origins only.
            // Prevents the "localhost service CSRF" attack vector where a malicious website
            // could make cross-origin requests to this localhost server.
            AddCorsHeaders(request, response, "GET, POST, OPTIONS");

            // Handle preflight OPTIONS requests
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            try
            {
                // Route based on path (strip the base path prefix)
                var path = request.Url?.AbsolutePath ?? "";
                var route = path.StartsWith(BasePath)
                    ? path.Substring(BasePath.Length).TrimEnd('/')
                    : path.TrimEnd('/');

                // Validate token for all endpoints except /status (allows discovery without auth)
                if (route != "status" && !ValidateToken(request))
                {
                    Log.Warning($"{LogPrefix} Unauthorized request to /{route}");
                    await WriteJsonResponse(response, 401, new RemoteErrorResponse { Error = "Unauthorized" });
                    return;
                }

                switch (route)
                {
                    case "prompt" when request.HttpMethod == "POST":
                        await HandlePromptAsync(request, response, ct);
                        break;
                    case "cancel" when request.HttpMethod == "POST":
                        await HandleCancelAsync(request, response);
                        break;
                    case "clear" when request.HttpMethod == "POST":
                        await HandleClearAsync(request, response);
                        break;
                    case "status" when request.HttpMethod == "GET":
                        await HandleStatusAsync(response);
                        break;
                    case "messages" when request.HttpMethod == "GET":
                        await HandleSseConnectionAsync(context, ct);
                        return; // SSE handler manages its own lifecycle
                    default:
                        await WriteJsonResponse(response, 404, new RemoteErrorResponse { Error = "Not found", Path = route });
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogPrefix} Error handling request: {ex.Message}");
                try
                {
                    await WriteJsonResponse(response, 500, new RemoteErrorResponse { Error = ex.Message });
                }
                catch { /* response may already be closed */ }
            }
        }

        #endregion

        #region Endpoint Handlers

        private static async Task HandlePromptAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken ct)
        {
            var promptRequest = await ReadRequestBodyAsync<RemotePromptRequest>(request);
            if (promptRequest == null || string.IsNullOrEmpty(promptRequest.Prompt))
            {
                await WriteJsonResponse(response, 400, new RemoteOperationResult { Success = false, Error = "Missing 'prompt' field" });
                return;
            }

            Log.Info($"{LogPrefix} Received prompt from '{promptRequest.CallerId}': {promptRequest.Prompt.Substring(0, Math.Min(promptRequest.Prompt.Length, 100))}...");

            // Associate SSE clients with this caller (for per-caller message filtering)
            AssociateCallerWithClients(promptRequest.CallerId, request.RemoteEndPoint);

            // Dispatch to AgentBridge on the main thread and await the result
            var tcs = new TaskCompletionSource<bool>();

            MainThreadDispatcher.ExecuteOnMainThread(async () =>
            {
                try
                {
                    var caller = new CallerIdentity(promptRequest.CallerId);
                    var success = await AgentBridgeManager.SendPromptAsync(promptRequest.Prompt, caller, systemPrompt: promptRequest.SystemPrompt);

                    if (!success)
                    {
                        // AI service reported failure - broadcast error to the caller's SSE clients
                        var errorMessage = AgentBridgeManager.GetLastError() ?? "Unknown error";
                        BroadcastSseEvent(SseEventType.Error, new RemoteSseError
                        {
                            Code = "SERVICE_ERROR",
                            Message = errorMessage,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            CallerId = promptRequest.CallerId
                        }, promptRequest.CallerId);
                    }

                    tcs.TrySetResult(success);
                }
                catch (Exception ex)
                {
                    // Exception during processing - broadcast error to the caller's SSE clients
                    BroadcastSseEvent(SseEventType.Error, new RemoteSseError
                    {
                        Code = "EXCEPTION",
                        Message = ex.Message,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        CallerId = promptRequest.CallerId
                    }, promptRequest.CallerId);

                    tcs.TrySetException(ex);
                }
            });

            // Respond immediately to acknowledge the request (don't wait for AI completion)
            // The client will receive the actual response via SSE stream
            await WriteJsonResponse(response, 200, new RemoteOperationResult { Success = true });
        }

        /// <summary>
        /// Associates SSE clients with a CallerId.
        /// When a prompt is received, we look for SSE clients from the same remote IP
        /// that don't have an associated caller yet and associate them with this caller.
        /// </summary>
        private static void AssociateCallerWithClients(string callerId, IPEndPoint? remoteEndPoint)
        {
            if (string.IsNullOrEmpty(callerId)) return;

            foreach (var kvp in _sseClients)
            {
                var client = kvp.Value;

                // If already associated with this caller, nothing to do
                if (client.AssociatedCallerId == callerId)
                    continue;

                // If not associated with any caller, associate with this one
                // In the future, we could also match by IP address for better precision
                if (string.IsNullOrEmpty(client.AssociatedCallerId))
                {
                    client.AssociatedCallerId = callerId;
                    Log.Info($"{LogPrefix} Associated SSE client {kvp.Key} with caller '{callerId}'");
                }
            }
        }

        private static async Task HandleCancelAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var callerRequest = await ReadRequestBodyAsync<RemoteCallerRequest>(request) ?? new RemoteCallerRequest();

            Log.Info($"{LogPrefix} Received cancel from '{callerRequest.CallerId}'");

            var tcs = new TaskCompletionSource<bool>();

            MainThreadDispatcher.ExecuteOnMainThread(async () =>
            {
                try
                {
                    var caller = new CallerIdentity(callerRequest.CallerId);
                    await AgentBridgeManager.CancelCurrentOperationAsync(caller);
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            try
            {
                await tcs.Task;
                await WriteJsonResponse(response, 200, new RemoteOperationResult { Success = true });
            }
            catch (Exception ex)
            {
                await WriteJsonResponse(response, 500, new RemoteOperationResult { Success = false, Error = ex.Message });
            }
        }

        private static async Task HandleClearAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var callerRequest = await ReadRequestBodyAsync<RemoteCallerRequest>(request) ?? new RemoteCallerRequest();

            Log.Info($"{LogPrefix} Received clear from '{callerRequest.CallerId}'");

            var tcs = new TaskCompletionSource<bool>();

            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                try
                {
                    var caller = new CallerIdentity(callerRequest.CallerId);
                    AgentBridgeManager.ClearConversation(caller);
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            try
            {
                await tcs.Task;
                await WriteJsonResponse(response, 200, new RemoteOperationResult { Success = true });
            }
            catch (Exception ex)
            {
                await WriteJsonResponse(response, 500, new RemoteOperationResult { Success = false, Error = ex.Message });
            }
        }

        private static Task HandleStatusAsync(HttpListenerResponse response)
        {
            var status = new RemoteAgentStatus();

            // Read state — this should be thread-safe as these are simple reads
            try
            {
                status.IsProcessing = MainThreadDispatcher.ExecuteOnMainThreadSync(() => AgentBridgeManager.IsProcessing());
                status.HasActiveSession = MainThreadDispatcher.ExecuteOnMainThreadSync(() => AgentBridgeManager.HasActiveSession());
                status.ServiceName = MainThreadDispatcher.ExecuteOnMainThreadSync(() => AgentBridgeManager.GetCurrentServiceName());
                status.LastError = MainThreadDispatcher.ExecuteOnMainThreadSync(() => AgentBridgeManager.GetLastError());
                status.ConnectedClients = _sseClients.Count;
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogPrefix} Error reading status: {ex.Message}");
            }

            return WriteJsonResponse(response, 200, status);
        }

        #endregion

        #region Server-Sent Events (SSE)

        private static async Task HandleSseConnectionAsync(HttpListenerContext context, CancellationToken ct)
        {
            var response = context.Response;
            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");
            AddCorsHeaders(context.Request, response, "GET");

            var clientId = Guid.NewGuid();
            var client = new SseClient(response);
            _sseClients.TryAdd(clientId, client);

            Log.Info($"{LogPrefix} SSE client connected: {clientId} (total: {_sseClients.Count})");

            try
            {
                // Send initial connection confirmation
                await client.SendEventAsync(SseEventType.Connected, new RemoteSseConnected { ClientId = clientId.ToString() });

                // Keep the connection alive with periodic pings
                while (!ct.IsCancellationRequested && !client.IsDisconnected)
                {
                    await Task.Delay(SsePingIntervalMs, ct);
                    await client.SendEventAsync(SseEventType.Ping, new RemoteSsePing { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                Log.Info($"{LogPrefix} SSE client disconnected: {clientId} ({ex.Message})");
            }
            finally
            {
                _sseClients.TryRemove(clientId, out _);
                client.Close();
                Log.Info($"{LogPrefix} SSE client removed: {clientId} (remaining: {_sseClients.Count})");
            }
        }

        private static void BroadcastSseEvent(SseEventType eventType, object data, string? callerId = null)
        {
            if (_sseClients.IsEmpty) return;

            var deadClients = new List<Guid>();

            foreach (var kvp in _sseClients)
            {
                try
                {
                    // If callerId is specified, only send to clients associated with that caller
                    // If client has no associated caller yet, still send (they may be establishing connection)
                    if (!string.IsNullOrEmpty(callerId) &&
                        !string.IsNullOrEmpty(kvp.Value.AssociatedCallerId) &&
                        kvp.Value.AssociatedCallerId != callerId)
                    {
                        continue;
                    }

                    // Fire-and-forget broadcast to each client
                    _ = kvp.Value.SendEventAsync(eventType, data);
                }
                catch
                {
                    deadClients.Add(kvp.Key);
                }
            }

            // Clean up dead clients
            foreach (var id in deadClients)
            {
                if (_sseClients.TryRemove(id, out var client))
                {
                    client.Close();
                }
            }
        }

        #endregion

        #region ConversationEventBroker Handlers

        private static void OnMessageAdded(ConversationMessage message)
        {
            // Filter by CallerId if the message has one
            BroadcastSseEvent(SseEventType.Message, message, message.CallerId);
        }

        private static void OnProcessingStateChanged(bool isProcessing)
        {
            // Processing state changes are broadcast to all clients (no caller filtering)
            // In future, we could add caller tracking for state changes too
            BroadcastSseEvent(SseEventType.Status, new RemoteProcessingState { IsProcessing = isProcessing });
        }

        private static void OnConversationCleared()
        {
            // Cleared events are broadcast to all clients (no caller filtering)
            // Each client should react to their own clear commands
            BroadcastSseEvent(SseEventType.Cleared, new RemoteSseCleared { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });
        }

        #endregion

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
                response.Headers.Add("Access-Control-Allow-Origin", origin);
                response.Headers.Add("Access-Control-Allow-Methods", methods);
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
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

        #region Helpers

        /// <summary>
        /// Validates the access token from the request's Authorization header.
        /// Expected format: "Bearer {token}"
        /// </summary>
        /// <param name="request">The HTTP request to validate.</param>
        /// <returns>True if the token is valid, false otherwise.</returns>
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
                // This can happen if server was started before token was ensured
                return false;
            }

            return string.Equals(providedToken, expectedToken, StringComparison.Ordinal);
        }

        private static async Task<T?> ReadRequestBodyAsync<T>(HttpListenerRequest request) where T : class
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var bodyString = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(bodyString);
            }
            catch
            {
                return null;
            }
        }

        private static async Task WriteJsonResponse(HttpListenerResponse response, int statusCode, object data)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";

            var json = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;

            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        #endregion

        #region SSE Client

        /// <summary>
        /// Represents a single SSE client connection.
        /// Handles serializing and writing SSE-formatted events to the response stream.
        /// </summary>
        private class SseClient
        {
            private readonly HttpListenerResponse _response;
            private readonly object _writeLock = new();
            private bool _isDisconnected;

            public bool IsDisconnected => _isDisconnected;

            /// <summary>
            /// The CallerId associated with this SSE client.
            /// Set when the client sends their first prompt request.
            /// Null until associated with a caller.
            /// </summary>
            public string? AssociatedCallerId { get; set; }

            public SseClient(HttpListenerResponse response)
            {
                _response = response;
            }

            public Task SendEventAsync(SseEventType eventType, object data)
            {
                if (_isDisconnected) return Task.CompletedTask;

                try
                {
                    var json = JsonConvert.SerializeObject(data);
                    var ssePayload = $"event: {SseEvent.ToWireName(eventType)}\ndata: {json}\n\n";
                    var bytes = Encoding.UTF8.GetBytes(ssePayload);

                    lock (_writeLock)
                    {
                        if (_isDisconnected) return Task.CompletedTask;
                        _response.OutputStream.Write(bytes, 0, bytes.Length);
                        _response.OutputStream.Flush();
                    }
                }
                catch
                {
                    _isDisconnected = true;
                }

                return Task.CompletedTask;
            }

            public void Close()
            {
                _isDisconnected = true;
                try { _response.Close(); } catch { /* ignored */ }
            }
        }

        #endregion
    }
}
