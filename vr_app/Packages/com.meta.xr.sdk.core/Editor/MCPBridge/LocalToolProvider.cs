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
using Meta.MCPBridge.Schemas;
using UnityEditor;
using UnityEngine;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Registers local Unity Editor tools with the ToolProviderManager so MCPBridge
    /// can execute tools without requiring an external device connection.
    ///
    /// This enables tools like CompilationTools, TestRunnerTools, UIVerificationTools,
    /// etc. to be called directly by AI agents via the MCP protocol.
    ///
    /// Inspired by ImmersiveDebugger.DevAgent/MCPBridgeIntegration.cs which manages
    /// the ToolProviderClient lifecycle for device-side tool execution.
    /// </summary>
    internal static class LocalToolProvider
    {
        internal const string LocalProviderId = "local-unity-editor";
        private static bool _isRegistered;

        /// <summary>
        /// Whether the local tool provider is currently registered.
        /// </summary>
        internal static bool IsRegistered => _isRegistered;

        /// <summary>
        /// Register the local tool provider with ToolProviderManager.
        /// Discovers all available tools via LocalExecutor and registers them
        /// so they can be called directly without a device connection.
        /// </summary>
        internal static void Register()
        {
            if (_isRegistered)
            {
                Debug.Log("[LocalToolProvider] Already registered, skipping.");
                return;
            }

            try
            {
                LocalExecutor.instance.EnsureInitialized();

                var capabilities = LocalExecutor.instance.GetCapabilities();

                var memoryStream = new MemoryStream();
                var writer = new StreamWriter(memoryStream) { AutoFlush = true };

                ToolProviderManager.Instance.RegisterProvider(LocalProviderId, writer);
                ToolProviderManager.Instance.CurrentProvider?.SetCapabilities(capabilities);

                _isRegistered = true;

                Debug.Log($"[LocalToolProvider] Registered with {capabilities.Tools?.Length ?? 0} tools, " +
                          $"{capabilities.Resources?.Length ?? 0} resources, {capabilities.Prompts?.Length ?? 0} prompts");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalToolProvider] Failed to register: {ex.Message}");
            }
        }

        /// <summary>
        /// Unregister the local tool provider from ToolProviderManager.
        /// </summary>
        internal static void Unregister()
        {
            if (!_isRegistered)
            {
                return;
            }

            try
            {
                ToolProviderManager.Instance.UnregisterProvider(LocalProviderId);
                _isRegistered = false;
                Debug.Log("[LocalToolProvider] Unregistered");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalToolProvider] Failed to unregister: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh the registered capabilities.
        /// Call this after tools have been added or modified at runtime.
        /// </summary>
        internal static void RefreshCapabilities()
        {
            if (!_isRegistered)
            {
                Debug.LogWarning("[LocalToolProvider] Cannot refresh - not registered. Call Register() first.");
                return;
            }

            try
            {
                LocalExecutor.instance.Reset();
                LocalExecutor.instance.EnsureInitialized();

                var capabilities = LocalExecutor.instance.GetCapabilities();
                ToolProviderManager.Instance.CurrentProvider?.SetCapabilities(capabilities);

                Debug.Log($"[LocalToolProvider] Refreshed capabilities: {capabilities.Tools?.Length ?? 0} tools, " +
                          $"{capabilities.Resources?.Length ?? 0} resources, {capabilities.Prompts?.Length ?? 0} prompts");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalToolProvider] Failed to refresh capabilities: {ex.Message}");
            }
        }
    }
}
