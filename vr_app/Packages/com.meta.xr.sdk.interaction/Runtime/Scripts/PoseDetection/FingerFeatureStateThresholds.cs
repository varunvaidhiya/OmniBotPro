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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    [Serializable]
    public class FingerFeatureStateThreshold : IFeatureStateThreshold<string>
    {
        public FingerFeatureStateThreshold() { }

        public FingerFeatureStateThreshold(float thresholdMidpoint,
            float thresholdWidth,
            string firstState,
            string secondState)
        {
            _thresholdMidpoint = thresholdMidpoint;
            _thresholdWidth = thresholdWidth;
            _firstState = firstState;
            _secondState = secondState;
        }
        /// <summary>
        /// The value at which a state will transition from A to B or B to A.
        /// </summary>
        [SerializeField]
        [Tooltip(FingerFeatureProperties.FeatureStateThresholdMidpointHelpText)]
        private float _thresholdMidpoint;
        /// <summary>
        /// How far the value must exceed the midpoint until the transition can occur. This is to prevent rapid flickering at transition edges.
        /// </summary>
        [SerializeField]
        [Tooltip(FingerFeatureProperties.FeatureStateThresholdWidthHelpText)]
        private float _thresholdWidth;
        [SerializeField]
        [Tooltip("State to transition to when value passes below the threshold")]
        private string _firstState;
        [SerializeField]
        [Tooltip("State to transition to when value passes above the threshold")]
        private string _secondState;

        public float ThresholdMidpoint => _thresholdMidpoint;
        public float ThresholdWidth => _thresholdWidth;
        public float ToFirstWhenBelow => _thresholdMidpoint - _thresholdWidth * 0.5f;
        public float ToSecondWhenAbove => _thresholdMidpoint + _thresholdWidth * 0.5f;
        public string FirstState => _firstState;
        public string SecondState => _secondState;
    }

    [Serializable]
    public class FingerFeatureThresholds : IFeatureStateThresholds<FingerFeature, string>
    {
        public FingerFeatureThresholds() { }

        public FingerFeatureThresholds(FingerFeature feature,
            IEnumerable<FingerFeatureStateThreshold> thresholds)
        {
            _feature = feature;
            _thresholds = new List<FingerFeatureStateThreshold>(thresholds);
        }

        [SerializeField]
        [Tooltip("Which feature this collection of thresholds controls. " +
            "Each feature should exist at most once.")]
        private FingerFeature _feature;

        [SerializeField]
        [Tooltip("List of state transitions, with thresold settings. " +
            "The entries in this list must be in ascending order, based on their 'midpoint' values.")]
        private List<FingerFeatureStateThreshold> _thresholds;

        public FingerFeature Feature => _feature;
        public IReadOnlyList<IFeatureStateThreshold<string>> Thresholds => _thresholds;
    }

    /// <summary>
    /// A ScriptableObject that defines the state thresholds for each finger feature.
    /// A state threshold is a set of boundaries that determine when a finger has transitioned between states.
    /// For example, the curl feature has 3 states: Open, Neutral, and Closed. So the state thresholds for curl use an angle in degrees to define when the finger's state has changed from Open to Neutral, Neutral to Closed, or vice-versa.
    /// </summary>
    [CreateAssetMenu(menuName = "Meta/Interaction SDK/Pose Detection/Finger Thresholds")]
    public class FingerFeatureStateThresholds : ScriptableObject,
        IFeatureThresholds<FingerFeature, string>
    {
        [SerializeField]
        [Tooltip("List of all supported finger features, along with the state entry/exit thresholds.")]
        private List<FingerFeatureThresholds> _featureThresholds;

        /// <summary>
        /// How long the value must be in the new state before the feature will actually change to that state.
        /// This is to prevent rapid flickering at transition edges. This value applies to all features.
        /// </summary>
        [SerializeField]
        [Tooltip("Length of time that the finger must be in the new state before the feature " +
                 "state provider will use the new value.")]
        private double _minTimeInState;

        public void Construct(List<FingerFeatureThresholds> featureThresholds,
            double minTimeInState)
        {
            _featureThresholds = featureThresholds;
            _minTimeInState = minTimeInState;
        }

        public IReadOnlyList<IFeatureStateThresholds<FingerFeature, string>>
            FeatureStateThresholds => _featureThresholds;

        public double MinTimeInState => _minTimeInState;
    }
}
