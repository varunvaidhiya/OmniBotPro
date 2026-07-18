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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Oculus.Interaction.PoseDetection.Debug
{
    /// <summary>
    /// Defines a model for managing hierarchical relationships between active states in the Interaction SDK.
    /// This interface is primarily used for debugging and visualization of active state hierarchies.
    /// </summary>
    /// <remarks>
    /// This interface is used in conjunction with the pose detection and debugging systems to:
    /// <list type="bullet">
    /// <item><description>Expose parent-child relationships between active states</description></item>
    /// <item><description>Enable debugging visualization of state hierarchies</description></item>
    /// <item><description>Support different types of active state implementations through the generic ActiveStateModel</description></item>
    /// </list>
    /// See <see cref="Oculus.Interaction.PoseDetection.Debug.ActiveStateModel{TActiveState}"/> for the base implementation.
    /// </remarks>
    public interface IActiveStateModel
    {
        /// <summary>
        /// Retrieves all child active states associated with the given parent active state.
        /// </summary>
        /// <param name="activeState">The parent <see cref="IActiveState"/> to get children for.</param>
        /// <returns>An enumerable collection of child active states. Returns empty if no children exist or if the activeState is not of the expected type.</returns>
        [Obsolete("Use async version of this method", true)]
        IEnumerable<IActiveState> GetChildren(IActiveState activeState) => throw new System.NotImplementedException();

        /// <summary>
        /// Retrieves all child active states associated with the given parent active state.
        /// </summary>
        /// <param name="activeState">The parent <see cref="IActiveState"/> to get children for.</param>
        /// <returns>An enumerable collection of child active states. Returns empty if no children exist or if the activeState is not of the expected type.</returns>
        Task<IEnumerable<IActiveState>> GetChildrenAsync(IActiveState activeState);
    }

    public abstract class ActiveStateModel<TActiveState> : IActiveStateModel
        where TActiveState : class, IActiveState
    {
        public async Task<IEnumerable<IActiveState>> GetChildrenAsync(IActiveState activeState)
        {
            if (activeState is TActiveState type)
            {
                var result = await GetChildrenAsync(type);
                return result;
            }
            return Enumerable.Empty<IActiveState>();
        }

        protected abstract Task<IEnumerable<IActiveState>> GetChildrenAsync(TActiveState instance);

        [Obsolete("Use async version of this method", true)]
        public IEnumerable<IActiveState> GetChildren(IActiveState activeState)
        {
            throw new System.NotImplementedException();
        }

        [Obsolete("Use async version of this method", true)]
        protected virtual IEnumerable<IActiveState> GetChildren(TActiveState activeState)
        {
            throw new System.NotImplementedException();
        }
    }
}
