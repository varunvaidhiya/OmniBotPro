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

using System;
using Oculus.Interaction.Input;
using UnityEngine;
using System.Collections.Generic;

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Tracks angular velocities (rotation deltas over the last two frames) for a list of joints and compares them to a rotation target around the provided axes.
    /// If the rotation target (degrees per second) is met for the minimum time threshold, the state becomes Active.
    /// </summary>
    public class JointRotationActiveState : MonoBehaviour,
        IActiveState, ITimeConsumer
    {
        public enum RelativeTo
        {
            Hand = 0,
            World = 1,
        }

        public enum WorldAxis
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
            PositiveZ = 4,
            NegativeZ = 5,
        }

        public enum HandAxis
        {
            Pronation = 0,
            Supination = 1,
            RadialDeviation = 2,
            UlnarDeviation = 3,
            Extension = 4,
            Flexion = 5,
        }

        [Serializable]
        public struct JointRotationFeatureState
        {
            /// <summary>
            /// The world target euler angles for a
            /// <see cref="JointRotationFeatureConfig"/>
            /// </summary>
            public readonly Vector3 TargetAxis;

            /// <summary>
            /// The normalized joint rotation along the target
            /// axis relative to <see cref="_degreesPerSecond"/>
            /// </summary>
            public readonly float Amount;

            public JointRotationFeatureState(Vector3 targetAxis, float amount)
            {
                TargetAxis = targetAxis;
                Amount = amount;
            }
        }

        [Serializable]
        public class JointRotationFeatureConfigList
        {
            [SerializeField]
            private List<JointRotationFeatureConfig> _values;

            public List<JointRotationFeatureConfig> Values => _values;
        }

        [Serializable]
        public class JointRotationFeatureConfig : FeatureConfigBase<HandJointId>
        {
            [Tooltip("The detection axis will be in this coordinate space.")]
            [SerializeField]
            private RelativeTo _relativeTo = RelativeTo.Hand;

            [Tooltip("The world axis used for detection.")]
            [SerializeField]
            private WorldAxis _worldAxis = WorldAxis.PositiveZ;

            [Tooltip("The axis of the hand root pose used for detection.")]
            [SerializeField]
            private HandAxis _handAxis = HandAxis.RadialDeviation;

            public RelativeTo RelativeTo
            {
                get => _relativeTo;
                set => _relativeTo = value;
            }

            public WorldAxis WorldAxis
            {
                get => _worldAxis;
                set => _worldAxis = value;
            }

            public HandAxis HandAxis
            {
                get => _handAxis;
                set => _handAxis = value;
            }
        }

        [Tooltip("Provided joints will be sourced from this IHand.")]
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [Tooltip("JointDeltaProvider caches joint deltas to avoid " +
            "unnecessary recomputing of deltas.")]
        [SerializeField, Interface(typeof(IJointDeltaProvider))]
        private UnityEngine.Object _jointDeltaProvider;

        [SerializeField]
        private JointRotationFeatureConfigList _featureConfigs;

        [SerializeField]
        private JointRotationFeatureConfigList _featureConfigurations;

        [Tooltip("The angular velocity used for the detection " +
            "threshold, in degrees per second.")]
        [SerializeField, Min(0)]
        private float _degreesPerSecond = 120f;

        [Tooltip("The degrees per second value will be modified by this width " +
            "to create differing enter/exit thresholds. Used to prevent " +
            "chattering at the threshold edge.")]
        [SerializeField, Min(0)]
        private float _thresholdWidth = 30f;

        [Tooltip("A new state must be maintaned for at least this " +
            "many seconds before the Active property changes.")]
        [SerializeField, Min(0)]
        private float _minTimeInState = 0.05f;

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }

                UpdateActiveState();
                return _activeState;
            }
        }

        private Func<float> _timeProvider = () => Time.time;
        public void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public IReadOnlyList<JointRotationFeatureConfig> FeatureConfigs =>
#if ISDK_OPENXR_HAND
            _featureConfigurations.Values;
#else
            _featureConfigs.Values;
