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

using System.Collections.Generic;

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// An IClippedSurface is a specific type of <see cref="ISurfacePatch"/> in which the <see cref="ISurface"/> for both the
    /// patch itself and its <see cref="ISurfacePatch.BackingSurface"/> is the same, but where the solutions to spatial
    /// queries against the ISurfacePatch itself are constrained by spatial constraints characterized as <see cref="TClipper"/>s.
    /// The canonical (and most instructive) example of a TClipper is <see cref="BoundsClipper"/>, which confines solutions
    /// to spatial queries against an ISurface to an allowable region within certain spatial bounds; results outside of these bounds
    /// will either be rejected outright (for example, for <see cref="ISurface.Raycast(in UnityEngine.Ray, out SurfaceHit, float)"/>
    /// queries) or "clamped" to within the acceptable region (for example, for
    /// <see cref="ISurface.ClosestSurfacePoint(in UnityEngine.Vector3, out SurfaceHit, float)"/> queries). In this way, an
    /// IClippedSurface can be thought of as a larger surface which has been trimmed down or "clipped" to a subsection.
    /// </summary>
    /// <typeparam name="TClipper">A type of "trimming" mechanism used to constrain the allowable region of an ISurface</typeparam>
    public interface IClippedSurface<TClipper> : ISurfacePatch
    {
        /// <summary>
        /// Retrieves the list of "clipper" constraints which are used to confine the <see cref="ISurfacePatch.BackingSurface"/> to
        /// a specific allowable region which serves as the base <see cref="ISurface"/> of the <see cref="ISurfacePatch"/>.
        /// As a colloquial example, if the backing surface were defined as a plane with origin _v_, and GetClippers returned a
        /// constraint which mandated, "Only points within 10cm world space of _v_ are considered part of the surface," then the
        /// <see cref="ISurfacePatch"/> itself would be defined as the region of the backing surface which lies within 10cm of _v_ in
        /// world space.
        /// </summary>
        /// <returns>
        /// The list of <typeparamref name="TClipper"/>s which define the <see cref="ISurfacePatch"/> as a subset of its
        /// <see cref="ISurfacePatch.BackingSurface"/>.
        /// </returns>
        IReadOnlyList<TClipper> GetClippers();
    }
}
