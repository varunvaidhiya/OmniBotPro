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
using Meta.MCPBridge;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Schemas;
using Meta.MCPBridge.Services;
using Newtonsoft.Json.Linq;

namespace MCPServices.Tools
{
    /// <summary>
    /// Meta-tool for AI introspection of available MCP tools.
    /// Follows the GraphQL introspection pattern (__type, __schema) to allow
    /// progressive disclosure of tool capabilities without bloating initial schemas.
    ///
    /// Use this tool when:
    /// - You need detailed documentation about a tool's methods and parameters
    /// - You want to understand how to use an unfamiliar tool
    /// - You need to see usage examples or understand return types
    /// - You want to search for tools that match certain capabilities
    /// </summary>
    [Tool(
        "Meta-tool for discovering and understanding available MCP tools. " +
        "Call this BEFORE using an unfamiliar tool to get detailed documentation, " +
        "method signatures, parameter types, and usage guidance.",
        "WHEN TO USE: Use when you encounter a new tool or need to understand its capabilities better.",
        "PROGRESSIVE DISCLOSURE: Initial tool listings are brief; use GetHelp() for full documentation.",
        "SEARCH: Use SearchTools() to find tools that match specific capabilities."
    )]
    internal class ToolHelp : SingletonService<ToolHelp>
    {
        [Tool(
            Description = "Get comprehensive documentation for a specific tool, including all method signatures, " +
                          "parameter types, return values, and usage guidance. Optionally filter to a specific method.",
            Returns = "Detailed tool documentation as structured JSON with methods, parameters, remarks, and examples.")]
        internal JObject GetHelp(string toolName, string methodName = null)
        {
            var executor = LocalExecutor.instance;
            executor.EnsureInitialized();

            var schemas = executor.GetToolSchemas();
            var toolSchema = schemas.FirstOrDefault(t =>
                string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));

            if (toolSchema == null)
            {
                return new JObject
                {
                    ["error"] = $"Tool '{toolName}' not found.",
                    ["availableTools"] = new JArray(schemas.Select(t => t.Name))
                };
            }

            var result = new JObject
            {
                ["name"] = toolSchema.Name,
                ["description"] = toolSchema.Description
            };

            // Add remarks if available
            if (toolSchema.Remarks != null && toolSchema.Remarks.Length > 0)
            {
                result["remarks"] = new JArray(toolSchema.Remarks);
            }

