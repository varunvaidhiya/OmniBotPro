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

#if !ISDK_OPENXR_HAND
using System;
using UnityEngine;

namespace Oculus.Interaction.Input.UnityXR
{
    /// <summary>
    ///   <para>Utility class to convert OpenXR hand data to OVR format</para>
    /// </summary>
    internal static class OpenXRHandDataAssetUtils
    {
        internal static void CopyFrom(this HandDataAsset destination, FromOpenXRHandDataSource.OpenXRHandDataAsset source)
        {
            destination.IsDataValid = source.IsDataValid;
            destination.IsConnected = source.IsConnected;
            destination.IsTracked = source.IsTracked;
            destination.Config = source.Config;
            CopyOpenXRPoses(source, destination);
            CopyXRHandTrackingAim(source, destination);
            // XR_FB_hand_tracking_mesh
            destination.HandScale = 1;
        }

        private static readonly Quaternion OpenXRToOVRLeftRotTipInverted = Quaternion.Inverse(
            Quaternion.AngleAxis(180, Vector3.right) * Quaternion.AngleAxis(-90, Vector3.up));
        private static readonly Quaternion OpenXRToOVRRightRotTipInverted = Quaternion.Inverse(
            Quaternion.AngleAxis(90, Vector3.up));
        private static readonly Pose XRLeftHandLegacyBindPoseThumb0 = new()
        {

            rotation = new Quaternion(0.375387f, 0.424584f, -0.007779f, 0.823864f).normalized,
            position = new Vector3(0.020069f, 0.011554f, -0.010497f),
        };

        private static readonly Pose XRRightHandLegacyBindPoseThumb0 = new()
        {

            rotation = new Quaternion(0.375387f, 0.424584f, -0.007779f, 0.823864f).normalized,
            position = new Vector3(-0.020069f, -0.011554f, 0.010497f),
        };

        private static readonly Pose XRLeftHandLegacyBindPoseThumb1 = new()
        {
            rotation = new Quaternion(0.260230f, 0.024331f, 0.125678f, 0.957023f).normalized,
            position = new Vector3(0.024853f, 0f, -0f),
        };

        private static readonly Pose XRRightHandLegacyBindPoseThumb1 = new()
        {
            rotation = new Quaternion(0.260230f, 0.024331f, 0.125678f, 0.957023f).normalized,
            position = new Vector3(-0.024853f, 0f, 0f),
        };

        private static readonly Quaternion WristFixupRotation = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);

        private static void CopyOpenXRPoses(FromOpenXRHandDataSource.OpenXRHandDataAsset source, HandDataAsset destination)
        {
            destination.IsHighConfidence = true;
            destination.IsFingerHighConfidence[(int)HandFinger.Thumb] = true;
            destination.IsFingerHighConfidence[(int)HandFinger.Index] = true;
            destination.IsFingerHighConfidence[(int)HandFinger.Middle] = true;
            destination.IsFingerHighConfidence[(int)HandFinger.Ring] = true;
            destination.IsFingerHighConfidence[(int)HandFinger.Pinky] = true;

            var wristPose = source.JointPoses[HandJointIdToXRHandJointIndex(HandJointId.HandWristRoot)];
            wristPose = FlipZ(wristPose);
            var invRotTip = (source.Config.Handedness == Handedness.Left)
                ? OpenXRToOVRLeftRotTipInverted
                : OpenXRToOVRRightRotTipInverted;
            wristPose.rotation *= invRotTip;
            var invRotSpace = Quaternion.Inverse(wristPose.rotation);

            destination.Joints[(int)HandJointId.HandForearmStub] = Quaternion.identity;
            destination.Joints[(int)HandJointId.HandWristRoot] = Quaternion.identity * WristFixupRotation;

            destination.Root = FlipZ(wristPose);
            destination.RootPoseOrigin = source.RootPoseOrigin;

            wristPose.rotation = invRotSpace * wristPose.rotation;

            OpenXRToOVRFingerRecursive(destination, source, wristPose, invRotSpace, HandFinger.Thumb, HandJointId.HandThumbTip);
            OpenXRToOVRFingerRecursive(destination, source, wristPose, invRotSpace, HandFinger.Index, HandJointId.HandIndexTip);
            OpenXRToOVRFingerRecursive(destination, source, wristPose, invRotSpace, HandFinger.Middle, HandJointId.HandMiddleTip);
            OpenXRToOVRFingerRecursive(destination, source, wristPose, invRotSpace, HandFinger.Ring, HandJointId.HandRingTip);
            OpenXRToOVRFingerRecursive(destination, source, wristPose, invRotSpace, HandFinger.Pinky, HandJointId.HandPinkyTip);
        }

