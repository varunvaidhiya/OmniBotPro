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
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.MCPBridge.Editor
{
    /// <summary>
    /// Interaction testing tools for MCP clients to programmatically drive Editor window UI.
    ///
    /// This tool enables AI agents like Devmate to click buttons, navigate pages, type in
    /// search bars, and verify behavior after each action. Combined with UIVerificationTools,
    /// it enables end-to-end interaction testing of Editor windows.
    ///
    /// Three Interaction Tiers:
    /// 1. Coordinate-Based (Tier 1): Works for ALL windows including IMGUI. Uses normalized
    ///    coordinates (0.0-1.0) within the window. AI vision identifies element positions.
    /// 2. Semantic Element Query (Tier 2): UI Toolkit windows only. Uses USS selectors to
    ///    find and interact with elements by type, name, or class.
    /// 3. Direct Method Invocation (Tier 3): Precise control via reflection. Call methods
    ///    or get/set properties on the EditorWindow directly.
    ///
    /// MCP Client Usage Pattern:
    /// 1. CaptureWindow() → get screenshot
    /// 2. AI Vision: "I see a collection card at ~(0.25, 0.35)"
    /// 3. ClickAt("BuildingBlocksWindow", 0.25, 0.35)
    /// 4. WaitForRepaint(500)
    /// 5. CaptureWindow() → verify page transition
    /// </summary>
    [Tool(
        "Tools for programmatically interacting with Unity Editor window UI.",
        "WHEN TO USE: To test interactive behavior of Editor windows (clicks, typing, navigation).",
        "WORKFLOW: 1) CaptureWindow 2) AI identifies target 3) ClickAt/TypeText 4) WaitForRepaint 5) Verify.",
        "IMPORTANT: Supports IMGUI (coordinate-based) and UI Toolkit (semantic query) windows."
    )]
    internal class InteractionTestingTools : SingletonService<InteractionTestingTools>
    {
        #region Tier 1: Coordinate-Based Interactions

        [Tool(Description = "Click at normalized coordinates (0.0-1.0) within a window. Works for IMGUI and UI Toolkit windows.",
            Returns = "JSON object confirming the click with screen coordinates")]
        internal async Task<object> ClickAt(string typeName, float normalizedX, float normalizedY, int button = 0, int clickCount = 1)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            if (normalizedX < 0 || normalizedX > 1 || normalizedY < 0 || normalizedY > 1)
            {
                return new { error = "Normalized coordinates must be between 0.0 and 1.0" };
            }

            window.Focus();

            var localPos = new Vector2(
                normalizedX * window.position.width,
                normalizedY * window.position.height);

            SendMouseEvent(window, EventType.MouseDown, localPos, button, clickCount);
            await Task.Delay(50);
            SendMouseEvent(window, EventType.MouseUp, localPos, button, clickCount);

            window.Repaint();
            await Task.Delay(100);

            return new
            {
                clicked = true,
                typeName = window.GetType().Name,
                normalizedX,
                normalizedY,
                localX = localPos.x,
                localY = localPos.y,
                button,
                clickCount
            };
        }

        [Tool(Description = "Double-click at normalized coordinates within a window",
            Returns = "JSON object confirming the double-click")]
        internal async Task<object> DoubleClickAt(string typeName, float normalizedX, float normalizedY)
        {
            return await ClickAt(typeName, normalizedX, normalizedY, 0, 2);
        }

        [Tool(Description = "Hover at normalized coordinates and wait for hover effects to appear",
            Returns = "JSON object confirming the hover action")]
        internal async Task<object> HoverAt(string typeName, float normalizedX, float normalizedY, int dwellMs = 200)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            window.Focus();

            var localPos = new Vector2(
                normalizedX * window.position.width,
                normalizedY * window.position.height);

            SendMouseEvent(window, EventType.MouseMove, localPos, -1, 0);
            window.Repaint();

            await Task.Delay(dwellMs);

            return new
            {
                hovering = true,
                typeName = window.GetType().Name,
                normalizedX,
                normalizedY,
                localX = localPos.x,
                localY = localPos.y,
                dwellMs
            };
        }

        [Tool(Description = "Drag from one position to another within a window",
            Returns = "JSON object confirming the drag action")]
        internal async Task<object> DragFromTo(string typeName, float fromX, float fromY, float toX, float toY, int steps = 10)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            window.Focus();

            var fromPos = new Vector2(fromX * window.position.width, fromY * window.position.height);
            var toPos = new Vector2(toX * window.position.width, toY * window.position.height);

            SendMouseEvent(window, EventType.MouseDown, fromPos, 0, 1);

            for (int i = 1; i <= steps; i++)
            {
                var t = (float)i / steps;
                var pos = Vector2.Lerp(fromPos, toPos, t);
                SendMouseEvent(window, EventType.MouseDrag, pos, 0, 0);
                await Task.Delay(10);
            }

            SendMouseEvent(window, EventType.MouseUp, toPos, 0, 1);
            window.Repaint();

            return new
            {
                dragged = true,
                typeName = window.GetType().Name,
                from = new { x = fromX, y = fromY },
                to = new { x = toX, y = toY },
                steps
            };
        }

        [Tool(Description = "Scroll at a position within a window",
            Returns = "JSON object confirming the scroll action")]
        internal async Task<object> ScrollAt(string typeName, float normalizedX, float normalizedY, float deltaY)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            window.Focus();

            var localPos = new Vector2(
                normalizedX * window.position.width,
                normalizedY * window.position.height);

            var evt = new Event
            {
                type = EventType.ScrollWheel,
                mousePosition = localPos,
                delta = new Vector2(0, deltaY)
            };

            window.SendEvent(evt);
            window.Repaint();

            await Task.Delay(100);

            return new
            {
                scrolled = true,
                typeName = window.GetType().Name,
                normalizedX,
                normalizedY,
                deltaY
            };
        }

        [Tool(Description = "Type text into the focused window (sends key events for each character)",
            Returns = "JSON object confirming the text was typed")]
        internal async Task<object> TypeText(string typeName, string text)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            window.Focus();

            foreach (char c in text)
            {
                var evt = new Event
                {
                    type = EventType.KeyDown,
                    character = c,
                    keyCode = KeyCode.None
                };
                window.SendEvent(evt);
                await Task.Delay(20);
            }

            window.Repaint();

            return new
            {
                typed = true,
                typeName = window.GetType().Name,
                text,
                length = text.Length
            };
        }

        [Tool(Description = "Press a specific key (Enter, Escape, Tab, Arrow keys, etc.)",
            Returns = "JSON object confirming the key press")]
        internal async Task<object> PressKey(string typeName, string keyCode, bool ctrl = false, bool shift = false, bool alt = false)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            if (!Enum.TryParse<KeyCode>(keyCode, true, out var key))
            {
                return new
                {
                    error = $"Unknown keyCode: {keyCode}",
                    hint = "Valid keys: Return, Escape, Tab, Space, UpArrow, DownArrow, LeftArrow, RightArrow, Backspace, Delete, etc."
                };
            }

            window.Focus();

            var modifiers = EventModifiers.None;
            if (ctrl) modifiers |= EventModifiers.Control;
            if (shift) modifiers |= EventModifiers.Shift;
            if (alt) modifiers |= EventModifiers.Alt;

            var evt = new Event
            {
                type = EventType.KeyDown,
                keyCode = key,
                modifiers = modifiers
            };
            window.SendEvent(evt);

            await Task.Delay(50);

            evt.type = EventType.KeyUp;
            window.SendEvent(evt);

            window.Repaint();

            return new
            {
                pressed = true,
                typeName = window.GetType().Name,
                keyCode = key.ToString(),
                modifiers = modifiers.ToString()
            };
        }

        #endregion

        #region Tier 2: Semantic Element Query (UI Toolkit)

        [Tool(Description = "Click a UI Toolkit element by USS query selector (e.g., 'Button.primary', '#searchField')",
            Returns = "JSON object confirming the element was clicked")]
        internal async Task<object> ClickElement(string typeName, string query)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var root = window.rootVisualElement;
            if (root == null)
            {
                return new { error = "Window does not have a rootVisualElement (not UI Toolkit)" };
            }

            var element = root.Q(query);
            if (element == null)
            {
                return new
                {
                    error = $"Element '{query}' not found",
                    hint = "Use GetElementTree to see available elements"
                };
            }

            window.Focus();

            using var evt = new ClickEvent { target = element };
            element.SendEvent(evt);

            window.Repaint();
            await Task.Delay(100);

            return new
            {
                clicked = true,
                typeName = window.GetType().Name,
                query,
                elementType = element.GetType().Name,
                elementName = element.name
            };
        }

        [Tool(Description = "Get the UI Toolkit VisualElement hierarchy with types, names, classes, and bounds",
            Returns = "JSON object with the element tree structure")]
        internal object GetElementTree(string typeName, int maxDepth = 5)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var root = window.rootVisualElement;
            if (root == null)
            {
                return new { error = "Window does not have a rootVisualElement (not UI Toolkit)" };
            }

            var tree = BuildElementTree(root, 0, maxDepth);

            return new
            {
                typeName = window.GetType().Name,
                tree
            };
        }

        [Tool(Description = "Get the current value of a UI Toolkit input element",
            Returns = "JSON object with the element value")]
        internal object GetElementValue(string typeName, string query)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var root = window.rootVisualElement;
            if (root == null)
            {
                return new { error = "Window does not have a rootVisualElement" };
            }

            var element = root.Q(query);
            if (element == null)
            {
                return new { error = $"Element '{query}' not found" };
            }

            object value = null;
            var elementType = element.GetType();

            var valueProp = elementType.GetProperty("value");
            if (valueProp != null)
            {
                value = valueProp.GetValue(element);
            }

            return new
            {
                query,
                elementType = elementType.Name,
                value = value?.ToString(),
                valueType = value?.GetType().Name
            };
        }

        [Tool(Description = "Set the value of a UI Toolkit input element",
            Returns = "JSON object confirming the value was set")]
        internal object SetElementValue(string typeName, string query, string value)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var root = window.rootVisualElement;
            if (root == null)
            {
                return new { error = "Window does not have a rootVisualElement" };
            }

            var element = root.Q(query);
            if (element == null)
            {
                return new { error = $"Element '{query}' not found" };
            }

            var elementType = element.GetType();
            var valueProp = elementType.GetProperty("value");

            if (valueProp == null)
            {
                return new { error = $"Element '{query}' does not have a value property" };
            }

            try
            {
                var targetType = valueProp.PropertyType;
                object convertedValue;

                if (targetType == typeof(string))
                {
                    convertedValue = value;
                }
                else if (targetType == typeof(int))
                {
                    convertedValue = int.Parse(value);
                }
                else if (targetType == typeof(float))
                {
                    convertedValue = float.Parse(value);
                }
                else if (targetType == typeof(bool))
                {
                    convertedValue = bool.Parse(value);
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }

                valueProp.SetValue(element, convertedValue);
                window.Repaint();

                return new
                {
                    set = true,
                    query,
                    value,
                    elementType = elementType.Name
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to set value: {ex.Message}" };
            }
        }

        #endregion

        #region Tier 3: Direct Method Invocation

        [Tool(Description = "Invoke a method on an EditorWindow via reflection",
            Returns = "JSON object with the method result")]
        internal object InvokeWindowMethod(string typeName, string methodName, string argsJson = null)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var windowType = window.GetType();
            var method = windowType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                return new
                {
                    error = $"Method '{methodName}' not found on {windowType.Name}",
                    availableMethods = windowType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Where(m => !m.IsSpecialName)
                        .Select(m => m.Name)
                        .Distinct()
                        .Take(20)
                        .ToArray()
                };
            }

            try
            {
                object[] args = null;
                if (!string.IsNullOrEmpty(argsJson))
                {
                    args = JsonUtility.FromJson<object[]>(argsJson);
                }

                var result = method.Invoke(window, args);
                window.Repaint();

                return new
                {
                    invoked = true,
                    methodName,
                    result = result?.ToString(),
                    returnType = method.ReturnType.Name
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Method invocation failed: {ex.Message}" };
            }
        }

        [Tool(Description = "Get a property or field value from an EditorWindow via reflection",
            Returns = "JSON object with the property value")]
        internal object GetWindowProperty(string typeName, string propertyName)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var windowType = window.GetType();

            var prop = windowType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop != null)
            {
                var value = prop.GetValue(window);
                return new
                {
                    propertyName,
                    value = value?.ToString(),
                    type = prop.PropertyType.Name
                };
            }

            var field = windowType.GetField(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
            {
                var value = field.GetValue(window);
                return new
                {
                    propertyName,
                    value = value?.ToString(),
                    type = field.FieldType.Name
                };
            }

            return new
            {
                error = $"Property or field '{propertyName}' not found on {windowType.Name}",
                availableProperties = windowType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Select(p => p.Name)
                    .Take(20)
                    .ToArray()
            };
        }

        [Tool(Description = "Get serialized state of a window (current page, selected item, etc.)",
            Returns = "JSON object with window state")]
        internal object GetWindowState(string typeName)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var windowType = window.GetType();
            var state = new Dictionary<string, object>();

            var properties = windowType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                if (IsSimpleType(prop.PropertyType))
                {
                    try
                    {
                        state[prop.Name] = prop.GetValue(window)?.ToString();
                    }
                    catch
                    {
                    }
                }
            }

            return new
            {
                typeName = windowType.Name,
                state
            };
        }

        #endregion

        #region Support Methods

        [Tool(Description = "Wait for window to repaint with optional delay for animations",
            Returns = "JSON object confirming the wait completed")]
        internal async Task<object> WaitForRepaint(string typeName, int delayMs = 200)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            window.Repaint();
            await Task.Delay(delayMs);

            return new
            {
                waited = true,
                typeName = window.GetType().Name,
                delayMs
            };
        }

        [Tool(Description = "Wait for a window property to reach an expected value (polling)",
            Returns = "JSON object with success status and final value")]
        internal async Task<object> WaitForCondition(string typeName, string propertyName, string expectedValue, int timeoutMs = 5000)
        {
            var window = FindWindowByTypeName(typeName);
            if (window == null)
            {
                return new { error = $"Window '{typeName}' not found" };
            }

            var windowType = window.GetType();
            var prop = windowType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var field = prop == null ? windowType.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null;

            if (prop == null && field == null)
            {
                return new { error = $"Property or field '{propertyName}' not found" };
            }

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                var currentValue = prop != null ? prop.GetValue(window) : field.GetValue(window);
                if (currentValue?.ToString() == expectedValue)
                {
                    return new
                    {
                        success = true,
                        propertyName,
                        value = currentValue?.ToString(),
                        elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds
                    };
                }

                await Task.Delay(100);
            }

            var finalValue = prop != null ? prop.GetValue(window) : field.GetValue(window);
            return new
            {
                success = false,
                timeout = true,
                propertyName,
                expectedValue,
                actualValue = finalValue?.ToString()
            };
        }

        [Tool(Description = "Annotate a screenshot with a coordinate grid overlay to help AI vision map elements to click coordinates",
            Returns = "JSON object with base64 annotated PNG")]
        internal object AnnotateScreenshot(string base64Png)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Png);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);

                int width = texture.width;
                int height = texture.height;

                var gridColor = Color.green;
                var labelColor = Color.green;

                for (int i = 1; i < 10; i++)
                {
                    float t = i / 10f;
                    int x = (int)(t * width);
                    int y = (int)(t * height);

                    for (int py = 0; py < height; py++)
                    {
                        texture.SetPixel(x, py, gridColor);
                    }

                    for (int px = 0; px < width; px++)
                    {
                        texture.SetPixel(px, y, labelColor);
                    }
                }

                texture.Apply();

                var annotatedBytes = texture.EncodeToPNG();
                var annotatedBase64 = Convert.ToBase64String(annotatedBytes);

                UnityEngine.Object.DestroyImmediate(texture);

                return new
                {
                    base64Png = annotatedBase64,
                    width,
                    height,
                    gridLines = 9,
                    note = "Grid lines at 0.1, 0.2, ..., 0.9 of width/height. Use these to map element positions to normalized coordinates."
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to annotate screenshot: {ex.Message}" };
            }
        }

        #endregion

        #region Helper Methods

        private static EditorWindow FindWindowByTypeName(string typeName)
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            return windows.FirstOrDefault(w =>
                w != null &&
                (w.GetType().Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) ||
                 w.GetType().FullName?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true));
        }

        private static void SendMouseEvent(EditorWindow window, EventType type, Vector2 position, int button, int clickCount)
        {
            var evt = new Event
            {
                type = type,
                mousePosition = position,
                button = button,
                clickCount = clickCount
            };
            window.SendEvent(evt);
        }

        private static object BuildElementTree(VisualElement element, int depth, int maxDepth)
        {
            if (depth > maxDepth || element == null)
                return null;

            var children = new List<object>();
            if (depth < maxDepth)
            {
                foreach (var child in element.Children())
                {
                    var childTree = BuildElementTree(child, depth + 1, maxDepth);
                    if (childTree != null)
                    {
                        children.Add(childTree);
                    }
                }
            }

            return new
            {
                type = element.GetType().Name,
                name = element.name,
                classes = element.GetClasses().ToArray(),
                bounds = new
                {
                    x = element.worldBound.x,
                    y = element.worldBound.y,
                    width = element.worldBound.width,
                    height = element.worldBound.height
                },
                visible = element.visible && element.resolvedStyle.display != DisplayStyle.None,
                children = children.Count > 0 ? children : null
            };
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type.IsEnum;
        }

        #endregion
    }
}
