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
using System.Buffers;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// TouchHandGrabInteractorVisual forwards the finger state of an associated
    /// TouchHandGrabInteractor to a SyntheticDataModifier to lock and unlock
    /// finger joints in the synthetic hand's target hand data.
    /// </summary>
    public class TouchHandGrabInteractorVisual : MonoBehaviour
    {

        [SerializeField]
        private TouchHandGrabInteractor _interactor;

        [SerializeField]
        private SyntheticHand _syntheticHand;

        #region Inject

        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        #endregion

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_interactor, nameof(_interactor));
            this.AssertField(_syntheticHand, nameof(_syntheticHand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _interactor.WhenFingerLocked += UpdateLocks;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _interactor.WhenFingerLocked -= UpdateLocks;
            }
        }

        private void UpdateLocks()
        {
            bool forceUpdate = false;
            for (int i = 0; i < 5; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (_interactor.IsFingerLocked(finger))
                {
                    Pose[] fingerJoints = _interactor.GetFingerJoints(finger);
                    Quaternion[] rotations = ArrayPool<Quaternion>.Shared.Rent(fingerJoints.Length);
                    for (int j = 0; j < fingerJoints.Length; j++)
                    {
                        rotations[j] = fingerJoints[j].rotation;
                    }
                    _syntheticHand.OverrideFingerRotations(finger, rotations, 1.0f);
                    _syntheticHand.SetFingerFreedom(finger, JointFreedom.Locked, true);
                    ArrayPool<Quaternion>.Shared.Return(rotations);
                    forceUpdate = true;
                }
                else
                {
                    _syntheticHand.SetFingerFreedom(finger, JointFreedom.Free);
                }
            }

            if (forceUpdate)
            {
                _syntheticHand.MarkInputDataRequiresUpdate();
            }
        }

        protected virtual void Update()
        {
            UpdateLocks();
        }
    }
}
