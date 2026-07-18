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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Defines the mode of feedback handling for a GameObject.
    /// Used by <see cref="FeedbackSettings"/> to determine whether to use default feedback,
    /// suppress all feedback, or override with custom actions.
    /// </summary>
    public enum FeedbackMode
    {
        /// <summary>
        /// The GameObject will use the default feedback behavior defined by the
        /// central <see cref="FeedbackManager"/> and its associated <see cref="FeedbackConfig"/>.
        /// </summary>
        Default = 0,
        /// <summary>
        /// All interaction feedback (default and override) for this GameObject will be suppressed.
        /// No feedback events will be triggered.
        /// </summary>
        Suppress = 1,
        /// <summary>
        /// Default feedback rules from <see cref="FeedbackConfig"/> will be ignored.
        /// Only the specific feedback actions listed in the <see cref="FeedbackSettings.OverrideEntry"/>
        /// list for matching <see cref="InteractionType"/>s will be executed.
        /// If no matching override entry is found for an interaction type, no feedback occurs.
        /// </summary>
        Override = 2
    }

    /// <summary>
    /// Attach this component to an Interactable <see cref="GameObject"/> to customize its
    /// interaction feedback behavior. It allows overriding or suppressing the default
    /// feedback defined by the global <see cref="FeedbackManager"/> and <see cref="FeedbackConfig"/>.
    /// </summary>
    [AddComponentMenu("Oculus/Interaction/SDK/Feedback/Feedback Settings")]
    public sealed class FeedbackSettings : MonoBehaviour
    {
        [Tooltip("Determines how feedback is handled for this GameObject: Default (use global config), Suppress (no feedback), or Override (use custom actions defined below).")]
        [SerializeField]
        private FeedbackMode _mode = FeedbackMode.Default;

        /// <summary>
        /// Defines a specific set of feedback actions to be executed for a particular
        /// <see cref="InteractionType"/> when <see cref="FeedbackSettings.Mode"/> is set to <see cref="FeedbackMode.Override"/>.
        /// </summary>
        [Serializable]
        public struct OverrideEntry
        {
            /// <summary>
            /// The type of interaction (e.g., HoverStart, SelectEnd) for which this override applies.
            /// </summary>
            [Tooltip("The type of interaction to override feedback for.")]
            public InteractionType _interaction; // Enum, typically doesn't need "Type" suffix by convention here

            /// <summary>
            /// A list of <see cref="FeedbackActionSO"/> assets that will be executed when
            /// an interaction of the specified <see cref="_interaction"/> type occurs and
            /// <see cref="FeedbackSettings.Mode"/> is <see cref="FeedbackMode.Override"/>.
            /// </summary>
            [Tooltip("The list of feedback actions to execute for this interaction type when Mode is Override.")]
            public List<FeedbackActionSO> _actions;
        }

        [Tooltip("A list of override entries. Each entry maps an InteractionType to a list of FeedbackActionSO scripts. These are only used if Mode is set to Override.")]
        [SerializeField]
        private List<OverrideEntry> _overrideEntries = new List<OverrideEntry>();

        /// <summary>
        /// Gets the currently selected feedback handling mode (<see cref="FeedbackMode.Default"/>,
        /// <see cref="FeedbackMode.Suppress"/>, or <see cref="FeedbackMode.Override"/>) for this GameObject.
        /// </summary>
        public FeedbackMode Mode => _mode;

        /// <summary>
        /// Attempts to retrieve a read-only list of custom <see cref="FeedbackActionSO"/>s
        /// defined for a specific <see cref="InteractionType"/>.
        /// This is only relevant if <see cref="Mode"/> is set to <see cref="FeedbackMode.Override"/>.
        /// </summary>
        /// <param name="type">The <see cref="InteractionType"/> to check for an override.</param>
        /// <param name="actions">When this method returns, if an override is found for the specified <paramref name="type"/>,
        /// contains the <see cref="IReadOnlyList{T}"/> of <see cref="FeedbackActionSO"/>s;
        /// otherwise, null.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <see cref="Mode"/> is <see cref="FeedbackMode.Override"/> and an entry
        /// for the specified <paramref name="type"/> with associated actions was found;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetOverrideActions(InteractionType type, out IReadOnlyList<FeedbackActionSO> actions)
        {
            if (_mode != FeedbackMode.Override || _overrideEntries == null)
            {
                actions = null;
                return false;
            }

            foreach (var entry in _overrideEntries)
            {
                if (entry._interaction == type)
                {
                    // Ensure actions list itself is not null, though an empty list is valid.
                    actions = entry._actions; // entry.Actions can be null if not initialized in inspector
                    return true; // Found the matching type
                }
            }

            actions = null;
            return false; // No override entry found for this type
        }

        /// <summary>
        /// Allows runtime configuration of the feedback settings for this GameObject.
        /// </summary>
        /// <param name="mode">The <see cref="FeedbackMode"/> to set.</param>
        /// <param name="overrideEntries">A list of <see cref="OverrideEntry"/>s to use if <paramref name="mode"/>
        /// is <see cref="FeedbackMode.Override"/>. This list will be copied. If null is provided
        /// while mode is <see cref="FeedbackMode.Override"/>, the existing overrides will be cleared.
        /// </param>
        public void InjectSettings(FeedbackMode mode, List<OverrideEntry> overrideEntries = null)
        {
            _mode = mode;
            if (overrideEntries != null)
            {
                // Create a new list to ensure no external modifications to the provided list affect this instance.
                _overrideEntries = new List<OverrideEntry>(overrideEntries);
            }
            else
            {
                // If null is passed, clear the list, especially important if mode is Override.
                _overrideEntries = new List<OverrideEntry>();
            }
        }
    }
}
