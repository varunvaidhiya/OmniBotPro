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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Meta.MCPBridge.Definitions;
using Meta.MCPBridge.Schemas;

namespace Meta.MCPBridge.Registries
{
    internal abstract class Registry<TDefinition> where TDefinition : Definition
    {
        protected Dictionary<string, TDefinition> _registry;
        internal abstract string Name { get; }

        internal Dictionary<string, TDefinition> FullRegistry => _registry;

        internal async Task<ResultSchema> ToSchema()
        {
            return ToSchemaInternal(await FetchAll());
        }

        protected abstract ResultSchema ToSchemaInternal(IEnumerable<TDefinition> definitions);

        protected virtual async Task<IEnumerable<TDefinition>> FetchAll()
        {
            _registry ??= await CompileRegistry();
            return _registry.Values;
        }

        internal async Task<TDefinition> Fetch(string name)
        {
            await FetchAll();
            TDefinition definition = null;
            _registry?.TryGetValue(name, out definition);
            return definition;
        }

        protected abstract IEnumerable<TDefinition> FetchFromTypes(IEnumerable<Type> types);

        protected virtual async Task<Dictionary<string, TDefinition>> CompileRegistry()
        {
            return await Task.Run(() =>
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var results = new ConcurrentBag<TDefinition>();

                Parallel.ForEach(assemblies, assembly =>
                {
                    try
                    {
                        var selection = FetchFromTypes(assembly.GetTypes());
                        foreach (var item in selection) results.Add(item);
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip problematic assemblies
                    }
                });

                return results
                    .GroupBy(item => item.Name)
                    .ToDictionary(g => g.Key, g => g.First());
            });
        }
    }
}
