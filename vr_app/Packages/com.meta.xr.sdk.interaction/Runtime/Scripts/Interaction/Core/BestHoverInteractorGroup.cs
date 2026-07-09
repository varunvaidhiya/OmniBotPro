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
    /// Manages a group of interactors where only the highest-priority interactor is active in the hover state.
    /// This class ensures that interactors with lower priority remain inactive until the higher-priority interactor
    /// ceases to hover or is superseded by an interactor with even higher priority.
    /// </summary>
    /// <remarks>
    /// In typical setups, you will add this class to manage hand or controller interactors. The group typically includes interactors such as the <see cref="PokeInteractor"/> and <see cref="GrabInteractor"/>.
    /// By default, the priority of interactors is determined by their index in the <see cref="InteractorGroup.Interactors"/> collection,
    /// a lower index corresponds to a higher priority. However, if a custom <see cref="CandidateComparer{T}"/> is provided, it may override this default behavior based on custom comparison logic.
    /// </remarks>
    public class BestHoverInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;
        private int _bestInteractorIndex = -1;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;
        /// <summary>
        /// Determines whether the highest-priority interactor should be in the hover state.
        /// </summary>
        /// <value>
        /// Returns true if the current state is normal and any interactor meets the hover criteria; otherwise, false.
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
        /// Determines whether the highest-priority interactor should cease to hover.
        /// </summary>
        /// <value>
        /// Returns true if the current state is hover and the highest-priority interactor should unhover; otherwise, false.
        /// </value>
        public override bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _bestInteractor != null
                        && _bestInteractor.ShouldUnhover;
            }
        }
        /// <summary>
        /// Determines whether the highest-priority interactor should transition to the select state.
        /// </summary>
        /// <value>
        /// True if the current state is hover and the highest-priority interactor meets the criteria for selection; otherwise, false.
        /// </value>
        /// <remarks>
        /// This property evaluates whether conditions are met for the highest-priority interactor to initiate the selection process.
        /// It checks if the current state is <see cref="InteractorState.Hover"/> and if the highest-priority interactor is ready to be selected.
        /// </remarks>
        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _bestInteractor != null
                    && _bestInteractor.ShouldSelect;
            }
        }
        /// <summary>
        /// Determines whether the highest-priority interactor should cease being in the select state.
        /// </summary>
        /// <value>
        /// True if the current state is select and the highest-priority interactor meets the criteria for unselection; otherwise, false.
        /// </value>
        /// <remarks>
        /// This property checks if the current state is <see cref="InteractorState.Select"/> and evaluates whether the highest-priority interactor should transition out of the select state.
        /// It is typically used to determine if conditions for selection are no longer met, prompting an unselection process.
        /// </remarks>
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
        /// Transitions the highest-priority interactor into the hover state if conditions are met.
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

        private bool TryHover(int betterThan = -1)
        {
            if (TryGetBestCandidateIndex(IsNormalAndShouldHoverPredicate,
                out int interactorIndex, betterThan, betterThan))
            {
                if (_bestInteractor != null)
                {
                    _bestInteractor.Unhover();
                }
                if (_bestInteractor == null
                    || CompareCandidates(_bestInteractorIndex, interactorIndex) > 0)
                {
                    HoverAtIndex(interactorIndex);
                    return true;
                }
            }
            return false;
        }

        private bool TryReplaceHover()
        {
            EnableAllExcept(_bestInteractor);
            this.ProcessCandidate();

            if (TryHover(_bestInteractorIndex))
            {
                return true;
            }

            DisableAllExcept(_bestInteractor);
            return false;
        }

        private void HoverAtIndex(int interactorIndex)
        {
            UnsuscribeBestInteractor();
            _bestInteractorIndex = interactorIndex;
            _bestInteractor = Interactors[_bestInteractorIndex];
            _bestInteractor.Hover();
            _bestInteractor.WhenStateChanged += HandleBestInteractorStateChanged;
            DisableAllExcept(_bestInteractor);
        }
        /// <summary>
        /// Ceases the hover state of the highest-priority interactor.
        /// </summary>
        /// <remarks>
        /// This method transitions the highest-priority interactor out of the hover state, when an interactor no longer meets the hover criteria.
        /// </remarks>
        public override void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            if (_bestInteractor != null)
            {
                _bestInteractor.Unhover();
                if (_bestInteractor != null
                    && _bestInteractor.State == InteractorState.Hover)
                {
                    return;
                }
            }
            State = InteractorState.Normal;
        }
        /// <summary>
        /// Initiates the selection process for the highest-priority interactor.
        /// </summary>
        /// <remarks>
        /// This method transitions the highest-priority interactor from the hover to the select state.
        /// </remarks>
        public override void Select()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            _bestInteractor.Select();

            State = InteractorState.Select;
        }
        /// <summary>
        /// Reverses the selection process for the highest-priority interactor.
        /// </summary>
        /// <remarks>
        /// This method unselects the highest-priority interactor if it no longer meets the selection criteria.
        /// If the unselection is successful, the group's state is updated to Hover.
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

            if (_bestInteractor == null
                && (State == InteractorState.Hover || State == InteractorState.Select))
            {
                this.ProcessCandidate();
                base.Process();
                if (TryHover())
                {
                    if (State == InteractorState.Select)
                    {
                        _bestInteractor.Process();
                        if (ShouldSelect)
                        {
                            Select();
                            State = InteractorState.Select;
                            return;
                        }
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
        /// Processes state transitions and interactions for interactors based on the current group state and individual interactor conditions.
        /// </summary>
        public override void Process()
        {
            base.Process();

            if (_bestInteractor != null
               && State == InteractorState.Hover)
            {
                if (TryReplaceHover())
                {
                    _bestInteractor.Process();
                }
            }
        }

        private void HandleBestInteractorStateChanged(InteractorStateChangeArgs stateChange)
        {
            if (stateChange.PreviousState == InteractorState.Hover
                && stateChange.NewState == InteractorState.Normal)
            {
                IInteractor prevBest = _bestInteractor;
                UnsuscribeBestInteractor();
                EnableAllExcept(prevBest);
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
                _bestInteractorIndex = -1;
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
                return _bestInteractor != null && _bestInteractor.HasInteractable;
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
        public void InjectAllInteractorGroupBestHover(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
