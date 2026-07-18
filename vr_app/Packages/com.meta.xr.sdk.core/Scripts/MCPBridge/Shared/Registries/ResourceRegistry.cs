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
using System.Threading.Tasks;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Definitions;
using Meta.MCPBridge.Schemas;

namespace Meta.MCPBridge.Registries
{
    internal class ResourceRegistry : Registry<Resource>
    {
        private Dictionary<string, Resource> _subRegistry;
        internal override string Name => "resources";

        protected override IEnumerable<Resource> FetchFromTypes(IEnumerable<Type> types)
        {
            return types.Where(type => type.IsEnum)
                .Where(type => type.GetCustomAttribute<ResourceAttribute>() != null)
                .Select(type => new Resource(type, type.GetCustomAttribute<ResourceAttribute>()));
        }

        protected override ResultSchema ToSchemaInternal(IEnumerable<Resource> definitions)
        {
            var schemas = definitions.Select(definition => definition.ToSchema());
            return new ResourceListResultSchema
            {
                Resources = schemas
            };
        }

        protected override async Task<IEnumerable<Resource>> FetchAll()
        {
            _subRegistry ??= await CompileRegistry();
            _registry ??= Setup();
            return _registry.Values;
        }

        private Dictionary<string, Resource> Setup()
        {
            var dictionary = new Dictionary<string, Resource>();
            var reference = new Resource(null, null);
            reference.Children.AddRange(_subRegistry.Values);
            dictionary.Add(reference.Name, reference);
            return dictionary;
        }
    }
}
