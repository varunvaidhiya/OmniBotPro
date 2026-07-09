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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Code analysis tools for MCP clients to query Roslyn analyzer results.
    ///
    /// This tool provides access to static analysis diagnostics beyond compilation errors,
    /// including code style warnings, potential bugs, and suggested improvements.
    ///
    /// While CompilationTools handles compile-time errors, CodeAnalysisTools surfaces
    /// analyzer warnings that might indicate code quality issues.
    ///
    /// Key Features:
    /// - Query analyzer diagnostics from Unity's compilation output
    /// - Filter by severity (error, warning, info)
    /// - Filter by file path
    /// - Get actionable suggestions for common issues
    ///
    /// MCP Client Usage Pattern:
    /// 1. After successful compilation, call GetAnalyzerDiagnostics() for quality checks
    /// 2. Filter to specific files being worked on
    /// 3. Review and address warnings as appropriate
    /// </summary>
    [Tool(
        "Tools for querying Roslyn analyzer diagnostics and code quality warnings.",
        "WHEN TO USE: After successful compilation, to check for code quality issues beyond compile errors.",
        "WORKFLOW: 1) GetAnalyzerDiagnostics() 2) Review warnings 3) Fix or suppress as appropriate.",
        "IMPORTANT: Compilation errors are in CompilationTools; this provides additional static analysis."
    )]
    internal class CodeAnalysisTools : SingletonService<CodeAnalysisTools>
    {
        private static readonly object _lock = new object();
        private static List<AnalyzerDiagnostic> _cachedDiagnostics = new List<AnalyzerDiagnostic>();
        private static DateTime _lastUpdateTime = DateTime.MinValue;

        /// <summary>
        /// Represents a diagnostic from a Roslyn analyzer.
        /// </summary>
        internal class AnalyzerDiagnostic
        {
            public string Id { get; set; }
            public string Message { get; set; }
            public string Severity { get; set; }
            public string FilePath { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public string Category { get; set; }
            public string SuggestedFix { get; set; }
        }

        private static readonly Dictionary<string, string> CommonDiagnosticFixes = new Dictionary<string, string>
        {
            { "CS0168", "Remove the unused variable or use it in your code." },
            { "CS0169", "Remove the unused field or use it in your code." },
            { "CS0219", "Remove the unused variable or use its value." },
            { "CS0414", "Remove the unused field or make it public if it's meant to be serialized." },
            { "CS0649", "Initialize the field or mark it with [SerializeField] if Unity should serialize it." },
            { "CS1591", "Add XML documentation comment (/// <summary>...</summary>) to the member." },
            { "CS8600", "Add null check or use null-conditional operator (?.)." },
            { "CS8601", "Ensure the value being assigned cannot be null, or make the target nullable." },
            { "CS8602", "Add null check before dereferencing: if (obj != null) or obj?.Method()." },
            { "CS8603", "Return a non-null value or change return type to nullable." },
            { "CS8604", "Ensure argument cannot be null or change parameter type to nullable." },
            { "CS8618", "Initialize the property/field in constructor or make it nullable." },
            { "CS8619", "Check nullability of generic type arguments." },
            { "IDE0044", "Make the field readonly if it's only assigned in constructor/initializer." },
            { "IDE0051", "Remove the unused private member or make it public if intended for external use." },
            { "IDE0052", "Remove the unread private member or use its value." },
            { "IDE0060", "Remove the unused parameter or use it in the method body." },
            { "IDE0059", "Remove the unnecessary assignment or use the assigned value." },
            { "IDE0090", "Use target-typed new expression: new() instead of new TypeName()." },
            { "CA1822", "Make the method static since it doesn't access instance state." },
            { "CA2211", "Make the field non-public or convert to a property." },
        };

        [Tool(Description = "Get all analyzer diagnostics, optionally filtered by file path and severity",
            Returns = "Array of diagnostics with id, message, severity, file, line, column, category, and suggested fix")]
        internal object GetAnalyzerDiagnostics(string filePath = null, string severity = null)
        {
            RefreshDiagnosticsFromLogs();

            lock (_lock)
            {
                var filtered = _cachedDiagnostics.AsEnumerable();

                if (!string.IsNullOrEmpty(filePath))
                {
                    var normalizedPath = filePath.Replace('/', '\\').ToLowerInvariant();
                    filtered = filtered.Where(d =>
                        d.FilePath != null &&
                        d.FilePath.Replace('/', '\\').ToLowerInvariant().Contains(normalizedPath));
                }

                if (!string.IsNullOrEmpty(severity))
                {
                    filtered = filtered.Where(d =>
                        d.Severity.Equals(severity, StringComparison.OrdinalIgnoreCase));
                }

                var results = filtered.ToList();

                return new
                {
                    count = results.Count,
                    lastUpdate = _lastUpdateTime.ToString("o"),
                    diagnostics = results.Select(d => new
                    {
                        id = d.Id,
                        message = d.Message,
                        severity = d.Severity,
                        file = d.FilePath,
                        line = d.Line,
                        column = d.Column,
                        category = d.Category,
                        suggestedFix = d.SuggestedFix
                    }).ToArray()
                };
            }
        }

        [Tool(Description = "Get a summary of diagnostics grouped by severity and category",
            Returns = "Summary with counts per severity and category")]
        internal object GetDiagnosticsSummary()
        {
            RefreshDiagnosticsFromLogs();

            lock (_lock)
            {
                var bySeverity = _cachedDiagnostics
                    .GroupBy(d => d.Severity)
                    .ToDictionary(g => g.Key, g => g.Count());

                var byCategory = _cachedDiagnostics
                    .Where(d => !string.IsNullOrEmpty(d.Category))
                    .GroupBy(d => d.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                var topIssues = _cachedDiagnostics
                    .GroupBy(d => d.Id)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new
                    {
                        id = g.Key,
                        count = g.Count(),
                        message = g.First().Message,
                        suggestedFix = g.First().SuggestedFix
                    })
                    .ToArray();

                return new
                {
                    total = _cachedDiagnostics.Count,
                    lastUpdate = _lastUpdateTime.ToString("o"),
                    bySeverity,
                    byCategory,
                    topIssues
                };
            }
        }

        [Tool(Description = "Get suggested fix for a specific diagnostic code",
            Returns = "Suggested fix description or guidance")]
        internal object GetSuggestedFix(string diagnosticId)
        {
            if (string.IsNullOrEmpty(diagnosticId))
            {
                return new { error = "diagnosticId is required" };
            }

            var normalizedId = diagnosticId.ToUpperInvariant();

            if (CommonDiagnosticFixes.TryGetValue(normalizedId, out var fix))
            {
                return new
                {
                    diagnosticId = normalizedId,
                    suggestedFix = fix,
                    source = "built-in"
                };
            }

            lock (_lock)
            {
                var matching = _cachedDiagnostics.FirstOrDefault(d =>
                    d.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));

                if (matching != null && !string.IsNullOrEmpty(matching.SuggestedFix))
                {
                    return new
                    {
                        diagnosticId = normalizedId,
                        suggestedFix = matching.SuggestedFix,
                        source = "analyzer"
                    };
                }
            }

            return new
            {
                diagnosticId = normalizedId,
                suggestedFix = $"No specific fix available for {normalizedId}. Check the diagnostic message for guidance.",
                source = "none"
            };
        }

        [Tool(Description = "Clear cached diagnostics to start fresh analysis",
            Returns = "Confirmation of cache clear")]
        internal object ClearDiagnostics()
        {
            lock (_lock)
            {
                _cachedDiagnostics.Clear();
                _lastUpdateTime = DateTime.MinValue;
            }

            return new
            {
                cleared = true,
                timestamp = DateTime.UtcNow.ToString("o")
            };
        }

        [Tool(Description = "Refresh diagnostics from Unity's compilation logs",
            Returns = "Number of diagnostics found")]
        internal object RefreshDiagnostics()
        {
            var count = RefreshDiagnosticsFromLogs();
            return new
            {
                count,
                timestamp = _lastUpdateTime.ToString("o")
            };
        }

        private static int RefreshDiagnosticsFromLogs()
        {
            lock (_lock)
            {
                _cachedDiagnostics.Clear();

                var logPath = GetUnityEditorLogPath();
                if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
                {
                    try
                    {
                        using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var diagnostic = ParseDiagnosticLine(line);
                                if (diagnostic != null)
                                {
                                    _cachedDiagnostics.Add(diagnostic);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[CodeAnalysisTools] Failed to read log file: {ex.Message}");
                    }
                }

                _lastUpdateTime = DateTime.UtcNow;
                return _cachedDiagnostics.Count;
            }
        }

        private static string GetUnityEditorLogPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logPath = Path.Combine(localAppData, "Unity", "Editor", "Editor.log");

            if (File.Exists(logPath))
                return logPath;

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var macLogPath = Path.Combine(homeDir, "Library", "Logs", "Unity", "Editor.log");

            if (File.Exists(macLogPath))
                return macLogPath;

            return null;
        }

        private static readonly Regex DiagnosticPattern = new Regex(
            @"^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s*(?<severity>error|warning|info)\s+(?<id>[A-Z]+\d+):\s*(?<message>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static AnalyzerDiagnostic ParseDiagnosticLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var match = DiagnosticPattern.Match(line);
            if (!match.Success)
                return null;

            var id = match.Groups["id"].Value.ToUpperInvariant();
            var diagnostic = new AnalyzerDiagnostic
            {
                FilePath = match.Groups["file"].Value,
                Line = int.Parse(match.Groups["line"].Value),
                Column = int.Parse(match.Groups["column"].Value),
                Severity = match.Groups["severity"].Value.ToLowerInvariant(),
                Id = id,
                Message = match.Groups["message"].Value,
                Category = GetDiagnosticCategory(id)
            };

            if (CommonDiagnosticFixes.TryGetValue(id, out var fix))
            {
                diagnostic.SuggestedFix = fix;
            }

            return diagnostic;
        }

        private static string GetDiagnosticCategory(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "Unknown";

            if (id.StartsWith("CS0") || id.StartsWith("CS1"))
                return "Compiler";

            if (id.StartsWith("CS8"))
                return "Nullability";

            if (id.StartsWith("IDE"))
                return "Style";

            if (id.StartsWith("CA"))
                return "CodeAnalysis";

            if (id.StartsWith("CS"))
                return "Compiler";

            return "Other";
        }
    }
}
