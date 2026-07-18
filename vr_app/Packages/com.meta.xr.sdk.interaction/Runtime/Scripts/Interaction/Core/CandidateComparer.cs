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

namespace Oculus.Interaction
{
    /// <summary>
    /// Type-specific descendent of <see cref="ICandidateComparer"/>, allowing typed-aware comparisons.
    /// </summary>
    /// <typeparam name="T">The type to be compared</typeparam>
    /// <remarks>
    /// Comparisons between objects of <typeparamref name="T"/> will result in the arguments being compared by
    /// <see cref="Compare(T, T)"/>. Comparisons of any type other than <typeparamref name="T"/> will evaluate
    /// the arguments as being equivalent.
    /// </remarks>
    public abstract class CandidateComparer<T> : MonoBehaviour, ICandidateComparer where T : class
    {
        /// <summary>
        /// Implementation of <see cref="ICandidateComparer.Compare(object, object)"/>; for details, please refer to
        /// the related documentation provided for that property.
        /// </summary>
        public int Compare(object a, object b)
        {
            T typedA = a as T;
            T typedB = b as T;

            if (typedA != null && typedB != null)
            {
                return Compare(typedA, typedB);
            }

            return 0;
        }

        /// <summary>
        /// Adapter API which <see cref="Compare(object, object)"/> uses to perform typed comparisons between its arguments.
        /// </summary>
        /// <param name="a">Left-hand side of the comparison</param>
        /// <param name="b">Right-hand side of the comparison</param>
        /// <returns>The result of the comparison</returns>
        public abstract int Compare(T a, T b);
    }
}
