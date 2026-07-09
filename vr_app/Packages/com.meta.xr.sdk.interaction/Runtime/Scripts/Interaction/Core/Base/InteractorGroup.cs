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
    /// Base class from which you should inherit if you are creating an interactor group. Interactor groups are collections of
    /// <see cref="IInteractor"/>s which control the interactors within them, managing their execution and selecting which
    /// interactors are permitted to adopt which states. Information on the role and usage of interactor groups can be found in
    /// [the documentation](https://developer.oculus.com/documentation/unity/unity-isdk-interactor-group/).
    /// </summary>
    public abstract class InteractorGroup : MonoBehaviour, IInteractor
    {
        [SerializeField, Interface(typeof(IInteractor))]
        protected List<UnityEngine.Object> _interactors;
        /// <summary>
        /// The list of <see cref="IInteractor"/>s contained in and controlled by this interactor group. Each interactor can only be
        /// in a single group to avoid conflicts from having multiple groups attempting to control the same interactor. This field is
        /// initialized during the MonoBehaviour start-up process from values set in the Unity Editor or through
        /// <see cref="InjectInteractors(List{IInteractor})"/>. Modifying the set of interactors in a group at runtime is not
        /// supported.
        /// </summary>
        public IReadOnlyList<IInteractor> Interactors;

        [SerializeField, Interface(typeof(IActiveState)), Optional]
        private UnityEngine.Object _activeState;
        private IActiveState ActiveState = null;

        [SerializeField, Interface(typeof(ICandidateComparer)), Optional]
        protected UnityEngine.Object _candidateComparer;
        protected ICandidateComparer CandidateComparer = null;

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

        /// <summary>
        /// Implementation of <see cref="IInteractorView.Data"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public object Data => null;

        /// <summary>
        /// Implementation of <see cref="IUpdateDriver.IsRootDriver"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool IsRootDriver { get; set; } = true;

        #region Abstract
        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldHover"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract bool ShouldHover { get; }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnhover"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract bool ShouldUnhover { get; }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldSelect"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract bool ShouldSelect { get; }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnselect"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract bool ShouldUnselect { get; }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Hover"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract void Hover();

        /// <summary>
        /// Implementation of <see cref="IInteractor.Unhover"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract void Unhover();

        /// <summary>
        /// Implementation of <see cref="IInteractor.Select"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract void Select();

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnselect"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract void Unselect();

        /// <summary>
        /// Implementation of <see cref="IInteractorView.HasCandidate"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract bool HasCandidate { get; }

        /// <summary>
        /// Implementation of <see cref="IInteractorView.HasInteractable"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public abstract bool HasInteractable { get; }

        /// <summary>
        /// Implementation of <see cref="IInteractorView.HasSelectedInteractable"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public abstract bool HasSelectedInteractable { get; }

        public abstract object CandidateProperties { get; }
        #endregion

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

        protected delegate bool InteractorPredicate(IInteractor interactor, int index);
        protected static readonly InteractorPredicate TruePredicate =
            (interactor, index) => true;

        protected static readonly InteractorPredicate HasCandidatePredicate =
            (interactor, index) => interactor.HasCandidate;

        protected static readonly InteractorPredicate HasInteractablePredicate =
            (interactor, index) => interactor.HasInteractable;


        private InteractorState _state = InteractorState.Disabled;

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
            protected set
            {
                if (_state != value)
                {
                    var changeArgs = new InteractorStateChangeArgs(_state, value);
                    _state = value;
                    WhenStateChanged.Invoke(changeArgs);
                }
            }
        }

        private UniqueIdentifier _identifier;

        /// <summary>
        /// Implementation of <see cref="IInteractorView.Identifier"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public int Identifier => _identifier.ID;

        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), this);
            if (ActiveState == null)
            {
                ActiveState = _activeState as IActiveState;
            }

            if (_interactors != null)
            {
                this.WarnInspectorCollectionItems(_interactors, nameof(_interactors));
                Interactors = _interactors
                    .FindAll(mono => mono != null)
                    .ConvertAll(mono => mono as IInteractor);
            }

            if (CandidateComparer == null)
            {
                CandidateComparer = _candidateComparer as ICandidateComparer;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionItems(Interactors, nameof(Interactors));

            for (int i = 0; i < Interactors.Count; i++)
            {
                IInteractor interactor = Interactors[i];
                interactor.IsRootDriver = false;
            }

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Disable();
            }
        }

        protected virtual void OnDestroy()
        {
            UniqueIdentifier.Release(_identifier);
        }

        /// <summary>
        /// Compares two InteractorStates indicating which one is higher
        /// in the Active State chain
        /// </summary>
        /// <param name="a">First state to compare</param>
        /// <param name="b">Seconds state to compare</param>
        /// <returns>1 if b is higher than a, -1 if a is higher than b. 0 of they are equal</returns>
        protected static int CompareStates(InteractorState a, InteractorState b)
        {
            if (a == b)
            {
                return 0;
            }
            if ((a == InteractorState.Disabled && b != InteractorState.Disabled)
                || (a == InteractorState.Normal && (b == InteractorState.Hover || b == InteractorState.Select))
                || (a == InteractorState.Hover && b == InteractorState.Select))
            {
                return 1;
            }
            return -1;
        }

        protected bool TryGetBestCandidateIndex(InteractorPredicate predicate, out int bestCandidateIndex, int betterThan = -1, int skipIndex = -1)
        {
            bestCandidateIndex = betterThan;
            for (int i = 0; i < Interactors.Count; i++)
            {
                if (i == skipIndex)
                {
                    continue;
                }
                IInteractor interactor = Interactors[i];
                if (!predicate(interactor, i))
                {
                    continue;
                }

                if (CompareCandidates(bestCandidateIndex, i) > 0)
                {
                    bestCandidateIndex = i;
                }
            }

            return bestCandidateIndex != betterThan;
        }

        protected bool AnyInteractor(InteractorPredicate predicate)
        {
            for (int i = 0; i < Interactors.Count; i++)
            {
                if (predicate(Interactors[i], i))
                {
                    return true;
                }
            }
            return false;
        }

        protected int CompareCandidates(int indexA, int indexB)
        {
            if (indexA < 0 && indexB >= 0)
            {
                return 1;
            }
            else if (indexA >= 0 && indexB < 0)
            {
                return -1;
            }
            else if (indexA < 0 && indexB < 0)
            {
                return 0;
            }
            else if (indexA == indexB)
            {
                return 0;
            }

            IInteractor a = Interactors[indexA];
            IInteractor b = Interactors[indexB];

            if (!a.HasCandidate && !b.HasCandidate)
            {
                return indexA < indexB ? -1 : 1;
            }

            if (a.HasCandidate && b.HasCandidate)
            {
                if (CandidateComparer == null)
                {
                    return indexA < indexB ? -1 : 1;
                }

                int result = CandidateComparer.Compare(a.CandidateProperties, b.CandidateProperties);
                return result < 0 ? -1 : 1;
            }

            return a.HasCandidate ? -1 : 1;
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Preprocess"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual void Preprocess()
        {
            if (!UpdateActiveState())
            {
                Disable();
            }
            else
            {
                for (int i = 0; i < Interactors.Count; i++)
                {
                    Interactors[i].Preprocess();
                }
            }
            WhenPreprocessed();
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Process"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual void Process()
        {
            for (int i = 0; Interactors != null && i < Interactors.Count; i++)
            {
                Interactors[i].Process();
            }
            WhenProcessed();
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Postprocess"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual void Postprocess()
        {
            for (int i = 0; Interactors != null && i < Interactors.Count; i++)
            {
                Interactors[i].Postprocess();
            }
            WhenPostprocessed();
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ProcessCandidate"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual void ProcessCandidate()
        {
            if (!UpdateActiveState())
            {
                return;
            }

            for (int i = 0; i < Interactors.Count; i++)
            {
                IInteractor interactor = Interactors[i];
                if (interactor.State == InteractorState.Hover
                    || interactor.State == InteractorState.Normal)
                {
                    interactor.ProcessCandidate();
                }
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Enable"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual void Enable()
        {
            if (!UpdateActiveState())
            {
                return;
            }

            for (int i = 0; i < Interactors.Count; i++)
            {
                Interactors[i].Enable();
            }

            if (State == InteractorState.Disabled)
            {
                State = InteractorState.Normal;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.Disable"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual void Disable()
        {
            for (int i = 0; Interactors != null && i < Interactors.Count; i++)
            {
                Interactors[i].Disable();
            }
            State = InteractorState.Disabled;
        }

        protected void DisableAllExcept(IInteractor mainInteractor)
        {
            for (int i = 0; i < Interactors.Count; i++)
            {
                IInteractor interactor = Interactors[i];
                if (interactor != mainInteractor)
                {
                    interactor.Disable();
                }
            }
        }


        protected void EnableAllExcept(IInteractor mainInteractor)
        {
            for (int i = 0; i < Interactors.Count; i++)
            {
                IInteractor interactor = Interactors[i];
                if (interactor != mainInteractor)
                {
                    interactor.Enable();
                }
            }
        }

        protected bool UpdateActiveState()
        {
            bool active = this.isActiveAndEnabled && _started;
            if (ActiveState != null)
            {
                active = active && ActiveState.Active;
            }
            return active;
        }

        protected virtual void Update()
        {
            if (!IsRootDriver)
            {
                return;
            }

            Drive();
        }

        /// <summary>
        /// Implementation of <see cref="IUpdateDriver.Drive"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
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

            for (int i = 0; i < MaxIterationsPerFrame; i++)
            {
                if (State == InteractorState.Normal || State == InteractorState.Hover)
                {
                    ProcessCandidate();
                }

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
        /// Convenience method that injects all required dependencies for a dynamically instantiated InteractorGroup; because
        /// only one dependency is required, this method simlpy wraps <see cref="InjectInteractors(List{IInteractor})"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity
        /// Editor-based usage.
        /// </summary>
        public void InjectAllInteractorGroupBase(List<IInteractor> interactors)
        {
            InjectInteractors(interactors);
        }

        /// <summary>
        /// Adds a list of <see cref="IInteractor"/>s to a dynamically instantiated InteractorGroup. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectInteractors(List<IInteractor> interactors)
        {
            Interactors = interactors;
            _interactors = interactors.ConvertAll(i => i as UnityEngine.Object);
        }

        /// <summary>
        /// Adds an <see cref="IActiveState"/> to a dynamically instantiated InteractorGroup. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalActiveState(IActiveState activeState)
        {
            ActiveState = activeState;
            _activeState = activeState as UnityEngine.Object;
        }

        /// <summary>
        /// Adds an <see cref="ICandidateComparer"/> to a dynamically instantiated InteractorGroup. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalCandidateComparer(ICandidateComparer candidateComparer)
        {
            CandidateComparer = candidateComparer;
            _candidateComparer = candidateComparer as UnityEngine.Object;
        }
        #endregion

    }
}
