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

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Data for the pose of a hand for grabbing an object. It contains not only the position of the hand and
    /// the fingers but other relevant data to the pose, such as which fingers are locked.
    /// </summary>
    /// <remarks>
    /// Even though this class is Serializable, it is not a Component. The HandPoseEditor class should be used
    /// (in conjunction with the HandGrabInteractableEditor class) to edit the values in the inspector.
    /// HandPose data can only be used in conjunction with interactors associated with posable hands, such
    /// as <see cref="HandGrabInteractor"/>.
    /// </remarks>
    [Serializable]
    public class HandPose
    {
        [SerializeField]
        private Handedness _handedness;

        [SerializeField]
        private JointFreedom[] _fingersFreedom = FingersMetadata.DefaultFingersFreedom();

        [SerializeField]
        private Quaternion[] _jointRotations = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];

        /// <summary>
        /// The <see cref="Input.Handedness"/> of the hand represented by the contained pose data.
        /// </summary>
        public Handedness Handedness
        {
            get => _handedness;
            set => _handedness = value;
        }

        /// <summary>
        /// The collection of joints and their rotations in this hand.
        /// </summary>
        /// <remarks>
        /// This data follows the conventions from <see cref="FingersMetadata.HAND_JOINT_IDS"/>.
        /// </remarks>
        public Quaternion[] JointRotations
        {
            get
            {
                if (_jointRotations == null
                    || _jointRotations.Length == 0)
                {
                    _jointRotations = new Quaternion[FingersMetadata.HAND_JOINT_IDS.Length];
                }
                return _jointRotations;
            }
            set
            {
                _jointRotations = value;
            }
        }

        /// <summary>
        /// Indicates which fingers can be locked, constrained, or free in this hand pose.
        /// </summary>
        /// <remarks>
        /// Fingers in this data are represented at their index in the <see cref="HandFinger"/> enum.
        /// </remarks>
        public JointFreedom[] FingersFreedom
        {
            get
            {
                if (_fingersFreedom == null
                    || _fingersFreedom.Length == 0)
                {
                    _fingersFreedom = FingersMetadata.DefaultFingersFreedom();
                }
                return _fingersFreedom;
            }
        }

        /// <summary>
        /// Empty constructor for HandPose.
        /// </summary>
        /// <remarks>
        /// This constructor is automatically invoked by Unity when instantiating deserialized poses
        /// and contains features for exposing those poses to Editor tools.
        /// </remarks>
        public HandPose()
        {
#if UNITY_EDITOR
            IReadOnlyHandSkeletonJointList jointCollection = _handedness == Handedness.Left ?
                HandSkeleton.DefaultLeftSkeleton : HandSkeleton.DefaultRightSkeleton;
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                JointRotations[i] = jointCollection[(int)jointID].pose.rotation;
            }
#endif
        }

        /// <summary>
        /// A basic handedness-setting constructor for HandPoses.
        /// </summary>
        /// <remarks>
        /// This is a convenience method equivalent to invoking <see cref="HandPose.HandPose"/>, then
        /// setting <paramref name="handedness"/> as the <see cref="Handedness"/> of the newly-created
        /// instance.
        /// </remarks>
        /// <param name="handedness">The handedness to be set on the new instance.</param>
        public HandPose(Handedness handedness)
        {
            _handedness = handedness;
        }

        /// <summary>
        /// A basic copy constructor for HandPoses.
        /// </summary>
        /// <remarks>
        /// This is a convenience method equivalent to invoking <see cref="HandPose.HandPose"/>, then
        /// passing <paramref name="other"/> and false to <see cref="CopyFrom(HandPose, bool)"/> on the
        /// newly-created instance.
        /// </remarks>
        /// <param name="other">The HandPose from which the new instance should copy its data.</param>
        public HandPose(HandPose other)
        {
            this.CopyFrom(other);
        }

        /// <summary>
        /// Copies the values over to the hand pose without requiring any new allocations.
        /// </summary>
        /// <remarks>
        /// This is safe thanks to the flat and predictable way data is stored in <see cref="JointRotations"/>,
        /// <see cref="FingersFreedom"/> etc. within HandPose instances.
        /// </remarks>
        /// <param name="from">The hand pose to copy the values from.</param>
        /// <param name="mirrorHandedness">Invert the received handedness.</param>
        public void CopyFrom(HandPose from, bool mirrorHandedness = false)
        {
            if (!mirrorHandedness)
            {
                _handedness = from.Handedness;
            }

            Array.Copy(from.FingersFreedom, FingersFreedom, Constants.NUM_FINGERS);
            Array.Copy(from.JointRotations, JointRotations, FingersMetadata.HAND_JOINT_IDS.Length);
        }

        /// <summary>
        /// Interpolates between two HandPoses, if they have the same <see cref="Handedness"/> and joints.
        /// </summary>
        /// <param name="from">Base HandPose to interpolate from.</param>
        /// <param name="to">Target HandPose to interpolate to.</param>
        /// <param name="t">Interpolation factor, 0 for the base, 1 for the target.</param>
        /// <param name="result">A HandPose positioned/rotated between the base and target.</param>
        public static void Lerp(in HandPose from, in HandPose to, float t, ref HandPose result)
        {
            t = Mathf.Clamp01(t);
            for (int i = 0; i < from.JointRotations.Length && i < to.JointRotations.Length; i++)
            {
                result.JointRotations[i] = Quaternion.SlerpUnclamped(
                    from.JointRotations[i],
                    to.JointRotations[i],
                    t);
            }

            HandPose dominantPose = t <= 0.5f ? from : to;
            result._handedness = dominantPose.Handedness;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                result.FingersFreedom[i] = dominantPose.FingersFreedom[i];
            }
        }
    }
}
