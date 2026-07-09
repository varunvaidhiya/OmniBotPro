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
using static Oculus.Interaction.HandJointsPose;

namespace Oculus.Interaction.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandJointsPose))]
    public class HandJointsPoseEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_weightedJoints", "_localPositionOffset", "_rotationOffset" };
#else
        private static readonly string[] _openXRPropertyNames =
            {"_joints", "_posOffset", "_rotOffset" };
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
            ConversionValues(target, out WeightedJoint[] weightedJoints, out Vector3 offsetPosition, out Quaternion rotation);
            GetOpenXRProperties(target,
                out SerializedProperty weightedJointsProp,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            WeightedJoint[] openXRweightedJoints = ReadWeightedJoints(weightedJointsProp);

            return (!CompareWeightedJoints(weightedJoints, openXRweightedJoints)
                || offsetProp.vector3Value != offsetPosition
                || rotationProp.quaternionValue != rotation);
        }

        public void Convert(SerializedObject target)
        {
            ConversionValues(target, out WeightedJoint[] weightedJoints, out Vector3 offsetPosition, out Quaternion rotation);

            GetOpenXRProperties(target,
                out SerializedProperty weightedJointsProp,
                out SerializedProperty offsetProp,
                out SerializedProperty rotationProp);

            WriteWeightedJoints(weightedJointsProp, weightedJoints);
            offsetProp.vector3Value = offsetPosition;
            rotationProp.quaternionValue = rotation;
        }

        private static void ConversionValues(SerializedObject target,
            out WeightedJoint[] weightedJoints, out Vector3 offsetPosition, out Quaternion rotation)
        {
            GetOVRProperties(target,
                out SerializedProperty ovrJointsProp,
                out SerializedProperty ovrOffsetProp,
                out SerializedProperty ovrRotationProp);


            weightedJoints = ReadWeightedJoints(ovrJointsProp);
            TransformWeightedJoints(ref weightedJoints);

            Vector3 localPositionOffset = ovrOffsetProp.vector3Value;
            Quaternion rotationOffset = ovrRotationProp.quaternionValue;
            Quaternion rot = HandTranslationUtils.TransformOVRToOpenXRRotation(rotationOffset, Handedness.Right);
            offsetPosition = rot * HandTranslationUtils.TransformOVRToOpenXRPosition(localPositionOffset, Handedness.Right);
            rotation = Quaternion.identity;
        }

        private static WeightedJoint[] ReadWeightedJoints(SerializedProperty serializedProperty)
        {
            WeightedJoint[] weightedJoints = new WeightedJoint[serializedProperty.arraySize];
            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                SerializedProperty weightedJoint = serializedProperty.GetArrayElementAtIndex(i);
                int jointId = weightedJoint.FindPropertyRelative("handJointId").intValue;
                float jointWeight = weightedJoint.FindPropertyRelative("weight").floatValue;

                weightedJoints[i] = new WeightedJoint() { handJointId = (HandJointId)jointId, weight = jointWeight };
            }
            return weightedJoints;
        }

        private static void WriteWeightedJoints(SerializedProperty serializedProperty, WeightedJoint[] weightedJoints)
        {
            serializedProperty.arraySize = weightedJoints.Length;
            for (int i = 0; i < weightedJoints.Length; i++)
            {
                SerializedProperty jointProperty = serializedProperty.GetArrayElementAtIndex(i);
                jointProperty.FindPropertyRelative("handJointId").intValue = (int) weightedJoints[i].handJointId;
                jointProperty.FindPropertyRelative("weight").floatValue = weightedJoints[i].weight;
            }
        }

        private static bool CompareWeightedJoints(WeightedJoint[] a, WeightedJoint[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].handJointId != b[i].handJointId
                    || a[i].weight != b[i].weight)
                {
                    return false;
                }
            }
            return true;
        }

        private static void TransformWeightedJoints(ref WeightedJoint[] weightedJoints)
        {
            for (int i = 0; i < weightedJoints.Length; i++)
            {
                weightedJoints[i].handJointId = HandTranslationUtils.OVRHandJointToOpenXR((int) weightedJoints[i].handJointId);
            }
        }

        private static void GetOVRProperties(SerializedObject target,
            out SerializedProperty weightedJointsProp,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            weightedJointsProp = target.FindProperty("_weightedJoints");
            offsetProp = target.FindProperty("_localPositionOffset");
            rotationProp = target.FindProperty("_rotationOffset");
        }

        private static void GetOpenXRProperties(SerializedObject target,
            out SerializedProperty weightedJointsProp,
            out SerializedProperty offsetProp,
            out SerializedProperty rotationProp)
        {
            weightedJointsProp = target.FindProperty("_joints");
            offsetProp = target.FindProperty("_posOffset");
            rotationProp = target.FindProperty("_rotOffset");
        }
#endif
    }
}
