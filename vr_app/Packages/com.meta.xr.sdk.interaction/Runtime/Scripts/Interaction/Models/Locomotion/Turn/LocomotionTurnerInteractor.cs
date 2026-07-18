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

using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Defines an <see cref="IInteractor"/> which performs locomotion turns (i.e., rotating the user in virtual space) based on
    /// input from a pair of Unity Transforms, which typically are independently positioned in the scene based on hand or body tracking.
    /// There is no real interactable for this interactor (<see cref="LocomotionTurnerInteractable"/> is an empty type, as described
    /// in the comments on that class).
    /// </summary>
    /// <remarks>
    /// For the controller-driven counterpart to this type, see <see cref="LocomotionAxisTurnerInteractor"/>.
    /// </remarks>
    public class LocomotionTurnerInteractor : Interactor<LocomotionTurnerInteractor, LocomotionTurnerInteractable>
        , IAxis1D
    {
        [SerializeField]
        [Tooltip("Point in space used to drive the axis.")]
        private Transform _origin;

        [SerializeField, Interface(typeof(ISelector))]
        [Tooltip("Selector for the interactor.")]
        private UnityEngine.Object _selector;

        [SerializeField]
        [Tooltip("Point used to stabilize the rotation of the point")]
        private Transform _stabilizationPoint;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        [Tooltip("Transformer is required so calculations can be done in Tracking space")]
        private UnityEngine.Object _transformer;
        /// <summary>
        /// <see cref="ITrackingToWorldTransformer"/> for moving spatial data between world space and tracking space.
        /// This is required so that turning calculations can be performed in tracking space.
        /// </summary>
        public ITrackingToWorldTransformer Transformer;

        [SerializeField]
        [Tooltip("Offset from the center point at which the pointer will be dragged")]
        private float _dragThresold = 0.1f;
        /// <summary>
        /// Offset from <see cref="MidPoint"/> at which the pointer will be dragged.
        /// </summary>
        /// <remarks>
        /// The <see cref="MidPoint"/> of the turner interactor implies a "reset distance" on turn direction choice, and dragging
        /// it making it easier to choose the other direction to turn. For example, if the user begins interacting by moving their
        /// hand fifteen centimeters to the left, that will select "left" as the turn direction. Then, if the user wishes to change
        /// direction an turn right, without dragging the user will have to move *more* than fifteen centimeters to the right.
        /// However, if DragThreshold was 0.05 (or any number smaller than 0.15), then the user would only have to move right by
        /// that amount to change direction because the <see cref="MidPoint"/> would have been dragged after left movement exceeded
        /// the threshold.
        /// </remarks>
        public float DragThresold
        {
            get
            {
                return _dragThresold;
            }
            set
            {
                _dragThresold = value;
            }
        }

        private Pose _midPoint = Pose.identity;

        /// <summary>
        /// Center point where the Axis value is 0. This point is set at the start of interaction and moved based on user movement
        /// (for example, if the user moves <see cref="Origin"/> further than <see cref="DragThresold"/> from the MidPoint.
        /// </summary>
        public Pose MidPoint => Transformer.ToWorldPose(_midPoint);

        /// <summary>
        /// Point of the actual origin in world space. The offset from Origin to MidPoint indicates the Axis value.
        /// </summary>
        /// <remarks>
        /// This point is articulated independently of the interactor, but it typically represents user motion (for example, a
        /// hand's position in space).
        /// </remarks>
        public Pose Origin => _origin.GetPose();

        private float _axisValue = 0f;

        private Action<float> _whenTurnDirectionChanged = delegate { };

        /// <summary>
        /// Event broadcast to indicate that the axis has changed sign (i.e., the user has switched from turning left to
        /// turning right, or vice versa).
        /// </summary>
        /// <remarks>
        /// For more information, see the remarks on <see cref="Value"/>.
        /// </remarks>
        public event Action<float> WhenTurnDirectionChanged
        {
            add
            {
                _whenTurnDirectionChanged += value;
            }
            remove
            {
                _whenTurnDirectionChanged -= value;
            }
        }

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldHover"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public override bool ShouldHover => State == InteractorState.Normal;

        /// <summary>
        /// Implementation of <see cref="IInteractor.ShouldUnhover"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public override bool ShouldUnhover => false;

        protected override void Awake()
        {
            base.Awake();
            Transformer = _transformer as ITrackingToWorldTransformer;
            Selector = _selector as ISelector;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_origin, nameof(_origin));
            this.AssertField(_stabilizationPoint, nameof(_stabilizationPoint));
            this.AssertField(Transformer, nameof(Transformer));
            this.AssertField(Selector, nameof(Selector));
            this.EndStart(ref _started);
        }

        protected override void HandleEnabled()
        {
            base.HandleEnabled();

            Pose pointer = _origin.GetPose();
            InitializeMidPoint(pointer);
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();
            UpdatePointers();
        }

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();
            UpdatePointers();
        }

        private void UpdatePointers()
        {
            Pose pointer = _origin.GetPose();
            UpdateMidPoint(pointer, MidPoint);
            DragMidPoint(MidPoint);
            UpdateAxisValue(pointer, MidPoint);
        }

        private void InitializeMidPoint(Pose pointer)
        {
            Vector3 direction = Vector3.ProjectOnPlane(pointer.position - _stabilizationPoint.position, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 point = pointer.position;
            _midPoint = Transformer.ToTrackingPose(new Pose(point, rotation));
        }

        private void UpdateMidPoint(Pose pointer, Pose midPoint)
        {
            float length = Vector3.ProjectOnPlane(pointer.position - _stabilizationPoint.position, Vector3.up).magnitude;
            Vector3 position = _stabilizationPoint.position + midPoint.forward * length;
            position.y = pointer.position.y;
            Quaternion rotation = midPoint.rotation;
            _midPoint = Transformer.ToTrackingPose(new Pose(position, rotation));
        }

        private void DragMidPoint(Pose worldMidPoint)
        {
            Vector3 midPointPos = worldMidPoint.position;
            float distance = Mathf.Abs(_axisValue) - _dragThresold * this.transform.lossyScale.x;
            if (distance <= 0)
            {
                return;
            }

            Vector3 right = worldMidPoint.right;
            float direction = Math.Sign(_axisValue);
            midPointPos += right * direction * distance;

            Vector3 lookDirection = Vector3.ProjectOnPlane(midPointPos - _stabilizationPoint.position, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            _midPoint = Transformer.ToTrackingPose(new Pose(midPointPos, rotation));
        }

        private void UpdateAxisValue(Pose pointer, Pose origin)
        {
            float prevSign = Mathf.Sign(_axisValue);
            Vector3 diff = pointer.position - origin.position;
            Vector3 deviation = Vector3.Project(pointer.position - origin.position, origin.right);
            _axisValue = deviation.magnitude * Mathf.Sign(Vector3.Dot(origin.right, diff));
            if (prevSign != Mathf.Sign(_axisValue))
            {
                _whenTurnDirectionChanged(prevSign);
            }
        }

        /// <summary>
        /// Axis value of the interactor, between -1 and 1 where a negative number indicates a `left turn and a positive value a
        /// right turn.
        /// </summary>
        /// <returns>A value between -1 and 1</returns>
        /// <remarks>
        /// "Axis" in this context refers to an underlying conceptual <see cref="IAxis1D"/> analogous to the <see cref="IAxis2D"/>
        /// used by <see cref="LocomotionAxisTurnerInteractor"/>. While there is no actual underlying axis for
        /// LocomotionTurnerInteractors, axis-like values are calculated from the Unity transforms the interactor observes, and
        /// those values are used to make interaction decisions.
        /// </remarks>
        public float Value()
        {
            return Mathf.Clamp(_axisValue / (_dragThresold * this.transform.lossyScale.x), -1f, 1f);
        }

        protected override LocomotionTurnerInteractable ComputeCandidate()
        {
            return null;
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated LocomotionTurnerInteractor; effectively wraps
        /// <see cref="InjectOrigin(Transform)"/>, <see cref="InjectSelector(ISelector)"/>, <see cref="InjectStabilizationPoint(Transform)"/>,
        /// and <see cref="InjectTransformer(ITrackingToWorldTransformer)"/>. This method exists to support Interaction SDK's dependency injection
        /// pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllLocomotionTurnerInteractor(Transform origin,
            ISelector selector,
            Transform stabilizationPoint,
            ITrackingToWorldTransformer transformer)
        {
            InjectOrigin(origin);
            InjectSelector(selector);
            InjectStabilizationPoint(stabilizationPoint);
            InjectTransformer(transformer);
        }

        /// <summary>
        /// Sets the origin transform (the point representing the user's direct input, typically a tracked hand) for a dynamically
        /// instantiated LocomotionTurnerInteractor. This method exists to support Interaction SDK's dependency injection pattern and is
        /// not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOrigin(Transform origin)
        {
            _origin = origin;
        }

        /// <summary>
        /// Sets an <see cref="ISelector"/> for a dynamically instantiated LocomotionTurnerInteractor. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        /// <summary>
        /// Sets the stabilization transform (the a point used in interaction calculations, typically a tracked or estimated shoulder
        /// or elbow  position) for a dynamically instantiated LocomotionTurnerInteractor. This method exists to
        /// support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectStabilizationPoint(Transform stabilizationPoint)
        {
            _stabilizationPoint = stabilizationPoint;
        }

        /// <summary>
        /// Sets an <see cref="ITrackingToWorldTransformer"/> for a dynamically instantiated LocomotionTurnerInteractor. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectTransformer(ITrackingToWorldTransformer transformer)
        {
            _transformer = transformer as UnityEngine.Object;
            Transformer = transformer;
        }
        #endregion
    }

    /// <summary>
    /// Contractually, <see cref="Interactor{TInteractor, TInteractable}"/>s require interactables, but
    /// <see cref="LocomotionTurnerInteractor"/>'s interaction (turning the user) doesn't logically require
    /// a target since it simply invokes a specific effect. LocomotionTurnerInteractable is thus
    /// provided as an empty interactable type, no instances of which should ever be created, simply as an
    /// implementation detail of <see cref="LocomotionTurnerInteractor"/>.
    /// </summary>
    public class LocomotionTurnerInteractable : Interactable<LocomotionTurnerInteractor, LocomotionTurnerInteractable> { }
}
