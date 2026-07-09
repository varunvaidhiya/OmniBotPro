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

using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using RO = Meta.XR.RuntimeOptimizer.Core;

namespace Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight
{
    static class PythonUtil
    {
        static public string RemoveInvalidChars(string filename)
        {
            string[] lines = filename.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int validPathIndex = 0;
#if UNITY_EDITOR_WIN
        if(lines.Length > 1) {
            for(int i = 0; i < lines.Length; i++) {
                if(!lines[i].Contains("WindowsApps")) {
                    validPathIndex = i;
                    break;
                }
            }
        }
        RO.Util.DebugLog("valid exe: " + lines[validPathIndex]);
#endif
            return string.Concat(lines[validPathIndex].Split(System.IO.Path.GetInvalidPathChars()));
        }

        static public string GetPythonPath()
        {

#if UNITY_EDITOR_OSX
        var commendToRun = "which";
#else
            var commendToRun = "where.exe";
#endif
            string result = "";
            string[] pythonCommands = { "python3", "python", "py" };

            // first try using keyword as it is to check for valid python path
            foreach (string pythonCmd in pythonCommands)
            {
                if (ValidatePythonPath(pythonCmd))
                {
                    return pythonCmd;
                }
            }

            foreach (string pythonCmd in pythonCommands)
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = commendToRun;
                start.Arguments = pythonCmd;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.CreateNoWindow = true;

                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = RemoveInvalidChars(reader.ReadToEnd().Trim());

                        if (File.Exists(result))
                        {
                            RO.Util.DebugLog($"{pythonCmd} exists at: {result}");

                            // Validate Python path with -V command on Windows

                            if (ValidatePythonPath(result))
                            {
                                return result;
                            }
                            else
                            {
                                RO.Util.DebugLog($"{result} failed validation test.");
                                continue;
                            }
                        }
                        else
                        {
                            RO.Util.DebugLog($"{pythonCmd} does not exist.");
                        }
                    }
                }
            }



            foreach (string pythonCmd in pythonCommands)
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = commendToRun;
                start.Arguments = pythonCmd;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.CreateNoWindow = true;

                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = RemoveInvalidChars(reader.ReadToEnd().Trim());

                        if (File.Exists(result))
                        {
                            RO.Util.DebugLog($"{pythonCmd} exists at: {result}");
                            return result;
                        }
                        else
                        {
                            RO.Util.DebugLog($"{pythonCmd} does not exist.");
                        }
                    }
                }
            }


            RO.Util.DebugLogError($"GetPythonPath Errors, cannot find python3, python, or py.");
            return result;
        }

        private static bool ValidatePythonPath(string pythonPath)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = pythonPath;
                start.Arguments = "-V";
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.CreateNoWindow = true;

                using (Process process = Process.Start(start))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                RO.Util.DebugLog($"Failed to validate Python path: {ex.Message}");
                return false;
            }
        }

        private static string _cachedPerformanceInsightDir;

        private static string GetPerformanceInsightDirectory()
        {
            if (_cachedPerformanceInsightDir != null)
                return _cachedPerformanceInsightDir;

            string[] guids = AssetDatabase.FindAssets("PythonUtil t:MonoScript");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith("PerformanceInsight/PythonUtil.cs"))
                {
                    string absPath = Path.GetFullPath(assetPath);
                    _cachedPerformanceInsightDir = Path.GetDirectoryName(absPath);
                    RO.Util.DebugLog($"Resolved PerformanceInsight directory: {_cachedPerformanceInsightDir}");
                    return _cachedPerformanceInsightDir;
                }
            }

            _cachedPerformanceInsightDir = "";
            return _cachedPerformanceInsightDir;
        }

        static public string GetScriptPath(string scriptName)
        {
            string[] found;

            try
            {
                found = System.IO.Directory.GetFiles(Application.dataPath, scriptName, SearchOption.AllDirectories);
                if (found.Length > 0)
                {
                    RO.Util.DebugLog($"Found {scriptName} in dataPath: {found[0]}");
                    return found[0];
                }
            }
            catch (Exception ex)
            {
                RO.Util.DebugLog($"dataPath search failed for {scriptName}: {ex.GetType().Name}: {ex.Message}");
            }

            string fullPath = Path.GetFullPath("Packages/com.meta.xr.sdk.core");
            try
            {
                found = System.IO.Directory.GetFiles(fullPath, scriptName, SearchOption.AllDirectories);
                if (found.Length > 0)
                {
                    RO.Util.DebugLog($"Found {scriptName} in package: {found[0]}");
                    return found[0];
                }
            }
            catch (Exception ex)
            {
                RO.Util.DebugLog($"Package search failed for {scriptName}: {ex.GetType().Name}: {ex.Message}");
            }

            string piDir = GetPerformanceInsightDirectory();
            if (!string.IsNullOrEmpty(piDir))
            {
                try
                {
                    found = System.IO.Directory.GetFiles(piDir, scriptName, SearchOption.AllDirectories);
                    if (found.Length > 0)
                    {
                        RO.Util.DebugLog($"Found {scriptName} relative to package: {found[0]}");
                        return found[0];
                    }
                }
                catch (Exception ex)
                {
                    RO.Util.DebugLog($"Package-relative search failed for {scriptName}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            RO.Util.DebugLogError("Not Found: " + scriptName);
            return "";
        }
    }
}
