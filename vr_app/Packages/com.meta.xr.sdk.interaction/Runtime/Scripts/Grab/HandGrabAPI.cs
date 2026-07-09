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

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// The <see cref="HandGrabAPI"/> class provides functionality to detect finger grabs using multiple <see cref="IFingerAPI"/> implementations.
    /// It distinguishes between pinch and palm grabs and allows customization of the grab detectors through dependency injection, using the <see cref="InjectOptionalFingerPinchAPI"/> and <see cref="InjectOptionalFingerGrabAPI"/> methods.
    /// </summary>
    public class HandGrabAPI : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// Gets the <see cref="IHand"/> implementation associated with this instance.
        /// This property is crucial for accessing hand-specific data required for grab detection.
        /// </summary>
        public IHand Hand { get; private set; }

        [SerializeField, Interface(typeof(IHmd)), Optional]
        private UnityEngine.Object _hmd;

        public IHmd Hmd { get; private set; } = null;

        private IFingerAPI _fingerPinchGrabAPI = null;
        private IFingerAPI _fingerPalmGrabAPI = null;

        private bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
            if (Hmd == null)
            {
                Hmd = _hmd as IHmd;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            if (_fingerPinchGrabAPI == null)
            {
                _fingerPinchGrabAPI = new PinchGrabAPI(Hmd);
            }
            if (_fingerPalmGrabAPI == null)
            {
                _fingerPalmGrabAPI = new PalmGrabAPI();
            }
            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += OnHandUpdated;
            }
        }

        private void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= OnHandUpdated;
            }
        }

        private void OnHandUpdated()
        {
            _fingerPinchGrabAPI.Update(Hand);
            _fingerPalmGrabAPI.Update(Hand);
        }

        /// <summary>
        /// Returns the flags indicating which fingers are currently performing a pinch grab.
        /// </summary>
        /// <returns>A <see cref="HandFingerFlags"/> value representing the fingers involved in pinch grabs.</returns>
        public HandFingerFlags HandPinchGrabbingFingers()
        {
            return HandGrabbingFingers(_fingerPinchGrabAPI);
        }

        /// <summary>
        /// Returns the flags indicating which fingers are currently performing a palm grab.
        /// </summary>
        /// <returns>A <see cref="HandFingerFlags"/> value representing the fingers involved in palm grabs.</returns>
        public HandFingerFlags HandPalmGrabbingFingers()
        {
            return HandGrabbingFingers(_fingerPalmGrabAPI);
        }

        private HandFingerFlags HandGrabbingFingers(IFingerAPI fingerAPI)
        {
            HandFingerFlags grabbingFingers = HandFingerFlags.None;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;

                bool isGrabbing = fingerAPI.GetFingerIsGrabbing(finger);
                if (isGrabbing)
                {
                    grabbingFingers |= (HandFingerFlags)(1 << i);
                }
            }

            return grabbingFingers;
        }

        /// <summary>
        /// Determines if the hand is currently performing a pinch grab according to the specified rules.
        /// </summary>
        /// <param name="fingers">The <see cref="GrabbingRule"/> defining required and optional fingers for the grab.</param>
        /// <returns>True if the hand meets the pinch grab conditions; otherwise, false.</returns>
        public bool IsHandPinchGrabbing(in GrabbingRule fingers)
        {
            HandFingerFlags pinchFingers = HandPinchGrabbingFingers();
            return IsSustainingGrab(fingers, pinchFingers);
        }

        /// <summary>
        /// Determines if the hand is currently performing a palm grab according to the specified rules.
        /// </summary>
        /// <param name="fingers">The <see cref="GrabbingRule"/> defining required and optional fingers for the grab.</param>
        /// <returns>True if the hand meets the palm grab conditions; otherwise, false.</returns>
        public bool IsHandPalmGrabbing(in GrabbingRule fingers)
        {
            HandFingerFlags palmFingers = HandPalmGrabbingFingers();
            return IsSustainingGrab(fingers, palmFingers);
        }

        /// <summary>
        /// Determines if the grab condition is sustained based on the specified grabbing rules and current grabbing fingers.
        /// </summary>
        /// <param name="fingers">The <see cref="GrabbingRule"/> specifying required and optional fingers.</param>
        /// <param name="grabbingFingers">The current state of fingers that are grabbing.</param>
        /// <returns>True if the grab condition is sustained; otherwise, false.</returns>
        public bool IsSustainingGrab(in GrabbingRule fingers, HandFingerFlags grabbingFingers)
        {
            bool anyHolding = false;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                HandFingerFlags fingerFlag = (HandFingerFlags)(1 << i);

                bool isFingerGrabbing = (grabbingFingers & fingerFlag) != 0;
                if (fingers[finger] == FingerRequirement.Required)
                {
                    anyHolding |= isFingerGrabbing;
                    if (fingers.UnselectMode == FingerUnselectMode.AnyReleased
                        && !isFingerGrabbing)
                    {
                        return false;
                    }

                    if (fingers.UnselectMode == FingerUnselectMode.AllReleased
                        && isFingerGrabbing)
                    {
                        return true;
                    }
                }
                else if (fingers[finger] == FingerRequirement.Optional)
                {
                    anyHolding |= isFingerGrabbing;
                }
            }

            return anyHolding;
        }

        /// <summary>
        /// Determine whether the state of any of the finger pinches have changed this frame to
        /// the target pinching state (on/off).
        /// </summary>
        /// <param name="fingers">Finger <see cref="GrabbingRule"/> to check.
        /// </param>
        /// <returns>True if any finger's pinch state has changed according to the rules; otherwise, false.
        /// </returns>
        public bool IsHandSelectPinchFingersChanged(in GrabbingRule fingers)
        {
            return IsHandSelectFingersChanged(fingers, _fingerPinchGrabAPI);
        }

        /// <summary>
        /// Determines whether the state of any of the finger grabs have changed this frame to
        /// the target grabbing state (on/off).
        /// </summary>
        /// <param name="fingers">Finger <see cref="GrabbingRule"/> to check.</param>
        /// <returns>True if any finger's grab state has changed according to the rules; otherwise, false.</returns>
        public bool IsHandSelectPalmFingersChanged(in GrabbingRule fingers)
        {
            return IsHandSelectFingersChanged(fingers, _fingerPalmGrabAPI);
        }

        /// <summary>
        /// Determines whether the state of any of the finger pinches have changed this frame to
        /// the target pinching state (on/off).
        /// </summary>
        /// <param name="fingers">Finger <see cref="GrabbingRule"/> to check.</param>
        /// <returns>True if any finger's pinch state has changed according to the rules; otherwise, false.</returns>
        public bool IsHandUnselectPinchFingersChanged(in GrabbingRule fingers)
        {
            return IsHandUnselectFingersChanged(fingers, _fingerPinchGrabAPI);
        }

        /// <summary>
        /// Determines whether the state of any of the finger grabs have changed this frame to
        /// the target grabbing state (on/off).
        /// </summary>
        /// <param name="fingers">Finger <see cref="GrabbingRule"/> to check.</param>
        /// <returns>True if any finger's grab state has changed according to the rules; otherwise, false.</returns>
        public bool IsHandUnselectPalmFingersChanged(in GrabbingRule fingers)
        {
            return IsHandUnselectFingersChanged(fingers, _fingerPalmGrabAPI);
        }

        private bool IsHandSelectFingersChanged(in GrabbingRule fingers, IFingerAPI fingerAPI)
        {
            bool selectsWithOptionals = fingers.SelectsWithOptionals;
            bool anyFingerBeganGrabbing = false;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (fingers[finger] == FingerRequirement.Required)
                {
                    if (!fingerAPI.GetFingerIsGrabbing(finger))
                    {
                        return false;
                    }

                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, true))
                    {
                        anyFingerBeganGrabbing = true;
                    }
                }
                else if (selectsWithOptionals
                    && fingers[finger] == FingerRequirement.Optional)
                {
                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, true))
                    {
                        return true;
                    }
                }
            }

            return anyFingerBeganGrabbing;
        }

        private bool IsHandUnselectFingersChanged(in GrabbingRule fingers, IFingerAPI fingerAPI)
        {
            bool isAnyFingerGrabbing = false;
            bool anyFingerStoppedGrabbing = false;
            bool selectsWithOptionals = fingers.SelectsWithOptionals;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (fingers[finger] == FingerRequirement.Ignored)
                {
                    continue;
                }

                isAnyFingerGrabbing |= fingerAPI.GetFingerIsGrabbing(finger);
                if (fingers[finger] == FingerRequirement.Required)
                {
                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, false))
                    {
                        anyFingerStoppedGrabbing = true;
                        if (fingers.UnselectMode == FingerUnselectMode.AnyReleased)
                        {
                            return true;
                        }
                    }
                }
                else if (fingers[finger] == FingerRequirement.Optional)
                {
                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, false))
                    {
                        anyFingerStoppedGrabbing = true;
                        if (fingers.UnselectMode == FingerUnselectMode.AnyReleased
                            && selectsWithOptionals)
                        {
                            return true;
                        }
                    }
                }
            }

            return !isAnyFingerGrabbing && anyFingerStoppedGrabbing;
        }

        /// <summary>
        /// Calculates the center position of the pinch grab based on the wrist offset.
        /// </summary>
        /// <returns>The world position of the pinch center.</returns>
        public Vector3 GetPinchCenter()
        {
            Vector3 localOffset = Vector3.zero;
            if (_fingerPinchGrabAPI != null)
            {
                localOffset = _fingerPinchGrabAPI.GetWristOffsetLocal();
            }
            return WristOffsetToWorldPoint(localOffset);
        }

        /// <summary>
        /// Calculates the center position of the palm grab based on the wrist offset.
        /// </summary>
        /// <returns>The world position of the palm center.</returns>
        public Vector3 GetPalmCenter()
        {
            Vector3 localOffset = Vector3.zero;
            if (_fingerPalmGrabAPI != null)
            {
                localOffset = _fingerPalmGrabAPI.GetWristOffsetLocal();
            }
            return WristOffsetToWorldPoint(localOffset);
        }

        private Vector3 WristOffsetToWorldPoint(Vector3 localOffset)
        {
            if (!Hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose))
            {
                return localOffset * Hand.Scale;
            }

            return wristPose.position + wristPose.rotation * localOffset * Hand.Scale;
        }

        /// <summary>
        /// Retrieves the overall score of how well the hand is performing a pinch grab based on specified rules.
        /// </summary>
        /// <param name="fingers">The rules defining required and optional fingers for the pinch grab.</param>
        /// <param name="includePinching">Indicates whether to include currently pinching fingers in the score calculation.</param>
        /// <returns>A float representing the pinch grab score, where higher values indicate a stronger grab.</returns>
        public float GetHandPinchScore(in GrabbingRule fingers,
            bool includePinching = true)
        {
            return GetHandGrabScore(fingers, includePinching, _fingerPinchGrabAPI);
        }

        /// <summary>
        /// Retrieves the overall score of how well the hand is performing a palm grab based on specified rules.
        /// </summary>
        /// <param name="fingers">The <see cref="GrabbingRule"/> defining required and optional fingers for the palm grab.</param>
        /// <param name="includeGrabbing">Indicates whether to include currently grabbing fingers in the score calculation.</param>
        /// <returns>A float representing the palm grab score, where higher values indicate a stronger grab.</returns>
        public float GetHandPalmScore(in GrabbingRule fingers,
            bool includeGrabbing = true)
        {
            return GetHandGrabScore(fingers, includeGrabbing, _fingerPalmGrabAPI);
        }

        /// <summary>
        /// Retrieves the strength of the pinch grab for a specific finger.
        /// </summary>
        /// <param name="finger">The <see cref="HandFinger"/> to check the pinch strength for.</param>
        /// <returns>A float representing the pinch strength, where higher values indicate a stronger pinch.</returns>
        public float GetFingerPinchStrength(HandFinger finger)
        {
            return _fingerPinchGrabAPI.GetFingerGrabScore(finger);
        }

        /// <summary>
        /// Retrieves the strength of the palm grab for a specific finger.
        /// </summary>
        /// <param name="finger">The <see cref="HandFinger"/> to check the palm grab strength for.</param>
        /// <returns>A float representing the palm grab strength, where higher values indicate a stronger grab.</returns>
        public float GetFingerPalmStrength(HandFinger finger)
        {
            return _fingerPalmGrabAPI.GetFingerGrabScore(finger);
        }

        private float GetHandGrabScore(in GrabbingRule fingers,
            bool includeGrabbing, IFingerAPI fingerAPI)
        {
            float requiredMin = 1.0f;
            float optionalMax = 0f;
            bool anyRequired = false;
            bool usesOptionals = fingers.SelectsWithOptionals;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (!includeGrabbing && fingerAPI.GetFingerIsGrabbing((HandFinger)i))
                {
                    continue;
                }

                if (fingers[finger] == FingerRequirement.Ignored)
                {
                    continue;
                }

                if (fingers[finger] == FingerRequirement.Optional)
                {
                    optionalMax = Mathf.Max(optionalMax, fingerAPI.GetFingerGrabScore(finger));
                }
                else if (fingers[finger] == FingerRequirement.Required)
                {
                    anyRequired = true;
                    requiredMin = Mathf.Min(requiredMin, fingerAPI.GetFingerGrabScore(finger));
                }
            }

            return usesOptionals ? optionalMax : anyRequired ? requiredMin : 0f;
        }

        /// <summary>
        /// Checks if a specific finger is currently grabbing.
        /// </summary>
        /// <param name="finger">The finger to check for grabbing status.</param>
        /// <returns>True if the specified finger is grabbing; otherwise, false.</returns>
        public bool GetFingerIsGrabbing(HandFinger finger)
        {
            return _fingerPinchGrabAPI.GetFingerIsGrabbing(finger);
        }

        public bool GetFingerIsPalmGrabbing(HandFinger finger)
        {
            return _fingerPalmGrabAPI.GetFingerIsGrabbing(finger);
        }

        #region Inject

        /// <summary>
        /// Injects custom <see cref="IHand"/> implementations for the <see cref="HandGrabAPI"/>. This method facilitates unit testing.
        /// </summary>
        /// <param name="hand">The custom <see cref="IHand"/> implementation to inject.</param>
        public void InjectAllHandGrabAPI(IHand hand)
        {
            InjectHand(hand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectOptionalHmd(IHmd hmd)
        {
            Hmd = hmd;
            _hmd = hmd as UnityEngine.Object;
        }

        /// <summary>
        /// Injects an optional custom implementation for the pinch grab API.
        /// </summary>
        /// <param name="fingerPinchAPI">The custom pinch grab <see cref="IFingerAPI"/> to inject.</param>
        public void InjectOptionalFingerPinchAPI(IFingerAPI fingerPinchAPI)
        {
            _fingerPinchGrabAPI = fingerPinchAPI;
        }

        /// <summary>
        /// Injects an optional custom implementation for the palm grab API.
        /// </summary>
        /// <param name="fingerGrabAPI">The custom palm grab <see cref="IFingerAPI"/> to inject.</param>
        public void InjectOptionalFingerGrabAPI(IFingerAPI fingerGrabAPI)
        {
            _fingerPalmGrabAPI = fingerGrabAPI;
        }
        #endregion
    }
}
