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
using System.Threading.Tasks;
using Meta.XR.AI.AgentBridge;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Status of an MCP registration with an AI agent CLI.
    /// </summary>
    internal enum McpRegistrationStatus
    {
        Checking,
        Unknown,
        NotInstalled,
        NotRegistered,
        Registered
    }

    /// <summary>
    /// Static utility for registering/unregistering the MCPBridge HTTP server
    /// with AI agent CLIs (Claude Code, DevMate). No UI — just the registration commands.
    /// </summary>
    internal static class McpRegistration
    {
        private static McpRegistrationStatus _claudeCodeStatus = McpRegistrationStatus.Checking;
        private static bool _isCheckingClaudeCode;
        private static int _checkedPort;

        // Cache resolved executable paths so we only shell out to `which` once per session
        private static readonly System.Collections.Generic.Dictionary<string, string> _resolvedPaths = new();

        /// <summary>
        /// Resolve a CLI executable name to its full path via login shell.
        /// Falls back to the bare name if resolution fails.
        /// </summary>
        private static string ResolveExecutable(string executableName)
        {
            if (_resolvedPaths.TryGetValue(executableName, out var cached))
                return cached;

            var resolved = AIServiceBase.ResolveExecutableViaShell(executableName) ?? executableName;
            _resolvedPaths[executableName] = resolved;
            return resolved;
        }

        /// <summary>
        /// Gets the cached Claude Code status and triggers an async refresh if needed.
        /// </summary>
        internal static McpRegistrationStatus GetClaudeCodeStatus(int port)
        {
            if (_claudeCodeStatus == McpRegistrationStatus.Checking && !_isCheckingClaudeCode)
            {
                RefreshClaudeCodeStatusAsync(port);
            }
            else if (_checkedPort != port && _claudeCodeStatus == McpRegistrationStatus.Registered)
            {
                // Port changed, need to re-check
                RefreshClaudeCodeStatusAsync(port);
            }
            return _claudeCodeStatus;
        }

        /// <summary>
        /// Forces a refresh of the Claude Code status check.
        /// </summary>
        internal static void RefreshClaudeCodeStatus(int port)
        {
            _claudeCodeStatus = McpRegistrationStatus.Checking;
            RefreshClaudeCodeStatusAsync(port);
        }

        private static async void RefreshClaudeCodeStatusAsync(int port)
        {
            if (_isCheckingClaudeCode)
                return;

            _isCheckingClaudeCode = true;
            _checkedPort = port;

            try
            {
                var status = await Task.Run(() => CheckClaudeCodeStatusSync(port));
                _claudeCodeStatus = status;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpRegistration] Error checking Claude Code status: {ex.Message}");
                _claudeCodeStatus = McpRegistrationStatus.Unknown;
            }
            finally
            {
                _isCheckingClaudeCode = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private static McpRegistrationStatus CheckClaudeCodeStatusSync(int port)
        {
            // Fast path: read ~/.claude.json directly instead of spawning `claude mcp list`
            // (which does health checks on every registered server and is very slow).
            try
            {
                var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                var configPath = System.IO.Path.Combine(home, ".claude.json");
                if (System.IO.File.Exists(configPath))
                {
                    var json = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(configPath));
                    // Normalize path to forward slashes to match .claude.json format (cross-platform)
                    // Windows: C:\foo\bar -> C:/foo/bar; Mac/Linux: /foo/bar -> /foo/bar (no-op)
                    var projectPath = ProjectDirectory.Replace('\\', '/');
                    var mcpServers = json["projects"]?[projectPath]?["mcpServers"];
                    if (mcpServers == null)
                    {
                        // Also check the top-level mcpServers (user scope)
                        mcpServers = json["mcpServers"];
                    }

                    if (mcpServers?[McpName] != null)
                    {
                        var url = mcpServers[McpName]?["url"]?.ToString();
                        if (url == ServerUrl(port))
                            return McpRegistrationStatus.Registered;
                    }

                    return McpRegistrationStatus.NotRegistered;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[McpRegistration] Could not read Claude config: {ex.Message}");
            }

            // Fallback: check if claude CLI is installed
            var resolved = ResolveExecutable("claude");
            if (resolved == "claude" || !System.IO.File.Exists(resolved))
                return McpRegistrationStatus.NotInstalled;

            return McpRegistrationStatus.NotRegistered;
        }

        private static string ServerUrl(int port) =>
            $"http://localhost:{port}/mcpbridge/";

        private const string McpName = "meta-xr-unity-runtime";

        internal static async void RegisterWithClaudeCodeAsync(int port)
        {
            _claudeCodeStatus = McpRegistrationStatus.Checking;
            _isCheckingClaudeCode = true;
            try
            {
                var token = McpBridgeSettings.EnsureToken();
                var result = await Task.Run(() =>
                    RunProcessWithOutput("claude", $"mcp add {McpName} --transport http {ServerUrl(port)} --header \"Authorization:Bearer {token}\""));

                // If "already exists" message appears (even if CLI returns success), remove and re-add
                if (result.error.Contains("already exists") || result.output.Contains("already exists"))
                {
                    await Task.Run(() => RunProcess("claude", $"mcp remove {McpName}"));
                    await Task.Run(() =>
                        RunProcessWithOutput("claude", $"mcp add {McpName} --transport http {ServerUrl(port)} --header \"Authorization:Bearer {token}\""));
                }

                _claudeCodeStatus = await Task.Run(() => CheckClaudeCodeStatusSync(port));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpRegistration] Error registering with Claude Code: {ex.Message}");
                _claudeCodeStatus = McpRegistrationStatus.Unknown;
            }
            finally
            {
                _isCheckingClaudeCode = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        internal static async void UnregisterFromClaudeCodeAsync(int port)
        {
            _claudeCodeStatus = McpRegistrationStatus.Checking;
            _isCheckingClaudeCode = true;
            try
            {
                await Task.Run(() =>
                    RunProcess("claude", $"mcp remove {McpName}"));
                _claudeCodeStatus = await Task.Run(() => CheckClaudeCodeStatusSync(port));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpRegistration] Error unregistering from Claude Code: {ex.Message}");
                _claudeCodeStatus = McpRegistrationStatus.Unknown;
            }
            finally
            {
                _isCheckingClaudeCode = false;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        internal static bool RegisterWithDevMate(int port)
        {
            var token = McpBridgeSettings.EnsureToken();
            var result = RunProcessWithOutput("devmate", $"mcp add {McpName} --transport http {ServerUrl(port)} --header \"Authorization:Bearer {token}\"");

            // If failed due to "already exists", remove and re-add
            if (!result.success && (result.error.Contains("already exists") || result.output.Contains("already exists")))
            {
                RunProcess("devmate", $"mcp remove {McpName}");
                return RunProcess("devmate", $"mcp add {McpName} --transport http {ServerUrl(port)} --header \"Authorization:Bearer {token}\"");
            }

            return result.success;
        }

        internal static bool UnregisterFromDevMate()
        {
            return RunProcess("devmate", $"mcp remove {McpName}");
        }

        /// <summary>
        /// Unity project directory — used as WorkingDirectory so that CLI tools
        /// (claude, devmate) resolve project-scoped config consistently.
        /// </summary>
        private static string ProjectDirectory => System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);

        private static (bool success, string output, string error) RunProcessWithOutput(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ResolveExecutable(fileName),
                    Arguments = arguments,
                    WorkingDirectory = ProjectDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                AIServiceBase.ApplyLoginShellPath(psi);

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return (false, "", "Failed to start process");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit(10000);

                return (process.ExitCode == 0, output, error);
            }
            catch (System.Exception ex)
            {
                return (false, "", ex.Message);
            }
        }

        private static bool RunProcess(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ResolveExecutable(fileName),
                    Arguments = arguments,
                    WorkingDirectory = ProjectDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                AIServiceBase.ApplyLoginShellPath(psi);

                using var process = Process.Start(psi);
                if (process == null)
                {
                    Debug.LogError($"[McpRegistration] Failed to start process: {fileName}");
                    return false;
                }

                process.WaitForExit(10000);

                if (process.ExitCode != 0)
                {
                    var stderr = process.StandardError.ReadToEnd();
                    Debug.LogError($"[McpRegistration] {fileName} {arguments} failed (exit {process.ExitCode}): {stderr}");
                    return false;
                }

                Debug.Log($"[McpRegistration] {fileName} {arguments} succeeded");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[McpRegistration] Error running {fileName}: {ex.Message}");
                return false;
            }
        }
    }
}
