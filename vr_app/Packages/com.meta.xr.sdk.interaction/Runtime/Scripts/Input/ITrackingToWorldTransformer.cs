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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Indicates that a type provides a mechanism for transforming transforms (i.e., position and orientation data)
    /// from tracking space to world space.
    /// </summary>
    /// <remarks>
    /// "Tracking space" is the coordinate space in which the underlying system tracks user movements, the environment,
    /// etc. "World space" is a simple characterization of the coordinate space in which game logic transpires. In
    /// order to translate real-world tracking data to virtual-world interactions, game logic needs to transform system
    /// observations in tracking space into game movements in world space; for example, if the user moves their hand by
    /// half a meter in the real world, but the game considers the user to currently be ten times their real size, the
    /// <see cref="Hand"/> in game needs to move by 5 meters instead of half a meter. Types such as <see cref="Hand"/>
    /// and <see cref="Controller"/>, which are responsible for this translation step, use ITrackingToWorldTransformers
    /// to make the required space conversions.
    /// </remarks>
    public interface ITrackingToWorldTransformer
    {
        /// <summary>
        /// The underlying transform of the tracking space; this can be used directly as an alternative to
        /// <see cref="ToWorldPose(Pose)"/> and <see cref="ToTrackingPose(in Pose)"/>.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Converts a tracking space pose to a pose in in Unity's world coordinate space (i.e. teleportation applied).
        /// </summary>
        /// <remarks>
        /// This is a convenience method for manipulating Pose types directly; for vector and quaternion types,
        /// equivalent behavior can be achieved by directly invoking the various Transform methods on the
        /// <see cref="Transform"/>.
        /// </remarks>
        Pose ToWorldPose(Pose poseRh);

        /// <summary>
        /// Converts a world space pose in Unity's coordinate space to a pose in tracking space (i.e. no teleportation
        /// applied).
        /// </summary>
        /// <remarks>
        /// This is a convenience method for manipulating Pose types directly; for vector and quaternion types,
        /// equivalent behavior can be achieved by directly invoking the various InverseTransform methods on the
        /// <see cref="Transform"/>.
        /// </remarks>
        Pose ToTrackingPose(in Pose worldPose);

        /// <summary>
        /// This is an internal and legacy capability which should not be needed in new usage. It is used specifically
        /// to address problems with wrist data from certain tracking solutions; if this field is not identity, its
        /// value can be applied to wrist data to rectify orientation problems.
        /// </summary>
        Quaternion WorldToTrackingWristJointFixup { get; }
    }
}
