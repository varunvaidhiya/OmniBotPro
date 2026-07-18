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
using Meta.MCPBridge;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using Meta.MCPBridge.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MCPServices.Tools
{
    [Tool(
        "Advanced Unity reflection utilities for MCP clients. Provides complete access to Unity's runtime API through an object registry system.",
        "WHEN TO USE: Use this tool as a fallback when SceneObjectsTools cannot accomplish what you need. This tool provides deeper access to Unity but requires more setup.",
        "WORKFLOW: 1) Find existing objects with FindGameObjectsAndRegister() or create new objects with CreateInstanceFromJsonAndRegister(). " +
        "2) Use returned registry IDs with 'ById' methods for all operations. 3) Use GetObjectInfo() to understand object structure.",
        "OBJECT REGISTRY: All Unity objects are stored in a registry system that survives across multiple MCP calls. Registry IDs are NOT the same as Unity instance IDs.",
        "UNITY GAMEOBJECT INTEGRATION: Use FindGameObjectsAndRegister() to automatically find and register GameObjects by name, then use the registry ID for operations.",
        "TYPE NAMES: ALWAYS use FULL NAMESPACE when specifying type names (e.g., 'UnityEngine.GameObject', not 'GameObject'). This ensures reliable type resolution.",
        "COMMON NAMESPACES: UnityEngine.* (core Unity), UnityEngine.UI.* (UI), UnityEditor.* (editor), System.* (C# types).",
        "EXAMPLE WORKFLOW: string objId = FindGameObjectsAndRegister(\"Main Camera\"); " +
        "SetPropertyFromJsonById(objId, \"name\", \"MainCamera_Modified\"); " +
        "string transformId = GetPropertyById(objId, \"transform\"); SetPropertyFromJsonById(transformId, \"position\", {\"x\": 0, \"y\": 5, \"z\": 0});",
        "JSON PREFERRED: Always use FromJson methods (SetPropertyFromJsonById, InvokeMethodFromJsonById) as they handle type conversion automatically.",
        "ERROR HANDLING: Methods provide detailed error messages. If a registry ID is not found, the object may have been destroyed or not registered."
    )]
    internal interface IReflectionService : IService
    {
        [Tool(Description = "Create a new instance of a type by name using JSON arguments and register it in the object registry. Use full namespace (e.g., 'UnityEngine.GameObject', not 'GameObject').")]
        public string CreateInstanceFromJsonAndRegister(string typeName, JObject arguments);

        [Tool(Description = "Get information about an object in the registry")]
        public JObject GetObjectInfo(string objectId);

        [Tool(Description = "List all objects in the registry")]
        public Dictionary<string, JObject> ListObjects();

        [Tool(Description = "List all objects of a specific type in the registry")]
        public Dictionary<string, JObject> ListObjectsOfType(string typeName);

        [Tool(Description = "Get a property value from an object by ID")]
        public object GetPropertyById(string objectId, string propertyName);

        [Tool(Description = "Set a property value on an object by ID using a JSON value")]
        public void SetPropertyFromJsonById(string objectId, string propertyName, JToken value);

        [Tool(Description = "Get a field value from an object by ID")]
        public object GetFieldById(string objectId, string fieldName);

        [Tool(Description = "Set a field value on an object by ID using a JSON value")]
        public void SetFieldFromJsonById(string objectId, string fieldName, JToken value);

        [Tool("Invoke a method on an object by ID using JSON arguments",
            "Never use generic methods, use the equivalent methods that accept type parameters",
            "Make sure to use the expected parameter names")]
        public object InvokeMethodFromJsonById(string objectId, string methodName, JObject arguments);

        [Tool("Invoke a static method on a type using JSON arguments",
            "Never use generic methods, use the equivalent methods that accept type parameters",
            "Make sure to use the expected parameter names")]
        public object InvokeStaticMethodFromJson(string typeName, string methodName, JObject arguments);

        [Tool(Description = "Get a type by name from Unity assemblies")]
        public Type GetType(string typeName);

        [Tool(Description = "List all methods of a type")]
        public IEnumerable<MethodInfo> GetMethods(string typeName);

        [Tool(Description = "List all properties of a type")]
        public IEnumerable<PropertyInfo> GetProperties(string typeName);

        [Tool(Description = "List all fields of a type")]
        public IEnumerable<FieldInfo> GetFields(string typeName);

        [Tool(Description = "Check if a type exists in Unity assemblies")]
        public bool TypeExists(string typeName);

        [Tool(Description = "Get all types in a namespace")]
        public IEnumerable<Type> GetTypesInNamespace(string namespaceName);

        [Tool(Description = "Find GameObjects by name and register them in the object registry. Returns registry IDs for all matching GameObjects.")]
        public Dictionary<string, JObject> FindGameObjectsAndRegister(string gameObjectName, bool exactMatch = false, bool includeInactive = false);

        [Tool(Description = "Register an existing Unity Object (GameObject, Component, etc.) in the object registry using its Unity instance ID. Returns the registry ID.")]
        public string RegisterUnityObjectById(int unityInstanceId);

        [Tool(Description = "Get the Unity instance ID of a registered object. Useful for cross-referencing with other Unity tools.")]
        public int? GetUnityInstanceId(string registryId);

        [Tool(Description = "Find all GameObjects with a specific component type and register them. Use full namespace for componentTypeName (e.g., 'UnityEngine.Rigidbody', not 'Rigidbody'). Returns registry IDs for GameObjects that have the component.")]
        public Dictionary<string, JObject> FindGameObjectsByComponentAndRegister(string componentTypeName, bool includeInactive = false);

        [Tool(Description = "Add a component to a GameObject by registry ID. Use full namespace for componentTypeName (e.g., 'UnityEngine.BoxCollider', not 'BoxCollider'). Returns the registry ID of the new component.")]
        public string AddComponentById(string gameObjectRegistryId, string componentTypeName);

        [Tool(Description = "Remove a component from a GameObject by registry ID. Use full namespace for componentTypeName (e.g., 'UnityEngine.Rigidbody', not 'Rigidbody'). Returns true if successful.")]
        public bool RemoveComponentById(string gameObjectRegistryId, string componentTypeName);

        [Tool(Description = "Get all components of a GameObject by registry ID and register them. Returns a dictionary of component registry IDs.")]
        public Dictionary<string, JObject> GetComponentsAndRegister(string gameObjectRegistryId);

        // NOTE: GameObject active state manipulation is fully supported through existing methods:
        // - Use SetPropertyFromJsonById(gameObjectId, "activeSelf", true/false) to enable/disable GameObjects
        // - Use GetPropertyById(gameObjectId, "activeSelf") to check current active state
        // - Use InvokeMethodFromJsonById(gameObjectId, "SetActive", {"active": true/false}) to call SetActive method
        // These methods work seamlessly with GameObjects registered through FindGameObjectsAndRegister()
    }

    internal class Reflection : SingletonService<Reflection>, IReflectionService
    {
        private readonly Dictionary<string, Type> _typeCache = new();
        private readonly Dictionary<string, string> _resolvedNameCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Assembly> _unityAssemblies;
        private readonly ObjectRegistry _objectRegistry = new();

        internal static readonly Dictionary<string, string> CommonTypeAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core
            { "Transform", "UnityEngine.Transform" },
            { "GameObject", "UnityEngine.GameObject" },
            { "Component", "UnityEngine.Component" },
            { "MonoBehaviour", "UnityEngine.MonoBehaviour" },

            // Physics
            { "Rigidbody", "UnityEngine.Rigidbody" },
            { "Rigidbody2D", "UnityEngine.Rigidbody2D" },
            { "Collider", "UnityEngine.Collider" },
            { "BoxCollider", "UnityEngine.BoxCollider" },
            { "SphereCollider", "UnityEngine.SphereCollider" },
            { "CapsuleCollider", "UnityEngine.CapsuleCollider" },
            { "MeshCollider", "UnityEngine.MeshCollider" },
            { "CharacterController", "UnityEngine.CharacterController" },

            // Rendering
            { "MeshRenderer", "UnityEngine.MeshRenderer" },
            { "MeshFilter", "UnityEngine.MeshFilter" },
            { "SkinnedMeshRenderer", "UnityEngine.SkinnedMeshRenderer" },
            { "SpriteRenderer", "UnityEngine.SpriteRenderer" },
            { "LineRenderer", "UnityEngine.LineRenderer" },
            { "TrailRenderer", "UnityEngine.TrailRenderer" },

            // Common components
            { "Camera", "UnityEngine.Camera" },
            { "Light", "UnityEngine.Light" },
            { "AudioSource", "UnityEngine.AudioSource" },
            { "AudioListener", "UnityEngine.AudioListener" },

            // Animation
            { "Animator", "UnityEngine.Animator" },
            { "Animation", "UnityEngine.Animation" },
            { "ParticleSystem", "UnityEngine.ParticleSystem" },

            // UI
            { "Canvas", "UnityEngine.Canvas" },
            { "RectTransform", "UnityEngine.RectTransform" },
            { "Text", "UnityEngine.UI.Text" },
            { "Image", "UnityEngine.UI.Image" },
            { "Button", "UnityEngine.UI.Button" },

            // AI
            { "NavMeshAgent", "UnityEngine.AI.NavMeshAgent" },
        };

        public Reflection()
        {
            // Initialize with Unity assemblies
            _unityAssemblies = new HashSet<Assembly>
            {
                typeof(GameObject).Assembly, // UnityEngine.CoreModule
                AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "UnityEditor")
            };

            // Add all Unity module assemblies and assemblies that reference them.
            // In Unity 6000.x types are spread across modules (e.g. BoxCollider
            // lives in UnityEngine.PhysicsModule, Animator in
            // UnityEngine.AnimationModule). These modules reference
            // UnityEngine.CoreModule, not "UnityEngine", so the previous exact-name
            // check missed them.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var assemblyName = assembly.GetName().Name;
                    if (assemblyName != null &&
                        (assemblyName.StartsWith("UnityEngine") ||
                         assemblyName.StartsWith("UnityEditor")))
                    {
                        _unityAssemblies.Add(assembly);
                        continue;
                    }

                    if (assembly.GetReferencedAssemblies().Any(a =>
                        a.Name == "UnityEngine" || a.Name == "UnityEditor" ||
                        (a.Name != null && (a.Name.StartsWith("UnityEngine.") ||
                                            a.Name.StartsWith("UnityEditor.")))))
                    {
                        _unityAssemblies.Add(assembly);
                    }
                }
                catch
                {
                    // Ignore assemblies that can't be inspected
                }
            }
        }

        #region Object Registry Methods

        internal string CreateInstanceAndRegister(string typeName, params object[] args)
        {
            // Create the instance using the existing method
            object instance = CreateInstance(typeName, args);

            // Register the instance in the object registry
            return _objectRegistry.RegisterObject(instance);
        }

        public string CreateInstanceFromJsonAndRegister(string typeName, JObject arguments)
        {
            // Create the instance using the existing method
            object instance = CreateInstanceFromJson(typeName, arguments);

            // Register the instance in the object registry
            return _objectRegistry.RegisterObject(instance);
        }

        /// <summary>
        /// Helper method to process return values from reflection methods.
        /// If the value is a reference type (not a primitive, enum, or struct), it registers it in the ObjectRegistry and returns the ID.
        /// Otherwise, it converts the value to a JObject representation.
        /// </summary>
        private object ProcessReturnValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            Type type = value.GetType();

            // If the value is already a JObject or JToken, return it as is
            if (value is JObject || value is JToken)
            {
                return value;
            }

            // If the value is a primitive type, enum, or string, return it as is
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
            {
                return value;
            }

            // If the value is a DateTime, TimeSpan, or Guid, return it as is
            if (type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid))
            {
                return value;
            }

            // If the value is a Unity struct like Vector3, Quaternion, etc., convert it to a JObject
            if (type.IsValueType)
            {
                try
                {
                    // Try to convert to JObject using JSON serialization
                    string json = JsonUtility.ToJson(value);
                    return JObject.Parse(json);
                }
                catch
                {
                    // If conversion fails, return the value as is
                    return value;
                }
            }

            // For reference types (classes), register the object and return its ID
            return _objectRegistry.RegisterObject(value);
        }

        private object GetObject(string objectId)
        {
            return _objectRegistry.GetObject(objectId);
        }

        public JObject GetObjectInfo(string objectId)
        {
            return _objectRegistry.GetObjectInfo(objectId);
        }

        public Dictionary<string, JObject> ListObjects()
        {
            var result = new Dictionary<string, JObject>();

            foreach (var kvp in _objectRegistry.GetAllObjects())
            {
                result[kvp.Key] = _objectRegistry.GetObjectInfo(kvp.Key);
            }

            return result;
        }

        public Dictionary<string, JObject> ListObjectsOfType(string typeName)
        {
            var result = new Dictionary<string, JObject>();
            Type targetType = GetType(typeName);

            if (targetType == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            foreach (var kvp in _objectRegistry.GetAllObjects())
            {
                if (targetType.IsAssignableFrom(kvp.Value.GetType()))
                {
                    result[kvp.Key] = _objectRegistry.GetObjectInfo(kvp.Key);
                }
            }

            return result;
        }

        internal bool UnregisterObject(string objectId)
        {
            return _objectRegistry.UnregisterObject(objectId);
        }

        #endregion

        #region Object Property and Field Methods by ID

        public object GetPropertyById(string objectId, string propertyName)
        {
            object target = GetObject(objectId);
            object value = GetProperty(target, propertyName);
            return ProcessReturnValue(value);
        }

        public void SetPropertyById(string objectId, string propertyName, object value)
        {
            object target = GetObject(objectId);
            SetProperty(target, propertyName, value);
        }

        public void SetPropertyFromJsonById(string objectId, string propertyName, JToken value)
        {
            object target = GetObject(objectId);
            SetPropertyFromJson(target, propertyName, value);
        }

        public object GetFieldById(string objectId, string fieldName)
        {
            object target = GetObject(objectId);
            object value = GetField(target, fieldName);
            return ProcessReturnValue(value);
        }

        public void SetFieldById(string objectId, string fieldName, object value)
        {
            object target = GetObject(objectId);
            SetField(target, fieldName, value);
        }

        public void SetFieldFromJsonById(string objectId, string fieldName, JToken value)
        {
            object target = GetObject(objectId);
            SetFieldFromJson(target, fieldName, value);
        }

        #endregion

        #region Method Invocation Methods by ID

        internal object InvokeMethodById(string objectId, string methodName, params object[] args)
        {
            object target = GetObject(objectId);
            object result = InvokeMethod(target, methodName, args);
            return ProcessReturnValue(result);
        }

        public object InvokeMethodFromJsonById(string objectId, string methodName, JObject arguments)
        {
            object target = GetObject(objectId);
            object result = InvokeMethodFromJson(target, methodName, arguments);
            return ProcessReturnValue(result);
        }

        #endregion

        #region Unity GameObject Integration Methods

        public Dictionary<string, JObject> FindGameObjectsAndRegister(string gameObjectName, bool exactMatch = false, bool includeInactive = false)
        {
            var result = new Dictionary<string, JObject>();
            var foundObjects = new List<GameObject>();

            // Search through all loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    SearchGameObjectsRecursive(rootObj, gameObjectName, foundObjects, exactMatch, includeInactive);
                }
            }

            // Register all found objects and return their information
            foreach (var gameObject in foundObjects)
            {
                string registryId = _objectRegistry.RegisterObject(gameObject);
                result[registryId] = _objectRegistry.GetObjectInfo(registryId);
            }

            return result;
        }

        public string RegisterUnityObjectById(int unityInstanceId)
        {
            // Find the Unity object by its instance ID
            Object unityObject = FindUnityObjectByInstanceId(unityInstanceId);
            if (unityObject == null)
                throw new ArgumentException($"Unity Object with instance ID '{unityInstanceId}' not found");

            return _objectRegistry.RegisterObject(unityObject);
        }

        public int? GetUnityInstanceId(string registryId)
        {
            try
            {
                object obj = _objectRegistry.GetObject(registryId);
                if (obj is Object unityObject)
                {
#if UNITY_6000_5_OR_NEWER
                    return unityObject.GetHashCode();
#else
                    return unityObject.GetHashCode();
#endif
                }
                return null;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public Dictionary<string, JObject> FindGameObjectsByComponentAndRegister(string componentTypeName, bool includeInactive = false)
        {
            var result = new Dictionary<string, JObject>();
            var foundObjects = new List<GameObject>();

            // Resolve the component type (aliases → exact lookup → partial match)
            Type componentType = ResolveTypeName(componentTypeName);
            if (componentType == null)
                throw new ArgumentException($"Component type '{componentTypeName}' not found");

            // Search through all loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    FindGameObjectsByComponentRecursive(rootObj, componentType, foundObjects, includeInactive);
                }
            }

            // Register all found objects and return their information
            foreach (var gameObject in foundObjects)
            {
                string registryId = _objectRegistry.RegisterObject(gameObject);
                result[registryId] = _objectRegistry.GetObjectInfo(registryId);
            }

            return result;
        }

        public string AddComponentById(string gameObjectRegistryId, string componentTypeName)
        {
            GameObject gameObject = (GameObject)GetObject(gameObjectRegistryId);
            if (gameObject == null)
                throw new ArgumentException($"Object with registry ID '{gameObjectRegistryId}' is not a GameObject");

            Type componentType = GetType(componentTypeName);
            if (componentType == null)
                throw new ArgumentException($"Component type '{componentTypeName}' not found");

            if (!typeof(Component).IsAssignableFrom(componentType))
                throw new ArgumentException($"Type '{componentTypeName}' is not a Component type");

            try
            {
                Component component = gameObject.AddComponent(componentType);
                return _objectRegistry.RegisterObject(component);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add component '{componentTypeName}': {ex.Message}", ex);
            }
        }

        public bool RemoveComponentById(string gameObjectRegistryId, string componentTypeName)
        {
            GameObject gameObject = (GameObject)GetObject(gameObjectRegistryId);
            if (gameObject == null)
                throw new ArgumentException($"Object with registry ID '{gameObjectRegistryId}' is not a GameObject");

            Type componentType = GetType(componentTypeName);
            if (componentType == null)
                throw new ArgumentException($"Component type '{componentTypeName}' not found");

            try
            {
                Component component = gameObject.GetComponent(componentType);
                if (component == null)
                    return false;

                // Unregister the component from our registry if it's registered
                if (_objectRegistry.TryGetId(component, out string componentId))
                {
                    _objectRegistry.UnregisterObject(componentId);
                }

                Object.DestroyImmediate(component);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to remove component '{componentTypeName}': {ex.Message}", ex);
            }
        }

        public Dictionary<string, JObject> GetComponentsAndRegister(string gameObjectRegistryId)
        {
            GameObject gameObject = (GameObject)GetObject(gameObjectRegistryId);
            if (gameObject == null)
                throw new ArgumentException($"Object with registry ID '{gameObjectRegistryId}' is not a GameObject");

            var result = new Dictionary<string, JObject>();
            Component[] components = gameObject.GetComponents<Component>();

            foreach (var component in components)
            {
                if (component != null)
                {
                    string componentId = _objectRegistry.RegisterObject(component);
                    result[componentId] = _objectRegistry.GetObjectInfo(componentId);
                }
            }

            return result;
        }

        // Helper methods for GameObject integration
        private void SearchGameObjectsRecursive(GameObject obj, string searchPattern, List<GameObject> foundObjects, bool exactMatch, bool includeInactive)
        {
            // Skip inactive objects if not requested
            if (!includeInactive && !obj.activeSelf) return;

            bool matches = exactMatch
                ? obj.name.Equals(searchPattern, StringComparison.OrdinalIgnoreCase)
                : obj.name.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0;

            if (matches)
            {
                foundObjects.Add(obj);
            }

            foreach (Transform child in obj.transform)
            {
                SearchGameObjectsRecursive(child.gameObject, searchPattern, foundObjects, exactMatch, includeInactive);
            }
        }

        private void FindGameObjectsByComponentRecursive(GameObject obj, Type componentType, List<GameObject> foundObjects, bool includeInactive)
        {
            // Skip inactive objects if not requested
            if (!includeInactive && !obj.activeSelf) return;

            Component component = obj.GetComponent(componentType);
            if (component != null)
            {
                foundObjects.Add(obj);
            }

            foreach (Transform child in obj.transform)
            {
                FindGameObjectsByComponentRecursive(child.gameObject, componentType, foundObjects, includeInactive);
            }
        }

        private Object FindUnityObjectByInstanceId(int instanceId)
        {
            // Search through all loaded scenes for GameObjects and their Components.
            // This covers all scene objects the AI agent interacts with.
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    var result = FindUnityObjectByInstanceIdRecursive(rootObj, instanceId);
                    if (result != null) return result;
                }
            }

            // Non-scene objects (assets like textures, materials, ScriptableObjects) are not
            // searched. The previous fallback used Resources.FindObjectsOfTypeAll<Object>()
            // which iterates every object in memory — catastrophic on Quest (seconds of stall,
            // massive GC pressure). The AI agent works with GameObjects and Components; looking
            // up assets by instance ID is not a supported workflow.
            return null;
        }

        private Object FindUnityObjectByInstanceIdRecursive(GameObject obj, int instanceId)
        {
            // Check the GameObject itself
#if UNITY_6000_5_OR_NEWER
            if (obj.GetHashCode() == instanceId)
#else
            if (obj.GetHashCode() == instanceId)
#endif
                return obj;

            // Check all components on this GameObject
            Component[] components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
#if UNITY_6000_5_OR_NEWER
                if (component != null && component.GetHashCode() == instanceId)
#else
                if (component != null && component.GetHashCode() == instanceId)
#endif
                    return component;
            }

            // Check children recursively
            foreach (Transform child in obj.transform)
            {
                var result = FindUnityObjectByInstanceIdRecursive(child.gameObject, instanceId);
                if (result != null) return result;
            }

            return null;
        }

        private Type FindTypeByPartialName(string partialName)
        {
            foreach (var assembly in _unityAssemblies)
            {
                if (assembly == null) continue;

                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    // Prefer exact matches first
                    var exactMatch = types.FirstOrDefault(t => t.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase));
                    if (exactMatch != null) return exactMatch;

                    // Then prefer types that start with the partial name
                    var startsWith = types.FirstOrDefault(t => t.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase));
                    if (startsWith != null) return startsWith;

                    // Finally, return any match
                    if (types.Count > 0) return types[0];
                }
                catch
                {
                    // Ignore assembly load failures
                }
            }

            return null;
        }

        #endregion

        private object CreateInstance(string typeName, params object[] args)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found. Use full namespace (e.g., 'UnityEngine.GameObject' instead of 'GameObject').");

            try
            {
                return Activator.CreateInstance(type, args);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of '{typeName}': {ex.Message}", ex);
            }
        }

        private object CreateInstanceFromJson(string typeName, JObject arguments)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            try
            {
                // Ensure arguments is a valid JObject
                JObject argsObj = EnsureJObject(arguments);

                // Special case for handling {"args": [...]} format
                if (argsObj.TryGetValue("args", out JToken argsToken) && argsToken is JArray argsArray)
                {
                    // Convert JArray to object[] and use the regular CreateInstance method
                    object[] args = argsArray.Select(token => token.ToObject<object>()).ToArray();
                    return CreateInstance(typeName, args);
                }

                // Get all constructors for the type
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

                if (constructors.Length == 0)
                {
                    // If no internal constructors, try to use the default constructor
                    return Activator.CreateInstance(type);
                }

                // Find the best matching constructor based on the JSON arguments
                ConstructorInfo bestMatch = null;
                object[] bestMatchArgs = null;

                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();

                    // Skip constructors with more required parameters than we have arguments
                    int requiredParams = parameters.Count(p => !p.IsOptional);
                    if (requiredParams > argsObj.Count)
                        continue;

                    try
                    {
                        // Try to convert the JSON arguments to the constructor parameters
                        var args = new object[parameters.Length];
                        bool allParamsMatched = true;

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var param = parameters[i];
                            if (argsObj.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, out var token))
                            {
                                args[i] = ArgumentConverter.ConvertJTokenToType(token, param.ParameterType);
                            }
                            else if (param.IsOptional)
                            {
                                args[i] = param.DefaultValue;
                            }
                            else
                            {
                                allParamsMatched = false;
                                break;
                            }
                        }

                        if (allParamsMatched)
                        {
                            bestMatch = constructor;
                            bestMatchArgs = args;
                            break;
                        }
                    }
                    catch
                    {
                        // If conversion fails, try the next constructor
                        continue;
                    }
                }

                if (bestMatch != null)
                {
                    return bestMatch.Invoke(bestMatchArgs);
                }

                // If no constructor matches the arguments, try to create an instance and set properties
                var instance = Activator.CreateInstance(type);

                // Set properties based on the JSON arguments
                foreach (var prop in argsObj.Properties())
                {
                    var property = type.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            var value = ArgumentConverter.ConvertJTokenToType(prop.Value, property.PropertyType);
                            property.SetValue(instance, value);
                        }
                        catch
                        {
                            // Ignore properties that can't be set
                        }
                    }
                }

                return instance;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of '{typeName}' from JSON: {ex.Message}", ex);
            }
        }

        private object GetProperty(object target, string propertyName)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{type.FullName}'");

            try
            {
                return property.GetValue(target);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get property '{propertyName}': {ex.Message}", ex);
            }
        }

        private void SetProperty(object target, string propertyName, object value)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{type.FullName}'");

            if (!property.CanWrite)
                throw new InvalidOperationException($"Property '{propertyName}' is read-only");

            try
            {
                // Try to convert the value to the property type if needed
                object convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set property '{propertyName}': {ex.Message}", ex);
            }
        }

        private void SetPropertyFromJson(object target, string propertyName, JToken value)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type '{type.FullName}'");

            if (!property.CanWrite)
                throw new InvalidOperationException($"Property '{propertyName}' is read-only");

            try
            {
                // Convert the JSON value to the property type
                object convertedValue = ArgumentConverter.ConvertJTokenToType(value, property.PropertyType);
                property.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set property '{propertyName}' from JSON: {ex.Message}", ex);
            }
        }

        private object GetField(object target, string fieldName)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            FieldInfo field = type.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found on type '{type.FullName}'");

            try
            {
                return field.GetValue(target);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get field '{fieldName}': {ex.Message}", ex);
            }
        }

        private void SetField(object target, string fieldName, object value)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            FieldInfo field = type.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found on type '{type.FullName}'");

            try
            {
                // Try to convert the value to the field type if needed
                object convertedValue = ConvertValue(value, field.FieldType);
                field.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set field '{fieldName}': {ex.Message}", ex);
            }
        }

        private void SetFieldFromJson(object target, string fieldName, JToken value)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();
            FieldInfo field = type.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found on type '{type.FullName}'");

            try
            {
                // Convert the JSON value to the field type
                object convertedValue = ArgumentConverter.ConvertJTokenToType(value, field.FieldType);
                field.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set field '{fieldName}' from JSON: {ex.Message}", ex);
            }
        }

        private object InvokeMethod(object target, string methodName, params object[] args)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();

            // Try to find a method with the exact parameter count first
            MethodInfo method = FindBestMatchingMethod(type, methodName, args);

            if (method == null)
                throw new ArgumentException($"Method '{methodName}' with matching parameters not found on type '{type.FullName}'");

            try
            {
                // Convert arguments to match parameter types
                object[] convertedArgs = ConvertArguments(args, method.GetParameters());
                object result = method.Invoke(target, convertedArgs);
                return ProcessReturnValue(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invoke method '{methodName}': {ex.Message}", ex);
            }
        }

        private object InvokeMethodFromJson(object target, string methodName, JObject arguments)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Type type = target.GetType();

            // Ensure arguments is a valid JObject
            JObject argsObj = EnsureJObject(arguments);

            // Special case for handling {"args": [...]} format
            if (argsObj.TryGetValue("args", out JToken argsToken) && argsToken is JArray argsArray)
            {
                // Convert JArray to object[] and use the regular InvokeMethod method
                object[] args = argsArray.Select(token => token.ToObject<object>()).ToArray();
                return InvokeMethod(target, methodName, args);
            }

            // Find all methods with the given name
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (methods.Count == 0)
                throw new ArgumentException($"Method '{methodName}' not found on type '{type.FullName}'");

            // Try to find the best matching method based on the JSON arguments
            foreach (var method in methods)
            {
                try
                {
                    // Convert JSON arguments to method parameters
                    object[] args = ArgumentConverter.ConvertJObjectToArguments(argsObj, method);
                    object result = method.Invoke(target, args);
                    return ProcessReturnValue(result);
                }
                catch
                {
                    // If conversion fails, try the next method
                    continue;
                }
            }

            throw new ArgumentException($"No compatible overload of method '{methodName}' found for the provided JSON arguments");
        }

        private object InvokeStaticMethod(string typeName, string methodName, params object[] args)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            // Try to find a method with the exact parameter count first
            MethodInfo method = FindBestMatchingMethod(type, methodName, args, true);

            if (method == null)
                throw new ArgumentException($"Static method '{methodName}' with matching parameters not found on type '{typeName}'");

            try
            {
                // Convert arguments to match parameter types
                object[] convertedArgs = ConvertArguments(args, method.GetParameters());
                object result = method.Invoke(null, convertedArgs);
                return ProcessReturnValue(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invoke static method '{methodName}': {ex.Message}", ex);
            }
        }

        public object InvokeStaticMethodFromJson(string typeName, string methodName, JObject arguments)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            // Ensure arguments is a valid JObject
            JObject argsObj = EnsureJObject(arguments);

            // Special case for handling {"args": [...]} format
            if (argsObj.TryGetValue("args", out JToken argsToken) && argsToken is JArray argsArray)
            {
                // Convert JArray to object[] and use the regular InvokeStaticMethod method
                object[] args = argsArray.Select(token => token.ToObject<object>()).ToArray();
                return InvokeStaticMethod(typeName, methodName, args);
            }

            // Find all static methods with the given name
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (methods.Count == 0)
                throw new ArgumentException($"Static method '{methodName}' not found on type '{typeName}'");

            // Try to find the best matching method based on the JSON arguments
            foreach (var method in methods)
            {
                try
                {
                    // Convert JSON arguments to method parameters
                    object[] args = ArgumentConverter.ConvertJObjectToArguments(argsObj, method);
                    object result = method.Invoke(null, args);
                    return ProcessReturnValue(result);
                }
                catch
                {
                    // If conversion fails, try the next method
                    continue;
                }
            }

            throw new ArgumentException($"No compatible overload of static method '{methodName}' found for the provided JSON arguments");
        }

        internal Type ResolveTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Check resolved name cache first
            if (_resolvedNameCache.TryGetValue(typeName, out string cachedResolved))
                return GetType(cachedResolved);

            // Check alias dictionary for common short names
            if (CommonTypeAliases.TryGetValue(typeName, out string qualifiedName))
            {
                Type aliasType = GetType(qualifiedName);
                if (aliasType != null)
                {
                    _resolvedNameCache[typeName] = qualifiedName;
                    return aliasType;
                }
            }

            // Try exact GetType lookup (handles fully qualified names)
            Type type = GetType(typeName);
            if (type != null)
            {
                _resolvedNameCache[typeName] = typeName;
                return type;
            }

            // Fall back to expensive partial name search
            type = FindTypeByPartialName(typeName);
            if (type != null)
            {
                _resolvedNameCache[typeName] = type.FullName;
                return type;
            }

            return null;
        }

        public Type GetType(string typeName)
        {
            // Check cache first
            if (_typeCache.TryGetValue(typeName, out Type cachedType))
                return cachedType;

            // Try to find the type in Unity assemblies
            foreach (var assembly in _unityAssemblies)
            {
                if (assembly == null) continue;

                try
                {
                    Type type = assembly.GetType(typeName, false, true);
                    if (type != null)
                    {
                        _typeCache[typeName] = type;
                        return type;
                    }
                }
                catch
                {
                    // Ignore assembly load failures
                }
            }

            // Try Type.GetType as a fallback (handles built-in types and fully qualified names)
            try
            {
                Type type = Type.GetType(typeName, false, true);
                if (type != null)
                {
                    _typeCache[typeName] = type;
                    return type;
                }
            }
            catch
            {
                // Ignore type load failures
            }

            return null;
        }

        public IEnumerable<MethodInfo> GetMethods(string typeName)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
        }

        public IEnumerable<PropertyInfo> GetProperties(string typeName)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
        }

        public IEnumerable<FieldInfo> GetFields(string typeName)
        {
            Type type = GetType(typeName);
            if (type == null)
                throw new ArgumentException($"Type '{typeName}' not found");

            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static);
        }

        public bool TypeExists(string typeName)
        {
            return GetType(typeName) != null;
        }

        public IEnumerable<Type> GetTypesInNamespace(string namespaceName)
        {
            List<Type> types = new List<Type>();

            foreach (var assembly in _unityAssemblies)
            {
                if (assembly == null) continue;

                try
                {
                    types.AddRange(assembly.GetTypes()
                        .Where(t => t.Namespace == namespaceName));
                }
                catch
                {
                    // Ignore assembly load failures
                }
            }

            return types;
        }

        // Helper methods
        private MethodInfo FindBestMatchingMethod(Type type, string methodName, object[] args, bool staticOnly = false)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic;
            flags |= staticOnly ? BindingFlags.Static : (BindingFlags.Instance | BindingFlags.Static);

            // Get all methods with the given name
            var methods = type.GetMethods(flags)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (methods.Count == 0)
                return null;

            // First try: exact parameter count match
            var exactMatches = methods.Where(m => m.GetParameters().Length == args.Length).ToList();

            if (exactMatches.Count == 1)
                return exactMatches[0];

            if (exactMatches.Count > 1)
            {
                // Try to find the best match based on parameter types
                foreach (var method in exactMatches)
                {
                    var parameters = method.GetParameters();
                    bool isMatch = true;

                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] != null && !parameters[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                        {
                            // Check if we can convert the argument to the parameter type
                            try
                            {
                                ConvertValue(args[i], parameters[i].ParameterType);
                            }
                            catch
                            {
                                isMatch = false;
                                break;
                            }
                        }
                    }

                    if (isMatch)
                        return method;
                }
            }

            // If no exact match, try methods with optional parameters or params array
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();

                // Check if the method has fewer required parameters than provided args
                int requiredParams = parameters.Count(p => !p.IsOptional && !p.IsDefined(typeof(ParamArrayAttribute), false));

                if (requiredParams <= args.Length)
                {
                    // Check if the last parameter is a params array
                    bool hasParamsArray = parameters.Length > 0 &&
                                         parameters[parameters.Length - 1].IsDefined(typeof(ParamArrayAttribute), false);

                    if (hasParamsArray || parameters.Length >= args.Length)
                    {
                        return method;
                    }
                }
            }

            // If still no match, return the first method with the correct name as a fallback
            return methods.FirstOrDefault();
        }

        private object[] ConvertArguments(object[] args, ParameterInfo[] parameters)
        {
            object[] convertedArgs = new object[parameters.Length];

            // Handle regular parameters
            for (int i = 0; i < Math.Min(args.Length, parameters.Length); i++)
            {
                convertedArgs[i] = ConvertValue(args[i], parameters[i].ParameterType);
            }

            // Fill in any missing parameters with default values
            for (int i = args.Length; i < parameters.Length; i++)
            {
                if (parameters[i].IsOptional)
                {
                    convertedArgs[i] = parameters[i].DefaultValue;
                }
                else if (parameters[i].IsDefined(typeof(ParamArrayAttribute), false))
                {
                    // Create an empty array for the params parameter
                    Type elementType = parameters[i].ParameterType.GetElementType();
                    convertedArgs[i] = Array.CreateInstance(elementType, 0);
                }
            }

            return convertedArgs;
        }

        private object ConvertValue(object value, Type targetType)
        {
            if (value == null)
            {
                if (targetType.IsValueType && !IsNullableType(targetType))
                {
                    throw new InvalidCastException($"Cannot convert null to value type {targetType.Name}");
                }
                return null;
            }

            Type sourceType = value.GetType();

            // If the value is already of the correct type, return it
            if (targetType.IsAssignableFrom(sourceType))
            {
                return value;
            }

            // Handle nullable types
            if (IsNullableType(targetType))
            {
                Type underlyingType = Nullable.GetUnderlyingType(targetType);
                return ConvertValue(value, underlyingType);
            }

            // Handle enum types
            if (targetType.IsEnum)
            {
                if (value is string stringValue)
                {
                    return Enum.Parse(targetType, stringValue, true);
                }
                else if (value is IConvertible)
                {
                    return Enum.ToObject(targetType, value);
                }
            }

            // Handle primitive type conversions
            if (value is IConvertible convertible &&
                (targetType.IsPrimitive || targetType == typeof(string) || targetType == typeof(decimal)))
            {
                try
                {
                    return Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // Fall through to other conversion methods
                }
            }

            // Handle Unity-specific conversions
            if (targetType == typeof(Vector2) && value is Vector3 v3)
            {
                return new Vector2(v3.x, v3.y);
            }
            else if (targetType == typeof(Vector3) && value is Vector2 v2)
            {
                return new Vector3(v2.x, v2.y, 0);
            }
            else if (targetType == typeof(Vector3) && value is Vector4 v4)
            {
                return new Vector3(v4.x, v4.y, v4.z);
            }

            // Try using a type converter as a last resort
            try
            {
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(sourceType))
                {
                    return converter.ConvertFrom(value);
                }
            }
            catch
            {
                // Ignore conversion failures
            }

            throw new InvalidCastException($"Cannot convert from {sourceType.Name} to {targetType.Name}");
        }

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        // Helper method to ensure we have a valid JObject
        private JObject EnsureJObject(JObject obj)
        {
            if (obj == null)
                return new JObject();

            // If the object is actually a string (happens when JSON is double-serialized)
            if (obj.Count == 1 && obj.Properties().First().Value.Type == JTokenType.String)
            {
                var prop = obj.Properties().First();
                if (prop.Name == "arguments" || prop.Name == "args")
                {
                    try
                    {
                        // Try to parse the string value as JSON
                        string jsonStr = prop.Value.ToString();
                        return JObject.Parse(jsonStr);
                    }
                    catch
                    {
                        // If parsing fails, return the original object
                    }
                }
            }

            return obj;
        }
    }
}


