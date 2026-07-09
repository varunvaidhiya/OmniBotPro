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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Base class for most concrete interactor types. New interactors can be created by inheriting from this class directly; however,
    /// it is also common for new interactors to inherit from <see cref="PointerInteractor{TInteractor, TInteractable}"/>, a descendent
    /// type which adds features for characterizing interactions as <see cref="PointerEvent"/>s.
    /// </summary>
    /// <remarks>
    /// Interactions can be wholly defined by three things: the concrete Interactor, the concrete Interactable, and the logic governing
    /// their coordination. Subclasses are responsible for implementing that coordination logic via template methods that operate on
    /// the concrete interactor and interactable classes.
    ///
    /// This type has a [curiously recurring](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern) generic argument
    /// <typeparamref name="TInteractor"/>, which should be the concrete interactor type which derives from this type and is uniquely
    /// associated with <typeparamref name="TInteractable"/>.
    /// </remarks>
    public abstract class Interactor<TInteractor, TInteractable> : MonoBehaviour, IInteractor
                                    where TInteractor : Interactor<TInteractor, TInteractable>
                                    where TInteractable : Interactable<TInteractor, TInteractable>
    {
        #region Oculus Library Variables and Constants
        private const ulong DefaultNativeId = 0x494e56414c494420;
        protected ulong _nativeId = DefaultNativeId;
        #endregion Oculus Library Methods and Constants

        /// <summary>
        /// An ActiveState whose value determines if the interactor is enabled or disabled.
        /// </summary>
        [Tooltip("An ActiveState whose value determines if the interactor is enabled or disabled.")]
        [SerializeField, Interface(typeof(IActiveState)), Optional]
        private UnityEngine.Object _activeState;
        private IActiveState ActiveState = null;

        /// <summary>
        /// The interactables this interactor can or can't use. Is determined by comparing this interactor's TagSetFilter component(s) to the TagSet component on the interactables.
        /// </summary>
        [Tooltip("The interactables this interactor can or can't use. Is determined by comparing this interactor's TagSetFilter component(s) to the TagSet component on the interactables.")]
        [SerializeField, Interface(typeof(IGameObjectFilter)), Optional]
        private List<UnityEngine.Object> _interactableFilters = new List<UnityEngine.Object>();
        private List<IGameObjectFilter> InteractableFilters = null;

        /// <summary>
        /// Custom logic used to determine the best interactable candidate.
        /// </summary>
        [Tooltip("Custom logic used to determine the best interactable candidate.")]
        [SerializeField, Interface(nameof(CandidateTiebreaker)), Optional]
        private UnityEngine.Object _candidateTiebreaker;
        private IComparer<TInteractable> CandidateTiebreaker;

        private Func<TInteractable> _computeCandidateOverride;
        private bool _clearComputeCandidateOverrideOnSelect = false;
        private Func<bool> _computeShouldSelectOverride;
        private bool _clearComputeShouldSelectOverrideOnSelect = false;
        private Func<bool> _computeShouldUnselectOverride;
        private bool _clearComputeShouldUnselectOverrideOnUnselect;

        protected virtual void DoPreprocess() { }
        protected virtual void DoNormalUpdate() { }
        protected virtual void DoHoverUpdate() { }
        protected virtual void DoSelectUpdate() { }
        protected virtual void DoPostprocess() { }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldHover"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal)
                {
                    return false;
                }

                return HasCandidate || ComputeShouldSelect();
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnhover"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _interactable != _candidate || _candidate == null;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldSelect"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                if (_computeShouldSelectOverride != null)
                {
                    return _computeShouldSelectOverride.Invoke();
                }

                return _candidate == _interactable && ComputeShouldSelect();
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnselect"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool ShouldUnselect
        {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                if (_computeShouldUnselectOverride != null)
                {
                    return _computeShouldUnselectOverride.Invoke();
                }

                return ComputeShouldUnselect();
            }
        }

        protected virtual bool ComputeShouldSelect()
        {
            return QueuedSelect;
        }

        protected virtual bool ComputeShouldUnselect()
        {
            return QueuedUnselect;
        }

        private InteractorState _state = InteractorState.Disabled;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.WhenStateChanged"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action<InteractorStateChangeArgs> WhenStateChanged = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractorView.WhenPreprocessed"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action WhenPreprocessed = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractorView.WhenProcessed"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action WhenProcessed = delegate { };

        /// <summary>
        /// Implementation of <see cref="IInteractorView.WhenPostprocessed"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public event Action WhenPostprocessed = delegate { };

        private ISelector _selector = null;

        /// <summary>
        /// The maximum number of state changes that can occur per frame. For example, the interactor switching from normal to hover
        /// or vice-versa counts as one state change.
        /// </summary>
        [Tooltip("The maximum number of state changes that can occur per frame. For example, the interactor switching from normal to hover or vice-versa counts as one state change.")]
        [SerializeField]
        private int _maxIterationsPerFrame = 3;

        /// <summary>
        /// This is an internal API which sets or retrieves the maximum number of times the interactor group will execute its
        /// processing loop (assessing and enacting changes in the groups <see cref="InteractorState"/>, among other effects)
        /// within a single frame. The implications of this are complex, and depending on or modifying this value is not
        /// recommended.
        /// </summary>
        public int MaxIterationsPerFrame
        {
            get
            {
                return _maxIterationsPerFrame;
            }
            set
            {
                _maxIterationsPerFrame = value;
            }
        }

        protected ISelector Selector
        {
            get
            {
                return _selector;
            }

            set
            {
                if (value != _selector)
                {
                    if (_selector != null && _started)
                    {
                        _selector.WhenSelected -= HandleSelected;
                        _selector.WhenUnselected -= HandleUnselected;
                    }
                }

                _selector = value;
                if (_selector != null && _started)
                {
                    _selector.WhenSelected += HandleSelected;
                    _selector.WhenUnselected += HandleUnselected;
                }
            }
        }

        private Queue<bool> _selectorQueue = new Queue<bool>();
        private bool QueuedSelect => _selectorQueue.Count > 0 && _selectorQueue.Peek();
        private bool QueuedUnselect => _selectorQueue.Count > 0 && !_selectorQueue.Peek();

        /// <summary>
        /// Implementation of <see cref="IInteractorView.State"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public InteractorState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state == value)
                {
                    return;
                }
                InteractorState previousState = _state;
                _state = value;

                WhenStateChanged(new InteractorStateChangeArgs(previousState, _state));

                // Update native component
                if (_nativeId != DefaultNativeId && _state == InteractorState.Select)
                {
                    NativeMethods.isdk_NativeComponent_Activate(_nativeId);
                }
            }
        }

        protected TInteractable _candidate;
        protected TInteractable _interactable;
        protected TInteractable _selectedInteractable;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.CandidateProperties"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual object CandidateProperties
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// The current interaction candidate. This is the <typeparamref name="TInteractable"/> with which this interactor can,
        /// but may not yet have begun to, interact.
        /// </summary>
        /// <remarks>
        /// The candidate is primarily used by the interactor itself (as the thing it knows it can interact with next) and by
        /// containing interactor groups, which frequently use the availability or unavailability of a candidate in a given frame to
        /// assess which interactors should perform the interactions of which they're capable.
        /// </remarks>
        public TInteractable Candidate => _candidate;

        /// <summary>
        /// The <typeparamref name="TInteractable"/> with which this interactor is currently interacting. This may or may not be
        /// the same as the <see cref="Candidate"/>
        /// </summary>
        public TInteractable Interactable => _interactable;

        /// <summary>
        /// The <typeparamref name="TInteractable"/> which this interactor is currently selecting. This must be the same as
        /// <see cref="Interactable"/>, and neither SelectedInteractable nor <see cref="Interactable"/> will change value until
        /// this interactor unselects.
        /// </summary>
        public TInteractable SelectedInteractable => _selectedInteractable;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.HasCandidate"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool HasCandidate => _candidate != null;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.HasInteractable"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool HasInteractable => _interactable != null;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.HasSelectedInteractable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public bool HasSelectedInteractable => _selectedInteractable != null;

        private MultiAction<TInteractable> _whenInteractableSet = new MultiAction<TInteractable>();
        private MultiAction<TInteractable> _whenInteractableUnset = new MultiAction<TInteractable>();
        private MultiAction<TInteractable> _whenInteractableSelected = new MultiAction<TInteractable>();
        private MultiAction<TInteractable> _whenInteractableUnselected = new MultiAction<TInteractable>();

        /// <summary>
        /// An event indicating that a new value is available via <see cref="Interactable"/>. In terms of <see cref="InteractorState"/>,
        /// this occurs when the interactor begins hovering a new interactable.
        /// </summary>
        public MAction<TInteractable> WhenInteractableSet => _whenInteractableSet;

        /// <summary>
        /// An event indicating that <see cref="Interactable"/> will now return `null`. In terms of <see cref="InteractorState"/>,
        /// this occurs when the interactor ceases hovering an interactable.
        /// </summary>
        public MAction<TInteractable> WhenInteractableUnset => _whenInteractableUnset;

        /// <summary>
        /// An event indicating that a new value is available via <see cref="SelectedInteractable"/>. In terms of
        /// <see cref="InteractorState"/>, this occurs when the interactor begins selecting a new interactable.
        /// </summary>
        public MAction<TInteractable> WhenInteractableSelected => _whenInteractableSelected;

        /// <summary>
        /// An event indicating that <see cref="SelectedInteractable"/> will now return `null`. In terms of
        /// <see cref="InteractorState"/>, this occurs when the interactor ceases selecting an interactable.
        /// </summary>
        public MAction<TInteractable> WhenInteractableUnselected => _whenInteractableUnselected;

        protected virtual void InteractableSet(TInteractable interactable)
        {
            _whenInteractableSet.Invoke(interactable);
        }

        protected virtual void InteractableUnset(TInteractable interactable)
        {
            _whenInteractableUnset.Invoke(interactable);
        }

        protected virtual void InteractableSelected(TInteractable interactable)
        {
            _whenInteractableSelected.Invoke(interactable);
        }

        protected virtual void InteractableUnselected(TInteractable interactable)
        {
            _whenInteractableUnselected.Invoke(interactable);
        }

        private UniqueIdentifier _identifier;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.Identifier"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public int Identifier => _identifier.ID;

        /// <summary>
        /// Can supply additional data (ex. data from an Interactable about a given Interactor, or vice-versa), or pass data along with events like PointerEvent (ex. the associated Interactor generating the event).
        /// </summary>
        [Tooltip("Can supply additional data (ex. data from an Interactable about a given Interactor, or vice-versa), or pass data along with events like PointerEvent (ex. the associated Interactor generating the event).")]
        [SerializeField, Optional]
        private UnityEngine.Object _data = null;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.Data"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public object Data { get; protected set; } = null;

        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
            ActiveState = _activeState as IActiveState;
            CandidateTiebreaker = _candidateTiebreaker as IComparer<TInteractable>;
            InteractableFilters =
                _interactableFilters.ConvertAll(mono => mono as IGameObjectFilter);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionItems(InteractableFilters, nameof(InteractableFilters));

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
            if (_started)
            {
                if (_selector != null)
                {
                    _selectorQueue.Clear();
                    _selector.WhenSelected += HandleSelected;
                    _selector.WhenUnselected += HandleUnselected;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_selector != null)
                {
                    _selector.WhenSelected -= HandleSelected;
                    _selector.WhenUnselected -= HandleUnselected;
                }
                Disable();
            }
        }

        protected virtual void OnDestroy()
        {
            UniqueIdentifier.Release(_identifier);
        }

        /// <summary>
        /// Overrides the interactor's <see cref="ComputeCandidate"/> method with a new method.
        /// <param name="computeCandidate">The method used instead of the interactable's existing ComputeCandidate() method.</param>
        /// <param name="shouldClearOverrideOnSelect">If true, clear the computeCandidate function once you select an interactable.</param>
        /// </summary>
        /// <remarks>
        /// <see cref="ComputeCandidate"/> is part of the core processing flow of <see cref="IInteractor"/>s and is the primary way that
        /// individual interactors decide what they want to interact with. Computing a new candidate does not actually _do_ the
        /// interaction because not all interaction candidates will result in interactions: an <see cref="InteractorGroup"/> might
        /// decide that one interactor's candidate cannot be acted upon because another interactor in the group is already interacting,
        /// for example. Managing this nuance, and the state associated with it, is why direct calls to methods like
        /// <see cref="IInteractor.Hover"/> cannot safely be used to force an interactor into a desired state, as attempting to do so
        /// violates the interactor's processing flow and may cause it to have invalid internal state. SetComputeCandidateOverride,
        /// however, can serve a similar function by arbitrarily controlling candidate discovery in the interactor. In this way, while
        /// an interactor still cannot be generally forced to interact with a specific candidate, it can be forced to _consider_
        /// a specific candidate within its proper processing flow, which unless prevented (by an intervening
        /// <see cref="InteractorGroup"/>, for example) can be made to result in the desired interaction.
        /// </remarks>
        public virtual void SetComputeCandidateOverride(Func<TInteractable> computeCandidate,
            bool shouldClearOverrideOnSelect = true)
        {
            _computeCandidateOverride = computeCandidate;
            _clearComputeCandidateOverrideOnSelect = shouldClearOverrideOnSelect;
        }

        /// <summary>
        /// Clears the function provided in <see cref="SetComputeCandidateOverride"/>. This is called when the interactor
        /// force releases an interactable.
        /// </summary>
        public virtual void ClearComputeCandidateOverride()
        {
            _computeCandidateOverride = null;
            _clearComputeCandidateOverrideOnSelect = false;
        }

        /// <summary>
        /// Overrides the interactor's ComputeShouldSelect() method with a new method.
        /// </summary>
        /// <param name="computeShouldSelect">The method used instead of the interactor's existing ComputeShouldSelect() method.</param>
        /// <param name="clearOverrideOnSelect">If true, clear the computeShouldSelect function once you select an interactable.</param>
        /// <remarks>
        /// For similar reasons to those discussed in the documentation for
        /// <see cref="SetComputeCandidateOverride(Func{TInteractable}, bool)"/>, an interactor cannot be directly forced to select
        /// an interactable (except in special, bespoke methods such as
        /// <see cref="HandGrab.HandGrabInteractor.ForceSelect(HandGrab.HandGrabInteractable, bool)"/>), but the logic it uses to
        /// decide whether or not to select can be overriden so that its normal processing is guaranteed to result in selection
        /// (again, absent intervening forces such as an <see cref="InteractorGroup"/>).
        /// </remarks>
        public virtual void SetComputeShouldSelectOverride(Func<bool> computeShouldSelect,
            bool clearOverrideOnSelect = true)
        {
            _computeShouldSelectOverride = computeShouldSelect;
            _clearComputeShouldSelectOverrideOnSelect = clearOverrideOnSelect;
        }

        /// <summary>
        /// Clears the function provided in <see cref="SetComputeShouldSelectOverride"/>. This is called when the interactor
        /// force releases an interactable.
        /// </summary>
        public virtual void ClearComputeShouldSelectOverride()
        {
            _computeShouldSelectOverride = null;
            _clearComputeShouldSelectOverrideOnSelect = false;
        }

        /// <summary>
        /// Overrides the interactor's <see cref="ComputeShouldUnselect"/> method with a new method.
        /// </summary>
        /// <param name="computeShouldUnselect">The method used instead of the interactor's existing ComputeShouldUnselect() method.</param>
        /// <param name="clearOverrideOnUnselect">If true, clear the computeShouldUnselect function once you unselect an interactable.</param>
        /// <remarks>See <see cref="SetComputeShouldSelectOverride(Func{bool}, bool)"/> for relevant details</remarks>
        public virtual void SetComputeShouldUnselectOverride(Func<bool> computeShouldUnselect,
            bool clearOverrideOnUnselect = true)
        {
            _computeShouldUnselectOverride = computeShouldUnselect;
            _clearComputeShouldUnselectOverrideOnUnselect = clearOverrideOnUnselect;
        }

        /// <summary>
        /// Clears the function provided in <see cref="SetComputeShouldUnselectOverride"/>. This is called when the interactor unselects
        /// an interactable.
        /// </summary>
        public virtual void ClearComputeShouldUnselectOverride()
        {
            _computeShouldUnselectOverride = null;
            _clearComputeShouldUnselectOverrideOnUnselect = false;
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Preprocess"/>; executes any logic that should run before the interactor-specific
        /// logic. Runs before <see cref="Process"/> and <see cref="Postprocess"/>. This method should never be invoked directly except
        /// by either this interactor instance itself or the <see cref="InteractorGroup"/> to which it belongs.
        /// </summary>
        public void Preprocess()
        {
            if (_started)
            {
                DoPreprocess();
            }

            if (!UpdateActiveState())
            {
                Disable();
            }

            WhenPreprocessed();
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Process"/>; runs interactor-specific logic based on the interactor's
        /// current state. Runs after <see cref="Preprocess"/> but before <see cref="Postprocess"/>. Can be called multiple times per
        /// interaction frame, with the number capped by the value of <see cref="MaxIterationsPerFrame"/>. This method should never be
        /// invoked directly except by either this interactor instance itself or the <see cref="InteractorGroup"/> to which it belongs.
        /// </summary>
        public void Process()
        {
            switch (State)
            {
                case InteractorState.Normal:
                    DoNormalUpdate();
                    break;
                case InteractorState.Hover:
                    DoHoverUpdate();
                    break;
                case InteractorState.Select:
                    DoSelectUpdate();
                    break;
            }
            WhenProcessed();
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Process"/>; executes any logic that should run after the interactor-specific
        /// logic. Runs after both <see cref="Preprocess"/> and <see cref="Process"/>. This method should never be
        /// invoked directly except by either this interactor instance itself or the <see cref="InteractorGroup"/> to which it belongs.
        /// </summary>
        public void Postprocess()
        {
            _selectorQueue.Clear();

            if (_started)
            {
                DoPostprocess();
            }

            WhenPostprocessed();
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ProcessCandidate"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public virtual void ProcessCandidate()
        {
            _candidate = null;

            if (!UpdateActiveState())
            {
                return;
            }

            if (_computeCandidateOverride != null)
            {
                _candidate = _computeCandidateOverride.Invoke();
            }
            else
            {
                _candidate = ComputeCandidate();
            }
        }

        /// <summary>
        /// Causes the interactor to unselect or unhover an interactable. Called when an interactable is currently selected or
        /// hovered but a <see cref="PointerEvent"/> of type <see cref="PointerEventType.Cancel"/> occurs.
        /// </summary>
        public void InteractableChangesUpdate()
        {
            if (_selectedInteractable != null &&
                !_selectedInteractable.HasSelectingInteractor(this as TInteractor))
            {
                UnselectInteractable();
            }

            if (_interactable != null &&
                !_interactable.HasInteractor(this as TInteractor))
            {
                UnsetInteractable();
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Hover"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Hover()
        {
            if (State != InteractorState.Normal)
            {
                return;
            }

            SetInteractable(_candidate);
            State = InteractorState.Hover;
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Unhover"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            UnsetInteractable();
            State = InteractorState.Normal;
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Select"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public virtual void Select()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            if (_clearComputeCandidateOverrideOnSelect)
            {
                ClearComputeCandidateOverride();
            }

            if (_clearComputeShouldSelectOverrideOnSelect)
            {
                ClearComputeShouldSelectOverride();
            }

            while (QueuedSelect)
            {
                _selectorQueue.Dequeue();
            }

            if (Interactable != null)
            {
                SelectInteractable(Interactable);
            }

            State = InteractorState.Select;
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Unselect"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public virtual void Unselect()
        {
            if (State != InteractorState.Select)
            {
                return;
            }
            if (_clearComputeShouldUnselectOverrideOnUnselect)
            {
                ClearComputeShouldUnselectOverride();
            }
            while (QueuedUnselect)
            {
                _selectorQueue.Dequeue();
            }
            UnselectInteractable();

            State = InteractorState.Hover;
        }

        // Returns the best interactable for selection or null
        protected abstract TInteractable ComputeCandidate();

        protected virtual int ComputeCandidateTiebreaker(TInteractable a, TInteractable b)
        {
            if (CandidateTiebreaker == null)
            {
                return 0;
            }

            return CandidateTiebreaker.Compare(a, b);
        }

        /// <summary>
        /// Determines if this interactor can interact with an interactable.
        /// </summary>
        /// <param name="interactable">The interactable to check against.</param>
        /// <returns>True if the interactor can interact with the given interactable.</returns>
        /// <remarks>
        /// Despite its name, this method is not a part of the interactor processing flow alongside <see cref="ShouldSelect"/> and
        /// <see cref="Select"/>; both of those refer to things that respectively should and do happen _now_, as part of processing.
        /// CanSelect does not specifically test whether or not an interactable can be selected _right now_; for example, a grab
        /// interactor can only select interactables that are spatially nearby, but proximity is not taken into account at all by
        /// CanSelect. Rather, CanSelect assesses whether an interactable can be selected more generally, such as whether there are any
        /// special rules which should prevent the selection even if every other condition were satisfied.
        /// <see cref="InteractableFilters"/> are the primary form such selection-blocking rules take, but descendent classes can
        /// introduce any other type of selection blocker they wish by overriding CanSelect.
        /// </remarks>
        public virtual bool CanSelect(TInteractable interactable)
        {
            if (InteractableFilters == null)
            {
                return true;
            }

            foreach (IGameObjectFilter interactableFilter in InteractableFilters)
            {
                if (!interactableFilter.Filter(interactable.gameObject))
                {
                    return false;
                }
            }

            return true;
        }

        private void SetInteractable(TInteractable interactable)
        {
            if (_interactable == interactable)
            {
                return;
            }
            UnsetInteractable();
            _interactable = interactable;
            interactable.AddInteractor(this as TInteractor);
            InteractableSet(interactable);
        }

        private void UnsetInteractable()
        {
            TInteractable interactable = _interactable;
            if (interactable == null)
            {
                return;
            }
            _interactable = null;
            interactable.RemoveInteractor(this as TInteractor);
            InteractableUnset(interactable);
        }

        private void SelectInteractable(TInteractable interactable)
        {
            Unselect();
            _selectedInteractable = interactable;
            interactable.AddSelectingInteractor(this as TInteractor);
            InteractableSelected(interactable);
        }

        private void UnselectInteractable()
        {
            TInteractable interactable = _selectedInteractable;

            if (interactable == null)
            {
                return;
            }

            _selectedInteractable = null;
            interactable.RemoveSelectingInteractor(this as TInteractor);
            InteractableUnselected(interactable);
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Enable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Enable()
        {
            if (!UpdateActiveState())
            {
                return;
            }

            if (State == InteractorState.Disabled)
            {
                State = InteractorState.Normal;
                HandleEnabled();
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Disable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public void Disable()
        {
            if (State == InteractorState.Disabled)
            {
                return;
            }

            HandleDisabled();

            if (State == InteractorState.Select)
            {
                UnselectInteractable();
                State = InteractorState.Hover;
            }

            if (State == InteractorState.Hover)
            {
                UnsetInteractable();
                State = InteractorState.Normal;
            }

            if (State == InteractorState.Normal)
            {
                State = InteractorState.Disabled;
            }
        }

        protected virtual void HandleEnabled() { }
        protected virtual void HandleDisabled() { }

        protected virtual void HandleSelected()
        {
            _selectorQueue.Enqueue(true);
        }

        protected virtual void HandleUnselected()
        {
            _selectorQueue.Enqueue(false);
        }

        private bool UpdateActiveState()
        {
            bool active = this.isActiveAndEnabled && _started;
            if (ActiveState != null)
            {
                active = active && ActiveState.Active;
            }
            return active;
        }

        /// <summary>
        /// Implementation of <see cref="IUpdateDriver.IsRootDriver"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public bool IsRootDriver { get; set; } = true;

        protected virtual void Update()
        {
            if (!IsRootDriver)
            {
                return;
            }

            Drive();
        }

        /// <summary>
        /// Implementation of <see cref="IUpdateDriver.Drive"/>. This method should never be invoked directly by anything other
        /// than this instance (when <see cref="IsRootDriver"/> is true) or the <see cref="IUpdateDriver"/> serving the role of root
        /// driver.
        /// </summary>
        /// <remarks>
        /// This method encapsulates the entire processing flow of the interactor, invoking many of the core methods from
        /// <see cref="IInteractor"/> (<see cref="Enable"/>, <see cref="Process"/>, etc.) in the correct order and with the
        /// appropriate state management. Modification to or deviation from this flow very fundamentally changes how interactors work
        /// and is likely to result in undefined behavior. Consequently, neither directly invoking nor overriding this method is
        /// recommended.
        /// </remarks>
        public virtual void Drive()
        {
            Preprocess();

            if (!UpdateActiveState())
            {
                Disable();
                Postprocess();
                return;
            }

            Enable();

            InteractorState previousState = State;
            for (int i = 0; i < MaxIterationsPerFrame; i++)
            {
                if (State == InteractorState.Normal ||
                    (State == InteractorState.Hover && previousState != InteractorState.Normal))
                {
                    ProcessCandidate();
                }
                previousState = State;

                Process();

                if (State == InteractorState.Disabled)
                {
                    break;
                }

                if (State == InteractorState.Normal)
                {
                    if (ShouldHover)
                    {
                        Hover();
                        continue;
                    }
                    break;
                }

                if (State == InteractorState.Hover)
                {
                    if (ShouldSelect)
                    {
                        Select();
                        continue;
                    }
                    if (ShouldUnhover)
                    {
                        Unhover();
                        continue;
                    }
                    break;
                }

                if (State == InteractorState.Select)
                {
                    if (ShouldUnselect)
                    {
                        Unselect();
                        continue;
                    }
                    break;
                }
            }

            Postprocess();
        }

        #region Inject

        /// <summary>
        /// Adds an <see cref="IActiveState"/>s to a dynamically instantiated Interactor. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalActiveState(IActiveState activeState)
        {
            _activeState = activeState as UnityEngine.Object;
            ActiveState = activeState;
        }

        /// <summary>
        /// Adds a set of <see cref="IGameObjectFilter"/>s to a dynamically instantiated Interactor. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalInteractableFilters(List<IGameObjectFilter> interactableFilters)
        {
            InteractableFilters = interactableFilters;
            _interactableFilters = interactableFilters.ConvertAll(interactableFilter =>
                                    interactableFilter as UnityEngine.Object);
        }

        /// <summary>
        /// Adds an <see cref="ICandidateComparer"/>s to a dynamically instantiated Interactor. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalCandidateTiebreaker(IComparer<TInteractable> candidateTiebreaker)
        {
            _candidateTiebreaker = candidateTiebreaker as UnityEngine.Object;
            CandidateTiebreaker = candidateTiebreaker;
        }

        /// <summary>
        /// Sets <see cref="Data"/> property on a dynamically instantiated Interactor. This method exists to support
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