        private static void CopyXRHandTrackingAim(FromOpenXRHandDataSource.OpenXRHandDataAsset source, HandDataAsset destination)
        {
            destination.IsFingerPinching[(int)HandFinger.Index] =
                source.AimFlags.HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.AimFlagsFB.IndexPinching);
            destination.IsFingerPinching[(int)HandFinger.Middle] =
                source.AimFlags.HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.AimFlagsFB.MiddlePinching);
            destination.IsFingerPinching[(int)HandFinger.Ring] =
                source.AimFlags.HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.AimFlagsFB.RingPinching);
            destination.IsFingerPinching[(int)HandFinger.Pinky] =
                source.AimFlags.HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.AimFlagsFB.LittlePinching);
            Array.Copy(source.FingerPinchStrength, destination.FingerPinchStrength, destination.FingerPinchStrength.Length);

            destination.PointerPose = source.PointerPose;
            destination.PointerPoseOrigin = source.PointerPoseOrigin;
            destination.IsDominantHand = source.AimFlags.HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.AimFlagsFB.DominantHand);
        }
        private static void OpenXRToOVRFingerRecursive(HandDataAsset destination, FromOpenXRHandDataSource.OpenXRHandDataAsset source, Pose wristPose, Quaternion invRotSpace, HandFinger finger, HandJointId targetJoint, Pose? targetPose = null)
        {
            var invRotTip = (source.Config.Handedness == Handedness.Left)
                ? OpenXRToOVRLeftRotTipInverted
                : OpenXRToOVRRightRotTipInverted;
            var targetOVRJointIndex = (int)targetJoint;
            var ovrParentJoint = HandJointUtils.JointParentList[targetOVRJointIndex];
            Pose parentPose = Pose.identity;
            while (true)
            {
                if (!targetPose.HasValue)
                {
                    if (targetJoint == HandJointId.HandThumb0)
                    {
                        break;
                    }
                    var xrJoint = HandJointIdToXRHandJointIndex(targetJoint);
                    if (xrJoint < 0) // Invalid
                    {
                        break;
                    }

                    var hasPose = source.JointStates[xrJoint]
                        .HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.JointTrackingState.Pose);
                    if (!hasPose)
                    {
                        destination.IsFingerHighConfidence[(int)finger] = false;
                        destination.IsHighConfidence = false;
                        break;
                    }
                    targetPose = FlipZ(source.JointPoses[xrJoint]);
                }

                Quaternion parentRot = Quaternion.identity;
                if (ovrParentJoint == HandJointId.HandWristRoot)
                {
                    parentPose = wristPose;
                    parentRot = parentPose.rotation;
                }
                else if (ovrParentJoint != HandJointId.Invalid)
                {
                    if (ovrParentJoint == HandJointId.HandThumb0)
                    {
                        // thumb0 doesn't exist in OpenXR, so we have to do some work to deduce its rotation

                        // parent-space bind poses for thumb0/1

                        var thumb0BindPoseParentSpace = (source.Config.Handedness == Handedness.Left) ? XRLeftHandLegacyBindPoseThumb0 : XRRightHandLegacyBindPoseThumb0;
                        var thumb1BindPoseParentSpace = (source.Config.Handedness == Handedness.Left) ? XRLeftHandLegacyBindPoseThumb1 : XRRightHandLegacyBindPoseThumb1;

                        // compute parent-space pose of thumb1
                        // if target is thumb1, parent is thumb0
                        Pose thumb1PoseWorldSpace = targetPose.Value;

                        wristPose = source.JointPoses[HandJointIdToXRHandJointIndex(HandJointId.HandWristRoot)];
                        wristPose = FlipZ(wristPose);
                        wristPose.rotation *= invRotTip;

                        Pose wristPoseInv = wristPose;
                        wristPoseInv.Invert();

                        Pose thumb1PoseWristSpace = thumb1PoseWorldSpace.GetTransformedBy(wristPoseInv);

                        Pose thumb0BindPoseParentSpaceInv = thumb0BindPoseParentSpace;
                        thumb0BindPoseParentSpaceInv.Invert();
                        Pose thumb1PoseParentSpace = thumb1PoseWristSpace.GetTransformedBy(thumb0BindPoseParentSpaceInv);

                        // deduce thumb0 bind space rotation from the change in thumb1 parent-space position
                        var thumb0BindSpaceRot =
                            Quaternion.FromToRotation(thumb1BindPoseParentSpace.position, thumb1PoseParentSpace.position);
                        //thumb0Rot
                        parentPose.rotation = parentRot = thumb0BindPoseParentSpace.rotation * thumb0BindSpaceRot;
                    }
                    else
                    {
                        var parentXrJoint = HandJointIdToXRHandJointIndex(ovrParentJoint);
                        var hasPose = source.JointStates[parentXrJoint]
                            .HasFlag(FromOpenXRHandDataSource.OpenXRHandDataAsset.JointTrackingState.Pose);
                        if (!hasPose)
                        {
                            destination.IsFingerHighConfidence[(int)finger] = false;
                            destination.IsHighConfidence = false;
                            break;
                        }

                        parentPose = source.JointPoses[parentXrJoint];
                        parentPose = FlipZ(parentPose);
                        parentRot = parentPose.rotation;
                        parentRot = invRotSpace * parentRot;
                        parentRot *= invRotTip;
                    }
                }

                var targetRot = targetPose.Value.rotation;
                if (targetJoint != HandJointId.HandThumb0)
                {
                    targetRot = invRotSpace * targetRot;
                    targetRot *= invRotTip;
                    targetRot = Quaternion.Inverse(parentRot) * targetRot;
                }

                targetRot = FlipX(targetRot);
                destination.Joints[targetOVRJointIndex] = targetRot;
                break;
            }
            if (ovrParentJoint is HandJointId.Invalid or HandJointId.HandStart)
            {
                return;
            }
            OpenXRToOVRFingerRecursive(destination, source, wristPose, invRotSpace, finger, ovrParentJoint, parentPose);
        }

        private static int HandJointIdToXRHandJointIndex(HandJointId jointId)
        {
            switch (jointId)
            {
                case HandJointId.HandWristRoot:
                    return 0; // Wrist;
                case HandJointId.HandForearmStub:
                    return -1; // Invalid; // undefined
                case HandJointId.HandThumb0:
                    return -1; // Invalid; // undefined
                case HandJointId.HandThumb1:
                    return 2; // ThumbMetacarpal;
                case HandJointId.HandThumb2:
                    return 3; // ThumbProximal;
                case HandJointId.HandThumb3:
                    return 4; // ThumbDistal;
                // case undefined:
                //  return XR_HAND_JOINT_INDEX_METACARPAL_EXT;
                case HandJointId.HandIndex1:
                    return 7; // IndexProximal;
                case HandJointId.HandIndex2:
                    return 8; // IndexIntermediate;
                case HandJointId.HandIndex3:
                    return 9; // IndexDistal;
                // case undefined:
                //  return XR_HAND_JOINT_MIDDLE_METACARPAL_EXT;
                case HandJointId.HandMiddle1:
                    return 12; // MiddleProximal;
                case HandJointId.HandMiddle2:
                    return 13; // MiddleIntermediate;
                case HandJointId.HandMiddle3:
                    return 14; // MiddleDistal;
                // case undefined:
                //  return XR_HAND_JOINT_RING_METACARPAL_EXT;
                case HandJointId.HandRing1:
                    return 17; // RingProximal;
                case HandJointId.HandRing2:
                    return 18; // RingIntermediate;
                case HandJointId.HandRing3:
                    return 19; // RingDistal;
                case HandJointId.HandPinky0:
                    return 21; // LittleMetacarpal;
                case HandJointId.HandPinky1:
                    return 22; // LittleProximal;
                case HandJointId.HandPinky2:
                    return 23; // LittleIntermediate;
                case HandJointId.HandPinky3:
                    return 24; // LittleDistal;
                case HandJointId.HandThumbTip:
                    return 5; // ThumbTip;
                case HandJointId.HandIndexTip:
                    return 10; // IndexTip;
                case HandJointId.HandMiddleTip:
                    return 15; // MiddleTip;
                case HandJointId.HandRingTip:
                    return 20; // RingTip;
                case HandJointId.HandPinkyTip:
                    return 25; // LittleTip;
                default:
                    return -1; // Invalid;
            }
        }

        private static Quaternion FlipX(Quaternion q)
        {
            return new Quaternion() { x = q.x, y = -q.y, z = -q.z, w = q.w };
        }
        private static Quaternion FlipZ(Quaternion q)
        {
            return new Quaternion() { x = -q.x, y = -q.y, z = q.z, w = q.w };
        }

        private static Pose FlipZ(Pose p)
        {
            p.rotation = FlipZ(p.rotation);
            p.position = new Vector3() { x = p.position.x, y = p.position.y, z = -p.position.z };
            return p;
        }
    }
}
#endif
