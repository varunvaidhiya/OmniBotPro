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

namespace Oculus.Interaction.Locomotion
{
    public struct TeleportHit
    {
        public Transform relativeTo;

        public Vector3 Point
        {
            get
            {
                if (relativeTo == null)
                {
                    return _localPose.position;
                }

                return PoseUtils.Multiply(relativeTo.GetPose(), _localPose).position;
            }
        }

        public Vector3 Normal
        {
            get
            {
                if (relativeTo == null)
                {
                    return _localPose.rotation * Vector3.forward;
                }

                return PoseUtils.Multiply(relativeTo.GetPose(), _localPose).rotation * Vector3.forward;
            }
        }

        private Pose _localPose;

        public TeleportHit(Transform relativeTo, Vector3 position, Vector3 normal)
        {
            this.relativeTo = relativeTo;
            Pose worldSpacePose = new Pose(position, Quaternion.LookRotation(normal));
            if (relativeTo == null)
            {
                this._localPose = worldSpacePose;
            }
            else
            {
                this._localPose = PoseUtils.Delta(relativeTo.GetPose(), worldSpacePose);
            }
        }

        public static readonly TeleportHit DEFAULT =
            new TeleportHit()
            {
                relativeTo = null,
                _localPose = Pose.identity
            };
    }

    /// <summary>
    /// A target to which a <see cref="TeleportInteractor"/> can teleport. This class encapsulates the target-specific information
    /// the teleportation interaction needs in order to execute properly, including the location, targeting information, and optionally
    /// details about how the player should be positioned after teleport.
    /// </summary>
    public class TeleportInteractable : Interactable<TeleportInteractor, TeleportInteractable>
    {
        [SerializeField]
        [Tooltip("Indicates if the interactable is valid for teleport. Setting it to false can be convenient to block the arc.")]
        private bool _allowTeleport = true;
        /// <summary>
        ///  Indicates if the interactable is valid for teleport. Setting it to false can be convenient to block the arc (this will
        ///  cause <see cref="TeleportInteractor.HasValidDestination"/> to return false).
        /// </summary>
        public bool AllowTeleport
        {
            get
            {
                return _allowTeleport;
            }
            set
            {
                _allowTeleport = value;
            }
        }

        [SerializeField, Optional, ConditionalHide("_allowTeleport", true)]
        [Tooltip("An override for the Interactor EqualDistanceThreshold used when comparing the interactable against other interactables that does not allow teleport.")]
        private float _equalDistanceToBlockerOverride;
        /// <summary>
        /// An override for the Interactor EqualDistanceThreshold used when comparing the interactable against other interactables
        /// that do not allow teleport. This can allow an interactable to more easily "present itself as equidistant" to something else,
        /// increasing the ease of selecting it specifically in preference to other interactions.
        /// </summary>
        public float EqualDistanceToBlockerOverride
        {
            get
            {
                return _equalDistanceToBlockerOverride;
            }
            set
            {
                _equalDistanceToBlockerOverride = value;
            }
        }

        [SerializeField, Optional]
        [Tooltip("Establishes the priority when several interactables are hit at the same time (EqualDistanceThreshold) by the arc.")]
        private int _tieBreakerScore;
        /// <summary>
        /// Establishes the priority when several interactables are hit at the same time (EqualDistanceThreshold) by the arc.
        /// Overriding equivalence comparison with <see cref="EqualDistanceToBlockerOverride"/> will cause this value to be
        /// consulted more frequently.
        /// </summary>
        public int TieBreakerScore
        {
            get
            {
                return _tieBreakerScore;
            }
            set
            {
                _tieBreakerScore = value;
            }
        }

        [SerializeField, Interface(typeof(ISurface))]
        [Tooltip("Surface against which the interactor will check collision with the arc.")]
        private UnityEngine.Object _surface;
        /// <summary>
        /// The surface against which the interactor will check collisions with <see cref="TeleportInteractor.TeleportArc"/>.
        /// </summary>
        /// <remarks>
        /// By default, the precise collision point will be treated as the teleport target destination, which is useful when
        /// specifying a single large teleport target (such as the floor of a room) as a single interactable. However, this
        /// can be overridden by supplying a target point (see <see cref="TargetPose(Pose)"/>), which if
        /// provided will supersede the actual collision and allow the interactable to function as a "hotspot" target moving
        /// the player to a specific location.
        /// </remarks>
        public ISurface Surface { get; private set; }

        /// <summary>
        /// The bounds constraining the region from which this interactable can be targeted. This can be used to disallow
        /// targeting of an interactable from beyond a certain range.
        /// </summary>
        public IBounds SurfaceBounds { get; private set; }

        [Header("Target", order = -1)]
        [SerializeField, Optional]
        [Tooltip("A specific point in space where the player should teleport to.")]
        private Transform _targetPoint;

        [SerializeField, Optional]
        [Tooltip("When true, the player will also face the direction specified by the target point.")]
        private bool _faceTargetDirection;
        /// <summary>
        /// Specifies whether teleporting to this interactable should override the player's orientation to match that of the
        /// target point (see <see cref="TargetPose(Pose)"/>).
        /// </summary>
        /// <remarks>
        /// This is useful for scenarios such as a "hotspot" in front of a control panel, where the goal is to make it easy
        /// for the player to quickly stand in a certain spot facing a certain direction. Note that this setting is only meaningful
        /// if a target point is supplied.
        /// </remarks>
        public bool FaceTargetDirection
        {
            get
            {
                return _faceTargetDirection;
            }
            set
            {
                _faceTargetDirection = value;
            }
        }

