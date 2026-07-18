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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// The Interaction SDK's canonical implementation of <see cref="IOneEuroFilter{TData}"/>,
    /// based on the paper https://hal.inria.fr/hal-00670496/document. An academic relative of the
    /// [$-family of gesture recognizers](https://depts.washington.edu/acelab/proj/dollar/impact.html), the
    /// [One Euro filter](https://dl.acm.org/doi/10.1145/2207676.2208639) is designed to make effective and efficient
    /// noise reduction in signal processing accessible to non-domain experts. Thus, this filter focuses on balancing
    /// result quality (bettering more naive approaches) with developer ease-of-use (contrasted with more
    /// sophisticated techniques such as Kalman filters).
    /// </summary>
    public partial class OneEuroFilter : IOneEuroFilter<float>
    {
        /// <summary>
        /// Default sampling rate expected by the OneEuroFilter. This is used to determine the default timestep
        /// (second argument) for <see cref="Step(float, float)"/>.
        /// </summary>
        public const float _DEFAULT_FREQUENCY_HZ = 60f;

        /// <summary>
        /// Implementation of <see cref="IOneEuroFilter{TData}.Value"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float Value { get; private set; }

        private OneEuroFilterPropertyBlock _properties;

        private bool _isFirstUpdate;
        private LowPassFilter _xfilt;
        private LowPassFilter _dxfilt;

        private OneEuroFilter()
        {
            _xfilt = new LowPassFilter();
            _dxfilt = new LowPassFilter();
            _isFirstUpdate = true;

            SetProperties(OneEuroFilterPropertyBlock.Default);
        }

        /// <summary>
        /// Implementation of <see cref="IOneEuroFilter{TData}.SetProperties(in OneEuroFilterPropertyBlock)"/>; for details, please refer
        /// to the related documentation provided for that interface.
        /// </summary>
        public void SetProperties(in OneEuroFilterPropertyBlock properties)
        {
            Assert.IsTrue(properties.MinCutoff >= 0, $"{nameof(properties.MinCutoff)} must be >= 0");
            Assert.IsTrue(properties.DCutoff >= 0, $"{nameof(properties.DCutoff)} must be >= 0");
            Assert.IsTrue(properties.Beta >= 0, $"{nameof(properties.Beta)} must be >= 0");

            _properties = properties;
        }

        /// <summary>
        /// Implementation of <see cref="IOneEuroFilter{TData}.Step(TData, float)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public float Step(float newValue, float deltaTime)
        {
            if (deltaTime > 0)
            {
                float freqHz = 1f / deltaTime;
                float dx = _isFirstUpdate ? 0 : (newValue - _xfilt.PrevValue) * freqHz;
                _isFirstUpdate = false;
                float edx = _dxfilt.Filter(dx, GetAlpha(freqHz, _properties.DCutoff));
                float cutoff = _properties.MinCutoff + _properties.Beta * Mathf.Abs(edx);
                Value = _xfilt.Filter(newValue, GetAlpha(freqHz, cutoff));
            }

            return Value;
        }

        /// <summary>
        /// Implementation of <see cref="IOneEuroFilter{TData}.Reset"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public void Reset()
        {
            Value = default;
            _xfilt.Reset();
            _dxfilt.Reset();
            _isFirstUpdate = true;
        }

        private float GetAlpha(float rate, float cutoff)
        {
            float tau = 1f / (2.0f * Mathf.PI * cutoff);
            float te = 1f / rate;
            return 1f / (1f + tau / te);
        }

        private class LowPassFilter
        {
            public float PrevValue => _hatxprev;

            private bool _isFirstUpdate;
            private float _hatx;
            private float _hatxprev;

            public LowPassFilter()
            {
                _isFirstUpdate = true;
            }

            public void Reset()
            {
                _isFirstUpdate = true;
                _hatx = _hatxprev = default;
            }

            public float Filter(float x, float alpha)
            {
                if (_isFirstUpdate)
                {
                    _isFirstUpdate = false;
                    _hatxprev = x;
                }
                _hatx = alpha * x + (1 - alpha) * _hatxprev;
                _hatxprev = _hatx;
                return _hatx;
            }
        }
    }
}
