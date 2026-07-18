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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Abstract base class for AI service implementations.
    /// Provides common functionality and implements the IAIService interface.
    /// </summary>
    public abstract class AIServiceBase : IAIService
    {
        private bool _disposed;
        private string? _resolvedExecutablePath;

        private static string? _loginShellPath;
        private static readonly object _shellPathLock = new();

        // Abstract interface members that must be implemented by derived classes
        public abstract string ServiceName { get; }
        public abstract bool HasActiveSession { get; }
        public abstract Task ProcessUserInputAsync(string userInput, CallerIdentity? caller, List<ImageAttachment>? images = null, System.Threading.CancellationToken cancellationToken = default, string? systemPrompt = null);
        public abstract void ClearSession();
        public abstract Task CancelCurrentOperationAsync();

        #region Shell Environment

        /// <summary>
        /// Get the login shell PATH for spawning subprocesses.
        /// On macOS, Unity launched from Finder/Spotlight doesn't inherit the user's shell PATH,
        /// so CLI tools and auth helpers (e.g. apiKeyHelper) aren't found.
        /// Cached globally across all service instances. No-op on Windows.
        /// </summary>
        protected static string? GetLoginShellPath()
        {
#if !UNITY_EDITOR_OSX
            return null;
#else
            if (_loginShellPath != null)
            {
                return _loginShellPath;
            }

            lock (_shellPathLock)
            {
                if (_loginShellPath != null)
                {
                    return _loginShellPath;
                }

                try
                {
                    var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";

                    var psi = new ProcessStartInfo
                    {
                        FileName = shell,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add("-l");
                    psi.ArgumentList.Add("-c");
                    psi.ArgumentList.Add("echo $PATH");

                    using var process = new Process { StartInfo = psi };
                    process.Start();

                    var output = process.StandardOutput.ReadToEnd().Trim();
                    if (!process.WaitForExit(5000))
                    {
                        try { process.Kill(); }
                        catch (InvalidOperationException) { }
                        return null;
                    }

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        _loginShellPath = output;
                        Log.Info($"Resolved login shell PATH ({output.Split(':').Length} entries)");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to resolve login shell PATH: {ex.Message}");
                }

                return _loginShellPath;
            }
#endif
        }

        /// <summary>
        /// Apply the login shell PATH to a ProcessStartInfo so the spawned process
        /// can find CLI tools and auth helpers.
        /// </summary>
        internal static void ApplyLoginShellPath(ProcessStartInfo psi)
        {
            var path = GetLoginShellPath();
            if (!string.IsNullOrEmpty(path))
            {
                psi.Environment["PATH"] = path;
            }
        }

        /// <summary>
        /// Resolve a CLI executable, checking the configured path first, then the login shell,
        /// then falling back to the bare name. Caches the resolved path per instance.
        /// </summary>
        /// <param name="configuredPath">User-configured executable path (empty = auto-resolve)</param>
        /// <param name="executableName">Bare executable name (e.g. "claude", "gemini")</param>
        protected string ResolveExecutable(string? configuredPath, string executableName)
        {
            if (!string.IsNullOrEmpty(configuredPath))
            {
                return configuredPath!;
            }

            if (_resolvedExecutablePath is { } cached)
            {
                return cached;
            }

            var resolved = ResolveExecutableViaShell(executableName);
            if (resolved != null)
            {
                Log.Info($"Resolved {executableName} executable via shell: {resolved}");
                _resolvedExecutablePath = resolved;
                return resolved;
            }

            return executableName;
        }

        /// <summary>
        /// Clear the cached resolved executable path (e.g. when settings are reset).
        /// </summary>
        protected void ClearResolvedExecutablePath()
        {
            _resolvedExecutablePath = null;
        }

        internal static string? ResolveExecutableViaShell(string executableName)
        {
#if !UNITY_EDITOR_OSX
            return null;
#else
            try
            {
                var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";

                var psi = new ProcessStartInfo
                {
                    FileName = shell,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("-l");
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add($"which {executableName}");

                using var process = new Process { StartInfo = psi };
                process.Start();

                var output = process.StandardOutput.ReadToEnd().Trim();
                if (!process.WaitForExit(5000))
                {
                    try { process.Kill(); }
                    catch (InvalidOperationException) { }
                    return null;
                }

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && File.Exists(output))
                {
                    return output;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to resolve executable path for '{executableName}': {ex.Message}");
            }

            return null;
#endif
        }

        #endregion

        /// <summary>
        /// Dispose of the service and release all managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation.
        /// Derived classes should override this method to dispose their own resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose managed resources here
                // Derived classes should override this method to dispose their resources
            }

            _disposed = true;
        }
    }
}
