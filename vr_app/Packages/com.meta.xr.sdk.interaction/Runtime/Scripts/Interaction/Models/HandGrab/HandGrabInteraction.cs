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
using UnityEngine;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Helper class for Hand Grabbing types such as <see cref="HandGrabInteractor"/>/<see cref="HandGrabInteractable"/> and
    /// <see cref="DistanceHandGrabInteractor"/>/<see cref="DistanceHandGrabInteractable"/>. This class keeps track of the
    /// grabbing anchors and updates the target and movement during a Hand Grab interaction.
    /// </summary>
    public static class HandGrabInteraction
    {
        /// <summary>
        /// Obsolete: Calculates a new target. That is the point of the interactable at which the hand grab interaction will occur.
        /// This function is obsolete and should be replaced by calls to
        /// <see cref="CalculateBestGrab(IHandGrabInteractor, IHandGrabInteractable, GrabTypeFlags, out GrabTypeFlags, ref HandGrabResult)"/>
        /// in new code.
        /// </summary>
        /// <param name="interactable">The interactable to be HandGrabbed</param>
        /// <param name="grabTypes">The supported GrabTypes</param>
        /// <param name="anchorMode">The anchor to use for grabbing</param>
        /// <param name="handGrabResult">The a variable to store the result</param>
        /// <returns>True if a valid pose was found</returns>
        [Obsolete("Use " + nameof(CalculateBestGrab) + " instead")]
        public static bool TryCalculateBestGrab(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable interactable, GrabTypeFlags grabTypes,
            out HandGrabTarget.GrabAnchor anchorMode, ref HandGrabResult handGrabResult)
        {
            CalculateBestGrab(handGrabInteractor, interactable, grabTypes,
                out GrabTypeFlags activeGrabFlags, ref handGrabResult);

            if (activeGrabFlags.HasFlag(GrabTypeFlags.Pinch))
            {
                anchorMode = HandGrabTarget.GrabAnchor.Pinch;
            }
            else if (activeGrabFlags.HasFlag(GrabTypeFlags.Palm))
            {
                anchorMode = HandGrabTarget.GrabAnchor.Palm;
            }
            else
            {
                anchorMode = HandGrabTarget.GrabAnchor.Wrist;
            }

            return true;
        }

        /// <summary>
        /// Obsolete: this is a convenience method for retrieving the current grab type from an <see cref="IHandGrabInteractor"/>. This
        /// method should not be used in new code; the value can instead be directly retrieved from the <see cref="HandGrabTarget.Anchor"/>
        /// property of the interactor's <see cref="IHandGrabState.HandGrabTarget"/>: `handGrabInteractor.HandGrabTarget.Anchor`.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor to query for its current grab type</param>
        /// <returns>The current grab type of the interactor</returns>
        [Obsolete]
        public static GrabTypeFlags CurrentGrabType(this IHandGrabInteractor handGrabInteractor)
        {
            return handGrabInteractor.HandGrabTarget.Anchor;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/>s which calculates a new interaction target. That is the point of the
        /// interactable at which the hand grab interaction will occur.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="interactable">The interactable to be HandGrabbed</param>
        /// <param name="grabFlags">The supported GrabTypes</param>
        /// <param name="activeGrabFlags">The anchor to use for grabbing</param>
        /// <param name="result">The a variable to store the result</param>
        public static void CalculateBestGrab(this IHandGrabInteractor handGrabInteractor,
        IHandGrabInteractable interactable, GrabTypeFlags grabFlags,
        out GrabTypeFlags activeGrabFlags, ref HandGrabResult result)
        {
            activeGrabFlags = grabFlags & interactable.SupportedGrabTypes;
            GetPoseOffset(handGrabInteractor, activeGrabFlags, out Pose handPose, out Pose grabOffset);

            interactable.CalculateBestPose(handPose, grabOffset, interactable.RelativeTo,
                handGrabInteractor.Hand.Scale, handGrabInteractor.Hand.Handedness,
               ref result);
        }

        /// <summary>
        /// Initiates the movement of the <see cref="IHandGrabInteractable"/> with the current <see cref="HandGrabTarget"/>.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="interactable">The interactable to be HandGrabbed</param>
        public static IMovement GenerateMovement(this IHandGrabInteractor handGrabInteractor, IHandGrabInteractable interactable)
        {
            Pose originPose = handGrabInteractor.GetTargetGrabPose();
            Pose grabPose = handGrabInteractor.GetHandGrabPose();
            return interactable.GenerateMovement(originPose, grabPose);
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which retrivies the current pose of the current grabbing point.
        /// </summary>
        /// <returns>The pose of the current grabbing point in world space</returns>
        public static Pose GetHandGrabPose(this IHandGrabInteractor handGrabInteractor)
        {
            GetPoseOffset(handGrabInteractor, GrabTypeFlags.None, out Pose wristPose, out _);
            return PoseUtils.Multiply(wristPose, handGrabInteractor.WristToGrabPoseOffset);
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which calculates the GrabPoseScore for a given interactable with the
        /// specified grab modes.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="interactable">The interactable to measure to</param>
        /// <param name="grabTypes">The supported grab types for the grab</param>
        /// <param name="result">Calculating the score requires finding the best grab pose. It is stored here.</param>
        /// <returns>The best GrabPoseScore considering the grabtypes at the interactable</returns>
        public static GrabPoseScore GetPoseScore(this IHandGrabInteractor handGrabInteractor, IHandGrabInteractable interactable,
            GrabTypeFlags grabTypes, ref HandGrabResult result)
        {
            GrabTypeFlags activeGrabFlags = grabTypes & interactable.SupportedGrabTypes;
            GetPoseOffset(handGrabInteractor, activeGrabFlags, out Pose handPose, out Pose grabOffset);

            interactable.CalculateBestPose(handPose, grabOffset, interactable.RelativeTo,
                handGrabInteractor.Hand.Scale, handGrabInteractor.Hand.Handedness,
                ref result);

            return result.Score;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which indicates if an <see cref="IHandGrabInteractor"/> can interact
        /// with (hover and select) a given <see cref="IHandGrabInteractable"/>. This depends on the handedness and the valid grab types
        /// of both elements.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="handGrabInteractable">The interactable to be grabbed</param>
        /// <returns>True if the interactor could grab the interactable</returns>
        public static bool CanInteractWith(this IHandGrabInteractor handGrabInteractor, IHandGrabInteractable handGrabInteractable)
        {
            if (!handGrabInteractable.SupportsHandedness(handGrabInteractor.Hand.Handedness))
            {
                return false;
            }

            return (handGrabInteractor.SupportedGrabTypes & handGrabInteractable.SupportedGrabTypes) != GrabTypeFlags.None;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which calculates the offset from the wrist to the actual grabbing point
        /// defined by the current anchor in the interactor <see cref="HandGrabTarget"/>.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor whose HandGrabTarget to inspect</param>
        /// <returns>The local offset from the wrist to the grab point</returns>
        public static Pose GetGrabOffset(this IHandGrabInteractor handGrabInteractor)
        {
            GetPoseOffset(handGrabInteractor, handGrabInteractor.HandGrabTarget.Anchor,
                out _, out Pose wristToGrabPoseOffset);
            return wristToGrabPoseOffset;

        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which calculates the strength of the fingers of an interactor trying
        /// (or grabbing) an interactable. "Strength" is a measure of of how similar/different the finger's state is to what the system
        /// considers "grabbing"; for a more detailed overview, see the documentation for
        /// <see cref="IHand.GetFingerPinchStrength(HandFinger)"/>.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="handGrabInteractable">The interactable being grabbed</param>
        /// <param name="handGrabTypes">A filter for the grab types to calculate</param>
        /// <param name="includeSelecting">Compute also fingers that are selecting</param>
        /// <returns>The maximum strength for the grabbing fingers, normalized</returns>
        public static float ComputeHandGrabScore(IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable, out GrabTypeFlags handGrabTypes, bool includeSelecting = false)
        {
            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            handGrabTypes = GrabTypeFlags.None;
            float handGrabScore = 0f;

            if (SupportsPinch(handGrabInteractor, handGrabInteractable))
            {
                float pinchStrength = api.GetHandPinchScore(handGrabInteractable.PinchGrabRules, includeSelecting);
                if (pinchStrength > handGrabScore)
                {
                    handGrabScore = pinchStrength;
                    handGrabTypes = GrabTypeFlags.Pinch;
                }
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable))
            {
                float palmStrength = api.GetHandPalmScore(handGrabInteractable.PalmGrabRules, includeSelecting);
                if (palmStrength > handGrabScore)
                {
                    handGrabScore = palmStrength;
                    handGrabTypes = GrabTypeFlags.Palm;
                }
            }

            return handGrabScore;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which calculates whether the interactor should select a given
        /// <see cref="IHandGrabInteractable"/>. This is conceptually related to
        /// <see cref="Interactor{TInteractor, TInteractable}.ComputeShouldSelect"/>, but rather than being a specific part of the
        /// general processing flow it's used in several places throughout the grab interaction logic.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor</param>
        /// <param name="handGrabInteractable">The interactable</param>
        /// <returns>
        /// True if the <paramref name="handGrabInteractor"/> should select (grab) the <paramref name="handGrabInteractable"/>,
        /// false otherwise
        /// </returns>
        public static GrabTypeFlags ComputeShouldSelect(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            if (handGrabInteractable == null)
            {
                return GrabTypeFlags.None;
            }

            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            GrabTypeFlags selectingGrabTypes = GrabTypeFlags.None;
            if (SupportsPinch(handGrabInteractor, handGrabInteractable) &&
                 api.IsHandSelectPinchFingersChanged(handGrabInteractable.PinchGrabRules))
            {
                selectingGrabTypes |= GrabTypeFlags.Pinch;
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable) &&
                 api.IsHandSelectPalmFingersChanged(handGrabInteractable.PalmGrabRules))
            {
                selectingGrabTypes |= GrabTypeFlags.Palm;
            }

            return selectingGrabTypes;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which calculates whether the interactor should unselect a given
        /// <see cref="IHandGrabInteractable"/>. This is conceptually related to
        /// <see cref="Interactor{TInteractor, TInteractable}.ComputeShouldUnselect"/>, but rather than being a specific part of the
        /// general processing flow it's used in several places throughout the grab interaction logic.

        /// </summary>
        /// <param name="handGrabInteractor">The interactor</param>
        /// <param name="handGrabInteractable">The interactable</param>
        /// <returns>
        /// True if the <paramref name="handGrabInteractor"/> should unselect (release) the <paramref name="handGrabInteractable"/>,
        /// false otherwise
        /// </returns>
        public static GrabTypeFlags ComputeShouldUnselect(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            HandFingerFlags pinchFingers = api.HandPinchGrabbingFingers();
            HandFingerFlags palmFingers = api.HandPalmGrabbingFingers();

            if (handGrabInteractable.SupportedGrabTypes == GrabTypeFlags.None)
            {
                if (!api.IsSustainingGrab(GrabbingRule.FullGrab, pinchFingers) &&
                    !api.IsSustainingGrab(GrabbingRule.FullGrab, palmFingers))
                {
                    return GrabTypeFlags.All;
                }
                return GrabTypeFlags.None;
            }

            GrabTypeFlags unselectingGrabTypes = GrabTypeFlags.None;
            if (SupportsPinch(handGrabInteractor, handGrabInteractable.SupportedGrabTypes)
                && !api.IsSustainingGrab(handGrabInteractable.PinchGrabRules, pinchFingers)
                && api.IsHandUnselectPinchFingersChanged(handGrabInteractable.PinchGrabRules))
            {
                unselectingGrabTypes |= GrabTypeFlags.Pinch;
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable.SupportedGrabTypes)
                && !api.IsSustainingGrab(handGrabInteractable.PalmGrabRules, palmFingers)
                && api.IsHandUnselectPalmFingersChanged(handGrabInteractable.PalmGrabRules))
            {
                unselectingGrabTypes |= GrabTypeFlags.Palm;
            }

            return unselectingGrabTypes;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which builds a bit flag enumeration of the fingers which are
        /// currently considered to be grabbing. Note that, because the returned value is a bit mask, it should not be checked
        /// directly against enum values as it may not appear in the enum at all. Instead, bitwise operations should be used to
        /// check for the presence of enum values within the mask: i.e., `(mask & <see cref="HandFingerFlags.Thumb"/>) != 0`.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor</param>
        /// <param name="handGrabInteractable">The interactable being grabbed</param>
        /// <returns>A bit mask of <see cref="HandFingerFlags"/> values indicating which fingers are currently grabbing</returns>
        public static HandFingerFlags GrabbingFingers(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            if (handGrabInteractable == null)
            {
                return HandFingerFlags.None;
            }

            HandFingerFlags fingers = HandFingerFlags.None;

            if (SupportsPinch(handGrabInteractor, handGrabInteractable))
            {
                HandFingerFlags pinchingFingers = api.HandPinchGrabbingFingers();
                handGrabInteractable.PinchGrabRules.StripIrrelevant(ref pinchingFingers);
                fingers = fingers | pinchingFingers;
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable))
            {
                HandFingerFlags grabbingFingers = api.HandPalmGrabbingFingers();
                handGrabInteractable.PalmGrabRules.StripIrrelevant(ref grabbingFingers);
                fingers = fingers | grabbingFingers;
            }

            return fingers;
        }

        private static bool SupportsPinch(IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            return SupportsPinch(handGrabInteractor, handGrabInteractable.SupportedGrabTypes);
        }

        private static bool SupportsPalm(IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            return SupportsPalm(handGrabInteractor, handGrabInteractable.SupportedGrabTypes);
        }

        private static bool SupportsPinch(IHandGrabInteractor handGrabInteractor,
            GrabTypeFlags grabTypes)
        {
            return (handGrabInteractor.SupportedGrabTypes & grabTypes & GrabTypeFlags.Pinch) != 0;
        }

        private static bool SupportsPalm(IHandGrabInteractor handGrabInteractor,
            GrabTypeFlags grabTypes)
        {
            return (handGrabInteractor.SupportedGrabTypes & grabTypes & GrabTypeFlags.Palm) != 0;
        }

        /// <summary>
        /// Extension method for <see cref="IHandGrabInteractor"/> which calculates the root of a grab and the practical offset to
        /// the grabbing point.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="anchorMode">The grab types to be used</param>
        /// <param name="pose">The root of the grab pose to use</param>
        /// <param name="offset">The offset form the root for accurate scoring</param>
        public static void GetPoseOffset(this IHandGrabInteractor handGrabInteractor, GrabTypeFlags anchorMode,
            out Pose pose, out Pose offset)
        {
            handGrabInteractor.Hand.GetRootPose(out pose);
            offset = Pose.identity;

            if (anchorMode == GrabTypeFlags.None)
            {
                return;
            }
            else if ((anchorMode & GrabTypeFlags.Pinch) != 0
                && handGrabInteractor.PinchPoint != null)
            {
                offset = PoseUtils.Delta(pose, handGrabInteractor.PinchPoint.GetPose());
            }
            else if ((anchorMode & GrabTypeFlags.Palm) != 0
                && handGrabInteractor.PalmPoint != null)
            {
                offset = PoseUtils.Delta(pose, handGrabInteractor.PalmPoint.GetPose());
            };
        }
    }
}
