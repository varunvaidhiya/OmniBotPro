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
using Unity.Profiling;
using UnityEngine;
using UnityEditor.Android;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using RO = Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Editor;

namespace Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight
{
    static class CaptureTool
    {
        static private OVRADBTool adbTool = new OVRADBTool(AndroidExternalToolsSettings.sdkRootPath);
        static private Thread captureThread = null;
        static private List<string> connectedDevices = new List<string>();
        private const string outFile = "/data/misc/perfetto-traces/trace";
        static private System.Object lockObj = new System.Object();
        static private int metricProcessWorkInQueue = 0;
        static public int lastKnownPID = -1;

        static private string LowOverheadCaptureConfig = @"
buffers {
  size_kb: 4096
  fill_policy: DISCARD
}
}
buffers {
  size_kb: 43008
  fill_policy: DISCARD
}
buffers {
  size_kb: 262144
  fill_policy: DISCARD
}
data_sources {
  config {
    name: ""linux.process_stats""
    target_buffer: 0
    process_stats_config {
      scan_all_processes_on_start: false
    }
  }
}
data_sources {
  config {
    name: ""linux.ftrace""
    target_buffer: 1
    ftrace_config {
      ftrace_events: ""sched/sched_switch""
      ftrace_events: ""power/suspend_resume""
      ftrace_events: ""sched/sched_wakeup""
      ftrace_events: ""sched/sched_wakeup_new""
      ftrace_events: ""sched/sched_waking""
      ftrace_events: ""sched/sched_process_exit""
      ftrace_events: ""sched/sched_process_free""
      ftrace_events: ""task/task_newtask""
      ftrace_events: ""task/task_rename""
      ftrace_events: ""ftrace/print""
      buffer_size_kb: 8192
      drain_period_ms: 10
    }
  }
  producer_name_filter: ""{bundle_name}""
}
data_sources {
  config {
    name: ""track_event""
    target_buffer: 2
  }
}
trigger_config {
    trigger_mode: STOP_TRACING
    triggers {
        name: ""perf_optimizer_auto_stop""
        stop_delay_ms: 0
    }
    trigger_timeout_ms: 100000
  }
";

        static private string captureConfig = @"
buffers {
  size_kb: 4096
  fill_policy: DISCARD
}
buffers {
  size_kb: 43008
  fill_policy: DISCARD
}
buffers {
  size_kb: 262144
  fill_policy: DISCARD
}
data_sources {
  config {
    name: ""linux.process_stats""
    target_buffer: 0
    process_stats_config {
      scan_all_processes_on_start: false
    }
  }
}
data_sources {
  config {
    name: ""linux.ftrace""
    target_buffer: 1
    ftrace_config {
      ftrace_events: ""sched/sched_switch""
      ftrace_events: ""power/suspend_resume""
      ftrace_events: ""task/task_newtask""
      ftrace_events: ""task/task_rename""
      ftrace_events: ""ftrace/print""
      atrace_apps: ""{bundle_name}""
      buffer_size_kb: 8192
      drain_period_ms: 10
      compact_sched {
       enabled: true
      }
    }
  }
}
data_sources {
  config {
    name: ""track_event""
    target_buffer: 2
    track_event_config {
      disabled_categories: ""*""
      disabled_categories: ""xr_runtime_server""
      disabled_categories: ""xr_runtime_client""
      disabled_categories: ""gpu_profiling_service""
      disabled_categories: ""gpu_profiling_service_low_frequency""
      disabled_categories: ""gpu_renderstage""
      disabled_categories: ""gpu_surface_workload""
      enabled_categories: ""gpu_profiling_service""
      enabled_categories: ""gpu_renderstage""
      enabled_categories: ""vulkan_os_layer""
    }
  }
}
duration_ms: 2000
";

        static public string RemoveLastFolderFromPath(string path)
        {
            var lastSlashIndex = path.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {
                return path.Substring(0, lastSlashIndex);
            }
            return path;
        }

        static private string CreateFolderIn(string path, string folderName)
        {
#if UNITY_EDITOR_OSX
        var wholePath = path + "/" + folderName;
#else
            var wholePath = path + "\\" + folderName;
#endif
            if (!Directory.Exists(wholePath))
            {
                Directory.CreateDirectory(wholePath);
            }
            return wholePath;
        }

