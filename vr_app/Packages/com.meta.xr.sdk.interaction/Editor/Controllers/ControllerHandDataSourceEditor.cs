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

using Oculus.Interaction.Editor;
using Oculus.Interaction.Hands.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Oculus.Interaction.Input.Editor
{
    [CustomEditor(typeof(ControllerHandDataSource))]
    public class ControllerHandDataSourceEditor : SimplifiedEditor
    {
        private SerializedProperty _rootProperty;
        private SerializedProperty _jointsProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
#if ISDK_OPENXR_HAND
            _jointsProperty = serializedObject.FindProperty("_openXRJointTransforms");
            _rootProperty = serializedObject.FindProperty("_openXRRoot");
            _editorDrawer.Hide("_root");
#else
            _jointsProperty = serializedObject.FindProperty("_jointTransforms");
            _rootProperty = serializedObject.FindProperty("_root");
            _editorDrawer.Hide("_openXRRoot");
#endif
            _editorDrawer.Hide("_jointTransforms", "_openXRJointTransforms");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();

            ControllerHandDataSource dataSource = (ControllerHandDataSource)target;
            HandJointsAutoPopulatorHelper.InitializeCollection(_jointsProperty);

            if (GUILayout.Button("Auto Map Joints"))
            {
                AutoMapJoints(dataSource);
                EditorUtility.SetDirty(dataSource);
                EditorSceneManager.MarkSceneDirty(dataSource.gameObject.scene);
            }

            HandJointsAutoPopulatorHelper.DisplayJoints(_jointsProperty);
            serializedObject.ApplyModifiedProperties();
        }

        private void AutoMapJoints(ControllerHandDataSource dataSource)
        {
            Transform rootTransform = dataSource.transform;
            if (_rootProperty.objectReferenceValue is Transform customRoot)
            {
                rootTransform = customRoot;
            }
            HandJointsAutoPopulatorHelper.AutoMapJoints(_jointsProperty, rootTransform);
        }
    }
}
