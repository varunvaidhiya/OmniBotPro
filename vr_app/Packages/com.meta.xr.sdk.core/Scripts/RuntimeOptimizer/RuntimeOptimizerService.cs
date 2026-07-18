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

#if ENABLE_RUNTIME_OPTIMIZER

using UnityEngine;
using System.Text;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.XR;
using System.Linq;
using Unity.Profiling;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using UnityEngine.AI;
using UnityEngine.Playables;
using Meta.XR.RuntimeOptimizer.Core;



namespace Meta.XR.RuntimeOptimizer.Scripts
{

    [Serializable]
    public class SerializableDictionary
    {
        public uint totalVertCount;
        public uint totalTriangleCount;
        public uint totalTransparentVertCount;
        public uint totalMeshCount;
        public uint totalMatCount;
        public uint totalDrawcount;
        public long totalGMemAlloc;
        public uint slowFrameCount;
        public float longFrameTime;
        public List<GameObjInsight> items = new List<GameObjInsight>();
        public List<TextureInsight> textures = new List<TextureInsight>();
    }

    [Serializable]
    public class GameObjInsight
    {
        public string name;
        public string parentName;
        public uint vertCount;
        public uint meshCount;
        public bool hasTransparency;
        public List<MeshInsight> meshes = new List<MeshInsight>();
        public List<string> materialNames = new List<string>();
    }

    [Serializable]
    public class MeshInsight
    {
        public string name;
        public uint vertexCount;
    }

    [Serializable]
    public class TextureInsight
    {
        public string name;
        public string path;
        public string bpp;
        public string format;
        public string filterMode;
        public long runtimeSize;
        public int width;
        public int height;
        public int anisoLevel;
        public int depth;
        public int mipCount;
        public bool isReadable;
    }

    [Serializable]
    public class FrustrumGameObjectsData
    {
        public List<string> gameObjectPaths = new List<string>();
        public int totalCount;
    }

    [Serializable]
    public class PerfData
    {
        public float fps;
        public int slowFrameCount;
        public float longFrameTime;
        public int processTime;
    }

    [Serializable]
    public class StateData
    {
        public float timeScale;
    }

    public class RuntimeOptimizerService : MonoBehaviour
    {
        private bool gpuTimeTest = false;
        private int gameObjectIndex;
        private float baselineMainThread = 0.0f;
        private float baselineCoeffiecientVariation = 0.0f;
        private bool currentlyLooping = false;
        private List<GameObject> gameObjectsInFrustrum = new List<GameObject>();
        private List<GameObject> renderablesInFrustrum = new List<GameObject>();
        private bool runAsGroupMode = false;
        private int currentFrameCount = 0;
        private int sampleFrameCount = 5;
        private int sampleBufferSize = 3; // 1 frame is flushed so sampleFrameCount - 1
        private int baselineSamplingCount = 20;
        private int flushBufferCount = 30;
        private List<double> frameTimeList = new List<double>();
        private List<GameObjectPerformanceMetaData> gameObjectTimes = new List<GameObjectPerformanceMetaData>();
        private FrameTiming[] frameTiming = new FrameTiming[1];
        private float beginCountdownTime = 0f;
        // for insights
        private const float kInsightDetectionIntervalInSec = 2.0f;
        private const float kInsightMinimalCaptureInterval = 5.0f;
        private const float kInsightMaxFrameTime = 13.9f;
        private const float kInsightSpikedFrameTime = kInsightMaxFrameTime * 3.0f;
        private const float kInsightSlowFrameTime = kInsightMaxFrameTime * 1.3f;
        private DateTime lastCaptureTime = DateTime.Now;
        private readonly Queue<(DateTime timestamp, float frameTime)> frameTimes = new Queue<(DateTime timestamp, float frameTime)>();
        private bool insightsEnabled = true;
        private List<Animator> previouslyDisabledAnimators;
        private List<AudioSource> previouslyPlayingAudioSources;
        private List<NavMeshAgent> previouslyEnabledNavMeshAgents;
        private List<ParticleSystem> previouslyPlayingParticleSystems;
        private List<PlayableDirector> previouslyPlayingDirectors;
        ProfilerRecorder drawCallsRecorder;
        ProfilerRecorder verticesRecorder;
        ProfilerRecorder triganleRecorder;
        SynchronizationContext context;
        SynchronizationContext backgroundThread;
        private OVRNetwork.OVRNetworkTcpServer server = new OVRNetwork.OVRNetworkTcpServer();
        private bool isFrozen = false;
        private float lastValidGpuFrameTime = -1.0f;

        // Unified performance monitoring
        private bool unifiedPerfRequested = false;
#pragma warning disable CS0414
        private bool quickPerfAutoUpdateEnabled = false;  // NEW: Control auto-update
#pragma warning restore CS0414
        private const float kUnifiedUpdateInterval = 2.0f;  // Auto-update every 2 seconds
        private DateTime lastUnifiedUpdate = DateTime.MinValue;

        // GPU frame time sliding window
        private readonly Queue<(DateTime timestamp, float frameTime)> gpuFrameTimes = new Queue<(DateTime, float)>();
        private const int kQuickPerfGpuBufferSize = 16;

        // Frame timing for CPU frame time
        private FrameTiming[] quickPerfFrameTiming = new FrameTiming[1];

