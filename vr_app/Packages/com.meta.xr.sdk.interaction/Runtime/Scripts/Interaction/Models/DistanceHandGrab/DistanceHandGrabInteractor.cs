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
    /// DistanceHandGrabInteractor lets you grab interactables at a distance with hands. It operates with <see cref="HandGrabPose"/>s to
    /// specify the final pose of the hand and manipulate the objects via <see cref="IMovement"/>s in order to attract them, use them
    /// at a distance, etc. The DistanceHandGrabInteractor uses <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/> to
    /// detect far-away interactables.
    /// </summary>
    public class DistanceHandGrabInteractor :
        PointerInteractor<DistanceHandGrabInteractor, DistanceHandGrabInteractable>,
        IHandGrabInteractor, IDistanceInteractor
    {
        /// <summary>
        /// The <see cref="IHand"/> to use.
        /// </summary>
        [Tooltip("The hand to use.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractor.Hand"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public IHand Hand { get; private set; }

        /// <summary>
        /// Detects when the hand grab selects or unselects.
        /// </summary>
        [Tooltip("Detects when the hand grab selects or unselects.")]
        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [Header("Grabbing")]

        /// <summary>
        /// The grab types to support.
        /// </summary>
        [Tooltip("The grab types to support.")]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;

        /// <summary>
        /// The point on the hand used as the origin of the grab.
        /// </summary>
        [Tooltip("The point on the hand used as the origin of the grab.")]
        [SerializeField]
        private Transform _grabOrigin;

        /// <summary>
        /// Specifies an offset from the wrist that can be used to search for the best HandGrabInteractable available,
        /// act as a palm grab without a HandPose, and also act as an anchor for attaching the object.
        /// </summary>
        [Tooltip("Specifies an offset from the wrist that can be used to search for the best HandGrabInteractable available, act as a palm grab without a HandPose, and also act as an anchor for attaching the object.")]
        [SerializeField, Optional]
        private Transform _gripPoint;

        /// <summary>
        /// Specifies a moving point at the center of the tips of the currently pinching fingers.
        /// It's used to align interactables that don’t have a HandPose to the center of the pinch.
        /// </summary>
        [Tooltip("Specifies a moving point at the center of the tips of the currently pinching fingers. It's used to align interactables that don’t have a HandPose to the center of the pinch.")]
        [SerializeField, Optional]
        private Transform _pinchPoint;

        [SerializeField]
        private DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> _distantCandidateComputer
            = new DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable>();

        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabResult _cachedResult = new HandGrabResult();
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
        /// The <see cref="HandGrabTarget"/> used by this interactor when grabbing.
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

        /// <summary>
        /// Retrieves the pose (position and orientation) from which distance grabbing is calculated. This value comes from the
        /// <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/>, but conceptually it is a point relative to the hand
        /// around which it feels natural for the hand to grab.
        /// </summary>
        public Pose Origin => _distantCandidateComputer.Origin;

        /// <summary>
        /// The point in space from which the interactor is considered to have "hit" its current interactable. This is conceptually
        /// similar to <see cref="RayInteractor.CollisionInfo"/>, though the it is not the result of a simple raycast and instead
        /// results from calculations in the <see cref="DistantCandidateComputer{TInteractor, TInteractable}"/>.
        /// </summary>
        public Vector3 HitPoint { get; private set; }

        /// <summary>
        /// Retrieves the current interactable (null if this interactor isn't interacting with anything) as an
        /// <see cref="IRelativeToRef"/>. This is primarily used for visualizations.
        /// </summary>
        public IRelativeToRef DistanceInteractable => this.Interactable;

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
            _nativeId = 0x4469737447726162;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(_handGrabApi, nameof(_handGrabApi));
            this.AssertField(_distantCandidateComputer, nameof(_distantCandidateComputer));

            this.EndStart(ref _started);
        }

        #region life cycle

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

        protected override void InteractableSet(DistanceHandGrabInteractable interactable)
        {
            base.InteractableSet(interactable);
            UpdateTarget(Interactable);
        }

        protected override void InteractableUnset(DistanceHandGrabInteractable interactable)
        {
            base.InteractableUnset(interactable);
            SetGrabStrength(0f);
        }

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

        protected override void InteractableSelected(DistanceHandGrabInteractable interactable)
        {
            if (interactable != null)
            {
                WristToGrabPoseOffset = this.GetGrabOffset();
                this.Movement = this.GenerateMovement(interactable);
                SetGrabStrength(1f);
            }

            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(DistanceHandGrabInteractable interactable)
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
            return _handGrabShouldUnselect;
        }

        /// <summary>
        /// Overrides <see cref="Interactor{TInteractor, TInteractable}.CanSelect(TInteractable)"/>, augmenting the behavior of that
        /// base method with an additional call to
        /// <see cref="HandGrabInteraction.CanInteractWith(IHandGrabInteractor, IHandGrabInteractable)"/>, which confirms the
        /// presence of hand-specific requirements for valid hand interaction.
        /// </summary>
        /// <param name="interactable">The interactable</param>
        /// <returns>True if it is possible for this interactable to select <paramref name="interactable"/>, false otherwise</returns>
        public override bool CanSelect(DistanceHandGrabInteractable interactable)
        {
            if (!base.CanSelect(interactable))
            {
                return false;
            }
            return this.CanInteractWith(interactable);
        }

        protected override DistanceHandGrabInteractable ComputeCandidate()
        {
            DistanceHandGrabInteractable interactable = _distantCandidateComputer.ComputeCandidate(
               DistanceHandGrabInteractable.Registry, this, out Vector3 bestHitPoint);
            HitPoint = bestHitPoint;

            if (interactable == null)
            {
                return null;
            }

            GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable);
            GrabPoseScore score = this.GetPoseScore(interactable, selectingGrabTypes, ref _cachedResult);

            if (score.IsValid())
            {
                return interactable;
            }

            return null;
        }

        private GrabTypeFlags SelectingGrabTypes(IHandGrabInteractable interactable)
        {
            GrabTypeFlags selectingGrabTypes;
            if (State == InteractorState.Select
                || (selectingGrabTypes = this.ComputeShouldSelect(interactable)) == GrabTypeFlags.None)
            {
                HandGrabInteraction.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
            }

            if (selectingGrabTypes == GrabTypeFlags.None)
            {
                selectingGrabTypes = interactable.SupportedGrabTypes & this.SupportedGrabTypes;
            }

            return selectingGrabTypes;
        }

        private void UpdateTarget(IHandGrabInteractable interactable)
        {
            WristToGrabPoseOffset = this.GetGrabOffset();
            GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable);
            SetTarget(interactable, selectingGrabTypes);
            float grabStrength = HandGrabInteraction.ComputeHandGrabScore(this, interactable, out _);
            SetGrabStrength(grabStrength);
        }

        private void UpdateTargetSliding(IHandGrabInteractable interactable)
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
        /// <see cref="InjectGrabOrigin(Transform)"/>, /// <see cref="InjectHand(IHand)"/>, and
        /// <see cref="InjectSupportedGrabTypes(GrabTypeFlags)"/>. This method exists to support Interaction SDK's dependency injection
        /// pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllDistanceHandGrabInteractor(HandGrabAPI handGrabApi,
            DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> distantCandidateComputer,
            Transform grabOrigin,
            IHand hand, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectDistantCandidateComputer(distantCandidateComputer);
            InjectGrabOrigin(grabOrigin);
            InjectHand(hand);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        /// <summary>
        /// Adds a <see cref="HandGrabAPI"/> to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHandGrabApi(HandGrabAPI handGrabApi)
        {
            _handGrabApi = handGrabApi;
        }

        /// <summary>
        /// Adds a <see cref="DistantCandidateComputer"/> to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectDistantCandidateComputer(
            DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> distantCandidateComputer)
        {
            _distantCandidateComputer = distantCandidateComputer;
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
        /// Adds a pinch point to a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }
        #endregion
    }
}
