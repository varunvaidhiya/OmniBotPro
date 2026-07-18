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

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Test runner tools for MCP clients to execute Unity tests without cold-starting the Editor.
    ///
    /// This tool enables AI agents like Devmate to run tests from within the already-running
    /// Unity Editor, providing a massive performance improvement over batchmode testing:
    /// - Batchmode: 2-5 minutes (cold start + asset import + full recompile)
    /// - MCPBridge: 5-30 seconds (incremental, already cached)
    ///
    /// Key Features:
    /// - List available tests with filtering by assembly and name (supports regex)
    /// - Run tests with filters (EditMode/PlayMode)
    /// - Wait for test completion with timeout
    /// - Get detailed results including passed/failed/skipped counts
    /// - State persists across domain reloads
    ///
    /// MCP Client Usage Pattern:
    /// 1. ListTests() to see available tests
    /// 2. RunFiltered(filter) to run relevant tests
    /// 3. WaitForTestRun(timeout) to wait for results
    /// 4. If failures, analyze results and fix code
    /// </summary>
    [Tool(
        "Tools for running Unity tests from within the already-running Editor.",
        "WHEN TO USE: After successful compilation, to verify code behavior without cold-starting Unity.",
        "WORKFLOW: 1) ListTests() 2) RunFiltered() 3) WaitForTestRun() 4) Analyze results.",
        "IMPORTANT: Tests run in 5-30 seconds vs 2-5 minutes in batchmode. No cold start needed."
    )]
    [InitializeOnLoad]
    internal class TestRunnerTools : SingletonService<TestRunnerTools>
    {
        private const string LogTag = "TestRunnerTools";
        private const string EditorPrefsKeyPrefix = "MCPBridge.TestRunnerTools.";
        private const string KeyRunId = EditorPrefsKeyPrefix + "RunId";
        private const string KeyIsRunning = EditorPrefsKeyPrefix + "IsRunning";
        private const string KeyRunStartTime = EditorPrefsKeyPrefix + "RunStartTime";
        private const string KeyResults = EditorPrefsKeyPrefix + "Results";

        private static TestRunnerApi? _testRunnerApi;
        private static TestRunnerCallbacks? _callbacks;
        private static readonly object _lock = new object();
        private static TaskCompletionSource<TestRunResult>? _testRunTcs;

        // In-memory cache for thread-safe access (synced with EditorPrefs on main thread)
        private static string? _cachedRunId;
        private static bool _cachedIsRunning;
        private static DateTime _cachedRunStartTime;
        private static List<TestResultData> _cachedResults = new List<TestResultData>();
        private static bool _cacheInitialized;

        static TestRunnerTools()
        {
            // Re-register callbacks after domain reload and initialize cache
            EditorApplication.delayCall += InitializeOnMainThread;
        }

        private static void InitializeOnMainThread()
        {
            EditorApplication.delayCall -= InitializeOnMainThread;
            EnsureCallbacksRegistered();
            EnsureCacheInitialized();
        }

        private static void EnsureCallbacksRegistered()
        {
            EditorApplication.delayCall -= EnsureCallbacksRegistered;

            if (_testRunnerApi == null)
            {
                _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                _callbacks = new TestRunnerCallbacks();
                _testRunnerApi.RegisterCallbacks(_callbacks);
                Debug.Log($"[{LogTag}] Callbacks registered after domain reload");
            }

            // Restore TCS if a run was in progress
            if (IsRunning && _testRunTcs == null)
            {
                _testRunTcs = new TaskCompletionSource<TestRunResult>();
                Debug.Log($"[{LogTag}] Restored TCS for in-progress run: {CurrentRunId}");
            }
        }

        #region Persisted State Properties (Thread-Safe via Cache)

        private static void EnsureCacheInitialized()
        {
            if (_cacheInitialized) return;

            lock (_lock)
            {
                if (_cacheInitialized) return;

                // If we're on the main thread, initialize directly
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                {
                    LoadCacheFromEditorPrefsUnsafe();
                    _cacheInitialized = true;
                }
                // If on background thread, use defaults (will be synced on main thread later)
                else
                {
                    _cachedRunId = null;
                    _cachedIsRunning = false;
                    _cachedRunStartTime = DateTime.MinValue;
                    _cachedResults = new List<TestResultData>();
                    _cacheInitialized = true;
                    Debug.Log($"[{LogTag}] Cache initialized with defaults (background thread)");
                }
            }
        }

        private static void LoadCacheFromEditorPrefsUnsafe()
        {
            _cachedRunId = EditorPrefs.GetString(KeyRunId, null);
            if (string.IsNullOrEmpty(_cachedRunId)) _cachedRunId = null;

            _cachedIsRunning = EditorPrefs.GetBool(KeyIsRunning, false);

            var ticks = EditorPrefs.GetString(KeyRunStartTime, "0");
            _cachedRunStartTime = new DateTime(long.Parse(ticks));

            var json = EditorPrefs.GetString(KeyResults, "{\"items\":[]}");
            try
            {
                var wrapper = JsonUtility.FromJson<TestResultListWrapper>(json);
                _cachedResults = wrapper?.items ?? new List<TestResultData>();
            }
            catch
            {
                _cachedResults = new List<TestResultData>();
            }

            Debug.Log($"[{LogTag}] Cache loaded from EditorPrefs: RunId={_cachedRunId}, IsRunning={_cachedIsRunning}");
        }

        private static string? CurrentRunId
        {
            get
            {
                EnsureCacheInitialized();
                lock (_lock) { return _cachedRunId; }
            }
            set
            {
                EnsureCacheInitialized();
                lock (_lock) { _cachedRunId = value; }
                SaveToEditorPrefsOnMainThread();
            }
        }

        private static bool IsRunning
        {
            get
            {
                EnsureCacheInitialized();
                lock (_lock) { return _cachedIsRunning; }
            }
            set
            {
                EnsureCacheInitialized();
                lock (_lock) { _cachedIsRunning = value; }
                SaveToEditorPrefsOnMainThread();
            }
        }

        private static DateTime RunStartTime
        {
            get
            {
                EnsureCacheInitialized();
                lock (_lock) { return _cachedRunStartTime; }
            }
            set
            {
                EnsureCacheInitialized();
                lock (_lock) { _cachedRunStartTime = value; }
                SaveToEditorPrefsOnMainThread();
            }
        }

        private static List<TestResultData> CurrentResults
        {
            get
            {
                EnsureCacheInitialized();
                lock (_lock) { return new List<TestResultData>(_cachedResults); }
            }
            set
            {
                EnsureCacheInitialized();
                lock (_lock) { _cachedResults = value ?? new List<TestResultData>(); }
                SaveToEditorPrefsOnMainThread();
            }
        }

        private static void AddResult(TestResultData result)
        {
            EnsureCacheInitialized();
            lock (_lock) { _cachedResults.Add(result); }
            SaveToEditorPrefsOnMainThread();
        }

        private static void SaveToEditorPrefsOnMainThread()
        {
            if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                EditorApplication.delayCall += SaveToEditorPrefsImpl;
            }
            else
            {
                SaveToEditorPrefsImpl();
            }
        }

        private static void SaveToEditorPrefsImpl()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_cachedRunId))
                    EditorPrefs.DeleteKey(KeyRunId);
                else
                    EditorPrefs.SetString(KeyRunId, _cachedRunId);

                EditorPrefs.SetBool(KeyIsRunning, _cachedIsRunning);
                EditorPrefs.SetString(KeyRunStartTime, _cachedRunStartTime.Ticks.ToString());

                var wrapper = new TestResultListWrapper { items = _cachedResults };
                var json = JsonUtility.ToJson(wrapper);
                EditorPrefs.SetString(KeyResults, json);
            }
        }

        #endregion

        /// <summary>
        /// Result of a test run operation.
        /// </summary>
        internal class TestRunResult
        {
            public string Status { get; set; } = string.Empty;
            public int Passed { get; set; }
            public int Failed { get; set; }
            public int Skipped { get; set; }
            public int Total { get; set; }
            public double DurationSeconds { get; set; }
            public List<TestResultData> Results { get; set; } = new List<TestResultData>();
        }

        /// <summary>
        /// Wrapper class for serializing List with JsonUtility.
        /// </summary>
        [Serializable]
        private class TestResultListWrapper
        {
            public List<TestResultData> items = new List<TestResultData>();
        }

        /// <summary>
        /// Detailed information about a single test result.
        /// JsonUtility requires public fields (not properties) for serialization.
        /// </summary>
        [Serializable]
        internal class TestResultData
        {
            public string Name = string.Empty;
            public string FullName = string.Empty;
            public string ResultState = string.Empty;
            public string? Message;
            public string? StackTrace;
            public double DurationSeconds;
        }

        /// <summary>
        /// Information about a test in the test list.
        /// </summary>
        internal class TestInfo
        {
            public string Name { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Assembly { get; set; } = string.Empty;
            public string TestMode { get; set; } = string.Empty;
        }

        private static TestRunnerApi GetTestRunnerApi()
        {
            if (_testRunnerApi == null)
            {
                if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                {
                    var tcs = new TaskCompletionSource<TestRunnerApi>();
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            if (_testRunnerApi == null)
                            {
                                _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                                _callbacks = new TestRunnerCallbacks();
                                _testRunnerApi.RegisterCallbacks(_callbacks);
                            }
                            tcs.SetResult(_testRunnerApi);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    };
                    return tcs.Task.GetAwaiter().GetResult();
                }

                _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                _callbacks = new TestRunnerCallbacks();
                _testRunnerApi.RegisterCallbacks(_callbacks);
            }
            return _testRunnerApi;
        }

        [Tool(Description = "List available tests with optional filtering by assembly and name pattern (supports regex)",
            Returns = "Array of test objects with name, fullName, assembly, and testMode")]
        internal Task<object> ListTests(string? assemblyFilter = null, string? nameFilter = null, string testPlatform = "EditMode")
        {
            var tcs = new TaskCompletionSource<object>();

            var mode = testPlatform.Equals("PlayMode", StringComparison.OrdinalIgnoreCase)
                ? TestMode.PlayMode
                : TestMode.EditMode;

            // Execute on main thread
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var api = GetTestRunnerApi();

                    api.RetrieveTestList(mode, (testRoot) =>
                    {
                        try
                        {
                            var tests = new List<TestInfo>();

                            Regex? regexNameFilter = null;
                            if (!string.IsNullOrEmpty(nameFilter))
                            {
                                try
                                {
                                    regexNameFilter = new Regex(nameFilter, RegexOptions.IgnoreCase);
                                }
                                catch (ArgumentException)
                                {
                                    regexNameFilter = new Regex(Regex.Escape(nameFilter), RegexOptions.IgnoreCase);
                                }
                            }

                            CollectTests(testRoot, tests, mode.ToString(), assemblyFilter, regexNameFilter);

                            tcs.TrySetResult(new
                            {
                                testPlatform = mode.ToString(),
                                count = tests.Count,
                                tests = tests.Select(t => new
                                {
                                    name = t.Name,
                                    fullName = t.FullName,
                                    assembly = t.Assembly,
                                    testMode = t.TestMode
                                }).ToArray()
                            });
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            return tcs.Task;
        }

        private static void CollectTests(ITestAdaptor test, List<TestInfo> tests, string testMode, string? assemblyFilter, Regex? regexNameFilter)
        {
            if (test == null) return;

            if (test.RunState != RunState.Runnable)
            {
                return;
            }

            if (!test.IsSuite)
            {
                var assembly = test.TypeInfo?.Assembly?.GetName()?.Name ?? "Unknown";

                if (!string.IsNullOrEmpty(assemblyFilter) && !assembly.Contains(assemblyFilter, StringComparison.OrdinalIgnoreCase))
                    return;

                if (regexNameFilter != null && !regexNameFilter.IsMatch(test.FullName))
                    return;

                tests.Add(new TestInfo
                {
                    Name = test.Name,
                    FullName = test.FullName,
                    Assembly = assembly,
                    TestMode = testMode
                });
            }

            if (test.HasChildren && test.Children != null)
            {
                foreach (var child in test.Children)
                {
                    CollectTests(child, tests, testMode, assemblyFilter, regexNameFilter);
                }
            }
        }

        [Tool(Description = "Run tests matching the specified filter (supports regex pattern or semicolon-separated exact names). Returns immediately with runId; use WaitForTestRun or GetResults to get results.",
            Returns = "JSON object with runId, testsQueued count, and matched test names")]
        internal Task<object> RunFiltered(string? testFilter, string? assemblyFilter = null, string testPlatform = "EditMode")
        {
            var tcs = new TaskCompletionSource<object>();

            lock (_lock)
            {
                if (IsRunning)
                {
                    tcs.SetResult(new
                    {
                        error = "A test run is already in progress",
                        currentRunId = CurrentRunId
                    });
                    return tcs.Task;
                }
            }

            var mode = testPlatform.Equals("PlayMode", StringComparison.OrdinalIgnoreCase)
                ? TestMode.PlayMode
                : TestMode.EditMode;

            // Execute on main thread
            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunFilteredOnMainThread(tcs, testFilter, assemblyFilter, mode);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            };

            return tcs.Task;
        }

        private static void RunFilteredOnMainThread(TaskCompletionSource<object> tcs, string? testFilter, string? assemblyFilter, TestMode mode)
        {
            var api = GetTestRunnerApi();

            // Retrieve the full test list
            api.RetrieveTestList(mode, (testRoot) =>
            {
                try
                {
                    // Collect all runnable tests
                    var allTests = new List<TestInfo>();
                    CollectTests(testRoot, allTests, mode.ToString(), null, null);

                    // Apply filters to get matching test names
                    var matchingTests = FilterTests(allTests, testFilter, assemblyFilter);

                    if (matchingTests.Count == 0)
                    {
                        tcs.TrySetResult(new
                        {
                            error = "No tests matched the filter",
                            filter = testFilter ?? "*",
                            assemblyFilter = assemblyFilter ?? "*",
                            testPlatform = mode.ToString(),
                            availableTestCount = allTests.Count
                        });
                        return;
                    }

                    // Now start the actual test run
                    lock (_lock)
                    {
                        CurrentRunId = Guid.NewGuid().ToString("N").Substring(0, 8);
                        CurrentResults = new List<TestResultData>();
                        IsRunning = true;
                        RunStartTime = DateTime.UtcNow;
                        _testRunTcs = new TaskCompletionSource<TestRunResult>();
                    }

                    var filter = new Filter
                    {
                        testMode = mode,
                        testNames = matchingTests.Select(t => t.FullName).ToArray()
                    };

                    var runId = CurrentRunId;

                    Debug.Log($"[{LogTag}] Starting test run: {runId}, filter: {testFilter ?? "*"}, assembly: {assemblyFilter ?? "*"}, platform: {mode}, matched: {matchingTests.Count} tests");

                    api.Execute(new ExecutionSettings(filter));

                    tcs.TrySetResult(new
                    {
                        runId,
                        filter = testFilter ?? "*",
                        assemblyFilter = assemblyFilter ?? "*",
                        testPlatform = mode.ToString(),
                        status = "started",
                        testsQueued = matchingTests.Count,
                        matchedTests = matchingTests.Select(t => t.FullName).ToArray(),
                        timestamp = DateTime.UtcNow.ToString("o")
                    });
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
        }

        private static List<TestInfo> FilterTests(List<TestInfo> allTests, string? testFilter, string? assemblyFilter)
        {
            var result = new List<TestInfo>();

            Regex? regexTestFilter = null;
            if (!string.IsNullOrEmpty(testFilter))
            {
                // Check if it's a semicolon-separated list of exact names
                if (testFilter!.Contains(';'))
                {
                    var exactNames = new HashSet<string>(
                        testFilter.Split(';', StringSplitOptions.RemoveEmptyEntries),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var test in allTests)
                    {
                        if (exactNames.Contains(test.FullName) || exactNames.Contains(test.Name))
                        {
                            if (string.IsNullOrEmpty(assemblyFilter) ||
                                test.Assembly.Contains(assemblyFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(test);
                            }
                        }
                    }
                    return result;
                }

                // Otherwise treat as regex pattern
                try
                {
                    regexTestFilter = new Regex(testFilter, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    // If invalid regex, escape and use as literal
                    regexTestFilter = new Regex(Regex.Escape(testFilter), RegexOptions.IgnoreCase);
                }
            }

            foreach (var test in allTests)
            {
                // Apply assembly filter
                if (!string.IsNullOrEmpty(assemblyFilter) &&
                    !test.Assembly.Contains(assemblyFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Apply test name filter
                if (regexTestFilter != null && !regexTestFilter.IsMatch(test.FullName))
                {
                    continue;
                }

                result.Add(test);
            }

            return result;
        }

        [Tool(Description = "Run all tests for the specified platform",
            Returns = "JSON object with runId and status")]
        internal Task<object> RunAll(string testPlatform = "EditMode")
        {
            return RunFiltered(null, null, testPlatform);
        }

        [Tool(Description = "Get the results of the last or specified test run",
            Returns = "JSON object with status, passed/failed/skipped counts, and detailed results")]
        internal object GetResults(string? runId = null)
        {
            lock (_lock)
            {
                if (runId != null && runId != CurrentRunId)
                {
                    return new
                    {
                        error = "Run ID not found",
                        currentRunId = CurrentRunId
                    };
                }

                var results = CurrentResults;
                var passed = results.Count(r => r.ResultState == "Passed");
                var failed = results.Count(r => r.ResultState == "Failed");
                var skipped = results.Count(r => r.ResultState == "Skipped" || r.ResultState == "Ignored");

                return new
                {
                    runId = CurrentRunId,
                    status = IsRunning ? "running" : (failed > 0 ? "failed" : "passed"),
                    isRunning = IsRunning,
                    passed,
                    failed,
                    skipped,
                    total = results.Count,
                    duration = (DateTime.UtcNow - RunStartTime).TotalSeconds,
                    results = results.Select(r => new
                    {
                        name = r.Name,
                        fullName = r.FullName,
                        resultState = r.ResultState,
                        message = r.Message,
                        stackTrace = r.StackTrace,
                        duration = r.DurationSeconds
                    }).ToArray()
                };
            }
        }

        [Tool(Description = "Wait for the current test run to complete. Blocks until tests finish or timeout is reached.",
            Returns = "JSON object with final status, passed/failed/skipped counts, and detailed results")]
        internal async Task<object> WaitForTestRun(string? runId = null, int timeoutSeconds = 120)
        {
            TaskCompletionSource<TestRunResult>? tcs;

            lock (_lock)
            {
                if (!IsRunning)
                {
                    return GetResults(runId);
                }

                if (runId != null && runId != CurrentRunId)
                {
                    return new
                    {
                        error = "Run ID not found",
                        currentRunId = CurrentRunId
                    };
                }

                tcs = _testRunTcs;
            }

            if (tcs == null)
            {
                return GetResults(runId);
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
                        return new
                        {
                            runId = CurrentRunId,
                            status = result.Failed > 0 ? "failed" : "passed",
                            passed = result.Passed,
                            failed = result.Failed,
                            skipped = result.Skipped,
                            total = result.Total,
                            duration = result.DurationSeconds,
                            results = result.Results.Select(r => new
                            {
                                name = r.Name,
                                fullName = r.FullName,
                                resultState = r.ResultState,
                                message = r.Message,
                                stackTrace = r.StackTrace,
                                duration = r.DurationSeconds
                            }).ToArray()
                        };
                    }
                    else
                    {
                        var results = CurrentResults;
                        return new
                        {
                            status = "timeout",
                            message = $"Test run did not complete within {timeoutSeconds} seconds",
                            runId = CurrentRunId,
                            completedTests = results.Count
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    return new
                    {
                        status = "timeout",
                        message = $"Test run did not complete within {timeoutSeconds} seconds",
                        runId = CurrentRunId
                    };
                }
            }
        }

        [Tool(Description = "Cancel the currently running test execution",
            Returns = "Confirmation of cancellation")]
        internal object CancelTestRun()
        {
            lock (_lock)
            {
                if (!IsRunning)
                {
                    return new
                    {
                        cancelled = false,
                        message = "No test run in progress"
                    };
                }

                var runId = CurrentRunId;
                IsRunning = false;
                _testRunTcs?.TrySetCanceled();
                _testRunTcs = null;

                return new
                {
                    cancelled = true,
                    runId,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    message = "Test run cancelled (note: Unity TestRunnerApi does not support programmatic cancellation; run state has been reset)"
                };
            }
        }

        #region Static Callback Handlers

        internal static void OnRunStarted(ITestAdaptor testsToRun)
        {
            lock (_lock)
            {
                var totalTests = CountTests(testsToRun);
                Debug.Log($"[{LogTag}] Run Started: {testsToRun.FullName}, total tests: {totalTests}");
            }
        }

        internal static void OnRunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[{LogTag}] Run Finished: {result.FullName}, " +
                     $"ResultState: {result.ResultState}, " +
                     $"PassCount: {result.PassCount}, " +
                     $"FailCount: {result.FailCount}, " +
                     $"SkipCount: {result.SkipCount}, " +
                     $"Duration: {result.Duration:F2}s");

            lock (_lock)
            {
                IsRunning = false;

                var results = CurrentResults;
                var testRunResult = new TestRunResult
                {
                    Status = result.TestStatus.ToString(),
                    Passed = results.Count(r => r.ResultState == "Passed"),
                    Failed = results.Count(r => r.ResultState == "Failed"),
                    Skipped = results.Count(r => r.ResultState == "Skipped" || r.ResultState == "Ignored"),
                    Total = results.Count,
                    DurationSeconds = (DateTime.UtcNow - RunStartTime).TotalSeconds,
                    Results = new List<TestResultData>(results)
                };

                _testRunTcs?.TrySetResult(testRunResult);
                _testRunTcs = null;
            }
        }

        internal static void OnTestStarted(ITestAdaptor test)
        {
            if (!test.IsSuite)
            {
                Debug.Log($"[{LogTag}] Test Started: {test.FullName}");
            }
        }

        internal static void OnTestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite)
                return;

            Debug.Log($"[{LogTag}] Test Finished: {result.FullName}, " +
                     $"ResultState: {result.ResultState}, " +
                     $"Duration: {result.Duration:F3}s" +
                     (string.IsNullOrEmpty(result.Message) ? "" : $", Message: {result.Message}"));

            AddResult(new TestResultData
            {
                Name = result.Test.Name,
                FullName = result.Test.FullName,
                ResultState = result.TestStatus.ToString(),
                Message = result.Message,
                StackTrace = result.StackTrace,
                DurationSeconds = result.Duration
            });
        }

        #endregion

        private static int CountTests(ITestAdaptor test)
        {
            if (test == null) return 0;
            if (!test.IsSuite) return 1;

            int count = 0;
            if (test.HasChildren && test.Children != null)
            {
                foreach (var child in test.Children)
                {
                    count += CountTests(child);
                }
            }
            return count;
        }

        /// <summary>
        /// Separate callbacks class that implements ICallbacks and delegates to static handlers.
        /// Uses static methods so state survives domain reload (via EditorPrefs).
        /// </summary>
        private class TestRunnerCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                TestRunnerTools.OnRunStarted(testsToRun);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerTools.OnRunFinished(result);
            }

            public void TestStarted(ITestAdaptor test)
            {
                TestRunnerTools.OnTestStarted(test);
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                TestRunnerTools.OnTestFinished(result);
            }
        }
    }
}
