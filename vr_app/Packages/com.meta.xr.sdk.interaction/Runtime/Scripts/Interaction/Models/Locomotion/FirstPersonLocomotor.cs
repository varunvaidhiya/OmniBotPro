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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This locomotion handler will respond to locomotion events by moving a CharacterController
    /// and keep it in sync with the VR player (represented by its root transform and the center eye transform).
    /// Appart from the locomotion event types it can also react to external calls for jumping,
    /// crouching or running.
    /// Velocity is applied during Update, but other Locomotion events are not applied instantly, instead they are collected
    /// and applied all once at the end of the frame to avoid desyncs between interactions.
    ///
    /// Translation events:
    /// LocomotionEvent.Translation.Velocity: will add to the current velocity of the character and
    /// a delta will be applied every update to the capsule, checking for collisions and also dampening
    /// the velocity along time.
    /// LocomotionEvent.Translation.Relative: will move the character in the requested direction by also checking
    /// for collisions.
    /// LocomotionEvent.Translation.Absolute: will move the character's feet to the requested position
    /// while trying to snap it to the ground if found under the requested pose.
    /// LocomotionEvent.Translation.AbsoluteEyeLevel: will move the character so it's head ends up placed at the requested
    /// pose (not the feet). By doing this the capsule can potentially become missaligned with the ground so physics will be
    /// disabled until a new event is registered, the player physically moves _exitHotspotDistance or EnableMovement is called manually.
    ///
    /// Rotation events:
    /// LocomotionEvent.Rotation.Velocity: will rotate the character by this angular velocity using the provided delta time.
    /// LocomotionEvent.Rotation.Absolute: will force the character to face the requested absolute rotation.
    /// LocomotionEvent.Rotation.Relative: will force the character to rotate the requested amount.
    ///
    /// Other behaviors:
    /// When Player transforms are provided, the height of the capsule will stay in sync with the actual player height
    /// (distance from the top of their head to their floor).
    /// When the character and player transforms differ more than _maxWallPenetrationDistance, the player will instantly be
    /// teleported to the character position if an input is registered. This can be manually controller by calling ResetPlayerToCharacter manually.
    /// </summary>
    public class FirstPersonLocomotor : MonoBehaviour,
        ILocomotionEventHandler, IDeltaTimeConsumer, ITimeConsumer
    {
        [Header("Character")]
        /// <summary>
        /// The CharacterController reprensenting
        /// the character that is used to move
        /// the player around the scene.
        /// </summary>
        [SerializeField]
        [Tooltip("The CharacterController reprensenting the character that is used to move the player around the scene.")]
        private CharacterController _characterController;

        [Header("VR Player")]
        /// <summary>
        /// Root of the actual VR player so it
        /// can be sync with with the CharacterController.
        /// If you provided a _playerEyes you must also
        /// provide a _playerOrigin.
        /// </summary>
        [SerializeField]
        [Tooltip("Root of the actual VR player so it can be sync with with the CharacterController. If you provided a _playerEyes you must also provide a _playerOrigin.")]
        private Transform _playerOrigin;

        /// <summary>
        /// Eyes of the actual VR player so it
        /// can be sync with the capsule.
        /// If you provided a _playerOrigin you must also
        /// provide a _playerEyes.
        /// </summary>
        [SerializeField]
        [Tooltip("Eyes of the actual VR player so it can be sync with the capsule. If you provided a _playerOrigin you must also provide a _playerEyes.")]
        private Transform _playerEyes;

        /// <summary>
        /// After the player penetrates the head inside a collider
        /// (for example a wall), the maximum distance before the player
        /// gets reset to the capsule position when trying to move synthetically.
        /// </summary>
        [SerializeField]
        [Tooltip("After the player penetrates the head inside a collider (for example a wall), the maximum distance before the player gets reset to the capsule position when trying to move synthetically.")]
        private float _maxWallPenetrationDistance = 0.3f;
        public float MaxWallPenetrationDistance
        {
            get => _maxWallPenetrationDistance;
            set => _maxWallPenetrationDistance = value;
        }

        /// <summary>
        /// After using LocomotionEvent.TranslationType.AbsoluteEyeLevel
        /// that disables the ground checks. What is the maximum deviation
        /// of the player before the physics are re-enabled.
        /// </summary>
        [SerializeField]
        [Tooltip("After using LocomotionEvent.TranslationType.AbsoluteEyeLevel that disables the ground checks. What is the maximum deviation of the player before the physics are re-enabled.")]
        private float _exitHotspotDistance = 0.3f;
        public float ExitHotspotDistance
        {
            get => _exitHotspotDistance;
            set => _exitHotspotDistance = value;
        }

        /// <summary>
        /// When _playerOrigin and _playerEyes are present. This will force
        /// the capsule height to update using the actual player height, instead
        /// of using _defaultHeight
        /// </summary>
        [SerializeField]
        [Tooltip("When _playerOrigin and _playerEyes are present. This will force the capsule height to update using the actual player height, instead of using _defaultHeight")]
        private bool _autoUpdateHeight = true;
        public bool AutoUpdateHeight
        {
            get => _autoUpdateHeight;
            set => _autoUpdateHeight = value;
        }

        [Header("Parameters")]
        /// <summary>
        /// Height of the character capsule
        /// when standing normally.
        /// This might be overriden by _autoUpdateHeight
        /// </summary>
        [SerializeField]
        [Tooltip("Height of the character capsule when standing normally. This might be overriden by _autoUpdateHeight")]
        private float _defaultHeight = 1.4f;
        public float DefaultHeight
        {
            get => _defaultHeight;
            set => _defaultHeight = value;
        }

        /// <summary>
        /// General height offset applied to the capsule.
        /// </summary>
        [SerializeField]
        [Tooltip("General height offset applied to the capsule.")]
        private float _heightOffset = 0f;
        public float HeightOffset
        {
            get => _heightOffset;
            set => _heightOffset = value;
        }

        /// <summary>
        /// Height offset added while crouching.
        /// </summary>
        [SerializeField]
        [Tooltip("Height offset added while crouching.")]
        private float _crouchHeightOffset = -0.5f;
        public float CrouchHeightOffset
        {
            get => _crouchHeightOffset;
            set => _crouchHeightOffset = value;
        }

        /// <summary>
        /// Speed multiplier applied while moving normally.
        /// </summary>
        [SerializeField]
        [Tooltip("Speed multiplier applied while moving normally.")]
        private float _speedFactor = 30.0f;
        public float SpeedFactor
        {
            get => _speedFactor;
            set => _speedFactor = value;
        }

        /// <summary>
        /// Speed multiplier applied while crouching.
        /// </summary>
        [SerializeField]
        [Tooltip("Speed multiplier applied while crouching.")]
        private float _crouchSpeedFactor = 10f;
        public float CrouchSpeedFactor
        {
            get => _crouchSpeedFactor;
            set => _crouchSpeedFactor = value;
        }

        /// <summary>
        /// Speed multiplier applied while running.
        /// </summary>
        [SerializeField]
        [Tooltip("Speed multiplier applied while running.")]
        private float _runningSpeedFactor = 50f;
        public float RunningSpeedFactor
        {
            get => _runningSpeedFactor;
            set => _runningSpeedFactor = value;
        }

        /// <summary>
        /// The rate of acceleration during movement.
        /// </summary>
        [SerializeField]
        [Tooltip("The rate of acceleration during movement.")]
        private float _acceleration = 5f;
        public float Acceleration
        {
            get => _acceleration;
            set => _acceleration = value;
        }

        /// <summary>
        /// The rate of damping on movement while grounded.
        /// </summary>
        [SerializeField]
        [Tooltip("The rate of damping on movement while grounded.")]
        private float _groundDamping = 40f;
        public float GroundDamping
        {
            get => _groundDamping;
            set => _groundDamping = value;
        }

        /// <summary>
        /// The rate of damping on the vertical movement while jumping.
        /// </summary>
        [SerializeField]
        [Tooltip("The rate of damping on the vertical movement while jumping.")]
        private float _jumpDamping = 0f;
        public float JumpDamping
        {
            get => _jumpDamping;
            set => _jumpDamping = value;
        }
        /// <summary>
        /// The rate of damping on the horizontal movement while in the air.
        /// </summary>
        [SerializeField]
        [Tooltip("The rate of damping on the horizontal movement while in the air.")]
        private float _airDamping = 1f;
        public float AirDamping
        {
            get => _airDamping;
            set => _airDamping = value;
        }

        /// <summary>
        /// The force applied to the character when jumping.
        /// </summary>
        [SerializeField]
        [Tooltip("The force applied to the character when jumping.")]
        private float _jumpForce = 2.5f;
        public float JumpForce
        {
            get => _jumpForce;
            set => _jumpForce = value;
        }

        /// <summary>
        /// Modifies the strength of gravity.
        /// </summary>
        [SerializeField]
        [Tooltip("Modifies the strength of gravity.")]
        private float _gravityFactor = 1f;
        public float GravityFactor
        {
            get => _gravityFactor;
            set => _gravityFactor = value;
        }

        /// <summary>
        /// Extra time after starting to fall to allow for jumping
        /// </summary>
        [SerializeField]
        [Tooltip("Extra time after starting to fall to allow jumping.")]
        private float _coyoteTime = 0f;
        public float CoyoteTime
        {
            get => _coyoteTime;
            set => _coyoteTime = value;
        }

        /// <summary>
        /// Correct the input velocity so it always points in the XZ plane
        /// Use with the _inputVelocityStabilization curve to adjust the range
        /// </summary>
        [SerializeField]
        [Tooltip("Correct the input velocity so it always points in the XZ plane." +
            "Use with the " + nameof(_inputVelocityStabilization) + " curve to adjust the range")]
        private bool _flattenInputVelocity = true;
        public bool FlattenInputVelocity
        {
            get => _flattenInputVelocity;
            set => _flattenInputVelocity = value;
        }

        /// <summary>
        /// When the input velocity points too far up or down
        /// the forward direction will be slerped between
        /// the .forward and the .up using this curve for the final forward to be stable
        /// x: from -1 to 1, represents the dot product of forward.worldUp.
        /// y: 0 represents the real forward, 1 the up direction and -1 the down direction.
        /// </summary>
        [SerializeField]
        [Tooltip("When the input velocity points too far up or down " +
            "the forward direction will be slerped between " +
            "the .forward and the .up using this curve for the final forward to be stable. " +
            "x: from -1 to 1, represents the dot product of forward.worldUp. " +
            "y: 0 represents the real forward, 1 the up direction and -1 the down direction.")]
        [ConditionalHide(nameof(_flattenInputVelocity), true, ConditionalHideAttribute.DisplayMode.ShowIfTrue)]
        private AnimationCurve _inputVelocityStabilization = AnimationCurve.EaseInOut(-1f, 0f, 1f, 0f);
        public AnimationCurve InputVelocityStabilization
        {
            get => _inputVelocityStabilization;
            set => _inputVelocityStabilization = value;
        }

        /// <summary>
        /// When Velocity is ignored the character will not try to catch up
        /// to the player and the character won't slide or fall.
        /// It is preferred to re-enable the movement by calling EnableMovement
        /// instead of setting this variable to false directly.
        /// </summary>
        [SerializeField]
        [Tooltip("When Velocity is ignored the character will not try to catch up " +
            "to the player and the character won't slide or fall." +
            "It is preferred to re-enable the movement by calling " + nameof(EnableMovement) +
            " instead of setting this variable to false directly.")]
        private bool _velocityDisabled;

        /// <summary>
        /// If no ground is detected below this distance in meters on Start, it will disable the velocity to prevent falling.
        /// Negative numbers disable this behavior.
        /// </summary>
        [SerializeField, Optional, Min(-1f)]
        [Tooltip("If no ground is detected below this distance in meters on Start, it will disable the velocity to prevent falling. " +
        "Negative numbers disable this behavior.")]
        private float _maxStartGroundDistance = 10f;

        [SerializeField, Optional]
        private Context _context;

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

        protected Action<LocomotionEvent, Pose> _whenLocomotionEventHandled = delegate { };
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

        /// <summary>
        /// Indicates whether the character was detected as grounded after
        /// the last move.
        /// </summary>
        public bool IsGrounded => _characterController.IsGrounded;
        /// <summary>
        /// Indicates whether the character is running
        /// </summary>
        public bool IsRunning => _isRunning;
        /// <summary>
        /// Indicates whether the character is crouching
        /// </summary>
        public bool IsCrouching => _isCrouching;
        /// <summary>
        /// Velocity movements can be disabled either by calling DisableMovement or
        /// teleporting to a Hostpot that sets the Head (instead of the feet) to a specific pose.
        /// </summary>
        public bool IgnoringVelocity => _velocityDisabled || _isHeadInHotspot;
        /// <summary>
        /// The current Velocity of the Locomotor.
        /// Use the setter with caution, a LocomotionEvent of type Velocity
        /// is the preferred way of influencing the velocity of the character
        /// </summary>
        public Vector3 Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        private Pose _accumulatedDeltaFrame;
        private Vector3 _velocity;
        private bool _isHeadInHotspot;
        private Vector3? _headHotspotCenter;
        private float _leftGroundTime;

        private bool _isRunning;
        private bool _isCrouching;

        //Distace from the Sellion to the Top of the Head.
        //Percentile 50 averaged between Male and Female
        private const float _sellionToTopOfHead = 0.1085f;

        //Half distace from the Sellion to the Back of the Head.
        //Percentile 50 averaged between Male and Female
        private const float _sellionToBackOfHeadHalf = 0.0965f;

        private Queue<LocomotionEvent> _deferredLocomotionEvent = new Queue<LocomotionEvent>();
        private YieldInstruction _endOfFrame = new WaitForEndOfFrame();
        private Coroutine _endOfFrameRoutine = null;

        private bool _jumpThisFrame = false;
        private bool _endedFrameGrounded = true;
        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_characterController, nameof(_characterController));
            this.AssertField(_playerOrigin, nameof(_playerOrigin));
            this.AssertField(_playerEyes, nameof(_playerEyes));

            if (!_velocityDisabled
                && _maxStartGroundDistance >= 0f
                && !_characterController.TryGround(_maxStartGroundDistance))
            {
                this.LogWarning(whyItFailed: $"The ground could not be found below the locomotor for {_maxStartGroundDistance} meters. Velocity will be disabled.",
                    howToFix: $"A) Add a ground collider under the character controller and/or adjust the {nameof(CharacterController)}.{nameof(CharacterController.LayerMask)}.\n" +
                    $"B) Set {nameof(_velocityDisabled)} to disable movement and prevent falling without showing this warning.\n" +
                    $"C) Set {nameof(_maxStartGroundDistance)} to 0 to disable this behaviour.\n");
                DisableMovement();
            }

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _endOfFrameRoutine = StartCoroutine(EndOfFrameCoroutine());
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _accumulatedDeltaFrame = Pose.identity;
                StopCoroutine(_endOfFrameRoutine);
                _endOfFrameRoutine = null;
            }
        }

        protected virtual void Update()
        {
            TryExitHotspot();
            UpdateCharacterHeight();
            CatchUpCharacterToPlayer();

            if (!IgnoringVelocity)
            {
                UpdateVelocity();
                Pose startPose = _characterController.Pose;
                Vector3 delta = _velocity * _deltaTimeProvider.Invoke();
                _characterController.Move(delta);
                Pose endPose = _characterController.Pose;
                AccumulateDelta(ref _accumulatedDeltaFrame, startPose, endPose);

                if (_endedFrameGrounded
                    && !IsGrounded)
                {
                    _leftGroundTime = _timeProvider();
                }
            }
        }

        protected virtual void LateUpdate()
        {
            ConsumeDeferredLocomotionEvents();
        }

        protected virtual void LastUpdate()
        {
            CatchUpPlayerToCharacter(_accumulatedDeltaFrame, GetCharacterFeet().y);
            _accumulatedDeltaFrame = Pose.identity;

            if (!_jumpThisFrame)
            {
                _endedFrameGrounded = IsGrounded;
            }
            _jumpThisFrame = false;
        }

        /// <summary>
        /// Jump vertically if it the character grounded.
        /// If the character is crouching it will stand up instead.
        /// </summary>
        public void Jump()
        {
            Jump(Vector3.up * _jumpForce);
        }

        /// <summary>
        /// If the character is crouching it will stand up
        /// and vice versa
        /// </summary>
        public void ToggleCrouch()
        {
            Crouch(!_isCrouching);
        }

        /// <summary>
        /// When true, it will enable crouching by applying the crouch height offset
        /// to the character and using the crouch speed factor for movement
        /// </summary>
        /// <param name="crouch">True to crouch, false to stand up</param>
        public void Crouch(bool crouch)
        {
            if (_isCrouching != crouch)
            {
                _isCrouching = crouch;
            }
        }

        /// <summary>
        /// If the character is walking it will run
        /// and vice versa
        /// </summary>
        public void ToggleRun()
        {
            Run(!_isRunning);
        }
        /// <summary>
        /// When true, it will enable running by applying
        /// the running speed factor to the movement
        /// </summary>
        /// <param name="run"></param>
        public void Run(bool run)
        {
            if (_isRunning != run)
            {
                _isRunning = run;
                TryExitHotspot(true);
            }
        }

        /// <summary>
        /// Disables grounding and velocity movements. Preventing the character
        /// from catching up to the player, sliding or falling.
        /// </summary>
        public void DisableMovement()
        {
            _velocityDisabled = true;
        }

        /// <summary>
        /// Re-enables movement if it was disabled. Making sure grounding is working again.
        /// </summary>
        public void EnableMovement()
        {
            if (!IgnoringVelocity)
            {
                return;
            }

            _velocityDisabled = false;
            _isHeadInHotspot = false;
            _headHotspotCenter = null;
            _velocity = Vector3.zero;

            _characterController.TryGround();
        }

        /// <summary>
        /// Instantly moves the Player to the character position
        /// </summary>
        public void ResetPlayerToCharacter()
        {
            Pose characterPose = _characterController.Pose;
            Vector3 characterFeet = GetCharacterFeet();
            Vector3 playerHeadOffset = _playerOrigin.position - GetPlayerHead();
            playerHeadOffset.y = 0f;
            _playerOrigin.position = characterFeet + playerHeadOffset;
            _accumulatedDeltaFrame = Pose.identity;

            _characterController.SetPose(characterPose);
        }

        #region Locomotion Events Handling

        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            //Velocity translations get added directly (but will be applied during update)
            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Velocity)
            {
                Vector3 velocity = locomotionEvent.Pose.position;
                if (_flattenInputVelocity)
                {
                    Quaternion velocityRot = Quaternion.LookRotation(velocity.normalized, locomotionEvent.Pose.up);
                    velocity = Vector3.ProjectOnPlane(FlattenForwardOffset(velocityRot) * velocity, Vector3.up).normalized * velocity.magnitude;
                }

                AddVelocity(velocity);
                if (IsHeadFarFromPoint(GetCharacterHead(), _maxWallPenetrationDistance))
                {
                    ResetPlayerToCharacter();
                }
                _whenLocomotionEventHandled.Invoke(locomotionEvent, locomotionEvent.Pose);
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None
                && locomotionEvent.Rotation == LocomotionEvent.RotationType.None)
            {
                //empty event? check if there is an action payload
                if (LocomotionActionsBroadcaster.TryGetLocomotionActions(locomotionEvent,
                        out LocomotionActionsBroadcaster.LocomotionAction action, _context)
                    && TryPerformLocomotionActions(action, locomotionEvent.Pose))
                {
                    _whenLocomotionEventHandled.Invoke(locomotionEvent, locomotionEvent.Pose);
                }
            }
            else //other events are stored to be excuted at the end of the frame
            {
                if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute
                    || locomotionEvent.Translation == LocomotionEvent.TranslationType.AbsoluteEyeLevel
                    || locomotionEvent.Translation == LocomotionEvent.TranslationType.Relative)
                {
                    _velocity = Vector3.zero;
                }
                _deferredLocomotionEvent.Enqueue(locomotionEvent);
            }
        }

        private void ConsumeDeferredLocomotionEvents()
        {
            if (_deferredLocomotionEvent.Count == 0)
            {
                return;
            }

            Pose startPose = _characterController.Pose;
            while (_deferredLocomotionEvent.Count > 0)
            {
                LocomotionEvent locomotionEvent = _deferredLocomotionEvent.Dequeue();
                HandleDeferredLocomotionEvent(locomotionEvent);
            }
            Pose endPose = _characterController.Pose;
            AccumulateDelta(ref _accumulatedDeltaFrame, startPose, endPose);
        }

        private void HandleDeferredLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            Pose startPose = _characterController.Pose;

            //Teleport
            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute)
            {
                MoveAbsoluteFeet(locomotionEvent.Pose.position);
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.AbsoluteEyeLevel)
            {
                MoveAbsoluteHead(locomotionEvent.Pose.position);
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Relative)
            {
                MoveRelative(locomotionEvent.Pose.position);
            }

            //Rotation
            if (locomotionEvent.Rotation == LocomotionEvent.RotationType.Absolute)
            {
                RotateAbsolute(locomotionEvent.Pose.rotation);
            }
            else if (locomotionEvent.Rotation == LocomotionEvent.RotationType.Relative)
            {
                RotateRelative(locomotionEvent.Pose.rotation);
            }
            else if (locomotionEvent.Rotation == LocomotionEvent.RotationType.Velocity)
            {
                RotateVelocity(locomotionEvent.Pose.rotation);
            }

            Pose endPose = _characterController.Pose;
            Pose delta = Pose.identity;
            AccumulateDelta(ref delta, startPose, endPose);
            _whenLocomotionEventHandled.Invoke(locomotionEvent, delta);
        }
        private bool TryPerformLocomotionActions(LocomotionActionsBroadcaster.LocomotionAction action, in Pose pose)
        {
            switch (action)
            {
                case LocomotionActionsBroadcaster.LocomotionAction.Crouch: Crouch(true); return true;
                case LocomotionActionsBroadcaster.LocomotionAction.StandUp: Crouch(false); return true;
                case LocomotionActionsBroadcaster.LocomotionAction.ToggleCrouch: ToggleCrouch(); return true;
                case LocomotionActionsBroadcaster.LocomotionAction.Run: Run(true); return true;
                case LocomotionActionsBroadcaster.LocomotionAction.Walk: Run(false); return true;
                case LocomotionActionsBroadcaster.LocomotionAction.ToggleRun: ToggleRun(); return true;
                case LocomotionActionsBroadcaster.LocomotionAction.Jump:
                    Jump(pose.position.sqrMagnitude > 0f ? pose.position : Vector3.up * _jumpForce);
                    return true;
                case LocomotionActionsBroadcaster.LocomotionAction.InvalidTarget: return true;
                default: return false;
            }
        }
        private void AccumulateDelta(ref Pose accumulator, in Pose from, in Pose to)
        {
            accumulator.position = accumulator.position + to.position - from.position;
            accumulator.rotation = (Quaternion.Inverse(from.rotation) * to.rotation) * accumulator.rotation;
        }

        #endregion Locomotion Events Handling

        private void Jump(Vector3 force)
        {
            bool coyoteMoment = _coyoteTime > 0f && _timeProvider() - _leftGroundTime <= _coyoteTime;
            if (!IsGrounded && !coyoteMoment)
            {
                return;
            }

            if (_isCrouching)
            {
                Crouch(false);
                return;
            }

            TryExitHotspot(true);

            if (coyoteMoment && _velocity.y < 0f)
            {
                _velocity.y = 0f;
            }

            _velocity += force;
            _leftGroundTime = 0;
            _endedFrameGrounded = false;
            _jumpThisFrame = true;
        }

        private void AddVelocity(Vector3 velocity)
        {
            TryExitHotspot(true);
            _velocity += velocity * GetModifiedSpeedFactor();
        }

        private void MoveAbsoluteFeet(Vector3 target)
        {
            TryExitHotspot(true);
            Vector3 characterFeet = GetCharacterFeet();
            Vector3 offset = target - characterFeet;
            Vector3 pos = _characterController.Pose.position + offset;
            _characterController.SetPosition(pos);
            _characterController.TryGround(_characterController.MaxStep);
            _velocity = Vector3.zero;
        }

        private void MoveAbsoluteHead(Vector3 target)
        {
            Vector3 characterHead = GetCharacterHead();
            Vector3 offset = target - characterHead;
            Vector3 pos = _characterController.Pose.position + offset;
            _characterController.SetPosition(pos);
            _isHeadInHotspot = true;
            _headHotspotCenter = GetCharacterHead();
            _velocity = Vector3.zero;
        }

        private void MoveRelative(Vector3 offset)
        {
            if (_characterController.IsGrounded || IgnoringVelocity)
            {
                TryExitHotspot(true);
                _velocity = Vector3.zero;
                MoveAndSlideDownStep(offset);
            }
        }

        private void RotateAbsolute(Quaternion target)
        {
            _characterController.SetRotation(target);
        }

        private void RotateRelative(Quaternion target)
        {
            target = target * _characterController.Pose.rotation;
            _characterController.SetRotation(target);
        }

        private void RotateVelocity(Quaternion target)
        {
            target.ToAngleAxis(out float angle, out Vector3 axis);
            angle *= _deltaTimeProvider.Invoke();
            target = Quaternion.AngleAxis(angle, axis) * _characterController.Pose.rotation;
            _characterController.SetRotation(target);
        }

        private bool TryExitHotspot(bool force = false)
        {
            if (_isHeadInHotspot && _headHotspotCenter.HasValue
                && (force || IsHeadFarFromPoint(_headHotspotCenter.Value, _exitHotspotDistance)))
            {
                _isHeadInHotspot = false;
                _headHotspotCenter = null;
                _velocity = Vector3.zero;
                _characterController.TryGround();
                return true;
            }
            return false;
        }

        private void UpdateCharacterHeight()
        {
            float playerHeight = _heightOffset
                + (_isCrouching ? _crouchHeightOffset : 0f)
                + (_autoUpdateHeight ? GetPlayerHeadTop().y - _playerOrigin.position.y : _defaultHeight);
            float desiredHeight = Mathf.Max(playerHeight, _characterController.Radius * 2f);
            _characterController.TrySetHeight(desiredHeight);
        }

        private void CatchUpCharacterToPlayer()
        {
            Vector3 head = GetPlayerHead();
            Vector3 delta = Vector3.ProjectOnPlane(head - _characterController.Pose.position, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(_playerEyes.forward, Vector3.up);
            _characterController.SetRotation(Quaternion.LookRotation(forward, Vector3.up));
            MoveAndSlideDownStep(delta);
        }

        private void CatchUpPlayerToCharacter(Pose delta, float feetHeight)
        {
            Pose characterPose = _characterController.Pose;
            Vector3 originalHeadPosition = GetPlayerHead();
            _playerOrigin.rotation = delta.rotation * _playerOrigin.rotation;
            _playerOrigin.position = _playerOrigin.position + originalHeadPosition - GetPlayerHead();

            Vector3 xzDelta = Vector3.ProjectOnPlane(delta.position, Vector3.up);
            Vector3 finalPosition = _playerOrigin.position + xzDelta;
            finalPosition.y = feetHeight
                + (_isCrouching ? _crouchHeightOffset : 0f)
                + _heightOffset;
            _playerOrigin.position = finalPosition;
            _characterController.SetPose(characterPose);
        }

        private void MoveAndSlideDownStep(Vector3 delta)
        {
            bool wasGrounded = _characterController.IsGrounded;
            float radius = _characterController.Radius;
            Vector3 groundHit = _characterController.GroundHit.point;
            Vector3 prevOffset = groundHit - _characterController.transform.position;

            _characterController.Move(delta);

            if (wasGrounded
                && !_characterController.IsGrounded
                && delta.y <= 0f)
            {
                Vector3 postOffset = groundHit - _characterController.transform.position;
                float prevHeight = RelativeGroundHeightNormalized(prevOffset);
                float postHeight = RelativeGroundHeightNormalized(postOffset);
                if (postHeight >= 0f)
                {
                    float deltaHeight = (prevHeight - postHeight) * radius + _characterController.SkinWidth;
                    _characterController.TryGround(deltaHeight);
                }
            }

            float RelativeGroundHeightNormalized(Vector3 delta)
            {
                float x = Mathf.Clamp01((delta.x * delta.x + delta.z * delta.z) / (radius * radius));
                return Mathf.Sqrt(1f - x);
            }
        }

        private void UpdateVelocity()
        {
            float deltaTime = _deltaTimeProvider.Invoke();

            if (IsGrounded
                && _velocity.y <= 0f)
            {
                _velocity *= 1f / (1f + _groundDamping * deltaTime);
                _velocity.y = 0f;
            }
            else
            {
                float airDamp = 1f / (1f + _airDamping * deltaTime);
                _velocity.x *= airDamp;
                _velocity.z *= airDamp;

                if (_velocity.y > 0f)
                {
                    _velocity.y *= 1f / (1f + _jumpDamping * deltaTime);
                }
                _velocity += Physics.gravity * _gravityFactor * deltaTime;
            }
        }

        private float GetModifiedSpeedFactor()
        {
            if (!IsGrounded || _velocity.y > 0f)
            {
                return 0f;
            }

            float speedFactor = _acceleration
                * (_isCrouching ? _crouchSpeedFactor : _isRunning ? _runningSpeedFactor : _speedFactor)
                * _deltaTimeProvider.Invoke();
            return speedFactor;
        }

        private Quaternion FlattenForwardOffset(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 worldUp = Vector3.up;

            float dot = Vector3.Dot(forward, worldUp);
            float t = _inputVelocityStabilization.Evaluate(dot);

            forward = Vector3.Slerp(forward, up * -Mathf.Sign(t), Mathf.Abs(t));
            forward = Vector3.ProjectOnPlane(forward, worldUp).normalized;

            return Quaternion.FromToRotation(rotation * Vector3.forward, forward);
        }

        private Vector3 GetCharacterFeet()
        {
            Vector3 characterFeet = _characterController.Pose.position
                + Vector3.down * (_characterController.Height * 0.5f + _characterController.SkinWidth);
            return characterFeet;
        }

        private Vector3 GetCharacterHead()
        {
            Vector3 characterHead = _characterController.Pose.position
                + Vector3.up * (_characterController.Height * 0.5f - _sellionToTopOfHead + _characterController.SkinWidth);
            return characterHead;
        }

        private Vector3 GetPlayerHead()
        {
            return _playerEyes.position - _playerEyes.forward * _sellionToBackOfHeadHalf;
        }

        private Vector3 GetPlayerHeadTop()
        {
            return GetPlayerHead() + Vector3.up * _sellionToTopOfHead;
        }

        private bool IsHeadFarFromPoint(Vector3 point, float maxDistance)
        {
            Vector3 head = GetPlayerHead();
            Vector3 vector = Vector3.ProjectOnPlane(head - point, Vector3.up);
            return vector.sqrMagnitude >= maxDistance * maxDistance;
        }

        private IEnumerator EndOfFrameCoroutine()
        {
            while (true)
            {
                yield return _endOfFrame;
                LastUpdate();
            }
        }

        #region Inject

        public void InjectAllFirstPersonLocomotor(CharacterController characterController,
            Transform playerEyes, Transform playerOrigin)
        {
            InjectCharacterController(characterController);
            InjectPlayerEyes(playerEyes);
            InjectPlayerOrigin(playerOrigin);
        }

        public void InjectCharacterController(CharacterController characterController)
        {
            _characterController = characterController;
        }

        public void InjectPlayerEyes(Transform playerEyes)
        {
            _playerEyes = playerEyes;
        }

        public void InjectPlayerOrigin(Transform playerOrigin)
        {
            _playerOrigin = playerOrigin;
        }

        public void InjectOptionalMaxStartGroundDistance(float maxStartGroundDistance)
        {
            _maxStartGroundDistance = maxStartGroundDistance;
        }

        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion
    }
}
