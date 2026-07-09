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

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines a sequence of connected points in 3D space that form a polyline shape.
    /// This interface is commonly used for defining paths, boundaries, or trajectories
    /// in spatial interactions and surface definitions.
    /// </summary>
    /// <remarks>
    /// See <see cref="Oculus.Interaction.TransformsPolyline"/> and <see cref="Oculus.Interaction.Locomotion.TeleportArcGravity"/> for example implementations.
    /// </remarks>
    public interface IPolyline
    {
        /// <summary>
        /// Max number of points that define the <see cref="IPolyline"/>.
        /// </summary>
        /// <returns>The number of points in the polyline sequence.</returns>
        int PointsCount { get; }

        /// <summary>
        /// Calculates the position N vertex of the <see cref="IPolyline"/>.
        /// </summary>
        /// <param name="index">The N vertex of the polyline been queried.</param>
        /// <returns>The position of the polyline at the index-th point</returns>
        Vector3 PointAtIndex(int index);
    }
}
