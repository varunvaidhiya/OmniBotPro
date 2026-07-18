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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This component will emit LocomotionEvent.Translation.Velocity every update
    /// in the direction and magnitude specified by the provided Axis and Aiming transform.
    /// Generally the aiming transform forward will be flattened in the Y plane, but when
    /// the angle is too extreme (near +-90, specified by the _dotSafeDirectionThreshold) it
    /// will resort to .up or .back to ensure the direction is stable.
    /// </summary>
    public class SlideLocomotionBroadcaster : MonoBehaviour
        , ILocomotionEventBroadcaster
    {
        /// <summary>
        /// The Axis that will provide the relative direction to the aiming transform
        /// and its magnitude
        /// </summary>
        [SerializeField, Interface(typeof(IAxis2D))]
        private UnityEngine.Object _axis2D;
        private IAxis2D Axis2D;

        /// <summary>
        /// The transform to use as a reference for the movement. Typically set to the
        /// hand or the head of the player
        /// </summary>
        [SerializeField, Optional]
        private Transform _aiming;
        public Transform Aiming
        {
            get => _aiming;
            set => _aiming = value;
        }

        /// <summary>
        /// Deadzone applied to the vertical axis
        /// </summary>
        [SerializeField, Optional]
        private AnimationCurve _verticalDeadZone = AnimationCurve.Linear(-1f, -1f, 1f, 1f);
        public AnimationCurve VerticalDeadZone
        {
            get => _verticalDeadZone;
            set => _verticalDeadZone = value;
        }

        /// <summary>
        /// Deadzone applied to the horizontal axis
        /// </summary>
        [SerializeField, Optional]
        private AnimationCurve _horizontalDeadZone = AnimationCurve.Linear(-1f, -1f, 1f, 1f);
        public AnimationCurve HorizontalDeadZone
        {
            get => _horizontalDeadZone;
            set => _horizontalDeadZone = value;
        }

        private Action<LocomotionEvent> _whenLocomotionPerformed = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add
            {
                _whenLocomotionPerformed += value;
            }
            remove
            {
                _whenLocomotionPerformed -= value;
            }
        }

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        protected bool _started = false;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
            Axis2D = _axis2D as IAxis2D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Axis2D, nameof(_axis2D));
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            Vector2 axis = ProcessAxisSensitivity();
            Pose step = StepDirection(new Vector3(axis.x, 0f, axis.y));

            if (!Mathf.Approximately(step.position.sqrMagnitude, 0f))
            {
                var locomotionEvent = new LocomotionEvent(this.Identifier, step,
                    LocomotionEvent.TranslationType.Velocity,
                    LocomotionEvent.RotationType.None);
                _whenLocomotionPerformed.Invoke(locomotionEvent);
            }
        }

        private Vector2 ProcessAxisSensitivity()
        {
            Vector2 value = Axis2D.Value();
            if (_horizontalDeadZone != null)
            {
                value.x = _horizontalDeadZone.Evaluate(value.x);
            }
            if (_verticalDeadZone != null)
            {
                value.y = _verticalDeadZone.Evaluate(value.y);
            }
            return value;
        }

        private Pose StepDirection(Vector3 axisValue)
        {
            if (_aiming == null)
            {
                return new Pose(axisValue, Quaternion.identity);
            }

            Vector3 step = _aiming.right * axisValue.x
                + _aiming.up * axisValue.y
                + _aiming.forward * axisValue.z;

            return new Pose(step, _aiming.rotation);
        }

        #region Inject

        public void InjectAllSlideLocomotionBroadcaster(IAxis2D axis2D)
        {
            InjectAxis2D(axis2D);
        }

        public void InjectAxis2D(IAxis2D axis2D)
        {
            _axis2D = axis2D as UnityEngine.Object;
            Axis2D = axis2D;
        }

        #endregion
    }
}
