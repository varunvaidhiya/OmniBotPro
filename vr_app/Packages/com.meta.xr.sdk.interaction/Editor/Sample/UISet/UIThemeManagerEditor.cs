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

using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(UIThemeManager))]
    public class UIThemeManagerEditor : SimplifiedEditor
    {
        private SerializedProperty _themesProperty;
        private SerializedProperty _currentThemeIndexProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            _themesProperty = serializedObject.FindProperty("_themes");
            _currentThemeIndexProperty = serializedObject.FindProperty("_currentThemeIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIThemeManager themeManager = (UIThemeManager)target;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_themesProperty, true);
            bool themesChanged = EditorGUI.EndChangeCheck();
            if (themesChanged)
            {
                serializedObject.ApplyModifiedProperties();
            }

            int themesCount = _themesProperty.arraySize;
            if (themesCount == 0)
            {
                _currentThemeIndexProperty.intValue = 0;
                return;
            }

            string[] options = new string[themesCount];
            for (int i = 0; i < options.Length; i++)
            {
                UITheme item = _themesProperty.GetArrayElementAtIndex(i).objectReferenceValue as UITheme;

                options[i] = item != null ? item.name : "(None)";
            }

            _currentThemeIndexProperty.intValue = Mathf.Clamp(_currentThemeIndexProperty.intValue, 0, themesCount - 1);
            int currentSelection = _currentThemeIndexProperty.intValue;
            int newSelection = EditorGUILayout.Popup("Select Theme", currentSelection, options);

            bool shouldApply = newSelection != currentSelection || themesChanged;

            if (shouldApply
                && (newSelection < themesCount)
                && (_themesProperty.GetArrayElementAtIndex(newSelection).objectReferenceValue is UITheme))
            {
                themeManager.ApplyTheme(newSelection);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
