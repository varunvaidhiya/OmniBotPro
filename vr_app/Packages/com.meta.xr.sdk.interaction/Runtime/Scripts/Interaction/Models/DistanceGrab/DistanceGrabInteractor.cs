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

using Oculus.Interaction.Throw;
using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// The <see cref="DistanceGrabInteractor"/> enables the grabbing of a <see cref="DistanceGrabInteractable"/> from a distance using controllers.
    /// It moves the interactable object using a configurable <see cref="IMovement"/> instance.
    /// Additionally, it employs a <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/> to determine and hover over the most suitable candidate for interaction.
    /// </summary>
    public class DistanceGrabInteractor : PointerInteractor<DistanceGrabInteractor, DistanceGrabInteractable>,
        IDistanceInteractor
    {
        /// <summary>
        /// The selection mechanism used to trigger the grab.
        /// </summary>
        [Tooltip("The selection mechanism to trigger the grab.")]
        [SerializeField, Interface(typeof(ISelector))]
        private UnityEngine.Object _selector;

        /// <summary>
        /// The location where the interactable will move when selected.
        /// </summary>
        [Tooltip("The location where the interactable will move when selected.")]
        [SerializeField, Optional]
        private Transform _grabTarget;

        [SerializeField]
        private DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable> _distantCandidateComputer
            = new DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable>();

        private IMovement _movement;

        /// <summary>
        /// The origin of the frustrums used by <cref="DistantCandidateComputer" />.
        /// This property provides the starting point for the detection frustum, which is used to identify potential interactables.
        /// </summary>
        public Pose Origin => _distantCandidateComputer.Origin;

        /// <summary>
        /// The hitpoint of your controller's frustrum.
        /// This value represents the exact point where the frustum intersects with an interactable, providing a precise location for interaction.
        /// </summary>
        public Vector3 HitPoint { get; private set; }

        /// <summary>
        /// A reference to the main <see cref="Transform"/> of the currently selected <see cref="DistanceGrabInteractable"/>.
        /// This property links back to the interactable object that is currently being manipulated by the user, allowing for continued interaction.
        /// </summary>
        public IRelativeToRef DistanceInteractable => this.Interactable;

        protected override void Awake()
        {
            base.Awake();
            if (Selector == null)
            {
                Selector = _selector as ISelector;
            }
            _nativeId = 0x4469737447726162;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Selector, nameof(Selector));
            this.AssertField(_distantCandidateComputer, nameof(_distantCandidateComputer));
            this.EndStart(ref _started);
        }

        /// <summary>
        /// Determines the best <see cref="DistanceGrabInteractable"/>
        /// candidate for selection based on the user's input and the current position of the interactor.
        /// </summary>
        /// <returns>
        /// The best <see cref="DistanceGrabInteractable"/> candidate, or null if no suitable candidate is found.
        /// </returns>
        /// <remarks>
        /// This method leverages the <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/>
        /// to analyze the available interactables and select the one that best aligns with the user's input and the interactor's position.
        /// It updates the <see cref="HitPoint"/> property with the exact point of interaction for the selected candidate.
        /// </remarks>
        protected override DistanceGrabInteractable ComputeCandidate()
        {
            DistanceGrabInteractable bestCandidate = _distantCandidateComputer.ComputeCandidate(
                DistanceGrabInteractable.Registry, this, out Vector3 hitPoint);
            HitPoint = hitPoint;
            return bestCandidate;
        }

        /// <summary> Handles the logic required when a <see cref="DistanceGrabInteractable"/> is selected by the interactor.
        /// </summary>
        /// <param name="interactable">The <see cref="DistanceGrabInteractable"/> that has been selected.
        /// </param>
        /// <remarks>
        /// This method initiates the movement sequence for the selected interactable using the configured movement provider.
        /// It also subscribes to pointer events from the interactable to manage its state during interaction.
        /// </remarks>
        protected override void InteractableSelected(DistanceGrabInteractable interactable)
        {
            _movement = interactable.GenerateMovement(GetTargetPose());
            base.InteractableSelected(interactable);
            interactable.WhenPointerEventRaised += HandleOtherPointerEventRaised;
        }
        /// <summary> Handles the logic required when a <see cref="DistanceGrabInteractable"/> is unselected by the interactor.
        /// </summary>
        /// <param name="interactable">The <see cref="DistanceGrabInteractable"/> that has been unselected.</param>
        /// <remarks>
        /// This method finalizes the interaction by unsubscribing from the interactable's pointer events and resetting the movement state.
        /// If a velocity calculator is available, it applies the calculated velocities to the interactable, simulating a throw or release action.
        /// </remarks>
        protected override void InteractableUnselected(DistanceGrabInteractable interactable)
        {
            interactable.WhenPointerEventRaised -= HandleOtherPointerEventRaised;
            base.InteractableUnselected(interactable);
            _movement = null;
        }

        private void HandleOtherPointerEventRaised(PointerEvent evt)
        {
            if (SelectedInteractable == null)
            {
                return;
            }

            if (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect)
            {
                Pose toPose = GetTargetPose();
                if (SelectedInteractable.ResetGrabOnGrabsUpdated)
                {
                    _movement = SelectedInteractable.GenerateMovement(toPose);
                    SelectedInteractable.PointableElement.ProcessPointerEvent(
                        new PointerEvent(Identifier, PointerEventType.Move, _movement.Pose, Data));
                }
            }

            if (evt.Identifier == Identifier && evt.Type == PointerEventType.Cancel)
            {
                SelectedInteractable.WhenPointerEventRaised -= HandleOtherPointerEventRaised;
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (_movement != null)
            {
                return _movement.Pose;
            }
            return GetTargetPose();
        }
        /// <summary>
        /// Updates the interactable's state while it is selected, ensuring it follows the target position.
        /// </summary>
        /// <remarks>This method is called during each frame while an interactable is selected,
        /// updating its position and rotation to match the target's current pose.
        /// It utilizes the movement provider to smoothly transition the interactable towards the target position.
        /// </remarks>
        protected override void DoSelectUpdate()
        {
            DistanceGrabInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                return;
            }

            _movement.UpdateTarget(GetTargetPose());
            _movement.Tick();
        }

        private Pose GetTargetPose()
        {
            if (_grabTarget != null)
            {
                return _grabTarget.GetPose();
            }
            return Origin;
        }

        #region Inject
        /// <summary>
        /// Adds a <see cref="DistanceGrabInteractor"/> to a dynamically instantiated GameObject.
        /// This method sets up the necessary components for a <see cref="DistanceGrabInteractor"/>, including the selector and candidate computer.
        /// </summary>
        public void InjectAllDistanceGrabInteractor(ISelector selector,
            DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable> distantCandidateComputer)
        {
            InjectSelector(selector);
            InjectDistantCandidateComputer(distantCandidateComputer);
        }

        /// <summary>
        /// Adds an <see cref="ISelector"/> to a dynamically instantiated GameObject.
        ///  This method injects the selector, which is responsible for detecting the user's input to initiate the grab action.
        /// </summary>
        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        /// <summary>
        /// Adds a <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/> to a dynamically instantiated GameObject.
        /// This method injects the candidate computer, which is used to determine the best interactable to hover based on the user's input.
        /// </summary>
        public void InjectDistantCandidateComputer(DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable> distantCandidateComputer)
        {
            _distantCandidateComputer = distantCandidateComputer;
        }

        /// <summary>
        /// Adds a grab target to a dynamically instantiated GameObject.
        /// </summary>
        public void InjectOptionalGrabTarget(Transform grabTarget)
        {
            _grabTarget = grabTarget;
        }

        #endregion
    }
}
