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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MCPServices.Tools
{
    /// <summary>
    /// Diagnostic tools for MCP clients to inspect console logs, warnings, and errors.
    ///
    /// This tool automatically captures Unity log messages and provides summary/detail views
    /// that help AI agents diagnose application issues without direct console access.
    ///
    /// Key Features:
    /// - Automatic log capture from Unity's Application.logMessageReceivedThreaded
    /// - Optional integration with ImmersiveDebugger's ConsoleLogsCache (via reflection)
    /// - Log deduplication with occurrence counting
    /// - Health status assessment based on error/warning patterns
    ///
    /// Thread Safety:
    /// Application.logMessageReceivedThreaded fires from any thread. All mutable state
    /// uses concurrent collections (ConcurrentDictionary, ConcurrentQueue) so that the
    /// OnLogReceived callback is lock-free while reader methods on the main thread see
    /// a consistent snapshot via .Values / .ToArray().
    ///
    /// MCP Client Usage Pattern:
    /// 1. Use GetHealthStatus() for quick overview of application state
    /// 2. Use GetErrorSummary()/GetWarningSummary() to see categorized issues
    /// 3. Use GetLogDetails() with keywords to drill down into specific issues
    /// 4. Use GetRecentActivity() to see chronological log flow
    /// 5. Use ClearDiagnosticData() to reset for fresh monitoring
    /// </summary>
    [Tool(
        "Tools for diagnosing application issues by inspecting console logs, warnings, and errors.",
        "WHEN TO USE: Use when investigating bugs, crashes, or unexpected behavior.",
        "WORKFLOW: 1) GetHealthStatus() for overview 2) GetErrorSummary() for issues 3) GetLogDetails() to drill down.",
        "IMPORTANT: Logs are captured automatically from Unity and ImmersiveDebugger (if available)."
    )]
    internal class DiagnosticTools : SingletonService<DiagnosticTools>
    {
        // Thread-safe: written from any thread via logMessageReceivedThreaded, read from main thread.
        private static readonly ConcurrentDictionary<string, LogSummary> _logSummaries =
            new ConcurrentDictionary<string, LogSummary>();

        // Thread-safe FIFO buffer for recent logs. ConcurrentQueue is lock-free for Enqueue/TryDequeue.
        private static readonly ConcurrentQueue<LogEntry> _recentLogs = new ConcurrentQueue<LogEntry>();

        internal const int MaxRecentLogs = 100;
        internal const int MaxUniqueLogs = 500;

        private static int _recentLogsCount;
        private static bool _initialized = false;

        // Reflection helpers for accessing internal ConsoleLogsCache
        private static Type _consoleLogsCacheType;
        private static FieldInfo _onLogReceivedField;
        private static Action<string, string, LogType> _logReceivedDelegate;

        private class LogSummary
        {
            internal string Message { get; set; }
            internal string StackTrace { get; set; }
            internal LogType Type { get; set; }
            internal int Count { get; set; }
            internal DateTime FirstOccurrence { get; set; }
            internal DateTime LastOccurrence { get; set; }
            internal string Hash { get; set; }
        }

        private class LogEntry
        {
            internal string Message { get; set; }
            internal string StackTrace { get; set; }
            internal LogType Type { get; set; }
            internal DateTime Timestamp { get; set; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            if (_initialized) return;

            // Initialize reflection helpers for ConsoleLogsCache
            InitializeConsoleLogsCacheReflection();

            Application.logMessageReceivedThreaded += OnLogReceived;

            // Subscribe to ConsoleLogsCache.OnLogReceived via reflection
            SubscribeToConsoleLogsCache();

            Application.quitting += OnApplicationQuitting;
            _initialized = true;
        }

        private static void OnApplicationQuitting()
        {
            Application.logMessageReceivedThreaded -= OnLogReceived;

            // Unsubscribe from ConsoleLogsCache.OnLogReceived via reflection
            UnsubscribeFromConsoleLogsCache();

            Application.quitting -= OnApplicationQuitting;
        }

        private static void InitializeConsoleLogsCacheReflection()
        {
            try
            {
                // Find the ConsoleLogsCache type in the ImmersiveDebugger assembly
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    _consoleLogsCacheType = assembly.GetType("Meta.XR.ImmersiveDebugger.Utils.ConsoleLogsCache");
                    if (_consoleLogsCacheType != null)
                        break;
                }

                if (_consoleLogsCacheType != null)
                {
                    // Get the OnLogReceived field
                    _onLogReceivedField = _consoleLogsCacheType.GetField("OnLogReceived",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    // Create delegate for our log handler
                    _logReceivedDelegate = OnLogReceived;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DiagnosticTools: Failed to initialize ConsoleLogsCache reflection: {ex.Message}");
            }
        }

        private static void SubscribeToConsoleLogsCache()
        {
            try
            {
                if (_onLogReceivedField != null && _logReceivedDelegate != null)
                {
                    // Get current value of OnLogReceived
                    var currentDelegate = (Action<string, string, LogType>)_onLogReceivedField.GetValue(null);

                    // Combine with our delegate
                    var combinedDelegate = (Action<string, string, LogType>)Delegate.Combine(currentDelegate, _logReceivedDelegate);

                    // Set the combined delegate back
                    _onLogReceivedField.SetValue(null, combinedDelegate);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DiagnosticTools: Failed to subscribe to ConsoleLogsCache: {ex.Message}");
            }
        }

        private static void UnsubscribeFromConsoleLogsCache()
        {
            try
            {
                if (_onLogReceivedField != null && _logReceivedDelegate != null)
                {
                    // Get current value of OnLogReceived
                    var currentDelegate = (Action<string, string, LogType>)_onLogReceivedField.GetValue(null);

                    // Remove our delegate
                    var updatedDelegate = (Action<string, string, LogType>)Delegate.Remove(currentDelegate, _logReceivedDelegate);

                    // Set the updated delegate back
                    _onLogReceivedField.SetValue(null, updatedDelegate);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DiagnosticTools: Failed to unsubscribe from ConsoleLogsCache: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from any thread via Application.logMessageReceivedThreaded.
        /// All writes use lock-free concurrent collections to avoid blocking.
        /// </summary>
        private static void OnLogReceived(string logString, string stackTrace, LogType type)
        {
            var timestamp = DateTime.UtcNow;
            var hash = ComputeLogHash(logString, stackTrace);

            // Update summary using ConcurrentDictionary.AddOrUpdate (lock-free).
            _logSummaries.AddOrUpdate(
                hash,
                _ => new LogSummary
                {
                    Message = logString,
                    StackTrace = stackTrace,
                    Type = type,
                    Count = 1,
                    FirstOccurrence = timestamp,
                    LastOccurrence = timestamp,
                    Hash = hash
                },
                (_, existing) =>
                {
                    existing.Count++;
                    existing.LastOccurrence = timestamp;
                    return existing;
                });

            // Evict oldest entries when _logSummaries exceeds the cap.
            // This is best-effort under concurrency — exact count may briefly overshoot,
            // which is acceptable for a diagnostic tool.
            if (_logSummaries.Count > MaxUniqueLogs)
            {
                var oldest = _logSummaries.Values
                    .OrderBy(s => s.LastOccurrence)
                    .Take(_logSummaries.Count - MaxUniqueLogs)
                    .ToList();
                foreach (var entry in oldest)
                {
                    _logSummaries.TryRemove(entry.Hash, out _);
                }
            }

            // Enqueue to the recent logs ring buffer (ConcurrentQueue is lock-free).
            _recentLogs.Enqueue(new LogEntry
            {
                Message = logString,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = timestamp
            });
            System.Threading.Interlocked.Increment(ref _recentLogsCount);

            // Trim excess entries. The count check is racy but harmless —
            // worst case we briefly hold a few extra entries.
            while (_recentLogsCount > MaxRecentLogs && _recentLogs.TryDequeue(out _))
            {
                System.Threading.Interlocked.Decrement(ref _recentLogsCount);
            }
        }

        [Tool(Description = "Get a summary of all errors and exceptions with their frequency count, helping identify the most critical issues",
            Returns = "Summary of errors and exceptions with occurrence counts, timestamps, and stack traces")]
        internal string GetErrorSummary()
        {
            // ConcurrentDictionary.Values returns a snapshot — safe to enumerate with LINQ.
            // LINQ is acceptable here: this is a diagnostic tool, not a production hot path.
            var errorSummaries = _logSummaries.Values
                .Where(s => s.Type == LogType.Error || s.Type == LogType.Exception || s.Type == LogType.Assert)
                .OrderByDescending(s => s.Count)
                .ToList();

            if (!errorSummaries.Any())
                return "No errors or exceptions found.";

            var sb = new StringBuilder();
            sb.AppendLine($"=== ERROR SUMMARY ({errorSummaries.Count} unique errors) ===");
            sb.AppendLine();

            foreach (var summary in errorSummaries)
            {
                sb.AppendLine($"   [{summary.Type}] Count: {summary.Count}");
                sb.AppendLine($"   Message: {summary.Message}");
                sb.AppendLine($"   First: {summary.FirstOccurrence:HH:mm:ss}");
                sb.AppendLine($"   Last: {summary.LastOccurrence:HH:mm:ss}");
                if (!string.IsNullOrEmpty(summary.StackTrace))
                {
                    var firstLine = summary.StackTrace.Split('\n')[0];
                    sb.AppendLine($"   Stack: {firstLine}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        [Tool(Description = "Get a summary of all warnings with their frequency count, helping identify potential issues",
            Returns = "Summary of warnings with occurrence counts and timestamps")]
        internal string GetWarningSummary()
        {
            // ConcurrentDictionary.Values returns a snapshot — safe to enumerate with LINQ.
            var warningSummaries = _logSummaries.Values
                .Where(s => s.Type == LogType.Warning)
                .OrderByDescending(s => s.Count)
                .ToList();

            if (!warningSummaries.Any())
                return "No warnings found.";

            var sb = new StringBuilder();
            sb.AppendLine($"=== WARNING SUMMARY ({warningSummaries.Count} unique warnings) ===");
            sb.AppendLine();

            foreach (var summary in warningSummaries)
            {
                sb.AppendLine($"   [Warning] Count: {summary.Count}");
                sb.AppendLine($"   Message: {summary.Message}");
                sb.AppendLine($"   First: {summary.FirstOccurrence:HH:mm:ss}");
                sb.AppendLine($"   Last: {summary.LastOccurrence:HH:mm:ss}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        [Tool(Description = "Get recent log activity to understand what the application has been doing lately",
            Returns = "Recent log entries with timestamps, useful for understanding application flow")]
        internal string GetRecentActivity(int maxEntries = 20)
        {
            // ConcurrentQueue.ToArray() returns a point-in-time snapshot — safe to slice with LINQ.
            var snapshot = _recentLogs.ToArray();
            var recentEntries = snapshot
                .Skip(Math.Max(0, snapshot.Length - maxEntries))
                .ToList();

            if (!recentEntries.Any())
                return "No recent log activity found.";

            var sb = new StringBuilder();
            sb.AppendLine($"=== RECENT ACTIVITY (last {recentEntries.Count} entries) ===");
            sb.AppendLine();

            foreach (var entry in recentEntries)
            {
                var icon = GetLogTypeIcon(entry.Type);
                sb.AppendLine($"{icon} [{entry.Timestamp:HH:mm:ss.fff}] {entry.Message}");
            }

            return sb.ToString();
        }

        [Tool(Description = "Get detailed information about a specific error or warning by searching for keywords in the message",
            Returns = "Detailed information including full stack trace and occurrence pattern")]
        internal string GetLogDetails(string searchKeyword)
        {
            // ConcurrentDictionary.Values returns a snapshot — safe to enumerate with LINQ.
            var matchingSummaries = _logSummaries.Values
                .Where(s => s.Message.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Count)
                .ToList();

            if (!matchingSummaries.Any())
                return $"No logs found containing keyword: '{searchKeyword}'";

            var sb = new StringBuilder();
            sb.AppendLine($"=== LOG DETAILS for '{searchKeyword}' ===");
            sb.AppendLine();

            foreach (var summary in matchingSummaries)
            {
                var icon = GetLogTypeIcon(summary.Type);
                sb.AppendLine($"{icon} [{summary.Type}] Occurrences: {summary.Count}");
                sb.AppendLine($"Message: {summary.Message}");
                sb.AppendLine($"First seen: {summary.FirstOccurrence:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Last seen: {summary.LastOccurrence:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(summary.StackTrace))
                {
                    sb.AppendLine("Stack Trace:");
                    sb.AppendLine(summary.StackTrace);
                }
                sb.AppendLine(new string('-', 50));
            }

            return sb.ToString();
        }

        [Tool(Description = "Get overall diagnostic health status of the application based on log patterns",
            Returns = "Health assessment with recommendations for addressing issues")]
        internal string GetHealthStatus()
        {
            // ConcurrentDictionary.Values returns a snapshot — safe to enumerate with LINQ.
            var errorCount = _logSummaries.Values.Where(s => s.Type == LogType.Error || s.Type == LogType.Exception).Sum(s => s.Count);
            var warningCount = _logSummaries.Values.Where(s => s.Type == LogType.Warning).Sum(s => s.Count);
            var infoCount = _logSummaries.Values.Where(s => s.Type == LogType.Log).Sum(s => s.Count);

            var sb = new StringBuilder();
            sb.AppendLine("=== APPLICATION HEALTH STATUS ===");
            sb.AppendLine();

            sb.AppendLine($"   Log Statistics:");
            sb.AppendLine($"   Errors: {errorCount}");
            sb.AppendLine($"   Warnings: {warningCount}");
            sb.AppendLine($"   Info: {infoCount}");
            sb.AppendLine();

            // Health assessment
            string healthStatus;
            if (errorCount == 0 && warningCount <= 5)
            {
                healthStatus = "HEALTHY";
            }
            else if (errorCount <= 3 && warningCount <= 20)
            {
                healthStatus = "NEEDS ATTENTION";
            }
            else
            {
                healthStatus = "CRITICAL ISSUES";
            }

            sb.AppendLine($"Overall Status: {healthStatus}");
            sb.AppendLine();

            // Top issues
            var topErrors = _logSummaries.Values
                .Where(s => s.Type == LogType.Error || s.Type == LogType.Exception)
                .OrderByDescending(s => s.Count)
                .Take(3)
                .ToList();

            if (topErrors.Any())
            {
                sb.AppendLine("Top Issues to Address:");
                foreach (var error in topErrors)
                {
                    sb.AppendLine($"   • {error.Message} (occurred {error.Count} times)");
                }
            }

            return sb.ToString();
        }

        [Tool(Description = "Clear all cached diagnostic data to start fresh monitoring",
            Returns = "Confirmation of data clearing")]
        internal string ClearDiagnosticData()
        {
            _logSummaries.Clear();

            // Drain the ConcurrentQueue
            while (_recentLogs.TryDequeue(out _)) { }
            System.Threading.Interlocked.Exchange(ref _recentLogsCount, 0);

            return "All diagnostic data has been cleared. Fresh monitoring started.";
        }

        private static string GetLogTypeIcon(LogType logType)
        {
            return logType switch
            {
                LogType.Error => "Error",
                LogType.Exception => "Exception",
                LogType.Assert => "Assert",
                LogType.Warning => "Warning",
                LogType.Log => "Log",
                _ => "Info"
            };
        }

        private static string ComputeLogHash(string content, string stackTrace)
        {
            var hash = new HashCode();
            hash.Add(content);
            hash.Add(stackTrace);
            return hash.ToHashCode().ToString();
        }
    }
}
