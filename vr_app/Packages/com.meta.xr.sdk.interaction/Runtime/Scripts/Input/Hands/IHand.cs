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
using System;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// The Interaction SDK's fundamental concept of a tracked hand, exposing data such as wrist and tracked joint poses. This
    /// interface encapsulates the core capabilities common to all hand tracking solutions supported by the Interaction SDK. Where
    /// possible, you should use this interface instead of concrete alternatives such as <see cref="Hand"/>, which are less
    /// general and portable.
    /// </summary>
    public interface IHand
    {
        /// <summary>
        /// Indicates whether the tracked hand is <see cref="Handedness.Left"/>- or <see cref="Handedness.Right"/>-handed.
        /// </summary>
        Handedness Handedness { get; }

        /// <summary>
        /// Indicates whether hand tracking is available and has valid data. Note that, because this reflects both the general
        /// availability and present validity of hand tracking data, this value can vary from frame to frame depending on whether
        /// the system is currently able to track the hand. This is a valid API to check in order to determine whether up-to-date
        /// hand tracking is available at a given moment.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Indicates whether the system has high confidence in the validity of its most recent hand tracking data. In order for this
        /// value to be true, <see cref="IsConnected"/> and <see cref="IsTrackedDataValid"/> must also be true.
        /// </summary>
        bool IsHighConfidence { get; }

        /// <summary>
        /// Indicates whether the current hand is the dominant hand. The availability of this feature depends on whether or not the
        /// platform provides a way to determine whether the user is left- or right-handed.
        /// </summary>
        bool IsDominantHand { get; }

        /// <summary>
        /// Indicates the detected scale of the tracked hand relative to the default tracked hand size.
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Checks whether a particular finger is considered to be pinching. How a given finger is determined to be pinching is an
        /// implementation detail which can vary by finger/platform and is not part of the contract of this method, which  merely
        /// provides a unified way to query for the semantic concept of a "pinch".
        /// </summary>
        /// <param name="finger">The finger to check for pinching behavior</param>
        /// <returns>Whether or not the requested finger is considered to be pinching</returns>
        bool GetFingerIsPinching(HandFinger finger);

        /// <summary>
        /// A convenience method to check for the most commonly-desired pinching behavior. Calling this method is equivalent to
        /// calling <see cref="GetFingerIsPinching(HandFinger)"/> with the argument <see cref="HandFinger.Index"/>.
        /// </summary>
        /// <returns>Whether or not the index finger is considered to be pinching</returns>
        bool GetIndexFingerIsPinching();

        /// <summary>
        /// Checks whether a valid "pointer pose" is currently available. See <see cref="GetPointerPose(out Pose)"/> for details
        /// on what a pointer pose is.
        /// </summary>
        bool IsPointerPoseValid { get; }

        /// <summary>
        /// Attempts to return a valid "pointer pose" for the hand. A "pointer pose" is a world-space pose (position and orientation)
        /// intended to be used as a directional input, conceptually comparable to "pointing at something." However, the nature of
        /// this pose and its relationship to the hand are not part of the contract of this method; it may or may not correspond to
        /// a "direction of pointing," and in many implementations it corresponds more closely to a "direction of reaching out."
        /// These details can vary, but in all cases this method provides a unified way to query for the semantic concept of a
        /// "pointer."
        /// </summary>
        /// <param name="pose">A valid world-space pointer pose if one could be calculated, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid pointer pose, false otherwise</returns>
        bool GetPointerPose(out Pose pose);

        /// <summary>
        /// Attempts to return the world-space pose of the requested hand joint.
        /// </summary>
        /// <param name="handJointId">The joint for which the pose is being requested</param>
        /// <param name="pose">A valid world-space joint pose if one is available, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid joint pose, false otherwise</returns>
        bool GetJointPose(HandJointId handJointId, out Pose pose);

        /// <summary>
        /// Attempts to return the pose of the requested hand joint in "local" space. This "local" space is loosely defined and can
        /// vary by platform (in some implementations, each joint is relative to a given parent joint), so the exact nature of what is
        /// returned by this method is not guaranteed. In general, it is recommended to use
        /// <see cref="GetJointPose(HandJointId, out Pose)"/> or <see cref="GetJointPoseFromWrist(HandJointId, out Pose)"/> instead
        /// where possible.
        /// </summary>
        /// <param name="handJointId">The joint for which the pose is being requested</param>
        /// <param name="pose">A valid "local" space joint pose if one is available, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid joint pose, false otherwise</returns>
        bool GetJointPoseLocal(HandJointId handJointId, out Pose pose);

        /// <summary>
        /// Retrieves all "local" space joint poses for this hand. The values returned by this method are the same as those returned
        /// by calling <see cref="GetJointPoseLocal(HandJointId, out Pose)"/> repeatedly, but as a batched query this method can be
        /// more efficient if many joint poses are needed.
        /// </summary>
        /// <param name="localJointPoses">The array of all "local" joint poses if available, or an empty array otherwise</param>
        /// <returns>True if the poses collection was correctly populated, false otherwise</returns>
        bool GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses);

        /// <summary>
        /// Attempts to return the pose of the requested hand joint in wrist space. Wrist space is strongly defined as a transform
        /// space located and oriented according to <see cref="HandJointId.HandWristRoot"/>. This space is of uniform scale across
        /// hand sizes, meaning all tracked hands are the same size in wrist space, and wrist space itself must be multipled by
        /// <see cref="Scale"/> in order to correctly reflect the observed size of the tracked hands in world space.
        /// </summary>
        /// <param name="handJointId">The joint for which the pose is being requested</param>
        /// <param name="pose">A valid wrist space joint pose if available, false otherwise</param>
        /// <returns>True if the pose argument was populated with a valid joint pose, false otherwise</returns>
        bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose);

        /// <summary>
        /// Retrieves all wrist space joint poses for this hand. The values returned by this method are the same as those returned
        /// by calling <see cref="GetJointPoseFromWrist(HandJointId, out Pose)"/> repeatedly, but as a batched query this method can
        /// be more efficient if many joint poses are needed.
        /// </summary>
        /// <param name="jointPosesFromWrist">The array of all wrist space joint poses if available, or an empty array otherwise</param>
        /// <returns>True if the poses collection was correctly populated, false otherwise</returns>
        bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist);

        /// <summary>
        /// Attempts to return the pose of the palm in "local" space. This "local" space is loosely defined and can vary by platform;
        /// in some implementations, it can be considered for this method in particular to be synonymous with wrist space.
        /// </summary>
        /// <param name="pose">The "local" palm position and orientation if available, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid palm pose, false otherwise</returns>
        bool GetPalmPoseLocal(out Pose pose);

        /// <summary>
        /// Queries whether the tracking data for a particular finger is high- or low-confidence.
        /// </summary>
        /// <param name="finger">The finger to be checked for high-confidence tracking data</param>
        /// <returns>True if the system is confident in its tracking data for the requested finger, false otherwise</returns>
        bool GetFingerIsHighConfidence(HandFinger finger);

        /// <summary>
        /// Queries the strength with which a particular finger is considered to be pinching. "Strength" in this case is an
        /// indication of how similar or different the finger's pose is to what could be considered a pinch; a finger with its
        /// tip far from the thumb will likely have a pinch strength at or near 0, while a finger with its tip touching the thumb
        /// will likely have a pinch strength at or near 1.
        /// </summary>
        /// <param name="finger">The finger to be checked for pinch strength</param>
        /// <returns>A number from 0 to 1 indicating how far or close, respectively, the finger is from pinching</returns>
        float GetFingerPinchStrength(HandFinger finger);

        /// <summary>
        /// Indicates whether the hand is currently tracked such that tracking poses are available for the hand root and finger
        /// joints. Note that this property being true does not necessarily mean that a valid pointer pose is available;
        /// <see cref="IsPointerPoseValid"/> may be false while valid tracking data is available.
        /// </summary>
        bool IsTrackedDataValid { get; }

        /// <summary>
        /// Attempts to retrieve the pose of the wrist joint in world space. This is a convenience method and is identical to calling
        /// <see cref="GetJointPose(HandJointId, out Pose)"/> with the argument <see cref="HandJointId.HandWristRoot"/>.
        /// </summary>
        /// <param name="pose">A void world space pose for the wrist joint if available, identity otherwise</param>
        /// <returns>True if the pose argument was populated with a valid joint pose, false otherwise</returns>
        bool GetRootPose(out Pose pose);

        /// <summary>
        /// Indicator which is incremented every time new tracked hand data becomes available.
        /// </summary>
        int CurrentDataVersion { get; }

        /// <summary>
        /// Event indicating that <see cref="CurrentDataVersion"/> has been incremented and thus the hand can be queried for
        /// potentially new data.
        /// </summary>
        event Action WhenHandUpdated;
    }
}
