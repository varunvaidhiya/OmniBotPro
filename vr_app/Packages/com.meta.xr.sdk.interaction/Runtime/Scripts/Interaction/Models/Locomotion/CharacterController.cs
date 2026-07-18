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
    /// This component moves a physics capsule collider around the scene
    /// checking for collisions, steps and slopes. It will also resolve collisions
    /// with surfaces sliding along them.
    /// </summary>
    public class CharacterController : MonoBehaviour
    {
        [Header("Character")]
        /// <summary>
        /// Capsule collider that represents the character
        /// and will be moved by the locomotor.
        /// </summary>
        [SerializeField]
        [Tooltip("Capsule collider that represents the character and will be moved by the locomotor.")]
        private CapsuleCollider _capsule;

        /// <summary>
        /// Extra offset added to the radius of the capsule for
        /// soft collisions.
        /// </summary>
        [SerializeField, Min(0f)]
        [Tooltip("Extra offset added to the radius of the capsule for soft collisions.")]
        private float _skinWidth = 0.005f;
        public float SkinWidth
        {
            get => _skinWidth;
            set => _skinWidth = value;
        }

        /// <summary>
        /// LayerMask check for collisions when moving.
        /// </summary>
        [SerializeField]
        [Tooltip("LayerMask check for collisions when moving.")]
        private LayerMask _layerMask = -1;
        public LayerMask LayerMask
        {
            get => _layerMask;
            set => _layerMask = value;
        }

        /// <summary>
        /// Max climbable slope angle in degrees.
        /// </summary>
        [SerializeField, Range(0f, 90f)]
        [Tooltip("Max climbable slope angle in degrees.")]
        private float _maxSlopeAngle = 50f;
        public float MaxSlopeAngle
        {
            get => _maxSlopeAngle;
            set => _maxSlopeAngle = value;
        }

        /// <summary>
        /// Max climbable height for steps.
        /// </summary>
        [SerializeField, Min(0f)]
        [Tooltip("Max climbable height for steps.")]
        private float _maxStep = 0.3f;
        public float MaxStep
        {
            get => _maxStep;
            set => _maxStep = value;
        }

        /// <summary>
        /// Max iterations for sliding the delta movement
        /// after colliding with an obstacle.
        /// </summary>
        [SerializeField, Min(1)]
        [Tooltip("Max iterations for sliding the delta movement after colliding with an obstacle.")]
        private int _maxReboundSteps = 3;
        public int MaxReboundSteps
        {
            get => _maxReboundSteps;
            set => _maxReboundSteps = value;
        }

        [Header("Anchors")]
        /// <summary>
        /// Optional. This transform pose will be updated
        /// with the pose of the character top.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("Optional. This transform pose will be updated with the pose of the character top.")]
        private Transform _headAnchor;

        /// <summary>
        /// Optional. This transform pose will be updated
        /// with the pose of the character feet.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("Optional. This transform pose will be updated with the pose of the character base.")]
        private Transform _feetAnchor;

        /// <summary>
        /// Whether the capsule is touching the ground
        /// </summary>
        public bool IsGrounded => _isGrounded;
        /// <summary>
        /// Actual height of the capsule
        /// </summary>
        public float Height => _capsule.height;
        /// <summary>
        /// Radius of the capsule
        /// </summary>
        public float Radius => _capsule.radius;
        /// <summary>
        /// Current Pose of the capsule
        /// </summary>
        public Pose Pose => _capsule.transform.GetPose();
        /// <summary>
        /// Current hit of the ground against the capsule
        /// </summary>
        public RaycastHit GroundHit => _groundHit;

        protected RaycastHit _groundHit;
        protected bool _isGrounded;
        protected bool _started;

        private const float _cornerHitEpsilon = 0.001f;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(_capsule, nameof(_capsule));

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                UpdateAnchorPoints();
            }
        }

        /// <summary>
        /// Attemps to set the height of the capsule.
        /// If the requested height is higher than the current height, it
        /// will check for collisions above the top and set the height to the
        /// maximum possible value withhin the range.
        /// It will ignore calls if the delta is smaller than the skin of the character.
        /// </summary>
        /// <param name="desiredHeight">The desired height for the capsule</param>
        /// <returns>True if it could change the height by any amount</returns>
        public bool TrySetHeight(float desiredHeight)
        {
            float characterHeight = _capsule.height;
            float deltaThreshold = _skinWidth;
            float heightDelta = desiredHeight - characterHeight;
            if (heightDelta > deltaThreshold
                && CheckMoveCharacter(Vector3.up * heightDelta, out Vector3 movement))
            {
                heightDelta = Mathf.Max(0f, movement.y - _skinWidth);
            }

            if (Mathf.Abs(heightDelta) <= deltaThreshold)
            {
                return false;
            }

            _capsule.height = characterHeight + heightDelta;
            _capsule.transform.position += Vector3.up * heightDelta * 0.5f;

            UpdateAnchorPoints();

            return true;
        }

        /// <summary>
        /// Attempts to snap the capsule to the ground, if it penetrates the capsule.
        /// </summary>
        /// <param name="extraDistance">Extra amount to check under the character for the ground</param>
        /// <returns>True if the ground was found and the character snapped to it</returns>
        public bool TryGround(float extraDistance = 0f)
        {
            if (CalculateGround(out RaycastHit groundHit, extraDistance) && IsFlat(groundHit.normal))
            {
                _isGrounded = true;
                _groundHit = groundHit;
                _capsule.transform.position += Vector3.down * groundHit.distance;
                UpdateAnchorPoints();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the global rotation of the character.
        /// </summary>
        /// <param name="rotation">The desired global rotation</param>
        public void SetRotation(in Quaternion rotation)
        {
            _capsule.transform.rotation = rotation;
            UpdateAnchorPoints();
        }

        /// <summary>
        /// Sets the global position of the character.
        /// </summary>
        /// <param name="position">The desired global position</param>
        public void SetPosition(in Vector3 position)
        {
            _capsule.transform.position = position;
            UpdateAnchorPoints();
        }

        /// <summary>
        /// Sets the global pose of the character
        /// </summary>
        /// <param name="pose">The desired global pose</param>
        public void SetPose(in Pose pose)
        {
            _capsule.transform.SetPose(pose);
            UpdateAnchorPoints();
        }

        /// <summary>
        /// Moves the character sliding along collisions and checking for slopes and steps
        /// </summary>
        /// <param name="delta">The desired delta movement</param>
        public void Move(Vector3 delta)
        {
            if (_isGrounded)
            {
                //if grounded, try to slide along the floor
                Vector3 flatDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
                Vector3 slopedFlatDelta = Vector3.ProjectOnPlane(flatDelta, _groundHit.normal);
                delta = slopedFlatDelta + Vector3.up * delta.y;
            }

            Vector3 movement = Rebound(delta, _maxReboundSteps);
            _capsule.transform.position += movement;
            UpdateGrounded(delta.y < 0f
                && delta.y < movement.y
                && Mathf.Abs(movement.y) < 0.001f);

            UpdateAnchorPoints();
        }

        private Vector3 Rebound(Vector3 delta, int bounces)
        {
            Vector3 capsuleHalfSegment = Vector3.up * Mathf.Max(0f, _capsule.height * 0.5f - _capsule.radius);
            Vector3 capsuleTop = _capsule.transform.position + capsuleHalfSegment;
            Vector3 capsuleBase = _capsule.transform.position - capsuleHalfSegment;

            Vector3 originalFlatDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
            return ReboundRecursive(capsuleBase, capsuleTop, _capsule.radius, delta, originalFlatDelta, bounces);

            Vector3 ReboundRecursive(Vector3 capsuleBase, Vector3 capsuleTop, float radius, Vector3 delta, Vector3 originalFlatDelta, int bounceStep)
            {
                if (bounceStep <= 0
                    || Mathf.Approximately(delta.sqrMagnitude, 0f))
                {
                    return Vector3.zero;
                }

                Vector3 accumulatedDelta = Vector3.zero;
                Vector3 originalDelta = delta;
                Vector3 extraDelta = Vector3.zero;

                RaycastHit? moveHit = null;
                RaycastHit? stepHit = null;

                //check if we collided against a wall
                if (MoveCapsuleCollides(capsuleBase, capsuleTop, radius, delta, out moveHit))
                {
                    (delta, extraDelta) = DecomposeDelta(delta, moveHit.Value);
                }

                capsuleBase += delta;
                capsuleTop += delta;
                accumulatedDelta += delta;

                //check when we collided, whether it was a step that we can climb
                if (_isGrounded
                     && _maxStep > 0f
                     && moveHit.HasValue
                     //early exit if the hitpoint is already too high
                     && moveHit.Value.point.y - (capsuleBase.y - radius - _skinWidth) <= _maxStep)
                {
                    bool stepClimbed = ClimbStep(capsuleBase, capsuleTop, radius, extraDelta,
                        out Vector3 climbDelta, out stepHit);
                    if (stepClimbed)
                    {
                        capsuleBase += climbDelta;
                        capsuleTop += climbDelta;
                        accumulatedDelta += climbDelta;

                        if (stepHit.HasValue)
                        {
                            (_, extraDelta) = DecomposeDelta(climbDelta, stepHit.Value);
                            extraDelta = SlideDelta(extraDelta, originalFlatDelta, stepHit.Value);
                        }
                        else
                        {
                            extraDelta = Vector3.zero;
                        }
                    }
                }

                if (moveHit.HasValue && !stepHit.HasValue)
                {
                    extraDelta = SlideDelta(extraDelta, originalFlatDelta, moveHit.Value);
                }

                return accumulatedDelta + ReboundRecursive(capsuleBase, capsuleTop, radius, extraDelta, originalFlatDelta, bounceStep - 1);
            }
        }

        private bool ClimbStep(Vector3 capsuleBase, Vector3 capsuleTop, float radius, Vector3 delta, out Vector3 climbDelta, out RaycastHit? stepHit)
        {
            stepHit = null;
            climbDelta = Vector3.zero;
            //step climbing just happens in the XZ plane
            delta.y = 0;

            float baseOffset = Mathf.Min(_maxStep, capsuleTop.y - capsuleBase.y);
            float topOffset = Mathf.Max(0f, _maxStep - baseOffset);
            Vector3 capsuleMaxStepBase = capsuleBase + Vector3.up * baseOffset;
            Vector3 capsuleMaxStepTop = capsuleTop + Vector3.up * topOffset;

            //then move the capsule forward but with the base floating the _maxStep amount
            if (MoveCapsuleCollides(capsuleMaxStepBase, capsuleMaxStepTop, radius, delta, out RaycastHit? hit))
            {
                stepHit = hit;
                //check if we hit one of the caps
                //if so, correct the surface normal in case it is a corner
                Vector3 capsuleDir = capsuleTop - capsuleBase;
                if (Mathf.Approximately(capsuleDir.sqrMagnitude, 0f)
                    || Mathf.Abs(Vector3.Dot(hit.Value.normal, capsuleDir.normalized)) > _cornerHitEpsilon)
                {
                    Vector3 rayDir = -hit.Value.normal;
                    Ray cornerRay = new Ray(hit.Value.point - rayDir * hit.Value.distance, rayDir);
                    if (hit.Value.collider.Raycast(cornerRay, out RaycastHit capHit, hit.Value.distance + _cornerHitEpsilon))
                    {
                        //replace only hit, not stephit, for correcting the delta
                        //but not overriding the capsule normal
                        hit = capHit;
                    }
                }

                (delta, _) = DecomposeDelta(delta, hit.Value);
            }

            //then we try to grow the base back to its original height to check for the ground
            bool groundCollided = GroundSphereCast(
                origin: capsuleTop + delta,
                radius: radius + _skinWidth,
                maxDistance: _capsule.height - 2 * radius,
                out RaycastHit stepDownHit);

            if (groundCollided)
            {
                stepDownHit.distance = Mathf.Max(0, _capsule.height - 2 * radius) - stepDownHit.distance;
                if (stepDownHit.distance <= _maxStep
                    && IsFlat(stepDownHit.normal))
                {
                    delta.y = Mathf.Max(delta.y, stepDownHit.distance);
                    //now that we know the size of the step, we must also check that the capsule
                    //can move up that amount before going forward
                    Vector3 stepUp = Vector3.up * delta.y;
                    if (MoveCapsuleCollides(capsuleBase, capsuleTop, radius, stepUp, out _))
                    {
                        return false;
                    }

                    climbDelta = delta;
                    return true;
                }
            }

            return false;
        }

        private bool CheckMoveCharacter(Vector3 delta, out Vector3 movement)
        {
            Vector3 capsuleHalfSegment = Vector3.up * Mathf.Max(0f, _capsule.height * 0.5f - _capsule.radius);
            Vector3 capsuleTop = _capsule.transform.position + capsuleHalfSegment;
            Vector3 capsuleBase = _capsule.transform.position - capsuleHalfSegment;
            float radius = _capsule.radius;
            if (MoveCapsuleCollides(capsuleBase, capsuleTop, radius, delta, out RaycastHit? hit))
            {
                (delta, _) = DecomposeDelta(delta, hit.Value);
                movement = delta;
                return true;
            }
            movement = Vector3.zero;
            return false;
        }

        private bool MoveCapsuleCollides(Vector3 capsuleBase, Vector3 capsuleTop, float radius, Vector3 delta, out RaycastHit? moveHit)
        {
            float sqrMagnitude = delta.sqrMagnitude;
            if (Mathf.Approximately(sqrMagnitude, 0f))
            {
                moveHit = null;
                return false;
            }

            float magnitude = sqrMagnitude < _skinWidth * _skinWidth ? _skinWidth : Mathf.Sqrt(sqrMagnitude);
            bool collided = Physics.CapsuleCast(capsuleBase, capsuleTop, radius, delta.normalized,
                       out RaycastHit hit, magnitude, _layerMask.value, QueryTriggerInteraction.Ignore);
            moveHit = collided ? hit : null;
            return collided;
        }

        private (Vector3, Vector3) DecomposeDelta(Vector3 delta, RaycastHit hit)
        {
            Vector3 deltaDir = delta.normalized;
            float projectedSkin = Mathf.Max(0.1f, Vector3.Dot(deltaDir, -hit.normal)) * _skinWidth;
            Vector3 freeDelta = deltaDir * Mathf.Max(0f, hit.distance - projectedSkin);
            Vector3 remainingDelta = delta - freeDelta;
            return (freeDelta, remainingDelta);
        }

        private Vector3 SlideDelta(Vector3 delta, Vector3 originalFlatDelta, RaycastHit hit)
        {
            //if the hit a steep ground slope
            //consider it a wall to avoid climbing attempts when sliding
            Vector3 hitNormal = hit.normal;
            if (!IsFlat(hitNormal))
            {
                hitNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
            }

            Vector3 flatDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
            flatDelta = Vector3.ProjectOnPlane(flatDelta, hitNormal);
            if (Vector3.Dot(flatDelta, originalFlatDelta) <= 0)
            {
                flatDelta = Vector3.zero;
            }

            Vector3 verticalDelta = Vector3.up * delta.y;
            verticalDelta = Vector3.ProjectOnPlane(verticalDelta, hit.normal);

            return flatDelta + verticalDelta;
        }

        private bool IsFlat(Vector3 groundNormal)
        {
            float angle = Vector3.Angle(Vector3.up, groundNormal);
            return angle <= _maxSlopeAngle;
        }

        private void UpdateGrounded(bool forceGrounded = false)
        {
            _isGrounded = CalculateGround(out _groundHit) && IsFlat(_groundHit.normal);
            if (!_isGrounded && forceGrounded)
            {
                _isGrounded = true;
                _groundHit.normal = Vector3.up;
                _groundHit.point = _capsule.transform.position + Vector3.down * (_capsule.height * 0.5f + _skinWidth);
            }
        }

        private bool CalculateGround(out RaycastHit groundHit, float extraDistance = 0f)
        {
            //check just the feet
            float sphereOffset = _capsule.height * 0.5f - _capsule.radius;
            Vector3 capsuleBase = _capsule.transform.position + Vector3.down * sphereOffset;
            bool feetHit = GroundSphereCast(
                origin: capsuleBase + Vector3.up * _skinWidth,
                radius: _capsule.radius + _skinWidth,
                maxDistance: _skinWidth + extraDistance,
                out groundHit);

            if (feetHit)
            {
                groundHit.distance -= _skinWidth * 0.5f;
                return true;
            }

            //check inside body under the head
            Vector3 capsuleTop = _capsule.transform.position + Vector3.up * sphereOffset;
            bool halfBodyHit = GroundSphereCast(
                origin: capsuleTop,
                radius: _capsule.radius,
                maxDistance: Mathf.Max(0, _capsule.height - 2 * _capsule.radius + _skinWidth),
                out groundHit);

            if (halfBodyHit)
            {
                groundHit.distance -= Mathf.Max(0, _capsule.height - 2 * _capsule.radius);
                return true;
            }

            return false;
        }

        private bool GroundSphereCast(Vector3 origin, float radius, float maxDistance, out RaycastHit groundHit)
        {
            Vector3 down = Vector3.down;
            bool sphereCollided = Physics.SphereCast(origin, radius, down,
                out groundHit, maxDistance,
                 _layerMask.value, QueryTriggerInteraction.Ignore);

            if (sphereCollided)
            {
                bool collided = groundHit.collider.Raycast(
                    new Ray(groundHit.point - down * _skinWidth, down),
                    out RaycastHit preciseHit, _skinWidth * 2);
                if (collided)
                {
                    groundHit.normal = preciseHit.normal;
                }
                else
                {
                    groundHit.normal = -down;
                }
                return true;
            }

            groundHit = default;
            return false;
        }

        private void UpdateAnchorPoints()
        {
            Vector3 capsuleHalfSegment = Vector3.up * Mathf.Max(0f, _capsule.height * 0.5f + _skinWidth);
            Vector3 capsuleTop = _capsule.transform.position + capsuleHalfSegment;
            Vector3 capsuleBase = _capsule.transform.position - capsuleHalfSegment;
            Quaternion rotation = _capsule.transform.rotation;
            if (_headAnchor != null)
            {
                _headAnchor.transform.SetPositionAndRotation(capsuleTop, rotation);
            }

            if (_feetAnchor != null)
            {
                _feetAnchor.transform.SetPositionAndRotation(capsuleBase, rotation);
            }
        }

        #region Inject

        public void InjectAllCharacterController(CapsuleCollider capsule)
        {
            InjectCapsule(capsule);
        }

        public void InjectCapsule(CapsuleCollider capsule)
        {
            _capsule = capsule;
        }

        public void InjectOptionalFeetAnchor(Transform feetAnchor)
        {
            _feetAnchor = feetAnchor;
        }

        public void InjectOptionalHeadAnchor(Transform headAnchor)
        {
            _headAnchor = headAnchor;
        }

        #endregion
    }
}
