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
using UnityEngine;
using UnityEngine.Assertions;
using Oculus.Interaction.Throw;
using System;
using Oculus.Interaction.Grab;

namespace Oculus.Interaction
{
    /// <summary>
    /// The <see cref="GrabInteractor"/> class facilitates interaction with objects that have a <see cref="GrabInteractable"/> component.
    /// It supports interaction via both controllers and hands, though the HandGrabInteractor is recommended for hand interactions.
    /// This class provides advanced control over grab interactions, including forced selection and release, and dynamic component injection.
    /// </summary>
    public class GrabInteractor : PointerInteractor<GrabInteractor, GrabInteractable>, IRigidbodyRef
    {
        /// <summary>
        /// The selection mechanism that broadcasts select and release events. For example, a <see cref="ControllerSelector" />.
        /// </summary>
        [Tooltip("The selection mechanism that broadcasts select and release events. For example, a ControllerSelector.")]
        [SerializeField, Interface(typeof(ISelector))]
        private UnityEngine.Object _selector;

        /// <summary>
        /// The hand or controller's Rigidbody, which detects interactables.
        /// </summary>
        [Tooltip("The hand or controller's Rigidbody, which detects interactables.")]
        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// The location where the interactable will move when selected.
        /// </summary>
        [Tooltip("The location where the interactable will move when selected.")]
        [SerializeField, Optional]
        private Transform _grabTarget;

        private Collider[] _colliders;
        private Tween _tween;
        private bool _outsideReleaseDist = false;

        private GrabInteractable _selectedInteractableOverride;
        private bool _isSelectionOverriden = false;

        protected override void Awake()
        {
            base.Awake();
            if (Selector == null)
            {
                Selector = _selector as ISelector;
            }
            _nativeId = 0x4772616249746f72;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Selector, nameof(Selector));
            this.AssertField(Rigidbody, nameof(Rigidbody));

            _colliders = Rigidbody.GetComponentsInChildren<Collider>();

            this.AssertCollectionField(_colliders, nameof(_colliders),
               $"The associated {AssertUtils.Nicify(nameof(Rigidbody))} must have at least one Collider.");

            foreach (Collider collider in _colliders)
            {
                this.AssertIsTrue(collider.isTrigger,
                    $"Associated Colliders in the {AssertUtils.Nicify(nameof(Rigidbody))} must be marked as Triggers.");
            }

            if (_grabTarget == null)
            {
                _grabTarget = Rigidbody.transform;
            }

            _tween = new Tween(Pose.identity);

            this.EndStart(ref _started);
        }

        protected override GrabInteractable ComputeCandidate()
        {
            Vector3 position = Rigidbody.transform.position;
            GrabInteractable closestInteractable = null;
            GrabPoseScore bestScore = GrabPoseScore.Max;

            var interactables = GrabInteractable.Registry.List(this);
            foreach (GrabInteractable interactable in interactables)
            {
                Collider[] colliders = interactable.Colliders;
                GrabPoseScore score = GrabPoseHelper.CollidersScore(position, interactable.Colliders, out Vector3 hit);
                if (score.IsBetterThan(bestScore))
                {
                    bestScore = score;
                    closestInteractable = interactable;
                }
            }

            return closestInteractable;
        }

        /// <summary>
        /// Forces the controller to select a specific <see cref="GrabInteractable"/> even if it is not the closest available option.
        /// This method overrides the default selection process, allowing for programmatic control over which interactable is selected.
        /// </summary>
        public void ForceSelect(GrabInteractable interactable)
        {
            _isSelectionOverriden = true;
            _selectedInteractableOverride = interactable;
            SetComputeCandidateOverride(() => interactable);
            SetComputeShouldSelectOverride(() => ReferenceEquals(interactable, Interactable));
            SetComputeShouldUnselectOverride(() => !ReferenceEquals(interactable, SelectedInteractable), false);
        }

        /// <summary>
        /// Forces the controller to unselect the currently selected <see cref="GrabInteractable"/>.
        /// This method clears any overrides set by <see cref="ForceSelect"/> and reverts to the default selection behavior.
        /// </summary>
        public void ForceRelease()
        {
            _isSelectionOverriden = false;
            _selectedInteractableOverride = null;
            ClearComputeCandidateOverride();
            ClearComputeShouldSelectOverride();
            if (State == InteractorState.Select)
            {
                SetComputeShouldUnselectOverride(() => true);
            }
            else
            {
                ClearComputeShouldUnselectOverride();
            }
        }

