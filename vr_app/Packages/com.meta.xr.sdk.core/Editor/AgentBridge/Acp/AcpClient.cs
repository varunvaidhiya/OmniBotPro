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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meta.XR.AI.AgentBridge.Acp
{
    /// <summary>
    /// Bidirectional JSON-RPC 2.0 client over stdio of a child process.
    /// Handles request/response correlation, streaming notifications, and agent-initiated requests.
    /// </summary>
    internal class AcpClient : IDisposable
    {
        private Process? _process;
        private readonly object _processLock = new();
        private readonly string _executablePath;
        private readonly string? _environmentPath;
        private readonly string _acpFlag;

        // Request correlation
        private int _nextRequestId;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JToken?>> _pendingRequests = new();

        // Agent request handling
        private readonly AcpRequestHandler _requestHandler = new();

        // Receive loop
        private Thread? _receiveThread;
        private volatile bool _stopping;

        // Stderr capture for error diagnostics
        private readonly StringBuilder _stderrBuffer = new();
        private readonly object _stderrLock = new();

        // Serialization settings
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Thread-safe Log.Info that dispatches to the main thread.
        /// Log.Info accesses EditorPrefs which is main-thread-only in Unity.
        /// </summary>
        private static void LogInfo(string message)
        {
            MainThreadDispatcher.ExecuteOnMainThread(() => Log.Info(message));
        }

        /// <summary>
        /// Thread-safe Log.Warning that dispatches to the main thread.
        /// Log.Warning accesses EditorPrefs which is main-thread-only in Unity.
        /// </summary>
        private static void LogWarning(string message)
        {
            MainThreadDispatcher.ExecuteOnMainThread(() => Log.Warning(message));
        }

        /// <summary>
        /// Fired when a session/update notification is received from the agent.
        /// </summary>
        public event Action<SessionUpdateParams>? OnSessionUpdate;

        /// <summary>
        /// Whether the underlying process is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_processLock)
                {
                    return _process != null && !_process.HasExited;
                }
            }
        }

        public AcpClient(string executablePath, string? environmentPath = null, string acpFlag = "--acp")
        {
            _executablePath = executablePath;
            _environmentPath = environmentPath;
            _acpFlag = acpFlag;
        }

        /// <summary>
        /// Start the ACP subprocess and perform the initialize handshake.
        /// </summary>
        public async Task<InitializeResult> InitializeAsync(string clientName, string clientVersion)
        {
            StartProcess();

            var initParams = new InitializeParams
            {
                ProtocolVersion = 1,
                ClientCapabilities = new ClientCapabilities { Streaming = true },
                ClientInfo = new ClientInfo { Name = clientName, Version = clientVersion }
            };

            var resultToken = await SendRequestAsync("initialize", initParams);
            return resultToken?.ToObject<InitializeResult>() ?? new InitializeResult();
        }

        /// <summary>
        /// Create a new session.
        /// </summary>
        public async Task<NewSessionResult> NewSessionAsync(string cwd)
        {
            var resultToken = await SendRequestAsync("session/new", new NewSessionParams { Cwd = cwd });
            return resultToken?.ToObject<NewSessionResult>() ?? new NewSessionResult();
        }

        /// <summary>
        /// Send a prompt to the agent. Returns when the agent finishes processing.
        /// Streaming updates arrive via OnSessionUpdate during execution.
        /// </summary>
        public async Task<PromptResult> PromptAsync(string sessionId, List<ContentBlock> prompt)
        {
            var resultToken = await SendRequestAsync("session/prompt",
                new PromptParams { SessionId = sessionId, Prompt = prompt });
            return resultToken?.ToObject<PromptResult>() ?? new PromptResult();
        }

        /// <summary>
        /// Send a cancel notification to the agent.
        /// </summary>
        public void Cancel(string sessionId)
        {
            SendNotification("session/cancel", new CancelNotificationParams { SessionId = sessionId });
        }

        /// <summary>
        /// Send a JSON-RPC request and wait for the correlated response.
        /// </summary>
        private Task<JToken?> SendRequestAsync(string method, object? @params)
        {
            var id = Interlocked.Increment(ref _nextRequestId);
            var tcs = new TaskCompletionSource<JToken?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[id] = tcs;

            var request = new JsonRpcRequest
            {
                Id = id,
                Method = method,
                Params = @params
            };

            var json = JsonConvert.SerializeObject(request, SerializerSettings);
            SendLine(json);

            return tcs.Task;
        }

        /// <summary>
        /// Send a JSON-RPC notification (no response expected).
        /// </summary>
        private void SendNotification(string method, object? @params)
        {
            var notification = new { jsonrpc = "2.0", method, @params };
            var json = JsonConvert.SerializeObject(notification, SerializerSettings);
            SendLine(json);
        }

        /// <summary>
        /// Thread-safe write of a single line to the process stdin.
        /// </summary>
        private void SendLine(string json)
        {
            lock (_processLock)
            {
                if (_process == null || _process.HasExited)
                {
                    throw new InvalidOperationException("ACP process is not running");
                }

                _process.StandardInput.WriteLine(json);
                _process.StandardInput.Flush();
            }
        }

        private void StartProcess()
        {
            lock (_processLock)
            {
                if (_process != null)
                {
                    throw new InvalidOperationException("ACP process already started");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = _executablePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                psi.ArgumentList.Add(_acpFlag);

                // Set the login shell PATH so dependencies like dotslash are found
                if (!string.IsNullOrEmpty(_environmentPath))
                {
                    psi.Environment["PATH"] = _environmentPath;
                }

                _process = new Process { StartInfo = psi };
                _stopping = false;

                _process.Start();

                // Start background receive loop
                _receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "AcpClient-ReceiveLoop"
                };
                _receiveThread.Start();

                // Start stderr reader for logging
                var stderrThread = new Thread(StderrLoop)
                {
                    IsBackground = true,
                    Name = "AcpClient-StderrLoop"
                };
                stderrThread.Start();
            }
        }

        /// <summary>
        /// Background loop that reads stdout lines and dispatches them.
        /// </summary>
        private void ReceiveLoop()
        {
            try
            {
                StreamReader? reader;
                lock (_processLock)
                {
                    reader = _process?.StandardOutput;
                }

                if (reader == null) return;

                while (!_stopping)
                {
                    string? line;
                    try
                    {
                        line = reader.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        if (!_stopping)
                        {
                            LogWarning($"ACP: ReadLine error in receive loop: {ex.Message}");
                        }
                        break;
                    }

                    if (line == null)
                    {
                        // EOF — process exited
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    try
                    {
                        DispatchMessage(line);
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"ACP: Error dispatching message: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_stopping)
                {
                    LogWarning($"ACP: Receive loop error: {ex.Message}");
                }
            }
            finally
            {
                // On disconnect, fail all pending requests
                FailAllPendingRequests("ACP process disconnected");
            }
        }

        /// <summary>
        /// Background loop that reads stderr for logging.
        /// </summary>
        private void StderrLoop()
        {
            try
            {
                StreamReader? reader;
                lock (_processLock)
                {
                    reader = _process?.StandardError;
                }

                if (reader == null) return;

                while (!_stopping)
                {
                    string? line;
                    try
                    {
                        line = reader.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        if (!_stopping)
                        {
                            LogWarning($"ACP: ReadLine error in stderr loop: {ex.Message}");
                        }
                        break;
                    }

                    if (line == null) break;
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lock (_stderrLock)
                        {
                            _stderrBuffer.AppendLine(line);
                        }
                        LogWarning($"Gemini CLI stderr: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_stopping)
                {
                    LogWarning($"ACP: Stderr loop error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Parse a JSON line and dispatch it as response, notification, or agent request.
        /// </summary>
        private void DispatchMessage(string line)
        {
            var jo = JObject.Parse(line);

            // Is this a response? (has "id" and either "result" or "error")
            if (jo["id"] != null && (jo["result"] != null || jo["error"] != null))
            {
                HandleResponse(jo);
                return;
            }

            // Is this a request from the agent? (has "id" and "method")
            if (jo["id"] != null && jo["method"] != null)
            {
                HandleAgentRequest(jo);
                return;
            }

            // Is this a notification? (has "method" but no "id")
            if (jo["method"] != null && jo["id"] == null)
            {
                HandleNotification(jo);
                return;
            }

            LogWarning($"ACP: Unknown message format: {line}");
        }

        private void HandleResponse(JObject jo)
        {
            var id = jo["id"]?.ToObject<int>() ?? -1;
            if (!_pendingRequests.TryRemove(id, out var tcs))
            {
                LogWarning($"ACP: Received response for unknown request ID: {id}");
                return;
            }

            var error = jo["error"];
            if (error != null)
            {
                var errorObj = error.ToObject<JsonRpcError>();
                tcs.SetException(new AcpException(
                    errorObj?.Code ?? -1,
                    errorObj?.Message ?? "Unknown ACP error"));
            }
            else
            {
                tcs.SetResult(jo["result"]);
            }
        }

        private void HandleAgentRequest(JObject jo)
        {
            var id = jo["id"]?.ToObject<int>() ?? -1;
            var method = jo["method"]?.ToString() ?? "";
            var @params = jo["params"];

            // Handle asynchronously to avoid blocking the receive loop
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _requestHandler.HandleRequestAsync(method, @params);
                    SendResponseSuccess(id, result);
                }
                catch (Exception ex)
                {
                    LogWarning($"ACP: Error handling agent request '{method}': {ex.Message}");
                    SendResponseError(id, -32603, ex.Message);
                }
            });
        }

        private void HandleNotification(JObject jo)
        {
            var method = jo["method"]?.ToString() ?? "";
            var @params = jo["params"];

            if (method == "session/update")
            {
                var updateParams = @params?.ToObject<SessionUpdateParams>();
                if (updateParams != null)
                {
                    OnSessionUpdate?.Invoke(updateParams);
                }
            }
            else
            {
                LogInfo($"ACP: Received notification: {method}");
            }
        }

        private void SendResponseSuccess(int id, object result)
        {
            var response = new { jsonrpc = "2.0", id, result };
            var json = JsonConvert.SerializeObject(response, SerializerSettings);

            try
            {
                SendLine(json);
            }
            catch (Exception ex)
            {
                LogWarning($"ACP: Error sending response for request {id}: {ex.Message}");
            }
        }

        private void SendResponseError(int id, int code, string message)
        {
            var response = new
            {
                jsonrpc = "2.0",
                id,
                error = new { code, message }
            };
            var json = JsonConvert.SerializeObject(response, SerializerSettings);

            try
            {
                SendLine(json);
            }
            catch (Exception ex)
            {
                LogWarning($"ACP: Error sending error response for request {id}: {ex.Message}");
            }
        }

        private void FailAllPendingRequests(string reason)
        {
            // Include stderr output and exit code for better diagnostics
            var diagnosticInfo = reason;
            lock (_processLock)
            {
                if (_process != null)
                {
                    try
                    {
                        if (_process.HasExited)
                        {
                            diagnosticInfo += $" (exit code: {_process.ExitCode})";
                        }
                    }
                    catch { }
                }
            }

            string stderr;
            lock (_stderrLock)
            {
                stderr = _stderrBuffer.ToString().Trim();
            }
            if (!string.IsNullOrEmpty(stderr))
            {
                diagnosticInfo += $"\nStderr: {stderr}";
            }

            foreach (var kvp in _pendingRequests.ToArray())
            {
                _pendingRequests.TryRemove(kvp.Key, out _);
                kvp.Value.TrySetException(new AcpException(-1, diagnosticInfo));
            }
        }

        public void Dispose()
        {
            _stopping = true;

            lock (_processLock)
            {
                if (_process != null)
                {
                    try
                    {
                        if (!_process.HasExited)
                        {
                            // Try graceful stdin close first
                            try
                            {
                                _process.StandardInput.Close();
                            }
                            catch { }

                            // Give a brief moment to exit
                            if (!_process.WaitForExit(2000))
                            {
                                _process.Kill();
                            }
                        }

                        _process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"ACP: Error disposing process: {ex.Message}");
                    }

                    _process = null;
                }
            }

            FailAllPendingRequests("ACP client disposed");
            _requestHandler.Dispose();
        }
    }

    /// <summary>
    /// Exception representing an ACP protocol error.
    /// </summary>
    internal class AcpException : Exception
    {
        public int Code { get; }

        public AcpException(int code, string message) : base(message)
        {
            Code = code;
        }
    }
}
