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

using Oculus.Interaction;
using Oculus.Interaction.Editor;
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractableColorVisual))]
public class InteractableColorVisualEditor : SimplifiedEditor
{
    private static readonly string MultipleInteractableWarning =
        $"With multiple sibling {nameof(IInteractable)}s, "
         + $"consider configuring an {nameof(InteractableGroupView)}";

    public override void OnInspectorGUI()
    {
        var interactableViewField = serializedObject.FindProperty("_interactableView");
        var interactableView = interactableViewField.objectReferenceValue as IInteractableView;
        var component = interactableView as MonoBehaviour;
        var componentExists = component != null;
        var components = (componentExists) ? component.GetComponents<IInteractable>() :
            Array.Empty<IInteractable>();
        if (interactableView != null
            && interactableView is not InteractableGroupView
            && components.Length > 1
            )
        {
            EditorGUILayout.HelpBox(MultipleInteractableWarning, MessageType.Warning, true);
        }

        base.OnInspectorGUI();
    }
}
