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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This interactable specifies the final pose a hand will have, via <see cref="HandGrabPose"/>s, when using the
    /// interactable, and also the rules to use it. It also provides the relaxed and tight poses to modify the visual
    /// hand depending on the progress of the interaction. By default, it will update the <see cref="UseProgress"/> of
    /// the interaction to the strength of usage, but it is possible to reference a <see cref="IHandGrabUseDelegate"/>
    /// to derive this calculation to a separate script.
    /// </summary>
    public class HandGrabUseInteractable : Interactable<HandGrabUseInteractor, HandGrabUseInteractable>
    {
        /// <summary>
        /// This delegate allows redirecting the Strength to Progress calculations
        /// to a separate script. Implement it in the usable object so it also
        /// receives updates from this interaction automatically.
        /// </summary>
        [SerializeField, Interface(typeof(IHandGrabUseDelegate))]
        [Optional(OptionalAttribute.Flag.DontHide)]
        private UnityEngine.Object _handUseDelegate;
        private IHandGrabUseDelegate HandUseDelegate { get; set; }

        /// <summary>
        /// The rules for using this item. All required fingers must be using in order
        /// to reach maximum progress, when no required fingers are present, the strongest
        /// optional finger can drive the progress value.
        /// </summary>
        [SerializeField]
        private GrabbingRule _useFingers;

        /// <summary>
        /// A <see cref="GrabbingRule"/> specifying the requirements for advancing <see cref="UseProgress"/> when grabbing and using
        /// this interactable. All required fingers must be using in order to reach maximum progress, when no required fingers are
        /// present, the strongest optional finger can drive the progress value.
        /// </summary>
        public GrabbingRule UseFingers
        {
            get
            {
                return _useFingers;
            }
            set
            {
                _useFingers = value;
            }
        }

        /// <summary>
        /// Fingers whose strength value is below this dead zone will not be
        /// considered as snappers.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float _strengthDeadzone = 0.2f;

        /// <summary>
        /// Fingers whose strength value is below this dead zone will not be considered as snappers.
        /// </summary>
        public float StrengthDeadzone
        {
            get
            {
                return _strengthDeadzone;
            }
            set
            {
                _strengthDeadzone = value;
            }

        }

        /// <summary>
        /// <see cref="HandGrabPose"/>s representing the final pose when <see cref="UseProgress"/> is at minimum. If this interactable
        /// is being used to drive a <see cref="Input.SyntheticHand"/> or other visualization, that visualization will override the
        /// tracking data and display whichever of these poses most closely matches the tracked data.
        /// </summary>
        [SerializeField]
        [Optional(OptionalAttribute.Flag.DontHide)]
        private List<HandGrabPose> _relaxedHandGrabPoses = new List<HandGrabPose>();

        /// <summary>
        /// <see cref="HandGrabPose"/>s representing the final pose when <see cref="UseProgress"/> is at maximum. If this interactable
        /// is being used to drive a <see cref="Input.SyntheticHand"/> or other visualization, that visualization will override the
        /// tracking data and display whichever of these poses most closely matches the tracked data.
        /// </summary>
        [SerializeField]
        [Optional(OptionalAttribute.Flag.DontHide)]
        private List<HandGrabPose> _tightHandGrabPoses = new List<HandGrabPose>();

        /// <summary>
        /// Value indicating the progress of the use interaction. This value is in the style of a "strength" as discussed in
        /// <see cref="Input.IHand.GetFingerPinchStrength(Input.HandFinger)"/> and varies from 0 to 1 depending on how weakly or
        /// strongly, respectively, this interactable is being used.
        /// </summary>
        public float UseProgress { get; private set; }

        public List<HandGrabPose> RelaxGrabPoints => _relaxedHandGrabPoses;
        public List<HandGrabPose> TightGrabPoints => _tightHandGrabPoses;

        /// <summary>
        /// This property is a pure alias for <see cref="StrengthDeadzone"/>, included here to support aliased usage in existing code.
        /// </summary>
        public float UseStrengthDeadZone => _strengthDeadzone;

        protected virtual void Reset()
        {
            HandGrabInteractable handGrabInteractable = this.GetComponentInParent<HandGrabInteractable>();
            if (handGrabInteractable != null)
            {
                _relaxedHandGrabPoses = new List<HandGrabPose>(handGrabInteractable.HandGrabPoses);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            HandUseDelegate = _handUseDelegate as IHandGrabUseDelegate;
        }

        protected override void SelectingInteractorAdded(HandGrabUseInteractor interactor)
        {
            base.SelectingInteractorAdded(interactor);
            HandUseDelegate?.BeginUse();
        }

        protected override void SelectingInteractorRemoved(HandGrabUseInteractor interactor)
        {
            base.SelectingInteractorRemoved(interactor);
            HandUseDelegate?.EndUse();

        }

        /// <summary>
        /// Updates <see cref="UseProgress"/>. If an <see cref="IHandGrabUseDelegate"/> has been provided to this instance (either
        /// via the Unity Editor or using <see cref="InjectOptionalForwardUseDelegate(IHandGrabUseDelegate)"/>), that delegate will
        /// be invoked to calculate the new value for <see cref="UseProgress"/>; otherwise, the provided parameter
        /// <paramref name="strength"/> will be set as the new <see cref="UseProgress"/>.
        /// </summary>
        /// <param name="strength">
        /// The value to which <see cref="UseProgress"/> should be set if this instance has not bee provided an
        /// <see cref="IHandGrabUseDelegate"/>
        /// </param>
        /// <returns>The new value of <see cref="UseProgress"/></returns>
        public float ComputeUseStrength(float strength)
        {
            UseProgress = HandUseDelegate != null ? HandUseDelegate.ComputeUseStrength(strength) : strength;
            return UseProgress;
        }

        /// <summary>
        /// Attempts to find the <see cref="HandPose"/>s representing both "tight" (maximum <see cref="UseProgress"/> and "relaxed"
        /// (minimum <see cref="UseProgress"/>) which are most suitable to the current state of the tracked hand. If this interactable
        /// is being used to drive a <see cref="Input.SyntheticHand"/> or other visualization, that visualization can leverage these
        /// poses to produce an interpolated intermediate pose reflective of the current <see cref="UseProgress"/>.
        /// </summary>
        /// <param name="handScale">The scale of the tracked hand</param>
        /// <param name="relaxedHandPose">Output parameter to be populated with the most suitable "relaxed" <see cref="HandPose"/></param>
        /// <param name="tightHandPose">Output parameter to be populated with the most suitable "tight" <see cref="HandPose"/></param>
        /// <param name="score">1 if suitable "relaxed" and "tight" <see cref="HandPose"/>s could both be found, 0 otherwise</param>
        /// <returns>Always returns true</returns>
        public bool FindBestHandPoses(float handScale, ref HandPose relaxedHandPose, ref HandPose tightHandPose, out float score)
        {
            if (FindScaledHandPose(_relaxedHandGrabPoses, handScale, ref relaxedHandPose)
                && FindScaledHandPose(_tightHandGrabPoses, handScale, ref tightHandPose))
            {
                score = 1f;
                return true;
            }

            score = 0f;
            return false;
        }

        private bool FindScaledHandPose(List<HandGrabPose> _handGrabPoses, float handScale, ref HandPose handPose)
        {
            if (_handGrabPoses.Count == 1 && _handGrabPoses[0].HandPose != null)
            {
                handPose.CopyFrom(_handGrabPoses[0].HandPose);
                return true;
            }
            else if (_handGrabPoses.Count > 1)
            {
                float relativeHandScale = handScale / this.transform.lossyScale.x;
                GrabPoseFinder.FindInterpolationRange(relativeHandScale, _handGrabPoses,
                    out HandGrabPose under, out HandGrabPose over, out float t);
                if (under.HandPose != null && over.HandPose != null)
                {
                    HandPose.Lerp(under.HandPose, over.HandPose, t, ref handPose);
                    return true;
                }
                else if (under.HandPose != null)
                {
                    handPose.CopyFrom(under.HandPose);
                    return true;
                }
                else if (over.HandPose != null)
                {
                    handPose.CopyFrom(over.HandPose);
                    return true;
                }

                return false;
            }

            return false;
        }

        #region Inject
        /// <summary>
        /// Adds an <see cref="IHandGrabUseDelegate"/> to a dynamically instantiated HandGrabUseInteractable. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalForwardUseDelegate(IHandGrabUseDelegate useDelegate)
        {
            _handUseDelegate = useDelegate as UnityEngine.Object;
            HandUseDelegate = useDelegate;
        }

        /// <summary>
        /// Sets the list of "relaxed" <see cref="HandGrabPose"/>s for a dynamically instantiated HandGrabUseInteractable. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalRelaxedHandGrabPoints(List<HandGrabPose> relaxedHandGrabPoints)
        {
            _relaxedHandGrabPoses = relaxedHandGrabPoints;
        }

        /// <summary>
        /// Sets the list of "tight" <see cref="HandGrabPose"/>s for a dynamically instantiated HandGrabUseInteractable. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalTightHandGrabPoints(List<HandGrabPose> tightHandGrabPoints)
        {
            _tightHandGrabPoses = tightHandGrabPoints;
        }

        #endregion
    }
}
