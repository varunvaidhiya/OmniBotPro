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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// An interface designating that implementing types represent one-dimensional measurements or "axes."
    /// Such measurements can be used to encapsulate trigger input states (such as from
    /// <see cref="ControllerInput.Trigger"/>), in contrast to thumbsticks and other higher-dimensional
    /// input modalities.
    /// </summary>
    public interface IAxis1D
    {
        /// <summary>
        /// Returns the most recent measured value of the IAxis1D; for example, a type representing a
        /// <see cref="ControllerInput.Trigger"/> might return a value between 0 and 1 depending on the
        /// degree to which the trigger is being depressed.
        /// </summary>
        /// <returns>The most recent measured value of the IAxis1D</returns>
        float Value();
    }
}
