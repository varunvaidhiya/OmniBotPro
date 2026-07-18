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

using System.Linq;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal static class Utils
    {
        /// <summary>
        /// Returns a bounds in local space of the provided GameObject, which encapsulates
        /// all of the GameObject's child renderers.
        /// </summary>
        internal static bool TryEncapsulateRenderers(GameObject obj, out Bounds localBounds)
        {
            // Supported render types to encapsulate
            var renderers = obj.GetComponentsInChildren<Renderer>().Where(
                r => r is MeshRenderer || r is SkinnedMeshRenderer || r is SpriteRenderer)
                .ToArray();

            if (renderers.Length == 0)
            {
                localBounds = default;
                return false;
            }

            Transform GetRendererTransform(Renderer renderer)
            {
                // Skinned mesh renderers do not report local bounds relative to root
                // though renderer bounds are used and visualized this way, so we
                // must treat them as local to the root bone.
                return (renderer is SkinnedMeshRenderer skinned &&
                    skinned.rootBone != null) ? skinned.rootBone : renderer.transform;
            }

            void EncapsulateRenderer(ref Bounds bounds, Renderer renderer)
            {
                void Encapsulate(ref Bounds bounds, Vector3 point)
                {
                    bounds.Encapsulate(obj.transform.InverseTransformPoint(
                        GetRendererTransform(renderer).TransformPoint(point)));
                }

                Vector3 center = renderer.localBounds.center;
                Vector3 extents = renderer.localBounds.extents;

                Encapsulate(ref bounds, center + extents);
                Encapsulate(ref bounds, center + new Vector3(-extents.x, extents.y, extents.z));
                Encapsulate(ref bounds, center + new Vector3(extents.x, extents.y, -extents.z));
                Encapsulate(ref bounds, center + new Vector3(-extents.x, extents.y, -extents.z));
                Encapsulate(ref bounds, center + new Vector3(extents.x, -extents.y, extents.z));
                Encapsulate(ref bounds, center + new Vector3(-extents.x, -extents.y, extents.z));
                Encapsulate(ref bounds, center + new Vector3(extents.x, -extents.y, -extents.z));
                Encapsulate(ref bounds, center - extents);
            }

            // Start with size zero bounding box at first renderer center
            localBounds = new Bounds(obj.transform.InverseTransformPoint(
                    GetRendererTransform(renderers[0]).TransformPoint(
                        renderers[0].localBounds.center)), Vector3.zero);

            // Expand bounds to encapsulate all renderers
            foreach (Renderer renderer in renderers)
            {
                EncapsulateRenderer(ref localBounds, renderer);
            }

            return true;
        }

        internal static Collider GenerateCollider(GameObject target)
        {
            Collider collider;
            if (Utils.TryEncapsulateRenderers(target,
                out Bounds localBounds))
            {
                var boxCollider = target.AddComponent<BoxCollider>();
                boxCollider.center = localBounds.center;
                boxCollider.size = localBounds.size;
                collider = boxCollider;
            }
            else if (target.TryGetComponent(out RectTransform rectTransform))
            {
                var boxCollider = target.AddComponent<BoxCollider>();
                boxCollider.center = rectTransform.rect.center;
                boxCollider.size = new Vector3(
                    rectTransform.rect.size.x,
                    rectTransform.rect.size.y,
                    0f
                );
                collider = boxCollider;
            }
            else
            {
                collider = target.AddComponent<SphereCollider>();
            }
            collider.isTrigger = true;
            return collider;
        }
    }

    internal static class MenuOrder
    {
        // Inputs and Interactors
        internal const int COMPREHENSIVE_RIG_NEW = 100;
        internal const int COMPREHENSIVE_RIG_ADD = 101;
        internal const int HAND_INTERACTORS = 102;
        internal const int CONTROLLER_INTERACTORS = 103;
        // 3D Interactions
        internal const int ORDER_GRAB = 110;
        internal const int RAY_GRAB = 111;
        internal const int DISTANCE_GRAB = 112;
        internal const int TELEPORT_LOCOMOTION = 115;
        // Canvas Interactions
        internal const int RAY_CANVAS = 120;
        internal const int POKE_CANVAS = 121;
    }
}
