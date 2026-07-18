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

using Oculus.Interaction.Input;
using UnityEngine;
#if ISDK_OPENXR_HAND
using OVRHandJointId = Oculus.Interaction.Input.Compatibility.OVR.HandJointId;
using OVRConstants = Oculus.Interaction.Input.Compatibility.OVR.Constants;
using OVRHandSkeleton = Oculus.Interaction.Input.Compatibility.OVR.HandSkeleton;
using OVRHandJointUtils = Oculus.Interaction.Input.Compatibility.OVR.HandJointUtils;
using OpenXRHandJointId = Oculus.Interaction.Input.HandJointId;
using OpenXRFinger = Oculus.Interaction.Input.HandFinger;
using OpenXRConstants = Oculus.Interaction.Input.Constants;
using OpenXRHandSkeleton = Oculus.Interaction.Input.HandSkeleton;
using OpenXRHandJointUtils = Oculus.Interaction.Input.HandJointUtils;
#else
using OVRHandJointId = Oculus.Interaction.Input.HandJointId;
using OVRConstants = Oculus.Interaction.Input.Constants;
using OVRHandSkeleton = Oculus.Interaction.Input.HandSkeleton;
using OVRHandJointUtils = Oculus.Interaction.Input.HandJointUtils;
using OpenXRHandJointId = Oculus.Interaction.Input.Compatibility.OpenXR.HandJointId;
using OpenXRFinger = Oculus.Interaction.Input.Compatibility.OpenXR.HandFinger;
using OpenXRConstants = Oculus.Interaction.Input.Compatibility.OpenXR.Constants;
using OpenXRHandSkeleton = Oculus.Interaction.Input.Compatibility.OpenXR.HandSkeleton;
using OpenXRHandJointUtils = Oculus.Interaction.Input.Compatibility.OpenXR.HandJointUtils;
#endif

using static Oculus.Interaction.Input.HandMirroring;

namespace Oculus.Interaction
{
    public static class HandTranslationUtils
    {
        public const string UpgradeRequiredMessage = "Some fields do not contain the expected " +
            "values of converting to OpenXR from the previous serialized data. Convert the values?";
        public const string UpgradeRequiredButton = "Convert";

        public static GUIStyle FixButtonStyle => new GUIStyle(GUI.skin.button)
        {
            stretchWidth = true,
            stretchHeight = true,
            fixedWidth = 60f,
        };

        private static readonly HandSpace _openXRLeft = new HandSpace(
            OpenXRConstants.LeftDistal, OpenXRConstants.LeftDorsal, OpenXRConstants.LeftThumbSide);

        private static readonly HandSpace _openXRRight = new HandSpace(
            OpenXRConstants.RightDistal, OpenXRConstants.RightDorsal, OpenXRConstants.RightThumbSide);

        private static readonly HandSpace _ovrLeft = new HandSpace(
            OVRConstants.LeftDistal, OVRConstants.LeftDorsal, OVRConstants.LeftThumbSide);

        private static readonly HandSpace _ovrRight = new HandSpace(
            OVRConstants.RightDistal, OVRConstants.RightDorsal, OVRConstants.RightThumbSide);

        public static readonly HandsSpace openXRHands = new HandsSpace(_openXRLeft, _openXRRight);
        public static readonly HandsSpace ovrHands = new HandsSpace(_ovrLeft, _ovrRight);

        /// <summary>
        /// Maps the index of the HAND_JOINT_IDS collections between OpenXR and OVR.
        /// For the n position in the OpenXR array it returns the equivalent position in the OVR array.
        /// Returns -1 when there is no OVR equivalent.
        /// </summary>
        public static int[] HAND_JOINT_IDS_OpenXRtoOVR = new int[]
        {
           1,//0 * 1 for rotations
           2,
           3,
           -1,
           4,
           5,
           6,
           -1,
           7,
           8,
           9,
           -1,
           10,
           11,
           12,
           13,
           14,
           15,
           16,
        };

        public static int OpenXRHandJointToOVR(int openXRJointId)
        {
            return (int)((OpenXRHandJointId)openXRJointId switch
            {
                OpenXRHandJointId.HandPalm => OVRHandJointId.Invalid,
                OpenXRHandJointId.HandWristRoot => OVRHandJointId.HandWristRoot,
                OpenXRHandJointId.HandThumb1 => OVRHandJointId.HandThumb1,
                OpenXRHandJointId.HandThumb2 => OVRHandJointId.HandThumb2,
                OpenXRHandJointId.HandThumb3 => OVRHandJointId.HandThumb3,
                OpenXRHandJointId.HandThumbTip => OVRHandJointId.HandThumbTip,
                OpenXRHandJointId.HandIndex0 => OVRHandJointId.Invalid,
                OpenXRHandJointId.HandIndex1 => OVRHandJointId.HandIndex1,
                OpenXRHandJointId.HandIndex2 => OVRHandJointId.HandIndex2,
                OpenXRHandJointId.HandIndex3 => OVRHandJointId.HandIndex3,
                OpenXRHandJointId.HandIndexTip => OVRHandJointId.HandIndexTip,
                OpenXRHandJointId.HandMiddle0 => OVRHandJointId.Invalid,
                OpenXRHandJointId.HandMiddle1 => OVRHandJointId.HandMiddle1,
                OpenXRHandJointId.HandMiddle2 => OVRHandJointId.HandMiddle2,
                OpenXRHandJointId.HandMiddle3 => OVRHandJointId.HandMiddle3,
                OpenXRHandJointId.HandMiddleTip => OVRHandJointId.HandMiddleTip,
                OpenXRHandJointId.HandRing0 => OVRHandJointId.Invalid,
                OpenXRHandJointId.HandRing1 => OVRHandJointId.HandRing1,
                OpenXRHandJointId.HandRing2 => OVRHandJointId.HandRing2,
                OpenXRHandJointId.HandRing3 => OVRHandJointId.HandRing3,
                OpenXRHandJointId.HandRingTip => OVRHandJointId.HandRingTip,
                OpenXRHandJointId.HandPinky0 => OVRHandJointId.HandPinky0,
                OpenXRHandJointId.HandPinky1 => OVRHandJointId.HandPinky1,
                OpenXRHandJointId.HandPinky2 => OVRHandJointId.HandPinky2,
                OpenXRHandJointId.HandPinky3 => OVRHandJointId.HandPinky3,
                OpenXRHandJointId.HandPinkyTip => OVRHandJointId.HandPinkyTip,
                _ => OVRHandJointId.Invalid
            });
        }

