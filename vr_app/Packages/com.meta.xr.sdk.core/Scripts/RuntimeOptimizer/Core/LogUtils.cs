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
using UnityEngine;

namespace Meta.XR.RuntimeOptimizer.Core
{

    [Serializable]
    public class ErrorDataStr
    {
        public string data;
        ErrorDataStr(string src)
        {
            data = src;
        }

        static public string ToJsonStr(string src)
        {
            ErrorDataStr item = new ErrorDataStr(src);
            return JsonUtility.ToJson(item);
        }
    }

    public static class Util
    {
        public static bool logToConsole = false;

        public static void DebugLog(object message)
        {
            if (logToConsole)
            {
                UnityEngine.Debug.Log(message);
            }
        }

        public static void DebugLogError(object message)
        {
            UnityEngine.Debug.LogError(message);
            string messageStr = message.ToString();

            // Filter out ADB "no devices/emulators found" error
            if (messageStr.Contains("adb.exe: no devices/emulators found"))
            {
                return;
            }



            string jsonData = ErrorDataStr.ToJsonStr(messageStr);
            RuntimeOptimizerPlugin.SendEvent("error_log", jsonData);
        }
    }
}
