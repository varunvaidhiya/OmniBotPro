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
{   /// <summary>
    /// Defines the essential functionalities for movement operations, modifying how an Interactable <see cref="IInteractable"/> moves when selected by an Interactor <see cref="IInteractor"/>.
    /// This interface provides methods to initiate, update, and stop movements, as well as to perform per-frame updates to support smooth and responsive interactions.
    /// </summary>
    public interface IMovement
    {
        /// <summary>
        /// Gets the current pose of the movement. This property allows developers to retrieve the real-time position and orientation of the object implementing this interface
        /// For an example implementation, see <see cref="FollowTarget.Pose"/>
        /// </summary>
        Pose Pose { get; }
        /// <summary>
        /// Indicates whether the movement has stopped.
        /// For an example implementation, see <see cref="ObjectPull.Stopped"/>
        /// </summary>
        bool Stopped { get; }
        /// <summary>
        /// Moves the object to the specified target pose. This method is essential for setting the initial target and starting the movement based on the provided pose.
        /// For an example implementation, see <see cref="MoveTowardsTarget.MoveTo"/>
        /// </summary>
        /// <param name="target">The target pose to which the object will move.</param>
        void MoveTo(Pose target);
        /// <summary>
        ///  Updates the current target to a new pose. This method allows for dynamic changes to the target during movement, which is useful in scenarios where the target is not static.
        ///  For an example implementation, see <see cref="MoveRelativeToTarget.UpdateTarget"/>
        /// </summary>
        /// <param name="target">The new target pose to which the object will move.</param>
        void UpdateTarget(Pose target);
        /// <summary>
        /// Stops the movement and sets the pose to the specified pose. This method provides a way to immediately halt any ongoing movement and reset the pose, which can be critical in stopping interactions gracefully when an Interactable is selected or unselected.
        /// For an example implementation, see <see cref="ObjectPull.StopAndSetPose"/>
        /// </summary>
        /// <param name="pose">The pose to set when the movement is stopped.</param>
        void StopAndSetPose(Pose pose);
        /// <summary>
        /// Updates the movement logic on each frame tick. This method ensures that the movement logic is processed, keeping the movement consistent with the frame rate.
        /// For an example implementation, see <see cref="ObjectPull.Tick"/>
        /// </summary>
        void Tick();
    }
}
