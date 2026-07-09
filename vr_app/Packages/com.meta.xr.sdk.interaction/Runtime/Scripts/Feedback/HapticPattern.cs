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
using System;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Feedback
{
    /// <summary>
    /// Defines the parameters for a haptic feedback pattern.
    /// </summary>
    [Serializable]
    public struct HapticPattern
    {
        /// <summary>
        /// The strength of the haptic vibration, typically from 0 (none) to 1 (full).
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("Vibration strength (0-1).")]
        public float _amplitude;

        /// <summary>
        /// The duration of the haptic vibration in seconds.
        /// </summary>
        [Range(0f, 2f)]
        [Tooltip("Duration in seconds.")]
        public float _duration;

        /// <summary>
        /// The frequency of the haptic vibration. The interpretation of this value
        /// can vary by haptics SDK (e.g., specific Hz, or a normalized 0-1 value).
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("Vibration frequency (interpretation depends on the IHapticsPlayer implementation).")]
        public float _frequency;

        /// <summary>
        /// Initializes a new instance of the <see cref="HapticPattern"/> struct.
        /// </summary>
        /// <param name="amplitude">Vibration strength (0-1).</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="frequency">Vibration frequency (0-1, interpretation varies).</param>
        public HapticPattern(float amplitude, float duration, float frequency = 0.5f)
        {
            _amplitude = Mathf.Clamp01(amplitude);
            _duration = Mathf.Max(0f, duration);
            _frequency = Mathf.Clamp01(frequency);
        }
    }
}
