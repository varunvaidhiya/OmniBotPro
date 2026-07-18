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
using static Oculus.Interaction.PoseDetection.JointRotationActiveState;

namespace Oculus.Interaction.PoseDetection.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JointRotationActiveState))]
    public class JointRotationActiveStateEditor : SimplifiedEditor
#if ISDK_OPENXR_HAND
        , IOpenXRMigrableEditor
#endif
    {
#if ISDK_OPENXR_HAND
        private bool _unrollConverter = true;
        private static readonly string[] _ovrPropertyNames =
            {"_featureConfigs" };
#else
        private static readonly string[] _openXRPropertyNames =
            {"_featureConfigurations" };
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
            this.OpenXRConversionMenu(ref _unrollConverter, (so) => Convert(so), _ovrPropertyNames);
        }

        public bool NeedsConversion(SerializedObject target)
        {
            ConversionValues(target, out JointRotationFeatureConfig[] jointConfigs);
            GetOpenXRProperties(target, out SerializedProperty featureConfigs);
            JointRotationFeatureConfig[] openXRJointConfigs = ReadCollection(featureConfigs);
            return !CompareCollections(jointConfigs, openXRJointConfigs);
        }

        public void Convert(SerializedObject target)
        {
            ConversionValues(target, out JointRotationFeatureConfig[] jointConfigs);
            GetOpenXRProperties(target,
                out SerializedProperty featureConfigs);

            WriteCollection(featureConfigs, jointConfigs);
        }

        private static void ConversionValues(SerializedObject target,
            out JointRotationFeatureConfig[] jointConfigs)
        {
            GetOVRProperties(target,
                out SerializedProperty featureConfigs);

            jointConfigs = ReadCollection(featureConfigs);
            TransformCollection(ref jointConfigs);
        }

        private static void GetOVRProperties(SerializedObject target,
            out SerializedProperty featureConfigs)
        {
            featureConfigs = target.FindProperty("_featureConfigs");
        }

        private static void GetOpenXRProperties(SerializedObject target,
            out SerializedProperty featureConfigs)
        {
            featureConfigs = target.FindProperty("_featureConfigurations");
        }

        private static JointRotationFeatureConfig[] ReadCollection(SerializedProperty serializedProperty)
        {
            serializedProperty = serializedProperty.FindPropertyRelative("_values");
            JointRotationFeatureConfig[] jointConfigs =
                new JointRotationFeatureConfig[serializedProperty.arraySize];

            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                SerializedProperty jointConfig = serializedProperty.GetArrayElementAtIndex(i);

                int jointID = jointConfig.FindPropertyRelative("_feature").intValue;
                int mode = jointConfig.FindPropertyRelative("_mode").intValue;
                string state = jointConfig.FindPropertyRelative("_state").stringValue;

                int relativeTo = jointConfig.FindPropertyRelative("_relativeTo").intValue;
                int worldAxis = jointConfig.FindPropertyRelative("_worldAxis").intValue;
                int handAxis = jointConfig.FindPropertyRelative("_handAxis").intValue;

                jointConfigs[i] = new JointRotationFeatureConfig()
                {
                    Feature = (Input.HandJointId)jointID,
                    Mode = (FeatureStateActiveMode)mode,
                    State = state,

                    RelativeTo = (RelativeTo)relativeTo,
                    WorldAxis = (WorldAxis)worldAxis,
                    HandAxis = (HandAxis)handAxis,
                };
            }
            return jointConfigs;
        }

        private static JointRotationFeatureConfig[] WriteCollection(
            SerializedProperty serializedProperty, JointRotationFeatureConfig[] jointConfigs)
        {
            serializedProperty = serializedProperty.FindPropertyRelative("_values");

            serializedProperty.arraySize = jointConfigs.Length;

            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                SerializedProperty jointConfig = serializedProperty.GetArrayElementAtIndex(i);

                jointConfig.FindPropertyRelative("_feature").intValue = (int)jointConfigs[i].Feature;
                jointConfig.FindPropertyRelative("_mode").intValue = (int)jointConfigs[i].Mode;
                jointConfig.FindPropertyRelative("_state").stringValue = jointConfigs[i].State;

                jointConfig.FindPropertyRelative("_relativeTo").intValue = (int)jointConfigs[i].RelativeTo;
                jointConfig.FindPropertyRelative("_worldAxis").intValue = (int)jointConfigs[i].WorldAxis;
                jointConfig.FindPropertyRelative("_handAxis").intValue = (int)jointConfigs[i].HandAxis;
            }
            return jointConfigs;
        }


        private static bool CompareCollections(
            JointRotationFeatureConfig[] a,
            JointRotationFeatureConfig[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].Feature != b[i].Feature)
                {
                    return false;
                }
            }
            return true;
        }

        private static void TransformCollection(ref JointRotationFeatureConfig[] jointConfigs)
        {
            for (int i = 0; i < jointConfigs.Length; i++)
            {
                jointConfigs[i].Feature =
                    HandTranslationUtils.OVRHandJointToOpenXR((int)jointConfigs[i].Feature);
            }
        }

#endif
    }
}