            // Add method details
            if (toolSchema.Methods != null && toolSchema.Methods.Count > 0)
            {
                var methodsObj = new JObject();

                foreach (KeyValuePair<string, MethodDetailSchema> methodEntry in toolSchema.Methods)
                {
                    var name = methodEntry.Key;
                    var methodDetail = methodEntry.Value;

                    // If methodName filter is specified, only include that method
                    if (!string.IsNullOrEmpty(methodName) &&
                        !string.Equals(name, methodName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var methodObj = new JObject
                    {
                        ["description"] = methodDetail.Description
                    };

                    if (!string.IsNullOrEmpty(methodDetail.Returns))
                    {
                        methodObj["returns"] = methodDetail.Returns;
                    }

                    if (!string.IsNullOrEmpty(methodDetail.ReturnType))
                    {
                        methodObj["returnType"] = methodDetail.ReturnType;
                    }

                    if (methodDetail.Remarks != null && methodDetail.Remarks.Length > 0)
                    {
                        methodObj["remarks"] = new JArray(methodDetail.Remarks);
                    }

                    // Add parameter details
                    if (methodDetail.Parameters != null && methodDetail.Parameters.Count > 0)
                    {
                        var paramsObj = new JObject();
                        foreach (KeyValuePair<string, ParameterDetailSchema> paramEntry in methodDetail.Parameters)
                        {
                            var paramName = paramEntry.Key;
                            var paramDetail = paramEntry.Value;
                            paramsObj[paramName] = new JObject
                            {
                                ["type"] = paramDetail.Type,
                                ["csharpType"] = paramDetail.CSharpType,
                                ["required"] = paramDetail.Required ?? false,
                                ["default"] = paramDetail.Default != null ? JToken.FromObject(paramDetail.Default) : null
                            };
                        }
                        methodObj["parameters"] = paramsObj;
                    }

                    methodsObj[name] = methodObj;
                }

                if (methodsObj.Count > 0)
                {
                    result["methods"] = methodsObj;
                }
                else if (!string.IsNullOrEmpty(methodName))
                {
                    result["error"] = $"Method '{methodName}' not found in tool '{toolName}'.";
                    result["availableMethods"] = new JArray(toolSchema.Methods.Keys);
                }
            }

            // Add input schema summary for parameter cross-reference
            if (toolSchema.InputSchema?.Properties != null)
            {
                var paramsUsage = new JObject();
                foreach (KeyValuePair<string, ToolPropertySchema> propEntry in toolSchema.InputSchema.Properties)
                {
                    var propName = propEntry.Key;
                    var propSchema = propEntry.Value;

                    if (propName == "method") continue; // Skip the method enum itself

                    var paramInfo = new JObject
                    {
                        ["type"] = propSchema.Type
                    };

                    if (!string.IsNullOrEmpty(propSchema.CSharpType))
                    {
                        paramInfo["csharpType"] = propSchema.CSharpType;
                    }

                    if (propSchema.UsedBy != null && propSchema.UsedBy.Length > 0)
                    {
                        paramInfo["usedByMethods"] = new JArray(propSchema.UsedBy);
                    }

                    if (propSchema.Enum != null && propSchema.Enum.Length > 0)
                    {
                        paramInfo["allowedValues"] = new JArray(propSchema.Enum);
                    }

                    if (!string.IsNullOrEmpty(propSchema.Remark))
                    {
                        paramInfo["remark"] = propSchema.Remark;
                    }

                    paramsUsage[propName] = paramInfo;
                }

                if (paramsUsage.Count > 0)
                {
                    result["parameterReference"] = paramsUsage;
                }
            }

            return result;
        }

        [Tool(
            Description = "List all available MCP tools with brief descriptions. " +
                          "Use this to get an overview of capabilities before diving into specific tools.",
            Returns = "Array of tool summaries with name, description, and method count.")]
        internal JArray ListTools()
        {
            var executor = LocalExecutor.instance;
            executor.EnsureInitialized();

            var schemas = executor.GetToolSchemas();
            var result = new JArray();

            foreach (var schema in schemas.OrderBy(s => s.Name))
            {
                var toolSummary = new JObject
                {
                    ["name"] = schema.Name,
                    ["description"] = schema.Description
                };

                // Add method count
                if (schema.Methods != null)
                {
                    toolSummary["methodCount"] = schema.Methods.Count;
                    toolSummary["methods"] = new JArray(schema.Methods.Keys);
                }

                // Add first remark as a hint (if available)
                if (schema.Remarks != null && schema.Remarks.Length > 0)
                {
                    toolSummary["hint"] = schema.Remarks[0];
                }

                result.Add(toolSummary);
            }

            return result;
        }

        [Tool(
            Description = "Search for tools by keyword in their names, descriptions, or method names. " +
                          "Useful for finding tools that match specific capabilities.",
            Returns = "Array of matching tools with relevance information.")]
        internal JArray SearchTools(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new JArray(new JObject
                {
                    ["error"] = "Search query cannot be empty."
                });
            }

            var executor = LocalExecutor.instance;
            executor.EnsureInitialized();

            var schemas = executor.GetToolSchemas();
            var queryLower = query.ToLowerInvariant();
            var results = new List<(ToolSchema schema, int score, string matchType)>();

            foreach (var schema in schemas)
            {
                var score = 0;
                var matchTypes = new List<string>();

                // Check tool name (highest priority)
                if (schema.Name.ToLowerInvariant().Contains(queryLower))
                {
                    score += 100;
                    matchTypes.Add("name");
                }

                // Check description
                if (!string.IsNullOrEmpty(schema.Description) &&
                    schema.Description.ToLowerInvariant().Contains(queryLower))
                {
                    score += 50;
                    matchTypes.Add("description");
                }

                // Check remarks
                if (schema.Remarks != null)
                {
                    foreach (var remark in schema.Remarks)
                    {
                        if (remark.ToLowerInvariant().Contains(queryLower))
                        {
                            score += 30;
                            matchTypes.Add("remarks");
                            break;
                        }
                    }
                }

                // Check method names and descriptions
                if (schema.Methods != null)
                {
                    foreach (KeyValuePair<string, MethodDetailSchema> methodEntry in schema.Methods)
                    {
                        var methodName2 = methodEntry.Key;
                        var methodDetail = methodEntry.Value;
                        if (methodName2.ToLowerInvariant().Contains(queryLower))
                        {
                            score += 40;
                            matchTypes.Add($"method:{methodName2}");
                        }
                        else if (!string.IsNullOrEmpty(methodDetail.Description) &&
                                 methodDetail.Description.ToLowerInvariant().Contains(queryLower))
                        {
                            score += 20;
                            matchTypes.Add($"method:{methodName2}");
                        }
                    }
                }

                if (score > 0)
                {
                    results.Add((schema, score, string.Join(", ", matchTypes.Distinct())));
                }
            }

            var resultArray = new JArray();
            foreach (var (schema, score, matchType) in results.OrderByDescending(r => r.score))
            {
                resultArray.Add(new JObject
                {
                    ["name"] = schema.Name,
                    ["description"] = schema.Description,
                    ["matchedIn"] = matchType,
                    ["relevance"] = score,
                    ["methodCount"] = schema.Methods?.Count ?? 0
                });
            }

            if (resultArray.Count == 0)
            {
                resultArray.Add(new JObject
                {
                    ["message"] = $"No tools found matching '{query}'.",
                    ["suggestion"] = "Try a broader search term or use ListTools() to see all available tools."
                });
            }

            return resultArray;
        }

