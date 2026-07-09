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
using System.IO;
using UnityEngine;

namespace Meta.XR.RuntimeOptimizer.Core
{
    /// <summary>
    /// Utility class for managing port discovery between Unity runtime and Editor.
    /// Handles writing and reading port information to a shared location.
    /// </summary>
    public static class RuntimeOptimizerPortDiscovery
    {
        private const string PORT_FILE_NAME = "runtime_optimizer_port.txt";

        /// <summary>
        /// Gets the path where the port file should be stored.
        /// Uses Application.persistentDataPath for all platforms to ensure write permissions.
        /// </summary>
        public static string GetPortFilePath()
        {
            string directory = Path.Combine(Application.persistentDataPath, "RuntimeOptimizer");
            return Path.Combine(directory, PORT_FILE_NAME);
        }

        /// <summary>
        /// Writes the active port number to the port discovery file.
        /// </summary>
        /// <param name="port">The port number to write</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool WritePortToFile(int port)
        {
            try
            {
                string filePath = GetPortFilePath();
                string directory = Path.GetDirectoryName(filePath);

                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write port number and timestamp
                string content = $"{port}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                File.WriteAllText(filePath, content);

                Debug.Log($"[RuntimeOptimizer] Port {port} written to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuntimeOptimizer] Failed to write port file: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads the active port number from the port discovery file.
        /// </summary>
        /// <returns>The port number, or -1 if failed</returns>
        public static int ReadPortFromFile()
        {
            try
            {
                string filePath = GetPortFilePath();

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[RuntimeOptimizer] Port file not found: {filePath}");
                    return -1;
                }

                string content = File.ReadAllText(filePath);
                string[] lines = content.Split('\n');

                if (lines.Length > 0 && int.TryParse(lines[0].Trim(), out int port))
                {
                    Debug.Log($"[RuntimeOptimizer] Port {port} read from {filePath}");
                    return port;
                }

                Debug.LogWarning($"[RuntimeOptimizer] Invalid port file content: {content}");
                return -1;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuntimeOptimizer] Failed to read port file: {e.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Deletes the port discovery file.
        /// </summary>
        public static void DeletePortFile()
        {
            try
            {
                string filePath = GetPortFilePath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[RuntimeOptimizer] Port file deleted: {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RuntimeOptimizer] Failed to delete port file: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the port file path for external tools (Python scripts, etc.)
        /// Returns the platform-appropriate path without Unity-specific APIs.
        /// </summary>
        public static string GetPortFilePathForExternalTools()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return $"/sdcard/RuntimeOptimizer/{PORT_FILE_NAME}";
#else
            // For PC mode, use a standard location
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string directory = Path.Combine(userProfile, "AppData", "LocalLow", "RuntimeOptimizer");
            return Path.Combine(directory, PORT_FILE_NAME);
#endif
        }
    }
}
