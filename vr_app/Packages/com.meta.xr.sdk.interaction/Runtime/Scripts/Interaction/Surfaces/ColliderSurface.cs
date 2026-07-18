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
    /// The primary solid surface used by the Interaction SDK, this is an <see cref="ISurface"/> constructed using
    /// a Unity Collider to characterize the topology of the surface.
    /// </summary>
    /// <remarks>
    /// This type is particularly useful for making existing in-scene objects interactable by simply connecting
    /// providing a ColliderSurface to represent their existing colliders in Interaction SDK logic.
    /// </remarks>
    public class ColliderSurface : MonoBehaviour, ISurface, IBounds
    {
        /// <summary>
        /// The Surface will be represented by this collider.
        /// </summary>
        [Tooltip("The Surface will be represented by this collider.")]
        [SerializeField]
        private Collider _collider;

        protected virtual void Start()
        {
            this.AssertField(_collider, nameof(_collider));
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
        public Bounds Bounds => _collider.bounds;

        /// <summary>
        /// Implementation of <see cref="ISurface.Raycast(in Ray, out SurfaceHit, float)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
        {
            hit = new SurfaceHit();

            RaycastHit hitInfo;

            if (_collider.Raycast(ray, out hitInfo,
                maxDistance <= 0 ? float.MaxValue : maxDistance))
            {
                hit.Point = hitInfo.point;
                hit.Normal = hitInfo.normal;
                hit.Distance = hitInfo.distance;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Implementation of <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            const float epsilon = Vector3.kEpsilon * Vector3.kEpsilon;

            Vector3 closest = _collider.ClosestPoint(point);
            Vector3 delta = closest - point;
            if (Vector3.SqrMagnitude(delta) > epsilon)
            {
                return Raycast(new Ray(point, delta), out hit, maxDistance);
            }
            else // Too small to normalize
            {
                Vector3 direction = _collider.bounds.center - point;
                return Raycast(new Ray(point - direction,
                    direction), out hit, float.MaxValue);
            }
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated ColliderSurface; effectively wraps <see cref="InjectCollider(Collider)"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllColliderSurface(Collider collider)
        {
            InjectCollider(collider);
        }

        /// <summary>
        /// Sets Unity Collider for a dynamically instantiated ColliderSurface. This method exists
        /// to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectCollider(Collider collider)
        {
            _collider = collider;
        }

        #endregion
    }
}
