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
using System.Linq;
using Meta.MCPBridge.Schemas;
using Newtonsoft.Json.Linq;

namespace Meta.MCPBridge.Definitions
{
    internal class Prompt : Definition<PromptSchema>
    {
        internal override string Name { get; set; }
        internal string Description { get; set; }
        internal string UnformatedMessage { get; set; }
        internal List<PromptArgumentSchema> Arguments { get; set; }

        internal override PromptSchema ToSchema()
        {
            return new()
            {
                Name = Name,
                Description = Description,
                Arguments = Arguments
            };
        }

        internal PromptGetResultSchema Get(JObject arguments)
        {
            // hardcoded
            return new PromptGetResultSchema
            {
                Description = Name,
                Messages = new List<PromptMessageSchema>
                {
                    new()
                    {
                        Role = "user",
                        Content = new PromptContentSchema
                        {
                            Type = "text",
                            Text = string.Format(UnformatedMessage, arguments.Values<object>().ToArray())
                        }
                    }
                }
            };
        }
    }
}
