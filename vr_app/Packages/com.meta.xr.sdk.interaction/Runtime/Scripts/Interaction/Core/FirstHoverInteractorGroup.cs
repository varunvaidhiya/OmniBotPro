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
    /// Manages a group of interactors where only the first interactor requesting hover is allowed to enter the hover state.
    /// This class ensures that all other interactors remain disabled for hover until the first interactor ceases to hover.
    /// </summary>
    /// <remarks>
    /// This class is typically used in scenarios where exclusive access by the first interactor is required to prevent conflicts.
    /// The priority is determined by the order of interaction requests; the first interactor to request hover gains priority.
    /// The group typically includes interactors such as the <see cref="PokeInteractor"/> and <see cref="GrabInteractor"/>.
    /// </remarks>
    public class FirstHoverInteractorGroup : InteractorGroup
    {
        private IInteractor _bestInteractor = null;
        private int _bestInteractorIndex = -1;

        private static readonly InteractorPredicate IsNormalAndShouldHoverPredicate =
            (interactor, index) => interactor.State == InteractorState.Normal && interactor.ShouldHover;

        /// <summary>
        /// Determines whether any interactor in the group should be in the hover state.
        /// </summary>
        /// <value>True if the state is <see cref="InteractorState.Normal"/> and any interactor meets the hover criteria; otherwise, false.</value>
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
        /// Determines whether the currently hovering interactor should exit the <see cref="InteractorState.Hover"/> state based on its current conditions.
        /// This method checks if the state is hover and if the best interactor should cease hovering, ensuring proper state transitions.
        /// </summary>
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
        /// Determines whether the currently hovering interactor should transition to the select state based on its conditions.
        /// This method checks if the state is hover and if the best interactor meets the criteria to be selected, managing interaction flow.
        /// </summary>
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
        /// Determines whether the currently selected interactor should exit the <see cref="InteractorState.Select"/> state based on its current conditions.
        /// This method checks if the state is select and if the best interactor should cease being selected, ensuring correct <see cref="InteractorState"/> management.
        /// </summary>
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
        /// Attempts to transition the first eligible interactor into the <see cref="InteractorState.Hover"/> state if conditions are met.
        /// This method triggers the hover action and updates the state to hover if the attempt is successful, facilitating interaction initiation.
        /// </summary>
        public override void Hover()
        {
            if (TryHover())
            {
                State = InteractorState.Hover;
            }
        }

        private bool TryHover(int skipIndex = -1)
        {
            if (TryGetBestCandidateIndex(IsNormalAndShouldHoverPredicate,
                out int interactorIndex, -1, skipIndex))
            {
                HoverAtIndex(interactorIndex);
                return true;
            }
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
                this.ProcessCandidate();
                TryHover(_bestInteractorIndex);
            }
            if (_bestInteractor == null)
            {
                State = InteractorState.Normal;
            }
        }

        public override void Select()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }

            _bestInteractor.Select();

            State = InteractorState.Select;
        }

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
        /// Processes the interactor group before the main interaction logic.
        /// This method ensures that the best candidate for interaction is processed if no interactor is currently selected or hovering.
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

        /// <summary>
        /// Enables the interactor group, allowing interactions to be processed, which is essential for dynamic interaction environments.
        /// If a best interactor is already determined, it will be enabled; otherwise, the base enable logic is applied.
        /// </summary>
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

        /// <summary>
        /// Disables the interactor group, preventing any further interactions and cleaning up any state related to the best interactor.
        /// This method is crucial for resetting the interactor group and avoiding unintended interactions.
        /// </summary>
        public override void Disable()
        {
            UnsuscribeBestInteractor();
            base.Disable();
        }

        /// <summary>
        /// Unsubscribes the current best interactor from state change events and resets the best interactor tracking variables.
        /// This method is vital for maintaining the integrity and accuracy of the <see cref="InteractorState"/> management.
        /// </summary>
        private void UnsuscribeBestInteractor()
        {
            if (_bestInteractor != null)
            {
                _bestInteractor.WhenStateChanged -= HandleBestInteractorStateChanged;
                _bestInteractor = null;
                _bestInteractorIndex = -1;
            }
        }

        /// <summary>
        /// Determines if there is a candidate interactor that could potentially become the best interactor.
        /// This property is essential for identifying potential interactors that can take over interaction responsibilities.
        /// </summary>
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

        /// <summary>
        /// Checks if there is an interactable object associated with the best interactor.
        /// This property is crucial for determining if the current best interactor is interacting with an object.
        /// </summary>
        public override bool HasInteractable
        {
            get
            {
                return _bestInteractor != null && _bestInteractor.HasInteractable;
            }
        }

        /// <summary>
        /// Checks if there is a selected interactable object associated with the best interactor.
        /// This property is important for understanding if the current best interactor has successfully selected an object.
        /// </summary>
        public override bool HasSelectedInteractable
        {
            get
            {
                return _bestInteractor != null && _bestInteractor.HasSelectedInteractable;
            }
        }

        /// <summary>
        /// Retrieves properties of the candidate interactor, if available, which is crucial for further processing or decision-making.
        /// This property provides detailed information about the candidate, aiding in the selection process.
        /// </summary>
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
        public void InjectAllInteractorGroupFirstHover(List<IInteractor> interactors)
        {
            base.InjectAllInteractorGroupBase(interactors);
        }
        #endregion
    }
}
