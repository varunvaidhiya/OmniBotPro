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
    /// Provides a collection of feature state thresholds and timing parameters for state transitions
    /// in pose and gesture detection systems. This interface manages multiple feature state configurations
    /// and ensures proper timing for state changes.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by components that need to track multiple feature states
    /// and manage their transition timing. It works in conjunction with <see cref="Oculus.Interaction.PoseDetection.IFeatureStateThresholds{TFeature, TFeatureState}"/>
    /// to provide comprehensive state management. See <see cref="FingerFeatureStateThresholds"/> and <see cref="TransformFeatureStateThresholds"/> for example implementations.
    /// </remarks>
    public interface IFeatureThresholds<TFeature, TFeatureState>
    {
        /// <summary>
        /// Gets the collection of feature state threshold configurations for all monitored features.
        /// </summary>
        /// <returns>
        /// A read-only list of <see cref="IFeatureStateThresholds"/> configurations that define when and how
        /// features transition between states.
        /// </returns>
        IReadOnlyList<IFeatureStateThresholds<TFeature, TFeatureState>>
            FeatureStateThresholds
        {
            get;
        }

        /// <summary>
        /// Gets the minimum time (in seconds) that a feature must maintain a state before
        /// a transition is recognized. This helps prevent rapid state flickering.
        /// </summary>
        /// <value>The minimum duration in seconds required for state validation</value>
        double MinTimeInState { get; }
    }
}
