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
    /// Implementation of <see cref="IBoundsClipper"/> specifically for use in <see cref="ClippedPlaneSurface"/>.
    /// Instances of this class encapsulate the "clipping" (comparable to a "crop" operation in image editing)
    /// to be applied to an <see cref="ISurface"/> in order to constrain it.
    /// </summary>
    public class BoundsClipper : MonoBehaviour, IBoundsClipper
    {
        /// <summary>
        /// The position offset from transform origin in local space.
        /// </summary>
        [Tooltip("The offset of the bounding box center relative to " +
            "the transform origin, in local space.")]
        [SerializeField]
        private Vector3 _position = Vector3.zero;

        /// <summary>
        /// The size of the bounding box in local space.
        /// </summary>
        [Tooltip("The size of the bounding box in local space.")]
        [SerializeField]
        private Vector3 _size = Vector3.one;

        /// <summary>
        /// The offset of the bounding box center relative to the transform origin of the surface to be clipped, in the
        /// local space of the surface.
        /// </summary>
        /// <remarks>
        /// This is used with <see cref="Size"/> to "clip" large (often infinite) surfaces to specific positioned
        /// regions; for the canonical example usage, see
        /// <see cref="ClippedPlaneSurface"/>.
        /// </remarks>
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// The size of the bounding box, in the local space of the surface.
        /// </summary>
        /// <remarks>
        /// This is used with <see cref="Position"/> to "clip" large (often infinite) surfaces to specific regions; for
        /// the canonical example usage, see <see cref="ClippedPlaneSurface"/>.
        /// </remarks>
        public Vector3 Size
        {
            get => _size;
            set => _size = value;
        }

        /// <summary>
        /// Implementation of <see cref="IBoundsClipper.GetLocalBounds(Transform, out Bounds)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        /// <param name="localTo"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool GetLocalBounds(Transform localTo, out Bounds bounds)
        {
            Vector3 localPos = localTo.InverseTransformPoint(
                transform.TransformPoint(Position));
            Vector3 localSize = localTo.InverseTransformVector(
                transform.TransformVector(_size));
            bounds = new Bounds(localPos, localSize);
            return isActiveAndEnabled;
        }
    }
}
