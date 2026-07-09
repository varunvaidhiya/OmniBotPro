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
    /// <summary>
    /// Used during hand pose detection to compare the current state of a hand's fingers to the state
    /// required by a given shape. The shape's required state is defined in a <see cref="ShapeRecognizer"/>.
    /// If the two match, this state becomes active.
    /// </summary>
    public class ShapeRecognizerActiveState : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// Provides the current state of the tracked hand's fingers.
        /// </summary>
        [SerializeField, Interface(typeof(IFingerFeatureStateProvider))]
        private UnityEngine.Object _fingerFeatureStateProvider;
        protected IFingerFeatureStateProvider FingerFeatureStateProvider;

        /// <summary>
        /// A list of shape configs that define the pose. The states in these shape configs are compared to the finger states from the <cref="IFingerFeatureStateProvider" />.
        /// </summary>
        [SerializeField]
        private ShapeRecognizer[] _shapes;

        /// <summary>
        /// The list of <see cref="ShapeRecognizer"/>s which define the satisfactory shapes for <see cref="Hand"/> to adopt
        /// in order for <see cref="Active"/> to become true.
        /// </summary>
        public IReadOnlyList<ShapeRecognizer> Shapes => _shapes;

        struct FingerFeatureStateUsage
        {
            public HandFinger handFinger;
            public ShapeRecognizer.FingerFeatureConfig config;
        }

        private List<FingerFeatureStateUsage> _allFingerStates = new List<FingerFeatureStateUsage>();

        // keeps track of native state
        private bool _nativeActive = false;

        protected virtual void Awake()
        {
            if (FingerFeatureStateProvider == null)
            {
                FingerFeatureStateProvider = _fingerFeatureStateProvider as IFingerFeatureStateProvider;
            }
        }

        protected virtual void Start()
        {
            this.AssertField(FingerFeatureStateProvider, nameof(FingerFeatureStateProvider));
            this.AssertCollectionField(_shapes, nameof(_shapes));

            _allFingerStates = FlattenUsedFeatures();

            // Warm up the proactive evaluation
            InitStateProvider();
        }

        private void InitStateProvider()
        {
            foreach (FingerFeatureStateUsage state in _allFingerStates)
            {
                FingerFeatureStateProvider.GetCurrentState(state.handFinger, state.config.Feature, out _);
            }
        }

        private List<FingerFeatureStateUsage> FlattenUsedFeatures()
        {
            var fingerFeatureStateUsages = new List<FingerFeatureStateUsage>();
            foreach (var sr in _shapes)
            {
                int configCount = 0;
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
                {
                    var handFinger = (HandFinger)fingerIdx;
                    foreach (var config in sr.GetFingerFeatureConfigs(handFinger))
                    {
                        ++configCount;
                        fingerFeatureStateUsages.Add(new FingerFeatureStateUsage()
                        {
                            handFinger = handFinger,
                            config = config
                        });
                    }
                }

                // If this assertion is hit, open the ScriptableObject in the Unity Inspector
                // and ensure that it has at least one valid condition.
                Assert.IsTrue(configCount > 0, $"Shape {sr.ShapeName} has no valid conditions.");
            }

            return fingerFeatureStateUsages;
        }

        /// <summary>
        /// Implementation of <see cref="IActiveState.Active"/>, in this case indicating whether the associated
        /// <see cref="Hand"/> is currently adopting any of the <see cref="Shapes"/> specified for recognition.
        /// </summary>
        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled || _allFingerStates.Count == 0)
                {
                    return (_nativeActive = false);
                }

                foreach (FingerFeatureStateUsage stateUsage in _allFingerStates)
                {
                    if (!FingerFeatureStateProvider.IsStateActive(stateUsage.handFinger,
                        stateUsage.config.Feature, stateUsage.config.Mode, stateUsage.config.State))
                    {
                        return (_nativeActive = false);
                    }
                }

                if (!_nativeActive)
                {
                    // Activate native component
                    int result = NativeMethods.isdk_NativeComponent_Activate(0x48506f7365446574);
                    this.AssertIsTrue(result == NativeMethods.IsdkSuccess, "Unable to Activate native recognizer!");
                }

                return (_nativeActive = true);
            }
        }

        #region Inject
        /// <summary>
        /// Sets all required dependencies for a dynamically instantiated ShapeRecognizerActiveState. This is a convenience
        /// method which wraps invocations of <see cref="InjectFingerFeatureStateProvider(IFingerFeatureStateProvider)"/>,
        /// and <see cref="InjectShapes(ShapeRecognizer[])"/>.
        /// This method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectAllShapeRecognizerActiveState(
            IFingerFeatureStateProvider fingerFeatureStateProvider,
            ShapeRecognizer[] shapes)
        {
            InjectFingerFeatureStateProvider(fingerFeatureStateProvider);
            InjectShapes(shapes);
        }

        /// <summary>
        /// Sets an <see cref="IFingerFeatureStateProvider"/> as the provider for a dynamically instantiated ShapeRecognizerActiveState. This
        /// method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectFingerFeatureStateProvider(IFingerFeatureStateProvider fingerFeatureStateProvider)
        {
            _fingerFeatureStateProvider = fingerFeatureStateProvider as UnityEngine.Object;
            FingerFeatureStateProvider = fingerFeatureStateProvider;
        }

        /// <summary>
        /// Sets a list of <see cref="ShapeRecognizer"/>s as the recognizable <see cref="Shapes"/> for a dynamically instantiated
        /// ShapeRecognizerActiveState. This method exists to support Interaction SDK's dependency injection pattern and is not needed for
        /// typical Unity Editor-based usage.
        /// </summary>
        public void InjectShapes(ShapeRecognizer[] shapes)
        {
            _shapes = shapes;
        }
        #endregion
    }
}
