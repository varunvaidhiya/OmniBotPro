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

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines a comparison mechanism for prioritizing interactors based on their candidate properties within an InteractorGroup.
    /// This interface enables custom sorting logic for determining which interactor should take precedence when multiple
    /// interactors are competing for interaction with the same target.
    /// </summary>
    /// <remarks>
    /// This interface is crucial for:
    /// <list type="bullet">
    /// <item><description>Implementing distance-based priority systems for interactors</description></item>
    /// <item><description>Creating custom sorting logic for interaction candidates</description></item>
    /// <item><description>Managing complex multi-interactor scenarios</description></item>
    /// </list>
    /// For implementation examples, see <see cref="Oculus.Interaction.CandidatePositionComparer"/>.
    /// </remarks>
    public interface ICandidateComparer
    {
        /// <summary>
        /// Compares two candidate objects to determine their relative priority.
        /// </summary>
        /// <param name="a">The first candidate object to compare</param>
        /// <param name="b">The second candidate object to compare</param>
        /// <returns>
        /// A negative value if a should be prioritized over b,
        /// zero if they have equal priority,
        /// or a positive value if b should be prioritized over a
        /// </returns>
        /// <remarks>
        /// The implementation should be consistent with the standard comparison contract:
        /// <list type="bullet">
        /// <item><description>If Compare(x,y) returns negative, then Compare(y,x) should return positive</description></item>
        /// <item><description>If Compare(x,y) returns zero, then Compare(y,x) should return zero</description></item>
        /// <item><description>If Compare(x,y) returns positive, then Compare(y,x) should return negative</description></item>
        /// </list>
        /// </remarks>
        int Compare(object a, object b);
    }
}
