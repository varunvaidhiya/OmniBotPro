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

using Oculus.Interaction.Collections;

namespace Oculus.Interaction.Body.Input
{
    /// <summary>
    /// Represents the different joint sets and parent/child relationships found in different skeletons.
    /// </summary>
    /// <remarks>
    /// An ISkeletonMapping encapsulates and characterizes the relationships among the of a skeleton; in essense,
    /// it describes the <see cref="BodyJointId"/> hierarchy within the skeleton.
    /// </remarks>
    public interface ISkeletonMapping
    {
        /// <summary>
        /// The set of <see cref="BodyJointId"/>s supported by this skeleton.
        /// </summary>
        IEnumerableHashSet<BodyJointId> Joints { get; }

        /// <summary>
        /// Get the parent joint for a given body joint.
        /// </summary>
        /// <param name="jointId">The <see cref="BodyJointId"/> to fetch the parent for</param>
        /// <param name="parent">The parent joint of the requested <paramref name="jointId"/></param>
        /// <returns>True if parent could be retrieved (i.e., if <paramref name="jointId"/> has a parent joint), false otherwise</returns>
        bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent);
    }
}
