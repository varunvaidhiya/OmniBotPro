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

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// UI verification tools for MCP clients to capture and inspect Unity Editor windows.
    ///
    /// This tool enables AI agents like Devmate to "see" the Unity Editor by capturing
    /// screenshots of Editor windows for visual comparison against Figma designs.
    ///
    /// Key Features:
    /// - List all open Editor windows with type, title, position, size
    /// - Open specific Editor windows by type name
    /// - Capture window screenshots as base64 PNG
    /// - Resize windows to specific dimensions for consistent captures
    /// - Simulate basic interactions for testing interactive states
    ///
    /// Use Cases:
    /// - Verify UI code changes match Figma designs
    /// - Capture hover/selected states for visual testing
    /// - Ensure UI consistency across Editor windows
    ///
    /// MCP Client Usage Pattern:
    /// 1. OpenWindow("BuildingBlocksWindow")
    /// 2. ResizeWindow("BuildingBlocksWindow", 1200, 800) to match Figma frame
    /// 3. CaptureWindow("BuildingBlocksWindow") → base64 PNG
    /// 4. AI Vision compares screenshot to Figma reference
    /// </summary>
    [Tool(
        "Tools for capturing and inspecting Unity Editor windows for visual verification.",
        "WHEN TO USE: After UI code changes, to verify Editor windows match Figma designs.",
        "WORKFLOW: 1) OpenWindow() 2) ResizeWindow() 3) CaptureWindow() 4) AI Vision comparison.",
        "IMPORTANT: Works with already-running Editor. Captures IMGUI and UI Toolkit windows."
    )]
    internal class UIVerificationTools : SingletonService<UIVerificationTools>
    {
        /// <summary>
        /// Information about an open Editor window.
        /// </summary>
        internal class WindowInfo
        {
            public string TypeName { get; set; }
            public string Title { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public bool IsFocused { get; set; }
            public bool IsDocked { get; set; }
        }

        [Tool(Description = "List all currently open Editor windows with their type, title, position, and size",
            Returns = "Array of window objects with typeName, title, x, y, width, height, isFocused, isDocked")]
        internal object ListOpenWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var focusedWindow = EditorWindow.focusedWindow;

            var windowInfos = windows
                .Where(w => w != null)
                .Select(w => new
                {
                    typeName = w.GetType().Name,
                    fullTypeName = w.GetType().FullName,
                    title = w.titleContent?.text ?? "",
                    x = w.position.x,
                    y = w.position.y,
                    width = w.position.width,
                    height = w.position.height,
                    isFocused = w == focusedWindow,
                    isDocked = w.docked
                })
                .OrderBy(w => w.typeName)
                .ToArray();

            return new
            {
                count = windowInfos.Length,
                windows = windowInfos
            };
        }

        [Tool(Description = "Open an Editor window by type name. Creates or focuses the window.",
            Returns = "JSON object confirming the window was opened with its properties")]
        internal object OpenWindow(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return new { error = "typeName is required" };
            }

            var windowType = FindWindowType(typeName);
            if (windowType == null)
            {
                return new
                {
                    error = $"Window type '{typeName}' not found",
                    hint = "Use ListOpenWindows() to see available window types"
                };
            }

            try
            {
                var window = EditorWindow.GetWindow(windowType);
                window.Focus();
                window.Repaint();

                return new
                {
                    opened = true,
                    typeName = window.GetType().Name,
                    title = window.titleContent?.text ?? "",
                    x = window.position.x,
                    y = window.position.y,
                    width = window.position.width,
                    height = window.position.height
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to open window: {ex.Message}",
                    typeName
                };
            }
        }

        [Tool(Description = "Capture a screenshot of an Editor window as base64-encoded PNG",
            Returns = "JSON object with base64Png data, width, height, and typeName")]
        internal async Task<object> CaptureWindow(string typeName = null, int? targetWidth = null, int? targetHeight = null)
        {
            EditorWindow window;

            if (string.IsNullOrEmpty(typeName))
            {
                window = EditorWindow.focusedWindow;
                if (window == null)
                {
                    return new { error = "No focused window and typeName not specified" };
                }
            }
            else
            {
                window = FindWindowByTypeName(typeName);
                if (window == null)
                {
                    return new
                    {
                        error = $"Window '{typeName}' not found",
                        hint = "Use ListOpenWindows() to see open windows, or OpenWindow() to open it"
                    };
                }
            }

            if (targetWidth.HasValue && targetHeight.HasValue)
            {
                var currentPos = window.position;
                window.position = new Rect(currentPos.x, currentPos.y, targetWidth.Value, targetHeight.Value);
            }

            window.Focus();
            window.Repaint();

            await Task.Delay(100);

            try
            {
                var pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
                var windowRect = window.position;

                var captureWidth = (int)(windowRect.width * pixelsPerPoint);
                var captureHeight = (int)(windowRect.height * pixelsPerPoint);

                var screenX = (int)(windowRect.x * pixelsPerPoint);
                var screenY = (int)(windowRect.y * pixelsPerPoint);

                Color[] pixels;

                try
                {
                    pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(
                        new Vector2(screenX, screenY),
                        captureWidth,
                        captureHeight);
                }
                catch
                {
                    return new
                    {
                        error = "Failed to capture screen pixels. Window may be obscured or minimized.",
                        typeName = window.GetType().Name,
                        suggestion = "Ensure the window is visible and not covered by other windows"
                    };
                }

                var texture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels);
                texture.Apply();

                var pngBytes = texture.EncodeToPNG();
                var base64Png = Convert.ToBase64String(pngBytes);

                UnityEngine.Object.DestroyImmediate(texture);

                return new
                {
                    base64Png,
                    width = captureWidth,
                    height = captureHeight,
                    logicalWidth = windowRect.width,
                    logicalHeight = windowRect.height,
                    dpiScale = pixelsPerPoint,
                    typeName = window.GetType().Name,
                    title = window.titleContent?.text ?? ""
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to capture window: {ex.Message}",
                    typeName = window.GetType().Name
                };
            }
        }

        [Tool(Description = "Resize an Editor window to specific dimensions",
            Returns = "JSON object confirming the resize with new dimensions")]
        internal object ResizeWindow(string typeName, int width, int height)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return new { error = "typeName is required" };
            }

            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new
                {
                    error = $"Window '{typeName}' not found",
                    hint = "Use OpenWindow() to open it first"
                };
            }

            var currentPos = window.position;
            window.position = new Rect(currentPos.x, currentPos.y, width, height);
            window.Repaint();

            return new
            {
                resized = true,
                typeName = window.GetType().Name,
                width = window.position.width,
                height = window.position.height
            };
        }

        [Tool(Description = "Focus an Editor window and bring it to the front",
            Returns = "JSON object confirming the window is focused")]
        internal object FocusWindow(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return new { error = "typeName is required" };
            }

            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new
                {
                    error = $"Window '{typeName}' not found",
                    hint = "Use OpenWindow() to open it first"
                };
            }

            window.Focus();
            window.Repaint();

            return new
            {
                focused = true,
                typeName = window.GetType().Name,
                title = window.titleContent?.text ?? ""
            };
        }

        [Tool(Description = "Set the Editor theme to dark or light mode",
            Returns = "JSON object confirming the theme change")]
        internal object SetEditorTheme(bool dark)
        {
            try
            {
                // EditorGUIUtility.isProSkin is read-only; theme must be changed via EditorPrefs
                // The user must change the theme manually via Edit > Preferences > General > Editor Theme
                // We can only report the current state
                bool currentTheme = EditorGUIUtility.isProSkin;

                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    window.Repaint();
                }

                return new
                {
                    requestedTheme = dark ? "dark" : "light",
                    currentTheme = currentTheme ? "dark" : "light",
                    applied = currentTheme == dark,
                    message = currentTheme == dark
                        ? "Theme already matches requested value"
                        : "Theme cannot be changed programmatically. Please change via Edit > Preferences > General > Editor Theme"
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to set theme: {ex.Message}",
                    note = "Theme setting may require Unity Pro license"
                };
            }
        }

        [Tool(Description = "Get information about a specific Editor window",
            Returns = "JSON object with detailed window information")]
        internal object GetWindowInfo(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return new { error = "typeName is required" };
            }

            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new
                {
                    error = $"Window '{typeName}' not found",
                    hint = "Use ListOpenWindows() to see available windows"
                };
            }

            var focusedWindow = EditorWindow.focusedWindow;

            return new
            {
                typeName = window.GetType().Name,
                fullTypeName = window.GetType().FullName,
                title = window.titleContent?.text ?? "",
                x = window.position.x,
                y = window.position.y,
                width = window.position.width,
                height = window.position.height,
                isFocused = window == focusedWindow,
                isDocked = window.docked,
                hasUnsavedChanges = window.hasUnsavedChanges,
                minSize = new { width = window.minSize.x, height = window.minSize.y },
                maxSize = new { width = window.maxSize.x, height = window.maxSize.y },
                dpiScale = EditorGUIUtility.pixelsPerPoint
            };
        }

        [Tool(Description = "Close an Editor window",
            Returns = "JSON object confirming the window was closed")]
        internal object CloseWindow(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return new { error = "typeName is required" };
            }

            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new
                {
                    error = $"Window '{typeName}' not found",
                    alreadyClosed = true
                };
            }

            var closedTypeName = window.GetType().Name;
            window.Close();

            return new
            {
                closed = true,
                typeName = closedTypeName
            };
        }

        [Tool(Description = "Repaint all Editor windows to ensure UI is up to date",
            Returns = "JSON object confirming repaint was triggered")]
        internal object RepaintAllWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var count = 0;

            foreach (var window in windows)
            {
                if (window != null)
                {
                    window.Repaint();
                    count++;
                }
            }

            return new
            {
                repainted = true,
                windowCount = count
            };
        }

        private static Type FindWindowType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null && typeof(EditorWindow).IsAssignableFrom(type))
                    {
                        return type;
                    }

                    var matchingType = assembly.GetTypes()
                        .FirstOrDefault(t =>
                            typeof(EditorWindow).IsAssignableFrom(t) &&
                            (t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                             t.FullName?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true));

                    if (matchingType != null)
                    {
                        return matchingType;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static EditorWindow FindWindowByTypeName(string typeName)
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            return windows.FirstOrDefault(w =>
                w != null &&
                (w.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                 w.GetType().FullName?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true));
        }
    }
}
