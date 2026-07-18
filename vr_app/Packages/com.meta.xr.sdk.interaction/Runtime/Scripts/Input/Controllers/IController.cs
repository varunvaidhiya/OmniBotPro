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

using System;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// The Interaction SDK's fundamental concept of an XR controller, exposing data such as pose, pointer pose, and button states.
    /// This  interface encapsulates the core capabilities common to all XR controllers supported by the Interaction SDK. Where
    /// possible, you should use this interface instead of concrete alternatives such as <see cref="Controller"/>, which are less
    /// general and portable.
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// Indicates whether the tracked controller is <see cref="Handedness.Left"/>- or <see cref="Handedness.Right"/>-handed.
        /// </summary>
        Handedness Handedness { get; }

        /// <summary>
        /// Indicates the expected scale of the controller in world space. Under most circumstances, this scale is 1; however, if
        /// the tracking space has been scaled relative to world space, that scaling will be reflected in this value.
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Indicates whether the controller is available and has valid data. Note that, because this reflects both the general
        /// availability and present validity of controller data, this value can vary from frame to frame depending on whether
        /// the system is currently able to track the controller. This is a valid API to check in order to determine whether
        /// up-to-date controller data is available at a given moment.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indicates whether the controller is currently being tracked by the system such that valid poses for it can be retrieved.
        /// </summary>
        bool IsPoseValid { get; }

        /// <summary>
        /// Attempts to retrieve the pose of the controller in world space. The exact relationship of the retrieved pose to the
        /// physical position of the controller can vary by platform, but it typically corresponds to the root transform of the
        /// intended visual affordance for the physical controller being tracked.
        /// </summary>
        /// <param name="pose">A void world space pose for the controller if available, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid pose, false otherwise</returns>
        bool TryGetPose(out Pose pose);

        /// <summary>
        /// Attempts to return a valid "pointer pose" for the controller. A "pointer pose" is a world-space pose (position and
        /// orientation) intended to be used as a directional input, conceptually comparable to "pointing at something." Typically,
        /// individual controllers provide their own "pointer pose" based on the ergonomics of the device; this method provides a
        /// unified way to query for this.
        /// </summary>
        /// <param name="pose">A valid world-space pointer pose if one could be calculated, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid pointer pose, false otherwise</returns>
        bool TryGetPointerPose(out Pose pose);

        /// <summary>
        /// Retrieves a <see cref="Input.ControllerInput"/> which can be used to query the state of the buttons, thumbsticks, and
        /// other input modalities available on the controller.
        /// </summary>
        ControllerInput ControllerInput { get; }

        /// <summary>
        /// Batch query method for determining whether any within a set of buttons (characterized by a bit mask of
        /// <see cref="ControllerButtonUsage"/>) is currently active.
        /// </summary>
        /// <param name="buttonUsage">The bit mask indicating which buttons are part of the query</param>
        /// <returns>True if any of the queried buttons are active, false otherwise</returns>
        bool IsButtonUsageAnyActive(ControllerButtonUsage buttonUsage);

        /// <summary>
        /// Batch query method for determining whether all of a set of buttons (characterized by a bit mask of
        /// <see cref="ControllerButtonUsage"/>) are currently active.
        /// </summary>
        /// <param name="buttonUsage">The bit mask indicating which buttons are part of the query</param>
        /// <returns>True if all of the queried buttons are active, false otherwise</returns>
        bool IsButtonUsageAllActive(ControllerButtonUsage buttonUsage);

        /// <summary>
        /// Event indicating that the controller has been updated and can now be queried for potentially new data.
        /// </summary>
        event Action WhenUpdated;
    }
}
