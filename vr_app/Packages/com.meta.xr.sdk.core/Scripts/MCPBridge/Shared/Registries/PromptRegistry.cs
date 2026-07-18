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
using System.Threading.Tasks;
using Meta.MCPBridge.Definitions;
using Meta.MCPBridge.Schemas;
using UnityEngine;

namespace Meta.MCPBridge.Registries
{
    /// <summary>
    /// Registry of AI prompts that can be fetched by MCP clients.
    /// Provides VR-specific guidance and interaction patterns for AI assistants.
    /// </summary>
    internal class PromptRegistry : Registry<Prompt>
    {
        /// <summary>Registry name identifier for MCP protocol.</summary>
        internal override string Name => "prompts";

        /// <summary>
        /// Converts prompt definitions to MCP schema format.
        /// </summary>
        protected override ResultSchema ToSchemaInternal(IEnumerable<Prompt> definitions)
        {
            var schemas = definitions.Select(definition => definition.ToSchema());
            return new PromptListResultSchema
            {
                Prompts = schemas
            };
        }

        protected override Task<Dictionary<string, Prompt>> CompileRegistry()
        {
            var registry = new Dictionary<string, Prompt>();

            // Check for prompts with message exceeding 7500 characters
            foreach (var kvp in registry)
            {
                var prompt = kvp.Value;
                if (!string.IsNullOrEmpty(prompt.UnformatedMessage) && prompt.UnformatedMessage.Length > 7500) // 8196 limitation for win command prompt, check for 7.5k to leave room for user msg.
                {
                    Debug.LogWarning($"Prompt '{prompt.Name}' has message with {prompt.UnformatedMessage.Length} characters, risk exceeding Windows command prompt limit of 8196 characters");
                }
            }

            return Task.FromResult(registry);
        }

        protected override IEnumerable<Prompt> FetchFromTypes(IEnumerable<Type> types)
        {
            return Enumerable.Empty<Prompt>();
        }
    }
}
