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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.AI.AgentBridge.Acp;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Service for Gemini CLI integration via ACP (Agent Client Protocol).
    /// Communicates over a bidirectional JSON-RPC 2.0 protocol over stdio,
    /// keeping the subprocess alive across prompts within a session.
    /// </summary>
    [RegisterAIService(ServiceId, "Gemini CLI", Priority = 20)]
    public class GeminiCliService : AIServiceBase, IServiceSettingsUI, IServiceValidation, IAIServiceSessionResume
    {
        /// <summary>
        /// The unique service identifier for Gemini CLI.
        /// </summary>
        public const string ServiceId = "geminicli";

        /// <summary>
        /// Settings specific to Gemini CLI service.
        /// </summary>
        public static class GeminiCliSettings
        {
            private static readonly IIdentified Owner = new GeminiCliDescriptor();

            internal static readonly UserString ExecutablePath = new UserString
            {
                Uid = nameof(ExecutablePath),
                Owner = Owner,
                Default = "",
                Label = "Gemini CLI Executable Path",
                Tooltip = "Optional path to Gemini CLI executable. Leave empty to use PATH environment variable.",
                SendTelemetry = false
            };

            private class GeminiCliDescriptor : IIdentified
            {
                public string Id => "AgentBridge.GeminiCli";
            }
        }

        private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
        private AcpClient? _acpClient;
        private string? _sessionId;
        private bool _isFirstPromptInSession = true;
        private bool _disposed;
        private ValidationResult _currentValidationResult = ValidationResult.Unknown();

        public override string ServiceName => "Gemini CLI";

        public override bool HasActiveSession => !string.IsNullOrEmpty(_sessionId);

        /// <inheritdoc />
        public ValidationResult CurrentValidationResult => _currentValidationResult;

        /// <inheritdoc />
        public bool CanResumeSession => !string.IsNullOrEmpty(_sessionId);

        /// <summary>
        /// Process user input through Gemini CLI via ACP.
        /// </summary>
        public override async Task ProcessUserInputAsync(
            string userInput,
            CallerIdentity? caller,
            List<ImageAttachment>? images = null,
            CancellationToken cancellationToken = default,
            string? systemPrompt = null)
        {
            await _executionSemaphore.WaitAsync(cancellationToken);
            try
            {
                Log.Info($"Processing user input through Gemini CLI: {userInput}");
                if (images != null && images.Count > 0)
                {
                    Log.Info($"Processing with {images.Count} image(s)");
                }

                ConversationManager.IsActive = true;
                ConversationManager.ClearError();

                // Add the user's input to the conversation history
                ConversationManager.AddMessage("user", userInput);

                // Ensure ACP client is connected and session exists
                await EnsureAcpClientAsync();

                if (_acpClient == null || _sessionId == null)
                {
                    throw new InvalidOperationException("Failed to initialize ACP client or session");
                }

                // Build prompt content blocks
                var promptBlocks = new List<ContentBlock>();

                // Include system prompt as first content block on first prompt in session
                if (systemPrompt != null && _isFirstPromptInSession)
                {
                    promptBlocks.Add(new TextContentBlock { Text = $"[System]: {systemPrompt}" });
                }

                // Add images as native base64 content blocks (no temp files needed)
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        if (string.IsNullOrEmpty(img.Data))
                        {
                            continue;
                        }
                        promptBlocks.Add(new ImageContentBlock
                        {
                            Data = img.Data,
                            MimeType = img.MediaType
                        });
                    }
                }

                // Add user input text
                promptBlocks.Add(new TextContentBlock { Text = userInput });

                _isFirstPromptInSession = false;

                // Send prompt — streaming updates arrive via OnSessionUpdate event
                await _acpClient.PromptAsync(_sessionId, promptBlocks);

                // Clear error on success
                ConversationManager.ClearError();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                ConversationManager.SetError($"{ex.Message} ({ex.GetType().Name})");
            }
            finally
            {
                ConversationManager.IsActive = false;
                _executionSemaphore.Release();
            }
        }

        /// <summary>
        /// Clear the current session. Kills the subprocess and resets state.
        /// </summary>
        public override void ClearSession()
        {
            Log.Info("Clearing Gemini CLI session");

            if (_acpClient != null)
            {
                _acpClient.OnSessionUpdate -= HandleSessionUpdate;
            }
            _acpClient?.Dispose();
            _acpClient = null;
            _sessionId = null;
            _isFirstPromptInSession = true;

            ConversationManager.Clear();
        }

        /// <summary>
        /// Cancel the current operation by sending a cancel notification and killing the process.
        /// </summary>
        public override async Task CancelCurrentOperationAsync()
        {
            if (_acpClient == null || _sessionId == null)
            {
                return;
            }

            try
            {
                Log.Info("Cancelling Gemini CLI operation");
                _acpClient.Cancel(_sessionId);

                // Give a moment for the cancel to take effect
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Log.Warning($"Error sending cancel notification: {ex.Message}");
            }

            // If the process already stopped after cancel, clean up state
            if (_acpClient == null || !_acpClient.IsRunning)
            {
                _acpClient = null;
                _sessionId = null;
                _isFirstPromptInSession = true;
                return;
            }

            // Force kill if still running after brief wait
            try
            {
                _acpClient.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning($"Error disposing ACP client during cancel: {ex.Message}");
            }
            _acpClient = null;
            _sessionId = null;
            _isFirstPromptInSession = true;
        }

        /// <summary>
        /// Ensure the ACP client is initialized with an active session.
        /// Creates the subprocess and performs the initialize handshake if needed.
        /// </summary>
        private async Task EnsureAcpClientAsync()
        {
            // If client is alive and session exists, reuse it
            if (_acpClient != null && _acpClient.IsRunning && _sessionId != null)
            {
                return;
            }

            // Clean up any dead client
            if (_acpClient != null)
            {
                _acpClient.OnSessionUpdate -= HandleSessionUpdate;
                _acpClient.Dispose();
                _acpClient = null;
                _sessionId = null;
            }

            var geminiExecutable = GetGeminiExecutable();

            _acpClient = new AcpClient(geminiExecutable, GetLoginShellPath(), "--experimental-acp");
            _acpClient.OnSessionUpdate += HandleSessionUpdate;

            Log.Info("Initializing ACP connection to Gemini CLI");
            await _acpClient.InitializeAsync("unity-agent-bridge", "1.0.0");

            var projectPath = GetProjectPath();
            var session = await _acpClient.NewSessionAsync(projectPath);
            _sessionId = session.SessionId;
            _isFirstPromptInSession = true;

            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                ConversationManager.SetSessionId(_sessionId);
                Log.Info($"Gemini CLI session created: {_sessionId}");
            });
        }

        /// <summary>
        /// Handle ACP session/update notifications by converting them to ConversationMessages.
        /// Called from the ACP receive loop background thread — all work is dispatched
        /// to the main thread since both the parser and ConversationManager use Unity APIs.
        /// </summary>
        private void HandleSessionUpdate(SessionUpdateParams update)
        {
            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                // Ignore updates from stale sessions
                if (update.SessionId != null && update.SessionId != _sessionId)
                {
                    return;
                }

                var conversationInfo = AcpParser.GetConversationContentFromAcp(update);
                if (conversationInfo == null || !conversationInfo.HasContent)
                {
                    return;
                }

                var message = new ConversationMessage
                {
                    MessageId = conversationInfo.MessageId ?? Guid.NewGuid().ToString(),
                    MessageType = conversationInfo.MessageType,
                    Content = conversationInfo.Content,
                    ToolName = conversationInfo.ToolName ?? string.Empty,
                    Method = conversationInfo.Method ?? string.Empty,
                    Target = conversationInfo.Target ?? string.Empty,
                    Rationale = conversationInfo.Rationale ?? string.Empty,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    IsStreaming = conversationInfo.IsStreaming,
                    IsDelta = conversationInfo.IsDelta
                };

                ConversationManager.AddMessage(message);
            });
        }

        private string GetGeminiExecutable()
        {
            return ResolveExecutable(GeminiCliSettings.ExecutablePath.Value, "gemini");
        }

        /// <summary>
        /// Get the Unity project root path for the ACP session working directory.
        /// </summary>
        private static string GetProjectPath()
        {
            // Application.dataPath returns "ProjectPath/Assets", go up one level
            var assetsPath = Application.dataPath;
            return Directory.GetParent(assetsPath)?.FullName ?? assetsPath;
        }

        /// <summary>
        /// Dispose of resources used by the service.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _executionSemaphore?.Dispose();

                if (_acpClient != null)
                {
                    _acpClient.OnSessionUpdate -= HandleSessionUpdate;
                    _acpClient.Dispose();
                    _acpClient = null;
                }

                _sessionId = null;
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        #region IServiceSettingsUI Implementation

        /// <summary>
        /// Render Gemini CLI-specific settings UI (simple version for 3P compatibility).
        /// </summary>
        public void DrawSettingsUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Gemini CLI Settings", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.LabelField("Executable Path", GeminiCliSettings.ExecutablePath.Value);
        }

        /// <summary>
        /// Render Gemini CLI-specific settings UI (full version with Meta settings infrastructure).
        /// </summary>
        public void DrawSettingsUI(Origins origins, IIdentified originData)
        {
            UnityEditor.EditorGUILayout.LabelField("Gemini CLI Settings", UnityEditor.EditorStyles.boldLabel);
            GeminiCliSettings.ExecutablePath.DrawForGUI(origins, originData);
        }

        /// <summary>
        /// Reset Gemini CLI settings to defaults.
        /// </summary>
        public void ResetSettingsToDefaults()
        {
            GeminiCliSettings.ExecutablePath.Reset();
            ClearResolvedExecutablePath();
        }

        #endregion

        #region IServiceValidation Implementation

        /// <inheritdoc />
        public async Task<ValidationResult> ValidateConfigurationAsync()
        {
            _currentValidationResult = ValidationResult.Validating();

            try
            {
                var geminiExecutable = GetGeminiExecutable();

                // Check if the executable exists (if a specific path is provided)
                var configuredPath = GeminiCliSettings.ExecutablePath.Value;
                if (!string.IsNullOrEmpty(configuredPath) && !File.Exists(configuredPath))
                {
                    _currentValidationResult = ValidationResult.Invalid($"Executable not found: {configuredPath}");
                    return _currentValidationResult;
                }

                // Try to run "gemini --version" to verify the CLI is available
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = geminiExecutable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                processStartInfo.ArgumentList.Add("--version");

                // Set login shell PATH so the executable and its dependencies are found
                ApplyLoginShellPath(processStartInfo);

                using var process = new Process { StartInfo = processStartInfo };
                var outputBuilder = new StringBuilder();

                process.Start();

                var readTask = Task.Run(() =>
                {
                    try
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        if (!string.IsNullOrEmpty(output))
                        {
                            outputBuilder.Append(output);
                        }
                    }
                    catch
                    {
                        // Ignore read errors
                    }
                });

                var waitTask = Task.Run(() =>
                {
                    try
                    {
                        return process.WaitForExit(5000);
                    }
                    catch
                    {
                        return false;
                    }
                });

                var completed = await waitTask;

                if (!completed)
                {
                    try { process.Kill(); }
                    catch (InvalidOperationException) { }
                    _currentValidationResult = ValidationResult.Error("Gemini CLI timed out");
                    return _currentValidationResult;
                }

                // Wait a short time for read task to complete after process exits
                await Task.WhenAny(readTask, Task.Delay(500));

                if (process.ExitCode == 0)
                {
                    var version = outputBuilder.ToString().Trim();
                    if (string.IsNullOrEmpty(version))
                    {
                        version = "unknown version";
                    }
                    _currentValidationResult = ValidationResult.Valid($"Gemini CLI available ({version})");
                }
                else
                {
                    _currentValidationResult = ValidationResult.Invalid("Gemini CLI not found or not configured");
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _currentValidationResult = ValidationResult.Invalid("Gemini CLI not found. Install Gemini CLI or set the executable path.");
            }
            catch (Exception ex)
            {
                _currentValidationResult = ValidationResult.Error($"Validation error: {ex.Message}");
            }

            return _currentValidationResult;
        }

        #endregion
    }
}
