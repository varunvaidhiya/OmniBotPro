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

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Compilation tools for MCP clients to monitor and control Unity script compilation.
    ///
    /// This tool enables AI agents like Devmate to autonomously verify code changes by:
    /// - Checking compilation status without launching a new Unity instance
    /// - Retrieving detailed compilation errors with file, line, and column info
    /// - Triggering recompilation after code edits
    /// - Waiting for compilation to complete with configurable timeout
    ///
    /// Key Features:
    /// - Real-time compilation status (compiling, error, clean)
    /// - Cached compilation errors from CompilationPipeline events
    /// - Async wait for compilation completion via TaskCompletionSource
    /// - Thread-safe main thread marshalling via SynchronizationContext
    ///
    /// Threading Model:
    /// Many Unity APIs (AssetDatabase.Refresh, CompilationPipeline.RequestScriptCompilation,
    /// EditorApplication.isCompiling) are main-thread-only. MCP tool calls arrive on
    /// background HTTP threads. We capture Unity's SynchronizationContext at initialization
    /// (runs on main thread) and use Post() to marshal calls back. Unlike EditorApplication.delayCall,
    /// SynchronizationContext.Post works even when the Editor window is unfocused.
    ///
    /// MCP Client Usage Pattern:
    /// 1. Edit .cs file
    /// 2. Call ForceRecompile() to trigger compilation
    /// 3. Call WaitForCompilation(timeout) to wait for result
    /// 4. If errors, call GetCompilationErrors() to get details, fix, repeat
    /// 5. If clean, proceed to run tests
    /// </summary>
    [Tool(
        "Tools for monitoring and controlling Unity script compilation.",
        "WHEN TO USE: After editing C# files, to verify compilation without launching a new Unity instance.",
        "WORKFLOW: 1) ForceRecompile() 2) WaitForCompilation() 3) If errors, GetCompilationErrors() and fix.",
        "IMPORTANT: Uses the already-running Editor - compilation takes 5-15 seconds, not minutes."
    )]
    internal class CompilationTools : SingletonService<CompilationTools>
    {
        private static readonly object _lock = new object();
        private static List<CompilerMessage> _cachedErrors = new List<CompilerMessage>();
        private static DateTime _lastCompileTime = DateTime.MinValue;
        private static bool _lastCompileHadErrors = false;
        private static TaskCompletionSource<CompilationResult> _compilationTcs;
        private static bool _initialized = false;

        // Captured on the main thread at initialization. Used to marshal calls from
        // background HTTP threads back to Unity's main thread. Unlike EditorApplication.delayCall,
        // SynchronizationContext.Post processes callbacks even when the Editor is unfocused.
        private static SynchronizationContext _mainThreadContext;

        /// <summary>
        /// Result of a compilation operation.
        /// </summary>
        internal class CompilationResult
        {
            public string Status { get; set; }
            public List<CompilationError> Errors { get; set; } = new List<CompilationError>();
            public double DurationSeconds { get; set; }
            public DateTime CompileTime { get; set; }
        }

        /// <summary>
        /// Detailed information about a compilation error.
        /// </summary>
        internal class CompilationError
        {
            public string File { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public string Code { get; set; }
            public string Message { get; set; }
            public string Severity { get; set; }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_initialized) return;

            _mainThreadContext = SynchronizationContext.Current;

            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            _initialized = true;
        }

        /// <summary>
        /// Posts a callback to the main thread via SynchronizationContext and returns the result asynchronously.
        /// Falls back to EditorApplication.delayCall if SynchronizationContext was not captured.
        /// </summary>
        private static Task<T> RunOnMainThread<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();

            if (_mainThreadContext != null)
            {
                _mainThreadContext.Post(_ =>
                {
                    try
                    {
                        tcs.TrySetResult(func());
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }, null);
            }
            else
            {
                // Fallback: delayCall works when editor is focused
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        tcs.TrySetResult(func());
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                };
            }

            return tcs.Task;
        }

        /// <summary>
        /// Posts a void callback to the main thread via SynchronizationContext.
        /// Falls back to EditorApplication.delayCall if SynchronizationContext was not captured.
        /// </summary>
        private static Task RunOnMainThread(Action action)
        {
            return RunOnMainThread(() =>
            {
                action();
                return true;
            });
        }

        private static void OnCompilationFinished(object context)
        {
            lock (_lock)
            {
                _lastCompileTime = DateTime.UtcNow;

                var result = new CompilationResult
                {
                    Status = _cachedErrors.Any(e => e.type == CompilerMessageType.Error) ? "error" : "clean",
                    Errors = _cachedErrors
                        .Where(e => e.type == CompilerMessageType.Error || e.type == CompilerMessageType.Warning)
                        .Select(e => new CompilationError
                        {
                            File = e.file,
                            Line = e.line,
                            Column = e.column,
                            Code = ExtractErrorCode(e.message),
                            Message = e.message,
                            Severity = e.type == CompilerMessageType.Error ? "error" : "warning"
                        })
                        .ToList(),
                    CompileTime = _lastCompileTime
                };

                _lastCompileHadErrors = result.Status == "error";

                if (_compilationTcs != null && !_compilationTcs.Task.IsCompleted)
                {
                    _compilationTcs.TrySetResult(result);
                }
            }
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            lock (_lock)
            {
                foreach (var message in messages)
                {
                    if (message.type == CompilerMessageType.Error || message.type == CompilerMessageType.Warning)
                    {
                        _cachedErrors.Add(message);
                    }
                }
            }
        }

        private static void OnAfterAssemblyReload()
        {
            lock (_lock)
            {
                _cachedErrors.Clear();
            }
        }

        private static string ExtractErrorCode(string message)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;

            var colonIndex = message.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 15)
            {
                var potentialCode = message.Substring(0, colonIndex).Trim();
                if (potentialCode.StartsWith("CS") || potentialCode.StartsWith("error CS"))
                {
                    return potentialCode.Replace("error ", "");
                }
            }
            return string.Empty;
        }

        [Tool(Description = "Get the current compilation status of the Unity project",
            Returns = "JSON object with status ('compiling', 'error', or 'clean'), lastCompileTime, and errorCount")]
        internal Task<object> GetCompilationStatus()
        {
            return RunOnMainThread<object>(() =>
            {
                lock (_lock)
                {
                    string status;
                    if (EditorApplication.isCompiling)
                    {
                        status = "compiling";
                    }
                    else if (_lastCompileHadErrors)
                    {
                        status = "error";
                    }
                    else
                    {
                        status = "clean";
                    }

                    var errorCount = _cachedErrors.Count(e => e.type == CompilerMessageType.Error);

                    return new
                    {
                        status,
                        lastCompileTime = _lastCompileTime == DateTime.MinValue ? null : _lastCompileTime.ToString("o"),
                        errorCount
                    };
                }
            });
        }

        [Tool(Description = "Get all compilation errors with file, line, column, and message details",
            Returns = "Array of error objects with file, line, column, code, message, and severity")]
        internal object GetCompilationErrors()
        {
            lock (_lock)
            {
                return _cachedErrors
                    .Where(e => e.type == CompilerMessageType.Error || e.type == CompilerMessageType.Warning)
                    .Select(e => new
                    {
                        file = e.file,
                        line = e.line,
                        column = e.column,
                        code = ExtractErrorCode(e.message),
                        message = e.message,
                        severity = e.type == CompilerMessageType.Error ? "error" : "warning"
                    })
                    .ToArray();
            }
        }

        [Tool(Description = "Force Unity to recompile all scripts. Refreshes the AssetDatabase to discover new/changed files, then triggers script compilation. Safe to call from any thread.",
            Returns = "JSON object confirming the recompile was triggered")]
        internal Task<object> ForceRecompile()
        {
            return RunOnMainThread<object>(() =>
            {
                lock (_lock)
                {
                    _cachedErrors.Clear();
                    _compilationTcs = new TaskCompletionSource<CompilationResult>();
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                CompilationPipeline.RequestScriptCompilation();

                return new
                {
                    triggered = true,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            });
        }

        [Tool(Description = "Wait for compilation to complete. Blocks until compilation finishes or timeout is reached.",
            Returns = "JSON object with final status, errors array, and duration")]
        internal async Task<object> WaitForCompilation(int timeoutSeconds = 60)
        {
            var startTime = DateTime.UtcNow;

            // EditorApplication.isCompiling is main-thread-only; marshal the check.
            var isCompiling = await RunOnMainThread(() => EditorApplication.isCompiling);
            if (!isCompiling)
            {
                await Task.Delay(500);

                isCompiling = await RunOnMainThread(() => EditorApplication.isCompiling);
                if (!isCompiling)
                {
                    lock (_lock)
                    {
                        return new
                        {
                            status = _lastCompileHadErrors ? "error" : "clean",
                            errors = _cachedErrors
                                .Where(e => e.type == CompilerMessageType.Error)
                                .Select(e => new
                                {
                                    file = e.file,
                                    line = e.line,
                                    column = e.column,
                                    code = ExtractErrorCode(e.message),
                                    message = e.message,
                                    severity = "error"
                                })
                                .ToArray(),
                            duration = 0.0,
                            message = "No compilation in progress"
                        };
                    }
                }
            }

            TaskCompletionSource<CompilationResult> tcs;
            lock (_lock)
            {
                if (_compilationTcs == null || _compilationTcs.Task.IsCompleted)
                {
                    _compilationTcs = new TaskCompletionSource<CompilationResult>();
                }
                tcs = _compilationTcs;
            }

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                try
                {
                    var completedTask = await Task.WhenAny(
                        tcs.Task,
                        Task.Delay(Timeout.Infinite, cts.Token)
                    );

                    if (completedTask == tcs.Task)
                    {
                        var result = await tcs.Task;
                        result.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                        return new
                        {
                            status = result.Status,
                            errors = result.Errors.Select(e => new
                            {
                                file = e.File,
                                line = e.Line,
                                column = e.Column,
                                code = e.Code,
                                message = e.Message,
                                severity = e.Severity
                            }).ToArray(),
                            duration = result.DurationSeconds
                        };
                    }
                    else
                    {
                        return new
                        {
                            status = "timeout",
                            errors = Array.Empty<object>(),
                            duration = timeoutSeconds,
                            message = $"Compilation did not complete within {timeoutSeconds} seconds"
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    return new
                    {
                        status = "timeout",
                        errors = Array.Empty<object>(),
                        duration = timeoutSeconds,
                        message = $"Compilation did not complete within {timeoutSeconds} seconds"
                    };
                }
            }
        }

        [Tool(Description = "Clear cached compilation errors. Useful before starting a new verification cycle.",
            Returns = "Confirmation that errors were cleared")]
        internal object ClearErrors()
        {
            lock (_lock)
            {
                _cachedErrors.Clear();
                _lastCompileHadErrors = false;
            }

            return new
            {
                cleared = true,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }
    }
}
