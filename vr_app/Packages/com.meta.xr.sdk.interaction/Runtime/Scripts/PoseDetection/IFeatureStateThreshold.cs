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

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Defines the threshold boundaries for transitioning between two feature states in pose and gesture detection.
    /// This interface provides a hysteresis-based state transition system to prevent rapid state flickering.
    /// </summary>
    /// <remarks>
    /// As seen in <see cref="Oculus.Interaction.PoseDetection.TransformFeatureStateThreshold"/>, this interface implements a midpoint-width
    /// threshold system where:
    /// - The midpoint defines the primary transition point
    /// - The width creates a buffer zone to prevent rapid state changes
    /// </remarks>
    public interface IFeatureStateThreshold<TFeatureState>
    {
        /// <summary>
        /// Gets the threshold value below which the feature will transition to the first state.
        /// This creates the lower boundary of the hysteresis band.
        /// </summary>
        float ToFirstWhenBelow { get; }

        /// <summary>
        /// Gets the threshold value above which the feature will transition to the second state.
        /// This creates the upper boundary of the hysteresis band.
        /// </summary>
        float ToSecondWhenAbove { get; }

        /// <summary>
        /// Retrieves the initial <see cref="TFeatureState"/> in the transition pair that represents the starting point of the transition process.
        /// </summary>
        TFeatureState FirstState { get; }

        /// <summary>
        /// Retrieves the secondary <see cref="TFeatureState"/> in the transition pair that represents the ending point of the transition process.
        /// </summary>
        TFeatureState SecondState { get; }
    }
}