        /// <summary>
        /// This method unselects the currently selected interactable, and in addition reverts any selection overrides if necessary.
        /// </summary>
        public override void Unselect()
        {
            if (State == InteractorState.Select
                && _isSelectionOverriden
                && (SelectedInteractable == _selectedInteractableOverride
                    || SelectedInteractable == null))
            {
                _isSelectionOverriden = false;
                _selectedInteractableOverride = null;
                ClearComputeShouldUnselectOverride();
            }
            base.Unselect();
        }

        protected override void InteractableSelected(GrabInteractable interactable)
        {
            Pose target = _grabTarget.GetPose();
            Pose source = _interactable.GetGrabSourceForTarget(target);

            _tween.StopAndSetPose(source);
            base.InteractableSelected(interactable);

            _tween.MoveTo(target);
        }

        protected override void HandlePointerEventRaised(PointerEvent evt)
        {
            base.HandlePointerEventRaised(evt);

            if (SelectedInteractable == null)
            {
                return;
            }

            if (evt.Type == PointerEventType.Select ||
                evt.Type == PointerEventType.Unselect ||
                evt.Type == PointerEventType.Cancel)
            {
                Pose target = _grabTarget.GetPose();
                if (SelectedInteractable.ResetGrabOnGrabsUpdated)
                {
                    Pose source = _interactable.GetGrabSourceForTarget(target);
                    _tween.StopAndSetPose(source);
                    SelectedInteractable.PointableElement.ProcessPointerEvent(
                        new PointerEvent(Identifier, PointerEventType.Move, _tween.Pose, Data));
                    _tween.MoveTo(target);
                }
                else
                {
                    _tween.StopAndSetPose(target);
                    SelectedInteractable.PointableElement.ProcessPointerEvent(
                        new PointerEvent(Identifier, PointerEventType.Move, target, Data));
                    _tween.MoveTo(target);
                }
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (SelectedInteractable != null)
            {
                return _tween.Pose;
            }
            return _grabTarget.GetPose();
        }

        protected override void DoSelectUpdate()
        {
            GrabInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                return;
            }

            _tween.UpdateTarget(_grabTarget.GetPose());
            _tween.Tick();

            _outsideReleaseDist = false;
            if (interactable.ReleaseDistance > 0.0f)
            {
                float closestSqrDist = float.MaxValue;
                Collider[] colliders = interactable.Colliders;
                foreach (Collider collider in colliders)
                {
                    float sqrDistanceFromCenter =
                        (collider.bounds.center - Rigidbody.transform.position).sqrMagnitude;
                    closestSqrDist = Mathf.Min(closestSqrDist, sqrDistanceFromCenter);
                }

                float sqrReleaseDistance = interactable.ReleaseDistance * interactable.ReleaseDistance;

                if (closestSqrDist > sqrReleaseDistance)
                {
                    _outsideReleaseDist = true;
                }
            }
        }

        protected override bool ComputeShouldUnselect()
        {
            return _outsideReleaseDist || base.ComputeShouldUnselect();
        }

        #region Inject

        /// <summary>
        /// Injects both a <see cref="ISelector"/> and a Rigidbody into a dynamically instantiated GameObject.
        /// </summary>
        /// <param name="selector">The selector component to be added.</param>
        /// <param name="rigidbody">The Rigidbody component to be added.</param>
        public void InjectAllGrabInteractor(ISelector selector, Rigidbody rigidbody)
        {
            InjectSelector(selector);
            InjectRigidbody(rigidbody);
        }

        /// <summary>
        /// Adds a <see cref="ISelector"/> to a dynamically instantiated GameObject.
        /// </summary>
        /// <param name="selector">The external selector component to be added.</param>
        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        /// <summary>
        /// Adds a Rigidbody to a dynamically instantiated GameObject,
        /// enhancing the physical interaction capabilities by providing an external Rigidbody component.
        /// </summary>
        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Optionally adds a grab target to a dynamically instantiated GameObject,
        /// enabling targeted grabbing operations by providing an external grab target transform.
        /// </summary>
        public void InjectOptionalGrabTarget(Transform grabTarget)
        {
            _grabTarget = grabTarget;
        }

        #endregion
    }
}
