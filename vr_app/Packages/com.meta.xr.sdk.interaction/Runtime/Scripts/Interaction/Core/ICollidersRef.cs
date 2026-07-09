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
    /// Provides access to a collection of Unity Collider components that define physical interaction boundaries in the Interaction SDK.
    /// This interface is implemented by an <see cref="Interactable{TInteractor,TInteractable}"/> to define its grabbable surface and maintain a reference to each collider.
    /// See <see cref="Oculus.Interaction.HandGrab.HandGrabInteractable"/> and <see cref="Oculus.Interaction.DistanceGrabInteractable"/> for examples of usage.
    /// </summary>
    public interface ICollidersRef
    {
        /// <summary>
        /// Gets an array of Unity Collider components associated with this reference.
        /// Each collider in the array contributes to the overall collision boundary or interaction surface.
        /// </summary>
        /// <remarks>
        /// The array may contain any type of Unity Collider (BoxCollider, SphereCollider, MeshCollider, etc.).
        /// The implementation should ensure that the array is never null.
        /// </remarks>
        Collider[] Colliders { get; }
    }
}
