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
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if ISDK_OPENXR_HAND
namespace Oculus.Interaction.Editor.Models.TouchHandGrab
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandSphereMap))]
    public class HandSphereMapEditor : SimplifiedEditor, IOpenXRMigrableEditor
    {
        private HandSphereMap _handSphereMap;
        private bool _unrollConverter;

        protected void Awake()
        {
            _handSphereMap = target as HandSphereMap;
        }

        protected override void OnEnable()
        {
            if (!target)
                return;
            
            base.OnEnable();
            _unrollConverter = NeedsConversion(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            this.OpenXRConversionMenu(ref _unrollConverter,
                ConvertAndCheckValidity,
                Array.Empty<string>());
        }

        public bool NeedsConversion(SerializedObject target)
        {
            var wrist = _handSphereMap._handPrefabDataSource.GetTransformFor(HandJointId.HandWristRoot);

            if (wrist == null)
            {
                return true;
            }

            int nullJointTransformCount = 0;
            for (int i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                var joint = _handSphereMap._handPrefabDataSource.GetTransformFor((HandJointId)i);
                if (joint == null)
                {
                    nullJointTransformCount++;
                }
            }

            if (nullJointTransformCount > 0)
            {
                return true;
            }

            var allKids = wrist.GetComponentsInChildren<Transform>();
            var firstJointSphere = allKids.FirstOrDefault(k => k.gameObject.name == "sphere");
            return firstJointSphere == null;
        }

        public void Convert(SerializedObject target)
        {
            int nullJointTransformCount = 0;
            for (int i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                var joint = _handSphereMap._handPrefabDataSource.GetTransformFor((HandJointId)i);
                if (joint == null)
                {
                    nullJointTransformCount++;
                }
            }
            if (nullJointTransformCount > 0)
            {
                Debug.LogError($"{nullJointTransformCount} missing "
                + $"joint{(nullJointTransformCount>1?"s":String.Empty)}. OpenXR Hand Joints must be "
                + "configured before generating joints for the HandSphereMap", _handSphereMap._handPrefabDataSource);
                return;
            }
            HandSphereMapGenerator.RegenerateJointSpheres(_handSphereMap._handPrefabDataSource);
        }

        private bool ConvertAndCheckValidity(SerializedObject target)
        {
            Convert(target);
            return !NeedsConversion(target);
        }
    }
}
#endif
