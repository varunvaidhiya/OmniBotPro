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
using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Interface for determining when a Hand should be rendered and contains all the data needed to render a <see cref="IHand"/> such as the the Hand itself. This interface adds to the <see cref="IHand"/> interface
    /// by adding additional attributes such as Hand visibility, and whether it should be rendered.
    /// Provides an event to notify subscribers when the HandVisual is updated.
    /// </summary>
    public interface IHandVisual
    {
        /// <summary>
        /// Hand Instance that provides a world transform and finger joint data for this visual.
        /// </summary>
        IHand Hand { get; }

        /// <summary>
        /// Determines if the hand should render in the scene, i.e the Hand is currently tracked inside the
        /// player's view and is not being obstructed by an object.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Used to determine if the hand should be rendered even if it is visible in the current scene.
        /// </summary>
        bool ForceOffVisibility { get; set; }

        /// <summary>
        /// Get the current <see cref="Pose"/> of the HandJoint given in the current <see cref="Space"/>.
        /// Used to grant quicker access to <see cref="GetJointPose(HandJointId, out Pose)"/> data.
        /// </summary>
        /// <param name="jointId">The hand joint to get the pose of</param>
        /// <param name="space">The space/transformation that the hand is in</param>
        /// <returns>The Pose of the HandJoint in the given Space</returns>
        Pose GetJointPose(HandJointId jointId, Space space);

        /// <summary>
        /// An event used to notify subscribers when to update the hand visuals
        /// </summary>
        /// <returns> The delegate function for the event</returns>
        event Action WhenHandVisualUpdated;
    }
}
