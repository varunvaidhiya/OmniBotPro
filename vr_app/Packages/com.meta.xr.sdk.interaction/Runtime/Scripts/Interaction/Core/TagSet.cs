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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// A set of string tags for use in filtering. At most one TagSet should ever be present on a single GameObject.
    /// </summary>
    /// <remarks>
    /// The most common use of tag filtering in the Interaction SDK is constraining which interactors can interact
    /// with which interactables by using the <see cref="Interactor{TInteractor, TInteractable}.InteractableFilters"/>
    /// and <see cref="Interactable{TInteractor, TInteractable}.InteractorFilters"/>. Any criteria which can be
    /// expressed in an <see cref="IGameObjectFilter"/> can be used for filtering, including string-based techniques
    /// leveraging TagSet, such as in <see cref="TagSetFilter"/>.
    /// </remarks>
    public class TagSet : MonoBehaviour
    {
        /// <summary>
        /// The tags that should apply to this GameObject.
        /// </summary>
        [Tooltip("The tags that should apply to this GameObject.")]
        [SerializeField]
        private List<string> _tags;

        private readonly HashSet<string> _tagSet =
            new HashSet<string>();

        protected virtual void Awake()
        {
            if (_tags != null)
            {
                foreach (string tag in _tags)
                {
                    _tagSet.Add(tag);
                }
            }
        }

        /// <summary>
        /// Checks whether the tag set contains the specified <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">The string to check for within the tag set</param>
        /// <returns>True if the set contains <paramref name="tag"/>, false otherwise</returns>
        public bool ContainsTag(string tag) => _tagSet.Contains(tag);

        /// <summary>
        /// Adds a new string to the tag set.
        /// </summary>
        /// <param name="tag">The string to be added as a new tag in the set</param>
        public void AddTag(string tag) => _tagSet.Add(tag);

        /// <summary>
        /// Removes a string from the tag set.
        /// </summary>
        /// <param name="tag">The string to be removed as a tag from the set</param>
        public void RemoveTag(string tag) => _tagSet.Remove(tag);

        #region Inject

        /// <summary>
        /// Sets the list of tags in a dynamically instantiated TagSet. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalTags(List<string> tags)
        {
            _tags = tags;
            _tagSet.Clear();
            if (_tags != null)
            {
                foreach (string tag in _tags)
                {
                    _tagSet.Add(tag);
                }
            }
        }

        #endregion
    }
}
