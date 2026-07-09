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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Produces a Relative Translation Locomotion event in the XZ direction
    /// relative to an aiming transform. This aiming transform will typically
    /// represent the head or the hand of the player.
    /// </summary>
    public class StepLocomotionBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {
        /// <summary>
        /// The aiming transform to use to generate the final step direction.
        /// Typically this can be set to the Head or the Hand of the player.
        /// </summary>
        [SerializeField]
        private Transform _origin;
        public Transform Origin
        {
            get => _origin;
            set => _origin = value;
        }
        /// <summary>
        /// The length of the relative movement that will be emitted
        /// </summary>
        [SerializeField]
        private float _stepLength = 0.5f;
        public float StepLength
        {
            get => _stepLength;
            set => _stepLength = value;
        }

        protected bool _started = false;

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        public event Action<LocomotionEvent> WhenLocomotionPerformed = delegate { };

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_origin, nameof(_origin));
            this.EndStart(ref _started);
        }

        /// <summary>
        /// Broadcasts a relative translation locomotion event to the
        /// left of the aiming transform.
        /// </summary>
        public void StepLeft()
        {
            Step(Vector2Int.left);
        }

        /// <summary>
        /// Broadcasts a relative translation locomotion event to the
        /// right of the aiming transform.
        /// </summary>
        public void StepRight()
        {
            Step(Vector2Int.right);
        }

        /// <summary>
        /// Broadcasts a relative translation locomotion event to the
        /// front of the aiming transform.
        /// </summary>
        public void StepForward()
        {
            Step(Vector2Int.up);
        }

        /// <summary>
        /// Broadcasts a relative translation locomotion event to the
        /// back of the aiming transform.
        /// </summary>
        public void StepBackward()
        {
            Step(Vector2Int.down);
        }

        /// <summary>
        /// Generates a relative translation locomotion event in the
        /// specified direction relative to the aiming transform.
        /// </summary>
        /// <param name="relativeDirection">The XZ direction, relative to the aiming
        /// transform, for the step</param>
        public void Step(Vector2Int relativeDirection)
        {
            Vector3 forward = _origin.forward;
            Vector3 up = Vector3.up;

            forward = Vector3.ProjectOnPlane(forward, up).normalized;

            Quaternion rotation = Quaternion.LookRotation(forward, up);
            Vector3 step = (rotation * new Vector3(relativeDirection.x, 0f, relativeDirection.y)).normalized * _stepLength;

            LocomotionEvent locomotionEvent = new LocomotionEvent(
                Identifier, step, LocomotionEvent.TranslationType.Relative);

            WhenLocomotionPerformed.Invoke(locomotionEvent);
        }
    }
}
