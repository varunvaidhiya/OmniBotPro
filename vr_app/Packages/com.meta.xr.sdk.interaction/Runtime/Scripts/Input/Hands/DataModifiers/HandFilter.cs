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

using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;

namespace Oculus.Interaction.Input.Filter
{
    /// <summary>
    /// Smoothes hand position input data. If you want to fine-tune the smoothing, it also accepts an optional set of filter parameters
    /// (<see cref="HandFilterParameterBlock"/>). Use High if your app requires a very steady hand and can tolerate some lag. High
    /// should probably only be used in controller circumstances. Use Low if only a small amount of steadiness needs to be added to the
    /// hand. For example, to do a steady raycast from a hand.
    /// </summary>
    public class HandFilter : Hand
    {
        #region Tuneable Values
        [Header("Settings", order = -1)]
        [Tooltip("Applies a One Euro Filter when filter parameters are provided")]
        [SerializeField, Optional]
        private HandFilterParameterBlock _filterParameters = null;
        #endregion Tuneable Values

#if ISDK_OPENXR_HAND
        private readonly IOneEuroFilter<Quaternion> _rootRotFilter = OneEuroFilter.CreateQuaternion();
        private readonly IOneEuroFilter<Vector3> _rootPosFilter = OneEuroFilter.CreateVector3();
        private readonly IOneEuroFilter<Vector3>[] _jointPosFilter = new IOneEuroFilter<Vector3>[Constants.NUM_HAND_JOINTS];
        private readonly IOneEuroFilter<Quaternion>[] _jointRotFilter = new IOneEuroFilter<Quaternion>[Constants.NUM_HAND_JOINTS];
#endif

        protected virtual void Awake()
        {
#if ISDK_OPENXR_HAND
            for (var i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                _jointPosFilter[i] = OneEuroFilter.CreateVector3();
                _jointRotFilter[i] = OneEuroFilter.CreateQuaternion();

            }
#endif
        }

        protected override void Apply(HandDataAsset handDataAsset)
        {
            base.Apply(handDataAsset);

            if (!handDataAsset.IsTracked)
            {
                return;
            }

            if (UpdateFilterParameters() && UpdateHandData(handDataAsset))
            {
                return;
            }
        }

        protected bool UpdateFilterParameters()
        {
            if (_filterParameters == null)
                return true;
#if ISDK_OPENXR_HAND

            _rootRotFilter.SetProperties(_filterParameters.wristRotationParameters);
            _rootPosFilter.SetProperties(_filterParameters.wristPositionParameters);
            for (var i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                _jointRotFilter[i].SetProperties(_filterParameters.fingerRotationParameters);
            }
#endif
            return true;
        }

        private HandJointCache _handJointCache = new();

        protected bool UpdateHandData(HandDataAsset handDataAsset)
        {
            // null parameters implies don't filter
            if (_filterParameters == null)
                return true;

#if ISDK_OPENXR_HAND
            var dt = 1.0f / _filterParameters.frequency;

            _handJointCache.Update(handDataAsset, CurrentDataVersion);

            var filteredRootPose = handDataAsset.Root;
            handDataAsset.Root = new Pose(
                _rootPosFilter.Step(filteredRootPose.position, dt),
                _rootRotFilter.Step(filteredRootPose.rotation, dt)
                );

            // Update rotations to filtered values
            for (var i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                var parentId = HandJointUtils.JointParentList[i];
                var parentPoseFromRoot = parentId == HandJointId.Invalid ?
                    Pose.identity : handDataAsset.JointPoses[(int)parentId];

                var jointId = (HandJointId)i;
                var jointPoseLocal = _handJointCache.GetLocalJointPose(jointId);

                var filterRot = _jointRotFilter[i].Step(
                    jointPoseLocal.rotation, dt
                );
                jointPoseLocal.rotation = filterRot;
                handDataAsset.JointPoses[i] = PoseUtils.Multiply(parentPoseFromRoot, jointPoseLocal);

#pragma warning disable 0618
                // Legacy local poses
                handDataAsset.Joints[i] = jointPoseLocal.rotation;
#pragma warning restore 0618
            }

#else 
            // OVR path is deprecated and performs no hand filtering. Use OpenXR path.
#endif
            return true;
        }
    }
}
