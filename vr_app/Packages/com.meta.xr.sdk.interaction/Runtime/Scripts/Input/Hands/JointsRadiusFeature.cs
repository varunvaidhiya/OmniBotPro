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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Enables the retrieval of the joint radius for
    /// joints within an <see cref="IHand"/>.
    /// </summary>
    public class JointsRadiusFeature : MonoBehaviour
    {
        [SerializeField]
        private Hand _hand;

        /// <summary>
        /// Gets the radius of the provided <see cref="HandJointId"/>
        /// </summary>
        /// <param name="id">The joint to retrieve the radius for.</param>
        /// <returns>The joint radius in world units.</returns>
        public float GetJointRadius(HandJointId id)
        {
#if ISDK_OPENXR_HAND
            return _hand.GetData().JointRadii[(int)id];
#else
            return _hand.GetData().Config.HandSkeleton.Joints[(int)id].radius;
#endif
        }

        #region Inject

        #endregion
    }
}
