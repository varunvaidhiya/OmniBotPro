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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor.Accessibility;
using UnityEditor.MPE;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Profiling;
using UnityEditor.Profiling.Analytics;
using UnityEditor.Profiling.ModuleEditor;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using System.Diagnostics;
using System.Threading;
#if HAS_AGENT_BRIDGE
using System.Threading.Tasks;
#endif
using System;
using System.Linq;
using UnityEngine;
using System.IO.Compression;
using RO = Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight;
using Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Editor.ToolingSupport;
#if UNITY_OPENXR
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
#endif
#if HAS_AGENT_BRIDGE
using Meta.XR.AI.AgentBridge;
#endif

namespace Meta.XR.RuntimeOptimizer.Editor
{
    /// <summary>
    /// Registers Runtime Optimizer as a first-class tool in the Meta XR Tools dropdown.
    /// </summary>
    [InitializeOnLoad]
    internal static class RuntimeOptimizerToolRegistration
    {
        private const string PublicName = "Quest Runtime Optimizer";
        private const string MenuDescription = "Diagnose Performance Issues";
        private const string ToolDescription =
            "Profile and optimize Quest app performance with real-time GPU/CPU metrics, " +
            "What-If analysis, and draw call inspection.";
        private const string DocumentationUrl =
            "https://developer.oculus.com/documentation/unity/unity-quest-runtime-optimizer";

        internal static readonly TextureContent StatusIcon = TextureContent.CreateContent(
            "ovr_icon_runtimeoptimizer.png",
            new TextureContent.Category("Icons", false, "Meta.XR.RuntimeOptimizer.Editor"));

        internal static readonly ToolDescriptor ToolDescriptor = new()
        {
            Name = PublicName,
            MenuDescription = MenuDescription,
            Description = ToolDescription,
            Color = RuntimeOptimizerWindow.Colors.Meta,
            Icon = StatusIcon,
            Order = 15,
            AddToStatusMenu = true,
            AddToMenu = true,
            CanBeNew = true,
            OnClickDelegate = OnClickDelegate,
            InfoTextDelegate = ComputeInfoText,
            Documentation = new List<Documentation>()
            {
                new()
                {
                    Title = "Developer Documentation",
                    Url = DocumentationUrl
                }
            },
        };

        static RuntimeOptimizerToolRegistration()
        {
        }

        private static void OnClickDelegate(Origins origin)
        {
            RuntimeOptimizerWindow.ShowWindow();
        }

        private static (string?, Color?) ComputeInfoText()
        {
            return (null, null);
        }
    }

    [InitializeOnLoadAttribute]
    public partial class RuntimeOptimizerWindow : EditorWindow
    {

        private const string WindowName = "Quest Runtime Optimizer";
        private const int DisableProxSensorDuration = 600000; // 10 minutes in milliseconds
        private string[] sortingOptions = new string[] { "GPU Frame Time" };
        private int sortingFilterSelection = 0;
        private int totalGameObjectCount = 0;
        private float renderThreadBaseline = 0;
        private float mainThreadBaseline = 0;
        private float samplingVariance = 0;
        private int remainingTimeSeconds = 0;
        private System.Threading.Timer? countdownTimer = null;
        private InsightData insight = new InsightData();
        private InsightType lastSelectedInsight = InsightType.EMPTY_NO_CAPTURES;
        private string lastSelectedInsightName = "";

        private string output = string.Empty;
        private string error = string.Empty;
        // private List<(string, float)> sortedGameObjectTimes = new List<(string, float)>();
        private string executablePath = "";
        private bool executablePathModified = false;
        private List<GameObjectPerformanceMetaData> gameObjectPerformanceDataList = new List<GameObjectPerformanceMetaData>();
        private List<(string, float)> sortedGameObjectTimes = new List<(string, float)>();

        private Vector2 scrollPosition;

        // insight UI use
        private Vector2 insightCapturePosition;
        private Vector2 insightPosition;
        private Dictionary<string, Texture2D> captureImageCache = new Dictionary<string, Texture2D>();
        private Dictionary<string, JSONObject> metricJsonCache = new Dictionary<string, JSONObject>();
        private Dictionary<string, JSONObject> snapshotJsonCache = new Dictionary<string, JSONObject>();
        private Dictionary<string, DateTime> processedJsonCache = new Dictionary<string, DateTime>();
        private const int kSnapshotWidth = 330 - Constants.Margin * 2;
        private const int kSnapshotHeight = 100;
        private string IsCapturingBottlenecks = "";
        private bool isCapturingWhatIf = false;
        private bool isWhatIfGroupMode = false;
        private DateTime lastTimeAdbValidate = default(DateTime);
        private int runtimeServicePID = -1;
        private string bundleName = "*";
        private const string snapshotPostfix = ".json";
        private const string snapshotUnityPostfix = "_unity.json";
        private const string snapshotASW = ".asw";
        private const string sessionid = ".id";
        private const string exportedPackagePostFix = "roz";
        private const string metricJsonOpened = ".op";
        private const int insightListLimit = 10;
        private Dictionary<string, int> insightListLimitDict = new Dictionary<string, int>();
        private Texture? BulletIcon = null;

#if HAS_AGENT_BRIDGE
        // AI Analysis state
        private bool _isAIAnalyzing = false;
        private StringBuilder _aiAnalysisResult = new StringBuilder();
        private string? _aiAnalysisError = null;
        private Vector2 _aiAnalysisScrollPosition;
        private CallerIdentity _aiCallerIdentity = new CallerIdentity("RuntimeOptimizer");
        private string? _bestPracticesContent = null;
#endif
        private Texture? MetaIcon = null;
        private Texture? LightBulbIcon = null;
        private Texture? OptionIcon = null;
        private bool needRepait = false;
        private bool runtimeIsFrozen = false;
        private bool hasAOCInstalled = false;
        private bool playerConnected = false;
        private bool pcTestingMode = false;
        private bool aswIsOn = false;
        private bool invalidOSVersion = false;
        private List<string> frozenGameObjectsForWhatIf = new List<string>();
        private DateTime kLastTimeDisabledSensor = DateTime.Now;

        private Dictionary<string, DateTime> captureItemProcessed = new Dictionary<string, DateTime>();

        // NEW: Quick Perf View fields
        private QuickPerfData? quickPerfMetrics = null;
        private DateTime lastQuickPerfRefresh = DateTime.MinValue;
        private const float kQuickPerfRefreshInterval = 2.0f;
        private const float kQuickPerfTolerenceGPUPercentage = 0.1f;
        private const float kQuickPerfTolerenceCPUPercentage = 0.1f;

        // Telemetry rate limiting for quick_perf_data_received event
        private DateTime lastQuickPerfTelemetrySent = DateTime.MinValue;
        private const float kQuickPerfTelemetryInterval = 60.0f; // Send every 60 seconds

        // New fields for enhanced capture monitoring (T240137389 + T240137791)
        // Used for Bottleneck, What If, and Quick Perf captures
        private DateTime captureStartTime = default(DateTime);
        private int maxCaptureTimeoutSeconds = 60; // Maximum capture duration (dynamically adjusted for What If)
        private bool isActivelyCapturing = false;

        // Staging area for temporary freeze preview (not saved to disk)
        private QuickPerfData? stagingPerfMetrics = null;
        private string stagingFrameName = "";
        private bool hasStagingData = false;
        private DateTime stagingCaptureTime = default(DateTime);
        private bool capturingForStaging = false;

        // Freeze frame cooldown
        private DateTime lastFreezeFrameTime = DateTime.MinValue;
        private const float kFreezeFrameCooldownSeconds = 5.0f;

        // Freeze frame completion tracking (T240137389 - prevent unpause before freeze completes)
        private readonly object freezeFrameLock = new object();
        private bool freezeFrameInProgress = false;
        private DateTime freezeFrameStartTime = default(DateTime);
        private bool freezeFrameScreenshotComplete = false;
        private bool freezeFrameMetricsComplete = false;
        private const float kFreezeFrameTimeoutSeconds = 20.0f;

        // Thread-safe queue for network payloads received on TCP background thread.
        // Replaces EditorApplication.delayCall which stalls when the editor loses focus.
        // Payloads are drained unconditionally at the top of Update(), which runs at a
        // reduced rate when unfocused but never stops entirely.
        private readonly ConcurrentQueue<(int payloadType, string message)> pendingPayloads =
            new ConcurrentQueue<(int payloadType, string message)>();

        // App launch debouncing

        // App launch debouncing (T230481516 - prevent multiple launches from rapid button clicks)
        private DateTime lastLaunchTime = DateTime.MinValue;
        private const float kLaunchCooldownSeconds = 3.0f;
        private bool isLaunchInProgress = false;

        private string currentSessionID = "";
        private string metaCoreSDKVersion = "Unknown";
        private bool sleepNotificationShown = false;
        private bool lostFocusDialogShown = false;
        private int lastKnownDeviceCount = -1;
        private bool windowHasFocus = true;
        private DateTime lastChildWindowClosedTime = DateTime.MinValue;
        private const float kChildWindowClosedGracePeriodSeconds = 0.5f;

        internal void NotifyChildWindowClosed()
        {
            lastChildWindowClosedTime = DateTime.Now;
        }

        // Tool Selection and Active Frame Display
        private enum ToolType { Bottleneck = 0, WhatIf = 1 }
        private ToolType selectedAnalysisTool = ToolType.Bottleneck;
        private string[] analysisToolOptions = new string[] { "Bottleneck Analysis", "What if? Analysis" };
        private Texture2D? activeFrameImage = null;
        private string activeFrameName = "";

        // App Connection State for Dynamic Launch Button
        private enum AppConnectionState
        {
            NotRunning,
            RunningConnected,
            RunningDisconnected
        }
        private AppConnectionState lastKnownAppState = AppConnectionState.NotRunning;

        // Network Error Handling - Phase 3
        private Dictionary<NetworkErrorCategory, int> networkErrorCounts = new Dictionary<NetworkErrorCategory, int>();
        private DateTime lastNetworkErrorTime = DateTime.MinValue;
        private const int MAX_ERROR_EVENTS_PER_MINUTE = 5; // Rate limiting
        private bool gpuProfilingDialogShown = false; // Track if GPU profiling dialog was just shown

        // Option Menu Keys
        private const string EnableAOCKey = "ROEnableAOC";
        private const string EnableFrameFreezeForInsight = "ROFreezeOnFrameCaptureForInsight";
        private const string EnableBackgorundCaptureInight = "ROBackgorundCaptureForInsight";
        private const string RenderUnityPercentageKey = "ROUsePercentage";
        private const string EnableDebugLogToConsole = "EnableRODebugLogToConsole";
        private const string ExtraAssetInfo = "RODisplayExtraInfo";
        private OVRNetwork.OVRNetworkTcpClient client = new OVRNetwork.OVRNetworkTcpClient();
        private int remoteListeningPort = 12345;
        private int headsetOSVersion = 0;

        string SessionIDPath()
        {
            return string.Format("{0}/{1}{2}", CaptureTool.GetOutputDirectory(), "session_id", sessionid);
        }
        string FetchSessionId()
        {
            string saveToFile = SessionIDPath();
            if (File.Exists(saveToFile))
            {
                currentSessionID = File.ReadAllText(saveToFile);
                return currentSessionID;
            }
            else
            {
                return "";
            }
        }
        void StoreSessionId(string sessionId)
        {
            File.WriteAllText(SessionIDPath(), sessionId);
        }
        void InitROPlugin()
        {
            string sessionId = FetchSessionId();
            RO.Util.DebugLog("Sessionid Then: " + sessionId);

            string saveToFilePathGuid = string.Format("{0}/{1}{2}", CaptureTool.GetOutputDirectory(), "guid", sessionid);
            File.WriteAllText(saveToFilePathGuid, "placeholder");
            string assetPath = string.Format("{0}/{1}{2}", CaptureTool.GetAssetOutputFolderName(), "guid", sessionid);

            string scriptGUID = AssetDatabase.AssetPathToGUID(assetPath);
            sessionId = RuntimeOptimizerPlugin.Initialize(sessionId, scriptGUID);
            RO.Util.DebugLog("Sessionid Now: " + sessionId);
            StoreSessionId(sessionId);
        }

#if UNITY_OPENXR
        void ForceMetaQuestInternetPermission()
        {
            try
            {
                var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                if (settings != null)
                {
                    var feature = settings.GetFeature<MetaQuestFeature>();
                    if (feature != null)
                    {
                        EditorPrefs.SetBool("PreviousForceRemoveInternetPermission", feature.ForceRemoveInternetPermission);
                        feature.ForceRemoveInternetPermission = false;
                    }
                }
            }
            catch (Exception ex)
            {
                RO.Util.DebugLog($"Failed to configure MetaQuestFeature.ForceRemoveInternetPermission: {ex.Message}");
            }
        }
        void ResetMetaQuestInternetPermission()
        {
            try
            {
                var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
                if (settings != null)
                {
                    var feature = settings.GetFeature<MetaQuestFeature>();
                    if (feature != null)
                    {
                        feature.ForceRemoveInternetPermission = EditorPrefs.GetBool("PreviousForceRemoveInternetPermission", false);
                    }
                }
            }
            catch (Exception ex)
            {
                RO.Util.DebugLog($"Failed to configure MetaQuestFeature.ForceRemoveInternetPermission: {ex.Message}");
            }
        }
#endif

        void OnEnable()
        {

            // Toggle the enableLog variable
            RO.Util.logToConsole = EditorPrefs.GetBool(EnableDebugLogToConsole, false);

            // Load PC Testing mode from EditorPrefs
            pcTestingMode = EditorPrefs.GetBool("PCTestingMode", false);

            InitROPlugin();
            RuntimeOptimizerPlugin.SendEvent("window_enabled", InsightEventDataStr.ToJsonStr("true"));

#if HAS_AGENT_BRIDGE
            // Debug: Verify AgentBridge integration
            try
            {
                RO.Util.DebugLog("=== AI Analysis Debug ===");
                RO.Util.DebugLog("AgentBridge Service: " + AgentBridgeAPI.GetCurrentServiceName());
                RO.Util.DebugLog("AgentBridge Has Session: " + AgentBridgeAPI.HasActiveSession());
                RO.Util.DebugLog("AgentBridge Is Processing: " + AgentBridgeAPI.IsProcessing());
                RO.Util.DebugLog("=========================");
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLogError("AgentBridge init check failed: " + ex.Message);
            }
#endif

            string newBackstageItemID = System.Guid.NewGuid().ToString();
            RO.Util.DebugLog("Runtime Optimizer: New Backstage Item ID: " + newBackstageItemID);

            lastSelectedInsight = InsightType.EMPTY_NO_CAPTURES;
            // ensure python components are installed
            MetricAPI.RunPythonUtil();
            bool exitCode = CaptureTool.ForwardPort(remoteListeningPort);
            CaptureTool.ValidateAdb();
            RO.Util.DebugLog("Runtime Optimizer: identifier:" + Application.identifier);
            executablePath = CaptureTool.GetExecutablePath(Application.identifier);
            RO.Util.DebugLog("Runtime Optimizer: executablePath: " + executablePath);

            // Extract package name from executable path if it has been set
            if (!string.IsNullOrEmpty(executablePath) && executablePath.Contains("/"))
            {
                // executablePath format is "com.example.package/.MainActivity"
                // Extract just the package name part
                bundleName = executablePath.Split('/')[0];
            }
            else
            {
                bundleName = Application.identifier;
            }
            CaptureTool.ValidateProcess(bundleName);

#if HAS_AGENT_BRIDGE
            AgentBridgeAPI.OnMessageAdded += OnAIMessageReceived;
            AgentBridgeAPI.OnProcessingStateChanged += OnAIProcessingStateChanged;
#endif
        }

        void OnLostFocus()
        {
            windowHasFocus = false;
            CheckWindowFocusDuringCapture();
        }

        void OnFocus()
        {
            windowHasFocus = true;

            // Always reset the lost focus dialog flag when window regains focus
            // This handles the case where capture completes while window doesn't have focus
            // (T243800621: Fix dropdown hang after Build Settings opened during capture)
            lostFocusDialogShown = false;
        }

        void OnDisable()
        {
            client.Disconnect();
            CaptureTool.ReleasePort(remoteListeningPort);
            runtimeServicePID = -1;
            RuntimeOptimizerPlugin.SendEvent("window_disabled", InsightEventDataStr.ToJsonStr("true"));

            // Disconnect when the window is closed
            if (runtimeIsFrozen)
            {
                SendToServer("unfreeze");
                StopHeadlock();
            }

            // Close GameObjectSelectionWindow if it's open
            var selectionWindow = EditorWindow.GetWindow<GameObjectSelectionWindow>(false, "", false);
            if (selectionWindow != null)
            {
                selectionWindow.Close();
            }

#if HAS_AGENT_BRIDGE
            AgentBridgeAPI.OnMessageAdded -= OnAIMessageReceived;
            AgentBridgeAPI.OnProcessingStateChanged -= OnAIProcessingStateChanged;
            AgentBridgeAPI.ClearConversation(_aiCallerIdentity);
#endif
            StopCountdownTimer();
            RuntimeOptimizerPlugin.Shutdown();
        }

#if HAS_AGENT_BRIDGE
        private void OnAIMessageReceived(ConversationMessage message)
        {
            if (message.CallerId != _aiCallerIdentity.Id) return;
            if (message.MessageType == "assistant" || message.MessageType == "thinking")
            {
                if (message.IsDelta)
                    _aiAnalysisResult.Append(message.Content);
                else
                {
                    _aiAnalysisResult.Clear();
                    _aiAnalysisResult.Append(message.Content);
                }
                Repaint();
            }
        }

        private void OnAIProcessingStateChanged(bool isProcessing)
        {
            if (!isProcessing && _isAIAnalyzing)
            {
                _isAIAnalyzing = false;
                var lastError = AgentBridgeAPI.GetLastError();
                if (!string.IsNullOrEmpty(lastError))
                    _aiAnalysisError = lastError;
                Repaint();
            }
        }

