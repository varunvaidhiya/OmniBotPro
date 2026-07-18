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

using Meta.XR.ImmersiveDebugger;
using System;
using System.Linq;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// MonoBehaviour component that provides bounding box data for a specific GameObject.
    /// This component is registered with the Immersive Debugger's Inspector system.
    /// </summary>
    internal class BoundingBoxGizmoProvider : MonoBehaviour
    {
        private string _targetObjectName;
        private GameObject _cachedTargetObject;
        private Transform _cachedTransform;

        // Caching for bounds calculation optimization
        private Bounds _cachedBounds;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;
        private bool _boundsCalculated = false;

        /// <summary>
        /// Sets up this provider to track a specific GameObject
        /// </summary>
        public void Setup(string targetObjectName)
        {
            _targetObjectName = targetObjectName;
            RefreshCache();
        }

        public Tuple<Pose, float, float, float> BoundingBoxData
        {
            get
            {
                if (string.IsNullOrEmpty(_targetObjectName))
                    return new Tuple<Pose, float, float, float>(Pose.identity, 1f, 1f, 1f);

                // Check if cached object is still valid
                if (_cachedTargetObject == null || _cachedTransform == null)
                {
                    RefreshCache();
                }

                if (_cachedTargetObject != null && _cachedTransform != null)
                {
                    // Check if transform has changed since last calculation
                    bool transformChanged = HasTransformChanged();

                    // Only recalculate bounds if transform changed or not calculated yet
                    if (!_boundsCalculated || transformChanged)
                    {
                        _cachedBounds = CalculateTighterBounds(_cachedTargetObject);
                        UpdateTransformCache();
                        _boundsCalculated = true;
                    }

                    Vector3 size = _cachedBounds.size;

                    // Create pose from bounds center with object's rotation
                    Pose boundsPose = new Pose(_cachedBounds.center, _cachedTransform.rotation);

                    return new Tuple<Pose, float, float, float>(
                        boundsPose,
                        size.x, // width
                        size.y, // height
                        size.z  // depth
                    );
                }

                // Return default box if object not found
                return new Tuple<Pose, float, float, float>(Pose.identity, 1f, 1f, 1f);
            }
        }

        /// <summary>
        /// Gets whether the target object exists (for determining if gizmo should be shown)
        /// </summary>
        public bool TargetExists
        {
            get
            {
                if (string.IsNullOrEmpty(_targetObjectName))
                    return false;
                var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                return allObjects.Any(obj => obj.name == _targetObjectName);
            }
        }

        public string TargetObject => _targetObjectName ?? "None";

        /// <summary>
        /// Calculate tighter bounds that only include the main object's components, not all children.
        /// This provides a more accurate bounding box size.
        /// </summary>
        private Bounds CalculateTighterBounds(GameObject target)
        {
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;

            // First try to get bounds from direct renderers (not all children)
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds rendererBounds;
                if (renderer.gameObject.activeInHierarchy)
                {
                    rendererBounds = renderer.bounds;
                }
                else
                {
                    rendererBounds = CalculateRendererBounds(renderer);
                }

                if (rendererBounds.size.magnitude > 0)
                {
                    combinedBounds = rendererBounds;
                    boundsInitialized = true;
                }
            }

            // If no direct renderer, try direct colliders
            if (!boundsInitialized)
            {
                Collider collider = target.GetComponent<Collider>();
                if (collider != null)
                {
                    Bounds colliderBounds;
                    if (collider.gameObject.activeInHierarchy)
                    {
                        colliderBounds = collider.bounds;
                    }
                    else
                    {
                        colliderBounds = CalculateColliderBounds(collider);
                    }

                    if (colliderBounds.size.magnitude > 0)
                    {
                        combinedBounds = colliderBounds;
                        boundsInitialized = true;
                    }
                }
            }

            // If still no bounds, fallback to all children (original method)
            if (!boundsInitialized)
            {
                return CalculateCombinedBounds(target);
            }

            return combinedBounds;
        }

        /// <summary>
        /// Calculate combined bounds from all Renderers and Colliders on the target object.
        /// This works even if the object is inactive by accessing components directly.
        /// </summary>
        private Bounds CalculateCombinedBounds(GameObject target)
        {
            Bounds combinedBounds = new Bounds();
            bool boundsInitialized = false;

            // Get bounds from Renderers (including inactive ones)
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                Bounds rendererBounds;
                if (renderer.gameObject.activeInHierarchy)
                {
                    rendererBounds = renderer.bounds;
                }
                else
                {
                    // Calculate bounds manually for inactive renderers
                    rendererBounds = CalculateRendererBounds(renderer);
                }

                if (rendererBounds.size.magnitude > 0)
                {
                    if (!boundsInitialized)
                    {
                        combinedBounds = rendererBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(rendererBounds);
                    }
                }
            }

            // Get bounds from Colliders (including inactive ones)
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                Bounds colliderBounds;
                if (collider.gameObject.activeInHierarchy)
                {
                    colliderBounds = collider.bounds;
                }
                else
                {
                    // Calculate bounds manually for inactive colliders
                    colliderBounds = CalculateColliderBounds(collider);
                }

                if (colliderBounds.size.magnitude > 0)
                {
                    if (!boundsInitialized)
                    {
                        combinedBounds = colliderBounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(colliderBounds);
                    }
                }
            }

            // If no bounds found, create a default 1x1x1 bounds around the object's position
            if (!boundsInitialized)
            {
                combinedBounds = new Bounds(target.transform.position, Vector3.one);
            }

            return combinedBounds;
        }

        /// <summary>
        /// Calculate bounds for a renderer, even if it's inactive
        /// </summary>
        private Bounds CalculateRendererBounds(Renderer renderer)
        {
            if (renderer == null) return new Bounds();

            // For mesh renderers, we can get the mesh bounds and transform them
            if (renderer is MeshRenderer meshRenderer)
            {
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var mesh = meshFilter.sharedMesh;
                    var bounds = mesh.bounds;

                    // Transform the bounds to world space
                    var transform = meshRenderer.transform;
                    var center = transform.TransformPoint(bounds.center);
                    var size = Vector3.Scale(bounds.size, transform.lossyScale);

                    return new Bounds(center, size);
                }
            }

            // For other renderer types, fallback to using the transform position
            return new Bounds(renderer.transform.position, Vector3.one);
        }

        /// <summary>
        /// Calculate bounds for a collider, even if it's inactive
        /// </summary>
        private Bounds CalculateColliderBounds(Collider collider)
        {
            if (collider == null) return new Bounds();

            // Different collider types have different bounds calculation methods
            switch (collider)
            {
                case BoxCollider boxCollider:
                    var center = boxCollider.transform.TransformPoint(boxCollider.center);
                    var size = Vector3.Scale(boxCollider.size, boxCollider.transform.lossyScale);
                    return new Bounds(center, size);

                case SphereCollider sphereCollider:
                    var sphereCenter = sphereCollider.transform.TransformPoint(sphereCollider.center);
                    var radius = sphereCollider.radius * Mathf.Max(sphereCollider.transform.lossyScale.x,
                                                                   sphereCollider.transform.lossyScale.y,
                                                                   sphereCollider.transform.lossyScale.z);
                    return new Bounds(sphereCenter, Vector3.one * radius * 2);

                case CapsuleCollider capsuleCollider:
                    var capsuleCenter = capsuleCollider.transform.TransformPoint(capsuleCollider.center);
                    var capsuleRadius = capsuleCollider.radius * Mathf.Max(capsuleCollider.transform.lossyScale.x, capsuleCollider.transform.lossyScale.z);
                    var capsuleHeight = capsuleCollider.height * capsuleCollider.transform.lossyScale.y;
                    return new Bounds(capsuleCenter, new Vector3(capsuleRadius * 2, capsuleHeight, capsuleRadius * 2));

                default:
                    // For other collider types, fallback to using the transform position
                    return new Bounds(collider.transform.position, Vector3.one);
            }
        }

        /// <summary>
        /// Refreshes the cached GameObject reference
        /// </summary>
        private void RefreshCache()
        {
            if (string.IsNullOrEmpty(_targetObjectName))
            {
                _cachedTargetObject = null;
                _cachedTransform = null;
                return;
            }

            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _cachedTargetObject = allObjects.FirstOrDefault(obj => obj.name == _targetObjectName);
            _cachedTransform = _cachedTargetObject?.transform;

            // Reset bounds calculation cache when object changes
            _boundsCalculated = false;
        }

        /// <summary>
        /// Checks if the transform has changed since last bounds calculation
        /// </summary>
        private bool HasTransformChanged()
        {
            if (_cachedTransform == null) return true;

            bool changed = _cachedTransform.position != _lastPosition ||
                          _cachedTransform.rotation != _lastRotation ||
                          _cachedTransform.localScale != _lastScale;

            return changed;
        }

        /// <summary>
        /// Updates the cached transform values for change detection
        /// </summary>
        private void UpdateTransformCache()
        {
            if (_cachedTransform != null)
            {
                _lastPosition = _cachedTransform.position;
                _lastRotation = _cachedTransform.rotation;
                _lastScale = _cachedTransform.localScale;
            }
        }

        /// <summary>
        /// Force refresh the cache (useful if the target object might have changed)
        /// </summary>
        public void InvalidateCache()
        {
            _cachedTargetObject = null;
            _cachedTransform = null;
            _boundsCalculated = false;
        }
    }
}
