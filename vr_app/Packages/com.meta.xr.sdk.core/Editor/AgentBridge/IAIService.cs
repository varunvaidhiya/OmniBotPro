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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Interface for AI service implementations.
    /// All AI services must implement this interface to provide core functionality.
    /// </summary>
    public interface IAIService : IDisposable
    {
        /// <summary>
        /// The display name of the service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Whether the service has an active session.
        /// </summary>
        bool HasActiveSession { get; }

        /// <summary>
        /// Process user input through the AI service.
        /// </summary>
        /// <param name="userInput">The user's input text</param>
        /// <param name="caller">Caller identity for telemetry tracking</param>
        /// <param name="images">Optional list of image attachments</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <param name="systemPrompt">Contextual information for AI</param>
        Task ProcessUserInputAsync(string userInput, CallerIdentity? caller, List<ImageAttachment>? images = null, System.Threading.CancellationToken cancellationToken = default, string? systemPrompt = null);

        /// <summary>
        /// Clear the current session to start fresh.
        /// </summary>
        void ClearSession();

        /// <summary>
        /// Cancel the current operation if one is in progress.
        /// </summary>
        Task CancelCurrentOperationAsync();
    }
}
