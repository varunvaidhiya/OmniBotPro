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

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Asset characterizing a "hand shape" which can be recognized.
    /// </summary>
    /// <remarks>
    /// Despite its name, this type does not actively recognize anything but merely stores the data, in the form
    /// of per-finger <see cref="FingerFeatureConfigList"/>s, which is needed for a
    /// <see cref="ShapeRecognizerActiveState"/> to perform the calculations needed to determine whether the
    /// described hand shape has been recognized or not. In this sense, ShapeRecognizer might be thought of as
    /// a definition or descriptor of a hand shape.
    /// </remarks>
    [CreateAssetMenu(menuName = "Meta/Interaction SDK/Pose Detection/Shape")]
    public class ShapeRecognizer : ScriptableObject
    {
        /// <summary>
        /// A serializable list of <see cref="FingerFeatureConfig"/>s, implicitly all associated with a single
        /// finger.
        /// </summary>
        /// <remarks>
        /// Each FingerFeatureConfig describes only one aspect (<see cref="FingerFeature"/>) of a finger, meaning
        /// multiple can be applicable at the same time; for example, the index finger in the "scissors" hand
        /// shape (as in "rock, paper, scissors") must have both low <see cref="FingerFeature.Curl"/> and high
        /// <see cref="FingerFeature.Abduction"/>. A FingerFeatureConfigList can thus be thought of as a list of
        /// requirements, all of which must be satisfied in order for a finger's shape to be acceptable for
        /// recognition.
        /// </remarks>
        [Serializable]
        public class FingerFeatureConfigList
        {
            [SerializeField]
            private List<FingerFeatureConfig> _value;

            /// <summary>
            /// The list of <see cref="FingerFeatureConfig"/>s which collectively describe the requirements for a
            /// specific finger in a <see cref="ShapeRecognizer"/>.
            /// </summary>
            /// <remarks>
            /// For an overview of the role of this list in shape recognition, see the remarks on
            /// <see cref="FingerFeatureConfigList"/>.
            /// </remarks>
            public IReadOnlyList<FingerFeatureConfig> Value => _value;

            /// <summary>
            /// Constructs a FingerFeatureConfigList with an empty <see cref="Value"/> list. Because that list
            /// is the set of all requirements which must be met for a finger to be acceptable in a ShapeRecognizer,
            /// an empty list indicates that there are no requirements and the finger is always acceptable.
            /// </summary>
            public FingerFeatureConfigList() { }

            /// <summary>
            /// Constructs a FingerFeatureConfigList with the provided <paramref name="value"/> to be assigned to
            /// <see cref="Value"/>.
            /// </summary>
            /// <param name="value">The list of required <see cref="FingerFeatureConfig"/>s</param>
            public FingerFeatureConfigList(List<FingerFeatureConfig> value)
            {
                _value = value;
            }
        }

        /// <summary>
        /// Specializes <see cref="FeatureConfigBase{TFeature}"/> (which simply stores information related to the
        /// state of a feature) for <see cref="FingerFeature"/>s. This is used by <see cref="ShapeRecognizerActiveState"/>
        /// and other consumers of ShapeRecognizer to assess whether individual fingers are acceptably posed for
        /// recognition.
        /// </summary>
        [Serializable]
        public class FingerFeatureConfig : FeatureConfigBase<FingerFeature>
        {
        }

        [SerializeField]
        private string _shapeName;

        [SerializeField]
        private FingerFeatureConfigList _thumbFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _indexFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _middleFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _ringFeatureConfigs = new FingerFeatureConfigList();
        [SerializeField]
        private FingerFeatureConfigList _pinkyFeatureConfigs = new FingerFeatureConfigList();

        /// <summary>
        /// The list of <see cref="FingerFeatureConfig"/>s which must be satisfied for the thumb to be acceptable
        /// for recognition by this ShapeRecognizer.
        /// </summary>
        /// <remarks>
        /// For an overview of the role of this list in shape recognition, see the remarks on
        /// <see cref="FingerFeatureConfigList"/>.
        /// </remarks>
        public IReadOnlyList<FingerFeatureConfig> ThumbFeatureConfigs => _thumbFeatureConfigs.Value;

        /// <summary>
        /// The list of <see cref="FingerFeatureConfig"/>s which must be satisfied for the index finger to be acceptable
        /// for recognition by this ShapeRecognizer.
        /// </summary>
        /// <remarks>
        /// For an overview of the role of this list in shape recognition, see the remarks on
        /// <see cref="FingerFeatureConfigList"/>.
        /// </remarks>
        public IReadOnlyList<FingerFeatureConfig> IndexFeatureConfigs => _indexFeatureConfigs.Value;

        /// <summary>
        /// The list of <see cref="FingerFeatureConfig"/>s which must be satisfied for the middle finger to be acceptable
        /// for recognition by this ShapeRecognizer.
        /// </summary>
        /// <remarks>
        /// For an overview of the role of this list in shape recognition, see the remarks on
        /// <see cref="FingerFeatureConfigList"/>.
        /// </remarks>
        public IReadOnlyList<FingerFeatureConfig> MiddleFeatureConfigs => _middleFeatureConfigs.Value;

        /// <summary>
        /// The list of <see cref="FingerFeatureConfig"/>s which must be satisfied for the ring finger to be acceptable
        /// for recognition by this ShapeRecognizer.
        /// </summary>
        /// <remarks>
        /// For an overview of the role of this list in shape recognition, see the remarks on
        /// <see cref="FingerFeatureConfigList"/>.
        /// </remarks>
        public IReadOnlyList<FingerFeatureConfig> RingFeatureConfigs => _ringFeatureConfigs.Value;

        /// <summary>
        /// The list of <see cref="FingerFeatureConfig"/>s which must be satisfied for the pinky finger to be acceptable
        /// for recognition by this ShapeRecognizer.
        /// </summary>
        /// <remarks>
        /// For an overview of the role of this list in shape recognition, see the remarks on
        /// <see cref="FingerFeatureConfigList"/>.
        /// </remarks>
        public IReadOnlyList<FingerFeatureConfig> PinkyFeatureConfigs => _pinkyFeatureConfigs.Value;

        /// <summary>
        /// The human-readable name assigned to this hand shape.
        /// </summary>
        public string ShapeName => _shapeName;

        /// <summary>
        /// Retrieves the list of <see cref="FingerFeatureConfig"/>s which must be satisfied for the provided
        /// <paramref name="finger"/> to be acceptable for recognition by this ShapeRecognizer. This is a
        /// equivalent to calling <see cref="ThumbFeatureConfigs"/>/<see cref="IndexFeatureConfigs"/>/etc. for the
        /// finger in question.
        /// </summary>
        /// <param name="finger">The finger for which to retrieve the list of required <see cref="FingerFeatureConfig"/>s.</param>
        /// <returns>The list of required <see cref="FingerFeatureConfig"/>s for <paramref name="finger"/>.</returns>
        public IReadOnlyList<FingerFeatureConfig> GetFingerFeatureConfigs(HandFinger finger)
        {
            switch (finger)
            {
                case HandFinger.Thumb:
                    return ThumbFeatureConfigs;
                case HandFinger.Index:
                    return IndexFeatureConfigs;
                case HandFinger.Middle:
                    return MiddleFeatureConfigs;
                case HandFinger.Ring:
                    return RingFeatureConfigs;
                case HandFinger.Pinky:
                    return PinkyFeatureConfigs;
                default:
                    throw new ArgumentException("must be a HandFinger enum value",
                        nameof(finger));
            }
        }

        /// <summary>
        /// Enumerates the required <see cref="FingerFeatureConfig"/>s for each finger in this ShapeRecognizer.
        /// This is a convenience method equivalent to calling <see cref="GetFingerFeatureConfigs(HandFinger)"/>
        /// for every finger in turn.
        /// </summary>
        /// <returns>
        /// An iterator exposing, in turn, each finger and its associated <see cref="FingerFeatureConfig"/>s
        /// as a tuple.
        /// </returns>
        public IEnumerable<ValueTuple<HandFinger, IReadOnlyList<FingerFeatureConfig>>>
            GetFingerFeatureConfigs()
        {
            for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
            {
                HandFinger finger = (HandFinger)fingerIdx;
                var configs = GetFingerFeatureConfigs(finger);
                if (configs.Count == 0)
                {
                    continue;
                }

                yield return new ValueTuple<HandFinger, IReadOnlyList<FingerFeatureConfig>>(finger,
                    configs);
            }
        }

        #region Inject
        /// <summary>
        /// Sets the <see cref="FingerFeatureConfig"/>s for all fingers for a dynamically instantiated ShapeRecognizer. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectAllShapeRecognizer(IDictionary<HandFinger, FingerFeatureConfig[]> fingerFeatureConfigs)
        {
            FingerFeatureConfigList ReadFeatureConfigs(HandFinger finger)
            {
                if (!fingerFeatureConfigs.TryGetValue(finger, out FingerFeatureConfig[] configs))
                {
                    configs = Array.Empty<FingerFeatureConfig>();
                }

                return new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
            }

            _thumbFeatureConfigs = ReadFeatureConfigs(HandFinger.Thumb);
            _indexFeatureConfigs = ReadFeatureConfigs(HandFinger.Index);
            _middleFeatureConfigs = ReadFeatureConfigs(HandFinger.Middle);
            _ringFeatureConfigs = ReadFeatureConfigs(HandFinger.Ring);
            _pinkyFeatureConfigs = ReadFeatureConfigs(HandFinger.Pinky);
        }

        /// <summary>
        /// Sets the <see cref="FingerFeatureConfig"/>s for the thumb for a dynamically instantiated ShapeRecognizer. This method
        /// exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectThumbFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _thumbFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        /// <summary>
        /// Sets the <see cref="FingerFeatureConfig"/>s for the index finger for a dynamically instantiated ShapeRecognizer. This
        /// method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectIndexFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _indexFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        /// <summary>
        /// Sets the <see cref="FingerFeatureConfig"/>s for the middle finger for a dynamically instantiated ShapeRecognizer. This
        /// method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectMiddleFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _middleFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        /// <summary>
        /// Sets the <see cref="FingerFeatureConfig"/>s for the ring finger for a dynamically instantiated ShapeRecognizer. This
        /// method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectRingFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _ringFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        /// <summary>
        /// Sets the <see cref="FingerFeatureConfig"/>s for the pinky finger for a dynamically instantiated ShapeRecognizer. This
        /// method exists to support Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based
        /// usage.
        /// </summary>
        public void InjectPinkyFeatureConfigs(FingerFeatureConfig[] configs)
        {
            _pinkyFeatureConfigs = new FingerFeatureConfigList(new List<FingerFeatureConfig>(configs));
        }

        /// <summary>
        /// Sets the <see cref="ShapeName"/> for a dynamically instantiated ShapeRecognizer. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectShapeName(string shapeName)
        {
            _shapeName = shapeName;
        }
        #endregion
    }
}
