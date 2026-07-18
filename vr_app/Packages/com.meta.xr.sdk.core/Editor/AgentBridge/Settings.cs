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
using System.Linq;
using System.Threading.Tasks;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles;
using static Meta.XR.Editor.UserInterface.Utils;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Shared settings for Agent Bridge, stored in EditorPrefs (user-specific preferences).
    /// Service-specific settings are defined in their respective service classes.
    /// These settings are not project-based and will not be checked into version control.
    /// </summary>
    internal static class Settings
    {
        /// <summary>
        /// IIdentified descriptor for telemetry and settings key generation.
        /// Uses ToolDescriptor when internal code is available, otherwise falls back to simple descriptor.
        /// </summary>
        internal static readonly IIdentified Owner = Utils.ToolDescriptor;

        /// <summary>
        /// Raised when the user enables AI Agent Bridge via the settings toggle.
        /// External assemblies (e.g., MCPBridge Editor) subscribe to this to trigger
        /// their own initialization without requiring a reverse assembly reference.
        /// </summary>
        internal static event Action? Activated;

        /// <summary>
        /// Global toggle for AI Agent Bridge. When disabled (the default), no HTTP servers are started,
        /// no assembly scanning occurs, and no background services run. All code remains compiled
        /// and present, but expensive operations stay dormant until the user explicitly opts in.
        /// </summary>
        public static readonly UserBool Enabled = new UserBool
        {
            Uid = nameof(Enabled),
            Owner = Owner,
            Default = false,
            Label = "Enable AI Agent Bridge",
            Tooltip = "Enable AI Agent Bridge features. When off, no servers or background services run.",
            SendTelemetry = true
        };

        /// <summary>
        /// The currently selected AI service ID (string-based for extensibility).
        /// Third-party services can register with AIServiceRegistry and will appear in the dropdown.
        /// </summary>
        public static readonly UserString SelectedServiceId = new UserString
        {
            Uid = nameof(SelectedServiceId),
            Owner = Owner,
            Default = ClaudeCodeService.ServiceId,
            Label = "Service Type",
            Tooltip = "The AI service to use for Agent Bridge operations.",
            SendTelemetry = true
        };

        /// <summary>
        /// Enable verbose logging for debugging purposes.
        /// When enabled, additional diagnostic information will be logged to the Unity console.
        /// </summary>
        public static readonly UserBool VerboseLogging = new UserBool
        {
            Uid = nameof(VerboseLogging),
            Owner = Owner,
            Default = false,
            Label = "Verbose Logging",
            Tooltip = "Enable detailed debug logging for Agent Bridge operations. Useful for troubleshooting.",
            SendTelemetry = false
        };

        // Track if we've already validated on open
        private static bool _hasValidatedOnOpen = false;
        private static string? _lastValidatedServiceId = null;

        // Track Advanced foldout state
        private static bool _advancedFoldoutExpanded = false;


        /// <summary>
        /// Render settings UI in Edit > Preferences > Meta XR / Agent Bridge.
        /// </summary>
        public static void OnGUI(Origins origin, string searchContext)
        {
            // Global toggle at the top, always visible
            EditorGUI.BeginChangeCheck();
            Enabled.DrawForGUI(origin, Utils.ToolDescriptor, OnEnabledChanged);
            var enabledChanged = EditorGUI.EndChangeCheck();

            if (!Enabled.Value)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal(GUIStyles.DialogBox);
                using (new ColorScope(ColorScope.Scope.Content, Colors.LightGray))
                {
                    EditorGUILayout.LabelField(Contents.InfoIcon, GUIStyles.DialogIconStyle,
                        GUILayout.Width(GUIStyles.DialogIconStyle.fixedWidth), GUILayout.Height(GUIStyles.DialogIconStyle.fixedWidth));
                }
                EditorGUILayout.LabelField(
                    $"{Utils.PublicName} is disabled. Enable it above to start AI services and configure settings.",
                    GUIStyles.DialogTextStyle, GUILayout.Height(GUIStyles.DialogIconStyle.fixedWidth));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Grey out all settings below when disabled, so the structure is visible
            // but clearly non-interactive (same pattern as ImmersiveDebugger).
            EditorGUI.BeginDisabledGroup(!Enabled.Value);

            EditorGUILayout.Space();

            var currentServiceId = SelectedServiceId.Value;

            // Draw service selector dropdown with change detection
            EditorGUI.BeginChangeCheck();
            DrawServiceSelector(origin);
            var serviceTypeChanged = EditorGUI.EndChangeCheck();

            if (serviceTypeChanged && Enabled.Value)
            {
                // Service type changed - ensure the new service is initialized
                AgentBridgeManager.EnsureServiceInitialized();
                // Reset validation state for new service
                _hasValidatedOnOpen = false;
            }

            EditorGUILayout.Space();

            // Ensure service is initialized so we can show its settings.
            // Skip when disabled: services are not initialized until the user opts in.
            IAIService? service = null;
            if (Enabled.Value)
            {
                service = AgentBridgeManager.GetCurrentService();
                if (service == null)
                {
                    AgentBridgeManager.EnsureServiceInitialized();
                    service = AgentBridgeManager.GetCurrentService();
                }
            }

            // Draw validation section (between service selection and specialized settings)
            DrawValidationSection(service, serviceTypeChanged);

            EditorGUILayout.Space();

            // Advanced settings foldout
            _advancedFoldoutExpanded = EditorGUILayout.Foldout(_advancedFoldoutExpanded, "Advanced", true);
            if (_advancedFoldoutExpanded)
            {
                EditorGUI.indentLevel++;

                // Draw service-specific settings (polymorphic)
                // Check for full IServiceSettingsUI first, then fallback to IServiceSettingsUISimple for 3P services
                if (service is IServiceSettingsUI serviceUI)
                {
                    serviceUI.DrawSettingsUI(origin, Utils.ToolDescriptor);
                }
                else if (service is IServiceSettingsUISimple simpleUI)
                {
                    simpleUI.DrawSettingsUI();
                }

                EditorGUILayout.Space();

                // Logging header
                EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);
                VerboseLogging.DrawForGUI(origin, Utils.ToolDescriptor,
                    () => Log.VerboseLogging = VerboseLogging.Value);

                EditorGUILayout.Space();

                // Remote Server section
                RemoteAgentSettings.DrawSettingsUI(origin, Utils.ToolDescriptor);

                EditorGUILayout.Space();

                // Reset button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset to Defaults"))
                {
                    SelectedServiceId.Reset();
                    VerboseLogging.Reset();
                    RemoteAgentSettings.ResetToDefaults();

                    // Reset service settings - check for both interfaces
                    if (service is IServiceSettingsUI resetService)
                    {
                        resetService.ResetSettingsToDefaults();
                    }
                    else if (service is IServiceSettingsUISimple simpleResetService)
                    {
                        simpleResetService.ResetSettingsToDefaults();
                    }

                    // Re-validate after reset
                    _hasValidatedOnOpen = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Called when the Enabled toggle changes. When toggled on, triggers
        /// deferred initialization of all AI services that were skipped at editor load.
        /// </summary>
        private static void OnEnabledChanged()
        {
            if (Enabled.Value)
            {
                Log.Info($"{Utils.PublicName} enabled by user. Initializing services...");
                InitializeAllServices();
                Activated?.Invoke();
            }
            else
            {
                Log.Info($"{Utils.PublicName} disabled by user.");
            }
        }

        /// <summary>
        /// Initialize all AI services that were gated behind the Enabled toggle.
        /// Called when the user enables AI Agent Bridge for the first time.
        /// </summary>
        internal static void InitializeAllServices()
        {
            AIServiceRegistry.Initialize();
            AgentBridgeManager.InitializeIfEnabled();
            MainThreadDispatcher.EnsureStarted();
            ToolbarItem.EnsureRegistered();
        }

        /// <summary>
        /// Draw the service selector dropdown using AIServiceRegistry.
        /// This allows third-party services to appear in the dropdown.
        /// </summary>
        private static void DrawServiceSelector(Origins origin)
        {
            var availableServices = AIServiceRegistry.GetAllServices().ToArray();
            if (availableServices.Length == 0)
            {
                return;
            }

            // Get current selection
            var currentServiceId = SelectedServiceId.Value;
            var currentIndex = Array.FindIndex(availableServices, s => s.Id == currentServiceId);

            // If current selection is not in the list (e.g., unregistered), default to first
            if (currentIndex < 0)
            {
                currentIndex = 0;
                SelectedServiceId.SetValue(availableServices[0].Id, Origins.UserSettings, Owner);
            }

            // Create display names array
            var displayNames = availableServices.Select(s => s.DisplayName).ToArray();

            // Draw popup
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent(SelectedServiceId.Label, SelectedServiceId.Tooltip));
            var newIndex = EditorGUILayout.Popup(currentIndex, displayNames);
            EditorGUILayout.EndHorizontal();

            // Update selection if changed
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < availableServices.Length)
            {
                SelectedServiceId.SetValue(availableServices[newIndex].Id, Origins.UserSettings, Owner);
            }
        }

        /// <summary>
        /// Draw the validation section with status and button.
        /// </summary>
        private static void DrawValidationSection(IAIService? service, bool serviceTypeChanged)
        {
            var validationService = service as IServiceValidation;
            if (validationService == null)
            {
                return;
            }

            var currentServiceId = SelectedServiceId.Value;

            // Auto-validate on settings page open (first time) or when service type changes
            if (!_hasValidatedOnOpen || _lastValidatedServiceId != currentServiceId || serviceTypeChanged)
            {
                _hasValidatedOnOpen = true;
                _lastValidatedServiceId = currentServiceId;
                TriggerValidation(validationService);
            }

            var result = validationService.CurrentValidationResult;

            // Main horizontal layout: [icon] [title/subtext vertical group] [flexible space] [refresh button]
            // Use custom style with left padding to align with other properties
            var validationBoxStyle = new GUIStyle(GUIStyles.DialogBox)
            {
                padding = new RectOffset(Constants.Margin + Constants.Padding, Constants.Margin, Constants.Margin, Constants.Margin)
            };
            EditorGUILayout.BeginHorizontal(validationBoxStyle);

            // Status icon (big, vertically centered)
            var (statusIcon, statusColor) = GetValidationStatusVisuals(result.Status);
            using (new ColorScope(ColorScope.Scope.Content, statusColor))
            {
                GUILayout.Label(statusIcon, GUIStyles.DialogIconStyle);
            }

            // Title and subtext in vertical group (no extra spacing)
            EditorGUILayout.BeginVertical();
            var titleStyle = new GUIStyle(GUIStyles.BoldLabel)
            { margin = new RectOffset(0, 0, 0, 0), padding = new RectOffset(0, 0, 0, 0) };
            var subtextStyle = new GUIStyle(GUIStyles.DialogTextStyle)
            { margin = new RectOffset(0, 0, 0, 0), padding = new RectOffset(Constants.Padding, 0, 0, 0) };
            EditorGUILayout.LabelField(GetValidationStatusTitle(result.Status), titleStyle);
            // Don't show subtext if it's redundant with the title (e.g., both saying "Validating...")
            if (result.Status != ValidationStatus.Validating && result.Status != ValidationStatus.Unknown)
            {
                EditorGUILayout.LabelField(result.Message, subtextStyle);
            }
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Refresh button (simple grey button, same as Reset to Defaults)
            var isValidating = result.Status == ValidationStatus.Validating;
            using (new EditorGUI.DisabledGroupScope(isValidating))
            {
                if (GUILayout.Button("Refresh"))
                {
                    TriggerValidation(validationService);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Trigger async validation and update the UI.
        /// </summary>
        private static async void TriggerValidation(IServiceValidation validationService)
        {
            try
            {
                await validationService.ValidateConfigurationAsync();
                // Force repaint to show updated status
                EditorApplication.delayCall += () =>
                {
                    // Request repaint of the settings window
                    EditorApplication.QueuePlayerLoopUpdate();
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AgentBridge] Validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the visual representation for a validation status.
        /// </summary>
        private static (GUIContent icon, Color color) GetValidationStatusVisuals(ValidationStatus status)
        {
            return status switch
            {
                ValidationStatus.Valid => (Contents.CheckIcon, Colors.AI),
                ValidationStatus.Invalid => (Contents.ErrorIcon, Colors.ErrorColor),
                ValidationStatus.Error => (Contents.ErrorIcon, Colors.ErrorColor),
                ValidationStatus.Validating => (Contents.InfoIcon, Colors.LightGray),
                _ => (Contents.InfoIcon, Colors.LightGray) // Unknown
            };
        }

        /// <summary>
        /// Get the title text for a validation status.
        /// </summary>
        private static string GetValidationStatusTitle(ValidationStatus status)
        {
            return status switch
            {
                ValidationStatus.Valid => "Configuration Valid",
                ValidationStatus.Invalid => "Configuration Invalid",
                ValidationStatus.Error => "Validation Error",
                ValidationStatus.Validating => "Validating...",
                _ => "Validation Required"
            };
        }

        /// <summary>
        /// Simple IIdentified implementation for AgentBridge telemetry (non-internal fallback).
        /// </summary>
        private class AgentBridgeDescriptor : IIdentified
        {
            public string Id => "AgentBridge";
        }
    }
}
