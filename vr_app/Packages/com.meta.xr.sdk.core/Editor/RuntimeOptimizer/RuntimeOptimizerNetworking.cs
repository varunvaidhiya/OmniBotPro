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
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using RO = Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight;


namespace Meta.XR.RuntimeOptimizer.Editor
{
    public partial class RuntimeOptimizerWindow
    {
        private const float kEmptyFrameThreshold = 0.01f;

        // Network connection management
        bool EnsureConnection()
        {
            // In PC Testing mode, check if we need to establish a connection
            if (pcTestingMode && (client == null || !client.Connected))
            {
                RO.Util.DebugLog("PC Testing Mode: Attempting to establish connection with port discovery...");

                if (client == null)
                {
                    client = new RO.OVRNetwork.OVRNetworkTcpClient();
                    client.payloadReceivedCallback += OnPayloadReceived;
                    client.connectionStateChangedCallback += OnConnectionStateChanged;
                    client.NetworkErrorOccurred += OnNetworkError;
                }

                // Try to discover the port from the port file
                int port = RO.RuntimeOptimizerPortDiscovery.ReadPortFromFile();

                if (port > 0)
                {
                    // Port discovered from file - try connecting to it
                    RO.Util.DebugLog($"Discovered port {port} from port file, attempting connection...");
                    client.Connect(port);

                    // Wait briefly for connection
                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 5)
                    {
                        Thread.Sleep(100);
                        if (client.connectionState == RO.OVRNetwork.OVRNetworkTcpClient.ConnectionState.Connected)
                        {
                            playerConnected = true;
                            runtimeServicePID = 1;
                            RO.Util.DebugLog($"Connection established successfully on discovered port {port}");
                            return true;
                        }
                    }

                    // If failed, fall through to try all ports
                    RO.Util.DebugLog($"Failed to connect to discovered port {port}, trying port range...");
                    client.Disconnect();
                }
                else
                {
                    RO.Util.DebugLog("Port file not found or invalid, trying port range 12345-12355...");
                }

                // Try all ports in range if port file discovery failed or connection to discovered port failed
                for (int tryPort = 12345; tryPort <= 12355; tryPort++)
                {
                    RO.Util.DebugLog($"Trying port {tryPort}...");
                    client.Connect(tryPort);

                    // Wait briefly for connection
                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 1)
                    {
                        Thread.Sleep(100);
                        if (client.connectionState == RO.OVRNetwork.OVRNetworkTcpClient.ConnectionState.Connected)
                        {
                            playerConnected = true;
                            runtimeServicePID = 1;
                            RO.Util.DebugLog($"Connection established successfully on port {tryPort}");
                            return true;
                        }
                    }

                    // Disconnect and try next port
                    client.Disconnect();
                }

                RO.Util.DebugLogError("Failed to establish connection on any port (12345-12355)");
                return false;
            }

