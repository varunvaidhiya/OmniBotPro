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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meta.MCPBridge.Schemas
{
    /// <summary>
    /// Root schema for JSON-RPC 2.0 Model Context Protocol requests. This class represents the outer structure
    /// of all MCP requests sent to the Unity bridge. It inherits from <see cref="BaseSchema"/>
    /// which provides common JSON-RPC fields like ID and JSON-RPC version. The Method property specifies which
    /// MCP operation to perform (e.g., "tools/call", "resources/read", "prompts/get"),
    /// while the Parameters property contains the specific data needed for that operation.
    /// </summary>
    internal class RequestSchema : BaseSchema
    {
        /// <summary>
        /// Gets or sets the MCP method to invoke. Common method names include "tools/list" to list available
        /// tools, "tools/call" to execute a tool, "resources/list" to list resources, "resources/read" to read
        /// a resource, "prompts/list" to list prompts, and "prompts/get" to fetch a prompt.
        /// The method name determines how the <see cref="Parameters"/> will be interpreted.
        /// </summary>
        [JsonProperty("method")] public string Method { get; set; }

        /// <summary>
        /// Gets or sets the parameters for the MCP method specified in <see cref="Method"/>. The structure
        /// and required fields of this object vary depending on the method being invoked. See
        /// <see cref="RequestParametersSchema"/> for all available parameter fields and their usage.
        /// </summary>
        [JsonProperty("params")] public RequestParametersSchema Parameters { get; set; }
    }

    /// <summary>
    /// Parameters for Model Context Protocol requests. This class contains the fields used across
    /// different MCP method types including tool operations, resource access, and prompt retrieval.
    /// Not all fields are used by every method - each MCP method only requires specific fields from
    /// this schema. For example, "tools/call" uses Name and Arguments, while "resources/read" uses Uri.
    /// See <see cref="RequestSchema"/> for the containing request structure.
    /// </summary>
    internal class RequestParametersSchema : ISchema
    {
        /// <summary>
        /// Gets or sets the name of the tool or prompt being requested. This field is required for "tools/call",
        /// "tools/get", "prompts/get", and related methods. The name should match a registered tool or prompt
        /// in the corresponding registry (<see cref="Meta.MCPBridge.Registries.ToolRegistry"/> or
        /// <see cref="Meta.MCPBridge.Registries.PromptRegistry"/>). Not used for resource methods.
        /// </summary>
        [JsonProperty("name")] public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URI of the resource being requested. This field is required for "resources/read"
        /// and related resource methods. The URI format is defined by your resource implementations and should
        /// match a registered resource in <see cref="Meta.MCPBridge.Registries.ResourceRegistry"/>. Examples
        /// might include "config://settings.json" or "state://gameworld". Not used for tool or prompt methods.
        /// </summary>
        [JsonProperty("uri")] public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the arguments for tool calls or prompt population. For "tools/call", this contains the
        /// input parameters needed by the tool method, including the required "method" field. For "prompts/get",
        /// this contains the template variables to populate the prompt with. The structure is a flexible JSON
        /// object (<see cref="JObject"/>) that varies based on the tool or prompt being invoked.
        /// </summary>
        [JsonProperty("arguments")] public JObject Arguments { get; set; }

        /// <summary>
        /// Gets or sets the method name for tool execution. When a tool call specifies a method at the
        /// parameter level (as opposed to within the arguments), this field carries that value.
        /// </summary>
        [JsonProperty("method")] public string Method { get; set; }

        /// <summary>
        /// Gets or sets additional arbitrary data that may be provided with MCP requests. This flexible JSON
        /// object (<see cref="JObject"/>) allows for future extensibility and custom data passing without
        /// requiring schema changes. The structure and usage of this field are application-specific and may
        /// vary based on your custom MCP implementations.
        /// </summary>
        [JsonProperty("additionalData")] public JObject AdditionalData { get; set; }
    }
}
