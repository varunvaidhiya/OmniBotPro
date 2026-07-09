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
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Manages discovery files so external tools (Devmate) can find the MCPBridge server.
    /// Discovery files are written to %TEMP% and contain port, token, and project info.
    /// </summary>
    internal static class ExternalDiscovery
    {
        private const string TempFilePrefix = "mcpbridge_";

        /// <summary>
        /// Writes discovery file so external tools (Devmate) can find the MCPBridge.
        /// File is written to %TEMP%/mcpbridge_{projectHash}.info containing all connection info.
        /// </summary>
        public static void WriteDiscoveryFiles()
        {
            var projectHash = GetProjectHash();
            var tempDir = Path.GetTempPath();

            try
            {
                var port = McpBridgeSettings.Port.Value;
                var token = McpBridgeSettings.AccessTokenValue;
                var projectPath = Application.dataPath.Replace("\\", "/").Replace("/Assets", "");
                var pid = System.Diagnostics.Process.GetCurrentProcess().Id;

                var infoFile = Path.Combine(tempDir, $"{TempFilePrefix}{projectHash}.info");

                var json = $@"{{
  ""port"": {port},
  ""token"": ""{token}"",
  ""projectPath"": ""{projectPath}"",
  ""unityVersion"": ""{Application.unityVersion}"",
  ""pid"": {pid}
}}";

                File.WriteAllText(infoFile, json);

                Debug.Log($"[MCPBridge] Discovery file written to {infoFile}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPBridge] Failed to write discovery file: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes discovery file when MCPBridge stops.
        /// </summary>
        public static void ClearDiscoveryFiles()
        {
            var projectHash = GetProjectHash();
            var tempDir = Path.GetTempPath();

            try
            {
                var infoFile = Path.Combine(tempDir, $"{TempFilePrefix}{projectHash}.info");
                if (File.Exists(infoFile)) File.Delete(infoFile);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPBridge] Failed to clear discovery file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a short hash of the project path for unique temp file naming.
        /// This allows multiple Unity projects to have their own discovery files.
        /// </summary>
        private static string GetProjectHash()
        {
            var projectPath = Application.dataPath.Replace("/Assets", "");
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(projectPath));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLowerInvariant();
            }
        }
    }
}
