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

using Oculus.Interaction.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Oculus.Interaction.Hands.Editor
{
    [CustomEditor(typeof(FromHandPrefabDataSource))]
    public class FromHandPrefabDataSourceEditor : UnityEditor.Editor
    {
        private SerializedProperty _jointsProperty;

        protected virtual void OnEnable()
        {
#if ISDK_OPENXR_HAND
            _jointsProperty = serializedObject.FindProperty("_jointTransformsOpenXR");
#else
            _jointsProperty = serializedObject.FindProperty("_jointTransforms");
#endif
        }

        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject);
            serializedObject.ApplyModifiedProperties();

            FromHandPrefabDataSource source = (FromHandPrefabDataSource)target;
            HandJointsAutoPopulatorHelper.InitializeCollection(_jointsProperty);

            if (GUILayout.Button("Auto Map Joints"))
            {
                AutoMapJoints(source);
                EditorUtility.SetDirty(source);
                EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
            }

            HandJointsAutoPopulatorHelper.DisplayJoints(_jointsProperty);
            serializedObject.ApplyModifiedProperties();
        }

        private void AutoMapJoints(FromHandPrefabDataSource source)
        {
            Transform rootTransform = source.transform;
            HandJointsAutoPopulatorHelper.AutoMapJoints(_jointsProperty, rootTransform);
        }

    }
}
