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

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Defines the core functionality for calculating throw velocities in the Interaction SDK.
    /// This interface provides the essential method for computing release velocities of thrown objects.
    /// </summary>
    /// <remarks>
    /// Note: This interface is marked as obsolete. Use <see cref="Oculus.Interaction.Grabbable"/> instead.
    /// </remarks>
    [Obsolete("Use " + nameof(Grabbable) + " instead")]
    public interface IThrowVelocityCalculator
    {
        /// <summary>
        /// Calculates the release velocity for a thrown object based on its transform and recent motion history.
        /// </summary>
        /// <param name="objectThrown">The transform of the object being thrown.</param>
        /// <returns>Velocity information containing linear velocity, angular velocity, and origin point of the throw.</returns>
        ReleaseVelocityInformation CalculateThrowVelocity(Transform objectThrown);
    }
}