        public static bool OVRHandRotationsToOpenXRPoses(Quaternion[] ovrJointRotations, Handedness handedness, ref Pose[] targetPoses)
        {
            if (ovrJointRotations.Length < OVRConstants.NUM_HAND_JOINTS ||
                targetPoses.Length < OpenXRConstants.NUM_HAND_JOINTS)
            {
                return false;
            }

            OVRHandSkeleton ovrSkeleton = handedness == Handedness.Left
                ? OVRHandSkeleton.DefaultLeftSkeleton : OVRHandSkeleton.DefaultRightSkeleton;

            OpenXRHandSkeleton openXRSkeleton = handedness == Handedness.Left ?
                OpenXRHandSkeleton.DefaultLeftSkeleton : OpenXRHandSkeleton.DefaultRightSkeleton;
            // preset the parent id
            targetPoses[(int)HandJointId.HandWristRoot] =
                openXRSkeleton.Joints[(int)HandJointId.HandWristRoot].pose;

            for (int jointIndex = 0; jointIndex < Constants.NUM_HAND_JOINTS; jointIndex++)
            {
                Pose basePose = openXRSkeleton[jointIndex].pose;
                int parent = (int)OpenXRHandJointUtils.JointParentList[jointIndex];
                if (parent < 0)
                {
                    continue;
                }

                OVRHandJointId ovrJointID = (OVRHandJointId)OpenXRHandJointToOVR(jointIndex);

                if (OpenXRHandJointUtils.JointToFingerList[jointIndex] == OpenXRFinger.Thumb)
                {
                    Pose pose = Pose.identity;
                    for (int ovrThumbID = (int)ovrJointID;
                        ovrThumbID >= 0;
                        ovrThumbID = (int)OVRHandJointUtils.JointParentList[ovrThumbID])
                    {
                        Pose ovrJoint = ovrSkeleton.Joints[ovrThumbID].pose;
                        ovrJoint.rotation = ovrJointRotations[ovrThumbID];
                        pose.Postmultiply(ovrJoint);
                    }
                    pose.rotation = pose.rotation.normalized;
                    targetPoses[jointIndex] = TransformPose(pose, ovrHands[handedness], openXRHands[handedness]);
                    continue;
                }
                else if (ovrJointID != OVRHandJointId.Invalid
                    && ovrJointID < OVRHandJointId.HandMaxSkinnable)
                {
                    basePose.rotation = TransformRotation(
                        ovrJointRotations[(int)ovrJointID],
                        ovrHands[handedness],
                        openXRHands[handedness]);
                }

                PoseUtils.Multiply(targetPoses[parent], basePose, ref targetPoses[jointIndex]);
            }
            return true;
        }

        public static HandJointId OVRHandJointToOpenXR(int ovrJointId)
        {
            return ovrJointId switch
            {
                0 => HandJointId.HandWristRoot,
                1 => HandJointId.Invalid, // Forearm Stub
                2 => HandJointId.Invalid, // Thumb Trapezium
                3 => HandJointId.HandThumb1,
                4 => HandJointId.HandThumb2,
                5 => HandJointId.HandThumb3,
                6 => HandJointId.HandIndex1,
                7 => HandJointId.HandIndex2,
                8 => HandJointId.HandIndex3,
                9 => HandJointId.HandMiddle1,
                10 => HandJointId.HandMiddle2,
                11 => HandJointId.HandMiddle3,
                12 => HandJointId.HandRing1,
                13 => HandJointId.HandRing2,
                14 => HandJointId.HandRing3,
                15 => HandJointId.HandPinky0,
                16 => HandJointId.HandPinky1,
                17 => HandJointId.HandPinky2,
                18 => HandJointId.HandPinky3,
                19 => HandJointId.HandThumbTip,
                20 => HandJointId.HandIndexTip,
                21 => HandJointId.HandMiddleTip,
                22 => HandJointId.HandRingTip,
                23 => HandJointId.HandPinkyTip,
                _ => HandJointId.Invalid
            };
        }

        public static Vector3 TransformOVRToOpenXRPosition(Vector3 position, Handedness handedness)
        {
            return TransformPosition(position, ovrHands[handedness], openXRHands[handedness]);
        }

        public static Quaternion TransformOVRToOpenXRRotation(Quaternion rotation, Handedness handedness)
        {
            return TransformRotation(rotation, ovrHands[handedness], openXRHands[handedness]);
        }
    }
}
