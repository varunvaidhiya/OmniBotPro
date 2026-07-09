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
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Meta.MCPBridge.Utils
{
    internal static class ReturnValueConverter
    {
        internal static JObject ConvertReturnValueToJObject(object returnValue)
        {
            if (returnValue == null) return new JObject { ["value"] = null, ["type"] = "null" };

            var result = new JObject();
            var valueType = returnValue.GetType();

            // Add type information
            result["type"] = valueType.Name;
            result["fullType"] = valueType.FullName;

            // Convert the actual value
            result["value"] = ConvertToJToken(returnValue);

            return result;
        }

        internal static JToken ConvertReturnValueToJObjectSimple(object returnValue)
        {
            return ConvertToJToken(returnValue);
        }

        private static JToken ConvertToJToken(object value)
        {
            if (value == null)
                return JValue.CreateNull();

            var valueType = value.GetType();

            // Handle primitive types and strings
            if (IsPrimitiveType(valueType)) return JToken.FromObject(value);

            // Handle DateTime specially for better serialization
            if (valueType == typeof(DateTime) || valueType == typeof(DateTime?)) return JToken.FromObject(value);

            // Handle Guid
            if (valueType == typeof(Guid) || valueType == typeof(Guid?)) return JToken.FromObject(value.ToString());

            if (valueType == typeof(Vector3) || valueType == typeof(Vector3?)) return JsonUtility.ToJson(value);

            // Handle enums
            if (valueType.IsEnum)
                return new JObject
                {
                    ["name"] = value.ToString(),
                    ["value"] = Convert.ToInt32(value)
                };

            if (typeof(TypeInfo).IsAssignableFrom(valueType))
            {
                if (value is TypeInfo type)
                {
                    return JToken.FromObject(type.FullName);
                }
            }

            if (typeof(MethodInfo).IsAssignableFrom(valueType))
            {
                if (value is MethodInfo type)
                {
                    return JToken.FromObject(type.Name);
                }
            }

            if (typeof(FieldInfo).IsAssignableFrom(valueType))
            {
                if (value is FieldInfo type)
                {
                    return JToken.FromObject(type.Name);
                }
            }

            if (typeof(MemberInfo).IsAssignableFrom(valueType))
            {
                if (value is MemberInfo type)
                {
                    return JToken.FromObject(type.Name);
                }
            }

            // Handle nullable enums
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>)
                                        && Nullable.GetUnderlyingType(valueType).IsEnum)
            {
                var underlyingType = Nullable.GetUnderlyingType(valueType);
                return new JObject
                {
                    ["name"] = value.ToString(),
                    ["value"] = Convert.ToInt32(value)
                };
            }

            // Handle collections (arrays, lists, etc.)
            if (value is IEnumerable enumerable && !(value is string))
            {
                var jArray = new JArray();
                foreach (var item in enumerable) jArray.Add(ConvertToJToken(item));

                return jArray;
            }

            // Handle dictionaries
            if (value is IDictionary dictionary)
            {
                var jObject = new JObject();
                foreach (DictionaryEntry entry in dictionary)
                {
                    var key = entry.Key?.ToString() ?? "null";
                    jObject[key] = ConvertToJToken(entry.Value);
                }

                return jObject;
            }

            // Handle generic dictionaries
            if (valueType.IsGenericType &&
                (valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                 valueType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                var jObject = new JObject();
                var keysProperty = valueType.GetProperty("Keys");
                var valuesProperty = valueType.GetProperty("Values");
                var indexer = valueType.GetProperty("Item");

                if (keysProperty != null && indexer != null)
                {
                    var keys = (IEnumerable)keysProperty.GetValue(value);
                    foreach (var key in keys)
                    {
                        var keyStr = key?.ToString() ?? "null";
                        var val = indexer.GetValue(value, new[] { key });
                        jObject[keyStr] = ConvertToJToken(val);
                    }
                }

                return jObject;
            }

            // Handle complex objects using reflection
            return ConvertComplexObjectToJObject(value);
        }

        private static JObject ConvertComplexObjectToJObject(object obj)
        {
            var jObject = new JObject();
            var type = obj.GetType();

            // Get all internal properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            foreach (var property in properties)
                try
                {
                    var propertyValue = property.GetValue(obj);
                    var propertyName = property.Name;

                    // Use camelCase for JSON property names
                    var jsonPropertyName = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);

                    jObject[jsonPropertyName] = ConvertToJToken(propertyValue);
                }
                catch (Exception ex)
                {
                    // If we can't read a property, add an error indicator
                    jObject[property.Name] = $"[Error reading property: {ex.Message}]";
                }

            // Get all internal fields if no properties found or if it's a simple data structure
            if (!jObject.HasValues)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                    try
                    {
                        var fieldValue = field.GetValue(obj);
                        var fieldName = field.Name;

                        // Use camelCase for JSON field names
                        var jsonFieldName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);

                        jObject[jsonFieldName] = ConvertToJToken(fieldValue);
                    }
                    catch (Exception ex)
                    {
                        jObject[field.Name] = $"[Error reading field: {ex.Message}]";
                    }
            }

            return jObject;
        }

        private static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    IsPrimitiveType(Nullable.GetUnderlyingType(type)));
        }
    }
}
