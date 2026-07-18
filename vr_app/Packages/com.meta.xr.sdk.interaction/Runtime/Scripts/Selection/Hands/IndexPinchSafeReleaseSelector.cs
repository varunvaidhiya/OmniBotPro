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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This Selector selects for a frame when the Pinch is released, as opposed to when it is pinching.
    /// It uses the system pinch (index and thumb) but due to some false-positives with pinch detection,
    /// to establish that a pinch has released the index must be not pinching and be extended.
    /// </summary>
    public class IndexPinchSafeReleaseSelector : MonoBehaviour,
        ISelector, IActiveState
    {
        /// <summary>
        /// The hand to check.
        /// </summary>
        [Tooltip("The hand to check.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        /// <summary>
        /// If checked, the selector will select during the frame when the pinch is released as opposed to when it's pinching.
        /// </summary>
        [Tooltip("If checked, the selector will select during the frame when the pinch is released as opposed to when it's pinching.")]
        [SerializeField]
        private bool _selectOnRelease = true;
        public bool SelectOnRelease
        {
            get => _selectOnRelease;
            set => _selectOnRelease = value;
        }

        /// <summary>
        /// Indicates how extended the index needs to be in order to be safe to unpinch
        /// </summary>
        [Tooltip("Indicates how extended the index needs to be in order to be safe to unpinch.")]
        [SerializeField, Range(-1f, 1f)]
        private float _safeReleaseThreshold = 0.5f;
        public float SafeReleaseThreshold
        {
            get => _safeReleaseThreshold;
            set => _safeReleaseThreshold = value;
        }

        /// <summary>
        /// Turns true when the hand is pinching, then goes back to false only if the hand
        /// releases the pinch with the index finger extended.
        /// </summary>
        public bool Active => _active;

        private bool _wasPinching;
        private bool _active;
        private bool _pendingUnselect;

        public event Action WhenSelected = delegate { };
        public event Action WhenUnselected = delegate { };

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
                _wasPinching = Hand.GetIndexFingerIsPinching();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;

                if (_active)
                {
                    _active = false;
                    this.WhenUnselected.Invoke();
                }

                _pendingUnselect = false;
            }
        }

        private void HandleHandUpdated()
        {
            if (_selectOnRelease && _pendingUnselect)
            {
                _pendingUnselect = false;
                this.WhenUnselected.Invoke();
            }

            bool isPinching = Hand.GetIndexFingerIsPinching();

            if (_wasPinching != isPinching)
            {
                _wasPinching = isPinching;
                if (isPinching)
                {
                    _active = true;
                    if (!_selectOnRelease)
                    {
                        this.WhenSelected.Invoke();
                    }
                }
            }

            if (_active && !isPinching && IsIndexExtended())
            {
                if (_selectOnRelease)
                {
                    this.WhenSelected.Invoke();
                    _pendingUnselect = true;
                }
                else
                {
                    this.WhenUnselected.Invoke();
                }
                _active = false;
            }
        }

        protected virtual bool IsIndexExtended()
        {
            if (Hand.GetFingerPinchStrength(HandFinger.Index) == 0f)
            {
                // release pinch if no strength
                return true;
            }

            if (!Hand.GetJointPoseFromWrist(HandJointId.HandIndex1, out Pose index1)
                || !Hand.GetJointPoseFromWrist(HandJointId.HandIndex2, out Pose index2)
                || !Hand.GetJointPoseFromWrist(HandJointId.HandIndexTip, out Pose indexTip))
            {
                // if hand not tracked, or if index finger has missing poses release pinch
                return true;
            }

            Vector3 proximalDir = (index2.position - index1.position).normalized;
            Vector3 medialDistalDir = (indexTip.position - index2.position).normalized;

            float angularDifference = Vector3.Dot(proximalDir, medialDistalDir);
            // release if index curl is past threshold
            return angularDifference >= _safeReleaseThreshold;
        }

        [Obsolete("Disable the component to Cancel any ongoing pinch")]
        public void Cancel() { }

        #region Inject

        public void InjectAllIndexPinchSafeReleaseSelector(IHand hand)
        {
            InjectHand(hand);
        }


        [Obsolete("Use " + nameof(SelectOnRelease) + " setter instead.")]
        public void InjectSelectOnRelease(bool selectOnRelease) { }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        #endregion
    }
}
