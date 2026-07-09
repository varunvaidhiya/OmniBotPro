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
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Oculus.Interaction
{
    /// <summary>
    /// Exposes Unity events that broadcast state changes from an <see cref="IInteractableView"/> (an interactable).
    /// </summary>
    /// <remarks>
    /// This is one of the most convenient ways to manage interaction consequences in the Unity Editor. Through the
    /// events exposed to the Editor by InteractableUnityEventWrapper, you can directly connect core interactable state
    /// changes (<see cref="WhenHover"/>, <see cref="WhenUnhover"/>, etc.) to invocations elsewhere in the scene, such
    /// as enabling or disabling a GameObject when a button is pressed, for example.
    /// </remarks>
    public class InteractableUnityEventWrapper : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="IInteractableView"/> (Interactable) component to wrap.
        /// </summary>
        [Tooltip("The IInteractableView (Interactable) component to wrap.")]
        [SerializeField, Interface(typeof(IInteractableView))]
        private UnityEngine.Object _interactableView;
        private IInteractableView InteractableView;

        /// <summary>
        /// Raised when an Interactor hovers over the Interactable.
        /// </summary>
        [Tooltip("Raised when an Interactor hovers over the Interactable.")]
        [SerializeField]
        private UnityEvent _whenHover;

        /// <summary>
        /// Raised when the Interactable was being hovered but now it isn't.
        /// </summary>
        [Tooltip("Raised when the Interactable was being hovered but now it isn't.")]
        [SerializeField]
        private UnityEvent _whenUnhover;

        /// <summary>
        /// Raised when an Interactor selects the Interactable.
        /// </summary>
        [Tooltip("Raised when an Interactor selects the Interactable.")]
        [SerializeField]
        private UnityEvent _whenSelect;

        /// <summary>
        /// Raised when the Interactable was being selected but now it isn't.
        /// </summary>
        [Tooltip("Raised when the Interactable was being selected but now it isn't.")]
        [SerializeField]
        private UnityEvent _whenUnselect;

        /// <summary>
        /// Raised each time an Interactor hovers over the Interactable, even if the Interactable is already being hovered by a different Interactor.
        /// </summary>
        [Tooltip("Raised each time an Interactor hovers over the Interactable, even if the Interactable is already being hovered by a different Interactor.")]
        [SerializeField]
        private UnityEvent _whenInteractorViewAdded;

        /// <summary>
        /// Raised each time an Interactor stops hovering over the Interactable, even if the Interactable is still being hovered by a different Interactor.
        /// </summary>
        [Tooltip("Raised each time an Interactor stops hovering over the Interactable, even if the Interactable is still being hovered by a different Interactor.")]
        [SerializeField]
        private UnityEvent _whenInteractorViewRemoved;

        /// <summary>
        /// Raised each time an Interactor selects the Interactable, even if the Interactable is already being selected by a different Interactor.
        /// </summary>
        [Tooltip("Raised each time an Interactor selects the Interactable, even if the Interactable is already being selected by a different Interactor.")]
        [SerializeField]
        private UnityEvent _whenSelectingInteractorViewAdded;

        /// <summary>
        /// Raised each time an Interactor stops selecting the Interactable, even if the Interactable is still being selected by a different Interactor.
        /// </summary>
        [Tooltip("Raised each time an Interactor stops selecting the Interactable, even if the Interactable is still being selected by a different Interactor.")]
        [SerializeField]
        private UnityEvent _whenSelectingInteractorViewRemoved;

        #region Properties

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.State"/> goes from
        /// <see cref="InteractableState.Normal"/> to <see cref="InteractableState.Hover"/>.
        /// </summary>
        /// <remarks>
        /// This is a decomposition of <see cref="IInteractableView.WhenStateChanged"/> intended to allow
        /// individual state changes to be specifically and conveniently leveraged through the Editor.
        /// </remarks>
        public UnityEvent WhenHover => _whenHover;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.State"/> goes from
        /// <see cref="InteractableState.Hover"/> to <see cref="InteractableState.Normal"/>.
        /// </summary>
        /// <remarks>
        /// This is a decomposition of <see cref="IInteractableView.WhenStateChanged"/> intended to allow
        /// individual state changes to be specifically and conveniently leveraged through the Editor.
        /// </remarks>
        public UnityEvent WhenUnhover => _whenUnhover;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.State"/> goes from
        /// <see cref="InteractableState.Hover"/> to <see cref="InteractableState.Select"/>.
        /// </summary>
        /// <remarks>
        /// This is a decomposition of <see cref="IInteractableView.WhenStateChanged"/> intended to allow
        /// individual state changes to be specifically and conveniently leveraged through the Editor.
        /// </remarks>
        public UnityEvent WhenSelect => _whenSelect;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.State"/> goes from
        /// <see cref="InteractableState.Select"/> to <see cref="InteractableState.Hover"/>.
        /// </summary>
        /// <remarks>
        /// This is a decomposition of <see cref="IInteractableView.WhenStateChanged"/> intended to allow
        /// individual state changes to be specifically and conveniently leveraged through the Editor.
        /// </remarks>
        public UnityEvent WhenUnselect => _whenUnselect;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.WhenInteractorViewAdded"/>
        /// event is invoked, which occurs whenever a new <see cref="IInteractorView"/> begins interacting with
        /// the underlying interactable.
        /// </summary>
        public UnityEvent WhenInteractorViewAdded => _whenInteractorViewAdded;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.WhenInteractorViewRemoved"/>
        /// event is invoked, which occurs whenever an <see cref="IInteractorView"/> ceases interacting with
        /// the underlying interactable.
        /// </summary>
        public UnityEvent WhenInteractorViewRemoved => _whenInteractorViewRemoved;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.WhenSelectingInteractorViewAdded"/>
        /// event is invoked, which occurs whenever a new <see cref="IInteractorView"/> begins selecting the underlying
        /// interactable.
        /// </summary>
        public UnityEvent WhenSelectingInteractorViewAdded => _whenSelectingInteractorViewAdded;

        /// <summary>
        /// Unity event invoked whenever the underlying <see cref="IInteractableView.WhenSelectingInteractorViewRemoved"/>
        /// event is invoked, which occurs whenever a new <see cref="IInteractorView"/> ceases selecting the underlying
        /// interactable.
        /// </summary>
        public UnityEvent WhenSelectingInteractorViewRemoved => _whenSelectingInteractorViewRemoved;

        #endregion

        protected bool _started = false;

        protected virtual void Awake()
        {
            if (InteractableView == null)
            {
                InteractableView = _interactableView as IInteractableView;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(InteractableView, nameof(InteractableView));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                InteractableView.WhenStateChanged += HandleStateChanged;
                InteractableView.WhenInteractorViewAdded += HandleInteractorViewAdded;
                InteractableView.WhenInteractorViewRemoved += HandleInteractorViewRemoved;
                InteractableView.WhenSelectingInteractorViewAdded += HandleSelectingInteractorViewAdded;
                InteractableView.WhenSelectingInteractorViewRemoved += HandleSelectingInteractorViewRemoved;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractableView.WhenStateChanged -= HandleStateChanged;
                InteractableView.WhenInteractorViewAdded -= HandleInteractorViewAdded;
                InteractableView.WhenInteractorViewRemoved -= HandleInteractorViewRemoved;
                InteractableView.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorViewAdded;
                InteractableView.WhenSelectingInteractorViewRemoved -= HandleSelectingInteractorViewRemoved;
            }
        }

        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            switch (args.NewState)
            {
                case InteractableState.Normal:
                    if (args.PreviousState == InteractableState.Hover)
                    {
                        _whenUnhover.Invoke();
                    }

                    break;
                case InteractableState.Hover:
                    if (args.PreviousState == InteractableState.Normal)
                    {
                        _whenHover.Invoke();
                    }
                    else if (args.PreviousState == InteractableState.Select)
                    {
                        _whenUnselect.Invoke();
                    }

                    break;
                case InteractableState.Select:
                    if (args.PreviousState == InteractableState.Hover)
                    {
                        _whenSelect.Invoke();
                    }

                    break;
            }
        }

        private void HandleInteractorViewAdded(IInteractorView interactorView)
        {
            WhenInteractorViewAdded.Invoke();
        }

        private void HandleInteractorViewRemoved(IInteractorView interactorView)
        {
            WhenInteractorViewRemoved.Invoke();
        }

        private void HandleSelectingInteractorViewAdded(IInteractorView interactorView)
        {
            WhenSelectingInteractorViewAdded.Invoke();
        }

        private void HandleSelectingInteractorViewRemoved(IInteractorView interactorView)
        {
            WhenSelectingInteractorViewRemoved.Invoke();
        }

        #region Inject

        /// <summary>
        /// Sets all required dependencies for a dynamically instantiated InteractableUnityEventWrapper. This is a
        /// convenience method wrapping <see cref="InjectInteractableView(IInteractableView)"/>. This method exists
        /// to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectAllInteractableUnityEventWrapper(IInteractableView interactableView)
        {
            InjectInteractableView(interactableView);
        }

        /// <summary>
        /// Sets the an <see cref="IInteractableView"/> as the underlying interactable for a dynamically instantiated
        /// InteractableUnityEventWrapper. This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectInteractableView(IInteractableView interactableView)
        {
            _interactableView = interactableView as UnityEngine.Object;
            InteractableView = interactableView;
        }

        #endregion
    }
}