#endif

        public IReadOnlyDictionary<JointRotationFeatureConfig, JointRotationFeatureState> FeatureStates =>
             _featureStates;

        private Dictionary<JointRotationFeatureConfig, JointRotationFeatureState> _featureStates =
            new Dictionary<JointRotationFeatureConfig, JointRotationFeatureState>();


        private JointDeltaConfig _jointDeltaConfig;
        private IJointDeltaProvider JointDeltaProvider;

        private int _lastStateUpdateFrame;
        private float _lastStateChangeTime;
        private float _lastUpdateTime;
        private bool _internalState;
        private bool _activeState;

        protected bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
            if (JointDeltaProvider == null)
            {
                JointDeltaProvider = _jointDeltaProvider as IJointDeltaProvider;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(Hand, nameof(Hand));
            this.AssertField(JointDeltaProvider, nameof(JointDeltaProvider));
            this.AssertCollectionField(FeatureConfigs, nameof(FeatureConfigs));
            this.AssertField(_timeProvider, nameof(_timeProvider));

            IList<HandJointId> allTrackedJoints = new List<HandJointId>();
            foreach (var config in FeatureConfigs)
            {
                allTrackedJoints.Add(config.Feature);
                _featureStates.Add(config, new JointRotationFeatureState());
            }
            _jointDeltaConfig = new JointDeltaConfig(GetHashCode(), allTrackedJoints);


            _lastUpdateTime = _timeProvider();
            this.EndStart(ref _started);
        }

        private bool CheckAllJointRotations()
        {
            bool result = true;

            float deltaTime = _timeProvider() - _lastUpdateTime;
            float threshold = _internalState ?
                  _degreesPerSecond + _thresholdWidth * 0.5f :
                  _degreesPerSecond - _thresholdWidth * 0.5f;

            threshold *= deltaTime;

            foreach (var config in FeatureConfigs)
            {
                if (Hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose) &&
                    Hand.GetJointPose(config.Feature, out _) &&
                    JointDeltaProvider.GetRotationDelta(
                        config.Feature, out Quaternion worldDeltaRotation))
                {
                    Vector3 worldTargetAxis = GetWorldTargetAxis(wristPose, config);
                    worldDeltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                    float axisDifference = Mathf.Abs(Vector3.Dot(axis, worldTargetAxis));
                    float rotationOnTargetAxis = angle * axisDifference;

                    _featureStates[config] = new JointRotationFeatureState(
                                             worldTargetAxis, threshold <= 0 ? 1f :
                                                Mathf.Clamp01(rotationOnTargetAxis / threshold));

                    bool rotationExceedsThreshold = rotationOnTargetAxis > threshold;
                    result &= rotationExceedsThreshold;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        protected virtual void Update()
        {
            UpdateActiveState();
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                JointDeltaProvider.RegisterConfig(_jointDeltaConfig);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                JointDeltaProvider.UnRegisterConfig(_jointDeltaConfig);
            }
        }

        private void UpdateActiveState()
        {
            if (Time.frameCount <= _lastStateUpdateFrame)
            {
                return;
            }
            _lastStateUpdateFrame = Time.frameCount;

            bool newState = CheckAllJointRotations();

            if (newState != _internalState)
            {
                _internalState = newState;
                _lastStateChangeTime = _timeProvider();
            }

            if (_timeProvider() - _lastStateChangeTime >= _minTimeInState)
            {
                _activeState = _internalState;
            }
            _lastUpdateTime = _timeProvider();
        }

        private Vector3 GetWorldTargetAxis(Pose wristPose, JointRotationFeatureConfig config)
        {
            switch (config.RelativeTo)
            {
                default:
                case RelativeTo.Hand:
                    return GetHandAxisVector(config.HandAxis, wristPose);
                case RelativeTo.World:
                    return GetWorldAxisVector(config.WorldAxis);
            }
        }

        private Vector3 GetWorldAxisVector(WorldAxis axis)
        {
            switch (axis)
            {
                default:
                case WorldAxis.PositiveX:
                    return Vector3.right;
                case WorldAxis.NegativeX:
                    return Vector3.left;
                case WorldAxis.PositiveY:
                    return Vector3.up;
                case WorldAxis.NegativeY:
                    return Vector3.down;
                case WorldAxis.PositiveZ:
                    return Vector3.forward;
                case WorldAxis.NegativeZ:
                    return Vector3.back;
            }
        }

        private Vector3 GetHandAxisVector(HandAxis axis, Pose wristPose)
        {
            switch (axis)
            {
                case HandAxis.Pronation:
                    return wristPose.rotation * (Hand.Handedness == Handedness.Left ?
                        Constants.LeftProximal : Constants.RightProximal);
                case HandAxis.Supination:
                    return wristPose.rotation * (Hand.Handedness == Handedness.Left ?
                        Constants.LeftDistal : Constants.RightDistal);
                case HandAxis.RadialDeviation:
                    return wristPose.rotation * (Hand.Handedness == Handedness.Left ?
                        Constants.LeftPalmar : Constants.RightPalmar);
                case HandAxis.UlnarDeviation:
                    return wristPose.rotation * (Hand.Handedness == Handedness.Left ?
                         Constants.LeftDorsal : Constants.RightDorsal);
                case HandAxis.Extension:
                    return wristPose.rotation * (Hand.Handedness == Handedness.Left ?
                        Constants.LeftThumbSide : Constants.RightThumbSide);
                case HandAxis.Flexion:
                    return wristPose.rotation * (Hand.Handedness == Handedness.Left ?
                        Constants.LeftPinkySide : Constants.RightPinkySide);
                default:
                    return Vector3.zero;
            }
        }

        #region Inject

        public void InjectAllJointRotationActiveState(JointRotationFeatureConfigList featureConfigs,
                                                      IHand hand, IJointDeltaProvider jointDeltaProvider)
        {
            InjectFeatureConfigList(featureConfigs);
            InjectHand(hand);
            InjectJointDeltaProvider(jointDeltaProvider);
        }

        public void InjectFeatureConfigList(JointRotationFeatureConfigList featureConfigs)
        {
            _featureConfigs = featureConfigs;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectJointDeltaProvider(IJointDeltaProvider jointDeltaProvider)
        {
            JointDeltaProvider = jointDeltaProvider;
            _jointDeltaProvider = jointDeltaProvider as UnityEngine.Object;
        }

        [Obsolete("Use SetTimeProvider()")]
        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        #endregion

    }
}


