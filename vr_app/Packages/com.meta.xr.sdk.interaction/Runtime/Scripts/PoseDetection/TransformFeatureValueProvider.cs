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
using System;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    public enum TransformFeature
    {
        WristUp,
        WristDown,
        PalmDown,
        PalmUp,
        PalmTowardsFace,
        PalmAwayFromFace,
        FingersUp,
        FingersDown,
        PinchClear
    };

    public class TransformFeatureValueProvider
    {
        public struct TransformProperties
        {
            public TransformProperties(Pose centerEyePos,
                Pose wristPose,
                Handedness handedness,
                Vector3 trackingSystemUp,
                Vector3 trackingSystemForward)
            {
                CenterEyePose = centerEyePos;
                WristPose = wristPose;
                Handedness = handedness;
                TrackingSystemUp = trackingSystemUp;
                TrackingSystemForward = trackingSystemForward;
            }

            public readonly Pose CenterEyePose;
            public readonly Pose WristPose;
            public readonly Handedness Handedness;
            public readonly Vector3 TrackingSystemUp;
            public readonly Vector3 TrackingSystemForward;
        }

        public static float GetValue(TransformFeature transformFeature, TransformJointData transformJointData,
            TransformConfig transformConfig)
        {
            TransformProperties transformProps =
                new TransformProperties(transformJointData.CenterEyePose, transformJointData.WristPose,
                    transformJointData.Handedness, transformJointData.TrackingSystemUp,
                    transformJointData.TrackingSystemForward);
            switch (transformFeature)
            {
                case TransformFeature.WristDown:
                    return GetWristDownValue(in transformProps, in transformConfig);
                case TransformFeature.WristUp:
                    return GetWristUpValue(in transformProps, in transformConfig);
                case TransformFeature.PalmDown:
                    return GetPalmDownValue(in transformProps, in transformConfig);
                case TransformFeature.PalmUp:
                    return GetPalmUpValue(in transformProps, in transformConfig);
                case TransformFeature.PalmTowardsFace:
                    return GetPalmTowardsFaceValue(in transformProps, in transformConfig);
                case TransformFeature.PalmAwayFromFace:
                    return GetPalmAwayFromFaceValue(in transformProps, in transformConfig);
                case TransformFeature.FingersUp:
                    return GetFingersUpValue(in transformProps, in transformConfig);
                case TransformFeature.FingersDown:
                    return GetFingersDownValue(in transformProps, in transformConfig);
                case TransformFeature.PinchClear:
                default:
                    return GetPinchClearValue(in transformProps, in transformConfig);
            }
        }

        [Obsolete("The " + nameof(TransformConfig) + " parameter is obsolete")]
        public static Vector3 GetHandVectorForFeature(TransformFeature transformFeature,
           in TransformJointData transformJointData,
           in TransformConfig transformConfig)
        {
            return GetHandVectorForFeature(transformFeature, in transformJointData);
        }

        public static Vector3 GetHandVectorForFeature(TransformFeature transformFeature,
            in TransformJointData transformJointData)
        {
            TransformProperties transformProps =
                new TransformProperties(transformJointData.CenterEyePose, transformJointData.WristPose,
                    transformJointData.Handedness, transformJointData.TrackingSystemUp,
                    transformJointData.TrackingSystemForward);
            return GetHandVectorForFeature(transformFeature, in transformProps);
        }

        private static Vector3 GetHandVectorForFeature(TransformFeature transformFeature,
            in TransformProperties transformProps)
        {
            Vector3 handVector;
            Quaternion rotation = transformProps.WristPose.rotation;
            bool isLeft = transformProps.Handedness == Handedness.Left;

            switch (transformFeature)
            {
                case TransformFeature.WristDown:
                    handVector = rotation * (isLeft ? Constants.LeftPinkySide : Constants.RightPinkySide);
                    break;
                case TransformFeature.WristUp:
                    handVector = rotation * (isLeft ? Constants.LeftThumbSide : Constants.RightThumbSide);
                    break;
                case TransformFeature.PalmDown:
                    handVector = rotation * (isLeft ? Constants.LeftDorsal : Constants.RightDorsal);
                    break;
                case TransformFeature.PalmUp:
                    handVector = rotation * (isLeft ? Constants.LeftPalmar : Constants.RightPalmar);
                    break;
                case TransformFeature.PalmTowardsFace:
                    handVector = rotation * (isLeft ? Constants.LeftDorsal : Constants.RightDorsal);
                    break;
                case TransformFeature.PalmAwayFromFace:
                    handVector = rotation * (isLeft ? Constants.LeftPalmar : Constants.RightPalmar);
                    break;
                case TransformFeature.FingersUp:
                    handVector = rotation * (isLeft ? Constants.LeftDistal : Constants.RightDistal);
                    break;
                case TransformFeature.FingersDown:
                    handVector = rotation * (isLeft ? Constants.LeftProximal : Constants.RightProximal);
                    break;
                case TransformFeature.PinchClear:
                    handVector = rotation * (isLeft ? Constants.LeftPinkySide : Constants.RightPinkySide);
                    break;
                default:
                    handVector = rotation * (isLeft ? Constants.LeftPinkySide : Constants.RightPinkySide);
                    break;
            }
            return handVector;
        }

        public static Vector3 GetTargetVectorForFeature(TransformFeature transformFeature,
            in TransformJointData transformJointData,
            in TransformConfig transformConfig)
        {
            TransformProperties transformProps =
                new TransformProperties(transformJointData.CenterEyePose, transformJointData.WristPose,
                    transformJointData.Handedness, transformJointData.TrackingSystemUp,
                    transformJointData.TrackingSystemForward);
            return GetTargetVectorForFeature(transformFeature, in transformProps, in transformConfig);
        }

        private static Vector3 GetTargetVectorForFeature(TransformFeature transformFeature,
           in TransformProperties transformProps,
           in TransformConfig transformConfig)
        {
            Vector3 targetVector = Vector3.zero;
            switch (transformFeature)
            {
                case TransformFeature.WristDown:
                case TransformFeature.PalmDown:
                case TransformFeature.FingersDown:
                case TransformFeature.WristUp:
                case TransformFeature.PalmUp:
                case TransformFeature.FingersUp:
                    targetVector = OffsetVectorWithRotation(transformProps,
                        GetVerticalVector(transformProps.CenterEyePose,
                            transformProps.TrackingSystemUp,
                            in transformConfig),
                        in transformConfig);
                    break;
                case TransformFeature.PalmTowardsFace:
                case TransformFeature.PalmAwayFromFace:
                case TransformFeature.PinchClear:
                    targetVector = OffsetVectorWithRotation(transformProps,
                        transformProps.CenterEyePose.forward,
                        in transformConfig);
                    break;
                default:
                    break;
            }
            return targetVector;
        }

        private static float GetWristDownValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.WristDown,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.WristDown,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetWristUpValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.WristUp,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.WristUp,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmDownValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.PalmDown,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.PalmDown,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmUpValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.PalmUp,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.PalmUp,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmTowardsFaceValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.PalmTowardsFace,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.PalmTowardsFace,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPalmAwayFromFaceValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.PalmAwayFromFace,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.PalmAwayFromFace,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetFingersUpValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.FingersUp,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.FingersUp,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetFingersDownValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.FingersDown,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.FingersDown,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static float GetPinchClearValue(in TransformProperties transformProps,
            in TransformConfig transformConfig)
        {
            Vector3 handVector = GetHandVectorForFeature(TransformFeature.PinchClear,
                in transformProps);
            Vector3 targetVector = GetTargetVectorForFeature(TransformFeature.PinchClear,
                in transformProps, in transformConfig);
            return Vector3.Angle(handVector, targetVector);
        }

        private static Vector3 GetVerticalVector(in Pose centerEyePose,
            in Vector3 trackingSystemUp,
            in TransformConfig transformConfig)
        {
            switch (transformConfig.UpVectorType)
            {
                case UpVectorType.Head:
                    return centerEyePose.up;
                case UpVectorType.Tracking:
                    return trackingSystemUp;
                case UpVectorType.World:
                    return Vector3.up;
                default:
                    return Vector3.up;
            }
        }

        private static Vector3 OffsetVectorWithRotation(in TransformProperties transformProps,
            in Vector3 originalVector,
            in TransformConfig transformConfig)
        {
            Quaternion baseRotation;
            switch (transformConfig.UpVectorType)
            {
                case UpVectorType.Head:
                    baseRotation = transformProps.CenterEyePose.rotation;
                    break;
                case UpVectorType.Tracking:
                    baseRotation =
                        Quaternion.LookRotation(transformProps.TrackingSystemForward,
                                                transformProps.TrackingSystemUp);
                    break;
                case UpVectorType.World:
                default:
                    baseRotation = Quaternion.identity;
                    break;
            }

            Quaternion offset = Quaternion.Euler(transformConfig.RotationOffset);
            return baseRotation * offset * Quaternion.Inverse(baseRotation) * originalVector;
        }
    }
}
