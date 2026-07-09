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

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Attribute to mark AI service classes for automatic registration with the AIServiceRegistry.
    /// Apply this attribute to classes that implement IAIService to enable automatic discovery
    /// and registration when the Unity Editor loads.
    /// </summary>
    /// <remarks>
    /// This attribute is primarily intended for built-in services. Third-party services
    /// can use this attribute or manually register via AIServiceRegistry.Register().
    ///
    /// Example usage:
    /// <code>
    /// [RegisterAIService("my-service", "My Custom AI Service", Priority = 100)]
    /// public class MyCustomService : AIServiceBase, IServiceSettingsUI
    /// {
    ///     // Implementation
    /// }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RegisterAIServiceAttribute : Attribute
    {
        /// <summary>
        /// Unique identifier for this service (e.g., "claudecode", "devmate", "my-custom-service").
        /// This ID is used for persistence in settings and must be stable across sessions.
        /// Use lowercase alphanumeric characters and hyphens only.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Human-readable display name shown in the UI dropdown (e.g., "Claude Code", "My Custom AI").
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Priority for ordering in the dropdown. Lower values appear first.
        /// Built-in services use 0-99. Third-party services should use 100+.
        /// Default is 100.
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Creates a new RegisterAIService attribute.
        /// </summary>
        /// <param name="id">Unique identifier for this service</param>
        /// <param name="displayName">Human-readable display name</param>
        public RegisterAIServiceAttribute(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Service ID cannot be null or whitespace", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be null or whitespace", nameof(displayName));
            }

            Id = id;
            DisplayName = displayName;
        }
    }
}
