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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Makes an object grabbable by hands so long as it's within arm's reach.
    /// </summary>
    /// <remarks>
    /// A HandGrabInteractable indicates the properties about how a hand can HandGrab an object.
    /// It specifies the fingers that must perform the grab and the release and generates the events
    /// for the Pointable to move the object.
    /// Optionally it can  reference a list of differently scaled HandGrabPoses to inform the
    /// interactor about the best pose the hand should adopt when grabbing the object with different
    /// sized hands.
    /// </remarks>
    [Serializable]
    public partial class HandGrabInteractable : PointerInteractable<HandGrabInteractor, HandGrabInteractable>,
        IHandGrabInteractable, IRigidbodyRef, ICollidersRef
    {
        /// <summary>
        /// The Rigidbody of the object.
        /// </summary>
        [Tooltip("The Rigidbody of the object.")]
        [SerializeField]
        private Rigidbody _rigidbody;

        /// <summary>
        /// The Unity Rigidbody used for collision detection. The returned value is from the _rigidbody field, which is set from the
        /// Unity Editor.
        /// </summary>
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// Forces a release on all other grabbing interactors when grabbed by a new interactor.
        /// </summary>
        [Tooltip("Forces a release on all other grabbing interactors when grabbed by a new interactor.")]
        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;

        /// <summary>
        /// Related to <see cref="GrabInteractable.ResetGrabOnGrabsUpdated"/>; forces a release on all other grabbing interactors when
        /// grabbed by a new interactor.
        /// </summary>
        public bool ResetGrabOnGrabsUpdated
        {
            get
            {
                return _resetGrabOnGrabsUpdated;
            }
            set
            {
                _resetGrabOnGrabsUpdated = value;
            }
        }

        /// <summary>
        /// A <cref="PoseMeasureParameters" /> used to modify the score of a pose.
        /// </summary>
        [Tooltip("A PoseMeasureParameters used to modify the score of a pose.")]
        [SerializeField, Optional]
        private PoseMeasureParameters _scoringModifier = new PoseMeasureParameters(0.8f);

        /// <summary>
        /// Defines the slippiness threshold so the interactor can slide along the interactable based on the
        /// strength of the grip. GrabSurfaces are required to slide. At min slippiness = 0, the interactor never moves.
        /// </summary>
        [SerializeField, Optional, Range(0f, 1f)]
        [Tooltip("Defines the slippiness threshold so the interactor can slide along the interactable based on the" +
            "strength of the grip. GrabSurfaces are required to slide. At min slippiness = 0, the interactor never moves.")]
        private float _slippiness = 0f;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.Slippiness"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public float Slippiness
        {
            get
            {
                return _slippiness;
            }
            set
            {
                _slippiness = value;
            }
        }

        /// <summary>
        /// The grab types that the object supports.
        /// </summary>
        [Tooltip("The grab types that the object supports.")]
        [Space]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;

        /// <summary>
        /// Uses the state of the fingers to define when a pinch grab starts and ends.
        /// </summary>
        [Tooltip("Uses the state of the fingers to define when a pinch grab starts and ends.")]
        [SerializeField]
        private GrabbingRule _pinchGrabRules = GrabbingRule.DefaultPinchRule;

        /// <summary>
        /// Uses the state of the fingers to define when a palm grab starts and ends.
        /// </summary>
        [Tooltip("Uses the state of the fingers to define when a palm grab starts and ends.")]
        [SerializeField]
        private GrabbingRule _palmGrabRules = GrabbingRule.DefaultPalmRule;

        [Header("Movement", order = -1)]

        /// <summary>
        /// Determines how the object will move when selected.
        /// </summary>
        [Tooltip("Determines how the object will move when selected.")]
        [SerializeField, Interface(typeof(IMovementProvider))]
        [Optional(OptionalAttribute.Flag.AutoGenerated)]
        private UnityEngine.Object _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        /// <summary>
        /// Determines when the hand will be aligned with the object.
        /// </summary>
        [Tooltip("Determines when the hand will be aligned with the object.")]
        [SerializeField]
        private HandAlignType _handAligment = HandAlignType.AlignOnGrab;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.HandAlignment"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public HandAlignType HandAlignment
        {
            get
            {
                return _handAligment;
            }
            set
            {
                _handAligment = value;
            }
        }

        /// <summary>
        /// Scaled <cref="HandGrabPose" />s that represent the hand's position during a grab.
        /// </summary>
        [Tooltip(" ")]
        [SerializeField]
        [Optional(OptionalAttribute.Flag.DontHide)]
        [UnityEngine.Serialization.FormerlySerializedAs("_handGrabPoints")]
        private List<HandGrabPose> _handGrabPoses = new List<HandGrabPose>();

        /// <summary>
        /// Retrieves the list of <see cref="HandGrabPose"/>s which can be used to grab this interactable. This returns the value of the
        /// _handGrabPoses field, which is typically set from the Unity Editor.
        /// </summary>
        public List<HandGrabPose> HandGrabPoses => _handGrabPoses;

        /// <summary>
        /// Implementation of <see cref="IRelativeToRef.RelativeTo"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public Transform RelativeTo => _rigidbody.transform;

        /// <summary>
        /// Returns the <see cref="PoseMeasureParameters"/> used in the scoring mechanism which selects which of the available
        /// hand grab poses should be used for interaction.
        /// </summary>
        public PoseMeasureParameters ScoreModifier => _scoringModifier;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.SupportedGrabTypes"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.PinchGrabRules"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public GrabbingRule PinchGrabRules => _pinchGrabRules;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.PalmGrabRules"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public GrabbingRule PalmGrabRules => _palmGrabRules;

        /// <summary>
        /// The list of Unity Colliders associated with this interactable. This list is automatically populated during the
        /// MonoBehaviour's start-up process and will contain a reference to every Collider in <see cref="Rigidbody"/>'s hierarchy
        /// at that time. This list is not allowed to be empty, meaning there must be at least one Collider in
        /// <see cref="Rigidbody"/>'s hierarchy at the time the DistanceHandGrabInteractable first becomes active.
        /// </summary>
        public Collider[] Colliders { get; private set; }

        private GrabPoseFinder _grabPoseFinder;

        private static CollisionInteractionRegistry<HandGrabInteractor, HandGrabInteractable> _registry = null;

        #region editor events
        protected virtual void Reset()
        {
            InjectRigidbody(this.GetComponentInParent<Rigidbody>());
            InjectOptionalPointableElement(this.GetComponentInParent<Grabbable>());
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            MovementProvider = _movementProvider as IMovementProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Rigidbody, nameof(Rigidbody));
            if (_registry == null)
            {
                _registry = new CollisionInteractionRegistry<HandGrabInteractor, HandGrabInteractable>();
                SetRegistry(_registry);
            }
            Colliders = Rigidbody.GetComponentsInChildren<Collider>();
            this.AssertCollectionField(Colliders, nameof(Colliders),
                whyItFailed: $"The associated {nameof(Rigidbody)} must have at least one collider.");

            if (MovementProvider == null)
            {
                IMovementProvider movementProvider;
                movementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            _grabPoseFinder = new GrabPoseFinder(_handGrabPoses, this.RelativeTo);
            this.EndStart(ref _started);
        }

        #region pose moving

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.GenerateMovement(in Pose, in Pose)"/>; for details, please refer to
        /// the related documentation provided for that interface.
        /// </summary>
        public IMovement GenerateMovement(in Pose from, in Pose to)
        {
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(from);
            movement.MoveTo(to);
            return movement;
        }

        #endregion
        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.CalculateBestPose(Pose, float, Handedness, ref HandGrabResult)"/>;
        /// for details, please refer to the related documentation provided for that interface.
        /// </summary>
        public bool CalculateBestPose(Pose userPose, float handScale, Handedness handedness,
            ref HandGrabResult result)
        {
            CalculateBestPose(userPose, Pose.identity, RelativeTo, handScale, handedness, ref result);
            return true;
        }

        /// <summary>
        /// Implementation of
        /// <see cref="IHandGrabInteractable.CalculateBestPose(in Pose, in Pose, Transform, float, Handedness, ref HandGrabResult)"/>;
        /// for details, please refer to the related documentation provided for that interface.
        /// </summary>
        public void CalculateBestPose(in Pose userPose, in Pose offset, Transform relativeTo,
            float handScale, Handedness handedness,
            ref HandGrabResult result)
        {
            bool poseFound = _grabPoseFinder.FindBestPose(userPose, offset,
                handScale, handedness, _scoringModifier, ref result);

            if (!poseFound)
            {
                Pose targetPose = PoseUtils.Multiply(userPose, offset);
                result.HasHandPose = false;
                result.Score = GrabPoseHelper.CollidersScore(targetPose.position, Colliders, out Vector3 hit);
                Pose worldSnap = new Pose(hit, targetPose.rotation);
                result.RelativePose = RelativeTo.Delta(worldSnap);
            }
        }

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.UsesHandPose"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public bool UsesHandPose => _grabPoseFinder.UsesHandPose;

        /// <summary>
        /// Implementation of <see cref="IHandGrabInteractable.SupportsHandedness(Handedness)"/>; for details, please refer to the
        /// related documentation provided for that interface.
        /// </summary>
        public bool SupportsHandedness(Handedness handedness)
        {
            return _grabPoseFinder.SupportsHandedness(handedness);
        }

        #region Inject
        /// <summary>
        /// Convenience method combining <see cref="InjectSupportedGrabTypes(GrabTypeFlags)"/>,
        /// <see cref="InjectRigidbody(Rigidbody)"/>, <see cref="InjectPinchGrabRules(GrabbingRule)"/>, and
        /// <see cref="InjectPalmGrabRules(GrabbingRule)"/>. This method exists to support Interaction SDK's dependency injection
        /// pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllHandGrabInteractable(GrabTypeFlags supportedGrabTypes,
            Rigidbody rigidbody,
            GrabbingRule pinchGrabRules, GrabbingRule palmGrabRules)
        {
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectRigidbody(rigidbody);
            InjectPinchGrabRules(pinchGrabRules);
            InjectPalmGrabRules(palmGrabRules);
        }

        /// <summary>
        /// Adds a <see cref="GrabTypeFlags"/> as <see cref="SupportedGrabTypes"/> in a dynamically instantiated
        /// DistanceHandGrabInteractable. This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        /// <summary>
        /// Adds a <see cref="GrabbingRule"/> as <see cref="PinchGrabRules"/> to a dynamically instantiated
        /// DistanceHandGrabInteractable. This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectPinchGrabRules(GrabbingRule pinchGrabRules)
        {
            _pinchGrabRules = pinchGrabRules;
        }

        /// <summary>
        /// Adds a <see cref="GrabbingRule"/> as <see cref="PalmGrabRules"/> to a dynamically instantiated
        /// DistanceHandGrabInteractable. This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectPalmGrabRules(GrabbingRule palmGrabRules)
        {
            _palmGrabRules = palmGrabRules;
        }

        /// <summary>
        /// Adds a Unity Rigidbody as <see cref="Rigidbody"/> to a dynamically instantiated DistanceHandGrabInteractable. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        /// <summary>
        /// Adds a list of <see cref="DistanceHandGrabInteractor"/> as <see cref="HandGrabPoses"/> to a dynamically instantiated
        /// DistanceHandGrabInteractable. This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalScoreModifier(PoseMeasureParameters scoreModifier)
        {
            _scoringModifier = scoreModifier;
        }

        /// <summary>
        /// Adds a list of <see cref="DistanceHandGrabInteractor"/> as <see cref="HandGrabPoses"/> to a dynamically instantiated
        /// DistanceHandGrabInteractable. This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalHandGrabPoses(List<HandGrabPose> handGrabPoses)
        {
            _handGrabPoses = handGrabPoses;
        }

        /// <summary>
        /// Adds an <see cref="IMovementProvider"/> as <see cref="MovementProvider"/> to a dynamically instantiated
        /// DistanceHandGrabInteractable. This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as UnityEngine.Object;
            MovementProvider = provider;
        }

        #endregion

    }
}
