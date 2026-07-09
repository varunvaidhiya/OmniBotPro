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

namespace Oculus.Interaction.Input
{
    public abstract class SkeletonJointsCache
    {
        private const int ULONG_BITS = 64;

        public int LocalDataVersion { get; private set; } = -1;

        protected Pose[] _originalPoses;
        protected Pose[] _posesFromRoot;
        protected Pose[] _localPoses;
        protected Pose[] _worldPoses;

        private ulong[] _dirtyJointsFromRoot;
        private ulong[] _dirtyLocalJoints;
        private ulong[] _dirtyWorldJoints;

        private Matrix4x4 _scale;
        private Pose _rootPose;
        private Pose _worldRoot;

        private readonly int _numJoints;
        private readonly int _dirtyArraySize;

        protected abstract bool TryGetParent(int joint, out int parent);

        public SkeletonJointsCache(int numJoints)
        {
            LocalDataVersion = -1;

            _numJoints = numJoints;
            _originalPoses = new Pose[numJoints];
            _posesFromRoot = new Pose[numJoints];
            _localPoses = new Pose[numJoints];
            _worldPoses = new Pose[numJoints];

            _dirtyArraySize = 1 + (numJoints / ULONG_BITS);
            _dirtyJointsFromRoot = new ulong[_dirtyArraySize];
            _dirtyLocalJoints = new ulong[_dirtyArraySize];
            _dirtyWorldJoints = new ulong[_dirtyArraySize];
        }

        public void Update(int dataVersion, Pose rootPose,
            Pose[] jointPoses, float scale, Transform trackingSpace = null)
        {
            LocalDataVersion = dataVersion;

            for (int i = 0; i < _dirtyArraySize; ++i)
            {
                _dirtyJointsFromRoot[i] = ulong.MaxValue;
                _dirtyLocalJoints[i] = ulong.MaxValue;
                _dirtyWorldJoints[i] = ulong.MaxValue;
            }

            _scale = Matrix4x4.Scale(Vector3.one * scale);
            _rootPose = rootPose;
            _worldRoot = _rootPose;

            if (trackingSpace != null)
            {
                _scale *= Matrix4x4.Scale(trackingSpace.lossyScale);
                _worldRoot.position = trackingSpace.TransformPoint(_rootPose.position);
                _worldRoot.rotation = trackingSpace.rotation * _rootPose.rotation;
            }

            System.Array.Copy(jointPoses, _originalPoses, _numJoints);
        }

        public Pose GetLocalJointPose(int jointId)
        {
            UpdateLocalJointPose(jointId);
            return _localPoses[jointId];
        }

        public Pose GetJointPoseFromRoot(int jointId)
        {
            UpdateJointPoseFromRoot(jointId);
            return _posesFromRoot[jointId];
        }

        public Pose GetWorldJointPose(int jointId)
        {
            UpdateWorldJointPose(jointId);
            return _worldPoses[jointId];
        }

        public Pose GetWorldRootPose()
        {
            return _worldRoot;
        }

        private void UpdateJointPoseFromRoot(int jointId)
        {
            if (!CheckJointDirty(jointId, _dirtyJointsFromRoot))
            {
                return;
            }

            int index = jointId;
            _posesFromRoot[index] = _originalPoses[index];
            SetJointClean(jointId, _dirtyJointsFromRoot);
        }

        private void UpdateLocalJointPose(int jointId)
        {
            if (!CheckJointDirty(jointId, _dirtyLocalJoints))
            {
                return;
            }

            if (TryGetParent(jointId, out int parentId))
            {
                Pose originalPose = _originalPoses[jointId];
                Pose parentPose = _originalPoses[parentId];

                Vector3 localPos = Quaternion.Inverse(parentPose.rotation) *
                    (originalPose.position - parentPose.position);
                Quaternion localRot = Quaternion.Inverse(parentPose.rotation) *
                    originalPose.rotation;
                _localPoses[jointId] = new Pose(localPos, localRot);
            }
            else
            {
                _localPoses[jointId] = Pose.identity;
            }
            SetJointClean(jointId, _dirtyLocalJoints);
        }

        private void UpdateWorldJointPose(int jointId)
        {
            if (!CheckJointDirty(jointId, _dirtyWorldJoints))
            {
                return;
            }

            Pose fromRoot = GetJointPoseFromRoot(jointId);
            fromRoot.position = _scale * fromRoot.position;
            fromRoot.Postmultiply(GetWorldRootPose());
            _worldPoses[jointId] = fromRoot;
            SetJointClean(jointId, _dirtyWorldJoints);
        }

        protected void UpdateAllWorldPoses()
        {
            for (int i = 0; i < _numJoints; ++i)
            {
                UpdateWorldJointPose(i);
            }
        }

        protected void UpdateAllLocalPoses()
        {
            for (int i = 0; i < _numJoints; ++i)
            {
                UpdateLocalJointPose(i);
            }
        }

        protected void UpdateAllPosesFromRoot()
        {
            for (int i = 0; i < _numJoints; ++i)
            {
                UpdateJointPoseFromRoot(i);
            }
        }

        private bool CheckJointDirty(int jointId, ulong[] dirtyFlags)
        {
            int outerIdx = jointId / ULONG_BITS;
            int innerIdx = jointId % ULONG_BITS;
            return (dirtyFlags[outerIdx] & (1UL << innerIdx)) != 0;
        }

        private void SetJointClean(int jointId, ulong[] dirtyFlags)
        {
            int outerIdx = jointId / ULONG_BITS;
            int innerIdx = jointId % ULONG_BITS;
            dirtyFlags[outerIdx] = dirtyFlags[outerIdx] & ~(1UL << innerIdx);
        }
    }
}
