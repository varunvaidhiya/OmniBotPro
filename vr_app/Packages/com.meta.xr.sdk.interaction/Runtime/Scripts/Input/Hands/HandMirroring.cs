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
    public static class HandMirroring
    {
        /// <summary>
        /// Defines a LeftHand and RightHand space and allows to quickly
        /// reference the correct one based on handedness.
        /// This is an utility class to avoid repeating conditionals based
        /// on handedness.
        /// </summary>
        public struct HandsSpace
        {
            private readonly HandSpace _leftHand;
            private readonly HandSpace _rightHand;

            public readonly HandSpace this[Handedness handedness]
            {
                get
                {
                    return handedness == Handedness.Left ? _leftHand : _rightHand;
                }
            }

            public HandsSpace(HandSpace leftHand, HandSpace rightHand)
            {
                this._leftHand = leftHand;
                this._rightHand = rightHand;
            }
        }

        /// <summary>
        /// Defines the XYZ space of the hand using 3 well defined
        /// orthogonal directions.
        /// </summary>
        public struct HandSpace
        {
            /// <summary>
            /// The direction from the wrist towards the fingers.
            /// </summary>
            public readonly Vector3 distal;
            /// <summary>
            /// The direction away from the back of the palm.
            /// </summary>
            public readonly Vector3 dorsal;
            /// <summary>
            /// The direction from the pinky to the thumb.
            /// </summary>
            public readonly Vector3 thumbSide;
            /// <summary>
            /// The precalculated rotation of the space.
            /// </summary>
            public readonly Quaternion rotation;

            public HandSpace(Vector3 distal, Vector3 dorsal, Vector3 thumbSide)
            {
                this.distal = distal;
                this.dorsal = dorsal;
                this.thumbSide = thumbSide;
                this.rotation = Quaternion.LookRotation(distal, dorsal);
            }
        }

        public static readonly HandSpace LeftHandSpace = new HandSpace(
            Constants.LeftDistal, Constants.LeftDorsal, Constants.LeftThumbSide);

        public static readonly HandSpace RightHandSpace = new HandSpace(
            Constants.RightDistal, Constants.RightDorsal, Constants.RightThumbSide);

        /// <summary>
        /// Transforms a Pose from the LeftHand space to the RightHand space
        /// in the current Skeleton.
        /// </summary>
        /// <param name="pose">The pose to transform</param>
        /// <returns>The transformed pose</returns>
        public static Pose Mirror(Pose pose)
        {
            pose.position = Mirror(pose.position);
            pose.rotation = Mirror(pose.rotation);
            return pose;
        }

        /// <summary>
        /// Transforms a position from the LeftHand space to the RightHand space
        /// in the current Skeleton.
        /// </summary>
        /// <param name="position">The position to transform</param>
        /// <returns>The transformed position</returns>
        public static Vector3 Mirror(in Vector3 position)
        {
            return TransformPosition(position, LeftHandSpace, RightHandSpace);
        }

        /// <summary>
        /// Transforms a rotation from the LeftHand space to the RightHand space
        /// in the current Skeleton.
        /// </summary>
        /// <param name="rotation">The rotation to transform</param>
        /// <returns>The transformed rotation</returns>
        public static Quaternion Mirror(in Quaternion rotation)
        {
            return TransformRotation(rotation, LeftHandSpace, RightHandSpace);
        }

        /// <summary>
        /// Reflects a rotation in the plane defined by a vector while also
        /// changing its handedness in the current skeleton.
        /// Useful for changing the handedness of a hand while keeping the exact same
        /// orientation so it aligns as expected.
        /// </summary>
        /// <param name="rotation">The rotation to reflect</param>
        /// <param name="normal">The vector that defines the reflection plane</param>
        /// <returns>The reflected rotation</returns>
        public static Quaternion Reflect(in Quaternion rotation, Vector3 normal)
        {
            Vector3 reflectedDistal = Vector3.Reflect(rotation * RightHandSpace.distal, normal);
            Vector3 reflectedDorsal = Vector3.Reflect(rotation * RightHandSpace.dorsal, normal);
            Quaternion reflectedOrientation = Quaternion.LookRotation(reflectedDistal, reflectedDorsal)
                * Quaternion.Inverse(LeftHandSpace.rotation);

            return reflectedOrientation;
        }

        /// <summary>
        /// Transforms a pose from one HandSpace to another.
        /// </summary>
        /// <param name="pose">The pose to transform</param>
        /// <param name="fromHand">The original HandSpace of the pose</param>
        /// <param name="toHand">The target HandSpace of the pose</param>
        /// <returns>The transformed pose</returns>
        public static Pose TransformPose(in Pose pose,
            in HandSpace fromHand, in HandSpace toHand)
        {
            Pose transformedPose = new Pose(
                TransformPosition(pose.position, fromHand, toHand),
                TransformRotation(pose.rotation, fromHand, toHand));
            return transformedPose;
        }

        /// <summary>
        /// Transforms a position from one HandSpace to another.
        /// </summary>
        /// <param name="position">The position to transform</param>
        /// <param name="fromHand">The original HandSpace of the positionb</param>
        /// <param name="toHand">The target HandSpace of the position</param>
        /// <returns>The transformed position</returns>
        public static Vector3 TransformPosition(in Vector3 position,
            in HandSpace fromHand, in HandSpace toHand)
        {
            Vector3 distal = Vector3.Dot(position, fromHand.distal) * toHand.distal;
            Vector3 dorsal = Vector3.Dot(position, fromHand.dorsal) * toHand.dorsal;
            Vector3 thumbSide = Vector3.Dot(position, fromHand.thumbSide) * toHand.thumbSide;
            return distal + dorsal + thumbSide;
        }

        /// <summary>
        /// Transforms a rotation from one HandSpace to another.
        /// </summary>
        /// <param name="rotation">The rotation to transform</param>
        /// <param name="fromHand">The original HandSpace of the rotation</param>
        /// <param name="toHand">The target HandSpace of the rotation</param>
        /// <returns>The transformed rotation</returns>
        public static Quaternion TransformRotation(in Quaternion rotation,
            in HandSpace fromHand, in HandSpace toHand)
        {
            Vector3 forward = TransformPosition(rotation * Vector3.forward, fromHand, toHand);
            Vector3 up = TransformPosition(rotation * Vector3.up, fromHand, toHand);

            return Quaternion.LookRotation(forward, up)
                * Quaternion.Inverse(toHand.rotation) * fromHand.rotation;
        }
    }
}
