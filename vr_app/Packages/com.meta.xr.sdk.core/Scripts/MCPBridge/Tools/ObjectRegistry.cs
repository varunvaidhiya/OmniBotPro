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
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MCPServices.Tools
{
    /// <summary>
    /// Registry for tracking objects by ID to enable serialization-friendly references
    /// </summary>
    internal class ObjectRegistry
    {
        private readonly Dictionary<string, object> _objectsById = new();
        private readonly Dictionary<object, string> _idsByObject = new();
        private int _nextId = 1;

        /// <summary>
        /// Number of objects currently registered
        /// </summary>
        internal int Count => _objectsById.Count;

        /// <summary>
        /// Register an object and get its ID
        /// </summary>
        /// <param name="obj">The object to register</param>
        /// <returns>The ID of the registered object</returns>
        internal string RegisterObject(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            // If the object is already registered, return its existing ID
            if (_idsByObject.TryGetValue(obj, out string existingId))
                return existingId;

            // Generate a new ID
            string id = GenerateId(obj);

            // Register the object
            _objectsById[id] = obj;
            _idsByObject[obj] = id;

            return id;
        }

        /// <summary>
        /// Get an object by its ID
        /// </summary>
        /// <param name="id">The ID of the object to get</param>
        /// <returns>The object with the specified ID</returns>
        internal object GetObject(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            if (_objectsById.TryGetValue(id, out object obj))
            {
                if (obj is Object unityObj && unityObj == null)
                {
                    _objectsById.Remove(id);
                    _idsByObject.Remove(obj);
                    throw new KeyNotFoundException($"Object with ID '{id}' not found in registry");
                }
                return obj;
            }

            throw new KeyNotFoundException($"Object with ID '{id}' not found in registry");
        }

        /// <summary>
        /// Try to get an object by its ID
        /// </summary>
        /// <param name="id">The ID of the object to get</param>
        /// <param name="obj">The object with the specified ID, if found</param>
        /// <returns>True if the object was found, false otherwise</returns>
        internal bool TryGetObject(string id, out object obj)
        {
            if (string.IsNullOrEmpty(id))
            {
                obj = null;
                return false;
            }

            if (_objectsById.TryGetValue(id, out obj))
            {
                if (obj is Object unityObj && unityObj == null)
                {
                    _objectsById.Remove(id);
                    _idsByObject.Remove(obj);
                    obj = null;
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if an object is registered
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if the object is registered, false otherwise</returns>
        internal bool IsRegistered(object obj)
        {
            if (obj == null)
                return false;

            return _idsByObject.ContainsKey(obj);
        }

        /// <summary>
        /// Get the ID of a registered object
        /// </summary>
        /// <param name="obj">The object to get the ID for</param>
        /// <returns>The ID of the object</returns>
        internal string GetId(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (_idsByObject.TryGetValue(obj, out string id))
                return id;

            throw new KeyNotFoundException("Object not found in registry");
        }

        /// <summary>
        /// Try to get the ID of a registered object
        /// </summary>
        /// <param name="obj">The object to get the ID for</param>
        /// <param name="id">The ID of the object, if found</param>
        /// <returns>True if the object was found, false otherwise</returns>
        internal bool TryGetId(object obj, out string id)
        {
            if (obj == null)
            {
                id = null;
                return false;
            }

            return _idsByObject.TryGetValue(obj, out id);
        }

        /// <summary>
        /// Unregister an object
        /// </summary>
        /// <param name="id">The ID of the object to unregister</param>
        /// <returns>True if the object was unregistered, false if it wasn't found</returns>
        internal bool UnregisterObject(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (_objectsById.TryGetValue(id, out object obj))
            {
                _objectsById.Remove(id);
                _idsByObject.Remove(obj);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get information about a registered object
        /// </summary>
        /// <param name="id">The ID of the object</param>
        /// <returns>A JObject containing information about the object</returns>
        internal JObject GetObjectInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            PurgeDestroyedObjects();

            if (!_objectsById.TryGetValue(id, out object obj))
                throw new KeyNotFoundException($"Object with ID '{id}' not found in registry");

            Type type = obj.GetType();

            var info = new JObject
            {
                ["id"] = id,
                ["type"] = type.FullName,
                ["typeName"] = type.Name,
                ["namespace"] = type.Namespace ?? "",
                ["assembly"] = type.Assembly.GetName().Name,
                ["isValueType"] = type.IsValueType,
                ["isPrimitive"] = type.IsPrimitive,
                ["isEnum"] = type.IsEnum,
                ["isClass"] = type.IsClass,
                ["isInterface"] = type.IsInterface,
                ["isArray"] = type.IsArray,
                ["isGenericType"] = type.IsGenericType
            };

            // Add base type information if available
            if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
            {
                info["baseType"] = type.BaseType.FullName;
            }

            // Add implemented interfaces
            var interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                var interfaceArray = new JArray();
                foreach (var iface in interfaces)
                {
                    interfaceArray.Add(iface.FullName);
                }
                info["interfaces"] = interfaceArray;
            }

            // Add additional information for specific types
            if (obj is GameObject gameObject)
            {
                info["name"] = gameObject.name;
                info["active"] = gameObject.activeSelf;
                info["layer"] = gameObject.layer;
                info["tag"] = gameObject.tag;
                info["transform"] = new JObject
                {
                    ["position"] = new JObject
                    {
                        ["x"] = gameObject.transform.position.x,
                        ["y"] = gameObject.transform.position.y,
                        ["z"] = gameObject.transform.position.z
                    },
                    ["rotation"] = new JObject
                    {
                        ["x"] = gameObject.transform.rotation.eulerAngles.x,
                        ["y"] = gameObject.transform.rotation.eulerAngles.y,
                        ["z"] = gameObject.transform.rotation.eulerAngles.z
                    },
                    ["scale"] = new JObject
                    {
                        ["x"] = gameObject.transform.localScale.x,
                        ["y"] = gameObject.transform.localScale.y,
                        ["z"] = gameObject.transform.localScale.z
                    }
                };

                // Add components
                var components = new JArray();
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component != null)
                    {
                        var componentInfo = new JObject
                        {
                            ["type"] = component.GetType().FullName,
                            ["typeName"] = component.GetType().Name
                        };

                        // Add enabled state for behaviors
                        if (component is Behaviour behaviour)
                        {
                            componentInfo["enabled"] = behaviour.enabled;
                        }

                        components.Add(componentInfo);
                    }
                }
                info["components"] = components;
            }
            else if (obj is Component component)
            {
                info["gameObject"] = component.gameObject.name;
                info["gameObjectId"] = TryGetId(component.gameObject, out string goId) ? goId : null;
                info["enabled"] = component is not Behaviour behaviour || behaviour.enabled;
            }
            else if (obj is string)
            {
                info["stringValue"] = obj.ToString();
                info["length"] = ((string)obj).Length;
            }
            else if (obj is ICollection collection)
            {
                info["count"] = collection.Count;
            }
            else
            {
                // For other types, try to add a string representation
                try
                {
                    info["toString"] = obj.ToString();
                }
                catch
                {
                    // Ignore if ToString() fails
                }
            }

            return info;
        }

        /// <summary>
        /// Get all registered objects
        /// </summary>
        /// <returns>A dictionary of all registered objects by ID</returns>
        internal Dictionary<string, object> GetAllObjects()
        {
            PurgeDestroyedObjects();
            return new Dictionary<string, object>(_objectsById);
        }

        /// <summary>
        /// Get all registered objects of a specific type
        /// </summary>
        /// <typeparam name="T">The type of objects to get</typeparam>
        /// <returns>A dictionary of all registered objects of the specified type by ID</returns>
        internal Dictionary<string, T> GetAllObjects<T>() where T : class
        {
            PurgeDestroyedObjects();
            var result = new Dictionary<string, T>();

            foreach (var kvp in _objectsById)
            {
                if (kvp.Value is T typedObj)
                {
                    result[kvp.Key] = typedObj;
                }
            }

            return result;
        }

        /// <summary>
        /// Clear the registry
        /// </summary>
        internal void Clear()
        {
            _objectsById.Clear();
            _idsByObject.Clear();
        }

        /// <summary>
        /// Remove entries for any UnityEngine.Object instances that have been destroyed.
        /// Plain C# objects are not affected.
        /// </summary>
        internal void PurgeDestroyedObjects()
        {
            List<string> staleIds = null;

            foreach (var kvp in _objectsById)
            {
                if (kvp.Value is Object unityObj && unityObj == null)
                {
                    staleIds ??= new List<string>();
                    staleIds.Add(kvp.Key);
                }
            }

            if (staleIds != null)
            {
                foreach (var id in staleIds)
                {
                    if (_objectsById.TryGetValue(id, out object obj))
                    {
                        _idsByObject.Remove(obj);
                        _objectsById.Remove(id);
                    }
                }
            }
        }

        /// <summary>
        /// Generate a unique ID for an object
        /// </summary>
        /// <param name="obj">The object to generate an ID for</param>
        /// <returns>A unique ID for the object</returns>
        private string GenerateId(object obj)
        {
            // Generate a simple unique ID without type-specific prefixes
            string id = $"{_nextId++}";
            return id;
        }
    }
}
