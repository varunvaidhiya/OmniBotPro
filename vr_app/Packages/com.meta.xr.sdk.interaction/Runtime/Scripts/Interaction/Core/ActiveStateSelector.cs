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

namespace Oculus.Interaction
{
    /// <summary>
    /// Selects and unselects based on the provided <see cref="ActiveState"/>. If this component is piped into
    /// the <see cref="Interactor{TInteractor, TInteractable}.Selector"/> property of an interactor, it can
    /// replace the selection mechanism (trigger pulls, pinch or grab poses, etc.) with any other mechanism which
    /// can be represented as an <see cref="IActiveState"/>.
    /// </summary>
    public class ActiveStateSelector : MonoBehaviour, ISelector
    {
        /// <summary>
        /// ISelector events will be raised based on state changes of this IActiveState.
        /// </summary>
        [Tooltip("ISelector events will be raised " +
            "based on state changes of this IActiveState.")]
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;
        protected IActiveState ActiveState { get; private set; }

        private bool _selecting = false;

        /// <summary>
        /// Implementation of <see cref="ISelector.WhenSelected"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action WhenSelected = delegate { };

        /// <summary>
        /// Implementation of <see cref="ISelector.WhenUnselected"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action WhenUnselected = delegate { };

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
        }

        protected virtual void Start()
        {
            this.AssertField(ActiveState, nameof(ActiveState));
        }

        protected virtual void Update()
        {
            if (_selecting != ActiveState.Active)
            {
                _selecting = ActiveState.Active;
                if (_selecting)
                {
                    WhenSelected();
                }
                else
                {
                    WhenUnselected();
                }
            }
        }

        #region Inject

        /// <summary>
        /// Wrapper for <see cref="InjectActiveState(IActiveState)"/> for injecting the required dependencies
        /// to a dynamically-allocated ActiveStateSelector instance. This method exists to support Interaction
        /// SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllActiveStateSelector(IActiveState activeState)
        {
            InjectActiveState(activeState);
        }

        /// <summary>
        /// Sets the underlying <see cref="IActiveState"/> for a dynamically-allocated ActiveStateSelector instance.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical
        /// Unity Editor-based usage.
        /// </summary>
        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as UnityEngine.Object;
            ActiveState = activeState;
        }
        #endregion
    }
}
