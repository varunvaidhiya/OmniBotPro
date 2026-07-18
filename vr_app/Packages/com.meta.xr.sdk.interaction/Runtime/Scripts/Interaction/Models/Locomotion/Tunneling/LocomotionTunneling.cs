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
    /// This component will listen to locomotion events and reduce the FOV of the player
    /// with a vignette based on the different character motions: rotation, linear movement, accelerations.
    /// </summary>
    public class LocomotionTunneling : MonoBehaviour,
        ITimeConsumer, IDeltaTimeConsumer
    {
        /// <summary>
        /// The character's locomotion handler to listen for events
        /// </summary>
        [SerializeField, Interface(typeof(ILocomotionEventHandler))]
        private UnityEngine.Object _locomotor;
        private ILocomotionEventHandler Locomotor { get; set; }
        /// <summary>
        /// The actual tunneling effect renderer that will apply the vignette
        /// </summary>
        [SerializeField]
        private TunnelingEffect _tunneling;
        /// <summary>
        /// The curve that describes the FOV in degrees per angular speed when
        /// the character rotates
        /// </summary>
        [SerializeField]
        private AnimationCurve _rotationStrength;
        public AnimationCurve RotationStrength
        {
            get => _rotationStrength;
            set => _rotationStrength = value;
        }
        /// <summary>
        /// The curve that describes the FOV in degrees per m/ss when
        /// the character accelerates
        /// </summary>
        [SerializeField]
        private AnimationCurve _accelerationStrength;
        public AnimationCurve AccelerationStrength
        {
            get => _accelerationStrength;
            set => _accelerationStrength = value;
        }
        /// <summary>
        /// The curve that describes the FOV in degrees per m/s when
        /// the character translates
        /// </summary>
        [SerializeField]
        private AnimationCurve _movementStrength;
        public AnimationCurve MovementStrength
        {
            get => _movementStrength;
            set => _movementStrength = value;
        }
        /// <summary>
        /// Seconds without motion to wait before starting to fade out the effect
        /// </summary>
        [SerializeField]
        private float _fadeOutTime = 0.2f;
        public float FadeOutTime
        {
            get => _fadeOutTime;
            set => _fadeOutTime = value;
        }
        /// <summary>
        /// Seconds for the fade out animation when no motion is detected
        /// </summary>
        [SerializeField]
        private float _fadeOutWait = 0.2f;
        public float FadeOutWait
        {
            get => _fadeOutWait;
            set => _fadeOutWait = value;
        }

        private Func<float> _deltaTimeProvider = () => Time.deltaTime;
        public void SetDeltaTimeProvider(Func<float> deltaTimeProvider)
        {
            _deltaTimeProvider = deltaTimeProvider;
        }

        private Func<float> _timeProvider = () => Time.time;
        public void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        private bool _started = false;
        private Vector3 _lastVelocity = Vector3.zero;
        private float _fadeOutStart;

        protected virtual void Awake()
        {
            Locomotor = _locomotor as ILocomotionEventHandler;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Locomotor, nameof(_locomotor));
            this.AssertField(_tunneling, nameof(_tunneling));
            this.AssertField(_rotationStrength, nameof(_rotationStrength));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _tunneling.UseAimingTarget = false;
                _tunneling.UserFOV = 360f;
                _lastVelocity = Vector3.zero;
                Locomotor.WhenLocomotionEventHandled += HandleLocomotionEventHandled;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _tunneling.enabled = false;
                Locomotor.WhenLocomotionEventHandled -= HandleLocomotionEventHandled;
            }
        }

        private void HandleLocomotionEventHandled(LocomotionEvent locomotionEvent, Pose pose)
        {
            if (locomotionEvent.Rotation == LocomotionEvent.RotationType.Velocity)
            {
                locomotionEvent.Pose.rotation.ToAngleAxis(out float angle, out _);
                float strength = _rotationStrength.Evaluate(Mathf.Abs(angle));
                SetFOV(strength);
            }

            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Velocity)
            {
                Vector3 velocity = pose.position;
                float acceleration = (velocity - _lastVelocity).magnitude / _deltaTimeProvider.Invoke();
                _lastVelocity = velocity;
                float movementStrength = _movementStrength.Evaluate(velocity.magnitude);
                float accelerationStrength = _accelerationStrength.Evaluate(acceleration);
                SetFOV(Mathf.Min(accelerationStrength, movementStrength));
            }

            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Relative
                || locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute
                || locomotionEvent.Translation == LocomotionEvent.TranslationType.AbsoluteEyeLevel)
            {
                if (pose.position.sqrMagnitude > 0f)
                {
                    float movementStrength = _movementStrength.keys[_movementStrength.length - 1].value;
                    SetFOV(movementStrength);
                }
            }
        }

        private void SetFOV(float fov)
        {
            if (Mathf.Approximately(fov, 0f))
            {
                return;
            }

            _tunneling.enabled = true;
            _tunneling.UserFOV = Mathf.Min(_tunneling.UserFOV, fov);
            _tunneling.AlphaStrength = 1f;
            _fadeOutStart = _timeProvider.Invoke();
        }

        protected virtual void LateUpdate()
        {
            float time = _timeProvider.Invoke();
            float ellapsedTime = time - _fadeOutStart;
            if (ellapsedTime < _fadeOutWait)
            {
                return;
            }

            float animationTime = ellapsedTime - _fadeOutWait;
            float alpha = Mathf.Lerp(1f, 0f, animationTime / _fadeOutTime);
            _tunneling.AlphaStrength = alpha;
            if (alpha <= 0f)
            {
                _tunneling.enabled = false;
                _tunneling.UserFOV = 360f;
            }
        }
    }
}
