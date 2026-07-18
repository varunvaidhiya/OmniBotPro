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
using Oculus.Interaction.Body.Input;

namespace Oculus.Interaction.Body.PoseDetection
{
    /// <summary>
    /// Encapsulates the pose of a body as a collection of joint poses together constituting a fully-posed skeleton.
    /// </summary>
    /// <remarks>
    /// IBodyPoses can be generated from live data (using <see cref="PoseFromBody"/>, for example) or stored/cached off
    /// as a reference for comparison (using <see cref="BodyPoseComparerActiveState"/>, for example).
    /// </remarks>
    public interface IBodyPose
    {
        /// <summary>
        /// Event indicating that the data in this IBodyPose has been updated.
        /// </summary>
        /// <remarks>
        /// Observing instances can hook into this event to run their logic only when new data is available (rather
        /// than polling).
        /// </remarks>
        event Action WhenBodyPoseUpdated;

        /// <summary>
        /// The mapping of the skeleton, which characterizes which <see cref="BodyJointId"/>s are populated by
        /// in this body pose and the structure/parentage among them.
        /// </summary>
        ISkeletonMapping SkeletonMapping { get; }

        /// <summary>
        /// Attempts to return the pose of the requested body joint, in local space relative to its parent
        /// joint (i.e., the <see cref="BodyJointId"/> retrievable from
        /// <see cref="ISkeletonMapping.TryGetParentJointId(BodyJointId, out BodyJointId)"/>).
        /// </summary>
        bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose);

        /// <summary>
        /// Attempts to return the pose of the requested body joint relative
        /// to the root joint (<see cref="BodyJointId.Body_Root"/>).
        /// </summary>
        bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose);
    }
}
