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

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;
#endif

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Inspector-only MonoBehaviour that surfaces all fields marked with
    /// [LocomotionSetting] attribute from components found in children.
    /// This component holds no state - it simply acts as a centralized view
    /// for tweaking settings in the Unity Inspector.
    /// </summary>
    [ExecuteAlways]
    public class LocomotionSettingsGroup : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("If true, it will show settings within Components that are both enabled " +
            "and their specific GameObject active.")]
        private bool _showDisabledSettings = false;
#pragma warning restore CS0414
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(LocomotionSettingsGroup))]
    public class LocomotionSettingsInspectorEditor : UnityEditor.Editor
    {
        private class SettingEntry
        {
            public SerializedObject serializedObject;
            public SerializedProperty property;
            public string displayName;
            public Component component;
        }

        private List<SettingEntry> _settingEntries = new List<SettingEntry>();
        private bool _needsRefresh = true;
        private SerializedProperty _showDisabledSettings;

        private void OnEnable()
        {
            _needsRefresh = true;
            _showDisabledSettings = this.serializedObject.FindProperty(nameof(_showDisabledSettings));
        }

        public override void OnInspectorGUI()
        {
            LocomotionSettingsGroup inspector = (LocomotionSettingsGroup)target;

            if (_needsRefresh)
            {
                RefreshSettings(inspector, _showDisabledSettings.boolValue);
                _needsRefresh = false;
            }

            if (_settingEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No [LocomotionSetting] fields found in children.\n\n" +
                    "Add the [LocomotionSetting] attribute to fields " +
                    "in child components to see them here.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "[LocomotionSetting] fields from child components are displayed below. " +
                "Changes made here are applied directly to the source components.",
                MessageType.None);

            foreach (SettingEntry entry in _settingEntries)
            {
                if (entry.serializedObject.targetObject == null)
                {
                    _needsRefresh = true;
                    continue;
                }

                entry.serializedObject.Update();

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(EditorGUIUtility.IconContent("Linked"), EditorStyles.iconButton))
                {
                    HighlightProperty(entry.component.gameObject, entry.property.name);
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(entry.property, new GUIContent(entry.displayName), true);
                if (EditorGUI.EndChangeCheck())
                {
                    entry.serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_showDisabledSettings);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                _needsRefresh = true;
            }

        }

        private void RefreshSettings(LocomotionSettingsGroup inspector, bool showDisabledSettings)
        {
            _settingEntries.Clear();

            MonoBehaviour[] components = inspector.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (!showDisabledSettings && !(component.enabled && component.gameObject.activeSelf))
                {
                    continue;
                }

                Type componentType = component.GetType();
                SerializedObject serializedObject = new SerializedObject(component);
                FieldInfo[] fields = componentType.GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (FieldInfo field in fields)
                {
                    if (!field.IsDefined(typeof(LocomotionSettingAttribute), true))
                    {
                        continue;
                    }

                    SerializedProperty property = serializedObject.FindProperty(field.Name);
                    if (property == null)
                    {
                        continue;
                    }

                    string localPath = GetLocalPath(inspector.transform, component.transform);
                    SettingEntry entry = new SettingEntry
                    {
                        serializedObject = serializedObject,
                        property = property,
                        displayName = localPath + ObjectNames.NicifyVariableName(property.name),
                        component = component,
                    };

                    _settingEntries.Add(entry);
                }
            }
        }

        private static void HighlightProperty(GameObject gameObject, string propertyName)
        {
            Selection.activeObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
            EditorApplication.delayCall += () =>
            {
                Highlighter.Highlight("Inspector",
                    ObjectNames.NicifyVariableName(propertyName),
                    HighlightSearchMode.PrefixLabel);
            };
        }

        private static string GetLocalPath(Transform root, Transform target)
        {
            string path = string.Empty;
            Transform current = target;
            while (current != null && current != root)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }
            return path;
        }
    }
#endif
}
