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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This class implements higher level logic to forward the highest <see cref="IInteractableView"/>
    /// state of any of the <see cref="IInteractableView"/> in its list
    /// </summary>
    public class InteractableGroupView : MonoBehaviour, IInteractableView
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private List<UnityEngine.Object> _interactables;

        private List<IInteractableView> Interactables;

        [SerializeField, Optional]
        private UnityEngine.Object _data = null;

        /// <summary>
        /// Implementation of <see cref="IInteractableView.Data"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public object Data { get; protected set; } = null;

        /// <summary>
        /// The sum of <see cref="IInteractableView.InteractorViews"/>
        /// counts for each interactable in <see cref="Interactables"/>.
        /// </summary>
        public int InteractorsCount
        {
            get
            {
                int count = 0;
                foreach (IInteractableView interactable in Interactables)
                {
                    count += interactable.InteractorViews.Count();
                }

                return count;
            }
        }

        /// <summary>
        /// The sum of <see cref="IInteractableView.SelectingInteractorViews"/>
        /// counts for each interactable in <see cref="Interactables"/>.
        /// </summary>
        public int SelectingInteractorsCount
        {
            get
            {
                int count = 0;
                foreach (IInteractableView interactable in Interactables)
                {
                    count += interactable.SelectingInteractorViews.Count();
                }

                return count;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractableView.InteractorViews"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public IEnumerable<IInteractorView> InteractorViews =>
            Interactables.SelectMany(interactable => interactable.InteractorViews).ToList();

        /// <summary>
        /// Implementation of <see cref="IInteractableView.SelectingInteractorViews"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public IEnumerable<IInteractorView> SelectingInteractorViews =>
            Interactables.SelectMany(interactable => interactable.SelectingInteractorViews).ToList();

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenInteractorViewAdded"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenInteractorViewAdded = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenInteractorViewRemoved"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenInteractorViewRemoved = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenSelectingInteractorViewAdded"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenSelectingInteractorViewAdded = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenSelectingInteractorViewRemoved"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenSelectingInteractorViewRemoved = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.MaxInteractors"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public int MaxInteractors
        {
            get
            {
                int max = 0;
                foreach (IInteractableView interactable in Interactables)
                {
                    max = Mathf.Max(interactable.MaxInteractors, max);
                }

                return max;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractableView.MaxSelectingInteractors"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public int MaxSelectingInteractors
        {
            get
            {
                int max = 0;
                foreach (IInteractableView interactable in Interactables)
                {
                    max = Mathf.Max(interactable.MaxSelectingInteractors, max);
                }

                return max;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenStateChanged"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<InteractableStateChangeArgs> WhenStateChanged = delegate { };

        private InteractableState _state = InteractableState.Normal;

        /// <summary>
        /// Implementation of <see cref="IInteractableView.State"/>;
        /// for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public InteractableState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state == value) return;
                InteractableState previousState = _state;
                _state = value;
                WhenStateChanged(new InteractableStateChangeArgs(
                    previousState, _state
                ));
            }
        }

        private void UpdateState()
        {
            if (SelectingInteractorsCount > 0)
            {
                State = InteractableState.Select;
                return;
            }
            if (InteractorsCount > 0)
            {
                State = InteractableState.Hover;
                return;
            }
            State = InteractableState.Normal;
        }

        protected virtual void Awake()
        {
            if (_interactables != null)
            {
                Interactables = _interactables.ConvertAll(mono => mono as IInteractableView);
            }
        }

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionItems(Interactables, nameof(Interactables));

            if (Data == null)
            {
                _data = this;
                Data = _data;
            }

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                foreach (IInteractableView interactable in Interactables)
                {
                    interactable.WhenStateChanged += HandleStateChange;
                    interactable.WhenInteractorViewAdded += HandleInteractorViewAdded;
                    interactable.WhenInteractorViewRemoved += HandleInteractorViewRemoved;
                    interactable.WhenSelectingInteractorViewAdded += HandleSelectingInteractorViewAdded;
                    interactable.WhenSelectingInteractorViewRemoved += HandleSelectingInteractorViewRemoved;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (IInteractableView interactable in Interactables)
                {
                    interactable.WhenStateChanged -= HandleStateChange;
                    interactable.WhenInteractorViewAdded -= HandleInteractorViewAdded;
                    interactable.WhenInteractorViewRemoved -= HandleInteractorViewRemoved;
                    interactable.WhenSelectingInteractorViewAdded -= HandleSelectingInteractorViewAdded;
                    interactable.WhenSelectingInteractorViewRemoved -= HandleSelectingInteractorViewRemoved;
                }
            }
        }

        private void HandleStateChange(InteractableStateChangeArgs args)
        {
            UpdateState();
        }

        private void HandleInteractorViewAdded(IInteractorView obj)
        {
            WhenInteractorViewAdded.Invoke(obj);
        }

        private void HandleInteractorViewRemoved(IInteractorView obj)
        {
            WhenInteractorViewRemoved.Invoke(obj);
        }

        private void HandleSelectingInteractorViewAdded(IInteractorView obj)
        {
            WhenSelectingInteractorViewAdded.Invoke(obj);
        }

        private void HandleSelectingInteractorViewRemoved(IInteractorView obj)
        {
            WhenSelectingInteractorViewRemoved.Invoke(obj);
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="InteractableGroupView"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllInteractableGroupView(List<IInteractableView> interactables)
        {
            InjectInteractables(interactables);
        }

        /// <summary>
        /// Sets the underlying <see cref="IInteractableView"/> set for a dynamically instantiated
        /// <see cref="InteractableGroupView"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectInteractables(List<IInteractableView> interactables)
        {
            Interactables = interactables;
            _interactables =
                Interactables.ConvertAll(interactable => interactable as UnityEngine.Object);
        }

        /// <summary>
        /// Sets the underlying optional data object for a dynamically instantiated
        /// <see cref="InteractableGroupView"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalData(object data)
        {
            _data = data as UnityEngine.Object;
            Data = data;
        }


        #endregion
    }
}
