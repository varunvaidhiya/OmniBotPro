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

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Defines the strategy for aligning the hand to the snapped object.
    /// The hand can go to the object upon selection, during hover or
    /// simply stay in its pose.
    /// </summary>
    public enum HandAlignType
    {
        None,
        AlignOnGrab,
        AttractOnHover,
        AlignFingersOnHover
    }

    /// <summary>
    /// Interface for interactors that allow aligning to an object. Contains information to drive the <see cref="HandGrabStateVisual"/>,
    /// moving the fingers and wrist of the rendered hand to emulate the real pose of the tracked hand as closely as possible while
    /// still representing a grab.
    /// </summary>
    public interface IHandGrabState
    {
        /// <summary>
        /// Property indicating whether or not the hand associated with this IHandGrabState instance is grabbing.
        /// </summary>
        /// <remarks>
        /// Certain <see cref="HandGrabStateVisual"/> features can apply even when the hand is not grabbing; for example, for a
        /// target with <see cref="HandGrabTarget.HandAlignment"/> set to <see cref="HandAlignType.AttractOnHover"/>, the
        /// <see cref="HandGrabStateVisual"/> can apply behavior even without grabbing.
        /// </remarks>
        bool IsGrabbing { get; }

        /// <summary>
        /// Value indicating the degree to which the fingers of the <see cref="HandGrabStateVisual"/> should conform to the grab
        /// pose versus the tracked pose.
        /// </summary>
        float FingersStrength { get; }

        /// <summary>
        /// Value indicating the degree to which the wrist of the <see cref="HandGrabStateVisual"/> should conform to the grab
        /// pose versus the tracked pose.
        /// </summary>
        float WristStrength { get; }

        /// <summary>
        /// Pose of the grab point relative to the wrist joint; used by <see cref="HandGrabInteraction"/> to calculate the
        /// position in world space from which <see cref="PointerEvent"/>s (among other outputs) should be generated.
        /// </summary>
        Pose WristToGrabPoseOffset { get; }

        /// <summary>
        /// The <see cref="HandGrabTarget"/> associated with this specific IHandGrabState instance.
        /// </summary>
        HandGrabTarget HandGrabTarget { get; }

        /// <summary>
        /// Returns a bit mask representing the fingers that are currently grabbing the interactable. Note that, because the returned
        /// value is a bit mask, it should not be checked directly against enum values as it may not appear in the enum at all.
        /// Instead, bitwise operations should be used to check for the presence of enum values within the mask: i.e.,
        /// `(mask & <see cref="HandFingerFlags.Thumb"/>) != 0`.
        /// </summary>
        HandFingerFlags GrabbingFingers();
    }

    public static class HandGrabStateExtensions
    {
        public static Pose GetVisualWristPose(this IHandGrabState grabState)
        {
            if (grabState.HandGrabTarget.HandPose != null)
            {
                return grabState.HandGrabTarget.GetWorldPoseDisplaced(Pose.identity);
            }
            else
            {
                Pose invertOffset = Pose.identity;
                PoseUtils.Inverse(grabState.WristToGrabPoseOffset, ref invertOffset);
                return grabState.HandGrabTarget.GetWorldPoseDisplaced(invertOffset);
            }
        }

        public static Pose GetTargetGrabPose(this IHandGrabState grabState)
        {
            if (grabState.HandGrabTarget.HandPose != null)
            {
                return grabState.HandGrabTarget.GetWorldPoseDisplaced(grabState.WristToGrabPoseOffset);
            }
            else
            {
                return grabState.HandGrabTarget.GetWorldPoseDisplaced(Pose.identity);
            }
        }
    }
}
