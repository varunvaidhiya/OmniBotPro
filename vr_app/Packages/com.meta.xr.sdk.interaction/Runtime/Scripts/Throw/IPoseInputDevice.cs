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

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Provides a unified interface for accessing pose and velocity data from input devices.
    /// This interface is primarily used for implementing throwing mechanics and other physics-based
    /// interactions that require accurate tracking of position, rotation, and velocity.
    /// </summary>
    /// <remarks>
    /// See <see cref="Oculus.Interaction.Throw.ControllerPoseInputDevice"/> and <see cref="Oculus.Interaction.Throw.HandPoseInputDevice"/> for common implementations.
    /// </remarks>
    public interface IPoseInputDevice
    {
        /// <summary>
        /// Indicates whether the current pose data from the input device is valid and can be used.
        /// </summary>
        /// <remarks>
        /// This should be checked before attempting to use pose data to ensure reliable tracking.
        /// Invalid poses might occur during tracking loss or device initialization.
        /// </remarks>
        bool IsInputValid { get; }

        /// <summary>
        /// Indicates whether the current pose tracking has high confidence.
        /// </summary>
        /// <remarks>
        /// Returns true if the system has high confidence in the current pose tracking accuracy.
        /// For controllers, this checks if the pose is valid and controller are connected. For hands, this checks additional confidence metrics.
        /// </remarks>
        bool IsHighConfidence { get; }

        /// <summary>
        /// Attempts to get the current root pose of the input device.
        /// </summary>
        /// <param name="pose">When successful, contains the current pose in world space coordinates.</param>
        /// <returns>
        /// Returns true if the pose was successfully retrieved, false if tracking is invalid or unavailable.
        /// </returns>
        /// <remarks>
        /// For hands, this returns the wrist root pose with palm offset applied. For controllers,
        /// this returns the controller's tracked pose.
        /// </remarks>
        bool GetRootPose(out Pose pose);

        /// <summary>
        /// Retrieves the current external velocities of the input device.
        /// </summary>
        /// <param name="linearVelocity">The current linear velocity in meters per second.</param>
        /// <param name="angularVelocity">The current angular velocity in radians per second.</param>
        /// <returns>
        /// Returns true if velocities were successfully calculated, false otherwise.
        /// </returns>
        /// <remarks>
        /// For hands, this calculates velocities based on finger joint movement. For controllers,
        /// this uses the device's built-in velocity tracking.
        /// </remarks>
        (Vector3, Vector3) GetExternalVelocities();
    }
}
