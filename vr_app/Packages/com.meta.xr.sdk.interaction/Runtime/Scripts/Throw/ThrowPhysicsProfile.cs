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
    /// A ScriptableObject that defines the physics behavior for thrown objects when used with <see cref="ThrowTuner"/>.
    /// This profile contains settings for velocity modification, rotation behavior, drag, and continuous flight forces.
    /// </summary>
    [CreateAssetMenu(fileName = "NewThrowPhysicsProfile",
                     menuName = "Meta/Interaction SDK/Throw Physics Profile")]
    public class ThrowPhysicsProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Multiplier for velocity in object's local space. (1,1,1) = no change, (2,1,1) = double forward speed")]
        private Vector3 _velocityScale = Vector3.one;
        [SerializeField]
        [Tooltip("Velocity added in object's local space after scaling. Useful for a boost or minimum throw speeds.")]
        private Vector3 _velocityAdd = Vector3.zero;
        [SerializeField]
        [Tooltip("Max speed limit after all velocity modifications. Use negative values for no limit.")]
        private float _maxSpeed = -1;
        [SerializeField]
        [Tooltip("Multiplier for angular velocity in object's local space. (0,0,0) = no spin, (1,1,1) = normal spin")]
        private Vector3 _spinScale = Vector3.one;
        [SerializeField]
        [Tooltip("Angular velocity added in object's local space. Useful for objects that should always spin.")]
        private Vector3 _spinAdd = Vector3.zero;
        [SerializeField]
        [Tooltip("Max angular speed limit. Use negative values for no limit.")]
        private float _maxSpin = -1;
        [SerializeField]
        [Tooltip("Instantly align object forward direction with velocity direction when thrown")]
        private bool _alignForwardOnce;
        [SerializeField]
        [Tooltip("Continuously align object with velocity direction during flight (like an arrow)")]
        private bool _keepForwardToVelocity;
        [SerializeField]
        [Range(0.1f, 20f)]
        [Tooltip("Speed of continuous alignment with velocity direction")]
        private float _forwardLerpSpeed = 6f;
        [SerializeField]
        [Tooltip("Enable built-in continuous forces (gravity scale, constant forces, damping)")]
        private bool _enableBuiltIns;
        [SerializeField]
        [Range(0f, 4f)]
        [Tooltip("Gravity multiplier. 1.0 = normal, 0.0 = weightless, 2.0 = heavy")]
        private float _gravityScale = 1f;
        [SerializeField]
        [Tooltip("Acceleration in LOCAL space each physics tick.")]
        private Vector3 _localConstantForce = Vector3.zero;
        [SerializeField]
        [Tooltip("Torque in LOCAL space each physics tick.")]
        private Vector3 _localConstantTorque = Vector3.zero;
        [SerializeField]
        [Tooltip("Local damping per second (1,1,1)=none")]
        private Vector3 _localVelocityDamping = Vector3.one;
        [SerializeField]
        private float _linearDrag;
        [SerializeField]
        private float _angularDrag = 0.05f;

        /// <summary>
        /// Gets the velocity scale multiplier applied in local space.
        /// </summary>
        public Vector3 VelocityScale => _velocityScale;
        /// <summary>
        /// Gets the velocity addition applied in local space.
        /// </summary>
        public Vector3 VelocityAdd => _velocityAdd;
        /// <summary>
        /// Gets the maximum speed limit for thrown objects.
        /// </summary>
        public float MaxSpeed => _maxSpeed;
        /// <summary>
        /// Gets the spin scale multiplier applied in local space.
        /// </summary>
        public Vector3 SpinScale => _spinScale;
        /// <summary>
        /// Gets the spin addition applied in local space.
        /// </summary>
        public Vector3 SpinAdd => _spinAdd;
        /// <summary>
        /// Gets the maximum angular speed limit for thrown objects.
        /// </summary>
        public float MaxSpin => _maxSpin;
        /// <summary>
        /// Gets whether to instantly align forward direction with velocity at throw time.
        /// </summary>
        public bool AlignForwardOnce => _alignForwardOnce;
        /// <summary>
        /// Gets whether to continuously align forward direction with velocity during flight.
        /// </summary>
        public bool KeepForwardToVelocity => _keepForwardToVelocity;
        /// <summary>
        /// Gets the speed of continuous velocity alignment.
        /// </summary>
        public float ForwardLerpSpeed => _forwardLerpSpeed;
        /// <summary>
        /// Gets whether built-in continuous forces are enabled.
        /// </summary>
        public bool EnableBuiltIns => _enableBuiltIns;
        /// <summary>
        /// Gets the gravity scale multiplier.
        /// </summary>
        public float GravityScale => _gravityScale;
        /// <summary>
        /// Gets the constant force applied in local space.
        /// </summary>
        public Vector3 LocalConstantForce => _localConstantForce;
        /// <summary>
        /// Gets the constant torque applied in local space.
        /// </summary>
        public Vector3 LocalConstantTorque => _localConstantTorque;
        /// <summary>
        /// Gets the velocity damping factors per second in local space.
        /// </summary>
        public Vector3 LocalVelocityDamping => _localVelocityDamping;
        /// <summary>
        /// Gets the linear drag coefficient.
        /// </summary>
        public float LinearDrag => _linearDrag;
        /// <summary>
        /// Gets the angular drag coefficient.
        /// </summary>
        public float AngularDrag => _angularDrag;
    }
}
