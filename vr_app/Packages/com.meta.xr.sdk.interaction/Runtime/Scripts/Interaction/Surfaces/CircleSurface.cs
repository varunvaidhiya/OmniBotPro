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
    /// Used for interaction with circular surfaces. Computes the closest world point on a coordinate plane defined by the X and Y axes of the transform, within a provided radius from the transform’s origin.
    /// </summary>
    public class CircleSurface : MonoBehaviour, ISurfacePatch
    {
        /// <summary>
        /// The circle will lay upon this plane, with the circle's center at the plane surface's origin.
        /// </summary>
        [Tooltip("The circle will lay upon this plane, with " +
            "the circle's center at the plane surface's origin.")]
        [SerializeField]
        private PlaneSurface _planeSurface;

        /// <summary>
        /// The radius of the circle, scaled by the lossy scale of the transform.
        /// </summary>
        [Tooltip("The radius of the circle.")]
        [SerializeField]
        private float _radius = 0.1f;

        public Transform Transform => _planeSurface.Transform;

        public ISurface BackingSurface => _planeSurface;

        protected virtual void Start()
        {
            this.AssertField(_planeSurface, nameof(_planeSurface));
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
        {
            if (!_planeSurface.Raycast(ray, out hit, maxDistance))
            {
                return false;
            }

            Vector3 hitPointLocal = Transform.InverseTransformPoint(hit.Point);
            return Vector3.SqrMagnitude(hitPointLocal) <= (_radius * _radius);
        }

        // Closest point to circle is computed by projecting point to the plane
        // the circle is on and then clamping to the circle
        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            if (!_planeSurface.ClosestSurfacePoint(point, out hit, maxDistance))
            {
                return false;
            }

            Vector3 hitPointLocal = Transform.InverseTransformPoint(hit.Point);
            Vector3 clampedLocal = Vector3.ClampMagnitude(hitPointLocal, _radius);
            Vector3 clampedWorld = Transform.TransformPoint(clampedLocal);

            hit.Point = clampedWorld;
            hit.Distance = Vector3.Distance(point, clampedWorld);

            return maxDistance <= 0 || hit.Distance <= maxDistance;
        }

        #region Inject
        [System.Obsolete("Use " + nameof(InjectAllCircleSurface) + " instead.")]
        public void InjectAllCircleProximityField(PlaneSurface planeSurface)
        {
            InjectAllCircleSurface(planeSurface);
        }

        public void InjectAllCircleSurface(PlaneSurface planeSurface)
        {
            InjectPlaneSurface(planeSurface);
        }

        public void InjectPlaneSurface(PlaneSurface planeSurface)
        {
            _planeSurface = planeSurface;
        }

        public void InjectOptionalRadius(float radius)
        {
            _radius = radius;
        }

        #endregion
    }
}