        [Tool(
            Description = "Get a quick reference card for a tool showing its most important methods and their signatures. " +
                          "More concise than GetHelp() - ideal for quick lookup.",
            Returns = "Compact method reference with signatures and brief descriptions.")]
        internal string GetQuickReference(string toolName)
        {
            var executor = LocalExecutor.instance;
            executor.EnsureInitialized();

            var schemas = executor.GetToolSchemas();
            var toolSchema = schemas.FirstOrDefault(t =>
                string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase));

            if (toolSchema == null)
            {
                var availableTools = string.Join(", ", schemas.Select(t => t.Name));
                return $"Tool '{toolName}' not found. Available tools: {availableTools}";
            }

            var lines = new List<string>
            {
                $"=== {toolSchema.Name} Quick Reference ===",
                "",
                toolSchema.Description,
                ""
            };

            if (toolSchema.Methods != null && toolSchema.Methods.Count > 0)
            {
                lines.Add("Methods:");
                foreach (KeyValuePair<string, MethodDetailSchema> methodEntry in toolSchema.Methods)
                {
                    var methodName2 = methodEntry.Key;
                    var methodDetail = methodEntry.Value;

                    // Build signature
                    var paramList = "";
                    if (methodDetail.Parameters != null && methodDetail.Parameters.Count > 0)
                    {
                        var paramStrs = methodDetail.Parameters.Select(p =>
                        {
                            var required = p.Value.Required == true ? "" : "?";
                            return $"{p.Value.Type}{required} {p.Key}";
                        });
                        paramList = string.Join(", ", paramStrs);
                    }

                    var returnInfo = !string.IsNullOrEmpty(methodDetail.ReturnType)
                        ? $" -> {methodDetail.ReturnType}"
                        : "";

                    lines.Add($"  • {methodName2}({paramList}){returnInfo}");
                    if (!string.IsNullOrEmpty(methodDetail.Description))
                    {
                        // Truncate long descriptions
                        var desc = methodDetail.Description;
                        if (desc.Length > 80)
                        {
                            desc = desc.Substring(0, 77) + "...";
                        }
                        lines.Add($"    {desc}");
                    }
                }
            }

            // Add first remark as usage hint
            if (toolSchema.Remarks != null && toolSchema.Remarks.Length > 0)
            {
                lines.Add("");
                lines.Add($"Hint: {toolSchema.Remarks[0]}");
            }

            return string.Join("\n", lines);
        }
    }
}
