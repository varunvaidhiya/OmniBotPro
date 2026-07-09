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
    public class PhysicsLayerSurface : MonoBehaviour
        , ISurface
    {
        [SerializeField]
        [Tooltip("Collision layers to detect hits against. -1 includes all layers.")]
        private LayerMask _layerMask = -1;
        public LayerMask LayerMask
        {
            get
            {
                return _layerMask;
            }
            set
            {
                _layerMask = value;
            }
        }

        [SerializeField, Optional]
        [Tooltip("When using ClosestSurfacePoint, the maximum number of Colliders to check")]
        private int _closeCollidersCacheSize = 20;
        public int CloseCollidersCacheSize
        {
            get
            {
                return _closeCollidersCacheSize;
            }
            set
            {
                _closeCollidersCacheSize = value;
            }
        }

        private Collider[] _cachedCloseColliders;
        private SphereCollider _sphereCollider;

        public Transform Transform => null;

        protected virtual void Awake()
        {
            _sphereCollider = this.gameObject.AddComponent<SphereCollider>();
            _sphereCollider.isTrigger = true;
            _sphereCollider.enabled = false;
        }

        private void OnDestroy()
        {
            if (_sphereCollider != null)
            {
                Destroy(_sphereCollider);
            }
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit surfaceHit, float maxDistance = 0)
        {
            if (_cachedCloseColliders == null
                || _cachedCloseColliders.Length != _closeCollidersCacheSize)
            {
                _cachedCloseColliders = new Collider[_closeCollidersCacheSize];
            }

            float radius = maxDistance > 0 ? maxDistance : float.MaxValue;
            surfaceHit = default;

            int layerMask = _layerMask.value;
            int collisions = Physics.OverlapSphereNonAlloc(point, radius, _cachedCloseColliders, layerMask, QueryTriggerInteraction.Ignore);
            if (collisions == 0)
            {
                return false;
            }

            float minDistance = radius;
            bool pointFound = false;

            _sphereCollider.enabled = true;
            for (int i = 0; i < collisions; i++)
            {
                Collider collider = _cachedCloseColliders[i];

                _sphereCollider.radius = minDistance;
                bool penetrates = Physics.ComputePenetration(_sphereCollider, point, Quaternion.identity,
                    collider, collider.transform.position, collider.transform.rotation,
                    out Vector3 exitDirection, out _);

                if (penetrates
                    && collider.Raycast(new Ray(point, -exitDirection), out RaycastHit hit, minDistance))
                {
                    if (hit.distance < minDistance)
                    {
                        pointFound = true;
                        minDistance = hit.distance;
                        surfaceHit = new SurfaceHit()
                        {
                            Point = hit.point,
                            Normal = hit.normal,
                            Distance = hit.distance,
                        };
                    }
                }
            }
            _sphereCollider.enabled = false;

            return pointFound;
        }

        public bool Raycast(in Ray ray, out SurfaceHit surfaceHit, float maxDistance = 0)
        {
            int layerMask = _layerMask.value;
            float distance = maxDistance > 0 ? maxDistance : float.MaxValue;
            if (Physics.Raycast(ray, out RaycastHit hit, distance, layerMask, QueryTriggerInteraction.Ignore))
            {
                surfaceHit = new SurfaceHit()
                {
                    Point = hit.point,
                    Normal = hit.normal,
                    Distance = hit.distance,
                };
                return true;
            }

            surfaceHit = default;
            return false;
        }
    }
}
