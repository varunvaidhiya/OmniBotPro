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
    /// Provides custom snap pose logic for handling tracked and snapped elements in the Interaction SDK.
    /// This interface enables implementing classes to define precise snapping behavior for objects
    /// as they move between tracking and snapped states.
    /// </summary>
    /// <remarks>
    /// The snap pose delegate manages the lifecycle of tracked and snapped elements through distinct states:
    /// - Tracking: When an element is being actively followed but not yet snapped
    /// - Snapped: When an element has been locked to a specific pose
    /// - Movement: Handling pose updates for tracked elements
    /// See <see cref="Oculus.Interaction.SurfaceSnapPoseDelegate"/> and <see cref="Oculus.Interaction.SequentialSlotsProvider"/> for example implementations.
    /// </remarks>
    public interface ISnapPoseDelegate
    {
        /// <summary>
        /// Begins tracking a new element at the specified pose. This initiates the tracking state
        /// for an element before it potentially snaps to a final position.
        /// </summary>
        /// <param name="id">Unique identifier for the element to track</param>
        /// <param name="pose">Initial pose of the element in world space</param>
        /// <remarks>
        /// This method should be called when an element first becomes eligible for snapping,
        /// typically when it enters a snap zone or grab volume.
        /// </remarks>
        void TrackElement(int id, Pose p);

        /// <summary>
        /// Stops tracking an element, removing it from the tracking system. This should be called
        /// when an element is no longer eligible for snapping or needs to be released.
        /// </summary>
        /// <param name="id">Unique identifier of the element to stop tracking</param>
        void UntrackElement(int id);

        /// <summary>
        /// Transitions an element from being tracked to being snapped at a specific pose.
        /// This locks the element to the specified position and rotation.
        /// </summary>
        /// <param name="id">Unique identifier of the element to snap</param>
        /// <param name="pose">Target pose where the element should snap to</param>
        /// <remarks>
        /// The snap pose may be different from the current tracked pose, allowing for
        /// smooth transitions to predefined positions.
        /// </remarks>
        void SnapElement(int id, Pose pose);

        /// <summary>
        /// Releases an element from its snapped state. This returns the element
        /// to a tracked state or releases it completely.
        /// </summary>
        /// <param name="id">Unique identifier of the element to unsnap</param>
        void UnsnapElement(int id);

        /// <summary>
        /// Updates the pose of a tracked element. This should be called continuously while
        /// an element is being tracked to update its position and rotation.
        /// </summary>
        /// <param name="id">Unique identifier of the element to update</param>
        /// <param name="pose">New pose for the tracked element</param>
        void MoveTrackedElement(int id, Pose p);

        /// <summary>
        /// Determines the target snap pose for a given element, if one exists.
        /// </summary>
        /// <param name="id">Unique identifier of the element to check</param>
        /// <param name="pose">Current pose of the element</param>
        /// <param name="result">The calculated snap pose if available</param>
        /// <returns>True if a valid snap pose exists for the element, false otherwise</returns>
        /// <remarks>
        /// This method is used to preview or validate potential snap positions
        /// before actually performing the snap operation.
        /// </remarks>
        bool SnapPoseForElement(int id, Pose pose, out Pose result);
    }
}
