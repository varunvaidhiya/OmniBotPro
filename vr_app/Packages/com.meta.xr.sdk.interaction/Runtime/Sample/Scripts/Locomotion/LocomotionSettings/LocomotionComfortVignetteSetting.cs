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

using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Locomotion setting that controls the vignette strength when different
    /// locomotion events occur. You can select between Turning, Acceleration and
    /// linear movement and it will apply the selected AnimationCurve when the
    /// referenced toggle is activated
    /// </summary>
    [System.Obsolete("Use " + nameof(ComfortVignetteSetting) + " instead")]
    public class LocomotionComfortVignetteSetting : MonoBehaviour
    {
        /// <summary>
        /// Types of movement that will trigger
        /// the comfort vignette
        /// </summary>
        public enum ComfortType
        {
            Turning,
            Accelerating,
            Moving
        }

        /// <summary>
        /// UI Toggle that will trigger setting the comfort vignette
        /// </summary>
        [SerializeField]
        private Toggle _toggle;
        /// <summary>
        /// Type of movement for which to set the animation curve
        /// </summary>
        [SerializeField]
        private ComfortType _comfortType;
        /// <summary>
        /// Curve indicating the FOV per amount of movement that
        /// will be applied to the tunneling component
        /// </summary>
        [SerializeField]
        private AnimationCurve _curve;
        /// <summary>
        /// Component that detects the motion and applies the tunneling
        /// effect.
        /// </summary>
        [SerializeField]
        private LocomotionTunneling _tunneling;

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_toggle, nameof(_toggle));
            this.AssertField(_curve, nameof(_curve));
            this.AssertField(_tunneling, nameof(_tunneling));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _toggle.onValueChanged.AddListener(InjectCurve);
                InjectCurve(_toggle.isOn);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _toggle.onValueChanged.RemoveListener(InjectCurve);
            }
        }

        private void InjectCurve(bool inject)
        {
            if (!inject)
            {
                return;
            }

            switch (_comfortType)
            {
                case ComfortType.Turning:
                    _tunneling.RotationStrength = _curve;
                    break;
                case ComfortType.Accelerating:
                    _tunneling.AccelerationStrength = _curve;
                    break;
                case ComfortType.Moving:
                    _tunneling.MovementStrength = _curve;
                    break;
            }
        }

        #region Injects

        public void InjectAllComfortOption(ComfortType comfortType, Toggle toggle, AnimationCurve curve, LocomotionTunneling tunneling)
        {
            InjectComfortType(comfortType);
            InjectToggle(toggle);
            InjectCurve(curve);
            InjectTunneling(tunneling);
        }

        public void InjectComfortType(ComfortType comfortType)
        {
            _comfortType = comfortType;
        }

        public void InjectToggle(Toggle toggle)
        {
            _toggle = toggle;
        }

        public void InjectCurve(AnimationCurve curve)
        {
            _curve = curve;
        }

        public void InjectTunneling(LocomotionTunneling tunneling)
        {
            _tunneling = tunneling;
        }

        #endregion
    }
}