        [SerializeField, Optional]
        [Tooltip("When true, instead of aligning the players feet to the TargetPoint it will align the head.")]
        private bool _eyeLevel;
        /// <summary>
        /// Allows overriding the default teleport behavior, which aligns the user's feet/floor with the teleport target, to instead
        /// align the user's head/eyes to the target.
        /// </summary>
        /// <remarks>
        /// This is useful for scenarios such as a "hotspot" in front of a window, where the goal is to make it easy
        /// for the player to quickly stand in a certain spot and see something specific. Note that this setting is only meaningful
        /// if a target point is supplied (see <see cref="TargetPose(Pose)"/>).
        /// </remarks>
        public bool EyeLevel
        {
            get
            {
                return _eyeLevel;
            }
            set
            {
                _eyeLevel = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            Surface = _surface as ISurface;
            SurfaceBounds = _surface as IBounds;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Surface, nameof(Surface));
            this.EndStart(ref _started);
        }

        /// <summary>
        /// Determines if a <see cref="TeleportInteractor"/> is close enough to target this interactable. This is
        /// called by internal logic of <see cref="TeleportInteractor"/> and should not be invoked independently.
        /// </summary>
        public bool IsInRange(in Pose origin, float maxSqrDistance)
        {
            if (SurfaceBounds == null)
            {
                return true;
            }

            Bounds bounds = SurfaceBounds.Bounds;
            Vector3 center = bounds.center;
            center.y = origin.position.y;

            Vector3 originToCenter = center - origin.position;
            float colliderSqrRadius = bounds.extents.x * bounds.extents.x
                + bounds.extents.z * bounds.extents.z;

            //point inside volume
            if (originToCenter.sqrMagnitude <= colliderSqrRadius)
            {
                return true;
            }

            //point radius too far away from volume radius
            float sqrDistanceToCenter = originToCenter.sqrMagnitude;
            if (!CheckSquaredDistances(sqrDistanceToCenter, colliderSqrRadius, maxSqrDistance))
            {
                return false;
            }

            //make the dir flat in XZ
            Vector3 dir = origin.forward;
            float invFactor = 1f / Mathf.Sqrt(1 - dir.y * dir.y);
            dir.y = 0;
            dir.x *= invFactor;
            dir.z *= invFactor;
            float sqrDistanceToDir = SqrDistanceToSegment(center, origin.position, dir, maxSqrDistance);
            if (sqrDistanceToDir <= colliderSqrRadius)
            {
                return true;
            }

            return false;

            //equivalent to sqrt(x) - sqrt(y) <= sqrt(threshold)
            bool CheckSquaredDistances(float x, float y, float threshold)
            {
                float num = x - y - threshold;
                if (x > y + threshold
                    && num * num > 4 * y * threshold)
                {
                    return false;
                }
                return true;
            }

            float SqrDistanceToSegment(Vector3 point, Vector3 origin, Vector3 dir, float sqrLength)
            {
                Vector3 startToPoint = point - origin;
                float t = Vector3.Dot(startToPoint, dir);

                if (t < 0f)
                {
                    t = 0f;
                }
                else if (t * t > sqrLength)
                {
                    t = Mathf.Sqrt(sqrLength);
                }

                Vector3 closestPoint = origin + dir * t;
                return (point - closestPoint).sqrMagnitude;
            }
        }

        /// <summary>
        /// Detects a hit from the teleport raycast on the object.
        /// </summary>
        /// <param name="from">The origin, in world space from which to cast the ray</param>
        /// <param name="to">The target, in world space, through which the cast ray should pass(in </param>
        /// <param name="hit">Information about the hit, if one occurred</param>
        /// <returns>True if a hit was detected, false otherwise</returns>
        public bool DetectHit(Vector3 from, Vector3 to, out TeleportHit hit)
        {
            Vector3 dir = to - from;
            Ray ray = new Ray(from, dir);
            if (Surface.Raycast(ray, out SurfaceHit surfaceHit, dir.magnitude))
            {
                hit = new TeleportHit(this.transform, surfaceHit.Point, surfaceHit.Normal);
                return true;
            }

            hit = TeleportHit.DEFAULT;
            return false;
        }

        /// <summary>
        /// Used when the interactor is looking for a teleport target. If you specify a target point, it'll use that.
        /// Otherwise it uses wherever the teleport arc is pointing.
        /// </summary>
        public Pose TargetPose(Pose hitPose)
        {
            Pose targetPose = hitPose;

            if (_targetPoint != null)
            {
                targetPose.position = _targetPoint.position;
                targetPose.rotation = _targetPoint.rotation;
            }

            return targetPose;
        }

        #region Inject
        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated TeleportInteractable; effectively wraps
        /// <see cref="InjectSurface(ISurface)"/>, since all other dependencies are optional. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllTeleportInteractable(ISurface surface)
        {
            InjectSurface(surface);
        }

        /// <summary>
        /// Sets an <see cref="ISurface"/> for a dynamically instantiated TeleportInteractable. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSurface(ISurface surface)
        {
            _surface = surface as UnityEngine.Object;
            Surface = surface;
            SurfaceBounds = surface as IBounds;
        }

        /// <summary>
        /// Sets the target point (see <see cref="TargetPose(Pose)"/> for a dynamically instantiated TeleportInteractable. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalTargetPoint(Transform targetPoint)
        {
            _targetPoint = targetPoint;
        }
        #endregion
    }
}
