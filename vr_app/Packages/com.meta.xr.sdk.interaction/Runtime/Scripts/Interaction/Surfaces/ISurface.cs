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
    /// This interface represents a surface (two-dimensional topology) in 3D space in a very generalized way. A wide variety of
    /// of surfaces can be represented in this way (planes, meshes, parametric curves, etc.), allowing logic built on top of this
    /// interface to be extremely versatile. Many Interaction SDK features make use of ISurface for their internal logic, and
    /// you should use this interface over less general types whenever practical.
    /// </summary>
    public interface ISurface
    {
        /// <summary>
        /// A 3D transform associated with the surface. This transform defines a space which serves as a consistent frame of
        /// reference for operations involving the surface, which is particular important for surfaces which can move. For example,
        /// consider the screen of a virtual tablet which can be held and manipulated, but also touched and poked. Even as the
        /// tablet moves through space, poke interactions on its screen should be calculated from the screen's perspective, not from
        /// the world's perspective: if a finger is held still and the tablet moved forward to touch it, from the tablet's perspective
        /// the finger moved to the screen, and thus the interaction is a poke. This transform provides the frame of reference that
        /// facilitates these sorts of calculations.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Raycast to the surface with an optional maximum distance value. The arguments and outputs of this operation are in world
        /// space.
        /// </summary>
        /// <param name="ray">The ray to cast, with position and direction both defined in world space</param>
        /// <param name="hit">The returned hit data in world space if the raycast hit the surface, default otherwise</param>
        /// <param name="maxDistance">If greater than zero, maximum distance of raycast; if zero, distance is unconstrained</param>
        /// <returns>True if the raycast hit the surface, false otherwise</returns>
        bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0);

        /// <summary>
        /// Find the nearest point to the surface. The arguments and outputs of this operation are in world space.
        /// </summary>
        /// <param name="point">The point, in world space, for which the nearest point on the surface must be found</param>
        /// <param name="hit">The returned hit data in world space if a nearest point could be found, default otherwise</param>
        /// <param name="maxDistance">If greater than zero, maximum distance of check; if zero distance is unconstrained</param>
        /// <returns>True if the racyast hit the surface, false otherwise</returns>
        bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0);
    }

    /// <summary>
    /// This struct contains the result of a spatial query against an <see cref="ISurface"/>, such as an
    /// <see cref="ISurface.Raycast(in Ray, out SurfaceHit, float)"/> or a
    /// <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/> call.
    /// </summary>
    public struct SurfaceHit
    {
        /// <summary>
        /// The position on the surface, in world space, where the solution to the query was found.
        /// </summary>
        public Vector3 Point { get; set; }

        /// <summary>
        /// The normal of the surface, in world space, at the position where the solution to the query was found.
        /// </summary>
        public Vector3 Normal { get; set; }

        /// <summary>
        /// The distance, in world space, from the origin of the query to the position where the solution was found. For a raycast,
        /// this is the distance from <see cref="Ray.origin"/> to <see cref="Point"/> of the result, whereas for a
        /// <see cref="ISurface.ClosestSurfacePoint(in Vector3, out SurfaceHit, float)"/> this is the distance from the first argument
        /// to the <see cref="Point"/> of the result.
        /// </summary>
        public float Distance { get; set; }
    }
}
