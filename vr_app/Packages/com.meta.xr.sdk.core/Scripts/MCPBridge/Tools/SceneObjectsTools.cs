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

using Meta.MCPBridge.Attributes;
using Meta.MCPBridge.Services;
using Meta.XR.ImmersiveDebugger.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MCPServices.Tools
{
    /// <summary>
    /// Unity Scene Object Reflection Tools for MCP Clients.
    /// Provides comprehensive access to Unity GameObjects and Components using instance IDs.
    /// </summary>
    [Tool(
        "PRIMARY Unity scene object manipulation tool. Use FIRST for all GameObject and Component operations.",
        "WHEN TO USE: Always use this tool BEFORE Reflection tool for Unity object operations.",
        "WORKFLOW: 1) SearchGameObjects/GetSceneHierarchy 2) InspectGameObject 3) InspectComponentMembers 4) SetComponentValue or InvokeComponentMethod.",
        "IMPORTANT: Uses Unity instance IDs for direct object identification. Store and reuse IDs for subsequent operations.",
        "VALUE FORMATS for SetComponentValue: string as-is, int/float/double/bool as literals, Vector3 as \"(x,y,z)\", Vector2 as \"(x,y)\", Color as \"(r,g,b)\" or \"(r,g,b,a)\" with float components 0-1. Parentheses are optional."
    )]
    internal class SceneObjectsTools : SingletonService<SceneObjectsTools>
    {
        [Tool(Description = "Search for GameObjects by name pattern (supports partial matches).",
            Returns = "List of matching GameObjects with instance IDs, paths, active status, and component counts.")]
        internal string SearchGameObjects(string searchPattern, bool exactMatch = false, bool includeInactive = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"GameObject Search Results for '{searchPattern}' (Exact: {exactMatch}, Include Inactive: {includeInactive}):");
            sb.AppendLine();

            var foundObjects = new List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    SearchRecursive(rootObj, searchPattern, foundObjects, exactMatch, includeInactive);
                }
            }

            if (foundObjects.Count == 0)
            {
                sb.AppendLine($"ERROR: No GameObjects found matching '{searchPattern}'.");
                sb.AppendLine("TIP: Try partial names or use GetSceneHierarchy() to browse all objects.");
                return sb.ToString();
            }

            sb.AppendLine($"SUCCESS: Found {foundObjects.Count} GameObject(s):");
            for (int i = 0; i < foundObjects.Count; i++)
            {
                var obj = foundObjects[i];
                var components = obj.GetComponents<Component>();
                sb.AppendLine($"{i + 1}. {obj.name}");
#if UNITY_6000_5_OR_NEWER
                sb.AppendLine($"   ID: {obj.GetEntityId()} <- Use this ID in other tools");
#else
                sb.AppendLine($"   ID: {obj.GetHashCode()} <- Use this ID in other tools");
#endif
                sb.AppendLine($"   Path: {GetGameObjectPath(obj)}");
                sb.AppendLine($"   Active: {obj.activeSelf} (Scene: {obj.scene.name})");
                sb.AppendLine($"   Components: {components.Length} ({string.Join(", ", components.Take(3).Select(c => c?.GetType().Name ?? "Missing"))}{(components.Length > 3 ? ", ..." : "")})");
                sb.AppendLine();
            }
            sb.AppendLine("NEXT STEPS: Use the ID numbers in InspectGameObject(), SetComponentValue(), etc.");
            return sb.ToString();
        }

        [Tool(Description = "Find GameObjects by component type.",
            Returns = "List of GameObjects with specified component type and their instance IDs.")]
        internal string FindGameObjectsByComponent(string componentTypeName, bool includeInactive = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"GameObjects with Component '{componentTypeName}' (Include Inactive: {includeInactive}):");
            sb.AppendLine();

            var foundObjects = new List<GameObject>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    FindByComponentRecursive(rootObj, componentTypeName, foundObjects, includeInactive);
                }
            }

            if (foundObjects.Count == 0)
            {
                sb.AppendLine($"ERROR: No GameObjects found with component '{componentTypeName}'.");
                return sb.ToString();
            }

            sb.AppendLine($"SUCCESS: Found {foundObjects.Count} GameObject(s):");
            foreach (var obj in foundObjects)
            {
#if UNITY_6000_5_OR_NEWER
                sb.AppendLine($"TARGET: {obj.name} (ID: {obj.GetEntityId()})");
#else
                sb.AppendLine($"TARGET: {obj.name} (ID: {obj.GetHashCode()})");
#endif
                sb.AppendLine($"  Path: {GetGameObjectPath(obj)}");
                sb.AppendLine($"  Active: {obj.activeSelf}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        [Tool(Description = "Get information about a GameObject using its instance ID.",
            Returns = "GameObject info including name, active status, position, rotation, scale, and components.")]
        internal string InspectGameObject(int instanceId)
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null)
                return $"GameObject with instance ID '{instanceId}' not found. Use SearchGameObjects to find the correct ID.";

            var sb = new StringBuilder();
            sb.AppendLine($"GameObject: {obj.name} (ID: {instanceId})");
            sb.AppendLine($"Path: {GetGameObjectPath(obj)}");
            sb.AppendLine($"ACTIVE STATE: {(obj.activeSelf ? "ENABLED" : "DISABLED")}");
            sb.AppendLine($"Layer: {LayerMask.LayerToName(obj.layer)}");
            sb.AppendLine($"Position: {obj.transform.position}");
            sb.AppendLine($"Rotation: {obj.transform.rotation.eulerAngles}");
            sb.AppendLine($"Scale: {obj.transform.localScale}");
            sb.AppendLine();
            sb.AppendLine("Components:");

            Component[] components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                string componentName = component.GetType().Name;
                string enabledStatus = GetComponentEnabledStatus(component);
                sb.AppendLine($"- {componentName} {enabledStatus}");
            }
            return sb.ToString();
        }

        [Tool(Description = "Get scene hierarchy with GameObject IDs.",
            Returns = "Scene hierarchy with names and IDs for precise identification.")]
        internal string GetSceneHierarchy()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Scene Hierarchy (with IDs for precise identification):");
            sb.AppendLine();

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                sb.AppendLine($"Scene: {scene.name}");
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (var obj in rootObjects)
                {
                    GetHierarchyRecursive(obj, 1, sb);
                }
                sb.AppendLine();
            }
            sb.AppendLine("Use the ID numbers in other tools for precise object identification.");
            return sb.ToString();
        }

        [Tool(Description = "Get component member details (properties, fields) with values and settability.",
            Returns = "Component members with names, values, types, and whether they can be modified.")]
        internal string InspectComponentMembers(int instanceId, string componentName, string memberFilter = "")
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null)
                return $"GameObject with instance ID '{instanceId}' not found.";

            Component component = FindComponent(obj, componentName);
            if (component == null)
            {
                var availableComponents = string.Join(", ", obj.GetComponents<Component>().Select(c => c.GetType().Name));
                return $"Component '{componentName}' not found. Available: {availableComponents}";
            }

            return InspectComponentMembersInternal(component, componentName, obj.name, memberFilter);
        }

        [Tool(Description = "Set a property or field value on a component. Pass value as string: int/float/double/bool as literals, Vector3 as \"(x,y,z)\", Vector2 as \"(x,y)\", Color as \"(r,g,b,a)\" with floats 0-1. Parentheses optional.",
            Returns = "Success confirmation with old and new values, or error with troubleshooting tips.")]
        internal string SetComponentValue(int instanceId, string componentName, string memberName, string value)
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null)
                return $"ERROR: GameObject with instance ID {instanceId} not found.";

            Component component = FindComponent(obj, componentName);
            if (component == null)
                return $"ERROR: Component '{componentName}' not found on '{obj.name}'.";

            var memberInfo = FindMember(component.GetType(), memberName);
            if (memberInfo == null)
                return $"ERROR: Member '{memberName}' not found on component '{componentName}'.";

            if (!memberInfo.CanBeChanged())
                return $"ERROR: Member '{memberName}' cannot be changed (read-only).";

            if (memberInfo is PropertyInfo prop && !prop.CanWrite)
                return $"ERROR: Property '{memberName}' is read-only.";

            try
            {
                var currentValue = GetMemberValueSafely(memberInfo, component);
                var targetType = memberInfo.GetDataType();
                object convertedValue = ConvertStringToType(value, targetType);
                memberInfo.SetValue(component, convertedValue);
                var newValue = GetMemberValueSafely(memberInfo, component);

                return $"SUCCESS: Updated {componentName}.{memberName} on '{obj.name}' (ID: {instanceId})\n" +
                       $"Changed from: {FormatValueForDisplay(currentValue)}\n" +
                       $"Changed to: {FormatValueForDisplay(newValue)}";
            }
            catch (Exception ex)
            {
                return $"ERROR: Failed to set {memberName} = {value}: {ex.Message}";
            }
        }

        [Tool(Description = "Get current value of a property or field on a component.",
            Returns = "Current value with type information.")]
        internal string GetComponentValue(int instanceId, string componentName, string memberName)
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null) return $"ERROR: GameObject with instance ID {instanceId} not found.";

            Component component = FindComponent(obj, componentName);
            if (component == null) return $"ERROR: Component '{componentName}' not found.";

            var memberInfo = FindMember(component.GetType(), memberName);
            if (memberInfo == null) return $"ERROR: Member '{memberName}' not found.";

            try
            {
                var value = GetMemberValueSafely(memberInfo, component);
                var targetType = memberInfo.GetDataType();
                return $"DATA: {componentName}.{memberName} = {FormatValueForDisplay(value)} (Type: {targetType?.Name})";
            }
            catch (Exception ex)
            {
                return $"ERROR: Failed to get value: {ex.Message}";
            }
        }

        [Tool(Description = "Set the active state of a GameObject.",
            Returns = "Success confirmation with new active state.")]
        internal string SetGameObjectActive(int instanceId, bool active)
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null) return $"ERROR: GameObject with instance ID {instanceId} not found.";

            try
            {
                bool previousState = obj.activeSelf;
                obj.SetActive(active);
                return $"SUCCESS: '{obj.name}' active state changed from {previousState} to {obj.activeSelf}";
            }
            catch (Exception ex)
            {
                return $"ERROR: Failed to set active state: {ex.Message}";
            }
        }

        [Tool(Description = "Invoke a method on a component. Optional args as comma-separated strings.",
            Returns = "Success confirmation (with return value if non-void) or error with available methods.")]
        internal string InvokeComponentMethod(int instanceId, string componentName, string methodName, string methodArgs = "")
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null) return $"ERROR: GameObject with instance ID {instanceId} not found.";

            Component component = FindComponent(obj, componentName);
            if (component == null) return $"ERROR: Component '{componentName}' not found.";

            bool hasArgs = !string.IsNullOrEmpty(methodArgs);
            string[] argStrings = hasArgs
                ? methodArgs.Split(',').Select(a => a.Trim()).ToArray()
                : Array.Empty<string>();

            var candidateMethods = component.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) &&
                            m.GetParameters().Length == argStrings.Length)
                .ToArray();

            if (candidateMethods.Length == 0)
            {
                var availableMethods = GetInvokableMethods(component);
                string methodList = string.Join(", ", availableMethods.Select(FormatMethodSignature));
                return $"ERROR: No method '{methodName}' with {argStrings.Length} parameter(s) found. Available: {methodList}";
            }

            foreach (var method in candidateMethods)
            {
                var parameters = method.GetParameters();
                try
                {
                    object[] convertedArgs = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        convertedArgs[i] = ConvertStringToType(argStrings[i], parameters[i].ParameterType);
                    }

                    object result = method.Invoke(component, convertedArgs);
                    string argDisplay = hasArgs ? $"({methodArgs})" : "()";
                    if (method.ReturnType == typeof(void))
                        return $"SUCCESS: Invoked {componentName}.{methodName}{argDisplay} on '{obj.name}'";
                    return $"SUCCESS: Invoked {componentName}.{methodName}{argDisplay} on '{obj.name}' → {FormatValueForDisplay(result)}";
                }
                catch (Exception ex) when (candidateMethods.Length > 1 && !(ex is TargetInvocationException))
                {
                    continue;
                }
                catch (TargetInvocationException ex)
                {
                    return $"ERROR: Method threw exception: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception)
                {
                    var paramTypes = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                    return $"ERROR: Failed to convert arguments for {methodName}({paramTypes}). " +
                           $"Provided: {methodArgs}";
                }
            }

            var firstMatch = candidateMethods[0];
            var expectedTypes = string.Join(", ", firstMatch.GetParameters().Select(p => $"{p.Name}:{p.ParameterType.Name}"));
            return $"ERROR: Could not match arguments to any overload of '{methodName}'. Expected: ({expectedTypes}), Provided: {methodArgs}";
        }

        // NOTE: DiagnoseVisibility was considered but deferred. A comprehensive visibility check
        // (active state, renderer, material, camera culling, occlusion, distance) is non-trivial
        // and a half-baked implementation gives false confidence. The AI agent can already check
        // these individually using InspectGameObject + InspectComponentMembers.

        [Tool(Description = "Move a GameObject by a relative offset (dx, dy, dz) from its current position. Saves multiple round trips compared to reading position, computing, and writing back.",
            Returns = "Old and new positions after the move.")]
        internal string MoveRelative(int instanceId, float dx, float dy, float dz)
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null)
                return $"ERROR: GameObject with instance ID {instanceId} not found. Use SearchGameObjects to find the correct ID.";

            var transform = obj.transform;
            var oldPosition = transform.position;
            var offset = new Vector3(dx, dy, dz);
            transform.position = oldPosition + offset;
            var newPosition = transform.position;

            return $"SUCCESS: Moved '{obj.name}' (ID: {instanceId})\n" +
                   $"Offset: {FormatValueForDisplay(offset)}\n" +
                   $"Old position: {FormatValueForDisplay(oldPosition)}\n" +
                   $"New position: {FormatValueForDisplay(newPosition)}";
        }

        [Tool(Description = "Rotate a GameObject by relative Euler angles (dx, dy, dz) from its current rotation. Saves multiple round trips compared to reading rotation, computing, and writing back.",
            Returns = "Old and new rotations after the rotation.")]
        internal string RotateRelative(int instanceId, float dx, float dy, float dz)
        {
            GameObject obj = FindGameObjectById(instanceId);
            if (obj == null)
                return $"ERROR: GameObject with instance ID {instanceId} not found. Use SearchGameObjects to find the correct ID.";

            var transform = obj.transform;
            var oldRotation = transform.rotation.eulerAngles;
            transform.Rotate(dx, dy, dz, Space.Self);
            var newRotation = transform.rotation.eulerAngles;

            return $"SUCCESS: Rotated '{obj.name}' (ID: {instanceId})\n" +
                   $"Rotation offset: {FormatValueForDisplay(new Vector3(dx, dy, dz))}\n" +
                   $"Old rotation: {FormatValueForDisplay(oldRotation)}\n" +
                   $"New rotation: {FormatValueForDisplay(newRotation)}";
        }

        // Helper methods
        private string InspectComponentMembersInternal(Component component, string componentName, string gameObjectName, string memberFilter)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var members = component.GetType().GetMembers(flags);
            var memberNames = ParseMemberFilter(memberFilter);

            var sb = new StringBuilder();
            sb.AppendLine($"Component Members for '{componentName}' on '{gameObjectName}':");
            sb.AppendLine($"Status: {GetComponentEnabledStatus(component)}");
            sb.AppendLine();

            var compatibleMembers = new List<MemberInfo>();
            foreach (var member in members)
            {
                if (memberNames.Count > 0 && !memberNames.Contains(member.Name)) continue;
                if (memberNames.Count == 0 && !member.IsPublic()) continue;
                if ((member.MemberType & (MemberTypes.Property | MemberTypes.Field)) == 0) continue;
                compatibleMembers.Add(member);
            }

            var settableMembers = compatibleMembers.Where(m => m.CanBeChanged()).ToList();
            var readOnlyMembers = compatibleMembers.Where(m => !m.CanBeChanged()).ToList();

            if (settableMembers.Count > 0)
            {
                sb.AppendLine("SETTABLE MEMBERS:");
                foreach (var member in settableMembers)
                {
                    var value = GetMemberValueSafely(member, component);
                    var typeName = member.GetDataType()?.Name ?? "Unknown";
                    sb.AppendLine($"  {member.Name} ({typeName}): {value}");
                }
            }

            if (readOnlyMembers.Count > 0)
            {
                sb.AppendLine("READ-ONLY MEMBERS:");
                foreach (var member in readOnlyMembers)
                {
                    var value = GetMemberValueSafely(member, component);
                    var typeName = member.GetDataType()?.Name ?? "Unknown";
                    sb.AppendLine($"  {member.Name} ({typeName}): {value}");
                }
            }
            return sb.ToString();
        }

        private object GetMemberValueSafely(MemberInfo member, Component component)
        {
            try { return member.GetValue(component) ?? "null"; }
            catch (Exception ex) { return $"[Error: {ex.Message}]"; }
        }

        private HashSet<string> ParseMemberFilter(string memberFilter)
        {
            var memberNames = new HashSet<string>();
            if (string.IsNullOrEmpty(memberFilter)) return memberNames;
            foreach (var name in memberFilter.Split(','))
            {
                var trimmed = name.Trim();
                if (!string.IsNullOrEmpty(trimmed)) memberNames.Add(trimmed);
            }
            return memberNames;
        }

        private object ConvertStringToType(string value, Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(float)) return float.Parse(value);
            if (targetType == typeof(double)) return double.Parse(value);
            if (targetType == typeof(bool)) return bool.Parse(value);

            if (targetType == typeof(Vector3))
            {
                string cleanValue = value.Trim('(', ')');
                string[] parts = cleanValue.Split(',');
                if (parts.Length == 3)
                    return new Vector3(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()));
            }

            if (targetType == typeof(Vector2))
            {
                string cleanValue = value.Trim('(', ')');
                string[] parts = cleanValue.Split(',');
                if (parts.Length == 2)
                    return new Vector2(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()));
            }

            if (targetType == typeof(Color))
            {
                string cleanValue = value.Trim('(', ')');
                string[] parts = cleanValue.Split(',');
                if (parts.Length >= 3)
                    return new Color(float.Parse(parts[0].Trim()), float.Parse(parts[1].Trim()), float.Parse(parts[2].Trim()),
                        parts.Length > 3 ? float.Parse(parts[3].Trim()) : 1.0f);
            }

            return Convert.ChangeType(value, targetType);
        }

        private void GetHierarchyRecursive(GameObject obj, int depth, StringBuilder sb)
        {
            string indent = new string(' ', depth * 2);
#if UNITY_6000_5_OR_NEWER
            sb.AppendLine($"{indent}- {obj.name} (ID: {obj.GetEntityId()})");
#else
            sb.AppendLine($"{indent}- {obj.name} (ID: {obj.GetHashCode()})");
#endif
            foreach (Transform child in obj.transform)
                GetHierarchyRecursive(child.gameObject, depth + 1, sb);
        }

        private GameObject FindGameObjectById(int instanceId)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    var result = FindGameObjectByIdRecursive(rootObj, instanceId);
                    if (result != null) return result;
                }
            }
            return null;
        }

        private GameObject FindGameObjectByIdRecursive(GameObject obj, int instanceId)
        {
#if UNITY_6000_5_OR_NEWER
            if (obj.GetEntityId() == instanceId) return obj;
#else
            if (obj.GetHashCode() == instanceId) return obj;
#endif
            foreach (Transform child in obj.transform)
            {
                var result = FindGameObjectByIdRecursive(child.gameObject, instanceId);
                if (result != null) return result;
            }
            return null;
        }

        private void SearchRecursive(GameObject obj, string searchPattern, List<GameObject> foundObjects, bool exactMatch, bool includeInactive)
        {
            if (!includeInactive && !obj.activeSelf) return;
            bool matches = exactMatch
                ? obj.name.Equals(searchPattern, StringComparison.OrdinalIgnoreCase)
                : obj.name.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0;
            if (matches) foundObjects.Add(obj);
            foreach (Transform child in obj.transform)
                SearchRecursive(child.gameObject, searchPattern, foundObjects, exactMatch, includeInactive);
        }

        private void FindByComponentRecursive(GameObject obj, string componentTypeName, List<GameObject> foundObjects, bool includeInactive)
        {
            if (!includeInactive && !obj.activeSelf) return;
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name.IndexOf(componentTypeName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundObjects.Add(obj);
                    break;
                }
            }
            foreach (Transform child in obj.transform)
                FindByComponentRecursive(child.gameObject, componentTypeName, foundObjects, includeInactive);
        }

        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            var path = obj.name;
            var parent = obj.transform.parent;
            while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }
            return path;
        }

        private Component FindComponent(GameObject obj, string componentName)
        {
            foreach (var comp in obj.GetComponents<Component>())
                if (comp != null && comp.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                    return comp;
            return null;
        }

        private MemberInfo FindMember(Type componentType, string memberName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var member in componentType.GetMembers(flags))
                if (member.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    return member;
            return null;
        }

        private static readonly HashSet<Type> _excludedDeclaringTypes = new HashSet<Type>
        {
            typeof(MonoBehaviour),
            typeof(Behaviour),
            typeof(Component),
            typeof(UnityEngine.Object),
            typeof(object),
        };

        private List<MethodInfo> GetInvokableMethods(Component component)
        {
            return component.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && !_excludedDeclaringTypes.Contains(m.DeclaringType))
                .ToList();
        }

        private static string FormatMethodSignature(MethodInfo m)
        {
            var paramList = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            var returnLabel = m.ReturnType == typeof(void) ? "void" : m.ReturnType.Name;
            return $"{returnLabel} {m.Name}({paramList})";
        }

        private string GetComponentEnabledStatus(Component component)
        {
            if (component == null) return "[NULL]";
            var enabledProp = component.GetType().GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            if (enabledProp != null && enabledProp.PropertyType == typeof(bool))
            {
                try { return (bool)enabledProp.GetValue(component) ? "[ENABLED]" : "[DISABLED]"; }
                catch { return "[UNKNOWN]"; }
            }
            return "[ALWAYS ACTIVE]";
        }

        private string FormatValueForDisplay(object value)
        {
            if (value == null) return "null";
            if (value is string str) return $"\"{str}\"";
            if (value is Vector3 v3) return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            if (value is Vector2 v2) return $"({v2.x:F2}, {v2.y:F2})";
            if (value is Color color) return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
            if (value is float f) return f.ToString("F2");
            if (value is double d) return d.ToString("F2");
            return value.ToString();
        }
    }
}

