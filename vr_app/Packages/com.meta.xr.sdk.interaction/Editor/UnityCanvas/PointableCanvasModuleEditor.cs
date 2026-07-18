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
using UnityEngine.EventSystems;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(PointableCanvasModule))]
    public class PointableCanvasModuleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawExclusiveWarning();
        }

        private void DrawExclusiveWarning()
        {
            var canvasModule = (PointableCanvasModule)target;
            var eventSystem = canvasModule.GetComponent<EventSystem>();

            if (canvasModule == null || eventSystem == null ||
                canvasModule.ExclusiveMode)
            {
                return;
            }

            var inputModules = eventSystem.GetComponents<BaseInputModule>();
            if (inputModules.Length > 0 && inputModules[0] != canvasModule)
            {
                EditorGUILayout.HelpBox($"To ensure the {nameof(PointableCanvasModule)} " +
                    $"functions properly, its component should either be the first (highest) " +
                    $"module on the EventSystem gameObject, or have " +
                    $"{nameof(PointableCanvasModule.ExclusiveMode)} enabled.",
                    MessageType.Warning);
            }
        }
    }
}
