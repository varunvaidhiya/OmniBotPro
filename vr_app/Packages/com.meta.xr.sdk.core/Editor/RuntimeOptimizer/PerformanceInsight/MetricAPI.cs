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
    static class MetricAPI
    {
        static private string pythonPath = PythonUtil.GetPythonPath();

        public const int processTimeOutMS = 120000;
        static public void RunPythonUtil()
        {
            string path = PythonUtil.GetScriptPath("PythonUtil.py");
            if (path.Length > 0)
            {
                ProcessStartInfo start = new ProcessStartInfo(pythonPath);
                start.Arguments = string.Format("{0}", path);
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                Process process = Process.Start(start);
                process.WaitForExit();
            }
        }

        static public void GetCaptureMetric(string traceFilePath, string saveToFilePath = null)
        {

#if UNITY_EDITOR_WIN
        string shellBinPath = PythonUtil.GetScriptPath("trace_processor_shell.exe");
#elif UNITY_EDITOR_OSX
            string shellBinPath = PythonUtil.GetScriptPath("trace_processor_shell");
#else
            string shellBinPath = "";
#endif

#if UNITY_EDITOR_WIN
            string pathEXE = PythonUtil.GetScriptPath("MetricAPI.exe");
#elif UNITY_EDITOR_OSX
            string pathEXE = PythonUtil.GetScriptPath("MetricAPI");
#else
            string pathEXE = "";
#endif
            if (pathEXE.Length > 0)
            {
#if UNITY_EDITOR_WIN
                pathEXE = pathEXE.Replace('/', '\\');
                traceFilePath = traceFilePath.Replace('/', '\\');
#endif

                ProcessStartInfo start = new ProcessStartInfo(pathEXE);

                start.Arguments = string.Format("{0} \"{1}\" \"{2}\"", traceFilePath, shellBinPath, saveToFilePath);
                start.UseShellExecute = true;
                start.CreateNoWindow = true;
                start.WindowStyle = ProcessWindowStyle.Hidden;
                RO.Util.DebugLog("MetricAPIEXE: " + start.Arguments);
                using (Process process = Process.Start(start))
                {
                    try
                    {
                        DateTime lastProcessed = DateTime.Now;
                        bool exited = process.WaitForExit(processTimeOutMS);
                        double timeSinceProcessed = (DateTime.Now - lastProcessed).TotalSeconds;

                        if (!exited)
                        {
                            RO.Util.DebugLogError(string.Format("MetricAPI: {0} timed out after {1:F1}s — killing process", Path.GetFileName(saveToFilePath), timeSinceProcessed));
                            try { process.Kill(); } catch { }
                            return;
                        }

                        RO.Util.DebugLog(string.Format("MetricAPI: {0} {1} / ext: {2}", Path.GetFileName(saveToFilePath), timeSinceProcessed, process.ExitCode));

                        string errorPath = saveToFilePath.Replace(".json", "_error.log");
                        bool hasError = File.Exists(errorPath);
                        if (process.ExitCode != 0 || hasError)
                        {
                            string error = File.Exists(errorPath) ? File.ReadAllText(errorPath) : "Unknown error";
                            RO.Util.DebugLogError(string.Format("MetricAPI: {0} \n execode: {1} \n {2}", Path.GetFileName(saveToFilePath), process.ExitCode, error));
                            File.Delete(errorPath);
                        }
                    }
                    catch (Exception e)
                    {
                        RO.Util.DebugLogError("MetricAPI: " + e.Message);
                    }
                }

                return;
            }


            string path = PythonUtil.GetScriptPath("MetricAPI.py");
            if (path.Length > 0)
            {
#if UNITY_EDITOR_WIN
            path = path.Replace('/', '\\');
            traceFilePath = traceFilePath.Replace('/', '\\');
#endif
                ProcessStartInfo start = new ProcessStartInfo(pythonPath);
                start.Arguments = string.Format("\"{0}\" {1} \"{2}\" \"{3}\"", path, traceFilePath, shellBinPath, saveToFilePath);
                start.UseShellExecute = true;
                start.CreateNoWindow = true;
                start.WindowStyle = ProcessWindowStyle.Hidden;
                RO.Util.DebugLog("MetricAPI: " + start.Arguments);
                using (Process process = Process.Start(start))
                {
                    try
                    {
                        DateTime lastProcessed = DateTime.Now;
                        bool exited = process.WaitForExit(processTimeOutMS);
                        double timeSinceProcessed = (DateTime.Now - lastProcessed).TotalSeconds;

                        if (!exited)
                        {
                            RO.Util.DebugLogError(string.Format("MetricAPI (Python): {0} timed out after {1:F1}s", Path.GetFileName(saveToFilePath), timeSinceProcessed));
                            try { process.Kill(); } catch { }
                            return;
                        }

                        RO.Util.DebugLog(string.Format("MetricAPI: {0} {1} / ext: {2}", Path.GetFileName(saveToFilePath), timeSinceProcessed, process.ExitCode));

                        string errorPath = saveToFilePath.Replace(".json", "_error.log");
                        bool hasError = File.Exists(errorPath);
                        if (process.ExitCode != 0 || hasError)
                        {
                            string error = File.Exists(errorPath) ? File.ReadAllText(errorPath) : "Unknown error";
                            RO.Util.DebugLogError(string.Format("MetricAPI: {0} \n execode: {1} \n {2}", Path.GetFileName(saveToFilePath), process.ExitCode, error));
                            File.Delete(errorPath);
                        }
                    }
                    catch (Exception e)
                    {
                        RO.Util.DebugLogError("MetricAPI: " + e.Message);
                    }
                }
            }
        }

        static public void OpenPerfettoTrace(string traceFilePath)
        {
            string script_path = PythonUtil.GetScriptPath("open_trace_in_ui.py");
            string arguments = string.Format("\"{0}\" \"{1}\"", script_path, traceFilePath);
            RO.Util.DebugLog("OpenPerfettoTrace: " + arguments);
            if (script_path.Length > 0)
            {
                ProcessStartInfo start = new ProcessStartInfo(pythonPath);
                start.Arguments = arguments;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = false;
                Process process = Process.Start(start);
            }
        }
    }
}
