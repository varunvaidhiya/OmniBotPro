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
using UnityEditor;

namespace Oculus.Interaction.PoseDetection.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JointDistanceActiveState))]
    public class JointDistanceActiveStateEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_jointIdA", "_jointIdB" };
#else
        private static readonly string[] _openXRPropertyNames =
            {"_jointA", "_jointB" };
#endif
        protected override void OnEnable()
        {
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
            this.OpenXRConversionMenu(ref _unrollConverter, (so) => Convert(so), () =>
            {
                GetOVRProperties(serializedObject,
                    out SerializedProperty jointIDAProp,
                    out SerializedProperty jointIDBProp);

                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.EnumPopup(jointIDAProp.displayName, (Input.Compatibility.OVR.HandJointId)jointIDAProp.intValue);
                    EditorGUILayout.EnumPopup(jointIDBProp.displayName, (Input.Compatibility.OVR.HandJointId)jointIDBProp.intValue);
                }
                EditorGUI.EndDisabledGroup();
            });
        }

        public bool NeedsConversion(SerializedObject target)
        {
            ConversionValues(target, out int jointA, out int jointB);
            GetOpenXRProperties(target, out SerializedProperty jointAProp, out SerializedProperty jointBProp);

            return (jointAProp.intValue != jointA
                || jointBProp.intValue != jointB);
        }

        public void Convert(SerializedObject target)
        {
            ConversionValues(target, out int jointA, out int jointB);
            GetOpenXRProperties(target, out SerializedProperty jointAProp, out SerializedProperty jointBProp);

            jointAProp.intValue = jointA;
            jointBProp.intValue = jointB;
        }

        private static void ConversionValues(SerializedObject target,
            out int jointA, out int jointB)
        {
            GetOVRProperties(target,
                out SerializedProperty jointAProp,
                out SerializedProperty jointBProp);

            jointA = (int)HandTranslationUtils.OVRHandJointToOpenXR(jointAProp.intValue);
            jointB = (int)HandTranslationUtils.OVRHandJointToOpenXR(jointBProp.intValue);
        }

        private static void GetOVRProperties(SerializedObject target,
            out SerializedProperty jointIDAProp,
            out SerializedProperty jointIDBProp)
        {
            jointIDAProp = target.FindProperty("_jointIdA");
            jointIDBProp = target.FindProperty("_jointIdB");
        }

        private static void GetOpenXRProperties(SerializedObject target,
            out SerializedProperty jointIDAProp,
            out SerializedProperty jointIDBProp)
        {
            jointIDAProp = target.FindProperty("_jointA");
            jointIDBProp = target.FindProperty("_jointB");
        }

#endif
    }
}
