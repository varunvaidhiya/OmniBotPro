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
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// The <see cref="ActiveStateUnityEventWrapper"/> class provides a way to link Unity events with the state of an <see cref="IActiveState"/>.
    /// This class is designed to trigger Unity events based on changes in the active state, allowing developers to easily connect Interaction SDK logic with Unity's built-in event system.
    /// </summary>
    /// <remarks>
    /// This class is particularly useful for triggering visual or audio effects, or other Unity-based events in response to changes in an interaction's active state.
    /// </remarks>
    public class ActiveStateUnityEventWrapper : MonoBehaviour
    {
        /// <summary>
        /// The active state that this class monitors
        /// </summary>
        [Tooltip("Events will fire based on the state of this IActiveState.")]
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;
        private IActiveState ActiveState;

        [Tooltip("This event will be fired when the provided IActiveState becomes active.")]
        [SerializeField]
        private UnityEvent _whenActivated;

        [Tooltip("This event will be fired when the provided IActiveState becomes inactive.")]
        [SerializeField]
        private UnityEvent _whenDeactivated;

        /// <summary>
        /// The Unity event that is triggered during an <see cref="Update"/> when the provided <see cref="IActiveState"/> becomes active.
        /// </summary>
        public UnityEvent WhenActivated => _whenActivated;
        /// <summary>
        /// The Unity event that is triggered during an <see cref="Update"/> when the provided <see cref="IActiveState"/> becomes inactive.
        /// </summary>
        public UnityEvent WhenDeactivated => _whenDeactivated;

        [SerializeField]
        [Tooltip("If true, the corresponding event will be fired at the beginning of Update.")]
        private bool _emitOnFirstUpdate = true;

        private bool _emittedOnFirstUpdate = false;

        private bool _savedState;

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
        }

        protected virtual void Start()
        {
            this.AssertField(ActiveState, nameof(ActiveState));
            _savedState = false;
        }
        /// <summary>
        /// This update method monitors the active state and triggers the appropriate Unity event based on the current state.
        /// </summary>
        protected virtual void Update()
        {
            if (_emitOnFirstUpdate && !_emittedOnFirstUpdate)
            {
                InvokeEvent();
                _emittedOnFirstUpdate = true;
            }

            bool active = ActiveState.Active;
            if (_savedState != active)
            {
                _savedState = active;
                InvokeEvent();
            }
        }

        private void InvokeEvent()
        {
            if (_savedState)
            {
                _whenActivated.Invoke();
            }
            else
            {
                _whenDeactivated.Invoke();
            }
        }

        #region Inject
        /// <summary>
        /// Injects an active state that this <see cref="ActiveStateUnityEventWrapper"/> will monitor. This method is used to
        /// set up the wrapper with the active state whose changes will trigger the associated Unity events.
        /// </summary>
        /// <param name="activeState">The <see cref="IActiveState"/> instance that will be monitored.</param>
        public void InjectAllActiveStateUnityEventWrapper(IActiveState activeState)
        {
            InjectActiveState(activeState);
        }
        /// <summary>
        ///  Sets the active state for the wrapper. This method is designed to support dependency injection for unit testing.
        /// </summary>
        /// <param name="activeState">The active state to set</param>
        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as UnityEngine.Object;
            ActiveState = activeState;
        }
        /// <summary>
        /// Optionally sets whether to emit events on the first update.
        /// </summary>
        /// <param name="emitOnFirstUpdate">If true, events will be emitted on the first update.</param>
        public void InjectOptionalEmitOnFirstUpdate(bool emitOnFirstUpdate)
        {
            _emitOnFirstUpdate = emitOnFirstUpdate;
        }
        /// <summary>
        /// Injects a Unity event that is triggered when the active state is activated.
        /// </summary>
        /// <param name="whenActivated">The Unity event to trigger on activation.</param>
        public void InjectOptionalWhenActivated(UnityEvent whenActivated)
        {
            _whenActivated = whenActivated;
        }
        /// <summary>
        ///  Injects a Unity event that is triggered when the active state is deactivated.
        /// </summary>
        /// <param name="whenDeactivated">The Unity event to trigger on deactivation.</param>
        public void InjectOptionalWhenDeactivated(UnityEvent whenDeactivated)
        {
            _whenDeactivated = whenDeactivated;
        }

        #endregion
    }
}
