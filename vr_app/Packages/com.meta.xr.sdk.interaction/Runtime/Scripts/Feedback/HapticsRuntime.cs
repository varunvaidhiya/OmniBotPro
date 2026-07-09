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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnityXRPlayer"/> class using a generic <see cref="MonoBehaviour"/>
    /// as the host for coroutines and requiring manual calls to the <see cref="FeedbackManager"/>
    /// to resolve interactor IDs to controllers if needed by a the <see cref="GetDevice"/> method.
    /// This constructor is less common if <see cref="FeedbackManager"/> provides the necessary controller lookup.
    /// </summary>
    internal sealed class UnityXRPlayer : IHapticsPlayer
    {
        private readonly FeedbackManager _manager;
        private readonly MonoBehaviour _coroutineHost;
        private readonly Dictionary<int, Coroutine> _playing = new();
        private readonly Dictionary<int, InputDevice> _cache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityXRPlayer"/> class using a <see cref="FeedbackManager"/>
        /// as both the coroutine host and the provider for controller information via <see cref="FeedbackManager.TryGetController"/>.
        /// </summary>
        /// <param name="manager">The <see cref="FeedbackManager"/> instance which serves as the coroutine host
        /// and provides controller lookup functionality.</param>
        public UnityXRPlayer(FeedbackManager manager)
        {
            _manager = manager;
            _coroutineHost = manager;
        }

        /// <summary>
        /// Plays a haptic pattern on the controller associated with the given interactor or pointer ID.
        /// If a haptic effect is already playing for the given ID, it is stopped first.
        /// </summary>
        /// <param name="interactorId">The identifier for the haptic source (e.g., interactor ID or pointer ID).</param>
        /// <param name="pattern">The <see cref="HapticPattern"/> (amplitude and duration) to play.</param>
        public void Play(int interactorId, in HapticPattern pattern)
        {
            Stop(interactorId);
            var device = GetDevice(interactorId);
            if (!device.isValid)
            {
                return;
            }

            _playing[interactorId] =
                _coroutineHost.StartCoroutine(PlayHapticCoroutine(interactorId, pattern, device));
        }

        /// <summary>
        /// Stops any active haptic effect on the controller associated with the given interactor or pointer ID.
        /// Also stops the haptic impulse on the <see cref="InputDevice"/> itself.
        /// </summary>
        /// <param name="interactorId">The identifier for the haptic source whose effect should be stopped.</param>
        public void Stop(int interactorId)
        {
            if (_playing.TryGetValue(interactorId, out var c))
            {
                _coroutineHost.StopCoroutine(c);
            }

            _playing.Remove(interactorId);

            if (_cache.TryGetValue(interactorId, out var d) && d.isValid)
            {
                d.StopHaptics();
            }
        }

        /// <summary>
        /// Coroutine that plays a haptic impulse for a specified duration.
        /// </summary>
        IEnumerator PlayHapticCoroutine(int id, HapticPattern pattern, InputDevice device)
        {
            device.SendHapticImpulse(0, pattern._amplitude, pattern._duration);
            yield return new WaitForSeconds(pattern._duration);
            _playing.Remove(id);
        }

        /// <summary>
        /// Retrieves the <see cref="InputDevice"/> associated with the given Interactor or pointer ID.
        /// Uses a cache for efficiency. Relies on <see cref="_manager"/> to map IDs to <see cref="IController"/> instances.
        /// Returns an invalid device if no controller is found.
        /// </summary>
        InputDevice GetDevice(int id)
        {
            if (_cache.TryGetValue(id, out var device) && device.isValid)
            {
                return device;
            }

            if (_manager.TryGetController(id, out var controller) && controller != null)
            {
                XRNode node = XRNode.LeftHand;

                node = controller.Handedness == Handedness.Left
                    ? XRNode.LeftHand
                    : XRNode.RightHand;

                device = InputDevices.GetDeviceAtXRNode(node);
                if (device.isValid)
                {
                    _cache[id] = device;
                }

                return device;
            }

            return default;
        }
    }

    //TODO: Implement Meta XR Haptics SDK
}
