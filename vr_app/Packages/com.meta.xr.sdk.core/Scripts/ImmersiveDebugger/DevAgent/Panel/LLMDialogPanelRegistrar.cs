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

using Meta.XR.ImmersiveDebugger.UserInterface;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Meta.XR.ImmersiveDebugger.Utils;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Panel registrar for the LLM Dialog Panel.
    /// This class implements IPanelRegistrar to dynamically register the LLM Dialog Panel
    /// with the Immersive Debugger interface at runtime.
    /// </summary>
    internal class LLMDialogPanelRegistrar : IPanelRegistrar
    {
        /// <summary>
        /// Register the LLM Dialog Panel with the debug interface.
        /// This method is called automatically by the DebugInterface during initialization.
        /// </summary>
        /// <param name="debugInterface">The debug interface to register the panel with</param>
        public void RegisterPanel(DebugInterface debugInterface)
        {
            if (debugInterface == null)
            {
                Debug.LogWarning("Cannot register LLM Dialog Panel: DebugInterface is null");
                return;
            }

            var settings = RuntimeSettings.Instance;
            if (settings == null || !settings.Enabled)
            {
                Debug.Log("[DevAgent] AI Assistant is disabled in settings, skipping panel registration");
                return;
            }

            try
            {
                // Create and configure the LLM Dialog Panel
                var llmDialogPanel = debugInterface.Append<LLMDialogPanel>("llmDialog");
                llmDialogPanel.LayoutStyle = Style.Load<LayoutStyle>("ConsolePanel"); // Reuse console panel style
                llmDialogPanel.BackgroundStyle = Style.Load<ImageStyle>("PanelBackground");
                llmDialogPanel.Title = "Assistant";
                llmDialogPanel.Icon = Resources.Load<Texture2D>("Textures/notice_icon"); // Temporary icon, will replace with mic icon

                // Register the panel with the debug interface
                debugInterface.RegisterDebugPanel(llmDialogPanel);

                // Show the panel by default for testing (can be removed later)
                llmDialogPanel.Show();

                Debug.Log("Successfully registered LLM Dialog Panel with Immersive Debugger");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to register LLM Dialog Panel: {ex.Message}");
            }
        }
    }
}
