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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Definitions;
using Meta.MCPBridge.Registries;
using Meta.MCPBridge.Schemas;
using Meta.MCPBridge.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Meta.MCPBridge
{
    /// <summary>
    /// Central coordinator for MCP capabilities (tools, resources, prompts).
    /// Manages registries, discovers capabilities, and executes requests locally.
    ///
    /// Used by <see cref="Runtime.ToolProviderClient"/> to:
    /// 1. Build capability schemas for registration with the MCP server
    /// 2. Execute tool calls, resource reads, and prompt gets locally
    ///
    /// Tools, resources, and prompts are discovered by scanning assemblies for
    /// classes with [Tool], [Resource], or prompt definitions.
    /// </summary>
    internal class LocalExecutor : SingletonService<LocalExecutor>
    {
        private Dictionary<string, Type> _toolTypes;
        private Dictionary<string, object> _toolInstances;
        private bool _initialized;

        /// <summary>
        /// Gets the registry of all available MCP tools that can be called by AI assistants.
        /// </summary>
        internal ToolRegistry ToolRegistry { get; } = new();

        /// <summary>
        /// Gets the registry of all available MCP resources that can be read by AI assistants.
        /// </summary>
        internal ResourceRegistry ResourceRegistry { get; } = new();

        /// <summary>
        /// Gets the registry of all available MCP prompts that can be fetched by AI assistants.
        /// </summary>
        internal PromptRegistry PromptRegistry { get; } = new();

        /// <summary>
        /// Initialize the tool registry by scanning assemblies.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized)
                return;

            _toolTypes = new Dictionary<string, Type>();
            _toolInstances = new Dictionary<string, object>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        var toolAttr = type.GetCustomAttribute<ToolAttribute>();
                        if (toolAttr != null)
                        {
                            _toolTypes[type.Name] = type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip problematic assemblies
                }
            }

            _initialized = true;
            Debug.Log($"[LocalExecutor] Discovered {_toolTypes.Count} tools: {string.Join(", ", _toolTypes.Keys)}");
        }

        #region Capability Discovery

        /// <summary>
        /// Build the complete set of capabilities for registration with the MCP server.
        /// </summary>
        public ProviderCapabilities GetCapabilities()
        {
            return new ProviderCapabilities
            {
                Tools = GetToolSchemas(),
                Resources = GetResourceSchemas(),
                Prompts = GetPromptSchemas()
            };
        }

        /// <summary>
        /// Build tool schemas from discovered [Tool]-attributed types.
        /// </summary>
        public ToolSchema[] GetToolSchemas()
        {
            EnsureInitialized();
            return _toolTypes.Select(kvp => BuildToolSchema(kvp.Key, kvp.Value)).ToArray();
        }

        /// <summary>
        /// Build resource schemas from the ResourceRegistry.
        /// </summary>
        public ResourceSchema[] GetResourceSchemas()
        {
            try
            {
                var result = Task.Run(async () => await ResourceRegistry.ToSchema()).GetAwaiter().GetResult();
                if (result is ResourceListResultSchema resourceList)
                {
                    return resourceList.Resources?.ToArray() ?? Array.Empty<ResourceSchema>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalExecutor] Could not discover resources: {ex.Message}");
            }

            return Array.Empty<ResourceSchema>();
        }

        /// <summary>
        /// Build prompt schemas from the PromptRegistry.
        /// </summary>
        public PromptSchema[] GetPromptSchemas()
        {
            try
            {
                var result = Task.Run(async () => await PromptRegistry.ToSchema()).GetAwaiter().GetResult();
                if (result is PromptListResultSchema promptList)
                {
                    return promptList.Prompts?.ToArray() ?? Array.Empty<PromptSchema>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalExecutor] Could not discover prompts: {ex.Message}");
            }

            return Array.Empty<PromptSchema>();
        }

        /// <summary>
        /// Build a tool schema using the canonical Tool.ToSchema() implementation.
        /// This ensures all metadata (remarks, return types, parameter info) is properly included.
        /// </summary>
        private ToolSchema BuildToolSchema(string toolName, Type toolType)
        {
            var toolAttr = toolType.GetCustomAttribute<ToolAttribute>();
            var tool = new Tool(toolType, toolAttr);
            return tool.ToSchema();
        }

        #endregion

        #region Tool Execution

        /// <summary>
        /// Execute a tool method with the given arguments.
        /// </summary>
        public async Task<ResultSchema> ExecuteToolCall(string toolName, string methodName, JObject arguments)
        {
            EnsureInitialized();

            if (!_toolTypes.TryGetValue(toolName, out var toolType))
            {
                throw new InvalidOperationException($"Tool '{toolName}' not found. Available tools: {string.Join(", ", _toolTypes.Keys)}");
            }

            var instance = GetOrCreateInstance(toolName, toolType);
            var method = FindMethod(toolType, methodName);
            if (method == null)
            {
                var availableMethods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<ToolAttribute>() != null)
                    .Select(m => m.Name);
                throw new InvalidOperationException(
                    $"Method '{methodName}' not found on tool '{toolName}'. Available methods: {string.Join(", ", availableMethods)}");
            }

            var parameters = BuildParameters(method, arguments);

            object result;
            try
            {
                result = method.Invoke(instance, parameters);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }

            if (result is Task task)
            {
                await task;
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var resultProperty = taskType.GetProperty("Result");
                    result = resultProperty?.GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            return ConvertToResultSchema(result);
        }

        #endregion

        #region Resource Execution

        /// <summary>
        /// Read a resource by URI using the ResourceRegistry.
        /// </summary>
        public async Task<ResultSchema> ExecuteResourceRead(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                throw new ArgumentNullException(nameof(uri));

            var resource = await ResourceRegistry.Fetch(uri);
            if (resource == null)
                throw new InvalidOperationException($"Could not find resource {uri}");

            return await resource.Read(this);
        }

        #endregion

        #region Prompt Execution

        /// <summary>
        /// Get a prompt by name with arguments using the PromptRegistry.
        /// </summary>
        public async Task<ResultSchema> ExecutePromptGet(string name, JObject arguments)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            var prompt = await PromptRegistry.Fetch(name);
            if (prompt == null)
                throw new InvalidOperationException($"Could not find prompt {name}");

            return prompt.Get(arguments);
        }

        #endregion

        #region Internal Helpers

        private object GetOrCreateInstance(string toolName, Type toolType)
        {
            if (_toolInstances.TryGetValue(toolName, out var existing))
            {
                return existing;
            }

            var instanceProperty = toolType.GetProperty("instance",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (instanceProperty != null)
            {
                var instance = instanceProperty.GetValue(null);
                if (instance != null)
                {
                    _toolInstances[toolName] = instance;
                    return instance;
                }
            }

            var newInstance = Activator.CreateInstance(toolType);
            _toolInstances[toolName] = newInstance;
            return newInstance;
        }

        private MethodInfo FindMethod(Type toolType, string methodName)
        {
            var methods = toolType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var toolAttr = method.GetCustomAttribute<ToolAttribute>();
                if (toolAttr != null && string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase))
                {
                    return method;
                }
            }

            return toolType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }

        private object[] BuildParameters(MethodInfo method, JObject arguments)
        {
            var paramInfos = method.GetParameters();
            var parameters = new object[paramInfos.Length];

            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramInfo = paramInfos[i];
                var paramName = paramInfo.Name;

                JToken argValue = null;
                foreach (var prop in arguments.Properties())
                {
                    if (string.Equals(prop.Name, paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        argValue = prop.Value;
                        break;
                    }
                }

                if (argValue == null || argValue.Type == JTokenType.Null)
                {
                    if (paramInfo.HasDefaultValue)
                    {
                        parameters[i] = paramInfo.DefaultValue;
                    }
                    else if (paramInfo.ParameterType.IsValueType)
                    {
                        parameters[i] = Activator.CreateInstance(paramInfo.ParameterType);
                    }
                    else
                    {
                        parameters[i] = null;
                    }
                }
                else
                {
                    try
                    {
                        parameters[i] = argValue.ToObject(paramInfo.ParameterType);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(
                            $"Failed to convert argument '{paramName}' to type {paramInfo.ParameterType.Name}: {ex.Message}");
                    }
                }
            }

            return parameters;
        }

        private ToolCallResultSchema ConvertToResultSchema(object result)
        {
            var responseData = new JObject
            {
                ["success"] = true
            };

            if (result != null)
            {
                if (result is string str)
                {
                    responseData["return value"] = str;
                }
                else if (result.GetType().IsPrimitive)
                {
                    responseData["return value"] = JToken.FromObject(result);
                }
                else
                {
                    responseData["return value"] = JToken.FromObject(result);
                }
            }

            return new ToolCallResultSchema
            {
                Content = new[]
                {
                    new ToolCallDataSchema
                    {
                        Text = responseData.ToString(Formatting.None)
                    }
                }
            };
        }

        #endregion

        /// <summary>
        /// Get the list of available tools (for debugging/status).
        /// </summary>
        public IEnumerable<string> GetAvailableTools()
        {
            EnsureInitialized();
            return _toolTypes.Keys;
        }

        /// <summary>
        /// Reset the executor (for testing).
        /// </summary>
        public void Reset()
        {
            _toolTypes?.Clear();
            _toolInstances?.Clear();
            _initialized = false;
        }
    }
}
