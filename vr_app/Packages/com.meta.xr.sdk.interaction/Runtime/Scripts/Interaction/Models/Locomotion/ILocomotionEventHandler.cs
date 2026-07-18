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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Defines an interface for handling locomotion events in the Interaction SDK. This interface enables
    /// components to process and respond to various types of movement and teleportation events. <see cref="Oculus.Interaction.Locomotion.CapsuleLocomotionHandler"/> and <see cref="Oculus.Interaction.Locomotion.FlyingLocomotor"/> for example implementations.
    /// </summary>
    public interface ILocomotionEventHandler
    {
        /// <summary>
        /// Processes an incoming locomotion event and applies the appropriate transformations
        /// or state changes based on the event type.
        /// </summary>
        /// <param name="locomotionEvent">The <see cref="LocomotionEvent"/> containing movement or teleportation data</param>
        void HandleLocomotionEvent(LocomotionEvent locomotionEvent);

        /// <summary>
        /// Event that fires after a <see cref="LocomotionEvent"/> has been successfully handled, providing
        /// the original event and the resulting pose transformation.
        /// </summary>
        /// <remarks>
        /// This event can be used to synchronize other components or update visual feedback
        /// systems after locomotion occurs.
        /// </remarks>
        event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled;
    }
}
