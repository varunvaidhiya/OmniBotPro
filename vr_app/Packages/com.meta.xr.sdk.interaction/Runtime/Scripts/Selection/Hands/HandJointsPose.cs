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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Updates the transform to be the interpolated position
    /// of a series of joints in the associated IHand.
    /// </summary>
    public class HandJointsPose : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [System.Serializable]
        public struct WeightedJoint
        {
            public HandJointId handJointId;
            public float weight;
        }

        #region OVR Fields

        [SerializeField]
        [InspectorName("Weighted Joints")]
        private List<WeightedJoint> _weightedJoints;

        [SerializeField]
        [InspectorName("Offset")]
        private Vector3 _localPositionOffset;

        [SerializeField]
        [InspectorName("Rotation")]
        private Quaternion _rotationOffset = Quaternion.identity;

        #endregion OVR Fields

        #region OpenXR Fields

        [SerializeField]
        [InspectorName("Weighted Joints")]
        private List<WeightedJoint> _joints;

        [SerializeField]
        [InspectorName("Offset")]
        private Vector3 _posOffset;

        [SerializeField]
        [InspectorName("Rotation")]
        private Quaternion _rotOffset = Quaternion.identity;

        #endregion OpenXR Fields

        [SerializeField]
        [Tooltip("When the attached hand's handedness is set to Left, this property will mirror the offsets. " +
            "This allows for offset values to be set in Right hand coordinates for both Left and Right hands.")]
        private bool _mirrorOffsetsForLeftHand = true;
        public bool MirrorOffsetsForLeftHand
        {
            get => _mirrorOffsetsForLeftHand;
            set => _mirrorOffsetsForLeftHand = value;
        }

        #region Properties

        public List<WeightedJoint> WeightedJoints
        {
#if ISDK_OPENXR_HAND
            get => _joints;
            set => _joints = value;
#else
            get => _weightedJoints;
            set => _weightedJoints = value;
#endif
        }

        public Vector3 LocalPositionOffset
        {
#if ISDK_OPENXR_HAND
            get => _posOffset;
            set => _posOffset = value;
#else
            get => _localPositionOffset;
            set => _localPositionOffset = value;
#endif
        }

        public Quaternion RotationOffset
        {
#if ISDK_OPENXR_HAND
            get => _rotOffset;
            set => _rotOffset = value;
#else
            get => _rotationOffset;
            set => _rotationOffset = value;
#endif
        }

        #endregion

        private Pose _cachedPose = Pose.identity;
        protected bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandUpdated;
                if (Hand.IsConnected)
                {
                    HandleHandUpdated();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        private void HandleHandUpdated()
        {
            Pose pose = Pose.identity;
            float accumulatedWeight = 0f;
            foreach (WeightedJoint weightedJoint in WeightedJoints)
            {
                if (!Hand.GetJointPose(weightedJoint.handJointId, out Pose jointPose))
                {
                    return;
                }

                float t = weightedJoint.weight / (accumulatedWeight + weightedJoint.weight);
                accumulatedWeight += weightedJoint.weight;
                pose.Lerp(jointPose, t);
            }

#if ISDK_OPENXR_HAND
            GetOffset(ref _cachedPose, Hand.Handedness, Hand.Scale);
            _cachedPose.Postmultiply(pose);
#else
            Vector3 positionOffsetWithHandedness = Hand.Handedness == Handedness.Left && MirrorOffsetsForLeftHand ?
                HandMirroring.Mirror(_localPositionOffset) : _localPositionOffset;
            //Note that RotationOffset should be on the right of pose.rotation in order to be applied locally.
            //having it pre-multiplying can yield unwanted results.
            pose.position += _rotationOffset * pose.rotation *
                              positionOffsetWithHandedness * Hand.Scale;
#endif

            transform.SetPose(pose);
        }

        private void GetOffset(ref Pose pose, Handedness handedness, float scale)
        {
            if (_mirrorOffsetsForLeftHand && handedness == Handedness.Left)
            {
                pose.position = HandMirroring.Mirror(LocalPositionOffset * scale);
                pose.rotation = HandMirroring.Mirror(RotationOffset);
            }
            else
            {
                pose.position = LocalPositionOffset * scale;
                pose.rotation = RotationOffset;
            }
        }

        #region Inject

        public void InjectAllHandJoint(IHand hand)
        {
            InjectHand(hand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        #endregion;
    }
}
