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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Schemas;
using Meta.MCPBridge.Utils;

namespace Meta.MCPBridge.Definitions
{
    internal class Tool : Definition<ToolAttribute, ToolSchema>
    {
        private Dictionary<string, Method> _methods;

        internal Tool(Type type, ToolAttribute attribute) : base(attribute)
        {
            Type = type;
        }

        internal override string Name
        {
            get => Type.Name;
            set => throw new NotImplementedException();
        }

        internal Type Type { get; }

        internal override ToolSchema ToSchema()
        {
            var properties = CompileProperties();
            var methods = FetchAllMethods().ToArray();

            var schema = new ToolSchema
            {
                Name = Name,
                Description = string.IsNullOrEmpty(Attribute.Description) ? "undefined" : Attribute.Description,
                InputSchema = new ToolInputSchema
                {
                    Properties = properties,
                    Required = new[]
                    {
                        "method"
                    }
                }
            };

            // Add remarks if available
            if (Attribute.Remarks.Count > 0)
            {
                schema.Remarks = Attribute.Remarks.ToArray();
            }

            // Add detailed per-method schemas for AI discoverability
            if (methods.Length > 0)
            {
                schema.Methods = methods.ToDictionary(
                    method => method.Name,
                    method => BuildMethodDetailSchema(method)
                );
            }

            return schema;
        }

        /// <summary>
        /// Build detailed method schema including parameter information for AI discoverability.
        /// </summary>
        private MethodDetailSchema BuildMethodDetailSchema(Method method)
        {
            var methodDetail = new MethodDetailSchema
            {
                Description = method.Attribute.Description,
                Returns = method.Attribute.Returns,
                ReturnType = method.ReturnType
            };

            // Add method-level remarks if available
            if (method.Attribute.Remarks.Count > 0)
            {
                methodDetail.Remarks = method.Attribute.Remarks.ToArray();
            }

            // Add parameter details
            var parameters = method.MethodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                methodDetail.Parameters = new Dictionary<string, ParameterDetailSchema>();
                foreach (var param in parameters)
                {
                    methodDetail.Parameters[param.Name] = new ParameterDetailSchema
                    {
                        Type = param.ParameterType.GetJsonSchemaType(),
                        CSharpType = param.ParameterType.FullName,
                        Required = !param.HasDefaultValue,
                        Default = param.HasDefaultValue ? param.DefaultValue : null,
                        Description = param.Name // Could be enhanced with XML doc comments in the future
                    };
                }
            }

            return methodDetail;
        }

        internal bool TryFetchMethod(string name, [NotNullWhen(true)] out Method method)
        {
            FetchAllMethods();
            method = null;
            return _methods?.TryGetValue(name, out method) ?? false;
        }

        private IEnumerable<Method> FetchAllMethods()
        {
            _methods ??= Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static)
                .Where(method => method.GetCustomAttribute<ToolAttribute>() != null)
                .Select(method => new Method(this, method, method.GetCustomAttribute<ToolAttribute>()))
                .ToDictionary(method => method.Name, method => method);

            return _methods.Values;
        }

        private Dictionary<string, ToolPropertySchema> CompileProperties()
        {
            const int maxEnumCountForEmbeddedEnumeration = 5;
            var methods = FetchAllMethods().ToArray();

            var properties = new Dictionary<string, ToolPropertySchema>
            {
                ["method"] = new()
                {
                    Description = "The method to call",
                    OneOf = methods.Select(method => new EnumValueSchema
                    {
                        Const = method.Name,
                        Description = method.Attribute.Description,
                        Title = method.ReturnType != null ? $"Return type: {method.ReturnType}" : null
                    }).ToArray()
                }
            };

            var registry = new Dictionary<string, (Type, List<Method>)>();

            foreach (var method in methods)
            {
                var parameters = method.MethodInfo.GetParameters();
                foreach (var parameter in parameters)
                {
                    if (!registry.TryGetValue(parameter.Name, out var registeredParameter))
                    {
                        registeredParameter = (parameter.ParameterType, new List<Method>());
                        registry.Add(parameter.Name, registeredParameter);
                    }

                    registeredParameter.Item2.Add(method);
                }
            }

            foreach (var (name, (type, list)) in registry)
            {
                var propertyDef = new ToolPropertySchema
                {
                    Type = type.GetJsonSchemaType(),
                    CSharpType = type.FullName,
                    UsedBy = list.Select(method => method.Name).ToArray()
                };

                if (type.IsEnum)
                {
                    var names = Enum.GetNames(type);
                    if (names.Length < maxEnumCountForEmbeddedEnumeration)
                        propertyDef.Enum = Enum.GetNames(type);
                    else
                        propertyDef.Remark =
                            "Use resources and api reference to get the list of enum values and their usage.";
                }

                properties[name] = propertyDef;
            }

            return properties;
        }
    }
}
