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

using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Interaction
{
    /// <summary>
    /// Per-ScrollRect settings for microgesture scrolling. Attach this component
    /// to a <see cref="ScrollRect"/> to override <see cref="MicrogestureScrollProvider"/>
    /// defaults or to opt out of microgesture scrolling entirely.
    /// </summary>
    /// <remarks>
    /// When <see cref="MicrogestureScrollProvider"/> resolves a ScrollRect from a
    /// UI hover event, it checks for this component. If present, the settings here
    /// take precedence over the provider's global defaults.
    ///
    /// To disable microgesture scrolling on a specific ScrollRect, add this component
    /// and uncheck <see cref="EnableMicrogestureScroll"/>.
    /// </remarks>
    [RequireComponent(typeof(ScrollRect))]
    public class MicrogestureScrollSettings : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("If false, microgesture scrolling is disabled on this ScrollRect.")]
        private bool _enableMicrogestureScroll = true;

        [SerializeField, Range(-1f, 5f)]
        [Tooltip("Override scroll strength for this ScrollRect. -1 uses the global default from MicrogestureScrollProvider.")]
        private float _scrollStrengthOverride = -1f;

        [SerializeField]
        [Tooltip("Override vertical scroll enable. Leave unchecked to use global default.")]
        private bool _overrideVerticalScroll = false;

        [SerializeField]
        [Tooltip("Enable vertical scrolling (only applies when Override Vertical Scroll is checked).")]
        private bool _enableVerticalScroll = true;

        [SerializeField]
        [Tooltip("Override horizontal scroll enable. Leave unchecked to use global default.")]
        private bool _overrideHorizontalScroll = false;

        [SerializeField]
        [Tooltip("Enable horizontal scrolling (only applies when Override Horizontal Scroll is checked).")]
        private bool _enableHorizontalScroll = true;

        /// <summary>
        /// Whether microgesture scrolling is enabled on this ScrollRect.
        /// </summary>
        public bool EnableMicrogestureScroll
        {
            get => _enableMicrogestureScroll;
            set => _enableMicrogestureScroll = value;
        }

        /// <summary>
        /// Override scroll strength for this ScrollRect.
        /// Returns -1 if no override is set (use global default).
        /// </summary>
        public float ScrollStrengthOverride
        {
            get => _scrollStrengthOverride;
            set => _scrollStrengthOverride = value;
        }

        /// <summary>
        /// Whether this component has a valid scroll strength override.
        /// </summary>
        public bool HasStrengthOverride => _scrollStrengthOverride >= 0;

        /// <summary>
        /// Whether the vertical scroll setting is overridden on this ScrollRect.
        /// </summary>
        public bool OverrideVerticalScroll
        {
            get => _overrideVerticalScroll;
            set => _overrideVerticalScroll = value;
        }

        /// <summary>
        /// Whether vertical scrolling is enabled (only applies when <see cref="OverrideVerticalScroll"/> is true).
        /// </summary>
        public bool EnableVerticalScroll
        {
            get => _enableVerticalScroll;
            set => _enableVerticalScroll = value;
        }

        /// <summary>
        /// Whether the horizontal scroll setting is overridden on this ScrollRect.
        /// </summary>
        public bool OverrideHorizontalScroll
        {
            get => _overrideHorizontalScroll;
            set => _overrideHorizontalScroll = value;
        }

        /// <summary>
        /// Whether horizontal scrolling is enabled (only applies when <see cref="OverrideHorizontalScroll"/> is true).
        /// </summary>
        public bool EnableHorizontalScroll
        {
            get => _enableHorizontalScroll;
            set => _enableHorizontalScroll = value;
        }
    }
}

