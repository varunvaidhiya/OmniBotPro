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
    /// The data asset used by <see cref="DataModifier{HandDataAsset}"/> to pipe
    /// data through the <see cref="DataModifier{TData}"/> stack,
    /// and contains hand state data.
    /// </summary>
    [Serializable]
    public class HandDataAsset : ICopyFrom<HandDataAsset>
    {
        /// <summary>
        /// Is the data in this asset considered valid.
        /// </summary>
        public bool IsDataValid;

        /// <summary>
        /// Is the hand a connected input device to the headset.
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// Is the hand being tracked by the headset.
        /// </summary>
        public bool IsTracked;

        /// <summary>
        /// The root pose of the hand, which typically represents the
        /// wrist position. See <see cref="RootPoseOrigin"/>
        /// for information on how this pose was created.
        /// </summary>
        public Pose Root;

        /// <summary>
        /// Information about how <see cref="RootPose"/> was created, for example
        /// whether it is the raw pose from tracking, or whether is has been
        /// filtered or modified in the <see cref="DataModifier{TData}"/> stack.
        /// </summary>
        public PoseOrigin RootPoseOrigin;

#if ISDK_OPENXR_HAND
        /// <summary>
        /// An array containing joint poses of the hand skeleton.
        /// This array is indexed by the integer values of the <see cref="HandJointId"/> enum.
        /// </summary>
        public Pose[] JointPoses = new Pose[Constants.NUM_HAND_JOINTS];

        /// <summary>
        /// An array containing radii for each hand skeleton joint.
        /// This array is indexed by the integer values of the <see cref="HandJointId"/> enum.
        /// </summary>
        public float[] JointRadii = new float[Constants.NUM_HAND_JOINTS];
        [System.Obsolete("Deprecated. Use JointPoses instead.")]
#endif

        /// <summary>
        /// An array containing joint orientations of the hand skeleton.
        /// This array is indexed by the integer values of the <see cref="HandJointId"/> enum.
        /// </summary>
        public Quaternion[] Joints = new Quaternion[Constants.NUM_HAND_JOINTS];

        /// <summary>
        /// True if the tracking system considers the tracking data as high confidence,
        /// which can be used to determine how accurate the supplied pose data is.
        /// </summary>
        public bool IsHighConfidence;

        /// <summary>
        /// An array of booleans representing finger pinch states, where True
        /// represents a finger that is performing a pinch gesture.
        /// This array is indexed by the values of the <see cref="HandFinger"/> enum.
        /// </summary>
        public bool[] IsFingerPinching = new bool[Constants.NUM_FINGERS];

        /// <summary>
        /// Similar to <see cref="IsHighConfidence"/> but per-finger. Represents the
        /// tracking system's confidence in the accuracy of each finger.
        /// This array is indexed by the values of the <see cref="HandFinger"/> enum.
        /// </summary>
        public bool[] IsFingerHighConfidence = new bool[Constants.NUM_FINGERS];

        /// <summary>
        /// An array of values representing finger pinch states, where the strength value
        /// represents the strength of the pinch being provided for each finger.
        /// This array is indexed by the values of the <see cref="HandFinger"/> enum.
        /// </summary>
        public float[] FingerPinchStrength = new float[Constants.NUM_FINGERS];

        /// <summary>
        /// The scale of the hand as provided by the system.
        /// </summary>
        public float HandScale;

        /// <summary>
        /// The pointer pose for the hand, which is generally used as the
        /// raycast source pose. See <see cref="PointerPoseOrigin"/>
        /// for information on how this pose was created.
        /// </summary>
        public Pose PointerPose;

        /// <summary>
        /// Information about how <see cref="PointerPose"/> was created, for example
        /// whether it is the raw pose from tracking, or whether is has been
        /// filtered or modified in the <see cref="DataModifier{TData}"/> stack.
        /// </summary>
        public PoseOrigin PointerPoseOrigin;

        /// <summary>
        /// True if this hand is considered the dominant hand,
        /// as set by the system handedness.
        /// </summary>
        public bool IsDominantHand;

        /// <summary>
        /// This data object contains configuration data that is shuttled
        /// through the <see cref="DataModifier{TData}"/> stack.
        /// </summary>
        public HandDataSourceConfig Config = new HandDataSourceConfig();

        /// <summary>
        /// Convenience property which returns true if <see cref="IsDataValid"/>
        /// and <see cref="IsConnected"/> are both true.
        /// </summary>
        public bool IsDataValidAndConnected => IsDataValid && IsConnected;

        /// <summary>
        /// Copies the provided <see cref="HandDataAsset"/> into the caller.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        public void CopyFrom(HandDataAsset source)
        {
            IsDataValid = source.IsDataValid;
            IsConnected = source.IsConnected;
            IsTracked = source.IsTracked;
            IsHighConfidence = source.IsHighConfidence;
            IsDominantHand = source.IsDominantHand;
            Config = source.Config;
            CopyPosesFrom(source);
        }

        /// <summary>
        /// Copies only pose data from a provided
        /// <see cref="HandDataAsset"/> into the caller.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        public void CopyPosesFrom(HandDataAsset source)
        {
            Root = source.Root;
            RootPoseOrigin = source.RootPoseOrigin;
#if ISDK_OPENXR_HAND
            Array.Copy(source.JointPoses, JointPoses, Constants.NUM_HAND_JOINTS);
            Array.Copy(source.JointRadii, JointRadii, source.JointRadii.Length);
#endif
#pragma warning disable 0618
            Array.Copy(source.Joints, Joints, Constants.NUM_HAND_JOINTS);
#pragma warning restore 0618
            Array.Copy(source.IsFingerPinching, IsFingerPinching, IsFingerPinching.Length);
            Array.Copy(source.IsFingerHighConfidence, IsFingerHighConfidence,
                IsFingerHighConfidence.Length);
            Array.Copy(source.FingerPinchStrength, FingerPinchStrength, FingerPinchStrength.Length);
            HandScale = source.HandScale;
            PointerPose = source.PointerPose;
            PointerPoseOrigin = source.PointerPoseOrigin;
            Config = source.Config;
        }
    }
}
