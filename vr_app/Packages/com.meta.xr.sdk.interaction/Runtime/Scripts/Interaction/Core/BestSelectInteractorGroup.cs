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

namespace Oculus.Interaction
{
    /// <summary>
    /// Manages a group of interactors where multiple interactors can be in the Hover state,
    /// but only the highest-priority interactor can transition to the Select state.
    /// Other interactors are disabled until the selecting interactor unselects.
    /// </summary>
    /// <remarks>
    /// In typical setups, you will add this class to manage hand or controller interactors. The group typically includes interactors such as the <see cref="PokeInteractor"/> and <see cref="GrabInteractor"/>.
    /// By default, the priority of interactors is determined by their index in the <see cref="InteractorGroup.Interactors"/> collection,
    /// a lower index corresponds to a higher priority. However, if a custom <see cref="CandidateComparer{T}"/> is provided, it may override this default behavior based on custom comparison logic.
    /// </remarks>
    public class BestSelectInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;

        private static readonly InteractorPredicate IsHoverAndShouldUnhoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Hover && interactor.ShouldUnhover;

        private static readonly InteractorPredicate IsHoverAndShouldSelectPredicate =
            (interactor, index) => interactor.State == InteractorState.Hover && interactor.ShouldSelect;

        private static readonly InteractorPredicate IsHover =
            (interactor, index) => interactor.State == InteractorState.Hover;
        /// <summary>
        /// Determines whether any interactor should transition to the Hover state.
        /// </summary>
        /// <value>
        /// True if the current state is Normal and any interactor meets the criteria for hovering; otherwise, false.
        /// </value>
        public override bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal)
                {
                    return false;
                }
                return AnyInteractor(IsNormalAndShouldHoverPredicate);
            }
        }
        /// <summary>
        /// Determines whether any interactor should cease to be in the Hover state.
        /// </summary>
        /// <value>
        /// True if the current state is Hover and any interactor meets the criteria for unhovering or no interactors are in the Hover state; otherwise, false.
        /// </value>
        public override bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return AnyInteractor(IsHoverAndShouldUnhoverPredicate)
                    || !AnyInteractor(IsHover);
            }
        }
        /// <summary>
        /// Determines whether any interactor should transition to the Select state.
        /// </summary>
        /// <value>
        /// True if the current state is Hover and any interactor meets the criteria for selection; otherwise, false.
        /// </value>
        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return AnyInteractor(IsHoverAndShouldSelectPredicate);
            }
        }
        /// <summary>
        /// Determines whether the highest-priority interactor should cease being in the Select state.
        /// </summary>
        /// <value>
        /// True if the current state is Select and the highest-priority interactor meets the criteria for unselection; otherwise, false.
        /// </value>
        public override bool ShouldUnselect
        {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return _bestInteractor != null
                    && _bestInteractor.ShouldUnselect;
            }
        }
        /// <summary>
        /// Attempts to transition any suitable interactor into the Hover state.
        /// </summary>
        /// <remarks>
        /// This method iterates through interactors, transitioning the first one that meets the hover criteria into the Hover state.
        /// The group's state updates to Hover if at least one interactor transitions successfully.
        /// </remarks>
        public override void Hover()
        {
            if (TryHover())
            {
                State = InteractorState.Hover;
            }
        }

        private bool TryHover(System.Action<IInteractor> whenHover = null)
        {
            bool anyHover = false;

            while (TryGetBestCandidateIndex(IsNormalAndShouldHoverPredicate, out int index))
            {
                IInteractor interactor = Interactors[index];
                interactor.Hover();
                whenHover?.Invoke(Interactors[index]);
                anyHover = true;
            }

            return anyHover;
        }

        /// <summary>
        /// Attempts to transition any interactor from the Hover state back to the Normal state.
        /// </summary>
        /// <remarks>
        /// This method iterates through interactors and transitions those that meet the unhover criteria out of the Hover state.
        /// If no interactors remain in the Hover state, the group's state is updated to <see cref="InteractorState.Normal"/>.
        /// </remarks>
        public override void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            while (TryGetBestCandidateIndex(IsHoverAndShouldUnhoverPredicate, out int index))
            {
                Interactors[index].Unhover();
            }

            if (!AnyInteractor(IsHover))
            {
                State = InteractorState.Normal;
            }
        }

        /// <summary>
        /// Selects the highest-priority interactor that is currently in the Hover state and transitions it to the Select state.
        /// </summary>
        /// <remarks>
        /// This method identifies the best candidate for selection, transitioning it to the Select state.
        /// It disables all other interactors to ensure that only the selected one is active.
        /// This method should only be called when <see cref="ShouldSelect"/> returns true, indicating that the conditions for selection are met.
        /// </remarks>
        public override void Select()
        {
            if (TryGetBestCandidateIndex(IsHoverAndShouldSelectPredicate,
                out int interactorIndex))
            {
                _bestInteractor = Interactors[interactorIndex];
                _bestInteractor.Select();
                _bestInteractor.WhenStateChanged += HandleBestInteractorStateChanged;
                DisableAllExcept(_bestInteractor);
            }

            State = InteractorState.Select;
        }

        /// <summary>
        /// Reverses the selection process, transitioning the currently selected interactor back to the Hover state.
        /// </summary>
        /// <remarks>
        /// This method should only be called when <see cref="ShouldUnselect"/> returns true, ensuring that the currently selected interactor meets the criteria to unselect.
        /// If the interactor unselects successfully, the group's state is updated to Hover.
        /// </remarks>
        public override void Unselect()
        {
            if (State != InteractorState.Select)
            {
                return;
            }

            if (_bestInteractor != null)
            {
                _bestInteractor.Unselect();
                if (_bestInteractor != null
                    && _bestInteractor.State == InteractorState.Select)
                {
                    return;
                }
            }

            State = InteractorState.Hover;
        }

        /// <summary>
        /// Prepares interactors for state transitions based on current conditions and processes any necessary state changes.
        /// </summary>
        public override void Preprocess()
        {
            base.Preprocess();

            if (_bestInteractor == null && State == InteractorState.Select)
            {
                this.ProcessCandidate();
                base.Process();

                if (TryHover((interactor) => interactor.Process()))
                {
                    if (ShouldSelect)
                    {
                        Select();
                        State = InteractorState.Select;
                        return;
                    }
                    State = InteractorState.Hover;
                    return;
                }

                if (State == InteractorState.Select)
                {
                    State = InteractorState.Hover;
                }
                if (State == InteractorState.Hover)
                {
                    State = InteractorState.Normal;
                }
            }
            else if (_bestInteractor != null
                && State == InteractorState.Select
                && _bestInteractor.State == InteractorState.Hover)
            {
                State = InteractorState.Hover;
            }
        }
        /// <summary>
        ///  Processes state transitions and interactions for interactors based on the current group state and individual interactor conditions.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (State == InteractorState.Hover
                && AnyInteractor(IsNormalAndShouldHoverPredicate))
            {
                if (TryHover((interactor) => interactor.Process()))
                {
                    State = InteractorState.Hover;
                }
            }

            if (State == InteractorState.Hover
               && AnyInteractor(IsHoverAndShouldUnhoverPredicate))
            {
                while (TryGetBestCandidateIndex(IsHoverAndShouldUnhoverPredicate, out int index))
                {
                    IInteractor interactor = Interactors[index];
                    interactor.Unhover();
                    if (interactor.State != InteractorState.Hover)
                    {
                        interactor.Process();
                    }
                }
            }
        }

        public override void Enable()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.Enable();
            }
            else
            {
                base.Enable();
            }
        }

        public override void Disable()
        {
            UnsuscribeBestInteractor();
            base.Disable();
        }

        private void UnsuscribeBestInteractor()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.WhenStateChanged -= HandleBestInteractorStateChanged;
                _bestInteractor = null;
            }
        }

        private void HandleBestInteractorStateChanged(InteractorStateChangeArgs stateChange)
        {
            if (stateChange.PreviousState == InteractorState.Select
                && stateChange.NewState == InteractorState.Hover)
            {
                IInteractor prevBestInteractor = _bestInteractor;
                UnsuscribeBestInteractor();
                EnableAllExcept(prevBestInteractor);
            }
        }

        public override bool HasCandidate
        {
            get
            {
                if (_bestInteractor != null && _bestInteractor.HasCandidate)
                {
                    return true;
                }
                return AnyInteractor(HasCandidatePredicate);
            }
        }
        public override bool HasInteractable
        {
            get
            {
                if (_bestInteractor != null)
                {
                    return _bestInteractor.HasInteractable;
                }
                return AnyInteractor(HasInteractablePredicate);
            }
        }
        public override bool HasSelectedInteractable
        {
            get
            {
                return _bestInteractor != null && _bestInteractor.HasSelectedInteractable;
            }
        }
        public override object CandidateProperties
        {
            get
            {
                if (_bestInteractor != null && _bestInteractor.HasCandidate)
                {
                    return _bestInteractor.CandidateProperties;
                }
                if (TryGetBestCandidateIndex(TruePredicate, out int interactorIndex))
                {
                    return Interactors[interactorIndex].CandidateProperties;
                }
                else
                {
                    return null;
                }
            }
        }

        #region Inject
        public void InjectAllInteractorGroupBestSelect(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
