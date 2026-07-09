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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Core interface for [One Euro filter](https://dl.acm.org/doi/10.1145/2207676.2208639) implementations. An academic relative of the
    /// [$-family of gesture recognizers](https://depts.washington.edu/acelab/proj/dollar/impact.html), the
    /// One Euro filter is designed to make effective and efficient
    /// noise reduction in signal processing accessible to non-domain experts. Thus, this filter focuses on balancing
    /// result quality (bettering more naive approaches) with developer ease-of-use (contrasted with more
    /// sophisticated techniques such as Kalman filters).
    /// </summary>
    /// <typeparam name="TData">The type of data to be filtered, such as a `float` or a `Vector3`</typeparam>
    /// <remarks>The Interaction SDK's canonical implementation of this interface is <see cref="OneEuroFilter"/>.</remarks>
    public interface IOneEuroFilter<TData>
    {
        /// <summary>
        /// The last value returned by <see cref="Step(TData, float)"/>, which is the up-to-date known filtered value
        /// of the signal being processed by this filter.
        /// </summary>
        TData Value { get; }

        /// <summary>
        /// Update the parameters of the filter
        /// </summary>
        /// <param name="propertyBlock">The property block containing the parameters to se</param>
        void SetProperties(in OneEuroFilterPropertyBlock properties);

        /// <summary>
        /// Update the filter with a new noisy value to be smoothed.
        /// This is a destructive operation that should be run once per frame, as
        /// calling this updates the previous frame data.
        /// </summary>
        /// <param name="rawValue">The noisy value to be filtered</param>
        /// <param name="deltaTime">The time between steps, use to derive filter frequency.
        /// Omitting this value will fallback to <see cref="OneEuroFilter._DEFAULT_FREQUENCY_HZ"/></param>
        /// <returns>The filtered value, equivalent to <see cref="Value"/></returns>
        TData Step(TData rawValue, float deltaTime = 1f / OneEuroFilter._DEFAULT_FREQUENCY_HZ);

        /// <summary>
        /// Clear previous values and reset the filter
        /// </summary>
        void Reset();
    }
}
