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

namespace Oculus.Interaction.Input
{
    public enum JointFreedom
    {
        Free,
        Constrained,
        Locked
    }

    /// <summary>
    /// This class contains a series of useful fingers-related data structures
    /// to be used for optimal calculations without relying in dictionaries.
    ///
    /// Since we always assume the hand pose information to be sorted in
    /// the HAND_JOINT_IDS order, we can align multiple data structures
    /// that follow that convention.
    /// </summary>
    public class FingersMetadata
    {
        public static JointFreedom[] DefaultFingersFreedom()
        {
            return new JointFreedom[Constants.NUM_FINGERS]
            {
                JointFreedom.Locked,
                JointFreedom.Locked,
                JointFreedom.Constrained,
                JointFreedom.Constrained,
                JointFreedom.Free
            };
        }

        /// <summary>
        /// The index within HAND_JOINT_IDS for the given joint
        /// </summary>
        /// <param name="id">The HandJoint to check the id for</param>
        /// <returns>An index in HAND_JOINT_IDS or -1 if invalud</returns>
        public static int HandJointIdToIndex(HandJointId id)
        {
            return JOINT_TO_INDEX[(int)id];
        }

        /// <summary>
        /// Valid identifiers for the i-bone of a hand.
        /// </summary>
        public static readonly HandJointId[] HAND_JOINT_IDS = new HandJointId[]
        {
#if ISDK_OPENXR_HAND
            HandJointId.HandThumb1,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
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
            HandJointId.HandPinky3
#else
            HandJointId.HandThumb0,
            HandJointId.HandThumb1,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
            HandJointId.HandIndex1,
            HandJointId.HandIndex2,
            HandJointId.HandIndex3,
            HandJointId.HandMiddle1,
            HandJointId.HandMiddle2,
            HandJointId.HandMiddle3,
            HandJointId.HandRing1,
            HandJointId.HandRing2,
            HandJointId.HandRing3,
            HandJointId.HandPinky0,
            HandJointId.HandPinky1,
            HandJointId.HandPinky2,
            HandJointId.HandPinky3
#endif
        };

        /// <summary>
        /// This collection contains the joints of each finger minus the tip in ascending order
        /// </summary>
        public static readonly HandJointId[][] FINGER_TO_JOINTS = InitializeFingerToJoint();

        /// <summary>
        /// This collection is used to convert from Finger id to the list indices
        /// of its joint in the HAND_JOINT_IDS list.
        /// </summary>
        public static readonly int[][] FINGER_TO_JOINT_INDEX = InitializeFingerToJointIndex();

        /// <summary>
        /// Array order following HAND_JOINT_IDS that indicates if the i joint
        /// can spread (rotate around Y). Should be true for the root of the fingers
        /// but Pink and Thumb are special cases
        /// </summary>
        public static readonly bool[] HAND_JOINT_CAN_SPREAD = InitializeCanSpread();

        /// <summary>
        /// Array order following HAND_JOINT_IDS that indicates if the i joint
        /// can move or it is fixed to the wrist. Should be true for the Metacarpals
        /// but Pink and Thumb are special cases
        /// </summary>
        public static readonly bool[] HAND_JOINT_CAN_MOVE = InitializeCanMove();

        /// <summary>
        /// Map any HandJointId to joint index of each finger, where the joint index
        /// starts at 0 (toward wrist) and increments for each joint toward the fingertip.
        /// </summary>
        public static readonly int[] JOINT_TO_FINGER_INDEX = InitializeJointToFingerIndex();

        /// <summary>
        /// Map HandJointId to HandFinger
        /// </summary>
        [Obsolete("Use " + nameof(HandJointUtils.JointToFingerList) + " instead")]
        public static readonly HandFinger[] JOINT_TO_FINGER = null;

        private static int[] JOINT_TO_INDEX = InitializeHandJointIdToIndex();

        #region Initializers
        private static int[] InitializeHandJointIdToIndex()
        {
            int[] indexes = new int[(int)HandJointId.HandEnd];

            for (HandJointId jointId = HandJointId.HandStart; jointId < HandJointId.HandEnd; jointId++)
            {
                indexes[(int)jointId] = Array.FindIndex(HAND_JOINT_IDS, (HandJointId joint) => joint == jointId);
            }
            return indexes;
        }

        private static HandJointId[][] InitializeFingerToJoint()
        {
            HandJointId[][] joints = new HandJointId[HandJointUtils.FingerToJointList.Count][];
            for (int i = 0; i < HandJointUtils.FingerToJointList.Count; i++)
            {
                //-1 to the Length to skip the Finger tip
                int fingerLength = HandJointUtils.FingerToJointList[i].Length - 1;
                joints[i] = new HandJointId[fingerLength];
                Array.Copy(HandJointUtils.FingerToJointList[i], joints[i], fingerLength);
            }
            return joints;
        }

        private static int[][] InitializeFingerToJointIndex()
        {
            HandJointId[][] fingerToJoints = InitializeFingerToJoint();
            int[] jointToIndex = InitializeHandJointIdToIndex();
            int[][] indexes = new int[fingerToJoints.Length][];
            for (int i = 0; i < fingerToJoints.Length; i++)
            {
                int[] fingerJoints = new int[fingerToJoints[i].Length];
                for (int j = 0; j < fingerToJoints[i].Length; j++)
                {
                    fingerJoints[j] = jointToIndex[(int)fingerToJoints[i][j]];
                }
                indexes[i] = fingerJoints;
            }
            return indexes;
        }

        private static int[] InitializeJointToFingerIndex()
        {
            int[] fingerIndexes = new int[(int)HandJointId.HandEnd];
            for (HandJointId jointId = HandJointId.HandStart; jointId < HandJointId.HandEnd; jointId++)
            {
                //Count the amount of joints in the hierarchy until it exits the finger
                int fingerIndex = -1;
                for (HandJointId i = jointId;
                    HandJointUtils.JointToFingerList[(int)i] != HandFinger.Invalid;
                    i = HandJointUtils.JointParentList[(int)i])
                {
                    fingerIndex++;
                }
                fingerIndexes[(int)jointId] = fingerIndex;
            }
            return fingerIndexes;
        }

        private static bool[] InitializeCanSpread()
        {
            int[] jointToFingerIndex = InitializeJointToFingerIndex();
            bool[] canSpread = new bool[HAND_JOINT_IDS.Length];
            for (int i = 0; i < HAND_JOINT_IDS.Length; i++)
            {
                HandJointId jointId = HAND_JOINT_IDS[i];
                int fingerIndex = jointToFingerIndex[(int)jointId];
#if ISDK_OPENXR_HAND
                canSpread[i] = fingerIndex <= 1 //most fingers start in the metacarpal
                    && jointId != HandJointId.HandThumb2;
#else
                canSpread[i] = fingerIndex == 0 //most fingers start in the proximal
                    || jointId == HandJointId.HandThumb1 //thumb starts in the metacarpal
                    || jointId == HandJointId.HandPinky1; //pinky starts in the metacarpal
#endif
            }
            return canSpread;
        }

        private static bool[] InitializeCanMove()
        {
            bool[] canMove = new bool[HAND_JOINT_IDS.Length];
#if ISDK_OPENXR_HAND
            for (int i = 0; i < HAND_JOINT_IDS.Length; i++)
            {
                HandJointId jointId = HAND_JOINT_IDS[i];
                canMove[i] = jointId != HandJointId.HandIndex0
                        && jointId != HandJointId.HandMiddle0
                        && jointId != HandJointId.HandRing0;
            }
#else
            Array.Fill(canMove, true);
#endif
            return canMove;
        }

        #endregion
    }
}
