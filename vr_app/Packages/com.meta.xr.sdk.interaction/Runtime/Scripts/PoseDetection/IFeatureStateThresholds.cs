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

using System.Collections.Generic;

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Defines threshold configurations for feature state transitions in pose and gesture detection systems.
    /// This interface provides a generic approach to defining boundaries between different states of a feature,
    /// such as finger poses or transform orientations.
    /// </summary>
    /// <typeparam name="TFeature">The type of feature being monitored (e.g., TransformFeature, FingerFeature)</typeparam>
    /// <typeparam name="TFeatureState">The type representing possible states of the feature</typeparam>
    /// <remarks>
    /// Used in conjunction with <see cref="Oculus.Interaction.PoseDetection.TransformFeatureStateThresholds"/> and
    /// <see cref="Oculus.Interaction.PoseDetection.FingerFeatureStateThresholds"/> to define when features should transition between states.
    /// The thresholds use a midpoint and width system to prevent rapid state flickering at transition boundaries.
    /// </remarks>
    public interface IFeatureStateThresholds<TFeature, TFeatureState>
    {
        /// <summary>
        /// Gets the specific feature type that these thresholds apply to.
        /// </summary>
        /// <value>The feature identifier (e.g., WristUp, PalmDown for TransformFeature)</value>
        TFeature Feature { get; }

        /// <summary>
        /// Gets the collection of state thresholds that define the boundaries between different feature states.
        /// Each threshold defines when a feature should transition between two states.
        /// </summary>
        /// <value>A read-only list of <see cref="IFeatureStateThreshold"/> thresholds</value>
        IReadOnlyList<IFeatureStateThreshold<TFeatureState>> Thresholds { get; }
    }
}
