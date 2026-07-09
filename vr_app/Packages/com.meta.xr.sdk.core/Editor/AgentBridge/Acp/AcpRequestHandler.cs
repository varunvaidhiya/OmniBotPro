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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meta.XR.AI.AgentBridge.Acp
{
    /// <summary>
    /// Handles agent-initiated requests over the ACP (Agent Client Protocol) channel.
    ///
    /// ACP separates reasoning from execution: the agent (Gemini CLI) decides what to do,
    /// but the client (this Unity Editor process) actually performs file I/O, runs commands,
    /// and grants permissions. This is by design — ACP agents do not access the filesystem
    /// or spawn processes directly. Instead, they send JSON-RPC requests back to the client
    /// for these operations, giving the client control over what the agent can touch.
    ///
    /// In a terminal-based IDE (VS Code, JetBrains), the terminal handlers would manage
    /// visible terminal tabs. In Unity Editor, they run headless subprocesses — the agent
    /// doesn't know or care about the difference, it just sees command output.
    ///
    /// Permission requests are auto-approved since AgentBridge runs in a developer-controlled
    /// Unity Editor, not an end-user-facing application.
    /// </summary>
    internal class AcpRequestHandler : IDisposable
    {
        private readonly ConcurrentDictionary<string, Process> _terminals = new();
        private readonly ConcurrentDictionary<string, StringBuilder> _terminalOutputs = new();
        private bool _disposed;

        /// <summary>
        /// Thread-safe Log.Info that dispatches to the main thread.
        /// </summary>
        private static void LogInfo(string message)
        {
            MainThreadDispatcher.ExecuteOnMainThread(() => Log.Info(message));
        }

        /// <summary>
        /// Thread-safe Log.Warning that dispatches to the main thread.
        /// </summary>
        private static void LogWarning(string message)
        {
            MainThreadDispatcher.ExecuteOnMainThread(() => Log.Warning(message));
        }

        /// <summary>
        /// Route an incoming agent request to the appropriate handler.
        /// </summary>
        /// <param name="method">The JSON-RPC method name</param>
        /// <param name="paramsToken">The params JToken from the request</param>
        /// <returns>The result object to serialize back as the response</returns>
        public Task<object> HandleRequestAsync(string method, JToken? paramsToken)
        {
            return method switch
            {
                // File I/O: ACP agents delegate filesystem access to the client rather than
                // reading/writing files directly. This lets the client enforce access control
                // and keeps the agent sandboxed.
                "fs/read_text_file" => Task.FromResult(HandleReadTextFile(paramsToken)),
                "fs/write_text_file" => Task.FromResult(HandleWriteTextFile(paramsToken)),

                // Permissions: ACP agents ask the client before performing sensitive actions.
                // Auto-approved here since we're in a developer-controlled Unity Editor.
                "session/request_permission" => Task.FromResult(HandleRequestPermission(paramsToken)),

                // Terminal: ACP agents request command execution via the client. In IDE contexts
                // these map to terminal tabs; in Unity Editor they run as headless subprocesses.
                "terminal/create" => Task.FromResult(HandleCreateTerminal(paramsToken)),
                "terminal/output" => Task.FromResult(HandleTerminalOutput(paramsToken)),
                "terminal/kill" => Task.FromResult(HandleKillTerminal(paramsToken)),
                "terminal/release" => Task.FromResult(HandleReleaseTerminal(paramsToken)),
                "terminal/wait_for_exit" => HandleWaitForTerminalExitAsync(paramsToken),

                _ => throw new InvalidOperationException($"Unknown agent request method: {method}")
            };
        }

        private object HandleReadTextFile(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<ReadTextFileParams>()
                ?? throw new ArgumentException("Missing params for fs/read_text_file");

            var content = File.ReadAllText(p.Path);
            return new ReadTextFileResult { Content = content };
        }

        private object HandleWriteTextFile(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<WriteTextFileParams>()
                ?? throw new ArgumentException("Missing params for fs/write_text_file");

            // Ensure parent directory exists
            var dir = Path.GetDirectoryName(p.Path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(p.Path, p.Content);
            return new JObject();
        }

        private object HandleRequestPermission(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<RequestPermissionParams>()
                ?? throw new ArgumentException("Missing params for session/request_permission");

            // Auto-approve: find the first "allow" style option, or just pick the first one
            // TODO: T253939468 properly implement permission requests when we have valid UI for it
            var optionId = FindAllowOption(p.Options);

            return new RequestPermissionResult
            {
                Outcome = new PermissionOutcome
                {
                    OutcomeType = "selected",
                    OptionId = optionId
                }
            };
        }

        private object HandleCreateTerminal(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<CreateTerminalParams>()
                ?? throw new ArgumentException("Missing params for terminal/create");

            var terminalId = Guid.NewGuid().ToString("N");

            var psi = new ProcessStartInfo
            {
                FileName = p.Command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (p.Args != null)
            {
                foreach (var arg in p.Args)
                {
                    psi.ArgumentList.Add(arg);
                }
            }

            if (!string.IsNullOrEmpty(p.Cwd))
            {
                psi.WorkingDirectory = p.Cwd;
            }

            var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            _terminalOutputs[terminalId] = outputBuilder;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (outputBuilder)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (outputBuilder)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _terminals[terminalId] = process;
            LogInfo($"ACP: Created terminal {terminalId} for command: {p.Command}");

            return new CreateTerminalResult { TerminalId = terminalId };
        }

        private object HandleTerminalOutput(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<TerminalOutputParams>()
                ?? throw new ArgumentException("Missing params for terminal/output");

            if (!_terminals.TryGetValue(p.TerminalId, out var process))
            {
                throw new InvalidOperationException($"Unknown terminal: {p.TerminalId}");
            }

            string output;
            if (_terminalOutputs.TryGetValue(p.TerminalId, out var builder))
            {
                lock (builder)
                {
                    output = builder.ToString();
                    builder.Clear();
                }
            }
            else
            {
                output = "";
            }

            var result = new TerminalOutputResult
            {
                Output = output,
                Truncated = false
            };

            if (process.HasExited)
            {
                result.ExitStatus = new ExitStatus
                {
                    Type = "exited",
                    Code = process.ExitCode
                };
            }

            return result;
        }

        private object HandleKillTerminal(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<KillTerminalParams>()
                ?? throw new ArgumentException("Missing params for terminal/kill");

            if (_terminals.TryGetValue(p.TerminalId, out var process))
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"ACP: Error killing terminal {p.TerminalId}: {ex.Message}");
                }
            }

            return new JObject();
        }

        private object HandleReleaseTerminal(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<ReleaseTerminalParams>()
                ?? throw new ArgumentException("Missing params for terminal/release");

            if (_terminals.TryRemove(p.TerminalId, out var process))
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    LogWarning($"ACP: Error releasing terminal {p.TerminalId}: {ex.Message}");
                }
            }

            _terminalOutputs.TryRemove(p.TerminalId, out _);
            return new JObject();
        }

        private async Task<object> HandleWaitForTerminalExitAsync(JToken? paramsToken)
        {
            var p = paramsToken?.ToObject<WaitForTerminalExitParams>()
                ?? throw new ArgumentException("Missing params for terminal/wait_for_exit");

            if (!_terminals.TryGetValue(p.TerminalId, out var process))
            {
                throw new InvalidOperationException($"Unknown terminal: {p.TerminalId}");
            }

            var timeoutMs = p.TimeoutMs ?? 30000;
            var completed = await Task.Run(() => process.WaitForExit(timeoutMs));

            var result = new WaitForTerminalExitResult { TimedOut = !completed };
            if (completed)
            {
                result.ExitStatus = new ExitStatus
                {
                    Type = "exited",
                    Code = process.ExitCode
                };
            }

            return result;
        }

        /// <summary>
        /// Find the best "allow" option from permission options. Prefers options whose
        /// id or label contains "allow", "yes", or "approve". Falls back to first option.
        /// </summary>
        private static string FindAllowOption(List<PermissionOption> options)
        {
            if (options.Count == 0)
            {
                return "";
            }

            // Look for an explicit allow/approve option
            var allowKeywords = new[] { "allow", "yes", "approve", "accept", "always" };
            foreach (var option in options)
            {
                var idLower = option.Id.ToLowerInvariant();
                var labelLower = option.Label.ToLowerInvariant();
                if (allowKeywords.Any(k => idLower.Contains(k) || labelLower.Contains(k)))
                {
                    return option.Id;
                }
            }

            // Fall back to first option
            return options[0].Id;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var kvp in _terminals)
            {
                try
                {
                    if (!kvp.Value.HasExited)
                    {
                        kvp.Value.Kill();
                    }
                    kvp.Value.Dispose();
                }
                catch { }
            }

            _terminals.Clear();
            _terminalOutputs.Clear();
        }
    }
}
