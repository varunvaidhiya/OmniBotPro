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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection
{
    public enum UpVectorType
    {
        Head,
        Tracking,
        World
    }

    /// <summary>
    /// Influences state transitions computed via <cref="TransformFeatureStateProvider" />. It becomes active whenever all of the listed transform states are active.
    /// </summary>
    [Serializable]
    public class TransformConfig
    {
        public TransformConfig()
        {
            PositionOffset = Vector3.zero;
            RotationOffset = Vector3.zero;
            UpVectorType = UpVectorType.Head;
            FeatureThresholds = null;
            InstanceId = 0;
        }

        // Position offset relative to the reference transform.
        public Vector3 PositionOffset;

        // Rotational offset relative to the reference transform.
        public Vector3 RotationOffset;

        public UpVectorType UpVectorType;

        public TransformFeatureStateThresholds FeatureThresholds;

        // set via component that uses this class
        public int InstanceId { get; set; }
    }

    public class TransformJointData
    {
        public bool IsValid;
        public Handedness Handedness;
        public Pose CenterEyePose, WristPose;
        public Vector3 TrackingSystemUp;
        public Vector3 TrackingSystemForward;
    }

    internal class TransformFeatureStateCollection
    {
        public class TransformStateInfo
        {
            public TransformStateInfo(TransformConfig transformConfig,
                FeatureStateProvider<TransformFeature, string> stateProvider)
            {
                Config = transformConfig;
                StateProvider = stateProvider;
            }

            public TransformConfig Config;
            public FeatureStateProvider<TransformFeature, string> StateProvider;
        }

        private Dictionary<int, TransformStateInfo> _idToTransformStateInfo =
            new Dictionary<int, TransformStateInfo>();

        public void RegisterConfig(TransformConfig transformConfig, TransformJointData jointData,
            Func<float> timeProvider)
        {
            bool containsKeyAlready = _idToTransformStateInfo.ContainsKey(transformConfig.InstanceId);
            Assert.IsFalse(containsKeyAlready,
                "Trying to register multiple configs with the same id into " +
                "TransformFeatureStateCollection.");

            var featureStateProvider = new FeatureStateProvider<TransformFeature, string>
                // note that jointData and transformConfig are reference types (classes), because they can change
                // during run time
                ((feature) => TransformFeatureValueProvider.GetValue(feature, jointData, transformConfig),
                    feature => (int)feature,
                    timeProvider);
            TransformStateInfo newTransfState = new TransformStateInfo(transformConfig, featureStateProvider);
            featureStateProvider.InitializeThresholds(transformConfig.FeatureThresholds);
            _idToTransformStateInfo.Add(transformConfig.InstanceId, newTransfState);
        }

        public void UnRegisterConfig(TransformConfig transformConfig)
        {
            _idToTransformStateInfo.Remove(transformConfig.InstanceId);
        }

        public FeatureStateProvider<TransformFeature, string> GetStateProvider(
            TransformConfig transformConfig)
        {
            return _idToTransformStateInfo[transformConfig.InstanceId].StateProvider;
        }

        public void SetConfig(int configId, TransformConfig config)
        {
            _idToTransformStateInfo[configId].Config = config;
        }

        public TransformConfig GetConfig(int configId)
        {
            return _idToTransformStateInfo[configId].Config;
        }

        public void UpdateFeatureStates(int lastUpdatedFrameId,
            bool disableProactiveEvaluation)
        {
            foreach (var transformStateInfo in _idToTransformStateInfo.Values)
            {
                var featureStateProvider = transformStateInfo.StateProvider;
                if (!disableProactiveEvaluation)
                {
                    featureStateProvider.LastUpdatedFrameId = lastUpdatedFrameId;
                    featureStateProvider.ReadTouchedFeatureStates();
                }
                else
                {
                    featureStateProvider.LastUpdatedFrameId = lastUpdatedFrameId;
                }
            }
        }
    }

    /// <summary>
    /// Provides state tracking and transition management for transform-based features in the Interaction SDK.
    /// This provider interprets transform values and quantizes them into discrete states while implementing
    /// hysteresis to prevent rapid state fluctuations.
    /// See <see cref="Oculus.Interaction.PoseDetection.TransformFeatureStateProvider"/> and <see cref="Oculus.Interaction.TransformFeatureStateProviderRef"/> for example implementations.
    /// </summary>
    public interface ITransformFeatureStateProvider
    {
        /// <summary>
        /// Checks if the specified feature is currently in the given state.
        /// </summary>
        /// <param name="config">The <see cref="TransformConfig"/> to use for the check</param>
        /// <param name="feature">The <see cref="TransformFeature"/> to check</param>
        /// <param name="mode">The <see cref="FeatureStateActiveMode"/> to use when checking the feature state</param>
        /// <param name="stateId">The ID of the state to compare against</param>
        /// <returns><c>true</c> if the feature is in the specified state; otherwise, <c>false</c></returns>
        bool IsStateActive(TransformConfig config, TransformFeature feature,
            FeatureStateActiveMode mode, string stateId);

        /// <summary>
        /// Retrieves the current state of a specified transform feature.
        /// </summary>
        /// <param name="config">The <see cref="TransformConfig"/> to use for the query</param>
        /// <param name="transformFeature">The <see cref="TransformFeature"/> to query</param>
        /// <param name="currentState">The current state of the transform feature</param>
        /// <returns><c>true</c> if the current state was successfully retrieved; otherwise, <c>false</c></returns>
        bool GetCurrentState(TransformConfig config, TransformFeature transformFeature,
            out string currentState);

        /// <summary>
        /// Registers a transform configuration with the state provider.
        /// </summary>
        /// <param name="transformConfig">The <see cref="TransformConfig"/> to register</param>
        void RegisterConfig(TransformConfig transformConfig);

        /// <summary>
        /// Unregisters a previously registered transform configuration from the state provider.
        /// </summary>
        /// <param name="transformConfig">The <see cref="TransformConfig"/> to unregister</param>
        void UnRegisterConfig(TransformConfig transformConfig);

        /// <summary>
        /// Retrieves the feature vector and wrist position for a specified transform feature.
        /// </summary>
        /// <param name="config">The <see cref="TransformConfig"/> to use for the query</param>
        /// <param name="transformFeature">The <see cref="TransformFeature"/> to query</param>
        /// <param name="isHandVector"><c>true</c> if the feature vector is a hand vector; otherwise, <c>false</c></param>
        /// <param name="featureVec">The feature vector</param>
        /// <param name="wristPos">The wrist position</param>
        void GetFeatureVectorAndWristPos(TransformConfig config,
            TransformFeature transformFeature, bool isHandVector, ref Vector3? featureVec,
            ref Vector3? wristPos);
    }

    /// <summary>
    /// Interprets transform feature values from a <see cref="TransformFeatureValueProvider"/>
    /// and uses the given <see cref="TransformFeatureStateThresholds"/> to quantize
    /// these values into states. To avoid rapid fluctuations at the edges
    /// of two states, this classes uses the calculated feature states from the previous
    /// frame and the given state thresholds to apply a buffer between
    /// state transition edges.
    /// </summary>
    public class TransformFeatureStateProvider : MonoBehaviour,
        ITransformFeatureStateProvider, ITimeConsumer
    {
        /// <summary>
        /// The hand to use for finger state data.
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        /// <summary>
        /// The Hmd component.
        /// </summary>
        [SerializeField, Interface(typeof(IHmd))]
        private UnityEngine.Object _hmd;
        public IHmd Hmd { get; private set; }

        /// <summary>
        /// The OVRInteraction prefab from the Interaction SDK Getting Started tutorial.
        /// </summary>
        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private UnityEngine.Object _trackingToWorldTransformer;

        public ITrackingToWorldTransformer TrackingToWorldTransformer { get; private set; }

        [Header("Advanced Settings")]
        [SerializeField]
        [Tooltip("If true, disables proactive evaluation of any TransformFeature that has been " +
                 "queried at least once. This will force lazy-evaluation of state within calls " +
                 "to IsStateActive, which means you must do so each frame to avoid missing " +
                 "transitions between states.")]
        private bool _disableProactiveEvaluation;

        private Func<float> _timeProvider = () => Time.time;
        public void SetTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        private TransformJointData _jointData = new TransformJointData();
        private TransformFeatureStateCollection _transformFeatureStateCollection;


        protected bool _started = false;

        protected virtual void Awake()
        {
            if (Hand == null)
            {
                Hand = _hand as IHand;
            }
            if (Hmd == null)
            {
                Hmd = _hmd as IHmd;
            }
            if (TrackingToWorldTransformer == null)
            {
                TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            }
            _transformFeatureStateCollection = new TransformFeatureStateCollection();
        }

        public void RegisterConfig(TransformConfig transformConfig)
        {
            //Register time provider indirectly in case reference changes
            Func<float> getTime = () => _timeProvider();
            _transformFeatureStateCollection.RegisterConfig(transformConfig, _jointData, getTime);
        }

        public void UnRegisterConfig(TransformConfig transformConfig)
        {
            _transformFeatureStateCollection.UnRegisterConfig(transformConfig);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(Hmd, nameof(Hmd));
            this.AssertField(_timeProvider, nameof(_timeProvider));
            this.AssertField(TrackingToWorldTransformer, nameof(TrackingToWorldTransformer));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandDataAvailable;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandDataAvailable;
            }
        }

        private void HandDataAvailable()
        {
            UpdateJointData();
            UpdateStateForHand();
        }

        private void UpdateJointData()
        {
            _jointData.IsValid = Hand.GetJointPose(HandJointId.HandWristRoot, out _jointData.WristPose) &&
                                 Hmd.TryGetRootPose(out _jointData.CenterEyePose);
            if (!_jointData.IsValid)
            {
                return;
            }

            _jointData.Handedness = Hand.Handedness;
            _jointData.TrackingSystemUp = TrackingToWorldTransformer.Transform.up;
            _jointData.TrackingSystemForward = TrackingToWorldTransformer.Transform.forward;
        }

        private void UpdateStateForHand()
        {
            // Update the frameId of all state providers to mark data as dirty. If
            // proactiveEvaluation is enabled, also read the state of any feature that has been
            // touched, which will force it to evaluate.
            _transformFeatureStateCollection.UpdateFeatureStates(
                Hand.CurrentDataVersion,
                _disableProactiveEvaluation);
        }

        public bool IsHandDataValid()
        {
            return _jointData.IsValid;
        }

        public bool IsStateActive(TransformConfig config, TransformFeature feature, FeatureStateActiveMode mode, string stateId)
        {
            var currentState = GetCurrentFeatureState(config, feature);
            switch (mode)
            {
                case FeatureStateActiveMode.Is:
                    return currentState == stateId;
                case FeatureStateActiveMode.IsNot:
                    return currentState != stateId;
                default:
                    return false;
            }
        }

        private string GetCurrentFeatureState(TransformConfig config,
            TransformFeature feature)
        {
            return _transformFeatureStateCollection.GetStateProvider(config).
                GetCurrentFeatureState(feature);
        }

        public bool GetCurrentState(TransformConfig config, TransformFeature transformFeature,
            out string currentState)
        {
            if (!IsHandDataValid())
            {
                currentState = default;
                return false;
            }

            currentState = GetCurrentFeatureState(config, transformFeature);
            return currentState != default;
        }

        /// <summary>
        /// Returns the current value of the feature. If the hand joints are not populated with
        /// valid data (for instance, due to a disconnected hand), the method will return null;
        /// </summary>
        public float? GetFeatureValue(TransformConfig config,
            TransformFeature transformFeature)
        {
            if (!IsHandDataValid())
            {
                return null;
            }

            return TransformFeatureValueProvider.GetValue(transformFeature,
                _jointData, config);
        }

        public void GetFeatureVectorAndWristPos(TransformConfig config,
            TransformFeature transformFeature, bool isHandVector, ref Vector3? featureVec,
            ref Vector3? wristPos)
        {
            featureVec = null;
            wristPos = null;
            if (!IsHandDataValid())
            {
                return;
            }

            featureVec = isHandVector ?
                         TransformFeatureValueProvider.GetHandVectorForFeature(transformFeature,
                            _jointData) :
                         TransformFeatureValueProvider.GetTargetVectorForFeature(transformFeature,
                            _jointData, in config);
            wristPos = _jointData.WristPose.position;
        }

        #region Inject
        public void InjectAllTransformFeatureStateProvider(IHand hand, IHmd hmd, bool disableProactiveEvaluation)
        {
            InjectHand(hand);
            InjectHmd(hmd);
            _disableProactiveEvaluation = disableProactiveEvaluation;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectHmd(IHmd hand)
        {
            _hmd = hand as UnityEngine.Object;
            Hmd = hand;
        }

        public void InjectDisableProactiveEvaluation(bool disabled)
        {
            _disableProactiveEvaluation = disabled;
        }

        [Obsolete("Use SetTimeProvider()")]
        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }
        #endregion
    }
}
