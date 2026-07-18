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

#if ISDK_OPENXR_HAND
using System;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    public interface IOpenXRMigrableEditor
    {
        bool NeedsConversion(SerializedObject target);
        void Convert(SerializedObject target);
    }

    public static class EditorOpenXRMigrationUtils
    {
        public static void OpenXRConversionMenu(this UnityEditor.Editor editor,
            ref bool unroll,
            Action<SerializedObject> convertProperties,
            params string[] oldPropertyNames)
        {
            OpenXRConversionMenu(editor, ref unroll,
                convertProperties,
                () => DrawDisabledProperties(editor, oldPropertyNames));
        }

        public static void OpenXRConversionMenu(this UnityEditor.Editor editor,
            ref bool unroll,
            Func<SerializedObject, bool> convertProperties,
            params string[] oldPropertyNames)
        {
            OpenXRConversionMenu(editor, ref unroll,
                convertProperties,
                () => DrawDisabledProperties(editor, oldPropertyNames));
        }

        public static void OpenXRConversionMenu(this UnityEditor.Editor editor,
            ref bool unroll,
            Action<SerializedObject> convertProperties,
            Action drawOldProperties)
        {
            OpenXRConversionMenu(editor, ref unroll,
                obj =>
                {
                     convertProperties.Invoke(obj);
                     return true;
                }, drawOldProperties);
        }

        public static void OpenXRConversionMenu(this UnityEditor.Editor editor,
            ref bool unroll,
            Func<SerializedObject, bool> convertProperties,
            Action drawOldProperties)
        {
        EditorGUILayout.Space();
            unroll = EditorGUILayout.Foldout(unroll, "OpenXR Migration tool");
            if (unroll)
            {
                EditorGUI.indentLevel++;

                drawOldProperties.Invoke();

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(HandTranslationUtils.UpgradeRequiredMessage, MessageType.Info);
                    if (GUILayout.Button(HandTranslationUtils.UpgradeRequiredButton, HandTranslationUtils.FixButtonStyle))
                    {
                        foreach (UnityEngine.Object target in editor.targets)
                        {
                            SerializedObject subSerializedObject = new SerializedObject(target);
                            unroll = !convertProperties.Invoke(subSerializedObject);
                            subSerializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(target);
                            subSerializedObject.Dispose();
                        }
                        editor.serializedObject.Update();
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            editor.serializedObject.ApplyModifiedProperties();
        }

        public static void DrawDisabledProperties(this UnityEditor.Editor editor,
            params string[] propertyNames)
        {
            EditorGUI.BeginDisabledGroup(true);
            {
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    SerializedProperty ovrProperty = editor.serializedObject.FindProperty(propertyNames[i]);
                    EditorGUILayout.PropertyField(ovrProperty);
                }
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