            // For non-PC mode or already connected, check existing connection
            return client != null && client.Connected;
        }

        void SendToServer(string message)
        {
            if (EnsureConnection())
            {
                byte[] payload = Encoding.ASCII.GetBytes(message);
                client.Send(RO.PerformanceDataUtils.RuntimeOptimizerServiceRequestId, payload);
            }
            else
            {
                RO.Util.DebugLogError($"Cannot send message '{message}': not connected to server");
            }
        }

        public void Send(int payloadType, byte[] payload)
        {
            if (client != null && client.Connected)
            {
                client.Send(payloadType, payload);
            }
            else
            {
                RO.Util.DebugLogError("Cannot send data: client not connected");
            }
        }

        void OnConnectionStateChanged()
        {
            if (client.connectionState == RO.OVRNetwork.OVRNetworkTcpClient.ConnectionState.Disconnected)
            {
                playerConnected = false;
                needRepait = true;
                RO.Util.DebugLog("Disconnected from server");
            }
            if (client.connectionState == RO.OVRNetwork.OVRNetworkTcpClient.ConnectionState.Connected)
            {
                RO.Util.DebugLog("Connected to server");
            }
        }

        // Network error handling
        private void OnNetworkError(object sender, RO.NetworkErrorEventArgs e)
        {
            // Rate limiting to prevent spam
            if (ShouldLogNetworkError(e.Category))
            {
                // Create detailed error context
                var errorContext = new
                {
                    Category = e.Category.ToString(),
                    Operation = e.Operation,
                    Message = e.ErrorMessage,
                    Context = e.Context,
                    Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ExceptionType = e.Exception?.GetType().Name ?? "Unknown"
                };

                string errorJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(errorContext));

                // Use centralized error logging
                RO.Util.DebugLogError($"Network Error [{e.Category}]: {e.Operation} - {e.ErrorMessage}");

                // Send specific telemetry event for network errors
                RO.RuntimeOptimizerPlugin.SendEvent("network_error", errorJson);

                // Update error statistics
                UpdateNetworkErrorStats(e.Category);

                // Attempt recovery for certain error categories
                AttemptNetworkRecovery(e.Category, e.Operation);
            }
        }

        private bool ShouldLogNetworkError(RO.NetworkErrorCategory category)
        {
            var now = DateTime.Now;
            if ((now - lastNetworkErrorTime).TotalMinutes >= 1)
            {
                networkErrorCounts.Clear();
                lastNetworkErrorTime = now;
            }

            if (!networkErrorCounts.ContainsKey(category))
            {
                networkErrorCounts[category] = 0;
            }

            if (networkErrorCounts[category] < MAX_ERROR_EVENTS_PER_MINUTE)
            {
                networkErrorCounts[category]++;
                return true;
            }

            return false;
        }

        private void UpdateNetworkErrorStats(RO.NetworkErrorCategory category)
        {
            // Could be used for displaying error statistics in UI
            // or for additional telemetry aggregation
            needRepait = true; // Trigger UI refresh if needed
        }

        private void AttemptNetworkRecovery(RO.NetworkErrorCategory category, string operation)
        {
            switch (category)
            {
                case RO.NetworkErrorCategory.ClientConnection:
                case RO.NetworkErrorCategory.ClientDisconnection:
                    // Attempt to reconnect after a delay
                    EditorApplication.delayCall += () =>
                    {
                        if (!client.Connected && playerConnected)
                        {
                            RO.Util.DebugLog("Attempting network recovery...");
                            // Trigger reconnection logic - this will be handled by the existing connection logic
                        }
                    };
                    break;

                case RO.NetworkErrorCategory.ServerStartup:
                    // Try to release and re-forward ports
                    CaptureTool.ReleasePort(remoteListeningPort);
                    CaptureTool.ForwardPort(remoteListeningPort);
                    break;
            }
        }

        // Payload handlers
        private void OnPayloadReceived(int payloadType, byte[] buffer, int offset, int length)
        {
            // Called on the TCP background thread. Enqueue for processing in Update() so
            // scan results are handled even when the editor is out of focus.
            // EditorApplication.delayCall stalls when Unity throttles its main loop on focus loss.
            string message = Encoding.ASCII.GetString(buffer, offset, length);
            pendingPayloads.Enqueue((payloadType, message));
        }

        // Dispatches a single network payload to the appropriate handler.
        // Must be called on the main thread (invoked from Update()).
        private void ProcessPayload(int payloadType, string message)
        {
            if (payloadType == RO.PerformanceDataUtils.RuntimeOptimizerPerfId)
            {
                OnReceivedPerfData(message);
            }
            else if (payloadType == RO.PerformanceDataUtils.RuntimeOptimizerStateId)
            {
                OnReceivedStateData(message);
            }
            else if (payloadType == RO.PerformanceDataUtils.SceneDiagnosisCaptureId)
            {
                OnReceivedCaptureRequest(message);
            }
            else if (payloadType == RO.PerformanceDataUtils.SceneDiagnosisDataId)
            {
                OnReceivedSceneDiagnosisData(message);
            }
            else if (payloadType == RO.PerformanceDataUtils.SceneDiagnosisMetadataId)
            {
                OnReceivedSceneDiagnosisMetadata(message);
                StartCountdownTimer();
            }
            else if (payloadType == RO.PerformanceDataUtils.QuickPerfDataId)
            {
                OnReceivedQuickPerfData(message);
            }
            else if (payloadType == RO.PerformanceDataUtils.FrustrumGameObjectsDataId)
            {
                OnReceivedFrustrumGameObjects(message);
            }
        }

        private void OnReceivedStateData(string message)
        {
            JSONObject? jsonNode = null;
            try
            {
                jsonNode = JSONObject.Parse(message) as JSONObject;
                if (jsonNode != null)
                {
                    float timeScale = UnityInsightsHelper.GetFloatVal(jsonNode, "timeScale");
                    runtimeIsFrozen = timeScale == 0.0f;
                    needRepait = true;
                }
                else
                {
                    UnityEngine.Debug.Log("Failed to parse StateData json: " + message);
                }
            }
            catch (Exception e)
            {
                RO.Util.DebugLogError("Failed to parse PerfData json: " + e.Message);
            }
        }

        private void OnReceivedPerfData(string message)
        {
            RO.Util.DebugLog("Perf Data: " + message);
            JSONObject? jsonNode = null;
            try
            {
                jsonNode = JSONObject.Parse(message) as JSONObject;
            }
            catch (Exception e)
            {
                RO.Util.DebugLogError("Failed to parse PerfData json: " + e.Message);
            }

            // runtime will send us message when framerate is low, we capture
            bool enableAutoCapture = EditorPrefs.GetBool(EnableBackgorundCaptureInight, false);
            if (IsCapturingBottlenecks == "" && enableAutoCapture && !runtimeIsFrozen)
            {
                bool enableFreeze = EditorPrefs.GetBool(EnableFrameFreezeForInsight, true);
                IssueCaptureInsight(enableFreeze);

                var eventData = new InsightEventData();
                eventData.IsForzen = enableFreeze;
                string json = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(eventData));
                RO.RuntimeOptimizerPlugin.SendEvent("insight_capture_background", json);
            }
        }

        private void OnReceivedCaptureRequest(string message)
        {

            string captureName = CaptureTool.EpochTime().ToString("X");

            RO.Util.DebugLog(message);

            JSONObject? jsonNode = null;
            if (!snapshotJsonCache.ContainsKey(captureName))
            {
                try
                {
                    jsonNode = JSONObject.Parse(message) as JSONObject;
                    if (jsonNode != null)
                    {
                        snapshotJsonCache.Add(captureName, jsonNode);
                    }
                }
                catch (Exception e)
                {
                    RO.Util.DebugLogError("Failed to parse CaptureRequest json: " + e.Message);
                }

                // Allow processing if we're not capturing or if this matches our immediate UI capture
                if (IsCapturingBottlenecks == "" || IsCapturingBottlenecks == captureName)
                {
                    // If not already set (legacy path), set it now
                    if (IsCapturingBottlenecks == "")
                    {
                        IsCapturingBottlenecks = captureName;
                    }

                    CaptureTool.IssuePerfettoCaptureAsync(true, bundleName, captureName, false, () =>
                    {
                        if (runtimeIsFrozen)
                        {
                            DisableProximitySensor();
                            StartHeadlock();
                        }
                    },
                    () =>
                    {
                        needRepait = true;
                    });

                    string saveToFilePath = string.Format("{0}/{1}{2}", CaptureTool.GetOutputDirectory(), captureName, snapshotUnityPostfix);
                    System.IO.File.WriteAllText(saveToFilePath, message);

                    if (aswIsOn)
                    {
                        string saveToFilePathAWS = string.Format("{0}/{1}{2}", CaptureTool.GetOutputDirectory(), captureName, snapshotASW);
                        System.IO.File.WriteAllText(saveToFilePathAWS, "AWS=");

                        captureName += "_ASW";
                    }

                    RO.RuntimeOptimizerPlugin.SendEvent("save_runtime_data_to_json", InsightEventDataStr.ToJsonStr(captureName));
                }
            }
        }

        private void OnReceivedSceneDiagnosisMetadata(string message)
        {
            if (!isCapturingWhatIf)
            {
                return;
            }

            lastSelectedInsight = InsightType.SCENE_INSIGHT;

            var matadata = message.Split(',');
            RO.RuntimeOptimizerPlugin.SendEvent("scene_diagnosis_metadata", InsightEventDataStr.ToJsonStr(message));
            foreach (var data in matadata)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    var keyValue = data.Split(':');
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();
                    switch (key)
                    {
                        case "TotalObjects":
                            totalGameObjectCount = int.Parse(value);
                            break;
                        case "RenderThreadBaseline":
                            renderThreadBaseline = float.Parse(value);
                            break;
                        case "MainThreadBaseline":
                            mainThreadBaseline = float.Parse(value);
                            break;
                        case "SamplingVariance":
                            samplingVariance = float.Parse(value);
                            break;
                    }
                }
            }
        }

        private void OnReceivedSceneDiagnosisData(string message)
        {
            if (!isCapturingWhatIf)
            {
                return;
            }
            lastSelectedInsight = InsightType.SCENE_INSIGHT;

            var parsedMessage = message.Split(';');

            if (parsedMessage.Length > 0 && parsedMessage[0].Trim().Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                RO.RuntimeOptimizerPlugin.SendEvent("scene_diagnosis_data", InsightEventDataStr.ToJsonStr("success"));
                foreach (var dataPoint in parsedMessage.Skip(1))
                {
                    if (!string.IsNullOrEmpty(dataPoint))
                    {
                        var properties = dataPoint.Split(',');
                        var gameObjectPerformanceData = new RO.GameObjectPerformanceMetaData();
                        foreach (var property in properties)
                        {
                            var keyValue = property.Split(':');
                            var key = keyValue[0].Trim();
                            var value = keyValue[1].Trim();
                            switch (key)
                            {
                                case "GameObjectName":
                                    gameObjectPerformanceData.GameObjectName = value;
                                    break;
                                case "CpuMainThreadTime":
                                    // Only parse if value is not empty (for group mode individual GOs)
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        gameObjectPerformanceData.CpuMainThreadTime = double.Parse(value);
                                    }
                                    break;
                            }
                        }
                        gameObjectPerformanceDataList.Add(gameObjectPerformanceData);
                    }
                }
                sortGameObjectDataListBy(ref gameObjectPerformanceDataList, sortingFilterSelection);

                // Clear monitoring flags when What If analysis completes successfully
                isActivelyCapturing = false;
                captureStartTime = default(DateTime);
            }
            else
            {
                // NOTE: Don't unfreeze automatically - let user control when to unfreeze
                // StopHeadlock();
                RO.Util.DebugLogError("Failed to get data from what if analysis");

                // Clear monitoring flags when What If analysis fails
                isActivelyCapturing = false;
                captureStartTime = default(DateTime);
            }

            StopCountdownTimer();
            isCapturingWhatIf = false;
            isWhatIfGroupMode = false;

            // Don't unfreeze automatically after What If analysis completes
            // User can manually unfreeze using the Unpause button

            // Stop headlock after what if analysis completes
            // StopHeadlock();

            // Refresh and redraw the window
            needRepait = true;
            Repaint();
        }

        private bool IsEmptyFrame(RO.QuickPerfData data)
        {
            return data.cpuFrameTime <= kEmptyFrameThreshold && data.gpuFrameTime <= kEmptyFrameThreshold;
        }

        private void OnReceivedQuickPerfData(string message)
        {
            try
            {
                RO.QuickPerfData quickPerfData = JsonUtility.FromJson<RO.QuickPerfData>(message);

                if (quickPerfData.isValid)
                {
                    quickPerfData.cpuFrameTime = quickPerfData.cpuFrameTime * (1.0f - kQuickPerfTolerenceCPUPercentage);
                    quickPerfData.gpuFrameTime = quickPerfData.gpuFrameTime * (1.0f - kQuickPerfTolerenceGPUPercentage);

                    if (IsEmptyFrame(quickPerfData))
                    {
                        RO.Util.DebugLogError("Empty frame detected: CPU and GPU frame times are near zero. Device may have entered sleep mode.");

                        EditorUtility.DisplayDialog(
                            "Empty Frame Captured",
                            "An empty frame was captured with 0.00ms CPU and GPU values. This typically occurs when the headset enters sleep mode during capture.\n\nPlease ensure the headset remains active during analysis and try again.",
                            "OK"
                        );

                        var emptyFrameEventData = new
                        {
                            cpuFrameTime = quickPerfData.cpuFrameTime,
                            gpuFrameTime = quickPerfData.gpuFrameTime,
                            frameRate = quickPerfData.frameRate,
                            capturingForStaging = capturingForStaging,
                            runtimeIsFrozen = runtimeIsFrozen,
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        string emptyFrameJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(emptyFrameEventData));
                        RO.RuntimeOptimizerPlugin.SendEvent("empty_frame_detected_device_sleep", emptyFrameJson);

                        if (capturingForStaging)
                        {
                            capturingForStaging = false;
                            lock (freezeFrameLock)
                            {
                                freezeFrameMetricsComplete = true;
                            }
                        }

                        isActivelyCapturing = false;
                        captureStartTime = default(DateTime);
                        needRepait = true;
                        return;
                    }

                    // Route to staging or permanent based on capture context
                    if (capturingForStaging)
                    {
                        // Store in staging area
                        stagingPerfMetrics = quickPerfData;
                        capturingForStaging = false;

                        // Mark metrics as complete for freeze frame
                        lock (freezeFrameLock)
                        {
                            freezeFrameMetricsComplete = true;
                        }
                        RO.Util.DebugLog("Freeze frame metrics received and marked as complete");

                        // Check if freeze frame is fully complete
                        CheckFreezeFrameCompletion();

                        // Log telemetry for frozen frame metrics displayed
                        RO.RuntimeOptimizerPlugin.SendEvent("frozen_frame_metrics_displayed",
                            InsightEventDataStr.ToJsonStr($"CPU:{quickPerfData.cpuFrameTime:F2},GPU:{quickPerfData.gpuFrameTime:F2},FPS:{quickPerfData.frameRate:F1}"));

                        RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_staging_received",
                            InsightEventDataStr.ToJsonStr($"CPU:{quickPerfData.cpuFrameTime:F2},GPU:{quickPerfData.gpuFrameTime:F2}"));
                    }

                    // Clear capture monitoring state
                    isActivelyCapturing = false;
                    captureStartTime = default(DateTime);
                    needRepait = true;
                }
                else
                {
                    if (quickPerfData.frameRate == 0.0f && quickPerfData.verticesCount == 0.0f && quickPerfData.cpuFrameTime == 0.0f && quickPerfData.gpuFrameTime == 0.0f)
                    {
                        // Show popup indicating capture is in progress
                        EditorUtility.DisplayDialog(
                            "Performance Capture In Progress",
                            "Performance capture is currently in progress. Please wait a moment for the data to be collected.",
                            "OK"
                        );
                        RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_data_zeroed", InsightEventDataStr.ToJsonStr("capture_in_progress"));
                    }
                    else
                    {
                        RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_data_invalid", InsightEventDataStr.ToJsonStr("received_invalid_data"));
                        RO.Util.DebugLogError("Received invalid quick perf data");
                    }
                }
            }
            catch (System.Exception ex)
            {
                var errorEventData = new
                {
                    errorMessage = ex.Message,
                    exceptionType = ex.GetType().Name,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string errorJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(errorEventData));
                RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_data_parse_failed", errorJson);
                RO.Util.DebugLogError($"Failed to parse quick perf data: {ex.Message}");
            }
        }

        private void RequestQuickPerfUpdate()
        {
            if (playerConnected && !isCapturingWhatIf && IsCapturingBottlenecks == "")
            {
                // Set capture monitoring state (uses unified timeout system)
                isActivelyCapturing = true;
                captureStartTime = DateTime.Now;
                needRepait = true;

                SendToServer("requestQuickPerf");

                // Log quick perf request with context
                var requestEventData = new
                {
                    playerConnected = playerConnected,
                    isCapturingWhatIf = isCapturingWhatIf,
                    isCapturingBottlenecks = IsCapturingBottlenecks != "",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string requestJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(requestEventData));
                RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_data_requested", requestJson);

                RO.Util.DebugLog("Quick perf data requested from editor");
            }
            else
            {
                // Log when request is blocked and why
                var blockedEventData = new
                {
                    playerConnected = playerConnected,
                    isCapturingWhatIf = isCapturingWhatIf,
                    isCapturingBottlenecks = IsCapturingBottlenecks != "",
                    reason = !playerConnected ? "player_not_connected" :
                            isCapturingWhatIf ? "whatif_in_progress" : "bottleneck_in_progress",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string blockedJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(blockedEventData));
                RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_request_blocked", blockedJson);
            }
        }

        private void RequestQuickPerfForStaging()
        {
            if (playerConnected)
            {
                // Mark that we're capturing for staging
                capturingForStaging = true;
                isActivelyCapturing = true;
                captureStartTime = DateTime.Now;

                SendToServer("requestQuickPerf");

                RO.Util.DebugLog("Quick perf data requested for staging area");
                RO.RuntimeOptimizerPlugin.SendEvent("quick_perf_staging_requested",
                    InsightEventDataStr.ToJsonStr("freeze_frame"));
            }
        }

        private void OnReceivedFrustrumGameObjects(string message)
        {
            try
            {
                // Deserialize the frustrum GameObject data
                var frustrumData = JsonUtility.FromJson<RO.FrustrumGameObjectsData>(message);

                if (frustrumData != null && frustrumData.gameObjectPaths != null)
                {
                    RO.Util.DebugLog($"Received {frustrumData.totalCount} GameObject paths from frustrum");

                    // Show the GameObject selection dialog
                    ShowGameObjectSelectionDialog(frustrumData.gameObjectPaths);

                    // Send telemetry
                    RO.RuntimeOptimizerPlugin.SendEvent("frustrum_gameobjects_received",
                        InsightEventDataStr.ToJsonStr($"Count:{frustrumData.totalCount}"));
                }
                else
                {
                    RO.Util.DebugLogError("Received invalid frustrum GameObject data");
                    RO.RuntimeOptimizerPlugin.SendEvent("frustrum_gameobjects_invalid",
                        InsightEventDataStr.ToJsonStr("invalid_data"));
                }
            }
            catch (System.Exception ex)
            {
                var errorEventData = new
                {
                    errorMessage = ex.Message,
                    exceptionType = ex.GetType().Name,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string errorJson = InsightEventDataStr.ToJsonStr(JsonUtility.ToJson(errorEventData));
                RO.RuntimeOptimizerPlugin.SendEvent("frustrum_gameobjects_parse_failed", errorJson);
                RO.Util.DebugLogError($"Failed to parse frustrum GameObject data: {ex.Message}");
            }
        }
    }
}
