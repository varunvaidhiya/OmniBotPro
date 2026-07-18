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
using System.Collections.Generic;
using System.Linq;

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// The primary flat surface used by the Interaction SDK, this is an <see cref="ISurface"/> constructed by
    /// "clipping" a <see cref="PlaneSurface"/> down to a subsection of itself using one or more
    /// <see cref="IBoundsClipper"/>s.
    /// </summary>
    /// <remarks>
    /// ClippedPlaneSurfaces apply the logical AND of all their contained <see cref="IBoundsClipper"/>s to the
    /// underlying <see cref="PlaneSurface"/>. Because <see cref="IBoundsClipper"/>s are axis-aligned in the
    /// space of the plane being clipped, this means all ClippedPlaneSurfaces will be rectangular subsections
    /// of the XY plane of the transform of the <see cref="PlaneSurface"/> they clip. They are not necessarily
    /// centered on this transform, however.
    /// </remarks>
    public class ClippedPlaneSurface : MonoBehaviour, IClippedSurface<IBoundsClipper>
    {
        private static readonly Bounds InfiniteBounds = new Bounds(Vector3.zero,
            Vector3.one * float.PositiveInfinity);

        private static readonly Bounds PlaneBounds = new Bounds(Vector3.zero,
            new Vector3(float.PositiveInfinity, float.PositiveInfinity, Vector3.kEpsilon));

        [Tooltip("The Plane Surface to be clipped.")]
        [SerializeField]
        private PlaneSurface _planeSurface;

        [Tooltip("The clippers that will be used to clip the Plane Surface.")]
        [SerializeField, Interface(typeof(IBoundsClipper))]
        private List<UnityEngine.Object> _clippers = new List<UnityEngine.Object>();
        private List<IBoundsClipper> Clippers { get; set; }

        /// <summary>
        /// Implementation of <see cref="ISurfacePatch.BackingSurface"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public ISurface BackingSurface => _planeSurface;

        /// <summary>
        /// Implementation of <see cref="ISurface.Transform"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public Transform Transform => _planeSurface.Transform;

        /// <summary>
        /// Implementation of <see cref="IClippedSurface{TClipper}.GetClippers"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public IReadOnlyList<IBoundsClipper> GetClippers()
        {
            if (Clippers != null)
            {
                return Clippers;
            }
            else
            {
                return _clippers.ConvertAll(clipper => clipper as IBoundsClipper);
            }
        }

        protected virtual void Awake()
        {
            Clippers = _clippers.ConvertAll(clipper => clipper as IBoundsClipper);
        }

        protected virtual void Start()
        {
            this.AssertField(_planeSurface, nameof(_planeSurface));
            this.AssertCollectionItems(Clippers, nameof(Clippers));
        }

        /// <summary>
        /// Clip a provided Bounds using the ClippedPlaneSurface's <see cref="IBoundsClipper"/>s.
        /// Comparable to applying the logical AND of the input <paramref name="bounds"/> with all
        /// the clippers returned by <see cref="GetClippers"/>.
        /// </summary>
        /// <param name="bounds">The Bounds to clip</param>
        /// <param name="clipped">The clipped result</param>
        /// <returns>True if resulting bounds are contain any space, false if the clipped bounds have no volume</returns>
        public bool ClipBounds(in Bounds bounds, out Bounds clipped)
        {
            clipped = bounds;

            IReadOnlyList<IBoundsClipper> clippers = GetClippers();
            for (int i = 0; i < clippers.Count; i++)
            {
                IBoundsClipper clipper = clippers[i];
                if (clipper == null ||
                    !clipper.GetLocalBounds(Transform, out Bounds clipTo))
                {
                    continue;
                }

                if (!clipped.Clip(clipTo, out clipped))
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 ClampPoint(in Vector3 point, in Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 localPoint = Transform.InverseTransformPoint(point);

            Vector3 clamped = new Vector3(
                Mathf.Clamp(localPoint.x, min.x, max.x),
                Mathf.Clamp(localPoint.y, min.y, max.y),
                Mathf.Clamp(localPoint.z, min.z, max.z));

            return Transform.TransformPoint(clamped);
        }

        /// <summary>
        /// Implementation of <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            if (_planeSurface.ClosestSurfacePoint(point, out hit, maxDistance) &&
                ClipBounds(PlaneBounds, out Bounds clippedPlane))
            {
                hit.Point = ClampPoint(hit.Point, clippedPlane);
                hit.Distance = Vector3.Distance(point, hit.Point);
                return maxDistance <= 0 || hit.Distance <= maxDistance;
            }
            return false;
        }

        /// <summary>
        /// Implementation of <see cref="ISurface.Raycast(in Ray, out SurfaceHit, float)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
        {
            return BackingSurface.Raycast(ray, out hit, maxDistance) &&
                   ClipBounds(InfiniteBounds, out Bounds clipBounds) &&
                   clipBounds.size != Vector3.zero &&
                   clipBounds.Contains(Transform.InverseTransformPoint(hit.Point));
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated ClippedPlaneSurface; effectively wraps
        /// <see cref="InjectPlaneSurface(PlaneSurface)"/> and <see cref="InjectClippers(IEnumerable{IBoundsClipper})"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical
        /// Unity Editor-based usage.
        /// </summary>
        public void InjectAllClippedPlaneSurface(
            PlaneSurface planeSurface,
            IEnumerable<IBoundsClipper> clippers)
        {
            InjectPlaneSurface(planeSurface);
            InjectClippers(clippers);
        }

        /// <summary>
        /// Sets the underlying <see cref="PlaneSurface"/> for a dynamically instantiated ClippedPlaneSurface. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectPlaneSurface(PlaneSurface planeSurface)
        {
            _planeSurface = planeSurface;
        }

        /// <summary>
        /// Sets the <see cref="IBoundsClipper"/>s for a dynamically instantiated ClippedPlaneSurface. This method exists
        /// to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectClippers(IEnumerable<IBoundsClipper> clippers)
        {
            _clippers = new List<UnityEngine.Object>(
                clippers.Select(c => c as UnityEngine.Object));
            Clippers = clippers.ToList();
        }

        #endregion
    }
}
