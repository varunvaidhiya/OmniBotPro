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
    /// Presents an interactor's state as an <see cref="IActiveState"/>,
    /// which will become active when selected propert(ies) of an
    /// <see cref="IInteractor"/> are recognized.
    /// </summary>
    public class InteractorActiveState : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// The property of the <see cref="IInteractor"/> to
        /// track with the <see cref="InteractorActiveState"/>
        /// </summary>
        [System.Flags]
        public enum InteractorProperty
        {
            /// <summary>
            /// Does the interactor have a candidate
            /// </summary>
            HasCandidate = 1 << 0,

            /// <summary>
            /// Does the interactor have an interactable
            /// </summary>
            HasInteractable = 1 << 1,

            /// <summary>
            /// Is the interactor selecting
            /// </summary>
            IsSelecting = 1 << 2,

            /// <summary>
            /// Does the interactor have a selected interactable
            /// </summary>
            HasSelectedInteractable = 1 << 3,

            /// <summary>
            /// Is the interactor in the <see cref="InteractorState.Normal"/> state
            /// </summary>
            IsNormal = 1 << 4,

            /// <summary>
            /// Is the interactor in the <see cref="InteractorState.Hover"/> state
            /// </summary>
            IsHovering = 1 << 5,

            /// <summary>
            /// Is the interactor in the <see cref="InteractorState.Select"/> state
            /// </summary>
            IsDisabled = 1 << 6,
        }

        [SerializeField, Interface(typeof(IInteractor))]
        private UnityEngine.Object _interactor;
        private IInteractor Interactor;

        [SerializeField]
        private InteractorProperty _property;

        /// <summary>
        /// The <see cref="InteractorProperty"/> that will be tested against.
        /// </summary>
        public InteractorProperty Property
        {
            get
            {
                return _property;
            }
            set
            {
                _property = value;
            }
        }

        /// <summary>
        /// When any of these conditions in <see cref="Property"/> are met,
        /// this <see cref="IActiveState"/> will become Active.
        /// </summary>
        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }

                if ((_property & InteractorProperty.HasCandidate) != 0
                    && Interactor.HasCandidate)
                {
                    return true;
                }
                if ((_property & InteractorProperty.HasInteractable) != 0
                    && Interactor.HasInteractable)
                {
                    return true;
                }
                if ((_property & InteractorProperty.IsSelecting) != 0
                    && Interactor.State == InteractorState.Select)
                {
                    return true;
                }
                if ((_property & InteractorProperty.HasSelectedInteractable) != 0
                    && Interactor.HasSelectedInteractable)
                {
                    return true;
                }
                if ((_property & InteractorProperty.IsNormal) != 0
                    && Interactor.State == InteractorState.Normal)
                {
                    return true;
                }
                if ((_property & InteractorProperty.IsHovering) != 0
                    && Interactor.State == InteractorState.Hover)
                {
                    return true;
                }
                if ((_property & InteractorProperty.IsDisabled) != 0
                    && Interactor.State == InteractorState.Disabled)
                {
                    return true;
                }
                return false;
            }
        }

        protected virtual void Awake()
        {
            Interactor = _interactor as IInteractor;
        }

        protected virtual void Start()
        {
            this.AssertField(Interactor, nameof(Interactor));
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="InteractorActiveState"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllInteractorActiveState(IInteractor interactor)
        {
            InjectInteractor(interactor);
        }

        /// <summary>
        /// Sets the underlying <see cref="IInteractor"/> for a dynamically instantiated
        /// <see cref="InteractorActiveState"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectInteractor(IInteractor interactor)
        {
            _interactor = interactor as UnityEngine.Object;
            Interactor = interactor;
        }
        #endregion
    }
}