        // GPU trace state tracking for 2-step profiling process
        private bool gpuTraceInProgress = false;
        private DateTime gpuTraceStartTime = DateTime.MinValue;
        private const float kGpuTraceWaitTime = 2.0f; // Wait 1 second between start and stop

#if UNITY_EDITOR
        // Mock frame time data for Unity Editor
        private System.Random mockRandom = new System.Random();
        private float[] GenerateMockFrameTimes(int count)
        {
            float[] mockFrameTimes = new float[count];
            for (int i = 0; i < count; i++)
            {
                // Generate realistic frame times between 12-20ms for VR
                mockFrameTimes[i] = 12.0f + (float)(mockRandom.NextDouble() * 8.0);
            }
            return mockFrameTimes;
        }

        private bool MockGetFrameTime(int bufferSize, out float[] frameTimes, string appId, bool clear = false)
        {
            frameTimes = GenerateMockFrameTimes(bufferSize);
            return true;
        }

        private void MockInitialize(string param1, string param2)
        {
            Debug.Log("[EDITOR MOCK] RuntimeOptimizerPlugin.Initialize called with mock implementation");
        }

        private void MockInitializeGpuProfiling()
        {
            Debug.Log("[EDITOR MOCK] RuntimeOptimizerPlugin.InitializeGpuProfiling called with mock implementation");
        }
#endif

        public void Start()
        {
#if UNITY_EDITOR
            MockInitialize("", "");
            MockInitializeGpuProfiling();
#else
            RuntimeOptimizerPlugin.Initialize("", "");
            RuntimeOptimizerPlugin.InitializeGpuProfiling();
#endif
            // Use dynamic port discovery to handle port conflicts
            int port = server.StartListeningWithPortDiscovery(12345, 12445);
            if (port > 0)
            {
                // Write port to file for client discovery
                RuntimeOptimizerPortDiscovery.WritePortToFile(port);
            }
            else
            {
                Debug.LogError("[RuntimeOptimizer] Failed to start server on any port in range 12345-12355");
            }
            server.messageReceivedCallback += OnMessageReceived;

            context = SynchronizationContext.Current;
                        // Register a callback for incoming messages
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            triganleRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }
        private void OnDestroy()
        {
            drawCallsRecorder.Dispose();
            verticesRecorder.Dispose();
            triganleRecorder.Dispose();
            server.StopListening();
        }

