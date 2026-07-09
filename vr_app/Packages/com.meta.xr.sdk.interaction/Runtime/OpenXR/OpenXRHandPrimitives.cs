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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ISDK_OPENXR_HAND
using OVRHandJointId = Oculus.Interaction.Input.Compatibility.OVR.HandJointId;
using OpenXRHandJointId = Oculus.Interaction.Input.HandJointId;
#else
using OVRHandJointId = Oculus.Interaction.Input.HandJointId;
using OpenXRHandJointId = Oculus.Interaction.Input.Compatibility.OpenXR.HandJointId;
#endif

/// <summary>
/// Primitive type serialization
/// </summary>
#if ISDK_OPENXR_HAND
namespace Oculus.Interaction.Input
#else
namespace Oculus.Interaction.Input.Compatibility.OpenXR
#endif
{
    public static class Constants
    {
        /// <summary>
        /// The number of joints in the hand skeleton.
        /// </summary>
        public const int NUM_HAND_JOINTS = (int)HandJointId.HandEnd;

        /// <summary>
        /// The number of fingers in the hand skeleton.
        /// </summary>
        public const int NUM_FINGERS = 5;

        /// <summary>
        /// The local proximal (toward-palm) axis of a Right hand joint.
        /// </summary>
        public static readonly Vector3 RightProximal = Vector3.back;

        /// <summary>
        /// The local distal (away-from-palm) axis of a Right hand joint.
        /// </summary>
        public static readonly Vector3 RightDistal = Vector3.forward;

        /// <summary>
        /// The local to-pinky axis of a Right hand joint.
        /// </summary>
        public static readonly Vector3 RightPinkySide = Vector3.right;

        /// <summary>
        /// The local to-thumb axis of a Right hand joint.
        /// </summary>
        public static readonly Vector3 RightThumbSide = Vector3.left;

        /// <summary>
        /// The local palmar (palm facing) axis of a Right hand joint.
        /// </summary>
        public static readonly Vector3 RightPalmar = Vector3.down;

        /// <summary>
        /// The local dorsal (back-of-hand facing) axis of a Right hand joint.
        /// </summary>
        public static readonly Vector3 RightDorsal = Vector3.up;

        /// <summary>
        /// The local proximal (toward-palm) axis of a Left hand joint.
        /// </summary>
        public static readonly Vector3 LeftProximal = Vector3.back;

        /// <summary>
        /// The local distal (away-from-palm) axis of a Left hand joint.
        /// </summary>
        public static readonly Vector3 LeftDistal = Vector3.forward;

        /// <summary>
        /// The local to-pinky axis of a Left hand joint.
        /// </summary>
        public static readonly Vector3 LeftPinkySide = Vector3.left;

        /// <summary>
        /// The local to-thumb axis of a Left hand joint.
        /// </summary>
        public static readonly Vector3 LeftThumbSide = Vector3.right;

        /// <summary>
        /// The local palmar (palm facing) axis of a Left hand joint.
        /// </summary>
        public static readonly Vector3 LeftPalmar = Vector3.down;

        /// <summary>
        /// The local dorsal (back-of-hand facing) axis of a Left hand joint.
        /// </summary>
        public static readonly Vector3 LeftDorsal = Vector3.up;
    }

    public enum Handedness
    {
        Left = 0,
        Right = 1,
    }

    public enum HandFinger
    {
        Invalid = -1,
        Thumb = 0,
        Index = 1,
        Middle = 2,
        Ring = 3,
        Pinky = 4,
        Max = 4
    }

    [Flags]
    public enum HandFingerFlags
    {
        None = 0,
        Thumb = 1 << 0,
        Index = 1 << 1,
        Middle = 1 << 2,
        Ring = 1 << 3,
        Pinky = 1 << 4,
        All = (1 << 5) - 1
    }


    public enum PinchGrabParam
    {
        PinchDistanceStart = 0,
        PinchDistanceStopMax,
        PinchDistanceStopOffset,
        PinchHqDistanceStart,
        PinchHqDistanceStopMax,
        PinchHqDistanceStopOffset,
        PinchHqViewAngleThreshold,
        ThumbDistanceStart,
        ThumbDistanceStopMax,
        ThumbDistanceStopOffset,
        ThumbMaxDot,
    }

    public enum PalmGrabParamID
    {
        PoseVolumeOffsetRightVec3 = 0,
        PoseVolumeOffsetLeftVec3,
        StartThresholdFloat,
        ReleaseThresholdFloat,
    }

    [Flags]
    public enum HandFingerJointFlags
    {
        None = 0,
        Palm = 1 << HandJointId.HandPalm,
        Wrist = 1 << HandJointId.HandWristRoot,
        Thumb1 = 1 << HandJointId.HandThumb1,
        Thumb2 = 1 << HandJointId.HandThumb2,
        Thumb3 = 1 << HandJointId.HandThumb3,
        ThumbTip = 1 << HandJointId.HandThumbTip,
        Index0 = 1 << HandJointId.HandIndex0,
        Index1 = 1 << HandJointId.HandIndex1,
        Index2 = 1 << HandJointId.HandIndex2,
        Index3 = 1 << HandJointId.HandIndex3,
        IndexTip = 1 << HandJointId.HandIndexTip,
        Middle0 = 1 << HandJointId.HandMiddle0,
        Middle1 = 1 << HandJointId.HandMiddle1,
        Middle2 = 1 << HandJointId.HandMiddle2,
        Middle3 = 1 << HandJointId.HandMiddle3,
        MiddleTip = 1 << HandJointId.HandMiddleTip,
        Ring0 = 1 << HandJointId.HandRing0,
        Ring1 = 1 << HandJointId.HandRing1,
        Ring2 = 1 << HandJointId.HandRing2,
        Ring3 = 1 << HandJointId.HandRing3,
        RingTip = 1 << HandJointId.HandRingTip,
        Pinky0 = 1 << HandJointId.HandPinky0,
        Pinky1 = 1 << HandJointId.HandPinky1,
        Pinky2 = 1 << HandJointId.HandPinky2,
        Pinky3 = 1 << HandJointId.HandPinky3,
        PinkyTip = 1 << HandJointId.HandPinkyTip,
        HandMaxSkinnable = 1 << HandJointId.HandMaxSkinnable,
        All = (1 << HandJointId.HandEnd) - 1
    }

    /// <summary>
    /// Utility methods for working with <see cref="HandFinger"/> data.
    /// </summary>
    public static class HandFingerUtils
    {
        /// <summary>
        /// Convert the provided <see cref="HandFinger"/> to <see cref="HandFingerFlags"/>.
        /// </summary>
        /// <param name="handFinger">The finger to convert to flags.</param>
        /// <returns>Flags representing the provided finger.</returns>
        public static HandFingerFlags ToFlags(HandFinger handFinger)
        {
            return (HandFingerFlags)(1 << (int)handFinger);
        }
    }

    public enum HandJointId
    {
        Invalid = -1,
        HandStart = 0,

        /// <summary>
        /// Palm
        /// </summary>
        [Tooltip("Palm")]
        HandPalm = HandStart + 0,

        /// <summary>
        /// Wrist Joint
        /// </summary>
        [Tooltip("Wrist Joint")]
        HandWristRoot = HandStart + 1,

        /// <summary>
        /// Thumb Metacarpal Joint
        /// </summary>
        [Tooltip("Thumb Metacarpal Joint")]
        HandThumb1 = HandStart + 2,

        /// <summary>
        /// Thumb Proximal Joint
        /// </summary>
        [Tooltip("Thumb Proximal Joint")]
        HandThumb2 = HandStart + 3,

        /// <summary>
        /// Thumb Distal Joint
        /// </summary>
        [Tooltip("Thumb Distal Joint")]
        HandThumb3 = HandStart + 4,

        /// <summary>
        /// Thumb Tip
        /// </summary>
        [Tooltip("Thumb Tip")]
        HandThumbTip = HandStart + 5,

        /// <summary>
        /// Index Finger Metacarpal Joint
        /// </summary>
        [Tooltip("Index Finger Metacarpal Joint")]
        HandIndex0 = HandStart + 6,

        /// <summary>
        /// Index Finger Proximal Joint
        /// </summary>
        [Tooltip("Index Finger Proximal Joint")]
        HandIndex1 = HandStart + 7,

        /// <summary>
        /// Index Finger Intermediate Joint
        /// </summary>
        [Tooltip("Index Finger Intermediate Joint")]
        HandIndex2 = HandStart + 8,

        /// <summary>
        /// Index Finger Distal Joint
        /// </summary>
        [Tooltip("Index Finger Distal Joint")]
        HandIndex3 = HandStart + 9,

        /// <summary>
        /// Index Finger Tip
        /// </summary>
        [Tooltip("Index Finger Tip")]
        HandIndexTip = HandStart + 10,

        /// <summary>
        /// Middle Finger Metacarpal Joint
        /// </summary>
        [Tooltip("Middle Finger Metacarpal Joint")]
        HandMiddle0 = HandStart + 11,

        /// <summary>
        /// Middle Finger Proximal Joint
        /// </summary>
        [Tooltip("Middle Finger Proximal Joint")]
        HandMiddle1 = HandStart + 12,

        /// <summary>
        /// Middle Finger Intermediate Joint
        /// </summary>
        [Tooltip("Middle Finger Intermediate Joint")]
        HandMiddle2 = HandStart + 13,

        /// <summary>
        /// Middle Finger Distal Joint
        /// </summary>
        [Tooltip("Middle Finger Distal Joint")]
        HandMiddle3 = HandStart + 14,

        /// <summary>
        /// Middle Finger Tip
        /// </summary>
        [Tooltip("Middle Finger Tip")]
        HandMiddleTip = HandStart + 15,

        /// <summary>
        /// Ring Finger Metacarpal Joint
        /// </summary>
        [Tooltip("Ring Finger Metacarpal Joint")]
        HandRing0 = HandStart + 16,

        /// <summary>
        /// Ring Finger Proximal Joint
        /// </summary>
        [Tooltip("Ring Finger Proximal Joint")]
        HandRing1 = HandStart + 17,

        /// <summary>
        /// Ring Finger Intermediate Joint
        /// </summary>
        [Tooltip("Ring Finger Intermediate Joint")]
        HandRing2 = HandStart + 18,

        /// <summary>
        /// Ring Finger Distal Joint
        /// </summary>
        [Tooltip("Ring Finger Distal Joint")]
        HandRing3 = HandStart + 19,

        /// <summary>
        /// Ring Finger Tip
        /// </summary>
        [Tooltip("Ring Finger Tip")]
        HandRingTip = HandStart + 20,

        /// <summary>
        /// Pinky Finger Metacarpal Joint
        /// </summary>
        [Tooltip("Pinky Finger Metacarpal Joint")]
        HandPinky0 = HandStart + 21,

        /// <summary>
        /// Pinky Finger Proximal Joint
        /// </summary>
        [Tooltip("Pinky Finger Proximal Joint")]
        HandPinky1 = HandStart + 22,

        /// <summary>
        /// Pinky Finger Intermediate Joint
        /// </summary>
        [Tooltip("Pinky Finger Intermediate Joint")]
        HandPinky2 = HandStart + 23,

        /// <summary>
        /// Pinky Finger Distal Joint
        /// </summary>
        [Tooltip("Pinky Finger Distal Joint")]
        HandPinky3 = HandStart + 24,

        /// <summary>
        /// Pinky Finger Tip
        /// </summary>
        [Tooltip("Pinky Finger Tip")]
        HandPinkyTip = HandStart + 25,

        HandEnd = HandStart + 26,
        HandMaxSkinnable = HandEnd,
    }

    /// <summary>
    /// Utility methods for working with <see cref="HandJointId"/>s.
    /// </summary>
    public class HandJointUtils
    {
        /// <summary>
        /// A list of joint arrays representing <see cref="HandJointId"/>s for each finger.
        /// The list is indexed by the integer value of <see cref="HandFinger"/>.
        /// </summary>
        public static List<HandJointId[]> FingerToJointList = new List<HandJointId[]>()
        {
            new[] {HandJointId.HandThumb1,HandJointId.HandThumb2,HandJointId.HandThumb3,HandJointId.HandThumbTip},
            new[] {HandJointId.HandIndex0,HandJointId.HandIndex1, HandJointId.HandIndex2, HandJointId.HandIndex3, HandJointId.HandIndexTip},
            new[] {HandJointId.HandMiddle0,HandJointId.HandMiddle1, HandJointId.HandMiddle2, HandJointId.HandMiddle3, HandJointId.HandMiddleTip},
            new[] {HandJointId.HandRing0,HandJointId.HandRing1,HandJointId.HandRing2,HandJointId.HandRing3, HandJointId.HandRingTip},
            new[] {HandJointId.HandPinky0, HandJointId.HandPinky1, HandJointId.HandPinky2, HandJointId.HandPinky3, HandJointId.HandPinkyTip}
        };

        /// <summary>
        /// An array indexed by the integer value of <see cref="HandJointId"/>
        /// which provides the corresponding <see cref="HandFinger"/>, if applicable.
        /// If the joint used to index into this array is not a finger joint,
        /// the value at that index will be <see cref="HandFinger.Invalid"/>
        /// </summary>
        public static HandFinger[] JointToFingerList = new[]
        {
            HandFinger.Invalid, //HandPalm == 0
            HandFinger.Invalid, //HandWrist
            HandFinger.Thumb, //ThumbMetacarpal
            HandFinger.Thumb, //ThumbProximal
            HandFinger.Thumb, //ThumbDistal
            HandFinger.Thumb, //ThumbTip
            HandFinger.Index, //IndexMetacarpal
            HandFinger.Index, //IndexProximal
            HandFinger.Index, //IndexIntermediate
            HandFinger.Index, //IndexDistal
            HandFinger.Index, //IndexTip
            HandFinger.Middle, //MiddleMetacarpal
            HandFinger.Middle, //MiddleProximal
            HandFinger.Middle, //MiddleIntermediate
            HandFinger.Middle, //MiddleDistal
            HandFinger.Middle, //MiddleTip
            HandFinger.Ring, //RingMetacarpal
            HandFinger.Ring, //RingProximal
            HandFinger.Ring, //RingIntermediate
            HandFinger.Ring, //RingDistal
            HandFinger.Ring, //RingTip
            HandFinger.Pinky, //PinkyMetacarpal
            HandFinger.Pinky, //PinkyProximal
            HandFinger.Pinky, //PinkyIntermediate
            HandFinger.Pinky, //PinkyDistal
            HandFinger.Pinky, //PinkyTip
        };

        /// <summary>
        /// An array indexed by the integer value of <see cref="HandJointId"/>
        /// which provides the parent <see cref="HandJointId"/>, if applicable.
        /// If the joint used to index into this array does not have a valid parent,
        /// the value at that index will be <see cref="HandFinger.Invalid"/>
        /// </summary>
        public static HandJointId[] JointParentList = new[]
        {
            HandJointId.HandWristRoot, //HandPalm == 0
            HandJointId.Invalid, //HandWrist
            HandJointId.HandWristRoot, //ThumbMetacarpal
            HandJointId.HandThumb1, //ThumbProximal
            HandJointId.HandThumb2, //ThumbDistal
            HandJointId.HandThumb3, //ThumbTip
            HandJointId.HandWristRoot, //IndexMetacarpal
            HandJointId.HandIndex0, //IndexProximal
            HandJointId.HandIndex1, //IndexIntermediate
            HandJointId.HandIndex2, //IndexDistal
            HandJointId.HandIndex3, //IndexTip
            HandJointId.HandWristRoot, //MiddleMetacarpal
            HandJointId.HandMiddle0, //MiddleProximal
            HandJointId.HandMiddle1, //MiddleIntermediate
            HandJointId.HandMiddle2, //MiddleDistal
            HandJointId.HandMiddle3, //MiddleTip
            HandJointId.HandWristRoot, //RingMetacarpal
            HandJointId.HandRing0, //RingProximal
            HandJointId.HandRing1, //RingIntermediate
            HandJointId.HandRing2, //RingDistal
            HandJointId.HandRing3, //RingTip
            HandJointId.HandWristRoot, //PinkyMetacarpal
            HandJointId.HandPinky0, //PinkyProximal
            HandJointId.HandPinky1, //PinkyIntermediate
            HandJointId.HandPinky2, //PinkyDistal
            HandJointId.HandPinky3, //PinkyTip
        };

        /// <summary>
        /// An array indexed by the integer value of <see cref="HandJointId"/>
        /// which provides an array of child <see cref="HandJointId"/>s, if applicable.
        /// If the joint used to index into this array does not have children,
        /// the value at that index will be a zero-length array.
        /// </summary>
        public static HandJointId[][] JointChildrenList = new[]
        {
            new HandJointId[0], //HandPalm
            new [] //HandWrist
            {
                HandJointId.HandPalm,
                HandJointId.HandThumb1,
                HandJointId.HandIndex0,
                HandJointId.HandMiddle0,
                HandJointId.HandRing0,
                HandJointId.HandPinky0
            },
            new []{ HandJointId.HandThumb2 }, //ThumbMetacarpal
            new []{ HandJointId.HandThumb3 }, //ThumbProximal
            new []{ HandJointId.HandThumbTip }, //ThumbDistal
            new HandJointId[0], //ThumbTip
            new []{ HandJointId.HandIndex1 }, //IndexMetacarpal
            new []{ HandJointId.HandIndex2 }, //IndexProximal
            new []{ HandJointId.HandIndex3 }, //IndexIntermediate
            new []{ HandJointId.HandIndexTip }, //IndexDistal
            new HandJointId[0], //IndexTip
            new []{ HandJointId.HandMiddle1 }, //MiddleMetacarpal
            new []{ HandJointId.HandMiddle2 }, //MiddleProximal
            new []{ HandJointId.HandMiddle3 }, //MiddleIntermediate
            new []{ HandJointId.HandMiddleTip }, //MiddleDistal
            new HandJointId[0], //MiddleTip
            new []{ HandJointId.HandRing1 }, //RingMetacarpal
            new []{ HandJointId.HandRing2 }, //RingProximal
            new []{ HandJointId.HandRing3 }, //RingIntermediate
            new []{ HandJointId.HandRingTip }, //RingDistal
            new HandJointId[0], //RingTip
            new []{ HandJointId.HandPinky1 }, //PinkyMetacarpal
            new []{ HandJointId.HandPinky2 }, //PinkyProximal
            new []{ HandJointId.HandPinky3 }, //PinkyIntermediate
            new []{ HandJointId.HandPinkyTip }, //PinkyDistal
            new HandJointId[0], //PinkyTip
        };

        [Obsolete("Use " + nameof(JointToFingerList) + "instead.")]
        public static List<HandJointId> JointIds = new List<HandJointId>()
        {
            HandJointId.HandIndex0,
            HandJointId.HandIndex1,
            HandJointId.HandIndex2,
            HandJointId.HandIndex3,
            HandJointId.HandMiddle0,
            HandJointId.HandMiddle1,
            HandJointId.HandMiddle2,
            HandJointId.HandMiddle3,
            HandJointId.HandRing0,
            HandJointId.HandRing1,
            HandJointId.HandRing2,
            HandJointId.HandRing3,
            HandJointId.HandPinky0,
            HandJointId.HandPinky1,
            HandJointId.HandPinky2,
            HandJointId.HandPinky3,
            HandJointId.HandThumb1,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3
        };

        /// <summary>
        /// This array is indexed by the integer value of <see cref="HandFinger"/>, and
        /// contains the Proximal <see cref="HandJointId"/> at each finger index.
        /// </summary>
        private static readonly HandJointId[] _handFingerProximals =
        {
            HandJointId.HandThumb2, HandJointId.HandIndex1, HandJointId.HandMiddle1,
            HandJointId.HandRing1, HandJointId.HandPinky1
        };

        /// <summary>
        /// Returns the Tip joint corresponding to the provided <see cref="HandFinger"/>.
        /// </summary>
        /// <param name="finger">The finger to retrieve the tip for.</param>
        /// <returns>The fingertip joint.</returns>
        public static HandJointId GetHandFingerTip(HandFinger finger)
        {
            switch (finger)
            {
                default:
                case HandFinger.Invalid:
                    return HandJointId.Invalid;
                case HandFinger.Thumb:
                    return HandJointId.HandThumbTip;
                case HandFinger.Index:
                    return HandJointId.HandIndexTip;
                case HandFinger.Middle:
                    return HandJointId.HandMiddleTip;
                case HandFinger.Ring:
                    return HandJointId.HandRingTip;
                case HandFinger.Pinky:
                    return HandJointId.HandPinkyTip;
            }
        }

        /// <summary>
        /// Returns true for HandJointIds that represent finger tips.
        /// </summary>
        public static bool IsFingerTip(HandJointId joint) => joint == HandJointId.HandThumbTip ||
                                                             joint == HandJointId.HandIndexTip ||
                                                             joint == HandJointId.HandMiddleTip ||
                                                             joint == HandJointId.HandRingTip ||
                                                             joint == HandJointId.HandPinkyTip;

        /// <summary>
        /// Returns the "proximal" JointId for the given finger.
        /// This is commonly known as the Knuckle.
        /// For fingers, proximal is the join with index 1; eg HandIndex1.
        /// For thumb, proximal is the joint with index 2; eg HandThumb2.
        /// </summary>
        public static HandJointId GetHandFingerProximal(HandFinger finger)
        {
            return _handFingerProximals[(int)finger];
        }

        /// <summary>
        /// Translates an array of joint poses in wrist space to an orientation-only
        /// <see cref="Quaternion"/> array in local space.
        /// </summary>
        /// <param name="jointPoses">Joint poses in wrist space.</param>
        /// <param name="joints">Joint orientations in local space (local to the joint's parent).</param>
        /// <returns>True if the arrays provided are valid for this operation.</returns>
        public static bool WristJointPosesToLocalRotations(Pose[] jointPoses, ref Quaternion[] joints)
        {
            if (jointPoses.Length < Constants.NUM_HAND_JOINTS ||
                joints.Length < Constants.NUM_HAND_JOINTS)
            {
                return false;
            }
            for (int i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                int parent = (int)HandJointUtils.JointParentList[i];
                joints[i] = parent < 0 ? Quaternion.identity :
                    Quaternion.Inverse(jointPoses[parent].rotation) *
                    jointPoses[i].rotation;
            }
            return true;
        }
    }

    /// <summary>
    /// Contains the data for a single joint within the <see cref="HandSkeleton"/> hierarchy.
    /// </summary>
    /// <remarks>
    /// <see cref="HandSkeleton"/>'s joint hierarchy is stored as an array, but structured as a tree; to calculate
    /// transform data for a join in the hierarchy, one need only start at the correct joint index in
    /// <see cref="HandSkeleton.Joints"/> and follow the <see cref="parent"/> indices backward up the hierarchy to
    /// the root, concatenating transforms throughout the process.
    /// </remarks>
    public struct HandSkeletonJoint
    {
        /// <summary>
        /// Index of the <see cref="HandSkeletonJoint"/> in the skeleton hierarchy (<see cref="HandSkeleton.Joints"/>).
        /// Must always have a lower index than this joint.
        /// </summary>
        public int parent;

        /// <summary>
        /// Stores the pose of the joint, in local space.
        /// </summary>
        /// <remarks>
        /// For an overview of the process of calculating joint poses in non-local space, see the remarks on <see cref="HandSkeletonJoint"/>.
        /// </remarks>
        public Pose pose;
    }

    public interface IReadOnlyHandSkeletonJointList
    {
        ref readonly HandSkeletonJoint this[int jointId] { get; }
    }

    public interface IReadOnlyHandSkeleton
    {
        IReadOnlyHandSkeletonJointList Joints { get; }
    }

    /// <summary>
    /// Curiously-recurring generic interface indicating that the implementing type is capable of copying data from other instances of the
    /// same type.
    /// </summary>
    /// <remarks>
    /// Because the curiously-recurring pattern requires the concrete type to be known in order to express the interface (i.e.,
    /// ICopyFrom<Foo> cannot be written without knowledge of the type Foo), this interface is only used within other generic types where
    /// <typeparamref name="TSelfType"/> is in turn generic. For a canonical example, see <see cref="DataModifier{TData}"/>.
    /// </remarks>
    /// <typeparam name="TSelfType"></typeparam>
    public interface ICopyFrom<in TSelfType>
    {
        /// <summary>
        /// Copies data from <paramref name="source"/> to the current ICopyFrom instance.
        /// </summary>
        /// <remarks>
        /// For a canonical example, see <see cref="HmdDataAsset"/>.
        /// </remarks>
        /// <param name="source"></param>
        void CopyFrom(TSelfType source);
    }

    public class ReadOnlyHandJointPoses : IReadOnlyList<Pose>
    {
        private Pose[] _poses;

        public ReadOnlyHandJointPoses(Pose[] poses)
        {
            _poses = poses;
        }

        public IEnumerator<Pose> GetEnumerator()
        {
            foreach (var pose in _poses)
            {
                yield return pose;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static ReadOnlyHandJointPoses Empty { get; } = new ReadOnlyHandJointPoses(Array.Empty<Pose>());

        public int Count => _poses.Length;

        public Pose this[int index] => _poses[index];

        public ref readonly Pose this[HandJointId index] => ref _poses[(int)index];
    }

    public class HandSkeleton : IReadOnlyHandSkeleton, IReadOnlyHandSkeletonJointList
    {
        public HandSkeletonJoint[] joints = new HandSkeletonJoint[Constants.NUM_HAND_JOINTS];
        public IReadOnlyHandSkeletonJointList Joints => this;
        public ref readonly HandSkeletonJoint this[int jointId] => ref joints[jointId];

        public static readonly HandSkeleton DefaultLeftSkeleton = new HandSkeleton()
        {
            joints = new HandSkeletonJoint[]
            {
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(0.000863f, -0.001272f, 0.047823f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = -1, pose = new Pose(new Vector3(0f, 0f, 0f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(0.030218f, -0.016084f, 0.034498f), new Quaternion(-0.06665304f, 0.3969201f, -0.5750258f, 0.71229f))},
                new HandSkeletonJoint() { parent = 2, pose = new Pose(new Vector3(-1.152053E-06f, -4.860463E-06f, 0.03251399f), new Quaternion(0.2201137f, -0.05016914f, 0.08162888f, 0.9707573f))},
                new HandSkeletonJoint() { parent = 3, pose = new Pose(new Vector3(-1.023574E-06f, -1.828242E-06f, 0.03379434f), new Quaternion(0.1129198f, 0.05065549f, -0.0791567f, 0.9891499f))},
                new HandSkeletonJoint() { parent = 4, pose = new Pose(new Vector3(-0.0006706798f, 0.001025644f, 0.02459195f), new Quaternion(-9.62965E-35f, -2.775558E-17f, -3.469447E-18f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(0.019819f, -0.009505f, 0.036448f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 6, pose = new Pose(new Vector3(0.003732f, 0.002189f, 0.059548f), new Quaternion(0.151882f, 0.07698268f, -0.0411778f, 0.9845354f))},
                new HandSkeletonJoint() { parent = 7, pose = new Pose(new Vector3(1.45847E-06f, -1.820559E-06f, 0.03792747f), new Quaternion(0.1307591f, -0.003759917f, 0.02628858f, 0.9910585f))},
                new HandSkeletonJoint() { parent = 8, pose = new Pose(new Vector3(9.285007E-07f, 1.593788E-07f, 0.02430516f), new Quaternion(-0.003017978f, -0.02607772f, 0.0164322f, 0.9995203f))},
                new HandSkeletonJoint() { parent = 9, pose = new Pose(new Vector3(-0.0002949532f, 0.001025431f, 0.02236465f), new Quaternion(0f, 4.336809E-19f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(0.00361f, -0.007648f, 0.034286f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 11, pose = new Pose(new Vector3(-0.001884f, 0.005105f, 0.061361f), new Quaternion(0.1896454f, -0.0117154f, 0.009750124f, 0.9817344f))},
                new HandSkeletonJoint() { parent = 12, pose = new Pose(new Vector3(-3.311234E-07f, -8.804644E-07f, 0.04292654f), new Quaternion(0.2042747f, -0.001967482f, 0.0123084f, 0.9788343f))},
                new HandSkeletonJoint() { parent = 13, pose = new Pose(new Vector3(1.703013E-07f, 5.871987E-07f, 0.02754843f), new Quaternion(-0.03223448f, -0.001938704f, 0.040453f, 0.9986595f))},
                new HandSkeletonJoint() { parent = 14, pose = new Pose(new Vector3(-0.0003095091f, 0.001137151f, 0.02496384f), new Quaternion(2.775558E-17f, 3.469447E-18f, 6.938894E-18f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(-0.014992f, -0.006016f, 0.034776f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 16, pose = new Pose(new Vector3(-0.002473f, -0.000513f, 0.053918f), new Quaternion(0.08123156f, -0.08615339f, 0.05587993f, 0.9913912f))},
                new HandSkeletonJoint() { parent = 17, pose = new Pose(new Vector3(5.266108E-07f, 3.652638E-07f, 0.03899503f), new Quaternion(0.3017412f, 0.007293773f, 0.03955524f, 0.9525411f))},
                new HandSkeletonJoint() { parent = 18, pose = new Pose(new Vector3(-1.039646E-06f, -5.423256E-07f, 0.02657356f), new Quaternion(0.09175414f, 0.02957179f, 0.008965106f, 0.9953021f))},
                new HandSkeletonJoint() { parent = 19, pose = new Pose(new Vector3(-0.0002563861f, 0.001608112f, 0.02432607f), new Quaternion(-2.942483E-05f, -2.775558E-17f, -8.16703E-22f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(-0.022999f, -0.009419999f, 0.034074f), new Quaternion(0.01833355f, -0.1403366f, 0.2070356f, 0.9680423f))},
                new HandSkeletonJoint() { parent = 21, pose = new Pose(new Vector3(2.530307E-06f, 1.160611E-06f, 0.04565198f), new Quaternion(-0.06267674f, -0.05101484f, -0.09903724f, 0.9917967f))},
                new HandSkeletonJoint() { parent = 22, pose = new Pose(new Vector3(1.720091E-07f, -6.646861E-07f, 0.03071994f), new Quaternion(0.3602582f, -0.025497f, 0.06776039f, 0.930039f))},
                new HandSkeletonJoint() { parent = 23, pose = new Pose(new Vector3(1.354028E-07f, 6.386686E-07f, 0.02031132f), new Quaternion(0.1151107f, 0.04873112f, -0.001109484f, 0.992156f))},
                new HandSkeletonJoint() { parent = 24, pose = new Pose(new Vector3(0.0002463258f, 0.001215198f, 0.02192333f), new Quaternion(-2.775558E-17f, 2.775558E-17f, -1.387779E-17f, 1f))},
            }
        };

        public static readonly HandSkeleton DefaultRightSkeleton = new HandSkeleton()
        {
            joints = new HandSkeletonJoint[]
            {
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(-0.000863f, -0.001272f, 0.047823f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = -1, pose = new Pose(new Vector3(0f, 0f, 0f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(-0.030218f, -0.016084f, 0.034498f), new Quaternion(-0.127078f, -0.4597233f, 0.5512751f, 0.6845447f))},
                new HandSkeletonJoint() { parent = 2, pose = new Pose(new Vector3(0.0009862719f, -0.005712928f, 0.03199217f), new Quaternion(0.3039057f, 0.06966191f, -0.09680318f, 0.9452078f))},
                new HandSkeletonJoint() { parent = 3, pose = new Pose(new Vector3(1.384091E-06f, -6.110277E-06f, 0.03379152f), new Quaternion(0.1129837f, -0.05061896f, 0.07914509f, 0.9891453f))},
                new HandSkeletonJoint() { parent = 4, pose = new Pose(new Vector3(0.0006700723f, 0.001027423f, 0.0245905f), new Quaternion(1.734723E-18f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(-0.019819f, -0.009505f, 0.036448f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 6, pose = new Pose(new Vector3(-0.003732f, 0.002189f, 0.059548f), new Quaternion(0.151882f, -0.07698268f, 0.0411778f, 0.9845354f))},
                new HandSkeletonJoint() { parent = 7, pose = new Pose(new Vector3(-1.294385E-06f, -1.527845E-06f, 0.03792841f), new Quaternion(0.1307591f, 0.003759917f, -0.02628858f, 0.9910585f))},
                new HandSkeletonJoint() { parent = 8, pose = new Pose(new Vector3(-9.285007E-07f, 1.593788E-07f, 0.02430516f), new Quaternion(-0.003017978f, 0.02607772f, -0.0164322f, 0.9995203f))},
                new HandSkeletonJoint() { parent = 9, pose = new Pose(new Vector3(0.0002948791f, 0.001024898f, 0.0223638f), new Quaternion(0f, -4.336809E-19f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(-0.00361f, -0.007648f, 0.034286f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 11, pose = new Pose(new Vector3(0.001884f, 0.005105f, 0.061361f), new Quaternion(0.1896454f, 0.0117154f, -0.009750124f, 0.9817344f))},
                new HandSkeletonJoint() { parent = 12, pose = new Pose(new Vector3(3.311234E-07f, -8.804644E-07f, 0.04292654f), new Quaternion(0.2042747f, 0.001967482f, -0.0123084f, 0.9788343f))},
                new HandSkeletonJoint() { parent = 13, pose = new Pose(new Vector3(-1.703013E-07f, 5.871987E-07f, 0.02754843f), new Quaternion(-0.03223448f, 0.001938704f, -0.040453f, 0.9986595f))},
                new HandSkeletonJoint() { parent = 14, pose = new Pose(new Vector3(0.0003095091f, 0.001137151f, 0.02496384f), new Quaternion(2.775558E-17f, -3.469447E-18f, -6.938894E-18f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(0.014992f, -0.006016f, 0.034776f), new Quaternion(0f, 0f, 0f, 1f))},
                new HandSkeletonJoint() { parent = 16, pose = new Pose(new Vector3(0.002473f, -0.000513f, 0.053918f), new Quaternion(0.08123156f, 0.08615339f, -0.05587993f, 0.9913912f))},
                new HandSkeletonJoint() { parent = 17, pose = new Pose(new Vector3(-5.266108E-07f, 3.652638E-07f, 0.03899503f), new Quaternion(0.3017412f, -0.007293773f, -0.03955524f, 0.9525411f))},
                new HandSkeletonJoint() { parent = 18, pose = new Pose(new Vector3(1.039646E-06f, -5.423256E-07f, 0.02657356f), new Quaternion(0.09172485f, -0.02957153f, -0.008965976f, 0.9953048f))},
                new HandSkeletonJoint() { parent = 19, pose = new Pose(new Vector3(0.0002563861f, 0.001606592f, 0.02432617f), new Quaternion(2.775558E-17f, 1.387779E-17f, -3.85186E-34f, 1f))},
                new HandSkeletonJoint() { parent = 1, pose = new Pose(new Vector3(0.022999f, -0.009419999f, 0.034074f), new Quaternion(0.01833355f, 0.1403366f, -0.2070356f, 0.9680423f))},
                new HandSkeletonJoint() { parent = 21, pose = new Pose(new Vector3(-2.530307E-06f, 1.160611E-06f, 0.04565198f), new Quaternion(-0.06267674f, 0.05101484f, 0.09903724f, 0.9917967f))},
                new HandSkeletonJoint() { parent = 22, pose = new Pose(new Vector3(-1.720091E-07f, -6.646861E-07f, 0.03071994f), new Quaternion(0.3602582f, 0.025497f, -0.06776039f, 0.930039f))},
                new HandSkeletonJoint() { parent = 23, pose = new Pose(new Vector3(-1.354028E-07f, 6.386686E-07f, 0.02031132f), new Quaternion(0.1151107f, -0.04873112f, 0.001109484f, 0.992156f))},
                new HandSkeletonJoint() { parent = 24, pose = new Pose(new Vector3(-0.0002463258f, 0.001215198f, 0.02192333f), new Quaternion(-2.775558E-17f, -2.775558E-17f, 1.387779E-17f, 1f))},
            }
        };


        public static HandSkeleton FromJoints(Transform[] joints)
        {
            HandSkeletonJoint[] skeletonJoints = new HandSkeletonJoint[joints.Length];

            for (int i = 0; i < joints.Length; i++)
            {
                Pose jointPose = joints[i].GetPose(Space.Self);
                skeletonJoints[i] = new HandSkeletonJoint()
                {
                    parent = FindParentIndex(i),
                    pose = jointPose
                };

            }

            HandSkeleton skeleton = new HandSkeleton() { joints = skeletonJoints };
            return skeleton;

            int FindParentIndex(int jointIndex)
            {
                Transform parent = joints[jointIndex].parent;
                if (parent == null)
                {
                    return -1;
                }
                for (int i = jointIndex - 1; i >= 0; i--)
                {
                    if (joints[i] == parent)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }
    }
}
