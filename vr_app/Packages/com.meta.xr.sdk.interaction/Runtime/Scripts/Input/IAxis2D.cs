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
    /// An interface designating that implementing types represent two-dimensional measurements or "axes."
    /// Such measurements can be used to encapsulate thumbsticks and similar input modalities (such as from
    /// <see cref="ControllerInput.Primary2DAxis"/>), in contrast to buttons and other lower-dimensional
    /// input modalities.
    /// </summary>
    public interface IAxis2D
    {
        /// <summary>
        /// Returns the most recent measured value of the IAxis2D; for example, a type representing a
        /// <see cref="ControllerInput.Primary2DAxis"/> might return (0.5f, 0.5f) if the underlying thumbstick
        /// is being held slightly up and to the right of its neutral position.
        /// </summary>
        /// <returns>The most recent measured value of the IAxis2D</returns>
        Vector2 Value();
    }
}