        private async void StartAIAnalysisAsync()
        {
            _aiAnalysisResult.Clear();
            _aiAnalysisError = null;
            _isAIAnalyzing = true;
            Repaint();

            try
            {
                if (_bestPracticesContent == null)
                {
                    string scriptPath = PythonUtil.GetScriptPath("PerfettoAnalysisBestPractices.md");
                    if (!string.IsNullOrEmpty(scriptPath) && File.Exists(scriptPath))
                    {
                        _bestPracticesContent = File.ReadAllText(scriptPath);
                    }
                    else
                    {
                        _aiAnalysisError = "Could not find PerfettoAnalysisBestPractices.md";
                        return;
                    }
                }

                string metricJson = metricJsonCache.ContainsKey(lastSelectedInsightName)
                    ? metricJsonCache[lastSelectedInsightName].ToString()
                    : "{}";

                string snapshotJson = snapshotJsonCache.ContainsKey(lastSelectedInsightName)
                    ? snapshotJsonCache[lastSelectedInsightName]?.ToString() ?? ""
                    : "";

                string userPrompt = $"Analyze this VR performance capture data and provide actionable recommendations:\n\n## Perfetto Metrics JSON\n{metricJson}";
                if (!string.IsNullOrEmpty(snapshotJson))
                {
                    userPrompt += $"\n\n## Unity Scene Snapshot\n{snapshotJson}";
                }

                AgentBridgeAPI.EnsureServiceInitialized();
                AgentBridgeAPI.ClearConversation(_aiCallerIdentity);

                bool success = await AgentBridgeAPI.SendPromptAsync(userPrompt, _aiCallerIdentity, systemPrompt: _bestPracticesContent);

                if (!success)
                {
                    _aiAnalysisError = AgentBridgeAPI.GetLastError() ?? "AI Analysis failed";
                    RuntimeOptimizerPlugin.SendEvent("ai_analysis_failed", InsightEventDataStr.ToJsonStr(_aiAnalysisError));
                }
                else
                {
                    RuntimeOptimizerPlugin.SendEvent("ai_analysis_completed", InsightEventDataStr.ToJsonStr(lastSelectedInsightName));
                }

                RuntimeOptimizerPlugin.SendEvent("ai_analysis_initiated", InsightEventDataStr.ToJsonStr("true"));
            }
            catch (System.Exception ex)
            {
                _aiAnalysisError = "AI Analysis failed: " + ex.Message;
                RO.Util.DebugLogError("AI Analysis error: " + ex.Message);
                RuntimeOptimizerPlugin.SendEvent("ai_analysis_error", InsightEventDataStr.ToJsonStr(ex.Message));
            }
            finally
            {
                _isAIAnalyzing = false;
                Repaint();
            }
        }
#endif

        void Update()
        {
            // Drain network payloads queued from the TCP background thread.
            // This runs unconditionally so scan results are processed even when the
            // editor is out of focus (EditorApplication.delayCall stalls in that case).
            while (pendingPayloads.TryDequeue(out var payload))
            {
                ProcessPayload(payload.payloadType, payload.message);
            }

            // Unified 1-second periodic checks for both ADB validation and capture monitoring
            DateTime now = DateTime.Now;
            double lastChecked = (now - lastTimeAdbValidate).TotalSeconds;
            if (lastChecked > 3.0)
            {

                // Standard ADB and process validation (runs every second regardless of capture state)
                if (!isActivelyCapturing)
                {
                    CaptureTool.ValidateAdb();
                    CaptureTool.ValidateProcess(bundleName);
                    if (runtimeServicePID != CaptureTool.lastKnownPID)
                    {
                        RO.Util.DebugLog("Connected device PID changed: " + " -> " + CaptureTool.lastKnownPID);
                        if (CaptureTool.lastKnownPID == -1)
                        {
                            runtimeServicePID = -1;
                        }
                        else
                        {
                            // CRITICAL FIX: When app is detected running after reconnection, sync runtimeServicePID
                            // This handles the case where device disconnects/reconnects while app is still running
                            if (runtimeServicePID == -1 || runtimeServicePID != CaptureTool.lastKnownPID)
                            {
                                RO.Util.DebugLog($"Syncing runtimeServicePID: {runtimeServicePID} -> {CaptureTool.lastKnownPID}");
                                runtimeServicePID = CaptureTool.lastKnownPID;

                                // Send telemetry for PID sync
                                var syncData = new
                                {
                                    oldPID = runtimeServicePID,
                                    newPID = CaptureTool.lastKnownPID,
                                    reason = "periodic_validation_sync",
                                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                };
                                string syncJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(syncData));
                                RuntimeOptimizerPlugin.SendEvent("runtime_pid_synced", syncJson);
                            }
                        }
                        needRepait = true;
                    }

                    // Phase 5: Detect app connection state changes and trigger UI repaint
                    AppConnectionState currentState = GetAppConnectionState();
                    if (currentState != lastKnownAppState)
                    {
                        RO.Util.DebugLog($"App connection state changed: {lastKnownAppState} -> {currentState}");

                        // Send telemetry for state transitions
                        var stateChangeData = new
                        {
                            previousState = lastKnownAppState.ToString(),
                            newState = currentState.ToString(),
                            runtimeServicePID = runtimeServicePID,
                            lastKnownPID = CaptureTool.lastKnownPID,
                            playerConnected = playerConnected,
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        string stateJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(stateChangeData));
                        RuntimeOptimizerPlugin.SendEvent("app_state_changed", stateJson);

                        // BUGFIX: If app stops running while frozen, automatically unfreeze
                        if (currentState == AppConnectionState.NotRunning && runtimeIsFrozen)
                        {
                            RO.Util.DebugLog("App stopped while frozen - automatically unfreezing to prevent headlock");
                            UnfreezeFrame("app_stopped_auto_unfreeze");
                        }

                        lastKnownAppState = currentState;
                        needRepait = true;
                    }

                    // Track device count changes and trigger repaint
                    int currentDeviceCount = CaptureTool.ConnectedDeviceCount();
                    if (lastKnownDeviceCount != currentDeviceCount)
                    {
                        RO.Util.DebugLog($"Device count changed: {lastKnownDeviceCount} -> {currentDeviceCount}");
                        lastKnownDeviceCount = currentDeviceCount;
                        needRepait = true;
                    }
                }


                // Only update executable path when not capturing
                if (!isActivelyCapturing && IsCapturingBottlenecks == "" && !executablePathModified)
                {
                    executablePath = CaptureTool.GetExecutablePath(bundleName);
                }

                // Enhanced monitoring during active capture (includes Quick Perf, Bottleneck, and What If)
                if (isActivelyCapturing)
                {
                    CheckCaptureTimeout();
                    CheckDeviceConnectionDuringCapture();
                    CheckWindowFocusDuringCapture();
                    needRepait = true;
                }

                // Check freeze frame timeout (20 seconds)
                if (freezeFrameInProgress)
                {
                    double freezeElapsedSeconds = (DateTime.Now - freezeFrameStartTime).TotalSeconds;
                    if (freezeElapsedSeconds > kFreezeFrameTimeoutSeconds)
                    {
                        RO.Util.DebugLog($"Freeze frame timeout after {freezeElapsedSeconds:F1}s - screenshot={freezeFrameScreenshotComplete}, metrics={freezeFrameMetricsComplete}");

                        // Mark both as complete to allow unpause (even if incomplete)
                        freezeFrameScreenshotComplete = true;
                        freezeFrameMetricsComplete = true;
                        freezeFrameInProgress = false;

                        var timeoutData = new
                        {
                            duration = freezeElapsedSeconds,
                            screenshotComplete = freezeFrameScreenshotComplete,
                            metricsComplete = freezeFrameMetricsComplete,
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        string timeoutJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(timeoutData));
                        RuntimeOptimizerPlugin.SendEvent("freeze_frame_timeout", timeoutJson);

                        needRepait = true;
                    }
                }

                if (IsDeviceAsleep())
                {
                    if (runtimeServicePID != -1 && !sleepNotificationShown)
                    {
                        RO.Util.DebugLog("Device is asleep, resetting connection state");
                        EditorUtility.DisplayDialog(
                            "Device Disconnected",
                            "The device appears to be asleep and has been disconnected.",
                            "OK"
                        );
                        runtimeServicePID = -1;
                        sleepNotificationShown = true;
                        needRepait = true;
                        if (isActivelyCapturing)
                        {
                            CancelCurrentCapture("Device Sleep. Please check device status and try again.", "device_sleep");
                        }
                    }
                }
                else
                {
                    // Device is awake - reset the notification flag so it can be shown again on next sleep
                    sleepNotificationShown = false;
                }

                lastTimeAdbValidate = now;
            }

            if (needRepait)
            {
                Repaint();
                needRepait = false;
            }

            string[] captures = CaptureTool.GetCapturedList();
            foreach (var item in captures)
            {
                string fileName = Path.GetFileNameWithoutExtension(item);
                if (!ProcessCaptureCaches(fileName, item))
                {
                    break;
                }
                captureItemProcessed[item] = DateTime.Now;
            }
        }

        private static void ToggleEnableDebugLogToConsole()
        {
            bool enableLog = EditorPrefs.GetBool(EnableDebugLogToConsole, false);
            // Toggle the enableLog variable
            enableLog = !enableLog;
            EditorPrefs.SetBool(EnableDebugLogToConsole, enableLog);
            RO.Util.logToConsole = enableLog;
        }

        private void ToggleEnablePCTesting()
        {
            pcTestingMode = !pcTestingMode;
            EditorPrefs.SetBool("PCTestingMode", pcTestingMode);

            // When enabling PC testing mode, don't automatically set playerConnected
            // It should only be set when actually connected
            if (!pcTestingMode)
            {
                playerConnected = false;
                runtimeServicePID = -1;
            }

            needRepait = true;
            RO.Util.DebugLog($"PC Testing mode {(pcTestingMode ? "enabled" : "disabled")}");
        }

        private static void ToggleEnableRenderUnit()
        {
            bool enabled = EditorPrefs.GetBool(RenderUnityPercentageKey, true);
            // Toggle the unit variable
            enabled = !enabled;
            EditorPrefs.SetBool(RenderUnityPercentageKey, enabled);
            RuntimeOptimizerPlugin.SendEvent("toggle_render_unit", InsightEventDataStr.ToJsonStr(enabled.ToString()));
        }

        private static void ToggleEnableExtraAssetInfo()
        {
            bool enabled = EditorPrefs.GetBool(ExtraAssetInfo, false);
            // Toggle the unit variable
            enabled = !enabled;
            EditorPrefs.SetBool(ExtraAssetInfo, enabled);
            RuntimeOptimizerPlugin.SendEvent("toggle_detail_info", InsightEventDataStr.ToJsonStr(enabled.ToString()));
        }

        private static void ToggleEnableAOC()
        {
            bool enableAOC = EditorPrefs.GetBool(EnableAOCKey, false);
            // Toggle the enableAOC variable
            enableAOC = !enableAOC;
            EditorPrefs.SetBool(EnableAOCKey, enableAOC);
            RuntimeOptimizerPlugin.SendEvent("toggle_aoc", InsightEventDataStr.ToJsonStr(enableAOC.ToString()));
        }

        private static void ToggleFreezeOnFrmeCaptureForInsight()
        {
            bool enableFreeze = EditorPrefs.GetBool(EnableFrameFreezeForInsight, true);
            // Toggle the enableAOC variable
            enableFreeze = !enableFreeze;
            EditorPrefs.SetBool(EnableFrameFreezeForInsight, enableFreeze);
            RuntimeOptimizerPlugin.SendEvent("toggle_freeze_on_capture", InsightEventDataStr.ToJsonStr(enableFreeze.ToString()));
        }

        private static void ToggleAutoCaptureForInsight()
        {
            bool enableAutoCapture = EditorPrefs.GetBool(EnableBackgorundCaptureInight, false);
            // Toggle the enableAOC variable
            enableAutoCapture = !enableAutoCapture;
            EditorPrefs.SetBool(EnableBackgorundCaptureInight, enableAutoCapture);

            RuntimeOptimizerPlugin.SendEvent("toggle_auto_capture", InsightEventDataStr.ToJsonStr(enableAutoCapture.ToString()));
        }

        [MenuItem("Meta/Tools/Quest Runtime Optimizer", false, 1)]
        public static void ShowWindow()
        {
            ShowWindow("Menu Item");
        }

        internal static void ShowWindow(string source)
        {
            var window = GetWindow<RuntimeOptimizerWindow>(WindowName);
            window.minSize = new Vector2(750, 750);
            window.maxSize = new Vector2(1400, 1400);

            //TODO: Add telemetry for showing window
        }

