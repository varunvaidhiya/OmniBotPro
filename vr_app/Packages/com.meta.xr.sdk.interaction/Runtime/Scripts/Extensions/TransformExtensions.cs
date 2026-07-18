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

namespace Oculus.Interaction
{
    /// <summary>
    /// A collection of utility functions expanding the functionality of Unity's built-in Transform type.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Transforms a 3D position from world space to local space, ignoring scaling.
        /// </summary>
        /// <remarks>
        /// This is useful for specialized operations which manipulate scale while performing calculations which would
        /// otherwise be affected by scale; for example usage, see the PanelwithManipulators sample.
        /// </remarks>
        public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
        {
            Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocal.MultiplyPoint3x4(position);
        }

        /// <summary>
        /// Transforms a 3D position from local space to world space, ignoring scaling.
        /// </summary>
        /// <remarks>
        /// This is useful for specialized operations which manipulate scale while performing calculations which would
        /// otherwise be affected by scale; for example usage, see the PanelwithManipulators sample.
        /// </remarks>
        public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
        {
            Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            return localToWorld.MultiplyPoint3x4(position);
        }

        /// <summary>
        /// Transforms a bounding box from local to world space.
        /// </summary>
        /// <param name="transform">Transfrom that <paramref name="bounds"/> is local to.</param>
        /// <param name="bounds">The bounds to transform, in local space.</param>
        /// <returns>The bounding box in world space.</returns>
        public static Bounds TransformBounds(this Transform transform, in Bounds bounds)
        {
            Bounds worldBounds = new Bounds();

            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            Vector3 min = transform.position;
            Vector3 max = transform.position;
            Matrix4x4 m = transform.localToWorldMatrix;

            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    float e = m[i, j] * boundsMin[j];
                    float f = m[i, j] * boundsMax[j];
                    min[i] += (e < f) ? e : f;
                    max[i] += (e < f) ? f : e;
                }
            }

            worldBounds.SetMinMax(min, max);
            return worldBounds;
        }

        /// <summary>
        /// Walks the hierarchy beneath <paramref name="parent"/> searching for a Transform whose GameObject is named
        /// <paramref name="name"/>.
        /// </summary>
        /// <remarks>
        /// This is a convience method equivalent to calling <see cref="FindChildRecursive(Transform, Predicate{Transform})"/>
        /// with a predicate that simply checks the name.
        /// </remarks>
        /// <param name="parent">The Transform which will serve as the root of the search.</param>
        /// <param name="name">The name for which to search.</param>
        /// <returns>
        /// The first child found with the requested name. If more than one candidate meets the requirement, there is
        /// no guarantee which of them will be returned. If there are no candidates, returns null.
        /// </returns>
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            return parent.FindChildRecursive((child) => child.name.Contains(name));
        }

        /// <summary>
        /// Walks the hierarchy beneath <paramref name="parent"/> searching for a Transform for which <paramref name="predicate"/>
        /// succeeds.
        /// </summary>
        /// <param name="parent">The Transform which will serve as the root of the search.</param>
        /// <param name="predicate">The predicate for which to search.</param>
        /// <returns>
        /// The first child found for which the predicate succeeds. If more than one candidate meets the requirement, there is
        /// no guarantee which of them will be returned. If there are no candidates, returns null.
        /// </returns>
        public static Transform FindChildRecursive(this Transform parent, Predicate<Transform> predicate)
        {
            foreach (Transform child in parent)
            {
                if (predicate.Invoke(child))
                {
                    return child;
                }

                Transform result = child.FindChildRecursive(predicate);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
