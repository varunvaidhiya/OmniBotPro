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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Moves a player when receiving events from <see cref="ILocomotionEventBroadcaster"/>s. The movement can be a combination
    /// of translations and rotations and it happens at the very end of the frame (after rendering).
    /// </summary>
    /// <remarks>
    /// This is a simplistic implementation of <see cref="ILocomotionEventHandler"/> which moves player by actuating the transforms
    /// directly, which is useful for reference but may interfere with more sophisticated player control mechanisms. For
    /// alternatives specialized to specific player controls, see <see cref="CapsuleLocomotionHandler"/>,
    /// <see cref="FirstPersonLocomotor"/>, and <see cref="FlyingLocomotor"/>.
    /// </remarks>
    public class PlayerLocomotor : MonoBehaviour, ILocomotionEventHandler
    {
        [SerializeField]
        private Transform _playerOrigin;
        [SerializeField]
        private Transform _playerHead;

        private Action<LocomotionEvent, Pose> _whenLocomotionEventHandled = delegate { };
        /// <summary>
        /// Signal indicating a <see cref="LocomotionEvent"/> has been handled, including the event itself and the new player
        /// pose resultant from the handling of that event.
        /// </summary>
        /// <remarks>
        /// Because this signal is on a per-event basis, this can be invoked multiple times in the same frame.
        /// </remarks>
        public event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled
        {
            add
            {
                _whenLocomotionEventHandled += value;
            }
            remove
            {
                _whenLocomotionEventHandled -= value;
            }
        }

        protected bool _started;

        private Queue<LocomotionEvent> _deferredEvent = new Queue<LocomotionEvent>();

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_playerOrigin, nameof(_playerOrigin));
            this.AssertField(_playerHead, nameof(_playerHead));

            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            if (_started)
            {
                this.RegisterEndOfFrameCallback(MovePlayer);
            }
        }

        private void OnDisable()
        {
            if (_started)
            {
                _deferredEvent.Clear();
                this.UnregisterEndOfFrameCallback();
            }
        }

        /// <summary>
        /// Consumes a <see cref="LocomotionEvent"/>, canonically directly from an <see cref="ILocomotionEventBroadcaster"/>.
        /// This event is not acted upon immediately, but is cached for consumption at the end of the frame when PlayerLocomotor
        /// processes all incoming events at once.
        /// </summary>
        /// <param name="locomotionEvent">The event to be handled</param>
        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            _deferredEvent.Enqueue(locomotionEvent);
        }

        private void MovePlayer()
        {
            while (_deferredEvent.Count > 0)
            {
                LocomotionEvent locomotionEvent = _deferredEvent.Dequeue();
                Pose originalPose = _playerOrigin.GetPose();
                MovePlayer(locomotionEvent.Pose.position, locomotionEvent.Translation);
                RotatePlayer(locomotionEvent.Pose.rotation, locomotionEvent.Rotation);
                Pose delta = PoseUtils.Delta(originalPose, _playerOrigin.GetPose());
                _whenLocomotionEventHandled.Invoke(locomotionEvent, delta);
            }
        }

        private void MovePlayer(Vector3 targetPosition, LocomotionEvent.TranslationType translationMode)
        {
            if (translationMode == LocomotionEvent.TranslationType.None)
            {
                return;
            }
            if (translationMode == LocomotionEvent.TranslationType.Absolute)
            {
                Vector3 positionOffset = _playerOrigin.position - _playerHead.position;
                positionOffset.y = 0f;
                _playerOrigin.position = targetPosition + positionOffset;
            }
            else if (translationMode == LocomotionEvent.TranslationType.AbsoluteEyeLevel)
            {
                Vector3 positionOffset = _playerOrigin.position - _playerHead.position;
                _playerOrigin.position = targetPosition + positionOffset;
            }
            else if (translationMode == LocomotionEvent.TranslationType.Relative)
            {
                _playerOrigin.position = _playerOrigin.position + targetPosition;
            }
            else if (translationMode == LocomotionEvent.TranslationType.Velocity)
            {
                _playerOrigin.position = _playerOrigin.position + targetPosition * Time.deltaTime;
            }
        }

        private void RotatePlayer(Quaternion targetRotation, LocomotionEvent.RotationType rotationMode)
        {
            if (rotationMode == LocomotionEvent.RotationType.None)
            {
                return;
            }
            Vector3 originalHeadPosition = _playerHead.position;
            if (rotationMode == LocomotionEvent.RotationType.Absolute)
            {
                Vector3 headForward = Vector3.ProjectOnPlane(_playerHead.forward, _playerOrigin.up).normalized;
                Quaternion headFlatRotation = Quaternion.LookRotation(headForward, _playerOrigin.up);
                Quaternion rotationOffset = Quaternion.Inverse(_playerOrigin.rotation) * headFlatRotation;
                _playerOrigin.rotation = Quaternion.Inverse(rotationOffset) * targetRotation;
            }
            else if (rotationMode == LocomotionEvent.RotationType.Relative)
            {
                _playerOrigin.rotation = targetRotation * _playerOrigin.rotation;
            }
            else if (rotationMode == LocomotionEvent.RotationType.Velocity)
            {
                targetRotation.ToAngleAxis(out float angle, out Vector3 axis);
                angle *= Time.deltaTime;

                _playerOrigin.rotation = Quaternion.AngleAxis(angle, axis) * _playerOrigin.rotation;
            }
            _playerOrigin.position = _playerOrigin.position + originalHeadPosition - _playerHead.position;
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated PlayerLocomotor; effectively wraps
        /// <see cref="InjectPlayerOrigin(Transform)"/> and <see cref="InjectPlayerHead(Transform)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllPlayerLocomotor(Transform playerOrigin, Transform playerHead)
        {
            InjectPlayerOrigin(playerOrigin);
            InjectPlayerHead(playerHead);
        }

        /// <summary>
        /// Sets the player's origin transform for a dynamically instantiated PlayerLocomotor. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectPlayerOrigin(Transform playerOrigin)
        {
            _playerOrigin = playerOrigin;
        }

        /// <summary>
        /// Sets the player's head transform for a dynamically instantiated PlayerLocomotor. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectPlayerHead(Transform playerHead)
        {
            _playerHead = playerHead;
        }

        #endregion
    }
}
