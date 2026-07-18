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
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This ActiveState checks that the wrist rotation relative to the arm
    /// (approximated by the direction from the estimated shoulder to the wrist) stays
    /// within a valid range.
    /// </summary>
    public class WristAngleActiveState : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// The hand whose wrist will be checked
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }
        /// <summary>
        /// The estimated position of the shoulder of the referenced hand
        /// </summary>
        [SerializeField]
        private Transform _shoulder;
        /// <summary>
        /// Min angle of the wrist relative to the arm allowed
        /// </summary>
        [SerializeField]
        private float _minAngle = -70f;
        public float MinAngle
        {
            get => _minAngle;
            set => _minAngle = value;
        }
        /// <summary>
        /// Max angle of the wrist relative to the arm allowed
        /// </summary>
        [SerializeField]
        private float _maxAngle = 170f;
        public float MaxAngle
        {
            get => _maxAngle;
            set => _maxAngle = value;
        }
        /// <summary>
        /// True if the angle of the wrist relative to the arm is
        /// whithin the valid range
        /// </summary>
        public bool Active { get; private set; } = false;

        private float _currentAngle;

        private const float _wristLimit = -70f;
        protected bool _started;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(Hand, nameof(_hand));
            this.AssertField(_shoulder, nameof(_shoulder));

            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            _currentAngle = CalculateAngle();
            Active = _currentAngle > _minAngle && _currentAngle < _maxAngle;
        }

        private float CalculateAngle()
        {
            if (!Hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose))
            {
                return _currentAngle;
            }

            bool isRightHand = Hand.Handedness == Handedness.Right;
            Vector3 trackingUp = Vector3.up;
            Vector3 shoulderToHand = (wristPose.position - _shoulder.position).normalized;
            Vector3 trackingRight = Vector3.Cross(trackingUp, shoulderToHand).normalized;
            trackingRight = isRightHand ? trackingRight : -trackingRight;
            Vector3 wristDir = wristPose.rotation * (isRightHand ? Constants.RightThumbSide : Constants.LeftThumbSide);

            wristDir = Vector3.ProjectOnPlane(wristDir, shoulderToHand).normalized;
            float angle = Vector3.SignedAngle(wristDir, trackingRight, shoulderToHand);

            angle = Hand.Handedness == Handedness.Right ? -angle : angle;
            if (angle < _wristLimit)
            {
                angle += 360f;
            }

            return angle;
        }

        #region Injects

        public void InjectAllWristAngleActiveState(IHand hand, Transform shoulder)
        {
            InjectHand(hand);
            InjectShoulder(shoulder);
        }

        public void InjectHand(IHand hand)
        {
            Hand = hand;
            _hand = hand as UnityEngine.Object;
        }

        public void InjectShoulder(Transform shoulder)
        {
            _shoulder = shoulder;
        }

        #endregion
    }
}
