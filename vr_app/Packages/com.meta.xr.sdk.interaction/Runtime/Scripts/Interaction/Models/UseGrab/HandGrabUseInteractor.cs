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

using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This interactor allows sending use-strength values to an interactable to create use interactions.
    /// For example, pressing a trigger, squeezing a ball, etc. In order to calculate the usage strength of a finger it uses a
    /// <see cref="IFingerUseAPI"/>. This class is also an <see cref="IHandGrabState"/>, so it can be attached to a
    /// <see cref="SyntheticHand"/> to drive the fingers rotations, lerping between the relaxed hand pose and the tight hand pose
    /// provided by the interactable depending on the progress of the action.
    /// </summary>
    public class HandGrabUseInteractor : Interactor<HandGrabUseInteractor, HandGrabUseInteractable>
        , IHandGrabState
    {
        /// <summary>
        /// The hand to use.
        /// </summary>
        [Tooltip("The hand to use.")]
        [SerializeField, Optional, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// The <see cref="IHand"/> used for both grabbing and using. Though HandGrabUseInteractor is not an
        /// <see cref="IHandGrabInteractor"/>, this property is contractually similar to <see cref="IHandGrabInteractor.Hand"/>.
        /// </summary>
        public IHand Hand { get; private set; }

        /// <summary>
        /// API that gets the finger use strength.
        /// </summary>
        [Tooltip("API that gets the finger use strength.")]
        [SerializeField, Interface(typeof(IFingerUseAPI))]
        private UnityEngine.Object _useAPI;

        /// <summary>
        /// The <see cref="IFingerUseAPI"/> invoked by this interactor to determine the "finger use strength" of the
        /// <see cref="Hand"/>'s fingers. "Strength" in this context is a measure of of how similar/different the finger's state is
        /// to what the system considers "using"; a related overview of "strength" can be found in the documentation for
        /// <see cref="IHand.GetFingerPinchStrength(HandFinger)"/>.
        /// </summary>
        public IFingerUseAPI UseAPI { get; private set; }

        private HandPose _relaxedHandPose = new HandPose();
        private HandPose _tightHandPose = new HandPose();

        private HandPose _cachedRelaxedHandPose = new HandPose();
        private HandPose _cachedTightHandPose = new HandPose();

        private HandFingerFlags _fingersInUse = HandFingerFlags.None;
        private float[] _fingerUseStrength = new float[Constants.NUM_FINGERS];
        private bool _usesHandPose;
        private bool _handUseShouldSelect;
        private bool _handUseShouldUnselect;

        /// <summary>
        /// The <see cref="HandGrab.HandGrabTarget"/> used by this interactor when grabbing.
        /// </summary>
        public HandGrabTarget HandGrabTarget { get; } = new HandGrabTarget();

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.IsGrabbing"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsGrabbing => SelectedInteractable != null;

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.WristStrength"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float WristStrength => 0f;

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.FingersStrength"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float FingersStrength => IsGrabbing ? 1f : 0f;

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.WristToGrabPoseOffset"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Pose WristToGrabPoseOffset => Pose.identity;

        /// <summary>
        /// An event which could indicate that the current interactor (which would be passed to the event as an
        /// <see cref="IHandGrabState"/>) began grabbing. In the current implementation, this event is never invoked. Instead,
        /// <see cref="Interactor{TInteractor, TInteractable}.WhenStateChanged"/> can be used to respond to the beginning of a grab;
        /// when that event is called with its argument's <see cref="InteractorStateChangeArgs.NewState"/> property set to
        /// <see cref="InteractorState.Select"/>, a grab interaction has started.
        /// </summary>
        public Action<IHandGrabState> WhenHandGrabStarted { get; set; } = delegate { };

        /// <summary>
        /// An event which could indicate that the current interactor (which would be passed to the event as an
        /// <see cref="IHandGrabState"/>) stopped grabbing. In the current implementation, this event is never invoked. Instead,
        /// <see cref="Interactor{TInteractor, TInteractable}.WhenStateChanged"/> can be used to respond to the beginning of a grab;
        /// when that event is called with its argument's <see cref="InteractorStateChangeArgs.PreviousState"/> property set to
        /// <see cref="InteractorState.Select"/>, a grab interaction has ended.
        /// </summary>
        public Action<IHandGrabState> WhenHandGrabEnded { get; set; } = delegate { };

        protected override bool ComputeShouldSelect()
        {
            return _handUseShouldSelect;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _handUseShouldUnselect || SelectedInteractable == null;
        }

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            UseAPI = _useAPI as IFingerUseAPI;
            _nativeId = 0x4847726162557365;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(UseAPI, nameof(UseAPI));
            this.EndStart(ref _started);
        }

        protected override void InteractableSelected(HandGrabUseInteractable interactable)
        {
            base.InteractableSelected(interactable);
            StartUsing();
        }

        protected override void InteractableUnselected(HandGrabUseInteractable interactable)
        {
            base.InteractableUnselected(interactable);

            _fingersInUse = HandFingerFlags.None;
        }

        private void StartUsing()
        {
            HandGrabResult result = new HandGrabResult()
            {
                HasHandPose = true,
                HandPose = _relaxedHandPose
            };

            HandGrabTarget.Set(SelectedInteractable.transform,
                HandAlignType.AlignOnGrab, Grab.GrabTypeFlags.None, result);
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();
            _handUseShouldSelect = IsUsingInteractable(Interactable);
        }

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();

            if (SelectedInteractable == null)
            {
                return;
            }

            float useStrength = CalculateUseStrength(ref _fingerUseStrength);
            float progress = SelectedInteractable.ComputeUseStrength(useStrength);
            _handUseShouldUnselect = !IsUsingInteractable(Interactable);
            if (_usesHandPose && !_handUseShouldUnselect)
            {
                MoveFingers(ref _fingerUseStrength, progress);
            }
        }

        private bool IsUsingInteractable(HandGrabUseInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (interactable.UseFingers[finger] == FingerRequirement.Ignored)
                {
                    continue;
                }
                float strength = UseAPI.GetFingerUseStrength(finger);
                if (strength > interactable.StrengthDeadzone)
                {
                    return true;
                }
            }
            return false;
        }

        private float CalculateUseStrength(ref float[] fingerUseStrength)
        {
            float requiredStrength = 1f;
            float optionalStrength = 0;
            bool requiredSet = false;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;

                if (SelectedInteractable.UseFingers[finger] == FingerRequirement.Ignored)
                {
                    fingerUseStrength[i] = 0f;
                    continue;
                }

                float strength = UseAPI.GetFingerUseStrength(finger);
                fingerUseStrength[i] = Mathf.Clamp01((strength - SelectedInteractable.UseStrengthDeadZone) / (1f - SelectedInteractable.UseStrengthDeadZone));

                if (SelectedInteractable.UseFingers[finger] == FingerRequirement.Required)
                {
                    requiredSet = true;
                    requiredStrength = Mathf.Min(requiredStrength, fingerUseStrength[i]);
                }
                else if (SelectedInteractable.UseFingers[finger] == FingerRequirement.Optional)
                {
                    optionalStrength = Mathf.Max(optionalStrength, fingerUseStrength[i]);
                }

                if (fingerUseStrength[i] > 0)
                {
                    MarkFingerInUse(finger);
                }
                else
                {
                    UnmarkFingerInUse(finger);
                }
            }

            return requiredSet ? requiredStrength : optionalStrength;
        }

        private void MoveFingers(ref float[] fingerUseProgress, float useProgress)
        {
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                float progress = Mathf.Min(useProgress, fingerUseProgress[i]);

                LerpFingerRotation(_relaxedHandPose.JointRotations,
                  _tightHandPose.JointRotations,
                  HandGrabTarget.HandPose.JointRotations,
                  finger, progress);
            }
        }

        private void MarkFingerInUse(HandFinger finger)
        {
            _fingersInUse = (HandFingerFlags)(((int)_fingersInUse) | (1 << (int)finger));
        }

        private void UnmarkFingerInUse(HandFinger finger)
        {
            _fingersInUse = (HandFingerFlags)(((int)_fingersInUse) & ~(1 << (int)finger));
        }

        private void LerpFingerRotation(Quaternion[] from, Quaternion[] to, Quaternion[] result, HandFinger finger, float t)
        {
            int[] joints = FingersMetadata.FINGER_TO_JOINT_INDEX[(int)finger];
            for (int i = 0; i < joints.Length; i++)
            {
                int jointIndex = joints[i];
                result[jointIndex] = Quaternion.Slerp(from[jointIndex], to[jointIndex], t);
            }
        }

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.GrabbingFingers"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public HandFingerFlags GrabbingFingers()
        {
            return _fingersInUse;
        }

        protected override HandGrabUseInteractable ComputeCandidate()
        {
            float bestScore = float.NegativeInfinity;
            HandGrabUseInteractable bestCandidate = null;

            _usesHandPose = false;
            var candidates = HandGrabUseInteractable.Registry.List(this);
            foreach (HandGrabUseInteractable candidate in candidates)
            {
                candidate.FindBestHandPoses(Hand != null ? Hand.Scale : 1f,
                    ref _cachedRelaxedHandPose, ref _cachedTightHandPose,
                    out float score);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                    _relaxedHandPose.CopyFrom(_cachedRelaxedHandPose);
                    _tightHandPose.CopyFrom(_cachedTightHandPose);
                    _usesHandPose = true;
                }
            }

            return bestCandidate;
        }

        #region Inject

        /// <summary>
        /// Adds all required scripts for a <see cref="HandGrabUseInteractor" /> to a dynamically instantiated GameObject.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectAllHandGrabUseInteractor(IFingerUseAPI useApi)
        {
            InjectUseApi(useApi);
        }

        /// <summary>
        /// Adds an <see cref="IFingerUseAPI"/> to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectUseApi(IFingerUseAPI useApi)
        {
            _useAPI = useApi as UnityEngine.Object;
            UseAPI = useApi;
        }

        /// <summary>
        /// Adds an <see cref="IHand"/> to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }


        #endregion
    }
}
