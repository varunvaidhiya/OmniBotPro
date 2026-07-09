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

using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Provides a standardized interface for tracking and measuring finger-based interactions in the Interaction SDK.
    /// This interface enables different implementation methods for detecting palm grab, pinching, finger curl, and other hand poses.
    /// See <see cref="Oculus.Interaction.GrabAPI.PalmGrabAPI"/> and <see cref="Oculus.Interaction.GrabAPI.PinchGrabAPI"/> for example implementations.
    /// </summary>
    public interface IFingerAPI
    {
        /// <summary>
        /// Determines if a specific finger is currently in a grabbing state.
        /// </summary>
        /// <param name="finger">The <see cref="HandFinger"/> to check for grabbing state.</param>
        /// <returns>True if the specified finger is grabbing, false otherwise.</returns>
        bool GetFingerIsGrabbing(HandFinger finger);

        /// <summary>
        /// Detects changes in a finger's grabbing state relative to a target state.
        /// </summary>
        /// <param name="finger">The <see cref="HandFinger"/> to check for state changes.</param>
        /// <param name="targetPinchState">The target grab state to compare against.</param>
        /// <returns>True if the finger's grab state has changed to match the target state.</returns>
        bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetPinchState);

        /// <summary>
        /// Retrieves a normalized score indicating how strongly a finger is grabbing.
        /// </summary>
        /// <param name="finger">The <see cref="HandFinger"/> to evaluate.</param>
        /// <returns>A value between 0 and 1, where 0 indicates no grab and 1 indicates maximum grab strength.</returns>
        float GetFingerGrabScore(HandFinger finger);

        /// <summary>
        /// Gets the local space offset from the wrist position for grab calculations.
        /// </summary>
        /// <returns>The offset vector in local space coordinates.</returns>
        Vector3 GetWristOffsetLocal();

        /// <summary>
        /// Updates the finger API with the latest hand tracking data.
        /// </summary>
        /// <param name="hand">The <see cref="IHand"/> tracking data to process.</param>
        void Update(IHand hand);
    }
}
