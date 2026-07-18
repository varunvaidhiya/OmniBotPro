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

namespace Oculus.Interaction.Surfaces
{
    /// <summary>
    /// This interface represents a surface as finite subsections or "patches" of mathematically simpler but less
    /// bounded surfaces. For example, the screen of a virtual tablet can be thought of as a "patch" of an unbounded mathematical
    /// plane. This representation is convenient for a number of scenarios, such as continuing an interaction which began within
    /// a "patch" even if it continues outside the "patch" but without departing the underlying surface. ISurfacePatch formalizes
    /// this type of surface as an <see cref="ISurface"/> in its own right, but containing an additional (canonically broader)
    /// <see cref="BackingSurface"/>.
    /// </summary>
    public interface ISurfacePatch : ISurface
    {
        /// <summary>
        /// The backing surface for this ISurfacePatch. Used correctly, this backing surface is a strict geometric superset of the
        /// space defined by ISurfacePatch itself. In other words, for every spatial query against the ISurfacePatch for which a
        /// solution exists (a raycast that hits the surface, for example), that same solution should be a valid solution to the same
        /// query for the BackingSurface; however, there may exist spatial queries for which the ISurfacePatch has no solution, but
        /// a solution exists for the BackingSurface.
        /// </summary>
        ISurface BackingSurface { get; }
    }
}