        static public uint EpochTime()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint currentEpochTime = (uint)(DateTime.UtcNow - epochStart).TotalSeconds;

            return currentEpochTime;
        }

        static public string GetAssetOutputFolderName()
        {
            return "Assets/#Meta";
        }

        static public string GetOutputDirectory()
        {
            string projectPath = RemoveLastFolderFromPath(Application.dataPath);
            string outdir = Path.Combine(projectPath, GetAssetOutputFolderName());
            if (!Directory.Exists(outdir))
            {
                Directory.CreateDirectory(outdir);
            }
            return outdir;
        }

        static public string GetMetricJsonOutputPath(string traceFilePath)
        {
            string originalFilePath = traceFilePath;
            // New file extension
            string newExtension = ".json";
            // Get the file name without the extension
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
            // Create the new file path with the new extension
            return Path.Combine(Path.GetDirectoryName(originalFilePath), $"{fileNameWithoutExtension}{newExtension}");
        }

        static private void StartServerCallback()
        {

        }

        static private void PerfettoCapture()
        {

        }

        static public bool isDetailedGPUServiceEnabled()
        {
            string outputString;
            string errorString;
            const string commandCapture = "shell getprop debug.egl.profiler";
            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandCapture }, PerfettoCapture, out outputString, out errorString);
            RO.Util.DebugLog("adb egl.profiler: " + outputString + " error: " + errorString);

            if (outputString.Contains("1"))
            {
                return true;
            }
            return false;
        }

        static public void ValidateProcess(string bundleName)
        {
            // Return early if IssuePerfettoCapture is currently running
            if (captureThread != null && captureThread.IsAlive)
            {
                return;
            }
            lastKnownPID = GetProcessPID(bundleName);
        }


        static public bool ValidateAdb()
        {
            // Return early if IssuePerfettoCapture is currently running
            if (captureThread != null && captureThread.IsAlive)
            {
                return true;
            }

            if (!IsADBReady())
            {
                return false;
            }

            connectedDevices = adbTool.GetDevices();

            if (connectedDevices.Count > 0)
            {
                if (connectedDevices.Count > 0)
                {
                    return false;
                }
                return true;
            }
            else
            {
                connectedDevices.Clear();
            }

            RO.Util.DebugLog("No device connected");
            return false;
        }

        static public bool ForwardPort(int port)
        {
            int exitCode = adbTool.ForwardPort(port, null);
            return exitCode == 0;
        }

        static public bool ReleasePort(int port)
        {
            int exitCode = adbTool.ReleasePort(port, null);
            return exitCode == 0;
        }

        static public int GetProcessPID(string bundleName)
        {
            string outputString = GetProcessInfo(bundleName);

            RO.Util.DebugLog($"[GetProcessPID] Searching for PID of '{bundleName}'");

            if (string.IsNullOrEmpty(outputString))
            {
                RO.Util.DebugLog($"[GetProcessPID] No output from GetProcessInfo - app not running");
                return -1;
            }

            RO.Util.DebugLog($"[GetProcessPID] Raw output: '{outputString}'");

            // Parse the output to extract PID
            string[] parts = outputString.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            RO.Util.DebugLog($"[GetProcessPID] Split into {parts.Length} parts");
            for (int i = 0; i < parts.Length && i < 5; i++)
            {
                RO.Util.DebugLog($"[GetProcessPID] Part[{i}]: '{parts[i]}'");
            }

            if (parts.Length >= 2)
            {
                int pid;
                if (int.TryParse(parts[1], out pid))
                {
                    RO.Util.DebugLog($"[GetProcessPID] Successfully extracted PID: {pid}");
                    return pid;
                }
                else
                {
                    RO.Util.DebugLog($"[GetProcessPID] Failed to parse PID from parts[1]: '{parts[1]}'");
                }
            }
            else
            {
                RO.Util.DebugLog($"[GetProcessPID] Insufficient parts in output (need at least 2, got {parts.Length})");
            }

            // Try to parse just the first part as PID (in case pidof returns just the PID)
            if (parts.Length >= 1)
            {
                int pid;
                if (int.TryParse(parts[0], out pid))
                {
                    RO.Util.DebugLog($"[GetProcessPID] Successfully extracted PID from first part: {pid}");
                    return pid;
                }
            }

            RO.Util.DebugLog($"[GetProcessPID] Failed to extract PID from output");
            return -1;
        }

        static public string GetProcessInfo(string bundleName)
        {
            string errorString = string.Empty;

            if (!IsADBReady() || connectedDevices.Count == 0)
            {
                RO.Util.DebugLog($"[GetProcessInfo] ADB not ready or no devices connected");
                return null;
            }

            string outputString;
            // Use pidof command which is more reliable for finding process by package name
            // Falls back to ps | grep if pidof is not available
            string command = $"shell \"pidof {bundleName} 2>/dev/null || ps -A | grep {bundleName}\"";

            RO.Util.DebugLog($"[GetProcessInfo] Executing command: {command}");

            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            // Debug logging to help diagnose detection issues
            RO.Util.DebugLog($"[GetProcessInfo] Bundle: '{bundleName}', Output: '{outputString}', Error: '{errorString}'");

            return !string.IsNullOrEmpty(outputString) && string.IsNullOrEmpty(errorString) ? outputString : null;
        }

        static public int ConnectedDeviceCount()
        {
            return connectedDevices.Count;
        }

        static public string[] ConnectedDevices()
        {
            return connectedDevices.ToArray();
        }

        static public bool IsADBReady()
        {
            if (adbTool == null)
            {
                adbTool = new OVRADBTool(AndroidExternalToolsSettings.sdkRootPath);
            }
            int code = adbTool.StartServer(StartServerCallback);
            if (code != 0)
            {
                RO.Util.DebugLog("adb StartServer failed: " + code.ToString());
                RO.Util.DebugLog("Please check your Android sdk path in Preferences/External Tools");
                adbTool = null;
                return false;
            }
            return true;
        }

        static public bool KillApp(string packageName)
        {
            string outputString;
            string errorString;
            string command = "shell am force-stop " + packageName;

            int result = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            if (result != 0 || !string.IsNullOrEmpty(errorString))
            {
                UnityEngine.Debug.LogError($"Failed to kill app: {packageName}. Error: {errorString}");
                return false;
            }

            RO.Util.DebugLog($"Successfully killed app: {packageName}");
            return true;
        }

        static public bool LaunchApp(string packageActivityPath)
        {
            if (!IsADBReady() || connectedDevices.Count == 0)
            {
                UnityEngine.Debug.LogError("ADB not ready or no devices connected");
                return false;
            }

            string outputString;
            string errorString;
            string command = "shell am start -n " + packageActivityPath;

            int result = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            if (result != 0 || !string.IsNullOrEmpty(errorString))
            {
                UnityEngine.Debug.LogError($"Failed to launch app: {packageActivityPath}. Error: {errorString}");
                return false;
            }

            RO.Util.DebugLog($"Successfully launched app: {packageActivityPath}");
            return true;
        }


        static public string GetExecutablePath(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName) || connectedDevices.Count == 0)
            {
                return "";
            }

            string outputString;
            string errorString;
            string command = $"shell cmd package resolve-activity --brief \"{bundleName}\"";

            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            if (!string.IsNullOrEmpty(outputString))
            {
                string[] lines = outputString.Trim().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    string activityPath = lines[1].Trim();
                    return activityPath;
                }
            }

            return "";
        }

        static public void Init(string packageName = "", bool shimLayer = false)
        {
            if (!IsADBReady())
            {
                return;
            }

            string outputString;
            string errorString;
            if (connectedDevices.Count > 0)
            {
                int code = adbTool.RunCommand(new string[] { "", "", $"shell ovrgpuprofiler -e" }, PerfettoCapture, out outputString, out errorString);
                RO.Util.DebugLog("adb ovrgpuprofiler : " + outputString);
                RO.Util.DebugLog("adb ovrgpuprofiler : " + errorString);
                if (code != 0)
                {
                    RO.Util.DebugLogError("adb ovrgpuprofiler failed: " + code.ToString());
                }

                if (shimLayer == true)
                {
                    adbTool.RunCommand(new string[] { "-s", connectedDevices[0], "shell setprop debug.oculus.forceVkShimLayer 1" }, PerfettoCapture, out outputString, out errorString);
                    RO.Util.DebugLog("forceVkShimLayer: " + outputString);
                    adbTool.RunCommand(new string[] { "-s", connectedDevices[0], "shell setprop debug.oculus.vkshim.forceVkLoadingFunctionShimming 1" }, PerfettoCapture, out outputString, out errorString);
                    RO.Util.DebugLog("forceVkLoadingFunctionShimming: " + outputString);
                }
                else
                {
                    adbTool.RunCommand(new string[] { "-s", connectedDevices[0], "shell setprop debug.oculus.forceVkShimLayer 0" }, PerfettoCapture, out outputString, out errorString);
                    // RO.Util.DebugLog("forceVkShimLayer: " + outputString);
                }
            }
            else
            {
                RO.Util.DebugLog("adb no adb device found");
            }
        }

        static public void StopPerfettoCapture()
        {
            try
            {
                List<string> devices = adbTool.GetDevices();
                if (devices.Count > 0)
                {
                    int commandResult = adbTool.RunCommand(new string[] { "-s", devices[0], "shell trigger_perfetto perf_optimizer_auto_stop" }, PerfettoCapture, out string outputString, out string errorString);
                    RO.Util.DebugLog("StopPerfettoCapture: " + outputString + " error: " + errorString.ToString());
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Exception caught while stopping perfetto capture. Details  \r\n" + ex.Message.ToString() + "\r\n" + ex.StackTrace);
            }
        }

        static public void IssuePerfettoCapture(bool screenshot, string packageName, bool lowOverhead = false)
        {
            RO.Util.DebugLog("Issue perfetto capture");

            if (adbTool == null)
            {
                UnityEngine.Debug.LogError("abd tool is not ready, did you call Init?");
            }

            connectedDevices = adbTool.GetDevices();

            if (connectedDevices.Count > 0)
            {
                PerfettoCaptureCommand(lowOverhead, packageName);
            }
            else
            {
                UnityEngine.Debug.LogError("adb no adb device found");
            }
        }

        private static void ClearPerfettoCapture()
        {
            string outputString;
            string errorString;
            // Directory exists, remove the file
            const string commandRemove = "shell rm " + outFile;
            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandRemove }, PerfettoCapture, out outputString, out errorString);
            RO.Util.DebugLog("adb Perfetto device: " + outputString + " error: " + errorString.ToString());
            Thread.Sleep(2000);
        }

        private static void PerfettoCaptureCommand(bool lowOverhead, string packageName)
        {
            string outputString;
            string errorString;

            ClearPerfettoCapture();
            const string commandCapture = "shell perfetto -c - --txt -o " + outFile;
            string finalConfig = captureConfig.Replace("{bundle_name}", packageName);
            string lowOverheadConfig = LowOverheadCaptureConfig.Replace("{bundle_name}", packageName);
            RO.Util.DebugLog("core sdk version 70+");

            int code2 = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandCapture }, PerfettoCapture, out outputString, out errorString, lowOverhead ? lowOverheadConfig : finalConfig);
            RO.Util.DebugLog("adb Perfetto device: " + code2.ToString());
            RO.Util.DebugLog("adb Perfetto device: " + outputString + " error: " + errorString);

        }

        public static bool ASWDetectionCommand()
        {
            string outputString = "";
            string errorString;
            const string commandCapture = "shell logcat -d | grep ASW";
            connectedDevices = adbTool.GetDevices();


            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandCapture }, PerfettoCapture, out outputString, out errorString);
            RO.Util.DebugLog("adb ASW: " + outputString + " error: " + errorString);

            if (outputString.Contains("ASW="))
            {
                return true;
            }
            return false;
        }

        public static bool ExecuteADBCommand(string[] command, out string output)
        {
            string error;
            output = string.Empty;

            // Log the full command being executed
            string commandStr = string.Join(" ", command);
            RO.Util.DebugLog($"[ADB] Executing command: adb {commandStr}");

            if (!adbTool.isReady)
            {
                error = "OVRADBTool not ready";
                RO.Util.DebugLogError($"[ADB] Error executing adb command: {command[0]}");
                RO.Util.DebugLogError($"[ADB] Error: {error}");
                return false;
            }

            // Log individual command parameters for debugging
            for (int i = 0; i < command.Length; i++)
            {
                RO.Util.DebugLog($"[ADB] Command[{i}]: '{command[i]}'");
            }

            int exitCode = adbTool.RunCommand(command, null, out output, out error);
            bool executed = exitCode == 0;

            // Log the result
            RO.Util.DebugLog($"[ADB] Command exit code: {exitCode}");
            RO.Util.DebugLog($"[ADB] Command executed successfully: {executed}");

            if (!string.IsNullOrEmpty(output))
            {
                RO.Util.DebugLog($"[ADB] Command output: {output}");
            }

            if (!string.IsNullOrEmpty(error))
            {
                RO.Util.DebugLogError($"[ADB] Command error: {error}");
            }

            if (!executed)
            {
                output = error;
            }

            return executed;
        }

        public static void PullPerfettoCapture(string captureName)
        {
            RO.Util.DebugLog("Pulling Perfetto Capture");
            string outputString;
            string errorString;
            string outPath = GetOutputDirectory();
            string outFileName = captureName + ".ptrace";
            string finalPath = Path.Combine(outPath, outFileName);

            string tempFile = Path.Combine(Path.GetTempPath(), outFileName);
            string commandPull = string.Format("pull {0} {1}", outFile, tempFile);

            int exitCode = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandPull }, PerfettoCapture, out outputString, out errorString);

            if (exitCode == 0 && File.Exists(tempFile))
            {
                try
                {
                    File.Copy(tempFile, finalPath, true);
                    File.Delete(tempFile);
                    RO.Util.DebugLog($"Perfetto trace pulled to: {finalPath}");
                }
                catch (System.Exception ex)
                {
                    RO.Util.DebugLogError($"Failed to copy trace from temp to project: {ex.Message}");
                }
            }
            else
            {
                RO.Util.DebugLogError($"ADB pull failed (exit code {exitCode}): {errorString}");
            }
        }

        static private void IssueScreenShotCapture(string outName)
        {
            if (adbTool == null)
            {
                UnityEngine.Debug.LogError("adb not found");
                return;
            }
            try
            {
                List<string> devices = adbTool.GetDevices();
                if (devices == null || devices.Count == 0)
                {
                    UnityEngine.Debug.LogError("adb no adb device found");
                    return;
                }
                const string outFile = "/sdcard/insight.png";
                string outputString;
                string errorString;
                string outPath = GetOutputDirectory();
                // Capture the screen on the device.
                string commandCapture = string.Format("-s {0} shell screencap -p {1}", devices[0], outFile);
                int code = adbTool.RunCommand(new string[] { "-s", devices[0], commandCapture }, PerfettoCapture, out outputString, out errorString);
                if (code == 0)
                {
                    string outFileName = outName + ".png";
                    string finalOutFile = string.Format("\"{0}\"", Path.Combine(outPath, outFileName));
                    string commandPull = string.Format("pull {0} {1}", outFile, finalOutFile);
                    RO.Util.DebugLog("adb screencap pull: " + commandPull);
                    adbTool.RunCommand(new string[] { "-s", devices[0], commandPull }, PerfettoCapture, out outputString, out errorString);
                    RO.Util.DebugLog("adb screencap pull: " + (outputString ?? "null"));
                    RO.Util.DebugLog("adb screencap pull: " + (errorString ?? "null"));
                }
                else
                {
                    UnityEngine.Debug.LogError("adb screencap device: " + code.ToString());
                    UnityEngine.Debug.LogError("adb screencap device: " + (outputString ?? "null"));
                    UnityEngine.Debug.LogError("adb screencap device: " + (errorString ?? "null"));
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Exception caught while capturing screen on device. Details  \r\n" + ex.Message.ToString() + "\r\n" + ex.StackTrace);
            }
        }

        static public int GetAndroidOSVersion()
        {
            string output;
            if (CaptureTool.ExecuteADBCommand(new[] { "shell getprop ro.build.version.release" }, out output))
            {
                if (int.TryParse(output.Trim(), out int osVersion))
                {
                    return osVersion;
                }
                else
                {
                    RO.Util.DebugLog("Getting Android OS Version failed with error: " + output);
                }
            }
            return 0;
        }

        static public int GetHeadsetOSVersion()
        {
            string output;
            if (CaptureTool.ExecuteADBCommand(new[] { "shell getprop ro.vros.build.version" }, out output))
            {
                if (int.TryParse(output.Trim(), out int osVersion))
                {
                    return osVersion;
                }
                else
                {
                    RO.Util.DebugLog("Getting Quest OS Version failed with error: " + output);
                }
            }
            return 0;
        }

        static public int CompareStringsAsHex(string filePath1, string filePath2)
        {
            string fileName1 = Path.GetFileNameWithoutExtension(filePath1);
            string fileName2 = Path.GetFileNameWithoutExtension(filePath2);
            bool valid1 = ulong.TryParse(fileName1, System.Globalization.NumberStyles.HexNumber, null, out ulong l1);
            bool valid2 = ulong.TryParse(fileName2, System.Globalization.NumberStyles.HexNumber, null, out ulong l2);
            if (!valid1 && !valid2) return string.Compare(fileName1, fileName2, StringComparison.Ordinal);
            if (!valid1) return 1;
            if (!valid2) return -1;
            return (int)(l2 - l1);
        }

        static public string[] GetCapturedList()
        {
            var outPath = GetOutputDirectory();
            // RO.Util.DebugLog("GetCapturedList: " + outPath);
            string[] dirs = Directory.GetFiles(outPath, "*.ptrace");
            Array.Sort(dirs, CompareStringsAsHex);
            return dirs;
        }

        static private void ProcessCaptureToJsonObjFunc(object filePathObj)
        {
            string filePath = (string)filePathObj;
            RO.Util.DebugLog("Async Loading: " + filePath);
            string finalInFile = string.Format("\"{0}\"", filePath);
            MetricAPI.GetCaptureMetric(finalInFile, GetMetricJsonOutputPath(filePath));
            lock (lockObj)
            {
                metricProcessWorkInQueue--;
            }
        }

        static public int CaptureToJsonObjAsyncCount()
        {
            return metricProcessWorkInQueue;
        }

        static public int CaptureToJsonAsyncTimeout()
        {
            lock (lockObj)
            {
                metricProcessWorkInQueue--;
            }
            return metricProcessWorkInQueue;
        }

        static public void ProcessCaptureToJsonObjAsync(string filePath)
        {
            lock (lockObj)
            {
                metricProcessWorkInQueue++;
                ThreadPool.QueueUserWorkItem(ProcessCaptureToJsonObjFunc, filePath);
            }
            RO.Util.DebugLog("Async ProcessCaptureToJsonObjAsync: " + filePath);
        }

        static public void IssuePerfettoCaptureAsync(bool screenshot, string packageName, string captureName, bool lowOverhead = false, Action onFirstExecute = null, Action onFinishedScreenshot = null)
        {
            if (captureThread != null && captureThread.IsAlive)
            {
                captureThread.Abort();
                captureThread.Join();
            }

            captureThread = new Thread(() =>
            {
                if (onFirstExecute != null)
                {
                    onFirstExecute();
                }
                IssuePerfettoCapture(screenshot, packageName, lowOverhead);
                PullPerfettoCapture(captureName);
                if (screenshot)
                {
                    IssueScreenShotCapture(captureName);
                }
                if (onFinishedScreenshot != null)
                {
                    onFinishedScreenshot();
                }
            });
            captureThread.Start();
        }

        /// <summary>
        /// Captures only a screenshot without any Perfetto trace capture.
        /// This is useful for freeze frame scenarios where you only need a visual thumbnail.
        /// </summary>
        /// <returns>True if capture was started, false if a capture is already in progress</returns>
        static public void IssueScreenShotCaptureAsync(string captureName, Action onFirstExecute = null, Action onFinished = null)
        {
            if (captureThread != null && captureThread.IsAlive)
            {
                captureThread.Abort();
                captureThread.Join();
            }

            captureThread = new Thread(() =>
            {
                if (onFirstExecute != null)
                {
                    onFirstExecute();
                }

                IssueScreenShotCapture(captureName);

                if (onFinished != null)
                {
                    onFinished();
                }
            });
            captureThread.Start();
        }
        static public bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If the file is not locked, the code above will reach here
                    return false; // File is not locked
                }
            }
            catch (IOException)
            {
                // Check if the exception is related to a file lock
                return true; // File is locked
            }
        }
    }
}
