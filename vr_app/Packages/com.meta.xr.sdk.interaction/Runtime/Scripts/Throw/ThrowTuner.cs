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

namespace Oculus.Interaction.Throw
{

    /// <summary>
    /// Provides tunable throwing physics behavior for grabbable objects in the Interaction SDK.
    /// This component applies customizable physics modifications when an object is thrown, including
    /// velocity adjustments, rotation alignment, and continuous flight forces.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ThrowTuner : MonoBehaviour
    {
        /// <summary>
        /// The physics profile that defines how this object behaves when thrown.
        /// This profile contains velocity scaling, drag settings, alignment options, and built-in forces.
        /// </summary>
        [SerializeField]
        [Tooltip("Physics profile that defines throwing behavior including velocity scaling, drag, and flight forces.")]
        private ThrowPhysicsProfile _profile;

        /// <summary>
        /// The grabbable component that provides throw events and interaction state.
        /// </summary>
        [SerializeField]
        [Tooltip("The Grabbable component that handles user interactions and provides throw events.")]
        private Grabbable _grabbable;

        /// <summary>
        /// The rigidbody component that physics modifications will be applied to.
        /// </summary>
        [SerializeField]
        [Tooltip("The Rigidbody component that will receive physics modifications during throws.")]
        private Rigidbody _rigidbody;

        private bool _inFlight;
        private bool _started = false;

        /// <summary>
        /// Gets the physics profile currently assigned to this ThrowTuner.
        /// </summary>
        public ThrowPhysicsProfile Profile => _profile;

        /// <summary>
        /// Gets whether the object is currently in flight after being thrown.
        /// </summary>
        public bool InFlight => _inFlight;

        #region Editor Events

        /// <summary>
        /// Auto-wires component references during development time.
        /// </summary>
        protected virtual void Reset()
        {
            _grabbable = this.GetComponent<Grabbable>();
            _rigidbody = this.GetComponent<Rigidbody>();
        }
        #endregion

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_profile, nameof(_profile));
            this.AssertField(_grabbable, nameof(_grabbable));
            this.AssertField(_rigidbody, nameof(_rigidbody));
            this.AssertField(_grabbable.VelocityThrow, nameof(_grabbable.VelocityThrow));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _grabbable.VelocityThrow.WhenThrown += HandleThrow;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _grabbable.VelocityThrow.WhenThrown -= HandleThrow;
            }

        }

        protected virtual void FixedUpdate()
        {
            if (_inFlight)
            {
                UpdateFlight();
            }
        }

        private void HandleThrow(Vector3 velocity, Vector3 torque)
        {
            ApplyProfile(velocity, torque);
            _inFlight = true;
        }

        /// <summary>
        /// Applies the physics profile to the rigidbody when a throw begins.
        /// Override this method to customize how the initial throw physics are applied.
        /// </summary>
        protected virtual void ApplyProfile(Vector3 velocity, Vector3 angularVelocity)
        {
            ApplyInitialTweaks(ref velocity, ref angularVelocity);
#pragma warning disable CS0618
            _rigidbody.velocity = velocity;
#pragma warning disable CS0618
            _rigidbody.angularVelocity = angularVelocity;

            if (_profile.AlignForwardOnce)
            {
                SnapForwardToVelocity();
            }
#pragma warning disable CS0618
            _rigidbody.drag = _profile.LinearDrag;
            _rigidbody.angularDrag = _profile.AngularDrag;
#pragma warning disable CS0618
        }

        /// <summary>
        /// Updates the object's flight behavior each physics frame.
        /// Override this method to add custom flight behaviors like homing or magnetism.
        /// </summary>
        protected virtual void UpdateFlight()
        {
            if (!_inFlight || _rigidbody.isKinematic)
            {
                _inFlight = false;
                return;
            }

            if (_profile.EnableBuiltIns)
            {
                ApplyBuiltInForces();
            }

            if (_profile.KeepForwardToVelocity)
            {
                AlignForward(_profile.ForwardLerpSpeed);
            }
        }

        /// <summary>
        /// Applies velocity and angular velocity modifications based on the physics profile.
        /// Override this method to add custom velocity calculations.
        /// </summary>
        protected virtual void ApplyInitialTweaks(ref Vector3 velocity, ref Vector3 angularVelocity)
        {
            velocity = transform.TransformVector(
                Vector3.Scale(transform.InverseTransformVector(velocity), _profile.VelocityScale)
              + _profile.VelocityAdd);

            angularVelocity = transform.TransformVector(
                Vector3.Scale(transform.InverseTransformVector(angularVelocity), _profile.SpinScale)
              + _profile.SpinAdd);

            if (_profile.MaxSpeed >= 0)
            {
                velocity = Vector3.ClampMagnitude(velocity, _profile.MaxSpeed);
            }

            if (_profile.MaxSpin >= 0)
            {
                angularVelocity = Vector3.ClampMagnitude(angularVelocity, _profile.MaxSpin);
            }
        }

        private void SnapForwardToVelocity()
        {
#pragma warning disable CS0618
            Vector3 v = _rigidbody.velocity;
#pragma warning disable CS0618
            if (v.sqrMagnitude < 1e-4f) return;

            Quaternion targetRotation = Quaternion.LookRotation(v, transform.up);
            _rigidbody.rotation = targetRotation;
        }

        /// <summary>
        /// Resets the object's rotation to identity (no rotation).
        /// Useful for resetting objects after they've been thrown.
        /// </summary>
        public void ResetRotation()
        {
            _rigidbody.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Applies the built-in forces defined in the physics profile.
        /// This includes gravity scaling, constant forces, and velocity damping.
        /// </summary>
        protected virtual void ApplyBuiltInForces()
        {
            if (!Mathf.Approximately(_profile.GravityScale, 1f))
            {
                _rigidbody.AddForce(Physics.gravity * (_profile.GravityScale - 1f), ForceMode.Acceleration);
            }

            if (_profile.LocalConstantForce != Vector3.zero)
            {
                _rigidbody.AddForce(transform.TransformDirection(_profile.LocalConstantForce), ForceMode.Acceleration);
            }

            if (_profile.LocalConstantTorque != Vector3.zero)
            {
                _rigidbody.AddTorque(transform.TransformDirection(_profile.LocalConstantTorque), ForceMode.Acceleration);
            }

            if (_profile.LocalVelocityDamping != Vector3.one)
            {
                Vector3 localV = transform.InverseTransformVector(_rigidbody.velocity);
                Vector3 factor = Vector3.one - _profile.LocalVelocityDamping * Time.fixedDeltaTime;
                localV.x *= Mathf.Max(0f, factor.x);
                localV.y *= Mathf.Max(0f, factor.y);
                localV.z *= Mathf.Max(0f, factor.z);
#pragma warning disable CS0618
                _rigidbody.velocity = transform.TransformVector(localV);
#pragma warning disable CS0618
            }
        }

        /// <summary>
        /// Gradually aligns the object's forward direction to match its velocity direction.
        /// </summary>
        /// <param name="lerpSpeed">The speed at which to perform the alignment</param>
        private void AlignForward(float lerpSpeed)
        {
            Vector3 v = _rigidbody.velocity;
            if (v.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(v, transform.up);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, target,
                                                     lerpSpeed * Time.fixedDeltaTime));
        }

        #region Injection Methods

        /// <summary>
        /// Sets the physics profile for a dynamically instantiated ThrowTuner.
        /// </summary>
        /// <param name="profile">The ThrowPhysicsProfile to be assigned</param>
        public void InjectProfile(ThrowPhysicsProfile profile)
        {
            _profile = profile;
        }

        /// <summary>
        /// Sets the Grabbable for a dynamically instantiated ThrowTuner.
        /// This method exists to support Interaction SDK's dependency injection pattern.
        /// </summary>
        /// <param name="grabbable">The Grabbable component to be assigned</param>
        public void InjectGrabbable(Grabbable grabbable)
        {
            _grabbable = grabbable;
        }

        /// <summary>
        /// Sets the Rigidbody for a dynamically instantiated ThrowTuner.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody component to be assigned</param>
        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Sets all required dependencies for a dynamically instantiated ThrowTuner.
        /// </summary>
        /// <param name="profile">The ThrowPhysicsProfile to be assigned</param>
        /// <param name="grabbable">The Grabbable component to be assigned</param>
        /// <param name="rigidbody">The Rigidbody component to be assigned</param>
        public void InjectAllThrowTuner(ThrowPhysicsProfile profile, Grabbable grabbable, Rigidbody rigidbody)
        {
            InjectProfile(profile);
            InjectGrabbable(grabbable);
            InjectRigidbody(rigidbody);
        }

        #endregion
    }
}
