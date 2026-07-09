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

using System;
using UnityEngine;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// The concrete implementation of <see cref="IHmd"/> and the Interaction SDK's default concrete representation of an
    /// head-mounted display.
    /// </summary>
    /// <remarks>
    /// This type is also a <see cref="DataModifier{TData}"/> and is thus technically permitted to alter system-provided HMD data.
    /// However, altering this data can dramatically impact user comfort, so this implementation does not perform any; likewise,
    /// descendent types are strongly encouraged to refrain from modifying the system-provided HMD data.
    /// </remarks>
    public class Hmd : DataModifier<HmdDataAsset>, IHmd
    {
        /// <summary>
        /// Implementation of <see cref="IHmd.WhenUpdated"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action WhenUpdated = delegate { };

        protected override void Apply(HmdDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        /// <summary>
        /// Implementation of <see cref="DataSource{TData}.MarkInputDataRequiresUpdate"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                WhenUpdated();
            }
        }

        /// <summary>
        /// Implementation of <see cref="IHmd.TryGetRootPose(out Pose)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public bool TryGetRootPose(out Pose pose)
        {
            var currentData = GetData();

            if (!currentData.IsTracked)
            {
                pose = Pose.identity;
                return false;
            }
            ITrackingToWorldTransformer transformer = GetData().Config.TrackingToWorldTransformer;
            pose = transformer.ToWorldPose(currentData.Root);
            return true;
        }

        #region Inject

        #endregion
    }
}
