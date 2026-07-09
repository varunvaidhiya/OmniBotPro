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
    /// Locomotion events broadcaster that can fire snap
    /// and smooth turning locomotion events on demand.
    /// </summary>
    public class TurnLocomotionBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {
        /// <summary>
        /// When in Snap turn mode, the amount of degrees to instantly turn. This ignores the strength of the axis and cares only about the direction.
        /// </summary>
        [SerializeField]
        [Tooltip("Degrees to instantly turn when in Snap turn mode. Note the direction is provided by the axis")]
        private float _snapTurnDegrees = 45f;
        /// <summary>
        /// Degrees to instantly turn when in Snap turn mode. Note the direction is provided by the axis
        /// </summary>
        public float SnapTurnDegrees
        {
            get
            {
                return _snapTurnDegrees;
            }
            set
            {
                _snapTurnDegrees = value;
            }
        }

        [SerializeField]
        [Tooltip("Degrees to continuously rotate during selection when in Smooth turn mode, it is remapped from the Axis value")]
        private AnimationCurve _smoothTurnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 100f);
        /// <summary>
        /// Degrees to continuously rotate during selection when in Smooth turn mode, it is remapped from the Axis value
        /// </summary>
        public AnimationCurve SmoothTurnCurve
        {
            get
            {
                return _smoothTurnCurve;
            }
            set
            {
                _smoothTurnCurve = value;
            }
        }

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        public event Action<LocomotionEvent> WhenLocomotionPerformed = delegate { };

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
        }

        /// <summary>
        /// Fires a Snap Turn in anti-clockwise direction
        /// </summary>
        public void SnapTurnLeft()
        {
            SnapTurn(-1f);
        }

        /// <summary>
        /// Fires a Snap Turn in clockwise direction
        /// </summary>
        public void SnapTurnRight()
        {
            SnapTurn(1f);
        }

        /// <summary>
        /// Fires a Relative Rotation event using the given direction.
        /// It ignores the magnitude of the direction and uses the _snapTurnDegrees
        /// </summary>
        /// <param name="direction">The direction of the turn, 1 for clockwise, -1 anti-clockwise</param>
        public void SnapTurn(float direction)
        {
            float sign = Mathf.Sign(direction);
            Quaternion rot = Quaternion.Euler(0f, sign * _snapTurnDegrees, 0f);

            LocomotionEvent locomotionEvent = new LocomotionEvent(
                Identifier, rot, LocomotionEvent.RotationType.Relative);
            WhenLocomotionPerformed.Invoke(locomotionEvent);
        }

        /// <summary>
        /// Fires a Velocity Rotation event using the given direction.
        /// It uses the magnitude of the direction and adjusts it using the _smoothTurnCurve
        /// </summary>
        /// <param name="direction">The direction and magnitude of the turn, 1 for clockwise, -1 anti-clockwise</param>
        public void SmoothTurn(float direction)
        {
            float sign = Mathf.Sign(direction);
            float vel = _smoothTurnCurve.Evaluate(Mathf.Abs(direction));
            Quaternion rot = Quaternion.Euler(0f, sign * vel, 0f);
            LocomotionEvent locomotionEvent = new LocomotionEvent(
                Identifier, rot, LocomotionEvent.RotationType.Velocity);
            WhenLocomotionPerformed.Invoke(locomotionEvent);
        }
    }
}
