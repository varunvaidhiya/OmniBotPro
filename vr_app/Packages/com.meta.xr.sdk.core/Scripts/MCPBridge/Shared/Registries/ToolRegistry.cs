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
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Definitions;
using Meta.MCPBridge.Schemas;

namespace Meta.MCPBridge.Registries
{
    internal class ToolRegistry : Registry<Tool>
    {
        internal override string Name => "tools";

        protected override ResultSchema ToSchemaInternal(IEnumerable<Tool> definitions)
        {
            var schemas = definitions.Select(definition => definition.ToSchema());
            return new ToolListResultSchema
            {
                Tools = schemas
            };
        }

        protected override IEnumerable<Tool> FetchFromTypes(IEnumerable<Type> types)
        {
            return types
                .Where(type => type.GetCustomAttribute<ToolAttribute>() != null)
                .Select(type => new Tool(type, type.GetCustomAttribute<ToolAttribute>()));
        }
    }
}
