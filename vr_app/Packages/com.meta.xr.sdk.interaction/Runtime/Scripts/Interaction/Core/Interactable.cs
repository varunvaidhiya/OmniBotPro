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
using Oculus.Interaction.Collections;

namespace Oculus.Interaction
{
    /// <summary>
    /// Base class for most concrete interactable types. New interactables can be created by inheriting from this class directly;
    /// however, it is also common for new interactables to inherit from <see cref="PointerInteractable{TInteractor, TInteractable}"/>,
    /// a descendent type which adds features for characterizing interactions as <see cref="PointerEvent"/>s.
    /// </summary>
    /// <remarks>
    /// Interactions can be wholly defined by three things: the concrete Interactor, the concrete Interactable, and the logic governing
    /// their coordination. Subclasses are responsible for implementing that coordination logic via template methods that operate on
    /// the concrete interactor and interactable classes.
    ///
    /// This type has a [curiously recurring](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern) generic argument
    /// <typeparamref name="TInteractable"/>, which should be the concrete interactable type which derives from this type and is
    /// uniquely associated with <typeparamref name="TInteractor"/>.
    /// </remarks>
    public abstract class Interactable<TInteractor, TInteractable> : MonoBehaviour, IInteractable
                                        where TInteractor : Interactor<TInteractor, TInteractable>
                                        where TInteractable : Interactable<TInteractor, TInteractable>
    {
        [SerializeField, Interface(typeof(IGameObjectFilter)), Optional]
        private List<UnityEngine.Object> _interactorFilters = new List<UnityEngine.Object>();
        private List<IGameObjectFilter> InteractorFilters = null;

        /// <summary>
        /// The max Interactors and max selecting Interactors that this Interactable can
        /// have acting on it.
        /// -1 signifies NO limit (can have any number of Interactors)
        /// </summary>
        [SerializeField]
        private int _maxInteractors = -1;

        /// <summary>
        /// The max selecting Interactors that this Interactable can
        /// have acting on it. -1 signifies no limit (can have any number of Interactors).
        /// </summary>
        [SerializeField]
        private int _maxSelectingInteractors = -1;

        /// <summary>
        /// A data object that can provide additional information.
        /// </summary>
        [SerializeField, Optional]
        private UnityEngine.Object _data = null;

        /// <summary>
        /// Implementation of <see cref="IInteractableView.Data"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public object Data { get; protected set; } = null;

        protected bool _started = false;

        private UniqueIdentifier _identifier;

        /// <summary>
        /// An identifier uniquely associated with the conceptual Interactable instance.
        /// Identifiers are associated with individual instances of concrete Interactable types,
        /// with each instance assigned a unique identifier.
        /// </summary>
        public int Identifier => _identifier.ID;

        #region Properties
        /// <summary>
        /// Implementation of <see cref="IInteractableView.MaxInteractors"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public int MaxInteractors
        {
            get
            {
                return _maxInteractors;
            }
            set
            {
                _maxInteractors = value;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractableView.MaxSelectingInteractors"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public int MaxSelectingInteractors
        {
            get
            {
                return _maxSelectingInteractors;
            }
            set
            {
                _maxSelectingInteractors = value;
            }
        }
        #endregion

        /// <summary>
        /// Implementation of <see cref="IInteractableView.InteractorViews"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public IEnumerable<IInteractorView> InteractorViews => _interactors.Cast<IInteractorView>();

        /// <summary>
        /// Implementation of <see cref="IInteractableView.SelectingInteractorViews"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public IEnumerable<IInteractorView> SelectingInteractorViews => _selectingInteractors.Cast<IInteractorView>();

        private EnumerableHashSet<TInteractor> _interactors = new EnumerableHashSet<TInteractor>();
        private EnumerableHashSet<TInteractor> _selectingInteractors = new EnumerableHashSet<TInteractor>();

        private InteractableState _state = InteractableState.Disabled;

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenStateChanged"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action<InteractableStateChangeArgs> WhenStateChanged = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenInteractorViewAdded"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenInteractorViewAdded = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenInteractorViewRemoved"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenInteractorViewRemoved = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenSelectingInteractorViewAdded"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenSelectingInteractorViewAdded = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractableView.WhenSelectingInteractorViewRemoved"/>; for details, please refer to the
        /// related documentation provided for that interface.
        /// </summary>
        public event Action<IInteractorView> WhenSelectingInteractorViewRemoved = delegate { };

        private MultiAction<TInteractor> _whenInteractorAdded = new MultiAction<TInteractor>();
        private MultiAction<TInteractor> _whenInteractorRemoved = new MultiAction<TInteractor>();
        private MultiAction<TInteractor> _whenSelectingInteractorAdded = new MultiAction<TInteractor>();
        private MultiAction<TInteractor> _whenSelectingInteractorRemoved = new MultiAction<TInteractor>();

        /// <summary>
        /// An event indicating that a new interactor has been added to <see cref="InteractorViews"/>. In terms of
        /// <see cref="InteractableState"/>, this occurs when this interactable is hovered by a new interactor.
        /// </summary>
        public MAction<TInteractor> WhenInteractorAdded => _whenInteractorAdded;

        /// <summary>
        /// An event indicating that an interactor has been removed from <see cref="InteractorViews"/>. In terms of
        /// <see cref="InteractableState"/>, this occurs when this interactable ceases to be hovered by an interactor.
        /// </summary>
        public MAction<TInteractor> WhenInteractorRemoved => _whenInteractorRemoved;

        /// <summary>
        /// An event indicating that a new interactor has been added to <see cref="SelectingInteractorViews"/>. In terms of
        /// <see cref="InteractableState"/>, this occurs when this interactable is selected by a new interactor.
        /// </summary>
        public MAction<TInteractor> WhenSelectingInteractorAdded => _whenSelectingInteractorAdded;

        /// <summary>
        /// An event indicating that an interactor has been removed from <see cref="SelectingInteractorViews"/>. In terms of
        /// <see cref="InteractableState"/>, this occurs when this interactable ceases to be selected by an interactor.
        /// </summary>
        public MAction<TInteractor> WhenSelectingInteractorRemoved => _whenSelectingInteractorRemoved;

        /// <summary>
        /// Implementation of <see cref="IInteractableView.State"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public InteractableState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state == value) return;
                InteractableState previousState = _state;
                _state = value;
                WhenStateChanged(new InteractableStateChangeArgs(previousState, _state));
            }
        }

        private static InteractableRegistry<TInteractor, TInteractable> _registry =
                                        new InteractableRegistry<TInteractor, TInteractable>();

        /// <summary>
        /// Retrieves the <see cref="InteractableRegistry{TInteractor, TInteractable}"/> for the concrete interactable type
        /// <typeparamref name="TInteractable"/>.
        /// </summary>
        /// <remarks>
        /// The interactable registry is a static global container which, at any given moment, contains all the
        /// <typeparamref name="TInteractable"/>s extant and enabled at that moment.
        /// </remarks>
        public static InteractableRegistry<TInteractor, TInteractable> Registry => _registry;

        protected virtual void InteractorAdded(TInteractor interactor)
        {
            WhenInteractorViewAdded(interactor);
            _whenInteractorAdded.Invoke(interactor);
        }
        protected virtual void InteractorRemoved(TInteractor interactor)
        {
            WhenInteractorViewRemoved(interactor);
            _whenInteractorRemoved.Invoke(interactor);
        }

        protected virtual void SelectingInteractorAdded(TInteractor interactor)
        {
            WhenSelectingInteractorViewAdded(interactor);
            _whenSelectingInteractorAdded.Invoke(interactor);
        }
        protected virtual void SelectingInteractorRemoved(TInteractor interactor)
        {
            WhenSelectingInteractorViewRemoved(interactor);
            _whenSelectingInteractorRemoved.Invoke(interactor);
        }

        /// <summary>
        /// Enumerates all the <typeparamref name="TInteractor"/>s currently associated with this instance. This list is identical
        /// to that returned by <see cref="InteractorViews"/>, but the returned elements are of type <typeparamref name="TInteractor"/>
        /// instead of <see cref="IInteractorView"/>.
        /// </summary>
        public IEnumerableHashSet<TInteractor> Interactors => _interactors;

        /// <summary>
        /// Enumerates all the <typeparamref name="TInteractor"/>s currently selecting this instance. This list is identical
        /// to that returned by <see cref="SelectingInteractorViews"/>, but the returned elements are of type
        /// <typeparamref name="TInteractor"/> instead of <see cref="IInteractorView"/>.
        /// </summary>
        public IEnumerableHashSet<TInteractor> SelectingInteractors => _selectingInteractors;

        /// <summary>
        /// Adds a <typeparamref name="TInteractor"/> to this interactable instance.
        /// </summary>
        /// <remarks>
        /// This is an internal API and should only be invoked by the <paramref name="interactor"/> being added; directly invoking
        /// this from outside the normal interactor processing flow risks creating an asymmetric interactor-interactable relationship
        /// (i.e., a situation where an interactor believes it is associated with an interactable in a certain way, but the
        /// interactable believes something else), which can result in undefined behavior.
        /// </remarks>
        public void AddInteractor(TInteractor interactor)
        {
            _interactors.Add(interactor);
            InteractorAdded(interactor);
            UpdateInteractableState();
        }

        /// <summary>
        /// Removes a <typeparamref name="TInteractor"/> from this interactable instance.
        /// </summary>
        /// <remarks>
        /// This is an internal API and should only be invoked by the <paramref name="interactor"/> being removed; directly invoking
        /// this from outside the normal interactor processing flow risks creating an asymmetric interactor-interactable relationship
        /// (i.e., a situation where an interactor believes it is associated with an interactable in a certain way, but the
        /// interactable believes something else), which can result in undefined behavior.
        /// </remarks>
        public void RemoveInteractor(TInteractor interactor)
        {
            if (!_interactors.Remove(interactor))
            {
                return;
            }
            interactor.InteractableChangesUpdate();
            InteractorRemoved(interactor);
            UpdateInteractableState();
        }

        /// <summary>
        /// Adds a <typeparamref name="TInteractor"/> to the list of interactors selecting this interactable instance.
        /// </summary>
        /// <remarks>
        /// This is an internal API and should only be invoked by the <paramref name="interactor"/> being added; directly invoking
        /// this from outside the normal interactor processing flow risks creating an asymmetric interactor-interactable relationship
        /// (i.e., a situation where an interactor believes it is associated with an interactable in a certain way, but the
        /// interactable believes something else), which can result in undefined behavior.
        /// </remarks>
        public void AddSelectingInteractor(TInteractor interactor)
        {
            _selectingInteractors.Add(interactor);
            SelectingInteractorAdded(interactor);
            UpdateInteractableState();
        }

        /// <summary>
        /// Removes a <typeparamref name="TInteractor"/> from the list of interactors selecting this interactable instance.
        /// </summary>
        /// <remarks>
        /// This is an internal API and should only be invoked by the <paramref name="interactor"/> being removed; directly invoking
        /// this from outside the normal interactor processing flow risks creating an asymmetric interactor-interactable relationship
        /// (i.e., a situation where an interactor believes it is associated with an interactable in a certain way, but the
        /// interactable believes something else), which can result in undefined behavior.
        /// </remarks>
        public void RemoveSelectingInteractor(TInteractor interactor)
        {
            if (!_selectingInteractors.Remove(interactor))
            {
                return;
            }
            interactor.InteractableChangesUpdate();
            SelectingInteractorRemoved(interactor);
            UpdateInteractableState();
        }

        private void UpdateInteractableState()
        {
            if (State == InteractableState.Disabled) return;

            if (_selectingInteractors.Count > 0)
            {
                State = InteractableState.Select;
            }
            else if (_interactors.Count > 0)
            {
                State = InteractableState.Hover;
            }
            else
            {
                State = InteractableState.Normal;
            }
        }

        /// <summary>
        /// Determines if the interactable can be interacted on by the given interactor.
        /// </summary>
        /// <param name="interactor"> The interactor that intends to interact with the interactable.</param>
        /// <returns>True if the interactor can interact with the interactable, false otherwise.</returns>
        /// <remarks>
        /// Similar to <see cref="Interactor{TInteractor, TInteractable}.CanSelect(TInteractable)"/>, the specific question answered
        /// by this method is not about interaction _candidacy_ but more about interaction _possibility_: it does not test whether
        /// the <paramref name="interactor"/> might select this interactable, but only whether the interactor would be able to if it
        /// tried. This is a more ephemeral answer for <typeparamref name="TInteractable"/> than for <typeparamref name="TInteractor"/>;
        /// enablement and number of currently active interactions (both highly variable considerations over time) factor into this
        /// test for the interactable. To emphasize this point <see cref="Interactor{TInteractor, TInteractable}.CanSelect(TInteractable)"/>
        /// and <see cref="Interactable{TInteractor, TInteractable}.CanBeSelectedBy(TInteractor)"/> are asymmetric: the fact that an
        /// interactor is capable of selecting an interactable does not imply that the interactable is capable of being selected by that
        /// interactor, nor vice versa, and _both_ must be true in order for a valid interaction to be possible.
        /// </remarks>
        public bool CanBeSelectedBy(TInteractor interactor)
        {
            if (State == InteractableState.Disabled)
            {
                return false;
            }

            if (MaxSelectingInteractors >= 0 &&
                _selectingInteractors.Count == MaxSelectingInteractors)
            {
                return false;
            }

            if (MaxInteractors >= 0 &&
                _interactors.Count == MaxInteractors &&
                !_interactors.Contains(interactor))
            {
                return false;
            }

            if (InteractorFilters == null)
            {
                return true;
            }

            foreach (IGameObjectFilter interactorFilter in InteractorFilters)
            {
                if (!interactorFilter.Filter(interactor.gameObject))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the interactable is being hovered by the given interactor. This is a convenience method identical to calling
        /// the `Contains()` method on <see cref="Interactors"/>.
        /// </summary>
        /// <param name="interactor">The interactor to check for hovering.</param>
        /// <returns>True if the interactor is hovering the interactable, false otherwise.</returns>
        public bool HasInteractor(TInteractor interactor)
        {
            return _interactors.Contains(interactor);
        }

        /// <summary>
        /// Determines if the interactable is being selected by the given interactor. This is a convenience method identical to calling
        /// the `Contains()` method on <see cref="SelectingInteractors"/>.
        /// </summary>
        /// <param name="interactor">The interactor to check for selecting.</param>
        /// <returns>True if the interactor is selecting the interactable, false otherwise.</returns>
        public bool HasSelectingInteractor(TInteractor interactor)
        {
            return _selectingInteractors.Contains(interactor);
        }

        /// <summary>
        /// Implementation of <see cref="IInteractable.Enable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Enable()
        {
            if (State != InteractableState.Disabled)
            {
                return;
            }

            if (_started)
            {
                _registry.Register((TInteractable)this);
                State = InteractableState.Normal;
            }

        }

        /// <summary>
        /// Implementation of <see cref="IInteractable.Disable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Disable()
        {
            if (State == InteractableState.Disabled)
            {
                return;
            }

            if (_started)
            {
                List<TInteractor> selectingInteractorsCopy = new List<TInteractor>(_selectingInteractors);
                foreach (TInteractor selectingInteractor in selectingInteractorsCopy)
                {
                    RemoveSelectingInteractor(selectingInteractor);
                }

                List<TInteractor> interactorsCopy = new List<TInteractor>(_interactors);
                foreach (TInteractor interactor in interactorsCopy)
                {
                    RemoveInteractor(interactor);
                }

                _registry.Unregister((TInteractable)this);
                State = InteractableState.Disabled;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractable.RemoveInteractorByIdentifier(int)"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void RemoveInteractorByIdentifier(int id)
        {
            TInteractor foundInteractor = null;
            foreach (TInteractor selectingInteractor in _selectingInteractors)
            {
                if (selectingInteractor.Identifier == id)
                {
                    foundInteractor = selectingInteractor;
                    break;
                }
            }

            if (foundInteractor != null)
            {
                RemoveSelectingInteractor(foundInteractor);
            }

            foundInteractor = null;

            foreach (TInteractor interactor in _interactors)
            {
                if (interactor.Identifier == id)
                {
                    foundInteractor = interactor;
                    break;
                }
            }

            if (foundInteractor == null)
            {
                return;
            }

            RemoveInteractor(foundInteractor);
        }

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
            InteractorFilters = _interactorFilters.ConvertAll(mono => mono as IGameObjectFilter);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionItems(InteractorFilters, nameof(InteractorFilters));

            if (Data == null)
            {
                if (_data == null)
                {
                    _data = this;
                }
                Data = _data;
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            Enable();
        }

        protected virtual void OnDisable()
        {
            Disable();
        }

        protected virtual void SetRegistry(InteractableRegistry<TInteractor, TInteractable> registry)
        {
            if (registry == _registry) return;

            var interactables = _registry.List();
            foreach (TInteractable interactable in interactables)
            {
                registry.Register(interactable);
                _registry.Unregister(interactable);
            }
            _registry = registry;
        }

        #region Inject

        /// <summary>
        /// Sets the interactor filters for this interactable on a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalInteractorFilters(List<IGameObjectFilter> interactorFilters)
        {
            InteractorFilters = interactorFilters;
            _interactorFilters = interactorFilters.ConvertAll(interactorFilter =>
                                    interactorFilter as UnityEngine.Object);
        }

        /// <summary>
        /// Sets the <see cref="Data"/> for this interactable on a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalData(object data)
        {
            _data = data as UnityEngine.Object;
            Data = data;
        }

        #endregion
    }
}
