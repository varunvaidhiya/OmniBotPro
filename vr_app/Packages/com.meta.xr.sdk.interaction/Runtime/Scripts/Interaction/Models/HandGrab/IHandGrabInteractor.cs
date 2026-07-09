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
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This interface characterizes an interactor which can use an <see cref="IHand"/> to grab interactables such as
    /// <see cref="HandGrabInteractable"/> or <see cref="DistanceHandGrabInteractable"/>. Interactors of this kind share a number of
    /// commonalities and requirements specific to hand grabbing interactions; IHandGrabInteractor concretizes the unified contract
    /// for such interactors.
    /// </summary>
    public interface IHandGrabInteractor : IHandGrabState
    {
        /// <summary>
        /// The <see cref="IHand"/> used for grabbing.
        /// </summary>
        IHand Hand { get; }

        /// <summary>
        /// The most fundamental grab pose for the interactor; in other words, the pose to which the grab is considered relative if
        /// no other point is applicable. Semantically, this pose is described as that of the "wrist"; however, as the human wrist is
        /// a joint and can curve, it's worth clarifying that this point evaluates the wrist (in both position and orientation) as
        /// an invariant pose relative to the immobile bones of the hand.
        /// </summary>
        Transform WristPoint { get; }

        /// <summary>
        /// The grab pose associated with pinch grabbing. When grabbing specifically with a pinching motion, this point (canonically
        /// located between the index and thumb) will be considered the origin of the grab, rather than the default
        /// <see cref="WristPoint"/> pose.
        /// </summary>
        Transform PinchPoint { get; }

        /// <summary>
        /// The grab pose associated with palm grabbing. When grabbing specifically with a palm grab, this point (canonically
        /// located on or near the surface of the hand palm) will be considered the origin of the grab, rather than the default
        /// <see cref="WristPoint"/> pose.
        /// </summary>
        Transform PalmPoint { get; }

        /// <summary>
        /// The <see cref="HandGrabAPI"/> responsible for defining what does and doesn't constitute grabbing given a particular hand
        /// state.
        /// </summary>
        HandGrabAPI HandGrabApi { get; }

        /// <summary>
        /// Returns a bit mask representing the different types of grab supported by this interactable. Note that, because the returned
        /// value is a bit mask, it should not be checked directly against enum values as it may not appear in the enum at all.
        /// Instead, bitwise operations should be used to check for the presence of enum values within the mask: i.e.,
        /// `(mask & <see cref="GrabTypeFlags.Pinch"/>) != 0`.
        /// </summary>
        GrabTypeFlags SupportedGrabTypes { get; }

        /// <summary>
        /// The interactable currently being targeted for grabbing. Typically, this is just another way of accessing
        /// the same value as <see cref="Interactor{TInteractor, TInteractable}.Interactable"/>.
        /// </summary>
        IHandGrabInteractable TargetInteractable { get; }
    }
}
