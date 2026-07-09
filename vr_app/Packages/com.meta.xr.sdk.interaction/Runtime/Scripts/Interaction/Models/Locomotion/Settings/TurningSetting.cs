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

using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Manages turning settings and applies them to turners at runtime.
    /// </summary>
    public class TurningSetting : MonoBehaviour
    {
        public enum RotationStyle
        {
            Snap = 0,
            Smooth = 1,
        }

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Snap or Smooth rotation style for controller turning.")]
        private ReactiveValue<RotationStyle> _controllerTurn;
        /// <summary>Snap or Smooth rotation style for controller turning.</summary>
        public ReactiveValue<RotationStyle> ControllerTurn => _controllerTurn;

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Rotation angle in degrees for snap turns.")]
        private ReactiveValue<float> _rotationSnapAngle;
        /// <summary>Rotation angle in degrees for snap turns.</summary>
        public ReactiveValue<float> RotationSnapAngle => _rotationSnapAngle;

        [SerializeField]
        [LocomotionSetting]
        [Tooltip("Maximum rotation velocity in degrees per second for smooth turns.")]
        private ReactiveValue<float> _rotationSmoothVelocity;
        /// <summary>Maximum rotation velocity in degrees per second for smooth turns.</summary>
        public ReactiveValue<float> RotationSmoothVelocity => _rotationSmoothVelocity;

        [Space]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("Turners activated by controller input.")]
        private TurnerEventBroadcaster[] _controllerTurners;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("Turners activated by hand tracking.")]
        private TurnerEventBroadcaster[] _handTurners;
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("General Locomotion turners.")]
        private TurnLocomotionBroadcaster[] _locomotionTurners;

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);
            if (_controllerTurners != null)
            {
                this.AssertCollectionItems(_controllerTurners, nameof(_controllerTurners));
            }
            if (_handTurners != null)
            {
                this.AssertCollectionItems(_handTurners, nameof(_handTurners));
            }
            if (_locomotionTurners != null)
            {
                this.AssertCollectionItems(_locomotionTurners, nameof(_locomotionTurners));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                ControllerTurn.WhenChanged += HandleTurnStyleChanged;
                RotationSnapAngle.WhenChanged += HandleSnapAngleChanged;
                RotationSmoothVelocity.WhenChanged += HandleRotationSmoothVelocityChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                ControllerTurn.WhenChanged -= HandleTurnStyleChanged;
                RotationSnapAngle.WhenChanged -= HandleSnapAngleChanged;
                RotationSmoothVelocity.WhenChanged -= HandleRotationSmoothVelocityChanged;
            }
        }

        private void HandleSnapAngleChanged(float snapDegrees)
        {
            if (_controllerTurners != null)
            {
                foreach (var turn in _controllerTurners)
                {
                    turn.SnapTurnDegrees = snapDegrees;
                }
            }
            if (_handTurners != null)
            {
                foreach (var turn in _handTurners)
                {
                    turn.SnapTurnDegrees = snapDegrees;
                }
            }
            if (_locomotionTurners != null)
            {
                foreach (var turn in _locomotionTurners)
                {
                    turn.SnapTurnDegrees = snapDegrees;
                }
            }
        }

        private void HandleRotationSmoothVelocityChanged(float smoothVelocity)
        {
            AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, smoothVelocity);
            if (_controllerTurners != null)
            {
                foreach (var turn in _controllerTurners)
                {
                    turn.SmoothTurnCurve = curve;
                }
            }
            if (_handTurners != null)
            {
                foreach (var turn in _handTurners)
                {
                    turn.SmoothTurnCurve = curve;
                }
            }
            if (_locomotionTurners != null)
            {
                foreach (var turn in _locomotionTurners)
                {
                    turn.SmoothTurnCurve = curve;
                }
            }
        }

        private void HandleTurnStyleChanged(RotationStyle style)
        {
            TurnerEventBroadcaster.TurnMode turnMode = style == RotationStyle.Snap ?
                TurnerEventBroadcaster.TurnMode.Snap : TurnerEventBroadcaster.TurnMode.Smooth;
            if (_controllerTurners != null)
            {
                foreach (var turn in _controllerTurners)
                {
                    turn.TurnMethod = turnMode;
                }
            }
        }

        #region Injects

        public void InjectOptionalControllerTurners(TurnerEventBroadcaster[] controllerTurners)
        {
            _controllerTurners = controllerTurners;
        }

        public void InjectOptionalHandTurners(TurnerEventBroadcaster[] handTurners)
        {
            _handTurners = handTurners;
        }

        public void InjectOptionalLocomotionTurners(TurnLocomotionBroadcaster[] locomotionTurners)
        {
            _locomotionTurners = locomotionTurners;
        }

        #endregion
    }
}
