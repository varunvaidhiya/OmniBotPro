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

using Meta.XR.AI.AgentBridge;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Orchestrator component that owns and wires up all DevAgent components.
    /// Creates <see cref="ConversationManager"/> for state management,
    /// <see cref="AgentBridgeIntegration"/> for remote AI communication,
    /// and <see cref="MCPBridgeIntegration"/> for MCP tool execution.
    ///
    /// This establishes clean unidirectional dependencies:
    /// LLMDialogPanel → DevAgentController → ConversationManager / AgentBridgeIntegration / MCPBridgeIntegration
    /// </summary>
    internal class DevAgentController : MonoBehaviour
    {
        private ConversationManager _conversationManager;
        private AgentBridgeIntegration _integration;
        private MCPBridgeIntegration _mcpIntegration;

        internal ConversationManager ConversationManager => _conversationManager;
        internal AgentBridgeIntegration Integration => _integration;
        internal MCPBridgeIntegration McpIntegration => _mcpIntegration;

        // For testing: allow client injection
        private IRemoteAgentBridgeClient _injectedClient;

        /// <summary>
        /// Set the AgentBridge client for testing. If called after Initialize(), forwards to the integration.
        /// Must be called before Start() runs (before the first yield return null).
        /// </summary>
        internal void SetClient(IRemoteAgentBridgeClient client)
        {
            _injectedClient = client;
            if (_integration != null)
            {
                _integration.SetClient(client);
            }
        }

        /// <summary>
        /// Set the MCP client for testing. If called after Initialize(), forwards to the MCP integration.
        /// Must be called before Start() runs (before the first yield return null).
        /// </summary>
        internal void SetMcpClient(Meta.MCPBridge.Runtime.ToolProviderClient client)
        {
            _mcpIntegration?.SetClient(client);
        }

        /// <summary>
        /// Initialize the controller and create child components.
        /// Called immediately after AddComponent to ensure ConversationManager is available.
        /// </summary>
        internal void Initialize()
        {
            if (_conversationManager != null) return; // Already initialized

            _conversationManager = gameObject.AddComponent<ConversationManager>();
            _integration = gameObject.AddComponent<AgentBridgeIntegration>();
            _mcpIntegration = gameObject.AddComponent<MCPBridgeIntegration>();

            _integration.Initialize(_conversationManager, _injectedClient);

            // Initialize MCP integration with server settings
            var settings = RuntimeSettings.Instance;
            if (settings != null)
            {
                _mcpIntegration.Initialize(settings.ServerAddress, settings.McpServerPort);
            }
        }

        private void Awake()
        {
            // Initialize if not already done (for backward compatibility)
            Initialize();
        }
    }
}
