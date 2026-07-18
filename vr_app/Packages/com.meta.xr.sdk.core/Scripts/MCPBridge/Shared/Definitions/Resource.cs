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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Schemas;

namespace Meta.MCPBridge.Definitions
{
    internal class Resource : Definition<ResourceAttribute, ResourceSchema>
    {
        internal Resource(Type type, ResourceAttribute attribute) : base(attribute)
        {
            Type = type;
            Name = type != null ? $"reference://enums/{type.Name}" : "reference://enums";
        }

        internal Type Type { get; }
        internal List<Resource> Children { get; } = new();

        internal override string Name { get; set; }

        internal string Uri => Name;
        internal string PublicName => "Api Reference";

        internal string Description =>
            "API Reference, containing more information on some types, structs, classes and enums that are used by the tools.";

        internal string MimeType => "text/markdown";

        internal async Task<ResourceGetResultSchema> Read(LocalExecutor executor)
        {
            var read = await ReadInternal();

            var result = new ResourceGetResultSchema
            {
                Content = new[]
                {
                    new ResourceGetDataSchema()
                    {
                        Uri = Uri,
                        Text = read
                    }
                }
            };
            return result;
        }

        private async Task<string> ReadInternal()
        {
            var sb = new StringBuilder();

            foreach (var child in Children) sb.Append(await child.ReadInternal());

            if (!(Type?.IsEnum ?? false)) return sb.ToString();

            sb.AppendLine($"# {Type.Name}");
            sb.AppendLine($"## {Type.FullName}");
            sb.AppendLine(Attribute.Description);

            var fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                var enumValue = Enum.Parse(Type, field.Name);
                var numericValue = Convert.ToInt32(enumValue);

                // Basic enum value line
                sb.Append($"- **{field.Name}** ({numericValue})");

                // Check for Resource attribute
                var resourceAttr = field.GetCustomAttribute<ResourceAttribute>();
                if (resourceAttr != null && !string.IsNullOrEmpty(resourceAttr.Description))
                    sb.Append($": {resourceAttr.Description}");

                sb.AppendLine();
            }

            return sb.ToString();
        }

        internal override ResourceSchema ToSchema()
        {
            return new()
            {
                Uri = Uri,
                Name = PublicName,
                Description = Description,
                MimeType = MimeType
            };
        }
    }
}
