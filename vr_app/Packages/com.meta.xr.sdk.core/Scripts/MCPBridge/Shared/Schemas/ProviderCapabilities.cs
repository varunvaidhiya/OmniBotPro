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

using Meta.MCPBridge.Schemas;
using Newtonsoft.Json;

namespace Meta.MCPBridge
{
    /// <summary>
    /// Capabilities registered by a Tool Provider, describing what tools, resources,
    /// and prompts it can handle. Sent from the Tool Provider to the MCP server
    /// during the registration handshake.
    /// </summary>
    internal class ProviderCapabilities
    {
        [JsonProperty("tools")]
        internal ToolSchema[] Tools { get; set; }

        [JsonProperty("resources")]
        internal ResourceSchema[] Resources { get; set; }

        [JsonProperty("prompts")]
        internal PromptSchema[] Prompts { get; set; }
    }
}
