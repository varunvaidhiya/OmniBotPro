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

namespace Oculus.Interaction
{   /// <summary>
    /// Creates instances of IMovement through the <see cref="CreateMovement"/> method. This interface acts as a factory, and allows for the dynamic creation of custom movement behaviors for Interactables<see cref="IInteractable"/> when selected by an Interactor<see cref="IInteractor"/>.
    /// </summary>
    public interface IMovementProvider
    {
        /// <summary>
        /// Creates and returns an instance of IMovement. The instance is used to generate movement when an Interactable is Selected <see cref="Interactor.InteractableSelected"/>
        /// </summary>
        /// <remarks>
        /// For an example implementation, see <see cref="ObjectPullProvider.CreateMovement"/>
        /// For example usage, see <see cref="RayInteractable.GenerateMovement"/>
        /// </remarks>
        /// <returns>An instance of IMovement configured for the selected Interactable.</returns>
        IMovement CreateMovement();
    }
}
