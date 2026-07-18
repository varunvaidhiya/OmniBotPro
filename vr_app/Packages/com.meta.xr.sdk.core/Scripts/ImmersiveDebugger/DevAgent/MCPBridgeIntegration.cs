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

using Meta.MCPBridge.Runtime;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Integration MonoBehaviour that connects <see cref="ToolProviderClient"/>
    /// with the DevAgent. Manages the MCP client lifecycle and connection state.
    ///
    /// As a Tool Provider, this component registers the device's capabilities
    /// (tools, resources, prompts) with the MCP server and handles execution
    /// requests forwarded from AI agents.
    ///
    /// Initialized by <see cref="DevAgentController"/> which provides the
    /// server connection settings.
    /// </summary>
    internal class MCPBridgeIntegration : MonoBehaviour
    {
        private ToolProviderClient _client;

        /// <summary>
        /// Event fired when the connection state changes.
        /// </summary>
        internal event System.Action<bool> OnConnectionStateChanged;

        /// <summary>
        /// The underlying Tool Provider client instance.
        /// </summary>
        internal ToolProviderClient Client => _client;

        /// <summary>
        /// Whether the client is connected to the MCP server.
        /// </summary>
        internal bool IsConnected => _client?.IsConnected ?? false;

        /// <summary>
        /// Initialize the integration with server settings.
        /// </summary>
        internal void Initialize(string serverAddress, int serverPort)
        {
            _client = new ToolProviderClient(serverAddress, serverPort);

            // Set access token for authentication (same token as AgentBridge)
            var settings = RuntimeSettings.Instance;
            if (settings != null && !string.IsNullOrEmpty(settings.McpAccessToken))
            {
                _client.AccessToken = settings.McpAccessToken;
            }

            SubscribeToClientEvents();
        }

        /// <summary>
        /// Set the client for testing. Must be called before Start() runs.
        /// Replaces and disposes any existing client.
        /// </summary>
        internal void SetClient(ToolProviderClient client)
        {
            UnsubscribeFromClientEvents();
            _client?.Dispose();
            _client = client;
            SubscribeToClientEvents();
        }

        private void UnsubscribeFromClientEvents()
        {
            if (_client == null) return;
            _client.OnConnectionStateChanged -= HandleClientConnectionStateChanged;
            _client.OnRequestExecuted -= OnRequestExecuted;
            _client.OnCapabilitiesRegistered -= OnCapabilitiesRegistered;
            _client.OnError -= OnError;
        }

        private void SubscribeToClientEvents()
        {
            if (_client == null) return;
            _client.OnConnectionStateChanged += HandleClientConnectionStateChanged;
            _client.OnRequestExecuted += OnRequestExecuted;
            _client.OnCapabilitiesRegistered += OnCapabilitiesRegistered;
            _client.OnError += OnError;
        }

        private async void Start()
        {
            if (_client == null)
            {
                Debug.LogWarning("[MCPBridgeIntegration] Client not initialized. Call Initialize() first.");
                return;
            }

            await _client.ConnectAsync();
        }

        private void OnDestroy()
        {
            UnsubscribeFromClientEvents();
            _client?.Dispose();
            _client = null;
        }

        internal void OnApplicationPause(bool pauseStatus)
        {
            if (_client == null) return;

            if (pauseStatus)
            {
                // Headset doffed — disconnect to release resources
                Debug.Log("[MCPBridgeIntegration] Application paused, disconnecting from MCP server");
                _client.Disconnect();
            }
            else
            {
                // Headset donned — reconnect
                Debug.Log("[MCPBridgeIntegration] Application resumed, reconnecting to MCP server");
                _ = _client.ConnectAsync();
            }
        }

        private void HandleClientConnectionStateChanged(bool connected)
        {
            Debug.Log($"[MCPBridgeIntegration] Connection state: {(connected ? "Connected" : "Disconnected")}");
            OnConnectionStateChanged?.Invoke(connected);
        }

        private void OnRequestExecuted(string method, bool success)
        {
            var status = success ? "completed" : "failed";
            Debug.Log($"[MCPBridgeIntegration] Request {method} {status}");
        }

        private void OnCapabilitiesRegistered()
        {
            Debug.Log("[MCPBridgeIntegration] Capabilities registered with MCP server");
        }

        private void OnError(string error)
        {
            Debug.LogError($"[MCPBridgeIntegration] Error: {error}");
        }

        /// <summary>
        /// Get a status string describing the current state.
        /// </summary>
        internal string GetStatusString()
        {
            if (_client == null)
                return "MCP Client: Not initialized";

            if (_client.IsConnected)
                return $"MCP Client: Connected to {_client.ServerUrl}";

            return $"MCP Client: Disconnected from {_client.ServerUrl}";
        }
    }
}
