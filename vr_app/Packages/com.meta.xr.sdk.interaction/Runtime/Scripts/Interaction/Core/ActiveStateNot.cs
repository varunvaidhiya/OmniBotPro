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

using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Interaction.PoseDetection;
using Oculus.Interaction.PoseDetection.Debug;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Provides a logical NOT operation on an <see cref="IActiveState"/> to invert its active state. This class is useful in scenarios where the activation condition needs to be negated.
    /// </summary>
    /// <remarks>
    /// The <see cref="ActiveStateNot"/> class is a component that can be attached to any GameObject in a Unity scene. It requires a reference to another object that implements the <see cref="IActiveState"/> interface. The active state of this referenced object is then logically inverted, meaning if the referenced state is active, <see cref="Active"/> will return false, and vice versa.
    /// This inversion is useful in cases where the absence of a condition should trigger an interaction or event, without needing to write additional code to handle this logic explicitly.
    /// </remarks>
    public class ActiveStateNot : MonoBehaviour, IActiveState
    {

        [Tooltip("The IActiveState that the NOT operation will be applied to.")]
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;

        private IActiveState ActiveState;

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
        }

        protected virtual void Start()
        {
            this.AssertField(ActiveState, nameof(ActiveState));
        }

        /// <summary>
        /// Gets the inverted active state of the associated <see cref="IActiveState"/>.
        /// </summary>
        /// <value>
        /// True if the associated <see cref="IActiveState"/> is not active, false otherwise.
        /// </value>
        public bool Active => !ActiveState.Active;

        static ActiveStateNot()
        {
            ActiveStateDebugTree.RegisterModel<ActiveStateNot>(new DebugModel());
        }

        private class DebugModel : ActiveStateModel<ActiveStateNot>
        {
            protected override Task<IEnumerable<IActiveState>> GetChildrenAsync(ActiveStateNot activeState)
            {
                return Task.FromResult<IEnumerable<IActiveState>>(new[] { activeState.ActiveState });
            }
        }

        #region Inject
        /// <summary>
        /// Injects the <see cref="IActiveState"/> dependency required by this component.
        /// </summary>
        /// <param name="activeState">The active state whose value will be inverted.</param>
        public void InjectAllActiveStateNot(IActiveState activeState)
        {
            InjectActiveState(activeState);
        }
        /// <summary>
        /// Injects the <see cref="IActiveState"/> dependency. This method is designed to support dependency injection for unit testing.
        /// </summary>
        /// <param name="activeState">The active state to be inverted.</param>
        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as UnityEngine.Object;
            ActiveState = activeState;
        }
        #endregion
    }
}
