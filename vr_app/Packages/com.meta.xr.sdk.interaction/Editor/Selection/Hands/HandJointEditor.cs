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
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandJoint))]
    public class HandJointEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_handJointId", "_localPositionOffset", "_rotationOffset" };
#else
        private static readonly string[] _openXRPropertyNames =
            {"_jointId", "_posOffset", "_rotOffset", "_useLegacyOrientation",
             "_freezeRotationX", "_freezeRotationY", "_freezeRotationZ"};
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

            this.OpenXRConversionMenu(ref _unrollConverter,
                (so) => Convert(so),
                () =>
                {
                    GetOVRProperties(serializedObject,
                        out SerializedProperty jointIDProp,
                        out SerializedProperty offsetProp,
                        out SerializedProperty rotationProp);

                    EditorGUI.BeginDisabledGroup(true);
                    {
                        EditorGUILayout.EnumPopup(jointIDProp.displayName, (Input.Compatibility.OVR.HandJointId)jointIDProp.intValue);
                        EditorGUILayout.PropertyField(offsetProp);
                        EditorGUILayout.PropertyField(rotationProp);
                    }
                    EditorGUI.EndDisabledGroup();
                });
        }

        public bool NeedsConversion(SerializedObject target)
        {
            ConversionValues(target, out int jointId, out Vector3 offsetPosition, out Quaternion rotation);
            GetOpenXRProperties(target,
                out SerializedProperty jointIDProp,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            return (jointIDProp.intValue != jointId
                || offsetProp.vector3Value != offsetPosition
                || rotationProp.quaternionValue != rotation);
        }

        public void Convert(SerializedObject target)
        {
            ConversionValues(target, out int jointId, out Vector3 offsetPosition, out Quaternion rotation);

            GetOpenXRProperties(target,
                out SerializedProperty jointIDProp,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            jointIDProp.intValue = jointId;
            offsetProp.vector3Value = offsetPosition;
            rotationProp.quaternionValue = rotation;

            target.FindProperty("_useLegacyOrientation").boolValue = true;
        }

        private static void ConversionValues(SerializedObject target,
            out int jointId, out Vector3 offsetPosition, out Quaternion rotation)
        {
            GetOVRProperties(target,
                out SerializedProperty ovrJointIDProp,
                out SerializedProperty ovrOffsetProp,
                out SerializedProperty ovrRotationProp);

            int handJointId = ovrJointIDProp.intValue;
            Vector3 localPositionOffset = ovrOffsetProp.vector3Value;
            Quaternion rotationOffset = ovrRotationProp.quaternionValue;

            jointId = (int)HandTranslationUtils.OVRHandJointToOpenXR(handJointId);
            Quaternion rot = HandTranslationUtils.TransformOVRToOpenXRRotation(rotationOffset, Handedness.Right);
            offsetPosition = rot * HandTranslationUtils.TransformOVRToOpenXRPosition(localPositionOffset, Handedness.Right);
            rotation = Quaternion.identity;
        }

        private static void GetOVRProperties(SerializedObject target,
            out SerializedProperty jointIDProp,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            jointIDProp = target.FindProperty("_handJointId");
            offsetProp = target.FindProperty("_localPositionOffset");
            rotationProp = target.FindProperty("_rotationOffset");
        }

        private static void GetOpenXRProperties(SerializedObject target,
            out SerializedProperty jointIDProp,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            jointIDProp = target.FindProperty("_jointId");
            offsetProp = target.FindProperty("_posOffset");
            rotationProp = target.FindProperty("_rotOffset");
        }
#endif
    }
}