        float DrawMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);


            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            // Open Manual
            if (GUILayout.Button(Styles.helpButtonContent, EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://developers.meta.com/horizon/documentation/unity/unity-quest-runtime-optimizer/");
                RuntimeOptimizerPlugin.SendEvent("help_manual_opened", InsightEventDataStr.ToJsonStr(""));
            }

            // Overflow Menu
            var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
            if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButton))
            {
                GenericMenu menu = new GenericMenu();
                bool enableAOC = EditorPrefs.GetBool(EnableAOCKey, false);
                menu.AddItem(new GUIContent("Enable Adreno Offline Compiler"), enableAOC, ToggleEnableAOC);
                bool enableLog = EditorPrefs.GetBool(EnableDebugLogToConsole, false);
                menu.AddItem(new GUIContent("Show Debug Log"), enableLog, ToggleEnableDebugLogToConsole);
                menu.AddItem(new GUIContent("Enable PC Testing"), pcTestingMode, ToggleEnablePCTesting);
                menu.DropDown(overflowMenuRect);
            }

            GUILayout.EndHorizontal();

            return EditorStyles.toolbar.fixedHeight;
        }

        void DrawOverview(string description, string title)
        {
            EditorGUILayout.BeginVertical(Styles.OverviewBox);
            EditorGUILayout.LabelField(title, Styles.DocumentationLabelStyle);
            EditorGUILayout.LabelField(description, Styles.DialogTextStyle);
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        void DrawDescriptionHeader(string description, string steps)
        {
            if (MetaIcon == null)
            {
                MetaIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.meta.xr.sdk.core/Editor/RuntimeOptimizer/Icons/icon_meta_raw.png");
            }
            EditorGUILayout.BeginHorizontal(Styles.OverviewNoticeBox);
            EditorGUILayout.LabelField(
            new GUIContent(MetaIcon),
            new GUIStyle(Styles.IconLargeStyle), GUILayout.Width(Constants.ItemHeight),
            GUILayout.Height(Constants.ItemHeight));

            DrawOverview(description, "Quest Runtime Optimizer");
            DrawOverview(steps, "Steps");
            EditorGUILayout.EndHorizontal();
        }

        public void DrawBulletLine(string text, Color dotColor, int labelWidth = 200)
        {
            if (BulletIcon == null)
            {
                BulletIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.meta.xr.sdk.core/Editor/RuntimeOptimizer/Icons/ro_bullet.png");
            }
            EditorGUILayout.BeginHorizontal();

            var prevColor = GUI.color;
            GUI.color = dotColor;
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel, GUILayout.Width(labelWidth));
            GUI.color = prevColor;

            prevColor = GUI.contentColor;
            GUI.contentColor = dotColor;
            {
                EditorGUILayout.LabelField(
                    new GUIContent(BulletIcon),
                    new GUIStyle(Styles.IconStyle), GUILayout.Width(Constants.SmallIconSize),
                    GUILayout.Height(Constants.SmallIconSize));
            }
            GUI.contentColor = prevColor;
            EditorGUILayout.EndHorizontal();
        }

        void DrawBuildSettings()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Quest Runtime Optimizer Enabled", GUILayout.Width(205));
            string currentDefines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            bool runtimeOptimizerPreviouslyEnabled = currentDefines.Contains("ENABLE_RUNTIME_OPTIMIZER");
            bool runtimeOptimizerEnabled = EditorGUILayout.Toggle(runtimeOptimizerPreviouslyEnabled, GUILayout.Width(20));
            if (runtimeOptimizerPreviouslyEnabled != runtimeOptimizerEnabled)
            {
                EditorPrefs.SetBool("RuntimeOptimizerEnabled", runtimeOptimizerEnabled);
                UpdateROEnabled(runtimeOptimizerEnabled);
            }
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            bool previousGUIEnabled = GUI.enabled;
            GUI.enabled = !isActivelyCapturing;
            if (GUILayout.Button("Open Build Settings", Styles.ButtonStyle, GUILayout.Width(150)))
            {
                CaptureTool.Init(bundleName);
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
                RuntimeOptimizerPlugin.SendEvent("open_build_setting", InsightEventDataStr.ToJsonStr(""));
            }
            GUI.enabled = previousGUIEnabled;

            EditorGUILayout.EndHorizontal();

        }

        void DrawConnectionSetting()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Device", EditorStyles.boldLabel, GUILayout.Width(50));
            var defaultColor = GUI.color;
            // pull OVRADBTool directly here will cause race condition w/ capturing.
            string sdkInfo = (headsetOSVersion.ToString() ?? "Unknown") + ", UnityEditorVersion: " + (Application.unityVersion ?? "Unknown") + ", GraphicsAPI: " + (SystemInfo.graphicsDeviceType.ToString() ?? "Unknown") + ", MetaCoreSDK: " + metaCoreSDKVersion;

            int connectionDeviceCount = CaptureTool.ConnectedDeviceCount();
            AppConnectionState currentConnectionState = GetAppConnectionState();
            RO.Util.DebugLog($"DrawConnectionSetting: DeviceCount={connectionDeviceCount}, IsCapturing={IsCapturingBottlenecks}, playerConnected={playerConnected}, runtimeServicePID={runtimeServicePID}, lastKnownPID={CaptureTool.lastKnownPID}, AppConnectionState={currentConnectionState}");

            // FIXED LOGIC: Handle PC Testing Mode separately from device mode
            if (pcTestingMode)
            {
                // PC Testing Mode: Check TCP connection and runtimeServicePID only
                bool isTcpConnected = (client != null && client.Connected);
                UnityEngine.Debug.Log($"[DRAW_CONNECTION_PC_MODE] tcpConnected={isTcpConnected}, runtimeServicePID={runtimeServicePID}, playerConnected={playerConnected}");

                if (isTcpConnected && runtimeServicePID > 0)
                {
                    DrawBulletLine("Connected (PC Mode)", Colors.SuccessColor, 145);
                    if (!playerConnected)
                    {
                        playerConnected = true;
                        RuntimeOptimizerPlugin.SendEvent("pc_mode_connected", InsightEventDataStr.ToJsonStr(""));
                    }
                }
                else
                {
                    DrawBulletLine("Not Connected (PC Mode)", Colors.ErrorColor, 160);
                    if (playerConnected)
                    {
                        playerConnected = false;
                        RuntimeOptimizerPlugin.SendEvent("pc_mode_disconnected", InsightEventDataStr.ToJsonStr(""));
                    }
                }
            }
            else
            {
                if (connectionDeviceCount == 0)
                {
                    if (playerConnected)
                    {
                        RuntimeOptimizerPlugin.SendEvent("headset_not_connected", InsightEventDataStr.ToJsonStr(""));
                    }
                    DrawBulletLine("Not Connected", Colors.ErrorColor, 95);
                    playerConnected = false;
                }
                else if (invalidOSVersion && (currentConnectionState == AppConnectionState.RunningConnected || currentConnectionState == AppConnectionState.RunningDisconnected))
                {
                    DrawBulletLine("Connected. Invalid OS", Colors.WarningColor, 180);
                    if (!playerConnected)
                    {
                        RuntimeOptimizerPlugin.SendEvent("headset_connect_failed", InsightEventDataStr.ToJsonStr(sdkInfo));
                    }
                }
                else if (currentConnectionState == AppConnectionState.RunningConnected)
                {
                    DrawBulletLine("Connected", Colors.SuccessColor, 70);
                    if (!playerConnected)
                    {
                        RuntimeOptimizerPlugin.SendEvent("headset_connected", InsightEventDataStr.ToJsonStr(sdkInfo));
                        playerConnected = true;
                    }
                }
                else if (currentConnectionState == AppConnectionState.RunningDisconnected)
                {
                    DrawBulletLine("App Running. Not Connected", Colors.WarningColor, 195);
                    if (playerConnected)
                    {
                        RuntimeOptimizerPlugin.SendEvent("headset_disconnected_while_running", InsightEventDataStr.ToJsonStr(""));
                    }
                    playerConnected = false;
                }
                else if (connectionDeviceCount > 0)
                {
                    // Device is connected but app/runtime service not running
                    if (playerConnected)
                    {
                        RuntimeOptimizerPlugin.SendEvent("headset_connected_not_launched", InsightEventDataStr.ToJsonStr(""));
                    }
                    DrawBulletLine("Connected. App not launched", Colors.WarningColor, 185);
                    playerConnected = false;
                    DisableProximitySensor();
                }
                else
                {
                    // Fallback case - should not normally happen with the above logic
                    DrawBulletLine("Unknown Status", Colors.ErrorColor, 95);
                    playerConnected = false;
                }
            }
            GUI.color = defaultColor;

            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            DrawAppLaunchField();

            GUI.color = defaultColor;

            EditorGUILayout.EndHorizontal();
        }

        void DrawQuickPerfView()
        {
            if (!playerConnected)
            {
                DrawQuickPerfDisconnected();
                return;
            }

            // Log when Quick Perf View is displayed (once per session)
            if (quickPerfMetrics == null)
            {
                RuntimeOptimizerPlugin.SendEvent("quick_perf_view_displayed", InsightEventDataStr.ToJsonStr("first_time"));
            }

            EditorGUILayout.BeginHorizontal(Styles.QuickPerfBox);

            // Performance metrics display
            if (quickPerfMetrics != null && quickPerfMetrics.isValid)
            {
                DrawQuickPerfMetrics(quickPerfMetrics);
            }
            else
            {
                EditorGUILayout.LabelField("📊 Click refresh to get performance insights", EditorStyles.boldLabel);
            }

            GUILayout.FlexibleSpace();

            // Refresh button
            bool canRefresh = (DateTime.Now - lastQuickPerfRefresh).TotalSeconds > kQuickPerfRefreshInterval;
            bool isWaitingForResponse = isActivelyCapturing;
            GUI.enabled = canRefresh && playerConnected && !isWaitingForResponse;

            string buttonText = isWaitingForResponse ? "⏳ Waiting..." : "🔄 Refresh";
            if (GUILayout.Button(buttonText, Styles.ButtonStyle, GUILayout.Width(90)))
            {
                RequestQuickPerfUpdate();
                lastQuickPerfRefresh = DateTime.Now;
                RuntimeOptimizerPlugin.SendEvent("quick_perf_refresh_clicked", "");
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Suggestion row
            if (quickPerfMetrics != null && quickPerfMetrics.isValid)
            {
                DrawQuickPerfSuggestion(quickPerfMetrics);
            }
        }

        private void DrawQuickPerfMetrics(QuickPerfData data)
        {
            // Target frame time for VR (11.1ms for 90fps)
            float targetFrameTime = 14.2f;

            // CPU Frame Time
            var cpuColor = GetPerformanceColor(data.cpuFrameTime, targetFrameTime);
            GUI.color = cpuColor;
            EditorGUILayout.LabelField($"CPU: {data.cpuFrameTime:F1}ms", EditorStyles.boldLabel, GUILayout.Width(80));

            // GPU Frame Time
            var gpuColor = GetPerformanceColor(data.gpuFrameTime, targetFrameTime);
            GUI.color = gpuColor;
            EditorGUILayout.LabelField($"GPU: {data.gpuFrameTime:F1}ms", EditorStyles.boldLabel, GUILayout.Width(80));

            // Target Frame Time
            GUI.color = Colors.InfoColor;
            EditorGUILayout.LabelField($"Target: {targetFrameTime:F1}ms", EditorStyles.label, GUILayout.Width(90));

            GUI.color = Color.white;
        }

        private Color GetPerformanceColor(float frameTime, float targetTime)
        {
            float ratio = frameTime / targetTime;
            if (ratio < 0.8f) return Colors.SuccessColor;      // Green - good performance
            if (ratio < 0.95f) return Colors.WarningColor;     // Yellow - concerning
            return Colors.ErrorColor;                          // Red - poor performance
        }

        private void DrawQuickPerfSuggestion(QuickPerfData data)
        {
            EditorGUILayout.BeginHorizontal(Styles.QuickPerfSuggestionBox);

            string suggestion = GetPerformanceSuggestion(data);
            EditorGUILayout.LabelField(suggestion, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndHorizontal();
        }

        private string GetPerformanceSuggestion(QuickPerfData data)
        {
            float targetFrameTime = 14.2f; // Target for VR
            string suggestionType;
            string suggestion;

            // CPU-bound: CPU time exceeds target but GPU time is under target
            if (data.cpuFrameTime > targetFrameTime && data.gpuFrameTime < targetFrameTime)
            {
                suggestionType = "cpu_bound";
                suggestion = "💡 CPU-bound: Use Unity Profiler to learn more about CPU bottlenecks";
            }
            // GPU-bound: GPU time exceeds target but CPU time is under target
            else if (data.gpuFrameTime > targetFrameTime && data.cpuFrameTime < targetFrameTime)
            {
                suggestionType = "gpu_bound";
                suggestion = "💡 GPU-bound: Use Bottleneck Analysis or What If? Analysis for detailed GPU insights";
            }
            // Both CPU and GPU over target
            else if (data.cpuFrameTime > targetFrameTime && data.gpuFrameTime > targetFrameTime)
            {
                suggestionType = "both_bound";
                suggestion = "⚠️ Both CPU and GPU under stress. Consider reducing overall scene complexity";
            }
            else
            {
                suggestionType = "good_performance";
                suggestion = "✅ Performance looks good! Consider Bottleneck Analysis for more GPU optimization ";
            }

            // Log performance suggestion with detailed metrics
            var suggestionEventData = new
            {
                suggestionType = suggestionType,
                cpuFrameTime = data.cpuFrameTime,
                gpuFrameTime = data.gpuFrameTime,
                targetFrameTime = targetFrameTime,
                cpuRatio = data.cpuFrameTime / targetFrameTime,
                gpuRatio = data.gpuFrameTime / targetFrameTime,
                frameRate = data.frameRate,
                drawCallsCount = data.drawCallsCount,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string suggestionJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(suggestionEventData));
            RuntimeOptimizerPlugin.SendEvent("quick_perf_suggestion_generated", suggestionJson);

            return suggestion;
        }

        private void DrawQuickPerfDisconnected()
        {
            EditorGUILayout.BeginHorizontal(Styles.QuickPerfBox);
            EditorGUILayout.LabelField("Connect device to view performance metrics", EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            // Log when Quick Perf View is shown but disconnected
            RuntimeOptimizerPlugin.SendEvent("quick_perf_view_disconnected", InsightEventDataStr.ToJsonStr("device_not_connected"));
        }

        void DrawSeperator(bool useBottomMargin = true)
        {
            EditorGUILayout.Separator();
            if (useBottomMargin)
            {
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = Colors.DarkerGray;
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUI.backgroundColor = prevColor;
            }
            EditorGUILayout.Separator();
        }

        void DrawAnalyzeMenu(string name, string description,
                             string durationNote, string note, GUIStyle noteBK,
                             int notewidth, string buttonLabelName,
                             bool enabled,
                             string tooltipMessage = "",
                             Action<Rect>? onOptionMenu = null,
                             Action? onAnalyzeButton = null,
                             bool showFrozenIcon = false)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(140));

                if (showFrozenIcon)
                {
                    EditorGUILayout.LabelField("Frozen", noteBK, GUILayout.Width(50));
                }

                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField(durationNote, noteBK, GUILayout.Width(60));
                // Overflow Menu
                var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
                if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButton))
                {
                    onOptionMenu?.Invoke(overflowMenuRect);
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField(description, Styles.DialogTextStyle, GUILayout.Height(35), GUILayout.MaxWidth(position.width * 0.35f));

            EditorGUILayout.LabelField(note, noteBK, GUILayout.Width(notewidth));

            EditorGUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string tooltipText = enabled ? "" : tooltipMessage;
            GUI.enabled = enabled;
            if (GUILayout.Button(new GUIContent(buttonLabelName, tooltipText), Styles.ButtonMiddleStyle, GUILayout.Width(150)))
            {
                //TODO: Callback to perform specific Analyze
                onAnalyzeButton?.Invoke();

                RuntimeOptimizerPlugin.SendEvent("on_analyze_button", InsightEventDataStr.ToJsonStr(buttonLabelName));
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // New helper methods for enhanced capture monitoring (T240137791)
        private void CheckCaptureTimeout()
        {
            if (captureStartTime != default(DateTime))
            {
                var elapsed = DateTime.Now - captureStartTime;
                if (elapsed.TotalSeconds > maxCaptureTimeoutSeconds)
                {
                    RO.Util.DebugLogError("Capture timeout: Device may have been disconnected or capture failed");
                    CancelCurrentCapture("Capture timed out. Please check device connection and try again.", "capture_timeout");
                }
            }
        }

        private void CheckDeviceConnectionDuringCapture()
        {
            int deviceCount = CaptureTool.ConnectedDeviceCount();

            // Enhanced debug logging to track the issue
            RO.Util.DebugLog($"CheckDeviceConnectionDuringCapture: DeviceCount={deviceCount}, IsCapturing={IsCapturingBottlenecks}");

            if (deviceCount == 0 || (GetAppConnectionState() == AppConnectionState.RunningDisconnected))
            {
                RO.Util.DebugLogError($"Device disconnected during capture - DeviceCount: {deviceCount}");

                // IMMEDIATE UI UPDATE: Set playerConnected to false right away when device is disconnected
                playerConnected = false;
                needRepait = true;

                string failureReason = deviceCount == 0 ? "headset_disconnection" : "runtime_service_lost";
                CancelCurrentCapture("Device disconnected during capture. Please reconnect device and try again.", failureReason);
            }
        }

        private void CheckWindowFocusDuringCapture()
        {
            if (!windowHasFocus && !lostFocusDialogShown)
            {
                bool gameObjectSelectionWindowOpen = EditorWindow.HasOpenInstances<GameObjectSelectionWindow>();
                if (gameObjectSelectionWindowOpen)
                {
                    return;
                }

                double timeSinceChildWindowClosed = (DateTime.Now - lastChildWindowClosedTime).TotalSeconds;
                if (timeSinceChildWindowClosed < kChildWindowClosedGracePeriodSeconds)
                {
                    return;
                }

                EditorWindow? currentlyFocusedWindow = EditorWindow.focusedWindow;
                if (currentlyFocusedWindow == this)
                {
                    windowHasFocus = true;
                    return;
                }

                if (isActivelyCapturing)
                {
                    EditorUtility.DisplayDialog(
                        "Warning",
                        "Warning: Don't take Runtime Optimizer out of focus. Errors may occur. Retrying capture is recommended.",
                        "OK"
                    );
                    lostFocusDialogShown = true;

                    var focusLostData = new
                    {
                        isCapturingBottleneck = !string.IsNullOrEmpty(IsCapturingBottlenecks),
                        isCapturingWhatIf = isCapturingWhatIf,
                        runtimeIsFrozen = runtimeIsFrozen,
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    string focusLostJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(focusLostData));
                    RuntimeOptimizerPlugin.SendEvent("window_lost_focus_during_capture", focusLostJson);
                }
            }
        }


        private void CancelCurrentCapture(string errorMessage, string failureReason = "device_disconnection", bool showDialog = true)
        {
            bool wasCapturing = isActivelyCapturing || IsCapturingBottlenecks != "" || isCapturingWhatIf || capturingForStaging;
            // Reset capture state for both Bottleneck Analysis and What If Analysis
            if (IsCapturingBottlenecks != "")
            {
                RemoveCapture(Path.GetFileNameWithoutExtension(IsCapturingBottlenecks));
            }
            IsCapturingBottlenecks = "";
            isCapturingWhatIf = false;
            isActivelyCapturing = false;
            captureStartTime = default(DateTime);
            capturingForStaging = false;

            // Stop countdown timer for What If Analysis
            StopCountdownTimer();

            // Reset frozen state if needed
            if (runtimeIsFrozen)
            {
                SendToServer("unfreeze");
                StopHeadlock();
            }

            runtimeIsFrozen = false;

            // Clear staging data
            ClearStagingData();

            // Clear active frame display
            activeFrameImage = null;
            activeFrameName = "";

            // Clear What If data
            gameObjectPerformanceDataList.Clear();
            frozenGameObjectsForWhatIf.Clear();

            // Clear Quick Perf metrics
            quickPerfMetrics = null;

            // Reset lost focus dialog flag when capture ends (T243800621)
            lostFocusDialogShown = false;

            // Force UI update
            needRepait = true;

            // Only set playerConnected to false if the failure is actually due to device disconnection
            // For timeouts, the device might still be connected and we should allow retry
            if (failureReason == "headset_disconnection" || failureReason == "runtime_service_lost")
            {
                playerConnected = false;
                RO.Util.DebugLog($"Device disconnection detected during capture - disabling UI until reconnection");
            }
            else
            {
                RO.Util.DebugLog($"Capture failed due to {failureReason} but device may still be connected - keeping UI enabled for retry");
            }

            // Show error dialog to user (optional)
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Capture Failed", errorMessage, "OK");
            }

            // Enhanced event logging with specific failure reasons
            if (wasCapturing)
            {
                var failureEventData = new
                {
                    errorMessage = errorMessage,
                    failureReason = failureReason,
                    captureTimeElapsed = captureStartTime != default(DateTime) ?
                        (DateTime.Now - captureStartTime).TotalSeconds : 0,
                    deviceConnectedAtFailure = CaptureTool.ConnectedDeviceCount() > 0,
                    runtimeServicePID = CaptureTool.lastKnownPID,
                    wasInFreezeMode = runtimeIsFrozen,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string eventJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(failureEventData));
                RuntimeOptimizerPlugin.SendEvent($"capture_failed_{failureReason}", eventJson);
                RO.Util.DebugLogError($"Capture failed - Reason: {failureReason}, Message: {errorMessage}");
            }
        }

        void IssueCaptureInsight(bool freezeCamera = false)
        {
            string captureMode = freezeCamera ? "captureInsightsFreeze" : "captureInsights";
            DisableProximitySensor();

            // Set capture monitoring flags
            isActivelyCapturing = true;
            captureStartTime = DateTime.Now;

            SendToServer(captureMode);
            RO.Util.DebugLog("Capture initiated with timeout monitoring");
        }

        void DrawBottleneckTool(bool enableAnalyze)
        {
            const string title = "Bottleneck Analysis";
            const string description = "Shows insights on possible graphics bottlenecks based on your target FPS.";
            const string durationNote = "~ 25 sec";
            const string highlight = "High-Level Direction";

            // Allow enabling in PC Testing mode even if not connected
            if (pcTestingMode && !playerConnected)
            {
                enableAnalyze = true;
            }

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // Header row
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(140));

            if (runtimeIsFrozen)
            {
                EditorGUILayout.LabelField("Frozen", Styles.InsightToolNote, GUILayout.Width(50));
            }
            else
            {
                EditorGUILayout.Space(20);
            }

            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(durationNote, Styles.InsightToolNote, GUILayout.Width(60));

            // Overflow Menu
            var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
            if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButton))
            {
                GenericMenu menu = new GenericMenu();
                menu.DropDown(overflowMenuRect);
            }
            GUILayout.EndHorizontal();


            EditorGUILayout.LabelField(description, Styles.DialogTextStyle, GUILayout.Height(35), GUILayout.MaxWidth(position.width * 0.35f));
            EditorGUILayout.LabelField(highlight, Styles.InsightToolNote, GUILayout.Width(135));

            EditorGUILayout.Space(30);

            // Analyze button - full width
            bool canCapture = (IsCapturingBottlenecks == "");
            bool enableAnalyzeButton = enableAnalyze && canCapture && runtimeIsFrozen && !freezeFrameInProgress;
            string analyzeButtonLabel = canCapture ? "Analyze" : "Processing...";
            string analyzeTooltip = !enableAnalyzeButton ? (freezeFrameInProgress ? "Waiting for freeze to complete..." : "Freeze a frame first to analyze") : "";

            GUI.enabled = enableAnalyzeButton;
            if (GUILayout.Button(new GUIContent(analyzeButtonLabel, analyzeTooltip), Styles.ButtonMiddleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                // IMPORTANT: OnReceivedCaptureRequest generates capture name using EpochTime,
                // so we set IsCapturingBottlenecks to empty string to allow it to be set automatically
                bool useExistingFrame = activeFrameImage != null && !string.IsNullOrEmpty(activeFrameName);
                string telemetryEvent = useExistingFrame ? "insight_analyze_existing_frame" : "insight_capture_button";

                // Set to empty to let OnReceivedCaptureRequest set the correct name
                IsCapturingBottlenecks = "";
                needRepait = true;
                Repaint();

                try
                {
                    // Trigger capture insight analysis - this will cause OnReceivedCaptureRequest to be called
                    // which will generate the proper capture name and set IsCapturingBottlenecks
                    IssueCaptureInsight(false);

                    // Send telemetry for successful initiation
                    var eventData = new InsightEventData();
                    eventData.IsForzen = true;
                    string json = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(eventData));
                    RuntimeOptimizerPlugin.SendEvent(telemetryEvent, json);
                }
                catch (System.Exception ex)
                {
                    // Reset state if capture initiation fails
                    IsCapturingBottlenecks = "";
                    isActivelyCapturing = false;
                    needRepait = true;

                    // Log capture initiation failure with detailed telemetry
                    var initiationFailureData = new
                    {
                        errorMessage = ex.Message,
                        exceptionType = ex.GetType().Name,
                        stackTrace = ex.StackTrace,
                        deviceConnected = CaptureTool.ConnectedDeviceCount() > 0,
                        runtimeServicePID = CaptureTool.lastKnownPID,
                        freezeModeEnabled = true,
                        useExistingFrame = useExistingFrame,
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    string failureJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(initiationFailureData));
                    RuntimeOptimizerPlugin.SendEvent("capture_initiation_failed", failureJson);
                    RO.Util.DebugLogError("Failed to initiate capture: " + ex.Message);
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        void DrawWhatIfTool(bool enableAnalyze)
        {
            const string title = "What if? Analysis";
            const string description = "Find out how much time can be saved for each GameObject.";
            const string durationNote = "~ 1 min";
            const string highlight = "Detailed & Precise";

            // Allow enabling in PC Testing mode even if not connected
            if (pcTestingMode && !playerConnected)
            {
                enableAnalyze = true;
            }

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // Header row
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(140));

            if (runtimeIsFrozen)
            {
                EditorGUILayout.LabelField("Frozen", Styles.GSDToolNote, GUILayout.Width(50));
            }
            else
            {
                EditorGUILayout.Space(20);
            }

            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(durationNote, Styles.GSDToolNote, GUILayout.Width(60));

            // Overflow Menu
            var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
            if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButton))
            {
                GenericMenu menu = new GenericMenu();
                menu.DropDown(overflowMenuRect);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField(description, Styles.DialogTextStyle, GUILayout.Height(35), GUILayout.MaxWidth(position.width * 0.35f));
            EditorGUILayout.LabelField(highlight, Styles.GSDToolNote, GUILayout.Width(120));

            EditorGUILayout.Space(25);

            // Analyze button - full width
            bool canCapture = !isCapturingWhatIf && (IsCapturingBottlenecks == "");
            bool enableAnalyzeButton = enableAnalyze && canCapture && runtimeIsFrozen && !freezeFrameInProgress;
            string analyzeButtonLabel = isCapturingWhatIf ? "Processing..." : "Analyze";
            string analyzeTooltip = !enableAnalyzeButton ? (freezeFrameInProgress ? "Waiting for freeze to complete..." : "Freeze a frame first to analyze") : "";

            GUI.enabled = enableAnalyzeButton;
            if (GUILayout.Button(new GUIContent(analyzeButtonLabel, analyzeTooltip), Styles.ButtonMiddleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                // Open GameObject selection dialog
                ShowGameObjectSelectionDialog();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        void ShowGameObjectSelectionDialog()
        {
            // Request frustrum GameObjects from runtime
            RO.Util.DebugLog("Requesting frustrum GameObjects from RuntimeOptimizerService...");

            // Ensure connection is established
            if (!EnsureConnection())
            {
                string errorMessage = pcTestingMode
                    ? "Failed to connect to RuntimeOptimizerService.\n\nPlease ensure:\n" +
                      "1. Unity Editor or PC build is running\n" +
                      "2. RuntimeOptimizerService component is active in the scene\n" +
                      "3. ENABLE_RUNTIME_OPTIMIZER is defined in Project Settings\n" +
                      "4. The game is playing (press Play in Unity Editor)"
                    : "Failed to connect to device.\n\nPlease ensure:\n" +
                      "1. Device is connected via USB\n" +
                      "2. App is running on the device\n" +
                      "3. RuntimeOptimizerService is active";

                EditorUtility.DisplayDialog("Connection Failed", errorMessage, "OK");
                return;
            }

            try
            {
                // Request frustrum GameObjects via network
                SendToServer("getFrustrumGameObjects");

                RO.Util.DebugLog("Frustrum GameObjects request sent");
                RO.RuntimeOptimizerPlugin.SendEvent("frustrum_gameobjects_requested",
                    InsightEventDataStr.ToJsonStr("what_if_analysis"));
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLogError("Failed to request GameObject list: " + ex.Message);
                EditorUtility.DisplayDialog("Error",
                    $"Failed to request GameObject list from runtime.\n\nError: {ex.Message}", "OK");
            }
        }

        void ShowGameObjectSelectionDialog(System.Collections.Generic.List<string> gameObjectPaths)
        {
            // This method is called when we receive the frustrum GameObject list
            try
            {
                // Show the selection window with the received GameObject paths
                GameObjectSelectionWindow.ShowWindow(this, gameObjectPaths);

                RO.Util.DebugLog($"Showing GameObject selection dialog with {gameObjectPaths.Count} objects");
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLogError("Failed to show GameObject selection dialog: " + ex.Message);
                EditorUtility.DisplayDialog("Error",
                    $"Failed to show GameObject selection dialog.\n\nError: {ex.Message}", "OK");
            }
        }

        internal void StartSelectiveWhatIfAnalysis(List<string> selectedGameObjects, bool runAsGroup = false)
        {
            if (selectedGameObjects == null || selectedGameObjects.Count == 0)
            {
                RO.Util.DebugLogError("No GameObjects selected for What If analysis");
                return;
            }

            if (!runtimeIsFrozen)
            {
                RO.Util.DebugLogError("Cannot start What If analysis: Frame is not frozen. Please freeze a frame first.");
                EditorUtility.DisplayDialog(
                    "Frame Not Frozen",
                    "Cannot start What If analysis because the frame is no longer frozen.\n\n" +
                    "Please freeze a frame first using the 'Freeze frame' button, then try again.",
                    "OK"
                );

                var blockedEventData = new
                {
                    reason = "frame_not_frozen",
                    selectedGameObjectCount = selectedGameObjects.Count,
                    runAsGroup = runAsGroup,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string blockedJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(blockedEventData));
                RuntimeOptimizerPlugin.SendEvent("selective_whatif_blocked_frame_not_frozen", blockedJson);
                return;
            }

            // IMMEDIATE UI FEEDBACK: Set capturing state right away
            isCapturingWhatIf = true;
            isWhatIfGroupMode = runAsGroup;

            // Set capture monitoring flags for What If Analysis
            isActivelyCapturing = true;
            captureStartTime = DateTime.Now;

            // Force immediate UI update
            needRepait = true;
            Repaint();

            try
            {
                HeadlockCameraRigPosition();

                // Send selective perftest command with specific GameObjects
                // Include GROUP flag if running in group mode
                string messagePrefix = runAsGroup ? "startPerfTest:GROUP" : "startPerfTest";
                string message = messagePrefix + ";" + string.Join(";", selectedGameObjects);
                string modeStr = runAsGroup ? " (GROUP MODE)" : " (INDIVIDUAL MODE)";
                RO.Util.DebugLog($"Sending selective perftest for {selectedGameObjects.Count} GameObjects{modeStr}");

                if (client != null && client.Connected)
                {
                    byte[] payload = Encoding.ASCII.GetBytes(message);
                    client.Send(PerformanceDataUtils.RuntimeOptimizerServiceRequestId, payload);
                }

                DisableProximitySensor();
                gameObjectPerformanceDataList.Clear();

                RuntimeOptimizerPlugin.SendEvent("selective_what_if_analysis_started", InsightEventDataStr.ToJsonStr($"Count:{selectedGameObjects.Count},GroupMode:{runAsGroup}"));
            }
            catch (System.Exception ex)
            {
                // Reset state if capture initiation fails
                isCapturingWhatIf = false;
                isWhatIfGroupMode = false;
                isActivelyCapturing = false;
                captureStartTime = default(DateTime);
                needRepait = true;

                // Log capture initiation failure with detailed telemetry
                var initiationFailureData = new
                {
                    errorMessage = ex.Message,
                    exceptionType = ex.GetType().Name,
                    stackTrace = ex.StackTrace,
                    deviceConnected = CaptureTool.ConnectedDeviceCount() > 0,
                    runtimeServicePID = CaptureTool.lastKnownPID,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string failureJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(initiationFailureData));
                RuntimeOptimizerPlugin.SendEvent("selective_whatif_initiation_failed", failureJson);
                RO.Util.DebugLogError("Failed to initiate Selective What If analysis: " + ex.Message);
            }
        }

        void DrawToolSelector()
        {
            EditorGUILayout.BeginHorizontal();

            int previousSelection = (int)selectedAnalysisTool;

            // Disable dropdown when actively capturing
            GUI.enabled = !isActivelyCapturing;
            selectedAnalysisTool = (ToolType)EditorGUILayout.Popup(
                (int)selectedAnalysisTool,
                analysisToolOptions,
                GUILayout.Width(200)
            );
            GUI.enabled = true;

            if (previousSelection != (int)selectedAnalysisTool)
            {
                // Don't automatically unfreeze when switching tools - keep the frozen state

                // Close GameObjectSelectionWindow if switching away from WhatIf tool
                if (previousSelection == (int)ToolType.WhatIf)
                {
                    var selectionWindow = EditorWindow.GetWindow<GameObjectSelectionWindow>(false, "", false);
                    if (selectionWindow != null)
                    {
                        selectionWindow.Close();
                    }
                }

                // Determine tool name for telemetry
                string toolName = selectedAnalysisTool == ToolType.Bottleneck ? "bottleneck" : "whatif";

                // Send telemetry
                var eventData = new { tool = toolName, timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
                string json = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(eventData));
                RuntimeOptimizerPlugin.SendEvent("analysis_tool_selected", json);

            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawActiveFramePanel()
        {
            bool enableAnalyze = playerConnected && !isCapturingWhatIf && !isActivelyCapturing;

            EditorGUILayout.BeginVertical(GUILayout.Height(150));

            // Title and timestamp on the same line
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Frame To Analyze", GUILayout.Width(150));
            if (!string.IsNullOrEmpty(activeFrameName))
            {
                DateTime captureDate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToInt64(activeFrameName, 16));
                string captureDateStr = captureDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                EditorGUILayout.LabelField($"{captureDateStr}", EditorStyles.label);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Horizontal layout: frame expands, metrics fixed width
            EditorGUILayout.BeginHorizontal();

            // Left side: Frame with darker background - expands to fill available space
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            var containerStyle = new GUIStyle();
            containerStyle.normal.background = CreateTextureFromColor(Colors.DarkerGray);
            containerStyle.padding = new RectOffset(Constants.Margin, Constants.Margin, Constants.Margin, Constants.Margin);
            containerStyle.margin = new RectOffset(0, 0, 0, 0);

            EditorGUILayout.BeginVertical(containerStyle, GUILayout.ExpandWidth(true));

            // Display image or grey placeholder - centered
            if (activeFrameImage != null)
            {
                // Center the snapshot image with reduced height
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(5);
                GUILayout.Box(activeFrameImage, GUILayout.Width(kSnapshotWidth * 0.85f), GUILayout.Height(kSnapshotHeight * 0.85f));
                EditorGUILayout.Space(5);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Center the grey placeholder with reduced height
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space(5);
                var greyStyle = new GUIStyle();
                greyStyle.normal.background = CreateTextureFromColor(Colors.DarkGray);
                greyStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Box("Freeze frame to start", greyStyle,
                    GUILayout.Width(kSnapshotWidth * 0.75f), GUILayout.Height(kSnapshotHeight * 0.85f));
                EditorGUILayout.Space(5);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical(); // End darker container
            EditorGUILayout.EndVertical(); // End frame vertical

            // Right side: Fixed width metrics (always visible)
            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            float targetFrameTime = 11.1f; // VR target

            // CPU metric - inline
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.gray;
            EditorGUILayout.LabelField("CPU:", EditorStyles.label, GUILayout.Width(35));
            GUI.color = Color.white;

            if (hasStagingData && stagingPerfMetrics != null)
            {
                var cpuColor = GetPerformanceColor(stagingPerfMetrics.cpuFrameTime, targetFrameTime);
                GUI.color = cpuColor;
                EditorGUILayout.LabelField($"{stagingPerfMetrics.cpuFrameTime:F1}ms", EditorStyles.boldLabel, GUILayout.Width(45));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("--", EditorStyles.label, GUILayout.Width(35));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // GPU metric - inline
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.gray;
            EditorGUILayout.LabelField("GPU:", EditorStyles.label, GUILayout.Width(35));
            GUI.color = Color.white;

            if (hasStagingData && stagingPerfMetrics != null)
            {
                var gpuColor = GetPerformanceColor(stagingPerfMetrics.gpuFrameTime, targetFrameTime);
                GUI.color = gpuColor;
                EditorGUILayout.LabelField($"{stagingPerfMetrics.gpuFrameTime:F1}ms", EditorStyles.boldLabel, GUILayout.Width(45));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("--", EditorStyles.label, GUILayout.Width(35));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Frame Rate metric - inline
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.gray;
            EditorGUILayout.LabelField("FPS:", EditorStyles.label, GUILayout.Width(35));
            GUI.color = Color.white;

            if (hasStagingData && stagingPerfMetrics != null)
            {
                EditorGUILayout.LabelField($"{stagingPerfMetrics.frameRate:F1}", EditorStyles.boldLabel, GUILayout.Width(45));
            }
            else
            {
                EditorGUILayout.LabelField("--", EditorStyles.label, GUILayout.Width(35));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical(); // End metrics vertical

            EditorGUILayout.EndHorizontal(); // End horizontal layout

            // Draw freeze/unpause button at the bottom, full width
            DrawFreezeButton(enableAnalyze);

            EditorGUILayout.EndVertical();
        }

        void DrawStagingDataPanel()
        {
            if (!hasStagingData || stagingPerfMetrics == null)
                return;

            float targetFrameTime = 11.1f; // VR target

            // Compact staging data display
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // CPU metric
            var cpuColor = GetPerformanceColor(stagingPerfMetrics.cpuFrameTime, targetFrameTime);
            GUI.color = cpuColor;
            EditorGUILayout.LabelField($"CPU: {stagingPerfMetrics.cpuFrameTime:F1}ms", EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = Color.white;

            EditorGUILayout.LabelField("|", EditorStyles.miniLabel, GUILayout.Width(10));

            // GPU metric
            var gpuColor = GetPerformanceColor(stagingPerfMetrics.gpuFrameTime, targetFrameTime);
            GUI.color = gpuColor;
            EditorGUILayout.LabelField($"GPU: {stagingPerfMetrics.gpuFrameTime:F1}ms", EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawFreezeButton(bool enableAnalyze)
        {
            EditorGUILayout.Space(6);

            // Freeze/Unpause button - full width
            bool canCapture = !isCapturingWhatIf && (IsCapturingBottlenecks == "");

            // Determine button label based on freeze state
            string freezeButtonLabel;
            if (!runtimeIsFrozen)
            {
                freezeButtonLabel = "Freeze frame";
            }
            else if (freezeFrameInProgress)
            {
                freezeButtonLabel = "Processing...";
            }
            else
            {
                freezeButtonLabel = "Unpause";
            }

            // Build tooltip message
            string freezeTooltip = "";
            if (runtimeIsFrozen && freezeFrameInProgress)
            {
                // Show completion status in tooltip when trying to unpause during freeze operation
                freezeTooltip = $"Please wait for freeze to complete... (Screenshot: {(freezeFrameScreenshotComplete ? "✓" : "...")} Metrics: {(freezeFrameMetricsComplete ? "✓" : "...")})";
            }
            else if (!enableAnalyze || !canCapture)
            {
                freezeTooltip = "Make sure tool is connected to enable feature";
            }

            // Disable unpause button if freeze frame is still in progress
            bool canUnpause = runtimeIsFrozen && !freezeFrameInProgress;
            bool buttonEnabled = enableAnalyze && canCapture && (!runtimeIsFrozen || canUnpause);

            GUI.enabled = buttonEnabled;
            if (GUILayout.Button(new GUIContent(freezeButtonLabel, freezeTooltip), Styles.ButtonMiddleStyle, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                if (!runtimeIsFrozen)
                {
                    // Check cooldown period
                    double timeSinceLastFreeze = (DateTime.Now - lastFreezeFrameTime).TotalSeconds;
                    if (timeSinceLastFreeze < kFreezeFrameCooldownSeconds)
                    {
                        double remainingCooldown = kFreezeFrameCooldownSeconds - timeSinceLastFreeze;
                        EditorUtility.DisplayDialog(
                            "Freeze Frame Cooldown",
                            $"Please wait {remainingCooldown:F1} more seconds before freezing the frame again. This cooldown ensures accurate performance data collection.",
                            "OK"
                        );
                        RO.RuntimeOptimizerPlugin.SendEvent("freeze_frame_cooldown_blocked", InsightEventDataStr.ToJsonStr($"remaining:{remainingCooldown:F1}"));
                    }
                    else
                    {
                        string source = selectedAnalysisTool == ToolType.Bottleneck ? "bottleneck" : "whatif";
                        FreezeFrame(source);
                    }
                }
                else
                {
                    string source = selectedAnalysisTool == ToolType.Bottleneck ? "bottleneck" : "whatif";
                    UnfreezeFrame(source);
                }
            }
            GUI.enabled = true;
        }

        void DrawToolMenus()
        {
            EditorGUILayout.BeginHorizontal(Styles.ToolBox, GUILayout.ExpandHeight(true), GUILayout.MinHeight(150));
            bool enableAnalyze = playerConnected && !isCapturingWhatIf && !isActivelyCapturing;

            // Left half (50%): Tool selector + tool UI
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 20));
            {
                DrawActiveFramePanel();
            }
            EditorGUILayout.EndVertical();

            // Right half (50%): Active Frame Display
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 20));
            {
                DrawToolSelector();

                EditorGUILayout.Space(5);

                if (selectedAnalysisTool == ToolType.Bottleneck)
                {
                    DrawBottleneckTool(enableAnalyze);
                }
                else if (selectedAnalysisTool == ToolType.WhatIf)
                {
                    DrawWhatIfTool(enableAnalyze);
                }

            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void UpdateROEnabled(bool isEnabled)
        {
            if (!isEnabled)
            {

                RuntimeOptimizerPlugin.SendEvent("ro_enabled", InsightEventDataStr.ToJsonStr(""));
                // Enable development build by default for Runtime Optimizer
                if (!EditorUserBuildSettings.development)
                {
                    EditorUserBuildSettings.development = true;
                }
                string currentDefines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
                if (currentDefines.Contains("ENABLE_RUNTIME_OPTIMIZER"))
                {
                    currentDefines = currentDefines.Replace("ENABLE_RUNTIME_OPTIMIZER;", "").Replace("ENABLE_RUNTIME_OPTIMIZER", "");
                    PlayerSettings.SetScriptingDefineSymbols(
                        UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                        currentDefines);
                }
#if UNITY_OPENXR
                    // Configure MetaQuest ResetMetaQuestInternetPermission setting
                    ResetMetaQuestInternetPermission();
#endif
            }
            else
            {
                RuntimeOptimizerPlugin.SendEvent("ro_disabled", InsightEventDataStr.ToJsonStr(""));
                string currentDefines = PlayerSettings.GetScriptingDefineSymbols(
                    UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
                if (!currentDefines.Contains("ENABLE_RUNTIME_OPTIMIZER"))
                {
                    if (!string.IsNullOrEmpty(currentDefines) && !currentDefines.EndsWith(";"))
                        currentDefines += ";";

                    currentDefines += "ENABLE_RUNTIME_OPTIMIZER";
                    PlayerSettings.SetScriptingDefineSymbols(
                        UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                        currentDefines);
                }
#if UNITY_OPENXR
                    // Configure MetaQuest ForceRemoveInternetPermission setting
                    ForceMetaQuestInternetPermission();
#endif

            }
        }

        private void StartCountdownTimer()
        {
            // Stop any existing timer
            StopCountdownTimer();
            remainingTimeSeconds = (int)(mainThreadBaseline * totalGameObjectCount * 60 / 1000);

            // Ensure there's always a minimum countdown time (at least 3 seconds)
            const int minCountdownSeconds = 5;
            if (remainingTimeSeconds < minCountdownSeconds)
            {
                remainingTimeSeconds = minCountdownSeconds;
                RO.Util.DebugLog($"Using minimum countdown of {minCountdownSeconds}s for What If analysis (calculated: {(int)(mainThreadBaseline * totalGameObjectCount * 60 / 1000)}s)");
            }

            // Update timeout if What If analysis will take longer than default timeout
            if (remainingTimeSeconds > 60)
            {
                maxCaptureTimeoutSeconds = remainingTimeSeconds + 10; // Add 10 second buffer
                RO.Util.DebugLog($"Updated capture timeout to {maxCaptureTimeoutSeconds}s for What If analysis with {totalGameObjectCount} GameObjects");
            }

            // Create a new timer that ticks every second
            countdownTimer = new System.Threading.Timer(
                _ =>
                {
                    if (remainingTimeSeconds > 0)
                    {
                        remainingTimeSeconds--;
                        // Force UI update
                        EditorApplication.delayCall += () => Repaint();
                    }
                    else
                    {
                        // Stop the timer when we reach zero
                        // NOTE: Don't unfreeze automatically - let user control when to unfreeze
                        StopCountdownTimer();
                        // StopHeadlock(); // Removed automatic unfreeze
                    }
                },
                null,
                0,
                1000); // 1000ms = 1 second
        }

        private void StopCountdownTimer()
        {
            if (countdownTimer != null)
            {
                countdownTimer.Dispose();
                countdownTimer = null;
            }
        }


        private void FreezeFrame(string source = "")
        {

            // Check if GPU profiling is enabled before freezing
            if (!CaptureTool.isDetailedGPUServiceEnabled())
            {
                string errorMessage = "GPU Profiling needs to be enabled, please relaunch the app to continue.";
                RO.Util.DebugLogError(errorMessage);
                EditorUtility.DisplayDialog("GPU Profiling Required", errorMessage, "OK");

                var gpuProfilingErrorData = new
                {
                    source = source,
                    reason = "gpu_profiling_disabled",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string gpuProfilingErrorJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(gpuProfilingErrorData));
                RuntimeOptimizerPlugin.SendEvent("freeze_frame_blocked_gpu_profiling_disabled", gpuProfilingErrorJson);
                return;
            }
            // Update last freeze time
            lastFreezeFrameTime = DateTime.Now;

            // Initialize freeze frame tracking state
            lock (freezeFrameLock)
            {
                freezeFrameInProgress = true;
                freezeFrameStartTime = DateTime.Now;
                freezeFrameScreenshotComplete = false;
                freezeFrameMetricsComplete = false;
            }
            // STEP 1: Freeze runtime first
            runtimeIsFrozen = true;
            SendToServer("freeze");
            StartHeadlock();
            needRepait = true;

            // Wait for freeze to take effect using non-blocking delay
            DateTime scheduledTime = DateTime.Now.AddMilliseconds(100);
            EditorApplication.update += WaitForFreezeToTakeEffect;

            void WaitForFreezeToTakeEffect()
            {
                if (DateTime.Now >= scheduledTime)
                {
                    EditorApplication.update -= WaitForFreezeToTakeEffect;
                    InitiateScreenshotCapture();
                }
            }

            string telemetrySource = string.IsNullOrEmpty(source) ? "unknown" : source;
            RuntimeOptimizerPlugin.SendEvent($"{telemetrySource}_freeze_frame", InsightEventDataStr.ToJsonStr(""));
        }

        private void InitiateScreenshotCapture()
        {
            // Generate staging identifier
            stagingFrameName = CaptureTool.EpochTime().ToString("X");
            stagingCaptureTime = DateTime.Now;
            hasStagingData = true;

            // STEP 2: Capture screenshot only (in-memory, no file save)
            try
            {
                // Set capture monitoring flags
                isActivelyCapturing = true;
                captureStartTime = DateTime.Now;

                // Use new screenshot-only API
                CaptureTool.IssueScreenShotCaptureAsync(stagingFrameName,
                    () =>
                    {
                        // After screenshot capture starts
                        RO.Util.DebugLog($"Screenshot capture started for frozen frame: {stagingFrameName}");
                    },
                    () =>
                    {
                        // After screenshot capture completes - load image directly
                        EditorApplication.delayCall += () =>
                        {
                            LoadStagingScreenshot(stagingFrameName);
                            isActivelyCapturing = false;
                            captureStartTime = default(DateTime);

                            // Mark screenshot as complete
                            lock (freezeFrameLock)
                            {
                                freezeFrameScreenshotComplete = true;
                            }
                            RO.Util.DebugLog($"Screenshot capture completed for frozen frame: {stagingFrameName}");

                            // Check if freeze frame is fully complete
                            CheckFreezeFrameCompletion();

                            needRepait = true;
                        };
                    });

                RO.Util.DebugLog($"Freeze frame screenshot capture initiated: {stagingFrameName}");
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLogError("Failed to capture screenshot on freeze: " + ex.Message);
                isActivelyCapturing = false;
                captureStartTime = default(DateTime);
                lock (freezeFrameLock)
                {
                    freezeFrameScreenshotComplete = true; // Mark as complete even on error to avoid blocking
                }
                CheckFreezeFrameCompletion();
            }

            // STEP 3: Request Quick Perf metrics for staging (with small delay to ensure freeze takes effect)
            DateTime metricsScheduledTime = DateTime.Now.AddMilliseconds(200);
            EditorApplication.update += WaitForMetricsRequest;

            void WaitForMetricsRequest()
            {
                if (DateTime.Now >= metricsScheduledTime)
                {
                    EditorApplication.update -= WaitForMetricsRequest;
                    RequestQuickPerfForStaging();
                }
            }
        }

        private void UnfreezeFrame(string source = "")
        {
            runtimeIsFrozen = false;
            frozenGameObjectsForWhatIf.Clear();
            SendToServer("unfreeze");
            StopHeadlock();

            // Clear staging data when unfreezing (includes GPU/CPU metrics)
            ClearStagingData();

            // Clear active frame when unfreezing
            activeFrameImage = null;

            // Reset freeze frame tracking state
            lock (freezeFrameLock)
            {
                freezeFrameInProgress = false;
                freezeFrameStartTime = default(DateTime);
                freezeFrameScreenshotComplete = false;
                freezeFrameMetricsComplete = false;
            }

            needRepait = true;

            string telemetrySource = string.IsNullOrEmpty(source) ? "unknown" : source;
            RuntimeOptimizerPlugin.SendEvent($"{telemetrySource}_unpause", InsightEventDataStr.ToJsonStr(""));
        }

        private void CheckFreezeFrameCompletion()
        {
            bool isComplete = false;
            double duration = 0;
            bool screenshotComplete = false;
            bool metricsComplete = false;

            lock (freezeFrameLock)
            {
                if (!freezeFrameInProgress)
                {
                    return;
                }

                // Check if both screenshot and metrics are complete
                if (freezeFrameScreenshotComplete && freezeFrameMetricsComplete)
                {
                    isComplete = true;
                    duration = (DateTime.Now - freezeFrameStartTime).TotalSeconds;
                    screenshotComplete = freezeFrameScreenshotComplete;
                    metricsComplete = freezeFrameMetricsComplete;
                    freezeFrameInProgress = false;
                }
            }

            // Perform expensive operations outside the lock
            if (isComplete)
            {
                RO.Util.DebugLog("Freeze frame fully completed - screenshot and metrics ready");

                var completionData = new
                {
                    duration = duration,
                    screenshotComplete = screenshotComplete,
                    metricsComplete = metricsComplete,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string completionJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(completionData));
                RuntimeOptimizerPlugin.SendEvent("freeze_frame_completed", completionJson);

                needRepait = true;
            }
        }

        private void StartHeadlock()
        {
            string output;
            if (CaptureTool.ExecuteADBCommand(new[] { "shell setprop debug.oculus.sysPropDebug 1" }, out output))
            {
                if (CaptureTool.ExecuteADBCommand(new[] { "shell setprop debug.oculus.headlock 2" }, out output))
                {
                    RO.Util.DebugLog("Successfully headlocked the device");
                }
            }
        }

        private void StopHeadlock()
        {
            string output;
            if (CaptureTool.ExecuteADBCommand(new[] { "shell setprop debug.oculus.sysPropDebug 1" }, out output))
            {
                if (CaptureTool.ExecuteADBCommand(new[] { "shell setprop debug.oculus.headlock 0" }, out output))
                {
                    RO.Util.DebugLog("Successfully stopped headlock on the device");
                }
            }
        }

        private void HeadlockCameraRigPosition()
        {
            RO.Util.DebugLog("Headlock!");
            StartHeadlock();
            LockPerformanceMode();
            client.Send(PerformanceDataUtils.RuntimeOptimizerServiceRequestId, Encoding.ASCII.GetBytes("startPerfTest"));

            DisableProximitySensor();
            gameObjectPerformanceDataList.Clear();
            isCapturingWhatIf = true;
        }

        private void ClearStagingData()
        {
            stagingPerfMetrics = null;
            stagingFrameName = "";
            hasStagingData = false;
            stagingCaptureTime = default(DateTime);
            capturingForStaging = false;

            RO.Util.DebugLog("Cleared staging data");
        }

        private void LoadStagingScreenshot(string fileName)
        {
            // Start non-blocking retry logic
            LoadStagingScreenshotWithRetry(fileName, 0, 500);
        }

        private void LoadStagingScreenshotWithRetry(string fileName, int attempt, int delayMs)
        {
            const int maxRetries = 4;

            try
            {
                string imagePath = $"{CaptureTool.GetOutputDirectory()}/{fileName}.png";
                string localAssetPath = $"{CaptureTool.GetAssetOutputFolderName()}/{fileName}.png";

                if (File.Exists(imagePath) && !CaptureTool.IsFileLocked(imagePath))
                {
                    AssetDatabase.Refresh();

                    Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(localAssetPath);

                    if (image != null)
                    {
                        activeFrameImage = image;
                        activeFrameName = fileName;

                        if (!captureImageCache.ContainsKey(fileName))
                        {
                            captureImageCache.Add(fileName, image);
                        }

                        RO.Util.DebugLog($"Loaded staging screenshot: {fileName} on attempt {attempt + 1}");
                        needRepait = true;
                        return; // Success - exit retry loop
                    }
                    else
                    {
                        RO.Util.DebugLogError($"Failed to load staging screenshot from asset path: {localAssetPath} on attempt {attempt + 1}");
                    }
                }

                // Retry logic - schedule next attempt if we haven't exceeded max retries
                if (attempt + 1 < maxRetries)
                {
                    int nextDelay = delayMs * 2; // Exponential backoff
                    RO.Util.DebugLog($"Scheduling retry attempt {attempt + 1}/{maxRetries - 1} to load screenshot: {fileName} after {delayMs}ms delay");

                    // Use EditorApplication.delayCall with a timer to schedule the next attempt
                    DateTime scheduledTime = DateTime.Now.AddMilliseconds(delayMs);
                    EditorApplication.update += WaitForRetry;

                    void WaitForRetry()
                    {
                        if (DateTime.Now >= scheduledTime)
                        {
                            EditorApplication.update -= WaitForRetry;
                            LoadStagingScreenshotWithRetry(fileName, attempt + 1, nextDelay);
                        }
                    }
                }
                else
                {
                    RO.Util.DebugLogError($"Staging screenshot file not found or locked after {maxRetries} attempts: {imagePath}");
                }
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLogError($"Exception loading staging screenshot: {ex.Message}");
            }
        }

        private void RemoveCaptureDirectory()
        {
            RuntimeOptimizerPlugin.SendEvent("remove_all_capture", InsightEventDataStr.ToJsonStr(""));
            FileUtil.DeleteFileOrDirectory(CaptureTool.GetOutputDirectory());
        }

        private void RemoveCapture(string item)
        {
            RuntimeOptimizerPlugin.SendEvent("remove_a_capture", InsightEventDataStr.ToJsonStr(""));
            RO.Util.DebugLog($"Removing Capture: {item}");

            try
            {
                captureImageCache.Remove(item);
                string[] files = Directory.GetFiles(CaptureTool.GetOutputDirectory());

                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains(item, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        RO.Util.DebugLog($"Deleted: {file}");
                    }
                }
                captureItemProcessed.Remove(item);

                // Clear active frame display if the deleted capture is the active frame
                if (activeFrameName == item)
                {
                    activeFrameImage = null;
                    activeFrameName = "";
                    RO.Util.DebugLog($"Cleared active frame display for deleted capture: {item}");
                }
            }
            catch (Exception ex)
            {
                RO.Util.DebugLogError($"Error removing capture: {ex.Message}");
            }
        }

        private void DisableProximitySensor()
        {
            string output;
            var timeNow = DateTime.Now;
            if ((timeNow - kLastTimeDisabledSensor) < TimeSpan.FromSeconds(10))
            {
                return;
            }

            if ((client != null && client.Connected) && CaptureTool.ExecuteADBCommand(new[] { "shell am broadcast -a com.oculus.vrpowermanager.prox_close" }, out output))
            {
                RO.Util.DebugLog("Successfully disabled proximity sensor");
                kLastTimeDisabledSensor = timeNow;
            }
        }

        private bool IsDeviceAsleep()
        {
            string output;
            // Check device wakefulness state using dumpsys power
            if (CaptureTool.ExecuteADBCommand(new[] { "shell dumpsys power | grep \"mWakefulness=\"" }, out output))
            {
                // Output will be like "mWakefulness=Asleep" or "mWakefulness=Awake"
                if (!string.IsNullOrEmpty(output) && output.Contains("Asleep"))
                {
                    return true;
                }
            }
            return false;
        }

        private void LockPerformanceMode()
        {
            int osVersion = CaptureTool.GetAndroidOSVersion();
            string output;
            if (osVersion >= 14)
            {
                if (CaptureTool.ExecuteADBCommand(new[] { "shell cmd power set-fixed-performance-mode-enabled true" }, out output))
                {
                    RO.Util.DebugLog("Successfully enabled performance mode");
                }
            }
            else
            {
                if (CaptureTool.ExecuteADBCommand(new[] { "shell root" }, out output))
                {
                    RO.Util.DebugLog("Shell Rooted");
                    if (CaptureTool.ExecuteADBCommand(new[] { "shell dumpsys crcs override fidelity set feature://system/mode/performance_locked_medium 1.0" }, out output))
                    {
                        RO.Util.DebugLog("Successfully enabled rooted performance mode");
                    }
                }
            }
        }
        private void sortGameObjectDataListBy(ref List<GameObjectPerformanceMetaData> gameObjectPerformanceMetrics, int sortingFilterSeletion)
        {
            switch (sortingOptions[sortingFilterSelection])
            {
                case "GPU Frame Time":
                    RO.Util.DebugLog("Sorting by GPU App Frame Time");
                    gameObjectPerformanceMetrics = gameObjectPerformanceMetrics.OrderByDescending(x => x.CpuMainThreadTime).ToList();
                    break;
            }
        }


        private void OpenAnalysisForTimestamp(string filename, JSONObject? runtimeSnapshot, JSONObject? metricsSnapshot)
        {
            if (runtimeSnapshot == null || metricsSnapshot == null)
            {
                return;
            }

            // Note: Do NOT set active frame when opening saved captures for viewing insights
            // Active frame should only be set when freezing for analysis

            // Maybe start loading wheel
            bool enableAOC = EditorPrefs.GetBool(EnableAOCKey, false);

            if (enableAOC)
            {
                RuntimeOptimizerPlugin.SendEvent("aoc_enabled", InsightEventDataStr.ToJsonStr(""));
            }

            UnityInsightsHelper.GenerateAnalysis(runtimeSnapshot, metricsSnapshot, insight, (enableAOC && hasAOCInstalled));

            lastSelectedInsight = InsightType.ACTIONABLE_INSIGHT;
            lastSelectedInsightName = filename;
            // reset load more count
            insightListLimitDict.Clear();
        }

        void DrawRenderBreakDownLine(string title, string value)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(300));
            {
                var prevColor = GUI.color;
                GUI.color = Colors.OffWhite;
                EditorGUILayout.LabelField(title, Styles.UnitText, GUILayout.Width(200));
                EditorGUILayout.LabelField(value, Styles.RightAlignedText, GUILayout.Width(100));
                GUI.color = prevColor;
            }
            GUILayout.EndHorizontal();
        }

        bool DrawMaterialSection(bool displayExtraInfo)
        {
            const int kRowHeight = 18;
            const int kLabelWidth = 25;
            const int kStatPairWidth = 60; // Width for PS/VS pair (25 + 25 + spacing)

            // Header row
            EditorGUILayout.BeginHorizontal(GUILayout.Width(300), GUILayout.Height(kRowHeight));
            EditorGUILayout.LabelField("Material Analysis", Styles.UnitText, GUILayout.Width(200));

            // Instructions header with proper alignment
            EditorGUILayout.LabelField("Instructions (PS/VS)", Styles.UnitText, GUILayout.Width(100));

            EditorGUILayout.Space(10);
            bool showMoreClicked = false;
            {
                showMoreClicked = GUILayout.Button(new GUIContent(OptionIcon),
                new GUIStyle(Styles.IconLargeStyle), GUILayout.Width(Constants.SmallIconSize),
                GUILayout.Height(Constants.SmallIconSize));
            }

            if (displayExtraInfo)
            {
                EditorGUILayout.LabelField("fp16 ", Styles.UnitText, GUILayout.Width(kStatPairWidth), GUILayout.Height(kRowHeight));
                EditorGUILayout.LabelField("TexRead", Styles.UnitText, GUILayout.Width(kStatPairWidth), GUILayout.Height(kRowHeight));
                EditorGUILayout.LabelField("Reg ", Styles.UnitText, GUILayout.Width(kStatPairWidth), GUILayout.Height(kRowHeight));
            }
            GUILayout.EndHorizontal();

            // Material data rows
            foreach (var objectData in insight.materialArray)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(kRowHeight));

                var objectField = EditorGUILayout.ObjectField(objectData.Item1, typeof(Material), false, GUILayout.Width(100), GUILayout.Height(kRowHeight));

                if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    RuntimeOptimizerPlugin.SendEvent("open_material", InsightEventDataStr.ToJsonStr(""));
                }

                // Add spacing to align with the Instructions header
                EditorGUILayout.LabelField("", GUILayout.Width(100));

                var defaultColor = GUI.color;
                // 0 instructions, 2 fp16 instructions, 9 texture fetchs, 15 registers
                int[] shaderStatMode = { 0, 2, 9, 15 };
                if (objectData.Item2.shaderStats != null && objectData.Item2.shaderStats.Count > 0)
                {
                    AdrenoOfflineCompilerUtilityRO.ShaderStatInfo stat = objectData.Item2.shaderStats[0];
                    for (int k = 0; k < shaderStatMode.Length; ++k)
                    {
                        int i = shaderStatMode[k];

                        // PS (Fragment) value
                        GUI.color = AdrenoOfflineCompilerUtilityRO.GetStatColor(stat.fragmentMainStats[i], AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].fragmentMainRed,
                            AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].fragmentMainGreen);
                        EditorGUILayout.LabelField(stat.fragmentMainStats[i] + "", Styles.UnitText, GUILayout.Width(kLabelWidth), GUILayout.Height(kRowHeight));

                        // VS (Vertex) value
                        GUI.color = AdrenoOfflineCompilerUtilityRO.GetStatColor(stat.vertexStats[i], AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].vertexRed,
                            AdrenoOfflineCompilerUtilityRO.kShaderStatConfigs[i].vertexGreen);
                        EditorGUILayout.LabelField(stat.vertexStats[i] + "", Styles.UnitText, GUILayout.Width(kLabelWidth), GUILayout.Height(kRowHeight));

                        // Add spacing between stat pairs when showing extra info
                        if (displayExtraInfo && k < shaderStatMode.Length - 1)
                        {
                            EditorGUILayout.Space(10);
                        }
                        else if (!displayExtraInfo)
                        {
                            break;
                        }
                    }
                }
                GUI.color = defaultColor;
                EditorGUILayout.EndHorizontal();
            }

            return showMoreClicked;
        }

        GUIContent GetGUPContentForAnalysButton(string filePath, string fileName, Texture2D imageToUse, JSONObject? metricJsonToUse, JSONObject? snapshotJsonToUse)
        {
            string result = "";
            {
                // RO.Util.DebugLog("GetGUPCOntentForAnalysButton Thumbnail maybe being created");
                // Display insights
                if (metricJsonToUse != null)
                {
                    // RO.Util.DebugLog("GetGUPCOntentForAnalysButton metricJsonToUse not null");

                    bool captureHasASW = false;
                    var aswFile = filePath.Replace(".ptrace", snapshotASW);
                    if (File.Exists(aswFile))
                    {
                        captureHasASW = true;
                    }

                    string[] insights = UnityInsightsHelper.GenerateLightweightInsights(metricJsonToUse, snapshotJsonToUse, captureHasASW);
                    if (insights != null && insights.Length > 0)
                    {
                        result = string.Join(Environment.NewLine, insights);
                    }
                }
            }
            return new GUIContent(result, imageToUse);
        }

        static string GetStringAfterLastSlash(string originalString)
        {
            if (string.IsNullOrEmpty(originalString))
                return string.Empty;
            int lastIndex = originalString.LastIndexOf('/');
            if (lastIndex == -1)
                return originalString;
            return originalString.Substring(lastIndex + 1);
        }

        void DisplayActionableInsightsScrollView()
        {
            if (LightBulbIcon == null)
            {
                LightBulbIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.meta.xr.sdk.core/Editor/RuntimeOptimizer/Icons/lightbulb-04.png");
            }
            GUILayout.BeginVertical(Styles.CaptureBox, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Insights", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            if (insight.NumOfTriggeredInsights() > 0)
            {
                int count = 1;
                var insightList = insight.GetTriggeredInsights();
                foreach (var insight in insightList)
                {
                    string composed = string.Format("{0}. {1}", count, insight.Description);
                    GUILayout.BeginHorizontal();

                    var prevColor = GUI.color;
                    GUI.color = Colors.OffWhite;
                    EditorGUILayout.LabelField(composed, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
                    GUI.color = prevColor;

                    if (GUILayout.Button(Styles.helpButtonContent, EditorStyles.iconButton, GUILayout.Width(20)))
                    {
                        Application.OpenURL(insight.LinksToDoc[0]);
                        RuntimeOptimizerPlugin.SendEvent("insight_help_button", InsightEventDataStr.ToJsonStr(insight.Description));
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    GUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(
                    new GUIContent(LightBulbIcon),
                    new GUIStyle(Styles.IconLargeStyle), GUILayout.Width(Constants.MiniIconHeight),
                    GUILayout.Height(Constants.MiniIconHeight));

                    // Fix for T257512738: Use ExpandWidth(true) to prevent Insight text truncation
                    EditorGUILayout.LabelField(insight.Insight, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));

                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space(10);

                    ++count;
                }

            }
            else
            {
                EditorGUILayout.LabelField("No Insights Found", EditorStyles.wordWrappedLabel);
            }

            GUILayout.EndVertical();

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Render Breakdown", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            DrawRenderBreakDownLine("Drawcall Count (< 300 is optimal)", insight.drawcallCount.ToString());
            DrawRenderBreakDownLine("MSAA", $"{insight.MSAALevel}");
            DrawRenderBreakDownLine("Triangle Count", $"{insight.totalTriangleCount / 1000}k");
            DrawRenderBreakDownLine("Loaded Texures Memory", $"{insight.totalTextureMemory / (1024 * 1024)} mb");

            EditorGUILayout.Space(20);

            // for detail popup window
            bool showMore = false;
            RuntimeOptimizerDetailPopup.PopupForType arrayToShow = RuntimeOptimizerDetailPopup.PopupForType.Unknown;


            EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
            bool usePercentage = EditorPrefs.GetBool(RenderUnityPercentageKey, true);
            bool displayExtraInfo = false; //EditorPrefs.GetBool(ExtraAssetInfo, false);
                                           // string vertexTitle = string.Format("Vertex Analysis", insightListLimit, insight.meshVertArray.Count);
            EditorGUILayout.LabelField("Vertex Analysis", Styles.UnitText, GUILayout.Width(220));

            string unit = usePercentage ? "Weight" : "Count";
            EditorGUILayout.LabelField(unit, Styles.UnitText, GUILayout.Width(80));

            EditorGUILayout.Space(10);

            if (OptionIcon == null)
            {
                OptionIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.meta.xr.sdk.core/Editor/RuntimeOptimizer/Icons/options.png");
            }
            {
                bool showMoreClicked = GUILayout.Button(new GUIContent(OptionIcon),
                new GUIStyle(Styles.IconLargeStyle), GUILayout.Width(Constants.SmallIconSize),
                GUILayout.Height(Constants.SmallIconSize));
                arrayToShow = showMoreClicked ? RuntimeOptimizerDetailPopup.PopupForType.VertexArray : arrayToShow;
                showMore |= showMoreClicked;
            }

            GUILayout.EndHorizontal();

            int displayCount = 0;
            bool showLoadMore = false;
            int displayLimit = 0;
            if (!insightListLimitDict.ContainsKey("meshArray"))
            {
                insightListLimitDict.Add("meshArray", insightListLimit);
            }
            displayLimit = insightListLimitDict["meshArray"];

            foreach (var objectData in insight.meshAssetVertArray)
            {
                var obj = objectData.Item1;
                int totlaVertCount = obj != null ? obj.vertexCount * objectData.Item2.drawCount : objectData.Item2.dynamicVertexCount * objectData.Item2.drawCount;
                var percentAttribution = (totlaVertCount * 100.0) / insight.totalVertexCount;
                EditorGUILayout.BeginHorizontal();
                if (obj != null)
                {
                    EditorGUILayout.ObjectField(obj, typeof(Mesh), false, GUILayout.Width(180));
                    if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        RuntimeOptimizerPlugin.SendEvent("open_mesh", InsightEventDataStr.ToJsonStr(""));
                    }
                }
                else
                {
                    // no asset link for dynamic mesh
                    string dmeshName = GetStringAfterLastSlash(objectData.Item2.shortPath) + " (dynamic)";

                    // Use compact style for long mesh names to fit better in the available space
                    GUIStyle meshLabelStyle = dmeshName.Length > 25 ? Styles.CompactMeshText : EditorStyles.label;
                    EditorGUILayout.LabelField(dmeshName, meshLabelStyle, GUILayout.Width(180), GUILayout.Height(18));
                }

                EditorGUILayout.LabelField("", GUILayout.Width(40));
                if (usePercentage)
                {
                    EditorGUILayout.LabelField($"{percentAttribution.ToString("F2")}%({objectData.Item2.drawCount})", Styles.UnitText, GUILayout.Width(80), GUILayout.Height(18));
                }
                else
                {
                    EditorGUILayout.LabelField($"{totlaVertCount}({objectData.Item2.drawCount})", Styles.UnitText, GUILayout.Width(80), GUILayout.Height(18));
                }

                if (displayExtraInfo)
                {
                    EditorGUILayout.LabelField(objectData.Item2.shortPath, EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.EndHorizontal();
                ++displayCount;
                if (displayCount > displayLimit)
                {
                    showLoadMore = true;
                    break;
                }
            }

            EditorGUILayout.Space(5);
            if (showLoadMore)
            {
                if (GUILayout.Button("Load More...", EditorStyles.linkLabel))
                {
                    insightListLimitDict["meshArray"] = displayLimit + 15;
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
            EditorGUILayout.LabelField("Texture Analysis", Styles.UnitText, GUILayout.Width(200));

            EditorGUILayout.LabelField("Memory Usage", Styles.UnitText, GUILayout.Width(100));

            EditorGUILayout.Space(10);
            {
                bool showMoreClicked = GUILayout.Button(new GUIContent(OptionIcon),
                new GUIStyle(Styles.IconLargeStyle), GUILayout.Width(Constants.SmallIconSize),
                GUILayout.Height(Constants.SmallIconSize));
                arrayToShow = showMoreClicked ? RuntimeOptimizerDetailPopup.PopupForType.TextureArray : arrayToShow;
                showMore |= showMoreClicked;
            }

            GUILayout.EndHorizontal();

            if (!insightListLimitDict.ContainsKey("textureArray"))
            {
                insightListLimitDict.Add("textureArray", insightListLimit);
            }
            displayLimit = insightListLimitDict["textureArray"];
            displayCount = 0;
            showLoadMore = false;
            foreach (var objectData in insight.textureArray)
            {
                // const int kTextureLabelWidth = 90;
                EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
                double runtimeSize = (double)objectData.Item2.runtimeSize / (1024.0 * 1024.0);
                double percentage = 100.0 * (double)objectData.Item2.runtimeSize / insight.totalTextureMemory;

                string usedLabel = hasAOCInstalled ? $"({objectData.Item2.usedByCount})" : "";

                string percentageLable = string.Format("{0:0.00}%{1} ", percentage, usedLabel);
                string sizeLable = string.Format("{0:0.00} mb{1}", runtimeSize, usedLabel);
                string fromat = objectData.Item2.format;
                string mipCount = string.Format("Mip:{0}", objectData.Item2.mipCount);
                string dimentionLable = string.Format("W:{0} H:{1}", objectData.Item2.width, objectData.Item2.height);
                var pbb_Comp = UnityInsightsHelper.GetTextureFormatInfo(fromat);
                string pbbAndCompressionStr = string.Format("bpp: {0} {1} {2}", pbb_Comp.Item1, pbb_Comp.Item2 ? "Comp." : "", objectData.Item2.isReadable ? "R/W" : "");

                var textureField = EditorGUILayout.ObjectField(objectData.Item1, typeof(Texture), false, GUILayout.Width(160));
                if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    RuntimeOptimizerPlugin.SendEvent("open_texture", InsightEventDataStr.ToJsonStr(""));
                }
                EditorGUILayout.LabelField("", GUILayout.Width(50));
                if (usePercentage)
                {
                    EditorGUILayout.LabelField(percentageLable, GUILayout.Width(60));
                }
                else
                {
                    EditorGUILayout.LabelField(sizeLable, GUILayout.Width(70));
                }

                if (displayExtraInfo)
                {
                    EditorGUILayout.LabelField(dimentionLable, GUILayout.Width(100));
                    EditorGUILayout.LabelField(mipCount, GUILayout.Width(60));
                    EditorGUILayout.LabelField(fromat, GUILayout.Width(90));
                    EditorGUILayout.LabelField(pbbAndCompressionStr, GUILayout.Width(100));
                }

                // EditorGUILayout.LabelField(sizeLable, GUILayout.Width(kTextureLabelWidth));
                // EditorGUILayout.LabelField(dimentionLable, GUILayout.Width(kTextureLabelWidth * 2));
                EditorGUILayout.EndHorizontal();

                ++displayCount;
                if (displayCount > displayLimit)
                {
                    showLoadMore = true;
                    break;
                }
            }

            EditorGUILayout.Space(5);
            if (showLoadMore)
            {
                if (GUILayout.Button("Load More...", EditorStyles.linkLabel))
                {
                    insightListLimitDict["textureArray"] = displayLimit + 15;
                }
            }

            EditorGUILayout.Space(20);

            if (hasAOCInstalled)
            {
                if (DrawMaterialSection(displayExtraInfo))
                {
                    arrayToShow = RuntimeOptimizerDetailPopup.PopupForType.MaterialArray;
                    showMore = true;
                }
            }

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("You've reached the bottom", Styles.UnitText);

            if (showMore)
            {
                RuntimeOptimizerPlugin.SendEvent("insight_show_more", InsightEventDataStr.ToJsonStr(arrayToShow.ToString()));
                var buttonRect = new Rect(0, 0, 1000, 200);
                UnityEditor.PopupWindow.Show(buttonRect, new RuntimeOptimizerDetailPopup(insight, arrayToShow, null));
            }
        }

        void DisplaySceneInsightsScrollView()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // Add help box explaining negative GPU times and variance
            if (gameObjectPerformanceDataList.Count > 0)
            {
                if (isWhatIfGroupMode)
                {
                    EditorGUILayout.HelpBox(
                        "Group mode: All selected GameObjects were disabled together in a single pass. " +
                        "The result shows the total GPU cost saved by disabling the entire group.",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Negative GPU times are expected when the disabled game object was occluding something more costly than itself. " +
                        "Also, be careful of indexing too much on values that fall above the ± variance. " +
                        "If this number is too high, look into ways to better freeze what is being rendered in your frame.",
                        MessageType.Info);
                }
                EditorGUILayout.Space(5);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(300));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                {
                    string timerText = isCapturingWhatIf && remainingTimeSeconds > 0
                        ? $"{totalGameObjectCount} GameObjects: {remainingTimeSeconds}s "
                        : $"{totalGameObjectCount} GameObjects";

                    EditorGUILayout.LabelField(timerText, EditorStyles.boldLabel);

                    // Check if this is group mode by looking for GROUP_RESULT marker
                    bool isGroupMode = gameObjectPerformanceDataList.Count > 0 &&
                                      gameObjectPerformanceDataList[0].GameObjectName == "GROUP_RESULT";

                    if (isGroupMode)
                    {
                        // Show "Group Cost" label first
                        EditorGUILayout.LabelField("Group Cost", EditorStyles.boldLabel);

                        // Then show all GOs in the group (starting from index 1)
                        for (int i = 1; i < gameObjectPerformanceDataList.Count; i++)
                        {
                            var item = gameObjectPerformanceDataList[i];
                            GameObject? obj = GameObject.Find(item.GameObjectName);
                            if (obj != null)
                            {
                                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                            }
                            else
                            {
                                EditorGUILayout.LabelField(new GUIContent("  " + item.GameObjectName, "Not in hierarchy window / dynamically created"));
                            }
                        }
                    }
                    else
                    {
                        // Individual mode - show each GO normally
                        foreach (var item in gameObjectPerformanceDataList)
                        {
                            GameObject? obj = GameObject.Find(item.GameObjectName);
                            if (obj != null)
                            {
                                var objField = EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                                if (Event.current.type == EventType.MouseUp && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                {
                                    try
                                    {
                                        RuntimeOptimizerPlugin.SendEvent("open_gameobject", item.CpuMainThreadTime.ToString());
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField(new GUIContent(item.GameObjectName, "Not in hierarchy window / dynamically created"));
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                {
                    EditorGUILayout.LabelField($"GPU Frame Time ± {samplingVariance:F3}ms", EditorStyles.boldLabel);

                    bool isGroupMode = gameObjectPerformanceDataList.Count > 0 &&
                                      gameObjectPerformanceDataList[0].GameObjectName == "GROUP_RESULT";

                    if (isGroupMode)
                    {
                        // Show total cost for group
                        if (gameObjectPerformanceDataList[0].CpuMainThreadTime is double groupCost)
                        {
                            EditorGUILayout.LabelField($"{groupCost:F3} ms", EditorStyles.boldLabel);
                        }

                        // Show empty labels for the individual GOs (no individual costs)
                        for (int i = 1; i < gameObjectPerformanceDataList.Count; i++)
                        {
                            EditorGUILayout.LabelField("");
                        }
                    }
                    else
                    {
                        // Individual mode - show each GO's cost
                        foreach (var item in gameObjectPerformanceDataList)
                        {
                            if (item.CpuMainThreadTime.HasValue)
                            {
                                float cpuMainThreadTimeInMs = (float)(item.CpuMainThreadTime);
                                EditorGUILayout.LabelField($"{cpuMainThreadTimeInMs:F3} ms");
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

        }

        void DisplayEmptyInsight()
        {
            EditorGUILayout.LabelField("Select a capture on the left to view details");
        }

        void DisplayNoCapturesUI()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No analysis selected", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }

        void DrawAppLaunchField()
        {
            EditorGUILayout.LabelField("Executable Path:", EditorStyles.boldLabel, GUILayout.Width(110));
            string newExecutablePath = EditorGUILayout.TextField(executablePath, GUILayout.MinWidth(250));

            if (newExecutablePath != executablePath)
            {
                executablePathModified = true;
                executablePath = newExecutablePath;
            }

            AppConnectionState currentState = GetAppConnectionState();

            switch (currentState)
            {
                case AppConnectionState.NotRunning:
                    DrawLaunchButton();
                    break;

                case AppConnectionState.RunningConnected:
                    DrawRelaunchButton();
                    break;

                case AppConnectionState.RunningDisconnected:
                    DrawRelaunchReconnectButtons();
                    break;
            }
        }

        private void DrawLaunchButton()
        {
            bool hasExecutablePath = !string.IsNullOrWhiteSpace(executablePath);
            bool canLaunch = hasExecutablePath && !isLaunchInProgress;
            GUI.enabled = canLaunch;

            string buttonLabel = isLaunchInProgress ? "Launching..." : "Launch";
            if (GUILayout.Button(buttonLabel, Styles.ButtonStyle, GUILayout.Width(90)))
            {
                LaunchApp();
            }

            GUI.enabled = true;
        }

        private void DrawRelaunchButton()
        {
            bool hasExecutablePath = !string.IsNullOrWhiteSpace(executablePath);
            bool canLaunch = hasExecutablePath && !isLaunchInProgress;
            GUI.enabled = canLaunch;

            string buttonLabel = isLaunchInProgress ? "Launching..." : "Relaunch";
            if (GUILayout.Button(buttonLabel, Styles.ButtonStyle, GUILayout.Width(90)))
            {
                LaunchApp();
            }

            GUI.enabled = true;
        }

        private void DrawRelaunchReconnectButtons()
        {
            bool hasExecutablePath = !string.IsNullOrWhiteSpace(executablePath);
            bool canLaunch = hasExecutablePath && !isLaunchInProgress;

            GUI.enabled = canLaunch;
            string buttonLabel = isLaunchInProgress ? "Launching..." : "Relaunch";
            if (GUILayout.Button(buttonLabel, Styles.ButtonStyle, GUILayout.Width(90)))
            {
                LaunchApp();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Connect", Styles.ButtonStyle, GUILayout.Width(90)))
            {
                AttemptReconnection();
            }
        }

        private void ConnectToServer(int timeOutSecond, string connectionType = "unknown")
        {
            DateTime startTime = DateTime.Now;
            bool connectionSuccess = false;
            if (pcTestingMode)
            {
                RO.Util.DebugLog("PC Testing Mode: Connecting to local RuntimeOptimizerService on port 12345");

                // Initialize client for PC mode
                if (client != null)
                {
                    client.Disconnect();
                }

                client = new OVRNetwork.OVRNetworkTcpClient();
                client.payloadReceivedCallback += OnPayloadReceived;
                client.connectionStateChangedCallback += OnConnectionStateChanged;
                client.NetworkErrorOccurred += OnNetworkError;

                // Try to connect directly to localhost:12345
                // We need to call Connect and wait for the async callback
                // First attempt to connect
                RO.Util.DebugLog("Attempting to connect to localhost:12345...");
                client.Connect(12345);

                // Wait for connection to establish (async operation)
                while ((DateTime.Now - startTime).TotalSeconds < 10)
                {
                    Thread.Sleep(100);

                    if (client.connectionState == OVRNetwork.OVRNetworkTcpClient.ConnectionState.Connected)
                    {
                        playerConnected = true;
                        runtimeServicePID = 1; // Set to non-negative value for PC mode
                        invalidOSVersion = false;
                        connectionSuccess = true;
                        // Reset lost focus dialog flag for new session
                        lostFocusDialogShown = false;
                        RO.Util.DebugLog("Successfully connected to PC RuntimeOptimizerService");
                        break;
                    }
                    else if (client.connectionState == OVRNetwork.OVRNetworkTcpClient.ConnectionState.Disconnected)
                    {
                        // Connection failed, break out early
                        RO.Util.DebugLog("Connection state is Disconnected, connection failed");
                        break;
                    }
                }
                if (!connectionSuccess)
                {
                    string detailedMessage = "Could not connect to RuntimeOptimizerService on port 12345.\n\n" +
                        "Please ensure:\n" +
                        "1. Unity Editor or PC build with your scene is running\n" +
                        "2. A GameObject with RuntimeOptimizerService component is in the scene\n" +
                        "3. ENABLE_RUNTIME_OPTIMIZER is defined in Project Settings > Player > Scripting Define Symbols\n" +
                        "4. The game is running (press Play in Unity Editor)\n\n" +
                        "Debug Info:\n" +
                        $"- Connection State: {client.connectionState}\n" +
                        $"- Time Elapsed: {(DateTime.Now - startTime).TotalSeconds:F1}s";

                    RO.Util.DebugLogError(detailedMessage);
                    EditorUtility.DisplayDialog("PC Connection Failed", detailedMessage, "OK");
                    playerConnected = false;
                    runtimeServicePID = -1;
                }
                return;
            }

            // Check if GPU profiling is enabled before connecting
            if (!CaptureTool.isDetailedGPUServiceEnabled())
            {
                string errorMessage = "GPU Profiling needs to be enabled, please relaunch the app to continue.";
                RO.Util.DebugLogError(errorMessage);
                EditorUtility.DisplayDialog("GPU Profiling Required", errorMessage, "OK");

                // Set flag to prevent showing additional error dialogs
                gpuProfilingDialogShown = true;

                var gpuProfilingErrorData = new
                {
                    connectionType = connectionType,
                    reason = "gpu_profiling_disabled",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string gpuProfilingErrorJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(gpuProfilingErrorData));
                RuntimeOptimizerPlugin.SendEvent("connect_blocked_gpu_profiling_disabled", gpuProfilingErrorJson);
                return;
            }

            var connectAttemptData = new
            {
                connectionType = connectionType,
                timeout = timeOutSecond,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string connectAttemptJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(connectAttemptData));
            RuntimeOptimizerPlugin.SendEvent("connect_to_server_start", connectAttemptJson);
            client = new OVRNetwork.OVRNetworkTcpClient();
            client.payloadReceivedCallback += OnPayloadReceived;
            client.connectionStateChangedCallback += OnConnectionStateChanged;
            client.NetworkErrorOccurred += OnNetworkError;

            int connectedCount = 0;
            while ((DateTime.Now - startTime).TotalSeconds < timeOutSecond)
            {
                Thread.Sleep(1000);
                try
                {
                    int processID = CaptureTool.GetProcessPID(bundleName);
                    if (processID != -1)
                    {
                        client.Connect(12345);
                        if (client.connectionState == OVRNetwork.OVRNetworkTcpClient.ConnectionState.Connected)
                        {
                            connectedCount++;
                            if (connectedCount > 1)
                            {
                                runtimeServicePID = processID;
                                if (!playerConnected)
                                {
                                    aswIsOn = CaptureTool.ASWDetectionCommand();
                                    headsetOSVersion = CaptureTool.GetHeadsetOSVersion();
                                    if (headsetOSVersion < 78)
                                    {
                                        EditorUtility.DisplayDialog("Failed OS Version Requirement", "HzOS v78 or higher is needed to use Runtime Optimizer", "OK");
                                        invalidOSVersion = true;
                                    }
                                    else
                                    {
                                        playerConnected = true;
                                        invalidOSVersion = false;
                                        // Reset lost focus dialog flag for new session
                                        lostFocusDialogShown = false;
                                    }

                                    try
                                    {
                                        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.meta.xr.sdk.core");
                                        if (packageInfo != null)
                                        {
                                            metaCoreSDKVersion = packageInfo.version;
                                        }
                                    }
                                    catch (Exception)
                                    { }
                                }
                                playerConnected = true;
                                connectionSuccess = true;
                                // Reset lost focus dialog flag for new session
                                lostFocusDialogShown = false;
                                DisableProximitySensor();
                                break;
                            }
                            runtimeServicePID = -1;
                        }
                    }
                    runtimeServicePID = -1;
                }
                catch (Exception ex)
                {
                    RO.Util.DebugLogError("Failed to connect to runtime service: " + ex.Message);
                }
            }

            var connectResultData = new
            {
                connectionType = connectionType,
                success = connectionSuccess,
                duration = (DateTime.Now - startTime).TotalSeconds,
                playerConnected = playerConnected,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string connectResultJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(connectResultData));
            RuntimeOptimizerPlugin.SendEvent("connect_to_server_complete", connectResultJson);
        }

        private void LaunchApp()
        {
            // Debounce: Check if a launch is already in progress or if we're within cooldown period
            if (isLaunchInProgress)
            {
                RO.Util.DebugLog("Launch already in progress, ignoring duplicate request");
                return;
            }

            double timeSinceLastLaunch = (DateTime.Now - lastLaunchTime).TotalSeconds;
            if (timeSinceLastLaunch < kLaunchCooldownSeconds)
            {
                RO.Util.DebugLog($"Launch cooldown active ({timeSinceLastLaunch:F1}s < {kLaunchCooldownSeconds}s), ignoring duplicate request");
                return;
            }

            // Set launch in progress flag
            isLaunchInProgress = true;
            needRepait = true;

            // Check if we're currently capturing and cancel it first
            CancelCurrentCapture("App launch requested. Canceling current capture.", "app_relaunch", showDialog: false);

            // IMPORTANT: Set playerConnected to false BEFORE launching
            // This greys out all capture options during relaunch
            playerConnected = false;
            runtimeServicePID = -1;

            // Force UI update to show disconnected state
            needRepait = true;

            RuntimeOptimizerPlugin.SendEvent("launch_app_start", InsightEventDataStr.ToJsonStr(executablePath));

            // Kill and relaunch app
            CaptureTool.ReleasePort(remoteListeningPort);
            CaptureTool.ForwardPort(remoteListeningPort);
            CaptureTool.KillApp(bundleName);
            CaptureTool.Init(bundleName);
            Thread.Sleep(1000);
            CaptureTool.LaunchApp(executablePath);

            // ConnectToServer will set playerConnected to true on successful connection
            ConnectToServer(35, "launch");

            // Reset launch in progress flag and record completion time for cooldown.
            // lastLaunchTime is set HERE (after ConnectToServer returns) so the 3-second
            // cooldown starts from when the launch *completes*, not when it started.
            // This prevents queued clicks (buffered while the main thread was blocked in
            // ConnectToServer) from bypassing the cooldown.
            isLaunchInProgress = false;
            lastLaunchTime = DateTime.Now;
            needRepait = true;
        }

        private AppConnectionState GetAppConnectionState()
        {
            bool appIsRunning = (runtimeServicePID != -1) &&
                                (CaptureTool.lastKnownPID != -1) &&
                                (runtimeServicePID == CaptureTool.lastKnownPID);

            if (!appIsRunning)
            {
                return AppConnectionState.NotRunning;
            }

            bool isConnected = playerConnected &&
                              (client != null) &&
                              (client.connectionState == OVRNetwork.OVRNetworkTcpClient.ConnectionState.Connected);

            return isConnected ? AppConnectionState.RunningConnected : AppConnectionState.RunningDisconnected;
        }

        private void AttemptReconnection()
        {
            RO.Util.DebugLog("Attempting to reconnect to running app...");

            // Reset GPU profiling dialog flag before attempting reconnection
            gpuProfilingDialogShown = false;

            // Check if device is asleep before attempting reconnection
            if (IsDeviceAsleep())
            {
                RO.Util.DebugLog("Device is asleep, cannot reconnect");
                EditorUtility.DisplayDialog(
                    "Device Asleep",
                    "The device appears to be asleep. Please wake the device and try again.",
                    "OK"
                );
                return;
            }

            try
            {
                if (client != null)
                {
                    client.Disconnect();
                    Thread.Sleep(500);
                    client = null!;
                }

                CaptureTool.ReleasePort(remoteListeningPort);
                CaptureTool.ForwardPort(remoteListeningPort);
                Thread.Sleep(500);

                int processID = CaptureTool.GetProcessPID(bundleName);
                if (processID != -1)
                {
                    ConnectToServer(15, "reconnect");
                }

                // Only show reconnection failed dialog if we didn't just show the GPU profiling dialog
                if (!playerConnected && !gpuProfilingDialogShown)
                {
                    RuntimeOptimizerPlugin.SendEvent("reconnect_failed",
                    InsightEventDataStr.ToJsonStr("Failed to reconnect"));
                    EditorUtility.DisplayDialog("Reconnection Failed",
                        "Could not reconnect to the running app. Try relaunching.", "OK");
                }

            }
            catch (Exception ex)
            {
                RO.Util.DebugLogError($"Reconnection failed: {ex.Message}");
                RuntimeOptimizerPlugin.SendEvent("reconnect_error",
                    InsightEventDataStr.ToJsonStr(ex.Message));
                EditorUtility.DisplayDialog("Reconnection Error",
                    $"An error occurred while reconnecting: {ex.Message}", "OK");
            }
        }

        void DisplayPlayerConnectionGUI()
        {
            EditorGUILayout.LabelField("Player Connection:", EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Width(80));
            EditorGUILayout.EndVertical();
        }

        private bool ProcessCaptureCaches(string fileName, string item)
        {
            const int maxAsyncJob = 2;

            // Update caches
            if (!captureImageCache.ContainsKey(fileName))
            {
                var imagePath = item.Replace(".ptrace", ".png");
                string localAssetPath = CaptureTool.GetAssetOutputFolderName() + "/" + fileName + ".png";

                if (File.Exists(imagePath) && !CaptureTool.IsFileLocked(imagePath))
                {
                    AssetDatabase.Refresh();
                    Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(localAssetPath);
                    captureImageCache.Add(fileName, image);

                    // Only set as active frame if runtime is frozen AND this is the staging frame
                    if (runtimeIsFrozen && fileName == stagingFrameName)
                    {
                        activeFrameImage = image;
                        activeFrameName = fileName;
                        RO.Util.DebugLog($"Set frame {fileName} as active frame (frozen={runtimeIsFrozen}, staging={fileName == stagingFrameName})");
                    }

                    needRepait = true;
                    return false;
                }
            }

            // CRITICAL FIX: Also check for capture completion when both image and metric JSON are available
            // This ensures the UI state is reset even if the image was processed in a previous frame
            if (captureImageCache.ContainsKey(fileName) && metricJsonCache.ContainsKey(fileName))
            {
                if (IsCapturingBottlenecks == fileName)
                {
                    IsCapturingBottlenecks = "";
                    isActivelyCapturing = false;
                    captureStartTime = default(DateTime);
                    RO.Util.DebugLog($"Capture {fileName} fully completed - ensuring UI state is reset");
                    needRepait = true;
                }
            }

            // Note This check need to happen before processedJsonCache to avoid json check
            if (!metricJsonCache.ContainsKey(fileName))
            {
                var jsonPath = item.Replace(".ptrace", snapshotPostfix);

                if (File.Exists(jsonPath) && !CaptureTool.IsFileLocked(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    JSONObject? jsonNode = JSONObject.Parse(jsonStr) as JSONObject;

                    if (jsonNode != null)
                    {
                        if (jsonNode["version"] != null)
                        {
                            if (0 != string.Compare(jsonNode["version"].Value, UnityInsightsHelper.GetMetricVersion()))
                            {
                                // different version, need to be reprocessed.
                                RO.Util.DebugLog("Metric version mismatch:" + jsonNode["version"].Value);
                                File.Delete(jsonPath);
                                return false;
                            }
                            else
                            {
                                if (!processedJsonCache.ContainsKey(fileName))
                                {
                                    // we got the same version, we don't need to reprocess
                                    processedJsonCache.Add(fileName, DateTime.Now);
                                }
                                // RO.Util.DebugLog("metricJsonCache added " + item);
                                metricJsonCache.Add(fileName, jsonNode);
                                RuntimeOptimizerPlugin.SendEvent("metric_json_exists", InsightEventDataStr.ToJsonStr(fileName));

                                string saveToFilePathMetricJson = string.Format("{0}/{1}{2}", CaptureTool.GetOutputDirectory(), fileName, metricJsonOpened);
                                if (!File.Exists(saveToFilePathMetricJson))
                                {
                                    File.WriteAllText(saveToFilePathMetricJson, fileName);
                                    RuntimeOptimizerPlugin.SendEvent("capture_generated", jsonStr);
                                }
                                needRepait = true;
                            }
                        }
                        else
                        {
                            // no version means old version, need to be reprocessed.
                            File.Delete(jsonPath);
                            return false;
                        }
                    }
                    else
                    {
                        // invalid json, need to be reprocessed.
                        RO.Util.DebugLogError("Invalid metric json");
                        File.Delete(jsonPath);
                        return false;
                    }
                }
            }

            if (!processedJsonCache.ContainsKey(fileName))
            {
                if (CaptureTool.CaptureToJsonObjAsyncCount() <= maxAsyncJob)
                {
                    processedJsonCache.Add(fileName, DateTime.Now);
                    CaptureTool.ProcessCaptureToJsonObjAsync(item);
                    RuntimeOptimizerPlugin.SendEvent("metric_json_started", InsightEventDataStr.ToJsonStr(fileName));
                }
                return false;
            }
            else
            {
                DateTime lastProcessed = processedJsonCache[fileName];
                double timeSinceProcessed = (DateTime.Now - lastProcessed).TotalSeconds;

                if (!metricJsonCache.ContainsKey(fileName) && timeSinceProcessed > (MetricAPI.processTimeOutMS / 1000.0))
                {
                    processedJsonCache.Remove(fileName);
                    processedJsonCache.Add(fileName, DateTime.Now);
                    CaptureTool.CaptureToJsonAsyncTimeout();
                    RO.Util.DebugLog("Load json timeout, reissue request: " + item);
                    RuntimeOptimizerPlugin.SendEvent("metric_json_timeout", InsightEventDataStr.ToJsonStr(fileName));
                    CaptureTool.ProcessCaptureToJsonObjAsync(item);
                    return false;
                }
            }


            if (!snapshotJsonCache.ContainsKey(fileName))
            {
                var jsonPath = item.Replace(".ptrace", snapshotUnityPostfix);
                // RO.Util.DebugLog("Looking for snapshot: " + jsonPath);
                if (File.Exists(jsonPath))
                {
                    string jsonStr = File.ReadAllText(jsonPath);
                    JSONObject? jsonNode = JSONObject.Parse(jsonStr) as JSONObject;
                    if (!snapshotJsonCache.ContainsKey(fileName) && jsonNode != null)
                    {
                        snapshotJsonCache.Add(fileName, jsonNode);
                        return false;
                    }
                }
            }

            return true;
        }
#if HAS_AGENT_BRIDGE
        void DisplayAIAnalysisPanel()
        {
            EditorGUILayout.LabelField("✨ AI Performance Analysis", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (!string.IsNullOrEmpty(_aiAnalysisError))
            {
                EditorGUILayout.HelpBox(_aiAnalysisError, MessageType.Error);
            }
            else if (_isAIAnalyzing && _aiAnalysisResult.Length == 0)
            {
                EditorGUILayout.HelpBox("AI is analyzing your performance data...", MessageType.Info);
                EditorApplication.delayCall += () => Repaint();
            }

            if (_aiAnalysisResult.Length > 0)
            {
                _aiAnalysisScrollPosition = EditorGUILayout.BeginScrollView(
                    _aiAnalysisScrollPosition,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true));
                string resultText = _aiAnalysisResult.ToString();
                float textHeight = EditorStyles.wordWrappedLabel.CalcHeight(
                    new GUIContent(resultText),
                    EditorGUIUtility.currentViewWidth - 40);
                EditorGUILayout.SelectableLabel(
                    resultText,
                    EditorStyles.wordWrappedLabel,
                    GUILayout.MinHeight(textHeight));
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (_aiAnalysisResult.Length > 0 && GUILayout.Button("Copy to Clipboard", GUILayout.Width(130)))
            {
                EditorGUIUtility.systemCopyBuffer = _aiAnalysisResult.ToString();
                RuntimeOptimizerPlugin.SendEvent("ai_analysis_copy", InsightEventDataStr.ToJsonStr(lastSelectedInsightName));
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _aiAnalysisResult.Clear();
                _aiAnalysisError = null;
                _isAIAnalyzing = false;
                lastSelectedInsight = InsightType.ACTIONABLE_INSIGHT;
                RuntimeOptimizerPlugin.SendEvent("ai_analysis_clear", InsightEventDataStr.ToJsonStr(""));
                Repaint();
            }
            if (_aiAnalysisResult.Length > 0 && GUILayout.Button("Back to Insights", GUILayout.Width(120)))
            {
                lastSelectedInsight = InsightType.ACTIONABLE_INSIGHT;
                RuntimeOptimizerPlugin.SendEvent("ai_analysis_back", InsightEventDataStr.ToJsonStr(lastSelectedInsightName));
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
        }
#endif

        void OnGUI()
        {
            // Extract package name from executable path if it has been set
            if (!string.IsNullOrEmpty(executablePath) && executablePath.Contains("/"))
            {
                // executablePath format is "com.example.package/.MainActivity"
                // Extract just the package name part
                bundleName = executablePath.Split('/')[0];
            }
            else
            {
                bundleName = Application.identifier;
            }
            DrawMainToolbar();
            DrawDescriptionHeader(Styles.Description.text, Styles.Steps.text);

            //TODO: Gate for versions that have access to internal classes
            // PerformanceDataUtils.ToolDescriptor.DrawHeaderFromWindow(Origins.ProjectSettings);
            // PerformanceDataUtils.ToolDescriptor.DrawDescriptionHeader(Description, Origins.ProjectSettings);

            bool enableAOC = EditorPrefs.GetBool(EnableAOCKey, false);
            if (enableAOC)
            {
                bool disabled = false;
                hasAOCInstalled = AdrenoOfflineCompilerUtilityRO.InitAOCPath(out disabled);
                if (!hasAOCInstalled)
                {
                    AdrenoOfflineCompilerUtilityRO.ShowAOCPathUI(disabled);
                    EditorGUILayout.HelpBox("AOC is necessary to estimate material rendering cost, please consider downloading & installing the package", MessageType.Warning);
                }
            }

            DrawSeperator();

            DrawBuildSettings();

            DrawSeperator();

            DrawConnectionSetting();

            DrawSeperator(false);

            DrawToolMenus();

            {
                EditorGUILayout.Space(10);

                string[] captures = CaptureTool.GetCapturedList();

                // Runtime Optimizer Results UI
                GUILayout.Space(10);
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                {
                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(position.width / 2 - 60));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Captured Frames", EditorStyles.boldLabel);

                    if (GUILayout.Button("Import Capture", Styles.ButtonStyle, GUILayout.Width(125), GUILayout.Height(25)))
                    {
                        RuntimeOptimizerPlugin.SendEvent("import_capture", InsightEventDataStr.ToJsonStr(""));
                        string zipPath = EditorUtility.OpenFilePanel($"Select {exportedPackagePostFix} File", "", exportedPackagePostFix);

                        if (!string.IsNullOrEmpty(zipPath))
                        {
                            string extractPath = CaptureTool.GetOutputDirectory();

                            try
                            {
                                // Extract ZIP contents
                                ZipFile.ExtractToDirectory(zipPath, extractPath);
                            }
                            catch (System.Exception e)
                            {
                                RO.Util.DebugLogError($"Failed to extract roz: {e.Message}");
                            }
                        }
                        else
                        {
                            RO.Util.DebugLog("No file selected");
                        }

                    }

                    GUILayout.EndHorizontal();

                    insightCapturePosition = EditorGUILayout.BeginScrollView(insightCapturePosition, Styles.ToolBox, GUILayout.ExpandWidth(true));

                    if (captures.Length > 0)
                    {
                        if (lastSelectedInsight == InsightType.EMPTY_NO_CAPTURES)
                        {
                            lastSelectedInsight = InsightType.EMPTY_WITH_CAPTURES;
                        }

                        foreach (var item in captures)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(item);

                            // start layout
                            if (captureItemProcessed.ContainsKey(item))
                            {
                                Texture2D imageToUse = captureImageCache.ContainsKey(fileName) ? captureImageCache[fileName] : Texture2D.whiteTexture;
                                JSONObject? metricJsonToUse = metricJsonCache.ContainsKey(fileName) ? metricJsonCache[fileName] : null;

                                EditorGUILayout.BeginHorizontal(Styles.CaptureBox);

                                JSONObject? snapshotJsonToUse = snapshotJsonCache.ContainsKey(fileName) ? snapshotJsonCache[fileName] : null;
                                {

                                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Width(kSnapshotWidth));

                                    GUILayout.BeginHorizontal();
                                    DateTime captureDate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToInt64(fileName, 16));
                                    string captureDateStr = captureDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                                    EditorGUILayout.LabelField(captureDateStr, EditorStyles.boldLabel);

                                    GUILayout.FlexibleSpace();
                                    GUILayout.FlexibleSpace();
                                    var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
                                    if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent))
                                    {
                                        GenericMenu menu = new GenericMenu();
                                        menu.AddItem(new GUIContent("Open PTrace"), false, () =>
                                        {
                                            string pathToPtrace = string.Format("{0}/{1}", CaptureTool.GetOutputDirectory(), fileName);
                                            if (File.Exists(item))
                                            {
                                                MetricAPI.OpenPerfettoTrace(item);
                                            }
                                            else
                                            {
                                                RO.Util.DebugLogError("PTrace file does not exist: " + item);
                                            }
                                        });

                                        menu.AddItem(new GUIContent("Delete Capture"), false, () =>
                                        {
                                            if (lastSelectedInsight == InsightType.ACTIONABLE_INSIGHT && lastSelectedInsightName == fileName)
                                            {
                                                if (captures.Length > 1)
                                                {
                                                    lastSelectedInsight = InsightType.EMPTY_WITH_CAPTURES;
                                                }
                                                else
                                                {
                                                    lastSelectedInsight = InsightType.EMPTY_NO_CAPTURES;
                                                }

                                                lastSelectedInsightName = "";
                                            }
                                            RemoveCapture(fileName);
                                            needRepait = true;
                                        });

                                        menu.AddItem(new GUIContent("Export"), false, () =>
                                        {

                                            RuntimeOptimizerPlugin.SendEvent("export_capture", InsightEventDataStr.ToJsonStr(fileName));

                                            string zipPath = EditorUtility.SaveFilePanel(
                                           "Save Your File",
                                           "",
                                           fileName,
                                           exportedPackagePostFix);

                                            string[] files = Directory.GetFiles(CaptureTool.GetOutputDirectory());
                                            List<string> filesToZip = new List<string>();
                                            foreach (string file in files)
                                            {
                                                if (Path.GetFileName(file).Contains(fileName, StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(file).Contains(".meta", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    filesToZip.Add(file);
                                                }
                                            }

                                            if (filesToZip.Count > 0)
                                            {
                                                string tempDir = Path.Combine(Application.temporaryCachePath, "ZipTemp");
                                                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                                                Directory.CreateDirectory(tempDir);

                                                foreach (string file in filesToZip)
                                                {
                                                    string destPath = Path.Combine(tempDir, Path.GetFileName(file));
                                                    File.Copy(file, destPath, true);
                                                }
                                                if (File.Exists(zipPath)) File.Delete(zipPath);
                                                if (!string.IsNullOrEmpty(zipPath)) ZipFile.CreateFromDirectory(tempDir, zipPath);

                                                Directory.Delete(tempDir, true);
                                            }

                                        });

#if HAS_AGENT_BRIDGE
                                        bool hasAIMetricData = metricJsonCache.ContainsKey(fileName);
                                        string aiLabel = _isAIAnalyzing ? "AI Analyzing..." : "\u2728 AI Analysis";
                                        if (hasAIMetricData)
                                        {
                                            menu.AddItem(new GUIContent(aiLabel), false, () =>
                                            {
                                                lastSelectedInsightName = fileName;
                                                lastSelectedInsight = InsightType.AI_ANALYSIS;
                                                RuntimeOptimizerPlugin.SendEvent("ai_analysis_clicked", InsightEventDataStr.ToJsonStr(fileName));
                                                StartAIAnalysisAsync();
                                            });
                                        }
                                        else
                                        {
                                            menu.AddDisabledItem(new GUIContent(aiLabel));
                                        }
#endif

                                        menu.DropDown(overflowMenuRect);
                                    }
                                    GUILayout.EndHorizontal();

                                    GUIContent buttonContent = GetGUPContentForAnalysButton(item, fileName, imageToUse, metricJsonToUse, snapshotJsonToUse);
                                    if (GUILayout.Button(buttonContent, GUILayout.Width(kSnapshotWidth), GUILayout.Height(kSnapshotHeight)))
                                    {
                                        if (!(lastSelectedInsight == InsightType.ACTIONABLE_INSIGHT && lastSelectedInsightName == fileName))
                                        {
                                            RO.Util.DebugLog("Opening Analysis for " + fileName);
                                            if (snapshotJsonToUse == null)
                                            {
                                                RO.Util.DebugLogError("Failed to read snapshot Analysis for " + fileName);
                                            }
                                            if (metricJsonToUse == null)
                                            {
                                                RO.Util.DebugLogError("Failed to generate perfetto Analysis for " + fileName);
                                            }
                                            if (snapshotJsonToUse != null && metricJsonToUse != null)
                                            {
                                                OpenAnalysisForTimestamp(fileName, snapshotJsonToUse, metricJsonToUse);
                                                RuntimeOptimizerPlugin.SendEvent("open_analysis", InsightEventDataStr.ToJsonStr(fileName));
                                            }
                                            else
                                            {
                                                RuntimeOptimizerPlugin.SendEvent("failed_to_open_analysis", InsightEventDataStr.ToJsonStr(fileName));
                                            }
                                        }
                                    }

                                    EditorGUILayout.LabelField("Bottleneck", Styles.InsightToolNote, GUILayout.Width(80));
                                    EditorGUILayout.EndVertical();
                                }

                                EditorGUILayout.EndHorizontal();
                                GUILayout.Space(5);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Run a Bottleneck Analysis or import an existing capture to start", EditorStyles.centeredGreyMiniLabel);
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndScrollView();
                    GUILayout.EndVertical();

                    GUILayout.Space(10);

                    GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                    // Start of Analysis UI
                    EditorGUILayout.BeginHorizontal();
                    if (InsightType.ACTIONABLE_INSIGHT == lastSelectedInsight)
                    {
                        var overflowAnalysis = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButton);
                        if (GUI.Button(overflowAnalysis, Styles.optionsButtonContent, EditorStyles.toolbarButton))
                        {
                            GenericMenu menu = new GenericMenu();
                            bool enabled = EditorPrefs.GetBool(RenderUnityPercentageKey, true);
                            menu.AddItem(new GUIContent("Use Percentage"), enabled, ToggleEnableRenderUnit);
                            menu.DropDown(overflowAnalysis);
                        }
                    }
                    EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel, GUILayout.Width(60));
                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    insightPosition = EditorGUILayout.BeginScrollView(insightPosition, Styles.ToolBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    switch (lastSelectedInsight)
                    {
                        case InsightType.SCENE_INSIGHT:
                            DisplaySceneInsightsScrollView();
                            break;
                        case InsightType.ACTIONABLE_INSIGHT:
                            DisplayActionableInsightsScrollView();
                            break;
                        case InsightType.EMPTY_WITH_CAPTURES:
                            DisplayEmptyInsight();
                            break;
                        case InsightType.EMPTY_NO_CAPTURES:
                            DisplayNoCapturesUI();
                            lastSelectedInsightName = "";
                            break;
#if HAS_AGENT_BRIDGE
                        case InsightType.AI_ANALYSIS:
                            DisplayAIAnalysisPanel();
                            break;
#endif
                    }
                    EditorGUILayout.EndScrollView();
                    GUILayout.EndVertical();
                }

                GUILayout.Space(10);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

        }
        private void DrawIntField(Func<int> get, Action<int> set, GUIContent content, bool sendTelemetry = true)
        {
            DrawSetting(get, set, content, (guiContent, func) => EditorGUILayout.IntField(guiContent, func.Invoke()),
                sendTelemetry);
        }
        private void DrawSetting<T>(Func<T> get, Action<T> set, GUIContent content,
            Func<GUIContent, Func<T>, T> editorGuiFunction, bool sendTelemetry = true)
        {
            EditorGUI.BeginChangeCheck();
            var value = editorGuiFunction.Invoke(content, get);
            if (EditorGUI.EndChangeCheck())
            {
                set.Invoke(value);
            }
        }
    }
    public enum InsightType
    {
        EMPTY_NO_CAPTURES = 0,
        EMPTY_WITH_CAPTURES = 1,
        SCENE_INSIGHT = 2,
        ACTIONABLE_INSIGHT = 3,
        QUICK_PERF_INSIGHT = 4,
#if HAS_AGENT_BRIDGE
        AI_ANALYSIS = 5,
#endif
    }
}
