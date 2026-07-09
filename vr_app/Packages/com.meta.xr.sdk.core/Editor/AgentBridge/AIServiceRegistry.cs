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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Represents a registered AI service entry in the registry.
    /// Contains the service type and its registration attribute.
    /// </summary>
    public readonly struct RegisteredService
    {
        /// <summary>The service type that implements IAIService.</summary>
        public readonly Type ServiceType;

        /// <summary>The registration attribute containing service metadata.</summary>
        public readonly RegisterAIServiceAttribute Attribute;

        public RegisteredService(Type serviceType, RegisterAIServiceAttribute attribute)
        {
            ServiceType = serviceType;
            Attribute = attribute;
        }

        /// <summary>Service ID from the attribute.</summary>
        public string Id => Attribute.Id;

        /// <summary>Display name from the attribute.</summary>
        public string DisplayName => Attribute.DisplayName;

        /// <summary>Priority from the attribute.</summary>
        public int Priority => Attribute.Priority;
    }

    /// <summary>
    /// Central registry for AI services in AgentBridge.
    /// Provides automatic discovery via [RegisterAIService] attribute.
    /// Third-party developers can register their own services by applying the attribute to their service class.
    /// </summary>
    /// <remarks>
    /// Services are auto-discovered via the [RegisterAIService] attribute.
    ///
    /// Example usage for third-party services:
    /// <code>
    /// [RegisterAIService("my-custom-service", "My Custom AI", Priority = 100)]
    /// public class MyCustomService : AIServiceBase, IServiceSettingsUISimple, IServiceValidation
    /// {
    ///     public override string ServiceName => "My Custom AI";
    ///     // ... implement required methods
    /// }
    /// </code>
    /// </remarks>
    [InitializeOnLoad]
    public static class AIServiceRegistry
    {
        private static readonly Dictionary<string, RegisteredService> _registeredServices = new();
        private static bool _initialized = false;
        private static readonly object _lock = new();

        /// <summary>
        /// Static constructor for auto-discovery when Unity Editor loads.
        /// Heavy assembly scanning is deferred until after all assets are post-processed.
        /// </summary>
        static AIServiceRegistry()
        {
            // Only scan assemblies for services if the master toggle is enabled.
            // When dormant, no assembly scanning occurs at editor load.
            if (Settings.Enabled.Value)
            {
                Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(Initialize);
            }
        }

        /// <summary>
        /// Initialize the registry by discovering all services with [RegisterAIService] attribute.
        /// Called automatically on Unity Editor load via [InitializeOnLoad].
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                _initialized = true;
                DiscoverAttributedServices();
                Log.Info($"AIServiceRegistry initialized with {_registeredServices.Count} service(s)");
            }
        }

        /// <summary>
        /// Get all registered services, ordered by priority (lower values first).
        /// </summary>
        public static IEnumerable<RegisteredService> GetAllServices()
        {
            lock (_lock)
            {
                return _registeredServices.Values
                    .OrderBy(s => s.Priority)
                    .ThenBy(s => s.DisplayName)
                    .ToList();
            }
        }

        /// <summary>
        /// Get a specific registered service by ID.
        /// </summary>
        /// <param name="serviceId">The unique ID of the service</param>
        /// <returns>The registered service if found, null otherwise</returns>
        public static RegisteredService? GetService(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                return null;
            }

            lock (_lock)
            {
                return _registeredServices.TryGetValue(serviceId, out var service) ? service : null;
            }
        }

        /// <summary>
        /// Create a new instance of an AI service by ID.
        /// </summary>
        /// <param name="serviceId">The unique ID of the service to create</param>
        /// <returns>A new instance of the requested AI service</returns>
        /// <exception cref="ArgumentException">Thrown if the service ID is not registered</exception>
        public static IAIService CreateService(string serviceId)
        {
            var service = GetService(serviceId);
            if (service == null)
            {
                throw new ArgumentException($"No service registered with ID '{serviceId}'", nameof(serviceId));
            }

            Log.Info($"Creating service: {service.Value.DisplayName}");
            return (IAIService)Activator.CreateInstance(service.Value.ServiceType)!;
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        /// <param name="serviceId">The unique ID of the service</param>
        /// <returns>True if the service is registered, false otherwise</returns>
        public static bool IsServiceRegistered(string serviceId)
        {
            lock (_lock)
            {
                return _registeredServices.ContainsKey(serviceId);
            }
        }

        /// <summary>
        /// Get the default service ID (first service by priority).
        /// </summary>
        /// <returns>The ID of the default service, or null if no services are registered</returns>
        public static string? GetDefaultServiceId()
        {
            var services = GetAllServices();
            var first = services.FirstOrDefault();
            return first.Attribute != null ? first.Id : null;
        }

        /// <summary>
        /// Clear all registered services. Primarily for testing purposes.
        /// </summary>
        internal static void ClearForTesting()
        {
            lock (_lock)
            {
                _registeredServices.Clear();
                _initialized = false;
            }
        }

        /// <summary>
        /// Reinitialize the registry. Primarily for testing purposes.
        /// </summary>
        internal static void ReinitializeForTesting()
        {
            ClearForTesting();
            Initialize();
        }

        /// <summary>
        /// Discover and register all classes with [RegisterAIService] attribute.
        /// </summary>
        private static void DiscoverAttributedServices()
        {
            try
            {
                // Get all loaded assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        DiscoverServicesInAssembly(assembly);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Some types may not be loadable, log and continue
                        Log.Warning($"Could not load all types from assembly {assembly.FullName}: {ex.Message}");

                        // Try to process the types that did load
                        foreach (var type in ex.Types.Where(t => t != null))
                        {
                            TryRegisterServiceType(type!);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error discovering services in assembly {assembly.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during service discovery: {ex.Message}");
            }
        }

        /// <summary>
        /// Discover services in a single assembly.
        /// </summary>
        private static void DiscoverServicesInAssembly(Assembly assembly)
        {
            // Skip system assemblies for performance
            var assemblyName = assembly.GetName().Name;
            if (assemblyName == null ||
                assemblyName.StartsWith("System", StringComparison.Ordinal) ||
                assemblyName.StartsWith("Microsoft", StringComparison.Ordinal) ||
                assemblyName.StartsWith("mscorlib", StringComparison.Ordinal) ||
                assemblyName.StartsWith("netstandard", StringComparison.Ordinal))
            {
                return;
            }

            foreach (var type in assembly.GetTypes())
            {
                TryRegisterServiceType(type);
            }
        }

        /// <summary>
        /// Try to register a type if it has the [RegisterAIService] attribute.
        /// </summary>
        private static void TryRegisterServiceType(Type type)
        {
            try
            {
                // Check for the attribute
                var attribute = type.GetCustomAttribute<RegisterAIServiceAttribute>();
                if (attribute == null)
                {
                    return;
                }

                // Validate the type implements IAIService
                if (!typeof(IAIService).IsAssignableFrom(type))
                {
                    Debug.LogWarning(
                        $"[AgentBridge] Type {type.FullName} has [RegisterAIService] attribute but does not implement IAIService. Skipping.");
                    return;
                }

                // Validate the type has a parameterless constructor
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogWarning(
                        $"[AgentBridge] Type {type.FullName} has [RegisterAIService] attribute but no parameterless constructor. Skipping.");
                    return;
                }

                // Skip if already registered (first one wins)
                if (_registeredServices.ContainsKey(attribute.Id))
                {
                    Log.Warning($"Duplicate service ID '{attribute.Id}' found. Type {type.FullName} will be skipped.");
                    return;
                }

                // Register the service
                var registeredService = new RegisteredService(type, attribute);
                _registeredServices[attribute.Id] = registeredService;
                Log.Info($"Discovered and registered AI service: {attribute.Id} ({attribute.DisplayName})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AgentBridge] Error registering service type {type.FullName}: {ex.Message}");
            }
        }
    }
}
