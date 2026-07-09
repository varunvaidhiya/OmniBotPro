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

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using Oculus.Interaction.Throw;
using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Enables hand grab for objects within arm's reach that have a <see cref="GrabInteractable"/>.
    /// When grabbing an object, the hands visually snap to any <see cref="HandPose"/> you've provided.
    /// To enable grab for controllers, use <see cref="GrabInteractor"/>.
    /// </summary>
    public class HandGrabInteractor : PointerInteractor<HandGrabInteractor, HandGrabInteractable>,
        IHandGrabInteractor, IRigidbodyRef
    {
        /// <summary>
        /// The <see cref="IHand"> that should be able to grab.
        /// </summary>
        [Tooltip("The IHand that should be able to grab.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.Hand"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public IHand Hand { get; private set; }

        /// <summary>
        /// The hand's Rigidbody, which detects interactables.
        /// </summary>
        [Tooltip("The hand's Rigidbody, which detects interactables.")]
        [SerializeField]
        private Rigidbody _rigidbody;

        /// <summary>
        /// Detects when the hand grab selects or unselects.
        /// </summary>
        [Tooltip("Detects when the hand grab selects or unselects.")]
        [SerializeField]
        private HandGrabAPI _handGrabApi;

        /// <summary>
        /// The grab types that the hand supports.
        /// </summary>
        [Tooltip("The grab types that the hand supports.")]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;

        /// <summary>
        /// When enabled, nearby interactables can become candidates even if the finger
        /// strength is 0.
        /// </summary>
        [SerializeField]
        [Tooltip("When enabled, nearby interactables can become candidates even if the" +
            "finger strength is 0")]
        private bool _hoverOnZeroStrength = false;

        /// <summary>
        /// When enabled, nearby <see cref="HandGrabInteractable"/>s can become candidates even if the finger
        /// strength  is 0. "Strength" in this context is a measure of of how similar/different the finger's state is to what the
        /// system considers "grabbing"; for a more detailed overview, see the documentation for
        /// <see cref="IHand.GetFingerPinchStrength(HandFinger)"/>.
        /// </summary>
        public bool HoverOnZeroStrength
        {
            get
            {
                return _hoverOnZeroStrength;
            }
            set
            {
                _hoverOnZeroStrength = value;
            }
        }

        /// <summary>
        /// The origin of the grab.
        /// </summary>
        [Tooltip("The origin of the grab.")]
        [SerializeField]
        private Transform _grabOrigin;

        /// <summary>
        /// Specifies an offset from the wrist that can be used to search for the best HandGrabInteractable available,
        /// act as a palm grab without a HandPose, and also act as an anchor for attaching the object.
        /// </summary>
        [Tooltip("Specifies an offset from the wrist that can be used to search for the best "
        + "HandGrabInteractable available, act as a palm grab without a HandPose, and " +
        "also act as an anchor for attaching the object.")]
        [SerializeField, Optional]
        private Transform _gripPoint;

        /// <summary>
        /// Collider used to detect a palm grab.
        /// </summary>
        [Tooltip("Collider used to detect a palm grab.")]
        [SerializeField, Optional]
        private Collider _gripCollider;

        /// <summary>
        /// Specifies a moving point at the center of the tips of the currently pinching fingers.
        /// It's used to align interactables that don’t have a HandPose to the center of the pinch.
        /// </summary>
        [Tooltip("Specifies a moving point at the center of the tips of the currently pinching fingers. It's used to align interactables that don’t have a HandPose to the center of the pinch.")]
        [SerializeField, Optional]
        private Transform _pinchPoint;

        /// <summary>
        /// Collider used to detect a pinch grab.
        /// </summary>
        [Tooltip("Collider used to detect a pinch grab.")]
        [SerializeField, Optional]
        private Collider _pinchCollider;

        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabResult _cachedResult = new HandGrabResult();
        private HandGrabInteractable _selectedInteractableOverride;
        private GrabTypeFlags _currentGrabType = GrabTypeFlags.None;

        #region IHandGrabInteractor
        /// <summary>
        /// The <see cref="IMovement"/> generated as a result of interacting with an interactable. This is created by the
        /// interactable's <see cref="DistanceHandGrabInteractable.MovementProvider"/>.
        /// </summary>
        public IMovement Movement { get; set; }

        /// <summary>
        /// Indicates whether or not the current <see cref="Movement"/> has finished.
        /// </summary>
        public bool MovementFinished { get; set; }

        /// <summary>
        /// The <see cref="HandGrab.HandGrabTarget"/> used by this interactor when grabbing.
        /// </summary>
        public HandGrabTarget HandGrabTarget { get; } = new HandGrabTarget();

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.WristPoint"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Transform WristPoint => _grabOrigin;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.PinchPoint"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Transform PinchPoint => _pinchPoint;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.PalmPoint"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Transform PalmPoint => _gripPoint;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.HandGrabApi"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public HandGrabAPI HandGrabApi => _handGrabApi;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.SupportedGrabTypes"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.TargetInteractable"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public IHandGrabInteractable TargetInteractable => Interactable;
        #endregion

        #region IHandGrabState
        /// <summary>
        /// Implementation of <see cref="IHandGrabState.IsGrabbing"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public virtual bool IsGrabbing => HasSelectedInteractable
            && (Movement != null && Movement.Stopped);

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.FingersStrength"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float FingersStrength { get; private set; }

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.WristStrength"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float WristStrength { get; private set; }

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.WristToGrabPoseOffset"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Pose WristToGrabPoseOffset { get; private set; }

        /// <summary>
        /// Implementation of <see cref="IHandGrabState.GrabbingFingers"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public HandFingerFlags GrabbingFingers()
        {
            return this.GrabbingFingers(SelectedInteractable);
        }
        #endregion

        #region IRigidbodyRef
        /// <summary>
        /// Implementation of <see cref="IRigidbodyRef.Rigidbody"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Rigidbody Rigidbody => _rigidbody;
        #endregion

        #region editor events
        protected virtual void Reset()
        {
            _hand = this.GetComponentInParent<IHand>() as MonoBehaviour;
            _handGrabApi = this.GetComponentInParent<HandGrabAPI>();
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            _nativeId = 0x4847726162497472;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Rigidbody, nameof(Rigidbody));
            Collider[] colliders = Rigidbody.GetComponentsInChildren<Collider>();

            this.AssertCollectionField(colliders, nameof(colliders),
                $"The associated {AssertUtils.Nicify(nameof(Rigidbody))} must have at least one Collider.");

            foreach (Collider collider in colliders)
            {
                this.AssertIsTrue(collider.isTrigger,
                    $"Associated Colliders in the {AssertUtils.Nicify(nameof(Rigidbody))} must be marked as Triggers.");
            }

            this.AssertField(_handGrabApi, nameof(_handGrabApi));
            this.AssertField(Hand, nameof(Hand));

            this.EndStart(ref _started);
        }

        #region life cycle

        /// <summary>
        /// Each call while the interactor is hovering, it checks whether there is an interaction
        /// being hovered and sets the target snap pose to it. In the HandToObject snapping
        /// behaviors this is relevant as the hand can approach the object progressively even before
        /// a true grab starts.
        /// </summary>
        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            _handGrabShouldSelect = false;

            if (Interactable == null)
            {
                return;
            }

            UpdateTarget(Interactable);

            _currentGrabType = this.ComputeShouldSelect(Interactable);
            if (_currentGrabType != GrabTypeFlags.None)
            {
                _handGrabShouldSelect = true;
            }
        }


        protected override void InteractableSet(HandGrabInteractable interactable)
        {
            base.InteractableSet(interactable);
            UpdateTarget(Interactable);
        }

        protected override void InteractableUnset(HandGrabInteractable interactable)
        {
            base.InteractableUnset(interactable);
            SetGrabStrength(0f);
        }

        /// <summary>
        /// Each call while the hand is selecting/grabbing an interactable, it moves the item to the
        /// new position while also attracting it towards the hand if the snapping mode requires it.
        ///
        /// In some cases the parameter can be null, for example if the selection was interrupted
        /// by another hand grabbing the object. In those cases it will come out of the release
        /// state once the grabbing gesture properly finishes.
        /// </summary>
        protected override void DoSelectUpdate()
        {
            _handGrabShouldUnselect = false;
            if (SelectedInteractable == null)
            {
                _handGrabShouldUnselect = true;
                return;
            }

            UpdateTargetSliding(SelectedInteractable);

            Pose handGrabPose = this.GetHandGrabPose();
            Movement.UpdateTarget(handGrabPose);
            Movement.Tick();

            GrabTypeFlags selectingGrabs = this.ComputeShouldSelect(SelectedInteractable);
            GrabTypeFlags unselectingGrabs = this.ComputeShouldUnselect(SelectedInteractable);
            _currentGrabType |= selectingGrabs;
            _currentGrabType &= ~unselectingGrabs;

            if (unselectingGrabs != GrabTypeFlags.None
                && _currentGrabType == GrabTypeFlags.None)
            {
                _handGrabShouldUnselect = true;
            }
        }

        /// <summary>
        /// When a new interactable is selected, start the grab at the ideal point. When snapping is
        /// involved that can be a point in the interactable offset from the hand
        /// which will be stored to progressively reduced it in the next updates,
        /// effectively attracting the object towards the hand.
        /// When no snapping is involved the point will be the grip point of the hand directly.
        /// Note: ideally this code would be in InteractableSelected but it needs
        /// to be called before the object is marked as active.
        /// </summary>
        /// <param name="interactable">The selected interactable</param>
        protected override void InteractableSelected(HandGrabInteractable interactable)
        {
            if (interactable != null)
            {
                WristToGrabPoseOffset = this.GetGrabOffset();
                this.Movement = this.GenerateMovement(interactable);
                SetGrabStrength(1f);
            }

            base.InteractableSelected(interactable);
        }

        /// <summary>
        /// When releasing an active interactable, calculate the releasing point in similar
        /// fashion to  InteractableSelected.
        /// </summary>
        /// <param name="interactable">The released interactable</param>
        protected override void InteractableUnselected(HandGrabInteractable interactable)
        {
            base.InteractableUnselected(interactable);

            this.Movement = null;
            _currentGrabType = GrabTypeFlags.None;
        }

        protected override void HandlePointerEventRaised(PointerEvent evt)
        {
            base.HandlePointerEventRaised(evt);

            if (SelectedInteractable == null
                || !SelectedInteractable.ResetGrabOnGrabsUpdated)
            {
                return;
            }

            if (evt.Identifier != Identifier &&
                (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect))
            {
                WristToGrabPoseOffset = this.GetGrabOffset();
                SetTarget(SelectedInteractable, _currentGrabType);
                this.Movement = this.GenerateMovement(SelectedInteractable);

                Pose fromPose = this.GetTargetGrabPose();
                PointerEvent pe = new PointerEvent(Identifier, PointerEventType.Move, fromPose, Data);
                SelectedInteractable.PointableElement.ProcessPointerEvent(pe);
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (Movement != null)
            {
                return Movement.Pose;
            }
            return this.GetHandGrabPose();
        }

        #endregion

        protected override bool ComputeShouldSelect()
        {
            return _handGrabShouldSelect;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _handGrabShouldUnselect
                || (_selectedInteractableOverride != null && _selectedInteractableOverride != SelectedInteractable);
        }

        /// <summary>
        /// Overrides <see cref="Interactor{TInteractor, TInteractable}.CanSelect(TInteractable)"/>, augmenting the behavior of that
        /// base method with an additional call to
        /// <see cref="HandGrabInteraction.CanInteractWith(IHandGrabInteractor, IHandGrabInteractable)"/>, which confirms the
        /// presence of hand-specific requirements for valid hand interaction.
        /// </summary>
        /// <param name="interactable">The interactable</param>
        /// <returns>True if it is possible for this interactable to select <paramref name="interactable"/>, false otherwise</returns>
        public override bool CanSelect(HandGrabInteractable interactable)
        {
            if (!base.CanSelect(interactable))
            {
                return false;
            }
            return this.CanInteractWith(interactable);
        }

        /// <summary>
        /// Compute the best interactable to snap to. In order to do it the method measures
        /// the score from the current grip pose to the closes pose in the surfaces
        /// of each one of the interactables in the registry.
        /// Even though it returns the best interactable, it also saves the entire Snap pose to
        /// it in which the exact pose within the surface is already recorded to avoid recalculations
        /// within the same frame.
        /// </summary>
        /// <returns>The best interactable to snap the hand to.</returns>
        protected override HandGrabInteractable ComputeCandidate()
        {
            var interactables = HandGrabInteractable.Registry.List(this);
            float bestFingerScore = float.NegativeInfinity;
            GrabPoseScore bestPoseScore = GrabPoseScore.Max;
            HandGrabInteractable bestInteractable = null;

            foreach (HandGrabInteractable interactable in interactables)
            {
                GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable, bestFingerScore,
                    out float fingerScore);

                if (selectingGrabTypes == GrabTypeFlags.None)
                {
                    continue;
                }

                GrabPoseScore poseScore = this.GetPoseScore(interactable, selectingGrabTypes, ref _cachedResult);
                if (fingerScore > bestFingerScore
                    || poseScore.IsBetterThan(bestPoseScore))
                {
                    bestFingerScore = fingerScore;
                    bestPoseScore = poseScore;
                    bestInteractable = interactable;
                }
            }

            return bestInteractable;
        }

        private GrabTypeFlags SelectingGrabTypes(HandGrabInteractable interactable,
            float minFingerScoreRequired, out float fingerScore)
        {
            fingerScore = 1.0f;
            GrabTypeFlags selectingGrabTypes;
            if (State == InteractorState.Select
                || (selectingGrabTypes = this.ComputeShouldSelect(interactable)) == GrabTypeFlags.None)
            {
                fingerScore = HandGrabInteraction.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
            }
            if (fingerScore < minFingerScoreRequired)
            {
                return GrabTypeFlags.None;
            }

            if (selectingGrabTypes == GrabTypeFlags.None)
            {
                if (_hoverOnZeroStrength)
                {
                    selectingGrabTypes = interactable.SupportedGrabTypes & this.SupportedGrabTypes;
                }
                else
                {
                    return GrabTypeFlags.None;
                }
            }

            if (_gripCollider != null
                && (selectingGrabTypes & GrabTypeFlags.Palm) != 0
                && !OverlapsVolume(interactable, _gripCollider))
            {
                selectingGrabTypes &= ~GrabTypeFlags.Palm;
            }

            if (_pinchCollider != null
                && (selectingGrabTypes & GrabTypeFlags.Pinch) != 0
                && !OverlapsVolume(interactable, _pinchCollider))
            {
                selectingGrabTypes &= ~GrabTypeFlags.Pinch;
            }

            return selectingGrabTypes;
        }

        /// <summary>
        /// Forces the interactor to select the passed <see cref="HandGrabInteractable"/>, which will be grabbed in the next
        /// interaction iteration.
        /// </summary>
        /// <param name="interactable">The interactable to grab.</param>
        /// <param name="allowManualRelease">If false, the interactable can only be released by calling ForceRelease.</param>
        public void ForceSelect(HandGrabInteractable interactable, bool allowManualRelease = false)
        {
            _selectedInteractableOverride = interactable;
            SetComputeCandidateOverride(() => interactable);
            SetComputeShouldSelectOverride(() => ReferenceEquals(interactable, Interactable));
            if (!allowManualRelease)
            {
                SetComputeShouldUnselectOverride(() => !ReferenceEquals(interactable, SelectedInteractable), false);
            }
        }
        /// <summary>
        /// Forces the interactor to deselect the currently grabbed <see cref="HandGrabInteractable"/> (if any).
        /// </summary>
        public void ForceRelease()
        {
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
        /// Overrides the interactor's ComputeCandidate() method with a new method. This method itself is an override
        /// <see cref="Interactor{TInteractor, TInteractable}.SetComputeCandidateOverride(Func{TInteractable}, bool)"/>, encapsulating the
        /// base behavior.
        /// <param name="computeCandidate">The method used instead of the interactable's existing ComputeCandidate() method.</param>
        /// <param name="shouldClearOverrideOnSelect">If true, clear the computeCandidate function once you select an interactable.</param>
        /// </summary>
        public override void SetComputeCandidateOverride(Func<HandGrabInteractable> computeCandidate, bool shouldClearOverrideOnSelect = true)
        {
            base.SetComputeCandidateOverride(() =>
            {
                HandGrabInteractable interactable = computeCandidate.Invoke();
                return interactable;
            },
            shouldClearOverrideOnSelect);
        }

        /// <summary>
        /// Overrides <see cref="Interactor{TInteractor, TInteractable}.Unselect"/>, augmenting the base behavior to ensure correct handling
        /// of overrides set with <see cref="Interactor{TInteractor, TInteractable}.SetComputeShouldUnselectOverride(Func{bool}, bool)"/>.
        /// </summary>
        public override void Unselect()
        {
            if (State == InteractorState.Select
                && _selectedInteractableOverride != null
                && (SelectedInteractable == _selectedInteractableOverride
                    || SelectedInteractable == null))
            {
                _selectedInteractableOverride = null;
                ClearComputeShouldUnselectOverride();
            }
            base.Unselect();
        }

        private bool OverlapsVolume(HandGrabInteractable interactable, Collider volume)
        {
            foreach (Collider collider in interactable.Colliders)
            {
                if (collider.enabled &&
                    Physics.ComputePenetration(
                        volume, volume.transform.position, volume.transform.rotation,
                        collider, collider.transform.position, collider.transform.rotation,
                        out _, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateTarget(HandGrabInteractable interactable)
        {
            WristToGrabPoseOffset = this.GetGrabOffset();
            GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable, float.NegativeInfinity, out float grabStrength);
            SetTarget(interactable, selectingGrabTypes);
            SetGrabStrength(grabStrength);
        }

        private void UpdateTargetSliding(HandGrabInteractable interactable)
        {
            if (interactable.Slippiness <= 0f)
            {
                return;
            }
            float grabStrength = HandGrabInteraction.ComputeHandGrabScore(this, interactable,
                out GrabTypeFlags selectingGrabTypes, true);
            if (grabStrength <= interactable.Slippiness)
            {
                SetTarget(interactable, selectingGrabTypes);
            }
        }

        private void SetTarget(IHandGrabInteractable interactable, GrabTypeFlags selectingGrabTypes)
        {
            this.CalculateBestGrab(interactable, selectingGrabTypes, out GrabTypeFlags activeGrabType, ref _cachedResult);
            HandGrabTarget.Set(interactable.RelativeTo, interactable.HandAlignment, activeGrabType, _cachedResult);
        }

        private void SetGrabStrength(float strength)
        {
            FingersStrength = strength;
            WristStrength = strength;
        }

        #region Inject
        /// <summary>
        /// Convenience method combining <see cref="InjectHandGrabApi(HandGrabAPI)"/>,
        /// <see cref="InjectDistantCandidateComputer(DistantCandidateComputer{DistanceHandGrabInteractor, DistanceHandGrabInteractable})"/>,
        /// <see cref="InjectGrabOrigin(Transform)"/>, <see cref="InjectHand(IHand)"/>, and
        /// <see cref="InjectSupportedGrabTypes(GrabTypeFlags)"/>. This method exists to support Interaction SDK's dependency injection
        /// pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandGrabInteractor(HandGrabAPI handGrabApi,
            Transform grabOrigin,
            IHand hand, Rigidbody rigidbody, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectGrabOrigin(grabOrigin);
            InjectHand(hand);
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        /// <summary>
        /// Adds a <see cref="HandGrabAPI"/> to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHandGrabApi(HandGrabAPI handGrabAPI)
        {
            _handGrabApi = handGrabAPI;
        }

        /// <summary>
        /// Adds an <see cref="IHand"/> to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        /// <summary>
        /// Adds a Unity Rigidbody to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Adds a list of supported grabs to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        /// <summary>
        /// Adds a grab origin to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectGrabOrigin(Transform grabOrigin)
        {
            _grabOrigin = grabOrigin;
        }

        /// <summary>
        /// Adds a grip point to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }

        /// <summary>
        /// Adds a grip collider to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalGripCollider(Collider gripCollider)
        {
            _gripCollider = gripCollider;
        }

        /// <summary>
        /// Adds a pinch point to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        /// <summary>
        /// Adds a pinch collider to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalPinchCollider(Collider pinchCollider)
        {
            _pinchCollider = pinchCollider;
        }

        #endregion
    }
}
