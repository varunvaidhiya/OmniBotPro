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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Leverages <see cref="TagSet"/>s to fill the <see cref="IGameObjectFilter"/> role of determining whether
    /// a GameObject should be included in or excluded from a certain operation - for example, whether an
    /// <see cref="Interactor{TInteractor, TInteractable}"/> should be allowed to interact with an
    /// <see cref="Interactable{TInteractor, TInteractable}"/>.
    /// </summary>
    /// <remarks>
    /// TagSetFilter supports both "require tags" and "exclude tags". "Require tags" are tags which must all be
    /// present on a GameObject's <see cref="TagSet"/> in order for the GameObject to not be excluded by
    /// <see cref="Filter(GameObject)"/>. Similarly, "exclude tags" are tags which, if any appear in a GameObject's
    /// <see cref="TagSet"/>, that GameObject will be excluded by <see cref="Filter(GameObject)"/>.
    /// </remarks>
    public class TagSetFilter : MonoBehaviour, IGameObjectFilter
    {
        /// <summary>
        /// A GameObject must meet all required tags.
        /// </summary>
        [Tooltip("A GameObject must meet all required tags.")]
        [SerializeField, Optional]
        private string[] _requireTags;

        /// <summary>
        /// A GameObject must not meet any exclude tags.
        /// </summary>
        [Tooltip("A GameObject must not meet any exclude tags.")]
        [SerializeField, Optional]
        [FormerlySerializedAs("_avoidTags")]
        private string[] _excludeTags;

        private readonly HashSet<string> _requireTagSet =
            new HashSet<string>();
        private readonly HashSet<string> _excludeTagSet =
            new HashSet<string>();

        protected virtual void Awake()
        {
            if (_requireTags != null)
            {
                foreach (string requireTag in _requireTags)
                {
                    _requireTagSet.Add(requireTag);
                }
            }

            if (_excludeTags != null)
            {
                foreach (string excludeTag in _excludeTags)
                {
                    _excludeTagSet.Add(excludeTag);
                }
            }
        }

        /// <summary>
        /// Checks whether a GameObject should be excluded ("filtered out") based on the presence
        /// or absence of tags in a <see cref="TagSet"/> on the GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to check for tags.</param>
        /// <returns>True if the GameObject should be _included_, false if it should be _excluded_ ("filtered out").</returns>
        public bool Filter(GameObject gameObject)
        {
            bool hasTagSet = gameObject.TryGetComponent(out TagSet tagSet);
            if (!hasTagSet && _requireTagSet.Count > 0)
            {
                return false;
            }

            foreach (string tag in _requireTagSet)
            {
                if (!tagSet.ContainsTag(tag))
                {
                    return false;
                }
            }

            if (!hasTagSet)
            {
                return true;
            }

            foreach (string tag in _excludeTagSet)
            {
                if (tagSet.ContainsTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the required tag set contains the specified <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The string to check for within the required tag set</param>
        /// <returns>True if the required set contains <paramref name="tag"/>, false otherwise</returns>
        public bool ContainsRequireTag(string tag) => _requireTagSet.Contains(tag);

        /// <summary>
        /// Adds a new string to the required tag set.
        /// </summary>
        /// <param name="tag">The string to be added as a new tag in the required set</param>
        public void AddRequireTag(string tag) => _requireTagSet.Add(tag);

        /// <summary>
        /// Removes a string from the required tag set.
        /// </summary>
        /// <param name="tag">The string to be removed as a tag from the required set</param>
        public void RemoveRequireTag(string tag) => _requireTagSet.Remove(tag);

        /// <summary>
        /// Checks whether the excluded tag set contains the specified <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The string to check for within the excluded tag set</param>
        /// <returns>True if the excluded set contains <paramref name="tag"/>, false otherwise</returns>
        public bool ContainsExcludeTag(string tag) => _excludeTagSet.Contains(tag);

        /// <summary>
        /// Adds a new string to the excluded tag set.
        /// </summary>
        /// <param name="tag">The string to be added as a new tag in the excluded set</param>
        public void AddExcludeTag(string tag) => _excludeTagSet.Add(tag);

        /// <summary>
        /// Removes a string from the excluded tag set.
        /// </summary>
        /// <param name="tag">The string to be removed as a tag from the excluded set</param>
        public void RemoveExcludeTag(string tag) => _excludeTagSet.Remove(tag);

        #region Inject

        /// <summary>
        /// Sets the list of "require tags" in a dynamically instantiated TagSetFilter. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalRequireTags(string[] requireTags)
        {
            _requireTags = requireTags;
            _requireTagSet.Clear();
            if (_requireTags != null)
            {
                foreach (string requireTag in _requireTags)
                {
                    _requireTagSet.Add(requireTag);
                }
            }
        }

        /// <summary>
        /// Sets the list of "exclude tags" in a dynamically instantiated TagSetFilter. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalExcludeTags(string[] excludeTags)
        {
            _excludeTags = excludeTags;
            _excludeTagSet.Clear();
            if (_excludeTags != null)
            {
                foreach (string excludeTag in _excludeTags)
                {
                    _excludeTagSet.Add(excludeTag);
                }
            }
        }

        #endregion
    }
}
