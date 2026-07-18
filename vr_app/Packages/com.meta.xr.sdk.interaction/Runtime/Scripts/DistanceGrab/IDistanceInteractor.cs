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
    /// Interface for Objects that can be interacted with <see cref="IDistanceInteractable"/>. contains attributes for
    /// the origin of the grab, the point of the grab and the a reference to the interactable object.
    /// </summary>
    public interface IDistanceInteractor : IInteractorView
    {
        /// <summary>
        /// Used to retrieves the pose (position and orientation) from which distance grabbing is calculated. This value comes from the
        /// <see cref="DistantCandidateComputer"/>, but conceptually it is a point relative to the hand
        /// around which it feels natural for the hand to grab.
        /// </summary>
        Pose Origin { get; }

        /// <summary>
        /// The point in space from which the interactor is considered to have "hit" its current interactable.
        /// Similar to <see cref="RayInteractor.CollisionInfo"/>, though the it is not the result of a simple raycast and instead
        /// results from calculations in the <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/>.
        /// This provides a precise location for interaction caculations.
        /// </summary>
        Vector3 HitPoint { get; }

        /// <summary>
        /// The main Transform of the currently selected <see cref="DistanceGrabInteractable"/>.
        /// This property links back to the interactable object that is currently being manipulated by the user, allowing for continued interaction.
        /// Can be null if there is no object being interacted with.
        /// </summary>
        IRelativeToRef DistanceInteractable { get; }
    }
}
