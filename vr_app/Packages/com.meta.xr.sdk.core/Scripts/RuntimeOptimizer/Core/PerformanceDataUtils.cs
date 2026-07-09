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

namespace Meta.XR.RuntimeOptimizer.Core
{
    public static class PerformanceDataUtils
    {
        public static readonly int RuntimeOptimizerConnected = 0;
        public static readonly int SceneDiagnosisDataId = 1;
        public static readonly int RuntimeOptimizerServiceRequestId = 2;
        public static readonly int SceneDiagnosisMetadataId = 3;
        public static readonly int SceneDiagnosisCaptureId = 4;
        public static readonly int RuntimeOptimizerPerfId = 5;
        public static readonly int RuntimeOptimizerStateId = 6;
        public static readonly int QuickPerfDataId = 7;           // NEW: For Quick Perf View data
        public static readonly int QuickPerfRequestId = 8;        // NEW: For requesting Quick Perf data
        public static readonly int FrustrumGameObjectsDataId = 9; // NEW: For What If GameObject selection
    }

    [Serializable]
    public class QuickPerfData
    {
        public int frameRate;           // Current frame rate
        public float cpuFrameTime;      // Total CPU frame time in milliseconds
        public float gpuFrameTime;      // GPU frame time in milliseconds
        public float drawCallsCount;   // drawcall count
        public int triangleCount;    // number of triangles
        public int verticesCount;    // number of vertices
        public DateTime timestamp;      // When the data was collected
        public bool isValid;            // Whether the data is valid

        // Performance issue detection
        public int slowFrameCount;      // Number of slow frames detected (> kInsightSlowFrameTime)
        public int spikedFrameCount;    // Number of spiked frames detected (> kInsightSpikedFrameTime)
        public float maxFrameTime;      // Maximum frame time in the window
        public bool hasPerformanceIssue;  // Whether performance issues were detected
    }

    [Serializable]
    public class FrustrumGameObjectsData
    {
        public List<string> gameObjectPaths = new List<string>();
        public int totalCount;
    }

    public struct GameObjectPerformanceMetaData
    {
        public string? GameObjectName { get; set; }
        public string? GameObjectParentName { get; set; }
        public double? CpuRenderThreadTime { get; set; }
        public double? CpuMainThreadTime { get; set; }
        public GameObjectPerformanceMetaData(string gameObjectName, string gameObjectParentName)
        {
            GameObjectParentName = gameObjectParentName;
            GameObjectName = gameObjectName;
            CpuRenderThreadTime = null;
            CpuMainThreadTime = null;
        }
        public override string ToString()
        {
            return $"Render Thread Time: {CpuRenderThreadTime?.ToString("F2")}ms, Main Thread CPU Time: {CpuMainThreadTime?.ToString("F2")}ms for GameObject: {GameObjectParentName}/{GameObjectName}";
        }
    }
}
