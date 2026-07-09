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

using Oculus.Interaction.Surfaces;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines a raycast interaction suitable to use at both short and long ranges. The origin and direction of the ray can be set by
    /// anything, but are typically associated with the motion of a <see cref="Input.IController"/> or <see cref="Input.IHand"/>.
    /// The interactable type for this interactor is <see cref="RayInteractable"/>.
    /// </summary>
    public class RayInteractor : PointerInteractor<RayInteractor, RayInteractable>
    {
        /// <summary>
        /// A selector indicating when the interactor should select or unselect the best available interactable.
        /// </summary>
        [Tooltip("A selector indicating when the Interactor should select or unselect the best available interactable.")]
        [SerializeField, Interface(typeof(ISelector))]
        private UnityEngine.Object _selector;

        /// <summary>
        /// The origin of the ray. The position of this Transform is used as the ray's origin, and the ray's direction is set to
        /// the Transform's forward direction.
        /// </summary>
        [Tooltip("The origin of the ray.")]
        [SerializeField]
        private Transform _rayOrigin;

        /// <summary>
        /// The maximum length of the ray, in meters in world space.
        /// </summary>
        [Tooltip("The maximum length of the ray.")]
        [SerializeField]
        private float _maxRayLength = 5f;

        /// <summary>
        /// (Meters, World) The threshold below which distances to a surface are treated as equal for the purposes of ranking.
        /// </summary>
        [SerializeField]
        [Tooltip("(Meters, World) The threshold below which distances to a surface " +
                 "are treated as equal for the purposes of ranking.")]
        private float _equalDistanceThreshold = 0.001f;

        private RayCandidateProperties _rayCandidateProperties = null;

        private IMovement _movement;
        private SurfaceHit _movedHit;
        private Pose _movementHitDelta = Pose.identity;

        /// <summary>
        /// The position, in world space, from which the interactor's raycast will begin. This position is derived from the
        /// Transform provided as _rayOrigin, which can be set either in the Unity Editor or programmatically using
        /// <see cref="InjectRayOrigin(Transform)"/>.
        /// </summary>
        public Vector3 Origin { get; protected set; }

        /// <summary>
        /// The rotation, in world space, of the interactor's raycast. This rotation is derived from the Transform provided as
        /// _rayOrigin, which can be set either in the Unity Editor or programmatically using <see cref="InjectRayOrigin(Transform)"/>.
        /// </summary>
        public Quaternion Rotation { get; protected set; }

        /// <summary>
        /// The forward direction, in world space, of the interactor's raycast. This is derived from the Transform provided as
        /// _rayOrigin, which can be set either in the Unity Editor or programmatically using <see cref="InjectRayOrigin(Transform)"/>.
        /// </summary>
        public Vector3 Forward { get; protected set; }

        /// <summary>
        /// The end position, in world space, of the interactor's raycast. This is discovered as part of the raycast process and will
        /// lie on the hit <see cref="RayInteractable"/> if one was hit, or at
        /// `<see cref="Origin"/> + <see cref="Forward"/> * <see cref="MaxRayLength"/>` if nothing was hit.
        /// </summary>
        public Vector3 End { get; set; }

        /// <summary>
        /// The maximum allowable length of the ray, in meters in world space. <see cref="RayInteractable"/>s intersected by the ray
        /// at points more distant from the <see cref="Origin"/> than this will be ignored.
        /// </summary>
        public float MaxRayLength
        {
            get
            {
                return _maxRayLength;
            }
            set
            {
                _maxRayLength = value;
            }
        }

        /// <summary>
        /// If the most recent raycast hit a <see cref="RayInteractable"/>, information about that hit will be exposed here. Otherwise,
        /// this property will be null.
        /// </summary>
        public SurfaceHit? CollisionInfo { get; protected set; }

        /// <summary>
        /// The ray which is used when raycasting for interaction. This is derived from <see cref="Origin"/> and <see cref="Forward"/>.
        /// </summary>
        public Ray Ray { get; protected set; }

        protected override void Awake()
        {
            base.Awake();
            if (Selector == null)
            {
                Selector = _selector as ISelector;
            }
            _nativeId = 0x52617949746f7220;
        }

        protected override void Start()
        {
            base.Start();
            this.AssertField(Selector, nameof(Selector));
            this.AssertField(_rayOrigin, nameof(_rayOrigin));
        }

        protected override void DoPreprocess()
        {
            var rayTransform = _rayOrigin.transform;
            Origin = rayTransform.position;
            Rotation = rayTransform.rotation;
            Forward = rayTransform.forward;
            End = Origin + MaxRayLength * Forward;
            Ray = new Ray(Origin, Forward);
        }

        /// <summary>
        /// Data type encapsulating information about the candidate selection process.
        /// </summary>
        /// <remarks>
        /// This can be passed to candidate comparison mechanisms such as <see cref="ICandidateComparer"/> or be used for
        /// visualizations. It also has potential applications for debugging.
        /// </remarks>
        public class RayCandidateProperties : ICandidatePosition
        {
            /// <summary>
            /// The <see cref="RayInteractable"/> candidate associated with these properties. The name ClosestInteractable refers
            /// to the fact that this interactable is the closest to the interactor out of all the interactables considered.
            /// </summary>
            public RayInteractable ClosestInteractable { get; }

            /// <summary>
            /// Implementation of <see cref="ICandidatePosition.CandidatePosition"/>; for details, please refer to the related
            /// documentation provided for that interface.
            /// </summary>
            public Vector3 CandidatePosition { get; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="closestInteractable">The <see cref="RayInteractable"/> characterized as a candidate in this type</param>
            /// <param name="candidatePosition">
            /// The position from which <paramref name="closestInteractable"/>'s candidacy should be considered
            /// </param>
            public RayCandidateProperties(RayInteractable closestInteractable, Vector3 candidatePosition)
            {
                ClosestInteractable = closestInteractable;
                CandidatePosition = candidatePosition;
            }
        }

        /// <summary>
        /// Implementation of <see cref="Interactor{TInteractor, TInteractable}.CandidateProperties"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public override object CandidateProperties => _rayCandidateProperties;

        protected override RayInteractable ComputeCandidate()
        {
            CollisionInfo = null;

            RayInteractable closestInteractable = null;
            float closestDist = float.MaxValue;
            Vector3 candidatePosition = Vector3.zero;
            var interactables = RayInteractable.Registry.List(this);

            foreach (RayInteractable interactable in interactables)
            {
                if (interactable.Raycast(Ray, out SurfaceHit hit, MaxRayLength, false))
                {
                    bool equal = Mathf.Abs(hit.Distance - closestDist) < _equalDistanceThreshold;
                    if ((!equal && hit.Distance < closestDist) ||
                        (equal && ComputeCandidateTiebreaker(interactable, closestInteractable) > 0))
                    {
                        closestDist = hit.Distance;
                        closestInteractable = interactable;
                        CollisionInfo = hit;
                        candidatePosition = hit.Point;
                    }
                }
            }

            float rayDist = (closestInteractable != null ? closestDist : MaxRayLength);
            End = Origin + rayDist * Forward;

            _rayCandidateProperties = new RayCandidateProperties(closestInteractable, candidatePosition);

            return closestInteractable;
        }

        protected override int ComputeCandidateTiebreaker(RayInteractable a, RayInteractable b)
        {
            int result = base.ComputeCandidateTiebreaker(a, b);
            if (result != 0)
            {
                return result;
            }

            return a.TiebreakerScore.CompareTo(b.TiebreakerScore);
        }

        protected override void InteractableSelected(RayInteractable interactable)
        {
            if (interactable != null && CollisionInfo.HasValue)
            {
                _movedHit = CollisionInfo.Value;
                Pose hitPose = new Pose(_movedHit.Point, Quaternion.LookRotation(_movedHit.Normal));
                Pose backHitPose = new Pose(_movedHit.Point, Quaternion.LookRotation(-_movedHit.Normal));
                _movement = interactable.GenerateMovement(_rayOrigin.GetPose(), backHitPose);
                if (_movement != null)
                {
                    _movementHitDelta = PoseUtils.Delta(_movement.Pose, hitPose);
                }
            }
            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(RayInteractable interactable)
        {
            if (_movement != null)
            {
                _movement.StopAndSetPose(_movement.Pose);
            }
            base.InteractableUnselected(interactable);
            _movement = null;
        }

        protected override void DoHoverUpdate()
        {
            TryUpdateCollisionInfoHit(Interactable);
            base.DoHoverUpdate();
        }

        protected override void DoSelectUpdate()
        {
            RayInteractable interactable = _selectedInteractable;

            if (_movement != null)
            {
                _movement.UpdateTarget(_rayOrigin.GetPose());
                _movement.Tick();
                Pose hitPoint = PoseUtils.Multiply(_movement.Pose, _movementHitDelta);
                _movedHit.Point = hitPoint.position;
                _movedHit.Normal = hitPoint.forward;
                CollisionInfo = _movedHit;
                End = _movedHit.Point;
                return;
            }

            TryUpdateCollisionInfoHit(interactable);
        }

        private bool TryUpdateCollisionInfoHit(RayInteractable interactable)
        {
            CollisionInfo = null;
            if (interactable != null &&
                interactable.Raycast(Ray, out SurfaceHit hit, MaxRayLength, true))
            {
                CollisionInfo = hit;
                End = hit.Point;
                return true;
            }
            else
            {
                End = Origin + MaxRayLength * Forward;
            }
            return false;
        }

        protected override Pose ComputePointerPose()
        {
            if (_movement != null)
            {
                return _movement.Pose;
            }

            if (CollisionInfo != null)
            {
                Vector3 position = CollisionInfo.Value.Point;
                Quaternion rotation = Quaternion.LookRotation(CollisionInfo.Value.Normal);
                return new Pose(position, rotation);
            }
            return Pose.identity;
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated RayInteractor; effectively wraps
        /// <see cref="InjectSelector(ISelector)"/> and <see cref="InjectRayOrigin(Transform)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllRayInteractor(ISelector selector, Transform rayOrigin)
        {
            InjectSelector(selector);
            InjectRayOrigin(rayOrigin);
        }

        /// <summary>
        /// Sets an <see cref="ISelector"/> for a dynamically instantiated RayInteractor. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        /// <summary>
        /// Sets a Unity Transform representing the raycast origin and direction for a dynamically instantiated RayInteractor. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRayOrigin(Transform rayOrigin)
        {
            _rayOrigin = rayOrigin;
        }

        /// <summary>
        /// Sets the equal distance threshold for a dynamically instantiated RayInteractor. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalEqualDistanceThreshold(float equalDistanceThreshold)
        {
            _equalDistanceThreshold = equalDistanceThreshold;
        }
        #endregion
    }
}
