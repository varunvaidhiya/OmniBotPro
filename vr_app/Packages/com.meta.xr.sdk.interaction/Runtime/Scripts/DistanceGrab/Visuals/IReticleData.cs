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

namespace Oculus.Interaction.DistanceReticles
{
    /// <summary>
    /// Interface for data instances pertaining to reticles --- visual indications of interactor targeting, such as a
    /// pointer highlight to be drawn at the end of a raycast for a <see cref="RayInteractor"/> --- which need to
    /// process the positions of raycast hits in order to position themselves correctly.
    /// </summary>
    public interface IReticleData
    {
        /// <summary>
        /// Tells this IReticleData the position of the most recent hit point; the IReticleData should use this hit
        /// point as the basis for its rendering.
        /// </summary>
        /// <param name="hitPoint">The most recent position relative to which the reticle should render</param>
        /// <returns>
        /// A potentially modified hit point, depending on whether the specific IReticleData implementation chooses
        /// to render elsewhere from the specified <paramref name="hitPoint"/> (see
        /// <see cref="ReticleDataTeleport.ProcessHitPoint(Vector3)"/> for an example).
        /// </returns>
        Vector3 ProcessHitPoint(Vector3 hitPoint);
    }
}
