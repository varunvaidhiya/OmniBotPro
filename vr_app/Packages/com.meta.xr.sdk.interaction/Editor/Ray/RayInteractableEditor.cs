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
    [CustomEditor(typeof(RayInteractable)), CanEditMultipleObjects]
    public class RayInteractableEditor : SimplifiedEditor
    {
        private RayInteractable _interactable;

        private SerializedProperty _pointableElementProperty;
        private SerializedProperty _movementProviderProperty;

        private void Awake()
        {
            _interactable = target as RayInteractable;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _pointableElementProperty = serializedObject.FindProperty("_pointableElement");
            _movementProviderProperty = serializedObject.FindProperty("_movementProvider");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool isUsedForGrab = _pointableElementProperty.objectReferenceValue is IGrabbable;
            bool hasMovementProvider = _movementProviderProperty.objectReferenceValue as IMovementProvider != null;

            if (isUsedForGrab && !hasMovementProvider)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.HelpBox(
                    $"It looks like you want to use this {nameof(RayInteractable)} for grab with an {nameof(IGrabbable)}. " +
                    $"But you did not provide a {nameof(IMovementProvider)}. Without it your ray might not grab as expected.",
                    MessageType.Warning);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Fix", GUILayout.MaxWidth(100), GUILayout.MaxHeight(40)))
                {
                    MoveFromTargetProvider movementProvider = _interactable.gameObject.AddComponent<MoveFromTargetProvider>();
                    _movementProviderProperty.objectReferenceValue = movementProvider;
                    serializedObject.ApplyModifiedProperties();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

    }
}