        private void ParseEditorMessage(string decodedString)
        {
            if (decodedString.StartsWith("startPerfTest"))
            {
                string[] parts = decodedString.Split(';');
                if (parts.Length > 1)
                {
                    // Check for group mode flag in the first part
                    runAsGroupMode = parts[0].Contains(":GROUP");

                    // Extract game object names from the message
                    List<string> gameObjectNames = new List<string>();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(parts[i]))
                        {
                            gameObjectNames.Add(parts[i]);
                        }
                    }

                    string modeStr = runAsGroupMode ? "GROUP MODE" : "INDIVIDUAL MODE";
                    Debug.Log($"[RuntimeOptimizer] Selective test requested for {gameObjectNames.Count} GameObjects in {modeStr}: {string.Join(", ", gameObjectNames)}");

                    // Find the specified game objects
                    List<GameObject> specificObjects = new List<GameObject>();
                    foreach (string name in gameObjectNames)
                    {
                        string searchName = name.Replace("//", "/");
                        Debug.Log($"[RuntimeOptimizer] Searching for: {searchName}");
                        GameObject obj = GameObject.Find(searchName);
                        if (obj != null)
                        {
                            Debug.Log($"[RuntimeOptimizer] Found object: {obj.name}");
                            specificObjects.Add(obj);
                        }
                        else
                        {
                            Debug.LogWarning($"[RuntimeOptimizer] GameObject not found: {searchName}");
                        }
                    }
                    Debug.Log($"[RuntimeOptimizer] Starting selective analysis with {specificObjects.Count} objects");
                    StartSceneAnalysis(specificObjects);
                }
                else
                {
                    runAsGroupMode = false;
                    Debug.Log("[RuntimeOptimizer] Starting full scene analysis (all objects)");
                    StartSceneAnalysis();
                }
            }
            else if (decodedString == "clear")
            {
                ClearSceneAnalysis();
            }
            else if (decodedString == "captureInsights")
            {
                InsightCaptureRequested();
            }
            else if (decodedString == "captureInsightsFreeze")
                {
                    FreezeDeltaTime(true);
                InsightCaptureRequested();
            }
            else if (decodedString == "freeze")
            {
                FreezeDeltaTime(true);
            }
            else if (decodedString == "unfreeze")
            {
                FreezeDeltaTime(false);
            }
            else if (decodedString == "requestQuickPerf")
            {
                RequestUnifiedPerfData();
            }
            else if (decodedString == "getFrustrumGameObjects")
            {
                GetFrustrumGameObjectsRequested();
            }
        }

        private void GetFrustrumGameObjectsRequested()
        {
            Debug.Log("[RuntimeOptimizer] Frustrum GameObject list requested");

            // Find all visible renderable objects in frustrum
            List<GameObject> visibleObjects = FindAllRenderableObjects();

            FrustrumGameObjectsData frustrumData = new FrustrumGameObjectsData();
            frustrumData.totalCount = visibleObjects.Count;

            // Build GameObject paths for each visible object
            foreach (GameObject go in visibleObjects)
            {
                Transform parent = go.transform.parent;
                string path = go.name;

                // Build full hierarchy path
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.transform.parent;
                }

                frustrumData.gameObjectPaths.Add(path);
            }

            // Serialize and send back to editor
            string json = JsonUtility.ToJson(frustrumData);
            var bytes = Encoding.ASCII.GetBytes(json);
            SendToClients(PerformanceDataUtils.FrustrumGameObjectsDataId, bytes);

            Debug.Log($"[RuntimeOptimizer] Sent {frustrumData.totalCount} GameObject paths to editor");
        }

        void SendToClients(int payloadType, byte[] payload) {
            if (server.HasConnectedClient()) {
                server.Broadcast(payloadType, payload); // Using Broadcast instead
            } else {
                Debug.Log("!! No clients connected !!");
            }
        }


        private void OnMessageReceived(TcpClient client, int payloadType, byte[] payload, int offset, int length)
        {
            // Get the current synchronization context
            string message = System.Text.Encoding.UTF8.GetString(payload, offset, length);

            backgroundThread = SynchronizationContext.Current;
            context.Post(state =>
            {
                if (payloadType == PerformanceDataUtils.RuntimeOptimizerServiceRequestId)
                {
                    ParseEditorMessage(message);
                }
            }, null);
        }

        private void ClearSceneAnalysis(bool unfreeze = true)
        {
            if (unfreeze)
            {
                FreezeDeltaTime(false);
            }
            currentlyLooping = false;
            gameObjectTimes.Clear();
            frameTimeList.Clear();
            currentFrameCount = 0;
            gpuTimeTest = false;
            runAsGroupMode = false;
            float[] frameTimes;
#if UNITY_EDITOR
            MockGetFrameTime(1, out frameTimes, Application.identifier, true);
#else
            RuntimeOptimizerPlugin.GetFrameTime(1, out frameTimes, Application.identifier, true);
#endif
        }

        private void StartSceneAnalysis(List<GameObject> specificGameObjects = null)
        {
            FreezeDeltaTime(true);
            gameObjectsInFrustrum = specificGameObjects ?? FindAllRenderableObjects(); // Use provided objects or find all
            renderablesInFrustrum = gameObjectsInFrustrum;

            // Increase sampling accuracy for small selective tests
            if (specificGameObjects != null && specificGameObjects.Count < 15)
            {
                sampleFrameCount = 10;
                sampleBufferSize = 7;
                baselineSamplingCount = 25;
            }
            else
            {
                // Reset to default values for larger tests
                sampleFrameCount = 5;
                sampleBufferSize = 3;
                baselineSamplingCount = 20;
            }

            currentlyLooping = true;
            gameObjectTimes.Clear();
            frameTimeList.Clear();
            gameObjectIndex = gameObjectsInFrustrum.Count;
            currentFrameCount = baselineSamplingCount;
            gpuTimeTest = true;
            beginCountdownTime = Time.unscaledTime;
            StartGpuProfiling();
            Debug.Log($"[RuntimeOptimizer] Starting Scene Analysis with {gameObjectsInFrustrum.Count} objects");
        }

        private void StartGpuProfiling()
        {
            float[] frameTimes;
            bool success;
#if UNITY_EDITOR
            success = MockGetFrameTime(1, out frameTimes, Application.identifier);
#else
            success = RuntimeOptimizerPlugin.GetFrameTime(1, out frameTimes, Application.identifier);
#endif
            if (!success)
            {
                ClearSceneAnalysis(unfreeze: false);
                var serializedData = "failed;";
                var bytes = Encoding.ASCII.GetBytes(serializedData);
                SendToClients(PerformanceDataUtils.SceneDiagnosisDataId, bytes);
#if UNITY_EDITOR
                Debug.LogError("[EDITOR MOCK] StartGpuProfiling failed to start (simulated failure for testing).");
#else
                Debug.LogError("StartGpuProfiling failed to start. Make sure you run adb shell ovrgpuprofiler -e before running this test.");
#endif
            }
        }

        void Update()
        {
            if (gpuTimeTest && !gpuTraceInProgress)
            {
                AnalyzeGpuTimeTest();
            }
            else if ((insightsEnabled && !isFrozen) || gpuTraceInProgress || unifiedPerfRequested)
            {
                UnifiedPerformanceUpdate();
            }
            /*
            else
            {
                Debug.Log($"[QUICK_PERF_DEBUG] Update() - Neither gpuTimeTest nor insightsEnabled is true. gpuTimeTest={gpuTimeTest}, insightsEnabled={insightsEnabled}");
            }
            */
        }

        void UpdateActiveStateForGameObjects(int start, int end, bool state)
        {
            for (int i = start; i <= end; i++)
            {
                gameObjectsInFrustrum[i].SetActive(state);
            }
        }

        private void FlushRenderstageTracking()
        {
            if (flushBufferCount > 0)
            {
                flushBufferCount--;
            } else
            {
                float[] frameTimes;
                flushBufferCount = 30;
#if UNITY_EDITOR
                MockGetFrameTime(flushBufferCount, out frameTimes, Application.identifier);
#else
                RuntimeOptimizerPlugin.GetFrameTime(flushBufferCount, out frameTimes, Application.identifier);
#endif
            }
        }


        private void SendStateData()
        {
            var state = new StateData();
            state.timeScale = Time.timeScale;
            string json = JsonUtility.ToJson(state);
            var bytes = Encoding.ASCII.GetBytes(json);
            SendToClients(PerformanceDataUtils.RuntimeOptimizerStateId, bytes);
        }

        private void UnifiedPerformanceUpdate()
        {
            // PHASE 1: Capture Frame Timing & Update CPU Sliding Window
            // Skip when frozen — frozen state (Time.timeScale=0) reports near-zero
            // frame times which would corrupt the pre-freeze CPU sliding window data
            if (!isFrozen)
            {
                FrameTimingManager.CaptureFrameTimings();
                var frameTimingCount = FrameTimingManager.GetLatestTimings(
                    (uint)quickPerfFrameTiming.Length,
                    quickPerfFrameTiming
                );

                if (frameTimingCount == 0)
                    return;

                var now = DateTime.Now;
                float currentCpuFrameTime = (float)quickPerfFrameTiming[0].cpuMainThreadFrameTime;

                // Add to CPU sliding window
                frameTimes.Enqueue((now, currentCpuFrameTime));

                // Remove old CPU frame times (keep 2-second window)
                bool hasEnoughData = (now - frameTimes.Peek().timestamp).TotalSeconds > kInsightDetectionIntervalInSec;
                while (frameTimes.Count > 0 && hasEnoughData)
                {
                    var oldestFrameTime = frameTimes.Dequeue();
                    hasEnoughData = (now - oldestFrameTime.timestamp).TotalSeconds > kInsightDetectionIntervalInSec;
                }
            }

            var currentTime = DateTime.Now;

            // PHASE 2: Handle GPU Trace State Machine
            if (gpuTraceInProgress)
            {
                double traceElapsed = (currentTime - gpuTraceStartTime).TotalSeconds;
                if (traceElapsed >= kGpuTraceWaitTime)
                {
                    float avgGpuFrameTime = StopGpuTrace();
                    gpuTraceInProgress = false;

                    if (avgGpuFrameTime <= 0.0f)
                    {
                        Debug.LogWarning("GPU trace completed but returned invalid data");
                    }
                    SendUnifiedPerfData(currentTime, avgGpuFrameTime);
                }
                return;
            }

            // PHASE 3: Check Update Triggers
            bool shouldUpdate = false;

            // Trigger 1: On-demand request
            if (unifiedPerfRequested)
            {
                shouldUpdate = true;
                unifiedPerfRequested = false;
            }

            if (!shouldUpdate)
                return;

            // When frozen, use pre-freeze cached data instead of starting a new GPU trace
            // (GPU trace during frozen state captures idle GPU producing near-zero values)
            if (isFrozen && frameTimes.Count > 0 && lastValidGpuFrameTime > 0)
            {
                Debug.Log("Using pre-freeze cached performance data (runtime is frozen)");
                SendUnifiedPerfData(currentTime, lastValidGpuFrameTime);
                return;
            }

            // PHASE 4: Start GPU Trace (normal non-frozen path)
            bool traceStarted = StartGpuTrace();
            if (traceStarted)
            {
                gpuTraceInProgress = true;
                gpuTraceStartTime = currentTime;
                Debug.Log("GPU trace started, waiting 1 second before collecting data");
            }
            else
            {
                Debug.LogWarning("Failed to start GPU trace");
            }
        }

        private bool StartGpuTrace()
        {
            float[] frameTimes;
            bool success;

#if UNITY_EDITOR
            success = MockGetFrameTime(1, out frameTimes, Application.identifier, false);
#else
            success = RuntimeOptimizerPlugin.GetFrameTime(1, out frameTimes, Application.identifier, false);
#endif

            return success;
        }

        private float StopGpuTrace()
        {
            float[] frameTimes;
            bool success;

#if UNITY_EDITOR
            success = MockGetFrameTimeForQuickPerf(kQuickPerfGpuBufferSize, out frameTimes, Application.identifier, true);
#else
            //poll
            success = RuntimeOptimizerPlugin.GetFrameTime(kQuickPerfGpuBufferSize, out frameTimes, Application.identifier);

            // then stop
            float[] flushTimes;
            RuntimeOptimizerPlugin.GetFrameTime(1,out flushTimes, Application.identifier, true);
#endif

            if (!success || frameTimes == null || frameTimes.Length == 0)
            {
                return 0.0f;
            }

            int validCount = 0;
            double totalGpuFrameTime = 0.0;
            for(int i = 0; i < kQuickPerfGpuBufferSize; i++){
                if(frameTimes[i] > 0.0f){
                    totalGpuFrameTime += frameTimes[i];
                    validCount++;
                }
            }
            if(validCount == 0){
                return -1.0f;
            }
            return (float)(totalGpuFrameTime / (double)validCount);
        }

        private void SendUnifiedPerfData(DateTime now, float averageGPUTime)
        {
            double totalGpuFrameTime = 0.0;
            foreach (var frame in gpuFrameTimes)
            {
                totalGpuFrameTime += frame.frameTime;
            }
            float averageGpuFrameTime = averageGPUTime;
            if (averageGpuFrameTime > 0)
            {
                lastValidGpuFrameTime = averageGpuFrameTime;
            }

            int slowFrameCount = 0;
            int spikedFrameCount = 0;
            float maxFrameTime = 0.0f;
            double totalFrameTime = 0.0;

            if (frameTimes.Count > 0)
            {
                foreach (var frame in frameTimes)
                {
                    float frameTime = frame.frameTime;
                    totalFrameTime += frameTime;
                    slowFrameCount += frameTime > kInsightSlowFrameTime ? 1 : 0;
                    spikedFrameCount += frameTime > kInsightSpikedFrameTime ? 1 : 0;
                    maxFrameTime = Mathf.Max(maxFrameTime, frameTime);
                }
            }

            double averageFrameTime = frameTimes.Count > 0
                ? totalFrameTime / frameTimes.Count
                : -1.0f;

            float slowFrameRatio = frameTimes.Count > 0
                ? (float)slowFrameCount / frameTimes.Count
                : 0.0f;
            bool hasPerformanceIssue = (slowFrameRatio > 0.1f) || (maxFrameTime > kInsightSpikedFrameTime);

            var unifiedData = new QuickPerfData
            {
                frameRate = Mathf.RoundToInt(1.0f / Time.unscaledDeltaTime),
                cpuFrameTime = (float)averageFrameTime,
                gpuFrameTime = averageGpuFrameTime,
                drawCallsCount = drawCallsRecorder.LastValue,
                triangleCount = (int)triganleRecorder.LastValue,
                verticesCount = (int)verticesRecorder.LastValue,
                timestamp = now,
                isValid = true,
                slowFrameCount = slowFrameCount,
                spikedFrameCount = spikedFrameCount,
                maxFrameTime = maxFrameTime,
                hasPerformanceIssue = hasPerformanceIssue
            };

            string json = JsonUtility.ToJson(unifiedData);
            var bytes = Encoding.ASCII.GetBytes(json);
            SendToClients(PerformanceDataUtils.QuickPerfDataId, bytes);

            lastUnifiedUpdate = now;

            Debug.Log($"Unified Perf: CPU={averageFrameTime:F2}ms, GPU={averageGpuFrameTime:F2}ms, " +
                      $"FPS={unifiedData.frameRate}, SlowFrames={slowFrameCount}, " +
                      $"Issue={hasPerformanceIssue}, CPUWindow={frameTimes.Count}, GPUWindow={gpuFrameTimes.Count}");
        }

        private void InsightCaptureRequested()
        {
            // this is important to update the time otherwise we might trigger a second capture
            lastCaptureTime = DateTime.Now;
            // only capture if we decided to capture
            var snapshotInfo = InsightCaptureGameSnapShoot();
            string json = JsonUtility.ToJson(snapshotInfo);
            var bytes = Encoding.ASCII.GetBytes(json);
            SendToClients(PerformanceDataUtils.SceneDiagnosisCaptureId, bytes);
        }

        private SerializableDictionary InsightCaptureGameSnapShoot()
        {
            SerializableDictionary insightData = new SerializableDictionary();
            // get all loaded textures
            // must turn first to get objects rendering using this texture
            foreach (Texture text in Resources.FindObjectsOfTypeAll(typeof(Texture)) as Texture[])
            {
                TextureInsight textureInsight = new TextureInsight();
                textureInsight.name = text.name;
                textureInsight.width = text.width;
                textureInsight.height = text.height;
                textureInsight.anisoLevel = text.anisoLevel;
                textureInsight.filterMode = text.filterMode.ToString();
                textureInsight.runtimeSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(text);
                textureInsight.depth = 1;
                textureInsight.isReadable = text.isReadable;
                if (text is Texture2D)
                {
                    Texture2D text2D = text as Texture2D;
                    textureInsight.format = text2D.format.ToString();
                    textureInsight.mipCount = text2D.mipmapCount;
                }
                else if (text is Texture3D)
                {
                    Texture3D text3D = text as Texture3D;
                    textureInsight.format = text3D.format.ToString();
                    textureInsight.mipCount = text3D.mipmapCount;
                    textureInsight.depth = text3D.depth;
                }
                insightData.textures.Add(textureInsight);
            }

            gameObjectsInFrustrum = FindAllRenderableObjects(); // Find all objects with Renderer component

            uint totalVertCount = 0;
            uint totalMeshCount = 0;
            uint totalMatCount = 0;
            uint totalTransparentVertCount = 0;
            foreach (GameObject go in gameObjectsInFrustrum)
            {
                // Get all Renderer components in the GameObject and its children
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                bool hasTransparency = false;
                uint goMatCount = 0;
                uint goMeshCount = 0;
                uint goMeshVertexCount = 0;
                string goName = go.name.Replace(" ", "_");
                GameObjInsight goInsight = new GameObjInsight();
                foreach (Renderer renderer in renderers)
                {
                    // Get the materials from the Renderer
                    Material[] materials = renderer.sharedMaterials;
                    foreach (Material material in materials)
                    {
                        if (material != null)
                        {
                            goMatCount++;
                            if (material.HasProperty("_Mode"))
                            {
                                int mode = material.GetInt("_Mode");
                                //    0 => "Opaque",
                                //    1 => "Cutout",
                                //    2 => "Fade",
                                //    3 => "Transparent",
                                //    _ => "Unknown"
                                if (mode > 1 && mode < 4)
                                {
                                    hasTransparency = true;
                                }
                            }

                            if (material.GetTag("Queue", true, "NotFound").Contains("Transparen"))
                            {
                                hasTransparency = true;
                            }

                            goInsight.materialNames.Add(material.name);
                        }
                    }
                }

                MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    // Get the mesh from the MeshFilter
                    Mesh mesh = meshFilter.sharedMesh;
                    if (mesh != null)
                    {
                        MeshInsight mi = new MeshInsight();
                        goMeshCount++;
                        goMeshVertexCount += (uint)mesh.vertexCount;
                        mi.name = mesh.name;
                        mi.vertexCount = (uint)mesh.vertexCount;
                        goInsight.meshes.Add(mi);
                    }
                }

                SkinnedMeshRenderer[] skinnedRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedRenderers)
                {
                    // Get the mesh from the MeshFilter
                    Mesh mesh = skinnedMeshRenderer.sharedMesh;
                    if (mesh != null)
                    {
                        MeshInsight mi = new MeshInsight();
                        goMeshCount++;
                        goMeshVertexCount += (uint)mesh.vertexCount;
                        mi.name = mesh.name;
                        mi.vertexCount = (uint)mesh.vertexCount;
                        goInsight.meshes.Add(mi);
                    }
                }

                goInsight.name = goName;

                Transform parent = go.transform.parent;
                string path = "";
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.transform.parent;
                }
                goInsight.parentName = path;
                goInsight.vertCount = goMeshVertexCount;
                goInsight.meshCount = goMeshCount;
                goInsight.hasTransparency = hasTransparency;
                insightData.items.Add(goInsight);

                totalVertCount += goMeshVertexCount;
                totalMeshCount += goMeshCount;
                totalMatCount += goMatCount;
                totalTransparentVertCount += hasTransparency ? goMeshVertexCount : 0;
            }

            insightData.totalVertCount = (uint)verticesRecorder.LastValue;
            insightData.totalTriangleCount = (uint)triganleRecorder.LastValue;
            insightData.totalTransparentVertCount = totalTransparentVertCount;
            insightData.totalMeshCount = totalMeshCount;
            insightData.totalMatCount = totalMatCount;
            insightData.totalGMemAlloc = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();
            insightData.totalDrawcount = (uint)drawCallsRecorder.LastValue;


            return insightData;
        }


        private void AnalyzeGpuTimeTest()
        {
            if (currentFrameCount > 0)
            {
                currentFrameCount--;

            }
            else if (currentlyLooping)
            {
                if (runAsGroupMode)
                {
                    // Group mode: disable all GameObjects together
                    if (gameObjectIndex == gameObjectsInFrustrum.Count)
                    {
                        // Collect baseline
                        float[] frameTimes;
                        bool success;
#if UNITY_EDITOR
                        success = MockGetFrameTime(sampleBufferSize, out frameTimes, Application.identifier);
#else
                        success = RuntimeOptimizerPlugin.GetFrameTime(sampleBufferSize, out frameTimes, Application.identifier);
#endif
                        if (!success || frameTimes == null || frameTimes.Length == 0)
                        {
                            currentFrameCount = 1;
                            return;
                        }

                        baselineCoeffiecientVariation = coefficientOfVariation(frameTimes);
                        baselineMainThread = GetAverageGPUFrameTime(frameTimes);
                        recordMetadata();

                        // Disable all GameObjects at once
                        foreach (GameObject go in gameObjectsInFrustrum)
                        {
                            go.SetActive(false);
                        }

                        gameObjectIndex--;
                        currentFrameCount = sampleFrameCount;
                    }
                    else if (gameObjectIndex == gameObjectsInFrustrum.Count - 1)
                    {
                        // Collect group measurement
                        float[] frameTimes;
                        bool success;
#if UNITY_EDITOR
                        success = MockGetFrameTime(sampleBufferSize, out frameTimes, Application.identifier);
#else
                        success = RuntimeOptimizerPlugin.GetFrameTime(sampleBufferSize, out frameTimes, Application.identifier);
#endif
                        if (!success || frameTimes == null || frameTimes.Length == 0)
                        {
                            currentFrameCount = 1;
                            return;
                        }

                        float averageGPUFrameTime = GetAverageGPUFrameTime(frameTimes);
                        double groupCost = baselineMainThread - averageGPUFrameTime;

                        // Create group entry - use special marker name
                        GameObjectPerformanceMetaData groupData = new GameObjectPerformanceMetaData("GROUP_RESULT", "");
                        groupData.CpuMainThreadTime = groupCost;
                        gameObjectTimes.Add(groupData);

                        // Add all individual GameObjects as part of the group (with no individual cost)
                        foreach (GameObject go in gameObjectsInFrustrum)
                        {
                            Transform parent = go.transform.parent;
                            string parentPath = "";
                            while (parent != null)
                            {
                                parentPath = parent.name + "/" + parentPath;
                                parent = parent.transform.parent;
                            }
                            if (parentPath.EndsWith("/"))
                            {
                                parentPath = parentPath.Substring(0, parentPath.Length - 1);
                            }

                            GameObjectPerformanceMetaData goData = new GameObjectPerformanceMetaData(go.name, parentPath);
                            goData.CpuMainThreadTime = null; // No individual cost in group mode
                            gameObjectTimes.Add(goData);
                        }

                        // Re-enable all GameObjects
                        foreach (GameObject go in gameObjectsInFrustrum)
                        {
                            go.SetActive(true);
                        }

                        gameObjectIndex = -1; // Skip to end
                    }

                    if (gameObjectIndex < 0)
                    {
                        recordMetrics();
                        ClearSceneAnalysis(unfreeze: false);
                    }
                }
                else
                {
                    // Individual mode: test each GameObject separately (original logic)
                    // Reactivating the game object that was deactivated in the previous loop
                    if (gameObjectIndex < gameObjectsInFrustrum.Count && gameObjectIndex >= 0)
                    {
                        gameObjectsInFrustrum[gameObjectIndex].SetActive(true);
                    }
                    if (gameObjectIndex >= 0)
                    {
                        float[] frameTimes;
                        bool success;
#if UNITY_EDITOR
                        success = MockGetFrameTime(sampleBufferSize, out frameTimes, Application.identifier);
#else
                        success = RuntimeOptimizerPlugin.GetFrameTime(sampleBufferSize, out frameTimes, Application.identifier);
#endif
                        if (!success || frameTimes == null || frameTimes.Length == 0)
                        {
                            currentFrameCount = 1; // Wait one more frame
                            return;
                        }
                        float averageGPUFrameTime = GetAverageGPUFrameTime(frameTimes);
                        if (gameObjectIndex == gameObjectsInFrustrum.Count)
                        {
                            baselineCoeffiecientVariation = coefficientOfVariation(frameTimes);
                            baselineMainThread = averageGPUFrameTime;
                            recordMetadata();
                        }

                        else
                        {
                            // Build full hierarchy path for the game object
                            GameObject currentGO = gameObjectsInFrustrum[gameObjectIndex];
                            Transform parent = currentGO.transform.parent;
                            string parentPath = "";

                            // Build full hierarchy path (similar to bottleneck analysis)
                            while (parent != null)
                            {
                                parentPath = parent.name + "/" + parentPath;
                                parent = parent.transform.parent;
                            }

                            // Remove trailing slash if exists
                            if (parentPath.EndsWith("/"))
                            {
                                parentPath = parentPath.Substring(0, parentPath.Length - 1);
                            }

                            GameObjectPerformanceMetaData gameObjectPerformanceData = new GameObjectPerformanceMetaData(currentGO.name, parentPath);
                            gameObjectPerformanceData.CpuMainThreadTime = baselineMainThread - averageGPUFrameTime;
                            gameObjectTimes.Add(gameObjectPerformanceData);
                        }

                        gameObjectIndex--;
                        if (gameObjectIndex >= 0)
                        {
                            frameTimeList.Clear();
                            gameObjectsInFrustrum[gameObjectIndex].SetActive(false);
                            currentFrameCount = sampleFrameCount;
                        }
                    }
                    else
                    {
                        recordMetrics();
                        gameObjectsInFrustrum[0].SetActive(true);
                        ClearSceneAnalysis(unfreeze: false);
                    }
                }
            }

            lastCaptureTime = DateTime.Now;
        }
        private float GetAverageGPUFrameTime(float[] frameTimes)
        {
            float cummulativeFrameTimes = 0;

            if (frameTimes == null)
            {
                return 0;
            }

            for (int i = 0; i < frameTimes.Length; i++)
            {
                frameTimeList.Add(frameTimes[i]);
                cummulativeFrameTimes += frameTimes[i];
            }

            float result = frameTimes.Length > 0 ? cummulativeFrameTimes / frameTimes.Length : 0;
            return result;
        }

        private float coefficientOfVariation(float[] values)
        {
            if (values == null || values.Length <= 1)
            {
                return 0f; // Return 0 if input is invalid or has insufficient data
            }

            float mean = values.Average();
            if (mean == 0)
            {
                return 0f; // Return 0 to avoid division by zero
            }

            float sumOfSquaredDifferences = values.Sum(value => (float)Math.Pow(value - mean, 2));
            float variance = sumOfSquaredDifferences / (values.Length - 1); // Sample variance
            float standardDeviation = (float)Math.Sqrt(variance);
            float coefficientOfVariation = (standardDeviation / mean); // CV as a percentage
            return float.IsNaN(coefficientOfVariation) ? 0f : coefficientOfVariation * mean;
        }

        private void recordMetadata()
        {
            var serializedMetadata = $"MainThreadBaseline:{baselineMainThread},TotalObjects:{gameObjectsInFrustrum.Count},SamplingVariance:{baselineCoeffiecientVariation}";
            var bytes = Encoding.ASCII.GetBytes(serializedMetadata);
            if (server != null && server.HasConnectedClient())
            {
                SendToClients(PerformanceDataUtils.SceneDiagnosisMetadataId, bytes);
            }
        }
        private void recordMetrics()
        {
            List<GameObjectPerformanceMetaData> normalizedGameObjectTimes = FilterOutliers(gameObjectTimes);
            var serializedData = "success;" + string.Join(";", normalizedGameObjectTimes.Select(x =>
            {
                string goName = string.IsNullOrEmpty(x.GameObjectParentName)
                    ? x.GameObjectName
                    : $"{x.GameObjectParentName}/{x.GameObjectName}";
                
                // For group mode individual GOs (null CpuMainThreadTime), only send name
                if (!x.CpuMainThreadTime.HasValue)
                {
                    return $"GameObjectName:{goName}";
                }
                
                // For normal GOs or group result, send with timing data
                return $"GameObjectName:{goName},CpuRenderThreadTime:{x.CpuRenderThreadTime},CpuMainThreadTime:{x.CpuMainThreadTime}";
            }));

            Debug.Log(serializedData);
            var bytes = Encoding.ASCII.GetBytes(serializedData);
            if (server != null && server.HasConnectedClient())
            {
                SendToClients(PerformanceDataUtils.SceneDiagnosisDataId, bytes);
            }
        }
        private List<GameObjectPerformanceMetaData> FilterOutliers(List<GameObjectPerformanceMetaData> gameObjectPerfDataList, double threshold = 4.0)
        {
            return gameObjectPerfDataList;
        }
        private List<GameObject> FindAllRenderableObjects()
        {
            // Find all objects with Renderer component
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            List<GameObject> renderableObjects = new List<GameObject>();
            foreach (Renderer renderer in renderers)
            {
                // Check if the renderer is enabled and in frustum
                if (renderer.enabled && renderer.isVisible)
                {
                    renderableObjects.Add(renderer.gameObject);
                }
            }
            return renderableObjects;
        }

        private void FreezeDeltaTime(bool enabled)
        {
            if (enabled)
            {
                isFrozen = true;
                Time.timeScale = 0.0f;
                // Disable animators
                previouslyDisabledAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None).ToList();
                foreach (Animator animator in previouslyDisabledAnimators)
                {
                    animator.enabled = false;
                }

                // Pause audio sources
                previouslyPlayingAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None)
                    .Where(audio => audio.isPlaying)
                    .ToList();
                foreach (AudioSource audioSource in previouslyPlayingAudioSources)
                {
                    audioSource.Pause();
                }

                // Disable NavMeshAgents
                previouslyEnabledNavMeshAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None)
                    .Where(agent => agent.enabled)
                    .ToList();
                foreach (NavMeshAgent agent in previouslyEnabledNavMeshAgents)
                {
                    agent.enabled = false;
                }

                // Pause particle systems
                previouslyPlayingParticleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                    .Where(ps => ps.isPlaying && !ps.isPaused)
                    .ToList();
                foreach (ParticleSystem ps in previouslyPlayingParticleSystems)
                {
                    ps.Pause(true); // true means including children
                }

                // Pause timeline directors
                previouslyPlayingDirectors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None)
                    .Where(pd => pd.state == PlayState.Playing)
                    .ToList();
                foreach (PlayableDirector director in previouslyPlayingDirectors)
                {
                    director.Pause();
                }
            }
            else
            {
                isFrozen = false;
                Time.timeScale = 1.0f;
                // Re-enable animators
                foreach (Animator animator in previouslyDisabledAnimators)
                {
                    animator.enabled = true;
                }
                previouslyDisabledAnimators = new List<Animator>();

                // Resume audio sources
                foreach (AudioSource audioSource in previouslyPlayingAudioSources)
                {
                    audioSource.Play();
                }
                previouslyPlayingAudioSources = new List<AudioSource>();

                // Re-enable NavMeshAgents
                foreach (NavMeshAgent agent in previouslyEnabledNavMeshAgents)
                {
                    agent.enabled = true;
                }
                previouslyEnabledNavMeshAgents = new List<NavMeshAgent>();

                // Resume particle systems
                foreach (ParticleSystem ps in previouslyPlayingParticleSystems)
                {
                    ps.Play(true); // true means including children
                }
                previouslyPlayingParticleSystems = new List<ParticleSystem>();

                // Resume timeline directors
                foreach (PlayableDirector director in previouslyPlayingDirectors)
                {
                    director.Play();
                }
                previouslyPlayingDirectors = new List<PlayableDirector>();
            }
            SendStateData();
        }

        private void RequestUnifiedPerfData()
        {
            Debug.Log($"[QUICK_PERF_DEBUG] RequestUnifiedPerfData called. gpuTimeTest={gpuTimeTest}, insightsEnabled={insightsEnabled}");

            // Don't interfere with active analysis
            if (gpuTimeTest || !insightsEnabled)
            {
                SendQuickPerfError("Analysis in progress. Try again later.");
                return;
            }

            unifiedPerfRequested = true;
            Debug.Log("Unified performance data requested");
        }

        private void SendQuickPerfError(string errorMessage)
        {
            var errorData = new QuickPerfData
            {
                frameRate = 0,
                cpuFrameTime = 0,
                gpuFrameTime = 0,
                drawCallsCount = 0,
                triangleCount = 0,
                verticesCount = 0,
                timestamp = DateTime.Now,
                isValid = false
            };

            string json = JsonUtility.ToJson(errorData);
            var bytes = Encoding.ASCII.GetBytes(json);
            SendToClients(PerformanceDataUtils.QuickPerfDataId, bytes);

            Debug.LogWarning($"Quick Perf Error: {errorMessage}");
        }

#if UNITY_EDITOR
        // Mock implementation for Unity Editor testing
        private float[] GenerateMockQuickPerfData()
        {
            float[] mockData = new float[kQuickPerfGpuBufferSize];
            for (int i = 0; i < mockData.Length; i++)
            {
                // Generate realistic GPU times between 8-15ms for VR
                mockData[i] = 8.0f + (float)(mockRandom.NextDouble() * 7.0);
            }
            return mockData;
        }

        // Override MockGetFrameTime for quick perf testing
        private bool MockGetFrameTimeForQuickPerf(int bufferSize, out float[] frameTimes, string appId, bool clear = false)
        {
            if (bufferSize == kQuickPerfGpuBufferSize)
            {
                frameTimes = GenerateMockQuickPerfData();
            }
            else
            {
                frameTimes = GenerateMockFrameTimes(bufferSize);
            }
            return true;
        }
#endif
    }
}
#endif
