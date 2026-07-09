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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using RO = Meta.XR.RuntimeOptimizer.Core;

#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || (UNITY_ANDROID && !UNITY_EDITOR))
#define OVRPLUGIN_UNSUPPORTED_PLATFORM
#endif

#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || (UNITY_ANDROID && !UNITY_EDITOR))
#define OVRPLUGIN_QPL_UNSUPPORTED_PLATFORM
#endif

#if OVRROPLUGIN_TESTING && UNITY_EDITOR && OVRPLUGIN_UNSUPPORTED_PLATFORM
#define OVRPLUGIN_EDITOR_MOCK_ENABLED
#undef OVRPLUGIN_UNSUPPORTED_PLATFORM
#endif

namespace Meta.XR.RuntimeOptimizer.Core
{
    public static partial class RuntimeOptimizerPlugin
    {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM && OVRPLUGIN_QPL_UNSUPPORTED_PLATFORM
        public const bool isSupportedPlatform = false;
#else
    public const bool isSupportedPlatform = true;
#endif

        public static readonly System.Version minVersion = new System.Version(1, 97, 0);

        private static string sessionId = "";

#if !(OVRPLUGIN_UNSUPPORTED_PLATFORM && OVRPLUGIN_QPL_UNSUPPORTED_PLATFORM)
    private static System.Version _version;
#endif

        public static string Initialize(string sessionid, string scriptGUID)
        {

            string newSessionid = "";
            try
            {
                RO.Util.DebugLog("RuntimeOptimizer_Plugin pguid: " + scriptGUID);
                IntPtr ptr = OVRROP_0_0_1.ovrro_Init("Unity", minVersion.ToString(), sessionid, scriptGUID);
                newSessionid = Marshal.PtrToStringAnsi(ptr);
                sessionId = newSessionid;
                RO.Util.DebugLog("RuntimeOptimizer_Plugin initialized: " + newSessionid);
            }
            catch (DllNotFoundException)
            {
                RO.Util.DebugLogError("RuntimeOptimizer_Plugin not found");
            }
            return newSessionid;
        }

        public static bool Shutdown()
        {
            try
            {
                OVRROP_0_0_1.ovrro_Shutdown("Unity", minVersion.ToString(), sessionId);
                RO.Util.DebugLog("RuntimeOptimizer_Plugin Shutdown");
            }
            catch (DllNotFoundException)
            {
                RO.Util.DebugLogError("RuntimeOptimizer_Plugin not found");
            }
            return true;
        }

        public static bool SendEvent(string eventName, string jsonData)
        {

            try
            {
                OVRROP_0_0_1.ovrro_SendEvent(eventName, jsonData, sessionId);
            }
            catch (Exception e)
            {
                RO.Util.DebugLog("SendEvent failed: " + e.Message);
                return false;
            }
            return true;
        }

#if UNITY_ANDROID
    public static bool InitializeGpuProfiling()
    {
        bool result = OVRROP_0_0_1.ovrro_InitializeGpuProfiling();
        return result;
    }

    public static bool StartRealtimeMetrics(uint[] metricIds, int intervalMs)
    {
        bool result = OVRROP_0_0_1.ovrro_StartRealtimeMetrics(metricIds, metricIds.Length, intervalMs);
        UnityEngine.Debug.Log("RuntimeOptimizer_Plugin realtime metrics started: " + result);
        return result;
    }

    public static bool GetMetrics(uint[] metricId, out float[] value, int metricCount)
    {
        // Pre-allocate the array for the values
        float[] buffer = new float[metricCount];

        // Call the native function with the pre-allocated buffer
        bool result = OVRROP_0_0_1.ovrro_GetMetrics(metricId, buffer, metricCount);

        // Assign the buffer to the out parameter
        value = buffer;

        return result;
    }

    public static bool GetFrameTime(int frameCount, out float[] frameTimes, string processName = "", bool stop = false)
    {
        // Allocate buffer for frame times
        float[] buffer = new float[frameCount];

        bool result = OVRROP_0_0_1.ovrro_GetFrameTime(frameCount, buffer, processName, stop);

        if (result)
        {
            frameTimes = buffer;
        }
        else
        {
            frameTimes = new float[0];
        }

        return result;
    }
#endif

#if (UNITY_ANDROID && !UNITY_EDITOR)
    private const string pluginName = "LibRuntimeOptimizer";
#else
        private const string pluginName = "RuntimeOptimizer_Plugin_dll";
#endif
        private static System.Version _versionZero = new System.Version(0, 0, 0);

#if OVRROPLUGIN_TESTING
    private static class OVRROP_0_0_1_PROD
#else
        private static class OVRROP_0_0_1
#endif // OVRPlugin_Testing
        {
            public static readonly System.Version version = new System.Version(0, 0, 1);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr ovrro_Init(string engineName, string engineVersion, string sessionid, string pguid);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ovrro_Shutdown(string engineName, string engineVersion, string sessionid);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ovrro_SendEvent(string eventName, string jsonData, string sessionid);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int ovrro_test(string engineName, string engineVersion, string sessionid);
#if UNITY_ANDROID
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_InitializeGpuProfiling();

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_StartRealtimeMetrics([In] uint[] metricIds, int metricCount, int intervalMs);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_GetMetrics([In] uint[] metricId, [Out] float[] value, int metricCount);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_GetFrameTime(int frameTimesCapacity, [Out] float[] frameTimes, string processName, bool stop = false);
#endif
        }
    }
}
