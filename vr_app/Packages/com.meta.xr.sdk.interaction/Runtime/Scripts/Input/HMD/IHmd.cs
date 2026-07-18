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
    /// Interaction SDK's fundamental concept of a head-mounted display device, such as a VR headset. This interface encapsulates
    /// the core capabilities common to all HMDs supported by the Interaction SDK. Where possible, you should use this
    /// interface instead of concrete alternatives such as <see cref="Hmd"/>, which are less general and portable.
    /// </summary>
    public interface IHmd
    {
        /// <summary>
        /// Attempts to retrieve the world-space pose (position and orientation) of the HMD. If a valid HMD pose is available,
        /// it will be stored in the pose argument and this method will return true; otherwise, the pose argument will be set to
        /// the identity pose and the method will return false.
        /// </summary>
        /// <param name="pose">The struct to be populated with the HMD pose</param>
        /// <returns>True if the out argument was populated with a valid HMD pose, false otherwise</returns>
        bool TryGetRootPose(out Pose pose);

        /// <summary>
        /// Event signaled when the HMD is updated with the latest data available from the system. For situations where only the
        /// most up-to-date HMD pose data will suffice, <see cref="TryGetRootPose(out Pose)"/> can be called from within a handler
        /// to this event.
        /// </summary>
        event Action WhenUpdated;
    }
}
