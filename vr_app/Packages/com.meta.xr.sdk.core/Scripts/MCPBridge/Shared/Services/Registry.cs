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
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace Meta.MCPBridge.Services
{
    internal static class Registry
    {
        internal static ConcurrentDictionary<Type, IService> Dictionary { get; } = new();

        internal static void Initialize()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var services = assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.ContainsGenericParameters && !type.IsAbstract && type.GetInterfaces().Contains(typeof(IService)));

            foreach (var service in services)
            {
                Activator.CreateInstance(service);
            }
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeRuntime()
        {
            Initialize();
        }
#endif

        internal static void Register(IService service)
        {
            var actualType = service.GetType();
            Dictionary[actualType] = service;
            foreach (var @interface in actualType.GetInterfaces())
            {
                Dictionary[@interface] = service;
            }
        }

        /// <summary>
        /// Ensures all services are initialized. Call this before accessing services
        /// in contexts where InitializeOnLoad callbacks may not have fired yet (e.g., tests),
        /// or where additional assemblies have been loaded since the initial scan.
        /// </summary>
        internal static void EnsureInitialized()
        {
            // Always re-run Initialize() because test assemblies may have been loaded
            // after the initial InitializeOnLoad callback fired
            Initialize();
        }

        internal static bool TryGet<T>(out T service) where T : class
        {
            service = null;
            if (TryGet(typeof(T), out var serviceRaw)) service = serviceRaw as T;

            return service != null;
        }

        internal static bool TryGet(Type type, out IService service)
        {
            return Dictionary.TryGetValue(type, out service);
        }
    }
}
