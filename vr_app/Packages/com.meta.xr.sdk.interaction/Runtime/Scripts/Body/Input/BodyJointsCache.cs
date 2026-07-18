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
using Oculus.Interaction.Input;
using System;

namespace Oculus.Interaction.Body.Input
{
    public class BodyJointsCache : SkeletonJointsCache
    {
        private ReadOnlyBodyJointPoses _posesFromRootCollection;
        private ReadOnlyBodyJointPoses _worldPosesCollection;
        private ReadOnlyBodyJointPoses _localPosesCollection;

        private readonly ISkeletonMapping _mapping;

        protected override bool TryGetParent(int joint, out int parent)
        {
            if (_mapping.TryGetParentJointId((BodyJointId)joint,
                out BodyJointId parentId))
            {
                parent = (int)parentId;
                return true;
            }
            parent = -1;
            return false;
        }

        public BodyJointsCache(ISkeletonMapping mapping) : base(
            Constants.NUM_BODY_JOINTS)
        {
            _mapping = mapping;
            _localPosesCollection = new ReadOnlyBodyJointPoses(_localPoses);
            _worldPosesCollection = new ReadOnlyBodyJointPoses(_worldPoses);
            _posesFromRootCollection = new ReadOnlyBodyJointPoses(_posesFromRoot);
        }

        public void Update(BodyDataAsset data, int dataVersion,
            Transform trackingSpace = null)
        {
            if (!data.IsDataValid)
            {
                return;
            }

            base.Update(dataVersion, data.Root,
                data.JointPoses, data.RootScale,
                trackingSpace);
        }

        public Pose GetLocalJointPose(BodyJointId jointId) => base.GetLocalJointPose((int)jointId);
        public Pose GetJointPoseFromRoot(BodyJointId jointId) => base.GetJointPoseFromRoot((int)jointId);
        public Pose GetWorldJointPose(BodyJointId jointId) => base.GetWorldJointPose((int)jointId);
    }
}
