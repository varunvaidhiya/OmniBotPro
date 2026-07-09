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
    /// This interface suggests that an implementing type is intended to serve as a TClipper type in an
    /// <see cref="IClippedSurface{TClipper}"/>. Specifically, this clipper type limits allowable results to a certain segment
    /// of a cylinder.
    /// </summary>
    public interface ICylinderClipper
    {
        /// <summary>
        /// Attempts to retrieve the cylinder segment corresponding to the allowed region of the clipped surface.
        /// </summary>
        /// <param name="segment">The cylinder segment to which the underlying <see cref="ISurface"/> should be clipped</param>
        /// <returns>True if clipping should be performed, false otherwise</returns>
        public bool GetCylinderSegment(out CylinderSegment segment);
    }

    /// <summary>
    /// This interface suggests than an implementing type is intended to serve as a TClipper type in an
    /// <see cref="IClippedSurface{TClipper}"/>. Specifically, this clipper type limits allowable results to those that lie within
    /// a certain spatial bounding box.
    /// </summary>
    public interface IBoundsClipper
    {
        /// <summary>
        /// Attempts to retrieve the bounding box corresponding to the allowed region of the clipped surface
        /// </summary>
        /// <param name="localTo">The transform defining the space in which the returned bounds should be expressed</param>
        /// <param name="bounds">The bounding box, expressed in the local space defined by <paramref name="localTo"/></param>
        /// <returns>True if clipping should be performed, false otherwise</returns>
        public bool GetLocalBounds(Transform localTo, out Bounds bounds);
    }
}
