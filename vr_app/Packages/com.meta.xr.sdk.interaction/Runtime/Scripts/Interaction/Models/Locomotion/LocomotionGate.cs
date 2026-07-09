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
    /// This Gate reads the <see cref="IHand"/> orientation towards the shoulder and decides
    /// if it should be in Teleport mode or Turning mode.
    /// It enables/disables said modes based on some Input <see cref="IActiveState"/>s
    /// (EnableShape and DisableShape).
    /// It outputs it result into two <see cref="IActiveState"/>s (for Teleport and Turn)
    /// </summary>
    public class LocomotionGate : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// Hand that will be performing the Turn and Teleport
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        /// <summary>
        /// The <see cref="IHand"/> that provides orientation data to this component.
        /// </summary>
        public IHand Hand { get; private set; }

        /// <summary>
        /// Shoulder of the relevant Hand, used for correctly
        /// measuring the angle of the wrist to swap between Turning or Teleport
        /// </summary>
        [SerializeField]
        private Transform _shoulder;

        [SerializeField]
        private GateSection[] _gateSections = new GateSection[] {
            new GateSection(){locomotionMode = LocomotionMode.Teleport, maxAngle = 95f },
            new GateSection(){locomotionMode = LocomotionMode.Turn, minAngle = 40f, maxAngle =  165f },
            new GateSection(){locomotionMode = LocomotionMode.Teleport, minAngle = 120f, canEnterDirectly = false }
        };

        /// <summary>
        /// When it becomes Active, if the hand is within the valid threshold, the
        /// gate will enter Teleport or Turning mode
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _enableShape;
        private IActiveState EnableShape { get; set; }

        /// <summary>
        /// When active, the gate will exit Teleport and Turning modes
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _disableShape;
        private IActiveState DisableShape { get; set; }

        /// <summary>
        /// Used as an Output. The gate will enable this ActiveState when in Turning mode
        /// </summary>
        [SerializeField]
        private VirtualActiveState _turningState;
        /// <summary>
        /// Used as an Output. The gate will enable this ActiveState when in Teleport mode
        /// </summary>
        [SerializeField]
        private VirtualActiveState _teleportState;

        /// <summary>
        /// The Gate is considered active if it is in any of the gated sections
        /// </summary>
        public bool Active => _currentGateIndex >= 0;

        protected bool _started;
        private bool _previousShapeEnabled;
        private int _currentGateIndex = -1;

        private LocomotionMode _activeMode = LocomotionMode.None;

        /// <summary>
        /// The current active <see cref="LocomotionMode"/> determined by the shape
        /// and orientation of the assigned <see cref="IHand"/>.
        /// </summary>
        public LocomotionMode ActiveMode
        {
            get
            {
                return _activeMode;
            }
            private set
            {
                if (_activeMode != value)
                {
                    LocomotionMode prevMode = _activeMode;
                    _activeMode = value;
                    _teleportState.Active = _activeMode == LocomotionMode.Teleport;
                    _turningState.Active = _activeMode == LocomotionMode.Turn;
                    _whenActiveModeChanged.Invoke(new LocomotionModeEventArgs(prevMode, _activeMode));
                }
            }
        }

        /// <summary>
        /// Set of locomotion modes supported by this component - see <see cref="ActiveMode"/>.
        /// </summary>
        public enum LocomotionMode
        {
            None,
            Teleport,
            Turn
        }

        /// <summary>
        /// Contains threshold data used to map hand orientation to <see cref="LocomotionMode"/>.
        /// </summary>
        [System.Serializable]
        public class GateSection
        {
            /// <summary>
            /// The minimum detection angle.
            /// </summary>
            public float minAngle = _wristLimit;

            /// <summary>
            /// The maximum detection angle.
            /// </summary>
            public float maxAngle = 360f + _wristLimit;

            /// <summary>
            /// Determines if this gate can be entered from <see cref="LocomotionMode.None"/>,
            /// or if it must be entered from another gate.
            /// </summary>
            public bool canEnterDirectly = true;

            /// <summary>
            /// The locomotion mode that this gate represents.
            /// </summary>
            public LocomotionMode locomotionMode = LocomotionMode.None;

            /// <summary>
            /// Translates the provided angle to a scoring factor, which is
            /// used to determine which gate should be entered.
            /// </summary>
            /// <param name="angle">The input angle to score.</param>
            /// <returns>The score of this gate determined by the input angle.</returns>
            public float ScoreToAngle(float angle)
            {
                float dif = Mathf.Repeat(angle - minAngle, 360f);
                float range = Mathf.Repeat(maxAngle - minAngle, 360f);
                if (dif > range)
                {
                    return float.PositiveInfinity;
                }

                float average = (minAngle + maxAngle) / 2f;
                float score = Mathf.Repeat(Mathf.DeltaAngle(angle, average), 360f);
                return score;
            }
        }

        /// <summary>
        /// Data sent in the <see cref="WhenActiveModeChanged"/> event, which
        /// provides some details about the current and previous locomotion mode(s).
        /// </summary>
        public struct LocomotionModeEventArgs
        {
            public LocomotionMode PreviousMode { get; }
            public LocomotionMode NewMode { get; }

            public LocomotionModeEventArgs(
                LocomotionMode previousMode,
                LocomotionMode newMode)
            {
                PreviousMode = previousMode;
                NewMode = newMode;
            }
        }

        /// <summary>
        /// The current hand angle computed by this component, using the
        /// orientation of the <see cref="Hand"/>.
        /// </summary>
        public float CurrentAngle { get; private set; }

        /// <summary>
        /// The wrist direction of the <see cref="Hand"/>.
        /// </summary>
        public Vector3 WristDirection { get; private set; }

        /// <summary>
        /// A pose used for hand stabilization, derived from the
        /// shoulder-to-hand orientation.
        /// </summary>
        public Pose StabilizationPose { get; private set; } = Pose.identity;

        private Action<LocomotionModeEventArgs> _whenActiveModeChanged = delegate { };

        /// <summary>
        /// This event is raised whenever the active locomotion mode is changed
        /// (see <see cref="ActiveMode"/>).
        /// </summary>
        public event Action<LocomotionModeEventArgs> WhenActiveModeChanged
        {
            add => _whenActiveModeChanged += value;
            remove => _whenActiveModeChanged -= value;
        }

        private static readonly GateSection DefaultSection = new GateSection() { locomotionMode = LocomotionMode.None };
        private const float _enterPoseThreshold = 0.5f;
        private const float _wristLimit = -70f;

        private bool _cancelled = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
            if (EnableShape == null)
            {
                EnableShape = _enableShape as IActiveState;
            }
            if (DisableShape == null)
            {
                DisableShape = _disableShape as IActiveState;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(EnableShape, nameof(EnableShape));
            this.AssertField(DisableShape, nameof(DisableShape));
            this.AssertField(_teleportState, nameof(_teleportState));
            this.AssertField(_turningState, nameof(_turningState));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandupdated;
                Disable();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandupdated;
                Disable();
            }
        }

        /// <summary>
        /// Disables the Gate, forcing the user to perform the enable shape
        /// </summary>
        public void Disable()
        {
            ActiveMode = LocomotionMode.None;
            _currentGateIndex = -1;
            _cancelled = false;
        }

        /// <summary>
        /// Disables the Gate, forcing the user to perform the disable shape
        /// and then the enable shape
        /// </summary>
        public void Cancel()
        {
            ActiveMode = LocomotionMode.None;
            if (_currentGateIndex >= 0)
            {
                _cancelled = true;
            }
        }

        private void HandleHandupdated()
        {
            if (!Hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose))
            {
                Disable();
                return;
            }

            bool isRightHand = Hand.Handedness == Handedness.Right;
            Vector3 trackingUp = Vector3.up;
            Vector3 shoulderToHand = (wristPose.position - _shoulder.position).normalized;
            Vector3 trackingRight = Vector3.Cross(trackingUp, shoulderToHand).normalized;
            trackingRight = isRightHand ? trackingRight : -trackingRight;
            Vector3 wristDir = wristPose.rotation * (isRightHand ? Constants.RightThumbSide : Constants.LeftThumbSide);
            Vector3 fingersDir = wristPose.rotation * (isRightHand ? Constants.RightDistal : Constants.LeftDistal);
            bool fingersAway = (Vector3.Dot(fingersDir, Vector3.ProjectOnPlane(shoulderToHand, trackingUp).normalized) * 0.5 + 0.5f) > _enterPoseThreshold;

            wristDir = Vector3.ProjectOnPlane(wristDir, shoulderToHand).normalized;
            float angle = Vector3.SignedAngle(wristDir, trackingRight, shoulderToHand);

            angle = Hand.Handedness == Handedness.Right ? -angle : angle;
            if (angle < _wristLimit)
            {
                angle += 360f;
            }

            CurrentAngle = angle;
            StabilizationPose = new Pose(_shoulder.position, Quaternion.LookRotation(shoulderToHand));
            WristDirection = wristDir;

            bool shapeGateEnabled = false;
            if (EnableShape.Active && !_previousShapeEnabled)
            {
                shapeGateEnabled = true;
            }
            _previousShapeEnabled = EnableShape.Active;

            if (_currentGateIndex < 0
                && shapeGateEnabled
                && fingersAway)
            {
                GateSection gateSection = GetBestGateSection(CurrentAngle, out _currentGateIndex);
                if (gateSection.canEnterDirectly)
                {
                    ActiveMode = gateSection.locomotionMode;
                }
                else
                {
                    _currentGateIndex = -1;
                }
                return;
            }

            if (_currentGateIndex >= 0
                && DisableShape.Active)
            {
                Disable();
                return;
            }

            if (_currentGateIndex < 0 || _cancelled)
            {
                return;
            }

            GateSection currentGate = _gateSections[_currentGateIndex];

            if (CurrentAngle < currentGate.minAngle)
            {
                _currentGateIndex = Mathf.Max(0, _currentGateIndex - 1);
                ActiveMode = _gateSections[_currentGateIndex].locomotionMode;
            }
            else if (CurrentAngle > currentGate.maxAngle)
            {
                _currentGateIndex = Mathf.Min(_gateSections.Length - 1, _currentGateIndex + 1);
                ActiveMode = _gateSections[_currentGateIndex].locomotionMode;
            }
        }

        private GateSection GetBestGateSection(float angle, out int index)
        {
            float bestScore = float.PositiveInfinity;
            index = -1;

            for (int i = 0; i < _gateSections.Length; i++)
            {
                float score = _gateSections[i].ScoreToAngle(angle);
                if (score < bestScore)
                {
                    bestScore = score;
                    index = i;
                }
            }

            if (index == -1)
            {
                return DefaultSection;
            }
            return _gateSections[index];
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated
        /// <see cref="LocomotionGate"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllLocomotionGate(IHand hand, Transform shoulder,
            IActiveState enableShape, IActiveState disableShape,
            VirtualActiveState turningState, VirtualActiveState teleportState,
            GateSection[] gateSections)
        {
            InjectHand(hand);
            InjectShoulder(shoulder);
            InjectEnableShape(enableShape);
            InjectDisableShape(disableShape);
            InjectTurningState(turningState);
            InjectTeleportState(teleportState);
            InjectGateSections(gateSections);
        }

        /// <summary>
        /// Sets the underlying <see cref="IHand"/> for a dynamically instantiated
        /// <see cref="LocomotionGate"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        /// <summary>
        /// Sets the underlying shoulder transform for a dynamically instantiated
        /// <see cref="LocomotionGate"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectShoulder(Transform shoulder)
        {
            _shoulder = shoulder;
        }

        /// <summary>
        /// Sets the underlying <see cref="IActiveState"/> enable shape for a dynamically instantiated
        /// <see cref="LocomotionGate"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectEnableShape(IActiveState enableShape)
        {
            _enableShape = enableShape as UnityEngine.Object;
            EnableShape = enableShape;
        }

        /// <summary>
        /// Sets the underlying <see cref="IActiveState"/> disable shape for a dynamically instantiated
        /// <see cref="LocomotionGate"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectDisableShape(IActiveState disableShape)
        {
            _disableShape = disableShape as UnityEngine.Object;
            DisableShape = disableShape;
        }

        /// <summary>
        /// Sets the underlying <see cref="VirtualActiveState"/> for a dynamically instantiated
        /// <see cref="LocomotionGate"/> representing the turning state.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectTurningState(VirtualActiveState turningState)
        {
            _turningState = turningState;
        }

        /// <summary>
        /// Sets the underlying <see cref="VirtualActiveState"/> for a dynamically instantiated
        /// <see cref="LocomotionGate"/> representing the teleport state.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectTeleportState(VirtualActiveState teleportState)
        {
            _teleportState = teleportState;
        }

        /// <summary>
        /// Sets the underlying <see cref="GateSection"/> set for a dynamically instantiated
        /// <see cref="LocomotionGate"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not
        /// needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectGateSections(GateSection[] gateSections)
        {
            _gateSections = gateSections;
        }

        #endregion
    }
}
