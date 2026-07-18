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
    internal class PromptArgumentSchema : ISchema
    {
        [JsonProperty("name")] internal string Name { get; set; }

        [JsonProperty("description")] internal string Description { get; set; }

        [JsonProperty("required")] internal bool Required { get; set; }
    }

    // Prompts don't have a Definition right now, the definition is the schema
    internal class PromptSchema : ISchema
    {
        [JsonProperty("name")] internal string Name { get; set; }

        [JsonProperty("description")] internal string Description { get; set; }

        [JsonProperty("arguments")] internal List<PromptArgumentSchema> Arguments { get; set; } = new();
    }

    internal class PromptMessageSchema : ISchema
    {
        [JsonProperty("role")] internal string Role { get; set; }

        [JsonProperty("content")] internal PromptContentSchema Content { get; set; }
    }

    internal class PromptContentSchema : ISchema
    {
        [JsonProperty("type")] internal string Type { get; set; }

        [JsonProperty("text")] internal string Text { get; set; }
    }

    internal class PromptGetResultSchema : ResultSchema
    {
        [JsonProperty("description")] internal string Description { get; set; }

        [JsonProperty("messages")] internal List<PromptMessageSchema> Messages { get; set; } = new();
    }

    internal class PromptListResultSchema : ResultSchema
    {
        [JsonProperty("prompts")] internal IEnumerable<PromptSchema> Prompts { get; set; }
    }
}
