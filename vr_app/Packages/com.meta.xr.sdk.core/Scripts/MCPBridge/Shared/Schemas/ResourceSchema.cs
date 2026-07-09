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
    internal class ResourceSchema : ISchema
    {
        [JsonProperty("uri")] internal string Uri { get; set; }

        [JsonProperty("name")] internal string Name { get; set; }

        [JsonProperty("description")] internal string Description { get; set; }

        [JsonProperty("mimeType")] internal string MimeType { get; set; }
    }

    internal class ResourceListResultSchema : ResultSchema
    {
        [JsonProperty("resources")] internal IEnumerable<ResourceSchema> Resources { get; set; }
    }

    internal class ResourceGetResultSchema : ResultSchema
    {
        [JsonProperty("contents")] internal IEnumerable<ResourceGetDataSchema> Content { get; set; }
    }

    internal class ResourceGetDataSchema : ISchema
    {
        [JsonProperty("uri")] internal string Uri { get; set; }

        [JsonProperty("text")] internal string Text { get; set; }

        [JsonProperty("mimeType")] internal string MimeType => "text/markdown";
    }
}
