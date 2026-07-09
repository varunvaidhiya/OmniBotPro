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

using Meta.XR.Editor.Id;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Simple interface for services that provide custom settings UI rendering.
    /// This is the recommended interface for third-party services as it has no
    /// dependencies on internal Meta packages.
    /// </summary>
    /// <remarks>
    /// Third-party developers should implement this interface to provide custom
    /// settings UI for their AI service. The DrawSettingsUI method will be called
    /// when the user opens the AgentBridge settings panel.
    ///
    /// Example usage:
    /// <code>
    /// [RegisterAIService("my-service", "My Custom AI", Priority = 100)]
    /// public class MyCustomService : AIServiceBase, IServiceSettingsUISimple
    /// {
    ///     public void DrawSettingsUI()
    ///     {
    ///         EditorGUILayout.LabelField("My Settings", EditorStyles.boldLabel);
    ///         // Draw your custom settings here using EditorGUILayout
    ///     }
    ///
    ///     public void ResetSettingsToDefaults()
    ///     {
    ///         // Reset your settings to defaults
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface IServiceSettingsUISimple
    {
        /// <summary>
        /// Render service-specific settings in the Unity Editor using IMGUI.
        /// Called from the AgentBridge settings panel.
        /// </summary>
        void DrawSettingsUI();

        /// <summary>
        /// Reset all service-specific settings to their default values.
        /// Called when user clicks "Reset to Defaults" button.
        /// </summary>
        void ResetSettingsToDefaults();
    }

    /// <summary>
    /// Extended interface for services that provide custom settings UI rendering
    /// with access to Meta's internal settings infrastructure.
    /// This interface depends on Meta.XR.Editor.Id and is intended for internal use.
    /// Third-party developers should use IServiceSettingsUISimple instead.
    /// </summary>
    public interface IServiceSettingsUI : IServiceSettingsUISimple
    {
        /// <summary>
        /// Render service-specific settings in the Unity Editor using IMGUI.
        /// Called from SimpleAgentWindow.DrawServiceSettings() with full settings context.
        /// </summary>
        /// <param name="origins">The origins for settings display</param>
        /// <param name="originData">The origin data for telemetry</param>
        void DrawSettingsUI(Origins origins, IIdentified originData);
    }
}
