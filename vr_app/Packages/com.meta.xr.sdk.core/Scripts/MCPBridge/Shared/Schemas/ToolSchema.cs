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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Meta.MCPBridge.Schemas
{
    internal class ToolSchema : ISchema
    {
        [JsonProperty("name")] internal string Name { get; set; }

        [JsonProperty("description")] internal string Description { get; set; }

        [JsonProperty("remarks", NullValueHandling = NullValueHandling.Ignore)]
        internal string[] Remarks { get; set; }

        [JsonProperty("inputSchema")] internal ToolInputSchema InputSchema { get; set; }

        // Extension field: Per-method detailed schemas for AI discoverability
        [JsonProperty("x-methods", NullValueHandling = NullValueHandling.Ignore)]
        internal Dictionary<string, MethodDetailSchema> Methods { get; set; }
    }

    /// <summary>
    /// Detailed per-method schema for AI discoverability.
    /// Provides structured information about each tool method including
    /// return types, remarks, and per-method parameter details.
    /// </summary>
    internal class MethodDetailSchema : ISchema
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        internal string Description { get; set; }

        [JsonProperty("returns", NullValueHandling = NullValueHandling.Ignore)]
        internal string Returns { get; set; }

        [JsonProperty("returnType", NullValueHandling = NullValueHandling.Ignore)]
        internal string ReturnType { get; set; }

        [JsonProperty("remarks", NullValueHandling = NullValueHandling.Ignore)]
        internal string[] Remarks { get; set; }

        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        internal Dictionary<string, ParameterDetailSchema> Parameters { get; set; }
    }

    /// <summary>
    /// Detailed parameter information for method schemas.
    /// </summary>
    internal class ParameterDetailSchema : ISchema
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        internal string Type { get; set; }

        [JsonProperty("csharpType", NullValueHandling = NullValueHandling.Ignore)]
        internal string CSharpType { get; set; }

        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        internal bool? Required { get; set; }

        [JsonProperty("default", NullValueHandling = NullValueHandling.Ignore)]
        internal object Default { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        internal string Description { get; set; }
    }

    internal class ToolInputSchema : ISchema
    {
        [JsonProperty("type")] internal string Type { get; set; } = "object";

        [JsonProperty("properties")] internal Dictionary<string, ToolPropertySchema> Properties { get; set; }

        [JsonProperty("required")] internal string[] Required { get; set; }
    }

    internal class ToolPropertySchema : ISchema
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        internal string Type { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        internal string Description { get; set; }

        [JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)]
        internal string[] Enum { get; set; }

        [JsonProperty("oneOf", NullValueHandling = NullValueHandling.Ignore)]
        internal EnumValueSchema[] OneOf { get; set; }

        // Extension fields for AI discoverability (x- prefix follows JSON Schema vendor extension convention)
        [JsonProperty("x-csharp-type", NullValueHandling = NullValueHandling.Ignore)]
        internal string CSharpType { get; set; }

        [JsonProperty("x-remark", NullValueHandling = NullValueHandling.Ignore)]
        internal string Remark { get; set; }

        [JsonProperty("x-used-by", NullValueHandling = NullValueHandling.Ignore)]
        internal string[] UsedBy { get; set; }
    }

    internal class EnumValueSchema : ISchema
    {
        [JsonProperty("const")]
        internal string Const { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        internal string Description { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        internal string Title { get; set; }
    }

    internal class MethodSchema : ISchema
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        internal string Description { get; set; }

        [JsonProperty("x-return-type", NullValueHandling = NullValueHandling.Ignore)]
        internal string ReturnType { get; set; }

        [JsonProperty("x-return-type-description", NullValueHandling = NullValueHandling.Ignore)]
        internal string ReturnTypeDescription { get; set; }

        [JsonProperty("x-remarks", NullValueHandling = NullValueHandling.Ignore)]
        internal string[] Remarks { get; set; }
    }

    internal class ToolListResultSchema : ResultSchema
    {
        [JsonProperty("tools")] internal IEnumerable<ToolSchema> Tools { get; set; }
    }

    internal class ToolCallResultSchema : ResultSchema
    {
        [JsonProperty("content")] internal IEnumerable<ToolCallDataSchema> Content { get; set; }
    }

    internal class ToolCallDataSchema : ISchema
    {
        [JsonProperty("type")] internal string Type => "text";
        [JsonProperty("text")] internal string Text { get; set; }
    }
}
