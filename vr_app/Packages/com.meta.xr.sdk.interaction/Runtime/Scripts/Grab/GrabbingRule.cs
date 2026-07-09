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

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// This enum indicates whether a finger within a given <see cref="GrabbingRule"/> is required, optional, or ignored when
    /// determining if the rule is satisfied.
    /// </summary>
    /// <remarks>
    /// Optional finger requirements allow for a grab to be started and continued with different fingers. Consider, for example, a
    /// <see cref="GrabbingRule"/> in which all four fingers are optional and the thumb is ignored. With this grab, it is possible
    /// to begin a grab using only the first two fingers, then close the remaining fingers into the grab, then release the first two
    /// fingers out of the grab while still keeping the others curled. This will all be considered one grab action, even though no
    /// single finger was grabbing throughout; the grab was allowed to start without the last two fingers because those two fingers
    /// are optional, and it was allowed to persist when the first two fingers released because those are _also_ optional.
    /// </remarks>
    public enum FingerRequirement
    {
        Ignored,
        Optional,
        Required
    }

    /// <summary>
    /// This enum indicates whether a grab defined by a <see cref="GrabbingRule"/> ceases when _any_ of its required fingers release,
    /// or when _all_ of them release. Fingers marked <see cref="FingerRequirement.Optional"/> or
    /// <see cref="FingerRequirement.Ignored"/> are not affected by this setting.
    /// </summary>
    public enum FingerUnselectMode
    {
        AllReleased,
        AnyReleased
    }

    /// <summary>
    /// This struct indicates which fingers should be taken in count for performing an interaction (for example, in
    /// <see cref="HandGrab.HandGrabInteractor"/>). All required fingers must be in use in order to start the interaction while any
    /// of the optional fingers is needed. For finishing the action it supports either releasing all fingers or any of them.
    /// </summary>
    [System.Serializable]
    public struct GrabbingRule
    {
        [SerializeField]
        private FingerRequirement _thumbRequirement;
        [SerializeField]
        private FingerRequirement _indexRequirement;
        [SerializeField]
        private FingerRequirement _middleRequirement;
        [SerializeField]
        private FingerRequirement _ringRequirement;
        [SerializeField]
        private FingerRequirement _pinkyRequirement;

        [SerializeField]
        private FingerUnselectMode _unselectMode;

        /// <summary>
        /// Returns the <see cref="FingerUnselectMode"/> for this instance, which dictates whether the grab defined by this rule
        /// ends when _any_ of its required fingers release, or when _all_ of them do.
        /// </summary>
        public FingerUnselectMode UnselectMode => _unselectMode;

        /// <summary>
        /// This property checks whether this grab can be performed using only fingers marked <see cref="FingerRequirement.Optional"/>.
        /// The property is true only if no fingers are marked as <see cref="FingerRequirement.Required"/>, false otherwise.
        /// </summary>
        public bool SelectsWithOptionals
        {
            get
            {
                return _thumbRequirement != FingerRequirement.Required
                    && _indexRequirement != FingerRequirement.Required
                    && _middleRequirement != FingerRequirement.Required
                    && _ringRequirement != FingerRequirement.Required
                    && _pinkyRequirement != FingerRequirement.Required;
            }
        }

        /// <summary>
        /// Indexer for retrieving and setting <see cref="FingerRequirement"/>s by their <see cref="HandFinger"/> type.
        /// </summary>
        /// <param name="fingerID">The <see cref="HandFinger"/> of the <see cref="FingerRequirement"/> to be accessed</param>
        /// <returns>Access to the <see cref="FingerRequirement"/> requested</returns>
        public FingerRequirement this[HandFinger fingerID]
        {
            get
            {
                switch (fingerID)
                {
                    case HandFinger.Thumb: return _thumbRequirement;
                    case HandFinger.Index: return _indexRequirement;
                    case HandFinger.Middle: return _middleRequirement;
                    case HandFinger.Ring: return _ringRequirement;
                    case HandFinger.Pinky: return _pinkyRequirement;
                }
                return FingerRequirement.Ignored;
            }
            set
            {
                switch (fingerID)
                {
                    case HandFinger.Thumb: _thumbRequirement = value; break;
                    case HandFinger.Index: _indexRequirement = value; break;
                    case HandFinger.Middle: _middleRequirement = value; break;
                    case HandFinger.Ring: _ringRequirement = value; break;
                    case HandFinger.Pinky: _pinkyRequirement = value; break;
                }
            }
        }

        /// <summary>
        /// In-place modifies a <see cref="HandFingerFlags"/> bit mask representing the grabbing state of the various fingers,
        /// suppressing any detected grabs by fingers marked <see cref="FingerRequirement.Ignored"/> in this rule.
        /// </summary>
        /// <param name="fingerFlags"></param>
        public void StripIrrelevant(ref HandFingerFlags fingerFlags)
        {
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (this[finger] == FingerRequirement.Ignored)
                {
                    fingerFlags = (HandFingerFlags)((int)fingerFlags & ~(1 << i));
                }
            }
        }

        /// <summary>
        /// Projecting constructor which creates a new GrabbingRule as a subset of another. Copies the <paramref name="otherRule"/>'s
        /// requirements for fingers included in the <paramref name="mask"/> bit mask, setting <see cref="FingerRequirement.Ignored"/>
        /// rules for all other fingers.
        /// </summary>
        /// <param name="mask">Bit mask specifying which requirements from <paramref name="otherRule"/> to copy</param>
        /// <param name="otherRule">Original rule from which requirements should be copied</param>
        public GrabbingRule(HandFingerFlags mask, in GrabbingRule otherRule)
        {
            _thumbRequirement = (mask & HandFingerFlags.Thumb) != 0 ?
                otherRule._thumbRequirement : FingerRequirement.Ignored;

            _indexRequirement = (mask & HandFingerFlags.Index) != 0 ?
                otherRule._indexRequirement : FingerRequirement.Ignored;

            _middleRequirement = (mask & HandFingerFlags.Middle) != 0 ?
                otherRule._middleRequirement : FingerRequirement.Ignored;

            _ringRequirement = (mask & HandFingerFlags.Ring) != 0 ?
                otherRule._ringRequirement : FingerRequirement.Ignored;

            _pinkyRequirement = (mask & HandFingerFlags.Pinky) != 0 ?
                otherRule._pinkyRequirement : FingerRequirement.Ignored;

            _unselectMode = otherRule.UnselectMode;
        }

        #region Defaults

        /// <summary>
        /// Standard definition for "palm grab": requires the middle three fingers to begin a grab, allowing that grab to persist
        /// as long as any finger continues grabbing.
        /// </summary>
        public static GrabbingRule DefaultPalmRule { get; } = new GrabbingRule()
        {
            _thumbRequirement = FingerRequirement.Optional,
            _indexRequirement = FingerRequirement.Required,
            _middleRequirement = FingerRequirement.Required,
            _ringRequirement = FingerRequirement.Required,
            _pinkyRequirement = FingerRequirement.Optional,

            _unselectMode = FingerUnselectMode.AllReleased
        };

        /// <summary>
        /// Standard definition for "pinch grab": allows grabbing to begin when any of the first three fingers (thumb, index, middle)
        /// grabs, persisting the grab as long as any of those three is grabbing, and ignoring the last two fingers.
        /// </summary>
        public static GrabbingRule DefaultPinchRule { get; } = new GrabbingRule()
        {
            _thumbRequirement = FingerRequirement.Optional,
            _indexRequirement = FingerRequirement.Optional,
            _middleRequirement = FingerRequirement.Optional,
            _ringRequirement = FingerRequirement.Ignored,
            _pinkyRequirement = FingerRequirement.Ignored,

            _unselectMode = FingerUnselectMode.AllReleased
        };

        /// <summary>
        /// Standard definition for "full grab": requires all five fingers to begin a grab, allowing that grab to persist
        /// as long as any finger continues grabbing.
        /// </summary>
        public static GrabbingRule FullGrab { get; } = new GrabbingRule()
        {
            _thumbRequirement = FingerRequirement.Required,
            _indexRequirement = FingerRequirement.Required,
            _middleRequirement = FingerRequirement.Required,
            _ringRequirement = FingerRequirement.Required,
            _pinkyRequirement = FingerRequirement.Required,

            _unselectMode = FingerUnselectMode.AllReleased
        };

        #endregion
    }
}
