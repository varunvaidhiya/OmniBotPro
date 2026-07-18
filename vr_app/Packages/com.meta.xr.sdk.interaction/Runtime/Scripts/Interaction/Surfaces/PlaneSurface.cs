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

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// A representation of an infinite plane which can serve as a surface for interactions. Rarely used on
    /// its own, but frequently used as an underlying component of <see cref="ClippedPlaneSurface"/>s.
    /// </summary>
    /// <remarks>
    /// While it is uncommon for a PlaneSurface to be used on its own (because few interactable elements can
    /// be considered infinite planes), it is quite common for multiple different <see cref="ClippedPlaneSurface"/>s
    /// to exist on the same plane: consider multiple buttons on a single interactable UI. In such a scenario, many
    /// <see cref="ClippedPlaneSurface"/>s can be backed by the same PlaneSurface, ensuring that they always remain
    /// coplanar regardless of edits to the underlying surface.
    /// </remarks>
    public class PlaneSurface : MonoBehaviour, ISurface, IBounds
    {
        /// <summary>
        /// Used for interaction with flat surfaces, and acts in much the same way as Unity’s Plane.
        /// </summary>
        public enum NormalFacing
        {
            /// <summary>
            /// Normal faces along the transform's negative Z axis
            /// </summary>
            Backward,

            /// <summary>
            /// Normal faces along the transform's positive Z axis
            /// </summary>
            Forward,
        }

        /// <summary>
        /// The direction the normal faces. If Forward,  the normal faces positive Z. If Backward, the normal faces negative Z.
        /// </summary>
        [Tooltip("The normal facing of the surface. Hits will be " +
            "registered either on the front or back of the plane " +
            "depending on this value.")]
        [SerializeField]
        private NormalFacing _facing = NormalFacing.Backward;

        /// <summary>
        /// Determines whether raycasts will hit both sides of the plane. Note that the raycast hit normal will respect Facing regardless of this setting.
        /// </summary>
        [SerializeField, Tooltip("Raycasts hit either side of plane, but hit normal " +
        "will still respect plane facing.")]
        private bool _doubleSided = false;

        /// <summary>
        /// Gets or sets the direction (<see cref="NormalFacing.Forward"/> or <see cref="NormalFacing.Backward"/>) for
        /// this plane.
        /// </summary>
        public NormalFacing Facing
        {
            get => _facing;
            set => _facing = value;
        }

        /// <summary>
        /// Determines whether this plane is double-sided. If true, this plane can be interacted with from either side;
        /// otherwise, the plane is only considered to exist if perceived from the direction of its normal.
        /// </summary>
        public bool DoubleSided
        {
            get => _doubleSided;
            set => _doubleSided = value;
        }

        /// <summary>
        /// Returns either the forward direction or the backward direction depending on whether <see cref="Facing"/> is
        /// <see cref="NormalFacing.Forward"/> or <see cref="NormalFacing.Backward"/>, respectively.
        /// </summary>
        public Vector3 Normal
        {
            get
            {
                return _facing == NormalFacing.Forward ?
                                  transform.forward :
                                  -transform.forward;
            }
        }

        /// <summary>
        /// Implementation of <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();
            GetPlaneParameters(out Vector3 planeNormal, out float planeDistance);
            float hitDistance = Vector3.Dot(planeNormal, point) + planeDistance;
            float absDistance = Mathf.Abs(hitDistance);
            if (maxDistance > 0 && absDistance > maxDistance)
            {
                return false;
            }

            hit.Point = point - planeNormal * hitDistance;
            hit.Distance = absDistance;
            hit.Normal = planeNormal.normalized;

            return true;

        }

        /// <summary>
        /// Implementation of <see cref="ISurface.Transform"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// Implementation of <see cref="IBounds.Bounds"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                Vector3 normal = Normal;
                Vector3 size = new Vector3(
                    Mathf.Abs(normal.x) == 1f ? float.Epsilon : float.PositiveInfinity,
                    Mathf.Abs(normal.y) == 1f ? float.Epsilon : float.PositiveInfinity,
                    Mathf.Abs(normal.z) == 1f ? float.Epsilon : float.PositiveInfinity);
                return new Bounds(transform.position, size);
            }
        }

        /// <summary>
        /// Implementation of <see cref="ISurface.Raycast(in Ray, out SurfaceHit, float)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();

            GetPlaneParameters(out Vector3 planeNormal, out float planeDistance);
            float originDistance = Vector3.Dot(planeNormal, ray.origin) + planeDistance;
            if (!_doubleSided && originDistance <= 0)
            {
                return false;
            }

            if (Raycast(ray, out float hitDistance))
            {
                if (maxDistance > 0 && hitDistance > maxDistance)
                {
                    return false;
                }
                hit.Point = ray.GetPoint(hitDistance);
                hit.Distance = hitDistance;
                hit.Normal = planeNormal.normalized;
                return true;
            }

            return false;

            bool Raycast(in Ray ray, out float enter)
            {
                float num = Vector3.Dot(ray.direction, planeNormal);
                if (Mathf.Approximately(num, 0f))
                {
                    enter = 0f;
                    return false;
                }
                enter = -originDistance / num;
                return enter > 0f;
            }
        }

        private static Vector3 _forward = Vector3.forward;
        private static Vector3 _back = Vector3.back;

        private void GetPlaneParameters(out Vector3 planeNormal, out float planeDistance)
        {
            transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
            planeNormal = rotation * (_facing == NormalFacing.Forward ? _forward : _back);
            planeDistance = -Vector3.Dot(planeNormal, position);
        }

        /// <summary>
        /// Gets a Unity Plane representation of the mathematical plane represented by this PlaneSurface.
        /// </summary>
        public Plane GetPlane()
        {
            return new Plane(Normal, transform.position);
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated PlaneSurface; effectively wraps
        /// <see cref="InjectNormalFacing(NormalFacing)"/> and <see cref="InjectDoubleSided(bool)"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical
        /// Unity Editor-based usage.
        /// </summary>
        public void InjectAllPlaneSurface(NormalFacing facing,
            bool doubleSided)
        {
            InjectNormalFacing(facing);
            InjectDoubleSided(doubleSided);
        }

        /// <summary>
        /// Sets the <see cref="NormalFacing"/> value for a dynamically instantiated PlaneSurface. This method exists
        /// to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectNormalFacing(NormalFacing facing)
        {
            _facing = facing;
        }

        /// <summary>
        /// Sets whether a dynamically instantiated PlaneSurface should be interactable from both sides. This method exists
        /// to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectDoubleSided(bool doubleSided)
        {
            _doubleSided = doubleSided;
        }

        #endregion
    }
}
