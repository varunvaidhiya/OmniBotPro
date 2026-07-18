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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Interface for the Remote Agent Bridge client that communicates with
    /// the Remote Agent Server running in the Unity Editor.
    /// Enables use on remote devices (e.g., Quest headsets) for AI inference.
    /// </summary>
    public interface IRemoteAgentBridgeClient : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when a conversation message is received from the server.
        /// </summary>
        event Action<ConversationMessage>? OnMessageReceived;

        /// <summary>
        /// Fired when the server's processing state changes (true = processing, false = idle).
        /// </summary>
        event Action<bool>? OnProcessingStateChanged;

        /// <summary>
        /// Fired when the server conversation is cleared.
        /// </summary>
        event Action? OnConversationCleared;

        /// <summary>
        /// Fired when the connection state to the server changes (true = connected, false = disconnected).
        /// </summary>
        event Action<bool>? OnConnectionStateChanged;

        /// <summary>
        /// Fired when an error is received from the server during AI processing.
        /// </summary>
        event Action<RemoteSseError>? OnErrorReceived;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the client is currently connected to the Remote Agent Server.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The base URL for the Remote Agent Server (e.g., "192.168.1.100").
        /// </summary>
        string ServerUrl { get; set; }

        /// <summary>
        /// The port for the Remote Agent Server.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// The access token for authenticating with the Remote Agent Server.
        /// Must be set before calling <see cref="ConnectAsync"/>.
        /// </summary>
        string? AccessToken { get; set; }

        #endregion

        #region Connection Management

        /// <summary>
        /// Connect to the Remote Agent Server. Starts the SSE stream and health check loop.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>True if connection was successful, false otherwise.</returns>
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from the server. Stops SSE stream and health checks.
        /// </summary>
        void Disconnect();

        #endregion

        #region AI Inference Methods

        /// <summary>
        /// Send a prompt to the Remote Agent Server for AI inference.
        /// The response will arrive asynchronously via <see cref="OnMessageReceived"/> events.
        /// </summary>
        /// <param name="request">The prompt request containing prompt text, caller ID, and optional system prompt.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (success, errorMessage).</returns>
        Task<(bool success, string? error)> SendPromptAsync(
            RemotePromptRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel the current AI operation on the server.
        /// </summary>
        /// <param name="request">The caller request identifying who is cancelling.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (success, errorMessage).</returns>
        Task<(bool success, string? error)> CancelAsync(
            RemoteCallerRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear the conversation on the server.
        /// </summary>
        /// <param name="request">The caller request identifying who is clearing the conversation.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (success, errorMessage).</returns>
        Task<(bool success, string? error)> ClearConversationAsync(
            RemoteCallerRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the current status of the Remote Agent Server.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Tuple of (status, errorMessage). Status is null on failure.</returns>
        Task<(RemoteAgentStatus? status, string? error)> GetStatusAsync(
            CancellationToken cancellationToken = default);

        #endregion
    }
}
