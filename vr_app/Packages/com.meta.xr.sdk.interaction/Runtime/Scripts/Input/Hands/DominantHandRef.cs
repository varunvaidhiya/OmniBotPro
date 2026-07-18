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

using System;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// DominantHandRef is a utility component that delegates all of its IHand implementation
    /// to the Left or the Right Hand provided depending on which one is the Dominant one.
    /// </summary>
    public class DominantHandRef : MonoBehaviour, IHand, IActiveState
    {
        /// <summary>
        /// The Left Hand
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _leftHand;
        public IHand LeftHand { get; private set; }

        /// <summary>
        /// The Right Hand
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _rightHand;
        public IHand RightHand { get; private set; }

        /// <summary>
        /// If true, the Hand will point to the Dominant hand.
        /// If false it will point to the Non Dominant Hand
        /// </summary>
        [SerializeField]
        [Tooltip("If true, the HandRef will point to the Dominant hand. " +
            "If false it will point to the Non Dominant Hand")]
        private bool _selectDominant = true;
        public bool SelectDominant
        {
            get => _selectDominant;
            set => _selectDominant = value;
        }

        public IHand Hand => LeftHand.IsDominantHand == _selectDominant ? LeftHand : RightHand;

        public Handedness Handedness => Hand.Handedness;

        public bool IsConnected => Hand.IsConnected;

        public bool IsHighConfidence => Hand.IsHighConfidence;

        public bool IsDominantHand => Hand.IsDominantHand;

        public float Scale => Hand.Scale;

        public bool IsPointerPoseValid => Hand.IsPointerPoseValid;

        public bool IsTrackedDataValid => Hand.IsTrackedDataValid;

        public int CurrentDataVersion => Hand.CurrentDataVersion;

        private Action _whenHandUpdated = delegate { };
        public event Action WhenHandUpdated
        {
            add => _whenHandUpdated += value;
            remove => _whenHandUpdated -= value;
        }

        public bool Active => IsConnected;

        protected bool _started = false;

        protected virtual void Awake()
        {
            if (LeftHand == null)
            {
                LeftHand = _leftHand as IHand;
            }
            if (RightHand == null)
            {
                RightHand = _rightHand as IHand;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(LeftHand, nameof(_leftHand));
            this.AssertField(RightHand, nameof(_rightHand));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                LeftHand.WhenHandUpdated += HandleLeftHandUpdated;
                RightHand.WhenHandUpdated += HandleRightHandUpdated;
                if (LeftHand.IsConnected)
                {
                    HandleLeftHandUpdated();
                }
                if (RightHand.IsConnected)
                {
                    HandleRightHandUpdated();
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                LeftHand.WhenHandUpdated -= HandleLeftHandUpdated;
                RightHand.WhenHandUpdated -= HandleRightHandUpdated;
            }
        }

        private void HandleLeftHandUpdated()
        {
            if (LeftHand.IsDominantHand == _selectDominant)
            {
                _whenHandUpdated.Invoke();
            }
        }

        private void HandleRightHandUpdated()
        {
            if (RightHand.IsDominantHand == _selectDominant)
            {
                _whenHandUpdated.Invoke();
            }
        }

        public bool GetFingerIsPinching(HandFinger finger)
        {
            return Hand.GetFingerIsPinching(finger);
        }

        public bool GetIndexFingerIsPinching()
        {
            return Hand.GetIndexFingerIsPinching();
        }

        public bool GetPointerPose(out Pose pose)
        {
            return Hand.GetPointerPose(out pose);
        }

        public bool GetJointPose(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPose(handJointId, out pose);
        }

        public bool GetJointPoseLocal(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPoseLocal(handJointId, out pose);
        }

        public bool GetJointPosesLocal(out ReadOnlyHandJointPoses jointPosesLocal)
        {
            return Hand.GetJointPosesLocal(out jointPosesLocal);
        }

        public bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose)
        {
            return Hand.GetJointPoseFromWrist(handJointId, out pose);
        }

        public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
        {
            return Hand.GetJointPosesFromWrist(out jointPosesFromWrist);
        }

        public bool GetPalmPoseLocal(out Pose pose)
        {
            return Hand.GetPalmPoseLocal(out pose);
        }

        public bool GetFingerIsHighConfidence(HandFinger finger)
        {
            return Hand.GetFingerIsHighConfidence(finger);
        }

        public float GetFingerPinchStrength(HandFinger finger)
        {
            return Hand.GetFingerPinchStrength(finger);
        }

        public bool GetRootPose(out Pose pose)
        {
            return Hand.GetRootPose(out pose);
        }

        #region Inject
        public void InjectAllDominantHandRef(IHand leftHand, IHand rightHand)
        {
            InjectLeftHand(leftHand);
            InjectRightHand(rightHand);
        }

        public void InjectLeftHand(IHand leftHand)
        {
            _leftHand = leftHand as UnityEngine.Object;
            LeftHand = leftHand;
        }

        public void InjectRightHand(IHand rightHand)
        {
            _rightHand = rightHand as UnityEngine.Object;
            RightHand = rightHand;
        }
        #endregion
    }
}
