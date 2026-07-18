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
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandPinchOffset))]
    public class HandPinchOffsetEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_localPositionOffset", "_rotationOffset" };
#else
        private static readonly string[] _openXRPropertyNames =
            {"_posOffset", "_rotOffset" };
#endif

        protected override void OnEnable()
        {
            if (!target)
                return;

            base.OnEnable();
#if ISDK_OPENXR_HAND
            _unrollConverter = NeedsConversion(serializedObject);
            _editorDrawer.Hide(_ovrPropertyNames);
#else
            _editorDrawer.Hide(_openXRPropertyNames);
#endif
        }

#if ISDK_OPENXR_HAND
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            this.OpenXRConversionMenu(ref _unrollConverter, (so) => Convert(so), _ovrPropertyNames);
        }

        public bool NeedsConversion(SerializedObject target)
        {
            ConversionValues(target, out Vector3 offsetPosition, out Quaternion rotation);
            GetOpenXRProperties(target,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            return (offsetProp.vector3Value != offsetPosition
                || rotationProp.quaternionValue != rotation);
        }

        public void Convert(SerializedObject target)
        {
            ConversionValues(target, out Vector3 offsetPosition, out Quaternion rotation);

            GetOpenXRProperties(target,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            offsetProp.vector3Value = offsetPosition;
            rotationProp.quaternionValue = rotation;
        }

        private static void ConversionValues(SerializedObject target,
            out Vector3 offsetPosition, out Quaternion rotation)
        {
            GetOVRProperties(target,
                out SerializedProperty ovrOffsetProp,
                out SerializedProperty ovrRotationProp);

            Vector3 localPositionOffset = ovrOffsetProp.vector3Value;
            Quaternion rotationOffset = ovrRotationProp.quaternionValue;
            Pose translatedRoot = HandRootOffsetEditor.TranslateRoot(target, localPositionOffset, rotationOffset);
            offsetPosition = translatedRoot.position;
            rotation = translatedRoot.rotation;
        }

        private static void GetOVRProperties(SerializedObject target,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            offsetProp = target.FindProperty("_localPositionOffset");
            rotationProp = target.FindProperty("_rotationOffset");
        }

        private static void GetOpenXRProperties(SerializedObject target,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            offsetProp = target.FindProperty("_posOffset");
            rotationProp = target.FindProperty("_rotOffset");
        }

#endif
    }
}
