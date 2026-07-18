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

using Oculus.Interaction.HandGrab;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// A ScriptableObject that defines a prioritized list of rules for triggering feedback
    /// based on interaction events. The first rule in the list that matches an incoming
    /// <see cref="InteractionEvent"/> (considering its <see cref="InteractionType"/>,
    /// the source GameObject's Unity Tag, and the <see cref="InteractorKind"/>) will be applied.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FeedbackConfig",
        menuName = "Meta/Interaction SDK/Feedback/Configuration")]
    public class FeedbackConfig : ScriptableObject
    {
        /// <summary>
        /// Defines a single rule for triggering feedback.
        /// A rule matches if its <see cref="Type"/>, <see cref="Tag"/> (if specified),
        /// and <see cref="Interactors"/> criteria are met by an <see cref="InteractionEvent"/>.
        /// </summary>
        [Serializable]
        public sealed class Rule
        {
            /// <summary>
            /// The specific <see cref="InteractionType"/> this rule applies to (e.g., HoverStart, SelectEnd).
            /// </summary>
            [Tooltip("HoverStart, SelectEnd, PointerSelectStart, …")]
            public InteractionType Type = InteractionType.None;

            /// <summary>
            /// Optional Unity Tag constraint. If not empty, the source GameObject of the interaction
            /// must have this tag for the rule to match. Leave blank to match any tag.
            /// </summary>
            [Tooltip("Leave blank to match every object. Otherwise " +
                     "the GameObject must have this Unity Tag.")]
            public string Tag = string.Empty;

            /// <summary>
            /// Specifies the kind(s) of interactor this rule applies to.
            /// Use <see cref="InteractorKind.Any"/> to match all interactor types.
            /// </summary>
            [Tooltip("Leave as Any to match every interactor type")]
            public InteractorKind Interactors = InteractorKind.Any;

            /// <summary>
            /// A list of <see cref="FeedbackActionSO"/> assets to execute when this rule matches.
            /// These actions define the actual feedback (haptic, audio, visual, etc.).
            /// </summary>
            [Tooltip("Extra feedback actions – Haptic / Audio / VFX …")]
            public List<FeedbackActionSO> Actions = new();

            /// <summary>
            /// If true, a default haptic pattern defined by <see cref="DefaultHaptics"/>
            /// will also be played when this rule matches, in addition to any <see cref="Actions"/>.
            /// </summary>
            [Tooltip("Also play this raw haptic pattern.")]
            public bool UseDefaultHaptics = false;

            /// <summary>
            /// The default haptic pattern to play if <see cref="UseDefaultHaptics"/> is true.
            /// </summary>
            public HapticPattern DefaultHaptics = new() { _amplitude = 0.4f, _duration = 0.05f };
        }

        /// <summary>
        /// Static mapping of Interactor types to their corresponding InteractorKind.
        /// Centralizes type-to-enum relationships for maintainability.
        /// </summary>
        private static readonly Dictionary<Type, InteractorKind> InteractorTypeMap = new()
        {
            { typeof(PokeInteractor), InteractorKind.Poke },
            { typeof(RayInteractor), InteractorKind.Ray },
            { typeof(GrabInteractor), InteractorKind.Grab },
            { typeof(HandGrabInteractor), InteractorKind.HandGrab },
            { typeof(HandGrabUseInteractor), InteractorKind.HandGrabUse },
            { typeof(TouchHandGrabInteractor), InteractorKind.TouchHandGrab },
            { typeof(DistanceGrabInteractor), InteractorKind.DistanceGrab },
            { typeof(DistanceHandGrabInteractor), InteractorKind.DistanceHandGrab },
        };

        /// <summary>
        /// Flags enum representing different kinds of interactors.
        /// Used by <see cref="FeedbackConfig.Rule"/> to filter based on the type of interactor
        /// that initiated an interaction.
        /// </summary>
        [System.Flags]
        public enum InteractorKind
        {
            /// <summary>Matches any interactor type.</summary>
            Any = 0,
            /// <summary>Represents a <see cref="PokeInteractor"/>.</summary>
            Poke = 1 << 0,
            /// <summary>Represents a <see cref="RayInteractor"/> or a UI pointer event.</summary>
            Ray = 1 << 1,
            /// <summary>Represents a <see cref="GrabInteractor"/>.</summary>
            Grab = 1 << 2,
            /// <summary>Represents a <see cref="HandGrabInteractor"/>.</summary>
            HandGrab = 1 << 3,
            /// <summary>Represents a <see cref="HandGrabUseInteractor"/>.</summary>
            HandGrabUse = 1 << 4,
            /// <summary>Represents a <see cref="TouchHandGrabInteractor"/>.</summary>
            TouchHandGrab = 1 << 5,
            /// <summary>Represents a <see cref="DistanceGrabInteractor"/>.</summary>
            DistanceGrab = 1 << 6,
            /// <summary>Represents a <see cref="DistanceHandGrabInteractor"/>.</summary>
            DistanceHandGrab = 1 << 7
        }

        [Tooltip("Top-to-bottom priority – first rule that matches wins.")]
        [SerializeField] private List<Rule> m_Rules = new();

        /// <summary>
        /// Lazy-initialized cache of the <see cref="m_Rules"/> list.
        /// This cache is rebuilt automatically after domain reloads.
        /// </summary>
        private List<Rule> m_Cache;

        private void OnEnable()
        {
            // Invalidate cache on enable (e.g., after domain reload or asset reimport)
            m_Cache = null;
        }

        /// <summary>
        /// Finds the first <see cref="Rule"/> that matches the provided interaction parameters.
        /// Rules are evaluated in the order they appear in the <see cref="m_Rules"/> list.
        /// A rule matches if its <see cref="Rule.Type"/> matches <paramref name="type"/>,
        /// its <see cref="Rule.Tag"/> matches the tag of <paramref name="source"/> (if the rule's tag is specified),
        /// and its <see cref="Rule.Interactors"/> flag matches the kind derived from <paramref name="view"/> or <paramref name="pointerId"/>.
        /// </summary>
        /// <param name="type">The <see cref="InteractionType"/> of the event.</param>
        /// <param name="source">The <see cref="GameObject"/> that is the source of the interaction (e.g., the interactable). Can be null.</param>
        /// <param name="view">The <see cref="IInteractorView"/> that initiated the interaction. Can be null.</param>
        /// <param name="pointerId">The pointer ID, relevant for UI pointer events. Typically -1 if not a UI pointer event.</param>
        /// <returns>The first matching <see cref="Rule"/>, or null if no rule matches.
        /// If multiple rules match without a specific tag, the first one encountered is returned as a fallback.
        /// Rules with specific matching tags take precedence over fallback rules.</returns>
        internal Rule FindRule(InteractionType type, GameObject source, IInteractorView view, int pointerId)
        {
            if (m_Cache == null)
            {
                m_Cache = new List<Rule>(m_Rules);
            }

            string srcTag = source ? source.tag : string.Empty;
            InteractorKind kind = KindOf(view, pointerId);
            Rule fallback = null;

            foreach (var rule in m_Cache)
            {
                if (rule.Type != type)
                {
                    continue;
                }

                // Interactor-kind filter
                if (rule.Interactors != InteractorKind.Any && (rule.Interactors & kind) == 0)
                {
                    continue;
                }

                // Unity Tag filter
                if (!string.IsNullOrEmpty(rule.Tag)) // Rule has a specific tag requirement
                {
                    if (srcTag.Equals(rule.Tag))
                    {
                        return rule; // This is a specific match, takes precedence
                    }
                    // Source tag does not match, or source has no tag
                    continue; // This rule doesn't apply
                }

                // If we reach here, the rule has no specific tag requirement, or its tag matched.
                // And its Type and InteractorKind matched.
                // This rule is a candidate. If it's the first one encountered without a specific tag,
                // it becomes the fallback.
                if (fallback == null)
                {
                    fallback = rule;
                }
            }

            return fallback; // Return the best fallback found, or null if nothing matched
        }

        /// <summary>
        /// Determines the <see cref="InteractorKind"/> based on the provided <see cref="IInteractorView"/>
        /// or pointer ID.
        /// </summary>
        /// <param name="view">The interactor view. Can be null.</param>
        /// <param name="pointerId">The pointer ID. Used if <paramref name="view"/> is null;
        /// a non-negative ID suggests a UI ray interaction.</param>
        /// <returns>The determined <see cref="InteractorKind"/>.</returns>
        internal static InteractorKind KindOf(IInteractorView view, int pointerId)
        {
            if (view != null)
            {
                // Check if the view's type exists in the map
                foreach (var keyValuePair in InteractorTypeMap)
                {
                    if (keyValuePair.Key.IsInstanceOfType(view))
                    {
                        return keyValuePair.Value;
                    }
                }
            }

            // If view is null or not a recognized specific type,
            return InteractorKind.Any;
        }

    }
}
