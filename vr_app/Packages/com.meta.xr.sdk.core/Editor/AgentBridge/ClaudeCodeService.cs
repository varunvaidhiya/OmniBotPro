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
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Service for handling Claude Code CLI integration in Unity Editor.
    /// Ported from MCPProxy to Unity.
    /// </summary>
    [RegisterAIService(ServiceId, "Claude Code", Priority = 0)]
    public class ClaudeCodeService : AIServiceBase, IServiceSettingsUI, IServiceValidation
    {
        /// <summary>
        /// The unique service identifier for Claude Code.
        /// </summary>
        public const string ServiceId = "claudecode";

        private const int ValidationTimeoutSeconds = 10;
        /// <summary>
        /// Settings specific to Claude Code service.
        /// Co-located with service implementation for better maintainability.
        /// </summary>
        public static class ClaudeCodeSettings
        {
            private static readonly IIdentified Owner = new ClaudeCodeDescriptor();

            internal static readonly UserString ExecutablePath = new UserString
            {
                Uid = nameof(ExecutablePath),
                Owner = Owner,
                Default = "",
                Label = "Claude Code Executable Path",
                Tooltip = "Optional path to Claude Code executable. Leave empty to use PATH environment variable.",
                SendTelemetry = false
            };

            private class ClaudeCodeDescriptor : IIdentified
            {
                public string Id => "AgentBridge.ClaudeCode";
            }
        }

        private readonly SemaphoreSlim _executionSemaphore = new(1, 1); // Only allow one execution at a time
        private Process? _currentProcess = null;
        private readonly object _processLock = new object();
        private bool _cancellationRequested = false;
        private bool _disposed = false;
        private ValidationResult _currentValidationResult = ValidationResult.Unknown();

        public override string ServiceName => "Claude Code";

        public override bool HasActiveSession => !string.IsNullOrEmpty(ConversationManager.GetSessionId());

        /// <inheritdoc />
        public ValidationResult CurrentValidationResult => _currentValidationResult;

        /// <summary>
        /// Constructor - performs startup cleanup of old temp files.
        /// </summary>
        public ClaudeCodeService()
        {
            CleanupOldTempFiles();
        }

        /// <summary>
        /// Clean up old temporary image files from previous sessions.
        /// This prevents temp file accumulation after crashes or abnormal termination.
        /// </summary>
        private void CleanupOldTempFiles()
        {
            try
            {
                var tempDir = Application.temporaryCachePath;
                if (!Directory.Exists(tempDir))
                {
                    return;
                }

                // Find all temp files matching our pattern: claude_image_*.{png,jpg,gif,webp}
                var patterns = new[] { "claude_image_*.png", "claude_image_*.jpg", "claude_image_*.gif", "claude_image_*.webp" };
                var oldFiles = new List<string>();

                foreach (var pattern in patterns)
                {
                    var files = Directory.GetFiles(tempDir, pattern);
                    oldFiles.AddRange(files);
                }

                if (oldFiles.Count > 0)
                {
                    Log.Info($"Cleaning up {oldFiles.Count} old temp image file(s)");

                    foreach (var file in oldFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Failed to delete old temp file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error during temp file cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Process user input through Claude Code CLI.
        /// </summary>
        public override async Task ProcessUserInputAsync(string userInput, CallerIdentity? caller, List<ImageAttachment>? images = null, CancellationToken cancellationToken = default, string? systemPrompt = null)
        {
            // Wait for any previous execution to complete
            await _executionSemaphore.WaitAsync(cancellationToken);
            try
            {
                Log.Info($"Processing user input through Claude Code: {userInput}");
                if (images != null && images.Count > 0)
                {
                    Log.Info($"Processing with {images.Count} image(s)");
                }

                ConversationManager.IsActive = true;
                ConversationManager.ClearError();

                // Add the user's input to the conversation history
                ConversationManager.AddMessage("user", userInput, caller: caller);

                await ExecuteClaudeCodeAsync(userInput, images, systemPrompt, caller);

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
        /// Clear the current session to start fresh.
        /// </summary>
        public override void ClearSession()
        {
            Log.Info("Clearing Claude Code session");
            ConversationManager.Clear();
        }

        /// <summary>
        /// Cancel the current operation if one is in progress.
        /// </summary>
        public override async Task CancelCurrentOperationAsync()
        {
            Process? processToKill = null;

            lock (_processLock)
            {
                if (_currentProcess != null && !_currentProcess.HasExited)
                {
                    processToKill = _currentProcess;
                    _cancellationRequested = true;
                }
            }

            if (processToKill != null)
            {
                try
                {
                    Log.Info("Cancelling Claude Code operation");
                    processToKill.Kill();
                    await Task.Run(() => processToKill.WaitForExit(5000));
                }
                catch (Exception ex)
                {
                    Log.Warning($"Error killing Claude Code process: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Execute Claude Code CLI with the given user input and optional images.
        /// </summary>
        private async Task ExecuteClaudeCodeAsync(string userInput, List<ImageAttachment>? images, string? systemPrompt, CallerIdentity? caller)
        {
            var currentSessionId = ConversationManager.GetSessionIdForCaller(caller);
            Log.Info($"Executing Claude Code (session: {currentSessionId ?? "new"}, caller: {caller?.Id ?? "default"})");

            // Create process start info with ArgumentList (safe from injection)
            // We redirect stdin to pass the user prompt, avoiding CLI argument parsing issues
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Add arguments individually (no escaping needed!)
            processStartInfo.ArgumentList.Add("-p"); // non-interactive mode
            processStartInfo.ArgumentList.Add("--output-format");
            processStartInfo.ArgumentList.Add("stream-json");
            processStartInfo.ArgumentList.Add("--include-partial-messages");
            processStartInfo.ArgumentList.Add("--verbose");
            processStartInfo.ArgumentList.Add("--dangerously-skip-permissions");

            var newSession = string.IsNullOrEmpty(currentSessionId);

            // Include system prompt only when starting a new session
            if (newSession && systemPrompt is not null)
            {
                processStartInfo.ArgumentList.Add("--system-prompt");
                processStartInfo.ArgumentList.Add(systemPrompt);
            }

            // If we have an existing session, resume it
            if (!newSession)
            {
                processStartInfo.ArgumentList.Add("--resume");
                processStartInfo.ArgumentList.Add(currentSessionId);
            }

            // Handle images if provided - save to temp files
            var tempImageFiles = new List<string>();
            if (images != null && images.Count > 0)
            {
                try
                {
                    var tempDir = Application.temporaryCachePath;
                    for (int i = 0; i < images.Count; i++)
                    {
                        var image = images[i];
                        if (string.IsNullOrEmpty(image.Data))
                        {
                            Log.Warning($"Image {i} has no data, skipping");
                            continue;
                        }

                        // Determine file extension from media type
                        var extension = ".png"; // default
                        if (!string.IsNullOrEmpty(image.MediaType))
                        {
                            if (image.MediaType.Contains("jpeg") || image.MediaType.Contains("jpg"))
                                extension = ".jpg";
                            else if (image.MediaType.Contains("png"))
                                extension = ".png";
                            else if (image.MediaType.Contains("gif"))
                                extension = ".gif";
                            else if (image.MediaType.Contains("webp"))
                                extension = ".webp";
                        }

                        // Create temp file
                        var tempFileName = $"claude_image_{Guid.NewGuid()}{extension}";
                        var tempFilePath = Path.Combine(tempDir, tempFileName);

                        // Decode base64 and write to file
                        var imageBytes = Convert.FromBase64String(image.Data);
                        File.WriteAllBytes(tempFilePath, imageBytes);
                        tempImageFiles.Add(tempFilePath);

                        Log.Info($"Saved image {i} to: {tempFilePath} ({imageBytes.Length} bytes)");
                    }

                    // Add image arguments
                    foreach (var imagePath in tempImageFiles)
                    {
                        processStartInfo.ArgumentList.Add("--image");
                        processStartInfo.ArgumentList.Add(imagePath);
                    }

                    Log.Info($"Added {tempImageFiles.Count} image(s) to command");
                }
                catch (Exception imgEx)
                {
                    Log.Error($"Failed to process images: {imgEx.Message}");
                    // Clean up temp files
                    foreach (var tempFile in tempImageFiles)
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                    tempImageFiles.Clear();
                }
            }

            // User input will be passed via stdin (not as an argument)
            // This avoids CLI argument parsing issues with special characters

            Log.Info($"Executing: claude with {processStartInfo.ArgumentList.Count} arguments");

            // Resolve executable via login shell if needed (Unity on macOS doesn't inherit shell PATH)
            processStartInfo.FileName = ResolveExecutable(ClaudeCodeSettings.ExecutablePath.Value, "claude");

            // Set login shell PATH so auth helpers (e.g. apiKeyHelper) and dependencies are found
            ApplyLoginShellPath(processStartInfo);

            using var process = new Process();
            process.StartInfo = processStartInfo;

            // Store current process for cancellation
            lock (_processLock)
            {
                _currentProcess = process;
                _cancellationRequested = false;
            }

            // Set up output handlers - capture caller in closure
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ProcessOutputLine(e.Data, caller);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Warning($"Claude Code stderr: {e.Data}");
                }
            };

            try
            {
                process.Start();

                // Write user input to stdin (handles special characters and multiline content safely)
                await process.StandardInput.WriteAsync(userInput);
                process.StandardInput.Close();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for process to complete with timeout
                var timeout = TimeSpan.FromMinutes(10); // Configurable timeout
                var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

                if (!completed)
                {
                    Log.Error("Process timed out");
                    process.Kill();
                    throw new TimeoutException($"Claude Code process exceeded {timeout.TotalMinutes} minute timeout");
                }

                // Caching the exit code for later use. This is needed because the process is disposed after this method.
                var exitCode = process.ExitCode;
                Log.Info($"Claude Code exited with code: {exitCode}");

                if (exitCode != 0 && !_cancellationRequested)
                {
                    MainThreadDispatcher.ExecuteOnMainThread(() =>
                    {
                        ConversationManager.SetError($"Claude Code exited with code {exitCode}");
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception running Claude Code: {ex.Message}");
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    ConversationManager.SetError($"Exception: {ex.Message}");
                });
            }
            finally
            {
                // Clean up temp image files
                foreach (var tempFile in tempImageFiles)
                {
                    try
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Failed to delete temp file {tempFile}: {ex.Message}");
                    }
                }

                lock (_processLock)
                {
                    _currentProcess = null;
                }
            }
        }

        /// <summary>
        /// Process a single line of output from Claude Code.
        /// </summary>
        private void ProcessOutputLine(string line, CallerIdentity? caller)
        {
            try
            {
                // Extract session ID from output
                if (line.Contains("\"session_id\":"))
                {
                    ExtractAndStoreSessionId(line, caller);
                }

                // Parse conversation content
                var conversationInfo = GetConversationContent(line);
                if (conversationInfo.HasContent)
                {
                    // Update conversation on main thread
                    MainThreadDispatcher.ExecuteOnMainThread(() =>
                    {
                        var message = new ConversationMessage
                        {
                            MessageId = conversationInfo.MessageId ?? Guid.NewGuid().ToString(),
                            MessageType = conversationInfo.MessageType,
                            Content = conversationInfo.Content,
                            ToolName = conversationInfo.ToolName ?? string.Empty,
                            Method = conversationInfo.Method ?? string.Empty,
                            Target = conversationInfo.Target ?? string.Empty,
                            Rationale = conversationInfo.Rationale ?? string.Empty,
                            CallerId = caller?.Id ?? string.Empty,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            IsStreaming = conversationInfo.IsStreaming,
                            IsDelta = conversationInfo.IsDelta
                        };

                        ConversationManager.AddMessage(message);
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error processing output line: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract and store session ID from Claude Code output for the given caller.
        /// </summary>
        private void ExtractAndStoreSessionId(string line, CallerIdentity? caller)
        {
            try
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(line);
                var sessionId = json["session_id"]?.ToString();

                if (!string.IsNullOrEmpty(sessionId))
                {
                    MainThreadDispatcher.ExecuteOnMainThread(() =>
                    {
                        ConversationManager.SetSessionIdForCaller(caller, sessionId!);
                        Log.Info($"Session ID for caller '{caller?.Id ?? "default"}': {sessionId}");
                    });
                }
            }
            catch
            {
                // Ignore parsing errors for session ID extraction
            }
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

                lock (_processLock)
                {
                    if (_currentProcess != null && !_currentProcess.HasExited)
                    {
                        try
                        {
                            _currentProcess.Kill();
                            _currentProcess.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Error disposing process: {ex.Message}");
                        }
                    }
                    _currentProcess = null;
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        #region IServiceSettingsUI Implementation

        /// <summary>
        /// Render Claude Code-specific settings UI (simple version for 3P compatibility).
        /// </summary>
        public void DrawSettingsUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Claude Code Settings", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.LabelField("Executable Path", ClaudeCodeSettings.ExecutablePath.Value);
        }

        /// <summary>
        /// Render Claude Code-specific settings UI (full version with Meta settings infrastructure).
        /// </summary>
        public void DrawSettingsUI(Origins origins, IIdentified originData)
        {
            UnityEditor.EditorGUILayout.LabelField("Claude Code Settings", UnityEditor.EditorStyles.boldLabel);
            ClaudeCodeSettings.ExecutablePath.DrawForGUI(origins, originData);
        }

        /// <summary>
        /// Reset Claude Code settings to defaults.
        /// </summary>
        public void ResetSettingsToDefaults()
        {
            ClaudeCodeSettings.ExecutablePath.Reset();
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
                var configuredPath = ClaudeCodeSettings.ExecutablePath.Value;

                // Check if the executable exists (if a specific path is provided)
                if (!string.IsNullOrEmpty(configuredPath) && !File.Exists(configuredPath))
                {
                    _currentValidationResult = ValidationResult.Invalid($"Executable not found: {configuredPath}");
                    return _currentValidationResult;
                }

                // Try to run "claude --version" to verify the CLI is available
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ResolveExecutable(configuredPath, "claude"),
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

                // Use synchronous reading with timeout to avoid async output handler hanging issues.
                // The BeginOutputReadLine() + WaitForExit(timeout) pattern can hang indefinitely
                // because async handlers may still be waiting after the timed wait returns.
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
                        return process.WaitForExit(ValidationTimeoutSeconds * 1000);
                    }
                    catch
                    {
                        return false;
                    }
                });

                var completed = await waitTask;

                if (!completed)
                {
                    try { process.Kill(); } catch { }
                    _currentValidationResult = ValidationResult.Error("Claude CLI timed out");
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
                    _currentValidationResult = ValidationResult.Valid($"Claude CLI available ({version})");
                }
                else
                {
                    _currentValidationResult = ValidationResult.Invalid("Claude CLI not found or not configured");
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _currentValidationResult = ValidationResult.Invalid("Claude CLI not found. Install Claude Code or set the executable path.");
            }
            catch (Exception ex)
            {
                _currentValidationResult = ValidationResult.Error($"Validation error: {ex.Message}");
            }

            return _currentValidationResult;
        }

        #endregion

        #region Claude Code CLI Parser

        /// <summary>
        /// Detects and extracts conversation content from stream output.
        /// </summary>
        /// <param name="line">The output line to check</param>
        /// <param name="logger">Optional logger for debug messages</param>
        /// <returns>Conversation info with content and message type</returns>
        internal static ConversationStreamParser.ConversationInfo GetConversationContent(string line, Action<string>? logger = null)
        {
            var result = new ConversationStreamParser.ConversationInfo();

            try
            {
                // Check if this is a JSON line that could contain conversational content
                if (!line.Trim().StartsWith("{"))
                    return result;

                var json = JObject.Parse(line);

                // Method 1: Look for assistant messages with tool_use
                if (json["type"]?.ToString() == "assistant")
                {
                    var messageId = json["message"]?["id"]?.ToString();
                    var messageContent = json["message"]?["content"];
                    if (messageContent is JArray contentArray && contentArray.Count > 0)
                    {
                        var firstContent = contentArray[0];

                        // Check for tool_use
                        if (firstContent?["type"]?.ToString() == "tool_use")
                        {
                            var fullToolName = firstContent["name"]?.ToString();

                            // Extract the actual tool name from MCP format (mcp__server-name__ToolName)
                            var toolName = fullToolName;
                            if (!string.IsNullOrEmpty(fullToolName) && fullToolName != null && fullToolName.Contains("__"))
                            {
                                // Get everything after the last "__"
                                var parts = fullToolName.Split(new[] { "__" }, StringSplitOptions.None);
                                if (parts.Length > 0)
                                {
                                    toolName = parts[parts.Length - 1]; // Get the last part
                                }
                            }

                            var toolInputStr = firstContent["input"]?.ToString();

                            // Try to parse the input as JSON to extract rationale, method, target, and other fields
                            string? rationale = null;
                            string? method = null;
                            string? target = null;
                            JObject? inputJson = null;
                            try
                            {
                                if (!string.IsNullOrEmpty(toolInputStr) && toolInputStr != null)
                                {
                                    inputJson = JObject.Parse(toolInputStr);
                                    rationale = inputJson["rationale"]?.ToString();
                                    method = inputJson["method"]?.ToString();
                                    target = inputJson["target"]?.ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                logger?.Invoke($"Could not parse tool input as JSON: {ex.Message}");
                            }

                            result.HasContent = true;
                            result.MessageType = "tool_use";
                            result.ToolName = toolName;
                            result.Method = method;
                            result.Target = target;
                            result.Rationale = rationale;
                            result.AdditionalData = inputJson;

                            // Build content string with optional method and target
                            var toolDisplay = !string.IsNullOrEmpty(method)
                                ? $"{toolName}/{method}"
                                : toolName;

                            // Format: [ToolName/Method] (Target) rationale
                            var formattedContent = $"[{toolDisplay}]";

                            // Add target in parentheses if present
                            if (!string.IsNullOrEmpty(target))
                            {
                                formattedContent += $" ({target})";
                            }

                            // Add rationale if present
                            if (!string.IsNullOrEmpty(rationale))
                            {
                                formattedContent += $" {rationale}";
                            }

                            result.Content = formattedContent;
                            return result;
                        }

                        // Check for regular text (thinking)
                        if (firstContent?["type"]?.ToString() == "text")
                        {
                            var textContent = firstContent["text"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(textContent))
                            {
                                result.HasContent = true;
                                result.MessageType = "assistant";
                                result.Content = textContent ?? string.Empty;
                                result.MessageId = messageId;
                                result.IsStreaming = false;
                                return result;
                            }
                        }
                    }
                }

                // Method 2: Look for user messages (including tool_result)
                if (json["type"]?.ToString() == "user")
                {
                    var messageContent = json["message"]?["content"];
                    if (messageContent is JArray contentArray && contentArray.Count > 0)
                    {
                        var firstContent = contentArray[0];

                        // Check for tool_result
                        if (firstContent?["type"]?.ToString() == "tool_result")
                        {
                            var toolUseId = firstContent["tool_use_id"]?.ToString();
                            var content = firstContent["content"]?.ToString();

                            logger?.Invoke($"TOOL_RESULT detected - ID: {toolUseId}, Content: {content}");

                            result.HasContent = true;
                            result.MessageType = "tool_result";
                            // Pass the actual content so ConversationManager can check for errors
                            result.Content = !string.IsNullOrEmpty(content) ? content! : "Tool completed";
                            return result;
                        }

                        // Check for plain text user message (server echo)
                        // ConversationManager will deduplicate if already added locally
                        if (firstContent?["type"]?.ToString() == "text")
                        {
                            var textContent = firstContent["text"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(textContent))
                            {
                                var messageId = json["message"]?["id"]?.ToString();

                                result.HasContent = true;
                                result.MessageType = "user";
                                result.Content = textContent ?? string.Empty;
                                result.MessageId = messageId;
                                result.IsStreaming = false;
                                return result;
                            }
                        }
                    }
                }

                // Method 3: Look for stream events with text content (stream-json format)
                if (json["type"]?.ToString() == "stream_event")
                {
                    var sessionId = json["session_id"]?.ToString();
                    var eventData = json["event"];
                    var eventType = eventData?["type"]?.ToString();

                    // Handle content_block_delta - streaming text (Claude Code sends DELTA text)
                    if (eventType == "content_block_delta")
                    {
                        var delta = eventData?["delta"];
                        if (delta?["type"]?.ToString() == "text_delta")
                        {
                            var text = delta["text"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                // Use session_id + index as message ID for streaming
                                var index = eventData?["index"]?.ToString() ?? "0";
                                var messageId = $"stream_{sessionId}_{index}";

                                result.HasContent = true;
                                result.MessageType = "thinking";
                                result.Content = text ?? string.Empty;
                                result.MessageId = messageId;
                                result.IsStreaming = true;
                                result.IsDelta = true; // Claude Code sends delta text, needs appending
                                return result;
                            }
                        }
                    }

                    // Handle message_stop - marks end of streaming, convert streaming message to final
                    if (eventType == "message_stop")
                    {
                        // Signal that streaming is complete (no content to add)
                        result.HasContent = false;
                        return result;
                    }
                }

                // Method 4: Look for partial messages (--include-partial-messages format)
                // These contain the full accumulated text (not deltas). Use a stable MessageId
                // matching the stream so they merge with the existing streaming entry instead
                // of creating duplicates.
                if (json["type"]?.ToString() == "partial_message")
                {
                    var content = json["content"];
                    if (content is JArray contentArray && contentArray.Count > 0)
                    {
                        var firstContent = contentArray[0];
                        if (firstContent?["type"]?.ToString() == "text")
                        {
                            var textContent = firstContent["text"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(textContent))
                            {
                                var sessionId = json["session_id"]?.ToString() ?? "partial";

                                result.HasContent = true;
                                result.MessageType = "thinking";
                                result.Content = textContent ?? string.Empty;
                                result.MessageId = $"stream_{sessionId}_0";
                                result.IsStreaming = true;
                                result.IsDelta = false; // Full text, not delta
                                return result;
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger?.Invoke($"Error parsing potential conversational content: {ex.Message}");
                return result;
            }
        }

        #endregion
    }
}
