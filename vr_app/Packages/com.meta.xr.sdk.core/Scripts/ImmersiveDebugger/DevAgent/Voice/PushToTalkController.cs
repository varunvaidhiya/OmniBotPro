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
using System.Collections;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Monitors input for push-to-talk voice activation.
    /// When controllers are active, uses <see cref="InputButton"/> (an OVRInput.Button).
    /// When hand tracking is active, uses <see cref="HandGesture"/> (a finger pinch detected
    /// via OVRPlugin.GetHandState) to avoid conflicting with the index pinch used for UI interaction.
    /// </summary>
    internal class PushToTalkController : MonoBehaviour
    {
        private OVRInput.Button _inputButton = OVRInput.Button.One;
        private HandPinchGesture _handGesture = HandPinchGesture.MiddleFingerPinch;

        internal OVRInput.Button InputButton
        {
            get => _inputButton;
            set => _inputButton = value;
        }

        internal HandPinchGesture HandGesture
        {
            get => _handGesture;
            set => _handGesture = value;
        }

        internal event Action OnButtonPressed;
        internal event Action OnButtonReleased;

        private bool _lastButtonState;
        private Coroutine _delayedReleaseCoroutine;
        private OVRPlugin.HandState _handState;

        private void Update()
        {
            bool currentButtonState = IsHandTrackingActive()
                ? GetHandPinchState(_handGesture)
                : OVRInput.Get(_inputButton);

            if (currentButtonState == _lastButtonState) return;

            _lastButtonState = currentButtonState;

            if (currentButtonState)
            {
                if (_delayedReleaseCoroutine != null)
                {
                    // Complete the pending release before starting a new press cycle
                    // to maintain event symmetry (every press gets a matching release)
                    StopCoroutine(_delayedReleaseCoroutine);
                    _delayedReleaseCoroutine = null;
                    OnButtonReleased?.Invoke();
                }

                OnButtonPressed?.Invoke();
            }
            else
            {
                var settings = RuntimeSettings.Instance;
                bool enableDelayedRelease = settings != null ? settings.EnableDelayedRelease : true;

                if (enableDelayedRelease)
                {
                    float releaseDelay = settings != null ? settings.ReleaseDelay : 0.8f;
                    _delayedReleaseCoroutine = StartCoroutine(DelayedReleaseCoroutine(releaseDelay));
                }
                else
                {
                    OnButtonReleased?.Invoke();
                }
            }
        }

        private IEnumerator DelayedReleaseCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnButtonReleased?.Invoke();
            _delayedReleaseCoroutine = null;
        }

        private static bool IsHandTrackingActive()
        {
            var active = OVRInput.GetActiveController();
            return active == OVRInput.Controller.Hands
                || active == OVRInput.Controller.LHand
                || active == OVRInput.Controller.RHand;
        }

        private bool GetHandPinchState(HandPinchGesture gesture)
        {
            var pinchFlag = gesture switch
            {
                HandPinchGesture.MiddleFingerPinch => OVRPlugin.HandFingerPinch.Middle,
                HandPinchGesture.RingFingerPinch => OVRPlugin.HandFingerPinch.Ring,
                HandPinchGesture.PinkyFingerPinch => OVRPlugin.HandFingerPinch.Pinky,
                _ => OVRPlugin.HandFingerPinch.Index,
            };

            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _handState))
            {
                if ((_handState.Pinches & pinchFlag) != 0)
                    return true;
            }

            if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref _handState))
            {
                if ((_handState.Pinches & pinchFlag) != 0)
                    return true;
            }

            return false;
        }
    }
}
