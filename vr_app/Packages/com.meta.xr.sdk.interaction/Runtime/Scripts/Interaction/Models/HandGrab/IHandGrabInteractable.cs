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

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This interface characterizes an interactable which can be grabbed with an <see cref="IHand"/> using an interactor such as
    /// <see cref="HandGrabInteractor"/> or <see cref="DistanceHandGrabInteractor"/>. Interactables of this kind share a number of
    /// commonalities and requirements specific to hand grabbing interactions; IHandGrabInteractable concretizes the unified contract
    /// for such interactables.
    /// </summary>
    public interface IHandGrabInteractable : IRelativeToRef
    {
        /// <summary>
        /// The <see cref="HandAlignType"/> by which this interactable will align itself to hand interactors which grab it. The
        /// default value for this property is <see cref="HandAlignType.AlignOnGrab"/>.
        /// </summary>
        HandAlignType HandAlignment { get; }

        /// <summary>
        /// Returns true if this uses any <see cref="HandGrabPose"/>s, false otherwise.
        /// </summary>
        bool UsesHandPose { get; }

        /// <summary>
        /// Defines the slippiness threshold so the interactor can slide along the interactable based on the
        /// strength of the grip. Grab surfaces are required to slide. At a Slippiness of 0, the interactor never moves.
        /// </summary>
        float Slippiness { get; }

        /// <summary>
        /// Checks whether or not the current interactable supports hand grab interaction by the specified hand.
        /// </summary>
        /// <param name="handedness">The handedness for which to confirm support</param>
        /// <returns>True if <paramref name="handedness"/> is supported by this interactable, false otherwise</returns>
        bool SupportsHandedness(Handedness handedness);

        /// <summary>
        /// Invokes <see cref="MovementProvider"/> to create an <see cref="IMovement"/> which can move this interactable from
        /// <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The pose from which movement should begin; typically this is the interactable's current pose</param>
        /// <param name="to">The pose to which the movement should move</param>
        /// <returns>The requested <see cref="IMovement"/> from <paramref name="from"/> to <paramref name="to"/></returns>
        IMovement GenerateMovement(in Pose from, in Pose to);

        /// <summary>
        /// This method determines which <see cref="HandGrabPose"/> in <see cref="HandGrabPoses"/> is the best choice for a given
        /// grabbing hand. This signature is obsolete;
        /// <see cref="CalculateBestPose(in Pose, in Pose, Transform, float, Handedness, ref HandGrabResult)"/> should be used instead.
        /// </summary>
        /// <param name="userPose">
        /// The position and orientation representing the grabbing hand; this can vary depending on whether the user is grabbing with
        /// pinch, palm, etc.
        /// </param>
        /// <param name="handScale">The scale of the grabbing hand</param>
        /// <param name="handedness">Whether the grabbing hand is a left hand or a right hand</param>
        /// <param name="result">The <see cref="HandGrabResult"/> representing the best pose</param>
        /// <returns>Always returns true</returns>
        [Obsolete("Use " + nameof(CalculateBestPose) + " with offset instead")]
        bool CalculateBestPose(Pose userPose, float handScale, Handedness handedness,
            ref HandGrabResult result);

        /// <summary>
        /// This method determines which <see cref="HandGrabPose"/> in <see cref="HandGrabPoses"/> is the best choice for a given
        /// grabbing hand.
        /// </summary>
        /// <param name="userPose">
        /// The position and orientation representing the grabbing hand; this can vary depending on whether the user is grabbing with
        /// pinch, palm, etc.
        /// </param>
        /// <param name="offset">The offset from <paramref name="userPose"/> for accurate scoring</param>
        /// <param name="relativeTo">The transform to which the found pose should be relative</param>
        /// <param name="handScale">The scale of the grabbing hand</param>
        /// <param name="handedness">Whether the grabbing hand is a left hand or a right hand</param>
        /// <param name="result">The <see cref="HandGrabResult"/> representing the best pose</param>
        void CalculateBestPose(in Pose userPose, in Pose offset, Transform relativeTo,
            float handScale, Handedness handedness,
            ref HandGrabResult result);

        /// <summary>
        /// Returns a bit mask representing the different types of grab supported by this interactable. Note that, because the returned
        /// value is a bit mask, it should not be checked directly against enum values as it may not appear in the enum at all.
        /// Instead, bitwise operations should be used to check for the presence of enum values within the mask: i.e.,
        /// `(mask & <see cref="GrabTypeFlags.Pinch"/>) != 0`.
        /// </summary>
        GrabTypeFlags SupportedGrabTypes { get; }

        /// <summary>
        /// The rules for pinch grabbing this interactable. If there are special conditions particular to this interactable as compared
        /// to pinch grabbing in general, this is where those conditions will be specified.
        /// </summary>
        GrabbingRule PinchGrabRules { get; }

        /// <summary>
        /// The rules for palm grabbing this interactable. If there are special conditions particular to this interactable as compared
        /// to palm grabbing in general, this is where those conditions will be specified.
        /// </summary>
        GrabbingRule PalmGrabRules { get; }
    }
}
