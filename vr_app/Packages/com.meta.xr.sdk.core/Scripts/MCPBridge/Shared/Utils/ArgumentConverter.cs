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
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Meta.MCPBridge.Utils
{
    internal static class ArgumentConverter
    {
        internal static object[] ConvertJObjectToArguments(JObject jObject, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var arguments = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterName = parameter.Name;
                var parameterType = parameter.ParameterType;

                // Try to get the value from JObject by parameter name
                if (jObject.TryGetValue(parameterName, StringComparison.OrdinalIgnoreCase, out var token))
                    arguments[i] = ConvertJTokenToType(token, parameterType);
                else if (parameter.HasDefaultValue)
                    // Use default value if parameter is optional
                    arguments[i] = parameter.DefaultValue;
                else if (IsNullableType(parameterType))
                    // Set to null for nullable types
                    arguments[i] = null;
                else
                    throw new ArgumentException(
                        $"Required parameter '{parameterName}' not found in JObject and has no default value.");
            }

            return arguments;
        }

        internal static object ConvertJTokenToType(JToken token, Type targetType)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                if (IsNullableType(targetType) || !targetType.IsValueType)
                    return null;
                throw new ArgumentException($"Cannot convert null to non-nullable type {targetType.Name}");
            }

            // Handle nullable types
            if (IsNullableType(targetType)) targetType = Nullable.GetUnderlyingType(targetType);

            try
            {
                // Direct conversion for primitive types and strings
                if (targetType == typeof(string))
                    return token.ToString();

                if (targetType == typeof(int))
                    return token.Value<int>();

                if (targetType == typeof(long))
                    return token.Value<long>();

                if (targetType == typeof(double))
                    return token.Value<double>();

                if (targetType == typeof(float))
                    return token.Value<float>();

                if (targetType == typeof(decimal))
                    return token.Value<decimal>();

                if (targetType == typeof(bool))
                    return token.Value<bool>();

                if (targetType == typeof(DateTime))
                    return token.Value<DateTime>();

                if (targetType == typeof(Guid))
                    return token.Value<Guid>();

                if (targetType == typeof(Vector3))
                    return JsonUtility.FromJson<Vector3>(token.ToString());

                if (targetType == typeof(Color))
                    return JsonUtility.FromJson<Color>(token.ToString());

                // Handle System.Type - useful for methods like GameObject.AddComponent(Type)
                if (targetType == typeof(Type))
                {
                    if (token.Type == JTokenType.String)
                    {
                        string typeName = token.ToString();

                        // First try Type.GetType which handles built-in types and fully qualified names
                        Type type = Type.GetType(typeName, false, true);

                        if (type != null)
                            return type;

                        // Try to find the type in common Unity assemblies
                        if (typeName.StartsWith("UnityEngine.") || typeName.StartsWith("UnityEditor."))
                        {
                            // Look in Unity assemblies
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                if (assembly.GetName().Name == "UnityEngine" ||
                                    assembly.GetName().Name == "UnityEditor" ||
                                    assembly.GetName().Name.StartsWith("UnityEngine.") ||
                                    assembly.GetName().Name.StartsWith("UnityEditor."))
                                {
                                    type = assembly.GetType(typeName, false, true);
                                    if (type != null)
                                        return type;
                                }
                            }
                        }

                        // As a last resort, search all loaded assemblies
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                type = assembly.GetType(typeName, false, true);
                                if (type != null)
                                    return type;
                            }
                            catch
                            {
                                // Ignore assembly load failures
                            }
                        }

                        throw new ArgumentException($"Could not find type '{typeName}'. Make sure the type name is fully qualified.");
                    }
                    else if (token.Type == JTokenType.Object && token is JObject typeObj)
                    {
                        // Handle case where the type is passed as an object with a "typeName" property
                        if (typeObj.TryGetValue("typeName", StringComparison.OrdinalIgnoreCase, out var typeNameToken))
                        {
                            return ConvertJTokenToType(typeNameToken, typeof(Type));
                        }
                    }

                    throw new ArgumentException($"Cannot convert JToken of type {token.Type} to System.Type. Expected a string with the type name.");
                }

                // Handle enums
                if (targetType.IsEnum)
                {
                    if (token.Type == JTokenType.String)
                        return Enum.Parse(targetType, token.Value<string>(), true);
                    return Enum.ToObject(targetType, token.Value<int>());
                }

                // Handle arrays
                if (targetType.IsArray && token is JArray jArray)
                {
                    var elementType = targetType.GetElementType();
                    var array = Array.CreateInstance(elementType, jArray.Count);

                    for (var i = 0; i < jArray.Count; i++)
                        array.SetValue(ConvertJTokenToType(jArray[i], elementType), i);

                    return array;
                }

                // Handle generic lists
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = Activator.CreateInstance(listType);
                    var addMethod = listType.GetMethod("Add");

                    if (token is JArray jArrayList)
                        foreach (var item in jArrayList)
                            addMethod.Invoke(list, new[] { ConvertJTokenToType(item, elementType) });

                    return list;
                }

                // Special handling for JObject target type
                if (targetType == typeof(JObject))
                {
                    // If the token is already a JObject, return it
                    if (token is JObject jObj)
                        return jObj;

                    // If the token is a string, try to parse it as JSON
                    if (token.Type == JTokenType.String)
                    {
                        try
                        {
                            string jsonStr = token.ToString();
                            return JObject.Parse(jsonStr);
                        }
                        catch (Exception)
                        {
                            // If parsing fails, create a new JObject with the string as a property
                            return new JObject { ["value"] = token };
                        }
                    }

                    // For other token types, create a new JObject with the token as a property
                    return new JObject { ["value"] = token };
                }

                // For complex objects, deserialize using ToObject
                return token.ToObject(targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Cannot convert JToken of type {token.Type} to {targetType.Name}: {ex.Message}",
                    ex);
            }
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
