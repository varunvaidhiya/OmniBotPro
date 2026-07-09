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
using Newtonsoft.Json.Linq;

namespace Meta.MCPBridge.Schemas
{
    internal class ResponseSchema : BaseSchema
    {
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        internal ResultSchema Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        internal ErrorSchema Error { get; set; }
    }

    internal class ErrorSchema : ISchema
    {
        [JsonProperty("code")] internal int Code { get; set; }

        [JsonProperty("message")] internal string Message { get; set; }
    }

    internal class ResultSchema : ISchema
    {
        /// <summary>
        /// Captures any JSON properties not mapped to a declared property.
        /// This is essential for the MCPBridge proxy path: when a remote Tool Provider
        /// returns a ToolCallResultSchema (with "content"), the server deserializes it
        /// as the base ResultSchema. Without this, the "content" field is silently dropped
        /// and the MCP client receives an empty {}.
        /// </summary>
        [JsonExtensionData]
        internal IDictionary<string, JToken> AdditionalData { get; set; }
    }
}
