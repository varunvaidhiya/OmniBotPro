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
    /// Interface indicating that the implementing type is a valid source of <see cref="HandSkeleton"/>s, addressable by
    /// <see cref="Handedness"/>.
    /// </summary>
    /// <remarks>
    /// This is almost exclusively by <see cref="DataSource{TData}"/>s, upon which downstream types depend rather than
    /// depending directly on IHandSkeletonProvider.
    /// </remarks>
    public interface IHandSkeletonProvider
    {
        /// <summary>
        /// Retrieves the current <see cref="HandSkeleton"/> for a single hand, addressed by <see cref="Handedness"/>.
        /// </summary>
        /// <param name="handedness">Which hand's skeleton to retrieve</param>
        /// <returns>The requested <see cref="HandSkeleton"/></returns>
        HandSkeleton this[Handedness handedness] { get; }
    }
}
