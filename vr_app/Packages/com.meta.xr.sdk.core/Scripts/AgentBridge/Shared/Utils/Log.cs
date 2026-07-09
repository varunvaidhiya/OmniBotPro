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

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Thread-safe logging utilities for Agent Bridge.
    /// Info/Warning are gated by VerboseLogging. Error always logs.
    /// </summary>
    internal static class Log
    {
        private const string Prefix = "[AgentBridge]";

        public static volatile bool VerboseLogging;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Info(string message)
        {
            if (VerboseLogging) UnityEngine.Debug.Log($"{Prefix} {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Warning(string message)
        {
            if (VerboseLogging) UnityEngine.Debug.LogWarning($"{Prefix} {message}");
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"{Prefix} {message}");
        }
    }
}
